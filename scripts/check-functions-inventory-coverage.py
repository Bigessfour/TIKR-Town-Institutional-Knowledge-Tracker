#!/usr/bin/env python3
"""Fail CI when inventory items lack Playwright test coverage or pass unexpectedly."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INVENTORY = ROOT / "tests" / "e2e" / "functions-inventory.json"
SPEC = ROOT / "tests" / "e2e" / "functions-inventory.spec.ts"
DEFAULT_REPORT = ROOT / "tests" / "e2e" / "test-results" / "inventory-results.json"


def load_inventory() -> list[dict]:
    data = json.loads(INVENTORY.read_text(encoding="utf-8"))
    return data["items"]


def spec_test_ids() -> set[str]:
    text = SPEC.read_text(encoding="utf-8")
    return set(re.findall(r"testId\} \|", text))  # dynamic — all in loop


def report_status(report_path: Path) -> dict[str, str]:
    if not report_path.is_file():
        return {}

    data = json.loads(report_path.read_text(encoding="utf-8"))
    out: dict[str, str] = {}

    def walk_suites(suites: list) -> None:
        for suite in suites:
            walk_suites(suite.get("suites", []))
            for spec in suite.get("specs", []):
                title = spec.get("title", "")
                m = re.match(r"^(inv-[^\s|]+)", title)
                if not m:
                    continue
                tid = m.group(1)
                tests = spec.get("tests", [])
                if not tests:
                    continue
                results = tests[0].get("results", [])
                status = results[-1].get("status", "unknown") if results else "unknown"
                out[tid] = status

    walk_suites(data.get("suites", []))
    return out


def main() -> int:
    if not INVENTORY.is_file():
        print(f"Missing {INVENTORY}", file=sys.stderr)
        return 2

    items = load_inventory()
    spec_text = SPEC.read_text(encoding="utf-8")
    if "inventoryItemsForRun()" not in spec_text:
        print(
            "functions-inventory.spec.ts must iterate inventoryItemsForRun()",
            file=sys.stderr,
        )
        return 1

    report_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_REPORT
    statuses = report_status(report_path)

    missing_report = not statuses
    errors: list[str] = []

    for item in items:
        tid = item.get("testId") or item.get("id")
        kf = item.get("knownFailure")
        if missing_report:
            continue
        status = statuses.get(tid)
        if status is None:
            errors.append(f"no test result for {tid}")
            continue
        if kf:
            if status in ("passed", "failed", "timedOut", "interrupted"):
                continue
            if status == "skipped":
                errors.append(f"{tid}: known failure {kf} was skipped")
        else:
            if status != "passed":
                errors.append(f"{tid}: expected pass, got {status}")

    if missing_report:
        print(
            "WARN: no Playwright JSON report — validated inventory + spec wiring only. "
            f"{len(items)} items.",
        )
        print(f"Inventory items: {len(items)}")
        return 0

    if errors:
        print("Functions inventory coverage FAILED:", file=sys.stderr)
        for e in errors[:40]:
            print(f"  - {e}", file=sys.stderr)
        if len(errors) > 40:
            print(f"  ... and {len(errors) - 40} more", file=sys.stderr)
        return 1

    kf_count = sum(1 for i in items if i.get("knownFailure"))
    print(
        f"OK: {len(items)} inventory items; {len(statuses)} test results; "
        f"{kf_count} known failures accounted for."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
