#!/usr/bin/env bash
# One-command local secret sync: macOS Passwords → docker/.env + dotnet user-secrets.
# Never commits secrets. Safe to re-run after updating Passwords.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

usage() {
  cat <<'EOF'
Usage: setup-local-secrets.sh [options]

Pulls TIKR secrets from macOS Passwords (Keychain) into gitignored local stores:
  - docker/.env          (Docker Compose + DotNetEnv in Development)
  - dotnet user-secrets    (TIKR.Api, TIKR.Web)
  - .cursor/syncfusion-api-key (MCP developer key, if present)

Options:
  --enable-agent-tools   Set USE_SYNCFUSION_AGENT_TOOLS=true in docker/.env
  --skip-mcp             Do not sync SYNCFUSION_API_KEY for Cursor MCP
  --skip-grok            Do not sync GROK_API_KEY (if present in Passwords)
  -h, --help             Show this help

After running:
  docker compose -f docker/docker-compose.yml up --build
  # or: cd src/TIKR.Api && dotnet run
EOF
}

ENABLE_AGENT_TOOLS=false
SKIP_MCP=false
SKIP_GROK=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --enable-agent-tools) ENABLE_AGENT_TOOLS=true; shift ;;
    --skip-mcp) SKIP_MCP=true; shift ;;
    --skip-grok) SKIP_GROK=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
  esac
done

sync_license_args=(--all)
[[ "$ENABLE_AGENT_TOOLS" == true ]] && sync_license_args+=(--enable-agent-tools)

echo "==> Syncfusion runtime license (SYNCFUSION_LICENSE_KEY)"
"$ROOT/scripts/sync-syncfusion-license-key.sh" "${sync_license_args[@]}"

if [[ "$SKIP_GROK" == false ]]; then
  echo ""
  echo "==> Grok API key (GROK_API_KEY) — skipped if not in Passwords"
  if "$ROOT/scripts/sync-grok-key.sh" --docker-env --user-secrets 2>/dev/null; then
    :
  else
    echo "    (no Grok key in Passwords — optional)"
  fi
fi

if [[ "$SKIP_MCP" == false ]]; then
  echo ""
  echo "==> Syncfusion MCP developer key (SYNCFUSION_API_KEY) — skipped if not in Passwords"
  if "$ROOT/scripts/sync-syncfusion-mcp-key.sh" 2>/dev/null; then
    :
  else
    echo "    (no MCP developer key in Passwords — optional for Cursor only)"
  fi
fi

echo ""
echo "Local secrets synced. docker/.env and user-secrets are gitignored."
echo "Restart TIKR containers or dotnet run to pick up changes."