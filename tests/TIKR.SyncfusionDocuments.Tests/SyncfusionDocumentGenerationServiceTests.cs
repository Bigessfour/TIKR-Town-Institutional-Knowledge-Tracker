using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Syncfusion.Pdf.Parsing;
using TIKR.Shared.DTOs;
using TIKR.SyncfusionDocuments;

namespace TIKR.SyncfusionDocuments.Tests;

public class SyncfusionDocumentGenerationServiceTests
{
    private static SyncfusionDocumentGenerationService CreateService(string? licenseKey = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SYNCFUSION_LICENSE_KEY"] = licenseKey
            })
            .Build();
        return new SyncfusionDocumentGenerationService(config, NullLogger<SyncfusionDocumentGenerationService>.Instance);
    }

    [Fact]
    public async Task GenerateCouncilAgendaPdf_WithoutLicense_Throws()
    {
        var service = CreateService();
        var request = new CouncilAgendaRequest(
            "Wiley",
            new DateOnly(2026, 7, 8),
            [new CouncilAgendaItem("Budget hearing", "Annual budget review", null)]);

        var act = () => service.GenerateCouncilAgendaPdfAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SYNCFUSION_LICENSE_KEY*");
    }

    [Fact]
    [Trait("Category", "SyncfusionLicensed")]
    public async Task GenerateCouncilAgendaPdf_WhenLicensed_ReturnsPdf()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        SyncfusionDocumentLicense.RegisterFromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = licenseKey })
                .Build());

        var service = CreateService(licenseKey);
        var result = await service.GenerateCouncilAgendaPdfAsync(
            new CouncilAgendaRequest(
                "Wiley",
                new DateOnly(2026, 7, 8),
                [new CouncilAgendaItem("TABOR notice", "Post election notice", new DateOnly(2026, 9, 1))]));

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
    }

    [Fact]
    public async Task GenerateMeetingMinutesDocx_WhenLicensed_ReturnsDocx()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        var service = CreateService(licenseKey);
        var result = await service.GenerateMeetingMinutesDocxAsync(
            new MeetingMinutesRequest(
                "Wiley",
                new DateOnly(2026, 7, 8),
                "Board of Trustees",
                ["Mayor Smith", "Clerk Jones"],
                ["Call to order", "Budget hearing"],
                "Motion carried unanimously."));

        result.ContentType.Should().Contain("wordprocessingml");
        result.FileName.Should().EndWith(".docx");
        result.Content.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task GenerateComplianceReportXlsx_WhenLicensed_ReturnsSpreadsheet()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        var service = CreateService(licenseKey);
        var result = await service.GenerateComplianceReportXlsxAsync(
            new ComplianceReportRequest(
                "Wiley",
                new DateOnly(2026, 7, 8),
                [new ComplianceReportRow("Audit due", "Annual audit", new DateOnly(2026, 7, 31), "Audit", false)]));

        result.ContentType.Should().Contain("spreadsheetml");
        result.FileName.Should().EndWith(".xlsx");
        result.Content.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task GenerateCouncilPacket_WithoutLicense_Throws()
    {
        var service = CreateService();
        var request = CreateSamplePacketRequest();

        var act = () => service.GenerateCouncilPacketAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SYNCFUSION_LICENSE_KEY*");
    }

    [Fact]
    [Trait("Category", "SyncfusionLicensed")]
    public async Task GenerateCouncilPacket_WhenLicensed_ReturnsPdfAndDocx()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        SyncfusionDocumentLicense.RegisterFromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = licenseKey })
                .Build());

        var service = CreateService(licenseKey);
        var result = await service.GenerateCouncilPacketAsync(CreateSamplePacketRequest());

        result.PdfFileName.Should().EndWith(".pdf");
        result.DocxFileName.Should().EndWith(".docx");
        result.PdfContent.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
        result.DocxContent.Length.Should().BeGreaterThan(200);
    }

    private static CreateCouncilPacketRequest CreateSamplePacketRequest() =>
        new(
            "Wiley",
            new DateOnly(2026, 7, 8),
            null,
            [
                new CouncilPacketRequirementItem(
                    Guid.NewGuid(),
                    "TABOR notice",
                    "Post election notice",
                    new DateOnly(2026, 9, 1),
                    "Compliance",
                    "Open",
                    "Medium",
                    false,
                    [
                        new CouncilPacketLinkedDocument(
                            Guid.NewGuid(),
                            "election-notice.pdf",
                            "Summary of election filing requirements for the November cycle.")
                    ])
            ]);

    [Fact]
    [Trait("Category", "SyncfusionLicensed")]
    public async Task CreateAgentArchivePdfAsync_WhenLicensed_ForPdf_ReturnsAiArchiveWithStampAndMetadata()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        SyncfusionDocumentLicense.RegisterFromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = licenseKey })
                .Build());

        var service = CreateService(licenseKey);

        // Use fixture to get a valid minimal PDF (exercises load + per-page stamp + metadata path)
        var pdfBytes = AgentScanPdfFixture.CreateMinimalClerkReportPdf();
        await using var stream = new MemoryStream(pdfBytes);
        var result = await service.CreateAgentArchivePdfAsync(stream, "clerk-report.pdf", DateTime.UtcNow);

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Contain(".ai-archive.pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);

        // Real behavior: reload and verify archive metadata (Title/Subject/Keywords contain key phrases + original name)
        using var reloaded = new PdfLoadedDocument(new MemoryStream(result.Content));
        reloaded.DocumentInformation.Title.Should().Contain("AI Archive");
        reloaded.DocumentInformation.Subject.Should().Contain("AI PROCESSED - TIKR VAULT");
        reloaded.DocumentInformation.Keywords.Should().Contain("clerk-report.pdf");
    }

    [Fact]
    [Trait("Category", "SyncfusionLicensed")]
    public async Task ConvertImageToPdfAsync_WhenLicensed_HandlesInvalidImageWithAiFallback()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        var service = CreateService(licenseKey);

        // Invalid bytes -> exercises fallback path (produces valid stamped PDF)
        await using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });
        var result = await service.ConvertImageToPdfAsync(stream, "bad-image.png");

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
    }

    [Fact]
    [Trait("Category", "SyncfusionLicensed")]
    public async Task ConvertImageToPdfAsync_WhenLicensed_ForValidImage_ReturnsPdf()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        var service = CreateService(licenseKey);

        // Synthesize a minimal valid PNG via Skia (no external asset dependency for CI)
        using var bitmap = new SKBitmap(64, 48);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.LightBlue);
            canvas.Flush();
        }
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        await using var imageStream = new MemoryStream(data.ToArray());

        var result = await service.ConvertImageToPdfAsync(imageStream, "diagram.png");

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
    }
}
