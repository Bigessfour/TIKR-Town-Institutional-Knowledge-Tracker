<!-- markdownlint-disable MD033 MD047 -->
# TIKR Action Items (Human + Agent Overlay)

**Generated inventory source:** [function-inventory.generated.md](./function-inventory.generated.md)  
**Visual tree:** [function-tree.md](./function-tree.md)  

**Update rule (see AGENTS.md):** After creating or modifying trackable functions:
1. Run `./scripts/update-function-inventory.sh` (delegates to your personal Python scanner).
2. Check the **Summary** and the "**Functions without proof**" list at the top of the generated file.
3. Add/update entries here for the unproven functions that matter. Record the actual proof (test) or verification + minimal-impl note.
4. Refresh RAG index: `.venv/bin/python3 scripts/update_tikr_rag_index.py`

This file owns **status, checkboxes, priorities, verification evidence**. The generated file is the raw auto-detected list only (never edit by hand). Focus on the ~30 without proof so no small detail breaks the whole system.

---

## Current Priorities (Phase 0 + 10C closure)

- [ ] Phase 0 PR #4 — Health UI closure + Done Detector sign-off (see incremental-plan.md)
- [ ] Manual NAS licensed smoke for Syncfusion agent tools (post #36)
- [ ] 10C-C extraction source badge in UI (UsedSyncfusionTools)
- [ ] Playwright E2E as required CI gate

---

## API Endpoints — Status & Verification

Reference lines from generated inventory (re-run script to refresh).

**Current functions without direct proof (from latest scan — only 9 items):**
- [ ] `AuthEndpoints.MapAuthEndpoints` (src/TIKR.Api/AuthEndpoints.cs:11) — internal mapper
- [ ] `CouncilPacketEndpoints.BuildCouncilPacketRequirementsAsync` (src/TIKR.Api/CouncilPacketEndpoints.cs:116)
- [ ] `CouncilPacketEndpoints.LoadRequirementLinksAsync` (src/TIKR.Api/CouncilPacketEndpoints.cs:99)
- [ ] `CouncilPacketEndpoints.MapRequirement` (src/TIKR.Api/CouncilPacketEndpoints.cs:147)
- [ ] `ThemeService.InitializeAsync` / `SetThemeAsync` (src/TIKR.Web/Services/ThemeService.cs)
- [ ] `TikrDbContextFactory.CreateDbContext`
- [ ] `IDocumentAgentExtractionBackend.AgentExtractionResult` (interface)
- [ ] `RequirementUrgencyHelper.GetLabel`

For each, either add a focused test or document "covered by <public test>" here.

- [x] GET /health — basic health. Verified: HealthEndpointTests.cs, docker smoke in CI.
- [x] GET /api/system/local-status — NAS + AI status footer. Used on every page.
- [x] GET /api/system/document-sdk-status — license flag for agent tools.
- [x] Full requirements CRUD + document links (`/api/requirements*`) — covered by RequirementsEndpointTests + bUnit + Playwright 05b.
- [x] Documents + generate group (council-agenda, meeting-minutes, clerk-memo, council-packet, compliance-report, converts) — CouncilPacketEndpointTests + Syncfusion tests.
- [x] Knowledge CRUD + auto-embed (`/api/knowledge*`).
- [x] Audit read (`/api/audit`).
- [x] AI surface: status, dashboard-priorities, tag-document, ask-advanced, semantic-search*, embed-*, agent-scan. See endpoint tests + HybridAi*Tests.
- [x] Auth group (optional) (`/api/auth/*`) — AuthEndpointTests, when TIKR_ADMIN_* set.
- [ ] Any newly detected endpoints (add here with test + manual curl evidence when added).

**Global verification for endpoints:**
- `dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Endpoint"`
- Docker smoke: `docker compose ... up` + `curl -sf http://localhost:5000/health`
- Playwright specs in tests/e2e/ (requirements-agent-scan.spec.ts, clerk-smoke.spec.ts)

---

## Blazor Pages & Major Components — Status

- [x] `/` (Home.razor) — Dashboard: urgency pills, AI card, quick actions, mini grid, activity. Phase 0 + #33.
- [x] `/requirements` (Requirements.razor) — Grid CRUD, CSV, AI Scan, packet print, confirm delete. 10A/10B/Phase 0.
- [x] `/documents` (Documents.razor) — Upload, TreeView, semantic search, download, preview split. Phase 9.
- [x] `/vault` (Vault.razor) — Tabs, RTE, Copy for New Clerk, voice sim. Phase 5/9.
- [x] `/assistant` (Assistant.razor) — SfAIAssistView + RAG (doc + knowledge semantic prepend). Phase 9.
- [x] `/calendar`, `/settings`, `/settings/users`, `/account`, `/login`.
- [x] Legacy `/knowledge` (redirects to /vault); NotFound, Error.
- Shared surfaces (PageHelp on main pages, ConfirmDelete + undo toast, TikrStatusFooter, offline banner, keyboard shortcuts) — Phase 0 #33/#34.

**Verification:** bUnit tests (Web.Tests/Components/*PageTests.cs), manual + Playwright smoke.

---

## Core Services & Public Methods — Status

- [x] `IHybridAiService` + impl (8 public methods): Tag, Priorities, AskAdvanced (Grok gate), Status, `SemanticSearch*` (docs + knowledge), `Embed*`. Phase 3/9. Tests: `HybridAiService*Tests.cs`.
- [x] `IDocumentAgentService` + `DocumentAgentService.ProcessUploadAsync` (stub + Syncfusion path via backend). 10B/10C. Tests + fixtures.
- [x] `SyncfusionDocumentAgentOrchestrator.TryExtractAsync` + `SyncfusionDocumentAgentToolRegistry` (A3 orchestration + full clerk tool set). feature/phase10c-document-tool-coverage.
- Supporting: AuditService, file storage impls (Local/Nas*), GrokService, text extraction, crypto for agent storage.

See generated inventory + interfaces in TIKR.Shared.

---

## AI Tools / Orchestrators — Status

- [x] Tool registry registers PDF/Word/Excel/PPT/OfficeToPDF/DataExtraction Storage Mode tools.
- [x] Orchestrator: Ollama + Microsoft.Extensions.AI function invocation loop (when flag enabled).
- Fallback: deterministic extractor (A2 on main).
- Exposed via `POST /api/ai/agent-scan`.

**Verification:** Infrastructure tests (SyncfusionDocumentAgent*Tests), licensed smoke workflow, 05b diagram.

---

## Key Workflows — Status + Evidence

- [x] **AI Scan flow** (Requirements → agent → prefill dialog)
  - Files: Requirements.razor (AI Scan button), DocumentAgentService, TikrApiClient.Scan..., RequirementWorkflowHelpers.ApplyAgentExtraction + FormatAgentScanMessage.
  - Verified: PR #31 (stub), #35/#36 (Syncfusion path + E2E), tests/fixtures/agent-scan/, requirements-agent-scan.spec.ts, docker curl.
  - Evidence: `dotnet test ...DocumentAgent...` + Playwright against licensed or stub.

- [x] **Semantic Search + RAG** (embed on write, retrieve in Assistant/Documents)
  - Files: HybridAiService (cosine + snippet), embeddings on Document/KnowledgeEntry, TikrApiClient, Assistant.razor (prepend top-K), Documents.razor (semantic toggle).
  - Phase 9 complete (core); PDF preview deferred.
  - Evidence: HybridAiServiceSemanticSearchTests, HybridAiServiceVaultRagTests, diagrams/05c + 05d.

- [x] **Council Packet + generation flows** (generate + persist to NAS + audit)
  - CouncilPacketEndpoints + IDocumentGenerationService (SyncfusionDocs).
  - Evidence: CouncilPacketEndpointTests + generation tests.

- [x] Dashboard priorities + urgency (RequirementUrgencyHelper + Hybrid + UI pills).
- [x] Knowledge CRUD + auto-embed on POST/PUT.
- [ ] Full document download streaming (stub in UI) — open item.
- [ ] Documents delete undo parity — open.

**Cross-cutting verification:** `dotnet test`, trunk, Playwright e2e against `docker compose`, manual NAS run.

---

## Meta / Process

- Always keep generated header comment intact.
- Prefer small PRs; link relevant diagram (05*) and test when touching a workflow.
- Before sign-off / Done Detector: re-run script + `./scripts/done-detector.sh`, ensure action-items + tree reflect reality (including Project-Level gate), RAG index current.
- Related living docs: incremental-plan.md, requirements-working-tree.md (Requirements-specific), diagrams/.

---

## Project-Level Done Detector / Release Readiness Gate

**IMPORTANT:** Review and complete this gate **only after** the function inventory is fully clean (Summary shows "0 without proof" at the top of `docs/function-inventory.generated.md`).

This is the final system-level verification layer. Individual functions proven (Layer 1) + this gate (Layer 2) = confident release / phase done.

### Checklist

- [ ] Function inventory clean: run `./scripts/update-function-inventory.sh` (or Python directly) → **0 without proof**. Then run `./scripts/done-detector.sh` for combined check.
- [ ] Full test suite green:
  ```bash
  dotnet test TIKR.sln --configuration Release
  ```
- [ ] Critical clerk workflows verified end-to-end:
  - AI Scan flow (Requirements → agent extraction → prefill)
  - Semantic search + RAG context in Assistant and Documents
  - Council packet / document generation + NAS persist + audit
  - Requirements CRUD + links + print/export
  - Document upload/tag/embed + download
  - Vault knowledge + "if I'm gone" content complete
  **Verification:** manual smoke + relevant `tests/e2e/*.spec.ts` + docker compose
- [ ] Docker / local / NAS smoke tests:
  ```bash
  docker compose -f docker/docker-compose.yml up --build
  # then: curl health, agent-scan fixture, web pages
  ```
- [ ] Key documentation current: AGENTS.md, incremental-plan.md, action-items.md, architecture/diagrams, function-tree.md
- [ ] "If I'm gone" / bus-factor coverage complete (see Vault + requirements-working-tree)
- [ ] No critical open action items (all high/medium in this file resolved or documented)
- [ ] RAG index refreshed:
  ```bash
  .venv/bin/python3 scripts/update_tikr_rag_index.py
  ```
- [ ] Lint + style clean:
  ```bash
  trunk check --all
  dotnet format TIKR.sln
  ```
- [ ] PR / branch ready: green CI (TIKR CI + Trunk), no secrets, docs updated

Once all checked, a phase or the project can be declared "done" with high confidence.

---

*This file is intentionally small and focused because the raw list is auto-generated.*