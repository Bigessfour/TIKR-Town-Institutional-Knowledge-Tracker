using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;

namespace TIKR.Infrastructure.Services;

public class OllamaChatClientFactory : IOllamaChatClientFactory
{
    public const string DefaultEmbeddingModel = "nomic-embed-text";

    private readonly FeatureSettingsState? _state;
    private readonly string? _fixedHost;
    private readonly string? _fixedModel;
    private readonly IConfiguration? _configuration;

    public OllamaChatClientFactory(FeatureSettingsState state, IConfiguration configuration)
    {
        _state = state;
        _configuration = configuration;
    }

    /// <summary>Test / legacy constructor with a fixed host and model.</summary>
    public OllamaChatClientFactory(string ollamaHost, string chatModel, IConfiguration? configuration = null)
    {
        _fixedHost = ollamaHost;
        _fixedModel = chatModel;
        _configuration = configuration;
    }

    public string ChatModel => _fixedModel ?? _state!.Current.OllamaChatModel;
    public string OllamaHost => _fixedHost ?? _state!.Current.OllamaHost;
    public string EmbeddingModel => DefaultEmbeddingModel;

    public IChatClient CreateChatClient()
    {
        var timeout = TimeSpan.FromSeconds(
            TikrConfiguration.GetChatCompletionTimeoutSeconds(_configuration));
        var http = CreateHttpClient(timeout);
        return new OllamaApiClient(http, ChatModel, jsonSerializerContext: null!);
    }

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string? embeddingModel = null)
    {
        var timeout = TimeSpan.FromSeconds(
            TikrConfiguration.GetEmbeddingGenerationTimeoutSeconds(_configuration));
        var http = CreateHttpClient(timeout);
        var model = string.IsNullOrWhiteSpace(embeddingModel) ? EmbeddingModel : embeddingModel.Trim();
        return (IEmbeddingGenerator<string, Embedding<float>>)new OllamaApiClient(
            http, model, jsonSerializerContext: null!);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var inventory = await GetModelInventoryAsync(cancellationToken);
        return inventory.HostReachable;
    }

    public async Task<OllamaModelInventoryDto> GetModelInventoryAsync(
        CancellationToken cancellationToken = default)
    {
        var chatModel = ChatModel;
        var embedModel = EmbeddingModel;
        var timeout = TimeSpan.FromSeconds(
            TikrConfiguration.GetOllamaHttpTimeoutSeconds(_configuration));

        try
        {
            using var http = CreateHttpClient(timeout);
            using var tagsResponse = await http.GetAsync("api/tags", cancellationToken);
            if (!tagsResponse.IsSuccessStatusCode)
            {
                return new OllamaModelInventoryDto(
                    HostReachable: false,
                    ChatModelPresent: false,
                    EmbeddingModelPresent: false,
                    LoadedModelCount: 0,
                    StatusMessage: "Local helper (Ollama) is not responding. Chat and document search pause until it returns.");
            }

            var tagNames = await ReadModelNamesFromTagsAsync(tagsResponse, cancellationToken);
            var chatPresent = HasModel(tagNames, chatModel);
            var embedPresent = HasModel(tagNames, embedModel);

            var loadedCount = 0;
            try
            {
                using var psResponse = await http.GetAsync("api/ps", cancellationToken);
                if (psResponse.IsSuccessStatusCode)
                    loadedCount = await ReadLoadedModelCountAsync(psResponse, cancellationToken);
            }
            catch
            {
                // /api/ps is optional for status; ignore probe failures.
            }

            var message = BuildStatusMessage(chatModel, embedModel, chatPresent, embedPresent, loadedCount);
            return new OllamaModelInventoryDto(
                HostReachable: true,
                ChatModelPresent: chatPresent,
                EmbeddingModelPresent: embedPresent,
                LoadedModelCount: loadedCount,
                StatusMessage: message);
        }
        catch
        {
            return new OllamaModelInventoryDto(
                HostReachable: false,
                ChatModelPresent: false,
                EmbeddingModelPresent: false,
                LoadedModelCount: 0,
                StatusMessage: "Local helper (Ollama) is offline. Files and Settings still work; AI features wait until it returns.");
        }
    }

    private HttpClient CreateHttpClient(TimeSpan timeout)
    {
        var http = OllamaHttpProbe.CreateClient(timeout);
        http.BaseAddress = new Uri(EnsureTrailingSlash(OllamaHost));
        return http;
    }

    private static async Task<HashSet<string>> ReadModelNamesFromTagsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!doc.RootElement.TryGetProperty("models", out var models) ||
            models.ValueKind != JsonValueKind.Array)
            return names;

        foreach (var model in models.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                var value = name.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    names.Add(value);
            }

            if (model.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String)
            {
                var value = modelProp.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    names.Add(value);
            }
        }

        return names;
    }

    private static async Task<int> ReadLoadedModelCountAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!doc.RootElement.TryGetProperty("models", out var models) ||
            models.ValueKind != JsonValueKind.Array)
            return 0;
        return models.GetArrayLength();
    }

    internal static bool HasModel(IReadOnlyCollection<string> names, string required)
    {
        if (string.IsNullOrWhiteSpace(required) || names.Count == 0)
            return false;

        var requiredHasTag = required.Contains(':', StringComparison.Ordinal);
        var requiredBase = required.Split(':')[0];

        foreach (var name in names)
        {
            if (name.Equals(required, StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.StartsWith(required + ":", StringComparison.OrdinalIgnoreCase))
                return true;

            // Tag-less required (e.g. "llama3.2") matches any tagged install of that family.
            if (!requiredHasTag
                && name.Split(':')[0].Equals(requiredBase, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static string BuildStatusMessage(
        string chatModel,
        string embedModel,
        bool chatPresent,
        bool embedPresent,
        int loadedCount)
    {
        if (!chatPresent && !embedPresent)
        {
            return $"Local helper is up, but required models are missing. Pull `{chatModel}` and `{embedModel}` "
                   + $"(e.g. `ollama pull {chatModel}` and `ollama pull {embedModel}`).";
        }

        if (!chatPresent)
        {
            return $"Local helper is up, but chat model `{chatModel}` is not installed. "
                   + $"Run `ollama pull {chatModel}` on the NAS.";
        }

        if (!embedPresent)
        {
            return $"Local helper is up, but embedding model `{embedModel}` is not installed. "
                   + $"Document search needs it — run `ollama pull {embedModel}`.";
        }

        if (loadedCount >= 2)
        {
            return $"Local helper ready ({chatModel} + {embedModel}). "
                   + $"{loadedCount} models are loaded — embedding recovery may wait to protect NAS memory.";
        }

        return $"Local helper ready ({chatModel} for chat, {embedModel} for search).";
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";
}
