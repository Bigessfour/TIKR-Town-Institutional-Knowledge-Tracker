#!/usr/bin/env bash
# Deploy TIKR to Synology NAS (Mr_Storage) over Tailscale SSH.
# Preserves NAS-specific compose overrides (town-docs mount, API entrypoint).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NAS_HOST="${TIKR_NAS_HOST:-mr-storage}"
NAS_DIR="${TIKR_NAS_DIR:-/volume1/tikr/app/docker}"
VERSION="${1:-1.0.1}"

# Synology often disables the scp/sftp subsystem; stream files over SSH instead.
nas_copy_file() {
	local src="$1"
	local dest="$2"
	ssh "${NAS_HOST}" "mkdir -p \"$(dirname "${dest}")\" && cat > \"${dest}\"" <"${src}"
}

echo "=== TIKR NAS deploy → ${NAS_HOST} (${VERSION}) ==="

if ! tailscale status >/dev/null 2>&1; then
	echo "Starting Tailscale..."
	tailscale up
fi

if ! ssh -o BatchMode=yes -o ConnectTimeout=10 "${NAS_HOST}" 'echo ok' >/dev/null 2>&1; then
	echo "❌ Cannot SSH to ${NAS_HOST}. Check Tailscale and ~/.ssh/config."
	exit 1
fi

echo "→ Sync prod compose + validate script"
ssh "${NAS_HOST}" "mkdir -p /volume1/tikr/app/docker /volume1/tikr/app/tests/fixtures/agent-scan"
nas_copy_file "${ROOT}/docker/docker-compose.prod.yml" "/volume1/tikr/app/docker/docker-compose.prod.yml.base"
nas_copy_file "${ROOT}/validate-prod.sh" "/volume1/tikr/app/validate-prod.sh"
nas_copy_file "${ROOT}/tests/fixtures/agent-scan/wiley-periodic-report.txt" "/volume1/tikr/app/tests/fixtures/agent-scan/wiley-periodic-report.txt"
ssh "${NAS_HOST}" "chmod +x /volume1/tikr/app/validate-prod.sh"

echo "→ Pull images, patch version, restart stack"
ssh "${NAS_HOST}" bash -s -- "${NAS_DIR}" "${VERSION}" <<'REMOTE'
set -euo pipefail
DIR="$1"
VER="$2"
cd "$DIR"

# Keep NAS overrides if compose already customized; otherwise use repo base.
if [[ ! -f docker-compose.prod.yml ]]; then
  cp docker-compose.prod.yml.base docker-compose.prod.yml
fi

sudo sed -i "s|^TIKR_VERSION=.*|TIKR_VERSION=${VER}|" .env 2>/dev/null || true
grep -q '^TIKR_VERSION=' .env || echo "TIKR_VERSION=${VER}" | sudo tee -a .env >/dev/null

# Prefer GHCR semver tags; fall back to local nas-main if pull fails.
if sudo /usr/local/bin/docker pull "ghcr.io/bigessfour/tikr-api:${VER}" \
  && sudo /usr/local/bin/docker pull "ghcr.io/bigessfour/tikr-web:${VER}"; then
  sudo sed -i "s|image:.*tikr-api.*|image: ghcr.io/bigessfour/tikr-api:${VER}|" docker-compose.prod.yml
  sudo sed -i "s|image:.*tikr-web.*|image: ghcr.io/bigessfour/tikr-web:${VER}|" docker-compose.prod.yml
fi

sudo /usr/local/bin/docker compose -f docker-compose.prod.yml --env-file .env pull tikr-api tikr-web 2>/dev/null || true
sudo /usr/local/bin/docker compose -f docker-compose.prod.yml --env-file .env up -d

echo "→ Waiting for API health..."
for _ in $(seq 1 30); do
  PORT=$(grep '^TIKR_API_HOST_PORT=' .env | cut -d= -f2)
  PORT="${PORT:-5050}"
  if curl -sf "http://localhost:${PORT}/health" >/dev/null 2>&1; then
    echo "✅ API healthy on port ${PORT}"
    exit 0
  fi
  sleep 5
done
echo "❌ API did not become healthy in time"
exit 1
REMOTE

API_PORT="$(ssh "${NAS_HOST}" "grep '^TIKR_API_HOST_PORT=' ${NAS_DIR}/.env | cut -d= -f2" 2>/dev/null || echo 5050)"
API_PORT="${API_PORT:-5050}"

echo ""
echo "→ Remote validation"
ssh "${NAS_HOST}" "cd /volume1/tikr/app && ROOT=/volume1/tikr/app TIKR_API_URL=http://localhost:${API_PORT} TIKR_WEB_URL=http://localhost:8080 TIKR_API_HOST_PORT=${API_PORT} ./validate-prod.sh" || true

echo ""
echo "→ Council endpoints (Feature 006)"
curl -sf "http://${NAS_HOST}:${API_PORT}/api/council/agenda-builder/preview?meetingDate=2026-08-10&board=TOW" | head -c 200 || echo "⚠️  Council preview check failed"
echo ""
echo "✅ Deploy complete. Web: http://${NAS_HOST}:8080  API: http://${NAS_HOST}:${API_PORT}/health"
