using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record KnowledgeEntryDto(
    Guid Id,
    string Title,
    string Content,
    KnowledgeCategory Category,
    int SortOrder);

public record CreateKnowledgeEntryRequest(
    string Title,
    string Content,
    KnowledgeCategory Category,
    int SortOrder);

public record UpdateKnowledgeEntryRequest(
    string Title,
    string Content,
    KnowledgeCategory Category,
    int SortOrder);
