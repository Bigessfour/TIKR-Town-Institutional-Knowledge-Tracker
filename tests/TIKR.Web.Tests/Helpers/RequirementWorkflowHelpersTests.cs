using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class RequirementWorkflowHelpersTests
{
    private static readonly DateOnly Today = new(2026, 6, 28);

    [Fact]
    public void GetUrgency_ReturnsCompleted_WhenMarkedDone()
    {
        var req = new RequirementDto(Guid.NewGuid(), "Done item", null, Today, RecurrenceType.Annual,
            RequirementCategory.Custom, false, true);

        RequirementWorkflowHelpers.GetUrgency(req, Today).Should().Be(RequirementUrgency.Completed);
    }

    [Theory]
    [InlineData(-1, RequirementUrgency.Overdue)]
    [InlineData(0, RequirementUrgency.High)]
    [InlineData(14, RequirementUrgency.High)]
    [InlineData(15, RequirementUrgency.Medium)]
    [InlineData(30, RequirementUrgency.Medium)]
    [InlineData(31, RequirementUrgency.Low)]
    public void GetUrgency_MapsDayWindows(int dayOffset, RequirementUrgency expected)
    {
        var req = new RequirementDto(Guid.NewGuid(), "Item", null, Today.AddDays(dayOffset),
            RecurrenceType.Annual, RequirementCategory.Custom, false, false);

        RequirementWorkflowHelpers.GetUrgency(req, Today).Should().Be(expected);
    }

    [Fact]
    public void GetUrgencyCssClass_MapsKnownLevels()
    {
        RequirementWorkflowHelpers.GetUrgencyCssClass(RequirementUrgency.Overdue).Should().Be("priority-overdue");
        RequirementWorkflowHelpers.GetUrgencyCssClass(RequirementUrgency.Completed).Should().Be("urgency-completed");
    }

    [Fact]
    public void FilterRequirements_AppliesSearchCategoryAndUrgency()
    {
        var items = new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Budget report", "Annual filing", Today.AddDays(5), RecurrenceType.Annual,
                RequirementCategory.Budget, true, false),
            new(Guid.NewGuid(), "Custom task", "Internal", Today.AddDays(60), RecurrenceType.None,
                RequirementCategory.Custom, false, false),
            new(Guid.NewGuid(), "Old filing", null, Today.AddDays(-3), RecurrenceType.Annual,
                RequirementCategory.Compliance, true, false)
        };

        var filtered = RequirementWorkflowHelpers.FilterRequirements(
            items,
            searchText: "budget",
            categoryFilter: RequirementCategory.Budget,
            urgencyFilter: RequirementUrgency.High,
            includeCompleted: false,
            Today).ToList();

        filtered.Should().ContainSingle().Which.Title.Should().Be("Budget report");
    }

    [Fact]
    public void FilterRequirements_ExcludesCompletedByDefault()
    {
        var items = new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Open", null, Today, RecurrenceType.Annual, RequirementCategory.Custom, false, false),
            new(Guid.NewGuid(), "Closed", null, Today, RecurrenceType.Annual, RequirementCategory.Custom, false, true)
        };

        RequirementWorkflowHelpers.FilterRequirements(items, null, null, null, includeCompleted: false, Today)
            .Should().ContainSingle().Which.Title.Should().Be("Open");
    }

    [Fact]
    public void BuildCsv_EscapesCommasAndQuotes()
    {
        var items = new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Report, annual", "Desc \"quoted\"", Today, RecurrenceType.Annual,
                RequirementCategory.Budget, true, false)
        };

        var csv = RequirementWorkflowHelpers.BuildCsv(items, Today);

        csv.Should().Contain("Title,Description,Due Date");
        csv.Should().Contain("\"Report, annual\"");
        csv.Should().Contain("\"Desc \"\"quoted\"\"\"");
        csv.Should().Contain("High");
    }

    [Fact]
    public void ApplyAgentExtraction_MapsAgentResultToCreateRequest()
    {
        var result = new DocumentAgentResult(
            "Periodic Report",
            "Extracted filing text",
            new DateOnly(2026, 12, 1),
            RecurrenceType.Annual,
            RequirementCategory.Compliance,
            2,
            "agent/report.pdf",
            true);

        var request = RequirementWorkflowHelpers.ApplyAgentExtraction(result);

        request.Title.Should().Be("Periodic Report");
        request.Description.Should().Be("Extracted filing text");
        request.DueDate.Should().Be(new DateOnly(2026, 12, 1));
        request.Category.Should().Be(RequirementCategory.Compliance);
    }

    [Fact]
    public void FormatAgentScanMessage_IncludesTableCount()
    {
        var result = new DocumentAgentResult(
            "x", null, null, RecurrenceType.None, RequirementCategory.Custom, 3, "p", true);

        RequirementWorkflowHelpers.FormatAgentScanMessage(result)
            .Should().Be("Processed on Synology • 3 tables extracted");
    }

    [Fact]
    public void ToCreateRequest_PreservesRequirementFields()
    {
        var req = new RequirementDto(
            Guid.NewGuid(), "Budget filing", "Annual", Today, RecurrenceType.Annual,
            RequirementCategory.Budget, false, false);

        var request = RequirementWorkflowHelpers.ToCreateRequest(req);

        request.Title.Should().Be("Budget filing");
        request.Description.Should().Be("Annual");
        request.DueDate.Should().Be(Today);
        request.Recurrence.Should().Be(RecurrenceType.Annual);
        request.Category.Should().Be(RequirementCategory.Budget);
    }
}
