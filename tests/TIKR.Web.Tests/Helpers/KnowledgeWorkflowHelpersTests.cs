using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class KnowledgeWorkflowHelpersTests
{
    [Fact]
    public void ToCreateRequest_PreservesEntryFields()
    {
        var entry = new KnowledgeEntryDto(
            Guid.NewGuid(), "Council packet", "Steps for packets", KnowledgeCategory.HowTo, 3);

        var request = KnowledgeWorkflowHelpers.ToCreateRequest(entry);

        request.Title.Should().Be("Council packet");
        request.Content.Should().Be("Steps for packets");
        request.Category.Should().Be(KnowledgeCategory.HowTo);
        request.SortOrder.Should().Be(3);
    }
}
