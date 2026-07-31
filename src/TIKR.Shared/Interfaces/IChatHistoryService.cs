using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IChatHistoryService
{
    /// <summary>Active (non-archived) conversation for the user, creating one if needed.</summary>
    Task<AssistantSessionDto> GetOrCreateSessionAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<ChatConversationSummaryDto>> ListConversationsAsync(string userId, int take = 20, CancellationToken ct = default);

    Task<ChatConversationDetailDto?> GetConversationAsync(string userId, Guid conversationId, CancellationToken ct = default);

    /// <summary>Persist a plain user/assistant pair and run lightweight memory extraction on the user text.</summary>
    Task<AssistantSessionDto> AppendTurnAsync(string userId, Guid conversationId, string userText, string assistantText, CancellationToken ct = default);

    /// <summary>Archive the active conversation and start a fresh one.</summary>
    Task<AssistantSessionDto> StartNewConversationAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<UserMemoryFactDto>> ListMemoryFactsAsync(string userId, CancellationToken ct = default);

    Task<UserMemoryFactDto> UpsertMemoryFactAsync(string userId, string key, string value, bool confirmed = true, Guid? sourceMessageId = null, CancellationToken ct = default);

    Task<bool> DeleteMemoryFactAsync(string userId, Guid factId, CancellationToken ct = default);
}
