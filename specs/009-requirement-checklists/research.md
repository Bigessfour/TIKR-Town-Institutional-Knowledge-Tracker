# Research: Requirement Checklists

**Feature**: `009-requirement-checklists`
**Date**: 2026-08-14

## Decisions

| Topic     | Choice                                                                 | Why                                                                               |
| --------- | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| Model     | Nested `RequirementChecklistItem` under Requirement                    | Keeps playbook scoped to a due-out Deb already manages; cascade delete is natural |
| Due dates | Prefer `DueOffsetDays` relative to parent; optional absolute `DueDate` | Election steps are usually “N days before canvass,” not independent calendars     |
| API shape | Nested `/api/requirements/{id}/checklist`                              | Matches document/contact sub-resources; avoids orphan checklist CRUD              |
| UI        | List in Requirements edit dialog + progress `n/m` on grid              | Clerk already lives in that dialog; no new page                                   |
| Seeds     | Election Canvass, Campaign Finance, Board Organizational Meeting       | Highest-value municipal Election workflows for Wiley                              |
| Logging   | TikrActionLog + AuditService                                           | Same mutation pattern as Contacts / Requirements                                  |

## Alternatives considered

- Standalone Task entity with many-to-many Requirements — deferred (overkill for Deb’s playbook).
- Syncfusion Stepper-only UI — deferred; editable list is enough for MVP.

## Proof

- Infra: `RequirementChecklistServiceTests`, `RequirementChecklistSeederTests`
- Api: `RequirementsEndpointTests` checklist CRUD/complete/reorder/seeded Election
- Web: `Requirements_ShowsChecklistPanelWhenEditing`, `Requirements_ShowsPlaybookProgressOnGrid`
- E2E: `page-readiness` Election Canvass playbook checklist
