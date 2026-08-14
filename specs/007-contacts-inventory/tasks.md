# Tasks: Contacts Inventory

**Input**: Design documents from `/specs/007-contacts-inventory/`

**Branch**: `feature/contacts-inventory` · Spec Kit ID: `007-contacts-inventory`

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [x] T001 Create git branch `feature/contacts-inventory` from latest `main`
- [x] T002 Create `specs/007-contacts-inventory/` artifacts
- [x] T003 Wire `.specify/feature.json`; add Phase 11 to `docs/incremental-plan.md`

## Phase 2: Foundational (Shared + EF)

- [x] T004 [P] `ContactCategory` flags enum
- [x] T005 [P] `Contact` + `RequirementContact` entities
- [x] T006 [P] DTOs + `IContactService`
- [x] T007 `TikrDbContext` + EF migration
- [x] T008 Register DI
- [x] T009 Seed 3 Election contacts + link to Election Canvass

## Phase 3: US1 Inventory CRUD (P1)

- [x] T010 `ContactService` with Audit + TikrActionLog
- [x] T011 API `/api/contacts` CRUD + restore
- [x] T012 `TikrApiClient` methods
- [x] T013 Vault Contacts tab grid/form (soft-delete UX)
- [x] T014 Infra unit tests `ContactServiceTests`
- [x] T015 Api integration `ContactsEndpointTests`
- [x] T016 bUnit Vault contacts tests

## Phase 4: US2 Requirement links (P1)

- [x] T017 Link/unlink + denormalized sync
- [x] T018 Requirement contact API endpoints
- [x] T019 Requirements.razor picker
- [x] T020 Api link/unlink tests
- [x] T021 bUnit Requirements picker tests
- [x] T022 CSV/search helpers unchanged (denormalized fields still searched)

## Phase 5: US3 Calendar (P2)

- [x] T023 Calendar contact picker
- [x] T024 Calendar/helper test

## Phase 6: US4 Docs / ship

- [x] T025 `docs/action-items.md` proof rows
- [x] T026 Function inventory refresh
- [x] T027 README + Phase 11 acceptance
- [x] T028 `dotnet test` Release + trunk on feature files
- [x] T029 RAG refresh after merge recommended
- [ ] T030 PR when CI green
