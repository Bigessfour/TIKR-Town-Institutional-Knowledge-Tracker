# Spec 003 — Syncfusion Document Management (repo-wide)

**Created:** 2026-07-27  
**Status:** Research complete · Implementation not started  
**Package pin:** Syncfusion Blazor / Document SDK **34.1.32**

## Goal

Review how Syncfusion **intends** document management to be used and displayed in a Blazor application (greenfield, assistant-aligned), compare that to TIKR today, and produce a single source of truth for gaps, opportunities, and a phased roadmap that **mirrors documented Syncfusion methods**.

## Documents in this folder

| File | Purpose |
|------|---------|
| [research.md](research.md) | Syncfusion documentation synthesis: FileManager, PDF Viewer, Word, Spreadsheet, Document SDK AI Agent Tools, Smart PDF Viewer, greenfield architecture |
| [gap-analysis.md](gap-analysis.md) | Capability matrix: Syncfusion-intended vs TIKR actual; prioritized opportunities |
| [spec.md](spec.md) | Target product specification (user stories, acceptance, non-goals) |
| [plan.md](plan.md) | Phased implementation plan (P0–P3) aligned with clerk UX |
| [../docs/diagrams/05f-document-management-sf.mmd](../../docs/diagrams/05f-document-management-sf.mmd) | E2E Mermaid of intended document management flow |

## Related existing docs

- [docs/sf-document-agent-tools.md](../../docs/sf-document-agent-tools.md) — backend Agent Tools (Storage Mode)
- [docs/syncfusion-control-audit.md](../../docs/syncfusion-control-audit.md) — page-level Sf* control audit
- [docs/syncfusion-e2e-audit-plan.md](../../docs/syncfusion-e2e-audit-plan.md) — quarterly E2E walk
- [docs/nas-agent-tools-setup.md](../../docs/nas-agent-tools-setup.md) — NAS licensed smoke
- Documents UI: `src/TIKR.Web/Components/Pages/Documents.razor`

## Decision summary (executive)

| Question | Recommendation |
|----------|----------------|
| Separate document management UI? | **Yes, as modes on `/documents`** — Library browse · Full-screen workspace · (optional) File Manager browse of NAS tree — not a second nav item for clerks |
| Follow Syncfusion FileManager? | **Yes for physical NAS folder ops** when clerks need Explorer-like rename/move; keep metadata Grid for AI tags / semantic search |
| PDF annotations save-back? | **P0 gap** — toolbar enabled; no `GetDocumentAsync` → `PUT /content` |
| Word/Excel save? | **MVP exists** — align more closely with Syncfusion `SaveAsBlobAsync` / `SaveAsStreamAsync` patterns |
| Smart PDF Viewer? | **P2 opportunity** (summarize/redact/fill with local Ollama via `IChatInferenceService`) |

## How to use this package

1. Read **research.md** for Syncfusion’s documented greenfield shape.  
2. Read **gap-analysis.md** for what TIKR already does well vs missing.  
3. Treat **spec.md** as the product contract for implementation PRs.  
4. Execute **plan.md** phases; update gap matrix when a phase ships.
