# TIKR Architecture

## Overview

TIKR (Town Institutional Knowledge Tracker) is a **local-first** web application for one-person town clerks in small Colorado municipalities. All data stays on the Synology NAS by default.

**Tagline:** *The Town Clerk's Second Brain*

## Layer Diagram

```
┌─────────────────────────────────────────────────────────┐
│  TIKR.Web (Blazor Interactive Server + Syncfusion)      │
│  Dashboard · Calendar · Documents · Knowledge · Settings│
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP (TIKR_API_URL)
┌──────────────────────────▼──────────────────────────────┐
│  TIKR.Api (Minimal API, .NET 10)                        │
│  Requirements · Documents · Knowledge · Audit · AI      │
└──────────────────────────┬──────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ SQLite / PG   │  │ Local Files   │  │ Ollama + Grok │
│ (EF Core)     │  │ /data/docs    │  │ (Hybrid AI)   │
└───────────────┘  └───────────────┘  └───────────────┘
```

## Projects

| Project | Purpose |
|---------|---------|
| `TIKR.Shared` | Domain entities, DTOs, enums, service interfaces |
| `TIKR.Infrastructure` | EF Core DbContext, file storage, AI services |
| `TIKR.Api` | HTTP endpoints, DI wiring, database migration on startup |
| `TIKR.Web` | Blazor UI with Syncfusion components |

## Hybrid AI Strategy

1. **Routine tasks** (auto-tagging, dashboard priorities, summaries) → **Ollama** via `Microsoft.Extensions.AI` + **OllamaSharp**
2. **Advanced reasoning** (explicit user action only) → **xAI Grok** when `USE_GROK=true`

```
User action → HybridAiService
                 ├── Local: IChatClient (OllamaApiClient)
                 └── Advanced: GrokService (gated)
```

### Models (default)

- Chat: `llama3.2:3b` or `phi3:mini`
- Embeddings: `nomic-embed-text` (scaffolded for future semantic search)

## Database

- **Default:** SQLite at `/data/tikr.db` (Docker volume)
- **Switch to PostgreSQL:** Set `DATABASE_PROVIDER=Postgres` and Npgsql connection string

## Audit Trail

All create/update/delete operations on Requirements, Documents, and Knowledge entries are logged to `AuditLog` for CORA/public records compliance.

## Deployment (Synology NAS)

1. Map `tikr-data` Docker volume to a shared folder (e.g. `/volume1/tikr/data`)
2. Import `docker/docker-compose.yml` in Container Manager
3. Set `SYNCFUSION_LICENSE_KEY` for the web container
4. Pull Ollama models on first run:
   ```bash
   docker exec -it tikr-ollama ollama pull llama3.2:3b
   docker exec -it tikr-ollama ollama pull nomic-embed-text
   ```

## vNext (not in initial scaffold)

- Authentication (single-clerk → multi-user)
- Semantic search with stored embeddings
- Email ingestion (IMAP / forward-to-folder)
- PDF preview and full-text extraction
