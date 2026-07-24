# Implementation Plan: Bulletproof Clerk RAG

**Branch**: `002-bulletproof-clerk-rag` | **Date**: 2026-07-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-bulletproof-clerk-rag/spec.md`

## Summary

Upgrade clerk document/vault retrieval from one-vector-per-item short snippets to chunked, hybrid, threshold-gated RAG with grounded assistant prompts and citations — without replacing Ollama, `HybridAiService`, or Postgres/SQLite.

## Technical Context

**Language/Version**: .NET 10 (pinned in `global.json`)
**Primary Dependencies**: EF Core, Microsoft.Extensions.AI, OllamaSharp (`nomic-embed-text` + chat model), Blazor Interactive Server
**Storage**: SQLite default / PostgreSQL optional; new `EmbeddingChunks` table (byte-packed float vectors)
**Testing**: xUnit + FluentAssertions + Moq; mock embedding generator (existing test pattern)
**Target Platform**: Local NAS / Windows PC clerk deploy
**Project Type**: Full-stack Blazor + Minimal API
**Performance Goals**: Sub-second search at town-clerk scale (≤ ~10k chunks)
**Constraints**: Local-first; best-effort embed when Ollama offline; no LangChain/FAISS/Bedrock rewrite
**Scale/Scope**: Hundreds of documents + vault entries; Assistant + Documents semantic search

## Constitution Check

| Gate                       | Status                                                         |
| -------------------------- | -------------------------------------------------------------- |
| I. Local-first             | Pass — Ollama only for embed/chat                              |
| II. Minimal proven changes | Pass — extend `HybridAiService` + DTOs; new chunk entity       |
| III. Test + CI gates       | Pass — mocked embed tests required                             |
| IV. Layer boundaries       | Pass — AI logic stays in Infrastructure/Api; Web packs context |
| V. RAG-aware development   | Pass — builds on Phase 9 patterns                              |

Post-design: unchanged — no unjustified new abstractions.

## Project Structure

### Documentation (this feature)

```text
specs/002-bulletproof-clerk-rag/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/rag-search-api.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (touched)

```text
src/TIKR.Shared/Entities/EmbeddingChunk.cs
src/TIKR.Shared/Enums/EmbeddingSourceType.cs
src/TIKR.Shared/DTOs/AiDto.cs
src/TIKR.Shared/Interfaces/IHybridAiService.cs
src/TIKR.Infrastructure/Data/TikrDbContext.cs
src/TIKR.Infrastructure/Data/Migrations/*_AddEmbeddingChunks.cs
src/TIKR.Infrastructure/Services/TextChunker.cs
src/TIKR.Infrastructure/Services/HybridAiService.cs
src/TIKR.Api/Program.cs
src/TIKR.Web/Helpers/PageWorkflowHelpers.cs
src/TIKR.Web/Components/Pages/Assistant.razor
src/TIKR.Web/Services/TikrApiClient.cs
docs/ai-tooling.md
tests/TIKR.Infrastructure.Tests/Services/*
tests/TIKR.Web.Tests/... (prompt builder if present)
```

## Complexity Tracking

| Addition               | Why needed                           | Simpler alternative rejected    |
| ---------------------- | ------------------------------------ | ------------------------------- |
| `EmbeddingChunk` table | Long docs need multi-passage vectors | Keep 4k truncate — fails SC-001 |
| Hybrid keyword boost   | Exact ordinance/filename recall      | Vector-only — fails SC-005      |
| minScore gate          | Stop weak-context hallucination      | Always top-K — fails SC-002     |

## Implementation Approach

1. **Grounding (US1)**: Prompt + context packing + citations + minScore on search results; fail-soft when embed unavailable.
2. **Chunks (US2)**: `TextChunker`, `EmbeddingChunk` migration, rewrite embed/search to chunks with legacy fallback.
3. **Hybrid + filters (US3)**: Token overlap blend + Folder/Category on `SemanticSearchRequest`.
4. **Ops (US4)**: `POST /api/ai/reindex-embeddings` + docs.
