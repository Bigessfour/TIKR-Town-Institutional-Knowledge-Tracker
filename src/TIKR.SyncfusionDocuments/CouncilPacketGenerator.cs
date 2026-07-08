using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;

namespace TIKR.SyncfusionDocuments;

internal static class CouncilPacketGenerator
{
    private const int SummaryMaxLength = 500;

    public static CouncilPacketGeneratedFiles Build(CreateCouncilPacketRequest request, CancellationToken cancellationToken)
    {
        using var wordDocument = CreateWordDocument(request, cancellationToken);
        var docxFileName = $"council-packet-{request.PacketDate:yyyy-MM-dd}.docx";
        var pdfFileName = $"council-packet-{request.PacketDate:yyyy-MM-dd}.pdf";

        byte[] docxBytes;
        using (var docxStream = new MemoryStream())
        {
            wordDocument.Save(docxStream, FormatType.Docx);
            docxBytes = docxStream.ToArray();
        }

        byte[] pdfBytes;
        using (var renderer = new DocIORenderer())
        {
            renderer.Settings.AutoTag = true;
            renderer.Settings.EmbedFonts = true;
            renderer.Settings.ExportBookmarks = ExportBookmarkType.Headings;
            using var pdfDocument = renderer.ConvertToPDF(wordDocument);
            pdfDocument.DocumentInformation.Title = $"{request.TownName} Town Council Packet";
            pdfDocument.DocumentInformation.Subject = $"Council packet generated {request.PacketDate:MMMM d, yyyy}";
            using var pdfStream = new MemoryStream();
            pdfDocument.Save(pdfStream);
            pdfBytes = pdfStream.ToArray();
        }

        wordDocument.Close();
        return new CouncilPacketGeneratedFiles(pdfBytes, pdfFileName, docxBytes, docxFileName);
    }

    private static WordDocument CreateWordDocument(CreateCouncilPacketRequest request, CancellationToken cancellationToken)
    {
        var document = new WordDocument();
        var section = document.AddSection();
        section.PageSetup.Margins.All = 72f;

        AddCoverPage(section, request);
        section.AddParagraph().AppendBreak(BreakType.PageBreak);
        AddDeadlinesTable(section, request, cancellationToken);
        AddLinkedDocumentsSection(section, request, cancellationToken);

        return document;
    }

    private static void AddCoverPage(IWSection section, CreateCouncilPacketRequest request)
    {
        TryInsertLogo(section, request.LogoPath);

        var titleParagraph = section.AddParagraph();
        titleParagraph.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        titleParagraph.ParagraphFormat.AfterSpacing = 12f;
        var title = titleParagraph.AppendText($"{request.TownName} Town Council Packet");
        title.CharacterFormat.FontSize = 22;
        title.CharacterFormat.Bold = true;

        var dateParagraph = section.AddParagraph();
        dateParagraph.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        dateParagraph.ParagraphFormat.AfterSpacing = 24f;
        var dateText = dateParagraph.AppendText(request.PacketDate.ToString("MMMM d, yyyy"));
        dateText.CharacterFormat.FontSize = 14;

        var subtitle = section.AddParagraph();
        subtitle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        var subtitleText = subtitle.AppendText("Colorado municipal compliance deadlines and supporting documents");
        subtitleText.CharacterFormat.FontSize = 11;
        subtitleText.CharacterFormat.Italic = true;
    }

