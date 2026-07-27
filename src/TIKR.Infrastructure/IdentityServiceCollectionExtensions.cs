using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Identity;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddTikrIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // DataProtection is registered once in AddTikrInfrastructure (shared key ring for secrets).

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<TikrDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<JwtTokenService>();

        var signingKey = TikrConfiguration.GetJwtSigningKey(configuration);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "tikr-api",
                    ValidAudience = "tikr-web",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var typ = context.Principal?.FindFirst(JwtTokenService.TokenTypeClaim)?.Value;
                        if (typ == JwtTokenService.RefreshTokenType)
                            context.Fail("Refresh token cannot be used as an access token.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(TikrAuthPolicies.AdminOnly, policy =>
                policy.RequireRole(TikrRoles.Admin))
            .AddPolicy(TikrAuthPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy(TikrAuthPolicies.CanWrite, policy =>
                policy.RequireRole(TikrRoles.Admin, TikrRoles.Clerk));

        return services;
    }
}
