using Microsoft.EntityFrameworkCore;
using TIKR.Api;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.SyncfusionDocuments;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;
using Serilog;
using Serilog.Events;

// Operational structured logging: console + rolling file.
// Docker/NAS uses /data/logs; Mac dev falls back to .local-data/logs (or TIKR_LOG_PATH).
var logDir = ResolveLogDirectory();
var logFile = Path.Combine(logDir, "tikr-.log");
const string consoleTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
const string fileTemplate =
    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TIKR")
    .WriteTo.Console(outputTemplate: consoleTemplate)
    .WriteTo.File(logFile,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        outputTemplate: fileTemplate)
    .CreateBootstrapLogger();

Log.Information("TIKR.Api bootstrap — log directory: {LogDir}", logDir);

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvLoader.LoadDevelopmentEnv(builder.Environment.ContentRootPath);
else
    EnvLoader.LoadRuntimeSecrets(builder.Configuration["TIKR_DATA_PATH"]);

builder.Configuration.AddEnvironmentVariables();

// Cancellation of background pollers during failed bind must not drown out the root cause.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddTikrInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// preserveStaticLogger: WebApplicationFactory hosts re-enter Program; freezing the
// bootstrap logger a second time throws "The logger is already frozen."
builder.Host.UseSerilog((ctx, _, configuration) =>
{
    configuration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TIKR")
        .WriteTo.Console(outputTemplate: consoleTemplate)
        .WriteTo.File(logFile,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true,
            outputTemplate: fileTemplate);
}, preserveStaticLogger: true);

WebApplication app;
try
{
    app = builder.Build();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TIKR.Api failed during service build (DI / configuration). Root: {Root}", RootCause(ex));
    throw;
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
LogStartupDiagnostics(app, logger, logDir);

SyncfusionLicenseBootstrap.RegisterIfConfigured(app.Configuration, logger, "Document SDK");
SyncfusionDocumentLicense.RegisterFromConfiguration(app.Configuration);

var authEnabled = TikrConfiguration.IsAuthEnabled(app.Configuration);

try
{
    await app.Services.InitializeDatabaseAsync();
    logger.LogInformation("Database initialized successfully");
}
catch (Exception ex)
{
    logger.LogCritical(ex,
        "Database initialization failed. Root: {Root}. Check ConnectionStrings__Default and that the SQLite directory is writable.",
        RootCause(ex));
    throw;
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.Use(async (ctx, next) =>
    {
        // Viewer is read-only on /api mutations (except auth self-service).
        if (HttpMethods.IsGet(ctx.Request.Method)
            || HttpMethods.IsHead(ctx.Request.Method)
            || HttpMethods.IsOptions(ctx.Request.Method))
        {
            await next();
            return;
        }

        var path = ctx.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/reset-password", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (ctx.User.Identity?.IsAuthenticated == true
            && !ctx.User.IsInRole(TikrRoles.Admin)
            && !ctx.User.IsInRole(TikrRoles.Clerk)
            && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { error = "Viewer role is read-only." });
            return;
        }

        await next();
    });
}

app.UseCors();

// Request logging — every button-driven HTTP call is visible here (method, path, status, elapsed).
// Domain services add "Action {name} started/completed" for behind-the-scenes work.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath}{QueryString} → {StatusCode} in {Elapsed:0.0} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : httpContext.Request.Method is "GET" or "HEAD"
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("QueryString", httpContext.Request.QueryString.HasValue
            ? httpContext.Request.QueryString.Value
            : string.Empty);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("ContentType", httpContext.Request.ContentType ?? string.Empty);
    };
});

if (authEnabled)
    app.MapAuthEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

var api = app.MapGroup("/api");
if (authEnabled)
    api.RequireAuthorization(TikrAuthPolicies.Authenticated);

