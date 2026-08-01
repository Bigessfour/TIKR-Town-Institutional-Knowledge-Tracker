# Feature Specification: City Council Agenda and Minutes

**Feature Branch**: `006-council-agenda-minutes`
**Created**: 2026-08-01
**Status**: Planned — implementation in progress
**Input**: Council meeting cycle for Wiley, CO — 2nd Monday monthly, agenda lead time, minutes close-out, AI unfinished business, NAS corpus reference.

**Related:** [004-clerk-command-dashboard](../004-clerk-command-dashboard/spec.md) · NAS path `COUNCIL MEETINGS/` on Town of Wiley Shared Documents

## North star

Deb plans and closes each **Town Council regular meeting** (2nd Monday, 6:00 PM, Town Hall) from one place: Requirements show meeting + agenda + minutes deadlines; Documents hold OCR’d agendas/minutes on the NAS; AI proposes **unfinished business** from prior minutes; TIKR generates CML-style agendas and minutes linked to the actioned agenda.

## User scenarios

### US1 — Meeting cycle on the calendar (P1)

**As** the town clerk, **I want** every 2026 Town Council meeting and its prep/close-out deadlines in Requirements and Calendar **so** I never miss agenda posting or minutes drafting.

**Independent test:** Fresh DB seed → Calendar shows 12 meeting dates in 2026 + linked prep deadlines; Requirements grid filterable by “Council meeting cycle”.

**Acceptance:**

1. **Given** a new TIKR install, **When** the app starts, **Then** Requirements include **Aug–Dec 2026** 2nd-Monday meetings for **Town Council and WSD** (5 dates × 2 boards × 3 tasks = 30 rows).
2. **Given** meeting date *M*, **When** viewing Requirements, **Then** agenda due date is *M − 2 days* and minutes due date is *M + 2 days*.
3. **Given** a seeded meeting requirement, **When** opening Calendar, **Then** the meeting appears on the correct date.

---

### US2 — NAS agendas and minutes for AI reference (P1)

**As** the clerk, **I want** council agendas and minutes under the existing NAS folder ingested, OCR’d, and embedded **so** the Assistant and future agenda builder can cite prior meetings.

**Independent test:** File exists under `COUNCIL MEETINGS/2026 Minutes-TOW/` → library scan → document in TIKR with `SuggestedFolder` Minutes or Agenda → semantic search returns it.

**Acceptance:**

1. **Given** `TIKR_LIBRARY_SCAN_PATH` points at the town share, **When** scan runs, **Then** council DOCX/PDF under `COUNCIL MEETINGS/` import without moving source files.
2. **Given** a filename containing “agenda”, **When** tagged, **Then** `SuggestedFolder` is `Agenda` (heuristic or AI).
3. **Given** embedded minutes for the prior meeting, **When** clerk asks Assistant about unfinished business, **Then** response cites source document text.

---

### US3 — CML agenda builder (P2)

**As** the clerk, **I want** to generate a Colorado Municipal League–style agenda PDF with procedural sections plus substantive items **so** posted agendas match council practice.

**Independent test:** Requirements → Agenda PDF for a chosen meeting date → PDF includes Call to Order, Approval of Minutes, Public Comment, Unfinished Business, New Business, Adjournment.

**Acceptance:**

1. **Given** a meeting date and selected requirements, **When** generating agenda PDF, **Then** output uses CML section scaffold (not a flat numbered requirement list).
2. **Given** prior minutes in Documents, **When** opening agenda builder, **Then** AI suggests unfinished-business lines with quoted source snippets; clerk accepts/rejects each.

---

### US4 — Minutes from actioned agenda (P2)

**As** the clerk, **I want** minutes pre-filled from the posted agenda lines **so** I close the meeting cycle by drafting minutes against what council actually heard.

**Independent test:** Link actioned agenda doc → Generate minutes → DOCX has one section per agenda line + motions placeholder.

**Acceptance:**

