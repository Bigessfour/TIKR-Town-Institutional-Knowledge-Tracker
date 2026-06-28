# Requirements Manager — Working Tree

Living checklist for Requirements Manager work. Tracks MVP (ship now) vs deferred Phase 2+.

**Repo reality**

- No local `RequirementService` — use `TikrApiClient` + `RequirementWorkflowHelpers`
- Entity: [`src/TIKR.Shared/Entities/Requirement.cs`](../src/TIKR.Shared/Entities/Requirement.cs)
- DTOs: [`src/TIKR.Shared/DTOs/RequirementDto.cs`](../src/TIKR.Shared/DTOs/RequirementDto.cs)
- [`Calendar.razor`](../src/TIKR.Web/Components/Pages/Calendar.razor) remains the timeline/schedule consumer; `/requirements` is the CRUD hub

---

## MVP (in progress / ship now)

- [x] DeleteRequirementAsync on TikrApiClient + test
- [x] RequirementWorkflowHelpers (urgency, filter, CSV) + tests
- [x] Expand DbSeeder to ~15 Colorado obligations (no schema migration)
- [x] Requirements.razor at /requirements: SfGrid CRUD, filters, urgency badges, Add/Edit SfDialog, CSV export, bus-factor banner
- [x] MainLayout nav link to /requirements
- [x] RequirementsPageTests bUnit smoke
- [x] Update docs/incremental-plan.md cleanup backlog: Requirements MVP done; link to this file
- [x] Update README features if needed
- [x] dotnet test + coverage + trunk check

---

## Deferred (Phase 2+ — swing back after MVP)

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
- [ ] Print Packet export
- [ ] Offline mode indicator / Synology NAS badge

### Integrations

- [ ] VaultService.AddSuccessorNote() from requirements
- [ ] CalendarService.CreateRecurringEvent() / calendar highlight from grid row
- [ ] AI Validate ("Does this match CO Periodic Report rules?")
- [ ] Export Council Packet (formatted for agenda — beyond basic CSV)
- [ ] ClerkHeader reuse, UrgencyBadge component extraction

### Infrastructure

- [ ] Add Syncfusion.Blazor.TreeGrid package (individual, not meta)
- [ ] New API endpoints for AI suggest, CSV server-side export if needed
- [ ] Playwright E2E clerk flows for requirements