api.MapGet("/system/local-status", async (IConfiguration config, IHybridAiService ai, FeatureSettingsState settings) =>
{
    var snap = settings.Current;
    var town = snap.TownName;
    var storageLabel = snap.StorageLabel;
    DateTime? dataModified = null;

    if (TryGetSqlitePath(config.GetConnectionString("Default"), out var dbPath) && File.Exists(dbPath))
        dataModified = File.GetLastWriteTimeUtc(dbPath);

    var aiStatus = await ai.GetStatusAsync();
    return Results.Ok(new LocalStorageStatusDto(town, storageLabel, dataModified, aiStatus.OllamaAvailable));
});

api.MapGet("/system/document-sdk-status", (IConfiguration config, FeatureSettingsState settings) =>
{
    // Prefer clerk Settings toggles for agent flags; license still from env/runtime secrets.
    var status = SyncfusionDocumentLicense.GetStatus(config);
    return Results.Ok(status with
    {
        AgentToolsEnabled = settings.Current.UseSyncfusionAgentTools,
        OrchestrationEnabled = settings.Current.UseSyncfusionAgentOrchestration
            && settings.Current.UseSyncfusionAgentTools
    });
});

api.MapPost("/email/ingest", async (IEmailIngestionService ingestion) =>
{
    if (!ingestion.IsConfigured)
        return Results.BadRequest(new { error = "Set TIKR_EMAIL_INBOX_PATH to enable forward-to-folder ingestion." });

    var result = await ingestion.IngestPendingAsync();
    return Results.Ok(result);
});

api.MapGet("/library/scan-status", (ILibraryScanService scanner) =>
    Results.Ok(scanner.GetStatus()));

api.MapPost("/library/scan", async (ILibraryScanService scanner) =>
{
    if (!scanner.IsConfigured)
        return Results.BadRequest(new { error = "Set TIKR_LIBRARY_SCAN_PATH to enable NAS library scan." });

    var result = await scanner.ScanAsync();
    return Results.Ok(result);
});

// Requirements
api.MapGet("/requirements", async (TikrDbContext db) =>
{
    var items = await db.Requirements.OrderBy(r => r.DueDate).ToListAsync();
    var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
    return items.Select(r => CouncilPacketEndpoints.MapRequirement(r, links.GetValueOrDefault(r.Id, []))).ToList();
});

api.MapGet("/requirements/{id:guid}", async (Guid id, TikrDbContext db) =>
{
    var item = await db.Requirements.FindAsync(id);
    if (item is null)
        return Results.NotFound();

    var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
    return Results.Ok(CouncilPacketEndpoints.MapRequirement(item, links.GetValueOrDefault(item.Id, [])));
});

api.MapPost("/requirements", async (CreateRequirementRequest request, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser, IRequirementService requirementService) =>
{
    var entity = await requirementService.CreateAsync(request, audit, currentUser);
    return Results.Created(
        $"/api/requirements/{entity.Id}",
        CouncilPacketEndpoints.MapRequirement(entity, []));
});

