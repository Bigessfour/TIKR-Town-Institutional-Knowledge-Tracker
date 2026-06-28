# TIKR Demo — Clerk Script & Cheat Sheet

**Presenter:** Deb (or stand-in town clerk)  
**Audience:** Council, CML peers, municipal IT, grant reviewers  
**Duration:** 15–20 minutes  
**Tone:** Practical, calm, “this survives if I’m out sick”

## How Deb uses TIKR (delivery)

| Mode | How to start | Who |
|------|--------------|-----|
| **Production (primary)** | Browser → `http://<nas-hostname>:8080` after IT runs [docker-compose.prod.yml](../docker/docker-compose.prod.yml) on Synology | Wiley clerk daily |
| **Optional Windows VM** | `./scripts/publish-tikr.sh` → run `TIKR.Api.exe` then `TIKR.Web.exe` | IT smoke test only |

Settings shows **Ollama** and **Grok** status (env-configured on the API) — there is no “Local PC / Full Synology” UI toggle.

---

## Before you walk on stage

| Check | How |
|-------|-----|
| Web loads | http://localhost:8080 (or NAS hostname) |
| API healthy | http://localhost:5001/health (Mac) or :5000 (NAS) |
| Ollama ready | Settings → Ollama **Connected** |
| Grok demo ready | `USE_GROK=false` in Act 1; real key staged for Act 2 |
| Sample PDF | Small town doc in Downloads for agent scan |
| Browser zoom | 100%; high-contrast theme optional (Settings footer toggle if enabled) |