    private static void TryInsertLogo(IWSection section, string? logoPath)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(logoPath))
            candidates.Add(logoPath);

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "town-logo.png"));

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                using var stream = File.OpenRead(candidate);
                var paragraph = section.AddParagraph();
                paragraph.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
                paragraph.ParagraphFormat.AfterSpacing = 18f;
                var picture = paragraph.AppendPicture(stream);
                picture.Width = 96;
                picture.Height = 96;
                picture.AlternativeText = "Town logo";
                return;
            }
            catch
            {
                // Try next candidate.
            }
        }
    }

    private static void AddDeadlinesTable(
        IWSection section,
        CreateCouncilPacketRequest request,
        CancellationToken cancellationToken)
    {
        AddHeading(section, "Statutory deadlines");
        AddParagraph(section, "Requirements sorted by due date. Urgency colors match the Requirements Manager.");

        var table = section.AddTable();
        table.ResetCells(request.Requirements.Count + 1, 5);

        SetHeaderCell(table[0, 0], "Requirement");
        SetHeaderCell(table[0, 1], "Due date");
        SetHeaderCell(table[0, 2], "Status");
        SetHeaderCell(table[0, 3], "Urgency");
        SetHeaderCell(table[0, 4], "Category");

        for (var rowIndex = 0; rowIndex < request.Requirements.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = request.Requirements[rowIndex];
            var row = rowIndex + 1;

            SetBodyCell(table[row, 0], item.Title);
            SetBodyCell(table[row, 1], item.DueDate.ToString("MMM d, yyyy"));
            SetBodyCell(table[row, 2], item.Status);
            SetUrgencyCell(table[row, 3], item.Urgency);
            SetBodyCell(table[row, 4], item.Category);
        }

        table.TableFormat.Borders.BorderType = BorderStyle.Single;
        table.TableFormat.Paddings.All = 4f;
        table.TableFormat.IsBreakAcrossPages = true;
    }

    private static void AddLinkedDocumentsSection(
        IWSection section,
        CreateCouncilPacketRequest request,
        CancellationToken cancellationToken)
    {
        var withLinks = request.Requirements
            .Where(r => r.LinkedDocuments.Count > 0)
            .ToList();

        if (withLinks.Count == 0)
            return;

        section.AddParagraph().AppendBreak(BreakType.PageBreak);
        AddHeading(section, "Linked document summaries");

        foreach (var requirement in withLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddSubheading(section, requirement.Title);

            foreach (var linked in requirement.LinkedDocuments)
            {
                AddParagraph(section, $"• {linked.FileName}");
                if (!string.IsNullOrWhiteSpace(linked.Summary))
                    AddParagraph(section, TruncateSummary(linked.Summary));
            }

            section.AddParagraph();
        }
    }

    private static void SetHeaderCell(WTableCell cell, string text)
    {
        cell.CellFormat.BackColor = Color.FromArgb(31, 56, 100);
        var paragraph = cell.AddParagraph();
        var range = paragraph.AppendText(text);
        range.CharacterFormat.Bold = true;
        range.CharacterFormat.TextColor = Color.White;
    }

    private static void SetBodyCell(WTableCell cell, string text)
    {
        cell.AddParagraph().AppendText(text);
    }

    private static void SetUrgencyCell(WTableCell cell, string urgencyLabel)
    {
        if (!Enum.TryParse<RequirementUrgency>(urgencyLabel, ignoreCase: true, out var urgency) &&
            !TryParseUrgencyLabel(urgencyLabel, out urgency))
        {
            SetBodyCell(cell, urgencyLabel);
            return;
        }

        var (r, g, b) = RequirementUrgencyHelper.GetTableColor(urgency);
        cell.CellFormat.BackColor = Color.FromArgb(r, g, b);
        var paragraph = cell.AddParagraph();
        var range = paragraph.AppendText(RequirementUrgencyHelper.GetLabel(urgency));
        range.CharacterFormat.Bold = true;
        range.CharacterFormat.TextColor = urgency is RequirementUrgency.Medium
            ? Color.FromArgb(31, 41, 55)
            : Color.White;
    }

    private static bool TryParseUrgencyLabel(string label, out RequirementUrgency urgency)
    {
        urgency = label switch
        {
            "Done" => RequirementUrgency.Completed,
            "Overdue" => RequirementUrgency.Overdue,
            "High" => RequirementUrgency.High,
            "Medium" => RequirementUrgency.Medium,
            "Low" => RequirementUrgency.Low,
            _ => RequirementUrgency.Low
        };
        return label is "Done" or "Overdue" or "High" or "Medium" or "Low";
    }

    private static string TruncateSummary(string summary)
    {
        var normalized = summary.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= SummaryMaxLength
            ? normalized
            : normalized[..SummaryMaxLength] + "…";
    }

    private static void AddHeading(IWSection section, string text)
    {
        var paragraph = section.AddParagraph();
        paragraph.ApplyStyle(BuiltinStyle.Heading1);
        paragraph.AppendText(text);
    }

    private static void AddSubheading(IWSection section, string text)
    {
        var paragraph = section.AddParagraph();
        paragraph.ApplyStyle(BuiltinStyle.Heading2);
        paragraph.AppendText(text);
    }

    private static void AddParagraph(IWSection section, string text) =>
        section.AddParagraph().AppendText(text);
}
