using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Web.Helpers;

public static class KnowledgeWorkflowHelpers
{
    public static CreateKnowledgeEntryRequest ToCreateRequest(KnowledgeEntryDto entry) =>
        new(entry.Title, entry.Content, entry.Category, entry.SortOrder);
}
