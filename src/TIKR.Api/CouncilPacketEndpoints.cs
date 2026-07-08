using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Api;

internal static class CouncilPacketEndpoints
{
    private const int SummaryMaxLength = 500;

    public static async Task<IResult> GenerateCouncilPacketAsync(
        CreateCouncilPacketRequest? request,
        IConfiguration config,
        TikrDbContext db,
        IDocumentGenerationService generator,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Council packet generation requested");
            var town = request?.TownName ?? config["TIKR_TOWN_NAME"] ?? "Wiley";
            var packetDate = request?.PacketDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var logoPath = request?.LogoPath ?? config["TIKR_TOWN_LOGO_PATH"];
            var requirements = request?.Requirements is { Count: > 0 }
                ? request.Requirements
                : await BuildCouncilPacketRequirementsAsync(db);

            if (requirements.Count == 0)
            {
                return Results.BadRequest(new CouncilPacketResponse(
                    null,
                    null,
                    "No requirements available for council packet."));
            }

            logger.LogInformation("Building council packet for {Town} ({Count} requirements)", town, requirements.Count);
            var packetRequest = new CreateCouncilPacketRequest(town, packetDate, logoPath, requirements);
            var files = await generator.GenerateCouncilPacketAsync(packetRequest);

            logger.LogInformation("Saving council packet PDF to NAS storage ({Bytes} bytes)", files.PdfContent.Length);
            var pdfEntity = await PersistGeneratedDocumentAsync(
                db, storage, files.PdfContent, files.PdfFileName, "application/pdf");

            logger.LogInformation("Saving council packet DOCX to NAS storage ({Bytes} bytes)", files.DocxContent.Length);
            var docxEntity = await PersistGeneratedDocumentAsync(
                db, storage, files.DocxContent, files.DocxFileName,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            await db.SaveChangesAsync();
            await audit.LogAsync(
                "Generate",
                nameof(Document),
                pdfEntity.Id,
                $"Council packet PDF {pdfEntity.FileName}",
                currentUser.UserId);
            await audit.LogAsync(
                "Generate",
                nameof(Document),
                docxEntity.Id,
                $"Council packet DOCX {docxEntity.FileName}",
                currentUser.UserId);

            logger.LogInformation(
                "Council packet saved: PDF {PdfId}, DOCX {DocxId}",
                pdfEntity.Id,
                docxEntity.Id);

            return Results.Ok(new CouncilPacketResponse(
                new CouncilPacketStoredFileDto(pdfEntity.Id, pdfEntity.FileName, BuildDownloadUrl(pdfEntity.Id)),
                new CouncilPacketStoredFileDto(docxEntity.Id, docxEntity.FileName, BuildDownloadUrl(docxEntity.Id)),
                null));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid council packet request");
            return Results.BadRequest(new CouncilPacketResponse(null, null, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Council packet generation unavailable");
            return Results.Json(new CouncilPacketResponse(null, null, ex.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Council packet generation failed");
            return Results.Json(
                new CouncilPacketResponse(null, null, "Council packet generation failed. Check API logs."),
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    public static async Task<Dictionary<Guid, List<RequirementLinkedDocumentDto>>> LoadRequirementLinksAsync(TikrDbContext db)
    {
        var rows = await db.RequirementDocuments
            .AsNoTracking()
            .Include(rd => rd.Document)
            .ToListAsync();

        return rows
            .GroupBy(row => row.RequirementId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => new RequirementLinkedDocumentDto(
                    row.DocumentId,
                    row.Document.FileName,
                    TruncateSummary(row.Document.FullTextContent))).ToList());
    }

    public static async Task<IReadOnlyList<CouncilPacketRequirementItem>> BuildCouncilPacketRequirementsAsync(TikrDbContext db)
    {
        var requirements = await db.Requirements
            .Where(r => !r.IsCompleted)
            .OrderBy(r => r.DueDate)
            .Take(50)
            .ToListAsync();

        var links = await LoadRequirementLinksAsync(db);

        return requirements.Select(requirement =>
        {
            var dto = MapRequirement(requirement, links.GetValueOrDefault(requirement.Id, []));
            var urgency = RequirementUrgencyHelper.GetUrgency(dto);
            var linked = links.GetValueOrDefault(requirement.Id, [])
                .Select(link => new CouncilPacketLinkedDocument(link.DocumentId, link.FileName, link.Summary))
                .ToList();

            return new CouncilPacketRequirementItem(
                requirement.Id,
                requirement.Title,
                requirement.Description,
                requirement.DueDate,
                requirement.Category.ToString(),
                requirement.IsCompleted ? "Completed" : "Open",
                urgency.ToString(),
                requirement.IsCompleted,
                linked);
        }).ToList();
    }

    public static RequirementDto MapRequirement(Requirement requirement, IReadOnlyList<RequirementLinkedDocumentDto> linkedDocuments) =>
        new(
            requirement.Id,
            requirement.Title,
            requirement.Description,
            requirement.DueDate,
            requirement.Recurrence,
            requirement.Category,
            requirement.IsSystemSeeded,
            requirement.IsCompleted,
            linkedDocuments);

    private static async Task<Document> PersistGeneratedDocumentAsync(
        TikrDbContext db,
        IFileStorageService storage,
        byte[] content,
        string fileName,
        string contentType)
    {
        await using var stream = new MemoryStream(content);
        var storagePath = await storage.SaveAsync(stream, fileName);
        var entity = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = content.Length,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Documents.Add(entity);
        return entity;
    }

    private static string BuildDownloadUrl(Guid documentId) => $"/api/documents/{documentId}/content";

    private static string? TruncateSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= SummaryMaxLength
            ? normalized
            : normalized[..SummaryMaxLength] + "…";
    }
}