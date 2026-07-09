# TIKR on Synology NAS — Install guide for the town clerk

This guide is for **Deb** (or whoever operates TIKR in production). It assumes **Docker images from GitHub Container Registry (GHCR)** after a release tag such as `v1.0.0`.

Developers: see [ship-to-production.md](ship-to-production.md) for tagging and CI release steps.

---

## What you need

| Item | Notes |
|------|--------|
| Synology NAS with **Container Manager** | DS225+ or similar |
| Shared folders | Default paths: `/volume1/tikr/data` (database + documents), `/volume1/tikr/ollama` (AI models) |
| Syncfusion license | [Community license](https://www.syncfusion.com/products/communitylicense) — key goes in `docker/.env` only |
| Network | Clerk browser → NAS Web UI on port **8080** |

Optional: Grok API key if you enable cloud AI (`USE_GROK=true` in `docker/.env`).

---

## One-time setup on the NAS

1. **Copy project files** to the NAS (SSH or File Station). You need at least:
   - `docker/docker-compose.prod.yml`
   - `docker/.env.example` → copy to `docker/.env`
   - `validate-prod.sh` (repo root)

2. **Create folders** (SSH example):

   ```bash
   mkdir -p /volume1/tikr/data /volume1/tikr/ollama
   ```

3. **Edit `docker/.env`** (never commit this file):

   ```bash
   cp docker/.env.example docker/.env
   ```

   Set at minimum:

   - `SYNCFUSION_LICENSE_KEY` — your Syncfusion key
   - `TIKR_DATA_PATH=/volume1/tikr/data`
   - `TIKR_OLLAMA_PATH=/volume1/tikr/ollama`
   - `TIKR_VERSION=v1.0.0` (or `latest` after first release)
   - `TIKR_TOWN_NAME` and `TIKR_STORAGE_LABEL` (footer text)

4. **Start TIKR**:

   ```bash
   docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --pull always
   ```

5. **Pull AI models** (first run, can take several minutes):

   ```bash
   docker exec -it tikr-ollama ollama pull llama3.2:3b
   docker exec -it tikr-ollama ollama pull nomic-embed-text
   ```

6. **Validate**:

   ```bash
   ./validate-prod.sh
   ```

---

## Daily use

| What | URL |
|------|-----|
| TIKR Web | `http://<nas-hostname>:8080` |
| API health (support) | `http://<nas-hostname>:5000/health` |

Walkthrough script for demos and training: [demo-deb.md](demo-deb.md).

---

## Where your data lives (“if I’m gone”)

| Data | Location |
|------|----------|
| SQLite database | `TIKR_DATA_PATH/tikr.db` |
| Uploaded documents | `TIKR_DATA_PATH/documents/` |
| Vault / handover content | In the database + export via **Vault → Generate Complete Handover Package** (PDF) |
| Audit trail | Settings page + `/api/audit` |

Back up the **`/volume1/tikr/data`** share regularly (Hyper Backup or equivalent).

---

## Troubleshooting

| Problem | What to try |
|---------|-------------|
| Web loads but “offline” banner | API container down — `docker compose ... logs tikr-api` |
| AI slow or unavailable | Ollama models not pulled; NAS CPU busy — clerk workflows still work without AI |
| Agent scan errors | Set `USE_SYNCFUSION_AGENT_TOOLS=true` only after license is in `.env`; see [nas-agent-tools-setup.md](nas-agent-tools-setup.md) |
| Mac dev note | Port **5001** for API is a **Mac AirPlay** workaround only; NAS uses **5000** |

---

## Optional: login for multiple users

If `TIKR_ADMIN_EMAIL`, `TIKR_ADMIN_PASSWORD`, and `TIKR_JWT_SIGNING_KEY` are set in `docker/.env`, the login page is enabled. See [README.md](../README.md) — Optional multi-user auth.
