# TIKR Test Strategy

TIKR targets **>90% line coverage** across unit, integration, and component tests. This folder is the foundation — coverage thresholds ramp up in CI as suites grow.

## Project layout

| Project | Scope | Examples |
|---------|-------|----------|
| `TIKR.Shared.Tests` | Pure configuration/helpers | `TikrConfiguration`, `EnvLoader` |
| `TIKR.Infrastructure.Tests` | Services, seeding, storage | `HybridAiService`, `DbSeeder`, `LocalFileStorageService` |
| `TIKR.Api.Tests` | HTTP integration (`WebApplicationFactory`) | Requirements CRUD, documents, AI endpoints |
| `TIKR.Web.Tests` | bUnit + HTTP client unit tests | Settings page, `TikrApiClient` |

**Planned (vNext):**

| Project | Scope |
|---------|-------|
| E2E (Playwright / `cursor-ide-browser`) | Clerk flows against Docker stack |

## Run locally

```bash
# All tests
dotnet test TIKR.sln

# With coverage (enforces floor in coverlet.runsettings)
dotnet test TIKR.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Single project
dotnet test tests/TIKR.Infrastructure.Tests
```

## Coverage policy

- **Current CI:** collects Cobertura coverage artifacts; no hard floor yet (~40% on Infrastructure today)
- **Target:** 90%+ on `TIKR.Shared`, `TIKR.Infrastructure`, and `TIKR.Api`
- **Excluded:** EF migrations, generated code
- **Ramp plan:** enable `Threshold` in `coverlet.runsettings` at 60 → 75 → 90 as suites grow

## Conventions

- **xUnit** for all test projects
- **FluentAssertions** for readable assertions
- **Moq** for external dependencies (Ollama, Grok HTTP)
- Integration tests use isolated SQLite files + temp storage (see `TikrWebApplicationFactory`)
- No real Ollama/Grok calls in CI — external AI is mocked or disabled

## Adding tests

1. Prefer testing behavior through public APIs
2. Use `TestDbContextFactory` for EF-backed unit tests
3. Use `TikrWebApplicationFactory` for API integration tests
4. Keep tests fast — no network except loopback stubs
