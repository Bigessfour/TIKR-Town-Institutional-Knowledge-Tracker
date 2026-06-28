# TIKR Architecture

## Overview

TIKR (Town Institutional Knowledge Tracker) is a **local-first** web application for one-person town clerks in small Colorado municipalities. All data stays on the Synology NAS by default.

**Tagline:** *The Town Clerk's Second Brain*

**E2E diagrams:** Living Mermaid sources in [docs/diagrams/](diagrams/README.md) (feature map, API surface, clerk flows, deployment). Rendered copies are embedded below.

## Layer Diagram (quick reference)

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

## Architecture diagrams (E2E)

Sources: `docs/diagrams/*.mmd`. Update diagram and this section together when routes or features change.

### System context

```mermaid
C4Context
title TIKR System Context

Person(clerk, "Town Clerk", "One-person municipal clerk in a small Colorado town")
System(tikr, "TIKR", "Local-first institutional knowledge tracker on Synology NAS")
System_Ext(ollama, "Ollama", "Local LLM chat and nomic-embed-text embeddings")
System_Ext(grok, "xAI Grok", "Optional advanced reasoning when USE_GROK=true")
System_Ext(syncfusion, "Syncfusion Document SDK", "Licensed PDF/Word agent tools when USE_SYNCFUSION_AGENT_TOOLS=true")

Rel(clerk, tikr, "Uses via browser", "HTTPS")
Rel(tikr, ollama, "Chat, tag, embed, agent orchestration", "HTTP")
Rel(tikr, grok, "Ask Advanced AI only", "HTTPS")
Rel(tikr, syncfusion, "Agent-scan extraction", "In-process SDK")
```

### Containers (Docker Compose)

```mermaid
C4Container
title TIKR Containers (docker-compose)

Person(clerk, "Town Clerk", "Browser on clerk workstation or NAS LAN")

System_Boundary(tikr_boundary, "TIKR on Synology NAS") {
    Container(web, "TIKR.Web", "Blazor Interactive Server", ".NET 10, Syncfusion UI")
    Container(api, "TIKR.Api", "Minimal Web API", ".NET 10, EF Core, Hybrid AI")
    ContainerDb(db, "SQLite or PostgreSQL", "EF Core TikrDbContext", "Requirements, Documents, Knowledge, Audit")
    Container(fs, "Local file storage", "IFileStorageService", "/data/documents, agent-scans/")
    Container(ai, "Ollama", "ollama/ollama", "llama3.2:3b, nomic-embed-text")
}

Rel(clerk, web, "HTTPS", ":8080")
Rel(web, api, "TIKR_API_URL", "HTTP + optional JWT")
Rel(api, db, "Read/write", "EF Core")
Rel(api, fs, "Upload/delete files", "Volume mount")
Rel(api, ai, "HybridAiService, embeddings", "OLLAMA_HOST")
```

### Clerk feature map

Status: **green** = shipped · **gray** = deferred vNext.

```mermaid
flowchart TB
    subgraph pages["Clerk pages (MainLayout nav)"]
        dash["/ Dashboard<br/>Urgency, AI summary, quick actions"]
        cal["/calendar<br/>SfSchedule timeline"]
        req["/requirements<br/>Grid CRUD, CSV, print packet, AI Scan"]
        docs["/documents<br/>Upload, TreeView, Grid, semantic search, download, preview"]
        asst["/assistant<br/>SfAIAssistView + RAG context"]
        vault["/vault<br/>Tabs, RTE, Copy for New Clerk, SpeechToText"]
        sett["/settings<br/>Audit list, NAS + Ollama health"]
        users["/settings/users<br/>Admin user grid"]
        acct["/account<br/>Change password"]
        login["/login<br/>JWT sign-in"]
    end

    subgraph cross["Cross-cutting UX"]
        offline["Offline banner"]
        footer["NAS status footer"]
        theme["Theme selector"]
        keys["Keyboard shortcuts ? and g-nav"]
        help["PageHelp on main pages"]
        del["Confirm delete + undo toast"]
        a11y["Skip link, focus-visible"]
    end

    subgraph defer["Deferred vNext"]
        smart["Phase 6 Smart Paste / TextArea / Scheduler NL"]
        pdf["Phase 9 IMAP ingestion, rich DOCX preview"]
        req2["Requirements TreeGrid, Stepper, bulk import"]
        authv["Auth SMTP reset, Viewer role"]
    end

    clerk((Town Clerk)) --> pages
    clerk --> cross

    class dash,cal,req,docs,asst,vault,sett,users,acct,login,offline,footer,theme,keys,help,del,a11y done
    class smart,pdf,req2,authv defer

    classDef done fill:#d4edda,stroke:#28a745,color:#155724
    classDef defer fill:#e9ecef,stroke:#6c757d,color:#495057
```

Legacy `/knowledge` redirects to `/vault`.

### API surface (UI → endpoints)

