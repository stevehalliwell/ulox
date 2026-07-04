---
name: test-results
description: Runs ULox tests quietly, then prints compact summary from TRX results. Use whenever running or reporting ULox test results.
---

# Test Results

Use for all test runs in this repo.
Builds tests first with quiet output. If compile fails, reports that tests cannot run. Otherwise runs silently, then runs a Python script to format results in concise readable form directly in tool output.

## Workflow

1. Run from repository root:

```bash
dotnet build ulox/ulox.sln --nologo --verbosity quiet || { echo "Tests cannot be run: there is a compile error."; exit 1; }
dotnet test ulox/ulox.sln --no-build --logger "trx;LogFileName=results.trx" --results-directory ./TestResults >/dev/null
python .pi/skills/test-results/scripts/summarize_test_results.py
```

2. Do not repeat script stdout in chat. User can read tool output directly. Only add brief takeaway when needed.

## Notes

- Script reads `TestResults/results.trx` by default.
- Script output is intended for direct reading in tool output.
- Writes `TestResults/previous-summary-<utc>.json`.
- Optional arg can be TRX path or results dir.
