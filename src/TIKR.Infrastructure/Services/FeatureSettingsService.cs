using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;
using TIKR.SyncfusionDocuments;

namespace TIKR.Infrastructure.Services;

public interface IFeatureSettingsService
{
    Task LoadIntoStateAsync(CancellationToken cancellationToken = default);
    Task<FeatureSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<FeatureSettingsDto> UpdateAsync(UpdateFeatureSettingsRequest request, CancellationToken cancellationToken = default);
}

public sealed class FeatureSettingsService(
    TikrDbContext db,
    IConfiguration configuration,
    FeatureSettingsState state,
    IOllamaChatClientFactory ollamaFactory,
    ISecretProtector secrets,
    IRuntimeSecretsStore runtimeSecrets,
    IAuditService audit,
    ICurrentUserService currentUser,
    IHostEnvironment hostEnvironment,
    ILogger<FeatureSettingsService> logger) : IFeatureSettingsService
{
    public async Task LoadIntoStateAsync(CancellationToken cancellationToken = default)
    {
        var overrides = await LoadOverridesAsync(cancellationToken);
        var snapshot = BuildSnapshot(overrides);
        state.Replace(snapshot);
        // Startup / read path: never wipe process env keys that IT set outside Settings.
        ApplySecrets(snapshot, clearMissingSecrets: false);
    }

    public async Task<FeatureSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        var available = await ollamaFactory.IsAvailableAsync(cancellationToken);
        return ToDto(state.Current, available);
    }

    public async Task<FeatureSettingsDto> UpdateAsync(
        UpdateFeatureSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        var preview = await LoadOverridesAsync(cancellationToken);
        var previousPath = state.Current.FileStoragePath;

        ApplyRequestToPreview(request, preview);

        var snapshot = BuildSnapshot(preview);
        if (snapshot.UseGrok && !snapshot.GrokApiKeyConfigured)
            throw new ArgumentException(
                "Advanced AI needs a Grok API key. Paste one under Licenses & keys, or ask IT to set GROK_API_KEY on the NAS.");

        if (request.FileStoragePath is not null)
        {
            ValidateContainedPath(snapshot.FileStoragePath, "Document storage");
            Directory.CreateDirectory(snapshot.FileStoragePath);
            if (request.MoveExistingDocuments == true
                && !string.Equals(previousPath, snapshot.FileStoragePath, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(previousPath))
            {
                ValidateContainedPath(previousPath, "Current document storage");
                MoveDirectoryContents(previousPath, snapshot.FileStoragePath);
            }
        }

        await PersistPreviewAsync(request, snapshot, preview, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        state.Replace(snapshot);
        // Clerk save may clear keys — allow clearing process env when snapshot has none.
        ApplySecrets(snapshot, clearMissingSecrets: true);

        if (snapshot.SyncfusionLicenseKeyConfigured)
        {
            try
            {
                SyncfusionDocumentLicense.RegisterFromConfiguration(configuration);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Syncfusion re-register after Settings save failed");
            }
        }

        await audit.LogAsync(
            "Updated",
            "FeatureSettings",
            null,
            $"UseGrok={snapshot.UseGrok}; OllamaHost={snapshot.OllamaHost}; Storage={snapshot.FileStoragePath}; SyncfusionConfigured={snapshot.SyncfusionLicenseKeyConfigured}",
            currentUser.UserId);

        logger.LogInformation(
            "Clerk settings updated: UseGrok={UseGrok}, OllamaHost={OllamaHost}, Storage={Storage}",
            snapshot.UseGrok,
            snapshot.OllamaHost,
            snapshot.FileStoragePath);

        var available = await ollamaFactory.IsAvailableAsync(cancellationToken);
        var dto = ToDto(snapshot, available);
        return dto with
        {
            StatusMessage = available
                ? "Saved. Local AI is reachable."
                : "Saved. Local AI is not reachable yet — check the Ollama address and that Ollama is running."
        };
    }

    private void ApplyRequestToPreview(UpdateFeatureSettingsRequest request, Dictionary<string, string> preview)
    {
        if (request.OllamaHost is not null)
        {
            var host = request.OllamaHost.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Ollama address cannot be empty.");
            if (!Uri.TryCreate(host, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Ollama address must be an http(s) URL, e.g. http://127.0.0.1:11434");
            preview[FeatureSettingKeys.OllamaHost] = host;
        }

        if (request.OllamaChatModel is not null)
        {
            var model = request.OllamaChatModel.Trim();
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Chat model cannot be empty.");
            preview[FeatureSettingKeys.OllamaChatModel] = model;
        }

        if (request.UseGrok is { } useGrok)
            preview[FeatureSettingKeys.UseGrok] = useGrok ? "true" : "false";

        if (request.GrokApiKey is not null)
        {
            if (string.IsNullOrWhiteSpace(request.GrokApiKey))
                preview.Remove(FeatureSettingKeys.GrokApiKey);
            else
                preview[FeatureSettingKeys.GrokApiKey] = request.GrokApiKey.Trim();
        }

        if (request.GrokModel is not null)
        {
            var model = TikrConfiguration.NormalizeGrokModel(request.GrokModel);
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Grok model cannot be empty.");
            preview[FeatureSettingKeys.GrokModel] = model;
        }

        if (request.SyncfusionLicenseKey is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SyncfusionLicenseKey))
                preview.Remove(FeatureSettingKeys.SyncfusionLicenseKey);
            else
                preview[FeatureSettingKeys.SyncfusionLicenseKey] = request.SyncfusionLicenseKey.Trim();
        }

        if (request.FileStoragePath is not null)
        {
            var path = request.FileStoragePath.Trim();
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Document storage path cannot be empty.");
            ValidateContainedPath(path, "Document storage");
            preview[FeatureSettingKeys.FileStoragePath] = path;
        }

        if (request.TownName is not null)
        {
            var town = request.TownName.Trim();
            if (string.IsNullOrWhiteSpace(town))
                throw new ArgumentException("Town name cannot be empty.");
            preview[FeatureSettingKeys.TownName] = town;
        }

        if (request.StorageLabel is not null)
            preview[FeatureSettingKeys.StorageLabel] = request.StorageLabel.Trim();

        if (request.TownLogoPath is not null)
        {
            if (string.IsNullOrWhiteSpace(request.TownLogoPath))
                preview.Remove(FeatureSettingKeys.TownLogoPath);
            else
            {
                var path = request.TownLogoPath.Trim();
                ValidateContainedPath(path, "Town logo");
                preview[FeatureSettingKeys.TownLogoPath] = path;
            }
        }

        if (request.OcrEnabled is { } ocr)
            preview[FeatureSettingKeys.OcrEnabled] = ocr ? "true" : "false";

        if (request.UseSyncfusionAgentTools is { } agents)
            preview[FeatureSettingKeys.UseSyncfusionAgentTools] = agents ? "true" : "false";

        if (request.UseSyncfusionAgentOrchestration is { } orch)
            preview[FeatureSettingKeys.UseSyncfusionAgentOrchestration] = orch ? "true" : "false";

        if (request.LibraryScanPath is not null)
        {
            if (string.IsNullOrWhiteSpace(request.LibraryScanPath))
                preview.Remove(FeatureSettingKeys.LibraryScanPath);
            else
            {
                var path = request.LibraryScanPath.Trim();
                ValidateContainedPath(path, "Town library scan");
                preview[FeatureSettingKeys.LibraryScanPath] = path;
            }
        }

        if (request.LibraryScanIntervalSeconds is { } interval)
        {
            if (interval < 30)
                throw new ArgumentException("Library scan interval must be at least 30 seconds.");
            preview[FeatureSettingKeys.LibraryScanIntervalSeconds] = interval.ToString();
        }

        if (request.LibraryScanMaxImports is { } maxImports)
        {
            if (maxImports < 0)
                throw new ArgumentException("Max imports cannot be negative (use 0 for unlimited).");
            preview[FeatureSettingKeys.LibraryScanMaxImports] = maxImports.ToString();
        }

        if (request.EmailInboxPath is not null)
        {
            if (string.IsNullOrWhiteSpace(request.EmailInboxPath))
                preview.Remove(FeatureSettingKeys.EmailInboxPath);
            else
            {
                var path = request.EmailInboxPath.Trim();
                ValidateContainedPath(path, "Email drop folder");
                preview[FeatureSettingKeys.EmailInboxPath] = path;
            }
        }
    }

    private async Task PersistPreviewAsync(
        UpdateFeatureSettingsRequest request,
        FeatureSettingsSnapshot snapshot,
        Dictionary<string, string> preview,
        CancellationToken cancellationToken)
    {
        async Task UpsertPlain(string key, string? value, bool clearIfMissing)
        {
            if (value is null && !clearIfMissing)
                return;
            if (string.IsNullOrWhiteSpace(value))
                await DeleteAsync(key, cancellationToken);
            else
                await UpsertAsync(key, value, cancellationToken);
        }

        if (request.OllamaHost is not null)
            await UpsertAsync(FeatureSettingKeys.OllamaHost, snapshot.OllamaHost, cancellationToken);
        if (request.OllamaChatModel is not null)
            await UpsertAsync(FeatureSettingKeys.OllamaChatModel, snapshot.OllamaChatModel, cancellationToken);
        if (request.UseGrok is not null)
            await UpsertAsync(FeatureSettingKeys.UseGrok, snapshot.UseGrok ? "true" : "false", cancellationToken);
        if (request.GrokModel is not null)
            await UpsertAsync(FeatureSettingKeys.GrokModel, snapshot.GrokModel, cancellationToken);

        if (request.GrokApiKey is not null)
        {
            if (string.IsNullOrWhiteSpace(request.GrokApiKey))
                await DeleteAsync(FeatureSettingKeys.GrokApiKey, cancellationToken);
            else
                await UpsertAsync(FeatureSettingKeys.GrokApiKey, secrets.Protect(request.GrokApiKey.Trim()), cancellationToken);
        }

        if (request.SyncfusionLicenseKey is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SyncfusionLicenseKey))
                await DeleteAsync(FeatureSettingKeys.SyncfusionLicenseKey, cancellationToken);
            else
                await UpsertAsync(
                    FeatureSettingKeys.SyncfusionLicenseKey,
                    secrets.Protect(request.SyncfusionLicenseKey.Trim()),
                    cancellationToken);
        }

        if (request.FileStoragePath is not null)
            await UpsertAsync(FeatureSettingKeys.FileStoragePath, snapshot.FileStoragePath, cancellationToken);
        if (request.TownName is not null)
            await UpsertAsync(FeatureSettingKeys.TownName, snapshot.TownName, cancellationToken);
        if (request.StorageLabel is not null)
            await UpsertAsync(FeatureSettingKeys.StorageLabel, snapshot.StorageLabel, cancellationToken);
        if (request.TownLogoPath is not null)
            await UpsertPlain(FeatureSettingKeys.TownLogoPath, snapshot.TownLogoPath, clearIfMissing: true);
        if (request.OcrEnabled is not null)
            await UpsertAsync(FeatureSettingKeys.OcrEnabled, snapshot.OcrEnabled ? "true" : "false", cancellationToken);
        if (request.UseSyncfusionAgentTools is not null)
            await UpsertAsync(
                FeatureSettingKeys.UseSyncfusionAgentTools,
                snapshot.UseSyncfusionAgentTools ? "true" : "false",
                cancellationToken);
        if (request.UseSyncfusionAgentOrchestration is not null)
            await UpsertAsync(
                FeatureSettingKeys.UseSyncfusionAgentOrchestration,
                snapshot.UseSyncfusionAgentOrchestration ? "true" : "false",
                cancellationToken);
        if (request.LibraryScanPath is not null)
            await UpsertPlain(FeatureSettingKeys.LibraryScanPath, snapshot.LibraryScanPath, clearIfMissing: true);
        if (request.LibraryScanIntervalSeconds is not null)
            await UpsertAsync(
                FeatureSettingKeys.LibraryScanIntervalSeconds,
                snapshot.LibraryScanIntervalSeconds.ToString(),
                cancellationToken);
        if (request.LibraryScanMaxImports is not null)
            await UpsertAsync(
                FeatureSettingKeys.LibraryScanMaxImports,
                snapshot.LibraryScanMaxImports.ToString(),
                cancellationToken);
        if (request.EmailInboxPath is not null)
            await UpsertPlain(FeatureSettingKeys.EmailInboxPath, snapshot.EmailInboxPath, clearIfMissing: true);

        _ = preview; // preview already applied into snapshot
    }

    private void ApplySecrets(FeatureSettingsSnapshot snapshot, bool clearMissingSecrets)
    {
        // WebApplicationFactory hosts share one process — mutating env bleeds into sibling test fixtures.
        if (hostEnvironment.IsEnvironment("Testing"))
            return;

        runtimeSecrets.ApplyToProcessEnvironment(
            snapshot.GrokApiKey,
            snapshot.SyncfusionLicenseKey,
            clearMissing: clearMissingSecrets);
        runtimeSecrets.WriteFile(snapshot.GrokApiKey, snapshot.SyncfusionLicenseKey);

        Environment.SetEnvironmentVariable("TIKR_OCR_ENABLED", snapshot.OcrEnabled ? "true" : "false");
        Environment.SetEnvironmentVariable(
            "USE_SYNCFUSION_AGENT_TOOLS",
            snapshot.UseSyncfusionAgentTools ? "true" : "false");
        Environment.SetEnvironmentVariable(
            "USE_SYNCFUSION_AGENT_ORCHESTRATION",
            snapshot.UseSyncfusionAgentOrchestration ? "true" : "false");
        Environment.SetEnvironmentVariable("TIKR_TOWN_NAME", snapshot.TownName);
        Environment.SetEnvironmentVariable("TIKR_STORAGE_LABEL", snapshot.StorageLabel);
        Environment.SetEnvironmentVariable("FILE_STORAGE_PATH", snapshot.FileStoragePath);
        Environment.SetEnvironmentVariable("FileStorage__BasePath", snapshot.FileStoragePath);
        Environment.SetEnvironmentVariable("OLLAMA_HOST", snapshot.OllamaHost);
        Environment.SetEnvironmentVariable("OLLAMA_CHAT_MODEL", snapshot.OllamaChatModel);
        Environment.SetEnvironmentVariable("GROK_MODEL", snapshot.GrokModel);
        Environment.SetEnvironmentVariable("USE_GROK", snapshot.UseGrok ? "true" : "false");

        if (!string.IsNullOrWhiteSpace(snapshot.LibraryScanPath))
            Environment.SetEnvironmentVariable("TIKR_LIBRARY_SCAN_PATH", snapshot.LibraryScanPath);
        else
            Environment.SetEnvironmentVariable("TIKR_LIBRARY_SCAN_PATH", null);

        Environment.SetEnvironmentVariable(
            "TIKR_LIBRARY_SCAN_INTERVAL_SECONDS",
            snapshot.LibraryScanIntervalSeconds.ToString());

        var maxImports = snapshot.LibraryScanMaxImports == int.MaxValue ? "0" : snapshot.LibraryScanMaxImports.ToString();
        Environment.SetEnvironmentVariable("TIKR_LIBRARY_SCAN_MAX_IMPORTS", maxImports);

        if (!string.IsNullOrWhiteSpace(snapshot.EmailInboxPath))
            Environment.SetEnvironmentVariable("TIKR_EMAIL_INBOX_PATH", snapshot.EmailInboxPath);
        else
            Environment.SetEnvironmentVariable("TIKR_EMAIL_INBOX_PATH", null);

        if (!string.IsNullOrWhiteSpace(snapshot.TownLogoPath))
            Environment.SetEnvironmentVariable("TIKR_TOWN_LOGO_PATH", snapshot.TownLogoPath);
        else
            Environment.SetEnvironmentVariable("TIKR_TOWN_LOGO_PATH", null);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        var overrides = await LoadOverridesAsync(cancellationToken);
        var snapshot = BuildSnapshot(overrides);
        state.Replace(snapshot);
        ApplySecrets(snapshot, clearMissingSecrets: false);
    }

    private FeatureSettingsSnapshot BuildSnapshot(IReadOnlyDictionary<string, string> overrides)
    {
        string? ResolveSecret(string key, string? envFallback)
        {
            if (overrides.TryGetValue(key, out var stored))
                return secrets.Unprotect(stored);
            return envFallback;
        }

        static bool ResolveBool(IReadOnlyDictionary<string, string> o, string key, bool fallback)
        {
            if (o.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
                return parsed;
            return fallback;
        }

        static int ResolveInt(IReadOnlyDictionary<string, string> o, string key, int fallback)
        {
            if (o.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed))
                return parsed;
            return fallback;
        }

        var host = overrides.TryGetValue(FeatureSettingKeys.OllamaHost, out var hostOverride)
            ? hostOverride
            : TikrConfiguration.GetOllamaHost(configuration);

        var chatModel = overrides.TryGetValue(FeatureSettingKeys.OllamaChatModel, out var modelOverride)
            ? modelOverride
            : TikrConfiguration.GetChatModel(configuration);

        var useGrok = overrides.TryGetValue(FeatureSettingKeys.UseGrok, out var useGrokRaw)
            && bool.TryParse(useGrokRaw, out var parsedUseGrok)
                ? parsedUseGrok
                : TikrConfiguration.GetUseGrok(configuration);

        var grokKey = ResolveSecret(FeatureSettingKeys.GrokApiKey, TikrConfiguration.GetGrokApiKey(configuration));
        var syncfusionKey = ResolveSecret(
            FeatureSettingKeys.SyncfusionLicenseKey,
            TikrConfiguration.GetSyncfusionLicenseKey(configuration));

        var grokModel = TikrConfiguration.NormalizeGrokModel(
            overrides.TryGetValue(FeatureSettingKeys.GrokModel, out var gm)
                ? gm
                : TikrConfiguration.GetGrokModel(configuration));

        var storage = overrides.TryGetValue(FeatureSettingKeys.FileStoragePath, out var fs)
            ? fs
            : TikrConfiguration.GetFileStoragePath(configuration);

        var town = overrides.TryGetValue(FeatureSettingKeys.TownName, out var tn)
            ? tn
            : configuration["TIKR_TOWN_NAME"] ?? "Wiley";

        var label = overrides.TryGetValue(FeatureSettingKeys.StorageLabel, out var sl)
            ? sl
            : configuration["TIKR_STORAGE_LABEL"] ?? "Synology NAS";

        string? logo = overrides.TryGetValue(FeatureSettingKeys.TownLogoPath, out var lp)
            ? lp
            : configuration["TIKR_TOWN_LOGO_PATH"];

        string? libraryPath = overrides.TryGetValue(FeatureSettingKeys.LibraryScanPath, out var lsp)
            ? lsp
            : TikrConfiguration.GetLibraryScanPath(configuration);

        string? emailPath = overrides.TryGetValue(FeatureSettingKeys.EmailInboxPath, out var eip)
            ? eip
            : TikrConfiguration.GetEmailInboxPath(configuration);

        return new FeatureSettingsSnapshot
        {
            OllamaHost = TikrConfiguration.RewriteDockerOnlyOllamaHost(host),
            OllamaChatModel = chatModel,
            UseGrok = useGrok,
            GrokApiKey = grokKey,
            GrokModel = grokModel,
            SyncfusionLicenseKey = syncfusionKey,
            FileStoragePath = storage,
            TownName = town,
            StorageLabel = label,
            TownLogoPath = string.IsNullOrWhiteSpace(logo) ? null : logo,
            OcrEnabled = ResolveBool(overrides, FeatureSettingKeys.OcrEnabled, TikrConfiguration.GetOcrEnabled(configuration)),
            UseSyncfusionAgentTools = ResolveBool(
                overrides,
                FeatureSettingKeys.UseSyncfusionAgentTools,
                TikrConfiguration.GetUseSyncfusionAgentTools(configuration)),
            UseSyncfusionAgentOrchestration = ResolveBool(
                overrides,
                FeatureSettingKeys.UseSyncfusionAgentOrchestration,
                TikrConfiguration.GetUseSyncfusionAgentOrchestration(configuration)),
            LibraryScanPath = string.IsNullOrWhiteSpace(libraryPath) ? null : libraryPath,
            LibraryScanIntervalSeconds = ResolveInt(
                overrides,
                FeatureSettingKeys.LibraryScanIntervalSeconds,
                TikrConfiguration.GetLibraryScanIntervalSeconds(configuration)),
            LibraryScanMaxImports = ResolveLibraryMaxImports(overrides, configuration),
            EmailInboxPath = string.IsNullOrWhiteSpace(emailPath) ? null : emailPath
        };
    }

    private static int ResolveLibraryMaxImports(
        IReadOnlyDictionary<string, string> overrides,
        IConfiguration configuration)
    {
        if (overrides.TryGetValue(FeatureSettingKeys.LibraryScanMaxImports, out var raw)
            && int.TryParse(raw, out var parsed))
            return parsed == 0 ? int.MaxValue : parsed;

        return TikrConfiguration.GetLibraryScanMaxImportsPerRun(configuration);
    }

    private async Task<Dictionary<string, string>> LoadOverridesAsync(CancellationToken cancellationToken)
    {
        return await db.AppSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
    }

    private async Task UpsertAsync(string key, string value, CancellationToken cancellationToken)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        row.Value = value;
        row.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (row is not null)
            db.AppSettings.Remove(row);
    }

    private static FeatureSettingsDto ToDto(FeatureSettingsSnapshot snap, bool ollamaAvailable) =>
        new(
            snap.OllamaHost,
            snap.OllamaChatModel,
            snap.UseGrok,
            snap.GrokApiKeyConfigured,
            ollamaAvailable,
            StatusMessage: null,
            GrokModel: snap.GrokModel,
            SyncfusionLicenseKeyConfigured: snap.SyncfusionLicenseKeyConfigured,
            SyncfusionLicenseHint: Hint(snap.SyncfusionLicenseKey),
            GrokApiKeyHint: Hint(snap.GrokApiKey),
            FileStoragePath: snap.FileStoragePath,
            TownName: snap.TownName,
            StorageLabel: snap.StorageLabel,
            TownLogoPath: snap.TownLogoPath,
            OcrEnabled: snap.OcrEnabled,
            UseSyncfusionAgentTools: snap.UseSyncfusionAgentTools,
            UseSyncfusionAgentOrchestration: snap.UseSyncfusionAgentOrchestration,
            LibraryScanPath: snap.LibraryScanPath,
            LibraryScanIntervalSeconds: snap.LibraryScanIntervalSeconds,
            LibraryScanMaxImports: snap.LibraryScanMaxImports == int.MaxValue ? 0 : snap.LibraryScanMaxImports,
            EmailInboxPath: snap.EmailInboxPath);

    private static string? Hint(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 4)
            return null;
        return "…" + secret[^4..];
    }

    /// <summary>
    /// Paths must resolve under an allowlisted data root (TIKR_DATA_PATH, /data, or local .local-data).
    /// </summary>
    private void ValidateContainedPath(string path, string label)
    {
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException($"{label} path contains invalid characters.");

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"{label} path is not valid.", ex);
        }

        if (full.Contains($"{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || full.EndsWith($"{Path.DirectorySeparatorChar}..", StringComparison.Ordinal))
            throw new ArgumentException($"{label} path must not contain '..' segments.");

        var roots = GetAllowedPathRoots().Select(Path.GetFullPath).ToList();
        if (roots.Count == 0)
            return; // Dev without known roots — still require absolute/parseable path above.

        var allowed = roots.Any(root =>
            full.Equals(root, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase));

        if (!allowed)
            throw new ArgumentException(
                $"{label} path must be under the TIKR data folder (e.g. /data/... on the NAS). Asked IT to bind-mount the share there.");
    }

    private IEnumerable<string> GetAllowedPathRoots()
    {
        var dataPath = configuration["TIKR_DATA_PATH"];
        if (!string.IsNullOrWhiteSpace(dataPath))
            yield return dataPath.Trim();

        if (Directory.Exists("/data"))
            yield return "/data";

        // Local Mac/Windows development under repo .local-data
        var cwd = Directory.GetCurrentDirectory();
        var localData = Path.GetFullPath(Path.Combine(cwd, "..", "..", ".local-data"));
        if (Directory.Exists(localData))
            yield return localData;

        localData = Path.GetFullPath(Path.Combine(cwd, ".local-data"));
        if (Directory.Exists(localData))
            yield return localData;
    }

    private static void MoveDirectoryContents(string sourceRoot, string destRoot)
    {
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(destRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (File.Exists(dest))
                File.Delete(dest);
            File.Move(file, dest);
        }
    }
}