```mermaid
flowchart LR
    subgraph web_ui["TIKR.Web pages"]
        W_Dash["Dashboard"]
        W_Req["Requirements"]
        W_Doc["Documents"]
        W_Vault["Vault"]
        W_Asst["Assistant"]
        W_Set["Settings"]
        W_Auth["Login / Account / Users"]
    end

    subgraph api_sys["System"]
        A_Health["GET /health"]
        A_Status["GET /api/system/local-status"]
    end

    subgraph api_crud["Domain CRUD"]
        A_Req["/api/requirements"]
        A_Doc["/api/documents"]
        A_DlContent["GET /api/documents/id/content"]
        A_Know["/api/knowledge"]
        A_Audit["GET /api/audit"]
    end

    subgraph api_ai["AI"]
        A_AIStat["GET /api/ai/status"]
        A_Prior["GET /api/ai/dashboard-priorities"]
        A_Tag["POST /api/ai/tag-document"]
        A_Adv["POST /api/ai/ask-advanced"]
        A_SSem["POST /api/ai/semantic-search"]
        A_KEmb["POST /api/ai/embed-document/id"]
        A_KSem["POST /api/ai/semantic-search-knowledge"]
        A_VEmb["POST /api/ai/embed-knowledge/id"]
        A_Agent["POST /api/ai/agent-scan"]
    end

    subgraph api_auth["Auth optional"]
        A_Login["POST /api/auth/login"]
        A_Me["GET /api/auth/me"]
        A_Pwd["POST /api/auth/change-password"]
        A_Users["/api/auth/users"]
    end

    W_Dash --> A_Prior
    W_Dash --> A_Status
    W_Req --> A_Req
    W_Req --> A_Agent
    W_Doc --> A_Doc
    W_Doc --> A_Tag
    W_Doc --> A_SSem
    W_Doc --> A_DlContent
    W_Vault --> A_Know
    W_Asst --> A_SSem
    W_Asst --> A_KSem
    W_Set --> A_Audit
    W_Set --> A_AIStat
    W_Set --> A_Status
    W_Auth --> A_Login
    W_Auth --> A_Me
    W_Auth --> A_Pwd
    W_Auth --> A_Users
```

### E2E flows (sequence)

**Clerk smoke** — `tests/e2e/clerk-smoke.spec.ts`

```mermaid
sequenceDiagram
    autonumber
    actor Clerk
    participant Web as TIKR.Web
    participant API as TIKR.Api

    Clerk->>Web: GET /
    Web->>API: GET /api/system/local-status
    API-->>Web: town, storage label, db mtime, Ollama flag
    Web-->>Clerk: Dashboard + Synology footer

    Clerk->>Web: Shift+/ keyboard shortcut
    Web-->>Clerk: Keyboard shortcuts dialog
```

**Requirements AI Scan** — `tests/e2e/requirements-agent-scan.spec.ts`

```mermaid
sequenceDiagram
    autonumber
    actor Clerk
    participant Web as TIKR.Web
    participant API as TIKR.Api
    participant Agent as DocumentAgentService
    participant Storage as Agent document storage
    participant Backend as Extraction backend

    Clerk->>Web: Upload file on /requirements
    Web->>API: POST /api/ai/agent-scan multipart
    API->>Storage: Store under agent-scans/
    API->>Agent: ScanAsync
    Agent->>Backend: Plain-text or Syncfusion PDF/Word extraction
    Backend-->>Agent: text, tables, UsedSyncfusionTools
    Agent-->>API: RequirementSuggestion
    API-->>Web: JSON result
    Web->>Clerk: Open Add requirement dialog with pre-filled notes
```

**Assistant RAG**

```mermaid
sequenceDiagram
    autonumber
    actor Clerk
    participant Web as TIKR.Web
    participant API as TIKR.Api
    participant AI as HybridAiService
    participant Ollama as Ollama

    Clerk->>Web: Send message on /assistant
    Web->>API: POST /api/ai/semantic-search query
    API->>AI: SemanticSearchDocumentsAsync
    AI->>Ollama: Embed query nomic-embed-text
    AI-->>API: Top-K document snippets
    Web->>API: POST /api/ai/semantic-search-knowledge query
    API->>AI: SemanticSearchKnowledgeAsync
    AI-->>API: Top-K vault snippets
    Web->>Ollama: Chat with RAG context prepended
    Ollama-->>Web: Assistant reply
    Web-->>Clerk: SfAIAssistView message
```

**Document lifecycle**

```mermaid
sequenceDiagram
    autonumber
    actor Clerk
    participant Web as TIKR.Web
    participant API as TIKR.Api
    participant FS as File storage
    participant DB as SQLite
    participant AI as HybridAiService
    participant Ollama as Ollama

    Clerk->>Web: Upload file on /documents
    Web->>API: POST /api/documents multipart
    API->>FS: Save to /data/documents
    API->>DB: Insert Document row plus FullTextContent if txt/md/csv
    API-->>Web: Created document DTO

    Clerk->>Web: Trigger AI tag
    Web->>API: POST /api/ai/tag-document
    API->>AI: TagDocumentAsync
    AI->>Ollama: Suggest tags from content
    AI->>AI: EmbedDocumentAsync best-effort
    AI-->>API: Tags plus embedding stored
    API-->>Web: Updated tags

    Clerk->>Web: Semantic search toggle
    Web->>API: POST /api/ai/semantic-search
    API->>AI: Cosine similarity over embeddings
    AI-->>Web: Ranked document matches
```

