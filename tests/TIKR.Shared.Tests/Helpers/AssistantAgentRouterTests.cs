using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class AssistantAgentRouterTests
{
    [Fact]
    public void Decide_SimpleDueQuestion_UsesOllamaWhenAvailable()
    {
        var d = AssistantAgentRouter.Decide(
            "What should I work on this week?",
            ollamaAvailable: true,
            grokEnabled: true);

        d.Route.Should().Be(AiRoute.LocalOllama);
    }

    [Fact]
    public void Decide_ComplexAnalysis_EscalatesToGrokWhenEnabled()
    {
        var d = AssistantAgentRouter.Decide(
            "Compare the pros and cons of TABOR revenue limits and draft a thorough board memo with step-by-step filing options.",
            ollamaAvailable: true,
            grokEnabled: true);

        d.Route.Should().Be(AiRoute.CloudGrok);
        d.Reason.Should().Contain("Grok");
    }

    [Fact]
    public void Decide_GrokDisabled_StaysLocalEvenIfComplex()
    {
        var d = AssistantAgentRouter.Decide(
            "Do a deep dive comparative analysis of mill levy options step by step.",
            ollamaAvailable: true,
            grokEnabled: false);

        d.Route.Should().Be(AiRoute.LocalOllama);
        d.Reason.Should().Contain("Grok disabled");
    }

    [Fact]
    public void Decide_OllamaOffline_UsesGrokWhenEnabled()
    {
        var d = AssistantAgentRouter.Decide(
            "hi",
            ollamaAvailable: false,
            grokEnabled: true);

        d.Route.Should().Be(AiRoute.CloudGrok);
    }

    [Fact]
    public void Decide_ProductHelpHowTo_PrefersLocal()
    {
        var d = AssistantAgentRouter.Decide(
            "How do I save PDF changes to the NAS?",
            ollamaAvailable: true,
            grokEnabled: true,
            productHelpHitCount: 2);

        d.Route.Should().Be(AiRoute.LocalOllama);
    }

    [Fact]
    public void Decide_ExplicitGrokRequest_Escalates()
    {
        var d = AssistantAgentRouter.Decide(
            "Please use Grok for this answer",
            ollamaAvailable: true,
            grokEnabled: true);

        d.Route.Should().Be(AiRoute.CloudGrok);
    }

    [Fact]
    public void EscalateAfterLocalFailure_GoesToGrokWhenEnabled()
    {
        AssistantAgentRouter.EscalateAfterLocalFailure(grokEnabled: true)
            .Route.Should().Be(AiRoute.CloudGrok);
    }
}
