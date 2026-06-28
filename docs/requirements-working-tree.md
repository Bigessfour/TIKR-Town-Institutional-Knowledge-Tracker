# Requirements Manager — Working Tree

Living checklist for Requirements Manager work. Tracks MVP (ship now) vs deferred Phase 2+.

**Repo reality**

- No local `RequirementService` — use `TikrApiClient` + `RequirementWorkflowHelpers`
- Entity: [`src/TIKR.Shared/Entities/Requirement.cs`](../src/TIKR.Shared/Entities/Requirement.cs)
- DTOs: [`src/TIKR.Shared/DTOs/RequirementDto.cs`](../src/TIKR.Shared/DTOs/RequirementDto.cs)
- [`Calendar.razor`](../src/TIKR.Web/Components/Pages/Calendar.razor) remains the timeline/schedule consumer; `/requirements` is the CRUD hub

**Syncfusion reference:** [AI Agent Tools for Document SDK](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/) — Storage Mode on NAS. Configuration: [sf-document-agent-tools.md](sf-document-agent-tools.md). **A2** wires deterministic PDF/Word extraction; **A3** adds Ollama tool orchestration.

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

## Phase 10C — Syncfusion AI Agent Tools (active)

**Goal:** Replace stub inference with [Syncfusion Document SDK AI Agent Tools](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/) — AI-callable, deterministic extraction (tables, KV pairs, OCR) on NAS, orchestrated by Ollama locally (no cloud required for core flows).

**NuGet (A2):** `Syncfusion.DocumentSDK.AI.AgentTools` (+ `Microsoft.Agents.AI.OpenAI` or existing `Microsoft.Extensions.AI` + Ollama). Requires `SYNCFUSION_LICENSE_KEY` (Document SDK entitlement).

