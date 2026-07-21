# Research: Requirements Manager & Document Agent

**Feature**: `001-requirements-document-agent` | **Date**: 2026-07-21

All technical context items resolved from existing TIKR codebase and docs — no open NEEDS CLARIFICATION.

## Decision 1: Extraction backend selection

**Decision**: Feature flag `USE_SYNCFUSION_AGENT_TOOLS` switches `IDocumentAgentExtractionBackend` between `StubDocumentAgentExtractionBackend` (plain-text + heuristics) and `SyncfusionDocumentAgentExtractionBackend` (licensed PDF/Word/Excel/PPT via Storage Mode).

**Rationale**: CI and default Docker run without Syncfusion license; production NAS enables licensed path. Matches Constitution I (local-first) and existing `DependencyInjection.cs` pattern.

**Alternatives considered**:

- Cloud-only extraction (rejected — violates local-first)
- Always Syncfusion (rejected — breaks CI and unlicensed dev)

## Decision 2: Ollama orchestration vs deterministic-only

**Decision**: Keep `SyncfusionDocumentAgentOrchestrator` (A3) as optional enhancement when flag enabled; deterministic `SyncfusionDocumentAgentExtractor` remains fallback inside backend.

**Rationale**: Already implemented on `main`; orchestrator selects tools via `Microsoft.Extensions.AI` without cloud dependency.

**Alternatives considered**:

- Full Microsoft Agent Framework separate service (deferred — out of spec scope)
- Remove orchestrator, extractor only (rejected — loses tool-selection for multi-format clerk docs)

## Decision 3: Dual PDF archive storage

**Decision**: On licensed scan success, `DocumentAgentService` saves original under `agent-scans/` then generates stamped archive via `IDocumentGenerationService.CreateAgentArchivePdfAsync` and saves processed copy with `.ai-archive.pdf` suffix.

**Rationale**: Satisfies FR-008 / US-3 institutional knowledge requirement; best-effort with logged warning on failure (original preserved).

**Alternatives considered**:

- Single copy only (rejected — loses audit trail)
- Cloud object storage (rejected — NAS-local requirement)

## Decision 4: Apply flow and table mapping

**Decision**: After a successful agent-scan, the Requirements page **auto-opens** the Add requirement dialog pre-filled via `RequirementWorkflowHelpers.ApplyAgentExtraction` (same helper as an explicit Apply). Clerk still reviews and must Save — no auto-persist. Structured tables append to `Description` when `StructuredTables` differs from `ExtractedText`.

**Rationale**: One fewer click for Deb after upload; still satisfies FR-003 (no auto-save without review). Auto-open is the accepted clerk UX (shipped + Playwright-covered).

**Alternatives considered**:

- Banner-only + explicit Apply button before dialog (rejected for ship — extra step; may return in Phase 2 polish)
- New Requirement columns for table JSON (deferred — Phase 2 schema)
- Auto-save requirement on scan (rejected — spec edge case: clerk must confirm)

**Accepted UX note (T040)**: Auto-open create dialog after scan is intentional; do not treat as a bug vs the earlier “banner then Apply” wording.

## Decision 5: Testing strategy

**Decision**: Three tiers — unit (Infrastructure/Web helpers), API integration (WebApplicationFactory + fixtures), E2E Playwright against Docker stack. Licensed PDF tests gated on `SYNCFUSION_LICENSE_KEY` in CI optional job / local.

**Rationale**: Constitution III; existing test files prove pattern; SC-003 requires blocking clerk smoke.

**Alternatives considered**:

- E2E only (rejected — slow feedback, harder to debug agent pipeline)
- No licensed path tests (rejected — SC-004 requires `UsedSyncfusionTools=true` proof)

## Decision 6: Brownfield Spec Kit scope

**Decision**: This feature spec/plan documents Phase 10 + Phase 0 closure gaps; `/speckit-converge` runs after tasks/implement to append only remaining work.

**Rationale**: Avoid re-specifying done MVP; align with Spec Kit evolving-specs flow-forward model for follow-up slices.

**Alternatives considered**:

- Retro-spec entire app (rejected — incremental-plan already covers done phases)
- Greenfield rewrite of Requirements (rejected — YAGNI)
