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
# Per-request embed timeout (seconds). Keep short so one stuck Ollama call cannot hang 10+ minutes.
EMBED_TIMEOUT = float(os.environ.get("TIKR_RAG_EMBED_TIMEOUT", "30"))
# Max source file size (bytes) to index — skips giant dumps like repomix-output.md.
MAX_FILE_BYTES = int(os.environ.get("TIKR_RAG_MAX_FILE_BYTES", str(400_000)))
# Parallel Ollama embed workers (1 = serial). 4 is a good default for local Ollama.
EMBED_WORKERS = max(1, int(os.environ.get("TIKR_RAG_EMBED_WORKERS", "4")))

# Directory *names* pruned during walk (basename match only).
EXCLUDE_DIR_NAMES = {
    ".git",
    ".rag_index",
    ".venv",
    ".agents",  # Syncfusion skill dumps — huge, low value for app RAG
    ".local-data",
    "local-data",
    "__pycache__",
    "bin",
    "obj",
    "coverage",
    "node_modules",
    "TestResults",
    "installer",
    "deploy",
    "apm_modules",  # vendored skill mirrors; not app source
    ".grok",
}

# Path globs (relative to ROOT, forward slashes) that are never indexed.
EXCLUDE_FILE_GLOBS = [
    "repomix-output.md",
    "**/repomix-output.md",
    "**/*.Designer.cs",
    "**/Migrations/*Designer.cs",
    "**/package-lock.json",
    "**/*.min.js",
    "**/*.map",
]

