# Requirements Manager — Working Tree

Living checklist for Requirements Manager work. Tracks MVP (ship now) vs deferred Phase 2+.

**Updated:** 2026-06-28 — honest snapshot of `main` vs active branch.

## Where we are

| Layer | Truth |
|-------|--------|
| **`main`** | 10A, 10B, 10C **A1+A2** merged ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)). **275 tests**. Syncfusion PDF/Word extraction wired behind `USE_SYNCFUSION_AGENT_TOOLS=true` (not exercised in default CI). No `UsedSyncfusionTools` on DTO yet. Playwright: `clerk-smoke.spec.ts` only. |
| **Active branch** | `feature/phase10c-e2e-proof` → open **[PR #36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36)** (10C-D E2E proof). Adds fixtures, API test, Playwright agent-scan spec, docker smoke curl, licensed workflow scaffold, `UsedSyncfusionTools`. **277 tests** when merged. |
| **Next after #36** | Manual NAS smoke (licensed PDF), 10C-C UI badge, 10C-A3 Ollama orchestration, Phase 0 docs/sign-off. |

**Repo reality**

- No local `RequirementService` — use `TikrApiClient` + `RequirementWorkflowHelpers`
- Entity: [`src/TIKR.Shared/Entities/Requirement.cs`](../src/TIKR.Shared/Entities/Requirement.cs)
- DTOs: [`src/TIKR.Shared/DTOs/RequirementDto.cs`](../src/TIKR.Shared/DTOs/RequirementDto.cs)
- [`Calendar.razor`](../src/TIKR.Web/Components/Pages/Calendar.razor) remains the timeline/schedule consumer; `/requirements` is the CRUD hub

**Syncfusion reference:** [AI Agent Tools for Document SDK](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/) — Storage Mode on NAS. Configuration: [sf-document-agent-tools.md](sf-document-agent-tools.md). **A2** (on `main`) wires deterministic PDF/Word extraction; **A3** adds Ollama tool orchestration (not started).

---

## MVP (done)

- [x] DeleteRequirementAsync on TikrApiClient + test
- [x] RequirementWorkflowHelpers (urgency, filter, CSV) + tests
- [x] Expand DbSeeder to ~15 Colorado obligations (no schema migration)
- [x] Requirements.razor at /requirements: SfGrid CRUD, filters, urgency badges, Add/Edit SfDialog, CSV export, bus-factor banner
- [x] MainLayout nav link to /requirements
- [x] RequirementsPageTests bUnit smoke
- [x] Update docs/incremental-plan.md cleanup backlog: Requirements MVP done; link to this file
- [x] Update README features if needed
- [x] dotnet test + coverage + trunk check

### Phase 10B — MVP AI agent stub (merged #31)

- [x] `IDocumentAgentService` + stub `DocumentAgentService` (filename heuristics; not Syncfusion tools)
- [x] `POST /api/ai/agent-scan` + `TikrApiClient.ScanDocumentWithAgentAsync`
- [x] Requirements toolbar **AI Scan uploaded doc** + banner message
- [x] `RequirementWorkflowHelpers.ApplyAgentExtraction` + `FormatAgentScanMessage`
- [x] Infrastructure, Api, Web helper, and bUnit tests

---

## Phase 10C — Syncfusion AI Agent Tools

**Goal:** Replace stub inference with [Syncfusion Document SDK AI Agent Tools](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/) — AI-callable, deterministic extraction (tables, KV pairs, OCR) on NAS, orchestrated by Ollama locally (no cloud required for core flows).

**NuGet (A2, on `main`):** `Syncfusion.DocumentSDK.AI.AgentTools` (33.2.15). Requires `SYNCFUSION_LICENSE_KEY` (Document SDK entitlement).

| Group | Scope | Status |
|-------|--------|--------|
| **A1** | `IAgentDocumentStorage`, optional AES (`TIKR_AGENT_STORAGE_KEY`), `IDocumentAgentExtractionBackend`, stub backend uses real plain-text extraction, `USE_SYNCFUSION_AGENT_TOOLS` flag | **done on `main`** ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)) |
| **A2** | NuGet + `NasSyncfusionDocumentStorage` (`IDocumentStorage`); `SyncfusionDocumentAgentExtractor` (PDF text, Word text, table JSON) | **done on `main`** ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)) — licensed path not CI-proven until #36 + NAS smoke |
| **A3** | Microsoft Agent Framework loop: Ollama selects tools → validated JSON → requirement mapping | planned |
| **B** | In-memory vs storage-backed `IDocumentStorage` parity with Syncfusion modes | partial (NAS Storage Mode in A2) |
| **C** | Requirements UI: show extraction source (stub vs Syncfusion), progress indicator on scan | partial — `UsedSyncfusionTools` on DTO in **PR #36 only**; UI badge not built |
| **D** | Playwright + API + docker proof for agent-scan | **PR #36 open** — not on `main` yet |
| **E** | Docs: NAS setup, license, E2E tiers | partial on `main`; E2E tier table completes in **PR #36** |

### A1+A2 (on `main` — merged #35)

