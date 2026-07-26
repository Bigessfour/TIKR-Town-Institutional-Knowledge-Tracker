# Feature Specification: Bulletproof Clerk RAG

**Feature Branch**: `002-bulletproof-clerk-rag`

**Created**: 2026-07-23

**Status**: Implemented

**Verification**: All tasks in `tasks.md` closed; independently verified by post-mortem audit + Release test suite (2026-07-25).

**Input**: User description: "Add best possible context awareness and retrieval for the documentation system without replacing Ollama/HybridAiService/Postgres. Make embedding and retrieval bulletproof and future-proof so the assistant answers from real docs/vault with citations."

**Related docs**: [incremental-plan.md](../../docs/incremental-plan.md) Phase 9 RAG, [ai-tooling.md](../../docs/ai-tooling.md)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Grounded assistant answers with sources (Priority: P1)

Deb asks the AI Assistant a question about a town procedure or uploaded document. The assistant answers using retrieved institutional knowledge and documents, lists the sources it used, and refuses to invent procedures when nothing relevant is found.

**Why this priority**: Prevents hallucinated clerk guidance — core trust for a one-person town office.

**Independent Test**: Seed vault/doc content, ask a matching and a non-matching question on `/assistant` (or via API search + prompt builder unit tests); matching answers cite sources; non-matching declines to invent.

**Acceptance Scenarios**:

1. **Given** relevant vault and/or document text is indexed, **When** Deb asks a related question, **Then** the reply is based on that content and names the source file or vault title.
2. **Given** no relevant indexed content, **When** Deb asks an unrelated question, **Then** the assistant states it has no matching docs/vault entries instead of inventing a procedure.
3. **Given** the local embedding service is unavailable, **When** Deb asks a question, **Then** she sees a clear soft notice that search is unavailable — not a silent empty context with a confident guess.

---

### User Story 2 - Long documents remain searchable (Priority: P1)

Deb uploads a long ordinance or minutes PDF. After indexing, she can ask about content that appears well into the document — not only the beginning — and still get a useful, sourced answer.

**Why this priority**: Real municipal PDFs exceed single-embedding limits; losing the middle/end breaks “hit by a bus” continuity.

**Independent Test**: Index a long fixture whose distinctive phrase appears after the first ~4k characters; semantic search returns a hit containing that phrase.

**Acceptance Scenarios**:

1. **Given** a long document with distinctive text past the opening pages, **When** Deb searches or asks about that topic, **Then** retrieval returns a passage from the correct later section.
2. **Given** document text is updated and re-indexed, **When** Deb searches again, **Then** results reflect the new text (stale chunks are replaced).

---

### User Story 3 - Reliable find by name or topic (Priority: P2)

Deb searches Documents or asks the assistant using an exact filename fragment, ordinance number, or folder/category. Results prefer exact matches alongside meaning-based matches, and can be narrowed by folder or vault category.

**Why this priority**: Clerks often remember exact labels; pure meaning search alone misses them.

**Independent Test**: Seed two similarly themed items with different filenames/titles; query the exact name and confirm it ranks first; apply folder/category filter and confirm out-of-facet items are excluded.

**Acceptance Scenarios**:

1. **Given** multiple indexed documents, **When** Deb queries an exact filename fragment, **Then** that document ranks at or near the top.
2. **Given** vault entries in different categories, **When** Deb filters by category (or asks within a constrained search), **Then** only that category is considered.

---

### User Story 4 - Reindex and operational clarity (Priority: P3)

After a model or schema change, Deb (or an operator) can reindex all documents and vault entries so search stays healthy, and documentation explains how local AI embedding vs chat roles work.

**Why this priority**: Future-proofs operations without requiring a stack rewrite.

**Independent Test**: Call reindex; confirm chunks exist for seeded items; docs describe when to reindex.

**Acceptance Scenarios**:

1. **Given** existing documents/entries without chunk index rows, **When** reindex runs, **Then** they become searchable via the new retrieval path.
2. **Given** Ollama is offline during upload/tag, **When** Deb uploads, **Then** tagging still succeeds (best-effort embed) and reindex later fills the gap.

---

### Edge Cases

- Empty query returns no hits without error.
- Documents with no extractable text are skipped for chunking with a clear embed failure reason.
- Low-score weak matches are excluded from assistant context.
- Duplicate near-identical chunks from overlap do not flood the prompt (dedupe by source + best score).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST index document and vault text as overlapping passages so long content remains searchable beyond a single truncated embedding.
- **FR-002**: System MUST retrieve passages using both meaning similarity and keyword/token overlap (hybrid ranking).
- **FR-003**: System MUST apply a minimum relevance threshold so weak matches are not injected into assistant context.
- **FR-004**: Assistant MUST answer using retrieved context when present, cite source filenames/titles, and refuse to invent when no relevant hits pass the threshold.
- **FR-005**: Assistant MUST surface a soft failure when embedding/search is unavailable instead of silently omitting context.
- **FR-006**: System MUST re-chunk and re-embed when source text changes (content-hash skip when unchanged).
- **FR-007**: System MUST support optional folder (documents) and category (vault) filters on semantic search.
- **FR-008**: System MUST provide a reindex-all operation to backfill embeddings/chunks for existing content.
- **FR-009**: Upload/tag and vault save MUST remain usable when the local embedding service is offline (best-effort indexing).
- **FR-010**: Automated tests MUST cover chunking, hybrid ranking, min-score filtering, and grounded no-hit behavior without a live embedding service.
- **FR-011**: Operator docs MUST explain local embed vs chat roles, citations, and when to reindex.

### Key Entities

- **Indexed passage (chunk)**: A slice of document or vault text with its vector, hash, order, and citation labels (filename/title, folder/category).
- **Document**: Uploaded municipal file; source of extractable text for indexing.
- **Knowledge entry**: Vault how-to / contact / tribal / voice note; source of vault indexing.
- **Search hit**: Ranked passage with score and citation metadata returned to Documents UI and Assistant.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A distinctive phrase placed after the first 4,000 characters of a fixture document is returned in top search hits for a matching query.
- **SC-002**: For an unrelated query with no strong matches, the assistant response (or grounded prompt path) declines to invent and does not attach weak sources.
- **SC-003**: For a matching query, the assistant path includes at least one named source (filename or vault title) in the user-visible answer or sources block.
- **SC-004**: `dotnet test TIKR.sln --configuration Release` passes with mocked embeddings (no live Ollama required in CI).
- **SC-005**: Exact filename/title queries rank the matching item above a thematically similar distractor in unit/integration tests.

## Assumptions

- Keep existing local-first stack (Ollama for embed/chat, existing AI service, relational storage) — no cloud embedding requirement for core path.
- Town-clerk scale (hundreds of docs, not millions) allows in-process ranking over stored vectors.
- Developer repo RAG MCP is out of scope for this feature; clerk Documents + Vault + Assistant are in scope.
- Legacy whole-document vectors may remain during migration as fallback until chunks exist.
