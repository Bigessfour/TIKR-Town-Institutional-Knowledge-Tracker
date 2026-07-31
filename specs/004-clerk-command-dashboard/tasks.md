# Tasks: Clerk Command Dashboard

## Phase 1: Setup

- [X] T001 Create specs/004-clerk-command-dashboard artifacts
- [X] T002 Update .specify/feature.json

## Phase 2: Foundation (P0)

- [X] T005 Add Dashboard DTOs to TIKR.Shared
- [X] T006 Implement DashboardService + IDashboardService
- [X] T007 Map GET /api/dashboard/summary
- [X] T008 TikrApiClient.GetDashboardSummaryAsync + test
- [X] T009 Endpoint integration test

## Phase 3: DocumentWorkspaceDialog (P1)

- [X] T010 Extract DocumentWorkspaceDialog.razor
- [ ] T011 bUnit DocumentWorkspaceDialogTests (deferred — SfDialog render mode)

## Phase 4: Dashboard (P2-P3)

- [X] T016 Dashboard panel components (inline in Home.razor)
- [X] T022 Rebuild Home.razor with SfDashboardLayout
- [X] T023-T026 Layout persistence + reset

## Phase 5: Dedupe + proof (P4)

- [X] T027 Refactor Documents.razor dialog to shared component
- [X] T029 Update HomePageTests + DashboardLayoutServiceTests
- [X] T032 dotnet test (Web/Infra green; Grok licensed tests env-dependent)
