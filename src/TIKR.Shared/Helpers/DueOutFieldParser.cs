using System.Globalization;
using System.Text.RegularExpressions;
using TIKR.Shared.DTOs;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Heuristic extraction of due dates and contact fields from clerk document text (confirm-before-save).
/// </summary>
public static partial class DueOutFieldParser
{
    public static ParsedDueOutFields Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ParsedDueOutFields(null, null, null, null, null);

        var email = EmailRegex().Match(text).Success ? EmailRegex().Match(text).Value : null;
        var phone = NormalizePhone(PhoneRegex().Match(text).Success ? PhoneRegex().Match(text).Value : null);
        var dueDate = TryParseDueDate(text);
        var submitTo = TryParseSubmitTo(text);
        var contactName = TryParseContactName(text);

        return new ParsedDueOutFields(dueDate, submitTo, contactName, email, phone);
    }

    private static DateOnly? TryParseDueDate(string text)
    {
        foreach (Match match in DueDateRegex().Matches(text))
        {
            var raw = match.Groups["date"].Value.Trim();
            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateOnly.TryParse(raw, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out d))
                return d;
            if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var dt))
                return DateOnly.FromDateTime(dt);
        }

        return null;
    }

    private static string? TryParseSubmitTo(string text)
    {
        var match = SubmitToRegex().Match(text);
        if (!match.Success)
            return null;
        var value = match.Groups["org"].Value.Trim().TrimEnd('.', ',', ';');
        return value.Length is >= 2 and <= 300 ? value : null;
    }

    private static string? TryParseContactName(string text)
    {
        var match = ContactNameRegex().Match(text);
        if (!match.Success)
            return null;
        var value = match.Groups["name"].Value.Trim().TrimEnd('.', ',', ';');
        // Stop at common field labels that may have been greedily captured.
        foreach (var stop in new[] { " Due ", " Email ", " Phone ", " Tel ", " Submit " })
        {
            var idx = value.IndexOf(stop, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                value = value[..idx].Trim();
        }
        return value.Length is >= 2 and <= 200 ? value : null;
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length >= 10 ? raw.Trim() : null;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(
        @"(?:due(?:\s+date)?|deadline|file\s+by|submit\s+by)[:\s]+(?<date>\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4}|\w+\s+\d{1,2},?\s+\d{4}|\d{4}-\d{2}-\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DueDateRegex();

    [GeneratedRegex(
        @"(?:submit(?:ted)?\s+to|file\s+with|mail\s+to|send\s+to)[:\s]+(?<org>[^\n\r]{2,120})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SubmitToRegex();

    [GeneratedRegex(
        @"(?:contact|attn|attention|clerk|coordinator)[:\s]+(?<name>[^\n\r]{2,80})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContactNameRegex();
}

public record ParsedDueOutFields(
    DateOnly? DueDate,
    string? SubmitTo,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone);
