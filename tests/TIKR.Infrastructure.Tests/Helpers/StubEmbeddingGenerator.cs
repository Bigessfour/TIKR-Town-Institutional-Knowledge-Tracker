using Microsoft.Extensions.AI;

namespace TIKR.Infrastructure.Tests.Helpers;

/// <summary>
/// Returns a deterministic embedding driven by the input text so tests can assert ranking
/// behavior without a live Ollama instance. Pass null to simulate generator failure.
/// </summary>
internal sealed class StubEmbeddingGenerator(Func<string, float[]?> mapper) : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new("stub-embedding");

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var value in values)
        {
            var vector = mapper(value);
            if (vector is null)
                throw new InvalidOperationException("Stub embedding generator simulated failure.");
            result.Add(new Embedding<float>(vector));
        }
        return Task.FromResult(result);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
