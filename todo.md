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
- When moving to a new item, regardless of keep or revert, update the todo and commit that change.

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

## Optimiser wall-clock TODOs

Goal: minimise optimiser-only wall clock time first. Program behaviour and tests must pass. If only optimiser tests fail because bytecode/report counts changed, stop and ask for review before continuing. These items are candidate speedups only; do not change optimiser code until an item is selected.

## 2. Batch instruction removals per chunk
- Current: `Optimiser.RemoveMarkedInstructions` sorts removals, then calls `List.RemoveAt`, `AdjustLabelIndicies`, and `AdjustLineNumbers` once per removed instruction.
- Change: dedupe removals per chunk, compact `Instructions` in one read/write pass, then adjust labels and run-length line numbers once from removal prefix counts.
- Expected win: removes repeated list shifting and repeated dictionary/list walks; likely biggest wall-clock win on scripts with many removed instructions.
- Risk: off-by-one label or line-number shifts; cover unreachable-code, goto-reorder, duplicate-removal, and report-count tests.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 404,156.100 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,549,466.000 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,859,557.700 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-04-25`: compile-only 387,647.200 μs (-4.08%), optimisations-on update-many 6,476,323.600 μs (-1.12%), optimisations-off update-many 6,813,529.400 μs (-0.67%). User chose keep.

## 3. Skip removal work when nothing was marked
- Current: `RemoveMarkedInstructions` sorts `_toRemove` after every pass, even when no pass marked instructions.
- Change: return immediately when `_toRemove.Count == 0`.
- Expected win: tiny but free; avoids sort/setup across most passes/chunks.
- Risk: very low; no behaviour change expected.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: completed as part of item 2 guard. Baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 404,156.100 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,549,466.000 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,859,557.700 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-04-25`: compile-only 387,647.200 μs (-4.08%), optimisations-on update-many 6,476,323.600 μs (-1.12%), optimisations-off update-many 6,813,529.400 μs (-0.67%).

## 4. Replace hot LINQ queries in optimiser passes with loops
- Current: optimiser passes use `Where`, `Any`, `Min`, `Count`, `Skip`, `Take`, `ToArray`, `ToList`, `OrderBy` in hot compile paths.
- Change: replace only measured hot spots with direct loops or reused scratch lists/dictionaries.
- Expected win: less allocation and iterator overhead during compile optimisation.
- Risk: readability loss and edge-case mistakes; keep changes local and benchmark each pass-specific edit separately.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-13-31`: compile-only 400,647.300 μs (+3.35%), optimisations-on update-many 6,634,085.500 μs (+2.44%), optimisations-off update-many 6,794,261.500 μs (-0.28%). User chose revert; no code kept.

## 5. Cache label positions per pass
- Current: passes repeatedly scan `chunk.Labels` to answer "is there a label at this instruction?" or "where is the next label?".
- Change: build sorted label-position list or `HashSet<int>` once per pass/chunk, then reuse it.
- Expected win: avoids O(instructions * labels) scans in collapse and control-flow passes.
- Risk: cache must be rebuilt after any pass mutates labels/instructions; stale positions break jumps.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-17-34`: compile-only 398,548.700 μs (+2.81% vs baseline), optimisations-on update-many 6,658,480.500 μs (+2.81% vs baseline), optimisations-off update-many 7,079,828.500 μs (+3.91% vs baseline). Worse; user chose revert; no code kept.

