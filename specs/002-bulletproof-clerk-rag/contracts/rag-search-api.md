# Contract: RAG search & reindex API

## POST `/api/ai/semantic-search`

**Request** (`SemanticSearchRequest`):

```json
{
  "query": "building permit fee",
  "topK": 5,
  "minScore": 0.38,
  "folder": "Permits"
}
```

**Response** (`SemanticSearchResponse`):

```json
{
  "query": "building permit fee",
  "considered": 12,
  "embeddingAvailable": true,
  "hits": [
    {
      "documentId": "...",
      "fileName": "fee-schedule.pdf",
      "suggestedFolder": "Permits",
      "snippet": "…building permit fee is $125…",
      "score": 0.71,
      "chunkIndex": 3
    }
  ]
}
```

- If embedding service unavailable: `embeddingAvailable=false`, `hits=[]`.
- Hits below `minScore` are omitted.

## POST `/api/ai/semantic-search-knowledge`

Same request shape; use `category` instead of `folder`. Hits use `entryId`, `title`, `category`.

## POST `/api/ai/embed-document/{id}`

Unchanged shape (`EmbedDocumentResponse`). Behavior: chunk + embed all passages; update legacy `Document.Embedding`.

## POST `/api/ai/embed-knowledge/{id}`

Same for vault entries.

## POST `/api/ai/reindex-embeddings` (new)

**Response**:

```json
{
  "documentsAttempted": 10,
  "documentsEmbedded": 8,
  "knowledgeAttempted": 5,
  "knowledgeEmbedded": 5,
  "errors": ["Document abc: Embedding generator unavailable"]
}
```

Best-effort: continues on per-item failures.

## Client wiring (2026-07-25)

- `TikrApiClient.SemanticSearchDocumentsAsync(query, topK, folder)` sends `folder` on document search.
- `TikrApiClient.SemanticSearchKnowledgeAsync(query, topK, category)` sends `category` on vault search.
- `Documents.razor` passes the selected tree folder (except Uncategorized) to the API during semantic mode.
- `Assistant.razor` optional vault category dropdown scopes knowledge search.
