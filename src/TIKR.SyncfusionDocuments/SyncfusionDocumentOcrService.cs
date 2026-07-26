using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.OCRProcessor;
using Syncfusion.Pdf.Parsing;
using TIKR.Shared.Configuration;
using TIKR.Shared.Interfaces;

namespace TIKR.SyncfusionDocuments;

/// <summary>
/// Robust OCR for town-office PDFs and Word docs via Syncfusion PDF OCR (Tesseract 5).
/// Native text is preferred; OCR runs only when extracted text is sparse (typical of scans).
/// </summary>
public sealed partial class SyncfusionDocumentOcrService(
    IConfiguration configuration,
    ILogger<SyncfusionDocumentOcrService> logger) : IDocumentOcrService
{
    /// <summary>Minimum letter characters before we trust native extraction without OCR.</summary>
    public const int MinLetterCharsWithoutOcr = 48;

    public bool IsEnabled => TikrConfiguration.GetOcrEnabled(configuration);

    public DocumentOcrResult EnrichPdf(Stream pdfContent, string? existingText, int pageCountHint = 1)
    {
        if (!IsEnabled)
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false);

        if (!NeedsOcr(existingText, pageCountHint))
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false);

        try
        {
            pdfContent.Position = 0;
            using var loaded = new PdfLoadedDocument(pdfContent);
            using var processor = CreateProcessor();
            ConfigureProcessor(processor);
            processor.PerformOCR(loaded);

            var ocrText = ExtractPdfText(loaded);
            if (string.IsNullOrWhiteSpace(ocrText))
                return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: true, "OCR completed but no text was recognized.");

            logger.LogInformation("PDF OCR produced {CharCount} characters (was sparse native text)", ocrText.Length);
            return new DocumentOcrResult(ocrText, UsedOcr: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PDF OCR failed; keeping native extraction");
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false, ex.Message);
        }
    }

    public DocumentOcrResult EnrichWord(Stream wordContent, string fileName, string? existingText)
    {
        if (!IsEnabled)
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false);

        if (!NeedsOcr(existingText, pageCountHint: 1))
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false);

        try
        {
            wordContent.Position = 0;
            using var word = new WordDocument(wordContent, FormatType.Automatic);
            using var renderer = new DocIORenderer();
            using var pdf = renderer.ConvertToPDF(word);
            using var pdfStream = new MemoryStream();
            pdf.Save(pdfStream);
            pdfStream.Position = 0;

            using var loaded = new PdfLoadedDocument(pdfStream);
            using var processor = CreateProcessor();
            ConfigureProcessor(processor);
            processor.PerformOCR(loaded);

            var ocrText = ExtractPdfText(loaded);
            if (string.IsNullOrWhiteSpace(ocrText))
                return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: true, "Word→PDF OCR completed but no text was recognized.");

            logger.LogInformation(
                "Word OCR ({File}) produced {CharCount} characters via PDF conversion",
                Path.GetFileName(fileName), ocrText.Length);
            return new DocumentOcrResult(ocrText, UsedOcr: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Word OCR failed for {File}; keeping native extraction", fileName);
            return new DocumentOcrResult(existingText ?? string.Empty, UsedOcr: false, ex.Message);
        }
    }

    /// <summary>True when text looks like a scan / empty layer (not enough letters).</summary>
    public static bool NeedsOcr(string? text, int pageCountHint = 1)
    {
        var letters = CountLetters(text);
        var threshold = Math.Max(MinLetterCharsWithoutOcr, pageCountHint * 12);
        return letters < threshold;
    }

    public static int CountLetters(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var count = 0;
        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
                count++;
        }

        return count;
    }

    private OCRProcessor CreateProcessor() => new();

    private void ConfigureProcessor(OCRProcessor processor)
    {
        processor.Settings.Language = Languages.English;
        processor.Settings.TesseractVersion = TesseractVersion.Version5_0;
        processor.Settings.PageSegment = PageSegMode.AutoOsd;

        var tessData = TikrConfiguration.GetTessDataPath(configuration);
        if (!string.IsNullOrWhiteSpace(tessData))
            processor.TessDataPath = tessData;
    }

    private static string ExtractPdfText(PdfLoadedDocument document)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < document.Pages.Count; i++)
        {
            var pageText = document.Pages[i].ExtractText() ?? string.Empty;
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(pageText);
        }

        return CollapseWhitespace().Replace(sb.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespace();
}
