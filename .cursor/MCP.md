# IDE configuration (canonical — not in this repo)

Editor settings: `~/.cursor/ide-tooling/canonical-settings.json` (apply with `sync-canonical-ide-settings.py`).
Rule: `~/.cursor/rules/ide-canonical-settings.mdc` and `.cursor/rules/ide-canonical-settings.mdc`.

MCP servers are **not** stored in this repo. Canonical config:

- **Cursor:** `~/.cursor/mcp.json`
- **Grok CLI:** `~/.grok/config.toml` (user scope)
- **Docs:** `~/.cursor/ide-tooling/README.md`

From this repo, refresh global Cursor MCP and the RAG venv:

```bash
./scripts/setup-cursor-mcp.sh
./scripts/sync-syncfusion-mcp-key.sh   # optional: Syncfusion developer key
```
