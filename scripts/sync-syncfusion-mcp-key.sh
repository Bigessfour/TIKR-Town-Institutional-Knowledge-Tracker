#!/usr/bin/env bash
# Copy SYNCFUSION_API_KEY from macOS Passwords / Keychain into .cursor/syncfusion-api-key
# for sf-blazor-mcp (Syncfusion_API_Key_Path). Never commits the key.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
KEYFILE="$ROOT/.cursor/syncfusion-api-key"
VALIDATE=false

usage() {
  cat <<'EOF'
Usage: sync-syncfusion-mcp-key.sh [--check]

Reads SYNCFUSION_API_KEY from macOS Keychain (Passwords app) and writes
.cursor/syncfusion-api-key for sf-blazor-mcp.

Store the developer API key (not SYNCFUSION_LICENSE_KEY) in Passwords:
  https://syncfusion.com/account/api-key

Keychain item (generic password) — any of these work:
  Service: com.wileyco.syncfusion.blazor-mcp  Account: your macOS username  (Passwords default for TIKR)
  Service: SYNCFUSION_API_KEY                 Account: SYNCFUSION_API_KEY

Options:
  --check   Verify the key against Syncfusion MCP API after sync (picks a valid entry when several exist)
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --check) VALIDATE=true; shift ;;
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

# Known Passwords / Keychain locations (TIKR + generic). Order: TIKR-specific first.
KEYCHAIN_CANDIDATES=(
  "com.wileyco.syncfusion.blazor-mcp|${USER:-}"
  "SYNCFUSION_API_KEY|SYNCFUSION_API_KEY"
  "SYNCFUSION_API_KEY|${USER:-}"
  "SYNCFUSION_API_KEY|"
)

collect_keychain_candidates() {
  local pair svc acct key
  for pair in "${KEYCHAIN_CANDIDATES[@]}"; do
    svc="${pair%%|*}"
    acct="${pair#*|}"
    key="$(try_keychain_lookup "$svc" "$acct")"
    [[ -n "$key" ]] && printf '%s\n' "$key"
  done | awk '!seen[$0]++'
}

read_keychain() {
  collect_keychain_candidates | head -1
}

validate_api_key() {
  local key="$1"
  local body http_code
  body=$(mktemp)
  http_code=$(curl -sS -o "$body" -w '%{http_code}' \
    -H "Content-Type: application/json" \
    -H "API-Key: $key" \
    -d '{"query":"SfGrid paging","components":["grid"]}' \
    "https://helpbot.syncfusion.com/api/documents/search") || {
    rm -f "$body"
    echo "Network error contacting Syncfusion MCP API." >&2
    return 1
  }

  if grep -q "Invalid API key" "$body" 2>/dev/null; then
    rm -f "$body"
    echo "Syncfusion rejected the API key (401 Invalid API key)." >&2
    echo "Generate a new developer key at https://syncfusion.com/account/api-key" >&2
    echo "Update Passwords (service SYNCFUSION_API_KEY), then re-run this script." >&2
    return 1
  fi

  rm -f "$body"
  case "$http_code" in
    200|201|422) return 0 ;;
    *)
      echo "Unexpected Syncfusion API response HTTP $http_code." >&2
      return 1
      ;;
  esac
}

KEY=""
SOURCE=""
while IFS= read -r candidate; do
  [[ -z "$candidate" ]] && continue
  if [[ "$VALIDATE" == true ]]; then
    if validate_api_key "$candidate"; then
      KEY="$candidate"
      break
    fi
  elif [[ -z "$KEY" ]]; then
    KEY="$candidate"
    break
  fi
done < <(collect_keychain_candidates)

if [[ -z "$KEY" ]]; then
  if [[ "$VALIDATE" == true ]]; then
    echo "No valid Syncfusion API key found in Keychain." >&2
  else
    echo "No SYNCFUSION_API_KEY found in Keychain." >&2
  fi
  echo "Add it in Passwords (generic password) or Keychain Access:" >&2
  echo "  Service: com.wileyco.syncfusion.blazor-mcp  Account: ${USER:-you}" >&2
  echo "  Password: <your key from https://syncfusion.com/account/api-key>" >&2
  exit 1
fi

mkdir -p "$ROOT/.cursor"
printf '%s' "$KEY" > "$KEYFILE"
chmod 600 "$KEYFILE"
echo "Wrote $KEYFILE (${#KEY} chars, mode 600)"

if [[ "$VALIDATE" == true ]]; then
  echo "Syncfusion MCP API accepted the key."
fi

echo "Restart Cursor (Settings → Tools & MCP) to reload sf-blazor-mcp."
