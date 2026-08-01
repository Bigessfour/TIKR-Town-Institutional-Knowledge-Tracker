# Contract: Council Meeting Cycle (Requirements seed)

**Feature:** `006-council-agenda-minutes`
**Type:** Database seed on startup (no new HTTP endpoints in P1)

## Trigger

`InitializeDatabaseAsync` → `CouncilMeetingSeeder.SeedAsync(db)` after `DbSeeder.SeedAsync`.

## Idempotency

Skip entire seed if any `Requirement.Description` contains:

```text
Council meeting cycle 2026 (Aug-Dec)
```

## Seed parameters

| Parameter        | Value                  |
| ---------------- | ---------------------- |
| Year             | 2026                   |
| Months           | 8–12 (August–December) |
| Meeting rule     | 2nd Monday of month    |
| Boards           | `TOW`, `WSD`           |
| Agenda lead days | 2 (due = meeting − 2)  |
| Minutes lag days | 2 (due = meeting + 2)  |

## Requirement shapes

### Town Council (TOW)

| Kind         | Title pattern                                   | DueDate     |
| ------------ | ----------------------------------------------- | ----------- |
| Meeting      | `Town Council Regular Meeting — {MMMM d, yyyy}` | 2nd Monday  |
| PostAgenda   | `Post Town Council Agenda — {MMMM d, yyyy}`     | meeting − 2 |
| DraftMinutes | `Draft Town Council Minutes — {MMMM d, yyyy}`   | meeting + 2 |

### WSD

| Kind         | Title pattern                          | DueDate     |
| ------------ | -------------------------------------- | ----------- |
| Meeting      | `WSD Regular Meeting — {MMMM d, yyyy}` | 2nd Monday  |
| PostAgenda   | `Post WSD Agenda — {MMMM d, yyyy}`     | meeting − 2 |
| DraftMinutes | `Draft WSD Minutes — {MMMM d, yyyy}`   | meeting + 2 |

## Description marker (machine-readable)

```text
Council meeting cycle 2026 (Aug-Dec); kind={Meeting|PostAgenda|DraftMinutes}; board={TOW|WSD}; meeting={yyyy-MM-dd}. …
```

## Calendar projection

Existing Calendar reads `Requirement.DueDate` — no API change.

## NAS document paths (reference only)

| Board | Agendas                                        | Minutes                             |
| ----- | ---------------------------------------------- | ----------------------------------- |
| TOW   | `COUNCIL MEETINGS/{Y} Minutes-TOW/Agenda's/`   | `COUNCIL MEETINGS/{Y} Minutes-TOW/` |
| WSD   | `COUNCIL MEETINGS/{Y} WSD MINUTES/WSD AGENDA/` | `COUNCIL MEETINGS/{Y} WSD MINUTES/` |

Ingest via `TIKR_LIBRARY_SCAN_PATH` + `LibraryScanService`.
