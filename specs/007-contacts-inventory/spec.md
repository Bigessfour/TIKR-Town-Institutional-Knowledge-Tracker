# Feature Specification: Contacts Inventory

**Feature Branch**: `007-contacts-inventory` / `feature/contacts-inventory`

**Created**: 2026-08-14

**Status**: Done (merged to `main` via PR #96; auth/E2E closure on `fix/contacts-inventory-closure`)

**Input**: Reusable Contacts inventory so Deb/Paige can store POCs (including Election contacts) with full details and link them from Requirements. Closes the gap where contacts are only free-text on Requirement and free-text Knowledge Vault Contact entries.

**Aligns with**: incremental-plan Phase 11

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage contact inventory (Priority: P1)

Deb opens Vault → Contacts and creates “Election – County Clerk” with address, office, email, phone, notes, and Election category. She can edit, soft-delete, and restore contacts from the same tab.

**Why this priority**: Without a durable inventory, clerks retype POCs on every requirement and lose institutional memory.

**Independent Test**: Create/list/update/soft-delete/restore via `/api/contacts` and Vault Contacts tab without touching Requirements.

**Acceptance Scenarios**:

1. **Given** an empty Contacts inventory, **When** Deb creates a contact with Election category and full details, **Then** it appears in Vault Contacts grid and GET `/api/contacts`.
2. **Given** an existing contact, **When** Deb soft-deletes it, **Then** it leaves the active list and can be restored.
3. **Given** legacy Knowledge Vault Contact entries, **When** Deb opens Contacts, **Then** inventory contacts are primary and legacy notes remain available in a secondary section.

---

### User Story 2 - Link contacts to Requirements (Priority: P1)

Deb opens Election Canvass & Certification, picks the County Clerk contact as primary, keeps SubmitTo editable, and sees denormalized ContactName/Email/Phone filled for CSV/print.

**Why this priority**: Core clerk workflow for due-outs depends on knowing who to call and where to submit.

**Independent Test**: Link/unlink via API and Requirements dialog; verify denormalized fields and audit Link/Unlink.

**Acceptance Scenarios**:

1. **Given** a seeded Election contact and Election Canvass requirement, **When** Deb links the contact as primary, **Then** Requirement ContactName/Email/Phone sync from the contact and SubmitTo remains editable.
2. **Given** a linked contact, **When** Deb unlinks it, **Then** the junction is removed and audit records Unlink.

---

### User Story 3 - Calendar event contact picker (Priority: P2)

On Calendar event editor, Deb can pick an inventory contact to fill contact fields while editing a requirement-backed event.

**Why this priority**: Calendar already surfaces contact columns; picker prevents drift from Vault inventory.

**Independent Test**: Apply a contact from Calendar editor and save; fields match inventory.

**Acceptance Scenarios**:

1. **Given** contacts exist, **When** Deb selects one in Calendar editor, **Then** ContactName/Email/Phone populate and SubmitTo stays free-text.

---

### User Story 4 - Audit and logging (Priority: P1)

Every create/update/delete/restore/link/unlink writes TikrActionLog structured details and AuditService entries with ContactId, category, and user id when auth is enabled.

**Why this priority**: Compliance and bus-factor trust require mutation trails.

**Independent Test**: API tests assert audit rows; service tests assert no silent mutations.

**Acceptance Scenarios**:

1. **Given** auth disabled (dev), **When** contacts mutate, **Then** audit still records actions.
2. **Given** `TIKR_AUTH_ENABLED`, **When** unauthenticated client hits `/api/contacts`, **Then** requests are rejected by the `/api` auth gate.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST persist Contact entities with Name, Role, Organization, Address, Office, Email, Phone, Notes, Categories flags, timestamps, CreatedBy, DeletedAt.
- **FR-002**: System MUST expose full CRUD under `/api/contacts` plus restore; soft-delete on DELETE.
- **FR-003**: System MUST support many-to-many Requirement↔Contact links with optional primary that syncs denormalized Requirement contact fields.
- **FR-004**: System MUST keep Requirement SubmitTo and free-text ContactName/Email/Phone as fallback.
- **FR-005**: Vault Contacts tab MUST manage Contact inventory (SfGrid + dialog/form); legacy Knowledge Contact entries remain accessible.
- **FR-006**: Requirements and Calendar editors MUST offer a contact picker.
- **FR-007**: Mutations MUST log via TikrActionLog and AuditService.
- **FR-008**: DbSeeder (or dedicated seeder) MUST seed ≥3 Election contacts and link County Clerk to Election Canvass as primary.
- **FR-009**: Proof-of-function tests MUST cover ContactService, API CRUD/link, and Web grid/picker.

### Key Entities

- **Contact** — durable POC record
- **ContactCategory** — `[Flags]` Election, Budget, Audit, MillLevy, Compliance, Custom
- **RequirementContact** — junction with LinkedAt, IsPrimary

## Success Criteria *(mandatory)*

- Deb can create Election – County Clerk and link it to Election Canvass Requirement.
- Soft-delete/restore works from Vault.
- Audit trail shows Create/Update/Link.
- CI green (`dotnet test` Release + trunk).
