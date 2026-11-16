# Feature Specification: Document Intake AI Scan

**Feature Branch**: `010-document-intake-scan` / `feature/document-intake-scan`

**Created**: 2026-11-16

**Status**: In progress

**Input**: After a clerk imports a document (upload, email drop, or manual scan), produce a confirmable brief of due dates, points of contact, and action items for Calendar (via Requirements) and Vault Contacts—local-first, with opt-in redacted Grok.

**Aligns with**: incremental-plan Phase 14

## User Scenarios & Testing

### User Story 1 - Upload notice → intake banner (Priority: P1)

Deb uploads a PDF with “Due: November 30, 2026” and “Contact: Jane Doe”. After tag, TIKR runs an intake scan and shows a Documents banner. Deb accepts → Requirement on Calendar + Contact in Vault.

### User Story 2 - Manual Scan for deadlines (Priority: P1)

Deb selects an existing library file → **Scan for deadlines** → same banner/accept flow.

### User Story 3 - Local-first / Grok guardrail (Priority: P1)

With advanced intake off, scan uses heuristics + Ollama only. With it on (and USE_GROK), Grok receives a redacted excerpt only; UI shows Advanced AI (redacted). Never auto-writes without Accept.

## Functional Requirements

- **FR-001**: `DocumentIntakeScanService` builds a pending brief (due dates, contacts, action items).
- **FR-002**: Seed from `DueOutFieldParser`; enrich via local Ollama JSON; optional redacted Grok when Settings allow.
- **FR-003**: Persist `DocumentIntakeBrief` (Pending/Accepted/Dismissed).
- **FR-004**: Accept upserts Contacts and creates Requirements (+ checklist for extra actions), links Document.
- **FR-005**: API scan/get/accept/dismiss; Web banner + Scan button; upload triggers scan after tag.
- **FR-006**: Fail-soft; logs counts/filenames not bodies.

## Success Criteria

- Labeled notice → Accept → Requirement due date on Calendar + Contact in Vault.
- Grok intake off still works locally; on uses `CloudOutboundGuard`.
