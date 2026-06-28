#!/usr/bin/env python3
"""TIKR RAG MCP Server.

Semantic search over repo code + docs for Cursor agents (stdio MCP).
Uses local Ollama embeddings (nomic-embed-text). Index: .rag_index/index.json

Setup: ./scripts/setup-cursor-mcp.sh
Refresh: .venv/bin/python3 scripts/update_tikr_rag_index.py
"""

from __future__ import annotations

import fnmatch
import json
import math
import os
import sys
import time
from pathlib import Path
from typing import Any

try:
    import requests
except ImportError:
    print("Error: 'requests' required. Run ./scripts/setup-python-rag.sh", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[1]
INDEX_DIR = ROOT / ".rag_index"
INDEX_FILE = INDEX_DIR / "index.json"
OLLAMA_HOST = os.environ.get("OLLAMA_HOST", "http://localhost:11434").rstrip("/")
EMBED_MODEL = os.environ.get("TIKR_RAG_EMBED_MODEL", "nomic-embed-text")
CHUNK_SIZE = 800
CHUNK_OVERLAP = 100

EXCLUDE_DIRS = {
    ".agents/skills",
    ".git",
    ".rag_index",
    ".venv",
    "__pycache__",
    "bin",
    "coverage",
    "local-data",
    "node_modules",
    "obj",
}

INCLUDE_GLOBS = [
    "docs/**/*.md",
    "README.md",
    "AGENTS.md",
    "src/**/*.cs",
    "src/**/*.razor",
    "src/**/*.py",
    "scripts/**/*",
    "tests/**/*.cs",
    "*.md",
]


def log(msg: str) -> None:
    print(f"[tikr-rag] {msg}", file=sys.stderr)


def read_text_safe(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return ""


def should_include(path: Path) -> bool:
    try:
        rel = str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return False
    return any(fnmatch.fnmatch(rel, g) for g in INCLUDE_GLOBS)


def chunk_text(text: str) -> list[str]:
    if not text:
        return []
    chunks: list[str] = []
    start = 0
    n = len(text)
    while start < n:
        end = min(start + CHUNK_SIZE, n)
        chunks.append(text[start:end])
        if end >= n:
            break
        start = max(end - CHUNK_OVERLAP, start + 1)
    return chunks


def get_embedding(text: str) -> list[float]:
    """Call Ollama embeddings API."""
    url = f"{OLLAMA_HOST}/api/embeddings"
    try:
        resp = requests.post(
            url,
            json={"model": EMBED_MODEL, "prompt": text},
            timeout=120,
        )
        resp.raise_for_status()
        data = resp.json()
        return data.get("embedding") or []
    except Exception as exc:
        log(f"Embedding error: {exc}")
        return []


def cosine_similarity(a: list[float], b: list[float]) -> float:
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b, strict=False))
    na = math.sqrt(sum(x * x for x in a))
    nb = math.sqrt(sum(y * y for y in b))
    if na == 0 or nb == 0:
        return 0.0
    return dot / (na * nb)


def load_index() -> dict[str, Any]:
    if not INDEX_FILE.exists():
        return {}
    try:
        return json.loads(INDEX_FILE.read_text(encoding="utf-8"))
    except Exception:
        return {}


def save_index(index: dict[str, Any]) -> None:
    INDEX_DIR.mkdir(parents=True, exist_ok=True)
    INDEX_FILE.write_text(json.dumps(index), encoding="utf-8")


def build_index(force: bool = False) -> dict[str, Any]:
    """Walk the repo and (re)build the vector index."""
    existing = load_index()
    if not force and existing.get("chunks"):
        return existing

    chunks: list[dict[str, Any]] = []
    files_processed = 0

    for root, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in EXCLUDE_DIRS]
        for fname in files:
            fpath = Path(root) / fname
            if not should_include(fpath):
                continue
            text = read_text_safe(fpath)
            if not text.strip():
                continue
            rel_path = str(fpath.relative_to(ROOT)).replace("\\", "/")
            for i, chunk in enumerate(chunk_text(text)):
                emb = get_embedding(chunk)
                chunks.append(
                    {
                        "path": rel_path,
                        "chunk_id": i,
                        "text": chunk[:2000],
                        "embedding": emb,
                    }
                )
            files_processed += 1
            if files_processed % 20 == 0:
                log(f"Processed {files_processed} files...")

    index = {
        "chunks": chunks,
        "last_indexed": time.strftime("%Y-%m-%d %H:%M:%S"),
        "model": EMBED_MODEL,
        "total_files": files_processed,
    }
    save_index(index)
    return index


def search(query: str, top_k: int = 6) -> list[dict[str, Any]]:
    idx = load_index()
    stored = idx.get("chunks") or []
    if not query.strip() or not stored:
        return []
    q_emb = get_embedding(query)
    if not q_emb:
        return []
    scored: list[tuple[float, dict[str, Any]]] = []
    for item in stored:
        emb = item.get("embedding") or []
        if not emb:
            continue
        score = cosine_similarity(q_emb, emb)
        scored.append(
            (
                score,
                {
                    "score": round(score, 4),
                    "path": item.get("path", ""),
                    "text": item.get("text", ""),
                },
            )
        )
    scored.sort(key=lambda x: x[0], reverse=True)
    return [s[1] for s in scored[:top_k]]


