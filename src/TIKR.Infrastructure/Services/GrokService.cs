using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Configuration;
using TIKR.Shared.Diagnostics;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// xAI Grok chat client (OpenAI-compatible <c>POST /v1/chat/completions</c>).
/// Endpoint: <see cref="TikrConfiguration.GrokApiBaseUrl"/>; default model: <see cref="TikrConfiguration.DefaultGrokModel"/>.
/// Docs: https://docs.x.ai/docs/models · https://docs.x.ai/docs/api-reference
/// </summary>
public class GrokService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GrokService> logger,
    FeatureSettingsState? featureSettings = null)
{
    public bool IsEnabled
    {
        get
        {
            if (featureSettings is not null)
                return featureSettings.Current.GrokEnabled;

            return TikrConfiguration.GetUseGrok(configuration)
                && !string.IsNullOrWhiteSpace(TikrConfiguration.GetGrokApiKey(configuration));
        }
    }

    private string? ApiKey =>
        featureSettings?.Current.GrokApiKey ?? TikrConfiguration.GetGrokApiKey(configuration);

    public string ResolvedModel =>
        TikrConfiguration.NormalizeGrokModel(
            featureSettings?.Current.GrokModel ?? TikrConfiguration.GetGrokModel(configuration));

    public async Task<string?> CompleteAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return null;

        var apiKey = ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var model = ResolvedModel;
        TikrActionLog.Started(logger, "Grok.Complete", $"Model={model} PromptLen={prompt?.Length ?? 0}");

        var request = new GrokChatRequest
        {
            Model = model,
            // Prefer max_completion_tokens (max_tokens is deprecated on xAI chat API).
            MaxCompletionTokens = 8192,
            Temperature = 0.4,
            Messages =
            [
                new GrokMessage
                {
                    Role = "system",
                    Content = systemPrompt
                              ?? "You are a helpful assistant for Colorado municipal clerks. "
                                 + "Answer clearly for non-technical clerks. Prefer grounded facts from the provided context."
                },
                new GrokMessage { Role = "user", Content = prompt ?? string.Empty }
            ]
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{TikrConfiguration.GrokApiBaseUrl}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = JsonContent.Create(request);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var snippet = body.Length > 400 ? body[..400] + "…" : body;
                TikrActionLog.Failed(logger, "Grok.Complete",
                    $"HTTP {(int)response.StatusCode} Model={model} Body={snippet}");
                logger.LogError(
                    "Grok API call failed: {StatusCode} model={Model} body={Body}",
                    (int)response.StatusCode, model, snippet);
                return null;
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<GrokChatResponse>(body);
            var message = result?.Choices?.FirstOrDefault()?.Message;
            // Prefer visible content; some reasoning models also return reasoning_content.
            var content = message?.Content;
            if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(message?.ReasoningContent))
            {
                // Do not surface raw reasoning to clerks; treat as failure if no final content.
                TikrActionLog.Failed(logger, "Grok.Complete",
                    $"Model={model} empty content (had reasoning_content only)");
                return null;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TikrActionLog.Failed(logger, "Grok.Complete", $"Model={model} empty choices/content");
                return null;
            }

            TikrActionLog.Completed(logger, "Grok.Complete",
                $"Model={result?.Model ?? model} AnswerLen={content.Length}");
            return content;
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(logger, "Grok.Complete", ex, $"Model={model}");
            logger.LogError(ex, "Grok API call failed for model {Model}", model);
            return null;
        }
    }

    private sealed class GrokChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GrokMessage> Messages { get; set; } = [];

        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }

    private sealed class GrokMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }

    private sealed class GrokChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<GrokChoice>? Choices { get; set; }
    }

    private sealed class GrokChoice
    {
        [JsonPropertyName("message")]
        public GrokMessage? Message { get; set; }
    }
}
