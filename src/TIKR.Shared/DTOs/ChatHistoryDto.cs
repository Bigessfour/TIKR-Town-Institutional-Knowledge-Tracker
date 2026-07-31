namespace TIKR.Shared.DTOs;

public record ChatMessageDto(Guid Id, string Role, string Content, DateTime CreatedAtUtc);

public record ChatConversationSummaryDto(
    Guid Id,
    string Title,
    DateTime UpdatedAtUtc,
    bool IsArchived,
    int MessageCount);

public record ChatConversationDetailDto(
    Guid Id,
    string Title,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ChatMessageDto> Messages);

public record UserMemoryFactDto(Guid Id, string Key, string Value, bool Confirmed, DateTime UpdatedAtUtc);

public record AssistantSessionDto(
    ChatConversationDetailDto Conversation,
    IReadOnlyList<UserMemoryFactDto> MemoryFacts);

public record AppendChatTurnRequest(string UserText, string AssistantText);

public record UpsertUserMemoryFactRequest(string Key, string Value, bool? Confirmed = true);
