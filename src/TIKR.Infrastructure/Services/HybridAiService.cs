using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class HybridAiService(
    TikrDbContext db,
    IOllamaChatClientFactory ollamaFactory,
    GrokService grokService,
    ILogger<HybridAiService> logger) : IHybridAiService
{
    public async Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var preview = (document.FullTextContent ?? document.FileName);
        preview = preview[..Math.Min(500, preview.Length)];
        var prompt =
            "Analyze this municipal document and respond with JSON only: " +
            "{\"tags\": [\"tag1\",\"tag2\"], \"suggestedFolder\": \"folder name\"}\n\n" +
            $"File name: {document.FileName}\nContent preview: {preview}";

        var response = await GetLocalCompletionAsync(prompt, cancellationToken);
        var tags = Array.Empty<string>();
        string? folder = null;

        if (!string.IsNullOrWhiteSpace(response))
        {
            try
            {
                using var doc = JsonDocument.Parse(ExtractJson(response));
                if (doc.RootElement.TryGetProperty("tags", out var tagsEl))
                    tags = tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(t => t.Length > 0).ToArray();
                if (doc.RootElement.TryGetProperty("suggestedFolder", out var folderEl))
                    folder = folderEl.GetString();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse AI tagging response");
                tags = ["uncategorized"];
            }
        }

        document.AiTags = JsonSerializer.Serialize(tags);
        document.SuggestedFolder = folder;
        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new TagDocumentResponse(documentId, tags, folder);
    }

    public async Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = await db.Requirements
            .Where(r => !r.IsCompleted && r.DueDate >= today.AddDays(-30))
            .OrderBy(r => r.DueDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var priorities = upcoming.Select(r =>
        {
            var daysUntil = r.DueDate.DayNumber - today.DayNumber;
            var priority = daysUntil < 0 ? "Overdue" : daysUntil <= 14 ? "High" : daysUntil <= 30 ? "Medium" : "Low";
            return new DashboardPriority(
                r.Title,
                r.Description ?? $"Due in {daysUntil} days",
                r.DueDate,
                priority);
        }).ToList();

        if (priorities.Count == 0)
        {
            priorities.Add(new DashboardPriority(
                "No urgent deadlines",
                "Add custom requirements or review the calendar.",
                null,
                "Low"));
        }

        return priorities;
    }

    public async Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default)
    {
        if (!grokService.IsEnabled)
            throw new InvalidOperationException("Advanced AI (Grok) is not enabled. Set USE_GROK=true and provide GROK_API_KEY.");

        var prompt = string.IsNullOrWhiteSpace(request.Context)
            ? request.Prompt
            : $"Context:\n{request.Context}\n\nQuestion:\n{request.Prompt}";

        var answer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken)
            ?? "Unable to get a response from Grok. Check your API key and network connection.";

        return new AskAdvancedResponse(answer, UsedGrok: true);
    }

    public async Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        return new AiStatusResponse(ollamaAvailable, ollamaFactory.ChatModel, grokService.IsEnabled);
    }

    private async Task<string?> GetLocalCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var client = ollamaFactory.CreateChatClient();
            var response = await client.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.Text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama completion failed");
            return null;
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}
