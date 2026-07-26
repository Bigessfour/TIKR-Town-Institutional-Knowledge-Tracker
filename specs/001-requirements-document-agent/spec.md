# Feature Specification: Requirements Manager & Document Agent

**Feature Branch**: `001-requirements-document-agent`

**Created**: 2026-07-21

**Status**: Implemented

**Verification**: All tasks in `tasks.md` closed; independently verified by post-mortem audit + Release test suite (2026-07-25).

**Input**: Brownfield spec for TIKR Phase 10 — clerk obligations hub plus NAS-local document AI intake. Documents what exists and what remains for ship-ready closure (Phase 0 + 10C gaps). Tech stack deferred to plan phase.

**Related docs**: [incremental-plan.md](../../docs/incremental-plan.md) Phase 10, [requirements-working-tree.md](../../docs/requirements-working-tree.md)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Colorado obligations in one place (Priority: P1)

Deb (town clerk) opens `/requirements` to see all municipal filing deadlines and obligations in a searchable grid with urgency badges. She can add, edit, filter, export CSV, and delete requirements without losing work accidentally.

**Why this priority**: Core clerk value — obligations tracking is the reason TIKR exists alongside the calendar.

**Independent Test**: Clerk can CRUD requirements, see urgency, export CSV, and delete with undo — verified by bUnit + API tests without AI scan.

**Acceptance Scenarios**:

1. **Given** seeded Colorado obligations, **When** Deb opens `/requirements`, **Then** she sees a grid with title, due date, urgency, and status filters.
2. **Given** a new obligation, **When** Deb adds via dialog and saves, **Then** the row appears and persists after refresh.
3. **Given** an existing row, **When** Deb deletes it, **Then** she gets confirmation and can undo within the toast window.

---

### User Story 2 - AI scan uploaded documents into requirement fields (Priority: P1)

Deb uploads a clerk report or periodic filing PDF/DOCX and runs **AI Scan** to pre-fill requirement fields (title, due date hints, extracted text/tables) without sending documents to the cloud.

**Why this priority**: Reduces manual data entry — primary differentiator for document-heavy clerk work.

**Independent Test**: Upload fixture `.txt` (stub path) or licensed PDF (Syncfusion path) → agent-scan returns structured extraction → Apply populates the form — API + Playwright proof.

**Acceptance Scenarios**:

1. **Given** a plain-text upload, **When** Deb clicks AI Scan, **Then** extracted fields appear in a review banner she can apply to the form.
2. **Given** `USE_SYNCFUSION_AGENT_TOOLS=true` and a valid license, **When** Deb scans a PDF, **Then** extraction uses Syncfusion Document SDK tools and UI shows Syncfusion vs stub source.
3. **Given** Ollama offline, **When** Deb scans, **Then** she sees a clear error — no silent failure or cloud fallback for core scan.

---

### User Story 3 - Trusted NAS-local document archive after AI processing (Priority: P2)

After AI scan on a PDF, TIKR keeps both the original and a processed copy on NAS with a visible "AI Processed" stamp and structured table data mapped to requirement fields — so the next clerk can trust what was machine-read.

**Why this priority**: Institutional knowledge and audit trail for one-person towns ("hit by a bus" continuity).

**Independent Test**: Agent scan on PDF fixture → processed archive stored under `agent-scans/` → requirement fields populated from tables → tests prove dual storage paths.

**Acceptance Scenarios**:

1. **Given** a successful licensed PDF scan, **When** processing completes, **Then** original and stamped processed PDF are stored on NAS-local storage.
2. **Given** extracted table JSON, **When** Deb applies results, **Then** relevant requirement fields receive mapped values without manual retyping.

---

### User Story 4 - Ship-ready clerk confidence (Priority: P2)

Deb can complete a recorded walkthrough: requirements grid, agent scan (txt minimum; PDF when licensed), documents download, vault handover — with honest local-first trust cues (NAS badge, last saved indicator).

**Why this priority**: Phase 0 closure gate before municipal sign-off.

