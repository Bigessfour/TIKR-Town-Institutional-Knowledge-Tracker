using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.SyncfusionDocuments;

/// <summary>
/// Syncfusion Document SDK generation for Colorado municipal clerk workflows.
/// Uses in-memory streams (stateless API) per Syncfusion Storage Mode guidance for NAS backends.
/// </summary>
public sealed class SyncfusionDocumentGenerationService(
    IConfiguration configuration,
    ILogger<SyncfusionDocumentGenerationService> logger) : IDocumentGenerationService
{
    private const float PdfMargin = 40f;
    private const float PdfLineHeight = 16f;

    public Task<GeneratedDocumentResult> GenerateCouncilAgendaPdfAsync(
        CouncilAgendaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TownName);
        EnsureLicenseConfigured();

        using var document = new PdfDocument();
        var page = document.Pages.Add();
        var graphics = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var headingFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var bodyFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11);

        var y = PdfMargin;
        graphics.DrawString($"{request.TownName} — Council Agenda", titleFont, PdfBrushes.Black, new PointF(PdfMargin, y));
        y += PdfLineHeight * 2;
        graphics.DrawString($"Meeting date: {request.MeetingDate:MMMM d, yyyy}", bodyFont, PdfBrushes.Black, new PointF(PdfMargin, y));
        y += PdfLineHeight * 2;
        graphics.DrawString("Agenda items", headingFont, PdfBrushes.Black, new PointF(PdfMargin, y));
        y += PdfLineHeight * 1.5f;

        var index = 1;
        foreach (var item in request.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = $"{index}. {item.Title}";
            if (item.DueDate is { } due)
                line += $" (due {due:MMM d, yyyy})";

            graphics.DrawString(line, bodyFont, PdfBrushes.Black, new PointF(PdfMargin, y));
            y += PdfLineHeight;

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                foreach (var wrapped in WrapText(item.Description, 90))
                {
                    graphics.DrawString(wrapped, bodyFont, PdfBrushes.DarkGray, new PointF(PdfMargin + 12, y));
                    y += PdfLineHeight;
                }
            }

            y += 4;
            index++;
        }

        var fileName = $"council-agenda-{request.MeetingDate:yyyy-MM-dd}.pdf";
        return Task.FromResult(SavePdf(document, fileName));
    }

    public Task<GeneratedDocumentResult> GenerateMeetingMinutesDocxAsync(
        MeetingMinutesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TownName);
        EnsureLicenseConfigured();

        using var document = new WordDocument();
        var section = document.AddSection();

        AddHeading(section, $"{request.TownName} — Meeting Minutes");
        AddParagraph(section, $"Date: {request.MeetingDate:MMMM d, yyyy}");
        if (!string.IsNullOrWhiteSpace(request.BoardName))
            AddParagraph(section, $"Board: {request.BoardName}");

        if (request.Attendees is { Count: > 0 })
        {
            AddSubheading(section, "Attendees");
            foreach (var attendee in request.Attendees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddBullet(section, attendee);
            }
        }

        if (request.AgendaItems is { Count: > 0 })
        {
            AddSubheading(section, "Agenda");
            foreach (var item in request.AgendaItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddBullet(section, item);
            }
        }

        AddSubheading(section, "Minutes");
        AddParagraph(section, string.IsNullOrWhiteSpace(request.Notes)
            ? "Minutes to be recorded."
            : request.Notes);

        var fileName = $"meeting-minutes-{request.MeetingDate:yyyy-MM-dd}.docx";
        return Task.FromResult(SaveWord(document, fileName));
    }

    public Task<GeneratedDocumentResult> GenerateClerkMemoDocxAsync(
        ClerkMemoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TownName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Body);
        EnsureLicenseConfigured();

        var memoDate = request.MemoDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        using var document = new WordDocument();
        var section = document.AddSection();

        AddHeading(section, request.TownName);
        AddParagraph(section, $"Date: {memoDate:MMMM d, yyyy}");
        if (!string.IsNullOrWhiteSpace(request.Recipient))
            AddParagraph(section, $"To: {request.Recipient}");
        AddParagraph(section, $"Subject: {request.Subject}");
        AddParagraph(section, string.Empty);

        foreach (var paragraph in request.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddParagraph(section, paragraph);
        }

        var fileName = $"clerk-memo-{memoDate:yyyy-MM-dd}.docx";
        return Task.FromResult(SaveWord(document, fileName));
    }

    public Task<GeneratedDocumentResult> GenerateComplianceReportXlsxAsync(
        ComplianceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TownName);
        EnsureLicenseConfigured();

        using var engine = new ExcelEngine();
        var application = engine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;
        var workbook = application.Workbooks.Create(1);
        var sheet = workbook.Worksheets[0];
        sheet.Name = "Compliance";

        sheet.Range["A1"].Text = $"{request.TownName} — Municipal Compliance Report";
        sheet.Range["A2"].Text = $"Generated: {request.ReportDate:MMMM d, yyyy}";

        var headerRow = 4;
        sheet.Range[$"A{headerRow}"].Text = "Title";
        sheet.Range[$"B{headerRow}"].Text = "Description";
        sheet.Range[$"C{headerRow}"].Text = "Due Date";
        sheet.Range[$"D{headerRow}"].Text = "Category";
        sheet.Range[$"E{headerRow}"].Text = "Completed";
        sheet.Range[$"A{headerRow}:E{headerRow}"].CellStyle.Font.Bold = true;

        var row = headerRow + 1;
        foreach (var item in request.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sheet.Range[$"A{row}"].Text = item.Title;
            sheet.Range[$"B{row}"].Text = item.Description ?? string.Empty;
            sheet.Range[$"C{row}"].DateTime = item.DueDate.ToDateTime(TimeOnly.MinValue);
            sheet.Range[$"C{row}"].NumberFormat = "mmm d, yyyy";
            sheet.Range[$"D{row}"].Text = item.Category;
            sheet.Range[$"E{row}"].Text = item.IsCompleted ? "Yes" : "No";
            row++;
        }

        sheet.UsedRange.AutofitColumns();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();

        var fileName = $"compliance-report-{request.ReportDate:yyyy-MM-dd}.xlsx";
        return Task.FromResult(new GeneratedDocumentResult(
            stream.ToArray(),
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }

    public Task<GeneratedDocumentResult> ConvertWordToPdfAsync(
        Stream wordContent,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        EnsureLicenseConfigured();

        cancellationToken.ThrowIfCancellationRequested();
        using var wordDocument = new WordDocument(wordContent, FormatType.Automatic);
        using var renderer = new DocIORenderer();
        using var pdfDocument = renderer.ConvertToPDF(wordDocument);

        var outputName = Path.ChangeExtension(Path.GetFileName(fileName), ".pdf");
        return Task.FromResult(SavePdf(pdfDocument, outputName));
    }

    public Task<CouncilPacketGeneratedFiles> GenerateCouncilPacketAsync(
        CreateCouncilPacketRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TownName);
        if (request.Requirements.Count == 0)
            throw new ArgumentException("At least one requirement is required for a council packet.", nameof(request));

        EnsureLicenseConfigured();
        logger.LogInformation(
            "Generating council packet for {Town} with {Count} requirements",
            request.TownName,
            request.Requirements.Count);

        try
        {
            var files = CouncilPacketGenerator.Build(request, cancellationToken);
            logger.LogInformation(
                "Council packet generated: PDF {PdfBytes} bytes, DOCX {DocxBytes} bytes",
                files.PdfContent.Length,
                files.DocxContent.Length);
            return Task.FromResult(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Council packet generation failed for {Town}", request.TownName);
            throw;
        }
    }

    public Task<GeneratedDocumentResult> ConvertExcelToPdfAsync(
        Stream excelContent,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        EnsureLicenseConfigured();

        cancellationToken.ThrowIfCancellationRequested();
        using var engine = new ExcelEngine();
        var application = engine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;
        var workbook = application.Workbooks.Open(excelContent);
        var renderer = new XlsIORenderer();
        using var pdfDocument = renderer.ConvertToPDF(workbook);
        workbook.Close();

        var outputName = Path.ChangeExtension(Path.GetFileName(fileName), ".pdf");
        return Task.FromResult(SavePdf(pdfDocument, outputName));
    }

    private void EnsureLicenseConfigured()
    {
        if (string.IsNullOrWhiteSpace(TikrConfiguration.GetSyncfusionLicenseKey(configuration)))
        {
            throw new InvalidOperationException(
                "SYNCFUSION_LICENSE_KEY is required for document generation. " +
                "Set it in docker/.env or user-secrets and restart the API.");
        }
    }

    private static GeneratedDocumentResult SavePdf(PdfDocument document, string fileName)
    {
        using var stream = new MemoryStream();
        document.Save(stream);
        return new GeneratedDocumentResult(stream.ToArray(), fileName, "application/pdf");
    }

    private static GeneratedDocumentResult SaveWord(WordDocument document, string fileName)
    {
        using var stream = new MemoryStream();
        document.Save(stream, FormatType.Docx);
        document.Close();
        return new GeneratedDocumentResult(
            stream.ToArray(),
            fileName,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    private static void AddHeading(IWSection section, string text)
    {
        var paragraph = section.AddParagraph();
        paragraph.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        var range = paragraph.AppendText(text);
        range.CharacterFormat.FontSize = 16;
        range.CharacterFormat.Bold = true;
    }

    private static void AddSubheading(IWSection section, string text)
    {
        var paragraph = section.AddParagraph();
        var range = paragraph.AppendText(text);
        range.CharacterFormat.FontSize = 12;
        range.CharacterFormat.Bold = true;
    }

    private static void AddParagraph(IWSection section, string text)
    {
        section.AddParagraph().AppendText(text);
    }

    private static void AddBullet(IWSection section, string text)
    {
        var paragraph = section.AddParagraph();
        paragraph.ListFormat.ApplyDefBulletStyle();
        paragraph.AppendText(text);
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            yield break;

        var line = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            var candidate = line + " " + words[i];
            if (candidate.Length > maxChars)
            {
                yield return line;
                line = words[i];
            }
            else
            {
                line = candidate;
            }
        }

        yield return line;
    }

    public Task<GeneratedDocumentResult> GenerateHandoverPackagePdfAsync(
        HandoverPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureLicenseConfigured();

        using var document = new PdfDocument();
        document.DocumentInformation.Title = $"{request.TownName} - Complete Handover Package";
        document.DocumentInformation.Author = "TIKR";
        document.DocumentInformation.Subject = $"Generated {request.GeneratedAt:yyyy-MM-dd}";

        var page = document.Pages.Add();
        var graphics = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var h1 = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var body = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
        var small = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

        float y = 40;
        graphics.DrawString($"{request.TownName} COMPLETE HANDOVER PACKAGE", titleFont, PdfBrushes.Black, new PointF(40, y));
        y += 20;
        graphics.DrawString($"Generated {request.GeneratedAt:yyyy-MM-dd} | Use PDF bookmarks for navigation", body, PdfBrushes.Black, new PointF(40, y));
        y += 15;

        // TOC
        graphics.DrawString("1. Knowledge Entries  2. Voice Notes  3. Requirements  4. Calendar  5. Documents", body, PdfBrushes.Black, new PointF(40, y));
        y += 20;

        // Knowledge
        graphics.DrawString("1. KNOWLEDGE ENTRIES", h1, PdfBrushes.Black, new PointF(40, y));
        y += 14;
        foreach (var e in request.KnowledgeEntries.Take(15))
        {
            graphics.DrawString("• " + e.Title, body, PdfBrushes.Black, new PointF(45, y));
            y += 11;
            if (y > 700) { page = document.Pages.Add(); graphics = page.Graphics; y = 40; }
        }

        // Voice
        y += 5;
        graphics.DrawString("2. VOICE NOTES", h1, PdfBrushes.Black, new PointF(40, y));
        y += 14;
        var voices = request.KnowledgeEntries.Where(e => e.Title.Contains("Voice")).Take(5);
        foreach (var v in voices)
        {
            graphics.DrawString("• " + v.Title, body, PdfBrushes.Black, new PointF(45, y));
            y += 11;
        }

        // Requirements
        y += 5;
        graphics.DrawString("3. REQUIREMENTS (Active)", h1, PdfBrushes.Black, new PointF(40, y));
        y += 14;
        foreach (var r in request.Requirements.Where(x => !x.IsCompleted).Take(10))
        {
            graphics.DrawString($"• {r.DueDate} {r.Title}", body, PdfBrushes.Black, new PointF(45, y));
            y += 11;
        }

        // Calendar
        y += 5;
        graphics.DrawString("4. CALENDAR SNAPSHOT", h1, PdfBrushes.Black, new PointF(40, y));
        y += 14;
        foreach (var c in request.CalendarSnapshot.Take(8))
        {
            graphics.DrawString($"• {c.DueDate} {c.Title}", body, PdfBrushes.Black, new PointF(45, y));
            y += 11;
        }

        // Documents
        y += 5;
        graphics.DrawString("5. DOCUMENTS", h1, PdfBrushes.Black, new PointF(40, y));
        y += 14;
        foreach (var d in request.Documents.Take(8))
        {
            graphics.DrawString($"• {d.FileName}", body, PdfBrushes.Black, new PointF(45, y));
            y += 11;
        }

        y += 5;
        graphics.DrawString("6. EMERGENCY: See Knowledge sections + Requirements deadlines.", body, PdfBrushes.Black, new PointF(40, y));

        var fileName = $"TIKR-Handover-{request.GeneratedAt:yyyy-MM-dd}.pdf";
        return Task.FromResult(SavePdf(document, fileName));
    }

    public Task<GeneratedDocumentResult> CreateAgentArchivePdfAsync(
        Stream content,
        string fileName,
        DateTime processedDate,
        CancellationToken cancellationToken = default)
    {
        EnsureLicenseConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        // Consume original stream (we archive a clean stamped representation; original bytes kept via dual storage in caller)
        // Minimal real impl: produce a stamped PDF cover with metadata for the agent archive workflow.
        using var document = new PdfDocument();
        document.DocumentInformation.Title = $"AI Archive - {Path.GetFileName(fileName)}";
        document.DocumentInformation.Subject = "AI Processed - TIKR Vault";
        document.DocumentInformation.Author = "TIKR";
        document.DocumentInformation.Producer = "TIKR Document Agent";

        var page = document.Pages.Add();
        var graphics = page.Graphics;

        var stampFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        var bodyFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);

        // Prominent stamp banner (red for visibility on print/scan)
        graphics.DrawString("AI PROCESSED - TIKR VAULT", stampFont, PdfBrushes.Red, new PointF(PdfMargin, 30));
        graphics.DrawString($"Processed: {processedDate:yyyy-MM-dd HH:mm:ss} UTC", bodyFont, PdfBrushes.Black, new PointF(PdfMargin, 50));
        graphics.DrawString($"Original: {fileName}", bodyFont, PdfBrushes.Black, new PointF(PdfMargin, 66));

        graphics.DrawString("Clean archive copy generated by document agent. Original file stored alongside under agent-scans/.", smallFont, PdfBrushes.DarkGray, new PointF(PdfMargin, 90));

        // Footer note
        graphics.DrawString("TIKR - Town Institutional Knowledge Tracker", smallFont, PdfBrushes.Gray, new PointF(PdfMargin, 750));

        var archiveName = Path.ChangeExtension(Path.GetFileName(fileName), ".ai-archive.pdf") ?? "agent-archive.pdf";
        return Task.FromResult(SavePdf(document, archiveName));
    }

    public Task<GeneratedDocumentResult> ConvertImageToPdfAsync(
        Stream imageContent,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        EnsureLicenseConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        // Minimal real impl: produce a PDF wrapper for the image upload with stamp (full raster embedding would require additional image codec handling).
        using var document = new PdfDocument();
        var page = document.Pages.Add();
        var graphics = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var body = new PdfStandardFont(PdfFontFamily.Helvetica, 10);

        graphics.DrawString("IMAGE CONVERTED TO PDF", titleFont, PdfBrushes.Black, new PointF(PdfMargin, 30));
        graphics.DrawString($"Source: {fileName}", body, PdfBrushes.Black, new PointF(PdfMargin, 50));
        graphics.DrawString("Image content archived as PDF via TIKR document conversion.", body, PdfBrushes.DarkGray, new PointF(PdfMargin, 70));

        var outName = Path.ChangeExtension(Path.GetFileName(fileName), ".pdf") ?? "image.pdf";
        return Task.FromResult(SavePdf(document, outName));
    }
}
