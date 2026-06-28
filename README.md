# TIKR – Town Institutional Knowledge Tracker

[![TIKR CI](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/ci.yml)
[![Trunk](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/trunk-check.yaml/badge.svg)](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/trunk-check.yaml)

**The Town Clerk's Second Brain**

Local-first web application for one-person town clerks in small Colorado municipalities (starting with Wiley, CO). Manage deadlines, documents, and institutional knowledge entirely on your Synology NAS — no cloud dependency required.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 (LTS) |
| Frontend | Blazor Interactive Server + Syncfusion Blazor |
| Backend | Minimal Web API |
| Database | SQLite (default), PostgreSQL (optional) |
| AI | Ollama (local) + optional xAI Grok via `Microsoft.Extensions.AI` |
| Containers | Docker Compose |

## Repository Structure

```
├── TIKR.sln / TIKR.slnx
├── global.json
├── docker/           # Docker Compose + Dockerfiles
├── docs/             # Architecture + AI tooling documentation
├── tests/            # Unit + integration tests (see tests/README.md)
├── scripts/          # Seed data reference
└── src/
    ├── TIKR.Web/           # Blazor UI
    ├── TIKR.Api/           # REST API
    ├── TIKR.Shared/        # Domain models & DTOs
    └── TIKR.Infrastructure/ # EF Core, storage, AI
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Synology Container Manager
- [Syncfusion Community License](https://www.syncfusion.com/products/communitylicense) (free for eligible small orgs)
- [Ollama](https://ollama.com/) (included in Docker Compose)

For Cursor IDE AI tooling (Syncfusion MCP, Agent Skills, Ollama MCP), see **[docs/ai-tooling.md](docs/ai-tooling.md)**.

Agent skills are **not** committed (`.agents/` is gitignored). Install locally from the pinned lock file:

```bash
npx skills add syncfusion/blazor-ui-components-skills -y
# skills-lock.json pins versions for reproducible installs
```

## Development Workflow

For Cursor and AI agents: **[AGENTS.md](AGENTS.md)** (rules) and **[docs/incremental-plan.md](docs/incremental-plan.md)** (phased roadmap).

### 1. Secrets setup

```bash
cp docker/.env.example docker/.env
# Edit docker/.env with your keys (Syncfusion license, optional Grok key)
```

For local `dotnet run` without Docker, use [user secrets](#local-development-user-secrets--recommended) or place keys in `docker/.env` (loaded in Development via `DotNetEnv`).

### 2. Local development

```bash
dotnet restore
cd src/TIKR.Web && dotnet run
# Web: http://localhost:8080 (API must be running separately)
```

Or run the full stack:

```bash
docker compose -f docker/docker-compose.yml up --build
```

### 3. Git workflow

- `main` is protected — all changes via pull requests
- CI runs on every PR: **[TIKR CI](.github/workflows/ci.yml)** (build, test, Docker smoke) and **[Trunk](.github/workflows/trunk-check.yaml)** (lint, format, secret scan)
- Merge when both checks pass

### 4. Tests

```bash
dotnet test TIKR.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

See **[tests/README.md](tests/README.md)** for the coverage policy (90% target; CI floor ramps up over time). Current suite: **226 tests** across Shared, Infrastructure, Api integration, and Web (bUnit).

## Local Development (detailed)

### 1. Clone and restore

```bash
git clone https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker.git
cd TIKR-Town-Institutional-Knowledge-Tracker
dotnet restore
```

### 2. Set Syncfusion license (optional for dev, required to remove trial banner)

```bash
export SYNCFUSION_LICENSE_KEY="your-license-key"
```

### 3. Start Ollama (if not using Docker)

```bash
ollama pull llama3.2:3b
ollama pull nomic-embed-text
ollama serve
```

### 4. Run the API

```bash
cd src/TIKR.Api
dotnet run
# API: http://localhost:5000
```

### 5. Run the Web app (separate terminal)

```bash
cd src/TIKR.Web
dotnet run
# Web: http://localhost:8080
```

## Docker (Recommended)

From the repo root:

```bash
export SYNCFUSION_LICENSE_KEY="your-license-key"   # optional
docker compose -f docker/docker-compose.yml up --build
```

| Service | URL |
|---------|-----|
| Web UI | http://localhost:8080 |
| API | http://localhost:5000 |
| Ollama | http://localhost:11434 |

### Pull AI models (first run)

```bash
docker exec -it tikr-ollama ollama pull llama3.2:3b
docker exec -it tikr-ollama ollama pull nomic-embed-text
```

## Synology NAS Deployment

1. Copy the repo to your NAS or clone via SSH.
2. Open **Container Manager** → **Project** → **Create**.
3. Set path to the repo and compose file: `docker/docker-compose.yml`.
4. Map the `tikr-data` volume to a shared folder (e.g. `/volume1/tikr/data`).
5. Create `docker/.env` from `docker/.env.example` with your Syncfusion license key (and optional auth bootstrap vars — see [Environment Variables](#optional-multi-user-auth)).
6. Deploy and pull Ollama models (see above).

All data (SQLite DB + uploaded documents) persists in the `/data` volume.

## Secrets Management

Never commit real keys to GitHub. Use the layered approach below.

### Docker / Synology

```bash
cp docker/.env.example docker/.env
# Edit docker/.env with your real keys
docker compose -f docker/docker-compose.yml up --build
```

Compose loads `docker/.env` automatically via `env_file`.

### Local development (user secrets — recommended)

ASP.NET loads user secrets automatically in Development when `UserSecretsId` is set:

```bash
cd src/TIKR.Api
dotnet user-secrets set "GROK_API_KEY" "xai-..."
dotnet user-secrets set "USE_GROK" "false"

cd ../TIKR.Web
dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "your_key_here"
```

### Local development (.env fallback)

In Development, the app also loads `.env` and `docker/.env` from the repo root if present (via `DotNetEnv`).

### On Synology NAS

- Place `docker/.env` in your project folder before deploying the Container Manager project, or
- Set environment variables individually in Container Manager → Project → Environment

## Environment Variables

| Variable | Service | Default | Description |
|----------|---------|---------|-------------|
| `SYNCFUSION_LICENSE_KEY` | Web | — | Syncfusion Community License key (runtime components) |
| `SYNCFUSION_API_KEY` | Cursor MCP only | — | Syncfusion account API key for Blazor MCP — see [docs/ai-tooling.md](docs/ai-tooling.md) |
| `TIKR_API_URL` | Web | `http://localhost:5000` | API base URL |
| `DATABASE_PROVIDER` | API | `Sqlite` | `Sqlite` or `Postgres` |
| `ConnectionStrings__Default` | API | `Data Source=tikr.db` | Database connection |
| `FILE_STORAGE_PATH` | API | `data/documents` | Document storage path |
| `OLLAMA_HOST` | API | `http://localhost:11434` | Ollama server URL |
| `OLLAMA_CHAT_MODEL` | API | `llama3.2:3b` | Chat model name |
| `USE_GROK` | API | `false` | Enable xAI Grok for advanced AI |
| `GROK_API_KEY` | API | — | xAI API key (required if USE_GROK=true) |
| `GROK_MODEL` | API | `grok-2-latest` | Grok model name |

### Optional multi-user auth

Auth is **off by default** (single-clerk open access). Set all three bootstrap variables in `docker/.env` to enable login on both **tikr-api** and **tikr-web** (Compose `env_file` applies to both):

| Variable | Service | Description |
|----------|---------|-------------|
| `TIKR_ADMIN_EMAIL` | API + Web | First admin account email |
| `TIKR_ADMIN_PASSWORD` | API + Web | Initial admin password (change after first login) |
| `TIKR_JWT_SIGNING_KEY` | API + Web | HMAC secret for API JWTs (≥32 chars) |
| `TIKR_AUTH_ENABLED` | API + Web | Optional override (`true` / `false`) |

Flow: Blazor login → `POST /api/auth/login` → JWT in HttpOnly cookie → protected `/api/*` routes. Roles: `Admin` (user management), `Clerk` (full workflows). See [docs/architecture.md](docs/architecture.md).

## Features (v1 Scaffold)

- **Deadline Calendar** — Pre-seeded Colorado municipal deadlines + custom requirements
- **Requirements Manager** — CRUD grid at `/requirements` with urgency filters, CSV export, and bus-factor banner
- **Document Management** — Upload, AI auto-tagging, search
- **Knowledge Vault** — "If I'm Gone" institutional knowledge entries
- **Hybrid AI** — Local Ollama chat on `/assistant`; Grok for "Ask Advanced AI" (API-gated)
- **Audit Trail** — All mutations logged for compliance

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Health check |
| GET/POST/PUT/DELETE | `/api/requirements` | Deadline CRUD |
| GET/POST/DELETE | `/api/documents` | Document upload & list |
| GET/POST/PUT/DELETE | `/api/knowledge` | Knowledge vault CRUD |
| GET | `/api/audit` | Audit log (read-only) |
| GET | `/api/ai/status` | AI service status |
| GET | `/api/ai/dashboard-priorities` | Dashboard priorities |
| POST | `/api/ai/tag-document` | Ollama auto-tagging |
| POST | `/api/ai/ask-advanced` | Grok escalation (gated) |

## Switching to PostgreSQL

```yaml
# docker-compose.yml (tikr-api environment)
DATABASE_PROVIDER: Postgres
ConnectionStrings__Default: Host=postgres;Database=tikr;Username=tikr;Password=yourpassword
```

## License

Application code: [MIT](LICENSE). Syncfusion components require a [Community License](https://www.syncfusion.com/products/communitylicense) for eligible organizations.
