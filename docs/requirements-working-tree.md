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
| **A1** | `IAgentDocumentStorage`, optional AES (`TIKR_AGENT_STORAGE_KEY`), `IDocumentAgentExtractionBackend`, stub backend uses real plain-text extraction, `USE_SYNCFUSION_AGENT_TOOLS` flag | done (PR #35) |
| **A2** | NuGet + `NasSyncfusionDocumentStorage` (`IDocumentStorage`); `SyncfusionDocumentAgentExtractor` (PDF text, Word text, table JSON) | in progress |
| **A3** | Microsoft Agent Framework loop: Ollama selects tools → validated JSON → requirement mapping | planned |
| **B** | In-memory vs storage-backed `IDocumentStorage` parity with Syncfusion modes | partial (NAS Storage Mode in A2) |
| **C** | Requirements UI: show extraction source (stub vs Syncfusion), progress indicator on scan | planned |
| **D** | Playwright: upload PDF → agent-scan → pre-filled requirement dialog | planned |
| **E** | Docs: NAS setup, license, `USE_SYNCFUSION_AGENT_TOOLS` runbook | partial ([sf-document-agent-tools.md](sf-document-agent-tools.md)) |

### A1 checklist

- [x] `IAgentDocumentStorage` + `NasAgentDocumentStorage` (`agent-scans/` prefix, optional AES-256-GCM)
- [x] `IDocumentAgentExtractionBackend` + `StubDocumentAgentExtractionBackend` (plain-text via `DocumentTextExtractionService`)
- [x] `USE_SYNCFUSION_AGENT_TOOLS` + `TIKR_AGENT_STORAGE_KEY` in `docker/.env.example`
- [x] Refactor `DocumentAgentService` to use storage + backend abstractions
- [x] Tests: crypto round-trip, NAS storage paths, `.txt` agent-scan extraction
- [ ] Merge PR #35 (A1)

### A2 checklist (current)

- [x] `Syncfusion.DocumentSDK.AI.AgentTools` + `Syncfusion.Licensing` NuGet (33.2.15)
- [x] `NasSyncfusionDocumentStorage` implements Syncfusion `IDocumentStorage` under `agent-scans/sf-work/`
- [x] `SyncfusionDocumentAgentExtractor` — `PdfContentExtractionAgentTools`, `WordImportExportAgentTools`, `DataExtractionAgentTools`
- [x] License registration in `AddTikrInfrastructure` (`SYNCFUSION_LICENSE_KEY`)
- [x] `SyncfusionDocumentAgentExtractionBackend` delegates to extractor (no throw)
- [x] Tests: `NasSyncfusionDocumentStorageTests`
- [ ] Manual NAS smoke: `USE_SYNCFUSION_AGENT_TOOLS=true` + PDF agent-scan
- [ ] Merge PR

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
