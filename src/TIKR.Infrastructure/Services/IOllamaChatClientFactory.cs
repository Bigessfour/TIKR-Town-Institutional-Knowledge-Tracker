using Microsoft.Extensions.AI;

namespace TIKR.Infrastructure.Services;

public interface IOllamaChatClientFactory
{
    string ChatModel { get; }
    string OllamaHost { get; }
    IChatClient CreateChatClient();
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
