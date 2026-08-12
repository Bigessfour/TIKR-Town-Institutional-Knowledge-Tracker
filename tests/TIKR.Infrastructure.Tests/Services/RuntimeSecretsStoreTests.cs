using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class RuntimeSecretsStoreTests
{
    [Fact]
    public void ApplyToProcessEnvironment_SetsKeys_AndClearMissingRemovesThem()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"tikr-secrets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "runtime-secrets.env");
        var config = BuildConfig(path);
        var sut = new RuntimeSecretsStore(config, NullLogger<RuntimeSecretsStore>.Instance);

        sut.ApplyToProcessEnvironment("grok-key", "sf-key");
        Environment.GetEnvironmentVariable("GROK_API_KEY").Should().Be("grok-key");
        Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY").Should().Be("sf-key");

        sut.ApplyToProcessEnvironment(null, null, clearMissing: true);
        Environment.GetEnvironmentVariable("GROK_API_KEY").Should().BeNull();
        Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY").Should().BeNull();

        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void WriteFile_PersistsSecrets_RuntimeSecretsEnvLoader_LoadIfExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"tikr-secrets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "runtime-secrets.env");
        var config = BuildConfig(path);
        var sut = new RuntimeSecretsStore(config, NullLogger<RuntimeSecretsStore>.Instance);

        sut.WriteFile("grok-from-file", "sf-from-file");
        File.Exists(path).Should().BeTrue();

        Environment.SetEnvironmentVariable("GROK_API_KEY", null);
        RuntimeSecretsEnvLoader.LoadIfExists(config);
        Environment.GetEnvironmentVariable("GROK_API_KEY").Should().Be("grok-from-file");

        Directory.Delete(dir, recursive: true);
    }

    private static IConfiguration BuildConfig(string secretsPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TIKR_RUNTIME_SECRETS_PATH"] = secretsPath
            })
            .Build();
}
