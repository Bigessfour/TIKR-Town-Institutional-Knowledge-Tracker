# TIKR Architecture Diagrams (Mermaid)

Living diagram set for TIKR end-to-end features. Sources are plain Mermaid (`.mmd`) files; rendered copies are embedded in [architecture.md](../architecture.md).

**Update rule:** When a feature ships, changes status, or gains a new API route, update the relevant `.mmd` file and the matching section in `architecture.md` in the same PR.

## Diagram index

| File | Type | Question it answers |
|------|------|-------------------|
| [01-system-context.mmd](01-system-context.mmd) | C4Context | Who uses TIKR and what external systems exist? |
| [02-containers.mmd](02-containers.mmd) | C4Container | What runs in Docker and how do containers talk? |
| [03-clerk-feature-map.mmd](03-clerk-feature-map.mmd) | Flowchart | What can the clerk do in the UI (with status)? |
| [04-api-surface.mmd](04-api-surface.mmd) | Flowchart | Which API backs each feature? |
| [05a-clerk-smoke.mmd](05a-clerk-smoke.mmd) | Sequence | Dashboard load + local footer (Playwright smoke) |
| [05b-requirements-agent-scan.mmd](05b-requirements-agent-scan.mmd) | Sequence | Requirements AI Scan upload → pre-fill dialog |
| [05c-assistant-rag.mmd](05c-assistant-rag.mmd) | Sequence | Assistant chat with doc + vault RAG context |
| [05d-document-lifecycle.mmd](05d-document-lifecycle.mmd) | Sequence | Upload → tag → embed → semantic search |
| [05e-auth-flow.mmd](05e-auth-flow.mmd) | Sequence | Optional multi-user login + protected API |
| [06-deployment.mmd](06-deployment.mmd) | C4Deployment | Synology NAS topology and data volumes |

## Status legend (diagram 03)

| Class | Meaning |
|-------|---------|
| `done` | Shipped on `main` |
| `wip` | In PR or partial UI |
| `stub` | Placeholder in code (clerk-visible gap) |
| `defer` | vNext / post-MVP (Phase 6+, deferred Phase 9) |

## Local render (optional)

```bash
npm install -g @mermaid-js/mermaid-cli
mmdc -i docs/diagrams/01-system-context.mmd -o docs/diagrams/out/01-system-context.svg
```

GitHub renders fenced ` ```mermaid ` blocks in Markdown without CLI.

## Key code references

| Area | Path |
|------|------|
| API routes | `src/TIKR.Api/Program.cs`, `AuthEndpoints.cs` |
| Clerk pages | `src/TIKR.Web/Components/Pages/` |
| Navigation | `src/TIKR.Web/Components/Layout/MainLayout.razor` |
| Docker | `docker/docker-compose.yml` |
| Roadmap | `docs/incremental-plan.md` |
| Playwright E2E | `tests/e2e/` |
