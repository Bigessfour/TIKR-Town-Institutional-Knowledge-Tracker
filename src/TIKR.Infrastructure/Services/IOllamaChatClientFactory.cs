using Microsoft.Extensions.AI;
using TIKR.Shared.DTOs;

namespace TIKR.Infrastructure.Services;

public interface IOllamaChatClientFactory
{
    string ChatModel { get; }
    string OllamaHost { get; }

    /// <summary>Required embedding model id (nomic-embed-text).</summary>
    string EmbeddingModel { get; }

    IChatClient CreateChatClient();
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string? embeddingModel = null);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Probes host reachability, required chat/embed models, and currently loaded model count (/api/ps).
    /// </summary>
    Task<OllamaModelInventoryDto> GetModelInventoryAsync(CancellationToken cancellationToken = default);
}