- [x] `IAgentDocumentStorage` + `NasAgentDocumentStorage` (`agent-scans/` prefix, optional AES-256-GCM)
- [x] `IDocumentAgentExtractionBackend` + `StubDocumentAgentExtractionBackend` (plain-text via `DocumentTextExtractionService`)
- [x] `USE_SYNCFUSION_AGENT_TOOLS` + `TIKR_AGENT_STORAGE_KEY` in `docker/.env.example`
- [x] Refactor `DocumentAgentService` to use storage + backend abstractions
- [x] Tests: crypto round-trip, NAS storage paths, Syncfusion extractor unit tests
- [ ] **Manual NAS smoke:** `USE_SYNCFUSION_AGENT_TOOLS=true` + `minimal-clerk-report.pdf` on real NAS/Docker with license (not recorded)

### 10C-D E2E proof — [PR #36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36) (open)

Code complete on branch; **not on `main` until merged.**

- [x] Fixtures: `tests/fixtures/agent-scan/` (txt, pdf, docx)
- [x] `DocumentAgentEndpointTests.AgentScan_ExtractsTxtFixture`
- [x] `DocumentAgentResult.UsedSyncfusionTools` for API/E2E assertions
- [x] Playwright: `tests/e2e/requirements-agent-scan.spec.ts` (manual against Docker)
- [x] CI docker smoke: curl agent-scan txt (stub; `continue-on-error` on docker job today)
- [x] Optional workflow: `.github/workflows/tikr-syncfusion-agent-smoke.yml` (needs repo secret `SYNCFUSION_LICENSE_KEY`)
- [x] `LocalFileStorageService` preserves `agent-scans/` prefix
- [ ] **Merge PR #36**
- [ ] TIKR CI green on #36

### Gap vs Syncfusion product (honest)

| Syncfusion capability | TIKR today |
|----------------------|------------|
| PDF/Word agent tools (Storage Mode) | On `main` when `USE_SYNCFUSION_AGENT_TOOLS=true`; not default CI path |
| Smart Data Extraction → JSON | Table count via `ExtractTableAsJson` |
| Microsoft Agent Framework `AITool` loop | Not wired (A3) |
| Storage-backed distributed agents | `NasSyncfusionDocumentStorage` on NAS volume |
| Included with Document SDK license | Same `SYNCFUSION_LICENSE_KEY` as Blazor |
| Automated proof of licensed extraction | PR #36 + optional smoke workflow; neither merged/run yet |

---

## Deferred (Phase 2+ — after 10C ship)

### Schema / data model

- [ ] ParentId hierarchy for nested CO obligations
- [ ] SubmitTo field (CO Sec of State, County Clerk, etc.)
- [ ] iCalendar RecurrenceRule strings (beyond RecurrenceType enum)
- [ ] Requirement ↔ KnowledgeEntry link (successor notes / "how we do it")
- [ ] Requirement ↔ Document attachments (FK or join table)
- [ ] KnowledgeMarkdown, Tags on Requirement entity

### UI / Syncfusion (Phase 2)

