# Quickstart: Validate Requirements & Document Agent

**Feature**: `001-requirements-document-agent`
**Prerequisites**: .NET 10 SDK, Docker (or local `dotnet run`), Ollama with `llama3.2:3b` optional for orchestrator

## 1. Local stack (stub path — no Syncfusion license required)

```bash
cd /Users/stephenmckitrick/TIKR/TIKR-Town-Institutional-Knowledge-Tracker
cp docker/.env.example docker/.env
# Ensure USE_SYNCFUSION_AGENT_TOOLS=false (default)

docker compose -f docker/docker-compose.yml --env-file docker/.env up --build
```

Open http://localhost:8080/requirements

**Expected**: Grid loads with seeded Colorado obligations; filters and CSV export work.

**Mac host conflicts:** If `:5000` (Control Center) or `:11434` (host Ollama) are busy, use licensed/alt-port ship-proof instead of default compose:

```bash
export SYNCFUSION_LICENSE_KEY=…   # or sync via ./scripts/setup-local-secrets.sh
./scripts/ship-proof-local.sh     # API :15000, Web :18080, Ollama :11435
```

## 2. Agent scan — plain text (FR-002, SC-001)

1. On `/requirements`, use **AI Scan uploaded doc**
2. Upload `tests/fixtures/agent-scan/wiley-periodic-report.txt`
3. Confirm banner: `Plain-text extraction`
4. Click **Apply** → create dialog pre-filled with title/text
5. Save → row appears in grid

**API-only check**:

```bash
curl -s -F "file=@tests/fixtures/agent-scan/wiley-periodic-report.txt" \
  http://localhost:5000/api/ai/agent-scan | jq '.usedSyncfusionTools, .suggestedTitle'
```

**Expected**: `false` and a non-empty suggested title.

## 3. Automated tests (SC-002)

```bash
dotnet test TIKR.sln --configuration Release
trunk check --all
python3 scripts/check_coverage.py coverage   # after test run with coverlet
```

**Expected**: All tests pass; Trunk clean.

## 4. Licensed Syncfusion path (SC-004 — optional)

Set in `docker/.env`:

```env
USE_SYNCFUSION_AGENT_TOOLS=true
SYNCFUSION_LICENSE_KEY=<your-key>
```

Restart stack, then:

```bash
curl -s -F "file=@tests/fixtures/agent-scan/minimal-clerk-report.pdf" \
  http://localhost:5000/api/ai/agent-scan | jq '.usedSyncfusionTools, .processedStoragePath'
```

**Expected**: `usedSyncfusionTools: true`; `processedStoragePath` set when archive generation succeeds.

Or run licensed integration tests locally:

```bash
SYNCFUSION_LICENSE_KEY=<key> dotnet test tests/TIKR.Api.Tests/TIKR.Api.Tests.csproj \
  --filter "FullyQualifiedName~DocumentAgentSyncfusionLicensed"
```

## 5. Playwright clerk smoke (SC-003)

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d
cd tests/e2e && npm ci && npx playwright test requirements-agent-scan.spec.ts
```

**Expected**: Spec passes against running stack (same as TIKR CI blocking gate).

## 6. Function inventory (SC-005)

```bash
python3 ~/.cursor/skills/function-inventory/scripts/update-function-inventory.py
# Curate docs/action-items.md for agent/requirements functions without proof
```

## 7. Convergence handoff

After implementing any tasks from `tasks.md`:

```
/speckit-converge
```

**Expected**: Either ✅ converged (no new tasks) or appended `## Phase N: Convergence` section with gap tasks.

## References

- [data-model.md](./data-model.md)
- [contracts/agent-scan-api.md](./contracts/agent-scan-api.md)
- [docs/nas-agent-tools-setup.md](../../docs/nas-agent-tools-setup.md)
- [docs/requirements-working-tree.md](../../docs/requirements-working-tree.md)
