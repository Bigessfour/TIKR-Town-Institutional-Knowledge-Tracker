#!/usr/bin/env bash
# Load SYNCFUSION_LICENSE_KEY from macOS Passwords / Keychain into TIKR env.
# Never commits the key — writes only to gitignored docker/.env and dotnet user-secrets.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=lib/env-file.sh
source "$ROOT/scripts/lib/env-file.sh"

usage() {
  cat <<'EOF'
Usage: sync-syncfusion-license-key.sh [options]

Reads SYNCFUSION_LICENSE_KEY from macOS Keychain (Passwords app) and applies it
to local TIKR configuration. The key is never printed or committed to git.

Store in Passwords as a generic password (any of these work):
  Service: com.wileyco.syncfusion.license
  Service: SYNCFUSION_LICENSE_KEY          Account: SYNCFUSION or SYNCFUSION_LICENSE_KEY
  Service: Syncfusion License Key          Account: syncfusion
  Label:   Syncfusion License Key / SYNCFUSION_LICENSE_KEY

Options:
  --export              Print shell export lines (for eval in your terminal)
  --user-secrets        Write to TIKR.Api + TIKR.Web dotnet user-secrets
  --docker-env          Merge into gitignored docker/.env (creates from .env.example)
  --enable-agent-tools  Also set USE_SYNCFUSION_AGENT_TOOLS=true in docker/.env
  --all                 --docker-env --user-secrets (recommended local setup)
  -h, --help            Show this help

TIKR.Web uses Syncfusion Blazor 34.1.29 — the key must be generated for Blazor v34.x
(https://www.syncfusion.com/account/downloads). A Document SDK-only key clears API probes
but leaves the Blazor trial overlay until the Blazor platform key matches.

Recommended one-time setup:
  ./scripts/sync-syncfusion-license-key.sh --all

Or sync every secret from Passwords:
  ./scripts/setup-local-secrets.sh
EOF
}

EXPORT=false
USER_SECRETS=false
DOCKER_ENV=false
ENABLE_AGENT_TOOLS=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --export) EXPORT=true; shift ;;
    --user-secrets) USER_SECRETS=true; shift ;;
    --docker-env) DOCKER_ENV=true; shift ;;
    --enable-agent-tools) ENABLE_AGENT_TOOLS=true; shift ;;
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

try_keychain_label() {
  local label="$1"
  local raw=""
  raw=$(security find-generic-password -l "$label" -w 2>/dev/null || true)
  trim_key "$raw"
}

CANDIDATES=(
  "SYNCFUSION_LICENSE_KEY|SYNCFUSION"
  "SYNCFUSION_LICENSE_KEY|SYNCFUSION_LICENSE_KEY"
  "com.wileyco.syncfusion.license|"
  "Syncfusion License Key|syncfusion"
  "Syncfusion License Key|"
  "Syncfusion|license"
  "Syncfusion|"
)

LABEL_CANDIDATES=(
  "Syncfusion License Key"
  "SYNCFUSION_LICENSE_KEY"
  "Syncfusion"
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
  for label in "${LABEL_CANDIDATES[@]}"; do
    candidate="$(try_keychain_label "$label")"
    if [[ -n "$candidate" ]]; then
      KEY="$candidate"
      break
    fi
  done
fi

if [[ -z "$KEY" ]]; then
  echo "No SYNCFUSION_LICENSE_KEY found in Keychain." >&2
  echo "Add a generic password in Passwords with service com.wileyco.syncfusion.license" >&2
  echo "or label 'Syncfusion License Key', then re-run this script." >&2
  exit 1
fi

alt_license="$(try_keychain_lookup "Syncfusion License Key" "syncfusion")"
if [[ -n "$alt_license" && "$alt_license" != "$KEY" ]]; then
  echo "WARNING: Keychain item 'Syncfusion License Key' (account syncfusion) differs from the entry used for sync (${#KEY} vs ${#alt_license} chars)." >&2
  echo "If you just updated the license in Passwords, edit the generic password with Service SYNCFUSION_LICENSE_KEY (not only the title), or delete duplicate items." >&2
fi

if [[ "$EXPORT" == true ]]; then
  printf 'export SYNCFUSION_LICENSE_KEY=%q\n' "$KEY"
  printf 'export USE_SYNCFUSION_AGENT_TOOLS=true\n'
fi

if [[ "$USER_SECRETS" == true ]]; then
  (cd "$ROOT/src/TIKR.Api" && dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "$KEY" >/dev/null)
  (cd "$ROOT/src/TIKR.Web" && dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "$KEY" >/dev/null)
  echo "Wrote SYNCFUSION_LICENSE_KEY to TIKR.Api and TIKR.Web user-secrets (${#KEY} chars)."
fi

if [[ "$DOCKER_ENV" == true ]]; then
  env_file="$(ensure_docker_env_file "$ROOT")"
  env_file_upsert "$env_file" "SYNCFUSION_LICENSE_KEY" "$KEY"
  echo "Merged SYNCFUSION_LICENSE_KEY into $env_file (${#KEY} chars)."
  if [[ "$ENABLE_AGENT_TOOLS" == true ]]; then
    env_file_upsert "$env_file" "USE_SYNCFUSION_AGENT_TOOLS" "true"
    echo "Set USE_SYNCFUSION_AGENT_TOOLS=true in $env_file."
  fi
fi

if [[ "$EXPORT" == false && "$USER_SECRETS" == false && "$DOCKER_ENV" == false ]]; then
  echo "Found SYNCFUSION_LICENSE_KEY in Keychain (${#KEY} chars)."
  echo "Run with --all to apply to docker/.env and user-secrets."
fi