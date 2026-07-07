#!/usr/bin/env bash
# Ollama-powered CI failure triage for GitHub Actions.
# Fetches failed workflow logs, runs structured prompts, writes artifacts under outputs/.
set -euo pipefail

MODEL="${OLLAMA_MODEL:-llama3.2:3b}"
OUTPUT_DIR="${OUTPUT_DIR:-outputs}"
MAX_LOG_CHARS="${MAX_LOG_CHARS:-24000}"
RUN_ID="${1:-}"

mkdir -p "$OUTPUT_DIR"

log() {
  echo "[ollama-ci-diagnosis] $*"
}

wait_for_ollama() {
  local attempt
  for attempt in $(seq 1 30); do
    if curl -sf http://localhost:11434/api/tags >/dev/null 2>&1; then
      log "Ollama API ready (attempt $attempt)"
      return 0
    fi
    sleep 2
  done
  echo "Ollama API did not become ready within 60 seconds" >&2
  return 1
}

truncate_logs() {
  local file="$1"
  local limit="$2"
  python3 - "$file" "$limit" <<'PY'
import sys
from pathlib import Path

path = Path(sys.argv[1])
limit = int(sys.argv[2])
text = path.read_text(encoding="utf-8", errors="replace")
if len(text) <= limit:
    print(text, end="")
else:
    head = limit // 2
    tail = limit - head
    omitted = len(text) - head - tail
    print(text[:head], end="")
    print(f"\n\n... [{omitted} characters truncated for model context] ...\n\n", end="")
    print(text[-tail:], end="")
PY
}

fetch_failed_logs() {
  local run_id="$1"
  local out_file="$OUTPUT_DIR/failed-workflow-logs.txt"

  if [ -z "$run_id" ]; then
    echo "No workflow run id supplied." >"$out_file"
    return 0
  fi

  log "Fetching failed logs for run $run_id"
  if gh run view "$run_id" --log-failed >"$out_file" 2>"$OUTPUT_DIR/gh-fetch-errors.txt"; then
    log "Saved failed logs ($(wc -c <"$out_file" | tr -d ' ') bytes)"
  else
    log "gh run view --log-failed failed; falling back to full run log"
    gh run view "$run_id" --log >"$out_file" 2>>"$OUTPUT_DIR/gh-fetch-errors.txt" || true
  fi

  if [ ! -s "$out_file" ]; then
    echo "No logs retrieved for run $run_id. Check permissions (actions: read) and run id." >"$out_file"
  fi
}

run_prompt() {
  local name="$1"
  local prompt="$2"
  local outfile="$OUTPUT_DIR/${name}.txt"

  log "Running prompt: $name (model=$MODEL)"
  {
    echo "# Ollama CI Diagnosis — ${name}"
    echo "# Model: ${MODEL}"
    echo "# Generated: $(date -u '+%Y-%m-%dT%H:%M:%SZ')"
    echo ""
    ollama run "$MODEL" "$prompt"
  } | tee "$outfile"
  echo ""
}

build_context_file() {
  local context_file="$OUTPUT_DIR/ci-context.txt"
  {
    echo "Repository: ${GITHUB_REPOSITORY:-unknown}"
    echo "Workflow: ${FAILED_WORKFLOW_NAME:-unknown}"
    echo "Run ID: ${RUN_ID:-unknown}"
    echo "Run URL: ${FAILED_RUN_URL:-unknown}"
    echo "Head SHA: ${FAILED_HEAD_SHA:-unknown}"
    echo "Branch: ${FAILED_HEAD_BRANCH:-unknown}"
    echo "Event: ${FAILED_EVENT:-unknown}"
    echo "Conclusion: ${FAILED_CONCLUSION:-unknown}"
    echo ""
    echo "=== Failed step logs (truncated) ==="
    truncate_logs "$OUTPUT_DIR/failed-workflow-logs.txt" "$MAX_LOG_CHARS"
  } >"$context_file"
  echo "$context_file"
}

