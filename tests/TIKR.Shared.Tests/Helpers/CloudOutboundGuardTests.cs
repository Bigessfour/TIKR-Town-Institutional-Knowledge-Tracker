using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class CloudOutboundGuardTests
{
    [Fact]
    public void Redact_ReplacesEmailPhoneAndSsn()
    {
        var text = "Email clerk@townofwiley.gov, call (970) 555-0142, id 123-45-6789.";
        var redacted = CloudOutboundGuard.Redact(text);

        redacted.Should().Contain("[email]");
        redacted.Should().Contain("[phone]");
        redacted.Should().Contain("[id]");
        redacted.Should().NotContain("clerk@townofwiley.gov");
        redacted.Should().NotContain("555-0142");
        redacted.Should().NotContain("123-45-6789");
    }

    [Fact]
    public void Redact_ReplacesBareTenDigitPhone()
    {
        CloudOutboundGuard.Redact("Call 9705550142 today.")
            .Should().Be("Call [phone] today.");
    }

    [Fact]
    public void Redact_LeavesOrdinaryTownText()
    {
        CloudOutboundGuard.Redact("Ordinance 12 mill levy for FY 2026")
            .Should().Be("Ordinance 12 mill levy for FY 2026");
    }
}
