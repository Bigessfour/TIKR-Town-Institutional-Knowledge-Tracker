# Data Model: Requirements Manager & Document Agent

**Feature**: `001-requirements-document-agent` | **Date**: 2026-07-21

## Requirement (persisted — EF Core)

**Table**: `Requirements` via `TikrDbContext`
**Entity**: `src/TIKR.Shared/Entities/Requirement.cs`

| Field                 | Type                     | Rules                                                                |
| --------------------- | ------------------------ | -------------------------------------------------------------------- |
| Id                    | Guid                     | PK, generated on create                                              |
| Title                 | string                   | Required, non-empty                                                  |
| Description           | string?                  | Optional; receives agent extracted text + structured tables on apply |
| DueDate               | DateOnly                 | Required                                                             |
| Recurrence            | RecurrenceType enum      | Default Annual                                                       |
| Category              | RequirementCategory enum | Default Custom; agent may suggest                                    |
| IsSystemSeeded        | bool                     | Seeded CO obligations                                                |
| IsCompleted           | bool                     | Clerk completion flag                                                |
| CreatedAt / UpdatedAt | DateTime                 | UTC audit                                                            |
| CreatedBy             | string?                  | Optional user id when auth enabled                                   |

**State transitions**: Created → Updated (edit dialog) → Completed (mark done) or Deleted (with undo in UI).

**API DTOs**: `RequirementDto`, `CreateRequirementRequest`, `UpdateRequirementRequest` in `TIKR.Shared/DTOs/RequirementDto.cs`.

## DocumentAgentResult (transient — API response)

**Record**: `src/TIKR.Shared/DTOs/DocumentAgentDto.cs`

| Field                | Type                | Purpose                            |
| -------------------- | ------------------- | ---------------------------------- |
| SuggestedTitle       | string              | Pre-fill title on apply            |
| ExtractedText        | string?             | Body text for description          |
| SuggestedDueDate     | DateOnly?           | Heuristic due date                 |
| SuggestedRecurrence  | RecurrenceType      | Default suggestion                 |
| SuggestedCategory    | RequirementCategory | Inferred from title keywords       |
| TablesExtractedCount | int                 | UI message + badge                 |
| StoragePath          | string              | Primary stored artifact path       |
| ProcessedLocally     | bool                | Always true for NAS path           |
| UsedSyncfusionTools  | bool                | FR-005 source indicator            |
| OriginalStoragePath  | string?             | Dual storage — original upload     |
| ProcessedStoragePath | string?             | Dual storage — stamped archive PDF |
| StructuredTables     | string?             | JSON or text for table mapping     |

Not persisted as its own table — clerk applies selected fields into a new or edited `Requirement`.

## Agent scan artifacts (file storage — not EF)

**Storage abstraction**: `IAgentDocumentStorage` → `NasAgentDocumentStorage`

| Artifact          | Path pattern                        | Encryption                                             |
| ----------------- | ----------------------------------- | ------------------------------------------------------ |
| Original upload   | `agent-scans/{safeName}`            | Optional AES-256-GCM when `TIKR_AGENT_STORAGE_KEY` set |
| Processed archive | `agent-scans/{name}.ai-archive.pdf` | Same                                                   |
| Syncfusion work   | `agent-scans/sf-work/...`           | Via `NasSyncfusionDocumentStorage`                     |

**Validation**: Max 100 MB per upload; filename sanitized; failures logged, no partial corrupt writes.

## AgentExtractionResult (internal — backend)

**Record**: `IDocumentAgentExtractionBackend` return type

| Field                | Type   |
| -------------------- | ------ |
| ExtractedText        | string |
| TablesExtractedCount | int    |
| UsedSyncfusionTools  | bool   |

Produced by `StubDocumentAgentExtractionBackend` or `SyncfusionDocumentAgentExtractionBackend` (+ orchestrator).

## Relationships

```text
Requirement (EF)          DocumentAgentResult (transient)
     ↑                              │
     │ ApplyAgentExtraction         │ ProcessUploadAsync
     └──────────────────────────────┘
                    │
                    ▼
         agent-scans/* (volume files)
```

No FK between Requirement and agent files in v1 — storage paths returned in API for clerk awareness only (future: Requirement ↔ Document attachments deferred Phase 2+).
