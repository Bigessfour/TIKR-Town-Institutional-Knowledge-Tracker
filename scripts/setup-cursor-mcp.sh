#!/usr/bin/env bash
# Write .cursor/mcp.json with tikr-rag-mcp + standard servers (uses repo .venv python3).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PYTHON="$("$ROOT/scripts/setup-python-rag.sh")"
RAG_SCRIPT="$ROOT/scripts/tikr_rag_mcp.py"
MCP="$ROOT/.cursor/mcp.json"
EXAMPLE="$ROOT/.cursor/mcp.json.example"

mkdir -p "$ROOT/.cursor"

# Preserve sf-blazor-mcp env style from existing mcp.json when present
SF_ENV='{"Syncfusion_API_Key": "${env:SYNCFUSION_API_KEY}"}'
if [[ -f "$MCP" ]] && grep -q 'Syncfusion_API_Key_Path' "$MCP" 2>/dev/null; then
  SF_ENV='{"Syncfusion_API_Key_Path": "'"$ROOT/.cursor/syncfusion-api-key"'"}'
fi

cat > "$MCP" <<EOF
{
  "mcpServers": {
    "sf-blazor-mcp": {
      "command": "npx",
      "args": ["-y", "@syncfusion/blazor-assistant@latest"],
      "env": $SF_ENV
    },
    "microsoft-learn": {
      "url": "https://learn.microsoft.com/api/mcp"
    },
    "ollama": {
      "command": "npx",
      "args": ["-y", "ollama-mcp"],
      "env": {
        "OLLAMA_HOST": "http://localhost:11434"
      }
    },
    "tikr-rag-mcp": {
      "command": "$PYTHON",
      "args": ["$RAG_SCRIPT"],
      "env": {
        "OLLAMA_HOST": "http://localhost:11434"
      }
    }
  }
}
EOF

echo "Wrote $MCP (tikr-rag-mcp → $PYTHON)"
echo "Restart Cursor or reload MCP servers in Settings → Tools & MCP."
