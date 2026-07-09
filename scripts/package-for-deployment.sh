#!/usr/bin/env bash
# Build and verify TIKR Docker images for NAS / GHCR deployment (clerk tour v2+).
# See docs/ship-to-production.md and docs/clerk-tour-deployment.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILES=(
  -f docker/docker-compose.yml
  -f docker/docker-compose.host-ollama.yml
)
ENV_FILE="docker/.env"
if [[ -f "$ENV_FILE" ]]; then
  COMPOSE_ENV=(--env-file "$ENV_FILE")
else
  echo "⚠️  $ENV_FILE missing — using docker/.env.example for config validation only"
  COMPOSE_ENV=(--env-file docker/.env.example)
fi

if [[ -f docker/docker-compose.dev-mac.yml ]]; then
  COMPOSE_FILES+=(-f docker/docker-compose.dev-mac.yml)
fi

echo "=== TIKR package for deployment ==="
echo "→ dotnet test (Release)"
dotnet test TIKR.sln --configuration Release --no-restore 2>/dev/null || dotnet test TIKR.sln --configuration Release

echo "→ Docker compose config"
docker compose "${COMPOSE_FILES[@]}" "${COMPOSE_ENV[@]}" config >/dev/null

echo "→ Build API + Web images"
docker compose "${COMPOSE_FILES[@]}" "${COMPOSE_ENV[@]}" build tikr-api tikr-web

echo "→ Apply DB migrations on next API start (includes ClerkTour user columns)"
echo "   Tour catalog version: v2 (bump ClerkTourCatalog.CurrentVersion when steps change)"

if [[ -f tests/e2e/package.json ]]; then
  echo "→ Playwright clerk-tour-anchors (optional; requires running stack on :8080)"
  if curl -sf -o /dev/null http://localhost:8080/ 2>/dev/null; then
    (cd tests/e2e && npm test -- clerk-tour-anchors.spec.ts --reporter=line) || {
      echo "⚠️  E2E tour anchors failed — fix before tag if this gate matters for your release"
    }
  else
    echo "   Skipped (start stack: docker compose ... up -d tikr-api tikr-web)"
  fi
fi

echo ""
echo "✅ Package build complete."
echo "   Deploy: docker compose ${COMPOSE_FILES[*]} ${COMPOSE_ENV[*]} up -d tikr-api tikr-web"
echo "   Clerks: first visit auto-tour (v2); Settings → Show me around TIKR; Tour this page on each route."