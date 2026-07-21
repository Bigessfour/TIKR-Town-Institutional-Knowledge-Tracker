# GitHub Spec Kit (SDD) in TIKR

[GitHub Spec Kit](https://github.com/github/spec-kit) adds **Spec-Driven Development (SDD)** to TIKR: structured specs, plans, and tasks before implementation — integrated with Cursor Agent skills.

**Upstream reference clone** (outside this repo, for reading templates/docs):

```
/Users/stephenmckitrick/TIKR/spec-kit
```

## What was installed

| Path                        | Purpose                                                   |
| --------------------------- | --------------------------------------------------------- |
| `.specify/`                 | Templates, scripts, constitution, integration manifest    |
| `.cursor/skills/speckit-*/` | Cursor Agent skills (slash commands)                      |
| `specs/`                    | Feature artifacts (created per feature — not yet present) |

**CLI:** `specify-cli` v0.13.0 via `uv tool install specify-cli`
**Integration:** `cursor-agent` (skills invoke as `/speckit-constitution`, `/speckit-specify`, etc.)

Verify status:

```bash
export PATH="$HOME/.local/bin:$PATH"
specify integration status
```

## Prerequisites (one-time, machine-wide)

```bash
# uv (already on this Mac via Homebrew)
brew install uv

# Spec Kit CLI
uv tool install specify-cli
uv tool update-shell   # adds ~/.local/bin to PATH if needed
```

Upgrade later:

```bash
specify self upgrade
specify integration upgrade cursor-agent
```

## Active feature

| Field       | Value                                                      |
| ----------- | ---------------------------------------------------------- |
| Feature ID  | `001-requirements-document-agent`                          |
| Path        | `specs/001-requirements-document-agent/`                   |
| Status      | PR #72 — 45/46 done; T031 Deb Dell sign-off remains open   |
| Aligns with | incremental-plan Phase 10 + Phase 0 ship closure           |

Shell exports (optional):

```bash
export SPECIFY_FEATURE=001-requirements-document-agent
export SPECIFY_FEATURE_DIRECTORY="$PWD/specs/001-requirements-document-agent"
```

## Brownfield workflow (existing TIKR)

TIKR already has [AGENTS.md](../AGENTS.md), [incremental-plan.md](incremental-plan.md), and architecture docs. Spec Kit **adds feature-scoped artifacts** under `specs/` without replacing the incremental plan.

### Recommended sequence

```mermaid
flowchart LR
  A["/speckit-constitution"] --> B["/speckit-specify"]
  B --> C["/speckit-clarify optional"]
  C --> D["/speckit-plan"]
  D --> E["/speckit-checklist optional"]
  E --> F["/speckit-tasks"]
  F --> G["/speckit-analyze optional"]
  G --> H["/speckit-implement"]
  H --> I["/speckit-converge"]
```

| Step | Skill                   | When                                                                   |
| ---- | ----------------------- | ---------------------------------------------------------------------- |
| 1    | `/speckit-constitution` | **Done** — seeded at `.specify/memory/constitution.md` from TIKR rules |
| 2    | `/speckit-specify`      | New feature or change — describe **what** and **why** (no stack yet)   |
| 3    | `/speckit-clarify`      | Optional — before plan, if requirements are fuzzy                      |
| 4    | `/speckit-plan`         | Provide stack: .NET 10, Blazor, EF Core, Ollama, etc.                  |
| 5    | `/speckit-checklist`    | Optional — quality checklist after plan                                |
| 6    | `/speckit-tasks`        | Break plan into ordered, testable tasks                                |
| 7    | `/speckit-analyze`      | Optional — cross-check spec/plan/tasks before coding                   |
| 8    | `/speckit-implement`    | Execute tasks (agent implements in repo)                               |
| 9    | `/speckit-converge`     | Brownfield gap analysis — append remaining work                        |

### Start a new feature (CLI helper)

From repo root:

```bash
.specify/scripts/bash/create-new-feature.sh "Add recurring deadline reminders" --short-name deadline-reminders
# Creates specs/00N-deadline-reminders/spec.md and prints SPECIFY_FEATURE exports
```

Then run `/speckit-specify` in Cursor with your feature description; artifacts land under `specs/<feature-id>/`.

### Git branch alignment

Spec Kit feature folders use names like `001-deadline-reminders`. Align git branches:

```bash
git checkout -b feature/deadline-reminders
```

## Relationship to other TIKR docs

| Doc                                        | Role                                     |
| ------------------------------------------ | ---------------------------------------- |
| [incremental-plan.md](incremental-plan.md) | Phased roadmap — **what phase we're in** |
| [AGENTS.md](../AGENTS.md)                  | Always-on agent rules (RAG, secrets, CI) |
| `.specify/memory/constitution.md`          | SDD governance for feature specs         |
| `specs/<feature>/`                         | Per-feature spec, plan, tasks            |

When starting work, read **incremental-plan** for phase context, then use **Spec Kit** for the specific feature slice.

## Optional: GitHub issues from tasks

```
/speckit-taskstoissues
```

Creates GitHub issues from the current feature's task list (requires `gh` auth).

## Troubleshooting

| Issue                                | Fix                                                                  |
| ------------------------------------ | -------------------------------------------------------------------- |
| `specify: command not found`         | `export PATH="$HOME/.local/bin:$PATH"` or run `uv tool update-shell` |
| Skills not visible in Cursor         | Reload window; skills live in `.cursor/skills/`                      |
| Modified managed files after upgrade | `specify integration upgrade cursor-agent --force`                   |
| Re-init after CLI upgrade            | `specify init --here --integration cursor-agent --force`             |

## References

- [Spec Kit docs](https://github.github.io/spec-kit/)
- [Spec Kit repo](https://github.com/github/spec-kit)
- Local clone: `/Users/stephenmckitrick/TIKR/spec-kit`
