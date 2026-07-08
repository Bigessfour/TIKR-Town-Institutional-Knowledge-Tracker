#!/usr/bin/env bash
# Load xAI Grok API key from macOS Passwords / Keychain into TIKR env (GROK_API_KEY).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=lib/env-file.sh
source "$ROOT/scripts/lib/env-file.sh"

usage() {
  cat <<'EOF'
Usage: sync-grok-key.sh [options]

Reads XAI_API_KEY from macOS Keychain (Passwords app) and maps it to TIKR GROK_API_KEY.

Keychain item (generic password) — any of these work:
  Service: XAI_API_KEY
  Service: XAI_API_KEY          Account: xai
  Service: xAI API Key (Grok CLI) Account: xai

Options:
  --export        Print export lines (GROK_API_KEY, USE_GROK, GROK_MODEL)
  --user-secrets  Write to TIKR.Api dotnet user-secrets
  --docker-env    Merge into gitignored docker/.env
  --all           --docker-env --user-secrets
EOF
}

EXPORT=false
USER_SECRETS=false
DOCKER_ENV=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --export) EXPORT=true; shift ;;
    --user-secrets) USER_SECRETS=true; shift ;;
    --docker-env) DOCKER_ENV=true; shift ;;
    --all) DOCKER_ENV=true; USER_SECRETS=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
  esac
done

trim_key() {
  local key="$1"
  key="${key//$'\r'/}"
  key="${key//$'\n'/}"
  key="${key#"${key%%[![:space:]]*}"}"
  key="${key%"${key##*[![:space:]]}"}"
  printf '%s' "$key"
}

try_keychain_lookup() {
  local svc="$1"
  local acct="${2:-}"
  local raw=""
  if [[ -n "$acct" ]]; then
    raw=$(security find-generic-password -s "$svc" -a "$acct" -w 2>/dev/null || true)
  else
    raw=$(security find-generic-password -s "$svc" -w 2>/dev/null || true)
  fi
  trim_key "$raw"
}

CANDIDATES=(
  "XAI_API_KEY|"
  "XAI_API_KEY|XAI_API_KEY"
  "XAI_API_KEY|xai"
  "xAI API Key (Grok CLI)|xai"
)

KEY=""
for pair in "${CANDIDATES[@]}"; do
  svc="${pair%%|*}"
  acct="${pair#*|}"
  candidate="$(try_keychain_lookup "$svc" "$acct")"
  if [[ -n "$candidate" ]]; then
    KEY="$candidate"
    break
  fi
done

if [[ -z "$KEY" ]]; then
  echo "No XAI_API_KEY found in Keychain." >&2
  echo "Add a generic password in Passwords with service XAI_API_KEY" >&2
  exit 1
fi

if [[ "$EXPORT" == true ]]; then
  printf 'export GROK_API_KEY=%q\n' "$KEY"
  printf 'export XAI_API_KEY=%q\n' "$KEY"
  printf 'export USE_GROK=true\n'
  printf 'export GROK_MODEL=grok-3\n'
fi

if [[ "$USER_SECRETS" == true ]]; then
  (cd "$ROOT/src/TIKR.Api" && dotnet user-secrets set "GROK_API_KEY" "$KEY" >/dev/null)
  (cd "$ROOT/src/TIKR.Api" && dotnet user-secrets set "USE_GROK" "true" >/dev/null)
  (cd "$ROOT/src/TIKR.Api" && dotnet user-secrets set "GROK_MODEL" "grok-3" >/dev/null)
  echo "Wrote GROK_API_KEY to TIKR.Api user-secrets (${#KEY} chars)."
fi

if [[ "$DOCKER_ENV" == true ]]; then
  env_file="$(ensure_docker_env_file "$ROOT")"
  env_file_upsert "$env_file" "GROK_API_KEY" "$KEY"
  env_file_upsert "$env_file" "USE_GROK" "true"
  env_file_upsert "$env_file" "GROK_MODEL" "grok-3"
  echo "Merged GROK_API_KEY into $env_file (${#KEY} chars)."
fi

if [[ "$EXPORT" == false && "$USER_SECRETS" == false && "$DOCKER_ENV" == false ]]; then
  echo "Found XAI/Grok API key in Keychain (${#KEY} chars)."
  echo "Run with --all to apply to docker/.env and user-secrets."
fi
