using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Council packet orchestration extracted from endpoints to enforce thin-API rule.
/// Ctor-injected per existing service patterns (see DocumentAgentService, HybridAiService, RequirementService).
/// Full flow: conditional build of requirements + links, generate (PDF+DOCX), persist docs under tx with audit.
/// </summary>
public class CouncilPacketService(
    TikrDbContext db,
    IDocumentGenerationService generator,
    IFileStorageService storage,
    IAuditService audit,
    ICurrentUserService currentUser,
    IConfiguration config,
    ILogger<CouncilPacketService> logger) : ICouncilPacketService
{
    private const int SummaryMaxLength = 500;

    public async Task<CouncilPacketResponse> GenerateCouncilPacketAsync(CreateCouncilPacketRequest? request, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Council packet generation requested");
            var town = request?.TownName ?? config["TIKR_TOWN_NAME"] ?? "Wiley";
            var packetDate = request?.PacketDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var logoPath = request?.LogoPath ?? config["TIKR_TOWN_LOGO_PATH"];
            var requirements = request?.Requirements is { Count: > 0 }
                ? request.Requirements
                : await BuildCouncilPacketRequirementsAsync(ct);

            if (requirements.Count == 0)
            {
                return new CouncilPacketResponse(
                    null,
                    null,
                    "No requirements available for council packet.");
            }

            logger.LogInformation("Building council packet for {Town} ({Count} requirements)", town, requirements.Count);
            var packetRequest = new CreateCouncilPacketRequest(town, packetDate, logoPath, requirements);
            var files = await generator.GenerateCouncilPacketAsync(packetRequest, ct);

            using var tx = await db.Database.BeginTransactionAsync(ct);

            logger.LogInformation("Saving council packet PDF to NAS storage ({Bytes} bytes)", files.PdfContent.Length);
            var pdfEntity = await PersistGeneratedDocumentAsync(
                storage, files.PdfContent, files.PdfFileName, "application/pdf", ct);

            logger.LogInformation("Saving council packet DOCX to NAS storage ({Bytes} bytes)", files.DocxContent.Length);
            var docxEntity = await PersistGeneratedDocumentAsync(
                storage, files.DocxContent, files.DocxFileName,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

            await audit.LogAsync(
                "Generate",
                nameof(Document),
                pdfEntity.Id,
                $"Council packet PDF {pdfEntity.FileName}",
                currentUser.UserId, ct);
            await audit.LogAsync(
                "Generate",
                nameof(Document),
                docxEntity.Id,
                $"Council packet DOCX {docxEntity.FileName}",
                currentUser.UserId, ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Council packet saved: PDF {PdfId}, DOCX {DocxId}",
                pdfEntity.Id,
                docxEntity.Id);

            return new CouncilPacketResponse(
                new CouncilPacketStoredFileDto(pdfEntity.Id, pdfEntity.FileName, BuildDownloadUrl(pdfEntity.Id)),
                new CouncilPacketStoredFileDto(docxEntity.Id, docxEntity.FileName, BuildDownloadUrl(docxEntity.Id)),
                null);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid council packet request");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Council packet generation unavailable");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Council packet generation failed");
            throw;
        }
    }

    private async Task<IReadOnlyList<CouncilPacketRequirementItem>> BuildCouncilPacketRequirementsAsync(CancellationToken ct)
    {
        var requirements = await db.Requirements
            .Where(r => !r.IsCompleted)
            .OrderBy(r => r.DueDate)
            .Take(50)
            .ToListAsync(ct);

        var links = await LoadRequirementLinksAsync(ct);

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

    private async Task<Dictionary<Guid, List<RequirementLinkedDocumentDto>>> LoadRequirementLinksAsync(CancellationToken ct)
    {
        var rows = await db.RequirementDocuments
            .AsNoTracking()
            .Include(rd => rd.Document)
            .ToListAsync(ct);

        return rows
            .GroupBy(row => row.RequirementId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => new RequirementLinkedDocumentDto(
                    row.DocumentId,
                    row.Document.FileName,
                    TruncateSummary(row.Document.FullTextContent))).ToList());
    }

    private RequirementDto MapRequirement(Requirement requirement, IReadOnlyList<RequirementLinkedDocumentDto> linkedDocuments) =>
        new(
            requirement.Id,
            requirement.Title,
            requirement.Description,
            requirement.DueDate,
            requirement.Recurrence,
            requirement.Category,
            requirement.IsSystemSeeded,
            requirement.IsCompleted,
            linkedDocuments,
            requirement.SubmitTo,
            requirement.ContactName,
            requirement.ContactEmail,
            requirement.ContactPhone);

    private async Task<Document> PersistGeneratedDocumentAsync(
        IFileStorageService storage,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        await using var stream = new MemoryStream(content);
        var storagePath = await storage.SaveAsync(stream, fileName, ct);
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