write_summary() {
  local summary_file="$OUTPUT_DIR/SUMMARY.md"
  {
    echo "# Ollama CI Failure Diagnosis"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Model | \`${MODEL}\` |"
    echo "| Workflow | ${FAILED_WORKFLOW_NAME:-unknown} |"
    echo "| Run | [${RUN_ID:-unknown}](${FAILED_RUN_URL:-#}) |"
    echo "| SHA | \`${FAILED_HEAD_SHA:-unknown}\` |"
    echo ""
    echo "## Reports"
    echo "- [01-triage.txt](./01-triage.txt) — root cause and failing step"
    echo "- [02-fix-plan.txt](./02-fix-plan.txt) — concrete fix steps"
    echo "- [03-feedback-loop.txt](./03-feedback-loop.txt) — validation and prevention"
    echo ""
    echo "## Recommended model"
    echo "- **Default:** \`llama3.2:3b\` — TIKR standard; best balance of log comprehension and runner RAM (~2GB)."
    echo "- **Fast triage:** \`llama3.2:1b\` — quicker runs when only a one-line error is needed."
    echo "- **Deep analysis:** \`qwen2.5:3b\` — stronger stack-trace reasoning; slower pull on cold runners."
  } >"$summary_file"
}

main() {
  log "Starting diagnosis (model=$MODEL)"
  wait_for_ollama

  log "Pulling model $MODEL (skip if cached)"
  ollama pull "$MODEL"

  fetch_failed_logs "$RUN_ID"
  local context
  context="$(build_context_file)"
  log "Context written to $context"

  local triage_prompt fix_prompt loop_prompt
  triage_prompt=$(cat <<EOF
You are a senior DevOps engineer triaging a failed GitHub Actions workflow for a .NET 10 Blazor project (TIKR).

Analyze ONLY the logs below. Respond in this exact structure:

FAILING_JOB:
FAILING_STEP:
ERROR_SIGNATURE: (one line — the key error message or exit code)
ROOT_CAUSE: (2-3 sentences)
CONFIDENCE: low|medium|high

Logs and metadata:
$(cat "$context")
EOF
)

  fix_prompt=$(cat <<EOF
You are fixing a failed TIKR CI workflow (.NET 10, EF Core, Docker, Trunk lint).

Using the triage below and the same logs, produce an actionable fix plan.

TRIAGE:
$(cat "$OUTPUT_DIR/01-triage.txt" 2>/dev/null || echo "unavailable")

Respond in this exact structure:

IMMEDIATE_FIX: (numbered steps — exact commands or file paths in this repo)
VERIFY_LOCALLY: (commands: dotnet test, trunk check, docker compose, etc.)
ESTIMATED_EFFORT: minutes|hours
RISKS: (what could still fail)

Logs and metadata:
$(cat "$context")
EOF
)

  loop_prompt=$(cat <<EOF
You are improving the CI feedback loop for TIKR (.NET 10, GitHub Actions, Ollama on NAS).

Given the triage and fix plan, recommend how to detect and fix this class of failure faster next time.

TRIAGE:
$(cat "$OUTPUT_DIR/01-triage.txt" 2>/dev/null || echo "unavailable")

FIX PLAN:
$(cat "$OUTPUT_DIR/02-fix-plan.txt" 2>/dev/null || echo "unavailable")

Respond in this exact structure:

PRE_COMMIT_CHECK: (what to run before push)
CI_HARDENING: (workflow or script changes — be specific)
OLLAMA_PROMPT_TIP: (one better prompt for this failure type)
PREVENTION: (one guardrail to add to the repo)
EOF
)

  run_prompt "01-triage" "$triage_prompt"
  run_prompt "02-fix-plan" "$fix_prompt"
  run_prompt "03-feedback-loop" "$loop_prompt"
  write_summary

  log "Diagnosis complete — artifacts in $OUTPUT_DIR"
}

main