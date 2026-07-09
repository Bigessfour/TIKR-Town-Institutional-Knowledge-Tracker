# Ship TIKR to production (GitHub + GHCR + NAS)

Checklist for maintainers closing active development and publishing **v1.0.0** (or next semver) for Deb’s NAS install.

**Clerk install (Deb):** [deb-nas-install.md](deb-nas-install.md)  
**Living status:** [action-items.md](action-items.md), [incremental-plan.md](incremental-plan.md)

---

## Phase 1 — Land code on `main`

1. Open PR from `feature/*` or `fix/*` (not direct commits to `main`).
2. Wait for green checks:
   - **TIKR CI** (`build-and-test`, Playwright when applicable)
   - **Trunk** (gitleaks, markdown, docker lint, `dotnet format` in workflow)
3. Merge to `main`.

Local verification before or after merge:

```bash
dotnet test TIKR.sln --configuration Release
trunk check --all
./scripts/done-detector.sh
```

---

## Phase 2 — Final sign-off (human + docs)

| Gate | Owner | Evidence |
|------|--------|----------|
| Phase 0 PR #3 — Deb NAS doc | Maintainer | [deb-nas-install.md](deb-nas-install.md) linked from README |
| Phase 0 PR #4 — Walkthrough | Deb / stand-in | Record session using [demo-deb.md](demo-deb.md); check bus-factor box in [action-items.md](action-items.md) |
| Condensed Syncfusion / UI pass | Maintainer | Note date in [syncfusion-control-audit.md](syncfusion-control-audit.md) |
| Layer 2 Done Detector | Maintainer | [action-items.md](action-items.md) Project-Level gate |

**Post-ship (do not block tag):** full axe pass, SfPdfViewer preview, IMAP, 10C-C badge, licensed NAS agent smoke — tracked under **Post-ship / v1.1+** in action-items.

---

## Phase 3 — Tag and GHCR release

Release workflow: [.github/workflows/release.yml](../.github/workflows/release.yml)

Tags must match `vMAJOR.MINOR.PATCH` (e.g. `v1.0.0`).

```bash
git checkout main
git pull origin main
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions will:

- Build and push `ghcr.io/<owner>/tikr-api` and `tikr-web` with tags `v1.0.0`, `latest`
- Create a GitHub Release with generated notes

Verify packages on GitHub → Packages.

---

## Phase 4 — NAS deploy

On Synology (SSH or task scheduler):

```bash
cd /path/to/tikr-checkout   # or minimal folder with compose + .env + validate-prod.sh
# docker/.env: TIKR_VERSION=v1.0.0, paths, SYNCFUSION_LICENSE_KEY
docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --pull always
./validate-prod.sh
```

Full clerk steps: [deb-nas-install.md](deb-nas-install.md).

---

## Phase 5 — Before first GHCR images exist

Build from source on the NAS or a build host:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up --build -d
```

Switch to `docker-compose.prod.yml` + GHCR after the first successful release workflow run.

---

## Rollback

1. Set `TIKR_VERSION` to the previous semver in `docker/.env`.
2. `docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --pull always`
3. Data remains in `TIKR_DATA_PATH`; only containers change.

---

## Related

| Doc | Purpose |
|-----|---------|
| [docker/README.md](../docker/README.md) | Dev compose, host Ollama, Mac port 5001 |
| [validate-prod.sh](../validate-prod.sh) | Automated smoke after deploy |
| [demo-deb.md](demo-deb.md) | Clerk demo script |
| [AGENTS.md](../AGENTS.md) | Agent rules + done-detector |
