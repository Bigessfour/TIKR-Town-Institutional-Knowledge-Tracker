namespace TIKR.Shared.Helpers;

/// <summary>Which model answers a clerk chat turn.</summary>
public enum AiRoute
{
    /// <summary>Local Ollama on NAS (default for routine work).</summary>
    LocalOllama,

    /// <summary>Cloud Grok when enabled and the prompt needs deeper reasoning.</summary>
    CloudGrok
}

/// <summary>LangChain-style routing decision for one Assistant turn.</summary>
public sealed record AiRouteDecision(
    AiRoute Route,
    string Reason,
    /// <summary>0–1 confidence in the routing choice (for logs/UI).</summary>
    double Confidence);

/// <summary>
/// Decision matrix: prefer Ollama for basic clerk tasks; auto-escalate to Grok
/// when the input looks complex and Grok is enabled. No manual "Ask Grok" required.
/// </summary>
public static class AssistantAgentRouter
{
    public static AiRouteDecision Decide(
        string? prompt,
        bool ollamaAvailable,
        bool grokEnabled,
        int historyTurnCount = 0,
        bool searchUnavailable = false,
        int productHelpHitCount = 0,
        int documentHitCount = 0,
        int vaultHitCount = 0)
    {
        // --- Hard constraints (availability) ---
        if (!ollamaAvailable && grokEnabled)
            return new AiRouteDecision(AiRoute.CloudGrok, "Ollama offline; using Grok", 1.0);

        if (!ollamaAvailable && !grokEnabled)
            return new AiRouteDecision(AiRoute.LocalOllama, "Ollama offline and Grok disabled", 1.0);

        if (ollamaAvailable && !grokEnabled)
            return new AiRouteDecision(AiRoute.LocalOllama, "Grok disabled; local only", 1.0);

        // Both available — score the prompt.
        var text = (prompt ?? string.Empty).Trim();
        if (text.Length == 0)
            return new AiRouteDecision(AiRoute.LocalOllama, "Empty prompt", 1.0);

        var score = 0;
        var reasons = new List<string>();
        var lower = text.ToLowerInvariant();

        // Explicit user intent for advanced model
        if (ContainsAny(lower, "use grok", "ask grok", "with grok", "advanced ai", "advanced reasoning"))
        {
            score += 50;
            reasons.Add("explicit advanced/Grok request");
        }

        // Multi-hop / complex reasoning
        if (ContainsAny(lower, "compare", "contrast", "pros and cons", "trade-off", "tradeoff",
                "analyze", "analysis", "evaluate", "critique", "why might", "what if"))
        {
            score += 25;
            reasons.Add("comparative/analytical language");
        }

        if (ContainsAny(lower, "step by step", "step-by-step", "walk me through", "detailed plan",
                "multi-step", "comprehensive", "thorough", "in depth", "in-depth", "deep dive"))
        {
            score += 25;
            reasons.Add("multi-step or thorough request");
        }

        // Drafting / sensitive municipal language
        if (ContainsAny(lower, "draft a", "draft an", "write a letter", "write an email",
                "rewrite", "polish this", "legal language", "board resolution", "ordinance language",
                "memo to the board", "press release"))
        {
            score += 20;
            reasons.Add("drafting or formal language");
        }

        // Long or multi-question prompts
        var questionMarks = text.Count(c => c == '?');
        if (questionMarks >= 2)
        {
            score += 15;
            reasons.Add("multiple questions");
        }

        if (text.Length > 400)
        {
            score += 15;
            reasons.Add("long prompt");
        }
        else if (text.Length > 220)
        {
            score += 8;
            reasons.Add("medium-long prompt");
        }

        // Long conversation threads can benefit from stronger model for synthesis
        if (historyTurnCount >= 6)
        {
            score += 10;
            reasons.Add("long conversation thread");
        }

        // Thin local RAG + complex ask → escalate
        var townHits = documentHitCount + vaultHitCount;
        if (searchUnavailable && score >= 10)
        {
            score += 10;
            reasons.Add("search offline on non-trivial ask");
        }
        else if (townHits == 0 && productHelpHitCount == 0 && text.Length > 100 && score >= 10)
        {
            score += 10;
            reasons.Add("little local context for complex ask");
        }

        // Prefer local for product how-to when we have help hits (3b + pack is enough)
        if (productHelpHitCount > 0 && score < 40)
        {
            score = Math.Max(0, score - 20);
            reasons.Add("product help available — prefer local");
        }

        // Simple local intents
        if (IsSimpleLocalIntent(lower))
        {
            score = Math.Min(score, 5);
            reasons.Add("simple local intent");
        }

        // Threshold: escalate when score high enough
        const int grokThreshold = 30;
        if (score >= grokThreshold)
        {
            var conf = Math.Min(1.0, 0.55 + (score - grokThreshold) / 50.0);
            var reason = reasons.Count > 0
                ? "Escalate to Grok: " + string.Join("; ", reasons.Take(3))
                : "Escalate to Grok";
            return new AiRouteDecision(AiRoute.CloudGrok, reason, conf);
        }

        var localReason = reasons.Count > 0
            ? "Local Ollama: " + string.Join("; ", reasons.Take(3))
            : "Routine clerk question — local Ollama";
        return new AiRouteDecision(AiRoute.LocalOllama, localReason, 0.75);
    }

    /// <summary>After a failed/empty local attempt, escalate if Grok is on.</summary>
    public static AiRouteDecision EscalateAfterLocalFailure(bool grokEnabled) =>
        grokEnabled
            ? new AiRouteDecision(AiRoute.CloudGrok, "Local Ollama failed or empty; escalate to Grok", 0.95)
            : new AiRouteDecision(AiRoute.LocalOllama, "Local failed and Grok unavailable", 1.0);

    private static bool IsSimpleLocalIntent(string lower) =>
        ContainsAny(lower,
            "what should i work on",
            "what's due",
            "whats due",
            "due this week",
            "due today",
            "hello",
            "hi ",
            "thanks",
            "thank you",
            "how do i upload",
            "how do i save",
            "how do i open",
            "what is smart redact",
            "link a packet",
            "who is chatting");

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));
}
