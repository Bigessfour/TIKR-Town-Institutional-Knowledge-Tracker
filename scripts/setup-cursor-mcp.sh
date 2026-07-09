#!/usr/bin/env bash
# Write .cursor/mcp.json with tikr-rag-mcp + standard servers (uses repo .venv python3).
# Optional: --with-chrome-devtools swaps ollama for chrome-devtools-mcp (keeps ≤4 servers).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PYTHON="$("$ROOT/scripts/setup-python-rag.sh")"
RAG_SCRIPT="$ROOT/scripts/tikr_rag_mcp.py"
MCP="$ROOT/.cursor/mcp.json"
WITH_CHROME=false

for arg in "$@"; do
  case "$arg" in
    --with-chrome-devtools) WITH_CHROME=true ;;
    -h | --help)
      echo "Usage: $0 [--with-chrome-devtools]"
      echo "  --with-chrome-devtools  Enable chrome-devtools-mcp (disables ollama MCP to stay ≤4 servers)."
      exit 0
      ;;
  esac
done

mkdir -p "$ROOT/.cursor"

SF_ENV='{"Syncfusion_API_Key": "${env:SYNCFUSION_API_KEY}"}'
if [[ -f "$MCP" ]] && grep -q 'Syncfusion_API_Key_Path' "$MCP" 2>/dev/null; then
  SF_ENV='{"Syncfusion_API_Key_Path": "'"$ROOT/.cursor/syncfusion-api-key"'"}'
fi

OLLAMA_BLOCK=""
CHROME_BLOCK=""
if [[ "$WITH_CHROME" == true ]]; then
  CHROME_BLOCK='    "chrome-devtools": {
      "command": "npx",
      "args": [
        "-y",
        "chrome-devtools-mcp@latest",
        "--slim",
        "--headless",
        "--no-usage-statistics"
      ],
      "env": {
        "CHROME_DEVTOOLS_MCP_NO_UPDATE_CHECKS": "1"
      }
    },'
else
  OLLAMA_BLOCK='    "ollama": {
      "command": "npx",
      "args": ["-y", "ollama-mcp"],
      "env": {
        "OLLAMA_HOST": "http://localhost:11434"
      }
    },'
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
${OLLAMA_BLOCK}${CHROME_BLOCK}
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
if [[ "$WITH_CHROME" == true ]]; then
  echo "chrome-devtools-mcp enabled (ollama MCP omitted). Prefetch: npx -y chrome-devtools-mcp@latest --help"
fi
echo "Restart Cursor or reload MCP servers in Settings → Tools & MCP."