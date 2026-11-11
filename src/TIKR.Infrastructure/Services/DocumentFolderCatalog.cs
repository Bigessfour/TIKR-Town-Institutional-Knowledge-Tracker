using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Services;

public sealed record FolderDirectory(IReadOnlyList<string> Allowed, IReadOnlyList<string> Declined)
{
    public static FolderDirectory BuiltIn { get; } = new(DocumentTagHeuristics.FolderVocabulary, []);

    public string? MatchAllowed(string? name) => FolderNamePolicy.Match(name, Allowed);

    public bool IsDeclined(string? name) => FolderNamePolicy.Match(name, Declined) is not null;
}

public interface IDocumentFolderCatalog
{
    Task<FolderDirectory> GetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderProposalGroupDto>> ListProposalsAsync(CancellationToken cancellationToken = default);

    Task<FolderProposalDecisionResponse> AcceptAsync(string name, CancellationToken cancellationToken = default);

    Task<FolderProposalDecisionResponse> DeclineAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// The nine built-in folders plus names the clerk has accepted. Declined names are not asked again.
/// </summary>
public sealed class DocumentFolderCatalog(TikrDbContext db) : IDocumentFolderCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<FolderDirectory> GetAsync(CancellationToken cancellationToken = default)
    {
        var extras = await ReadListAsync(DocumentFolderSettingKeys.Allowed, cancellationToken);
        var declined = await ReadListAsync(DocumentFolderSettingKeys.Declined, cancellationToken);

        var allowed = new List<string>(DocumentTagHeuristics.FolderVocabulary);
        foreach (var extra in extras)
        {
            var canonical = FolderNamePolicy.CanonicalOrNull(extra);
            if (canonical is null || canonical == DocumentTagHeuristics.General)
                continue;
            if (FolderNamePolicy.Match(canonical, allowed) is not null)
                continue;
            if (DocumentTagHeuristics.FolderVocabulary.Contains(canonical))
                continue;
            allowed.Add(canonical);
        }

        var declinedNames = declined
            .Select(FolderNamePolicy.CanonicalOrNull)
            .Where(name => name is not null && name != DocumentTagHeuristics.General)
            .Cast<string>()
            .Where(name => FolderNamePolicy.Match(name, DocumentTagHeuristics.FolderVocabulary) is null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FolderDirectory(allowed, declinedNames);
    }

    public async Task<IReadOnlyList<FolderProposalGroupDto>> ListProposalsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Documents
            .AsNoTracking()
            .Where(d => d.DeletedAt == null && d.ProposedFolder != null && d.ProposedFolder != "")
            .Select(d => d.ProposedFolder!)
            .ToListAsync(cancellationToken);

        return rows
            .Select(FolderNamePolicy.CanonicalOrNull)
            .Where(name => name is not null)
            .Cast<string>()
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FolderProposalGroupDto(group.First(), group.Count()))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<FolderProposalDecisionResponse> AcceptAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var canonical = FolderNamePolicy.CanonicalOrNull(name)
            ?? throw new ArgumentException("Folder name is empty or uses path characters.");
        if (canonical == DocumentTagHeuristics.General)
            throw new ArgumentException("General is already a folder.");

        var directory = await GetAsync(cancellationToken);
        var storedName = directory.MatchAllowed(canonical) ?? canonical;

        if (directory.MatchAllowed(storedName) is null)
        {
            var extras = await ReadListAsync(DocumentFolderSettingKeys.Allowed, cancellationToken);
            extras.Add(storedName);
            await WriteListAsync(DocumentFolderSettingKeys.Allowed, extras, cancellationToken);
        }

        var declined = await ReadListAsync(DocumentFolderSettingKeys.Declined, cancellationToken);
        var kept = declined.Where(item => !item.Equals(storedName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (kept.Count != declined.Count)
            await WriteListAsync(DocumentFolderSettingKeys.Declined, kept, cancellationToken);

        var updated = await ApplyDecisionAsync(storedName, accept: true, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new FolderProposalDecisionResponse(storedName, updated);
    }

    public async Task<FolderProposalDecisionResponse> DeclineAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var canonical = FolderNamePolicy.CanonicalOrNull(name)
            ?? throw new ArgumentException("Folder name is empty or uses path characters.");
        if (FolderNamePolicy.Match(canonical, DocumentTagHeuristics.FolderVocabulary) is not null)
            throw new ArgumentException("Built-in folders stay in the list.");

        var declined = await ReadListAsync(DocumentFolderSettingKeys.Declined, cancellationToken);
        if (FolderNamePolicy.Match(canonical, declined) is null)
        {
            declined.Add(canonical);
            await WriteListAsync(DocumentFolderSettingKeys.Declined, declined, cancellationToken);
        }

        var extras = await ReadListAsync(DocumentFolderSettingKeys.Allowed, cancellationToken);
        var kept = extras.Where(item => !item.Equals(canonical, StringComparison.OrdinalIgnoreCase)).ToList();
        if (kept.Count != extras.Count)
            await WriteListAsync(DocumentFolderSettingKeys.Allowed, kept, cancellationToken);

        var updated = await ApplyDecisionAsync(canonical, accept: false, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new FolderProposalDecisionResponse(canonical, updated);
    }

    private async Task<int> ApplyDecisionAsync(string name, bool accept, CancellationToken cancellationToken)
    {
        var pending = await db.Documents
            .Where(d => d.DeletedAt == null && d.ProposedFolder != null && d.ProposedFolder != "")
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var document in pending)
        {
            if (!string.Equals(document.ProposedFolder, name, StringComparison.OrdinalIgnoreCase))
                continue;

            document.ProposedFolder = null;
            if (accept)
                document.SuggestedFolder = name;
            else if (string.IsNullOrWhiteSpace(document.SuggestedFolder)
                     || string.Equals(document.SuggestedFolder, name, StringComparison.OrdinalIgnoreCase))
                document.SuggestedFolder = DocumentTagHeuristics.General;

            document.UpdatedAt = DateTime.UtcNow;
            updated++;
        }

        return updated;
    }

    private async Task<List<string>> ReadListAsync(string key, CancellationToken cancellationToken)
    {
        var row = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (string.IsNullOrWhiteSpace(row?.Value))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(row.Value, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task WriteListAsync(string key, List<string> names, CancellationToken cancellationToken)
    {
        var distinct = names
            .Select(FolderNamePolicy.CanonicalOrNull)
            .Where(name => name is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var json = JsonSerializer.Serialize(distinct);
        if (json.Length > 4000)
            throw new ArgumentException("The folder list is too long to store.");

        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = json,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