api.MapPost("/requirements/{id:guid}/documents", async (
    Guid id,
    LinkRequirementDocumentRequest request,
    TikrDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser,
    IRequirementService requirementService) =>
{
    try
    {
        await requirementService.LinkDocumentAsync(id, request.DocumentId, audit, currentUser);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    var requirement = await db.Requirements.FindAsync(id);
    if (requirement is null) return Results.NotFound();
    var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
    return Results.Ok(CouncilPacketEndpoints.MapRequirement(requirement, links.GetValueOrDefault(id, [])));
});

api.MapDelete("/requirements/{id:guid}/documents/{documentId:guid}", async (
    Guid id,
    Guid documentId,
    TikrDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser,
    IRequirementService requirementService) =>
{
    try
    {
        await requirementService.UnlinkDocumentAsync(id, documentId, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapPut("/requirements/{id:guid}", async (Guid id, UpdateRequirementRequest request, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser, IRequirementService requirementService) =>
{
    try
    {
        var entity = await requirementService.UpdateAsync(id, request, audit, currentUser);
        var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
        return Results.Ok(CouncilPacketEndpoints.MapRequirement(entity, links.GetValueOrDefault(entity.Id, [])));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapDelete("/requirements/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser, IRequirementService requirementService) =>
{
    try
    {
        await requirementService.DeleteAsync(id, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Documents
api.MapGet("/documents", async (TikrDbContext db, string? q, bool deleted = false) =>
{
    // deleted=false → active library; deleted=true → recycle bin.
    var query = deleted
        ? db.Documents.Where(d => d.DeletedAt != null)
        : db.Documents.Where(d => d.DeletedAt == null);
    if (!string.IsNullOrWhiteSpace(q))
    {
        query = query.Where(d =>
            d.FileName.Contains(q) ||
            (d.FullTextContent != null && d.FullTextContent.Contains(q)) ||
            (d.AiTags != null && d.AiTags.Contains(q)));
    }

    var items = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
    var ids = items.Select(d => d.Id).ToList();
    var reqCounts = await db.RequirementDocuments
        .Where(rd => ids.Contains(rd.DocumentId))
        .GroupBy(rd => rd.DocumentId)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.Count);
    var verCounts = await db.DocumentVersions
        .Where(v => ids.Contains(v.DocumentId))
        .GroupBy(v => v.DocumentId)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.Count);

    return items.Select(d => MapDocument(
        d,
        reqCounts.GetValueOrDefault(d.Id),
        verCounts.GetValueOrDefault(d.Id))).ToList();
});

api.MapPost("/documents", async (HttpRequest request, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser, IDocumentService documentService) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null) return Results.BadRequest("No file uploaded.");
    if (file.Length > 100 * 1024 * 1024) return Results.BadRequest("File too large (max 100MB).");
    if (string.IsNullOrWhiteSpace(file.FileName)) return Results.BadRequest("Invalid filename.");

    var isTransient = bool.TryParse(form["isTransient"], out var transientFlag) && transientFlag;

    // Delegate to centralized DocumentService (thin endpoint)
    try
    {
        await using var fileStream = file.OpenReadStream();
        var entity = await documentService.UploadAsync(
            fileStream, file.FileName, file.ContentType, file.Length, storage, audit, currentUser,
            isTransient: isTransient);
        return Results.Created($"/api/documents/{entity.Id}", MapDocument(entity));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);  // or Results.Problem for better
    }
});

api.MapGet("/documents/{id:guid}/content", async (Guid id, TikrDbContext db, IFileStorageService storage, ILogger<Program> endpointLog) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null)
    {
        endpointLog.LogWarning("Action {Action} {Phase} DocumentId={DocumentId}", "Document.Content", "not_found", id);
        return Results.NotFound();
    }

    endpointLog.LogInformation(
        "Action {Action} {Phase} DocumentId={DocumentId} FileName={FileName} Bytes={Bytes}",
        "Document.Content", "started", id, entity.FileName, entity.FileSizeBytes);

    var stream = await storage.OpenReadAsync(entity.StoragePath);
    return Results.File(stream, entity.ContentType ?? "application/octet-stream", entity.FileName);
});

api.MapPut("/documents/{id:guid}/content", async (
    Guid id,
    HttpRequest request,
    IFileStorageService storage,
    IAuditService audit,
    ICurrentUserService currentUser,
    IDocumentService documentService,
    IHybridAiService hybridAi,
    ILogger<Program> endpointLog) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null) return Results.BadRequest("No file uploaded.");
    if (file.Length > 100 * 1024 * 1024) return Results.BadRequest("File too large (max 100MB).");

    try
    {
        await using var fileStream = file.OpenReadStream();
        var entity = await documentService.ReplaceContentAsync(
            id, fileStream, file.ContentType, file.Length, storage, audit, currentUser);

        // Best-effort re-index so Assistant RAG reflects saved edits (Syncfusion save-back).
        try
        {
            await hybridAi.EmbedDocumentAsync(id);
        }
        catch (Exception ex)
        {
            endpointLog.LogWarning(ex, "Post-save embed failed for document {DocumentId}", id);
        }

        return Results.Ok(MapDocument(entity));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

api.MapPatch("/documents/{id:guid}", async (
    Guid id,
    UpdateDocumentMetadataRequest request,
    IDocumentService documentService,
    IAuditService audit,
    ICurrentUserService currentUser) =>
{
    try
    {
        // ClearSuggestedFolder wins; otherwise update folder when SuggestedFolder was sent.
        var shouldUpdateFolder = request.ClearSuggestedFolder || request.SuggestedFolder is not null;
        var folderValue = request.ClearSuggestedFolder ? null : request.SuggestedFolder;
        var entity = await documentService.UpdateMetadataAsync(
            id,
            request.FileName,
            folderValue,
            shouldUpdateFolder,
            audit,
            currentUser);
        return Results.Ok(MapDocument(entity));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

api.MapDelete("/documents/{id:guid}", async (Guid id, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser, IDocumentService documentService) =>
{
    try
    {
        // Soft-delete → recycle bin (recoverable). Use /purge for permanent remove.
        await documentService.DeleteAsync(id, storage, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapPost("/documents/{id:guid}/restore", async (
    Guid id, IDocumentService documentService, IAuditService audit, ICurrentUserService currentUser) =>
{
    try
    {
        await documentService.RestoreAsync(id, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapDelete("/documents/{id:guid}/purge", async (
    Guid id, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser, IDocumentService documentService) =>
{
    try
    {
        await documentService.PurgeAsync(id, storage, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapGet("/documents/{id:guid}/versions", async (Guid id, IDocumentService documentService, TikrDbContext db) =>
{
    var exists = await db.Documents.AnyAsync(d => d.Id == id);
    if (!exists) return Results.NotFound();
    var versions = await documentService.ListVersionsAsync(id);
    return Results.Ok(versions.Select(v => new DocumentVersionDto(
        v.Id, v.DocumentId, v.VersionNumber, v.FileName, v.FileSizeBytes, v.Note, v.CreatedAt)).ToList());
});

api.MapPost("/documents/{id:guid}/versions/{versionId:guid}/restore", async (
    Guid id,
    Guid versionId,
    IDocumentService documentService,
    IFileStorageService storage,
    IAuditService audit,
    ICurrentUserService currentUser,
    IHybridAiService hybridAi,
    ILogger<Program> endpointLog) =>
{
    try
    {
        var entity = await documentService.RestoreVersionAsync(id, versionId, storage, audit, currentUser);
        try { await hybridAi.EmbedDocumentAsync(id); }
        catch (Exception ex) { endpointLog.LogWarning(ex, "Post-version-restore embed failed for {DocumentId}", id); }
        return Results.Ok(MapDocument(entity));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

api.MapGet("/documents/{id:guid}/requirements", async (Guid id, TikrDbContext db) =>
{
    var exists = await db.Documents.AnyAsync(d => d.Id == id);
    if (!exists) return Results.NotFound();

    var links = await db.RequirementDocuments
        .Where(rd => rd.DocumentId == id)
        .Join(db.Requirements, rd => rd.RequirementId, r => r.Id, (rd, r) => new DocumentRequirementLinkDto(
            r.Id, r.Title, r.DueDate))
        .OrderBy(x => x.DueDate)
        .ToListAsync();
    return Results.Ok(links);
});

// Vault Complete Handover Package (last feature - searchable PDF with TOC/bookmarks using Document SDK)
api.MapGet("/vault/handover-package", async (IConfiguration config, TikrDbContext db, IDocumentGenerationService generator) =>
{
    try
    {
        var town = config["TIKR_TOWN_NAME"] ?? "Wiley";
        var knowledge = await db.KnowledgeEntries.OrderBy(k => k.SortOrder).ThenBy(k => k.Title).ToListAsync();
        var requirements = await db.Requirements.OrderBy(r => r.DueDate).ToListAsync();
        var documents = await db.Documents.OrderByDescending(d => d.UploadedAt).ToListAsync();
        var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);

        // Calendar snapshot: upcoming active requirements
        var calendarSnapshot = requirements
            .Where(r => !r.IsCompleted)
            .OrderBy(r => r.DueDate)
            .Take(25)
            .Select(r => new CalendarSnapshotItem(r.Title, r.DueDate, r.Category.ToString()))
            .ToList();

        var req = new HandoverPackageRequest(
            town,
            DateTime.UtcNow,
            knowledge.Select(MapKnowledge).ToList(),
            requirements.Select(r => CouncilPacketEndpoints.MapRequirement(r, links.GetValueOrDefault(r.Id, []))).ToList(),
            documents.Select(d => MapDocument(d)).ToList(),
            calendarSnapshot);

        var result = await generator.GenerateHandoverPackagePdfAsync(req);
        var fileName = $"TIKR-Complete-Handover-Package-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
        return Results.File(result.Content, result.ContentType, fileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// On-demand extract using Document SDK (for "Extract Text/Tables to Vault" in Documents.razor)
api.MapGet("/documents/{id:guid}/extract", async (Guid id, TikrDbContext db, IFileStorageService storage, IDocumentAgentExtractionBackend extractor) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null) return Results.NotFound();

    try
    {
        var stream = await storage.OpenReadAsync(entity.StoragePath);
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        buffer.Position = 0;

        var result = await extractor.ExtractAsync(buffer, entity.FileName);
        return Results.Ok(new DocumentTextExtractResult(result.ExtractedText, result.TablesExtractedCount));
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

var generate = api.MapGroup("/documents/generate");
generate.MapPost("/council-agenda", async (CouncilAgendaRequest? request, IConfiguration config, TikrDbContext db, IDocumentGenerationService generator) =>
{
    try
    {
        var town = request?.TownName ?? config["TIKR_TOWN_NAME"] ?? "Wiley";
        var meetingDate = request?.MeetingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var items = request?.Items is { Count: > 0 }
            ? request.Items
            : await BuildCouncilAgendaItemsAsync(db);

        var result = await generator.GenerateCouncilAgendaPdfAsync(
            new CouncilAgendaRequest(town, meetingDate, items));
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

generate.MapPost("/meeting-minutes", async (MeetingMinutesRequest request, IDocumentGenerationService generator) =>
{
    try
    {
        var result = await generator.GenerateMeetingMinutesDocxAsync(request);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

generate.MapPost("/clerk-memo", async (ClerkMemoRequest request, IDocumentGenerationService generator) =>
{
    try
    {
        var result = await generator.GenerateClerkMemoDocxAsync(request);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

generate.MapPost("/council-packet", async (
    CreateCouncilPacketRequest? request,
    ICouncilPacketService councilPacketService,
    ILogger<Program> logger) =>
{
    try
    {
        // Thin handler: auth/validation/mapping/service call (logic in ICouncilPacketService)
        var response = await councilPacketService.GenerateCouncilPacketAsync(request);
        if (response.ErrorMessage is not null)
            return Results.BadRequest(response);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new CouncilPacketResponse(null, null, ex.Message));
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new CouncilPacketResponse(null, null, ex.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Council packet generation failed");
        return Results.Json(
            new CouncilPacketResponse(null, null, "Council packet generation failed. Check API logs."),
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

generate.MapPost("/compliance-report", async (ComplianceReportRequest? request, IConfiguration config, TikrDbContext db, IDocumentGenerationService generator) =>
{
    try
    {
        var town = request?.TownName ?? config["TIKR_TOWN_NAME"] ?? "Wiley";
        var reportDate = request?.ReportDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = request?.Rows is { Count: > 0 }
            ? request.Rows
            : await BuildComplianceRowsAsync(db);

        var result = await generator.GenerateComplianceReportXlsxAsync(
            new ComplianceReportRequest(town, reportDate, rows));
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

api.MapPost("/documents/convert/word-to-pdf", async (HttpRequest request, IDocumentGenerationService generator) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var file = request.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    if (file.Length > 50 * 1024 * 1024) return Results.BadRequest("File too large (max 50MB for conversion).");

    try
    {
        await using var stream = file.OpenReadStream();
        var result = await generator.ConvertWordToPdfAsync(stream, file.FileName);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

api.MapPost("/documents/convert/excel-to-pdf", async (HttpRequest request, IDocumentGenerationService generator) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var file = request.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    try
    {
        await using var stream = file.OpenReadStream();
        var result = await generator.ConvertExcelToPdfAsync(stream, file.FileName);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

api.MapPost("/documents/convert/image-to-pdf", async (HttpRequest request, IDocumentGenerationService generator) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var file = request.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    try
    {
        await using var stream = file.OpenReadStream();
        var result = await generator.ConvertImageToPdfAsync(stream, file.FileName);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Knowledge vault
api.MapGet("/knowledge", async (TikrDbContext db) =>
{
    var items = await db.KnowledgeEntries.OrderBy(k => k.SortOrder).ThenBy(k => k.Title).ToListAsync();
    return items.Select(MapKnowledge).ToList();
});

api.MapPost("/knowledge", async (CreateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, IKnowledgeService knowledgeService) =>
{
    var entity = await knowledgeService.CreateAsync(request, audit, ai, currentUser);
    return Results.Created($"/api/knowledge/{entity.Id}", MapKnowledge(entity));
});

api.MapPut("/knowledge/{id:guid}", async (Guid id, UpdateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, IKnowledgeService knowledgeService) =>
{
    try
    {
        var entity = await knowledgeService.UpdateAsync(id, request, audit, ai, currentUser);
        return Results.Ok(MapKnowledge(entity));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

api.MapDelete("/knowledge/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser, IKnowledgeService knowledgeService) =>
{
    try
    {
        await knowledgeService.DeleteAsync(id, audit, currentUser);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

// Audit log (read-only)
api.MapGet("/audit", async (TikrDbContext db, int limit = 100) =>
{
    var items = await db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(limit).ToListAsync();
    return items;
});

// AI endpoints
api.MapGet("/ai/status", async (IHybridAiService ai) => await ai.GetStatusAsync());
api.MapGet("/ai/feature-settings", async (IFeatureSettingsService settings) =>
    Results.Ok(await settings.GetAsync()));
api.MapPut("/ai/feature-settings", async (UpdateFeatureSettingsRequest request, IFeatureSettingsService settings) =>
{
    try
    {
        return Results.Ok(await settings.UpdateAsync(request));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
api.MapGet("/ai/dashboard-priorities", async (IHybridAiService ai) => await ai.GetDashboardPrioritiesAsync());
api.MapPost("/ai/tag-document", async (TagDocumentRequest request, IHybridAiService ai) =>
{
    try
    {
        return Results.Ok(await ai.TagDocumentAsync(request.DocumentId));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});
api.MapPost("/ai/ask-advanced", async (AskAdvancedRequest request, IHybridAiService ai) =>
    Results.Ok(await ai.AskAdvancedAsync(request)));

api.MapPost("/ai/semantic-search", async (SemanticSearchRequest request, IHybridAiService ai) =>
    Results.Ok(await ai.SemanticSearchDocumentsAsync(request)));

api.MapPost("/ai/embed-document/{id:guid}", async (Guid id, IHybridAiService ai) =>
{
    try
    {
        return Results.Ok(await ai.EmbedDocumentAsync(id));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

api.MapPost("/ai/semantic-search-knowledge", async (SemanticSearchRequest request, IHybridAiService ai) =>
    Results.Ok(await ai.SemanticSearchKnowledgeAsync(request)));

api.MapPost("/ai/embed-knowledge/{id:guid}", async (Guid id, IHybridAiService ai) =>
{
    try
    {
        return Results.Ok(await ai.EmbedKnowledgeEntryAsync(id));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

api.MapPost("/ai/reindex-embeddings", async (IHybridAiService ai, EmbeddingRecoveryState recovery) =>
{
    var result = await ai.ReindexAllEmbeddingsAsync(trigger: "manual");
    recovery.NoteReindexResult("manual", result, DateTime.UtcNow);
    try
    {
        var health = await ai.GetCorpusHealthAsync();
        recovery.NoteCorpus(health);
    }
    catch { /* best-effort status refresh */ }
    return Results.Ok(result);
});

api.MapGet("/ai/corpus-health", async (IHybridAiService ai, EmbeddingRecoveryState recovery) =>
{
    var health = await ai.GetCorpusHealthAsync();
    recovery.NoteCorpus(health);
    return Results.Ok(health);
});

api.MapGet("/ai/embedding-recovery-status", (EmbeddingRecoveryState recovery) =>
    Results.Ok(recovery.Snapshot()));

api.MapPost("/ai/agent-scan", async (HttpRequest request, IDocumentAgentService agent, ILogger<Program> endpointLog) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var file = request.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    if (file.Length > 100 * 1024 * 1024) return Results.BadRequest("File too large (max 100MB).");
    if (string.IsNullOrWhiteSpace(file.FileName)) return Results.BadRequest("Invalid filename.");

    endpointLog.LogInformation(
        "Action {Action} {Phase} FileName={FileName} Bytes={Bytes}",
        "API.AgentScan", "started", file.FileName, file.Length);

    try
    {
        await using var stream = file.OpenReadStream();
        var result = await agent.ProcessUploadAsync(stream, file.FileName);
        endpointLog.LogInformation(
            "Action {Action} {Phase} FileName={FileName} UsedSyncfusion={UsedSyncfusion} TextChars={TextChars}",
            "API.AgentScan", "completed", file.FileName, result.UsedSyncfusionTools,
            result.ExtractedText?.Length ?? 0);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        endpointLog.LogError(ex, "Action {Action} {Phase} FileName={FileName}", "API.AgentScan", "failed", file.FileName);
        throw;
    }
});

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

try
{
    logger.LogInformation("Starting Kestrel. URLs from ASPNETCORE_URLS / launch settings will bind now…");
    app.Run();
}
catch (IOException ex) when (IsAddressInUse(ex))
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(default / launchSettings)";
    logger.LogCritical(ex,
        "PORT BIND FAILED — address already in use. ASPNETCORE_URLS={Urls}. " +
        "Root cause: another TIKR.Api (or other process) is listening. " +
        "On Mac: lsof -nP -iTCP:5001 -sTCP:LISTEN  then kill <pid>, or reuse the existing healthy process (curl http://localhost:5001/health).",
        urls);
    throw;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "TIKR.Api host terminated unexpectedly. Root: {Root}", RootCause(ex));
    throw;
}

static DocumentDto MapDocument(Document d, int linkedRequirementCount = 0, int versionCount = 0) =>
    new(d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.AiTags, d.SuggestedFolder, d.UploadedAt,
        d.FullTextContent, d.IsTransient, d.DeletedAt, linkedRequirementCount, versionCount);

static string ResolveLogDirectory()
{
    var candidates = new List<string?>();
    if (Directory.Exists("/data"))
        candidates.Add("/data/logs");
    candidates.Add(Environment.GetEnvironmentVariable("TIKR_LOG_PATH"));
    // From bin/Debug/netX.0 → repo .local-data/logs
    candidates.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".local-data", "logs")));
    // From src/TIKR.Api working directory
    candidates.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".local-data", "logs")));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

    foreach (var candidate in candidates)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            continue;
        try
        {
            var full = Path.GetFullPath(candidate);
            Directory.CreateDirectory(full);
            return full;
        }
        catch
        {
            /* try next */
        }
    }

    var fallback = Path.Combine(Path.GetTempPath(), "tikr-logs");
    Directory.CreateDirectory(fallback);
    return fallback;
}

static void LogStartupDiagnostics(WebApplication app, Microsoft.Extensions.Logging.ILogger logger, string logDir)
{
    var config = app.Configuration;
    var cs = config.GetConnectionString("Default") ?? "(null)";
    TryGetSqlitePath(cs, out var dbPath);
    var storage = config["FileStorage:BasePath"]
                  ?? config["FileStorage__BasePath"]
                  ?? "(null)";
    var ollama = TikrConfiguration.GetOllamaHost(config);
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(launchSettings / defaults)";
    var dataPath = config["TIKR_DATA_PATH"] ?? Environment.GetEnvironmentVariable("TIKR_DATA_PATH") ?? "(unset)";
    var contentRoot = app.Environment.ContentRootPath;
    var env = app.Environment.EnvironmentName;

    logger.LogInformation(
        "Startup diagnostics — Env={Env}, ContentRoot={ContentRoot}, ASPNETCORE_URLS={Urls}, LogDir={LogDir}",
        env, contentRoot, urls, logDir);
    logger.LogInformation(
        "Data paths — ConnectionString={ConnectionString}, SqlitePath={SqlitePath}, Exists={DbExists}, FileStorage={Storage}, TIKR_DATA_PATH={DataPath}",
        cs,
        dbPath ?? "(not sqlite file path)",
        dbPath is not null && File.Exists(dbPath),
        storage,
        dataPath);
    logger.LogInformation(
        "AI — OllamaHost={OllamaHost}, AuthEnabled={AuthEnabled}, Grok={UseGrok}",
        ollama,
        TikrConfiguration.IsAuthEnabled(config),
        TikrConfiguration.GetUseGrok(config));

    if (dbPath is not null)
    {
        var parent = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
            logger.LogWarning("SQLite parent directory does not exist yet: {Parent}", parent);
    }

    if (!string.IsNullOrWhiteSpace(storage) && storage is not "(null)" && !Directory.Exists(storage))
    {
        try
        {
            Directory.CreateDirectory(storage);
            logger.LogInformation("Created missing FileStorage directory: {Storage}", storage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot create FileStorage directory {Storage}. Root: {Root}", storage, RootCause(ex));
        }
    }
}

static bool IsAddressInUse(Exception ex)
{
    for (var e = ex; e is not null; e = e.InnerException!)
    {
        if (e is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.AddressAlreadyInUse })
            return true;
        if (e.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

static string RootCause(Exception ex)
{
    var cur = ex;
    while (cur.InnerException is not null)
        cur = cur.InnerException;
    return $"{cur.GetType().Name}: {cur.Message}";
}

static KnowledgeEntryDto MapKnowledge(KnowledgeEntry k) =>
    new(k.Id, k.Title, k.Content, k.Category, k.SortOrder);

static async Task<IReadOnlyList<CouncilAgendaItem>> BuildCouncilAgendaItemsAsync(TikrDbContext db)
{
    var requirements = await db.Requirements
        .Where(r => !r.IsCompleted)
        .OrderBy(r => r.DueDate)
        .Take(25)
        .ToListAsync();

    return requirements
        .Select(r => new CouncilAgendaItem(r.Title, r.Description, r.DueDate))
        .ToList();
}

static async Task<IReadOnlyList<ComplianceReportRow>> BuildComplianceRowsAsync(TikrDbContext db)
{
    var requirements = await db.Requirements.OrderBy(r => r.DueDate).ToListAsync();
    return requirements
        .Select(r => new ComplianceReportRow(
            r.Title,
            r.Description,
            r.DueDate,
            r.Category.ToString(),
            r.IsCompleted))
        .ToList();
}

static bool TryGetSqlitePath(string? connectionString, out string path)
{
    path = string.Empty;
    if (string.IsNullOrWhiteSpace(connectionString))
        return false;

    const string prefix = "Data Source=";
    var idx = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (idx < 0)
        return false;

    var value = connectionString[(idx + prefix.Length)..].Trim();
    var semi = value.IndexOf(';');
    if (semi >= 0)
        value = value[..semi];

    path = value.Trim('"');
    return !string.IsNullOrWhiteSpace(path);
}

// NOTE: Upload orchestration moved to DocumentService (final cleanup for centralization/testability).
