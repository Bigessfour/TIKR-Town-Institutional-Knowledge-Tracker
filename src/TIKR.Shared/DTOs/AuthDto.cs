namespace TIKR.Shared.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    IReadOnlyList<string> Roles);

public record CreateUserRequest(
    string Email,
    string Password,
    string? DisplayName,
    string Role);

public record UpdateUserRequest(
    bool? IsActive,
    string? NewPassword,
    string? Role);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UserSummaryDto(
    string Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles);

public record UserProfileDto(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