**Open tabs:** Dashboard · Requirements · Documents · Vault · Assistant · Settings  
**Second screen (optional):** Terminal with `$API` curl script from [demo-code-platoon.md](demo-code-platoon.md#7-live-demo--full-api-matrix-curl)

---

## Clerk script (minute-by-minute)

### 0:00 — Hook (Dashboard, `g d`)

> “I’m the only clerk in Wiley. When a council member asks *‘When is TABOR notice due?’* at 9 PM, I can’t dig through three binders. TIKR is my second brain — and it stays on our Synology NAS, not someone else’s cloud.”

**Show:** Urgency pills, upcoming deadlines grid, quick actions.

**Say:** “These priorities come from real due dates — overdue items float to the top.”

---

### 2:00 — Requirements lifeline (`g r`)

> “Colorado deadlines are pre-seeded. If something happens to me, my deputy opens this page first.”

**Show:** Emergency banner, filter by urgency, seeded “CO Default” column.

**Do:** Click **AI Scan uploaded doc** → pick sample PDF/txt → wait for banner → point at extraction badge (“Plain-text extraction” or “Syncfusion tools”) → review pre-filled dialog → **Save**.

**Say:** “I still approve every row — AI suggests, I decide.”

---

### 5:00 — Documents (`g o`)

> “Every resolution, election packet, and trustee letter lands here — searchable, tagged, downloadable.”

**Do:**

1. Upload a small file (or show existing).
2. Select row → open preview pane (PDF viewer or text fallback).
3. Right-click → **Download** (streams from NAS storage).
4. Mention AI tag suggestion if visible.

**Say:** “Files never leave the NAS unless I export them.”

---

### 8:00 — Vault (`g v`)

> “Institutional memory that isn’t a formal document — who to call, how we run council night, voice notes from walking the hall.”

**Do:** Show a **Contacts** or **HowTo** entry; optionally record a short voice note (Speech-to-Text) → save.

**Say:** “This is what the next clerk needs on day one.”

---

### 10:00 — Assistant (`g a`)

> “Day-to-day questions stay local. I ask in plain English; Ollama answers on our box.”

**Do:** Type: *“What should I prioritize this week for Wiley?”* — let stream finish.

**Say:** “Nothing here phones home unless I choose Advanced AI.”

---

### 12:00 — Grok toggle (Settings + Assistant)

> “For harder legal reasoning I can turn on Grok — but only when IT sets it on the server. Let me show you both modes.”

#### Act A — Grok OFF (default)

1. **`g s`** Settings → AI Status → **Grok: Disabled**
2. Assistant → **Ask Advanced AI (Grok)** after a chat question → note mentions local fallback
3. *(Optional co-presenter)* terminal: `"usedGrok": false`

**Say:** “Default is local-only. Good for everyday work and budget.”

#### Act B — Grok ON

1. Co-presenter sets `USE_GROK=true` + key, restarts API (30 sec)
2. Refresh Settings → **Grok: Enabled**
3. Assistant → **Ask Advanced AI** again → “Grok response shown below”
4. Terminal: `"usedGrok": true`

**Say:** “Same button — different backend — and the app tells me which one answered.”

---

### 15:00 — Trust & audit (Settings)

**Show:** Local storage card (town name, NAS label, last DB save), recent audit log entries from your demo actions.

**Say:** “Every create, update, delete is logged. Council can ask what changed and when.”

---

### 17:00 — Close

> “One person, one town, one NAS — statutory deadlines, documents, tribal knowledge, and AI that respects our boundary. Questions?”

**Leave on:** Dashboard or Requirements emergency banner.

---

## Cheat sheet

### URLs (local Docker)

| What | URL |
|------|-----|
| TIKR app | http://localhost:8080 |
| API health (Mac dev) | http://localhost:5001/health |
| API health (NAS/Linux) | http://localhost:5000/health |

### Keyboard shortcuts

Press **`?`** anytime (when not typing in a field):

| Keys | Go to |
|------|-------|
| `g` then `d` | Dashboard (`/`) |
| `g` then `r` | Requirements |
| `g` then `o` | Documents |
| `g` then `v` | Vault |
| `g` then `a` | Assistant |
| `g` then `s` | Settings |
| `?` | This shortcut list |

---

### Page quick reference

| Route | Clerk name | One-liner |
|-------|------------|-----------|
| `/` | Dashboard | “What’s urgent this week?” |
| `/requirements` | Requirements | “Statutory deadlines + custom filings” |
| `/calendar` | Calendar | “Month view of due dates” |
| `/documents` | Documents | “Upload, preview, download, AI tags” |
| `/vault` | Vault | “Contacts, how-tos, voice notes” |
| `/knowledge` | Knowledge (legacy nav) | Redirects conceptually to Vault |
| `/assistant` | AI Assistant | “Local chat + optional Grok” |
| `/settings` | Settings | “NAS health, AI status, audit log” |

---

### Demo phrases (if something breaks)

| Situation | Say |
|-----------|-----|
| Ollama offline | “Local AI pauses — my data is still safe on the NAS. I can keep filing manually.” |
| Grok disabled | “That’s intentional — we’re in local-only mode today.” |
| Agent scan slow | “First scan warms up models; typical on a small NAS.” |
| PDF preview blank | “Fallback shows extracted text — file is still stored correctly.” |
| API curl fails | “Web UI is the clerk path; API checks are for IT in the back row.” |

---

## Live validation checklist (front of audience)

Use this with a co-presenter on terminal, or rehearse solo the night before.

### UI path (Deb)

- [ ] Dashboard loads priorities
- [ ] Requirements: agent scan → save one row
- [ ] Documents: upload → preview → download
- [ ] Vault: show or add one knowledge entry
- [ ] Assistant: Ollama chat streams
- [ ] Grok OFF: Settings shows Disabled; Advanced AI uses fallback
- [ ] Grok ON: Settings shows Enabled; Advanced AI shows Grok note
- [ ] Settings: audit log shows demo actions

### API path (co-presenter, ~3 min)

```bash
export API=http://localhost:5001   # Mac dev; else :5000

curl -sf "$API/health" && echo OK
curl -sf "$API/api/ai/status" | grep -q ollamaAvailable
curl -sf -X POST "$API/api/ai/ask-advanced" \
  -H 'Content-Type: application/json' \
  -d '{"prompt":"One word: healthy?","context":null}' | grep -q usedGrok
# Toggle USE_GROK, restart API, re-run — usedGrok flips true/false
```

Full script: [demo-code-platoon.md §7](demo-code-platoon.md#7-live-demo--full-api-matrix-curl)

---

## Grok toggle quick reference (IT)

| Step | Action |
|------|--------|
| 1 | Edit `docker/.env` on NAS or dev machine |
| 2 | Set `USE_GROK=false` (Act 1) or `USE_GROK=true` + `GROK_API_KEY=…` (Act 2) |
| 3 | `docker compose -f docker/docker-compose.yml --env-file docker/.env restart tikr-api` |
| 4 | Confirm `GET /api/ai/status` → `grokEnabled` matches |
| 5 | Confirm `POST /api/ai/ask-advanced` → `usedGrok` matches |

**Never** paste real API keys on screen — pre-stage in `.env` before demo.

---

## Colorado clerk anchors (optional color)

- Pre-seeded requirements reflect common municipal cycles (elections, TABOR, budgets).
- Town name defaults to **Wiley** in demo env — change `TIKR_TOWN_NAME` in `docker/.env`.
- Footer shows NAS storage label (`TIKR_STORAGE_LABEL`) for “where your data lives” talking point.

---

## After demo

```bash
# Stop stack (optional)
docker compose -f docker/docker-compose.yml down
```

**Follow-ups to offer:** [architecture.md](architecture.md) one-pager · [docker/README.md](../docker/README.md) for IT · GitHub repo link for grant appendix.
