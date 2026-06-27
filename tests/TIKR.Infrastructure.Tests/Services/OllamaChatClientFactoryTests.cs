using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class OllamaChatClientFactoryTests
{
    [Fact]
    public void Constructor_StoresHostAndModel()
    {
        var factory = new OllamaChatClientFactory("http://localhost:11434", "llama3.2:3b");

        factory.OllamaHost.Should().Be("http://localhost:11434");
        factory.ChatModel.Should().Be("llama3.2:3b");
    }

    [Fact]
    public void CreateChatClient_ReturnsClient()
    {
        var factory = new OllamaChatClientFactory("http://localhost:11434", "llama3.2:3b");
        factory.CreateChatClient().Should().NotBeNull();
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalseForUnreachableHost()
    {
        var factory = new OllamaChatClientFactory("http://127.0.0.1:1", "llama3.2:3b");
        (await factory.IsAvailableAsync()).Should().BeFalse();
    }
}