| Group | Scope | Status |
|-------|--------|--------|
| **A1** | `IAgentDocumentStorage`, optional AES (`TIKR_AGENT_STORAGE_KEY`), `IDocumentAgentExtractionBackend`, stub backend uses real plain-text extraction, `USE_SYNCFUSION_AGENT_TOOLS` flag | done ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)) |
| **A2** | NuGet + `NasSyncfusionDocumentStorage` (`IDocumentStorage`); `SyncfusionDocumentAgentExtractor` (PDF text, Word text, table JSON) | done ([#35](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/35)) |
| **A3** | Microsoft Agent Framework loop: Ollama selects tools → validated JSON → requirement mapping | planned |
| **B** | In-memory vs storage-backed `IDocumentStorage` parity with Syncfusion modes | partial (NAS Storage Mode in A2) |
| **C** | Requirements UI: show extraction source (stub vs Syncfusion), progress indicator on scan | partial (`UsedSyncfusionTools` on DTO; UI badge deferred) |
| **D** | Playwright: upload → agent-scan → pre-filled requirement dialog | done (txt stub in `tests/e2e/requirements-agent-scan.spec.ts`; PDF via licensed workflow) |
| **E** | Docs: NAS setup, license, `USE_SYNCFUSION_AGENT_TOOLS` runbook | done ([sf-document-agent-tools.md](sf-document-agent-tools.md) E2E tiers) |

### A1 checklist

- [x] `IAgentDocumentStorage` + `NasAgentDocumentStorage` (`agent-scans/` prefix, optional AES-256-GCM)
- [x] `IDocumentAgentExtractionBackend` + `StubDocumentAgentExtractionBackend` (plain-text via `DocumentTextExtractionService`)
- [x] `USE_SYNCFUSION_AGENT_TOOLS` + `TIKR_AGENT_STORAGE_KEY` in `docker/.env.example`
- [x] Refactor `DocumentAgentService` to use storage + backend abstractions
- [x] Tests: crypto round-trip, NAS storage paths, `.txt` agent-scan extraction
- [x] Merge PR #35 (A1+A2)

### A2 checklist

- [x] `Syncfusion.DocumentSDK.AI.AgentTools` + `Syncfusion.Licensing` NuGet (33.2.15)
- [x] `NasSyncfusionDocumentStorage` implements Syncfusion `IDocumentStorage` under `agent-scans/sf-work/`
- [x] `SyncfusionDocumentAgentExtractor` — `PdfContentExtractionAgentTools`, `WordImportExportAgentTools`, `DataExtractionAgentTools`
- [x] License registration in `AddTikrInfrastructure` (`SYNCFUSION_LICENSE_KEY`)
- [x] `SyncfusionDocumentAgentExtractionBackend` delegates to extractor (no throw)
- [x] Tests: `NasSyncfusionDocumentStorageTests`
- [x] E2E: API txt fixture test + Playwright stub spec + licensed workflow scaffold
- [x] `DocumentAgentResult.UsedSyncfusionTools` for assertion in API/E2E
- [ ] Manual NAS smoke: `USE_SYNCFUSION_AGENT_TOOLS=true` + PDF agent-scan

### E2E proof (10C-D — open PR)

- [x] Fixtures: `tests/fixtures/agent-scan/` (txt, pdf, docx)
- [x] `DocumentAgentEndpointTests.AgentScan_ExtractsTxtFixture`
- [x] Playwright: `tests/e2e/requirements-agent-scan.spec.ts`
- [x] CI docker smoke: curl agent-scan txt (stub)
- [x] Optional workflow: `.github/workflows/tikr-syncfusion-agent-smoke.yml`

### Gap vs Syncfusion product (honest)

| Syncfusion capability | TIKR today |
|----------------------|------------|
| PDF/Word agent tools (Storage Mode) | Wired in A2 when flag enabled |
| Smart Data Extraction → JSON | Table count via `ExtractTableAsJson` |
| Microsoft Agent Framework `AITool` loop | Not wired (A3) |
| Storage-backed distributed agents | `NasSyncfusionDocumentStorage` on NAS volume |
| Included with Document SDK license | Same `SYNCFUSION_LICENSE_KEY` as Blazor |

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
- [ ] Print Packet export (basic print shipped in Phase 0 #33)
- [ ] Offline mode indicator / Synology NAS badge (Phase 0 #33)

### Integrations

- [ ] VaultService.AddSuccessorNote() from requirements
- [ ] CalendarService.CreateRecurringEvent() / calendar highlight from grid row
- [ ] AI Validate ("Does this match CO Periodic Report rules?")
- [ ] Export Council Packet (formatted for agenda — beyond basic CSV)
- [ ] ClerkHeader reuse, UrgencyBadge component extraction

### Infrastructure

- [ ] Add Syncfusion.Blazor.TreeGrid package (individual, not meta)
- [ ] Playwright E2E clerk flows for requirements (extend `tests/e2e/`)

---

## Cross-repo unfinished work (2026 audit)

Captured from codebase + plan review. Tracks stubs, Phase 0 closure, and non-Requirements gaps that affect clerk ship confidence. See also [incremental-plan.md](incremental-plan.md) Phase 0 and Phase 9 deferred items.

### Recommended finish order (lean PRs)

| Order | PR focus | Closes |
|-------|----------|--------|
| 1 | Phase 0 PR #2 — E2E + a11y | Playwright CI gate, `FullyTested` trait, extend smoke flows |
| 2 | Documents download API + UI | Remove `DownloadPlaceholder` stub |
| 3 | Documents delete undo | Toast undo parity with Requirements/Vault |
| 4 | 10C A2 merge + NAS smoke | PDF/DOCX agent-scan; extraction source badge (10C-C) |
| 5 | Phase 0 PR #3 — Docs | Deb handover; honest footer wording |
| 6 | Phase 0 PR #4 — Sign-off | Done Detector checklist + recorded walkthrough |
| Later | Voice STT, PDF preview, 10C-A3, Phase 6, Requirements Phase 2 | Post-ship / vNext |

### Phase 0 closure (PRs 2–4 — [incremental-plan.md](incremental-plan.md))

**Status:** PR #33 merged; items below remain before Deb sign-off.

- [ ] **PR #2 — Test & accessibility:** Expand Playwright beyond 4 specs (`clerk-smoke.spec.ts`, `requirements-agent-scan.spec.ts`); wire as ship gate (dedicated workflow or CI step — today Docker smoke is `continue-on-error`)
- [ ] **PR #2:** Add `FullyTested` trait/category + `dotnet test --filter FullyTested`
- [ ] **PR #2:** Mobile/tablet manual verification (44px touch targets baseline shipped)
- [ ] **PR #3 — Documentation:** Clerk handover doc + Deb walkthrough checklist
- [ ] **PR #3:** Footer wording — today shows SQLite mtime as "Last saved"; spec said "Last backed up" (Synology Hyper Backup not wired)
- [ ] **PR #4 — Sign-off:** Deb walkthrough recorded; Done Detector criteria signed off
- [ ] **PR #4:** Confirm `TIKR_STORAGE_LABEL` env matches clerk NAS model (e.g. DS225+)

### Stubs & placeholders in code (circle back)

#### High impact — clerk-visible

- [ ] **Document download** — `Documents.razor` `DownloadPlaceholder()`; `PageWorkflowHelpers.DownloadPlaceholder()` — needs `GET /api/documents/{id}/content` streaming from `IFileStorageService`
- [ ] **PDF/DOCX preview pane** — `Documents.razor` placeholder text; defer `SfPdfViewer` or show extracted text when available
- [ ] **Voice notes** — `Vault.razor` + `VaultVoiceNoteSimulator` — timer simulates transcription; no mic/Ollama STT yet
- [ ] **Agent scan binary PDF/DOCX (stub path)** — `StubDocumentAgentExtractionBackend` when `USE_SYNCFUSION_AGENT_TOOLS=false` (CI default); finish via 10C A2 merge + NAS flag
- [ ] **Documents delete — no undo** — `ConfirmDeleteAsync` shows toast without undo callback (Requirements/Vault have undo)
- [ ] **Extraction source badge (10C-C)** — `FormatAgentScanMessage` ignores `UsedSyncfusionTools`; add Stub vs Syncfusion badge + scan progress spinner

#### Medium impact — spec vs implementation

- [ ] **Field-level Help?** — `PageHelp` on main pages only; not per-field
- [ ] **Login / NotFound polish** — no `PageHelp`; NotFound minimal
- [ ] **Non-text FullTextContent on upload** — PDF/DOCX uploads save file but `FullTextContent` null until agent-scan or Syncfusion path

#### Low impact

- [ ] **10C-A3** — `IAgentDocumentStorage` comment references A3 orchestration; Microsoft Agent Framework loop not wired
- [ ] **SyncfusionDocumentAgentExtractor** — A2 code present; manual NAS smoke + merge pending (see A2 checklist above)

### Phase 9 / 6 / auth deferred (not Requirements-specific)

- [ ] PDF preview (`SfPdfViewer`), Rich DOCX / Spreadsheet preview
- [ ] IMAP / forward-to-folder email ingestion
- [ ] Phase 6 — Smart Paste, Smart TextArea, Scheduler NL recurring
- [ ] Auth vNext — email password reset (SMTP), token refresh, read-only `Viewer` role
- [ ] GitHub manual — Settings → Actions read-only default `GITHUB_TOKEN` (Phase 5B)

### Doc drift (fix when closing Phase 0)

- [ ] Mark done in **Deferred → UI** above: offline banner + Synology footer shipped in Phase 0 #33 (lines 122–123)
- [ ] Reconcile **Deferred → Infrastructure** Playwright line (line 135) with Phase 0 PR #2 scope above
