using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentIntakeEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentIntakeEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task IntakeScan_Accept_Dismiss_RoundTrips()
    {
        var text = """
            Notice of Hearing
            Submit to: Prowers County Clerk
            Contact: Casey Clerk
            Due date: 06/01/2027
            Email casey@example.com
            Phone (719) 555-0144
            Please post the notice and call the county.
            """;
        var upload = new MultipartFormDataContent
        {
            {
                new ByteArrayContent(Encoding.UTF8.GetBytes(text))
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
                },
                "file",
                "hearing-notice.txt"
            }
        };

        var create = await _client.PostAsync("/api/documents", upload);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await create.Content.ReadFromJsonAsync<DocumentDto>();
        doc.Should().NotBeNull();

        var scan = await _client.PostAsync($"/api/documents/{doc!.Id}/intake-scan", null);
        scan.StatusCode.Should().Be(HttpStatusCode.OK);
        var brief = await scan.Content.ReadFromJsonAsync<DocumentIntakeBriefDto>();
        brief.Should().NotBeNull();
        brief!.DocumentId.Should().Be(doc.Id);
        brief.Status.Should().Be("Pending");
        brief.ProcessedLocally.Should().BeTrue();

        var get = await _client.GetAsync($"/api/documents/{doc.Id}/intake-brief");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var accept = await _client.PostAsJsonAsync(
            $"/api/documents/{doc.Id}/intake-brief/accept",
            new AcceptDocumentIntakeBriefRequest());
        accept.StatusCode.Should().Be(HttpStatusCode.OK);
        var accepted = await accept.Content.ReadFromJsonAsync<AcceptDocumentIntakeBriefResponse>();
        accepted.Should().NotBeNull();
        (accepted!.ContactsUpserted + accepted.RequirementsCreated).Should().BeGreaterThan(0);

        var afterAccept = await _client.GetAsync($"/api/documents/{doc.Id}/intake-brief");
        afterAccept.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fresh scan then dismiss
        (await _client.PostAsync($"/api/documents/{doc.Id}/intake-scan", null)).EnsureSuccessStatusCode();
        var dismiss = await _client.PostAsync($"/api/documents/{doc.Id}/intake-brief/dismiss", null);
        dismiss.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/documents/{doc.Id}/intake-brief")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task IntakeScan_UnknownDocument_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/documents/{Guid.NewGuid()}/intake-scan", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
