#!/usr/bin/env python3
"""Generate docs/functions-inventory.md and tests/e2e/functions-inventory.json."""

from __future__ import annotations

import json
import re
import sys
from dataclasses import dataclass, field, asdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src" / "TIKR.Web"
OVERRIDES_PATH = Path(__file__).with_name("inventory-overrides.json")
MANUAL_PATH = Path(__file__).with_name("inventory-manual-items.json")
OUT_JSON = ROOT / "tests" / "e2e" / "functions-inventory.json"
OUT_MD = ROOT / "docs" / "functions-inventory.md"

PAGE_ROUTE_RE = re.compile(r'@page\s+"([^"]+)"')
SF_BUTTON_CONTENT_RE = re.compile(
    r"<SfButton\b([^>]*?)>",
    re.IGNORECASE | re.DOTALL,
)
CONTENT_ATTR_RE = re.compile(r'\bContent="([^"]*)"')
ONCLICK_RE = re.compile(r'\bOnClick="([^"]+)"')
NAVLINK_RE = re.compile(r'<NavLink\s+href="([^"]+)"[^>]*>([^<]+)</NavLink>')
HTML_BUTTON_RE = re.compile(
    r'<button\b[^>]*\btype="button"[^>]*>\s*([^<@]{1,80})\s*</button>',
    re.IGNORECASE,
)
SF_TEXTBOX_RE = re.compile(r"<SfTextBox\b[^>]*>")
SF_DROP_DOWN_RE = re.compile(r"<SfDropDownList\b[^>]*>")
SF_CHECKBOX_RE = re.compile(r"<SfCheckBox\b[^>]*>")
SF_UPLOADER_RE = re.compile(r"<SfUploader\b[^>]*>")
SF_TAB_RE = re.compile(r'<SfTab\b[^>]*\bHeader="@\(?"([^"]+)"\)?"')
SF_TAB_HEADER_RE = re.compile(r'<SfTab\b[^>]*\bHeader="([^"]+)"')
INPUT_TYPE_RE = re.compile(r'<input\b[^>]*\btype="([^"]+)"[^>]*>', re.IGNORECASE)
ROLE_BUTTON_RE = re.compile(
    r'<SfButton[^>]*aria-label="([^"]+)"',
    re.IGNORECASE,
)


def slug(s: str) -> str:
    s = s.lower().strip()
    s = re.sub(r"[^a-z0-9]+", "-", s)
    return s.strip("-")[:64] or "control"


@dataclass
class InventoryItem:
    id: str
    page: str
    route: str
    label: str
    type: str
    fileLine: str
    intendedBehavior: str
    dependencies: list[str] = field(default_factory=list)
    permissions: list[str] = field(default_factory=list)
    expectedResult: str = ""
    testId: str = ""
    smokeSafe: bool = True
    writesData: bool = False
    knownFailure: str | None = None
    playwright: dict = field(default_factory=dict)

    def finalize(self, overrides: dict) -> None:
        if not self.testId:
            self.testId = self.id
        kf = overrides.get("knownFailures", {})
        for pattern, rb in kf.items():
            if pattern.endswith("*"):
                if self.id.startswith(pattern[:-1]):
                    self.knownFailure = rb
            elif self.id == pattern:
                self.knownFailure = rb
        patches = overrides.get("intendedBehaviorPatches", {})
        for key, patch in patches.items():
            if self.id == key or self.id.startswith(key):
                self.intendedBehavior = patch.get("intendedBehavior", self.intendedBehavior)
                self.expectedResult = patch.get("expectedResult", self.expectedResult)
                self.dependencies = patch.get("dependencies", self.dependencies)
        exclude = [p.lower() for p in overrides.get("smokeExcludePatterns", [])]
        label_l = self.label.lower()
        if self.writesData or any(p in label_l or p in self.id for p in exclude):
            self.smokeSafe = False
        if "call steve" in label_l and not self.knownFailure:
            self.knownFailure = "RB-002"
        if "try again" in label_l and self.page == "layout":
            self.knownFailure = "RB-004"
        if label_l == "ai scan":
            self.knownFailure = "RB-001"


def route_for_file(rel: str, routes: dict[str, str]) -> tuple[str, str]:
    if rel in routes:
        return routes[rel], routes[rel]
    if "Layout/" in rel or "Shared/" in rel:
        return "layout", "*"
    return "unknown", "/"


