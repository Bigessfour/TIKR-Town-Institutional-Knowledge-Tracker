#!/usr/bin/env bash
# CI Docker Smoke Test & E2E Gate (extracted from .github/workflows/ci.yml)
# Run this locally before pushing to validate the expensive steps without burning GH Actions runs.
#
# Usage (local, no license):
#   ./scripts/ci-smoke.sh
#
# With license (for full Syncfusion licensed path + E2E):
#   SYNCFUSION_LICENSE_KEY=xxx ./scripts/ci-smoke.sh
#
# The script will:
# - set up docker/.env
# - docker compose build + up + health checks
# - run agent-scan smoke (stub + licensed PDF if key)
# - run playwright E2E
# - tear down
#
# It mirrors the CI step as closely as possible so local run == what CI will do.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

LICENSE_KEY="${SYNCFUSION_LICENSE_KEY:-}"

echo "=== TIKR CI Smoke (local simulation) ==="
echo "License present: $([ -n "$LICENSE_KEY" ] && echo yes || echo no)"

if [ -z "$LICENSE_KEY" ]; then
  echo "SYNCFUSION_LICENSE_KEY not set — running without licensed Syncfusion tools (stub path only)."
  cp docker/.env.example docker/.env
else
  cp docker/.env.example docker/.env
  {
    echo "SYNCFUSION_LICENSE_KEY=${LICENSE_KEY}"
    echo "USE_SYNCFUSION_AGENT_TOOLS=true"
  } >> docker/.env
fi

echo ">>> docker compose build"
docker compose -f docker/docker-compose.yml build

echo ">>> docker compose up -d --wait"
docker compose -f docker/docker-compose.yml up -d --wait --wait-timeout 180 || {
  echo "docker compose up --wait failed or timed out"
  docker compose -f docker/docker-compose.yml logs --tail=50 || true
  exit 1
}

echo ">>> API health"
curl -sf http://localhost:5000/health || {
  echo "API health failed post-wait"
  docker compose -f docker/docker-compose.yml logs --tail=50 tikr-api || true
  exit 1
}

echo ">>> Web check"
code=$(curl -sf -o /dev/null -w "%{http_code}" http://localhost:8080/ 2>/dev/null || echo "000")
if ! echo "$code" | grep -qE '200|302'; then
  echo "Web check failed with code $code"
  docker compose -f docker/docker-compose.yml logs --tail=50 tikr-web || true
  exit 1
fi

echo ">>> Agent scan smoke (plain text fixture -> stub)"
agent_ok=false
for attempt in $(seq 1 6); do
  response=$(curl -sf -F "file=@tests/fixtures/agent-scan/wiley-periodic-report.txt" \
    http://localhost:5000/api/ai/agent-scan || echo '{"error":"curl failed"}')
  if node --input-type=module -e '
    try {
      const data = JSON.parse(process.argv[1] || "{}");
      const hasText = typeof data.extractedText === "string" && data.extractedText.includes("Wiley periodic report due Q1 2026");
      const hasStorage = typeof data.storagePath === "string" && data.storagePath.startsWith("agent-scans/");
      if (hasText && hasStorage) process.exit(0);
    } catch { /* ignore */ }
    process.exit(1);
  ' "$response"; then
    agent_ok=true
    break
  fi
  sleep 4
done
if [ "$agent_ok" != "true" ]; then
  echo "::error::Agent scan smoke failed after retries"
  docker compose -f docker/docker-compose.yml logs --tail=30 tikr-api || true
  exit 1
fi

echo "$response" | grep -q '"usedSyncfusionTools":false' || {
  echo "::error::Expected usedSyncfusionTools:false for plain-text agent-scan fixture"
  exit 1
}

if [ -n "$LICENSE_KEY" ]; then
  echo ">>> Licensed PDF agent-scan smoke"
  pdf_response=$(curl -sf -F "file=@tests/fixtures/agent-scan/minimal-clerk-report.pdf" \
    http://localhost:5000/api/ai/agent-scan || echo '{"error":"curl failed"}')
  echo "$pdf_response" | grep -q "Wiley clerk report" || {
    echo "::error::Licensed PDF agent-scan smoke failed to extract expected text"
    exit 1
  }
  echo "$pdf_response" | grep -q '"usedSyncfusionTools":true' || {
    echo "::error::Expected usedSyncfusionTools:true for PDF when SYNCFUSION_LICENSE_KEY is set"
    exit 1
  }
fi

echo ">>> E2E (Playwright)"
cd tests/e2e
npm ci
for i in $(seq 1 10); do
  if curl -sf http://localhost:8080/ > /dev/null; then break; fi
  sleep 2
done
if [ -n "$LICENSE_KEY" ]; then
  SYNCFUSION_LICENSE_KEY=${LICENSE_KEY} TIKR_E2E_BASE_URL=http://localhost:8080 \
    npx playwright install --with-deps chromium
  SYNCFUSION_LICENSE_KEY=${LICENSE_KEY} TIKR_E2E_BASE_URL=http://localhost:8080 \
    npx playwright test --reporter=list --timeout=120000
else
  TIKR_E2E_BASE_URL=http://localhost:8080 \
    npx playwright install --with-deps chromium
  TIKR_E2E_BASE_URL=http://localhost:8080 \
    npx playwright test --reporter=list --timeout=120000
fi
cd -

echo ">>> docker compose down"
docker compose -f docker/docker-compose.yml down

echo "=== CI smoke completed successfully ==="