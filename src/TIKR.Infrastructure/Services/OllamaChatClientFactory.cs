using Microsoft.Extensions.AI;
using OllamaSharp;

namespace TIKR.Infrastructure.Services;

public class OllamaChatClientFactory : IOllamaChatClientFactory
{
    private readonly FeatureSettingsState? _state;
    private readonly string? _fixedHost;
    private readonly string? _fixedModel;

    public OllamaChatClientFactory(FeatureSettingsState state)
    {
        _state = state;
    }

    /// <summary>Test / legacy constructor with a fixed host and model.</summary>
    public OllamaChatClientFactory(string ollamaHost, string chatModel)
    {
        _fixedHost = ollamaHost;
        _fixedModel = chatModel;
    }

    public string ChatModel => _fixedModel ?? _state!.Current.OllamaChatModel;
    public string OllamaHost => _fixedHost ?? _state!.Current.OllamaHost;

    public IChatClient CreateChatClient() =>
        new OllamaApiClient(new Uri(EnsureTrailingSlash(OllamaHost)), ChatModel);

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string embeddingModel = "nomic-embed-text") =>
        (IEmbeddingGenerator<string, Embedding<float>>)new OllamaApiClient(new Uri(EnsureTrailingSlash(OllamaHost)), embeddingModel);

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = OllamaHttpProbe.CreateClient(TimeSpan.FromSeconds(5));
            var response = await http.GetAsync($"{EnsureTrailingSlash(OllamaHost)}api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";
}
