# TIKR – Town Institutional Knowledge Tracker

[![TIKR CI](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/actions/workflows/ci.yml)

**The Town Clerk's Second Brain**

Local-first web application for one-person town clerks in small Colorado municipalities (starting with Wiley, CO). Manage deadlines, documents, and institutional knowledge entirely on your Synology NAS — no cloud dependency required.

## Tech Stack

| Layer      | Technology                                                       |
| ---------- | ---------------------------------------------------------------- |
| Runtime    | .NET 10 (LTS)                                                    |
| Frontend   | Blazor Interactive Server + Syncfusion Blazor                    |
| Backend    | Minimal Web API                                                  |
| Database   | SQLite (default), PostgreSQL (optional)                          |
| AI         | Ollama (local) + optional xAI Grok via `Microsoft.Extensions.AI` |
| Containers | Docker Compose                                                   |

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

For **Spec-Driven Development** with GitHub Spec Kit (Cursor `/speckit-*` skills), see **[docs/spec-kit.md](docs/spec-kit.md)**.

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

For local `dotnet run` without Docker, use [user secrets](#local-development-user-secrets) or place keys in `docker/.env` (loaded in Development via `DotNetEnv`).

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
- CI runs on every PR: **[TIKR CI](.github/workflows/ci.yml)** (build, test, Trunk lint/format, Ollama failure triage)
- Merge when both checks pass

### 4. Tests

```bash
dotnet test TIKR.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

See **[tests/README.md](tests/README.md)** for the coverage policy (90% target; CI floor ramps up over time). Current suite: **277 tests** across Shared, Infrastructure, Api integration, and Web (bUnit). Playwright smoke tests live in `tests/e2e/` (manual against Docker).

## Local Development (detailed)

### 1. Clone and restore

```bash
git clone https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker.git
cd TIKR-Town-Institutional-Knowledge-Tracker
dotnet restore
```

### 2. Sync secrets from macOS Passwords (recommended on Mac)

Store your Syncfusion license in the **Passwords** app (generic password; see `scripts/sync-syncfusion-license-key.sh --help` for accepted labels). Then sync into gitignored `docker/.env` and dotnet user-secrets — the key is never committed:

```bash
./scripts/setup-local-secrets.sh
# Document SDK agent tools: ./scripts/setup-local-secrets.sh --enable-agent-tools
```

Manual fallback: `cp docker/.env.example docker/.env` and edit, or `export SYNCFUSION_LICENSE_KEY="your-license-key"`.

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
./scripts/setup-local-secrets.sh   # macOS: Passwords → docker/.env (once)
docker compose -f docker/docker-compose.yml --env-file docker/.env up --build
```

| Service | URL                    |
| ------- | ---------------------- |
| Web UI  | http://localhost:8080  |
| API     | http://localhost:5000  |
| Ollama  | http://localhost:11434 |

### Pull AI models (first run)

```bash
docker exec -it tikr-ollama ollama pull llama3.2:3b
docker exec -it tikr-ollama ollama pull nomic-embed-text
```

### Production (Synology DS225+ / GHCR)

After a release tag (e.g. `v1.0.0`) publishes images to GHCR:

```bash
cp docker/.env.example docker/.env   # SYNCFUSION_LICENSE_KEY + /volume1 paths
mkdir -p /volume1/tikr/data /volume1/tikr/ollama
docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --pull always
./validate-prod.sh
```

Before the first GHCR release, build from source with `docker/docker-compose.yml` instead. See [docker/README.md](docker/README.md) and [docs/ship-to-production.md](docs/ship-to-production.md).

### Install for clerks (Windows) — canonical day-1

**Deb and Paige:** install with **`Setup-TIKR.exe`** on the shared municipal Windows PC. No login required on this trusted machine. Clerk one-pager: [docs/clerk-windows-install.md](docs/clerk-windows-install.md).

| Step             | Who                   | Action                                                                             |
| ---------------- | --------------------- | ---------------------------------------------------------------------------------- |
| 1. Build payload | IT                    | `./scripts/package-thumb-drive.sh` → `publish/TIKR-Deploy/`                        |
| 2. Compile Setup | IT (Windows + Inno 6) | See [installer/README.md](installer/README.md) → `installer/Output/Setup-TIKR.exe` |
| 3. Install       | Clerk / IT            | Run Setup → Syncfusion license → Start Menu **TIKR - Clerk's Vault**               |
| 4. Daily use     | Deb / Paige           | Desktop **Start TIKR** → `http://localhost:8080`                                   |
| 5. Backup        | Named owner           | Copy `C:\ProgramData\TIKR` regularly                                               |

Alternate (no Setup.exe): copy the USB folder and use `Start-TIKR.bat` — [docs/windows-thumb-drive-deploy.md](docs/windows-thumb-drive-deploy.md).

Publish binaries only (no deploy scripts):

```bash
./scripts/publish-tikr.sh
```

### Demo scripts

| Audience                      | Doc                                                            |
| ----------------------------- | -------------------------------------------------------------- |
| Deb & Paige (Windows install) | [docs/clerk-windows-install.md](docs/clerk-windows-install.md) |
| Code Platoon (developers)     | [docs/demo-code-platoon.md](docs/demo-code-platoon.md)         |
| Deb (clerk walkthrough)       | [docs/demo-deb.md](docs/demo-deb.md)                           |
| Deb (NAS Phase 2)             | [docs/deb-nas-install.md](docs/deb-nas-install.md)             |
| Maintainer (release / GHCR)   | [docs/ship-to-production.md](docs/ship-to-production.md)       |

## Synology NAS Deployment (Phase 2)

1. Copy the repo to your NAS or clone via SSH.
2. Open **Container Manager** → **Project** → **Create**.
3. Set path to the repo and compose file: `docker/docker-compose.yml`.
4. Map the `tikr-data` volume to a shared folder (e.g. `/volume1/tikr/data`).
5. Create `docker/.env` from `docker/.env.example` with your Syncfusion license key (and optional auth bootstrap vars — see [Environment Variables](#optional-multi-user-auth)).
6. Deploy and pull Ollama models (see above).

All data (SQLite DB + uploaded documents) persists in the `/data` volume.

## Secrets Management

Never commit real keys to GitHub. Use the layered approach below.

### macOS Passwords → local env (recommended)

Keep secrets in the **Passwords** app; sync into gitignored stores with one command:

```bash
./scripts/setup-local-secrets.sh
# Optional: --enable-agent-tools, --skip-grok, --skip-mcp
```

This writes `SYNCFUSION_LICENSE_KEY` to `docker/.env` and TIKR.Api/TIKR.Web user-secrets, plus optional `GROK_API_KEY` and MCP `SYNCFUSION_API_KEY`. Re-run after updating Passwords.

Syncfusion only: `./scripts/sync-syncfusion-license-key.sh --all`

### Docker / Synology

```bash
cp docker/.env.example docker/.env
# Edit docker/.env with your real keys (or run setup-local-secrets.sh on Mac)
docker compose -f docker/docker-compose.yml --env-file docker/.env up --build
```

Compose loads `docker/.env` automatically via `env_file`.

### Local development (user secrets)

`setup-local-secrets.sh` populates user-secrets automatically. Manual alternative:

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

| Variable                             | Service         | Default                  | Description                                                                              |
| ------------------------------------ | --------------- | ------------------------ | ---------------------------------------------------------------------------------------- |
| `SYNCFUSION_LICENSE_KEY`             | Web             | —                        | Syncfusion Community License key (runtime components)                                    |
| `SYNCFUSION_API_KEY`                 | Cursor MCP only | —                        | Syncfusion account API key for Blazor MCP — see [docs/ai-tooling.md](docs/ai-tooling.md) |
| `TIKR_API_URL`                       | Web             | `http://localhost:5000`  | API base URL                                                                             |
| `DATABASE_PROVIDER`                  | API             | `Sqlite`                 | `Sqlite` or `Postgres`                                                                   |
| `ConnectionStrings__Default`         | API             | `Data Source=tikr.db`    | Database connection                                                                      |
| `FILE_STORAGE_PATH`                  | API             | `data/documents`         | Document storage path                                                                    |
| `OLLAMA_HOST`                        | API             | `http://localhost:11434` | Ollama server URL                                                                        |
| `OLLAMA_CHAT_MODEL`                  | API             | `llama3.2:3b`            | Chat model name (optional: `tikr-clerk` — see [docs/ai-tooling.md](docs/ai-tooling.md))  |
| `USE_GROK`                           | API             | `false`                  | Enable xAI Grok for advanced AI                                                          |
| `GROK_API_KEY`                       | API             | —                        | xAI API key (required if USE_GROK=true)                                                  |
| `GROK_MODEL`                         | API             | `grok-4.3`               | xAI chat model ([docs](https://docs.x.ai/docs/models); aliases: `grok-latest`)           |
| `USE_SYNCFUSION_AGENT_TOOLS`         | API             | `false`                  | Enable Syncfusion Document SDK agent-scan (PDF/Word/Excel/PPT)                           |
| `USE_SYNCFUSION_AGENT_ORCHESTRATION` | API             | `false`                  | Ollama tool loop over Syncfusion tools (requires agent tools + Ollama)                   |
| `TIKR_AGENT_STORAGE_KEY`             | API             | —                        | Optional AES-256-GCM for agent-scan blobs on NAS                                         |
| `TIKR_LIBRARY_SCAN_PATH`             | API             | —                        | Existing NAS document library root (recursive scan → copy → tag/embed for Assistant)     |
| `TIKR_LIBRARY_SCAN_INTERVAL_SECONDS` | API             | `300`                    | Background library scan poll interval                                                    |
| `TIKR_OCR_ENABLED`                   | API             | `true`                   | OCR scanned PDF/Word when native text is sparse (Syncfusion Tesseract)                   |
| `TIKR_TESSADATA_PATH`                | API             | —                        | Optional Tesseract language data folder override                                         |
| `TIKR_EMAIL_INBOX_PATH`              | API             | —                        | Forward-to-folder email drop inbox                                                       |

Document SDK setup: [docs/sf-document-agent-tools.md](docs/sf-document-agent-tools.md) · NAS smoke tracker: [docs/nas-agent-tools-setup.md](docs/nas-agent-tools-setup.md)

### Optional multi-user auth

Auth is **off by default** (Deb + Paige on a trusted shared PC need no login). Enable later if the app leaves that trusted machine. Set all three bootstrap variables in `docker/.env` to enable login on both **tikr-api** and **tikr-web** (Compose `env_file` applies to both). Example:

```bash
TIKR_ADMIN_EMAIL=clerk@yourtown.gov
TIKR_ADMIN_PASSWORD=your-strong-bootstrap-password
TIKR_JWT_SIGNING_KEY=your-local-hmac-secret-at-least-32-characters
```

| Variable               | Service   | Description                                       |
| ---------------------- | --------- | ------------------------------------------------- |
| `TIKR_ADMIN_EMAIL`     | API + Web | First admin account email                         |
| `TIKR_ADMIN_PASSWORD`  | API + Web | Initial admin password (change after first login) |
| `TIKR_JWT_SIGNING_KEY` | API + Web | HMAC secret for API JWTs (≥32 chars)              |
| `TIKR_AUTH_ENABLED`    | API + Web | Optional override (`true` / `false`)              |

Flow: Blazor login → `POST /api/auth/login` → JWT in HttpOnly cookie → protected `/api/*` routes. Roles: `Admin` (user management), `Clerk` (full workflows). See [docs/architecture.md](docs/architecture.md).

**Manual smoke test (auth enabled):**

1. Uncomment/set bootstrap vars in `docker/.env` (use a strong password and random ≥32-char signing key).
2. `docker compose -f docker/docker-compose.yml up --build`
3. Open Web → redirect to `/login` → sign in with `TIKR_ADMIN_EMAIL` / `TIKR_ADMIN_PASSWORD`.
4. Confirm `/dashboard` loads; create a requirement; check `/api/audit` shows your email as `UserId`.
5. As Admin, open `/settings/users` → add a Clerk user → sign out → sign in as Clerk.

## Features (v1 Scaffold)

- **Deadline Calendar** — Pre-seeded Colorado municipal deadlines + custom requirements
- **Requirements Manager** — CRUD grid at `/requirements` with urgency filters, CSV export, and bus-factor banner
- **Document Management** — Upload, AI auto-tagging, search
- **Knowledge Vault** — "If I'm Gone" institutional knowledge entries
- **Hybrid AI** — Local Ollama chat on `/assistant`; Grok for "Ask Advanced AI" (API-gated)
- **Audit Trail** — All mutations logged for compliance

## API Endpoints

| Method              | Route                          | Description             |
| ------------------- | ------------------------------ | ----------------------- |
| GET                 | `/health`                      | Health check            |
| GET/POST/PUT/DELETE | `/api/requirements`            | Deadline CRUD           |
| GET/POST/DELETE     | `/api/documents`               | Document upload & list  |
| GET/POST/PUT/DELETE | `/api/knowledge`               | Knowledge vault CRUD    |
| GET                 | `/api/audit`                   | Audit log (read-only)   |
| GET                 | `/api/ai/status`               | AI service status       |
| GET                 | `/api/ai/dashboard-priorities` | Dashboard priorities    |
| POST                | `/api/ai/tag-document`         | Ollama auto-tagging     |
| POST                | `/api/ai/ask-advanced`         | Grok escalation (gated) |

## Switching to PostgreSQL

```yaml
# docker-compose.yml (tikr-api environment)
DATABASE_PROVIDER: Postgres
ConnectionStrings__Default: Host=postgres;Database=tikr;Username=tikr;Password=yourpassword
```

## License

Application code: [MIT](LICENSE). Syncfusion components require a [Community License](https://www.syncfusion.com/products/communitylicense) for eligible organizations.
