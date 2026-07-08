# TIKR Project Rules for Cursor / AI Agents

You are **TIKR's AI development partner** — a local-first institutional knowledge tool for one-person town clerks in small Colorado municipalities.

> **North Star (architecture):** [docs/architecture.md](docs/architecture.md) — layers, hybrid AI, NAS deployment.  
> **North Star (roadmap):** [docs/incremental-plan.md](docs/incremental-plan.md) — current phase and acceptance criteria.  
> **Never propose direct commits to `main`** — use `feature/*` or `fix/*` branches and PRs with green CI.

## Role

Help design, implement, and document TIKR: Blazor Interactive Server UI, Minimal Web API, EF Core + SQLite, Ollama/Grok AI, Docker on Synology NAS.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 (pinned in `global.json`) |
| Frontend | Blazor Interactive Server + Syncfusion (individual packages) |
| Backend | Minimal Web API |
| Data | EF Core, SQLite default, PostgreSQL optional |
| AI | Ollama + optional Grok via `Microsoft.Extensions.AI` |
| Quality | Trunk (gitleaks, yaml/md/docker lint) + `dotnet format` in CI |
| CI | GitHub Actions — **TIKR CI** + **Trunk** |

## Git Workflow

1. Read [docs/incremental-plan.md](docs/incremental-plan.md) for the active phase.
2. Branch from `main`: `feature/...` or `fix/...`.
3. Before opening a PR:
   ```bash
   dotnet test TIKR.sln --configuration Release
   trunk check --all
   ```
4. Open PR; merge only when **TIKR CI** and **Trunk** are green.
5. Do not commit secrets, `.env` files, or `.cursor/mcp.json`.
6. **Dependabot:** follow [docs/dependabot-policy.md](docs/dependabot-policy.md) — never merge red dependency PRs; majors are manual.

## Secrets

| Secret | Storage |
|--------|---------|
| `SYNCFUSION_LICENSE_KEY` | `docker/.env`, Web user-secrets |
| `SYNCFUSION_API_KEY` | User env only (MCP) — not the license key |
| `GROK_API_KEY` | `docker/.env`, Api user-secrets |

Never commit: `docker/.env`, `.cursor/mcp.json`, `**/appsettings.Development.json`, key files (`*.pem`, `*.key`). CI runs gitleaks via Trunk.

**macOS local setup:** `./scripts/setup-local-secrets.sh` reads Passwords → `docker/.env` + user-secrets (never prints or commits keys). Syncfusion only: `./scripts/sync-syncfusion-license-key.sh --all`.

Templates: `docker/.env.example`, `.cursor/mcp.json.example`. See [docs/ai-tooling.md](docs/ai-tooling.md).

## Syncfusion

- Use **individual** NuGet packages (`Syncfusion.Blazor.Grid`, `InteractiveChat`, etc.) — **not** the meta `Syncfusion.Blazor` package together with individual packages (duplicate component errors).
- Runtime license: `SYNCFUSION_LICENSE_KEY`. MCP developer key: `SYNCFUSION_API_KEY` (different credential).

## Agent Skills

Skills are **not** in the repo (`.agents/` is gitignored, ~15MB). Install locally:

```bash
npx skills add syncfusion/blazor-ui-components-skills -y
```

Versions pinned in [skills-lock.json](skills-lock.json). Priority skills: schedule, grid, uploader, common, license.

## MCP (Cursor)

```bash
cp .cursor/mcp.json.example .cursor/mcp.json
./scripts/setup-cursor-mcp.sh   # writes .cursor/mcp.json with tikr-rag-mcp + .venv python3
```

Keep ≤4 active MCP servers. See [docs/ai-tooling.md](docs/ai-tooling.md) for `sf-blazor-mcp`, Microsoft Learn, Ollama, **tikr-rag-mcp**.

## RAG (mandatory before code changes)

Before **any** substantial implementation (new files, API endpoints, Blazor pages, refactors):

1. Ensure Ollama is running (`OLLAMA_HOST`, default `http://localhost:11434`) with `nomic-embed-text` pulled.
2. Call **`tikr-rag-mcp` → `search_knowledge`** with a query describing the task (patterns, paths, prior art).
3. After merging or large local edits, run:
   ```bash
   .venv/bin/python3 scripts/update_tikr_rag_index.py
   ```
   Or MCP **`refresh_index`**.

Agents must cite or apply RAG hits (file paths) in their plan — do not invent APIs or duplicate existing helpers.

## Code Conventions

- Match [.editorconfig](.editorconfig) and existing patterns in `src/` and `tests/`.
- Minimal diffs — reuse `TIKR.Shared` DTOs, Infrastructure services, `TikrApiClient`.
- Business AI logic stays in **TIKR.Api** (`HybridAiService`); Web chat is UX only.
- Comments only for non-obvious business or compliance logic (audit trail, Grok gating).

## Always

- Update docs when behavior or setup changes (`docs/`, README, incremental plan phase status).
- Run tests after API or Infrastructure changes.
- Keep EF migrations in `src/TIKR.Infrastructure/Data/Migrations/` committed (not gitignored).

### Function Inventory — Solo Superpower (lightweight function tracker)

