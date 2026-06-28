#!/usr/bin/env bash
# Create/update repo .venv for tikr-rag-mcp. Prints absolute path to python3 on success.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VENV="$ROOT/.venv"
REQ="$ROOT/scripts/requirements-rag.txt"

if ! command -v python3 >/dev/null 2>&1; then
  echo "Error: python3 not found. Install Python 3 (e.g. brew install python)." >&2
  exit 1
fi

if [[ ! -d "$VENV" ]]; then
  python3 -m venv "$VENV"
fi

"$VENV/bin/pip" install -q -r "$REQ"
echo "$VENV/bin/python3"
