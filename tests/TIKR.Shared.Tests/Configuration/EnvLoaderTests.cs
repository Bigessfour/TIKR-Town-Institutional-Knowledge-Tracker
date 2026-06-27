using FluentAssertions;
using TIKR.Shared.Configuration;

namespace TIKR.Shared.Tests.Configuration;

public class EnvLoaderTests
{
    [Fact]
    public void LoadDevelopmentEnv_LoadsExistingEnvFiles()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"tikr-env-{Guid.NewGuid():N}");
        var dockerDir = Path.Combine(repoRoot, "docker");
        var contentRoot = Path.Combine(repoRoot, "src", "TIKR.Api");
        Directory.CreateDirectory(contentRoot);
        Directory.CreateDirectory(dockerDir);

        var envPath = Path.Combine(repoRoot, ".env");
        File.WriteAllText(envPath, "TIKR_ENV_LOADER_TEST=from_root\n");
        File.WriteAllText(Path.Combine(dockerDir, ".env"), "TIKR_ENV_LOADER_TEST=from_docker\n");

        try
        {
            EnvLoader.LoadDevelopmentEnv(contentRoot);
            // docker/.env loads after root .env and wins on duplicate keys
            Environment.GetEnvironmentVariable("TIKR_ENV_LOADER_TEST")
                .Should().Be("from_docker");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TIKR_ENV_LOADER_TEST", null);
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public void LoadDevelopmentEnv_NoOpWhenFilesMissing()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"tikr-env-{Guid.NewGuid():N}");
        var contentRoot = Path.Combine(repoRoot, "src", "TIKR.Api");
        Directory.CreateDirectory(contentRoot);

        try
        {
            var act = () => EnvLoader.LoadDevelopmentEnv(contentRoot);
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }
}
