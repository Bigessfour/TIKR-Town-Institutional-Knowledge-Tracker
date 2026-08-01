# Research: City Council Agenda and Minutes

**Feature:** `006-council-agenda-minutes` · **Date:** 2026-08-01 · **Updated:** clerk QA confirmed

## R1 — Agenda lead time

| Decision  | Post/build agenda **2 calendar days** before the meeting.                                           |
| --------- | --------------------------------------------------------------------------------------------------- |
| Rationale | Clerk preference: sooner rather than later while still exceeding C.R.S. § 24-6-402 24-hour minimum. |
| Confirmed | 2026-08-01 — Deb/Stephen QA                                                                         |

## R2 — Minutes draft deadline

| Decision  | Draft minutes due **2 calendar days** after the meeting.                      |
| --------- | ----------------------------------------------------------------------------- |
| Rationale | Close the meeting cycle quickly; aligns with “sooner than later” for item #2. |
| Confirmed | 2026-08-01                                                                    |

## R3 — Meeting schedule

| Decision | **2nd Monday** of each month; TOW at Town Hall 6 PM; WSD per posted agenda. |
| -------- | --------------------------------------------------------------------------- |

**Aug–Dec 2026 seed dates:**

| Month | 2nd Monday |
| ----- | ---------- |
| Aug   | 2026-08-10 |
| Sep   | 2026-09-14 |
| Oct   | 2026-10-12 |
| Nov   | 2026-11-09 |
| Dec   | 2026-12-14 |

## R4 — NAS document layout

Unchanged — see [contracts/council-meeting-cycle.md](./contracts/council-meeting-cycle.md).

## R5 — Boards in v1

| Decision  | **TOW + WSD** only. WHA deferred. |
| --------- | --------------------------------- |
| Confirmed | 2026-08-01                        |

## R6 — Seed window

| Decision  | **Aug–Dec 2026** only (30 requirements). Jan–Jul 2026 not seeded. |
| --------- | ----------------------------------------------------------------- |
| Confirmed | 2026-08-01 — roll forward Jan 2027 in P4                          |

## R7 — CML agenda / AI unfinished business (P2)

| Decision       | Procedural scaffold from [CO DLG Parliamentary Procedure](https://dlg.colorado.gov/Parliamentary-Procedure): Call to Order → Approval of Minutes → Public Comment → Reports → Old Business → New Business → Adjourn. |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation | `CouncilAgendaScaffold`, `CouncilAgendaBuilderService`, Requirements agenda builder dialog, sectioned PDF generation.                                                                                                |

## R8 — Document tagging

Add **`Agenda`** to heuristics (implemented P1).

## R9 — Minutes from actioned agenda (US4)

| Decision       | Pre-fill minutes agenda lines from the **linked Post Agenda document** (`RequirementDocuments` on the seeded Post Agenda requirement). Parse `Document.FullTextContent` via `ActionedAgendaLineExtractor` (skip boilerplate headers, numbered markers, town address lines). |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fallback       | When no link, empty text, or extractor yields zero lines → use **DLG scaffold** flattened sections (same order as agenda builder), not raw requirement titles alone.                                                                                                        |
| DOCX structure | `GenerateMeetingMinutesDocxAsync` with `StructuredByAgendaItem: true` → one block per line: **Discussion / Motion / Vote** placeholders (clerk fills after meeting).                                                                                                        |
| Close-out      | Optional **Save to library and link** uploads DOCX, calls `LinkRequirementDocumentAsync` on the seeded **Draft Minutes** requirement; **Mark complete** only runs after a successful link. Suggested filename from `CouncilMinutesFileNaming.SuggestFileName`.              |
| API            | `GET /api/council/minutes-builder/preview?meetingDate=&board=` → `CouncilMinutesBuilderPreview` (draft requirement id, linked agenda filename, `AgendaLines`, `SuggestedFileName`).                                                                                         |
| Confirmed      | 2026-08-01 — code review: minutes must prefer linked agenda text over scaffold; link failure must not toast “linked”.                                                                                                                                                       |

**Prerequisite for best results:** Post Agenda requirement has a linked document with extracted text (`FullTextContent`). Upload + library scan/OCR, or use Documents **Extract to Vault** if PDF/DOCX lacks text. Without text, preview still works but lines come from scaffold (clerk should edit before generate).

## R10 — DLG order of business (reference)

Official Colorado DLG parliamentary procedure — used for both agenda PDF sections and minutes scaffold fallback:

1. Call to Order
2. Approval of Minutes
3. Public Comment
4. Reports
5. Old Business (unfinished business)
6. New Business
7. Adjourn

Source: [CO DLG Parliamentary Procedure](https://dlg.colorado.gov/Parliamentary-Procedure) (see R7).
