# Quickstart: Council Meeting Cycle (006)

**Feature:** `006-council-agenda-minutes` · **Branch:** `006-council-agenda-minutes`

## Prerequisites

- TIKR API + Web running (local or NAS)
- Syncfusion Document SDK licensed (`SYNCFUSION_LICENSE_KEY` in `docker/.env` or user-secrets)
- Fresh or existing DB (seeder is idempotent)

## Automated smoke

```bash
cd /path/to/TIKR-Town-Institutional-Knowledge-Tracker
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~CouncilMeeting|CouncilAgenda|ActionedAgenda"
```

Expected: all council-cycle tests green (seeder, agenda builder, minutes preview, extractors).

---

## P1 validation — seeded requirements

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~CouncilMeeting"
```

### Manual check (local)

1. Start stack: `dotnet run --project src/TIKR.Api` and `dotnet run --project src/TIKR.Web` (or `docker compose -f docker/docker-compose.yml up --build`)
2. Open **Requirements** or **Calendar**
3. Confirm **30** rows with description marker `Council meeting cycle 2026 (Aug-Dec)`:
   - Meetings: Aug 10, Sep 14, Oct 12, Nov 9, Dec 14 (2026)
   - Each date × **TOW + WSD** × (meeting, post agenda, draft minutes)
4. **Post agenda** due = meeting − **2 calendar days**
5. **Draft minutes** due = meeting + **2 calendar days**

### NAS deploy (after merge)

Release **v1.0.1** includes Feature 006. On NAS (Tailscale SSH):

```bash
# From Mac (Mr_Storage repo):
./scripts/deploy-tikr-nas.sh

# Or manually on NAS:
ssh mr-storage 'cd /volume1/tikr/app/docker && \
  sudo sed -i "s/^TIKR_VERSION=.*/TIKR_VERSION=1.0.1/" .env && \
  sudo docker compose -f docker-compose.prod.yml --env-file .env pull tikr-api tikr-web && \
  sudo docker compose -f docker-compose.prod.yml --env-file .env up -d tikr-api tikr-web'
```

Verify council endpoints after restart:

```bash
curl -sS http://mr-storage:5050/api/council/agenda-builder/preview?meetingDate=2026-08-10&board=TOW
curl -sS http://mr-storage:5050/api/council/minutes-builder/preview?meetingDate=2026-08-10&board=TOW
```

---

## P1 validation — document heuristics

Upload or library-scan a file named `7 JULY 13 2026.docx` with “agenda” in text → `SuggestedFolder` should be **Agenda**.

---

## P2 validation — agenda builder (US3)

### UI

1. **Requirements** → **Download agenda PDF** (opens builder dialog)
2. Pick **2026-08-10**, board **TOW** → preview shows **7 DLG sections**
3. **Suggest unfinished business** → items appear under Old Business when prior minutes are embedded in Documents
4. **Download agenda PDF** → PDF has numbered sections through Adjourn

### API — agenda builder?meetingDate=2026-08-10&board=TOW" | python3 -m json.tool

curl -sS -X POST "http://localhost:5050/api/council/agenda-builder/unfinished-business" \
  -H "Content-Type: application/json" \
  -d '{"meetingDate":"2026-08-10","board":"TOW"}' | python3 -m json.tool
```

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~CouncilAgenda"
```

---

## P3 validation — minutes close-out (US4)

Minutes pre-fill from the **actioned agenda** linked on the seeded **Post … Agenda** requirement for the same meeting date and board.

### Setup (once per meeting)

1. Find **Post Town Council Agenda — August 10, 2026** (or WSD equivalent) in Requirements
2. Link or upload the **posted/actioned agenda** document to that requirement
3. Ensure the document has **extracted text** (`FullTextContent`):
   - Library scan/OCR from NAS, or
   - Documents → select doc → **Extract to Vault** if upload lacked text

Without extracted text, the minutes dialog falls back to DLG scaffold lines (edit before generating).

### UI — minutes builder

1. **Requirements** → **Meeting minutes**
2. Set meeting date **2026-08-10**, board **TOW**
3. Confirm preview shows **Actioned agenda:** filename and **Close-out requirement:** Draft Town Council Minutes…
4. **Load from actioned agenda** refreshes agenda lines from linked doc text
5. Edit **Attendees**, **Agenda items** (one line per item), **Notes** as needed
6. **Generate minutes DOCX** → download with Discussion / Motion / Vote blocks per line
7. Optional: check **Save to document library and link to Draft Minutes requirement**
   - If no Draft Minutes row exists for date/board, save is disabled (download-only)
8. Optional: check **Mark Draft Minutes requirement complete** (only after successful link)

### API — minutes preview?meetingDate=2026-08-10&board=TOW" | python3 -m json.tool
```

Response fields to verify:

| Field                       | Expect                                                     |
| --------------------------- | ---------------------------------------------------------- |
| `draftMinutesRequirementId` | Guid for seeded Draft Minutes row                          |
| `actionedAgendaFileName`    | Linked doc name when Post Agenda has a link                |
| `agendaLines`               | Non-empty; from linked text when `FullTextContent` present |
| `suggestedFileName`         | e.g. `2026-08-10 TOW Minutes.docx` pattern                 |

Generate DOCX (licensed):

```bash
curl -sS -X POST "http://localhost:5050/api/documents/generate/meeting-minutes" \
  -H "Content-Type: application/json" \
  -d '{
    "townName": "Town of Wiley",
    "meetingDate": "2026-08-10",
    "boardName": "Board of Trustees",
    "agendaItems": ["Call to order", "Budget hearing"],
    "structuredByAgendaItem": true
  }' --output /tmp/minutes.docx
```

### Tests

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~ActionedAgenda|GetMinutesPreview|StructuredByAgendaItem|BuildMinutesPreview"
```

---

## End-to-end clerk workflow (Aug 2026 example)

```text
Post Agenda due (Aug 8)
  → Build agenda PDF (US3) → post to council → link actioned agenda on Post Agenda requirement

Meeting (Aug 10)

Draft Minutes due (Aug 12)
  → Meeting minutes dialog (US4) → generate DOCX → save + link + mark complete
```

---

## vNext — US5 public posting (P3)

Not implemented in 006. After NAS validation, add a manual checklist requirement or future automation to post agenda/minutes to townofwiley.gov (OML alignment; NAS remains source of truth until posted).

See [spec.md](./spec.md) US5.
