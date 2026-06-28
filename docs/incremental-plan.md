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

- `/assistant` page with `SfAIAssistView` + `IChatClient` → Ollama
- Hybrid AI in API (`HybridAiService`, Grok gated)
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

**Status:** done (5B manual GH settings pending — Actions read-only `GITHUB_TOKEN` only open item)

**Note on UI Completion (2026 analysis):** Major UI elements delivered:
- Dashboard (Prompt 2): Urgency pills, AI summary, quick actions, grids, activity.
- Documents (Prompt 4): Uploader, TreeView folders, Grid with search/filters, ContextMenu, Splitter preview, AI banners.
- Knowledge Vault (Prompt 5 at `/vault`): Red "hit by a bus" banner, SfTab (How-To/Contacts/Tribal/Voice), Accordion/Grid, RichTextEditor, voice sim, "Copy for New Clerk".
- Calendar solid. Legacy `/knowledge` redirects to `/vault`. **Requirements Manager MVP** at `/requirements` — see [requirements-working-tree.md](requirements-working-tree.md).
- Assistant semantic doc + vault RAG context wired (Phase 9). Docker/CI support strong for shipping.

### 5A — Fix CI (code)

| Step | Action | Status |
|------|--------|--------|
| 1 | Fix `.gitignore`: `data/` → `/data/` so `src/TIKR.Infrastructure/Data/` is tracked | done |
| 2 | Commit EF Core `TikrDbContext` + Migrations | done |
| 3 | Run `dotnet format TIKR.sln`; verify `dotnet test` + Trunk on PR | done |
| 4 | Merge PR `fix/ci-green-main` with green **TIKR CI** + **Trunk** | done ([PR #9](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/9)) |
| 5 | Triage Dependabot PRs after `main` is green — see [dependabot-policy.md](dependabot-policy.md) | done |

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
- [ ] **Settings → Actions:** allow actions; read-only default `GITHUB_TOKEN`

---

## Phase 6 — Smart Components

**Status:** planned

**Goal:** Syncfusion Smart AI on clerk forms (paste, textarea, scheduler).

**Acceptance criteria:**

- Smart Paste on Knowledge / requirement forms
- Scheduler natural-language recurring events
- Ollama wired via `Syncfusion.Blazor.AI` per docs

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

**Key paths:** `src/TIKR.Infrastructure/Identity/`, `src/TIKR.Api/AuthEndpoints.cs`, `src/TIKR.Web/Components/Pages/Login.razor`, `docker/.env.example`

---

## Phase 9 — Search and documents

**Status:** done (MVP core); PDF/DOCX preview + IMAP deferred

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
- [ ] PDF preview (Syncfusion `SfPdfViewer` — deferred)
- [ ] Rich DOCX editor / Spreadsheet preview (Syncfusion DocumentEditor / Spreadsheet — deferred)
- [ ] IMAP or forward-to-folder email ingestion scaffold

**Key paths:** `src/TIKR.Infrastructure/Services/HybridAiService.cs`, `src/TIKR.Infrastructure/Services/DocumentTextExtractionService.cs`, `src/TIKR.Shared/Entities/Document.cs`, `tests/TIKR.Infrastructure.Tests/Services/HybridAiServiceSemanticSearchTests.cs`

---

## Phase 10 — Requirements Manager + Document Agent

**Status:** in progress (10A + 10B merged; 10C groups A–E deferred)

**Goal:** `/requirements` CRUD hub + incremental NAS-local document agent without breaking MVP grid.

| Slice | Status | PR |
|-------|--------|-----|
| **10A** Requirements grid MVP | done | [#30](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/30) |
| **10B** MVP agent stub + AI Scan | done | [#31](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/31) |
| **10C** AgentTools + AES storage + hooks | in progress (A1+A2 done #35; E2E PR) | [requirements-working-tree.md](requirements-working-tree.md), [sf-document-agent-tools.md](sf-document-agent-tools.md) |

**Key paths:** `src/TIKR.Web/Components/Pages/Requirements.razor`, `src/TIKR.Infrastructure/Services/DocumentAgentService.cs`

---

## Phase 0 — Final Gap Closure & Ship-Ready Polish

**Status:** in progress (PR #33 merged; PR sequence 2–4 remaining)

**Purpose:** Clerk-facing polish before Deb sign-off — local-first trust cues, safe deletes, accessibility, and E2E smoke.

### PR sequence

| # | Slice | Status |
|---|-------|--------|
| 1 | UI polish + NAS footer (#33) | done |
| 2 | Final test & accessibility pass (keyboard nav, bUnit, Playwright) | in progress |
| 3 | Documentation & clerk touches | planned |
| 4 | Health UI closure + Done Detector sign-off | planned |

### Acceptance criteria (combined PR #33 + follow-ups)

- [x] Help (`PageHelp`) on every MainLayout page (Dashboard, Calendar, Requirements, Documents, Assistant, Vault, Settings, Account, Users)
- [x] Confirm delete dialog + 5s undo toast (Requirements, Vault; toast-only for Documents)
- [x] Audit note on delete + recent audit list on Settings
- [x] Print-friendly council packet export on Requirements (`Print council packet` + print CSS)
- [x] Theme switch (Light / Dark / High contrast) persisted in `localStorage`
- [x] Offline banner on every page when API unreachable
- [x] Live Synology footer (`GET /api/system/local-status`) on all pages
- [x] Keyboard shortcuts help modal (`?`) + `g` navigation (d/r/o/v/a/s)
- [x] Mobile touch targets (44px) and responsive sidebar
- [x] Settings: Synology health + Ollama status card
- [x] Playwright E2E scaffold (`tests/e2e/`) — run manually against Docker stack
- [x] bUnit coverage for footer, toast, helpers
- [x] Skip link + `:focus-visible` accessibility baseline

**Env vars:** `TIKR_TOWN_NAME` (default Wiley), `TIKR_STORAGE_LABEL` (default Synology NAS)

**Key paths:** `src/TIKR.Web/Components/Shared/`, `src/TIKR.Web/wwwroot/css/tikr-clerk-polish.css`, `tests/e2e/`

---

## How to update this doc

When a phase completes, set **Status** to `done` and move **in progress** to the next phase. Keep acceptance criteria honest — check boxes only when verified in CI or manual test.

---

## MVP remaining (2026-06-28)

**Ship bar:** Phases **1–9 core**, **10A–10B**, and **Phase 0 PR #33** merged on `main`. Phase 0 PR sequence items 2–4 remain before Deb sign-off.

### Phases 1–9 summary

| Phase | Status | Notes |
|-------|--------|-------|
| 1 Scaffold | done | |
| 2 Tests | done | 235+ tests; coverage floors in CI |
| 3 Syncfusion AI | done | `/assistant`, HybridAiService |
| 4 GitHub + Trunk | done | |
| 5 Hardening | done | 5B Actions `GITHUB_TOKEN` manual only |
| 6 Smart Components | planned | post-ship |
| 7 Coverage | done | Playwright → Phase 0 |
| 8 Auth | done | optional multi-user |
| 9 Search/docs | done (core) | RAG + semantic UI; PDF/IMAP deferred |

### Open acceptance criteria (by phase)

| Phase | Item | Blocks ship? |
|-------|------|--------------|
| **5B** | GitHub **Settings → Actions:** read-only default `GITHUB_TOKEN` | No |
| **9** | Plain-text `FullTextContent` on upload | done ([#32](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/32)) |
| **9** | PDF/DOCX preview, IMAP ingestion | No (deferred) |
| **0** | Playwright E2E + polish checklist | In progress (PR #33 done) |

Remaining polish, accessibility, and E2E coverage are tracked in **Phase 0** above.

### CI status

All feature PRs through #31 merged; `main` green on **TIKR CI** + **Trunk** + **GitGuardian**.

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

### Auth follow-ups (vNext — not MVP)

- [ ] Email password reset (requires SMTP on NAS)
- [ ] Token refresh / rotation hardening
- [ ] Read-only `Viewer` role for council read access
- [x] Manual auth smoke test on Docker with `docker/.env` bootstrap creds (document in README test plan)

### Docs and repo hygiene

- [x] README: optional multi-user auth env vars documented
- [x] Deploy docs: shared `env_file` applies auth vars to both `tikr-api` and `tikr-web`
- [x] `.rag_index/` — gitignored (local RAG index only)

### Phase 6+ (explicitly post-MVP)

- [ ] Phase 6 — Smart Paste, Smart TextArea, Scheduler NL recurring (Syncfusion.Blazor.AI)
- [ ] Phase 9 deferred — PDF/DOCX preview, IMAP ingestion, full-text extraction pipeline
- [ ] Phase 0 — Playwright E2E clerk flows + accessibility pass (see Phase 0)

### Suggested merge order

1. **Phase 0** PR sequence ([#34](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/34) merged; docs → sign-off)
2. **Phase 10C A1+A2** — agent storage + Syncfusion extraction ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35) merged)
3. **Phase 10C-D** — E2E proof (fixtures, Playwright, licensed workflow) — **current**
4. Phase 6 when clerk forms need Smart AI
