# Contract: Council Agenda Builder API

**Feature:** `006-council-agenda-minutes` · **P2**

## GET `/api/council/agenda-builder/preview`

**Query**

| Param         | Type           | Default |
| ------------- | -------------- | ------- |
| `meetingDate` | `yyyy-MM-dd`   | today   |
| `board`       | `TOW` \| `WSD` | `TOW`   |

**Response:** `CouncilAgendaBuilderPreview`

- Sections follow [CO DLG Order of Business](https://dlg.colorado.gov/Parliamentary-Procedure)
- `new_business` populated from open Requirements (excluding council cycle marker)
- `old_business` populated when prior minutes yield unfinished items

## POST `/api/council/agenda-builder/unfinished-business`

**Body:** `UnfinishedBusinessRequest { meetingDate, board }`

**Response:** `UnfinishedBusinessSuggestion[]`

| Field              | Purpose                     |
| ------------------ | --------------------------- |
| `title`            | Proposed agenda line        |
| `sourceDocumentId` | Minutes doc in TIKR         |
| `sourceQuote`      | Provenance for clerk review |

**Logic:** Prior meeting = previous 2nd Monday → match minutes doc by filename → keyword extract (`tabled`, `continued`, `postponed`, …) → fallback semantic search on `Folder=Minutes`.

## POST `/api/documents/generate/council-agenda` (extended)

**Body:** `CouncilAgendaRequest`

| Field         | Purpose                                                                   |
| ------------- | ------------------------------------------------------------------------- |
| `townName`    | Wiley                                                                     |
| `meetingDate` | 2nd Monday                                                                |
| `board`       | `TOW` / `WSD`                                                             |
| `sections`    | Optional — when omitted, server builds via `ICouncilAgendaBuilderService` |
| `items`       | Legacy flat list (used when sections empty for new business fallback)     |

**Response:** PDF file (`council-agenda-{tow|wsd}-{date}.pdf`)
