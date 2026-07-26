# TIKR Incremental Plan

Living roadmap for TIKR development. Agents and contributors: read the **current phase** before large changes. See also [AGENTS.md](../AGENTS.md).

**Repo:** https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker

---

## Phase 1 — Scaffold

**Status:** done

**Goal:** Greenfield .NET 10 solution for local-first clerk tooling on Synology NAS.

**Acceptance criteria:**

- Blazor Interactive Server + Minimal API + EF Core Infrastructure
- Docker Compose for API, Web, Ollama
- Colorado deadline seed reference in `scripts/`

**Key paths:** `src/`, `docker/`, `docs/architecture.md`, `TIKR.sln`

---

## Phase 2 — Tests

**Status:** done

**Goal:** Automated test foundation with coverage policy ramping to 90%.

**Acceptance criteria:**

- 63+ tests across Shared, Infrastructure, Api integration, Web (bUnit)
- `coverlet.runsettings` + CI coverage artifact upload
- Policy documented in `tests/README.md`

**Key paths:** `tests/`, `.github/workflows/ci.yml`

---

## Phase 3 — Syncfusion AI

**Status:** done

**Goal:** Developer-time AI (MCP, skills) and runtime clerk chat via Ollama.

**Acceptance criteria:**

- `/assistant` page with `SfAIAssistView` + `IChatClient` → Ollama (validate first, Grok fallback on context/availability)
- Hybrid AI in API (`HybridAiService`, Ollama first then Grok fallback by prompt context)
- `docs/ai-tooling.md`, `.cursor/mcp.json.example`

**Key paths:** `src/TIKR.Web/Components/Pages/Assistant.razor`, `docs/ai-tooling.md`

---

## Phase 4 — GitHub + Trunk

**Status:** done

**Goal:** Public repo hygiene, secret scanning, lint CI, first push.

**Acceptance criteria:**

- `.gitignore` hardened (`.agents/`, coverage, keys, env variants)
- Trunk: gitleaks, yamllint, markdownlint, hadolint (+ dotnet format via workflow SDK 10)
- LICENSE (MIT), SECURITY.md, PR template, dependabot
- Initial commit pushed to `main`

**Key paths:** `.trunk/`, `.github/workflows/`, `.gitleaks.toml`

---

## Phase 5 — Post-push hardening

**Status:** done (5B Actions read-only `GITHUB_TOKEN` verified 2026-07-25)

**Note on UI Completion (2026 analysis):** Major UI elements delivered:
- Dashboard (Prompt 2): Urgency pills, AI summary, quick actions, grids, activity.
- Documents (Prompt 4): Uploader, TreeView folders, Grid with search/filters, ContextMenu, Splitter preview, AI banners.
- Knowledge Vault (Prompt 5 at `/vault`): Red "hit by a bus" banner, SfTab (How-To/Contacts/Tribal/Voice), Accordion/Grid, RichTextEditor, voice sim, "Copy for New Clerk".
- Calendar solid. Legacy `/knowledge` redirects to `/vault`. **Requirements Manager MVP** at `/requirements` — see [requirements-working-tree.md](requirements-working-tree.md).
- Assistant semantic doc + vault RAG context wired (Phase 9). Docker/CI support strong for shipping.

### 5A — Fix CI (code)

