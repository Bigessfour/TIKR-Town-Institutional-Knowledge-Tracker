using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;

namespace TIKR.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        TikrDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!TikrConfiguration.IsAuthEnabled(configuration))
            return;

        foreach (var role in new[] { TikrRoles.Admin, TikrRoles.Clerk })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.Users.AnyAsync(cancellationToken))
            return;

        var email = TikrConfiguration.GetAdminBootstrapEmail(configuration);
        var password = TikrConfiguration.GetAdminBootstrapPassword(configuration);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Auth enabled but no admin bootstrap credentials configured; skipping admin seed.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Town Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, TikrRoles.Admin);
        logger.LogInformation("Seeded initial admin user {Email}", email);
    }
}
