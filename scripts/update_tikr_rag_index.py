#!/usr/bin/env python3
"""Rebuild .rag_index/index.json — run after code or doc changes."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from tikr_rag_mcp import build_index, log  # noqa: E402


def main() -> int:
    log("Rebuilding RAG index (force=True)...")
    idx = build_index(force=True)
    log(f"Done. Files: {idx.get('total_files', 0)}, chunks: {len(idx.get('chunks', []))}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
