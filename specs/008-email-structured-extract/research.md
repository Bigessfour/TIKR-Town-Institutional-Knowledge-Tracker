# Research: Email Structured Extract

**Feature**: `008-email-structured-extract`
**Date**: 2026-08-14

## Decisions

| Topic    | Choice                                                               | Why                                          |
| -------- | -------------------------------------------------------------------- | -------------------------------------------- |
| Parser   | Deterministic `EmailStructuredExtractor` (regex/heuristics)          | Fail-soft, no AI dependency for ingest path  |
| Apply    | Upsert Contact by email or name+org; optional Knowledge Contact note | Matches Phase 11 inventory                   |
| Election | Keyword heuristics → `ContactCategory.Election`                      | Deb’s highest-risk category                  |
| Flag     | `TIKR_EMAIL_STRUCTURED_EXTRACT` default true                         | Can disable without stopping Document ingest |
| UI       | Documents banner/toast + optional Create Requirement                 | Clerk reviews before creating deadlines      |

## Proof

`EmailStructuredExtractorTests` (election / contact / generic / malformed), `FolderEmailIngestionServiceTests`, `EmailIngestEndpointTests`.