**Optional auth**

```mermaid
sequenceDiagram
    autonumber
    actor Clerk
    participant Web as TIKR.Web
    participant API as TIKR.Api
    participant Id as ASP.NET Identity

    Note over Clerk,Id: Only when TIKR_ADMIN_EMAIL plus password set

    Clerk->>Web: GET protected page
    Web-->>Clerk: Redirect to /login
    Clerk->>Web: Submit credentials SfDataForm
    Web->>API: POST /api/auth/login
    API->>Id: Validate user
    Id-->>API: JWT claims Admin or Clerk
    API-->>Web: Token in HttpOnly cookie
    Web->>API: GET /api/requirements Authorization Bearer
    API-->>Web: Data with audit UserId on writes

    opt Admin
        Clerk->>Web: /settings/users
        Web->>API: POST /api/auth/users
        API->>Id: Create Clerk account
    end
```

### Deployment (Synology NAS)

```mermaid
C4Deployment
title TIKR Deployment on Synology NAS

Deployment_Node(nas, "Synology NAS", "Container Manager, shared folder") {
    Deployment_Node(docker, "Docker host") {
        Container(web_c, "tikr-web", "Blazor", "Port 8080")
        Container(api_c, "tikr-api", "Minimal API", "Port 5000 to 8080")
        Container(ollama_c, "tikr-ollama", "Ollama", "Port 11434")
    }
    Deployment_Node(vol, "Volumes") {
        ContainerDb(data_vol, "tikr-data", "SQLite tikr.db plus uploaded files")
        ContainerDb(ollama_vol, "ollama-models", "Pulled LLM weights")
    }
}

Person(clerk, "Town Clerk", "Browser on LAN")

Rel(clerk, web_c, "HTTPS", "8080")
Rel(web_c, api_c, "TIKR_API_URL", "internal")
Rel(api_c, data_vol, "Read/write", "/data mount")
Rel(api_c, ollama_c, "OLLAMA_HOST", "internal")
Rel(ollama_c, ollama_vol, "Model cache")
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
- Embeddings: `nomic-embed-text` (documents, vault entries, semantic search, Assistant RAG)

## Database

- **Default:** SQLite at `/data/tikr.db` (Docker volume)
- **Switch to PostgreSQL:** Set `DATABASE_PROVIDER=Postgres` and Npgsql connection string

## Audit Trail

All create/update/delete operations on Requirements, Documents, and Knowledge entries are logged to `AuditLog` for CORA/public records compliance. When multi-user auth is enabled, `UserId` is set to the clerk's email from the JWT.

## Authentication (optional multi-user)

Auth is **off by default** (single-clerk open access). Set bootstrap credentials in `docker/.env` to enable:

| Variable | Purpose |
|----------|---------|
| `TIKR_ADMIN_EMAIL` | First admin account (with password below, auto-enables auth) |
| `TIKR_ADMIN_PASSWORD` | Initial admin password (change after first login) |
| `TIKR_JWT_SIGNING_KEY` | HMAC secret for API JWTs (required when auth enabled) |
| `TIKR_AUTH_ENABLED` | Optional explicit override (`true` / `false`) |

**Flow:** Blazor Web login → `POST /api/auth/login` → JWT stored in HttpOnly cookie → `TikrApiClient` sends `Authorization: Bearer` to API. Roles: `Admin` (user management), `Clerk` (full clerk workflows).

**UI:** `/login`, `/account` (change password), `/settings/users` (Admin only, Syncfusion Grid + DataForm).

## NAS setup (Synology)

1. Map `tikr-data` Docker volume to a shared folder (e.g. `/volume1/tikr/data`)
2. Import `docker/docker-compose.yml` in Container Manager
3. Set `SYNCFUSION_LICENSE_KEY` for the web container
4. Optional multi-user: set `TIKR_ADMIN_EMAIL`, `TIKR_ADMIN_PASSWORD`, and `TIKR_JWT_SIGNING_KEY` on **both** `tikr-api` and `tikr-web` via `docker/.env`
5. Pull Ollama models on first run:
   ```bash
   docker exec -it tikr-ollama ollama pull llama3.2:3b
   docker exec -it tikr-ollama ollama pull nomic-embed-text
   ```

## vNext (post-MVP)

See [incremental-plan.md](incremental-plan.md) and the **Deferred vNext** subgraph in [diagrams/03-clerk-feature-map.mmd](diagrams/03-clerk-feature-map.mmd).

- Phase 6 — Smart Paste, Smart TextArea, Scheduler natural-language recurring
- Phase 9 deferred — IMAP ingestion, rich DOCX / Spreadsheet preview
- Auth vNext — email password reset (SMTP), read-only `Viewer` role
- Requirements Phase 2 — TreeGrid, Stepper wizard, requirement ↔ document links
