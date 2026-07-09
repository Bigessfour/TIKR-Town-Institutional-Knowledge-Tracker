# Windows thumb-drive deploy (Dell laptop test)

Use this path when you want **TIKR on a Windows PC without Docker** — for example copying from a USB stick to a Dell Inspiron before the Synology NAS is ready.

**Production for the clerk** remains **Docker on Synology** ([deb-nas-install.md](deb-nas-install.md)). This laptop package is a self-contained smoke test and demo vehicle.

## What you get

| Piece | Role |
|-------|------|
| `TIKR.Api.exe` | SQLite, documents, AI API (`http://localhost:5000`) |
| `TIKR.Web.exe` | Blazor UI (`http://localhost:8080`) |
| `Data\` | Database + uploads (persists next to the exes) |
| PowerShell scripts | One-time firewall + secrets; daily start/stop |

There is **no single `TIKR.exe`** — the app is two processes (same as the optional VM path in [demo-deb.md](demo-deb.md)).

**Guided tour:** already shipped — Settings → **Show me around TIKR**, plus **Tour this page** on each route ([clerk-tour-deployment.md](clerk-tour-deployment.md)). No separate `/tour` page.

## Build the USB package (Mac or dev PC)

```bash
./scripts/package-thumb-drive.sh
# Output: publish/TIKR-Deploy/ and publish/TIKR-Deploy-win-x64.zip
```

Copy `publish/TIKR-Deploy` (or the zip) to the thumb drive.

Fast iteration without tests:

```bash
SKIP_TESTS=1 ./scripts/package-thumb-drive.sh
```

If Syncfusion fails to start with a single-file exe:

```bash
PUBLISH_SINGLE_FILE=false ./scripts/package-thumb-drive.sh
```

## On the Dell (first time)

1. Copy `TIKR-Deploy` to Desktop (or `C:\TIKR`).
2. **Run as Administrator:** `Install-TIKR.ps1` (firewall + `tikr-secrets.ps1` template).
3. Edit `tikr-secrets.ps1` — set `SYNCFUSION_LICENSE_KEY` ([Community license](https://www.syncfusion.com/products/communitylicense)).
4. Optional but recommended: install [Ollama](https://ollama.com), then:

   ```powershell
   ollama pull llama3.2:3b
   ollama pull nomic-embed-text
   ```

5. Double-click `Start-TIKR.bat` → browser opens `http://localhost:8080`.

See `README-QuickStart.txt` inside the deploy folder for clerk-facing steps.

## NAS (Phase 2)

Do **not** rely on the Windows folder for NAS production. Use existing Docker:

- Dev/local build: `docker/docker-compose.yml`
- Production: `docker/docker-compose.prod.yml` + `docker/.env`

`Deploy-To-NAS.ps1` in the deploy folder is a checklist helper only. Full steps: [deb-nas-install.md](deb-nas-install.md), [docker/README.md](../docker/README.md).

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Blank Syncfusion UI / license banner | `tikr-secrets.ps1` and restart |
| Settings shows Ollama disconnected | Ollama running on laptop; `OLLAMA_HOST=http://localhost:11434` |
| Web loads but data errors | API window still open; `http://localhost:5000/health` |
| HTTPS redirect loop | Use `http://localhost:8080` (not https) |

## Related

- [ship-to-production.md](ship-to-production.md) — GHCR tag and NAS release
- [scripts/publish-tikr.sh](../scripts/publish-tikr.sh) — publish only (no USB layout)