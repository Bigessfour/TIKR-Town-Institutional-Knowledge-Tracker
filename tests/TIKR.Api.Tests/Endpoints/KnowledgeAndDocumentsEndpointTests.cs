using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class KnowledgeEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public KnowledgeEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    // Covers KnowledgeService.CreateAsync / UpdateAsync / DeleteAsync via thin /api/knowledge endpoints (Program.cs delegation)

    [Fact]
    public async Task PostAndGetKnowledgeEntry_RoundTrips()
    {
        // Covers KnowledgeService.CreateAsync via thin endpoint
        var request = new CreateKnowledgeEntryRequest(
            "How to run elections",
            "Step-by-step guide for the backup clerk.",
            KnowledgeCategory.HowTo,
            SortOrder: 1);

        var create = await _client.PostAsJsonAsync("/api/knowledge", request);
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<KnowledgeEntryDto>();
        var items = await _client.GetFromJsonAsync<List<KnowledgeEntryDto>>("/api/knowledge");

        items.Should().Contain(k => k.Id == created!.Id && k.Title == request.Title);
    }

    [Fact]
    public async Task PutKnowledgeEntry_UpdatesContent()
    {
        // Covers KnowledgeService.UpdateAsync via thin endpoint
        var create = await _client.PostAsJsonAsync("/api/knowledge", new CreateKnowledgeEntryRequest(
            "Original title",
            "Original content",
            KnowledgeCategory.HowTo,
            SortOrder: 1));

        var created = await create.Content.ReadFromJsonAsync<KnowledgeEntryDto>();

        var update = new UpdateKnowledgeEntryRequest(
            "Updated title",
            "Updated content",
            KnowledgeCategory.Emergency,
            SortOrder: 2);

        var response = await _client.PutAsJsonAsync($"/api/knowledge/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<KnowledgeEntryDto>();
        updated!.Title.Should().Be("Updated title");
        updated.Category.Should().Be(KnowledgeCategory.Emergency);
    }

    [Fact]
    public async Task DeleteKnowledgeEntry_ReturnsNoContent()
    {
        var create = await _client.PostAsJsonAsync("/api/knowledge", new CreateKnowledgeEntryRequest(
            "To delete",
            "Content",
            KnowledgeCategory.Contact,
            SortOrder: 0));

        var created = await create.Content.ReadFromJsonAsync<KnowledgeEntryDto>();

        var response = await _client.DeleteAsync($"/api/knowledge/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var items = await _client.GetFromJsonAsync<List<KnowledgeEntryDto>>("/api/knowledge");
        items.Should().NotContain(k => k.Id == created.Id);
    }

    [Fact]
    public async Task DeleteKnowledgeEntry_ReturnsNotFoundForMissingId()
    {
        var response = await _client.DeleteAsync($"/api/knowledge/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutKnowledgeEntry_ReturnsNotFoundForMissingId()
    {
        var update = new UpdateKnowledgeEntryRequest("x", "y", KnowledgeCategory.HowTo, 0);
        var response = await _client.PutAsJsonAsync($"/api/knowledge/{Guid.NewGuid()}", update);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.FullyTested)]
public class DocumentsEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentsEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    // Covers DocumentService.PrepareDocumentUploadAsync (and UploadAsync which calls it) via thin POST /api/documents
    // + DocumentService.DeleteAsync etc.

    [Fact]
    public async Task UploadDocument_PersistsMetadata()
    {
        // Covers DocumentService.PrepareDocumentUploadAsync via document upload endpoint
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("sample pdf bytes"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample.pdf");

        var response = await _client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<DocumentDto>();
        var items = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?q=sample");

        items.Should().Contain(d => d.Id == created!.Id && d.FileName == "sample.pdf");
    }

    [Fact]
    public async Task UploadTextFile_PersistsFullTextContent()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("Quarterly budget summary for council review"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "budget-notes.txt");

        var response = await _client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<DocumentDto>();
        var items = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?q=council");

        items.Should().Contain(d =>
            d.Id == created!.Id
            && d.FileName == "budget-notes.txt");
    }

    [Fact]
    public async Task UploadWithoutMultipart_ReturnsBadRequest()
    {
        using var content = new StringContent("not multipart", System.Text.Encoding.UTF8, "text/plain");
        var response = await _client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadEmptyMultipart_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("notes only"), "description");
        var response = await _client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDocuments_Unfiltered_ReturnsUploadedItems()
    {
        using var upload = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("list test"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(fileContent, "file", "list-test.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        var items = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
        items.Should().Contain(d => d.Id == created!.Id);
    }

    [Fact]
    public async Task ReplaceDocumentContent_UpdatesStoredBytes()
    {
        using var upload = new MultipartFormDataContent();
        var original = new ByteArrayContent("original body"u8.ToArray());
        original.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(original, "file", "replace-me.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        using var replace = new MultipartFormDataContent();
        var updatedBytes = new ByteArrayContent("updated body for clerk"u8.ToArray());
        updatedBytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        replace.Add(updatedBytes, "file", "replace-me.txt");

        var put = await _client.PutAsync($"/api/documents/{created!.Id}/content", replace);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetAsync($"/api/documents/{created.Id}/content");
        var text = await get.Content.ReadAsStringAsync();
        text.Should().Be("updated body for clerk");
    }

    [Fact]
    public async Task PatchDocumentMetadata_RenamesAndMovesFolder()
    {
        using var upload = new MultipartFormDataContent();
        var original = new ByteArrayContent("meta body"u8.ToArray());
        original.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(original, "file", "rename-me.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        var patch = await _client.PatchAsJsonAsync(
            $"/api/documents/{created!.Id}",
            new UpdateDocumentMetadataRequest(FileName: "renamed.txt", SuggestedFolder: "Finance"));
        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await patch.Content.ReadFromJsonAsync<DocumentDto>();
        updated!.FileName.Should().Be("renamed.txt");
        updated.SuggestedFolder.Should().Be("Finance");
    }

    [Fact]
    public async Task SoftDelete_ThenRestore_AndVersionHistoryOnReplace()
    {
        using var upload = new MultipartFormDataContent();
        var original = new ByteArrayContent("soft-delete body v1 enough"u8.ToArray());
        original.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(original, "file", "soft.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        // Replace content → creates version snapshot
        using var replace = new MultipartFormDataContent();
        var v2 = new ByteArrayContent("soft-delete body v2 enough"u8.ToArray());
        v2.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        replace.Add(v2, "file", "soft.txt");
        (await _client.PutAsync($"/api/documents/{created!.Id}/content", replace)).EnsureSuccessStatusCode();

        var versions = await _client.GetFromJsonAsync<List<DocumentVersionDto>>($"/api/documents/{created.Id}/versions");
        versions.Should().ContainSingle(v => v.VersionNumber == 1);

        (await _client.DeleteAsync($"/api/documents/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var active = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
        active.Should().NotContain(d => d.Id == created.Id);

        var recycled = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?deleted=true");
        recycled.Should().Contain(d => d.Id == created.Id);

        (await _client.PostAsync($"/api/documents/{created.Id}/restore", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var restored = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
        restored.Should().Contain(d => d.Id == created.Id);
    }

    [Fact]
    public async Task DeleteDocument_RemovesMetadata()
    {
        using var upload = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("delete me"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(fileContent, "file", "remove-me.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        var response = await _client.DeleteAsync($"/api/documents/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var items = await _client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?q=remove-me");
        items.Should().NotContain(d => d.Id == created.Id);
    }

    [Fact]
    public async Task GetDocumentContent_ReturnsUploadedBytes()
    {
        const string payload = "download-me-from-nas";
        using var upload = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(payload));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(fileContent, "file", "download-me.txt");

        var create = await _client.PostAsync("/api/documents", upload);
        var created = await create.Content.ReadFromJsonAsync<DocumentDto>();

        var response = await _client.GetAsync($"/api/documents/{created!.Id}/content");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.UTF8.GetString(bytes).Should().Be(payload);
    }

    [Fact]
    public async Task GetDocumentContent_ReturnsNotFoundForMissingId()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}/content");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNotFoundForMissingId()
    {
        var response = await _client.DeleteAsync($"/api/documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

[Trait("Category", TestCategories.FullyTested)]
public class AiEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetAiStatus_ReturnsPayload()
    {
        var status = await _client.GetFromJsonAsync<AiStatusResponse>("/api/ai/status");
        status.Should().NotBeNull();
        status!.OllamaModel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDashboardPriorities_ReturnsList()
    {
        var priorities = await _client.GetFromJsonAsync<List<DashboardPriority>>("/api/ai/dashboard-priorities");
        priorities.Should().NotBeNull();
        priorities!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AskAdvanced_WhenGrokDisabled_FallsBackToOllamaStub()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/ai/ask-advanced",
            new AskAdvancedRequest("What is TABOR?", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AskAdvancedResponse>();
        body.Should().NotBeNull();
        body!.UsedGrok.Should().BeFalse();
        body.Answer.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetLocalStatus_ReturnsPayload()
    {
        var status = await _client.GetFromJsonAsync<LocalStorageStatusDto>("/api/system/local-status");
        status.Should().NotBeNull();
        status!.TownName.Should().NotBeNullOrWhiteSpace();
        status.StorageLabel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TagDocument_WhenDocumentMissing_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/ai/tag-document",
            new TagDocumentRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
