using Microsoft.Extensions.Configuration;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Allowlisted roots for clerk-editable paths (document storage, library scan, email drop).
/// Includes local TIKR data dirs plus mounted network/external volumes (not the system disk).
/// Browse UI and Save stay aligned on this list.
/// </summary>
public static class TikrDataPathRoots
{
    /// <summary>
    /// Optional extra roots (colon or semicolon separated), e.g. Synology bind mounts
    /// <c>TIKR_BROWSE_EXTRA_ROOTS=/volume1/town-docs:/mnt/nas</c>.
    /// </summary>
    public const string ExtraRootsConfigKey = "TIKR_BROWSE_EXTRA_ROOTS";

    public static IReadOnlyList<string> Resolve(IConfiguration configuration, string? currentDirectory = null)
    {
        var roots = new List<string>();
        var dataPath = configuration["TIKR_DATA_PATH"];
        if (!string.IsNullOrWhiteSpace(dataPath))
            Add(roots, dataPath.Trim());

        if (Directory.Exists("/data"))
            Add(roots, "/data");

        var cwd = string.IsNullOrWhiteSpace(currentDirectory)
            ? Directory.GetCurrentDirectory()
            : currentDirectory;

        AddIfExists(roots, Path.GetFullPath(Path.Combine(cwd, "..", "..", ".local-data")));
        AddIfExists(roots, Path.GetFullPath(Path.Combine(cwd, ".local-data")));

        AddMountedVolumeRoots(roots);
        AddConfiguredExtraRoots(configuration, roots);

        return roots;
    }

    public static bool IsUnderAllowedRoot(string path, IReadOnlyList<string> roots)
    {
        if (roots.Count == 0)
            return true;

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        return roots.Select(Path.GetFullPath).Any(root =>
            full.Equals(root, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(
                root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>True for macOS system / recovery / Time Machine volume names (not town shares).</summary>
    public static bool IsExcludedVolumeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var n = name.Trim();
        if (n.Equals("Macintosh HD", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.Equals("Macintosh HD - Data", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.Equals("Recovery", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.StartsWith("com.apple.TimeMachine", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.StartsWith("Backups of ", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.StartsWith(".", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static void AddMountedVolumeRoots(List<string> roots)
    {
        // macOS: SMB/AFP/USB mounts appear under /Volumes (skip system disk).
        AddChildDirectoriesAsRoots(roots, "/Volumes");
        // Linux desktop mounts
        AddChildDirectoriesAsRoots(roots, "/mnt");
        AddChildDirectoriesAsRoots(roots, "/media");
        // Synology DSM host paths (when API runs on the NAS, not only in Docker /data)
        AddIfExists(roots, "/volume1");
        AddIfExists(roots, "/volume2");
        AddIfExists(roots, "/volumeUSB1");
        AddIfExists(roots, "/volumeUSB2");
    }

    private static void AddChildDirectoriesAsRoots(List<string> roots, string parent)
    {
        if (!Directory.Exists(parent))
            return;

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(parent);
        }
        catch
        {
            return;
        }

        foreach (var dir in children)
        {
            string name;
            try
            {
                name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch
            {
                continue;
            }

            if (IsExcludedVolumeName(name))
                continue;

            // Resolve symlinks: /Volumes/Macintosh HD → / must never become a root.
            try
            {
                var full = Path.GetFullPath(dir);
                if (full is "/" or "\\" || full.Length <= 1)
                    continue;
                // Windows-style drive root only
                if (full.Length == 3 && full[1] == ':' && (full[2] == '\\' || full[2] == '/'))
                    continue;
            }
            catch
            {
                continue;
            }

            AddIfExists(roots, dir);
        }
    }

    private static void AddConfiguredExtraRoots(IConfiguration configuration, List<string> roots)
    {
        var raw = configuration[ExtraRootsConfigKey];
        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var part in raw.Split([':', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains("..", StringComparison.Ordinal))
                continue;
            AddIfExists(roots, part);
        }
    }

    private static void Add(List<string> roots, string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (full is "/" or "\\" || full.Length <= 1)
                return;
            if (roots.Any(r => r.Equals(full, StringComparison.OrdinalIgnoreCase)))
                return;
            roots.Add(full);
        }
        catch
        {
            // ignore unparseable
        }
    }

    private static void AddIfExists(List<string> roots, string path)
    {
        try
        {
            if (Directory.Exists(path))
                Add(roots, path);
        }
        catch
        {
            // ignore
        }
    }
}