def infer_behavior(label: str, onclick: str | None, ctrl_type: str) -> tuple[str, str, list[str], bool]:
    deps: list[str] = []
    writes = False
    onclick_l = (onclick or "").lower()
    label_l = label.lower()

    if "navigate" in onclick_l or "navigation" in onclick_l:
        intended = f"Navigate via Blazor handler ({onclick or 'navigation'})."
        expected = "URL changes or target page loads."
    elif "callsteve" in onclick_l:
        intended = "Contact support (intended: real Steve contact)."
        expected = "Support contact surfaced (not toast-only)."
        deps = []
    elif "toast" in onclick_l:
        intended = "Show clerk toast feedback."
        expected = "Toast message visible."
    elif ctrl_type == "link-nav":
        intended = "Sidebar or in-app navigation."
        expected = "Target route loads with expected heading."
    elif ctrl_type == "file-upload":
        intended = "Upload file to document library via API."
        expected = "File appears in grid after upload."
        deps = ["API", "NAS storage path"]
        writes = True
    elif "delete" in label_l or "delete" in onclick_l:
        intended = "Delete or request delete confirmation."
        expected = "Record removed or confirm dialog shown."
        writes = True
    elif "save" in label_l or "create" in label_l or "add" in label_l:
        intended = f"Persist data via API ({label})."
        expected = "Data saved and UI reflects change."
        deps = ["API"]
        writes = True
    elif "export" in label_l or "print" in label_l or "pdf" in label_l or "packet" in label_l:
        intended = f"Generate or export document ({label})."
        expected = "Download/print dialog or PDF bytes returned."
        deps = ["SYNCFUSION_LICENSE_KEY", "API"]
    elif "assistant" in label_l or "chat" in label_l or ctrl_type == "assistant-chat":
        intended = "Send prompt to hybrid AI (Ollama/Grok) with RAG."
        expected = "Progress visible; assistant message returned."
        deps = ["Ollama", "API"]
        writes = True
    elif "semantic" in label_l or "search" in label_l:
        intended = "Search documents or knowledge."
        expected = "Grid or results update."
        deps = ["Ollama embeddings optional"]
    elif ctrl_type in ("input", "dropdown", "checkbox"):
        intended = f"Edit field: {label}."
        expected = "Value binds and persists on save."
        writes = True
    elif ctrl_type == "help":
        intended = "Show tooltip/page help (no navigation)."
        expected = "Help tooltip visible on hover/focus."
    else:
        intended = f"Clerk action: {label}."
        expected = "Control responds without unhandled error."

    if "syncfusion" in label_l or "license" in label_l:
        deps.append("SYNCFUSION_LICENSE_KEY")
    if "ollama" in label_l or "helper" in label_l:
        deps.append("Ollama")
    return intended, expected, sorted(set(deps)), writes