- [ ] SfTab with three tabs: Grid (done in MVP as single grid), Hierarchical Tree (SfTreeGrid), Timeline Preview (embedded SfSchedule)
- [ ] SfTreeGrid with IdMapping/ParentIdMapping, drag-drop, Excel/PDF export
- [ ] SfStepper wizard (4-step Add/Edit) with SfRichTextEditor + voice/Ollama transcription
- [ ] SfSplitter side panel detail view (Timeline History, Knowledge Links, Attached Docs, Audit Trail tabs)
- [ ] Semantic AI Search toggle on requirements page
- [ ] Floating FAB "Bulk Import from Mail"
- [ ] EnablePersistence on grid (column order/filters)
- [ ] Keyboard shortcuts (Ctrl+N, Ctrl+E)
- [ ] Mobile stacked tabs + bottom FAB
- [ ] "Copy for Deputy Clerk" per row
- [ ] "AI Fill Gaps" button (Ollama suggest similar CO requirements)
- [ ] "Test Submit" action
- [x] Print Packet export (basic print shipped in Phase 0 [#33](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/33))
- [x] Offline mode indicator / Synology NAS badge (Phase 0 [#33](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/33))

### Integrations

- [ ] VaultService.AddSuccessorNote() from requirements
- [ ] CalendarService.CreateRecurringEvent() / calendar highlight from grid row
- [ ] AI Validate ("Does this match CO Periodic Report rules?")
- [ ] Export Council Packet (formatted for agenda — beyond basic CSV)
- [ ] ClerkHeader reuse, UrgencyBadge component extraction

### Infrastructure

- [ ] Add Syncfusion.Blazor.TreeGrid package (individual, not meta)
- [ ] Playwright E2E clerk flows — `clerk-smoke.spec.ts` on `main`; `requirements-agent-scan.spec.ts` in PR #36; neither wired as required CI gate yet

---

## Cross-repo unfinished work (2026 audit)

Captured from codebase + plan review. Tracks stubs, Phase 0 closure, and non-Requirements gaps that affect clerk ship confidence. See also [incremental-plan.md](incremental-plan.md) Phase 0 and Phase 9 deferred items.

### Recommended finish order (lean PRs)

| Order | PR focus | Closes |
|-------|----------|--------|
| **Now** | **Merge [PR #36](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/36)** — 10C-D E2E | Fixtures, `UsedSyncfusionTools`, agent-scan API/Playwright/docker smoke |
| 1 | Manual NAS smoke + optional licensed workflow dispatch | Proves PDF/DOCX with `SYNCFUSION_LICENSE_KEY` |
| 2 | 10C-C UI badge | `FormatAgentScanMessage` shows stub vs Syncfusion |
| 3 | Phase 0 PR #3 — Docs | Deb handover; honest footer wording ("Last saved" vs "Last backed up") |
| 4 | Phase 0 PR #4 — Sign-off | Done Detector checklist + recorded walkthrough |
| 5 | Documents download API + UI | Remove `DownloadPlaceholder` stub |
| 6 | Documents delete undo | Toast undo parity with Requirements/Vault |
| Later | Phase 0 Playwright CI gate, `FullyTested` trait, Voice STT, PDF preview, 10C-A3, Phase 6, Requirements Phase 2 | Post-ship / vNext |

### Phase 0 closure ([incremental-plan.md](incremental-plan.md))

**Status:** [#33](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/33) + [#34](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/34) merged on `main`. PRs 3–4 remain before Deb sign-off.

- [x] **PR #2 — partial ([#34](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/34)):** Keyboard nav, bUnit polish tests, plan cleanup
- [ ] **PR #2 — remaining:** Playwright as required CI gate (docker smoke still `continue-on-error`); `FullyTested` trait + `dotnet test --filter FullyTested`; mobile/tablet manual pass
- [ ] **PR #3 — Documentation:** Clerk handover doc + Deb walkthrough checklist
- [ ] **PR #3:** Footer wording — SQLite mtime shown as "Last saved"; spec wanted "Last backed up" (Hyper Backup not wired)
- [ ] **PR #4 — Sign-off:** Deb walkthrough recorded; Done Detector criteria signed off
- [ ] **PR #4:** Confirm `TIKR_STORAGE_LABEL` matches clerk NAS model (e.g. DS225+)

### Stubs & placeholders in code (circle back)

#### High impact — clerk-visible

- [ ] **Document download** — `Documents.razor` `DownloadPlaceholder()`; `PageWorkflowHelpers.DownloadPlaceholder()` — needs `GET /api/documents/{id}/content` streaming from `IFileStorageService`
- [ ] **PDF/DOCX preview pane** — `Documents.razor` placeholder text; defer `SfPdfViewer` or show extracted text when available
- [ ] **Voice notes** — `Vault.razor` + `VaultVoiceNoteSimulator` — timer simulates transcription; no mic/Ollama STT yet
- [ ] **Agent scan PDF/DOCX (stub path)** — **By design** when `USE_SYNCFUSION_AGENT_TOOLS=false` (CI/default docker). Plain `.txt` works. Licensed PDF/Word on `main` via Syncfusion when flag + key set; proof pending NAS smoke / PR #36 merge + licensed workflow
- [ ] **Documents delete — no undo** — `ConfirmDeleteAsync` shows toast without undo callback (Requirements/Vault have undo)
- [ ] **Extraction source badge (10C-C)** — `FormatAgentScanMessage` ignores `UsedSyncfusionTools` (field lands with PR #36); UI badge not built

#### Medium impact — spec vs implementation

- [ ] **Field-level Help?** — `PageHelp` on main pages only; not per-field
- [ ] **Login / NotFound polish** — no `PageHelp`; NotFound minimal
- [ ] **Non-text FullTextContent on upload** — PDF/DOCX uploads save file but `FullTextContent` null until agent-scan or Syncfusion path

#### Low impact

- [ ] **10C-A3** — Microsoft Agent Framework / Ollama tool loop not wired
- [ ] **Licensed Syncfusion smoke** — workflow file in PR #36; needs secret + manual dispatch or weekly run after merge

### Phase 9 / 6 / auth deferred (not Requirements-specific)

- [ ] PDF preview (`SfPdfViewer`), Rich DOCX / Spreadsheet preview
- [ ] IMAP / forward-to-folder email ingestion
- [ ] Phase 6 — Smart Paste, Smart TextArea, Scheduler NL recurring
- [ ] Auth vNext — email password reset (SMTP), token refresh, read-only `Viewer` role
- [ ] GitHub manual — Settings → Actions read-only default `GITHUB_TOKEN` (Phase 5B)

### Repo hygiene (2026-06-28)

- [x] Merged feature branches deleted from origin (`phase10b`, `phase0-*`, `phase9-text-extraction`, `phase10c-agent-tools-a1`)
- [x] E2E work rebased to `feature/phase10c-e2e-proof` after #35 squash-merge
- [ ] Merge PR #36; delete `feature/phase10c-e2e-proof` after merge
- [ ] Dependabot action bumps ([#15](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/15)–[#17](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/17)) — green CI; `@dependabot rebase` requested
