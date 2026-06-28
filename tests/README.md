# TIKR Test Strategy

TIKR targets **>90% line coverage** across unit, integration, and component tests. This folder is the foundation — coverage thresholds ramp up in CI as suites grow.

## Project layout

| Project | Scope | Examples |
|---------|-------|----------|
| `TIKR.Shared.Tests` | Pure configuration/helpers | `TikrConfiguration`, `EnvLoader` |
| `TIKR.Infrastructure.Tests` | Services, seeding, storage | `HybridAiService`, `DbSeeder`, `LocalFileStorageService` |
| `TIKR.Api.Tests` | HTTP integration (`WebApplicationFactory`) | Requirements CRUD, documents, AI endpoints, auth |
| `TIKR.Web.Tests` | bUnit + HTTP client unit tests | Settings page, login/users pages, `TikrApiClient` |

**Planned (vNext):**

| Project | Scope |
|---------|-------|
| E2E (Playwright) | Clerk flows against Docker stack — `tests/e2e/` (includes Requirements AI Scan stub) |

Fixtures for agent-scan live in **`tests/fixtures/agent-scan/`** (shared by API integration tests, Playwright, and licensed CI workflow).

Licensed Syncfusion PDF/DOCX tests (`Category=SyncfusionLicensed`) skip when `SYNCFUSION_LICENSE_KEY` is unset. Run locally with the key set, or via **TIKR Syncfusion Agent Smoke** workflow.

Licensed Grok tests (`Category=GrokLicensed`) skip when `GROK_API_KEY` / `XAI_API_KEY` is unset. Default model is **`grok-3`** (xAI returns HTTP 400 for deprecated `grok-2-latest`).

Keychain helpers (macOS Passwords):

```bash
./scripts/sync-syncfusion-license-key.sh --export   # SYNCFUSION_LICENSE_KEY
./scripts/sync-grok-key.sh --export                 # GROK_API_KEY from XAI_API_KEY
```

```bash
eval "$(./scripts/sync-syncfusion-license-key.sh --export)"
eval "$(./scripts/sync-grok-key.sh --export)"
dotnet test tests/TIKR.Api.Tests/TIKR.Api.Tests.csproj --filter "Category=SyncfusionLicensed|Category=GrokLicensed"
```

### Playwright (Phase 0 + 10C)

```bash
docker compose -f docker/docker-compose.yml up --build
cd tests/e2e && npm ci && npx playwright install chromium
TIKR_E2E_BASE_URL=http://localhost:8080 npm test
```

## Run locally

```bash
# All tests
dotnet test TIKR.sln

# With coverage (enforces floor in coverlet.runsettings)
dotnet test TIKR.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Single project
dotnet test tests/TIKR.Infrastructure.Tests
```

## FullyTested ship-bar filter

Core MVP endpoints and stub closures are tagged `[Trait("Category", FullyTested)]`:

```bash
dotnet test TIKR.sln --configuration Release --filter "Category=FullyTested"
```

## Coverage policy

- **Targets (line coverage, per assembly):** Shared ≥90%, Infrastructure ≥90%, Api ≥90% (integration-tested endpoints), Web ≥85% on `Helpers/` + `Services/` (Blazor pages smoke-tested via bUnit)
- **CI enforcement:** `scripts/check_coverage.py` parses Cobertura artifacts after `dotnet test`
- **Excluded:** EF migrations, `Program.cs` / `Program.Partial.cs`, generated code
- **Run locally:**

```bash
dotnet test TIKR.sln --configuration Release \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory coverage
python3 scripts/check_coverage.py coverage
```

## Conventions

- **xUnit** for all test projects
- **FluentAssertions** for readable assertions
- **Moq** for external dependencies (Ollama, Grok HTTP)
- Integration tests use isolated SQLite files + temp storage (see `TikrWebApplicationFactory`)
- **Auth:** default API factory sets `TIKR_AUTH_ENABLED=false` so existing tests stay open; use `AuthEnabledWebApplicationFactory` for JWT/login tests
- No real Ollama/Grok calls in CI — external AI is mocked or disabled

## Adding tests

1. Prefer testing behavior through public APIs
2. Use `TestDbContextFactory` for EF-backed unit tests
3. Use `TikrWebApplicationFactory` for API integration tests
4. Keep tests fast — no network except loopback stubs
