using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TIKR.Infrastructure.Identity;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;

namespace TIKR.Api;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwt) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, request.Password))
            {
                await userManager.AccessFailedAsync(user);
                return Results.Unauthorized();
            }

            await userManager.ResetAccessFailedCountAsync(user);
            var roles = await userManager.GetRolesAsync(user);
            var (access, expiresAt, refresh, refreshExpires) = jwt.CreateTokenPair(user, roles);
            return Results.Ok(new LoginResponse(
                access, expiresAt, user.Email ?? request.Email, roles.ToList(), refresh, refreshExpires));
        });

        auth.MapPost("/refresh", async (
            RefreshTokenRequest request,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.Unauthorized();

            var principal = jwt.ValidateRefreshToken(request.RefreshToken);
            if (principal is null)
                return Results.Unauthorized();

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            var roles = await userManager.GetRolesAsync(user);
            var (access, expiresAt, refresh, refreshExpires) = jwt.CreateTokenPair(user, roles);
            return Results.Ok(new LoginResponse(
                access, expiresAt, user.Email ?? string.Empty, roles.ToList(), refresh, refreshExpires));
        });

        auth.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IHostEnvironment env,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var user = await userManager.FindByEmailAsync(request.Email);
            const string generic = "If an account exists for that email, a password reset token was issued.";

            if (user is null || !user.IsActive)
                return Results.Ok(new ForgotPasswordResponse(generic, null));

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            logger.LogWarning(
                "Password reset token for {Email} (local/no-SMTP). Use POST /api/auth/reset-password with this token.",
                user.Email);

            var expose = TikrConfiguration.ExposePasswordResetToken(configuration, env.IsDevelopment());
            return Results.Ok(new ForgotPasswordResponse(generic, expose ? token : null));
        });

        auth.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive)
                return Results.BadRequest(new { error = "Invalid reset request." });

            var result = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        });

        auth.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await FindUserAsync(principal, userManager);
            if (user is null) return Results.Unauthorized();
            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new UserProfileDto(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList()));
        }).RequireAuthorization(TikrAuthPolicies.Authenticated);

        auth.MapGet("/me/tour", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await FindUserAsync(principal, userManager);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(new ClerkTourStateDto(user.ClerkTourCompletedVersion, user.ClerkTourAutoDisabled));
        }).RequireAuthorization(TikrAuthPolicies.Authenticated);

        auth.MapPut("/me/tour", async (
            UpdateClerkTourStateRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await FindUserAsync(principal, userManager);
            if (user is null) return Results.Unauthorized();

            if (request.CompletedVersion is not null)
                user.ClerkTourCompletedVersion = request.CompletedVersion;

            if (request.AutoTourDisabled.HasValue)
                user.ClerkTourAutoDisabled = request.AutoTourDisabled.Value;

            await userManager.UpdateAsync(user);
            return Results.Ok(new ClerkTourStateDto(user.ClerkTourCompletedVersion, user.ClerkTourAutoDisabled));
        }).RequireAuthorization(TikrAuthPolicies.Authenticated);

        auth.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await FindUserAsync(principal, userManager);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }).RequireAuthorization(TikrAuthPolicies.Authenticated);

        auth.MapGet("/users", async (UserManager<ApplicationUser> userManager) =>
        {
            var users = userManager.Users.OrderBy(u => u.Email).ToList();
            var summaries = new List<UserSummaryDto>();
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                summaries.Add(new UserSummaryDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName,
                    user.IsActive,
                    roles.ToList()));
            }

            return Results.Ok(summaries);
        }).RequireAuthorization(TikrAuthPolicies.AdminOnly);

        auth.MapPost("/users", async (
            CreateUserRequest request,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            if (!await roleManager.RoleExistsAsync(request.Role))
                return Results.BadRequest(new { error = $"Unknown role: {request.Role}" });

            if (!TikrRoles.IsAssignableRole(request.Role))
                return Results.BadRequest(new { error = "Role must be Admin, Clerk, or Viewer." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                DisplayName = request.DisplayName,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await userManager.AddToRoleAsync(user, request.Role);
            var roles = await userManager.GetRolesAsync(user);
            return Results.Created($"/api/auth/users/{user.Id}", new UserSummaryDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsActive,
                roles.ToList()));
        }).RequireAuthorization(TikrAuthPolicies.AdminOnly);

        auth.MapPut("/users/{id}", async (
            string id,
            UpdateUserRequest request,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null) return Results.NotFound();

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!resetResult.Succeeded)
                    return Results.BadRequest(new { errors = resetResult.Errors.Select(e => e.Description) });
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                if (!await roleManager.RoleExistsAsync(request.Role))
                    return Results.BadRequest(new { error = $"Unknown role: {request.Role}" });

                if (!TikrRoles.IsAssignableRole(request.Role))
                    return Results.BadRequest(new { error = "Role must be Admin, Clerk, or Viewer." });

                var currentRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, request.Role);
            }

            await userManager.UpdateAsync(user);
            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new UserSummaryDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsActive,
                roles.ToList()));
        }).RequireAuthorization(TikrAuthPolicies.AdminOnly);

        return auth;
    }

    private static async Task<ApplicationUser?> FindUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await userManager.FindByIdAsync(userId);
        return user is { IsActive: true } ? user : null;
    }
}
