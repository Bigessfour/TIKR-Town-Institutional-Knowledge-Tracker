#!/usr/bin/env bash
# Ollama-powered CI failure triage for GitHub Actions.
# Fetches failed workflow logs, runs structured prompts, writes artifacts under outputs/.
set -euo pipefail

MODEL="${OLLAMA_MODEL:-qwen2.5:7b}"
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
  local snapshot_dir="$OUTPUT_DIR/failure-snapshot"

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

  # Best-effort: download the structured failure snapshot emitted by build-and-test (or trunk)
  mkdir -p "$snapshot_dir"
  downloaded=false
  for name in "ci-failure-snapshot-${run_id}" "ci-failure-snapshot" "ci-failure-snapshot-trunk-${run_id}"; do
    if gh run download "$run_id" -n "$name" --dir "$snapshot_dir" 2>>"$OUTPUT_DIR/gh-fetch-errors.txt"; then
      log "Downloaded snapshot $name into $snapshot_dir"
      cat "$snapshot_dir"/* 2>/dev/null | head -c 8000 >> "$out_file" || true
      downloaded=true
      break
    fi
  done
  if [ "$downloaded" = false ]; then
    log "No ci-failure-snapshot* artifact available (or download failed) — will rely on job metadata + prefilter"
  fi

  # Additional structured signal: list failed jobs/steps via API (even if body logs missing)
  {
    echo ""
    echo "=== JOB/STEP METADATA (from GH API, resilient to log fetch) ==="
    gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${run_id}/jobs" \
      --jq '.jobs[] | select(.conclusion=="failure") | {job: .name, steps: [.steps[] | select(.conclusion=="failure") | {step: .name, number: .number, conclusion: .conclusion}]}' 2>/dev/null || echo "(job metadata unavailable)"
  } >> "$out_file" || true

  if [ ! -s "$out_file" ]; then
    echo "No logs retrieved for run $run_id. Check permissions (actions: read) and run id." >"$out_file"
  fi
}

# Pre-filter logs for high-signal lines (coverage, errors, format, trunk) to help when full logs are huge or missing bodies.
prefilter_logs() {
  local raw="$OUTPUT_DIR/failed-workflow-logs.txt"
  local filtered="$OUTPUT_DIR/high-signal-logs.txt"
  if [ ! -f "$raw" ]; then
    echo "no raw logs" > "$filtered"
    return 0
  fi
  # Extract lines with strong signals + surrounding context via grep -B/-A where available
  python3 - "$raw" "$filtered" <<'PY'
import sys, re
from pathlib import Path
raw = Path(sys.argv[1]).read_text(errors="replace")
outp = Path(sys.argv[2])
signals = re.compile(r'(?i)(error|fail|exception|coverage|threshold|dotnet format|verify-no-changes|trunk|exit code|FAIL|error:|at |in .*cs:line)', re.M)
lines = raw.splitlines()
kept = []
for i, line in enumerate(lines):
    if signals.search(line):
        # include a little context
        for j in range(max(0, i-1), min(len(lines), i+2)):
            kept.append(lines[j])
        kept.append("---")
if not kept:
    kept = ["(no high-signal lines matched; full logs may be empty or only headers)"]
# Dedup consecutive dups lightly
result = []
prev = None
for l in kept:
    if l != prev:
        result.append(l)
    prev = l
outp.write_text("\n".join(result[:400]) + "\n", encoding="utf-8")
print(f"prefiltered {len(result)} high-signal lines")
PY
  log "High-signal prefilter written: $filtered ($(wc -c <"$filtered" | tr -d ' ') bytes)"
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
    echo "=== TIKR CI STRUCTURE NOTE ==="
    echo "Primary gates: build-and-test (coverage verify via scripts/check_coverage.py, docker+e2e) + trunk-check (dotnet format + trunk)."
    echo "Ollama diagnosis runs only on failure(). Structured failure snapshots (if present) and job metadata are appended to logs."
    echo ""
    if [ -f "$OUTPUT_DIR/high-signal-logs.txt" ]; then
      echo "=== HIGH-SIGNAL PREFILTERED LINES (errors/coverage/format) ==="
      cat "$OUTPUT_DIR/high-signal-logs.txt"
      echo ""
    fi
    echo "=== Failed step logs (truncated; may include appended snapshot) ==="
    truncate_logs "$OUTPUT_DIR/failed-workflow-logs.txt" "$MAX_LOG_CHARS"
    echo ""
    echo "=== FAILURE SNAPSHOT ARTIFACT (if downloaded) ==="
    if ls "$OUTPUT_DIR/failure-snapshot/"* >/dev/null 2>&1; then
      for f in "$OUTPUT_DIR/failure-snapshot/"*; do
        echo "--- $(basename "$f") ---"
        head -c 4000 "$f" || true
        echo ""
      done
    else
      echo "(no snapshot files)"
    fi
  } >"$context_file"
  echo "$context_file"
}

write_github_step_summary() {
  local summary_path="${GITHUB_STEP_SUMMARY:-}"
  if [ -z "$summary_path" ]; then
    return 0
  fi

  {
    echo "## Ollama CI Diagnosis"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Model | \`${MODEL}\` |"
    echo "| Workflow | ${FAILED_WORKFLOW_NAME:-unknown} |"
    echo "| Failed run | [${RUN_ID:-unknown}](${FAILED_RUN_URL:-#}) |"
    echo "| SHA | \`${FAILED_HEAD_SHA:-unknown}\` |"
    echo ""
    echo "### Triage"
    echo '```'
    head -n 30 "$OUTPUT_DIR/01-triage.txt" 2>/dev/null || echo "unavailable"
    echo '```'
    echo ""
    echo "### Immediate fix"
    echo '```'
    sed -n '/IMMEDIATE_FIX:/,/VERIFY_LOCALLY:/p' "$OUTPUT_DIR/02-fix-plan.txt" 2>/dev/null | head -n 20 || echo "unavailable"
    echo '```'
    echo ""
    echo "Download artifact \`ollama-ci-diagnosis-${RUN_ID}\` for full reports."
  } >>"$summary_path"
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
    echo "- **Default:** \`qwen2.5:7b\` — better reasoning for logs, coverage thresholds, and .NET stack traces. ~4.5-5.5 GB on GH ubuntu-latest (7 GB RAM); use OLLAMA_MODEL override if tight."
    echo "- **Fast/low-RAM fallback:** \`llama3.2:3b\` — TIKR runtime default; ~2 GB."
    echo "- **Speed:** \`llama3.2:1b\` — one-line triage only."
  } >"$summary_file"
}

main() {
  log "Starting diagnosis (model=$MODEL)"
  wait_for_ollama

  log "Pulling model $MODEL (skip if cached)"
  ollama pull "$MODEL"

  fetch_failed_logs "$RUN_ID"
  prefilter_logs
  local context
  context="$(build_context_file)"
  log "Context written to $context"

  local triage_prompt fix_prompt loop_prompt
  triage_prompt=$(cat <<EOF
You are a senior DevOps engineer triaging a failed GitHub Actions workflow for TIKR (local-first .NET 10 Blazor Interactive Server + Minimal API + EF Core/SQLite, Syncfusion, Trunk + dotnet format, Docker on NAS).

TIKR-SPECIFIC CONTEXT:
- CI structure (see .github/workflows/ci.yml): build-and-test runs restore/build/test --collect XPlat coverage, "Verify per-assembly coverage thresholds" (python3 scripts/check_coverage.py), Docker smoke + E2E; trunk-check runs "dotnet format TIKR.sln --verify-no-changes" + trunk-io action.
- Coverage targets (scripts/check_coverage.py): TIKR.Shared 83%, Infrastructure/Api 90%, Web 85% (testable Helpers/Services only; DTOs pre-excluded).
- Common past failures: coverage 0.1% under threshold (e.g. 82.9 vs 83 after Dto exclusion), dotnet format FINALNEWLINE or CHARSET on .cs/migrations, gh log fetch "No logs" due to timing/permissions, docker wait or agent-scan smoke, E2E playwright.
- Always prefer root cause in code/config/CI steps over "logs unavailable".

CRITICAL INSTRUCTION — HANDLE MISSING / EMPTY / TRUNCATED LOGS:
If "failed-workflow-logs.txt" or the logs section shows only "No logs retrieved...", fetch errors, empty output, or is heavily truncated:
- DO NOT stop and report only the log-fetch problem as the diagnosis.
- Use ALL signals: metadata (job names, workflow, conclusion, SHA, branch), known TIKR CI steps above, any "failure-context/" or snapshot contents referenced in context, step ordering, and prior patterns.
- Explicitly note "logs unavailable or limited — best-effort triage from CI structure + metadata + TIKR patterns".
- Still fill FAILING_JOB / FAILING_STEP (infer from step names like "Verify per-assembly..." or "dotnet format" if evident) and give ROOT_CAUSE + fix.

Respond in this exact structure:

FAILING_JOB:
FAILING_STEP:
ERROR_SIGNATURE: (one line — the key error message or exit code)
ROOT_CAUSE: (2-3 sentences; cite TIKR pattern if logs missing)
CONFIDENCE: low|medium|high

Logs and metadata:
$(cat "$context")
EOF
)

  fix_prompt=$(cat <<EOF
You are fixing a failed TIKR CI workflow (.NET 10 Blazor + API + EF, Trunk, Docker smoke).

TIKR-SPECIFIC: Fix must be compatible with AGENTS.md (run "dotnet test TIKR.sln --configuration Release" + "trunk check --all" locally before PR). Prefer edits to scripts/check_coverage.py, .cs files for format, ci.yml steps, or threshold comments. Coverage often fixed by DTO exclusion or tiny target tweak.

Using the triage below and the same logs, produce an actionable fix plan.

CRITICAL: If logs were missing in triage, base IMMEDIATE_FIX on the inferred failing step from metadata + TIKR patterns (e.g. "coverage verify just under" → inspect check_coverage output + recent DTO changes; "format" → run dotnet format locally and commit).

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

TIKR-SPECIFIC: The goal of this tool is to make "done detector" (see docs/action-items.md Project-Level Done Detector gate) reliable — agents and humans need fast, accurate diagnosis even when gh log fetch is incomplete.

Given the triage and fix plan, recommend how to detect and fix this class of failure faster next time.

CRITICAL: When logs were missing, recommend concrete artifact or pre-filter improvements (emit structured failure-*.txt from coverage/format steps; download prior-job artifacts in ollama job; pre-grep for ERROR/FAIL/coverage lines in fetch_failed_logs).

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
  write_github_step_summary

  log "Diagnosis complete — artifacts in $OUTPUT_DIR"
}

main