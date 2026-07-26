namespace TIKR.Web.Helpers;

/// <summary>
/// Routes Documents library selection into Syncfusion preview modes (PDF Viewer, Word Editor, Spreadsheet).
/// Preview-only — no save-back to NAS.
/// </summary>
public static class DocumentPreviewHelper
{
    public enum PreviewKind
    {
        None,
        Pdf,
        Word,
        Spreadsheet,
        Text,
        ConvertHint
    }

    public static string GetExtension(string? fileName) =>
        Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

    public static PreviewKind ResolveKind(string? fileName, string? contentType, bool hasFullTextContent)
    {
        var ext = GetExtension(fileName);
        if (ext is ".pdf" || contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true)
            return PreviewKind.Pdf;

        if (ext is ".doc" or ".docx")
            return PreviewKind.Word;

        if (ext is ".xls" or ".xlsx")
            return PreviewKind.Spreadsheet;

        if (hasFullTextContent)
            return PreviewKind.Text;

        if (DocumentUiMessages.CanConvertToPdf(fileName ?? string.Empty))
            return PreviewKind.ConvertHint;

        return PreviewKind.None;
    }

    /// <summary>True when the payload starts with a PDF magic header (%PDF).</summary>
    public static bool LooksLikePdf(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
            return false;

        return bytes[0] == (byte)'%'
               && bytes[1] == (byte)'P'
               && bytes[2] == (byte)'D'
               && bytes[3] == (byte)'F';
    }

    public static bool IsLegacyDoc(string? fileName) =>
        GetExtension(fileName) == ".doc";

    public static string PreviewLabel(PreviewKind kind) => kind switch
    {
        PreviewKind.Pdf => "PDF Viewer",
        PreviewKind.Word => "Word preview",
        PreviewKind.Spreadsheet => "Spreadsheet preview",
        PreviewKind.Text => "Full text",
        PreviewKind.ConvertHint => "Preview via Convert to PDF",
        _ => "Preview"
    };
}
