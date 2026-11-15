using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Enums;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class ChatHistoryServiceTests
{
    [Fact]
    public async Task GetConversationAsync_IsScopedToTheOwner()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);
        var session = await sut.GetOrCreateSessionAsync("deb");

        var own = await sut.GetConversationAsync("deb", session.Conversation.Id);
        var other = await sut.GetConversationAsync("paige", session.Conversation.Id);

        own.Should().NotBeNull();
        own!.Id.Should().Be(session.Conversation.Id);
        other.Should().BeNull();
    }

    [Fact]
    public async Task UpsertMemoryFactAsync_UpdatesTheSameKey()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);

        var created = await sut.UpsertMemoryFactAsync("deb", "Binder", "blue");
        var updated = await sut.UpsertMemoryFactAsync("deb", "Binder", "red");

        updated.Id.Should().Be(created.Id);
        updated.Key.Should().Be("binder");
        updated.Value.Should().Be("red");
        (await sut.ListMemoryFactsAsync("deb")).Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMemoryFactAsync_DoesNotDeleteAnotherClerksFact()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);
        var fact = await sut.UpsertMemoryFactAsync("deb", "birthday", "March 15");

        (await sut.DeleteMemoryFactAsync("paige", fact.Id)).Should().BeFalse();
        (await sut.DeleteMemoryFactAsync("deb", fact.Id)).Should().BeTrue();
        (await sut.ListMemoryFactsAsync("deb")).Should().BeEmpty();
    }

    [Fact]
    public async Task StartNewConversationAsync_ArchivesTheActiveThread()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);
        var first = await sut.GetOrCreateSessionAsync("deb");
        await sut.AppendTurnAsync("deb", first.Conversation.Id, "hello", "hi");

        var second = await sut.StartNewConversationAsync("deb");

        second.Conversation.Id.Should().NotBe(first.Conversation.Id);
        second.Conversation.Messages.Should().BeEmpty();
        var archived = await sut.GetConversationAsync("deb", first.Conversation.Id);
        archived.Should().NotBeNull();
        (await sut.ListConversationsAsync("deb")).Should().HaveCount(2);
        (await sut.GetOrCreateSessionAsync("deb")).Conversation.Id.Should().Be(second.Conversation.Id);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_RejectsAnonymous()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);

        var act = () => sut.GetOrCreateSessionAsync("anonymous");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AppendTurn_WritesChatTurnChunkForThatUser_AndClearRemovesOnlyThatConversation()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var user = "cognito:11111111-1111-1111-1111-111111111111";
        var other = "cognito:22222222-2222-2222-2222-222222222222";
        var memory = new ChatTurnMemory(db, new ConstantEmbedder());
        var sut = new ChatHistoryService(db, chatTurns: memory);
        var session = await sut.GetOrCreateSessionAsync(user);
        var otherSession = await sut.GetOrCreateSessionAsync(other);
        await sut.AppendTurnAsync(user, session.Conversation.Id, "Where is the mill levy file?", "It is in the vault.");
        await sut.AppendTurnAsync(other, otherSession.Conversation.Id, "Other clerk question", "Other answer");

        var own = await db.EmbeddingChunks.Where(c => c.Facet == user).ToListAsync();
        own.Should().NotBeEmpty();
        own.Should().OnlyContain(c => c.SourceType == EmbeddingSourceType.ChatTurn && c.SourceId == session.Conversation.Id);

        await sut.StartNewConversationAsync(user);

        (await db.EmbeddingChunks.CountAsync(c => c.SourceId == session.Conversation.Id && c.Facet == user))
            .Should().Be(0);
        (await db.EmbeddingChunks.CountAsync(c => c.Facet == other)).Should().BeGreaterThan(0);
    }

    private sealed class ConstantEmbedder : ITextEmbedder
    {
        public Task<float[]?> EmbedAsync(string text, CancellationToken ct = default) =>
            Task.FromResult<float[]?>([1f, 0f, 0f]);
    }
}
