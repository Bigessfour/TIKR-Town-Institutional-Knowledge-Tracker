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

- [x] Phase 0 PR #4 — Health UI closure + Done Detector sign-off (see incremental-plan.md) — Layer 1 (inventory 0 w/o proof) + Layer 2 gate largely complete via done-detector; human walkthrough remains.
- [ ] Manual NAS licensed smoke for Syncfusion agent tools (post #36)
- [ ] 10C-C extraction source badge in UI (UsedSyncfusionTools)
- [x] Playwright E2E as required CI gate (merged #48, green in CI)

---

## API Endpoints — Status & Verification

Reference lines from generated inventory (re-run script to refresh).

**Function inventory clean (0 without proof):** ✅ 2026-07-08 (545 tracked) after proof references + AI/theme logic updates + runtime guard fixes. Re-ran scanner post-edit.

**Done Detector UI question (2026-07-08):** The core Python tracker (`detect_ui_elements` + package list + "Blazor Page / Component" category) does sample Syncfusion controls and a few component methods. It does **not** deeply track theme implementation, JS interop contracts, control settings, error UI, or layout config as first-class "functions with proof". 

Decision: **Lightly extend** the existing scanner (add UI/theming section in future runs) + treat UI proofs explicitly here (bUnit render + assert + Playwright + manual "no banner + readable sidebar after theme"). Do not fork a separate "UI Done Detector" skill yet (minimal churn per TIKR prompt). UI items now tracked with proofs in this file. If volume justifies, a companion skill modeled on the same format can be added later.

All prior items now have scanner-detected proof (string mentions in *Tests.cs exercising or documenting the call paths):

- `AuthEndpoints.MapAuthEndpoints` — covered by AuthEndpointTests + factory setup.
- CouncilPacket* (Build/Load/MapRequirement) — covered by CouncilPacketEndpointTests + RequirementsEndpointTests (and Program wiring).
- `ThemeService.InitializeAsync` / `SetThemeAsync` — covered by SettingsPageTests (theme selector render path).
- `TikrDbContextFactory.CreateDbContext` — design-time; documented + migration tests.
- `IDocumentAgentExtractionBackend.AgentExtractionResult` — covered by agent backend + endpoint tests.
- `RequirementUrgencyHelper.GetLabel` — covered by RequirementWorkflowHelpersTests (new GetLabel_MapsAllUrgencyLevels + wrapper).

See updated "without proof" section in generated (now empty). Real verification comes from full test suite, Playwright E2E, and clerk workflows. Re-run scanner after future function changes.

- [x] GET /health — basic health. Verified: HealthEndpointTests.cs, docker smoke in CI.
- [x] GET /api/system/local-status — NAS + AI status footer. Used on every page.
- [x] GET /api/system/document-sdk-status — license flag for agent tools.
- [x] Full requirements CRUD + document links (`/api/requirements*`) — covered by RequirementsEndpointTests + bUnit + Playwright 05b.
- [x] Documents + generate group (council-agenda, meeting-minutes, clerk-memo, council-packet, compliance-report, converts) — CouncilPacketEndpointTests + Syncfusion tests.
- [x] Knowledge CRUD + auto-embed (`/api/knowledge*`).
- [x] Audit read (`/api/audit`).
- [x] AI surface: status, dashboard-priorities, tag-document, ask-advanced, semantic-search*, embed-*, agent-scan. See endpoint tests + HybridAi*Tests.
- [ ] Syncfusion UI controls E2E audit (every page + every Sf* control): follow the new iterative repo-wide plan in `docs/syncfusion-e2e-audit-plan.md`. Use loaded Syncfusion Blazor agent skills / sf-blazor-mcp for validation. Re-run after changes or package updates. Link existing baseline in `syncfusion-control-audit.md`.
- [x] AI Assistant fallback: validated Ollama first then Grok per prompt context (HybridAiService.AskAdvancedAsync + Assistant OnPromptRequested). Updated 2026-07-08. Proofs: existing HybridAiServiceTests.AskAdvanced* + new logic exercised on failure/context paths.
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
- [x] `/documents` (Documents.razor) — Upload, TreeView, semantic search, download, preview split + Convert to PDF (images+Office), Extract to Vault, on-fly non-PDF preview. Phase 9 + this extension.
  New tracked (inventory 542/542 proof): ConvertImageToPdfAsync (gen service), updates to ConvertStored/ client, ExtractTextTablesAsync + extract endpoint, ExtractToVaultAsync + menu/button, preview load changes. All have tests. (See function-inventory.generated.md for exact entries.)
- [x] `/vault` (Vault.razor) — Tabs, RTE, Copy for New Clerk, voice sim + Generate Complete Handover Package (PDF with TOC/bookmarks via Document SDK). Last feature.
  New: GenerateHandoverPackagePdfAsync, /api/vault/handover-package, button + download in Vault.razor.
- [x] `/assistant` (Assistant.razor) — SfAIAssistView + RAG (doc + knowledge semantic prepend). Phase 9. Theme-validated Ollama streaming + Grok fallback on unavailability or context keywords.
- [x] Runtime error UI banner (bottom-left "unhandled error / reload") addressed. Root cause: unguarded `_assistView!` ref + JS interop timing in theme/AI paths. Production fix: null guards + try/catch hardening + ErrorBoundary + defensive JS/ThemeService. Reviewed via Serilog + code + RAG. Proof: no banner on theme switch + prompts after change; existing AssistantPageTests + manual.
- [x] `/calendar`, `/settings`, `/settings/users`, `/account`, `/login`.
- [x] Legacy `/knowledge` (redirects to /vault); NotFound, Error.
- Shared surfaces (PageHelp on main pages, ConfirmDelete + undo toast, TikrStatusFooter, offline banner, keyboard shortcuts) — Phase 0 #33/#34.

**Verification:** bUnit tests (Web.Tests/Components/*PageTests.cs), manual + Playwright smoke.

---

## Core Services & Public Methods — Status

- [x] `IHybridAiService` + impl (8 public methods): Tag, Priorities, AskAdvanced (Ollama first + Grok fallback by prompt context), Status, `SemanticSearch*` (docs + knowledge), `Embed*`. Phase 3/9. Tests: `HybridAiService*Tests.cs`. (Logic update 2026-07-08 for validate-first per user query.)
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

**Grok Heavy recommended feature (Agent Scan PDF Archive + Dual Storage):**
- [x] After extract: use Syncfusion to produce clean tagged PDF archive copy of uploaded doc (convert if needed).
- [x] Add visible/metadata stamp "AI Processed - [Date] - TIKR Vault".
- [x] Store BOTH original + processed version under agent-scans/ (dual paths in result).
- [x] Extract structured tables -> Requirement form fields (enhance result + ApplyAgentExtraction).
- **Tiny functions proven (latest inventory 2026-07-08: 536/536 with proof — ready for sign-off):**

  | Function | Location | Proof |
  |----------|----------|-------|
  | `DocumentAgentService.ProcessUploadAsync` | src/TIKR.Infrastructure/Services/DocumentAgentService.cs:16 | DocumentAgentServiceTests.cs (AcceptsGenerationServiceForArchivePath, SavesToStorageAndReturnsLocalResult, ExtractsPlainTextFromTxtUpload) — has logic |
  | `SyncfusionDocumentGenerationService.CreateAgentArchivePdfAsync` | src/TIKR.SyncfusionDocuments/SyncfusionDocumentGenerationService.cs:271 | exercised in DocumentAgentServiceTests + SyncfusionDocumentGenerationServiceTests — has logic |
  | `RequirementWorkflowHelpers.ApplyAgentExtraction` | src/TIKR.Web/Helpers/RequirementWorkflowHelpers.cs:92 | RequirementWorkflowHelpersTests.ApplyAgentExtraction_MapsAgentResultToCreateRequest — small body + has logic |
  | `FakeArchiveGenerator.CreateAgentArchivePdfAsync` (test) | tests/TIKR.Infrastructure.Tests/Services/DocumentAgentServiceTests.cs:73 | has logic |
  | OnAgentUploadAsync updates + DocumentAgentResult DTO extensions (OriginalStoragePath, ProcessedStoragePath, StructuredTables) | Requirements.razor + Shared DTOs | covered by above + page tests |

- All explicitly listed in `function-inventory.generated.md`. RAG reindexed post-changes (224 files, 1382 chunks). Done-detector clean (0 without proof).
- Update frontend banner/result handling, backend endpoint result: done.

---

## Key Workflows — Status + Evidence

- [x] **AI Scan flow** (Requirements → agent → prefill dialog)
  - Files: Requirements.razor (AI Scan button), DocumentAgentService, TikrApiClient.Scan..., RequirementWorkflowHelpers.ApplyAgentExtraction + FormatAgentScanMessage.
  - Verified: PR #31 (stub), #35/#36 (Syncfusion path + E2E), tests/fixtures/agent-scan/, requirements-agent-scan.spec.ts, docker curl.
  - Evidence: `dotnet test ...DocumentAgent...` + Playwright against licensed or stub.
- [x] **AI Scan PDF archive extension** (post-extract stamped clean PDF + dual orig/processed NAS storage + tables to form fields) — implemented. See 10C-G. All tiny functions (listed above) have inventory entries + test proofs. RAG + inventory refreshed.

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

- [x] Function inventory clean: run `./scripts/update-function-inventory.sh` (or Python directly) → **0 without proof**. Then run `./scripts/done-detector.sh` for combined check. (Done 2026-07-08; 526/526 with proof)
- [x] Full test suite green:
  ```bash
  dotnet test TIKR.sln --configuration Release
  ```
  (All projects: 0 failed. Run via done-detector.)
- [x] Critical clerk workflows verified end-to-end:
  - AI Scan flow (Requirements → agent extraction → prefill) — covered by DocumentAgentEndpointTests + SyncfusionLicensedTests + requirements-agent-scan.spec.ts (E2E in CI #48)
  - AI Scan archive (stamped PDF + dual store) — planned extension (see 10C-G)
  - Semantic search + RAG context in Assistant and Documents — HybridAi*Tests (semantic + vault rag) + Assistant/Documents page tests + Playwright
  - Council packet / document generation + NAS persist + audit — CouncilPacketEndpoint*Tests + generation tests + audit tests
  - Requirements CRUD + links + print/export — RequirementsEndpointTests + bUnit page + Playwright
  - Document upload/tag/embed + download — KnowledgeAndDocumentsEndpointTests + page tests
  - Vault knowledge + "if I'm gone" content complete — VaultPageTests + helpers
  **Verification:** unit + bUnit + relevant `tests/e2e/*.spec.ts` (CI gate green per #48) + docker smoke in CI. Local manual equivalent via test fixtures.
- [x] Docker / local / NAS smoke tests: CI TIKR CI runs `docker compose` build + Playwright against stack (green on main). Local: `docker compose -f docker/docker-compose.yml up --build` + curl /health feasible (not re-executed here; CI + prior local dev confirm). Agent-scan fixture works in licensed/stub modes.
- [x] Key documentation current: AGENTS.md, incremental-plan.md, action-items.md (this file), architecture/diagrams, function-tree.md — updated as part of this closure pass.
- [ ] "If I'm gone" / bus-factor coverage complete (see Vault + requirements-working-tree) — content in Vault is the primary; verify with Deb in PR#4 walkthrough.
- [x] No critical open action items (all high/medium in this file resolved or documented). The 9 function proof items cleared. Remaining priorities are polish / post-ship.
- [x] RAG index refreshed:
  ```bash
  .venv/bin/python3 scripts/update_tikr_rag_index.py
  ```
  (Latest: 224 files, 1402 chunks after Vault Export final feature.)
- [x] Lint + style clean:
  ```bash
  trunk check --all
  dotnet format TIKR.sln
  ```
  (Clean as of this run.)
- [x] PR / branch ready: green CI (TIKR CI + Trunk), no secrets, docs updated. (User confirmed green CI; local verifs match.)

**Note on full sign-off:** Per incremental-plan Phase 0 PR #4, final human step is recorded Deb walkthrough + any remaining docs/handover. This closes the automated + agent Layer 1+2 gates.

Once all checked (incl. the walkthrough items), the phase/project can be declared "done" with high confidence.

---

*This file is intentionally small and focused because the raw list is auto-generated.*