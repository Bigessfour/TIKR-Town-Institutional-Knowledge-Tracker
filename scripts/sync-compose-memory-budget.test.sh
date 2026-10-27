#!/usr/bin/env bash
# Proves the 16 GB budget replaces an old fence without dropping NAS overrides.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
sync="${ROOT}/scripts/sync-compose-memory-budget.sh"
tmp="$(mktemp -d)"
trap 'rm -rf "${tmp}"' EXIT

cat >"${tmp}/base.yml" <<'EOF'
services:
  ollama:
    image: ollama/ollama:latest
    restart: unless-stopped
    mem_limit: 8g
    memswap_limit: 8g
  tikr-api:
    image: ghcr.io/bigessfour/tikr-api:1.0.3
    restart: unless-stopped
    mem_limit: 3g
    memswap_limit: 3g
  tikr-web:
    image: ghcr.io/bigessfour/tikr-web:1.0.3
    restart: unless-stopped
    mem_limit: 1g
    memswap_limit: 1g
EOF

cat >"${tmp}/nas.yml" <<'EOF'
services:
  ollama:
    image: ollama/ollama:latest
    restart: on-failure:5
    mem_limit: 1792m
    memswap_limit: 1792m
  tikr-api:
    image: tikr-api:corpus-fix
    restart: on-failure:5
    mem_limit: 1536m
    entrypoint: ["/bin/custom"]
    volumes:
      - /volume1/Town of Wiley Shared Documents:/town-docs:ro
  tikr-web:
    image: tikr-web:corpus-fix
    restart: on-failure:5
    mem_limit: 512m
EOF

bash "${sync}" "${tmp}/base.yml" "${tmp}/nas.yml"

grep -q 'mem_limit: 8g' "${tmp}/nas.yml"
grep -q 'mem_limit: 3g' "${tmp}/nas.yml"
grep -q 'mem_limit: 1g' "${tmp}/nas.yml"
grep -q 'memswap_limit: 3g' "${tmp}/nas.yml"
grep -q 'restart: unless-stopped' "${tmp}/nas.yml"
grep -q 'image: tikr-api:corpus-fix' "${tmp}/nas.yml"
grep -q 'entrypoint: \["/bin/custom"\]' "${tmp}/nas.yml"
grep -q '/volume1/Town of Wiley Shared Documents:/town-docs:ro' "${tmp}/nas.yml"
if grep -q '1792m\|1536m\|512m\|on-failure:5' "${tmp}/nas.yml"; then
	echo "old memory fence still present" >&2
	exit 1
fi

echo "sync-compose-memory-budget: pass"
