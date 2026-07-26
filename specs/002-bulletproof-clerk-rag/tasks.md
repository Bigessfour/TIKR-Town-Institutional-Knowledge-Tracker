# Tasks: Bulletproof Clerk RAG

**Input**: Design documents from `/specs/002-bulletproof-clerk-rag/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/
**Tests**: Required per FR-010 / SC-004

## Phase 1: Setup

- [x] T001 Create Spec Kit feature directory and branch `002-bulletproof-clerk-rag`
- [x] T002 Persist `.specify/feature.json` → `specs/002-bulletproof-clerk-rag`

## Phase 2: Foundational

- [x] T003 [P] Add `EmbeddingSourceType` enum in `src/TIKR.Shared/Enums/EmbeddingSourceType.cs`
- [x] T004 [P] Add `EmbeddingChunk` entity in `src/TIKR.Shared/Entities/EmbeddingChunk.cs`
- [x] T005 Extend `AiDto.cs` search request/response + `ReindexEmbeddingsResponse`
- [x] T006 Extend `IHybridAiService` with `ReindexAllEmbeddingsAsync`
- [x] T007 Register `EmbeddingChunks` DbSet + model config in `TikrDbContext.cs`
- [x] T008 Add EF migration `AddEmbeddingChunks`
- [x] T009 [P] Implement `TextChunker` in `src/TIKR.Infrastructure/Services/TextChunker.cs`
- [x] T010 [P] Unit tests for `TextChunker` in `tests/TIKR.Infrastructure.Tests/Services/TextChunkerTests.cs`

## Phase 3: User Story 1 — Grounded assistant (P1)

- [x] T011 [US1] Add minScore filtering + `EmbeddingAvailable` in `HybridAiService` search methods
- [x] T012 [US1] Grounded system prompt + context packing helpers in `PageWorkflowHelpers.cs`
- [x] T013 [US1] Wire Assistant.razor: passages, citations, fail-soft search unavailable
- [x] T014 [US1] Tests for no-hit / low-score / embedding-unavailable in Infrastructure + prompt helpers

## Phase 4: User Story 2 — Chunked long docs (P1)

- [x] T015 [US2] Rewrite `EmbedDocumentAsync` / `EmbedKnowledgeEntryAsync` to chunk+store; content-hash skip
- [x] T016 [US2] Search prefer chunks with legacy Document/KnowledgeEntry.Embedding fallback
- [x] T017 [US2] Update TagDocument + knowledge save paths to use chunk embed
- [x] T018 [US2] Tests: long-text phrase after 4k chars retrieved; hash skip; existing semantic tests updated

## Phase 5: User Story 3 — Hybrid + filters (P2)

- [x] T019 [US3] Hybrid keyword boost in ranking
- [x] T020 [US3] Folder/Category filters on search
- [x] T021 [US3] Tests: exact filename ranks above distractor; category filter excludes others

## Phase 6: User Story 4 — Reindex + docs (P3)

- [x] T022 [US4] `POST /api/ai/reindex-embeddings` in `Program.cs` + `TikrApiClient` helper
- [x] T023 [US4] Document Ollama embed vs chat + reindex in `docs/ai-tooling.md`
- [x] T024 [US4] Update `docs/action-items.md` / function inventory notes for new endpoint

## Phase 7: Polish

- [x] T025 Run `dotnet test TIKR.sln --configuration Release` and fix failures
- [x] T026 Mark Spec Kit tasks complete; update incremental-plan Phase 9 note if needed

## Dependencies

- Foundational (T003–T010) before US stories
- US1 can ship before chunks if minScore applied to legacy vectors; this implementation does US1+US2 together for one coherent PR
- US3 depends on chunk/legacy search path
- US4 depends on embed rewrite

## MVP

T003–T018 (grounding + chunks) delivers SC-001–SC-004. US3/US4 complete the feature.
