using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class AiSemanticEndpointTests : IClassFixture<AiStubWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiSemanticEndpointTests(AiStubWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task SemanticSearch_ReturnsHitsFromStub()
    {
        var response = await _client.PostAsJsonAsync("/api/ai/semantic-search", new SemanticSearchRequest("budget", 3));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        body!.Query.Should().Be("budget");
        body.Hits.Should().ContainSingle();
        body.Hits[0].FileName.Should().Be("stub-doc.pdf");
    }

    [Fact]
    public async Task SemanticSearch_EmptyQuery_ReturnsEmptyHits()
    {
        var response = await _client.PostAsJsonAsync("/api/ai/semantic-search", new SemanticSearchRequest("   ", 3));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        body!.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SemanticSearchKnowledge_ReturnsHitsFromStub()
    {
        var response = await _client.PostAsJsonAsync("/api/ai/semantic-search-knowledge", new SemanticSearchRequest("permit", 2));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SemanticSearchKnowledgeResponse>();
        body!.Hits.Should().ContainSingle().Which.Title.Should().Be("Stub entry");
    }

    [Fact]
    public async Task EmbedDocument_ReturnsEmbeddedTrue()
    {
        var docId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/ai/embed-document/{docId}", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<EmbedDocumentResponse>();
        body!.DocumentId.Should().Be(docId);
        body.Embedded.Should().BeTrue();
    }

    [Fact]
    public async Task EmbedKnowledge_ReturnsEmbeddedTrue()
    {
        var entryId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/ai/embed-knowledge/{entryId}", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<EmbedKnowledgeEntryResponse>();
        body!.EntryId.Should().Be(entryId);
        body.Embedded.Should().BeTrue();
    }

    [Fact]
    public async Task TagDocument_ReturnsOkFromStub()
    {
        var docId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/api/ai/tag-document", new TagDocumentRequest(docId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TagDocumentResponse>();
        body!.DocumentId.Should().Be(docId);
        body.Tags.Should().Contain("stub-tag");
    }

    [Fact]
    public async Task AskAdvanced_ReturnsStubAnswerWhenGrokStubEnabled()
    {
        var response = await _client.PostAsJsonAsync("/api/ai/ask-advanced", new AskAdvancedRequest("What is TABOR?", "deadline context"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AskAdvancedResponse>();
        body!.UsedGrok.Should().BeTrue();
        body.Answer.Should().Contain("Stub answer");
    }
}