INCLUDE_GLOBS = [
    "docs/**/*.md",
    "specs/**/*.md",
    "README.md",
    "AGENTS.md",
    "src/**/*.cs",
    "src/**/*.razor",
    "src/**/*.py",
    "scripts/**/*.py",
    "scripts/**/*.sh",
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


def rel_path_str(path: Path) -> str | None:
    try:
        return str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return None


def should_include(path: Path) -> bool:
    rel = rel_path_str(path)
    if not rel:
        return False
    if any(fnmatch.fnmatch(rel, g) for g in EXCLUDE_FILE_GLOBS):
        return False
    try:
        if path.stat().st_size > MAX_FILE_BYTES:
            return False
    except OSError:
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
    """Call Ollama embeddings API (bounded timeout so reindex cannot hang forever)."""
    url = f"{OLLAMA_HOST}/api/embeddings"
    try:
        resp = requests.post(
            url,
            json={"model": EMBED_MODEL, "prompt": text[:8000]},
            timeout=EMBED_TIMEOUT,
        )
        resp.raise_for_status()
        data = resp.json()
        return data.get("embedding") or []
    except Exception as exc:
        log(f"Embedding error: {exc}")
        return []


def file_fingerprint(path: Path) -> str:
    """Cheap change detector (mtime + size) — enough to skip re-embed of unchanged files."""
    try:
        st = path.stat()
        return f"{int(st.st_mtime_ns)}:{st.st_size}"
    except OSError:
        return "0:0"


def iter_source_files() -> list[Path]:
    files: list[Path] = []
    for root, dirs, names in os.walk(ROOT):
        # Prune excluded directory basenames in-place (os.walk convention).
        dirs[:] = [d for d in dirs if d not in EXCLUDE_DIR_NAMES]
        for fname in names:
            fpath = Path(root) / fname
            if should_include(fpath):
                files.append(fpath)
    files.sort(key=lambda p: str(p))
    return files


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


def build_index(force: bool = False, full: bool = False) -> dict[str, Any]:
    """Walk the repo and (re)build the vector index.

    * force=False and index exists → return existing (no work).
    * force=True, full=False → **incremental**: re-embed only new/changed files (default for MCP).
    * force=True, full=True → wipe and re-embed everything (slow; use CLI --full only).
    """
    existing = load_index()
    if not force and existing.get("chunks"):
        log(
            f"Index already present ({len(existing.get('chunks') or [])} chunks, "
            f"last={existing.get('last_indexed')}); use force=True to refresh."
        )
        return existing

    t0 = time.time()
    source_files = iter_source_files()
    log(f"Scanning {len(source_files)} source files (full={full})...")

    # Previous chunks keyed by path for incremental reuse.
    prev_by_path: dict[str, list[dict[str, Any]]] = {}
    prev_fp: dict[str, str] = dict(existing.get("file_fingerprints") or {})
    # Old indexes had no fingerprints — one-time migrate: reuse existing vectors, only embed new paths.
    legacy_index = not full and bool(existing.get("chunks")) and not prev_fp
    if legacy_index:
        log("Legacy index detected (no fingerprints). Migrating: reuse existing embeddings, embed new files only.")
    if not full:
        for item in existing.get("chunks") or []:
            p = item.get("path") or ""
            if not p:
                continue
            prev_by_path.setdefault(p, []).append(item)

    new_chunks: list[dict[str, Any]] = []
    new_fp: dict[str, str] = {}
    files_processed = 0
    files_reused = 0
    files_embedded = 0
    embed_calls = 0

    from concurrent.futures import ThreadPoolExecutor, as_completed

    for fpath in source_files:
        rel = rel_path_str(fpath) or ""
        fp = file_fingerprint(fpath)
        new_fp[rel] = fp

        has_prev = rel in prev_by_path and any((c.get("embedding") or []) for c in prev_by_path[rel])
        unchanged = prev_fp.get(rel) == fp
        # Reuse prior embeddings when file unchanged, or when migrating a legacy index entry.
        if not full and has_prev and (unchanged or legacy_index):
            new_chunks.extend(prev_by_path[rel])
            files_reused += 1
            files_processed += 1
            continue

        text = read_text_safe(fpath)
        if not text.strip():
            files_processed += 1
            continue

        parts = chunk_text(text)
        # Embed chunks in parallel (bounded workers).
        embeddings: list[list[float]] = [[] for _ in parts]
        if parts:
            with ThreadPoolExecutor(max_workers=EMBED_WORKERS) as pool:
                futures = {pool.submit(get_embedding, part): i for i, part in enumerate(parts)}
                for fut in as_completed(futures):
                    i = futures[fut]
                    try:
                        embeddings[i] = fut.result() or []
                    except Exception as exc:  # pragma: no cover
                        log(f"Embed worker error {rel}#{i}: {exc}")
                        embeddings[i] = []
                    embed_calls += 1

        for i, part in enumerate(parts):
            new_chunks.append(
                {
                    "path": rel,
                    "chunk_id": i,
                    "text": part[:2000],
                    "embedding": embeddings[i],
                }
            )
        files_embedded += 1
        files_processed += 1
        if files_embedded % 10 == 0 or files_processed == len(source_files):
            elapsed = time.time() - t0
            log(
                f"Progress files={files_processed}/{len(source_files)} "
                f"embedded={files_embedded} reused={files_reused} "
                f"chunks={len(new_chunks)} embed_calls={embed_calls} "
                f"elapsed={elapsed:.1f}s"
            )

    index = {
        "chunks": new_chunks,
        "last_indexed": time.strftime("%Y-%m-%d %H:%M:%S"),
        "model": EMBED_MODEL,
        "total_files": files_processed,
        "files_embedded": files_embedded,
        "files_reused": files_reused,
        "file_fingerprints": new_fp,
        "build_seconds": round(time.time() - t0, 1),
        "mode": "full" if full else "incremental",
    }
    save_index(index)
    log(
        f"Done mode={index['mode']} files={files_processed} "
        f"chunks={len(new_chunks)} embedded={files_embedded} reused={files_reused} "
        f"in {index['build_seconds']}s"
    )
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
            "description": (
                "Refresh the code/docs RAG index after code changes. "
                "Default is *incremental* (only re-embeds changed files). "
                "Set full=true only for a complete rebuild (slow)."
            ),
            "inputSchema": {
                "type": "object",
                "properties": {
                    "full": {
                        "type": "boolean",
                        "description": "If true, re-embed every file. Default false (incremental).",
                        "default": False,
                    }
                },
            },
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
        full = bool(arguments.get("full", False))
        idx = build_index(force=True, full=full)
        return _text_content(
            f"Index refreshed ({idx.get('mode', '?')}). "
            f"Files: {idx.get('total_files', 0)} "
            f"(embedded={idx.get('files_embedded', 0)}, reused={idx.get('files_reused', 0)}), "
            f"chunks: {len(idx.get('chunks') or [])}, "
            f"build_seconds: {idx.get('build_seconds', '?')}"
        )

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

        # JSON-RPC notifications have no id — never reply (Cursor sends
        # notifications/initialized after initialize; answering breaks the client).
        if "id" not in msg or method.startswith("notifications/"):
            continue

        try:
            if method == "initialize":
                result = handle_initialize(params)
            elif method == "ping":
                result = {}
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