## 6. Use sorted label positions for unreachable-code bounds
- Current: `OptimiserUnreachableCodeRemovalPass.Complete` finds next label with `chunk.Labels.Values.Where(...).DefaultIfEmpty(...).Min()` for each unreachable point.
- Change: reuse `_allLabelEnds` and binary-search/lower-bound next label position.
- Expected win: cheaper unreachable-code removal with many labels or many exits.
- Risk: default end index must match current inclusive removal behaviour exactly.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-20-08`: compile-only 413,202.700 μs (+6.59% vs baseline), optimisations-on update-many 6,914,787.800 μs (+6.77% vs baseline), optimisations-off update-many 7,872,806.600 μs (+15.55% vs baseline). Worse; user chose revert; no code kept.

## 7. Precompute label usage counts
- Current: `OptimiserGotoLabelReorderPass` calls `labelUsage.Count(x => x.label == label)` inside loop.
- Change: build `Dictionary<Label, int>` usage counts once in `Complete`.
- Expected win: avoids O(label-usage^2) behaviour in goto reorder.
- Risk: low; counts must include TEST/GOTO/GOTO_IF_FALSE same as current accumulator.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-22-24`: compile-only 379,830.100 μs (-2.02% vs baseline), optimisations-on update-many 6,594,750.000 μs (+1.83% vs baseline), optimisations-off update-many 6,896,121.100 μs (+1.21% vs baseline). Mixed/noisy; user chose revert; no code kept.

## 8. Precompute used labels for unused-label removal
- Current: `OptimiserRemoveUnusedLabelOpCodesPass` checks each label with `LabelUsage.Any(...)`.
- Change: build `HashSet<Label>` from label usage once, then remove labels not present.
- Expected win: turns O(labels * usages) into O(labels + usages).
- Risk: low; internal-label filtering must stay unchanged.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-24-43`: compile-only 406,582.500 μs (+4.88% vs baseline), optimisations-on update-many 6,559,925.600 μs (+1.29% vs baseline), optimisations-off update-many 6,959,597.000 μs (+2.14% vs baseline). Worse; user chose revert; no code kept.

## 9. Speed duplicate-label collapse grouping
- Current: `OptimiserCollapseDuplicateLabelsPass` sorts labels, then allocates arrays via `Skip/Take/ToArray`, checks existence with `ContainsKey`, joins names, and redirects by scanning usage per label.
- Change: process same-position ranges by index, build usage lookup by label, and avoid temporary arrays unless a collapse is actually needed.
- Expected win: less allocation and fewer repeated scans in label-heavy code.
- Risk: combined label names and redirect semantics must remain byte-for-byte compatible if tests depend on reports/disassembly.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-31-53`: compile-only 395,329.400 μs (+1.98% vs baseline), optimisations-on update-many 7,102,084.300 μs (+9.66% vs baseline), optimisations-off update-many 7,797,239.600 μs (+14.43% vs baseline). Worse; user chose revert; no code kept.

## 10. Avoid label-weave work when pass does not need it
- Current: `OptimiserLabelUsageAccumulator.AddLabelUsage` always calls `Optimiser.IsIndexWeaved`, even for passes that only need label ids.
- Change: add cheap usage collection path without `isWeaved`, or make weave detection optional per accumulator user.
- Expected win: avoids extra instruction lookups and branch checks in duplicate-label and unused-label passes.
- Risk: goto-reorder still needs exact `isWeaved` behaviour.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-37-12`: compile-only 407,054.600 μs (+5.01% vs baseline), optimisations-on update-many 6,489,553.300 μs (+0.20% vs baseline), optimisations-off update-many 6,831,186.200 μs (+0.26% vs baseline). No improvement; user chose revert; no code kept.

## 11. Early-skip label passes with no relevant labels/usages
- Current: label passes still scan chunks with no labels, only internal labels, or no label-using opcodes.
- Change: add cheap guards before full pass work where correctness is obvious.
- Expected win: reduces fixed per-chunk overhead, especially many small function chunks.
- Risk: guard must not skip internal-label or TEST cases that current passes intentionally handle.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 387,647.200 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,476,323.600 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,813,529.400 μs. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-04-21-40-09`: compile-only 398,694.200 μs (+2.85% vs baseline), optimisations-on update-many 6,488,643.700 μs (+0.19% vs baseline), optimisations-off update-many 6,772,914.500 μs (-0.60% vs baseline). No meaningful improvement; user chose revert; no code kept.

