# Data Model: City Council Agenda and Minutes

**Feature:** `006-council-agenda-minutes`

## v1 — Requirements-only cycle (shipping first)

No new tables. Three seeded `Requirement` rows per meeting month **per board** (TOW + WSD).

### Seed scope

- **Months:** August–December 2026 (5 meetings)
- **Boards:** TOW, WSD
- **Total rows:** 30
- **Marker:** `Council meeting cycle 2026 (Aug-Dec)`

### Requirement fields used

| Field            | Meeting                                                               | Post agenda                               | Draft minutes                               |
| ---------------- | --------------------------------------------------------------------- | ----------------------------------------- | ------------------------------------------- |
| `Title`          | `{Board} Regular Meeting — {MMM d, yyyy}`                             | `Post {Board} Agenda — …`                 | `Draft {Board} Minutes — …`                 |
| `DueDate`        | 2nd Monday                                                            | Meeting − **2** days                      | Meeting + **2** days                        |
| `Recurrence`     | `None`                                                                | `None`                                    | `None`                                      |
| `Category`       | `Compliance`                                                          | `Compliance`                              | `Compliance`                                |
| `IsSystemSeeded` | `true`                                                                | `true`                                    | `true`                                      |
| `Description`    | Contains marker `Council meeting cycle 2026; kind=Meeting; board=TOW` | `… kind=PostAgenda; meeting={yyyy-MM-dd}` | `… kind=DraftMinutes; meeting={yyyy-MM-dd}` |
| `SubmitTo`       | `Board of Trustees`                                                   | `Public notice / town website`            | `Town records`                              |

### Idempotency

Seeder exits early if any requirement description contains:

```text
Council meeting cycle 2026 (Aug-Dec)
```

### Calendar projection

Existing Calendar page maps open Requirements to events by `DueDate` — no schema change.

---

## v2 — CouncilMeeting entity (planned)

```text
CouncilMeeting
├── Id (Guid, PK)
├── Board (enum: TOW, WSD, WHA)
├── MeetingDate (DateOnly)
├── MeetingTime (TimeOnly?) default 18:00
├── Location (string) default Town Hall address
├── Status (enum: Planned, AgendaPosted, Held, MinutesDraft, MinutesApproved)
├── AgendaDocumentId (Guid?, FK Document)
├── MinutesDocumentId (Guid?, FK Document)
├── PriorMeetingId (Guid?, self-FK)
├── CreatedAt / UpdatedAt
```

Optional link table `CouncilMeetingRequirement` if Requirements stay separate.

---

## Document / NAS conventions

| Kind    | NAS relative path                                                        | SuggestedFolder |
| ------- | ------------------------------------------------------------------------ | --------------- |
| Agenda  | `COUNCIL MEETINGS/{YEAR} Minutes-TOW/Agenda's/{n} {MON} {d} {YEAR}.docx` | `Agenda`        |
| Minutes | `COUNCIL MEETINGS/{YEAR} Minutes-TOW/{n} {MON} {d} {YEAR} TOW.docx`      | `Minutes`       |

`LibraryImportRecord.RelativePath` tracks scan fingerprint per source file.

---

## DTO extensions (P2)

```csharp
public record CouncilAgendaSection(
    string SectionKey,      // e.g. "unfinished_business"
    string Title,
    string? Body,
    string Source,          // template | requirement | prior-minutes | clerk
    Guid? LinkedRequirementId,
    Guid? LinkedDocumentId);

public record CouncilAgendaRequest(
    string TownName,
    DateOnly MeetingDate,
    IReadOnlyList<CouncilAgendaSection> Sections);

public record UnfinishedBusinessSuggestion(
    string Title,
    string Rationale,
    Guid SourceDocumentId,
    string SourceQuote);
```

---

## Configuration keys (v2)

| Key                             | Default | Purpose                             |
| ------------------------------- | ------- | ----------------------------------- |
| `TIKR_COUNCIL_AGENDA_LEAD_DAYS` | 2       | Days before meeting to post agenda  |
| `TIKR_COUNCIL_MINUTES_LAG_DAYS` | 2       | Days after meeting to draft minutes |
| `TIKR_COUNCIL_MEETING_BOARD`    | TOW     | Board filter for seeder             |
