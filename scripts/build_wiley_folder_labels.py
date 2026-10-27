#!/usr/bin/env python3
"""Build a local JSONL of Wiley folder labels from the town library tree.

Labels come from the clerk's folder names, not from the chat model.
Payroll and employee files contribute path and filename only.
The JSONL belongs under models/ and must not be committed.

Usage:
  python3 scripts/build_wiley_folder_labels.py --root "/volume1/Town of Wiley Shared Documents" > models/wiley-folder-labels.jsonl
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree

SKIP_DIRS = {"#recycle", "@eaDir", ".DS_Store"}
SKIP_SUFFIXES = ("@synoeastream", "@eadir")
DOC_EXTENSIONS = {".pdf", ".docx", ".doc", ".rtf", ".txt", ".xlsx", ".xls"}
TEXT_EXTENSIONS = {".pdf", ".docx", ".txt", ".rtf"}
# Path and filename only. These trees hold payroll, checks, tax, and employee records.
NO_BODY_FOLDERS = {
    "EMPLOYEE INFO",
    "PAY ROLL  SPREADSHEETS",
    "PAYROLL TAXES AND FORMS",
    "WORKERS COMP",
    "IRA HEALTH INS",
    "INSURANCE BREAKDOWN",
    "CHECK LISTING",
    "MONTHLY BILLS",
    "SALES TAX-WILEY",
    "ACCOUNTANT-AMANDA BROWN",
    "Amanda-CPA- Audit List",
    "Abatement-assessment-certification",
}

# Top folder -> TIKR folder. Council meetings are split by filename below.
DIRECT_FOLDERS = {
    "ELECTION INFORMATION": "General",
    "BUDGET COMPARISON": "Budget / Finance",
    "SALES TAX-WILEY": "Budget / Finance",
    "ACCOUNTANT-AMANDA BROWN": "Budget / Finance",
    "CHECK LISTING": "Budget / Finance",
    "2026 AUDIT WSD-FARMER": "Budget / Finance",
    "Amanda-CPA- Audit List": "Budget / Finance",
    "MONTHLY BILLS": "Budget / Finance",
    "Abatement-assessment-certification": "Budget / Finance",
    "EMPLOYEE INFO": "Personnel / HR",
    "PAY ROLL  SPREADSHEETS": "Personnel / HR",
    "PAYROLL TAXES AND FORMS": "Personnel / HR",
    "WORKERS COMP": "Personnel / HR",
    "IRA HEALTH INS": "Personnel / HR",
    "INSURANCE BREAKDOWN": "Personnel / HR",
    "RESOLUTIONS&ORDINANCE": "Ordinances",
    "Town Code Book": "Ordinances",
    "FIRE DEPARTMENT MINUTES": "Minutes",
}

WORD_NS = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
SSN = re.compile(r"\b\d{3}-\d{2}-\d{4}\b")
LONG_DIGITS = re.compile(r"\b\d{9,}\b")


def folder_for(top: str, file_name: str) -> str | None:
    if top == "COUNCIL MEETINGS":
        low = file_name.lower()
        if low.endswith(".wma") or "@" in file_name:
            return None
        if "minutes" in low:
            return "Minutes"
        if "agenda" in low or "notice" in low:
            return "Agenda"
        if low.endswith((".docx", ".doc")):
            return "Agenda"
        return None
    return DIRECT_FOLDERS.get(top)


def clean_text(text: str, limit: int) -> str:
    text = SSN.sub(" ", text)
    text = LONG_DIGITS.sub(" ", text)
    text = " ".join(text.split())
    return text[:limit]


def docx_text(path: Path, limit: int) -> str:
    with zipfile.ZipFile(path) as archive:
        xml = archive.read("word/document.xml")
    root = ElementTree.fromstring(xml)
    parts = [node.text or "" for node in root.iter(WORD_NS + "t")]
    return clean_text(" ".join(parts), limit)


def pdf_text(path: Path, limit: int) -> str:
    result = subprocess.run(
        ["pdftotext", "-f", "1", "-l", "1", "-q", str(path), "-"],
        check=False,
        capture_output=True,
        timeout=25,
    )
    if result.returncode != 0:
        return ""
    return clean_text(result.stdout.decode("utf-8", errors="replace"), limit)


def plain_text(path: Path, limit: int) -> str:
    return clean_text(path.read_text(encoding="utf-8", errors="replace"), limit)


def excerpt(path: Path, top: str, limit: int) -> str:
    if top in NO_BODY_FOLDERS:
        return ""
    suffix = path.suffix.lower()
    if suffix not in TEXT_EXTENSIONS:
        return ""
    if path.stat().st_size > 15_000_000:
        return ""
    try:
        if suffix == ".pdf":
            return pdf_text(path, limit)
        if suffix == ".docx":
            return docx_text(path, limit)
        if suffix in {".txt", ".rtf"}:
            return plain_text(path, limit)
    except (
        OSError,
        KeyError,
        zipfile.BadZipFile,
        ElementTree.ParseError,
        subprocess.TimeoutExpired,
    ):
        return ""
    return ""


def iter_rows(root: Path, limit: int):
    for dirpath, dirnames, filenames in _walk(root):
        dirnames[:] = [
            name
            for name in dirnames
            if name not in SKIP_DIRS and not name.startswith("@")
        ]
        rel_dir = dirpath.relative_to(root)
        parts = rel_dir.parts
        if not parts:
            continue
        top = parts[0]
        if top not in DIRECT_FOLDERS and top != "COUNCIL MEETINGS":
            continue
        for name in filenames:
            if name.startswith(".") or name.lower().endswith(SKIP_SUFFIXES):
                continue
            path = dirpath / name
            if not path.is_file():
                continue
            if path.suffix.lower() not in DOC_EXTENSIONS:
                continue
            folder = folder_for(top, name)
            if folder is None:
                continue
            rel = path.relative_to(root).as_posix()
            yield {
                "path": rel,
                "fileName": name,
                "text": excerpt(path, top, limit),
                "folder": folder,
            }


def _walk(root: Path):
    import os

    for dirpath, dirnames, filenames in os.walk(root):
        yield Path(dirpath), dirnames, filenames


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--text-chars", type=int, default=600)
    parser.add_argument("--out", type=Path)
    args = parser.parse_args()
    if not args.root.is_dir():
        raise SystemExit(f"Library root not found: {args.root}")

    counts: dict[str, int] = {}
    written = 0
    destination = args.out.open("w", encoding="utf-8") if args.out else sys.stdout
    try:
        for row in iter_rows(args.root, args.text_chars):
            destination.write(json.dumps(row, ensure_ascii=False) + "\n")
            counts[row["folder"]] = counts.get(row["folder"], 0) + 1
            written += 1
    finally:
        if args.out:
            destination.close()
    print(f"wrote {written} rows", file=sys.stderr)
    for folder, count in sorted(counts.items()):
        print(f"  {count:5}  {folder}", file=sys.stderr)


if __name__ == "__main__":
    main()
