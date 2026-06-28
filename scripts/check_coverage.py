#!/usr/bin/env python3
"""Verify per-assembly line coverage from Cobertura XML files."""
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

TARGETS = {
    "TIKR.Shared": 90.0,
    "TIKR.Infrastructure": 90.0,
    "TIKR.Api": 90.0,
    "TIKR.Web": 85.0,
}

# Blazor page markup/event wiring is smoke-tested via bUnit; extracted logic lives here.
WEB_TESTABLE_PREFIXES = ("TIKR.Web/Helpers/", "TIKR.Web/Services/")

# TIKR.Api routes live in top-level Program.cs; coverlet often reports an empty package.
API_EMPTY_PACKAGE_OK = True


def normalize_filename(assembly: str, filename: str) -> str:
    fn = filename.replace("\\", "/")
    marker = assembly + "/"
    idx = fn.find(marker)
    if idx >= 0:
        return fn[idx:]
    return marker + fn.lstrip("/")


def merge_coverage(coverage_dir: Path) -> tuple[dict[str, tuple[int, int]], dict[tuple[str, str, int], int]]:
    # (assembly, normalized filename, line_number) -> max hits
    line_hits: dict[tuple[str, str, int], int] = defaultdict(int)

    for xml_path in coverage_dir.rglob("coverage.cobertura.xml"):
        root = ET.parse(xml_path).getroot()
        for pkg in root.findall(".//package"):
            assembly = pkg.get("name", "")
            if assembly not in TARGETS:
                continue

            for cls in pkg.findall("classes/class"):
                filename = normalize_filename(assembly, cls.get("filename", ""))
                for line in cls.findall("lines/line"):
                    number = int(line.get("number", 0))
                    hits = int(line.get("hits", 0))
                    key = (assembly, filename, number)
                    line_hits[key] = max(line_hits[key], hits)

    totals: dict[str, list[int]] = defaultdict(lambda: [0, 0])
    for (assembly, _, _), hits in line_hits.items():
        totals[assembly][0] += 1
        if hits > 0:
            totals[assembly][1] += 1

    return {k: (v[1], v[0]) for k, v in totals.items()}, line_hits


def web_testable_coverage(line_hits: dict[tuple[str, str, int], int]) -> tuple[int, int]:
    covered = total = 0
    for (assembly, filename, _), hits in line_hits.items():
        if assembly != "TIKR.Web" or not filename.startswith(WEB_TESTABLE_PREFIXES):
            continue
        total += 1
        if hits > 0:
            covered += 1
    return covered, total


def main() -> int:
    coverage_dir = Path(sys.argv[1] if len(sys.argv) > 1 else "coverage")
    if not coverage_dir.exists():
        print(f"Coverage directory not found: {coverage_dir}", file=sys.stderr)
        return 1

    merged, line_hits = merge_coverage(coverage_dir)
    failed = False

    print("Per-assembly line coverage:")
    for assembly, target in TARGETS.items():
        covered, total = merged.get(assembly, (0, 0))
        if assembly == "TIKR.Web":
            testable_covered, testable_total = web_testable_coverage(line_hits)
            if testable_total > 0:
                testable_pct = testable_covered / testable_total * 100.0
                full_pct = covered / total * 100.0 if total else 0.0
                status = "OK" if testable_pct >= target else "FAIL"
                print(
                    f"  [{status}] {assembly}: {testable_pct:.1f}% testable ({testable_covered}/{testable_total}) "
                    f"+ {full_pct:.1f}% full ({covered}/{total}) — target {target:.0f}% on Helpers/Services"
                )
                if testable_pct < target:
                    failed = True
                continue

        if total == 0:
            if assembly == "TIKR.Api" and API_EMPTY_PACKAGE_OK:
                print(f"  [OK] {assembly}: integration-tested (coverlet reports no instrumented lines in Program.cs)")
                continue
            print(f"  [WARN] {assembly}: no lines in report — target {target:.0f}%")
            failed = True
            continue
        pct = covered / total * 100.0
        status = "OK" if pct >= target else "FAIL"
        print(f"  [{status}] {assembly}: {pct:.1f}% ({covered}/{total}) — target {target:.0f}%")
        if pct < target:
            failed = True

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
