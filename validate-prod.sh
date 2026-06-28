#!/usr/bin/env bash
# TIKR production validation — run on Synology SSH or against a local prod compose stack.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

WEB_PORT="${TIKR_WEB_HOST_PORT:-8080}"
API_PORT="${TIKR_API_HOST_PORT:-5000}"
OLLAMA_PORT="${TIKR_OLLAMA_HOST_PORT:-11434}"
WEB_URL="${TIKR_WEB_URL:-http://localhost:${WEB_PORT}}"
API_URL="${TIKR_API_URL:-http://localhost:${API_PORT}}"
DATA_PATH="${TIKR_DATA_PATH:-/volume1/tikr/data}"
AGENT_FIXTURE="${ROOT}/tests/fixtures/agent-scan/wiley-periodic-report.txt"

pass=0
fail=0

check() {
  local label="$1"
  shift
  if "$@"; then
    echo "✅ ${label}"
    pass=$((pass + 1))
  else
    echo "❌ ${label}"
    fail=$((fail + 1))
  fi
}

check "Web ${WEB_PORT}" curl -sf "${WEB_URL}/" -o /dev/null
check "API ${API_PORT} /health" curl -sf "${API_URL}/health" -o /dev/null
check "Ollama ${OLLAMA_PORT}" curl -sf "http://localhost:${OLLAMA_PORT}/" -o /dev/null

if [[ -d "${DATA_PATH}" ]]; then
  check "NAS share writable (${DATA_PATH})" test -w "${DATA_PATH}"
else
  echo "⚠️  Skipping NAS write test — ${DATA_PATH} not present on this host"
fi

if curl -sf "${API_URL}/api/system/local-status" -o /dev/null; then
  check "DB / local-status" true
else
  check "DB / local-status" false
fi

if command -v jq >/dev/null 2>&1; then
  grok_enabled="$(curl -sf "${API_URL}/api/ai/status" | jq -r '.grokEnabled')"
  if [[ "${grok_enabled}" == "false" ]]; then
    check "Grok fallback disabled (default)" true
  else
    check "Grok fallback disabled (default)" false
  fi

  used_grok="$(curl -sf -X POST "${API_URL}/api/ai/ask-advanced" \
    -H 'Content-Type: application/json' \
    -d '{"prompt":"Reply with one word: ok","context":null}' \
    | jq -r '.usedGrok')"
  if [[ "${used_grok}" == "false" ]]; then
    check "Ask-advanced uses Ollama when Grok off" true
  else
    check "Ask-advanced uses Ollama when Grok off" false
  fi
else
  echo "⚠️  Install jq for Grok/AI JSON checks"
fi

if [[ -f "${AGENT_FIXTURE}" ]]; then
  check "Agent-scan endpoint" curl -sf -X POST "${API_URL}/api/ai/agent-scan" \
    -F "file=@${AGENT_FIXTURE}" -o /dev/null
else
  echo "⚠️  Skipping agent-scan — fixture not found"
fi

echo ""
echo "Results: ${pass} passed, ${fail} failed"
if [[ "${fail}" -gt 0 ]]; then
  exit 1
fi

echo "All production checks passed."
