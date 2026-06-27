# TIKR Incremental Plan

Living roadmap for TIKR development. Agents and contributors: read the **current phase** before large changes. See also [AGENTS.md](../AGENTS.md).

**Repo:** https://github.com/Bigessfour/TIKR-Town_Instutional_Knowledge_Tracker-

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
- Trunk: gitleaks, dotnet-format, yamllint, markdownlint, hadolint
- LICENSE (MIT), SECURITY.md, PR template, dependabot
- Initial commit pushed to `main`

**Key paths:** `.trunk/`, `.github/workflows/`, `.gitleaks.toml`

---

## Phase 5 — Post-push hardening

**Status:** in progress

### 5A — Fix CI (code)

| Step | Action | Status |
|------|--------|--------|
| 1 | Fix `.gitignore`: `data/` → `/data/` so `src/TIKR.Infrastructure/Data/` is tracked | in progress |
| 2 | Commit EF Core `TikrDbContext` + Migrations | in progress |
| 3 | Run `dotnet format TIKR.sln`; verify `dotnet test` + Trunk on PR | in progress |
| 4 | Merge PR `fix/ci-green-main` with green **TIKR CI** + **Trunk** | pending |
| 5 | Triage Dependabot PRs after `main` is green | pending |

**Verify locally:**

```bash
git check-ignore -v src/TIKR.Infrastructure/Data/TikrDbContext.cs   # should NOT match
dotnet build TIKR.sln --configuration Release
dotnet test TIKR.sln --configuration Release
trunk check --all
```

### 5B — GitHub settings (manual, idempotent)

Repeat-safe checklist — safe to re-run anytime:

- [ ] **Settings → Branches:** protect `main`; require PR; require status checks **Trunk** + **TIKR CI**
- [ ] **Settings → Code security:** Secret scanning + Push protection
- [ ] **Settings → General:** description + topics (`blazor`, `dotnet`, `sqlite`, `ollama`, `municipal`)
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

**Status:** planned

**Goal:** Raise line coverage toward 90% per `tests/README.md`.

**Acceptance criteria:**

- CI coverage floor increases incrementally
- Gaps filled in Api endpoints, Infrastructure edge cases, Web pages

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

**Status:** planned

**Goal:** Semantic search, email ingestion, PDF preview.

**Acceptance criteria:**

- Embeddings stored; grid semantic search
- IMAP or forward-to-folder ingestion scaffold
- PDF preview / full-text extraction

**Key paths:** `src/TIKR.Infrastructure/`, `docs/architecture.md` (vNext)

---

## How to update this doc

When a phase completes, set **Status** to `done` and move **in progress** to the next phase. Keep acceptance criteria honest — check boxes only when verified in CI or manual test.
