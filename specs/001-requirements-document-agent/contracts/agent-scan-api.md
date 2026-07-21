# Contract: Agent Scan API

**Feature**: `001-requirements-document-agent`
**Endpoint**: `POST /api/ai/agent-scan`
**Implementation**: `src/TIKR.Api/Program.cs`

## Request

| Property     | Value                                            |
| ------------ | ------------------------------------------------ |
| Content-Type | `multipart/form-data`                            |
| Body         | Single file field (first file in form)           |
| Max size     | 100 MB                                           |
| Auth         | Same as other API routes — JWT when auth enabled |

### Supported file types (behavior)

| Extension              | `USE_SYNCFUSION_AGENT_TOOLS=false` | `USE_SYNCFUSION_AGENT_TOOLS=true` + license  |
| ---------------------- | ---------------------------------- | -------------------------------------------- |
| `.txt`, `.md`, `.csv`  | Plain-text extraction              | Plain-text extraction                        |
| `.pdf`                 | Heuristic table count              | Syncfusion PDF extraction + optional archive |
| `.docx`, `.doc`        | Limited / heuristic                | Syncfusion Word extraction                   |
| `.xlsx`, `.pptx`, etc. | Heuristic                          | Orchestrator tool registry                   |

## Response `200 OK`

JSON body: `DocumentAgentResult` (camelCase serialized)

```json
{
  "suggestedTitle": "Annual Financial Report",
  "extractedText": "...",
  "suggestedDueDate": "2026-08-21",
  "suggestedRecurrence": "Annual",
  "suggestedCategory": "Budget",
  "tablesExtractedCount": 3,
  "storagePath": "agent-scans/report.ai-archive.pdf",
  "processedLocally": true,
  "usedSyncfusionTools": true,
  "originalStoragePath": "agent-scans/report.pdf",
  "processedStoragePath": "agent-scans/report.ai-archive.pdf",
  "structuredTables": null
}
```

## Error responses

| Status | Condition                                                                |
| ------ | ------------------------------------------------------------------------ |
| 400    | Not multipart, missing file, empty file, invalid filename, file > 100 MB |
| 500    | Unhandled extraction/storage failure (logged server-side)                |

## Client contract

**Web client**: `TikrApiClient.ScanDocumentWithAgentAsync(Stream, fileName)` → `POST /api/ai/agent-scan`

**UI flow** (`Requirements.razor`):

1. User selects file → client posts multipart
2. Display `FormatAgentScanMessage(result)` + extraction badge
3. User clicks Apply → `ApplyAgentExtraction(result)` → create/edit dialog

## Related endpoints (Requirements CRUD — FR-001)

| Method | Path                     | Purpose |
| ------ | ------------------------ | ------- |
| GET    | `/api/requirements`      | List    |
| GET    | `/api/requirements/{id}` | Get one |
| POST   | `/api/requirements`      | Create  |
| PUT    | `/api/requirements/{id}` | Update  |
| DELETE | `/api/requirements/{id}` | Delete  |

Document download (FR-012): `GET /api/documents/{id}/content`
