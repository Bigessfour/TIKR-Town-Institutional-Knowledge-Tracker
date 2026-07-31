# Implementation Plan: Clerk Command Dashboard

**Spec:** [spec.md](spec.md) · **Branch:** `feature/clerk-command-dashboard`

## Phases

- **P0** DTOs + `GET /api/dashboard/summary`
- **P1** `DocumentWorkspaceDialog` extraction
- **P2** Dashboard panels + `Home.razor` on `SfDashboardLayout`
- **P3** `DashboardLayoutService` + `tikr-dashboard-layout.js`
- **P4** Documents dedupe + tests

## Syncfusion validation

Use `sf-blazor-mcp` + Blazor skills for SfDashboardLayout, SfGrid, SfSmartPdfViewer, DocumentEditor, Spreadsheet.
