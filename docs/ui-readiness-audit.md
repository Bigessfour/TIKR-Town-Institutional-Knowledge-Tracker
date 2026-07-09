# TIKR UI readiness audit (clerk routes)

Living log for **page-by-page** operational readiness before `v1.0.0`. Complements [syncfusion-e2e-audit-plan.md](syncfusion-e2e-audit-plan.md) and Playwright specs in `tests/e2e/`.

**Base URL (local Docker):** `http://localhost:8080`  
**Tools:** Chrome DevTools MCP, Playwright (`tests/e2e/`), `TIKR.Web.Tests` (bUnit).

---

## Completion report — 2026-07-09 (full pass)

**Environment:** `tikr-web` + `tikr-api` healthy; host Ollama via `docker-compose.host-ollama.yml`; API `http://localhost:5001`.

| Layer | Result |
|--------|--------|
| **Playwright E2E** | **15 / 15 passed** (after agent-scan file-chooser fix) |
| **TIKR.Web.Tests (bUnit)** | **135 / 135 passed** |
| **Chrome DevTools MCP** | Clerk routes exercised (see matrix below) |
| **API smoke** | `ollamaAvailable: true`, `grokEnabled: true`; Document SDK + agent tools enabled |

### Playwright detail

| Spec | Tests | Result |
|------|------:|--------|
| `page-readiness.spec.ts` | 11 | **All pass** (nav, requirements dialog, documents search + row checkbox/preview, vault tabs, assistant affordances, settings cards, calendar schedule, keyboard `?`, no trial overlay) |
| `clerk-smoke.spec.ts` | 3 | **All pass** |
| `requirements-agent-scan.spec.ts` | 1 | **Pass** — Browse + `filechooser` triggers SfUploader (hidden `setInputFiles` was unreliable) |

Command:

```bash
cd tests/e2e && npm test -- --reporter=line
```

### API / AI probes

```bash
curl -s http://localhost:5001/api/ai/status
# {"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":true}

curl -s http://localhost:5001/api/system/document-sdk-status
# licenseProbePassed: true, agentToolsEnabled: true
```

`POST /api/ai/ask-advanced` with Grok-intent prompt returns an answer; with Ollama up, **`usedGrok` may still be `false`** (local model preferred unless Grok path wins or Ollama fails). UI **Send** uses Ollama directly from `tikr-web`; **Ask Advanced AI** uses the API.

---

## Chrome DevTools MCP — control matrix (2026-07-09 session 2)

Console **errors:** one transient **404** on dashboard load early in session; **no errors** after requirements onward.

| Route | Control / action | Result |
|-------|------------------|--------|
| **Dashboard** | Page help | **OK** (click) |
| **Dashboard** | Display theme combobox | **Partial** — opens; a11y `value` still shows placeholder-style “Display theme”, not “Light” (verify visually) |
| **Dashboard** | `?` shortcuts | Covered by Playwright (**OK**) |
| **Requirements** | Add requirement → dialog | **OK** |
| **Requirements** | Cancel closes dialog | **OK** |
| **Requirements** | Export CSV | **OK** (click; download not asserted) |
| **Requirements** | Council packet / Agenda PDF / Compliance Excel / Meeting minutes / Print council packet | **Present**, not clicked |
| **Requirements** | AI Scan file upload | **Present**; E2E upload path **fail** (banner) |
| **Requirements** | Edit (row) | **Present** (not clicked this session; prior session **OK**) |
| **Requirements** | Filters / Show completed / Reset Wiley view | **Present**, not clicked |
| **Calendar** | Schedule month view + events | **OK** (render) |
| **Calendar** | Agenda view button | **OK** (click) |
| **Calendar** | Today / Month / Next / Prev | **Present** (Agenda exercised) |
| **Documents** | Full-text / Semantic search | **OK** (semantic clicked) |
| **Documents** | Folder tree | **OK** (3 docs) |
| **Documents** | Row “Select row” checkbox | **Fail MCP** (timeout); **OK Playwright** |
| **Documents** | Upload / preview actions | **Partial** (uploader visible; preview via Playwright only) |
| **Vault** | Tabs How-To / Contacts / … | **OK** (Contacts clicked) |
| **Vault** | Copy Everything for New Clerk | **OK** (click) |
| **Vault** | Generate Complete Handover Package | **Present**, not clicked |
| **Assistant** | Send (default prompt) | **OK** (click; streaming not fully waited in snapshot) |
| **Assistant** | Ask Advanced AI (Grok) | **Present** (not clicked this session) |
| **Settings** | Status cards (NAS, Syncfusion, Ollama, Grok) | **OK** — Grok **Enabled**, Ollama **Connected** |
| **Settings** | Clerk preferences / deployment (source) | **Not in running image** — rebuild `tikr-web` to pick up latest `Settings.razor` |
| **Auth routes** | `/login`, `/account`, `/settings/users` | **Not tested** (auth off) |

---

## Per-page readiness (summary)

| Route | Status | Notes |
|-------|--------|--------|
| `/` | **Ready** | Playwright + MCP help |
| `/calendar` | **Ready** | Schedule + grid; smoke views |
| `/requirements` | **Ready** (core) | CRUD dialog smoke; exports/generate PDFs not download-tested |
| `/documents` | **Ready** | Playwright checkbox/preview; MCP checkbox flaky |
| `/assistant` | **Ready** | Ollama Send exercised; Grok via Advanced AI / API |
| `/vault` | **Ready** (smoke) | Tabs + copy; handover PDF not clicked |
| `/settings` | **Ready** (read-only health) | Editable clerk prefs in repo pending web image rebuild |
| Auth pages | **Pending** | Optional `TIKR_AUTH_ENABLED` |

---

## Known gaps / follow-ups

1. ~~**`requirements-agent-scan.spec.ts`**~~ — **Fixed:** use Syncfusion Browse + Playwright `filechooser` (API `/api/ai/agent-scan` was already OK).
2. **Documents grid checkbox** — Chrome DevTools click timeout; rely on Playwright or force-click wrapper pattern.
3. **Theme dropdown** — Confirm selected label in UI after `TikrThemeSelector` fix; MCP a11y tree may not reflect `Text` field.
4. **Export / PDF / handover** — Click + assert download or API response in a dedicated E2E spec.
5. **Grok `usedGrok: true`** — Confirm with UI Ask Advanced after Ollama stopped or Grok-preferring prompt when x.ai key valid.
6. **Rebuild `tikr-web`** after local UI changes (Settings clerk preferences, theme, assistant streaming fix).

---

## Historical

### Playwright — early 2026-07-09 (10/10)

Before documents-checkbox test and agent-scan failure documentation.

### MCP session 1 (2026-07-09)

Initial smoke; documents checkbox MCP timeout; Assistant Send deferred (Ollama offline in notes at that time).
