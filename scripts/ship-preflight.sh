#!/usr/bin/env bash
# Pre-merge / pre-tag checks for TIKR release. See docs/ship-to-production.md.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== TIKR ship preflight ==="

echo "→ Function inventory + done detector"
./scripts/done-detector.sh

echo "→ Trunk"
trunk check --all

echo "→ Prod compose config (syntax)"
docker compose -f docker/docker-compose.prod.yml --env-file docker/.env.example config >/dev/null

echo ""
echo "✅ Preflight passed. Next: merge to main, Deb walkthrough, git tag v1.0.0 (see docs/ship-to-production.md)."