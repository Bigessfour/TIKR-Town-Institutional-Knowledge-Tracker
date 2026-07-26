namespace TIKR.Shared.Interfaces;

/// <summary>
/// OCR enrichment for scanned PDF and Word documents (town office formats).
/// Uses Syncfusion PDF OCR (Tesseract) when native text extraction is sparse.
/// </summary>
public interface IDocumentOcrService
{
    /// <summary>True when OCR is enabled via configuration.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// If <paramref name="existingText"/> looks like a scan (sparse text), run OCR on the PDF
    /// and return searchable text. Otherwise returns the existing text unchanged.
    /// </summary>
    DocumentOcrResult EnrichPdf(Stream pdfContent, string? existingText, int pageCountHint = 1);

    /// <summary>
    /// If Word text is sparse, convert DOCX/DOC → PDF, OCR, and return text.
    /// Otherwise returns the existing Word text unchanged.
    /// </summary>
    DocumentOcrResult EnrichWord(Stream wordContent, string fileName, string? existingText);
}

/// <summary>Result of optional OCR enrichment.</summary>
public record DocumentOcrResult(string Text, bool UsedOcr, string? Error = null);
