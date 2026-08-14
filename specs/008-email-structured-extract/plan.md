# Implementation Plan: Email Structured Extract

**Branch**: `feature/email-structured-extract` | **Spec Kit**: `008-email-structured-extract`

## Summary

After `FolderEmailIngestionService` uploads a drop-folder file as a Document, optionally run deterministic email structured extraction → upsert Contacts + Knowledge Contact entries + clerk-facing notices/suggestions.

## File map

| Layer | Paths |
| --- | --- |
| Shared | `Helpers/EmailStructuredExtractor.cs`, DTOs, `TikrConfiguration.GetEmailStructuredExtractEnabled`, extend `EmailIngestionResult` |
| Infra | `EmailStructuredApplyService`, `EmailIngestionNoticeStore`, wire `FolderEmailIngestionService`, `.eml` text extract |
| Api | `GET /api/email/notices`, ingest returns extended result |
| Web | Documents banner + toast; TikrApiClient |
| Tests | Shared extractor fixtures; Infra ingest+extract; Api ingest |

## Auth

`/api` group auth gate unchanged.
