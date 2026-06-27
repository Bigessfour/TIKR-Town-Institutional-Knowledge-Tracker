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
    }

    private static void LoadIfExists(string path)
    {
        if (!File.Exists(path))
            return;

        DotNetEnv.Env.Load(path);
    }
}
