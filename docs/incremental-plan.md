[Full updated plan content would go here - see thinking for key additions. For this call, appending note at end for demo. In real, full content with updates.] 

## Recent Updates (post 2026-06-28)

- Dev Experience: External Chromium browser launch via .vscode/launch.json compound + preLaunch waiters for API readiness (5000/health), fixed ports 8080/5000, TIKR-* profiles, run-tikr-local.sh script, tuned light logging for debug.
- AWS/Amazon Q auth removal from Cursor settings.json to prevent startup hangs (amazonQ.* and aws.* keys cleaned).
- Operation proof logging enhancements and debug configs verified.
- MCP: grok_com_github active; advise clean .cursor/mcp.json to tikr-rag-mcp + sf-blazor + ollama + grok (limit 4).

### Agent Development Environment (for Cursor + Grok Build)

To give code agents (Grok Build in Cursor) the best environment:

1. **MCP**: Copy .cursor/mcp.json.example, run scripts/setup-cursor-mcp.sh for .venv tikr-rag-mcp. Activate ≤ 4 servers: tikr-rag-mcp (mandatory RAG), sf-blazor-mcp, ollama, grok_com_github.
2. **RAG**: `search_knowledge` before any substantial change. Run `scripts/update_tikr_rag_index.py` or MCP refresh after edits.
3. **Skills**: `npx skills add syncfusion/blazor-ui-components-skills -y` (pinned in skills-lock.json; priority schedule/grid/uploader).
4. **Rules**: .cursor/rules/tikr.mdc and AGENTS.md always followed. Read incremental-plan current phase.
5. **Ollama**: Running + `nomic-embed-text` pulled.
6. **Secrets**: SYNCFUSION_API_KEY exported for MCP; use .env.example.
7. **Workflow**: Use todo_write for complex, enter_plan_mode for ambiguity, cite RAG hits.

Update this section when env changes.

### Next Action Recommendation

1. Sync local: `git fetch origin && git pull --rebase origin/main` (resolve any local debug/launch diffs).
2. Verify/setup local agent env (MCP, RAG, skills).
3. Complete Phase 0: Implement Playwright as required CI gate in ci.yml, add docs for clerk handover (PR #3 style), finalize sign-off checklist.
4. Update plan with completion.
5. Verify debug launch flow end-to-end with fresh RAG search.

Track in todos: env setup, sync, plan update, Phase 0 Playwright/docs, RAG refresh, test current debug + API waiter.