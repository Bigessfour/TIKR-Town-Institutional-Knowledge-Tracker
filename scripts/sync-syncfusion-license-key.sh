#!/usr/bin/env bash
# Load SYNCFUSION_LICENSE_KEY from macOS Passwords / Keychain for local Document SDK tests.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

usage() {
  cat <<'EOF'
Usage: sync-syncfusion-license-key.sh [--export] [--user-secrets]

Reads SYNCFUSION_LICENSE_KEY from macOS Keychain (Passwords app).

Keychain item (generic password) — any of these work:
  Service: com.wileyco.syncfusion.license
  Service: SYNCFUSION_LICENSE_KEY          Account: SYNCFUSION_LICENSE_KEY
  Service: Syncfusion License Key          Account: syncfusion

Options:
  --export        Print export lines for your shell (SYNCFUSION_LICENSE_KEY + USE_SYNCFUSION_AGENT_TOOLS)
  --user-secrets  Also write to TIKR.Api and TIKR.Web dotnet user-secrets
EOF
}

EXPORT=false
USER_SECRETS=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --export) EXPORT=true; shift ;;
    --user-secrets) USER_SECRETS=true; shift ;;
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
  "com.wileyco.syncfusion.license|"
  "SYNCFUSION_LICENSE_KEY|SYNCFUSION_LICENSE_KEY"
  "Syncfusion License Key|syncfusion"
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
  echo "No SYNCFUSION_LICENSE_KEY found in Keychain." >&2
  echo "Add a generic password in Passwords with service com.wileyco.syncfusion.license" >&2
  exit 1
fi

if [[ "$EXPORT" == true ]]; then
  printf 'export SYNCFUSION_LICENSE_KEY=%q\n' "$KEY"
  printf 'export USE_SYNCFUSION_AGENT_TOOLS=true\n'
fi

if [[ "$USER_SECRETS" == true ]]; then
  (cd "$ROOT/src/TIKR.Api" && dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "$KEY")
  (cd "$ROOT/src/TIKR.Web" && dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "$KEY")
  echo "Wrote SYNCFUSION_LICENSE_KEY to TIKR.Api and TIKR.Web user-secrets (${#KEY} chars)."
fi

if [[ "$EXPORT" == false && "$USER_SECRETS" == false ]]; then
  echo "Found SYNCFUSION_LICENSE_KEY in Keychain (${#KEY} chars)."
  echo "Run with --export or --user-secrets to apply."
fi
