# Research: Bulletproof Clerk RAG

## Decision: Keep Ollama + in-process cosine; add chunks

**Rationale**: Phase 9 already stores float vectors as BLOBs and ranks in memory. Town-clerk scale does not need pgvector yet. Chunking fixes the 4k truncation gap without a stack swap.

**Alternatives considered**:
- FAISS/LangChain Python sidecar — duplicates ops surface; rejected per constitution minimal change.
- Bedrock Titan — cloud dependency for core path; rejected (local-first).
- pgvector now — premature; can migrate later behind same DTOs.

## Decision: Hybrid score = 0.7 cosine + 0.3 keyword overlap

**Rationale**: Exact filenames and ordinance numbers underperform on embeddings alone. Simple token Jaccard/overlap is enough at small scale and easy to unit test.

**Alternatives considered**:
- BM25 index — more moving parts for little gain at clerk volume.
- Reranker model — second model call; defer.

## Decision: Default minScore = 0.38

**Rationale**: Cosine on nomic-style vectors for unrelated text often sits well below ~0.35–0.4 in practice; gate weak hits out of assistant context. Tunable via request `MinScore`.

**Alternatives considered**: Relative gap from top score only — harder to explain; use absolute floor first.

## Decision: Content-hash skip on re-embed

**Rationale**: Avoid re-calling Ollama for unchanged vault/docs; speeds reindex and tag paths.

## Decision: Legacy Document/KnowledgeEntry.Embedding fallback

**Rationale**: Existing rows and tests seed whole-item vectors. Search uses chunks when present for a source type, else falls back so deploy is non-breaking before reindex.
