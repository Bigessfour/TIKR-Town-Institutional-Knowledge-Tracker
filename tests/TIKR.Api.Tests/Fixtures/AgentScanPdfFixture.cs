using Syncfusion.Drawing;
using Syncfusion.Licensing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace TIKR.Api.Tests.Fixtures;

/// <summary>
/// Builds a Syncfusion-valid PDF for licensed agent-scan tests (hand-crafted PDF bytes fail parser validation).
/// </summary>
internal static class AgentScanPdfFixture
{
    public const string ExpectedText = "Wiley clerk report";

    public static byte[] CreateMinimalClerkReportPdf()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (!string.IsNullOrWhiteSpace(licenseKey))
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        using var document = new PdfDocument();
        var page = document.Pages.Add();
        var font = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
        page.Graphics.DrawString(ExpectedText, font, PdfBrushes.Black, new PointF(72, 720));

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    public static async Task<byte[]> EnsureMinimalClerkReportPdfAsync(string fixturePath)
    {
        var bytes = CreateMinimalClerkReportPdf();
        var dir = Path.GetDirectoryName(fixturePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllBytesAsync(fixturePath, bytes);
        return bytes;
    }
}
