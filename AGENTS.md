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
```

Keep ≤3–4 active MCP servers. See [docs/ai-tooling.md](docs/ai-tooling.md) for `sf-blazor-mcp`, Microsoft Learn, Ollama.

## Code Conventions

- Match [.editorconfig](.editorconfig) and existing patterns in `src/` and `tests/`.
- Minimal diffs — reuse `TIKR.Shared` DTOs, Infrastructure services, `TikrApiClient`.
- Business AI logic stays in **TIKR.Api** (`HybridAiService`); Web chat is UX only.
- Comments only for non-obvious business or compliance logic (audit trail, Grok gating).

## Always

- Update docs when behavior or setup changes (`docs/`, README, incremental plan phase status).
- Run tests after API or Infrastructure changes.
- Keep EF migrations in `src/TIKR.Infrastructure/Data/Migrations/` committed (not gitignored).

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
```

## Related Files

| Path | Purpose |
|------|---------|
| [AGENTS.md](AGENTS.md) | This file — agent rules |
| [docs/incremental-plan.md](docs/incremental-plan.md) | Phased roadmap |
| [docs/ai-tooling.md](docs/ai-tooling.md) | MCP, skills, runtime AI |
| [docs/architecture.md](docs/architecture.md) | System design |
| [.github/workflows/ci.yml](.github/workflows/ci.yml) | Build, test, Docker smoke |
| [.github/workflows/trunk-check.yaml](.github/workflows/trunk-check.yaml) | Lint + secret scan |
| [docs/dependabot-policy.md](docs/dependabot-policy.md) | Dependabot PR handling |
| [.github/SECURITY.md](.github/SECURITY.md) | Vulnerability reporting |
| [.cursor/rules/tikr.mdc](.cursor/rules/tikr.mdc) | Always-on Cursor rule |
