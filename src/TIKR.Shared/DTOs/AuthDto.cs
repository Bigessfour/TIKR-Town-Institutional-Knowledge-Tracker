namespace TIKR.Shared.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    IReadOnlyList<string> Roles,
    string? RefreshToken = null,
    DateTime? RefreshExpiresAt = null);

public record RefreshTokenRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(string Message, string? ResetToken);

public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

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

public record ClerkTourStateDto(string? CompletedVersion, bool AutoTourDisabled);

public record UpdateClerkTourStateRequest(string? CompletedVersion, bool? AutoTourDisabled);