**Independent Test**: Done Detector Layer 1+2 checklists in action-items; blocking Playwright clerk smoke in CI green.

**Acceptance Scenarios**:

1. **Given** Docker or Windows deploy, **When** Deb follows clerk install doc, **Then** `/requirements` and AI Scan txt path work without cloud keys.
2. **Given** CI pipeline, **When** PR merges, **Then** TIKR CI + Trunk are green including agent-scan smoke.

---

### Edge Cases

- What happens when Syncfusion license is missing? Stub/plain-text path still works; licensed PDF/DOCX returns actionable message.
- What happens when agent storage key is wrong? Encrypted storage fails gracefully with logged error, no data corruption.
- What happens when scan returns low-confidence extraction? Clerk can dismiss or partially apply — never auto-save without review.
- What happens on duplicate requirement titles? Grid remains usable; clerk resolves manually (no auto-merge in this feature scope).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide `/requirements` CRUD hub with grid filters, urgency badges, CSV export, and delete-with-undo.
- **FR-002**: System MUST expose `POST /api/ai/agent-scan` that returns structured extraction for uploaded documents.
- **FR-003**: Clerk MUST be able to review and apply agent extraction to a requirement form before save.
- **FR-004**: System MUST support NAS-local agent document storage (`agent-scans/`) with optional encryption when configured.
- **FR-005**: System MUST indicate extraction source (stub vs Syncfusion) in the Requirements UI when scan completes.
- **FR-006**: When Syncfusion agent tools are enabled, system MUST use licensed Document SDK extraction for PDF/Word — not filename heuristics alone.
- **FR-007**: System MUST keep core agent-scan flows on Ollama/local processing — no cloud requirement for default scan path.
- **FR-008**: System MUST store processed PDF archive with audit-friendly stamp when 10C-G archive flow is complete.
- **FR-009**: System MUST map structured table extraction to requirement fields when apply is confirmed.
- **FR-010**: Automated tests MUST cover agent-scan API (txt stub minimum; licensed path when CI secret present).
- **FR-011**: Playwright clerk smoke MUST include requirements agent-scan path in blocking CI.
- **FR-012**: Documents page MUST allow download of stored files (`GET /api/documents/{id}/content`).

### Key Entities

- **Requirement**: Municipal obligation — title, due date, urgency, status, recurrence, notes; target of CRUD and agent apply.
- **Document / Agent scan artifact**: Uploaded file plus optional processed copy under NAS agent storage.
- **DocumentAgentResult**: Extraction payload — text, tables, tool source flag, confidence hints for UI apply.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Clerk completes add-requirement + txt AI Scan + apply in under 5 minutes on local deploy without cloud keys.
- **SC-002**: `dotnet test TIKR.sln --configuration Release` and `trunk check --all` pass before merge.
- **SC-003**: Blocking Playwright clerk smoke passes in TIKR CI on every PR touching requirements or agent code.
- **SC-004**: When licensed path enabled, PDF fixture agent-scan returns `UsedSyncfusionTools=true` in API response.
- **SC-005**: Function inventory shows proof for all trackable agent/requirements endpoints and helpers changed in this feature scope.

## Assumptions

- Single-clerk or small-town deployment on Synology NAS or Windows PC — not multi-tenant SaaS.
- Ollama available locally for AI features; Grok remains optional fallback elsewhere, not required for agent scan.
- Syncfusion Document SDK license available in production; CI uses stub path when license secret absent.
- Requirements Phase 2 (TreeGrid, Stepper wizard, hierarchy) is **out of scope** for this feature — deferred to vNext.
- Phase 6 Smart Components is a separate future Spec Kit feature.

## Out of Scope (explicit)

- Parent/child requirement hierarchy, iCalendar recurrence strings, requirement ↔ vault linking (Phase 2+).
- IMAP email ingestion, PDF viewer polish, voice STT (separate phases).
- Microsoft Agent Framework multi-tool orchestration beyond current Syncfusion + Ollama wiring (10C-A3 stretch — plan phase decides).
