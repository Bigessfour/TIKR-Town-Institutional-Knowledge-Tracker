namespace TIKR.Infrastructure.Services;

/// <summary>Per-user chat-turn embeddings. Failures must not block saving the message row.</summary>
public interface IChatTurnMemory
{
    Task IndexTurnAsync(string userId, Guid conversationId, string userText, string assistantText, CancellationToken ct = default);

    Task DeleteConversationAsync(string userId, Guid conversationId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> SearchAsync(string userId, string query, int topK = 3, CancellationToken ct = default);
}
