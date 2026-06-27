# Dependabot PR Policy

How TIKR handles dependency update pull requests. Config: [`.github/dependabot.yml`](../.github/dependabot.yml). Auto-merge: [`.github/workflows/dependabot-auto-merge.yml`](../.github/workflows/dependabot-auto-merge.yml).

## Principles (GitHub-recommended)

- **Security updates** take priority over routine version bumps.
- **Reduce PR noise** via grouped patch/minor updates and `open-pull-requests-limit`.
- **Never merge** a Dependabot PR with failing required checks (`build-and-test`, `trunk_check`).
- **Major upgrades** require manual review; some test-stack majors are deferred (see ignore rules in config).

## Merge tiers

| Update type | Ecosystem | Action |
|-------------|-----------|--------|
| Security | Any | Merge within 7 days; prefer grouped security PR; CI must pass |
| Patch / minor | NuGet, GitHub Actions | **Auto-merge** when required checks pass |
| Major | NuGet (general) | Manual review + `dotnet test`; owner merges |
| Major | `bunit`, `coverlet.collector`, `Microsoft.NET.Test.Sdk` | **Deferred** until Phase 7 test harness work — ignored in config |

## Required CI (non-negotiable)

- **TIKR CI** — `build-and-test` (restore, build, test, Docker smoke optional)
- **Trunk** — `trunk_check` (gitleaks, yaml/md/docker lint, dotnet format verify)

## Weekly rhythm

- Dependabot runs **Mondays 09:00 America/Denver**.
- Maintainer triage: review open Dependabot PRs, close superseded stale PRs, confirm auto-merge on eligible PRs.

## Triage playbook

### Stale PRs (red CI on old base)

If `main` moved or CI was broken when a PR opened:

1. Close the stale PR with a short comment, **or** comment `@dependabot rebase`.
2. After [`.github/dependabot.yml`](../.github/dependabot.yml) changes merge, wait for fresh grouped PRs.

### Never do

- Merge with red `build-and-test` or `trunk_check`.
- Auto-merge **major** version updates.
- Auto-merge **security** PRs without review (manual merge preferred).

## Labels

Dependabot PRs receive: `dependencies`, `dependabot`.

## References

- [Optimizing Dependabot PRs](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/optimizing-pr-creation-version-updates)
- [Automating Dependabot with GitHub Actions](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/automating-dependabot-with-github-actions)
- [Grouped security updates](https://docs.github.com/en/code-security/dependabot/dependabot-security-updates/configuring-dependabot-security-updates)
