using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
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

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Requirements
app.MapGet("/api/requirements", async (TikrDbContext db) =>
{
    var items = await db.Requirements.OrderBy(r => r.DueDate).ToListAsync();
    return items.Select(MapRequirement).ToList();
});

app.MapGet("/api/requirements/{id:guid}", async (Guid id, TikrDbContext db) =>
{
    var item = await db.Requirements.FindAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(MapRequirement(item));
});

app.MapPost("/api/requirements", async (CreateRequirementRequest request, TikrDbContext db, IAuditService audit) =>
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
    await audit.LogAsync("Create", nameof(Requirement), entity.Id, entity.Title);
    return Results.Created($"/api/requirements/{entity.Id}", MapRequirement(entity));
});

app.MapPut("/api/requirements/{id:guid}", async (Guid id, UpdateRequirementRequest request, TikrDbContext db, IAuditService audit) =>
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
    await audit.LogAsync("Update", nameof(Requirement), entity.Id, entity.Title);
    return Results.Ok(MapRequirement(entity));
});

app.MapDelete("/api/requirements/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit) =>
{
    var entity = await db.Requirements.FindAsync(id);
    if (entity is null) return Results.NotFound();
    if (entity.IsSystemSeeded) return Results.BadRequest("System-seeded requirements cannot be deleted.");

    db.Requirements.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(Requirement), id, entity.Title);
    return Results.NoContent();
});

// Documents
app.MapGet("/api/documents", async (TikrDbContext db, string? q) =>
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

app.MapPost("/api/documents", async (HttpRequest request, TikrDbContext db, IFileStorageService storage, IAuditService audit) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null) return Results.BadRequest("No file uploaded.");

    await using var stream = file.OpenReadStream();
    var storagePath = await storage.SaveAsync(stream, file.FileName);

    var entity = new Document
    {
        Id = Guid.NewGuid(),
        FileName = file.FileName,
        StoragePath = storagePath,
        ContentType = file.ContentType,
        FileSizeBytes = file.Length,
        UploadedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Documents.Add(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName);
    return Results.Created($"/api/documents/{entity.Id}", MapDocument(entity));
});

app.MapDelete("/api/documents/{id:guid}", async (Guid id, TikrDbContext db, IFileStorageService storage, IAuditService audit) =>
{
    var entity = await db.Documents.FindAsync(id);
    if (entity is null) return Results.NotFound();

    await storage.DeleteAsync(entity.StoragePath);
    db.Documents.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(Document), id, entity.FileName);
    return Results.NoContent();
});

// Knowledge vault
app.MapGet("/api/knowledge", async (TikrDbContext db) =>
{
    var items = await db.KnowledgeEntries.OrderBy(k => k.SortOrder).ThenBy(k => k.Title).ToListAsync();
    return items.Select(MapKnowledge).ToList();
});

app.MapPost("/api/knowledge", async (CreateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit) =>
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
    await audit.LogAsync("Create", nameof(KnowledgeEntry), entity.Id, entity.Title);
    return Results.Created($"/api/knowledge/{entity.Id}", MapKnowledge(entity));
});

app.MapPut("/api/knowledge/{id:guid}", async (Guid id, UpdateKnowledgeEntryRequest request, TikrDbContext db, IAuditService audit) =>
{
    var entity = await db.KnowledgeEntries.FindAsync(id);
    if (entity is null) return Results.NotFound();

    entity.Title = request.Title;
    entity.Content = request.Content;
    entity.Category = request.Category;
    entity.SortOrder = request.SortOrder;
    entity.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    await audit.LogAsync("Update", nameof(KnowledgeEntry), entity.Id, entity.Title);
    return Results.Ok(MapKnowledge(entity));
});

app.MapDelete("/api/knowledge/{id:guid}", async (Guid id, TikrDbContext db, IAuditService audit) =>
{
    var entity = await db.KnowledgeEntries.FindAsync(id);
    if (entity is null) return Results.NotFound();

    db.KnowledgeEntries.Remove(entity);
    await db.SaveChangesAsync();
    await audit.LogAsync("Delete", nameof(KnowledgeEntry), id, entity.Title);
    return Results.NoContent();
});

// Audit log (read-only)
app.MapGet("/api/audit", async (TikrDbContext db, int limit = 100) =>
{
    var items = await db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(limit).ToListAsync();
    return items;
});

// AI endpoints
app.MapGet("/api/ai/status", async (IHybridAiService ai) => await ai.GetStatusAsync());
app.MapGet("/api/ai/dashboard-priorities", async (IHybridAiService ai) => await ai.GetDashboardPrioritiesAsync());
app.MapPost("/api/ai/tag-document", async (TagDocumentRequest request, IHybridAiService ai) =>
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
app.MapPost("/api/ai/ask-advanced", async (AskAdvancedRequest request, IHybridAiService ai) =>
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

app.Run();

static RequirementDto MapRequirement(Requirement r) =>
    new(r.Id, r.Title, r.Description, r.DueDate, r.Recurrence, r.Category, r.IsSystemSeeded, r.IsCompleted);

static DocumentDto MapDocument(Document d) =>
    new(d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.AiTags, d.SuggestedFolder, d.UploadedAt);

static KnowledgeEntryDto MapKnowledge(KnowledgeEntry k) =>
    new(k.Id, k.Title, k.Content, k.Category, k.SortOrder);
