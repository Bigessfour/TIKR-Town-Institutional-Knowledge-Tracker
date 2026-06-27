# Pull Request

## Summary

<!-- What changed and why? -->

## Test plan

- [ ] `dotnet test TIKR.sln --configuration Release`
- [ ] `trunk check` (or `trunk check --all` before merge)
- [ ] Manual smoke test if UI/API behavior changed

## Secrets check

- [ ] No `.env`, `docker/.env`, `.cursor/mcp.json`, or key files added
- [ ] Placeholder values only in committed templates and test fixtures
