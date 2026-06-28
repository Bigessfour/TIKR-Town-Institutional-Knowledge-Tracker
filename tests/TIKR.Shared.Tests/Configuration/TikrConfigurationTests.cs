using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Shared.TestFixtures;

namespace TIKR.Shared.Tests.Configuration;

public class TikrConfigurationTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();

    [Fact]
    public void GetDatabaseProvider_DefaultsToSqlite()
    {
        TikrConfiguration.GetDatabaseProvider(BuildConfig([]))
            .Should().Be("Sqlite");
    }

    [Fact]
    public void GetDatabaseProvider_ReadsEnvironmentKey()
    {
        TikrConfiguration.GetDatabaseProvider(BuildConfig(new Dictionary<string, string?>
        {
            ["DATABASE_PROVIDER"] = "Postgres"
        })).Should().Be("Postgres");
    }

    [Fact]
    public void GetFileStoragePath_UsesNestedConfigFirst()
    {
        TikrConfiguration.GetFileStoragePath(BuildConfig(new Dictionary<string, string?>
        {
            ["FileStorage:BasePath"] = "/nested/path",
            ["FILE_STORAGE_PATH"] = "/flat/path"
        })).Should().Be("/nested/path");
    }

    [Fact]
    public void GetFileStoragePath_FallsBackToFlatEnvKey()
    {
        TikrConfiguration.GetFileStoragePath(BuildConfig(new Dictionary<string, string?>
        {
            ["FILE_STORAGE_PATH"] = "/flat/path"
        })).Should().Be("/flat/path");
    }

    [Fact]
    public void GetOllamaHost_UsesDefaultsAndOverrides()
    {
        TikrConfiguration.GetOllamaHost(BuildConfig([]))
            .Should().Be("http://localhost:11434");

        TikrConfiguration.GetOllamaHost(BuildConfig(new Dictionary<string, string?>
        {
            ["AI:OllamaHost"] = "http://ollama:11434"
        })).Should().Be("http://ollama:11434");
    }

    [Fact]
    public void GetChatModel_UsesDefaultsAndOverrides()
    {
        TikrConfiguration.GetChatModel(BuildConfig([]))
            .Should().Be("llama3.2:3b");

        TikrConfiguration.GetChatModel(BuildConfig(new Dictionary<string, string?>
        {
            ["OLLAMA_CHAT_MODEL"] = "mistral"
        })).Should().Be("mistral");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("invalid", false)]
    public void GetUseGrok_ParsesFlatEnvKey(string value, bool expected)
    {
        TikrConfiguration.GetUseGrok(BuildConfig(new Dictionary<string, string?>
        {
            ["USE_GROK"] = value
        })).Should().Be(expected);
    }

    [Fact]
    public void GetUseGrok_PrefersNestedAiSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:UseGrok"] = "true",
                ["USE_GROK"] = "false"
            })
            .Build();

        TikrConfiguration.GetUseGrok(config).Should().BeTrue();
    }

    [Fact]
    public void GetGrokApiKey_ReadsKey()
    {
        TikrConfiguration.GetGrokApiKey(BuildConfig(new Dictionary<string, string?>
        {
            ["GROK_API_KEY"] = "xai-test"
        })).Should().Be("xai-test");
    }

    [Fact]
    public void GetGrokApiKey_FallsBackToXaiApiKey()
    {
        TikrConfiguration.GetGrokApiKey(BuildConfig(new Dictionary<string, string?>
        {
            ["XAI_API_KEY"] = "xai-from-keychain"
        })).Should().Be("xai-from-keychain");
    }

    [Fact]
    public void GetGrokApiKey_ReturnsNullWhenUnset()
    {
        TikrConfiguration.GetGrokApiKey(BuildConfig([])).Should().BeNull();
    }

    [Fact]
    public void GetFileStoragePath_UsesDefaultWhenUnset()
    {
        var path = TikrConfiguration.GetFileStoragePath(BuildConfig([]));
        path.Should().EndWith(Path.Combine("data", "documents"));
    }

    [Fact]
    public void GetGrokModel_UsesDefaultsAndOverrides()
    {
        TikrConfiguration.GetGrokModel(BuildConfig([]))
            .Should().Be("grok-3");

        TikrConfiguration.GetGrokModel(BuildConfig(new Dictionary<string, string?>
        {
            ["GROK_MODEL"] = "grok-3"
        })).Should().Be("grok-3");
    }

    [Fact]
    public void IsAuthEnabled_TrueWhenAdminBootstrapCredsPresent()
    {
        TikrConfiguration.IsAuthEnabled(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_ADMIN_EMAIL"] = "admin@town.gov",
            ["TIKR_ADMIN_PASSWORD"] = TestAuthFixtures.BootstrapPassword
        })).Should().BeTrue();
    }

    [Fact]
    public void IsAuthEnabled_FalseWhenExplicitlyDisabled()
    {
        TikrConfiguration.IsAuthEnabled(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_AUTH_ENABLED"] = "false",
            ["TIKR_ADMIN_EMAIL"] = "admin@town.gov",
            ["TIKR_ADMIN_PASSWORD"] = TestAuthFixtures.BootstrapPassword
        })).Should().BeFalse();
    }

    [Fact]
    public void IsAuthEnabled_FalseWhenNoCreds()
    {
        TikrConfiguration.IsAuthEnabled(BuildConfig([])).Should().BeFalse();
    }

    [Fact]
    public void GetJwtSigningKey_ThrowsWhenAuthEnabledAndKeyMissing()
    {
        var act = () => TikrConfiguration.GetJwtSigningKey(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_ADMIN_EMAIL"] = "admin@town.gov",
            ["TIKR_ADMIN_PASSWORD"] = TestAuthFixtures.BootstrapPassword
        }));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetJwtSigningKey_ReturnsConfiguredKey()
    {
        TikrConfiguration.GetJwtSigningKey(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_JWT_SIGNING_KEY"] = TestAuthFixtures.JwtSigningKey
        })).Should().Be(TestAuthFixtures.JwtSigningKey);
    }

    [Fact]
    public void GetJwtSigningKey_ReturnsDevFallbackWhenAuthDisabled()
    {
        TikrConfiguration.GetJwtSigningKey(BuildConfig([]))
            .Should().Be(TikrAuthDefaults.DevDisabledJwtSigningKey);
    }

    [Fact]
    public void GetAdminBootstrapEmail_ReturnsValue()
    {
        TikrConfiguration.GetAdminBootstrapEmail(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_ADMIN_EMAIL"] = "clerk@town.gov"
        })).Should().Be("clerk@town.gov");
    }

    [Fact]
    public void GetAdminBootstrapPassword_ReturnsValue()
    {
        TikrConfiguration.GetAdminBootstrapPassword(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_ADMIN_PASSWORD"] = TestAuthFixtures.BootstrapPassword
        })).Should().Be(TestAuthFixtures.BootstrapPassword);
    }

    [Fact]
    public void IsAuthEnabled_TrueWhenExplicitlyEnabled()
    {
        TikrConfiguration.IsAuthEnabled(BuildConfig(new Dictionary<string, string?>
        {
            ["TIKR_AUTH_ENABLED"] = "true"
        })).Should().BeTrue();
    }

    [Theory]
    [InlineData(null, 8)]
    [InlineData("24", 24)]
    [InlineData("0", 8)]
    [InlineData("-1", 8)]
    public void GetJwtExpirationHours_UsesDefaultOrOverride(string? hours, int expected)
    {
        var values = new Dictionary<string, string?>();
        if (hours is not null)
            values["TIKR_JWT_EXPIRATION_HOURS"] = hours;

        TikrConfiguration.GetJwtExpirationHours(BuildConfig(values)).Should().Be(expected);
    }
}