| Step | Action                                                                                         | Status                                                                                         |
| ---- | ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| 1    | Fix `.gitignore`: `data/` → `/data/` so `src/TIKR.Infrastructure/Data/` is tracked             | done                                                                                           |
| 2    | Commit EF Core `TikrDbContext` + Migrations                                                    | done                                                                                           |
| 3    | Run `dotnet format TIKR.sln`; verify `dotnet test` + Trunk on PR                               | done                                                                                           |
| 4    | Merge PR `fix/ci-green-main` with green **TIKR CI** + **Trunk**                                | done ([PR #9](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/9)) |
| 5    | Triage Dependabot PRs after `main` is green — see [dependabot-policy.md](dependabot-policy.md) | done                                                                                           |

**Verify locally:**

```bash
git check-ignore -v src/TIKR.Infrastructure/Data/TikrDbContext.cs   # should NOT match
dotnet build TIKR.sln --configuration Release
dotnet test TIKR.sln --configuration Release
trunk check --all
```

### 5B — GitHub settings (manual, idempotent)

Repeat-safe checklist — safe to re-run anytime:

- [x] **Settings → Branches:** protect `main`; require PR; require checks `build-and-test` + `trunk_check`
- [x] **Settings → General:** **Allow auto-merge** enabled
- [x] **Settings → Code security:** Secret scanning + Push protection (already enabled)
- [x] **Settings → Advanced Security:** Dependabot security updates + grouped security (security updates enabled via API; grouping via `dependabot.yml` `applies-to: security-updates` groups)
- [x] **Settings → General:** topics (`blazor`, `dotnet`, `sqlite`, `ollama`, `municipal`)
- [x] **Settings → Actions:** allow actions; read-only default `GITHUB_TOKEN` — verified 2026-07-25 via `gh api` (`default_workflow_permissions: read`)

---

## Recent Updates (Agent Dev Env, Debug, Move-in - 2026-07)

- **Dev Experience:** External Chromium browser launch via .vscode/launch.json compound + preLaunch waiters for API readiness (5000/health), fixed ports 8080/5000, `TIKR-*` profiles, scripts/run-tikr-local.sh, tuned light logging for debug (`TIKR.*` at Info, Microsoft at Warning).
- **AWS/Amazon Q auth cleanup:** Removed from Cursor settings.json (`amazonQ.*` and `aws.*` keys) to prevent startup hangs.
- **Operation proof logging:** Enhancements and debug configs verified.
- **MCP:** grok_com_github active; clean .cursor/mcp.json to tikr-rag-mcp + sf-blazor-mcp + ollama + grok_com_github (limit 4). Full move-in completed (see below).
- **RAG Index:** Rebuilt successfully with 956 files processed, 19,782 chunks (via .venv/bin/python3 scripts/update_tikr_rag_index.py).

### Agent Development Environment (for Cursor + Grok Build)

To give code agents (Grok Build in Cursor) the best environment:

1. **MCP**: Copy .cursor/mcp.json.example, run scripts/setup-cursor-mcp.sh for .venv tikr-rag-mcp. Activate ≤ 4 servers: tikr-rag-mcp (mandatory RAG), sf-blazor-mcp, ollama, grok_com_github.
2. **RAG**: `search_knowledge` before any substantial change. Run `scripts/update_tikr_rag_index.py` or MCP `refresh_index` after edits.
3. **Inventory**: After public endpoints/pages/services/AI tools: `./scripts/update-function-inventory.sh` + update action-items.md (AGENTS.md rule).
4. **Skills**: `npx skills add syncfusion/blazor-ui-components-skills -y` (pinned in skills-lock.json; priority schedule/grid/uploader).
5. **Rules**: .cursor/rules/tikr.mdc and AGENTS.md always followed. Read incremental-plan current phase.
5. **Ollama**: Running + `nomic-embed-text` pulled.
6. **Secrets**: SYNCFUSION_API_KEY exported for MCP; use .env.example.
7. **Workflow**: Use todo_write for complex, enter_plan_mode for ambiguity, cite RAG hits.

Update this section when env changes.

---

## Phase 6 — Smart Components

**Status:** done (MVP)

**Goal:** Syncfusion Smart AI on clerk forms (paste, textarea, scheduler NL).

**Acceptance criteria:**

- [x] Smart Paste on Requirements forms (`SfSmartPasteButton` + Ollama)
- [x] Smart TextArea on Requirements description + Vault AI draft assist
- [x] Calendar natural-language deadline create (Ollama → Requirements API)
- [x] Ollama wired via `AddSyncfusionSmartComponents().InjectOpenAIInference()` + shared `IChatClient`

**Key paths:** `docs/ai-tooling.md` (Part C), `src/TIKR.Web/`

---

## Phase 7 — Coverage ramp

**Status:** done

**Goal:** Raise line coverage toward per-assembly targets in `tests/README.md`.

**Acceptance criteria:**

- [x] CI coverage floor via `scripts/check_coverage.py` (Shared/Infra ≥90%, Api integration-tested, Web Helpers/Services ≥85%)
- [x] Gaps filled in Api AI endpoints, Infrastructure edge cases, Web client + bUnit pages
- [x] Playwright E2E scaffold (`tests/e2e/`) — run manually against Docker stack

**Key paths:** `tests/`, `coverlet.runsettings`

---

## Phase 8 — Auth

**Status:** done

**Goal:** Single-clerk today → optional multi-user for larger towns.

**Acceptance criteria:**

- [x] ASP.NET Core Identity + JWT (NAS-local SQLite/Postgres; no cloud IdP)
- [x] Auth auto-enables when `TIKR_ADMIN_EMAIL` + `TIKR_ADMIN_PASSWORD` are set; off otherwise
- [x] Protected API routes when auth enabled; audit `UserId` populated from JWT
- [x] Syncfusion login (`SfDataForm`), account password change, admin user grid (`/settings/users`)
- [x] Auth vNext local MVP: `Viewer` role (read-only API mutations), JWT refresh, password reset without SMTP

**Key paths:** `src/TIKR.Infrastructure/Identity/`, `src/TIKR.Api/AuthEndpoints.cs`, `src/TIKR.Web/Components/Pages/Login.razor`, `docker/.env.example`

---

## Phase 9 — Search and documents

**Status:** done (MVP core); forward-to-folder email scaffold shipped; PDF/DOCX/Spreadsheet preview + edit/save shipped

**Goal:** Semantic search, email ingestion, PDF preview.

**Acceptance criteria:**

- [x] `Document.Embedding` (BLOB) via EF migration `AddDocumentEmbedding`
- [x] `nomic-embed-text` wired through `IOllamaChatClientFactory.CreateEmbeddingGenerator`
- [x] `HybridAiService.SemanticSearchDocumentsAsync` (cosine similarity, in-memory; town-clerk scale)
- [x] `HybridAiService.EmbedDocumentAsync` backfill endpoint
- [x] Auto-embed docs on `TagDocumentAsync` (best-effort, graceful when Ollama is offline)
- [x] `/api/ai/semantic-search` and `/api/ai/embed-document/{id}` endpoints
- [x] `TikrApiClient.SemanticSearchDocumentsAsync` / `EmbedDocumentAsync` helpers
- [x] `KnowledgeEntry.Embedding` (BLOB) via EF migration `AddKnowledgeEntryEmbedding`
- [x] `HybridAiService.SemanticSearchKnowledgeAsync` / `EmbedKnowledgeEntryAsync` (mirrors doc RAG)
- [x] Auto-embed Vault entries on `POST /api/knowledge` and `PUT /api/knowledge/{id}` (best-effort)
- [x] `/api/ai/semantic-search-knowledge` and `/api/ai/embed-knowledge/{id}` endpoints
- [x] `TikrApiClient.SemanticSearchKnowledgeAsync` / `EmbedKnowledgeEntryAsync` helpers
- [x] `Documents.razor` Semantic toggle wired to the new endpoint
- [x] `Assistant.razor` prepends top-K semantically relevant **doc + vault** snippets (closes the original "hit by a bus" gap end-to-end)
- [x] Full-text extraction for plain-text uploads (`.txt`, `.md`, `.csv` via `DocumentTextExtractionService`)
- [x] Spec `002-bulletproof-clerk-rag`: chunked `EmbeddingChunks`, hybrid keyword+vector search, minScore gate, grounded assistant citations, `POST /api/ai/reindex-embeddings`
- [x] PDF preview (Syncfusion `SfPdfViewer2` — Documents pane, PDF magic-byte gate)
- [x] Rich DOCX / Spreadsheet preview + edit/save (Syncfusion DocumentEditor + Spreadsheet)
- [x] Forward-to-folder email ingestion scaffold (`TIKR_EMAIL_INBOX_PATH`, `FolderEmailIngestionService`, `POST /api/email/ingest`); real IMAP still optional later

**Key paths:** `src/TIKR.Infrastructure/Services/HybridAiService.cs`, `src/TIKR.Infrastructure/Services/DocumentTextExtractionService.cs`, `src/TIKR.Shared/Entities/Document.cs`, `tests/TIKR.Infrastructure.Tests/Services/HybridAiServiceSemanticSearchTests.cs`

---

## Phase 10 — Requirements Manager + Document Agent

**Status:** in progress — 10C **A1–A3 + document tool coverage** on branch; **10C-C** extraction badge in [#37](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/37)

**Goal:** `/requirements` CRUD hub + incremental NAS-local document agent without breaking MVP grid.

| Slice                                                                                                                                                                                                 | Status                                                          | PR                                                                                                                                                                                                             |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **10A** Requirements grid MVP                                                                                                                                                                         | done                                                            | [#30](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/30)                                                                                                                         |
| **10B** MVP agent stub + AI Scan                                                                                                                                                                      | done                                                            | [#31](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/31)                                                                                                                         |
| **10C A1+A2** Agent storage, AES, Syncfusion Storage Mode extraction                                                                                                                                  | done on `main`                                                  | [#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)                                                                                                                         |
| **10C-D** E2E proof (fixtures, Playwright, licensed workflow)                                                                                                                                         | done on `main`                                                  | [#36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36)                                                                                                                         |
| **10C A3** Ollama + Microsoft.Extensions.AI function loop over Storage Mode tools                                                                                                                     | done                                                            | `SyncfusionDocumentAgentOrchestrator`, `USE_SYNCFUSION_AGENT_ORCHESTRATION`                                                                                                                                    |
| **10C-F** Clerk document tool coverage (PDF ops, Word, Excel, PPT, Office→PDF registry + deterministic paths)                                                                                         | done                                                            | `SyncfusionDocumentAgentToolRegistry`, `feature/phase10c-document-tool-coverage`                                                                                                                               |
| **10C-G (Grok Heavy rec)** Agent scan PDF archive: clean tagged PDF copy + visible stamp + dual (orig + processed) NAS storage + structured tables -> Requirement fields | **done** | `CreateAgentArchivePdfAsync`: red header line `AI PROCESSED - TIKR VAULT` + separate date line; metadata in `DocumentInformation.Subject/Keywords`. Proofs: `SyncfusionDocumentGenerationServiceTests`, `DocumentAgentServiceTests`. |

**Key paths:** `src/TIKR.Web/Components/Pages/Requirements.razor`, `src/TIKR.Infrastructure/Services/DocumentAgentService.cs`, `src/TIKR.SyncfusionDocuments/*`, `src/TIKR.Shared/DTOs/DocumentAgentDto.cs`

---

## Phase 0 adjunct — Clerk guided tour

**Status:** done (MVP)

**Goal:** Onboarding tour for new clerks without blocking daily workflows.

**Acceptance criteria:**

- [x] Stable `data-tour` anchors on all clerk routes (`ClerkTourCatalog`, `ClerkTourIds`)
- [x] Global + per-page tour steps; replay from Settings
- [x] Auth-backed tour prefs when login enabled (`/api/auth/me/tour`, localStorage fallback)
- [x] Playwright anchor smoke (`tests/e2e/clerk-tour-anchors.spec.ts`)

**Key paths:** `src/TIKR.Web/ClerkTour/`, `src/TIKR.Web/Services/ClerkTourService.cs`, `docs/clerk-tour-deployment.md`

---

## Phase 0 — Final Gap Closure & Ship-Ready Polish

**Status:** closing ([#33](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/33) + [#34](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/34) + [#48](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/48) merged; PR #3 docs done; **PR #4 recorded walkthrough** remains). Active development complete after tag `v1.0.0`; polish in action-items Post-ship.

**Purpose:** Clerk-facing polish before Deb sign-off — local-first trust cues, safe deletes, accessibility, and E2E smoke.

### PR sequence

| #            | Slice                                      | Status                                                                                                                                    |
| ------------ | ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 1            | UI polish + NAS footer (#33)               | done                                                                                                                                      |
| 2            | Test & accessibility pass (#34 + #48)      | **done** — keyboard nav + bUnit + blocking Playwright E2E in CI; `FullyTested` CI filter deferred until coverage targets pass with subset |
| 3            | Documentation & clerk touches              | done ([deb-nas-install.md](deb-nas-install.md), [ship-to-production.md](ship-to-production.md))                                           |
| 4            | Health UI closure + Done Detector sign-off | in progress (Layer 1+2 automated gates done; awaiting recorded Deb walkthrough)                                                           |
| Vault Export | Generate Complete Handover Package (PDF)   | done (last feature - project complete)                                                                                                    |

### Acceptance criteria (combined PR #33 + follow-ups)

- [x] Help (`PageHelp`) on every MainLayout page (Dashboard, Calendar, Requirements, Documents, Assistant, Vault, Settings, Account, Users)
- [x] Confirm delete dialog + 5s undo toast (Requirements, Vault; toast-only for Documents)
- [x] Audit note on delete + recent audit list on Settings
- [x] Print-friendly council packet export on Requirements (`Print council packet` + print CSS)
- [x] Theme switch (Light / Dark / High contrast) persisted in `localStorage`; Syncfusion theme CSS link swap + side panel CSS + body attr for full control readability (via sf-blazor-mcp informed config). Production hardened (guards, ErrorBoundary) to eliminate runtime error banner on switch.
- [x] Offline banner on every page when API unreachable
- [x] Live Synology footer (`GET /api/system/local-status`) on all pages
- [x] Keyboard shortcuts help modal (`?`) + `g` navigation (d/r/o/v/a/s)
- [x] Mobile touch targets (44px) and responsive sidebar
- [x] Settings: Synology health + Ollama status card
- [x] Playwright E2E required CI gate (`tests/e2e/` against Docker stack in **TIKR CI** — [#48](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/48))
- [x] bUnit coverage for footer, toast, helpers
- [x] Skip link + `:focus-visible` accessibility baseline
- [x] Agent Dev Env + move-in complete (MCP, RAG, skills, rules, debug launch with external browser)
- [x] RAG index rebuilt (956 files, 19,782 chunks)

**Env vars:** `TIKR_TOWN_NAME` (default Wiley), `TIKR_STORAGE_LABEL` (default Synology NAS)

**Key paths:** `src/TIKR.Web/Components/Shared/`, `src/TIKR.Web/wwwroot/css/tikr-clerk-polish.css`, `tests/e2e/`, `.cursor/`, `scripts/`

**Done Detector note:** Layer 1 (function inventory clean) + Layer 2 (Project-Level Done Detector checklist in action-items.md + `scripts/done-detector.sh`) is the final sign-off gate for Phase 0 / PR #4. See AGENTS.md and action-items.md.

---

## How to update this doc

When a phase completes, set **Status** to `done` and move **in progress** to the next phase. Keep acceptance criteria honest — check boxes only when verified in CI or manual test.

**Function inventory (hybrid):** After significant endpoint/page/service/AI tool work, run `./scripts/update-function-inventory.sh`, then curate status/verification in `docs/action-items.md` and tree in `docs/function-tree.md`. See AGENTS.md for the agent rule. Rebuild RAG index after edits.

When inventory is clean, use `./scripts/done-detector.sh` + complete the Project-Level Done Detector gate in action-items.md before declaring done.

---

## MVP remaining (2026-07-07)

**Ship bar:** Phases **1–9 core**, **10A–10B**, **10C A1+A2** ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)), and **Phase 0 PR #33–#34 + #48** on `main`. **10C-D** ([#36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36)) + Phase 0 PRs 3–4 remain before full Deb sign-off. Agent move-in and dev tooling (launch/debug, RAG/MCP) complete.

### Phases 1–9 summary

| Phase              | Status      | Notes                                                          |
| ------------------ | ----------- | -------------------------------------------------------------- |
| 1 Scaffold         | done        |                                                                |
| 2 Tests            | done        | 235+ tests; coverage floors in CI                              |
| 3 Syncfusion AI    | done        | `/assistant`, HybridAiService                                  |
| 4 GitHub + Trunk   | done        |                                                                |
| 5 Hardening        | done        | 5B Actions `GITHUB_TOKEN` manual only                          |
| 6 Smart Components | done        | Smart Paste/TextArea + Calendar NL                             |
| 7 Coverage         | done        | Playwright → Phase 0                                           |
| 8 Auth             | done        | optional multi-user + Viewer/refresh/local reset               |
| 9 Search/docs      | done (core) | RAG + semantic UI; PDF/Word/Spreadsheet edit+save; folder email |

### Open acceptance criteria (by phase)

| Phase  | Item                                                            | Blocks ship?                                                                                                                                              |
| ------ | --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **5B** | GitHub **Settings → Actions:** read-only default `GITHUB_TOKEN` | No                                                                                                                                                        |
| **9**  | Plain-text `FullTextContent` on upload                          | done ([#32](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/32))                                                             |
| **9**  | Forward-to-folder email scaffold                                | done (`TIKR_EMAIL_INBOX_PATH`); real IMAP optional later                                                                                                  |
| **9**  | PDF / Word / Spreadsheet preview + edit/save                    | done                                                                                                                                                      |
| **0**  | Playwright E2E + polish checklist                               | **partial** — E2E CI gate done ([#48](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/48)); PRs 3–4 (docs + sign-off) remain |

Remaining polish, accessibility, E2E coverage, and agent tooling are tracked in **Phase 0** above.

### CI status

`main` green on **TIKR CI** (build, test, coverage, Docker smoke, Playwright E2E) + **Trunk** + **GitGuardian** — latest gate [#48](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/48) merged 2026-07-08.

---

## Cleanup backlog (post–Phase 8)

Technical debt and UX consolidation. Safe to tackle in small PRs after #27 merges.

### Navigation and pages

- [x] **Retire legacy `/knowledge` page** — replaced with redirect to `/vault`
- [x] **Point sidebar nav to `/vault`**
- [x] **Redirect `/knowledge` → `/vault`**
- [x] **Requirements page** — MVP shipped at `/requirements` ([requirements-working-tree.md](requirements-working-tree.md)); calendar remains timeline view

### Phase 5 note carryover

- [x] Reconcile Phase 5 **Status** — UI complete; 5B Actions setting remains manual
- [x] Update Phase 5 note — RAG + vault semantic search wired; `/knowledge` redirect done

### Auth follow-ups

- [x] Token refresh (`POST /api/auth/refresh`) — local JWT pair with `jti`
- [x] Read-only `Viewer` role for council read access
- [x] Password reset without SMTP (`forgot-password` / `reset-password` + `TIKR_AUTH_EXPOSE_RESET_TOKEN`)
- [ ] Email password reset via SMTP on NAS (optional hardening)
- [x] Manual auth smoke test on Docker with `docker/.env` bootstrap creds (document in README test plan)

### Docs and repo hygiene

- [x] README: optional multi-user auth env vars documented
- [x] Deploy docs: shared `env_file` applies auth vars to both `tikr-api` and `tikr-web`
- [x] `.rag_index/` — gitignored (local RAG index only)

### Phase 6+ follow-ups

- [x] Phase 6 — Smart Paste, Smart TextArea, Calendar NL (Syncfusion.Blazor.SmartComponents + Ollama)
- [x] Phase 9 — Word/Spreadsheet edit+save to NAS (`PUT /api/documents/{id}/content`)
- [x] Phase 9 — forward-to-folder email scaffold (`TIKR_EMAIL_INBOX_PATH`)
- [x] Phase 0 — axe critical a11y smoke (`tests/e2e/a11y-smoke.spec.ts`); deeper Syncfusion a11y still iterative
- [ ] Real IMAP client (optional; folder drop covers MVP)

### Suggested merge order

1. **Phase 0** PR sequence ([#34](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/34) merged; docs → sign-off)
2. **Phase 10C A1+A2** — agent storage + Syncfusion extraction ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35) merged)
3. **Phase 10C-D** — E2E proof ([#36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36)) — **current**
4. Phase 0 PR #3–4 (docs + Deb sign-off)
5. Remaining v1.0 backlog in [action-items.md](action-items.md)

---

## Agent Move-in & Tooling (2026-07 update)

**Status:** completed (see move-in commands and verification).

### Recommended items added
- Agent Development Environment subsection (MCP ≤4, RAG mandatory, skills, rules, Ollama, workflow).
- Recent Updates section covering dev experience (external browser debug with API waiter, fixed ports 8080/5000, TIKR profiles, run-tikr-local.sh), AWS/AmazonQ cleanup from Cursor settings, operation proof logging, MCP (grok_com_github active), RAG index rebuild (956 files, 19,782 chunks).
- Updated MVP remaining and Phase 0 with agent tooling and move-in notes.
- Next actions tied to todos (sync local, verify RAG, Phase 0 completion: Playwright CI gate, docs/handover, sign-off).

Track via todo_write. Always cite RAG hits before changes.

Update this section with future agent env changes.

---

## Current Next Task for Development (from todos + plan)

**Ship order (2026-07-25):** v1.0 feature backlog (former deferred/vNext) → Setup.exe smoke → Deb walkthrough → **tag `v1.0.0` last**.

- [x] Playwright E2E required CI gate ([#48](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/48) merged)
- [x] Phase 0 PR #3 docs / handover
- [ ] **Next:** v1.0 backlog in [action-items.md](action-items.md) (Documents delete undo first)
- [ ] Compile `Setup-TIKR.exe` + clerk Windows smoke/handoff
- [ ] Phase 0 PR #4 / T031 recorded Deb walkthrough + bus-factor gate
- [ ] Tag `v1.0.0` + GHCR (**after** Deb walkthrough)

**Immediate recommendation:** Close v1.0 feature backlog starting with Documents delete undo, then Windows Setup smoke, Deb walkthrough, then tag.
