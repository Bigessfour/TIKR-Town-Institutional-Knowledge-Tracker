using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using TIKR.Shared.Configuration;

namespace TIKR.Infrastructure.Services;

public class OllamaChatClientFactory : IOllamaChatClientFactory
{
    private readonly FeatureSettingsState? _state;
    private readonly string? _fixedHost;
    private readonly string? _fixedModel;
    private readonly IConfiguration? _configuration;

    public OllamaChatClientFactory(FeatureSettingsState state, IConfiguration? configuration = null)
    {
        _state = state;
        _configuration = configuration;
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
        new OllamaApiClient(CreateHttpClient(), ChatModel);

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string embeddingModel = "nomic-embed-text") =>
        (IEmbeddingGenerator<string, Embedding<float>>)new OllamaApiClient(CreateHttpClient(), embeddingModel);

    public async Task<IReadOnlyList<string>> ListRunningModelNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = OllamaHttpProbe.CreateClient(TimeSpan.FromSeconds(5));
            var response = await http.GetAsync($"{EnsureTrailingSlash(OllamaHost)}api/ps", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != System.Text.Json.JsonValueKind.Array)
                return [];

            var names = new List<string>();
            foreach (var model in models.EnumerateArray())
            {
                if (model.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } value)
                    names.Add(value);
            }

            return names;
        }
        catch
        {
            return [];
        }
    }

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

    private HttpClient CreateHttpClient()
    {
        var seconds = TikrConfiguration.GetChatCompletionTimeoutSeconds(_configuration) + 30;
        return new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(OllamaHost)),
            Timeout = TimeSpan.FromSeconds(seconds)
        };
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";
}