1. **Given** an actioned agenda document linked to the meeting requirement, **When** generating minutes, **Then** agenda items pre-populate the minutes template.
2. **Given** completed minutes saved to NAS naming convention, **When** marking “Draft minutes” requirement complete, **Then** document link is attached.

---

### US5 — Public posting handoff (P3)

**As** the clerk, **I want** a checklist item to post agenda/minutes to townofwiley.gov **so** public notice stays aligned with OML (site currently empty; NAS is source of truth until posted).

## Functional requirements

| ID     | Requirement                                                                                                                     |
| ------ | ------------------------------------------------------------------------------------------------------------------------------- |
| FR-001 | System MUST seed **Town Council (TOW)** and **WSD** regular meetings on the **2nd Monday** for **Aug–Dec 2026**.                |
| FR-002 | For each meeting, system MUST create companion requirements: **Post agenda** (due *M − 2*) and **Draft minutes** (due *M + 2*). |
| FR-003 | Seeding MUST be **idempotent** (re-start does not duplicate rows).                                                              |
| FR-004 | Meeting requirements MUST appear on the **Deadline Calendar** via existing Requirement → Calendar mapping.                      |
| FR-005 | Document heuristics MUST recognize **Agenda** as a `SuggestedFolder` distinct from **Minutes**.                                 |
| FR-006 | Agenda generation MUST evolve to **CML procedural scaffold** + substantive items (P2).                                          |
| FR-007 | AI MUST suggest **unfinished business** from prior approved minutes with provenance (P2).                                       |
| FR-008 | Minutes generation MUST prefer **actioned agenda lines** over generic open requirements (P2).                                   |
| FR-009 | Generated/saved files SHOULD follow NAS path: `COUNCIL MEETINGS/{YEAR} Minutes-TOW/Agenda's/` and `{YEAR} Minutes-TOW/*.docx`.  |

## Key entities (target)

| Entity                            | v1 (Requirements only)                                                    | v2 (planned)                                                                                  |
| --------------------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| **CouncilMeeting**                | Encoded as grouped Requirements sharing meeting date in title/description | First-class entity: `MeetingDate`, `Board`, `AgendaDocumentId`, `MinutesDocumentId`, `Status` |
| **CouncilMeetingRequirementKind** | Enum in seeder: `Meeting`, `PostAgenda`, `DraftMinutes`                   | FK on Requirement or CouncilMeeting                                                           |
| **Document**                      | Existing; linked via `RequirementDocument`                                | Same + folder `Agenda` / `Minutes`                                                            |

## Success criteria

| ID     | Criterion                                                                                                 |
| ------ | --------------------------------------------------------------------------------------------------------- |
| SC-001 | Clerk sees **30** seeded cycle rows (5 meetings × 2 boards × 3 tasks) after first boot.                   |
| SC-002 | All 5 meeting dates match computed 2nd Mondays for Aug–Dec 2026.                                          |
| SC-003 | At least one council agenda and one minutes file from NAS are searchable in Assistant after library scan. |
| SC-004 | Agenda PDF for a test meeting includes ≥8 CML procedural sections (P2 gate).                              |
| SC-005 | Unfinished-business suggestions include document id + quote when prior minutes exist (P2 gate).           |

## Assumptions (confirmed 2026-08-01)

| Topic                    | Value                           |
| ------------------------ | ------------------------------- |
| Agenda lead time         | **2 days** before meeting       |
| Minutes due              | **2 days** after meeting        |
| Board scope v1           | **Town Council (TOW) + WSD**    |
| Seed range               | **Aug–Dec 2026** only           |
| Meeting time/place (TOW) | 6:00 PM, Town Hall, 304 Main St |
| WHA                      | Out of scope v1                 |

## Edge cases

- **Holiday conflicts:** 2nd Monday still meets unless clerk manually moves requirements (no auto-holiday skip in v1).
- **Special meetings:** Out of scope for v1 seed; clerk adds ad-hoc Requirements.
- **Re-seed:** Idempotent marker in description prevents duplicate 2026 rows.
- **Past 2026 meetings:** Seeded for audit/planning; clerk marks completed as work is done.
