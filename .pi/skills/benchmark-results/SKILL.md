---
name: benchmark-results
description: Runs ULox benchmarks quietly, then prints compact markdown summary from newest joined BenchmarkDotNet CSV, with previous-run drift when available. Use whenever running or reporting ULox benchmark results.
benchmark_thoroughness:
  default: MODERATE
  categories:
    - name: LITE
      intensity: low
      includes: core benchmarks plus compile-only optimiser pair
    - name: MODERATE
      intensity: medium
      includes: core benchmarks plus RunScriptThenUpdateMany, excludes compile-only optimiser pair and longest full-run cases
    - name: ALL
      intensity: high
      includes: every benchmark, including compile-only optimiser pair and longest full-run cases
---

# Benchmark Results

Use for all benchmark runs in this repo.
Default to `MODERATE` category. Run `ALL` only when user explicitly asks.
Runs silently, then runs a Python script to format results in concise readable form directly in tool output.

## Workflow

1. Run from repository root.

Default `MODERATE` run:

```bash
dotnet run --project ulox/ulox.core.bench/ulox.core.bench.csproj -c Release --no-restore -- --anyCategories=MODERATE --join >/dev/null
python .pi/skills/benchmark-results/scripts/show_latest_benchmark_results.py
```

Optional quicker `LITE` run:

```bash
dotnet run --project ulox/ulox.core.bench/ulox.core.bench.csproj -c Release --no-restore -- --anyCategories=LITE --join >/dev/null
python .pi/skills/benchmark-results/scripts/show_latest_benchmark_results.py
```

Explicit full `ALL` run:

```bash
dotnet run --project ulox/ulox.core.bench/ulox.core.bench.csproj -c Release --no-restore -- --anyCategories=ALL --join >/dev/null
python .pi/skills/benchmark-results/scripts/show_latest_benchmark_results.py
```

2. Do not repeat script stdout in chat. User can read tool output directly. Only add brief takeaway when needed.

## Notes

- `LITE` runs core benchmarks plus `Program.BytecodeOptimiser_CompileOnly_OptimisationsOff` and `Program.BytecodeOptimiser_CompileOnly_OptimisationsOn`.
- `MODERATE` runs core benchmarks plus `RunScriptThenUpdateMany.OptimisationsOff` and `RunScriptThenUpdateMany.OptimisationsOn`, but excludes compile-only optimiser pair.
- `ALL` runs every benchmark, including compile-only optimiser pair and `BytecodeOptimiserFullRun.OptimisationsOff` / `BytecodeOptimiserFullRun.OptimisationsOn`.
- BenchmarkDotNet writes joined reports under `BenchmarkDotNet.Artifacts/results`.
- Script reads newest `BenchmarkRun-joined-*-report.csv`.
- Summary keeps full benchmark names; no shortening.
- Writes matching `*-simple-summary.md` beside CSV.
