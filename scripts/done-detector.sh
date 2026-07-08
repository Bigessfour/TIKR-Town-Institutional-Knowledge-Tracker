#!/usr/bin/env bash
# Lightweight Done Detector / Release Readiness helper.
#
# Philosophy: Layer 1 (functions proven) + Layer 2 (system gate) = done with confidence.
# Run this only after the function inventory looks clean.
#
# Usage:
#   ./scripts/done-detector.sh
#
# It:
# - Updates the function inventory
# - Checks for zero unproven functions (best-effort parse of generated)
# - Runs full test suite (Release)
# - Reminds you to finish the Project-Level Done Detector checklist in action-items.md
#
# Keep it simple. Optional. Agent-friendly.

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$REPO_ROOT"

echo "=== Done Detector: Layer 1 (Function Inventory) ==="
./scripts/update-function-inventory.sh || true

GEN="docs/function-inventory.generated.md"
if [[ -f "$GEN" ]]; then
  if grep -q "0 without proof" "$GEN"; then
    echo "✅ Function inventory clean (0 without proof found in summary)"
  else
    echo "⚠️  Function inventory not yet clean. Review the 'Functions without proof' section before proceeding."
    echo "   (Run again after curating action-items.md)"
  fi
else
  echo "⚠️  $GEN not found — run inventory first."
fi

echo ""
echo "=== Layer 1.5: Core tests ==="
dotnet test TIKR.sln --configuration Release --no-build --verbosity minimal || {
  echo "❌ Tests failed. Fix before gate."
  exit 1
}
echo "✅ dotnet test --configuration Release passed"

echo ""
echo "=== Layer 2: Project-Level Done Detector / Release Readiness Gate ==="
echo "Now complete the checklist at the bottom of docs/action-items.md:"
echo "  - Only after function inventory is 0 without proof"
echo "  - Full workflows, Docker/NAS smoke, docs, bus-factor, no critical opens, trunk, RAG, etc."
echo ""
echo "When the gate checklist is 100% checked, you can confidently say the phase/project is done."
echo ""
echo "Reminder commands:"
echo "  trunk check --all"
echo "  .venv/bin/python3 scripts/update_tikr_rag_index.py   # if using RAG"
echo ""
echo "Done Detector complete (for now). Re-run as needed."