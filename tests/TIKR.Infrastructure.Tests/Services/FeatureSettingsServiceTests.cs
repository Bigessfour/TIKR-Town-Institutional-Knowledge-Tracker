using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class FeatureSettingsServiceTests
{
    [Fact]
    public async Task LoadIntoStateAsync_PopulatesStateFromDatabase()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.AppSettings.Add(new Shared.Entities.AppSetting
        {
            Key = FeatureSettingKeys.TownName,
            Value = "Wiley",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var state = new FeatureSettingsState();
        var sut = CreateService(db, state);

        await sut.LoadIntoStateAsync();

        state.Current.TownName.Should().Be("Wiley");
    }

    [Fact]
    public async Task GetAsync_ReturnsDtoWithOllamaAvailability()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var state = new FeatureSettingsState();
        var ollama = new Mock<IOllamaChatClientFactory>();
        ollama.Setup(o => o.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateService(db, state, ollama.Object);

        var dto = await sut.GetAsync();

        dto.OllamaAvailable.Should().BeTrue();
        dto.TownName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoadIntoStateAsync_EnvChatModelWinsWhenDatabaseRowIsMissing()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var state = new FeatureSettingsState();
        var sut = CreateService(db, state, configuration: ChatModelConfig(), environmentName: "Testing");

        await sut.LoadIntoStateAsync();

        state.Current.OllamaChatModel.Should().Be("qwen2.5:7b");
    }

    [Fact]
    public async Task LoadIntoStateAsync_DatabaseChatModelWinsOverEnv()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.AppSettings.Add(new Shared.Entities.AppSetting
        {
            Key = FeatureSettingKeys.OllamaChatModel,
            Value = "llama3.2:3b",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var state = new FeatureSettingsState();
        var sut = CreateService(db, state, configuration: ChatModelConfig(), environmentName: "Testing");

        await sut.LoadIntoStateAsync();

        state.Current.OllamaChatModel.Should().Be("llama3.2:3b");
    }

    private static IConfiguration ChatModelConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:DefaultModel"] = "llama3.2:3b",
                ["OLLAMA_CHAT_MODEL"] = "qwen2.5:7b"
            })
            .Build();

    private static FeatureSettingsService CreateService(
        TikrDbContext db,
        FeatureSettingsState state,
        IOllamaChatClientFactory? ollama = null,
        IConfiguration? configuration = null,
        string environmentName = "Development")
    {
        var config = configuration ?? new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var hostEnv = new Mock<IHostEnvironment>();
        hostEnv.Setup(h => h.EnvironmentName).Returns(environmentName);

        return new FeatureSettingsService(
            db,
            config,
            state,
            ollama ?? Mock.Of<IOllamaChatClientFactory>(),
            new SecretProtector(
                Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create(nameof(FeatureSettingsServiceTests))),
            Mock.Of<IRuntimeSecretsStore>(),
            Mock.Of<IAuditService>(),
            Mock.Of<ICurrentUserService>(),
            hostEnv.Object,
            NullLogger<FeatureSettingsService>.Instance);
    }
}
