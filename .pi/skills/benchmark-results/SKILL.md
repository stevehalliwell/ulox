---
name: benchmark-results
description: Runs ULox benchmarks quietly, then prints compact markdown summary from newest joined BenchmarkDotNet CSV, with previous-run drift when available. Use whenever running or reporting ULox benchmark results.
---

# Benchmark Results

Use for all benchmark runs in this repo.
Runs silently, then runs a Python script to format results in a concise readable format for user and agents.

## Workflow

1. Run from repository root:

```bash
dotnet run --project ulox/ulox.core.bench/ulox.core.bench.csproj -c Release --no-restore -- --filter "*" --join >/dev/null
python .pi/skills/benchmark-results/scripts/show_latest_benchmark_results.py
```

2. Show script stdout to user.

## Notes

- BenchmarkDotNet writes joined reports under `BenchmarkDotNet.Artifacts/results`.
- Script reads newest `BenchmarkRun-joined-*-report.csv`.
- Writes matching `*-simple-summary.md` beside CSV.
