using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Helpers;

internal static class TestGrokServiceFactory
{
    public static GrokService CreateDisabled() =>
        Create(new Dictionary<string, string?> { ["USE_GROK"] = "false" });

    public static GrokService Create(Dictionary<string, string?> settings, HttpMessageHandler? handler = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
        var http = new HttpClient(handler ?? new HttpClientHandler());
        return new GrokService(http, config, NullLogger<GrokService>.Instance);
    }
}
