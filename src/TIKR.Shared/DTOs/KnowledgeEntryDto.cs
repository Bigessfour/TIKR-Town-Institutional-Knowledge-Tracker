using System.ComponentModel.DataAnnotations;
using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record KnowledgeEntryDto(
    Guid Id,
    string Title,
    string Content,
    KnowledgeCategory Category,
    int SortOrder);

public record CreateKnowledgeEntryRequest(
    [Required, MaxLength(500)] string Title,
    [Required] string Content,
    KnowledgeCategory Category,
    int SortOrder);

public record UpdateKnowledgeEntryRequest(
    [Required, MaxLength(500)] string Title,
    [Required] string Content,
    KnowledgeCategory Category,
    int SortOrder);
