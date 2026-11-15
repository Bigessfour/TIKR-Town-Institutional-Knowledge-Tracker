using System.Text.RegularExpressions;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Strips contact details from text before it is sent to a cloud model.
/// Local Ollama still receives the original clerk context.
/// </summary>
public static partial class CloudOutboundGuard
{
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var redacted = EmailRegex().Replace(text, "[email]");
        redacted = SsnRegex().Replace(redacted, "[id]");
        redacted = PhoneRegex().Replace(redacted, "[phone]");
        redacted = BarePhoneRegex().Replace(redacted, "[phone]");
        return redacted;
    }

    [GeneratedRegex(@"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)\d{3}-\d{2}-\d{4}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?1[\s.\-]?)?\(?\d{3}\)?[\s.\-]\d{3}[\s.\-]\d{4}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?<!\d)\d{10}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex BarePhoneRegex();
}
