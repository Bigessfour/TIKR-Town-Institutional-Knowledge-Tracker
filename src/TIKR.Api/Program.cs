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

// Operational structured logging via Serilog (console + rolling file to /data/logs/tikr-*.log).
// Captures detailed runtime info for observability, debugging, and proof of operation.
// Verbosity: Debug (Microsoft overrides to reduce noise).
try { Directory.CreateDirectory("/data/logs"); } catch { }
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TIKR")
    .WriteTo.Console()
    .WriteTo.File("/data/logs/tikr-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvLoader.LoadDevelopmentEnv(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTikrInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Serilog host integration for structured logging
builder.Host.UseSerilog();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var docSdkKey = TikrConfiguration.GetSyncfusionLicenseKey(app.Configuration);
if (!string.IsNullOrWhiteSpace(docSdkKey))
{
    logger.LogInformation("Syncfusion license key found for Document SDK (length: {Length})", docSdkKey.Length);
}
else
{
    logger.LogWarning("No Syncfusion license key found for Document SDK in configuration");
}

SyncfusionDocumentLicense.RegisterFromConfiguration(app.Configuration);

var authEnabled = TikrConfiguration.IsAuthEnabled(app.Configuration);

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseCors();

// Request logging via Serilog for observability (HTTP, headers, timing)
app.UseSerilogRequestLogging();

if (authEnabled)
    app.MapAuthEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

var api = app.MapGroup("/api");
if (authEnabled)
    api.RequireAuthorization(TikrAuthPolicies.Authenticated);

api.MapGet("/system/local-status", async (IConfiguration config, IHybridAiService ai) =>
{
    var town = config["TIKR_TOWN_NAME"] ?? "Wiley";
    var storageLabel = config["TIKR_STORAGE_LABEL"] ?? "Synology NAS";
    DateTime? dataModified = null;

    if (TryGetSqlitePath(config.GetConnectionString("Default"), out var dbPath) && File.Exists(dbPath))
        dataModified = File.GetLastWriteTimeUtc(dbPath);

    var aiStatus = await ai.GetStatusAsync();
    return Results.Ok(new LocalStorageStatusDto(town, storageLabel, dataModified, aiStatus.OllamaAvailable));
});

api.MapGet("/system/document-sdk-status", (IConfiguration config) =>
    Results.Ok(SyncfusionDocumentLicense.GetStatus(config)));

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

api.MapPost("/requirements", async (CreateRequirementRequest request, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = new Requirement
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        DueDate = request.DueDate,
        Recurrence = request.Recurrence,
        Category = request.Category,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Requirements.Add(entity);
    await audit.LogAsync("Create", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId);
    await db.SaveChangesAsync();
    return Results.Created(
        $"/api/requirements/{entity.Id}",
        CouncilPacketEndpoints.MapRequirement(entity, []));
});

api.MapPost("/requirements/{id:guid}/documents", async (
    Guid id,
    LinkRequirementDocumentRequest request,
    TikrDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser) =>
{
    var requirement = await db.Requirements.FindAsync(id);
    if (requirement is null)
        return Results.NotFound();

    var document = await db.Documents.FindAsync(request.DocumentId);
    if (document is null)
        return Results.NotFound(new { error = "Document not found." });

    var existing = await db.RequirementDocuments.FindAsync(id, request.DocumentId);
    if (existing is null)
    {
        db.RequirementDocuments.Add(new RequirementDocument
        {
            RequirementId = id,
            DocumentId = request.DocumentId,
            LinkedAt = DateTime.UtcNow
        });
        await audit.LogAsync("Link", nameof(Requirement), id, document.FileName, currentUser.UserId);
        await db.SaveChangesAsync();
    }
    var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
    return Results.Ok(CouncilPacketEndpoints.MapRequirement(requirement, links.GetValueOrDefault(id, [])));
});

api.MapDelete("/requirements/{id:guid}/documents/{documentId:guid}", async (
    Guid id,
    Guid documentId,
    TikrDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser) =>
{
    var link = await db.RequirementDocuments.FindAsync(id, documentId);
    if (link is null)
        return Results.NotFound();

    db.RequirementDocuments.Remove(link);
    await audit.LogAsync("Unlink", nameof(Requirement), id, documentId.ToString(), currentUser.UserId);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPut("/requirements/{id:guid}", async (Guid id, UpdateRequirementRequest request, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.Requirements.FindAsync(id);
    if (entity is null) return Results.NotFound();

    entity.Title = request.Title;
    entity.Description = request.Description;
    entity.DueDate = request.DueDate;
    entity.Recurrence = request.Recurrence;
    entity.Category = request.Category;
    entity.IsCompleted = request.IsCompleted;
    entity.UpdatedAt = DateTime.UtcNow;

    await audit.LogAsync("Update", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId);
    var links = await CouncilPacketEndpoints.LoadRequirementLinksAsync(db);
    await db.SaveChangesAsync();
    return Results.Ok(CouncilPacketEndpoints.MapRequirement(entity, links.GetValueOrDefault(entity.Id, [])));
});

api.MapDelete("/requirements/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.Requirements.FindAsync(id);
    if (entity is null) return Results.NotFound();
    if (entity.IsSystemSeeded) return Results.BadRequest("System-seeded requirements cannot be deleted.");

    db.Requirements.Remove(entity);
    await audit.LogAsync("Delete", nameof(Requirement), id, entity.Title, currentUser.UserId);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Documents
api.MapGet("/documents", async (TikrDbContext db, string? q) =>
{
    var query = db.Documents.AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        query = query.Where(d =>
            d.FileName.Contains(q) ||
            (d.FullTextContent != null && d.FullTextContent.Contains(q)) ||
            (d.AiTags != null && d.AiTags.Contains(q)));
    }

    var items = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
    return items.Select(MapDocument).ToList();
});

api.MapPost("/documents", async (HttpRequest request, TikrDbContext db, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null) return Results.BadRequest("No file uploaded.");
    if (file.Length > 100 * 1024 * 1024) return Results.BadRequest("File too large (max 100MB).");
    if (string.IsNullOrWhiteSpace(file.FileName)) return Results.BadRequest("Invalid filename.");

    var (entity, _, _) = await PrepareDocumentUploadAsync(file, storage);

    using var tx = await db.Database.BeginTransactionAsync();
    db.Documents.Add(entity);
    await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName, currentUser.UserId);
    await db.SaveChangesAsync();
    await tx.CommitAsync();
    return Results.Created($"/api/documents/{entity.Id}", MapDocument(entity));
});

api.MapGet("/documents/{id:guid}/content", async (Guid id, TikrDbContext db, IFileStorageService storage) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null) return Results.NotFound();

    var stream = await storage.OpenReadAsync(entity.StoragePath);
    return Results.File(stream, entity.ContentType ?? "application/octet-stream", entity.FileName);
});

