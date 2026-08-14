using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TIKR.Shared.Enums;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Deterministic parse of email drop files (.eml preferred; .msg best-effort text scrape).
/// Prefer regex + heuristics; optional AI summaries stay outside this type.
/// </summary>
public static partial class EmailStructuredExtractor
{
    private static readonly string[] ElectionKeywords =
    [
        "election", "canvass", "ballot", "secretary of state", "sos",
        "county clerk", "special district", "wsd", "poll book", "tabulation",
        "certification", "voter", "precinct"
    ];

    public static EmailStructuredExtractResult Parse(string? rawContent, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return EmailStructuredExtractResult.Failed("Empty content");

        var text = rawContent;
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (ext == ".msg" || LooksLikeBinaryMsg(text))
            text = ScrapePrintable(text);

        text = NormalizeNewlines(text);
        var isEml = ext is ".eml" or ".txt" || LooksLikeEml(text);

        string? from = null, replyTo = null, subject = null, dateHeader = null, body = text;
        if (isEml)
        {
            var headers = ParseHeaders(text);
            from = DecodeHeaderValue(GetHeader(headers, "From"));
            replyTo = DecodeHeaderValue(GetHeader(headers, "Reply-To"));
            subject = DecodeHeaderValue(GetHeader(headers, "Subject"));
            dateHeader = GetHeader(headers, "Date");
            body = ExtractBody(text);
            body = DecodeQuotedPrintableLight(body);
        }

        // Fallback: first mailbox in content when headers missing (scraped .msg / odd drops)
        from ??= FirstEmail(text);

        var dueOut = DueOutFieldParser.Parse(body);
        var labeledContact = TryParseLabeledContact(body);
        if (labeledContact is not null)
        {
            dueOut = dueOut with
            {
                ContactName = labeledContact.Value.Name,
                ContactEmail = labeledContact.Value.Email ?? dueOut.ContactEmail,
                ContactPhone = labeledContact.Value.Phone ?? dueOut.ContactPhone
            };
        }

        var election = DetectElection(subject, body, from);
        var categories = election ? ContactCategory.Election : ContactCategory.Custom;

        var contacts = new List<ExtractedEmailContact>();
        var primary = BuildContactFromMailbox(from, replyTo, subject, body, dueOut, categories);
        if (primary is not null)
            contacts.Add(primary);

        // Body-only contact when From was a no-reply / missing display name but Contact: line exists
        if (dueOut.ContactName is not null &&
            contacts.All(c => !string.Equals(c.Name, dueOut.ContactName, StringComparison.OrdinalIgnoreCase)))
        {
            contacts.Add(new ExtractedEmailContact(
                dueOut.ContactName,
                Role: null,
                Organization: dueOut.SubmitTo,
                Email: dueOut.ContactEmail,
                Phone: dueOut.ContactPhone,
                Notes: Truncate($"From email: {subject}", 2000),
                Categories: categories));
        }

        EmailRequirementSuggestionHint? suggestion = null;
        if (dueOut.DueDate is not null || !string.IsNullOrWhiteSpace(dueOut.SubmitTo) || election)
        {
            var title = !string.IsNullOrWhiteSpace(subject)
                ? subject!
                : election
                    ? "Election follow-up from email"
                    : "Follow-up from email";
            suggestion = new EmailRequirementSuggestionHint(
                title.Length > 200 ? title[..200] : title,
                dueOut.DueDate,
                dueOut.SubmitTo,
                dueOut.ContactName ?? primary?.Name,
                dueOut.ContactEmail ?? primary?.Email,
                dueOut.ContactPhone ?? primary?.Phone);
        }

        return new EmailStructuredExtractResult(
            Succeeded: true,
            Error: null,
            From: from,
            ReplyTo: replyTo,
            Subject: subject,
            DateHeader: dateHeader,
            BodyPreview: Truncate(body, 2000),
            IsElectionRelated: election,
            Contacts: contacts,
            RequirementSuggestion: suggestion,
            ParsedDueOut: dueOut);
    }

    public static EmailStructuredExtractResult ParseBytes(ReadOnlySpan<byte> bytes, string? fileName = null)
    {
        string text;
        try
        {
            text = Encoding.UTF8.GetString(bytes);
            if (text.Contains('\0') || LooksLikeBinaryMsg(text))
                text = Encoding.Unicode.GetString(bytes);
        }
        catch
        {
            text = Encoding.Latin1.GetString(bytes);
        }

        return Parse(text, fileName);
    }

