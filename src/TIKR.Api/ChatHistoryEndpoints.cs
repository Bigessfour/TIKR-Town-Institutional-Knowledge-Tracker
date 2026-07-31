using Microsoft.Extensions.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Api;

public static class ChatHistoryEndpoints
{
    public static RouteGroupBuilder MapChatHistoryEndpoints(this RouteGroupBuilder api)
    {
        var chat = api.MapGroup("/assistant");

        chat.MapGet("/session", async (
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            return Results.Ok(await chatHistory.GetOrCreateSessionAsync(userId));
        });

        chat.MapPost("/session/new", async (
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            return Results.Ok(await chatHistory.StartNewConversationAsync(userId));
        });

        chat.MapPost("/session/turns", async (
            AppendChatTurnRequest request,
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.UserText) && string.IsNullOrWhiteSpace(request.AssistantText))
                return Results.BadRequest(new { error = "Turn text is required." });

            var session = await chatHistory.GetOrCreateSessionAsync(userId);
            var updated = await chatHistory.AppendTurnAsync(
                userId,
                session.Conversation.Id,
                request.UserText ?? string.Empty,
                request.AssistantText ?? string.Empty);
            return Results.Ok(updated);
        });

        chat.MapGet("/conversations", async (
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            return Results.Ok(await chatHistory.ListConversationsAsync(userId));
        });

        chat.MapGet("/conversations/{id:guid}", async (
            Guid id,
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            var detail = await chatHistory.GetConversationAsync(userId, id);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        chat.MapPost("/conversations/{id:guid}/turns", async (
            Guid id,
            AppendChatTurnRequest request,
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();

            try
            {
                var updated = await chatHistory.AppendTurnAsync(
                    userId,
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

        chat.MapGet("/memory", async (
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            return Results.Ok(await chatHistory.ListMemoryFactsAsync(userId));
        });

        chat.MapPut("/memory", async (
            UpsertUserMemoryFactRequest request,
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Value))
                return Results.BadRequest(new { error = "Key and value are required." });

            if (request.Key.Trim().Length > ChatHistoryLimits.MaxMemoryFactKeyChars
                || request.Value.Trim().Length > ChatHistoryLimits.MaxMemoryFactValueChars)
            {
                return Results.BadRequest(new
                {
                    error = $"Key max {ChatHistoryLimits.MaxMemoryFactKeyChars} chars; value max {ChatHistoryLimits.MaxMemoryFactValueChars} chars."
                });
            }

            var fact = await chatHistory.UpsertMemoryFactAsync(
                userId,
                request.Key,
                request.Value,
                request.Confirmed ?? true);
            return Results.Ok(fact);
        });

        chat.MapDelete("/memory/{id:guid}", async (
            Guid id,
            HttpRequest httpRequest,
            IConfiguration configuration,
            IChatHistoryService chatHistory,
            ICurrentUserService currentUser) =>
        {
            var userId = ChatUserResolver.TryResolve(currentUser, httpRequest, configuration);
            if (userId is null)
                return Results.Unauthorized();
            var deleted = await chatHistory.DeleteMemoryFactAsync(userId, id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return chat;
    }
}
