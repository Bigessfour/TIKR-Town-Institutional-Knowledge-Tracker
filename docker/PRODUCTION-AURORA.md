# TIKR — Aurora / App Runner production notes

Same images run on Synology (SQLite) or AWS (Postgres). Blazor Server UI (`TIKR.Web`) and API (`TIKR.Api`) business logic are unchanged.

## Production Dockerfiles (Blazor + API)

| File | Builds |
| ---- | ------ |
| [`Dockerfile.web`](./Dockerfile.web) | Blazor Interactive Server UI |
| [`Dockerfile.api`](./Dockerfile.api) | ASP.NET Core API + EF |

```bash
# From repo root
docker build -t tikr-web:latest -f docker/Dockerfile.web .
docker build -t tikr-api:latest -f docker/Dockerfile.api .
```

App Runner / ECS: one service (or task) per image. Only **tikr-api** needs the database connection string.

## Compose

| File | Database | Use |
| ---- | -------- | --- |
| [`docker-compose.yml`](./docker-compose.yml) | SQLite (default) | Dev / build |
| [`docker-compose.prod.yml`](./docker-compose.prod.yml) | **SQLite** via `.env` | Synology NAS clerk workflow |
| [`docker-compose.prod.postgres.yml`](./docker-compose.prod.postgres.yml) | **Postgres** | Aurora/RDS / App Runner verification |

Keep NAS on `docker-compose.prod.yml` + `DATABASE_PROVIDER=Sqlite`.

## Environment variables (Aurora / RDS)

Set on **tikr-api** (and in `docker/.env` when using compose):

| Variable | Required | Notes |
| -------- | -------- | ----- |
| `DATABASE_PROVIDER` | Yes | `Postgres` (default if unset remains `Sqlite`) |
| `ConnectionStrings__Default` | Yes | Npgsql string; use `SSL Mode=Require` for Aurora |
| `FILE_STORAGE_PATH` | No | Default `/data/documents` |
| `TIKR_DATA_PATH` | Host/compose | Host folder mounted at `/data` for documents + DP keys |
| `SYNCFUSION_LICENSE_KEY` | Yes in practice | Never bake into the image |
| `TIKR_API_URL` | Web → API | Internal URL, e.g. `http://tikr-api:8080` |
| `TIKR_PUBLIC_API_URL` | Browser | Public API URL for downloads |
| `OLLAMA_HOST` / `USE_GROK` | AI | Optional; Grok via `GROK_API_KEY` |

### Example Aurora connection string

```text
Host=tikr.cluster-xxxx.us-east-2.rds.amazonaws.com;Port=5432;Database=tikr;Username=tikr;Password=***;SSL Mode=Require;Trust Server Certificate=true
```

### App Runner task env (API)

```text
DATABASE_PROVIDER=Postgres
ConnectionStrings__Default=Host=...;Port=5432;Database=tikr;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
FILE_STORAGE_PATH=/data/documents
ASPNETCORE_ENVIRONMENT=Production
```

## Local / Synology SQLite (unchanged)

```text
DATABASE_PROVIDER=Sqlite
ConnectionStrings__Default=Data Source=/data/tikr.db
```

Provider selection is already implemented in `TIKR.Infrastructure` (`UseSqlite` vs `UseNpgsql`).

## Related

- [`docs/architecture.md`](../docs/architecture.md) — Database section
- [`docker/.env.example`](./.env.example) — full template including Aurora comments
- Suite hosting: Wiley Widget `docs/aws-low-cost-architecture.md`