def scan_razor(path: Path, routes: dict[str, str], overrides: dict) -> list[InventoryItem]:
    rel = str(path.relative_to(ROOT)).replace("\\", "/")
    page_name, route = route_for_file(rel, routes)
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    items: list[InventoryItem] = []
    seen_ids: set[str] = set()

    def add(
        line_no: int,
        label: str,
        ctrl_type: str,
        onclick: str | None = None,
        extra_id: str = "",
    ) -> None:
        label = label.strip() or "(unlabeled)"
        if not label or label.startswith("@"):
            return
        base = f"inv-{slug(page_name)}-{slug(label)}{('-' + slug(extra_id)) if extra_id else ''}"
        item_id = base
        n = 2
        while item_id in seen_ids:
            item_id = f"{base}-{n}"
            n += 1
        seen_ids.add(item_id)
        intended, expected, deps, writes = infer_behavior(label, onclick, ctrl_type)
        pw: dict = {"strategy": "role", "role": "button", "name": label}
        if ctrl_type == "link-nav":
            pw = {"strategy": "link", "name": label}
        elif ctrl_type == "tab":
            pw = {"strategy": "tab", "name": label}
        elif ctrl_type == "help":
            pw = {"strategy": "help"}
        elif ctrl_type == "route":
            pw = {"strategy": "goto", "path": route}
        item = InventoryItem(
            id=item_id,
            page=page_name,
            route=route,
            label=label,
            type=ctrl_type,
            fileLine=f"{rel}:{line_no}",
            intendedBehavior=intended,
            dependencies=deps,
            expectedResult=expected,
            writesData=writes,
            playwright=pw,
        )
        item.finalize(overrides)
        items.append(item)

    if rel.startswith("src/TIKR.Web/Components/Pages/"):
        m = PAGE_ROUTE_RE.search(text)
        if m:
            add(1, f"Route {m.group(1)}", "route", extra_id="page-route")
            items[-1].route = m.group(1)
            items[-1].playwright = {"strategy": "goto", "path": m.group(1)}

    for i, line in enumerate(lines, start=1):
        for m in NAVLINK_RE.finditer(line):
            add(i, m.group(2).strip(), "link-nav", extra_id=m.group(1))

    for i, line in enumerate(lines, start=1):
        if "<SfButton" not in line and "SfButton" not in line:
            continue
        # multi-line SfButton: grab until closing >
        start = i - 1
        chunk = line
        while ">" not in chunk and start + 1 < len(lines):
            start += 1
            chunk += " " + lines[start]
        content_m = CONTENT_ATTR_RE.search(chunk)
        onclick_m = ONCLICK_RE.search(chunk)
        aria_m = ROLE_BUTTON_RE.search(chunk)
        label = ""
        if content_m:
            label = content_m.group(1)
        elif aria_m:
            label = aria_m.group(1)
        else:
            inner = re.search(r">([^<]+)<", chunk)
            if inner:
                label = inner.group(1).strip()
        if label:
            add(i, label, "button", onclick_m.group(1) if onclick_m else None)

    for i, line in enumerate(lines, start=1):
        if "<button" not in line.lower():
            continue
        start = i - 1
        chunk = line
        while "</button>" not in chunk.lower() and start + 1 < len(lines):
            start += 1
            chunk += " " + lines[start]
        end_tag = chunk.lower().rfind("</button>")
        if end_tag > 0:
            gt = chunk.rfind(">", 0, end_tag)
            label = chunk[gt + 1 : end_tag].strip() if gt >= 0 else ""
            if label and "@" not in label:
                add(i, label, "button")

    for i, line in enumerate(lines, start=1):
        if "<SfTextBox" in line:
            add(i, "Text field", "input")
        if "<SfDropDownList" in line:
            add(i, "Dropdown", "dropdown")
        if "<SfCheckBox" in line:
            add(i, "Checkbox", "checkbox")
        if "<SfUploader" in line:
            add(i, "File upload", "file-upload")

    for i, line in enumerate(lines, start=1):
        m = SF_TAB_HEADER_RE.search(line) or SF_TAB_RE.search(line)
        if m:
            add(i, m.group(1), "tab")

    if "Assistant.razor" in rel and "SfAIAssistView" in text:
        line_no = next((i for i, l in enumerate(lines, 1) if "SfAIAssistView" in l), 1)
        item = InventoryItem(
            id="inv-assistant-chat-stream",
            page="assistant",
            route="/assistant",
            label="Assistant chat (SfAIAssistView)",
            type="assistant-chat",
            fileLine=f"{rel}:{line_no}",
            intendedBehavior="Stream tokens with immediate progress; RAG context prepended.",
            dependencies=["Ollama", "API", "SYNCFUSION_LICENSE_KEY"],
            expectedResult="User sees thinking/progress immediately; full answer streams or appears.",
            testId="inv-assistant-chat-stream",
            writesData=True,
            smokeSafe=False,
            playwright={"strategy": "assistant-chat"},
        )
        item.finalize(overrides)
        if item.id not in seen_ids:
            seen_ids.add(item.id)
            items.append(item)

    if "Vault.razor" in rel and not any(i.id == "inv-vault-add-howto" for i in items):
        item = InventoryItem(
            id="inv-vault-add-howto",
            page="vault",
            route="/vault",
            label="Add how-to / tribal entry (missing control)",
            type="button",
            fileLine="src/TIKR.Web/Components/Pages/Vault.razor:—",
            intendedBehavior="Create new vault how-to entry from empty grid.",
            dependencies=["API"],
            expectedResult="New entry dialog or inline editor opens.",
            testId="inv-vault-add-howto",
            writesData=True,
            smokeSafe=False,
            playwright={"strategy": "missing-control"},
        )
        item.finalize(overrides)
        items.append(item)

    return items


def collect_routes() -> dict[str, str]:
    routes: dict[str, str] = {}
    for path in sorted((WEB / "Components").rglob("*.razor")):
        rel = str(path.relative_to(ROOT)).replace("\\", "/")
        m = PAGE_ROUTE_RE.search(path.read_text(encoding="utf-8", errors="replace"))
        if m:
            routes[rel] = m.group(1)
    return routes


