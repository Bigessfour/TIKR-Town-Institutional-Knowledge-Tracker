namespace TIKR.Shared.Configuration;

public static class EnvLoader
{
    /// <summary>
    /// Loads .env files from the repo root and docker/ folder (development only).
    /// Does not override existing environment variables.
    /// </summary>
    public static void LoadDevelopmentEnv(string contentRootPath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(contentRootPath, "..", ".."));
        LoadIfExists(Path.Combine(repoRoot, ".env"));
        LoadIfExists(Path.Combine(repoRoot, "docker", ".env"));
        // Clerk-rotated secrets written by Settings (shared NAS volume).
        LoadIfExists(Path.Combine(repoRoot, ".local-data", "runtime-secrets.env"));
        LoadIfExists("/data/runtime-secrets.env");
    }

    /// <summary>Load runtime secrets in any environment (NAS volume).</summary>
    public static void LoadRuntimeSecrets(string? dataPath = null)
    {
        if (!string.IsNullOrWhiteSpace(dataPath))
            LoadIfExists(Path.Combine(dataPath.Trim(), "runtime-secrets.env"));
        LoadIfExists("/data/runtime-secrets.env");
    }

    private static void LoadIfExists(string path)
    {
        if (!File.Exists(path))
            return;

        // Preserve process env (e.g. local ConnectionStrings__Default) over docker/.env paths.
        DotNetEnv.Env.Load(path, DotNetEnv.LoadOptions.NoClobber());
    }
}
