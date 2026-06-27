using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class TikrApiClientTests
{
    [Fact]
    public async Task GetRequirementsAsync_DeserializesResponse()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(id, "Budget", null, DateOnly.FromDateTime(DateTime.UtcNow), Shared.Enums.RecurrenceType.Annual,
                Shared.Enums.RequirementCategory.Budget, true, false)
        });

        var client = CreateClient(json, "/api/requirements");
        var sut = new TikrApiClient(client);

        var items = await sut.GetRequirementsAsync();
        items.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetDocumentsAsync_AppendsSearchQuery()
    {
        string? requestedPath = null;
        var handler = new StubHandler((request, _) =>
        {
            requestedPath = request.RequestUri!.PathAndQuery;
            return JsonResponse("[]");
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new TikrApiClient(client);

        await sut.GetDocumentsAsync("minutes");

        requestedPath.Should().Be("/api/documents?q=minutes");
    }

    [Fact]
    public async Task AskAdvancedAsync_ReturnsNullOnFailure()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new TikrApiClient(client);

        var result = await sut.AskAdvancedAsync("hello");
        result.Should().BeNull();
    }

    private static HttpClient CreateClient(string json, string path)
    {
        var handler = new StubHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be(path);
            return JsonResponse(json);
        });

        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
