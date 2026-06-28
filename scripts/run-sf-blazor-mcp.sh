#!/usr/bin/env bash
# Launch sf-blazor-mcp with Syncfusion_API_Key from .cursor/syncfusion-api-key or Keychain.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
KEYFILE="$ROOT/.cursor/syncfusion-api-key"

if [[ -f "$KEYFILE" ]]; then
  Syncfusion_API_Key="$(tr -d '\r\n' < "$KEYFILE")"
  export Syncfusion_API_Key
elif [[ -x "$ROOT/scripts/sync-syncfusion-mcp-key.sh" ]]; then
  "$ROOT/scripts/sync-syncfusion-mcp-key.sh" >/dev/null 2>&1 || true
  if [[ -f "$KEYFILE" ]]; then
    Syncfusion_API_Key="$(tr -d '\r\n' < "$KEYFILE")"
    export Syncfusion_API_Key
  fi
fi

exec npx -y @syncfusion/blazor-assistant@latest "$@"
