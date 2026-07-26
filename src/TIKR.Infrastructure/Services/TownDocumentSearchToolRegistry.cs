using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Shared.DTOs;
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
        sb.AppendLine($"Found {response.Hits.Count} document hit(s):");
        foreach (var hit in response.Hits)
        {
            sb.AppendLine(
                $"- [{hit.FileName}] (score {hit.Score:F2})" +
                (string.IsNullOrWhiteSpace(hit.SuggestedFolder) ? "" : $" folder={hit.SuggestedFolder}") +
                $": {hit.Snippet}");
        }

        return sb.ToString();
    }
}