## 12. Run follow-up passes only when previous pass changed relevant state
- Current: pass list repeats unreachable, duplicate-label, and collapse passes unconditionally.
- Change: track whether a pass removed instructions, changed labels, or rewrote packets; skip dependent repeat passes when no relevant change happened.
- Expected win: fewer full instruction scans on chunks where earlier passes did nothing.
- Risk: needs precise dirty flags; false negatives leave old optimisation result different.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 396,837.700 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,438,873.100 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,905,278.900 μs. User chose full dirty flags. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-06-39-07`: compile-only 383,385.100 μs (-3.39% vs baseline), optimisations-on update-many 6,569,707.100 μs (+2.03% vs baseline), optimisations-off update-many 6,981,348.000 μs (+1.10% vs baseline). Optimisations-on/off are within/no worse than ~3% run-to-run variance; compile-only improved just beyond noise. User chose revert; no code kept.

## 13. Combine safe adjacent scan-only passes
- Current: each optimiser pass scans every instruction separately.
- Change: only after measuring, combine compatible passes that do not depend on each other's mutations into one scan, preserving existing pass order effects.
- Expected win: fewer full instruction scans per chunk.
- Risk: highest risk in this list; pass ordering is observable through optimisation counts and bytecode shape.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 396,837.700 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,438,873.100 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,905,278.900 μs. Maximalist practical attempt: share instruction scans for pass pairs 5+6 and 9+10, preserving completion order. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-06-51-28`: compile-only 386,710.300 μs (-2.55% vs baseline), optimisations-on update-many 6,438,755.300 μs (-0.00% vs baseline), optimisations-off update-many 6,564,432.000 μs (-4.94% vs baseline). User chose revert; no code kept.

## 14. Process registerise candidates inline or with reusable storage
- Current: `OptimiserRegisterisePass` collects potential instruction indexes in a list, then loops them in `Complete`.
- Change: either process safe cases during scan, or keep current two-phase logic but reuse/grow scratch storage with fewer tuple writes.
- Expected win: less allocation and list traffic in expression-heavy code.
- Risk: overlapping removals and lookahead/lookbehind rules are easy to break; keep exact existing registerised bytecode counts.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 396,837.700 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,438,873.100 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,905,278.900 μs. Attempt: encode registerise candidates into one `int` list entry instead of tuple list. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-06-58-02`: compile-only 384,978.000 μs (-2.99% vs baseline), optimisations-on update-many 6,713,950.300 μs (+4.27% vs baseline), optimisations-off update-many 6,822,851.000 μs (-1.19% vs baseline). Worse for optimisations-on. User chose revert; no code kept.

## 15. Reduce `Chunk.InsertInstructionsAt` adjustment cost
- Current: goto reorder inserts a copied range, then adjusts labels once and line numbers once per inserted instruction.
- Change: adjust line numbers once by inserted count, paired with batched removal if item 2 lands.
- Expected win: faster goto block reordering.
- Risk: line-number run lengths must still map to same source lines after reordering/removal.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 396,837.700 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,438,873.100 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,905,278.900 μs. Attempt: replace per-inserted-instruction line-number adjustment with one `AdjustLineNumbers(at, toMove.Count)` call. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-07-00-14`: compile-only 389,069.400 μs (-1.96% vs baseline), optimisations-on update-many 6,338,192.500 μs (-1.56% vs baseline), optimisations-off update-many 6,644,422.000 μs (-3.78% vs baseline). User chose keep.

