# Tasks: City Council Agenda and Minutes

**Feature:** `006-council-agenda-minutes` · **Branch:** `feature/council-agenda-minutes`
**Input:** P2 agenda builder — [CO DLG Parliamentary Procedure](https://dlg.colorado.gov/Parliamentary-Procedure) order of business + AI unfinished business.

## Format

`- [ ] T### [P?] [USn] Description with file path`

---

## Phase 1: Setup (P1 complete)

- [x] T001 `CouncilMeetingSchedule.SecondMonday` in `src/TIKR.Shared/Helpers/CouncilMeetingSchedule.cs`
- [x] T002 `CouncilMeetingSeeder` Aug–Dec 2026 TOW+WSD in `src/TIKR.Infrastructure/CouncilMeetingSeeder.cs`
- [x] T003 Wire seeder in `src/TIKR.Infrastructure/DependencyInjection.cs`
- [x] T004 `Agenda` folder heuristic in `src/TIKR.Infrastructure/Services/DocumentTagHeuristics.cs`

---

## Phase 2: Foundational (P2 shared)

- [x] T005 [P] Add DTOs in `src/TIKR.Shared/DTOs/CouncilAgendaDto.cs`
- [x] T006 [P] Add `CouncilAgendaScaffold` + `PreviousSecondMonday`
- [x] T007 Add `ICouncilAgendaBuilderService`
- [x] T008 Implement `CouncilAgendaBuilderService`
- [x] T009 Register service in `DependencyInjection.cs`
- [x] T010 [P] Tests in `CouncilAgendaScaffoldTests.cs`
- [x] T011 Tests in `CouncilAgendaBuilderServiceTests.cs`

---

## Phase 3: US3 — DOLG agenda builder (P2)

- [x] T012 [US3] Extend `CouncilAgendaRequest` in `DocumentGenerationDto.cs`
- [x] T013 [US3] Sectioned PDF in `SyncfusionDocumentGenerationService.cs`
- [x] T014 [US3] Update council-agenda endpoint in `Program.cs`
- [x] T015 [US3] `GET /api/council/agenda-builder/preview`
- [x] T016 [US3] `POST /api/council/agenda-builder/unfinished-business`
- [x] T017 [US3] API client in `TikrApiClient.cs`
- [x] T018 [US3] Agenda builder dialog in `Requirements.razor`
- [x] T019 [US3] Licensed PDF test (existing)
- [x] T020 [US3] `CouncilAgendaBuilderEndpointTests.cs`

---

## Phase 4: US4 — Minutes close-out (P3)

- [x] T021 [US4] Minutes dialog loads actioned agenda lines in `src/TIKR.Web/Components/Pages/Requirements.razor`
- [x] T022 [US4] Link draft-minutes requirement to saved doc via `TikrApiClient.LinkRequirementDocumentAsync`

---

## Phase 5: Polish

- [x] T023 Document DOLG reference + minutes-builder research in `specs/006-council-agenda-minutes/research.md` (R9–R10)
- [x] T024 Update `specs/006-council-agenda-minutes/quickstart.md` with agenda + minutes builder validation steps
- [x] T025 Run `dotnet test TIKR.sln --configuration Release` and curate function inventory

---

## Dependencies

```text
P1 (done) → Phase 2 (T005–T011) → Phase 3 US3 (T012–T020) → Phase 4 US4 → Polish
```

## Parallel opportunities

- T005 + T006 + T010 after T007 interface stub
- T012 + T015 + T016 after T008

## MVP for P2 ship

**T005–T020** (US3 only). US4 remains P3.

## Task counts

| Phase              | Tasks  | Status      |
| ------------------ | ------ | ----------- |
| P1 Setup           | 4      | Done        |
| P2 Foundational    | 7      | Done        |
| US3 Agenda builder | 9      | Done        |
| US4 Minutes        | 2      | Done        |
| Polish             | 3      | Done        |
| **Total**          | **25** | **25 done** |