def send_response(resp: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(resp) + "\n")
    sys.stdout.flush()


def handle_initialize(params: dict[str, Any]) -> dict[str, Any]:
    return {
        "protocolVersion": "2024-11-05",
        "capabilities": {"tools": {}},
        "serverInfo": {"name": "tikr-rag-mcp", "version": "1.0.0"},
    }


def handle_list_tools() -> dict[str, Any]:
    tools = [
        {
            "name": "get_repo_status",
            "description": "Return current phase, last indexed time, and high-level summary.",
            "inputSchema": {"type": "object", "properties": {}},
        },
        {
            "name": "search_knowledge",
            "description": "Semantic search over the repo (code + docs + vault).",
            "inputSchema": {
                "type": "object",
                "properties": {
                    "query": {"type": "string"},
                    "top_k": {"type": "integer", "default": 6},
                },
                "required": ["query"],
            },
        },
        {
            "name": "get_file",
            "description": "Return the full content of a file in the repo.",
            "inputSchema": {
                "type": "object",
                "properties": {"path": {"type": "string"}},
                "required": ["path"],
            },
        },
        {
            "name": "get_gaps",
            "description": "Return the documented frontend + AI Assistant gap analysis.",
            "inputSchema": {"type": "object", "properties": {}},
        },
        {
            "name": "get_ai_context_status",
            "description": (
                "Summarize what context the runtime AI Assistant currently has vs what exists in the repo."
            ),
            "inputSchema": {"type": "object", "properties": {}},
        },
        {
            "name": "refresh_index",
            "description": "Rebuild the RAG index (call after code changes).",
            "inputSchema": {"type": "object", "properties": {}},
        },
    ]
    return {"tools": tools}


def _text_content(text: str) -> dict[str, Any]:
    return {"content": [{"type": "text", "text": text}]}


def handle_call_tool(name: str, arguments: dict[str, Any]) -> dict[str, Any]:
    if name == "get_repo_status":
        idx = load_index()
        status = {
            "phase": "See docs/incremental-plan.md (Phase 5 in progress, 6-9 planned)",
            "last_indexed": idx.get("last_indexed"),
            "model": idx.get("model"),
            "total_chunks": len(idx.get("chunks") or []),
            "note": "Run refresh_index after any code changes.",
        }
        return _text_content(json.dumps(status, indent=2))

    if name == "search_knowledge":
        q = arguments.get("query", "")
        k = arguments.get("top_k", 6)
        results = search(q, top_k=int(k))
        return _text_content(json.dumps(results, indent=2))

    if name == "get_file":
        path = arguments.get("path", "")
        full = ROOT / path
        if full.exists() and full.is_file():
            return _text_content(read_text_safe(full))
        return _text_content(f"File not found: {path}")

    if name == "get_gaps":
        gaps = "See plan.md 'Gap Analysis' section or query search_knowledge('frontend and AI gaps')."
        return _text_content(gaps)

    if name == "get_ai_context_status":
        summary = (
            "Current runtime Assistant (Assistant.razor) only receives:\n"
            "- DashboardPriorities (requirements/deadlines)\n"
            "- Static system prompt\n"
            "- Current chat session\n\n"
            "It does NOT automatically retrieve Knowledge Vault entries or Document content.\n"
            "This is a documented gap (see plan.md)."
        )
        return _text_content(summary)

    if name == "refresh_index":
        idx = build_index(force=True)
        return _text_content(f"Index refreshed. Chunks: {len(idx['chunks'])}")

    return _text_content(f"Unknown tool: {name}")


def main() -> None:
    log("TIKR RAG MCP starting...")
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError as exc:
            log(f"Error handling message: {exc}")
            continue

        msg_id = msg.get("id")
        method = msg.get("method", "")
        params = msg.get("params") or {}

        try:
            if method == "initialize":
                result = handle_initialize(params)
            elif method == "tools/list":
                result = handle_list_tools()
            elif method == "tools/call":
                result = handle_call_tool(
                    params.get("name", ""),
                    params.get("arguments") or {},
                )
            else:
                send_response(
                    {
                        "jsonrpc": "2.0",
                        "id": msg_id,
                        "error": {"code": -32601, "message": f"Unsupported method: {method}"},
                    }
                )
                continue

            send_response({"jsonrpc": "2.0", "id": msg_id, "result": result})
        except Exception as exc:
            log(f"Error handling message: {exc}")
            send_response(
                {
                    "jsonrpc": "2.0",
                    "id": msg_id,
                    "error": {"code": -32000, "message": str(exc)},
                }
            )


if __name__ == "__main__":
    main()
