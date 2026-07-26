# Quickstart: Bulletproof Clerk RAG

## Prerequisites

- Ollama running with `nomic-embed-text` (and chat model for Assistant)
- API + Web up (`docker compose` or local)
- DB migrated (includes `EmbeddingChunks`)

## Validate chunk search (API)

```bash
# After uploading/tagging a long doc or creating a vault entry:
curl -s -X POST http://localhost:5000/api/ai/reindex-embeddings | jq .

curl -s -X POST http://localhost:5000/api/ai/semantic-search \
  -H 'Content-Type: application/json' \
  -d '{"query":"YOUR DISTINCTIVE PHRASE","topK":3,"minScore":0.3}' | jq .
```

Expect `embeddingAvailable: true` and a hit whose `snippet` contains the phrase.

## Validate Assistant grounding

1. Open `/assistant`
2. Ask a question matching vault/doc content → answer cites Sources
3. Ask something unrelated → declines to invent; no fake procedure
4. Stop Ollama and ask again → soft “search unavailable” notice

## Automated

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~HybridAiService|FullyQualifiedName~TextChunker|FullyQualifiedName~AssistantPrompt"
```

## Ops note

After changing embedding model or migration: `POST /api/ai/reindex-embeddings`. See [docs/ai-tooling.md](../../docs/ai-tooling.md).
