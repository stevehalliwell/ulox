from __future__ import annotations

import csv
from pathlib import Path
import sys

RESULTS_DIR = Path("BenchmarkDotNet.Artifacts/results")


def latest_joined_csv_files() -> list[Path]:
    files = sorted(
        RESULTS_DIR.glob("BenchmarkRun-joined-*-report.csv"),
        key=lambda p: p.stat().st_mtime,
        reverse=True,
    )
    if not files:
        raise SystemExit(f"No joined benchmark CSV reports found in {RESULTS_DIR}")
    return files


def parse_number(value: str) -> float | None:
    value = (value or "").strip()
    if not value or value in {"-", "NA"}:
        return None
    for suffix in (" μs", " ns", " B"):
        if value.endswith(suffix):
            value = value[: -len(suffix)]
            break
    value = value.replace(",", "")
    try:
        return float(value)
    except ValueError:
        return None


def load_rows(path: Path) -> list[dict]:
    with path.open("r", encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))


def build_key(row: dict) -> tuple[str, str]:
    return row["Type"], row["Method"]


def format_mean(value: float | None) -> str:
    if value is None:
        return "NA"
    return f"{value:,.3f} μs"


def format_stddev(value: float | None) -> str:
    if value is None:
        return "-"
    return f"{value:,.3f} μs"


def format_diff(current: float | None, previous: float | None) -> str:
    if current is None or previous is None or previous == 0:
        return "-"
    diff = current - previous
    diff_sign = "+" if diff > 0 else ""
    percent = (diff / previous) * 100
    percent_sign = "+" if percent > 0 else ""
    return f"{diff_sign}{diff:,.3f} μs ({percent_sign}{percent:.2f}%)"


def format_gen_sum(row: dict) -> str:
    total = sum(
        parse_number(row.get(name, "")) or 0.0 for name in ("Gen0", "Gen1", "Gen2")
    )
    if total == 0:
        return "-"
    return f"{total:,.4f}"


def format_allocated(row: dict) -> str:
    allocated = parse_number(row.get("Allocated", ""))
    if allocated is None:
        return "-"
    return f"{int(allocated):,} B"


def make_summary_rows(
    current_rows: list[dict], previous_rows: list[dict]
) -> tuple[list[dict[str, str]], int]:
    previous_by_key = {build_key(row): row for row in previous_rows}
    rows_with_mean = [
        row for row in current_rows if parse_number(row.get("Mean", "")) is not None
    ]
    summary_rows = []
    missing_count = 0
    for row in current_rows:
        key = build_key(row)
        previous_row = previous_by_key.get(key)
        current_mean = parse_number(row.get("Mean", ""))
        if current_mean is None:
            missing_count += 1
            continue
        previous_mean = (
            parse_number(previous_row.get("Mean", "")) if previous_row else None
        )
        current_stddev = parse_number(row.get("StdDev", ""))
        summary_rows.append(
            {
                "Name": f"{row['Type']}.{row['Method']}",
                "Mean": format_mean(current_mean),
                "StdDev": format_stddev(current_stddev),
                "Prev Mean dif": format_diff(current_mean, previous_mean),
                "Gen": format_gen_sum(row),
                "Allocated": format_allocated(row),
            }
        )
    summary_rows.sort(key=lambda row: row["Name"])
    return summary_rows, missing_count


def render_table(rows: list[dict[str, str]], headers: list[str]) -> str:
    right_aligned = {"Mean", "StdDev", "Prev Mean dif", "Gen", "Allocated"}
    widths = {
        header: max(len(header), *(len(row[header]) for row in rows))
        for header in headers
    }

    def format_cell(header: str, value: str) -> str:
        return (
            value.rjust(widths[header])
            if header in right_aligned
            else value.ljust(widths[header])
        )

    header_line = (
        "| " + " | ".join(format_cell(header, header) for header in headers) + " |"
    )
    divider = (
        "| "
        + " | ".join(
            (
                ("-" * (widths[header] - 1) + ":")
                if header in right_aligned
                else (":" + "-" * (widths[header] - 1))
            )
            for header in headers
        )
        + " |"
    )
    body = [
        "| " + " | ".join(format_cell(header, row[header]) for header in headers) + " |"
        for row in rows
    ]
    return "\n".join([header_line, divider, *body])


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    csv_files = latest_joined_csv_files()
    current_csv = csv_files[0]
    previous_csv = csv_files[1] if len(csv_files) > 1 else None

    current_rows = load_rows(current_csv)
    previous_rows = load_rows(previous_csv) if previous_csv else []
    summary_rows, missing_count = make_summary_rows(current_rows, previous_rows)

    output = []
    if missing_count:
        output.append(f"{missing_count} tests failed to report results.\n")
    output.append(
        render_table(
            summary_rows,
            ["Name", "Mean", "StdDev", "Prev Mean dif", "Gen", "Allocated"],
        )
    )
    output_text = "\n".join(output)

    summary_path = current_csv.with_name(
        current_csv.name.replace("-report.csv", "-simple-summary.md")
    )
    summary_path.write_text(output_text + "\n", encoding="utf-8")

    print(output_text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
