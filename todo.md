# TODO

## Compiler optimisation process

- Add/confirm tests around likely impacted compiler/desugar behavior before changing code.
- Do one todo item at a time; keep diff as small as possible.
- Only change compiler/desugar code unless item explicitly says otherwise.
- Do not optimise scanner or optimiser here.
- Preserve bytecode/runtime behaviour; expected output and existing tests must stay unchanged.
- Before each item, note baseline benchmark row(s) most relevant to that item.
- Make the change, then run ULox tests with `/skill:test-results` if runtime/compiler code changed.
- If tests fail, make no more than 2 implementation fix attempts, then stop and ask for help.
- If tests pass, run MODERATE benchmarks with `/skill:benchmark-results`.
- Compare benchmark results to baseline and add result/drift to that item's Notes.
- If benchmark does not improve beyond noise, ask whether to revert, keep, or stop.
- If implementation needs broader refactor than todo describes, stop and ask before changing code.
- When user says to keep an item, update status, update baseline if requested, commit, then ask before moving on.
- Do not attempt any item marked `SKIP`.

## Todo item template

Use this shape when adding new todo items.

## 1. Example item title
- Current: what exists now.
- Change: smallest specific change to try.
- Expected win: why this might help.
- Risk: what can break; include test coverage needed.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: add test/benchmark result or failure reason here.

## Benchmark baseline - MODERATE - 2026-07-04

Source: `BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2026-07-04-16-50-08-report.csv`

| Name                                           |             Mean |         StdDev |           Prev Mean dif |      Gen |   Allocated |
| :--------------------------------------------- | ---------------: | -------------: | ----------------------: | -------: | ----------: |
| CompileExecuteSplit.Execute                    |       216.100 μs |       6.170 μs |      -6.800 μs (-3.05%) |        - |       192 B |
| CompileExecuteSplitOptimiserOff.Execute        |       214.700 μs |       3.770 μs |      +0.100 μs (+0.05%) |        - |       192 B |
| Program.CompileVsExecute_All                   |    88,868.800 μs |   1,572.100 μs |  -2,557.800 μs (-2.80%) |        - |   136,528 B |
| Program.CompileVsExecute_CompileOnly           |   396,860.900 μs | 117,085.790 μs | -34,026.800 μs (-7.90%) | 125.0000 | 1,004,802 B |
| Program.CompileVsExecute_DeepCloneOnly         |    11,588.200 μs |     147.220 μs |    -395.000 μs (-3.30%) |        - |    53,512 B |
| Program.CompileVsExecute_NewEngineOnly         |     8,748.200 μs |     167.660 μs |    -136.000 μs (-1.53%) |        - |    38,592 B |
| Program.CompileVsExecute_TokeniseOnly          |    13,949.300 μs |     630.370 μs |    +356.700 μs (+2.62%) |        - |    48,672 B |
| Program.Conditional_If                         |     9,676.100 μs |     281.890 μs |    +239.800 μs (+2.54%) |        - |    38,648 B |
| Program.Dissasm_Script                         |    66,976.400 μs |   1,703.600 μs |  +1,314.200 μs (+2.00%) |        - |    96,549 B |
| Program.Looping_Loop                           |    34,136.200 μs |     517.580 μs |    -246.900 μs (-0.72%) |        - |    44,280 B |
| Program.ScriptVsNativeFunctional_NativeMethods |   103,081.200 μs |   2,160.870 μs |  -3,013.200 μs (-2.84%) |        - |   154,744 B |
| Program.ScriptVsNativeFunctional_UloxMethods   |   272,174.500 μs |  51,631.310 μs | -11,426.100 μs (-4.03%) |  15.6250 |   344,472 B |
| RunScriptThenUpdateMany.OptimisationsOff       | 6,790,137.600 μs |  82,993.320 μs | -10,470.600 μs (-0.15%) |        - |    72,692 B |
| RunScriptThenUpdateMany.OptimisationsOn        | 6,470,548.000 μs |  96,443.740 μs |  -4,254.100 μs (-0.07%) |        - |    78,833 B |