## 16. Avoid repeated label-name scans in `Chunk.AddLabel`
- Current: adding a label scans `LabelNames` for name equality and computes max label id with `Labels.Keys.Max(...)`.
- Change: add a small reverse lookup/next-label counter, or a specialised internal path for optimiser-created combined labels.
- Expected win: cheaper duplicate-label collapse when many labels exist.
- Risk: broader `Chunk` behaviour; avoid unless profiler shows label creation matters.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 396,837.700 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,438,873.100 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,905,278.900 μs. Attempt: keep reverse label-name lookup and next-label counter inside `Chunk`. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-07-05-56`: compile-only 390,939.600 μs (-1.49% vs baseline), optimisations-on update-many 6,411,732.300 μs (-0.42% vs baseline), optimisations-off update-many 6,880,987.800 μs (-0.35% vs baseline). No meaningful wall-clock improvement; allocations increased. User chose revert; no code kept.

## 17. Measure optimiser-only wall clock before code changes
- Current: benchmark table covers compile-only and optimise-on/off scenarios, but not isolated optimiser time per pass.
- Change: add temporary/local measurement or benchmark around `Optimiser.Optimise`, ideally per pass/chunk, before attempting speedups.
- Expected win: prevents optimising noise; ranks items above by actual cost.
- Risk: benchmark-only work can grow; keep it local or remove after choosing first implementation item.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: SKIP per user; no code attempted.

## Non-wall-clock optimiser-output ideas

These may make generated bytecode faster or smaller. They do not target optimiser wall clock time directly, so do them after wall-clock items unless explicitly chosen.

## 18. Registerise property access before binary ops
- Current: `OptimiserRegisterisePass` notes that doing get/set property work first could remove `GET_LOCAL 0` and unlock more lhs/rhs binary registerise.
- Change: split or reorder just enough registerise logic to expose existing binary-op optimisations.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: pass-order changes can alter report counts; if only optimiser tests fail, stop and ask for review.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 400,906.800 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,539,628.000 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,888,315.100 μs. Attempt: changed `GET_PROPERTY b2` VM semantics to cache result in a local while preserving stack value, with optimiser assignment fold adjusted to keep cleanup `POP`. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-07-43-41`: compile-only 393,629.500 μs (-1.82% vs baseline), optimisations-on update-many 6,337,265.600 μs (-3.09% vs baseline), optimisations-off update-many 6,706,413.300 μs (-2.64% vs baseline). User chose revert; no code kept.

## 19. Fold `GET_PROPERTY` followed by `SET_LOCAL`
- Current: code comment says a `GET_PROPERTY` followed by `SET_LOCAL` could store target local in the `GET_PROPERTY` packet.
- Change: add narrow pattern fold for `var a = obj.prop` style code where stack effects are proven unchanged.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: stack discipline around `POP` and property getter side effects must stay identical.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: already implemented in `OptimiserRegisterisePass` (`GET_PROPERTY`, `SET_LOCAL`, `POP` -> `GET_PROPERTY b2`) and covered by `Optimiser_GetProp_CollapsesToRegisterBasedOp`; no code change needed.

## 20. Store binary-op result directly to following local
- Current: binary registerise has disabled TODO for folding following `SET_LOCAL` because operation internals need stack/local distinction.
- Change: only if VM semantics support it, let binary ops write to following local and remove `SET_LOCAL`.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: higher VM risk; add focused arithmetic/overload tests before attempting.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: already implemented in `OptimiserRegisterisePass` and covered by `Optimiser_BinaryLocalMath_WhenOptimised_ShouldUseRegisterOperandsAndTarget`; removed stale TODO comment only.

## 21. Remove more `GET_LOCAL` chains for property/index ops
- Current: registerise handles many nearby `GET_LOCAL` patterns, but only specific shapes.
- Change: add more narrow pattern folds after measuring bytecode wins on samples.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: easy to remove a value still needed by stack consumers; ask before changing tests if optimiser counts differ.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline rows before attempt: `Program.CompileVsExecute_CompileOnly` 400,906.800 μs, `RunScriptThenUpdateMany.OptimisationsOn` 6,539,628.000 μs, `RunScriptThenUpdateMany.OptimisationsOff` 6,888,315.100 μs. Attempt: narrow fold for `this.a = this.a op this.a`, removing preserved target `GET_LOCAL` and using `SET_PROPERTY` target register. Tests: 775 passed. MODERATE benchmark `BenchmarkRun-joined-2026-07-05-07-53-01`: compile-only 389,170.800 μs (-2.93% vs baseline), optimisations-on update-many 7,289,969.800 μs (+11.47% vs baseline), optimisations-off update-many 7,251,254.100 μs (+5.27% vs baseline). User chose revert; no code kept.

