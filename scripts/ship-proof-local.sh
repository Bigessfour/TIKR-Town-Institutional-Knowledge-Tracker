#!/usr/bin/env bash
# Local ship-proof using alternate host ports (avoids macOS :5000 / host Ollama :11434).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

LICENSE_KEY="${SYNCFUSION_LICENSE_KEY:-}"
if [ -z "$LICENSE_KEY" ]; then
  echo "SYNCFUSION_LICENSE_KEY required" >&2
  exit 1
fi

BACKUP="$(mktemp)"
cp "$ROOT/docker/.env" "$BACKUP"
restore_env() {
  cp "$BACKUP" "$ROOT/docker/.env"
  rm -f "$BACKUP"
  docker compose -f "$ROOT/docker/docker-compose.yml" -f "$ROOT/docker/docker-compose.ship-proof.yml" down >/dev/null 2>&1 || true
}
trap restore_env EXIT

cp "$ROOT/docker/.env.example" "$ROOT/docker/.env"
{
  echo "SYNCFUSION_LICENSE_KEY=${LICENSE_KEY}"
  echo "USE_SYNCFUSION_AGENT_TOOLS=true"
  echo 'TIKR_STORAGE_LABEL="Synology NAS"'
} >> "$ROOT/docker/.env"

API_URL="http://localhost:15000"
WEB_URL="http://localhost:18080"

echo "=== Ship-proof (alt ports 15000/18080/11435) ==="
echo "License present: yes (${#LICENSE_KEY} chars)"

docker compose -f "$ROOT/docker/docker-compose.yml" -f "$ROOT/docker/docker-compose.ship-proof.yml" up -d --wait --wait-timeout 180

echo ">>> API health"
curl -sf "$API_URL/health"
echo

echo ">>> Web check"
code=$(curl -sf -o /dev/null -w "%{http_code}" "$WEB_URL/" || echo 000)
echo "web:$code"
echo "$code" | grep -qE '200|302'

echo ">>> Agent scan smoke (txt)"
response=$(curl -sf -F "file=@tests/fixtures/agent-scan/wiley-periodic-report.txt" "$API_URL/api/ai/agent-scan")
python3 - "$response" <<'PY'
import json, sys
d = json.loads(sys.argv[1])
assert "Wiley periodic report" in (d.get("extractedText") or "")
assert (d.get("storagePath") or "").startswith("agent-scans/")
assert d.get("usedSyncfusionTools") is False
print("txt_ok usedSyncfusionTools=false")
PY

echo ">>> Licensed PDF agent-scan smoke"
pdf_response=$(curl -sf -F "file=@tests/fixtures/agent-scan/minimal-clerk-report.pdf" "$API_URL/api/ai/agent-scan")
python3 - "$pdf_response" <<'PY'
import json, sys
d = json.loads(sys.argv[1])
text = (d.get("extractedText") or "")
print(f"pdf usedSyncfusionTools={d.get('usedSyncfusionTools')} processed={bool(d.get('processedStoragePath'))}")
assert d.get("usedSyncfusionTools") is True
assert ("Wiley clerk report" in text) or ("wiley" in text.lower())
print("pdf_ok")
PY

echo ">>> E2E Playwright (requirements-agent-scan + clerk-smoke)"
cd "$ROOT/tests/e2e"
npm ci
npx playwright install chromium
TIKR_E2E_BASE_URL="$WEB_URL" SYNCFUSION_LICENSE_KEY="$LICENSE_KEY" \
  npx playwright test requirements-agent-scan.spec.ts clerk-smoke.spec.ts --reporter=list --timeout=120000
cd "$ROOT"

docker compose -f "$ROOT/docker/docker-compose.yml" -f "$ROOT/docker/docker-compose.ship-proof.yml" down
trap - EXIT
cp "$BACKUP" "$ROOT/docker/.env"
rm -f "$BACKUP"
echo "=== Ship-proof completed successfully ==="
