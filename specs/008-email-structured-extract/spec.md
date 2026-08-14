# Feature Specification: Email Structured Extract

**Feature Branch**: `008-email-structured-extract` / `feature/email-structured-extract`

**Created**: 2026-08-14

**Status**: Done (implementation on `feature/email-structured-extract`)

**Input**: Turn email folder drops into structured institutional knowledge — contacts, dates, Election awareness — after Document ingest.

**Aligns with**: incremental-plan Phase 12

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Election procedure email → Contact (Priority: P1)

Deb (or IMAP drop) places `Update to election procedures.eml` in `TIKR_EMAIL_INBOX_PATH`. TIKR uploads it as a Document, extracts From/contact fields, upserts a Contact tagged Election, and optionally a Knowledge Contact note. Structured logs record ingest + extract outcomes.

**Independent Test**: Drop fixture .eml with extraction enabled; assert Document + Contact (Election) + logs; disable flag and assert Document only.

### User Story 2 - Clerk review in UI (Priority: P2)

Documents (or toast) shows “Extracted contact(s) from email X; review in Vault Contacts” with optional Create Requirement from suggestion.

### User Story 3 - Bad files never crash poller (Priority: P1)

Corrupt/unsupported content is skipped or logged; hosted service continues.

## Functional Requirements

- **FR-001**: Deterministic `EmailStructuredExtractor` parses .eml (headers + body); best-effort .msg text scrape.
- **FR-002**: Detect Election keywords → `ContactCategory.Election`.
- **FR-003**: After Document upload, when `TIKR_EMAIL_STRUCTURED_EXTRACT` is true, upsert Contact by email or name+org; create KnowledgeEntry `Contact` when useful.
- **FR-004**: Optional Requirement suggestion via DueOutFieldParser (clerk confirm).
- **FR-005**: TikrActionLog + AuditService on Contact/Knowledge mutations; never throw out of ingest loop.
- **FR-006**: Existing folder ingestion works when extract disabled or fails.

## Success Criteria

- Dropping election procedure .eml → Document + ≥1 Election Contact (or clear suggestion) + green tests.