api.MapDelete("/documents/{id:guid}", async (Guid id, TikrDbContext db, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null) return Results.NotFound();

    await storage.DeleteAsync(entity.StoragePath);
    db.Documents.Remove(entity);
    await audit.LogAsync("Delete", nameof(Document), id, entity.FileName, currentUser.UserId);
    await db.SaveChangesAsync();
    return Results.NoContent();
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
            requirements.Select(r => new RequirementDto(r.Id, r.Title, r.Description, r.DueDate, r.Recurrence, r.Category, r.IsSystemSeeded, r.IsCompleted, new List<RequirementLinkedDocumentDto>())).ToList(),
            documents.Select(MapDocument).ToList(),
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
    IConfiguration config,
    TikrDbContext db,
    IDocumentGenerationService generator,
    IFileStorageService storage,
    IAuditService audit,
    ICurrentUserService currentUser,
    ILogger<Program> logger) =>
    await CouncilPacketEndpoints.GenerateCouncilPacketAsync(
        request,
        config,
        db,
        generator,
        storage,
        audit,
        currentUser,
        logger));

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

api.MapPost("/knowledge", async (CreateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser) =>
{
    var entity = new KnowledgeEntry
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Content = request.Content,
        Category = request.Category,
        SortOrder = request.SortOrder,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.KnowledgeEntries.Add(entity);
    await audit.LogAsync("Create", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId);

    _ = await ai.EmbedKnowledgeEntryAsync(entity.Id);

    await db.SaveChangesAsync();
    return Results.Created($"/api/knowledge/{entity.Id}", MapKnowledge(entity));
});

api.MapPut("/knowledge/{id:guid}", async (Guid id, UpdateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser) =>
{
    var entity = await db.KnowledgeEntries.FindAsync(id);
    if (entity is null) return Results.NotFound();

    entity.Title = request.Title;
    entity.Content = request.Content;
    entity.Category = request.Category;
    entity.SortOrder = request.SortOrder;
    entity.UpdatedAt = DateTime.UtcNow;

    await audit.LogAsync("Update", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId);

    _ = await ai.EmbedKnowledgeEntryAsync(entity.Id);

    await db.SaveChangesAsync();
    return Results.Ok(MapKnowledge(entity));
});

api.MapDelete("/knowledge/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.KnowledgeEntries.FindAsync(id);
    if (entity is null) return Results.NotFound();

    db.KnowledgeEntries.Remove(entity);
    await audit.LogAsync("Delete", nameof(KnowledgeEntry), id, entity.Title, currentUser.UserId);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Audit log (read-only)
api.MapGet("/audit", async (TikrDbContext db, int limit = 100) =>
{
    var items = await db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(limit).ToListAsync();
    return items;
});

// AI endpoints
api.MapGet("/ai/status", async (IHybridAiService ai) => await ai.GetStatusAsync());
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

api.MapPost("/ai/agent-scan", async (HttpRequest request, IDocumentAgentService agent) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var file = request.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    if (file.Length > 100 * 1024 * 1024) return Results.BadRequest("File too large (max 100MB).");
    if (string.IsNullOrWhiteSpace(file.FileName)) return Results.BadRequest("Invalid filename.");

    await using var stream = file.OpenReadStream();
    var result = await agent.ProcessUploadAsync(stream, file.FileName);
    return Results.Ok(result);
});

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

static DocumentDto MapDocument(Document d) =>
    new(d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.AiTags, d.SuggestedFolder, d.UploadedAt, d.FullTextContent);

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

// Extracted document upload orchestration (addresses scattered logic in endpoint).
// Keeps storage + text extraction + entity creation centralized here for testability.
static async Task<(Document Entity, string? FullText, string StoragePath)> PrepareDocumentUploadAsync(
    IFormFile file,
    IFileStorageService storage,
    CancellationToken ct = default)
{
    string storagePath;
    string? fullText = null;

    if (DocumentTextExtractionService.CanExtract(file.FileName))
    {
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        fullText = await DocumentTextExtractionService.TryExtractAsync(buffer, file.FileName, ct);
        buffer.Position = 0;
        storagePath = await storage.SaveAsync(buffer, file.FileName, ct);
    }
    else
    {
        await using var stream = file.OpenReadStream();
        storagePath = await storage.SaveAsync(stream, file.FileName, ct);
    }

    var entity = new Document
    {
        Id = Guid.NewGuid(),
        FileName = file.FileName,
        StoragePath = storagePath,
        ContentType = file.ContentType,
        FileSizeBytes = file.Length,
        FullTextContent = fullText,
        UploadedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    return (entity, fullText, storagePath);
}
