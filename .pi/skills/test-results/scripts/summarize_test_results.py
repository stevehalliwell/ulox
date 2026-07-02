from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
import xml.etree.ElementTree as ET

TRX_NS = {"trx": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
DEFAULT_RESULTS_DIR = Path("TestResults")
DEFAULT_TRX_NAME = "results.trx"


def parse_trx(trx_path: Path) -> dict:
    root = ET.parse(trx_path).getroot()
    results = root.findall(".//trx:UnitTestResult", TRX_NS)
    outcomes = Counter((result.get("outcome") or "Unknown") for result in results)
    failed_tests = [
        result.get("testName")
        for result in results
        if (result.get("outcome") or "Unknown") == "Failed"
    ]
    times = root.find("trx:Times", TRX_NS)
    finish_time = times.get("finish") if times is not None else None

    total = len(results)
    passed = outcomes.get("Passed", 0)
    failed = outcomes.get("Failed", 0)

    return {
        "trx_path": str(trx_path),
        "generated_at_utc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "run_finished_at": finish_time,
        "run_finished_at_utc": iso_to_utc_z(finish_time) if finish_time else None,
        "total": total,
        "passed": passed,
        "failed": failed,
        "outcomes": dict(sorted(outcomes.items())),
        "failing_tests": failed_tests,
    }


def find_trx(path_arg: str | None) -> Path:
    if path_arg:
        path = Path(path_arg)
        if path.is_dir():
            path = path / DEFAULT_TRX_NAME
        return path
    return DEFAULT_RESULTS_DIR / DEFAULT_TRX_NAME


def iso_to_utc_z(value: str) -> str:
    return (
        datetime.fromisoformat(value)
        .astimezone(timezone.utc)
        .strftime("%Y-%m-%dT%H:%M:%SZ")
    )


def summary_path_for(trx_path: Path, run_finished_at: str | None) -> Path:
    if run_finished_at:
        stamp = (
            datetime.fromisoformat(run_finished_at)
            .astimezone(timezone.utc)
            .strftime("%Y%m%dT%H%M%SZ")
        )
    else:
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return trx_path.parent / f"previous-summary-{stamp}.json"


def print_summary(summary: dict) -> None:
    print(f"Total: {summary['total']}")
    print(f"Passed: {summary['passed']}")
    print(f"Failed: {summary['failed']}")
    print("Failing tests:")
    if summary["failing_tests"]:
        for test_name in summary["failing_tests"]:
            print(f"- {test_name}")
    else:
        print("- none")


def main() -> int:
    parser = argparse.ArgumentParser(description="Summarize dotnet TRX test results.")
    parser.add_argument(
        "path",
        nargs="?",
        help="TRX file path, or results directory containing results.trx",
    )
    args = parser.parse_args()

    trx_path = find_trx(args.path)
    if not trx_path.exists():
        raise SystemExit(f"TRX file not found: {trx_path}")

    summary = parse_trx(trx_path)
    summary_path = summary_path_for(trx_path, summary["run_finished_at"])
    summary["summary_path"] = str(summary_path)

    summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print_summary(summary)
    print(f"Summary JSON: {summary_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
