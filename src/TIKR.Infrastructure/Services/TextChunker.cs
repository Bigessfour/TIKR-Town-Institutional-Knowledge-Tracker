using System.Security.Cryptography;
using System.Text;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Recursive-style splitter: paragraphs → lines → spaces → hard cut, with overlap.
/// </summary>
public static class TextChunker
{
    public const int DefaultChunkSize = 700;
    public const int DefaultOverlap = 100;

    public static IReadOnlyList<string> Chunk(
        string text,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var normalized = text.Replace("\r\n", "\n").Trim();
        if (normalized.Length <= chunkSize)
            return [normalized];

        overlap = Math.Clamp(overlap, 0, Math.Max(0, chunkSize / 2));
        var parts = SplitRecursive(normalized, chunkSize);
        return MergeWithOverlap(parts, chunkSize, overlap);
    }

    public static string Sha256Hex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static List<string> SplitRecursive(string text, int chunkSize)
    {
        if (text.Length <= chunkSize)
            return [text];

        foreach (var separator in new[] { "\n\n", "\n", " ", "" })
        {
            if (separator.Length == 0)
            {
                var hard = new List<string>();
                for (var i = 0; i < text.Length; i += chunkSize)
                    hard.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
                return hard;
            }

            if (!text.Contains(separator, StringComparison.Ordinal))
                continue;

            var pieces = text.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pieces.Length <= 1)
                continue;

            var result = new List<string>();
            foreach (var piece in pieces)
            {
                if (piece.Length <= chunkSize)
                    result.Add(piece);
                else
                    result.AddRange(SplitRecursive(piece, chunkSize));
            }
            return result;
        }

        return [text];
    }

    private static IReadOnlyList<string> MergeWithOverlap(List<string> parts, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var part in parts)
        {
            if (current.Length == 0)
            {
                current.Append(part);
                continue;
            }

            if (current.Length + 1 + part.Length <= chunkSize)
            {
                current.Append(' ').Append(part);
                continue;
            }

            chunks.Add(current.ToString());
            var previous = current.ToString();
            current.Clear();

            if (overlap > 0 && previous.Length > 0)
            {
                var overlapText = previous.Length <= overlap
                    ? previous
                    : previous[^overlap..];
                current.Append(overlapText.TrimStart());
                if (current.Length > 0)
                    current.Append(' ');
            }

            current.Append(part);
            while (current.Length > chunkSize)
            {
                chunks.Add(current.ToString(0, chunkSize));
                var remainder = current.ToString(Math.Max(0, chunkSize - overlap), current.Length - Math.Max(0, chunkSize - overlap));
                current.Clear();
                current.Append(remainder.TrimStart());
            }
        }

        if (current.Length > 0)
            chunks.Add(current.ToString());

        return chunks;
    }
}
