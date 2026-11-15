using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TIKR.Api;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Api.Tests.Endpoints;

public class ChatUserResolverTests
{
    [Fact]
    public void TryResolve_AcceptsCognitoSub()
    {
        var sub = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var http = new DefaultHttpContext();
        http.Request.Headers[ChatHistoryLimits.ChatUserHeaderName] = CognitoChatIdentity.Format(sub);

        ChatUserResolver.TryResolve(new NoUser(), http.Request, Config(auth: false))
            .Should().Be(CognitoChatIdentity.Format(sub));
    }

    [Fact]
    public void TryResolve_RejectsEmail()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[ChatHistoryLimits.ChatUserHeaderName] = "clerk@townofwiley.gov";

        ChatUserResolver.TryResolve(new NoUser(), http.Request, Config(auth: false))
            .Should().BeNull();
    }

    private static IConfiguration Config(bool auth) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TIKR_AUTH_ENABLED"] = auth ? "true" : "false"
            })
            .Build();

    private sealed class NoUser : ICurrentUserService
    {
        public string? UserId => null;
        public bool IsAuthenticated => false;
    }
}
