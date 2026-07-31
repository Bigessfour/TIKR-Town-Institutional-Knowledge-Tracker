using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;

namespace TIKR.Infrastructure.Tests.Services;

public class ChatHistoryServiceTests
{
    [Fact]
    public async Task AppendTurn_IsolatesUsers_AndExtractsBirthday()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);

        var deb = await sut.GetOrCreateSessionAsync("deb@town.gov");
        await sut.AppendTurnAsync("deb@town.gov", deb.Conversation.Id,
            "My birthday is March 15", "Got it — March 15.");

        var paige = await sut.GetOrCreateSessionAsync("paige@town.gov");
        await sut.AppendTurnAsync("paige@town.gov", paige.Conversation.Id,
            "What is due this week?", "Budget hearing.");

        var debSession = await sut.GetOrCreateSessionAsync("deb@town.gov");
        var paigeSession = await sut.GetOrCreateSessionAsync("paige@town.gov");

        debSession.Conversation.Id.Should().NotBe(paigeSession.Conversation.Id);
        debSession.Conversation.Messages.Should().HaveCount(2);
        paigeSession.Conversation.Messages.Should().HaveCount(2);
        debSession.MemoryFacts.Should().ContainSingle(f => f.Key == "birthday" && f.Value == "March 15");
        paigeSession.MemoryFacts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartNewConversation_ArchivesPrior_KeepsMemoryFacts()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ChatHistoryService(db);

        var first = await sut.GetOrCreateSessionAsync("deb@town.gov");
        await sut.AppendTurnAsync("deb@town.gov", first.Conversation.Id,
            "Call me Deb", "Okay Deb.");

        var second = await sut.StartNewConversationAsync("deb@town.gov");
        second.Conversation.Id.Should().NotBe(first.Conversation.Id);
        second.Conversation.Messages.Should().BeEmpty();
        second.MemoryFacts.Should().ContainSingle(f => f.Key == "preferred_name" && f.Value == "Deb");

        var list = await sut.ListConversationsAsync("deb@town.gov");
        list.Should().Contain(c => c.Id == first.Conversation.Id && c.IsArchived);
        list.Should().Contain(c => c.Id == second.Conversation.Id && !c.IsArchived);
    }
}
