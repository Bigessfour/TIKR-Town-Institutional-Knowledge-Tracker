using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Configuration;

namespace TIKR.Infrastructure.Services;

public class GrokService(HttpClient httpClient, IConfiguration configuration, ILogger<GrokService> logger)
{
    private readonly bool _enabled = TikrConfiguration.GetUseGrok(configuration);
    private readonly string? _apiKey = TikrConfiguration.GetGrokApiKey(configuration);

    public bool IsEnabled => _enabled && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string?> CompleteAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return null;

        var request = new GrokChatRequest
        {
            Model = TikrConfiguration.GetGrokModel(configuration),
            Messages =
            [
                new GrokMessage { Role = "system", Content = systemPrompt ?? "You are a helpful assistant for Colorado municipal clerks." },
                new GrokMessage { Role = "user", Content = prompt }
            ]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.x.ai/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = JsonContent.Create(request);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GrokChatResponse>(cancellationToken);
            return result?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Grok API call failed");
            return null;
        }
    }

    private sealed class GrokChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GrokMessage> Messages { get; set; } = [];
    }

    private sealed class GrokMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GrokChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GrokChoice>? Choices { get; set; }
    }

    private sealed class GrokChoice
    {
        [JsonPropertyName("message")]
        public GrokMessage? Message { get; set; }
    }
}