def markdown_report(meta: dict, items: list[dict]) -> str:
    lines = [
        "# TIKR functions inventory (user-facing controls)",
        "",
        f"**Generated:** {meta['generatedAt']}  ",
        f"**Source:** `scripts/generate-functions-inventory.py`  ",
        f"**Machine-readable:** [tests/e2e/functions-inventory.json](../tests/e2e/functions-inventory.json)  ",
        f"**UX audit baseline:** [tikr-ux-evaluation.md](./tikr-ux-evaluation.md)  ",
        f"**Production walkthrough (main e1c5399):** manual items in `scripts/inventory-manual-items.json`",
        "",
        "## Summary",
        "",
        f"| Metric | Count |",
        f"|--------|------:|",
        f"| Routes / pages | {meta['pageCount']} |",
        f"| Buttons | {meta['buttonCount']} |",
        f"| Links / nav | {meta['linkCount']} |",
        f"| Inputs / fields | {meta['fieldCount']} |",
        f"| **Total items** | **{meta['itemCount']}** |",
        f"| Known failures (release blockers) | {meta['knownFailureCount']} |",
        f"| Smoke-safe (read-only live) | {meta['smokeSafeCount']} |",
        "",
        "## Inventory",
        "",
        "| testId | page | label | type | file:line | deps | expected | blocker |",
        "|--------|------|-------|------|-----------|------|----------|---------|",
    ]
    for it in sorted(items, key=lambda x: (x["page"], x["testId"])):
        deps = ", ".join(it.get("dependencies") or []) or "—"
        blocker = it.get("knownFailure") or "—"
        exp = (it.get("expectedResult") or "")[:60].replace("|", "/")
        lines.append(
            f"| `{it['testId']}` | {it['page']} | {it['label'][:40]} | {it['type']} | {it['fileLine']} | {deps} | {exp} | {blocker} |"
        )
    lines.extend(
        [
            "",
            "## Regenerate",
            "",
            "```bash",
            "python3 scripts/generate-functions-inventory.py",
            "```",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    overrides = json.loads(OVERRIDES_PATH.read_text(encoding="utf-8"))
    routes = collect_routes()
    all_items: list[InventoryItem] = []

    razor_roots = [
        WEB / "Components" / "Pages",
        WEB / "Components" / "Layout",
        WEB / "Components" / "Shared",
    ]
    for root in razor_roots:
        if not root.exists():
            continue
        for path in sorted(root.rglob("*.razor")):
            all_items.extend(scan_razor(path, routes, overrides))

    manual_path = MANUAL_PATH
    if manual_path.is_file():
        manual = json.loads(manual_path.read_text(encoding="utf-8")).get("items", [])
        seen = {x.id for x in all_items}
        for raw in manual:
            item = InventoryItem(
                id=raw["id"],
                page=raw["page"],
                route=raw["route"],
                label=raw["label"],
                type=raw["type"],
                fileLine=raw["fileLine"],
                intendedBehavior=raw["intendedBehavior"],
                dependencies=raw.get("dependencies", []),
                permissions=raw.get("permissions", []),
                expectedResult=raw.get("expectedResult", ""),
                testId=raw.get("testId", raw["id"]),
                smokeSafe=raw.get("smokeSafe", True),
                writesData=raw.get("writesData", False),
                knownFailure=raw.get("knownFailure"),
                playwright=raw.get("playwright", {}),
            )
            item.finalize(overrides)
            if item.id not in seen:
                all_items.append(item)
                seen.add(item.id)

    items_dict = [asdict(x) for x in all_items]
    pages = {i["route"] for i in items_dict if i["type"] == "route"}
    meta = {
        "generatedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "itemCount": len(items_dict),
        "pageCount": len(pages),
        "buttonCount": sum(1 for i in items_dict if i["type"] == "button"),
        "linkCount": sum(1 for i in items_dict if i["type"] in ("link-nav", "route")),
        "fieldCount": sum(
            1 for i in items_dict if i["type"] in ("input", "dropdown", "checkbox", "file-upload")
        ),
        "knownFailureCount": sum(1 for i in items_dict if i.get("knownFailure")),
        "smokeSafeCount": sum(1 for i in items_dict if i.get("smokeSafe")),
    }

    payload = {"meta": meta, "items": items_dict}
    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    OUT_MD.write_text(markdown_report(meta, items_dict), encoding="utf-8")
    print(f"Wrote {len(items_dict)} items -> {OUT_JSON.relative_to(ROOT)}")
    print(f"Wrote {OUT_MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())