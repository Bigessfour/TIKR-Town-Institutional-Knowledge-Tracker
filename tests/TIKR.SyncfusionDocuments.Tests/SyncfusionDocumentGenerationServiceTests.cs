using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
        return new SyncfusionDocumentGenerationService(config);
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
}
