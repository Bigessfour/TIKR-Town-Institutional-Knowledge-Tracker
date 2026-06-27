using Microsoft.Extensions.AI;
using OllamaSharp;

namespace TIKR.Infrastructure.Services;

public class OllamaChatClientFactory : IOllamaChatClientFactory
{
    public string ChatModel { get; }
    public string OllamaHost { get; }

    public OllamaChatClientFactory(string ollamaHost, string chatModel)
    {
        OllamaHost = ollamaHost;
        ChatModel = chatModel;
    }

    public IChatClient CreateChatClient() =>
        new OllamaApiClient(new Uri(EnsureTrailingSlash(OllamaHost)), ChatModel);

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string embeddingModel = "nomic-embed-text") =>
        (IEmbeddingGenerator<string, Embedding<float>>)new OllamaApiClient(new Uri(EnsureTrailingSlash(OllamaHost)), embeddingModel);

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
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
