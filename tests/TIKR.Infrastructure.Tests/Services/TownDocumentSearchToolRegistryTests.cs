using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class TownDocumentSearchToolRegistryTests
{
    [Fact]
    public void GetFunctions_ExposesSearchTownDocuments()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IHybridAiService>());
        var provider = services.BuildServiceProvider();
        var sut = new TownDocumentSearchToolRegistry(provider.GetRequiredService<IServiceScopeFactory>());

        sut.GetFunctions().Should().ContainSingle(f => f.Name == "search_town_documents");
    }

    [Fact]
    public async Task SearchTownDocumentsAsync_FormatsHits()
    {
        var ai = new Mock<IHybridAiService>();
        ai.Setup(a => a.SemanticSearchDocumentsAsync(It.IsAny<SemanticSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SemanticSearchResponse(
                "aqueduct",
                1,
                [
                    new SemanticSearchHit(
                        Guid.NewGuid(),
                        "water-rate.txt",
                        "Finance",
                        "aqueduct levy",
                        0.91,
                        ChunkIndex: 0,
                        Topic: "Aqueduct levy schedule",
                        Summary: "Town water rates for the aqueduct service area.")
                ],
                EmbeddingAvailable: true));

        var services = new ServiceCollection();
        services.AddSingleton(ai.Object);
        var provider = services.BuildServiceProvider();
        var tools = new TownDocumentSearchTools(provider.GetRequiredService<IServiceScopeFactory>());

        var text = await tools.SearchTownDocumentsAsync("aqueduct");

        text.Should().Contain("[Aqueduct levy schedule] water-rate.txt");
        text.Should().Contain("About: Town water rates");
        text.Should().Contain("Excerpt: aqueduct levy");
        text.Should().Contain("Score: 0.91");
    }
}
