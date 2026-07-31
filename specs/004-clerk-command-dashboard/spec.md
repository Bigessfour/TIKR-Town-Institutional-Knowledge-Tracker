# Feature Specification: Clerk Command Dashboard

**Feature Branch**: `004-clerk-command-dashboard`
**Created**: 2026-07-31
**Status**: In progress

**Related:** [003-syncfusion-document-management](../003-syncfusion-document-management/spec.md)

## North star

The TIKR dashboard is Deb's due-out queue on the NAS: what's overdue, who it goes to, whether the packet is attached, and whether she can fix the packet without leaving the screen.

## User stories (summary)

- **US1** Urgency strip + due-out grid with SubmitTo, contacts, linked docs
- **US2** Due-out drill-down → Edit → DocumentWorkspaceDialog → save → return
- **US3** SfDashboardLayout drag/resize + localStorage + Reset layout
- **US4** Shared DocumentWorkspaceDialog extracted from Documents.razor
- **US5** Recent activity + corpus attention panels
- **US6** Quick actions

See [plan.md](plan.md) for implementation phases.
