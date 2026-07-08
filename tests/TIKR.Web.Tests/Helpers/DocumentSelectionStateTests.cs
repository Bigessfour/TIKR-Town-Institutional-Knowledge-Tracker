using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class DocumentSelectionStateTests
{
    private static DocumentDto Doc(Guid id) =>
        new(id, "file.pdf", "application/pdf", 100, null, null, DateTime.UtcNow);

    [Fact]
    public void Toggle_AddsAndRemovesSelection()
    {
        var id = Guid.NewGuid();
        var docs = new List<DocumentDto> { Doc(id) };
        var state = new DocumentSelectionState();

        state.Toggle(docs[0], docs);
        state.Count.Should().Be(1);
        state.SelectedPreview!.Id.Should().Be(id);

        state.Toggle(docs[0], docs);
        state.Count.Should().Be(0);
        state.SelectedPreview.Should().BeNull();
    }

    [Fact]
    public void Clear_ResetsSelection()
    {
        var state = new DocumentSelectionState();
        state.Toggle(Doc(Guid.NewGuid()), [Doc(Guid.NewGuid())]);
        state.Clear();
        state.Count.Should().Be(0);
        state.SelectedPreview.Should().BeNull();
    }

    [Fact]
    public void RemoveIfSelected_DropsPreviewWhenNeeded()
    {
        var id = Guid.NewGuid();
        var docs = new List<DocumentDto> { Doc(id) };
        var state = new DocumentSelectionState();
        state.Toggle(docs[0], docs);

        state.RemoveIfSelected(id);
        state.Contains(id).Should().BeFalse();
        state.SelectedPreview.Should().BeNull();
    }

    [Fact]
    public void VaultVoiceNoteMapper_IdentifiesAndMapsVoiceNotes()
    {
        var voice = new KnowledgeEntryDto(
            Guid.NewGuid(),
            VaultVoiceNoteSimulator.BuildDefaultTitle(new DateTime(2026, 6, 28)),
            "transcription text",
            KnowledgeCategory.TribalKnowledge,
            1);

        VaultVoiceNoteMapper.IsVoiceNote(voice).Should().BeTrue();
        var note = VaultVoiceNoteMapper.ToVoiceNote(voice);
        note.Transcription.Should().Be("transcription text");
    }

    [Fact]
    public void VaultVoiceNoteSimulator_BuildsTitleAndTranscription()
    {
        var when = new DateTime(2026, 6, 28, 14, 30, 0);
        VaultVoiceNoteSimulator.BuildDefaultTitle(when).Should().Contain("Jun 28");
        VaultVoiceNoteSimulator.BuildSimulatedTranscription(when).Should().Contain("county assessor");
    }
}
