using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class ChatHistoryEndpointTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly AuthEnabledWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatHistoryEndpointTests(AuthEnabledWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Session_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/assistant/session");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_WithToken_PersistsTurnAndMemory()
    {
        var token = await LoginAsync(_client, AuthEnabledWebApplicationFactory.AdminEmail, AuthEnabledWebApplicationFactory.AdminPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sessionResponse = await _client.GetAsync("/api/assistant/session");
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await sessionResponse.Content.ReadFromJsonAsync<AssistantSessionDto>();
        session.Should().NotBeNull();

        var turn = await _client.PostAsJsonAsync(
            $"/api/assistant/conversations/{session!.Conversation.Id}/turns",
            new AppendChatTurnRequest("My birthday is March 15", "Got it."));
        turn.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await turn.Content.ReadFromJsonAsync<AssistantSessionDto>();
        updated!.Conversation.Messages.Should().HaveCount(2);
        updated.MemoryFacts.Should().Contain(f => f.Key == "birthday" && f.Value == "March 15");
    }

    [Fact]
    public async Task Sessions_AreIsolatedPerUser()
    {
        var adminToken = await LoginAsync(_client, AuthEnabledWebApplicationFactory.AdminEmail, AuthEnabledWebApplicationFactory.AdminPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsJsonAsync("/api/auth/users", new CreateUserRequest(
            TestAuthFixtures.ClerkEmail,
            TestAuthFixtures.NewUserPassword,
            "Paige",
            "Clerk"));

        var adminSession = await _client.GetFromJsonAsync<AssistantSessionDto>("/api/assistant/session");
        await _client.PostAsJsonAsync(
            $"/api/assistant/conversations/{adminSession!.Conversation.Id}/turns",
            new AppendChatTurnRequest("Remember that Deb keeps the blue binder", "ok"));

        var clerkClient = _factory.CreateClient();
        var clerkToken = await LoginAsync(clerkClient, TestAuthFixtures.ClerkEmail, TestAuthFixtures.NewUserPassword);
        clerkClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clerkToken);
        var clerkSession = await clerkClient.GetFromJsonAsync<AssistantSessionDto>("/api/assistant/session");

        clerkSession!.Conversation.Id.Should().NotBe(adminSession.Conversation.Id);
        clerkSession.Conversation.Messages.Should().BeEmpty();
        clerkSession.MemoryFacts.Should().BeEmpty();
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }
}

[Trait("Category", TestCategories.FullyTested)]
public class ChatHistoryAuthOffEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly TikrWebApplicationFactory _factory;

    public ChatHistoryAuthOffEndpointTests(TikrWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Session_WithoutChatUserHeader_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/assistant/session");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_WithDistinctChatUserHeaders_AreIsolated()
    {
        var deb = _factory.CreateClient();
        deb.DefaultRequestHeaders.Add(ChatHistoryLimits.ChatUserHeaderName, Guid.NewGuid().ToString("N"));
        var debSession = await deb.GetFromJsonAsync<AssistantSessionDto>("/api/assistant/session");
        await deb.PostAsJsonAsync(
            $"/api/assistant/conversations/{debSession!.Conversation.Id}/turns",
            new AppendChatTurnRequest("Call me Deb", "Hi Deb"));

        var paige = _factory.CreateClient();
        paige.DefaultRequestHeaders.Add(ChatHistoryLimits.ChatUserHeaderName, Guid.NewGuid().ToString("N"));
        var paigeSession = await paige.GetFromJsonAsync<AssistantSessionDto>("/api/assistant/session");

        paigeSession!.Conversation.Id.Should().NotBe(debSession.Conversation.Id);
        paigeSession.Conversation.Messages.Should().BeEmpty();
        paigeSession.MemoryFacts.Should().BeEmpty();

        var debReload = await deb.GetFromJsonAsync<AssistantSessionDto>("/api/assistant/session");
        debReload!.Conversation.Messages.Should().HaveCount(2);
        debReload.MemoryFacts.Should().Contain(f => f.Key == "preferred_name" && f.Value == "Deb");
    }
}
