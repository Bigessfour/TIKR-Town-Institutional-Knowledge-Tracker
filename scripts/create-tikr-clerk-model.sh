#!/usr/bin/env bash
# Create the tikr-clerk Ollama model from docker/ollama/Modelfile.tikr-clerk.
# Usage:
#   ./scripts/create-tikr-clerk-model.sh
#   OLLAMA_HOST=http://localhost:11434 ./scripts/create-tikr-clerk-model.sh
#   # Against Docker Ollama:
#   docker exec -i tikr-ollama ollama create tikr-clerk -f - < docker/ollama/Modelfile.tikr-clerk
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODELFILE="${ROOT}/docker/ollama/Modelfile.tikr-clerk"
MODEL_NAME="${TIKR_CLERK_MODEL_NAME:-tikr-clerk}"
BASE_MODEL="${TIKR_CLERK_BASE_MODEL:-llama3.2:3b}"

if [[ ! -f "$MODELFILE" ]]; then
  echo "Modelfile not found: $MODELFILE" >&2
  exit 1
fi

if ! command -v ollama >/dev/null 2>&1; then
  echo "ollama CLI not found on PATH. Install from https://ollama.com or use:" >&2
  echo "  docker exec -i tikr-ollama ollama create ${MODEL_NAME} -f - < ${MODELFILE}" >&2
  exit 1
fi

echo "Ensuring base model ${BASE_MODEL} is available..."
ollama pull "$BASE_MODEL"

echo "Creating ${MODEL_NAME} from ${MODELFILE}..."
ollama create "$MODEL_NAME" -f "$MODELFILE"

echo "Done. Set OLLAMA_CHAT_MODEL=${MODEL_NAME} and restart tikr-api."
ollama show "$MODEL_NAME" >/dev/null && echo "Verified: ollama show ${MODEL_NAME} ok"
