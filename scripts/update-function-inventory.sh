#!/usr/bin/env bash
# TIKR convenience wrapper for the personal function-inventory skill.
#
# The real solo superpower lives at:
#   ~/.cursor/skills/function-inventory/
#
# This script:
# - Prefers the Python lightweight tracker (function-level + packages + proof)
# - Falls back gracefully
#
# Usage:
#   ./scripts/update-function-inventory.sh
#
# Philosophy (see AGENTS.md):
# Track individual functions. Prove them with tests. Keep implementations minimal.
# Run before claiming done. Protect the whole project from small unproven details.

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$REPO_ROOT"

PERSONAL_PY="$HOME/.cursor/skills/function-inventory/scripts/update-function-inventory.py"

if [[ -x "$PERSONAL_PY" ]] || [[ -f "$PERSONAL_PY" && -x "$(command -v python3)" ]]; then
  echo "[function-inventory] Using personal Python lightweight tracker..."
  python3 "$PERSONAL_PY" . --output "docs/function-inventory.generated.md"
  exit $?
fi

echo "[function-inventory] Personal Python scanner not found."
echo "Install / update it in ~/.cursor/skills/function-inventory/"
echo "Falling back to simple scan (limited)..."

# Minimal fallback using rg if available
OUTPUT="docs/function-inventory.generated.md"
mkdir -p "$(dirname "$OUTPUT")"

{
  echo "# TIKR Function Inventory (fallback)"
  echo "Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo ""
  echo "Run the personal Python scanner for full function + proof tracking."
  echo ""
  if command -v rg >/dev/null; then
    echo "## Quick API surface"
    rg --type cs 'app\.Map(Get|Post|Put|Delete)|MapGroup' src/TIKR.Api --no-heading | head -30 || true
  fi
} > "$OUTPUT"

echo "Wrote $OUTPUT (limited fallback)"