using Microsoft.EntityFrameworkCore;
using TIKR.Api;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

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

var app = builder.Build();

// ASP.NET Core / Document SDK: register after Build, before handling requests (Syncfusion guidance).
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

// Requirements
api.MapGet("/requirements", async (TikrDbContext db) =>
{
    var items = await db.Requirements.OrderBy(r => r.DueDate).ToListAsync();
    return items.Select(MapRequirement).ToList();
});

api.MapGet("/requirements/{id:guid}", async (Guid id, TikrDbContext db) =>
{
    var item = await db.Requirements.FindAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(MapRequirement(item));
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
    await db.SaveChangesAsync();
    await audit.LogAsync("Create", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId);
    return Results.Created($"/api/requirements/{entity.Id}", MapRequirement(entity));
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

    await db.SaveChangesAsync();
    await audit.LogAsync("Update", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId);
    return Results.Ok(MapRequirement(entity));
});

api.MapDelete("/requirements/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.Requirements.FindAsync(id);
    if (entity is null) return Results.NotFound();
    if (entity.IsSystemSeeded) return Results.BadRequest("System-seeded requirements cannot be deleted.");

    db.Requirements.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(Requirement), id, entity.Title, currentUser.UserId);
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

    string storagePath;
    string? fullText = null;

    if (DocumentTextExtractionService.CanExtract(file.FileName))
    {
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        buffer.Position = 0;
        fullText = await DocumentTextExtractionService.TryExtractAsync(buffer, file.FileName);
        buffer.Position = 0;
        storagePath = await storage.SaveAsync(buffer, file.FileName);
    }
    else
    {
        await using var stream = file.OpenReadStream();
        storagePath = await storage.SaveAsync(stream, file.FileName);
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

    db.Documents.Add(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName, currentUser.UserId);
    return Results.Created($"/api/documents/{entity.Id}", MapDocument(entity));
});

api.MapGet("/documents/{id:guid}/content", async (Guid id, TikrDbContext db, IFileStorageService storage) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null)
        return Results.NotFound();

    if (!File.Exists(storage.GetFullPath(entity.StoragePath)))
        return Results.NotFound();

    var stream = await storage.OpenReadAsync(entity.StoragePath);
    var contentType = string.IsNullOrWhiteSpace(entity.ContentType)
        ? "application/octet-stream"
        : entity.ContentType;

    return Results.File(stream, contentType, entity.FileName);
});

api.MapDelete("/documents/{id:guid}", async (Guid id, TikrDbContext db, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null) return Results.NotFound();

    await storage.DeleteAsync(entity.StoragePath);
    db.Documents.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(Document), id, entity.FileName, currentUser.UserId);
    return Results.NoContent();
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
    await db.SaveChangesAsync();
    await audit.LogAsync("Create", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId);

    _ = await ai.EmbedKnowledgeEntryAsync(entity.Id);

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

    await db.SaveChangesAsync();
    await audit.LogAsync("Update", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId);

    _ = await ai.EmbedKnowledgeEntryAsync(entity.Id);

    return Results.Ok(MapKnowledge(entity));
});

api.MapDelete("/knowledge/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit, ICurrentUserService currentUser) =>
{
    var entity = await db.KnowledgeEntries.FindAsync(id);
    if (entity is null) return Results.NotFound();

    db.KnowledgeEntries.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(KnowledgeEntry), id, entity.Title, currentUser.UserId);
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
{
    try
    {
        return Results.Ok(await ai.AskAdvancedAsync(request));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

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

    await using var stream = file.OpenReadStream();
    var result = await agent.ProcessUploadAsync(stream, file.FileName);
    return Results.Ok(result);
});

app.Run();

static RequirementDto MapRequirement(Requirement r) =>
    new(r.Id, r.Title, r.Description, r.DueDate, r.Recurrence, r.Category, r.IsSystemSeeded, r.IsCompleted);

static DocumentDto MapDocument(Document d) =>
    new(d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.AiTags, d.SuggestedFolder, d.UploadedAt);

static KnowledgeEntryDto MapKnowledge(KnowledgeEntry k) =>
    new(k.Id, k.Title, k.Content, k.Category, k.SortOrder);

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
