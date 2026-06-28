using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Shared.TestFixtures;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class AuthEndpointTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(AuthEnabledWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Requirements_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/requirements");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthEnabledWebApplicationFactory.AdminEmail,
            AuthEnabledWebApplicationFactory.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Email.Should().Be(AuthEnabledWebApplicationFactory.AdminEmail);
        login.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task PostRequirement_WithToken_AuditsUserEmail()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateRequirementRequest(
            "Auth Test Task",
            "Created under auth",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TIKR.Shared.Enums.RecurrenceType.None,
            TIKR.Shared.Enums.RequirementCategory.Custom);

        var response = await _client.PostAsJsonAsync("/api/requirements", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var audit = await _client.GetFromJsonAsync<List<AuditLog>>("/api/audit?limit=5");
        audit.Should().Contain(a =>
            a.Action == "Create"
            && a.EntityType == nameof(Requirement)
            && a.UserId == AuthEnabledWebApplicationFactory.AdminEmail);
    }

    [Fact]
    public async Task Admin_CanCreateClerkUser()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = new CreateUserRequest(
            TestAuthFixtures.ClerkEmail,
            TestAuthFixtures.NewUserPassword,
            "Test Clerk",
            "Clerk");

        var response = await _client.PostAsJsonAsync("/api/auth/users", create);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var users = await _client.GetFromJsonAsync<List<UserSummaryDto>>("/api/auth/users");
        users.Should().Contain(u => u.Email == TestAuthFixtures.ClerkEmail && u.Roles.Contains("Clerk"));
    }

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthEnabledWebApplicationFactory.AdminEmail,
            AuthEnabledWebApplicationFactory.AdminPassword));
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }
}
