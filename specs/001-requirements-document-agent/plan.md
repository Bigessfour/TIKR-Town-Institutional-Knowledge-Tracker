# Implementation Plan: Requirements Manager & Document Agent

**Branch**: `001-requirements-document-agent` | **Date**: 2026-07-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-requirements-document-agent/spec.md`

## Summary

Brownfield closure plan for TIKR Phase 10: the Requirements CRUD hub, NAS-local agent-scan pipeline (stub + licensed Syncfusion), dual PDF archive storage, and ship-ready clerk validation. **Most P1 code paths exist on `main`**; remaining work is proof (NAS licensed smoke, function inventory), Phase 0 sign-off docs, and small gap fixes surfaced by `/speckit-converge`.

Technical approach: extend existing layered .NET 10 solution — no new services or databases. Reuse `DocumentAgentService`, `SyncfusionDocumentAgentOrchestrator`, `Requirements.razor`, and `TikrApiClient`. Gate merges with `dotnet test`, `trunk check`, and blocking Playwright clerk smoke.

## Technical Context

**Language/Version**: C# / .NET 10.0.103 (pinned in `global.json`)

**Primary Dependencies**:

- Blazor Interactive Server + individual Syncfusion Blazor packages (`Syncfusion.Blazor.Grid`, etc.)
- `Syncfusion.DocumentSDK.AI.AgentTools` (Storage Mode extraction)
- EF Core 10 + SQLite (default) / PostgreSQL (optional)
- Ollama via `Microsoft.Extensions.AI` (`SyncfusionDocumentAgentOrchestrator`)
- Serilog, Minimal API (`Program.cs` route groups)

**Storage**:

- Requirements: EF Core `Requirement` entity (`TikrDbContext`)
- Agent uploads: NAS/local volume under `agent-scans/` via `NasAgentDocumentStorage` (+ optional AES via `TIKR_AGENT_STORAGE_KEY`)
- Syncfusion work files: `agent-scans/sf-work/` via `NasSyncfusionDocumentStorage`

**Testing**:

- xUnit + FluentAssertions + coverlet (`coverlet.runsettings`, `scripts/check_coverage.py`)
- bUnit (`RequirementsPageTests`, `RequirementWorkflowHelpersTests`)
- WebApplicationFactory (`DocumentAgentEndpointTests`, `DocumentAgentSyncfusionLicensedTests`)
- Playwright E2E (`tests/e2e/requirements-agent-scan.spec.ts`, `clerk-smoke.spec.ts`) — blocking in TIKR CI

**Target Platform**: Synology NAS (Docker Compose) and Windows clerk PC (`Setup-TIKR.exe` / thumb-drive deploy)

**Project Type**: Local-first web application (Blazor UI + Minimal API)

**Performance Goals**: Agent-scan on town-clerk documents (<10 MB typical PDF) completes in <30s on NAS with Ollama; grid CRUD responsive for ≤500 requirements

**Constraints**:

- Local-first: no cloud required for agent-scan default path (FR-007)
- `USE_SYNCFUSION_AGENT_TOOLS=false` in CI/docker default; licensed path when flag + `SYNCFUSION_LICENSE_KEY`
- Business logic in Api/Infrastructure only — Web is UX (Constitution IV)
- Max upload 100 MB (`Program.cs` agent-scan route)

**Scale/Scope**: Single-clerk municipal deployment; one active feature folder; ~15 seeded CO obligations + clerk-added rows

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle                   | Status | Notes                                                        |
| --------------------------- | ------ | ------------------------------------------------------------ |
| I. Local-First              | ✅ Pass | Agent-scan uses Ollama + NAS storage; Grok not on scan path  |
| II. Minimal, Proven Changes | ✅ Pass | Reuses existing services/DTOs; tasks target gaps only        |
| III. Test & CI Gates        | ✅ Pass | Plan includes test + Trunk + Playwright verification tasks   |
| IV. Layer Boundaries        | ✅ Pass | No AI logic added to Web; orchestration stays Infrastructure |
| V. RAG Before Code          | ✅ Pass | RAG hits cited in plan paths below                           |

**Post-design re-check**: ✅ No violations. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/001-requirements-document-agent/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions
├── data-model.md        # Phase 1 — entities
├── quickstart.md        # Phase 1 — validation scenarios
├── contracts/
│   └── agent-scan-api.md
└── tasks.md             # Phase 2 — /speckit-tasks (next)
```

### Source Code (repository root)