## 22. Collapse more adjacent stack cleanup ops
- Current: `OptimiserCollapseOpsPass` merges adjacent `POP`s in limited cases.
- Change: look for additional adjacent cleanup patterns that can be represented by existing opcodes.
- Expected win: smaller bytecode, not optimiser wall clock.
- Risk: label boundaries and source line mapping must remain correct.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 23. Add compile-time peephole optimisation while writing packets
- Current: optimiser waits until chunk bytecode is fully written, then scans passes and removes/shuffles packets after the fact.
- Change: add a narrow peephole step in `Chunk.WritePacket` or compiler emit path that checks preceding packet(s) when a new packet is added and folds safe local patterns immediately.
- Expected win: less optimiser wall-clock time by avoiding later full-pass scans, removals, label/line-number shifts, and packet shuffling.
- Risk: compiler emit path is broader than optimiser; must preserve labels, line numbers, stack effects, and current tests. Start with one tiny pattern already handled by optimiser, then compare output.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## Benchmark baseline - MODERATE - 2026-07-05

Source: `BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2026-07-05-07-10-55-report.csv`

Run-to-run variance estimate from immediate rerun `BenchmarkRun-joined-2026-07-05-07-10-55` vs `BenchmarkRun-joined-2026-07-05-07-10-16`: compile-only -0.02%, optimisations-on -2.90%, optimisations-off +2.81%. Treat optimiser wall-clock changes under ~3% as noise unless repeated.

| Name                                           |             Mean |         StdDev |            Prev Mean dif |      Gen |   Allocated |
| :--------------------------------------------- | ---------------: | -------------: | -----------------------: | -------: | ----------: |
| CompileExecuteSplit.Execute                    |       214.600 μs |       5.620 μs |       +7.100 μs (+3.42%) |        - |       192 B |
| CompileExecuteSplitOptimiserOff.Execute        |       217.800 μs |       5.540 μs |       -9.300 μs (-4.10%) |        - |       192 B |
| Program.CompileVsExecute_All                   |    90,283.400 μs |   2,769.060 μs |   +4,904.000 μs (+5.74%) |        - |   134,720 B |
| Program.CompileVsExecute_CompileOnly           |   400,906.800 μs | 123,356.840 μs |      -78.800 μs (-0.02%) | 125.0000 | 1,004,842 B |
| Program.CompileVsExecute_DeepCloneOnly         |    11,613.400 μs |     234.570 μs |      +39.800 μs (+0.34%) |        - |    53,552 B |
| Program.CompileVsExecute_NewEngineOnly         |     8,942.700 μs |     321.060 μs |      +45.800 μs (+0.51%) |        - |    38,632 B |
| Program.CompileVsExecute_TokeniseOnly          |    13,710.700 μs |     511.070 μs |     +339.200 μs (+2.54%) |        - |    48,712 B |
| Program.Conditional_If                         |     9,542.900 μs |     287.770 μs |     +185.800 μs (+1.99%) |        - |    38,688 B |
| Program.Dissasm_Script                         |    66,997.600 μs |   2,083.690 μs |     +562.400 μs (+0.85%) |        - |    96,549 B |
| Program.Looping_Loop                           |    34,530.000 μs |   1,235.720 μs |     -171.800 μs (-0.50%) |        - |    44,320 B |
| Program.ScriptVsNativeFunctional_NativeMethods |   113,059.500 μs |   6,175.730 μs |   +9,084.700 μs (+8.74%) |        - |   154,600 B |
| Program.ScriptVsNativeFunctional_UloxMethods   |   286,635.100 μs |  53,780.220 μs |   +3,501.700 μs (+1.24%) |  15.6250 |   322,672 B |
| RunScriptThenUpdateMany.OptimisationsOff       | 6,888,315.100 μs |  90,722.010 μs | +188,060.500 μs (+2.81%) |        - |    72,732 B |
| RunScriptThenUpdateMany.OptimisationsOn        | 6,539,628.000 μs |  81,724.750 μs | -195,503.200 μs (-2.90%) |        - |    78,849 B |
