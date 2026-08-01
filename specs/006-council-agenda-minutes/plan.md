# Implementation Plan: City Council Agenda and Minutes

**Branch**: `006-council-agenda-minutes` | **Date**: 2026-08-01 | **Spec**: [spec.md](./spec.md)

## Summary

Seed **Aug–Dec 2026** meeting-cycle Requirements for **Town Council (TOW)** and **WSD** on each 2nd Monday: meeting date, **post agenda 2 days before**, **draft minutes 2 days after**. Extend document heuristics for `Agenda` folder. P2: CML agenda PDF, AI unfinished business from prior minutes, minutes from actioned agenda.

## Technical Context

**Language/Version**: .NET 10
**Primary Dependencies**: EF Core, Syncfusion Document SDK, Ollama (`HybridAiService`), existing `LibraryScanService`
**Storage**: SQLite/Postgres Requirements; NAS `COUNCIL MEETINGS/` via library scan
**Testing**: xUnit + FluentAssertions; in-memory DB for seeder
**Target Platform**: Synology NAS (Wiley)
**Constraints**: Local-first; idempotent seed; no relocation of NAS folders

## Clerk policy (confirmed 2026-08-01)

| Setting       | Value                                                                         |
| ------------- | ----------------------------------------------------------------------------- |
| Agenda lead   | **2 days** before meeting                                                     |
| Minutes draft | **2 days** after meeting                                                      |
| Boards        | **TOW + WSD**                                                                 |
| Seed window   | **Aug–Dec 2026** only (5 meetings × 2 boards × 3 tasks = **30** requirements) |

## Constitution Check

| Gate                       | Status                                          |
| -------------------------- | ----------------------------------------------- |
| I. Local-first             | Pass                                            |
| II. Minimal proven changes | Pass — seeder + heuristics; no new tables in P1 |
| III. Test + CI gates       | Pass — seeder + schedule tests                  |
| IV. Layer boundaries       | Pass                                            |
| V. RAG-aware               | Pass — builds on library scan + embeddings      |

## Project Structure

```text
specs/006-council-agenda-minutes/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/council-meeting-cycle.md
└── tasks.md                    # /speckit-tasks next

src/TIKR.Shared/Helpers/CouncilMeetingSchedule.cs
src/TIKR.Infrastructure/CouncilMeetingSeeder.cs
src/TIKR.Infrastructure/Services/DocumentTagHeuristics.cs
tests/TIKR.Shared.Tests/Helpers/CouncilMeetingScheduleTests.cs
tests/TIKR.Infrastructure.Tests/CouncilMeetingSeederTests.cs
```

## Phases

### P1 — Meeting cycle seed (shipping)

- [x] `CouncilMeetingSchedule.SecondMonday`
- [x] `CouncilMeetingSeeder` Aug–Dec 2026 TOW + WSD
- [x] Wire in `InitializeDatabaseAsync`
- [x] `Agenda` folder heuristic
- [x] Unit tests

### P2 — Agenda builder (shipping)

- [x] DOLG order-of-business scaffold (`CouncilAgendaScaffold`)
- [x] `CouncilAgendaBuilderService` + unfinished business extraction
- [x] Sectioned PDF in `GenerateCouncilAgendaPdfAsync`
- [x] API: preview + unfinished-business + extended council-agenda
- [x] Requirements **Council agenda builder** dialog
- [x] Tests (Shared, Infrastructure, Api)

### P3 — Minutes close-out

- Minutes dialog loads actioned agenda lines
- Save to NAS naming convention; link `Draft Minutes` requirement

### P4 — Ops

- Roll 2027 seed (January cron or manual)
- Optional `TIKR_COUNCIL_*` config keys

## Complexity Tracking

None — P1 uses existing `Requirement` entity only.