**Intent:** Track individual functions so the whole project works reliably. Every important function must have **proof of function** (a test or verification that exercises it) and should use the **minimal code** required to do its job. This prevents one small unproven detail from silently breaking the overall system and gives real peace of mind without engineering the project into the ground.

**What counts as a trackable function (for TIKR):**
- Public API endpoints / handlers (Program.cs Map*, endpoint classes)
- Blazor page/component logic (Pages/*.razor + @code methods, major Shared components)
- Core service public methods (HybridAiService, DocumentAgentService, Orchestrators, etc.)
- AI tools / function-calling registrations
- Workflow helpers that are part of clerk value paths

**After you create or modify a trackable function:**

1. Run the personal Python scanner (preferred):
   ```bash
   python3 ~/.cursor/skills/function-inventory/scripts/update-function-inventory.py
   ```
   (Or simply `./scripts/update-function-inventory.sh` — it delegates.)

2. Look at the top of the generated inventory:
   - **Summary line**: X tracked | Y with proof | Z without proof
   - The short "**Functions without proof (review these)**" list (usually ~30 items).

3. Curate `docs/action-items.md`:
   - Reference the specific function (e.g. `HybridAiService.TagDocumentAsync` or the mapper methods).
   - Record the actual proof (test name + file, or the manual verification you did).
   - Note the "Minimal Impl Signal".
   - Only add tasks/checkboxes for the ones that matter to your clerk workflows. You do **not** need to prove every internal helper.

4. Update `docs/function-tree.md` only when the major areas or flows change.

**Rules:**
- The `.generated.md` file is **never edited by hand**.
- Before claiming any slice, page, or phase is "done", run the scanner. Look at the summary and the without-proof list. Make sure the functions your change depends on have proof.
- This is your lightweight "peace of mind" tool — it surfaces the ~30 potential gaps so one small unproven detail doesn't break the whole project. Use it for confidence, not bureaucracy. Prefer a single focused test that proves the behavior.

**Two-layer Done Detector approach:**
- **Layer 1 (Function level):** Use the inventory to track and prove individual functions (proof of function + minimal viable code). Only move on when relevant functions have evidence.
- **Layer 2 (Project / Release level):** Once Layer 1 is clean for the scope (0 without proof), complete the **Project-Level Done Detector / Release Readiness Gate** checklist in `docs/action-items.md`.

Agents must help complete **both layers** before recommending that a phase or the overall project is "done". The final gate covers system-level items (tests green, critical workflows, smoke tests, docs, bus-factor coverage, no critical opens, etc.).

After large edits also refresh RAG as usual:
`.venv/bin/python3 scripts/update_tikr_rag_index.py`

See also the personal skill at `~/.cursor/skills/function-inventory/SKILL.md` for the full solo workflow.

## Avoid

- Cloud-only dependencies for core clerk workflows (local-first is the product).
- Committing `.agents/skills/`, `coverage/`, `bin/`, `obj/`, `*.db`, repo-root `/data/`.
- Mixing Syncfusion meta-package with granular packages.

## Key Commands

```bash
dotnet restore
dotnet test TIKR.sln --configuration Release
dotnet format TIKR.sln
trunk check --all
docker compose -f docker/docker-compose.yml up --build
cp docker/.env.example docker/.env   # then edit locally

# After changes to functions (endpoints, pages, services, AI tools):
python3 ~/.cursor/skills/function-inventory/scripts/update-function-inventory.py
# Then curate action-items.md with proof of function for the changed items.

# When function inventory is clean (0 without proof), run the final gate:
./scripts/done-detector.sh
# (completes Layer 1 checks + reminds you to finish the Project-Level checklist)
```

## Related Files

| Path | Purpose |
|------|---------|
| [AGENTS.md](AGENTS.md) | This file — agent rules |
| [docs/incremental-plan.md](docs/incremental-plan.md) | Phased roadmap |
| [docs/ai-tooling.md](docs/ai-tooling.md) | MCP, skills, runtime AI |
| [docs/architecture.md](docs/architecture.md) | System design |
| [docs/action-items.md](docs/action-items.md) | Human+agent overlay (status, verification, checkboxes) |
| [docs/function-inventory.generated.md](docs/function-inventory.generated.md) | Auto-generated inventory (run script to refresh) |
| [docs/function-tree.md](docs/function-tree.md) | Maintained visual Mermaid function tree |
| [scripts/update-function-inventory.sh](scripts/update-function-inventory.sh) | Legacy bash scanner (project specific) |
| `~/.cursor/skills/function-inventory/scripts/update-function-inventory.py` | Personal Python lightweight function tracker (preferred) |
| [.github/workflows/ci.yml](.github/workflows/ci.yml) | Build, test, Docker smoke |
| [.github/workflows/ci.yml](.github/workflows/ci.yml) | Build, test, Trunk lint, Ollama failure triage |
| [docs/dependabot-policy.md](docs/dependabot-policy.md) | Dependabot PR handling |
| [.github/SECURITY.md](.github/SECURITY.md) | Vulnerability reporting |
| [.cursor/rules/tikr.mdc](.cursor/rules/tikr.mdc) | Always-on Cursor rule |
