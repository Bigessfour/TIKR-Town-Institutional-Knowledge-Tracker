# Data Model: Bulletproof Clerk RAG

## EmbeddingChunk (new)

| Field       | Type                       | Notes                                           |
| ----------- | -------------------------- | ----------------------------------------------- |
| Id          | Guid                       | PK                                              |
| SourceType  | enum Document \| Knowledge | Discriminator                                   |
| SourceId    | Guid                       | Document.Id or KnowledgeEntry.Id                |
| ChunkIndex  | int                        | 0-based order within source                     |
| Content     | string                     | Passage text                                    |
| Embedding   | byte[]                     | Packed float32 vector                           |
| ContentHash | string                     | SHA-256 hex of Content (or source text version) |
| DisplayName | string?                    | FileName or Title for citations                 |
| Facet       | string?                    | SuggestedFolder or Category name                |
| UpdatedAt   | DateTime                   | UTC                                             |

**Indexes**: `(SourceType, SourceId)`, `(SourceType, Facet)`

**Rules**:
- On re-embed of a source: delete existing chunks for that SourceType+SourceId, insert new set.
- Skip re-embed if hash of full source text matches stored source-level hash when all chunks present and unchanged (implementation: hash of BuildEmbeddingText / BuildKnowledgeEmbeddingText).

## Document / KnowledgeEntry (existing)

- Keep `Embedding` byte[] as optional legacy/summary vector (first chunk or full-text truncated embed).
- `FullTextContent` / `Content` remain the text source for chunking.

## Search request extensions

| Field    | Default  | Purpose                 |
| -------- | -------- | ----------------------- |
| Query    | required | User text               |
| TopK     | 3        | Max hits                |
| MinScore | 0.38     | Floor for returned hits |
| Folder   | null     | Document facet filter   |
| Category | null     | Knowledge facet filter  |

## Search response extensions

| Field              | Purpose                            |
| ------------------ | ---------------------------------- |
| EmbeddingAvailable | false when query embed failed      |
| Hits[].Snippet     | Richer passage (up to ~1000 chars) |
| Hits[].ChunkIndex  | Optional; -1 or null for legacy    |