    private static ExtractedEmailContact? BuildContactFromMailbox(
        string? from,
        string? replyTo,
        string? subject,
        string body,
        ParsedDueOutFields dueOut,
        ContactCategory categories)
    {
        var mailbox = replyTo ?? from;
        if (string.IsNullOrWhiteSpace(mailbox) &&
            string.IsNullOrWhiteSpace(dueOut.ContactName) &&
            string.IsNullOrWhiteSpace(dueOut.ContactEmail))
            return null;

        var (display, email) = SplitMailbox(mailbox);
        email ??= dueOut.ContactEmail;
        var name = dueOut.ContactName
                   ?? display
                   ?? (email is not null ? email.Split('@')[0].Replace('.', ' ') : null)
                   ?? "Email contact";
        name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Trim());

        var org = dueOut.SubmitTo;
        if (org is null && email is not null)
        {
            var domain = email.Split('@').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(domain) &&
                !domain.Contains("gmail", StringComparison.OrdinalIgnoreCase) &&
                !domain.Contains("outlook", StringComparison.OrdinalIgnoreCase))
                org = domain;
        }

        var notes = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(subject))
            notes.AppendLine($"Subject: {subject}");
        if (!string.IsNullOrWhiteSpace(from))
            notes.AppendLine($"From: {from}");
        notes.AppendLine("Source: email folder ingest");

        return new ExtractedEmailContact(
            Name: name.Length > 200 ? name[..200] : name,
            Role: null,
            Organization: org is { Length: > 300 } ? org[..300] : org,
            Email: email is { Length: > 200 } ? email[..200] : email,
            Phone: dueOut.ContactPhone,
            Notes: Truncate(notes.ToString().Trim(), 2000),
            Categories: categories);
    }

    private static (string? Display, string? Email) SplitMailbox(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        var emailMatch = EmailRegex().Match(raw);
        var email = emailMatch.Success ? emailMatch.Value : null;
        var display = raw;
        if (email is not null)
            display = raw.Replace(email, "", StringComparison.OrdinalIgnoreCase);
        display = display.Trim().Trim('<', '>', '"', '\'', ' ');
        if (string.IsNullOrWhiteSpace(display) || display.Contains('@'))
            display = null;
        return (display, email);
    }

    private static (string Name, string? Email, string? Phone)? TryParseLabeledContact(string body)
    {
        var match = LabeledContactRegex().Match(body);
        if (!match.Success)
            return null;
        var name = match.Groups["name"].Value.Trim().TrimEnd('.', ',', ';');
        if (name.Length is < 2 or > 200)
            return null;

        // Prefer nearby Email:/Phone: lines under the Contact block (next ~6 lines).
        string? email = null;
        string? phone = null;
        var start = match.Index + match.Length;
        var window = body[start..Math.Min(body.Length, start + 400)];
        var emailMatch = EmailRegex().Match(window);
        if (emailMatch.Success)
            email = emailMatch.Value;
        var phoneMatch = PhoneLineRegex().Match(window);
        if (phoneMatch.Success)
            phone = phoneMatch.Groups["phone"].Value.Trim();

        return (name, email, phone);
    }

    private static bool DetectElection(string? subject, string body, string? from)
    {
        var hay = $"{subject}\n{body}\n{from}";
        return ElectionKeywords.Any(k => hay.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstEmail(string text)
    {
        var m = EmailRegex().Match(text);
        return m.Success ? m.Value : null;
    }

    private static Dictionary<string, string> ParseHeaders(string text)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headerBlock = text;
        var sep = text.IndexOf("\n\n", StringComparison.Ordinal);
        if (sep < 0)
            sep = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (sep >= 0)
            headerBlock = text[..sep];

        string? currentKey = null;
        foreach (var line in headerBlock.Split('\n'))
        {
            var trimmedEnd = line.TrimEnd('\r');
            if (trimmedEnd.Length == 0)
                break;
            if (trimmedEnd[0] is ' ' or '\t' && currentKey is not null)
            {
                headers[currentKey] = headers[currentKey] + " " + trimmedEnd.Trim();
                continue;
            }

            var idx = trimmedEnd.IndexOf(':');
            if (idx <= 0)
                continue;
            currentKey = trimmedEnd[..idx].Trim();
            headers[currentKey] = trimmedEnd[(idx + 1)..].Trim();
        }

        return headers;
    }

    private static string? GetHeader(Dictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var v) ? v : null;

    private static string ExtractBody(string text)
    {
        var sep = text.IndexOf("\n\n", StringComparison.Ordinal);
        if (sep < 0)
            sep = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (sep < 0)
            return text;
        return text[(sep + 2)..].Trim();
    }

    private static string? DecodeHeaderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        // Minimal RFC 2047: =?utf-8?B?...?= / =?utf-8?Q?...?=
        var match = EncodedWordRegex().Match(value);
        if (!match.Success)
            return value.Trim();
        try
        {
            var charset = match.Groups["cs"].Value;
            var encoding = match.Groups["enc"].Value;
            var data = match.Groups["data"].Value;
            Encoding enc;
            try { enc = Encoding.GetEncoding(charset); }
            catch { enc = Encoding.UTF8; }

            if (encoding.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = Convert.FromBase64String(data.Replace(" ", ""));
                return enc.GetString(bytes);
            }

            if (encoding.Equals("Q", StringComparison.OrdinalIgnoreCase))
                return DecodeQuotedPrintableLight(data.Replace("_", " "));
        }
        catch
        {
            return value.Trim();
        }

        return value.Trim();
    }

    private static string DecodeQuotedPrintableLight(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains('='))
            return input;
        try
        {
            var sb = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '=' && i + 2 < input.Length)
                {
                    if (input[i + 1] == '\r' || input[i + 1] == '\n')
                    {
                        i++;
                        if (i < input.Length && input[i] == '\n')
                        { /* soft break */ }
                        continue;
                    }

                    if (char.IsAsciiHexDigit(input[i + 1]) && char.IsAsciiHexDigit(input[i + 2]))
                    {
                        sb.Append((char)Convert.ToInt32(input.Substring(i + 1, 2), 16));
                        i += 2;
                        continue;
                    }
                }

                sb.Append(input[i]);
            }

            return sb.ToString();
        }
        catch
        {
            return input;
        }
    }

    private static bool LooksLikeEml(string text) =>
        text.Contains("From:", StringComparison.OrdinalIgnoreCase) &&
        (text.Contains("Subject:", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("To:", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeBinaryMsg(string text) =>
        text.Contains('\0') || text.StartsWith("\xD0\xCF\x11\xE0", StringComparison.Ordinal);

    private static string ScrapePrintable(string text)
    {
        var sb = new StringBuilder(Math.Min(text.Length, 64_000));
        foreach (var ch in text)
        {
            if (ch is >= ' ' and <= '~' or '\n' or '\r' or '\t')
                sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != ' ')
                sb.Append(' ');
            if (sb.Length >= 64_000)
                break;
        }

        return sb.ToString();
    }

    private static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(
        @"^Contact:\s*(?<name>[^\n\r]{2,80})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex LabeledContactRegex();

    [GeneratedRegex(
        @"^Phone:\s*(?<phone>[^\n\r]{7,40})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex PhoneLineRegex();

    [GeneratedRegex(@"=\?(?<cs>[^?]+)\?(?<enc>[bBqQ])\?(?<data>[^?]+)\?=", RegexOptions.CultureInvariant)]
    private static partial Regex EncodedWordRegex();
}

public record ExtractedEmailContact(
    string Name,
    string? Role,
    string? Organization,
    string? Email,
    string? Phone,
    string? Notes,
    ContactCategory Categories);

public record EmailRequirementSuggestionHint(
    string Title,
    DateOnly? DueDate,
    string? SubmitTo,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone);

public record EmailStructuredExtractResult(
    bool Succeeded,
    string? Error,
    string? From,
    string? ReplyTo,
    string? Subject,
    string? DateHeader,
    string? BodyPreview,
    bool IsElectionRelated,
    IReadOnlyList<ExtractedEmailContact> Contacts,
    EmailRequirementSuggestionHint? RequirementSuggestion,
    ParsedDueOutFields? ParsedDueOut)
{
    public static EmailStructuredExtractResult Failed(string error) =>
        new(false, error, null, null, null, null, null, false, [], null, null);
}
