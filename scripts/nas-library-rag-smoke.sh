#!/usr/bin/env bash
# NAS shared-repository → embed → Assistant RAG smoke (API-level).
# Knowledge target: entire Town of Wiley Shared Documents mount (/data/town-docs),
# not a council-minutes subset.
#
# Usage (from Mac, after deploying the branch under test):
#   ./scripts/nas-library-rag-smoke.sh
#   TIKR_API_URL=http://mr-storage:5050 ./scripts/nas-library-rag-smoke.sh
#
# Optional: seed a distinctive plaintext fixture into the share first
#   (see docs/nas-library-rag-smoke.md).
set -euo pipefail

NAS_HOST="${TIKR_NAS_HOST:-mr-storage}"
API_URL="${TIKR_API_URL:-}"
QUERY="${TIKR_RAG_SMOKE_QUERY:-Town of Wiley Board of Trustees mill levy ordinance}"
TOP_K="${TIKR_RAG_SMOKE_TOP_K:-5}"

if [[ -z ${API_URL} ]]; then
	PORT="$(ssh -o BatchMode=yes -o ConnectTimeout=10 "${NAS_HOST}" \
		"grep '^TIKR_API_HOST_PORT=' /volume1/tikr/app/docker/.env 2>/dev/null | cut -d= -f2" || true)"
	PORT="${PORT:-5050}"
	API_URL="http://${NAS_HOST}:${PORT}"
fi

API_URL="${API_URL%/}"
echo "=== NAS library RAG smoke → ${API_URL} ==="

need_jq() {
	if ! command -v jq >/dev/null 2>&1; then
		echo "❌ jq required (brew install jq)"
		exit 1
	fi
}
need_jq

echo "→ Health"
curl -sf "${API_URL}/health" | jq -c .
echo

echo "→ AI status (Ollama)"
curl -sf "${API_URL}/api/ai/status" | jq '{ollamaAvailable, ollamaModel, grokEnabled}'
echo

echo "→ Library scan status"
curl -sf "${API_URL}/api/library/scan-status" | jq '{
  configured,
  libraryPath,
  scanInProgress,
  lastScanUtc,
  lastResult: (.lastResult // .LastResult // null)
}'
echo

echo "→ Trigger library scan (may take a while on first incomplete-retry pass)"
HTTP_CODE="$(curl -sS -o /tmp/tikr-library-scan.json -w '%{http_code}' -X POST "${API_URL}/api/library/scan" || true)"
if [[ ${HTTP_CODE} != "200" ]]; then
	echo "⚠️  POST /api/library/scan returned HTTP ${HTTP_CODE}"
	head -c 1200 /tmp/tikr-library-scan.json 2>/dev/null || true
	echo
	echo "   Continuing with corpus-health + search (existing embeddings may still prove RAG)."
else
	jq '{scanned, imported, skipped, failed, errorCount: (.errors|length)}' /tmp/tikr-library-scan.json
	jq -r '.errors[:8][]?' /tmp/tikr-library-scan.json 2>/dev/null || true
fi
echo

echo "→ Corpus health"
HEALTH_JSON="$(curl -sf "${API_URL}/api/ai/corpus-health")"
echo "${HEALTH_JSON}" | jq '{
  documentsTotal,
  documentsWithChunks,
  documentsSparseText,
  documentsChunkCoveragePercent,
  needsAttentionCount: (.needsAttention|length)
}'
echo "${HEALTH_JSON}" | jq -r '.needsAttention[:10][]?' 2>/dev/null || true
echo

echo "→ Semantic search: ${QUERY}"
SEARCH_BODY="$(jq -n --arg q "${QUERY}" --argjson k "${TOP_K}" '{query:$q, topK:$k, minScore:0.3}')"
SEARCH_JSON="$(curl -sf -X POST "${API_URL}/api/ai/semantic-search" \
	-H 'Content-Type: application/json' \
	-d "${SEARCH_BODY}")"
echo "${SEARCH_JSON}" | jq '{
  embeddingAvailable,
  considered,
  hitCount: (.hits|length),
  top: [.hits[:3][] | {fileName, suggestedFolder, topic, snippet: (.snippet[:160]), score}]
}'
echo

HIT_COUNT="$(echo "${SEARCH_JSON}" | jq '.hits|length')"
EMBED_OK="$(echo "${SEARCH_JSON}" | jq -r '.embeddingAvailable')"
HAS_SNIPPET="$(echo "${SEARCH_JSON}" | jq '[.hits[].snippet | select(. != null and . != "")] | length')"

if [[ ${EMBED_OK} != "true" ]]; then
	echo "❌ FAIL: embeddings unavailable (is Ollama running with nomic-embed-text?)"
	exit 1
fi
if [[ ${HIT_COUNT} -lt 1 ]]; then
	echo "❌ FAIL: no semantic hits for query=${QUERY}"
	echo "   Try a distinctive phrase from a known share filing, or wait for OCR/incomplete retry."
	exit 1
fi
if [[ ${HAS_SNIPPET} -lt 1 ]]; then
	echo "❌ FAIL: hits returned but no snippets (filename-only — corpus text still thin)"
	exit 1
fi

echo "✅ API smoke passed: ${HIT_COUNT} hit(s) with content snippets from the shared repository corpus."
echo
echo "Manual UI (required for Sources footer across share domains):"
echo "  1. Open http://${NAS_HOST}:8080/assistant"
echo "  2. Ask across domains (minutes, budget/mill levy, ordinance/permit) — not council-only"
echo "  3. Confirm **Sources** lists label + excerpt text from Town of Wiley Shared Documents"
echo "  Default API query was: ${QUERY}"
