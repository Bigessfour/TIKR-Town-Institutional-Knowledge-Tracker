#!/usr/bin/env python3
"""Train the nine-folder classifier on this Mac and export it for TIKR.

Writes a scikit-learn joblib artifact plus a JSON linear model the API scores
with no Python runtime and no GPU. Town-document text stays on this machine:
commit only the synthetic fixture, not a model trained on Wiley records.

Usage:
  .venv/bin/python3 scripts/train_folder_classifier.py
  .venv/bin/python3 scripts/train_folder_classifier.py --data my-labels.jsonl --out models/folder-classifier.json
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DATA = ROOT / "scripts" / "folder-classifier-sample.jsonl"
DEFAULT_OUT = ROOT / "models" / "folder-classifier.json"
FIXTURE_OUT = (
    ROOT / "tests" / "TIKR.Infrastructure.Tests" / "Fixtures" / "folder-classifier.json"
)

# Held out of the sample file. The election manual must not land in Personnel / HR.
# Nine-way softmax spreads probability, so confidence is top score plus a gap
# over the runner-up. A vague note must stay under that bar.
HELD_OUT = [
    {
        "path": "ELECTIONS/2025 Special District Election Manual.pdf",
        "fileName": "2025 Special District Election Manual.pdf",
        "text": (
            "This manual explains the special district election, mail ballot "
            "procedures, and the canvass board. It is not a personnel file."
        ),
        "folder": "General",
        "forbid": "Personnel / HR",
    },
    {
        "path": "PERSONNEL/Stephen_Resume.pdf",
        "fileName": "Stephen_Resume.pdf",
        "text": "Curriculum vitae. Experience: deputy clerk. Employment history and payroll.",
        "folder": "Personnel / HR",
        "forbid": "General",
    },
]

UNCERTAIN = [
    {
        "path": "NOTES/hello.pdf",
        "fileName": "hello.pdf",
        "text": "hello world miscellaneous note about the weather",
    }
]


def document_text(row: dict) -> str:
    parts = [row.get("path") or "", row.get("fileName") or "", row.get("text") or ""]
    return "\n".join(part.strip() for part in parts if part and str(part).strip())


def load_rows(path: Path) -> list[dict]:
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        rows.append(json.loads(line))
    if len(rows) < 9:
        raise SystemExit(f"{path} needs at least one row for each folder")
    return rows


def train(rows: list[dict]):
    try:
        from sklearn.feature_extraction.text import TfidfVectorizer
        from sklearn.linear_model import LogisticRegression
        from sklearn.pipeline import Pipeline
    except ImportError as ex:
        raise SystemExit(
            "scikit-learn is not installed. Run: .venv/bin/python3 -m pip install 'scikit-learn>=1.5,<1.8'"
        ) from ex

    texts = [document_text(row) for row in rows]
    labels = [row["folder"] for row in rows]
    pipeline = Pipeline(
        [
            (
                "tfidf",
                TfidfVectorizer(
                    lowercase=True,
                    token_pattern=r"(?u)\b\w\w+\b",
                    ngram_range=(1, 1),
                    norm="l2",
                    use_idf=True,
                    smooth_idf=True,
                    sublinear_tf=False,
                ),
            ),
            (
                "clf",
                LogisticRegression(
                    C=4.0,
                    max_iter=500,
                    solver="lbfgs",
                ),
            ),
        ]
    )
    pipeline.fit(texts, labels)
    return pipeline


def export_payload(pipeline, threshold: float, min_margin: float) -> dict:
    vectorizer = pipeline.named_steps["tfidf"]
    classifier = pipeline.named_steps["clf"]
    vocabulary = [""] * len(vectorizer.vocabulary_)
    for term, index in vectorizer.vocabulary_.items():
        vocabulary[index] = term
    return {
        "version": 1,
        "labels": [str(label) for label in classifier.classes_],
        "vocabulary": vocabulary,
        "idf": [float(value) for value in vectorizer.idf_],
        "coefficients": [[float(value) for value in row] for row in classifier.coef_],
        "intercept": [float(value) for value in classifier.intercept_],
        "threshold": threshold,
        "minMargin": min_margin,
    }


def top_two(probabilities, labels):
    order = sorted(
        range(len(labels)), key=lambda index: probabilities[index], reverse=True
    )
    best = order[0]
    runner_up = order[1]
    return labels[best], float(probabilities[best]), float(probabilities[runner_up])


def is_confident(
    confidence: float, runner_up: float, threshold: float, min_margin: float
) -> bool:
    return confidence >= threshold and (confidence - runner_up) >= min_margin


def assert_held_out(pipeline, threshold: float, min_margin: float) -> None:
    labels = list(pipeline.named_steps["clf"].classes_)
    texts = [document_text(row) for row in HELD_OUT]
    for row, probs in zip(HELD_OUT, pipeline.predict_proba(texts), strict=True):
        folder, confidence, runner_up = top_two(probs, labels)
        if (
            folder != row["folder"]
            or folder == row["forbid"]
            or not is_confident(confidence, runner_up, threshold, min_margin)
        ):
            raise SystemExit(
                f"Held-out failed for {row['fileName']}: predicted {folder} "
                f"({confidence:.3f}, runner-up {runner_up:.3f}), expected {row['folder']}"
            )
        print(f"held-out {row['fileName']}: {folder} {confidence:.3f}")

    vague = [document_text(row) for row in UNCERTAIN]
    for row, probs in zip(UNCERTAIN, pipeline.predict_proba(vague), strict=True):
        folder, confidence, runner_up = top_two(probs, labels)
        if is_confident(confidence, runner_up, threshold, min_margin):
            raise SystemExit(
                f"Vague file {row['fileName']} looked confident: {folder} {confidence:.3f}"
            )
        print(f"uncertain {row['fileName']}: {folder} {confidence:.3f}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data", type=Path, default=DEFAULT_DATA)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--threshold", type=float, default=0.40)
    parser.add_argument("--min-margin", type=float, default=0.20)
    parser.add_argument(
        "--fixture",
        action="store_true",
        help="Also write the synthetic JSON fixture used by the .NET tests",
    )
    args = parser.parse_args()

    rows = load_rows(args.data)
    pipeline = train(rows)
    assert_held_out(pipeline, args.threshold, args.min_margin)
    payload = export_payload(pipeline, args.threshold, args.min_margin)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    joblib_path = args.out.with_suffix(".joblib")
    import joblib

    joblib.dump(pipeline, joblib_path)
    reloaded = joblib.load(joblib_path)
    assert_held_out(reloaded, args.threshold, args.min_margin)
    print(f"wrote {args.out}")
    print(f"wrote {joblib_path}")

    if args.fixture:
        FIXTURE_OUT.parent.mkdir(parents=True, exist_ok=True)
        FIXTURE_OUT.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
        print(f"wrote {FIXTURE_OUT}")


if __name__ == "__main__":
    try:
        main()
    except BrokenPipeError:
        sys.exit(0)
