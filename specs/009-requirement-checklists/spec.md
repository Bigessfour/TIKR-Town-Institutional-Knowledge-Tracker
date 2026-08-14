# Feature Specification: Requirement Checklists (Election Playbooks)

**Feature Branch**: `009-requirement-checklists` / `feature/requirement-checklists`

**Created**: 2026-08-14

**Status**: Done (implemented on branch; awaiting merge to `main`)

**Input**: Multi-step election (and other) planning via lightweight checklists under a Requirement.

**Aligns with**: incremental-plan Phase 13

## User Stories

### US1 — Election Canvass playbook (P1)

Deb opens Election Canvass & Certification and sees seeded checklist steps (notice, filings, canvass packet, certification) with due guidance. She can complete items; mutations are audited.

### US2 — Any requirement can have a checklist (P2)

Non-Election requirements support the same CRUD checklist UI.

## Functional Requirements

- **FR-001**: `RequirementChecklistItem` with Title, Description?, IsRequired, IsCompleted, DueOffsetDays?, DueDate?, SortOrder, LinkedDocumentId?, DocumentTemplateHint?, SubmitTo?, ContactId?
- **FR-002**: Cascade delete with Requirement; EF migration
- **FR-003**: API `/api/requirements/{id}/checklist` CRUD + complete + reorder
- **FR-004**: Requirements dialog editable checklist + progress on grid (and Calendar subject hint if easy)
- **FR-005**: Seed checklists for Election Canvass, Campaign Finance, Board Organizational Meeting
- **FR-006**: TikrActionLog + AuditService on mutations; proof tests

## Success Criteria

Opening Election Canvass shows a checklist; completing items is logged and tested. Demo path: [docs/demo-deb.md](../../docs/demo-deb.md).
