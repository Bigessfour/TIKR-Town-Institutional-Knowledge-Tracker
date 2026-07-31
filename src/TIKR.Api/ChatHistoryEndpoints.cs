using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Api;

public static class ChatHistoryEndpoints
{
    public static RouteGroupBuilder MapChatHistoryEndpoints(this RouteGroupBuilder api)
    {
        var chat = api.MapGroup("/assistant");

        chat.MapGet("/session", async (IChatHistoryService chatHistory, ICurrentUserService currentUser) =>
        {
            var session = await chatHistory.GetOrCreateSessionAsync(ResolveUserId(currentUser));
            return Results.Ok(session);
        });

        chat.MapPost("/session/new", async (IChatHistoryService chatHistory, ICurrentUserService currentUser) =>
        {
            var session = await chatHistory.StartNewConversationAsync(ResolveUserId(currentUser));
            return Results.Ok(session);
        });

        chat.MapPost("/session/turns", async (
            AppendChatTurnRequest request,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserText) && string.IsNullOrWhiteSpace(request.AssistantText))
                return Results.BadRequest(new { error = "Turn text is required." });

            var userId = ResolveUserId(currentUser);
            var session = await chatHistory.GetOrCreateSessionAsync(userId);
            var updated = await chatHistory.AppendTurnAsync(
                userId,
                session.Conversation.Id,
                request.UserText ?? string.Empty,
                request.AssistantText ?? string.Empty);
            return Results.Ok(updated);
        });

        chat.MapGet("/conversations", async (IChatHistoryService chatHistory, ICurrentUserService currentUser) =>
        {
            var list = await chatHistory.ListConversationsAsync(ResolveUserId(currentUser));
            return Results.Ok(list);
        });

        chat.MapGet("/conversations/{id:guid}", async (
            Guid id,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var detail = await chatHistory.GetConversationAsync(ResolveUserId(currentUser), id);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        chat.MapPost("/conversations/{id:guid}/turns", async (
            Guid id,
            AppendChatTurnRequest request,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            try
            {
                var updated = await chatHistory.AppendTurnAsync(
                    ResolveUserId(currentUser),
                    id,
                    request.UserText ?? string.Empty,
                    request.AssistantText ?? string.Empty);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        chat.MapGet("/memory", async (IChatHistoryService chatHistory, ICurrentUserService currentUser) =>
        {
            var facts = await chatHistory.ListMemoryFactsAsync(ResolveUserId(currentUser));
            return Results.Ok(facts);
        });

        chat.MapPut("/memory", async (
            UpsertUserMemoryFactRequest request,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Value))
                return Results.BadRequest(new { error = "Key and value are required." });

            var fact = await chatHistory.UpsertMemoryFactAsync(
                ResolveUserId(currentUser),
                request.Key,
                request.Value,
                request.Confirmed ?? true);
            return Results.Ok(fact);
        });

        chat.MapDelete("/memory/{id:guid}", async (
            Guid id,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var deleted = await chatHistory.DeleteMemoryFactAsync(ResolveUserId(currentUser), id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return chat;
    }

    private static string ResolveUserId(ICurrentUserService currentUser) =>
        string.IsNullOrWhiteSpace(currentUser.UserId) ? "anonymous" : currentUser.UserId;
}
