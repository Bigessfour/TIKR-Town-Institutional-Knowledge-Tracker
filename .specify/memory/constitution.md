# TIKR Constitution

Governing principles for the Town Institutional Knowledge Tracker — a local-first institutional knowledge app for one-person Colorado town clerks.

## Core Principles

### I. Local-First, Clerk-Centered

Core clerk workflows (deadlines, documents, knowledge, audit) must work without cloud dependency. Data stays on the municipal NAS or Windows PC by default. Optional cloud AI (Grok) is a fallback, never a requirement for daily operations.

### II. Minimal, Proven Changes

Every change uses the smallest correct diff. Reuse `TIKR.Shared` DTOs, Infrastructure services, and `TikrApiClient` before adding new abstractions. Trackable functions (API endpoints, Blazor pages, core services, AI tools) need proof of function before a phase is marked done.

### III. Test and CI Gates (NON-NEGOTIABLE)

Before any PR: `dotnet test TIKR.sln --configuration Release` and `trunk check --all`. Merge only when **TIKR CI** and **Trunk** are green. Never commit secrets, `.env`, or key material.

### IV. Layer Boundaries

- **TIKR.Web** — Blazor UX only; no business AI logic in the UI layer.
- **TIKR.Api** — Minimal API, `HybridAiService`, orchestrators, agent tools.
- **TIKR.Infrastructure** — EF Core, storage, migrations (committed, not gitignored).
- **TIKR.Shared** — Domain models and DTOs shared across layers.

### V. Agent-Assisted Development with RAG

Before substantial implementation, search `tikr-rag-mcp` for existing patterns and cite file paths in plans. Refresh the RAG index after large merges. Do not invent APIs that already exist in the codebase.

## Technology Constraints

| Layer    | Technology                                                 |
| -------- | ---------------------------------------------------------- |
| Runtime  | .NET 10 (pinned in `global.json`)                          |
| Frontend | Blazor Interactive Server + individual Syncfusion packages |
| Backend  | Minimal Web API                                            |
| Data     | EF Core, SQLite default, PostgreSQL optional               |
| AI       | Ollama first, optional Grok via `Microsoft.Extensions.AI`  |
| Quality  | Trunk + GitHub Actions CI                                  |

- Use **individual** Syncfusion NuGet packages — never mix the meta `Syncfusion.Blazor` package with granular packages.
- Secrets: `SYNCFUSION_LICENSE_KEY` and `GROK_API_KEY` in `docker/.env` / user-secrets only.
- MCP and editor settings are machine-scoped (`~/.cursor/mcp.json`), not per-repo.

## Development Workflow

1. Read [docs/incremental-plan.md](../../docs/incremental-plan.md) for the active phase and acceptance criteria.
2. Branch from `main`: `feature/*` or `fix/*` — never commit directly to `main`.
3. For new features, use Spec Kit: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement` (see [docs/spec-kit.md](../../docs/spec-kit.md)).
4. Update docs when behavior or setup changes.
5. Run function inventory after endpoint/page/service changes; curate [docs/action-items.md](../../docs/action-items.md).

## Governance

This constitution guides Spec Kit artifacts (specs, plans, tasks) and complements [AGENTS.md](../../AGENTS.md). When they conflict on process, AGENTS.md wins for day-to-day agent rules; this constitution wins for feature-scoped SDD artifacts under `specs/`.

Amendments require updating this file with a semver bump and noting the change in the feature PR or docs.

**Version**: 1.0.0 | **Ratified**: 2026-07-21 | **Last Amended**: 2026-07-21
