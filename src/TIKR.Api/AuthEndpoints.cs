using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TIKR.Infrastructure.Identity;
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
            var (token, expiresAt) = jwt.CreateToken(user, roles);
            return Results.Ok(new LoginResponse(token, expiresAt, user.Email ?? request.Email, roles.ToList()));
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

            if (request.Role != TikrRoles.Admin && request.Role != TikrRoles.Clerk)
                return Results.BadRequest(new { error = "Role must be Admin or Clerk." });

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
