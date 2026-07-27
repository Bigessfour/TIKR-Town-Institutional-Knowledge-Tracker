using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Thin Ollama agent-tool bridge: <c>search_town_documents</c> wraps HybridAi semantic search
/// so tool-calling agents can retrieve the same EmbeddingChunks used by Assistant RAG.
/// </summary>
public sealed class TownDocumentSearchToolRegistry(IServiceScopeFactory scopeFactory)
{
    private readonly IReadOnlyList<AIFunction> _functions =
    [
        AIFunctionFactory.Create(
            new TownDocumentSearchTools(scopeFactory).SearchTownDocumentsAsync,
            new AIFunctionFactoryOptions
            {
                Name = "search_town_documents",
                Description =
                    "Search the town document library (NAS-ingested and uploaded files) using hybrid " +
                    "semantic + keyword retrieval over EmbeddingChunks. Returns cited snippets for clerk questions."
            })
    ];

    public IReadOnlyList<AIFunction> GetFunctions() => _functions;
}

public sealed class TownDocumentSearchTools(IServiceScopeFactory scopeFactory)
{
    [Description("Search indexed town documents by natural language query and return top matching snippets.")]
    public async Task<string> SearchTownDocumentsAsync(
        [Description("Natural language question or keywords about town documents")] string query,
        [Description("Maximum hits to return (1-10)")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "No query provided.";

        await using var scope = scopeFactory.CreateAsyncScope();
        var ai = scope.ServiceProvider.GetRequiredService<IHybridAiService>();
        var response = await ai.SemanticSearchDocumentsAsync(
            new SemanticSearchRequest(query.Trim(), Math.Clamp(topK, 1, 10)),
            cancellationToken);

        if (!response.EmbeddingAvailable)
            return "Document search unavailable (Ollama embeddings offline). Try again when Ollama is running with nomic-embed-text.";

        if (response.Hits.Count == 0)
            return "No matching documents found above the relevance threshold.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {response.Hits.Count} document hit(s). Each hit has a topic label, optional About summary, and Excerpt:");
        foreach (var hit in response.Hits)
        {
            sb.AppendLine(DocumentContextLabel.FormatRagHit(
                hit.FileName,
                hit.Topic,
                hit.SuggestedFolder,
                hit.ChunkIndex,
                hit.Summary,
                hit.Snippet));
            sb.AppendLine($"  Score: {hit.Score:F2}");
        }

        return sb.ToString().TrimEnd();
    }
}
