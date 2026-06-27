# Security Policy

## Supported versions

TIKR is pre-production software with no deployed releases yet. Security fixes apply to the `main` branch.

## Reporting a vulnerability

If you discover a security issue, please **do not** open a public GitHub issue.

Instead, email the repository owner with:

- A description of the vulnerability
- Steps to reproduce
- Potential impact

We will acknowledge receipt within a few business days and work on a fix before public disclosure when appropriate.

## Secrets hygiene

Never commit real API keys, licenses, or database credentials. Use:

- `docker/.env` (gitignored) from `docker/.env.example`
- `.cursor/mcp.json` (gitignored) from `.cursor/mcp.json.example`
- `dotnet user-secrets` for local development

CI runs [gitleaks](https://github.com/gitleaks/gitleaks) via Trunk on every push and pull request.
