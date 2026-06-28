using TIKR.Shared.DTOs;

namespace TIKR.Web.Helpers;

public sealed class DocumentSelectionState
{
    private readonly HashSet<Guid> _selectedIds = new();

    public DocumentDto? SelectedPreview { get; private set; }

    public int Count => _selectedIds.Count;

    public IReadOnlyCollection<Guid> SelectedIds => _selectedIds;

    public void Toggle(DocumentDto doc, IReadOnlyList<DocumentDto> documents)
    {
        if (_selectedIds.Contains(doc.Id))
            _selectedIds.Remove(doc.Id);
        else
            _selectedIds.Add(doc.Id);

        SelectedPreview = documents.FirstOrDefault(d => _selectedIds.Contains(d.Id));
    }

    public void Clear()
    {
        _selectedIds.Clear();
        SelectedPreview = null;
    }

    public void RemoveIfSelected(Guid id)
    {
        _selectedIds.Remove(id);
        if (SelectedPreview?.Id == id)
            SelectedPreview = null;
    }

    public bool Contains(Guid id) => _selectedIds.Contains(id);
}

public static class VaultVoiceNoteSimulator
{
    public static string BuildDefaultTitle(DateTime timestamp) =>
        $"Voice Note - {timestamp:MMM dd}";

    public static string BuildSimulatedTranscription(DateTime timestamp) =>
        $"[Voice Note - {timestamp:HH:mm}] " +
        "Key thing I always forget: call the county assessor before Dec 15 for mill levy. Also, the safe combination is in the red folder under 'E'. The new clerk should know the trustees meet on the second Tuesday.";
}
