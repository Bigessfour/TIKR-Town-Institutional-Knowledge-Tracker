# TIKR Docker (local + NAS)

## Quick start

```bash
cp docker/.env.example docker/.env   # add SYNCFUSION_LICENSE_KEY
docker compose -f docker/docker-compose.yml --env-file docker/.env up --build -d
```

| URL | Service |
|-----|---------|
| http://localhost:8080 | TIKR.Web |
| http://localhost:5000 | TIKR.Api (`/health`) — NAS / Linux default |
| http://localhost:5001 | TIKR.Api when `TIKR_API_HOST_PORT=5001` (macOS AirPlay on :5000) |
| http://localhost:11434 | Ollama (container) |

Pull models after first up:

```bash
docker exec -it tikr-ollama ollama pull llama3.2:3b
docker exec -it tikr-ollama ollama pull nomic-embed-text
```

## Host Ollama already on :11434

If `docker compose up` fails with `bind: address already in use` on 11434, use the override:

```bash
docker compose -f docker/docker-compose.yml -f docker/docker-compose.host-ollama.yml --env-file docker/.env up --build -d
```

This skips the `tikr-ollama` container and points `tikr-api` at `host.docker.internal:11434`.

On macOS, the **host Ollama** task also applies `docker-compose.dev-mac.yml` and sets `TIKR_API_HOST_PORT=5001` (AirPlay often occupies `:5000`). API health: http://localhost:5001/health

## Production (Synology / GHCR)

Use pre-built images after a release tag (`v1.0.0+`):

```bash
cp docker/.env.example docker/.env
mkdir -p /volume1/tikr/data /volume1/tikr/ollama
docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --pull always
./validate-prod.sh
```

| Variable | Default | Purpose |
|----------|---------|---------|
| `TIKR_VERSION` | `latest` | GHCR tag (`ghcr.io/bigessfour/tikr-*`) |
| `TIKR_DATA_PATH` | `/volume1/tikr/data` | SQLite + documents bind mount |
| `TIKR_OLLAMA_PATH` | `/volume1/tikr/ollama` | Ollama model cache |

`USE_GROK=false` by default. Healthchecks require `curl` in API/Web images (see Dockerfiles).

Demo walkthroughs: [demo-code-platoon.md](../docs/demo-code-platoon.md) · [demo-deb.md](../docs/demo-deb.md)

## IDE (Cursor / VS Code)

Install recommended extensions when prompted (or Extensions view):

| Extension | ID | Role |
|-----------|-----|------|
| Container Tools | `ms-azuretools.vscode-containers` | Compose up/down, container tree, .NET attach |
| Docker DX | `docker.docker` | Dockerfile lint, Compose outline, BuildKit debug |

Workspace settings live in [`.vscode/settings.json`](../.vscode/settings.json).

**Tasks** (Terminal → Run Task):

- **TIKR: Docker Compose Up** — full stack with container Ollama
- **TIKR: Docker Compose Up (host Ollama override)** — when port 11434 is taken
- **TIKR: Docker Compose Down**
- **TIKR: Docker Compose Logs**
- **TIKR: Pull Ollama models (container)** — after full stack with `tikr-ollama`
- **TIKR: Pull Ollama models (host)** — when using host Ollama override

**Docker DX build debug:** Run and Debug → *Docker DX: Build API* or *Build Web* (requires Docker Desktop 4.46+ and Buildx 0.29+). Set breakpoints on `RUN` lines in `docker/Dockerfile.*`.

Right-click `docker/docker-compose.yml` → **Compose Up** / **Compose Down** also works via Container Tools.

## Build context

Docker builds use the repo root as context (see `context: ..` in compose). A root [`.dockerignore`](../.dockerignore) keeps transfers small — avoid removing those exclusions or builds will stall on multi-GB context uploads.

## Synology NAS

Import `docker/docker-compose.yml` in Container Manager; map `tikr-data` to a shared folder. See [docs/architecture.md](../docs/architecture.md#nas-setup-synology).
