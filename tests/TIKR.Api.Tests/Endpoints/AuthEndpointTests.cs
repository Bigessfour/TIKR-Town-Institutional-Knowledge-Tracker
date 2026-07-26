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

    // AuthEndpoints.MapAuthEndpoints is the startup extension (called from Program when authEnabled);
    // exercised by all tests in this fixture which hit /api/auth/* routes via the configured factory.

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
    public async Task MeTour_CanReadAndUpdateTourPreferences()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initial = await _client.GetFromJsonAsync<ClerkTourStateDto>("/api/auth/me/tour");
        initial.Should().NotBeNull();
        initial!.AutoTourDisabled.Should().BeFalse();

        var update = await _client.PutAsJsonAsync("/api/auth/me/tour",
            new UpdateClerkTourStateRequest("v1", true));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await _client.GetFromJsonAsync<ClerkTourStateDto>("/api/auth/me/tour");
        saved!.CompletedVersion.Should().Be("v1");
        saved.AutoTourDisabled.Should().BeTrue();
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

    [Fact]
    public async Task Login_ReturnsRefreshToken_AndRefreshIssuesNewAccessToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthEnabledWebApplicationFactory.AdminEmail,
            AuthEnabledWebApplicationFactory.AdminPassword));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        login!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(login.RefreshToken!));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.AccessToken.Should().NotBe(login.AccessToken);
    }

    [Fact]
    public async Task ForgotAndResetPassword_WorksWithoutSmtp()
    {
        var forgot = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest(AuthEnabledWebApplicationFactory.AdminEmail));
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await forgot.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        payload!.ResetToken.Should().NotBeNullOrWhiteSpace();

        var newPassword = "ResetPass1!";
        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(
                AuthEnabledWebApplicationFactory.AdminEmail,
                payload.ResetToken!,
                newPassword));
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthEnabledWebApplicationFactory.AdminEmail,
            newPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore bootstrap password for other tests in this fixture.
        var restoreForgot = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest(AuthEnabledWebApplicationFactory.AdminEmail));
        var restorePayload = await restoreForgot.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(
                AuthEnabledWebApplicationFactory.AdminEmail,
                restorePayload!.ResetToken!,
                AuthEnabledWebApplicationFactory.AdminPassword));
    }

    [Fact]
    public async Task Viewer_CanReadButNotWrite()
    {
        var adminToken = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var viewerEmail = $"viewer-{Guid.NewGuid():N}@town.gov";
        var create = await _client.PostAsJsonAsync("/api/auth/users", new CreateUserRequest(
            viewerEmail,
            TestAuthFixtures.NewUserPassword,
            "Read Only",
            "Viewer"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        _client.DefaultRequestHeaders.Authorization = null;
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            viewerEmail, TestAuthFixtures.NewUserPassword));
        var viewerLogin = await login.Content.ReadFromJsonAsync<LoginResponse>();
        viewerLogin!.Roles.Should().Contain("Viewer");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", viewerLogin.AccessToken);

        var get = await _client.GetAsync("/api/requirements");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var post = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Viewer blocked",
            "Should fail",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            TIKR.Shared.Enums.RecurrenceType.None,
            TIKR.Shared.Enums.RequirementCategory.Custom));
        post.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
