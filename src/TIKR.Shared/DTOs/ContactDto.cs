using System.ComponentModel.DataAnnotations;
using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record ContactDto(
    Guid Id,
    string Name,
    string? Role,
    string? Organization,
    string? Address,
    string? Office,
    string? Email,
    string? Phone,
    string? Notes,
    ContactCategory Categories,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CreatedBy,
    DateTime? DeletedAt,
    bool? IsPrimary = null);

public record CreateContactRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(200)] string? Role = null,
    [MaxLength(300)] string? Organization = null,
    [MaxLength(500)] string? Address = null,
    [MaxLength(200)] string? Office = null,
    [MaxLength(200)] string? Email = null,
    [MaxLength(50)] string? Phone = null,
    [MaxLength(2000)] string? Notes = null,
    ContactCategory Categories = ContactCategory.Custom);

public record UpdateContactRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(200)] string? Role = null,
    [MaxLength(300)] string? Organization = null,
    [MaxLength(500)] string? Address = null,
    [MaxLength(200)] string? Office = null,
    [MaxLength(200)] string? Email = null,
    [MaxLength(50)] string? Phone = null,
    [MaxLength(2000)] string? Notes = null,
    ContactCategory Categories = ContactCategory.Custom);
