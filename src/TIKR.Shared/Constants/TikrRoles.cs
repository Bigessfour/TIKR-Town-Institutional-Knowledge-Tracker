namespace TIKR.Shared.Constants;

public static class TikrRoles
{
    public const string Admin = "Admin";
    public const string Clerk = "Clerk";
    public const string Viewer = "Viewer";

    public static bool IsAssignableRole(string role) =>
        role is Admin or Clerk or Viewer;

    public static bool CanWrite(string role) =>
        role is Admin or Clerk;
}
