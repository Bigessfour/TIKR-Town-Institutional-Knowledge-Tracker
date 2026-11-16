using Microsoft.Extensions.AI;

namespace TIKR.Infrastructure.Services;

public interface IOllamaChatClientFactory
{
    string ChatModel { get; }
    string OllamaHost { get; }
    IChatClient CreateChatClient();
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string embeddingModel = "nomic-embed-text");
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Names from Ollama <c>/api/ps</c>. Empty when nothing is loaded or the probe fails.</summary>
    Task<IReadOnlyList<string>> ListRunningModelNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
