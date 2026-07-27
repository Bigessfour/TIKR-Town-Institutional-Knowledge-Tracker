#!/usr/bin/env python3
"""Rebuild .rag_index/index.json — run after code or doc changes.

Default: *incremental* (only re-embeds files whose mtime/size changed).
Full rebuild:  .venv/bin/python3 scripts/update_tikr_rag_index.py --full

Env:
  OLLAMA_HOST              default http://localhost:11434
  TIKR_RAG_EMBED_MODEL     default nomic-embed-text
  TIKR_RAG_EMBED_TIMEOUT   seconds per embed request (default 30)
  TIKR_RAG_EMBED_WORKERS   parallel embed workers (default 4)
  TIKR_RAG_MAX_FILE_BYTES  skip larger sources (default 400000)
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from tikr_rag_mcp import build_index, log  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Refresh TIKR code/docs RAG index")
    parser.add_argument(
        "--full",
        action="store_true",
        help="Re-embed every file (slow). Default is incremental.",
    )
    parser.add_argument(
        "--if-missing",
        action="store_true",
        help="Only build when no index exists (never force refresh).",
    )
    args = parser.parse_args()

    if args.if_missing:
        log("Building index only if missing...")
        idx = build_index(force=False, full=False)
    else:
        mode = "full" if args.full else "incremental"
        log(f"Rebuilding RAG index ({mode})...")
        idx = build_index(force=True, full=args.full)

    log(
        f"Done. mode={idx.get('mode')} files={idx.get('total_files', 0)} "
        f"embedded={idx.get('files_embedded', 0)} reused={idx.get('files_reused', 0)} "
        f"chunks={len(idx.get('chunks', []))} seconds={idx.get('build_seconds', '?')}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
