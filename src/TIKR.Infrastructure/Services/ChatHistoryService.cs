using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class ChatHistoryService(TikrDbContext db, ILogger<ChatHistoryService>? logger = null) : IChatHistoryService
{
    private readonly ILogger _log = logger ?? NullLogger<ChatHistoryService>.Instance;

    public async Task<AssistantSessionDto> GetOrCreateSessionAsync(string userId, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        var conversation = await GetActiveConversationAsync(userId, ct)
            ?? await CreateConversationAsync(userId, ct);
        var facts = await ListMemoryFactsAsync(userId, ct);
        return new AssistantSessionDto(ToDetail(conversation), facts);
    }

    public async Task<IReadOnlyList<ChatConversationSummaryDto>> ListConversationsAsync(
        string userId, int take = 20, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        take = Math.Clamp(take, 1, 100);

        return await db.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(take)
            .Select(c => new ChatConversationSummaryDto(
                c.Id,
                c.Title,
                c.UpdatedAtUtc,
                c.IsArchived,
                c.Messages.Count))
            .ToListAsync(ct);
    }

    public async Task<ChatConversationDetailDto?> GetConversationAsync(
        string userId, Guid conversationId, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        var conversation = await db.ChatConversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAtUtc))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);
        return conversation is null ? null : ToDetail(conversation);
    }

    public async Task<AssistantSessionDto> AppendTurnAsync(
        string userId,
        Guid conversationId,
        string userText,
        string assistantText,
        CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        userText = ChatHistoryLimits.TruncateMessage(userText);
        assistantText = ChatHistoryLimits.TruncateMessage(assistantText);

        TikrActionLog.Started(_log, "Chat.AppendTurn",
            $"User={userId} Conversation={conversationId} UserLen={userText.Length}");

        var conversation = await db.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct)
            ?? throw new InvalidOperationException("Conversation not found for this user.");

        if (conversation.IsArchived)
            throw new InvalidOperationException("Cannot append to an archived conversation.");

        var now = DateTime.UtcNow;
        var userMsg = new ChatMessageRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            UserId = userId,
            Role = "user",
            Content = userText,
            CreatedAtUtc = now
        };
        var assistantMsg = new ChatMessageRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            UserId = userId,
            Role = "assistant",
            Content = assistantText,
            CreatedAtUtc = now.AddMilliseconds(1)
        };

        db.ChatMessages.Add(userMsg);
        db.ChatMessages.Add(assistantMsg);

        if (conversation.Title == "New chat" && !string.IsNullOrWhiteSpace(userMsg.Content))
            conversation.Title = TruncateTitle(userMsg.Content);

        conversation.UpdatedAtUtc = now;

        foreach (var (key, value) in UserMemoryFactExtractor.Extract(userMsg.Content))
            await UpsertMemoryFactCoreAsync(userId, key, value, confirmed: true, userMsg.Id, ct);

        await db.SaveChangesAsync(ct);

        await db.Entry(conversation).Collection(c => c.Messages).Query()
            .OrderBy(m => m.CreatedAtUtc).LoadAsync(ct);

        var facts = await ListMemoryFactsAsync(userId, ct);
        TikrActionLog.Completed(_log, "Chat.AppendTurn",
            $"Conversation={conversation.Id} Messages={conversation.Messages.Count}");
        return new AssistantSessionDto(ToDetail(conversation), facts);
    }

    public async Task<AssistantSessionDto> StartNewConversationAsync(string userId, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        var actives = await db.ChatConversations
            .Where(c => c.UserId == userId && !c.IsArchived)
            .ToListAsync(ct);
        if (actives.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var active in actives)
            {
                active.IsArchived = true;
                active.UpdatedAtUtc = now;
            }

            await db.SaveChangesAsync(ct);
        }

        var created = await CreateConversationAsync(userId, ct);
        var facts = await ListMemoryFactsAsync(userId, ct);
        return new AssistantSessionDto(ToDetail(created), facts);
    }

    public async Task<IReadOnlyList<UserMemoryFactDto>> ListMemoryFactsAsync(string userId, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        return await db.UserMemoryFacts
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Confirmed)
            .OrderBy(f => f.Key)
            .Select(f => new UserMemoryFactDto(f.Id, f.Key, f.Value, f.Confirmed, f.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<UserMemoryFactDto> UpsertMemoryFactAsync(
        string userId,
        string key,
        string value,
        bool confirmed = true,
        Guid? sourceMessageId = null,
        CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        var fact = await UpsertMemoryFactCoreAsync(userId, key, value, confirmed, sourceMessageId, ct);
        await db.SaveChangesAsync(ct);
        return new UserMemoryFactDto(fact.Id, fact.Key, fact.Value, fact.Confirmed, fact.UpdatedAtUtc);
    }

    public async Task<bool> DeleteMemoryFactAsync(string userId, Guid factId, CancellationToken ct = default)
    {
        userId = RequireUserId(userId);
        var fact = await db.UserMemoryFacts
            .FirstOrDefaultAsync(f => f.Id == factId && f.UserId == userId, ct);
        if (fact is null)
            return false;
        db.UserMemoryFacts.Remove(fact);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<UserMemoryFact> UpsertMemoryFactCoreAsync(
        string userId,
        string key,
        string value,
        bool confirmed,
        Guid? sourceMessageId,
        CancellationToken ct)
    {
        key = ChatHistoryLimits.Truncate((key ?? string.Empty).Trim().ToLowerInvariant(), ChatHistoryLimits.MaxMemoryFactKeyChars);
        value = ChatHistoryLimits.Truncate(value, ChatHistoryLimits.MaxMemoryFactValueChars);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Memory fact key and value are required.");

        var existing = await db.UserMemoryFacts
            .FirstOrDefaultAsync(f => f.UserId == userId && f.Key == key, ct);
        if (existing is null)
        {
            existing = new UserMemoryFact
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Key = key,
                Value = value,
                Confirmed = confirmed,
                SourceMessageId = sourceMessageId,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.UserMemoryFacts.Add(existing);
        }
        else
        {
            existing.Value = value;
            existing.Confirmed = confirmed;
            existing.SourceMessageId = sourceMessageId ?? existing.SourceMessageId;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        return existing;
    }

    private async Task<ChatConversation?> GetActiveConversationAsync(string userId, CancellationToken ct) =>
        await db.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAtUtc))
            .Where(c => c.UserId == userId && !c.IsArchived)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

    private async Task<ChatConversation> CreateConversationAsync(string userId, CancellationToken ct)
    {
        // Unique filtered index enforces one active thread per user; retry if a race lost.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var existing = await GetActiveConversationAsync(userId, ct);
            if (existing is not null)
                return existing;

            var conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "New chat",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.ChatConversations.Add(conversation);
            try
            {
                await db.SaveChangesAsync(ct);
                return conversation;
            }
            catch (DbUpdateException)
            {
                db.Entry(conversation).State = EntityState.Detached;
                // Another request created the active conversation — loop and load it.
            }
        }

        return await GetActiveConversationAsync(userId, ct)
            ?? throw new InvalidOperationException("Could not create or load an active conversation.");
    }

    private static ChatConversationDetailDto ToDetail(ChatConversation conversation) =>
        new(
            conversation.Id,
            conversation.Title,
            conversation.UpdatedAtUtc,
            conversation.Messages
                .OrderBy(m => m.CreatedAtUtc)
                .Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAtUtc))
                .ToList());

    private static string RequireUserId(string? userId)
    {
        var id = userId?.Trim();
        if (string.IsNullOrWhiteSpace(id) || id.Equals("anonymous", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A concrete clerk user id is required for chat history.");
        return id;
    }

    private static string TruncateTitle(string text)
    {
        var oneLine = text.Replace('\n', ' ').Trim();
        return oneLine.Length <= 80 ? oneLine : oneLine[..77] + "…";
    }
}