```text
src/
├── TIKR.Web/
│   ├── Components/Pages/Requirements.razor      # Grid CRUD, AI Scan UI, extraction badge
│   ├── Helpers/RequirementWorkflowHelpers.cs    # ApplyAgentExtraction, CSV, urgency
│   └── Services/TikrApiClient.cs                # ScanDocumentWithAgentAsync
├── TIKR.Api/
│   └── Program.cs                               # POST /api/ai/agent-scan, requirements CRUD
├── TIKR.Infrastructure/
│   ├── Services/DocumentAgentService.cs         # Orchestration + dual archive
│   ├── Services/SyncfusionDocumentAgentOrchestrator.cs
│   ├── Services/SyncfusionDocumentAgentExtractor.cs
│   ├── Services/NasAgentDocumentStorage.cs
│   └── DependencyInjection.cs                   # Backend selection by USE_SYNCFUSION_AGENT_TOOLS
├── TIKR.SyncfusionDocuments/
│   └── SyncfusionDocumentGenerationService.cs # CreateAgentArchivePdfAsync (stamp)
└── TIKR.Shared/
    ├── Entities/Requirement.cs
    ├── DTOs/DocumentAgentDto.cs               # DocumentAgentResult
    └── Interfaces/IDocumentAgentService.cs

tests/
├── TIKR.Api.Tests/Endpoints/DocumentAgent*.cs
├── TIKR.Infrastructure.Tests/Services/DocumentAgent*.cs
├── TIKR.Web.Tests/Helpers/RequirementWorkflowHelpersTests.cs
├── fixtures/agent-scan/                         # txt, pdf, docx fixtures
└── e2e/requirements-agent-scan.spec.ts
```

**Structure Decision**: Standard TIKR four-project layout. Feature touches Web (UX), Api (endpoint), Infrastructure (agent pipeline), SyncfusionDocuments (PDF archive). No new projects.

## Implementation Phases (brownfield)

### Phase A — Verify shipped baseline (mostly done)

| ID  | Requirement         | Existing implementation                                | Verification                      |
| --- | ------------------- | ------------------------------------------------------ | --------------------------------- |
| A1  | FR-001 CRUD hub     | `Requirements.razor`, requirements API in `Program.cs` | bUnit + API tests                 |
| A2  | FR-002 agent-scan   | `POST /api/ai/agent-scan`                              | `DocumentAgentEndpointTests`      |
| A3  | FR-003 apply flow   | `ApplyAgentExtraction`, dialog pre-fill                | `RequirementWorkflowHelpersTests` |
| A4  | FR-004 NAS storage  | `NasAgentDocumentStorage`, crypto                      | `NasAgentDocumentStorageTests`    |
| A5  | FR-005 source badge | `FormatAgentScanMessage`, CSS badges                   | `Requirements.razor` + bUnit      |
| A6  | FR-012 doc download | `GET /api/documents/{id}/content`                      | Api + Documents UI tests          |

### Phase B — Licensed Syncfusion path (code done; proof pending)

| ID  | Requirement                | Implementation                                            | Remaining                                                          |
| --- | -------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ |
| B1  | FR-006 licensed extraction | `SyncfusionDocumentAgentExtractionBackend` + orchestrator | Manual NAS smoke with `USE_SYNCFUSION_AGENT_TOOLS=true`            |
| B2  | SC-004 UsedSyncfusionTools | `DocumentAgentSyncfusionLicensedTests`                    | Ensure CI secret path documented in quickstart                     |
| B3  | FR-008 dual archive        | `DocumentAgentService` + `CreateAgentArchivePdfAsync`     | Add/verify test for `ProcessedStoragePath` when generator succeeds |

### Phase C — Ship closure (Phase 0 + spec SC-003/SC-005)

| ID  | Requirement                | Action                                                                                                   |
| --- | -------------------------- | -------------------------------------------------------------------------------------------------------- |
| C1  | SC-003 Playwright blocking | Confirm `requirements-agent-scan.spec.ts` green in TIKR CI on feature branch                             |
| C2  | SC-005 function inventory  | Run inventory; curate `docs/action-items.md` proofs for changed agent/requirements functions             |
| C3  | US-4 walkthrough           | Complete Phase 0 PR #3 docs + PR #4 recorded Deb walkthrough per incremental-plan                        |
| C4  | FR-009 table mapping       | Review `StructuredTables` → `ApplyAgentExtraction`; improve if JSON tables not mapped to distinct fields |

### Phase D — Explicitly deferred (out of spec scope)

- Requirements Phase 2 UI (TreeGrid, Stepper, hierarchy)
- Documents delete undo toast
- Voice STT, IMAP ingestion, Phase 6 Smart Components

## Complexity Tracking

> No constitution violations requiring justification.

## Generated Artifacts

| Artifact     | Path                                                         |
| ------------ | ------------------------------------------------------------ |
| Research     | [research.md](./research.md)                                 |
| Data model   | [data-model.md](./data-model.md)                             |
| API contract | [contracts/agent-scan-api.md](./contracts/agent-scan-api.md) |
| Quickstart   | [quickstart.md](./quickstart.md)                             |

## Next Step

Run **`/speckit-tasks`** to generate ordered `tasks.md` from this plan and the spec acceptance criteria.
