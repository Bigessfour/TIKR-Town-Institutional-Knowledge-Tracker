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

**Status:** in progress (5A done; 5B manual GH settings pending; UI features largely complete via Prompts 2/4/5)

**Note on UI Completion (2026 analysis):** Major UI elements delivered:
- Dashboard (Prompt 2): Urgency pills, AI summary, quick actions, grids, activity.
- Documents (Prompt 4): Uploader, TreeView folders, Grid with search/filters, ContextMenu, Splitter preview, AI banners.
- Knowledge Vault (Prompt 5 at /vault): Red "hit by bus" banner, SfTab (How-To/Contacts/Tribal/Voice), Accordion/Grid, RichTextEditor, voice sim, "Copy for New Clerk".
Calendar solid. Legacy /knowledge and thin Requirements remain as cleanup items.
AI runtime context still limited (see RAG MCP and gaps). Docker/CI support strong for shipping.

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
- [ ] Playwright E2E (deferred)

**Key paths:** `tests/`, `coverlet.runsettings`

---

## Phase 8 — Auth

**Status:** planned

**Goal:** Single-clerk today → optional multi-user for larger towns.

**Acceptance criteria:**

- Identity provider or simple NAS-local auth design
- Protected API routes; audit trail preserved

**Key paths:** `src/TIKR.Api/`, `docs/architecture.md`

---

## Phase 9 — Search and documents

**Status:** in progress (doc + vault RAG backend done; UI wiring + ingestion + PDF preview pending)

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
- [ ] PDF preview (Syncfusion `SfPdfViewer` — deferred)
- [ ] Rich DOCX editor / Spreadsheet preview (Syncfusion DocumentEditor / Spreadsheet — deferred)
- [ ] Full-text extraction for non-text files (still stubbed; biggest backend lever remaining)
- [ ] IMAP or forward-to-folder email ingestion scaffold

**Key paths:** `src/TIKR.Infrastructure/Services/HybridAiService.cs`, `src/TIKR.Shared/Entities/Document.cs`, `src/TIKR.Shared/Entities/KnowledgeEntry.cs`, `tests/TIKR.Infrastructure.Tests/Services/HybridAiServiceSemanticSearchTests.cs`, `tests/TIKR.Infrastructure.Tests/Services/HybridAiServiceVaultRagTests.cs`

---

## How to update this doc

When a phase completes, set **Status** to `done` and move **in progress** to the next phase. Keep acceptance criteria honest — check boxes only when verified in CI or manual test.
