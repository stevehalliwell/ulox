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
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 5. Cache label positions per pass
- Current: passes repeatedly scan `chunk.Labels` to answer "is there a label at this instruction?" or "where is the next label?".
- Change: build sorted label-position list or `HashSet<int>` once per pass/chunk, then reuse it.
- Expected win: avoids O(instructions * labels) scans in collapse and control-flow passes.
- Risk: cache must be rebuilt after any pass mutates labels/instructions; stale positions break jumps.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 6. Use sorted label positions for unreachable-code bounds
- Current: `OptimiserUnreachableCodeRemovalPass.Complete` finds next label with `chunk.Labels.Values.Where(...).DefaultIfEmpty(...).Min()` for each unreachable point.
- Change: reuse `_allLabelEnds` and binary-search/lower-bound next label position.
- Expected win: cheaper unreachable-code removal with many labels or many exits.
- Risk: default end index must match current inclusive removal behaviour exactly.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 7. Precompute label usage counts
- Current: `OptimiserGotoLabelReorderPass` calls `labelUsage.Count(x => x.label == label)` inside loop.
- Change: build `Dictionary<Label, int>` usage counts once in `Complete`.
- Expected win: avoids O(label-usage^2) behaviour in goto reorder.
- Risk: low; counts must include TEST/GOTO/GOTO_IF_FALSE same as current accumulator.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 8. Precompute used labels for unused-label removal
- Current: `OptimiserRemoveUnusedLabelOpCodesPass` checks each label with `LabelUsage.Any(...)`.
- Change: build `HashSet<Label>` from label usage once, then remove labels not present.
- Expected win: turns O(labels * usages) into O(labels + usages).
- Risk: low; internal-label filtering must stay unchanged.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 9. Speed duplicate-label collapse grouping
- Current: `OptimiserCollapseDuplicateLabelsPass` sorts labels, then allocates arrays via `Skip/Take/ToArray`, checks existence with `ContainsKey`, joins names, and redirects by scanning usage per label.
- Change: process same-position ranges by index, build usage lookup by label, and avoid temporary arrays unless a collapse is actually needed.
- Expected win: less allocation and fewer repeated scans in label-heavy code.
- Risk: combined label names and redirect semantics must remain byte-for-byte compatible if tests depend on reports/disassembly.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 10. Avoid label-weave work when pass does not need it
- Current: `OptimiserLabelUsageAccumulator.AddLabelUsage` always calls `Optimiser.IsIndexWeaved`, even for passes that only need label ids.
- Change: add cheap usage collection path without `isWeaved`, or make weave detection optional per accumulator user.
- Expected win: avoids extra instruction lookups and branch checks in duplicate-label and unused-label passes.
- Risk: goto-reorder still needs exact `isWeaved` behaviour.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 11. Early-skip label passes with no relevant labels/usages
- Current: label passes still scan chunks with no labels, only internal labels, or no label-using opcodes.
- Change: add cheap guards before full pass work where correctness is obvious.
- Expected win: reduces fixed per-chunk overhead, especially many small function chunks.
- Risk: guard must not skip internal-label or TEST cases that current passes intentionally handle.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 12. Run follow-up passes only when previous pass changed relevant state
- Current: pass list repeats unreachable, duplicate-label, and collapse passes unconditionally.
- Change: track whether a pass removed instructions, changed labels, or rewrote packets; skip dependent repeat passes when no relevant change happened.
- Expected win: fewer full instruction scans on chunks where earlier passes did nothing.
- Risk: needs precise dirty flags; false negatives leave old optimisation result different.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 13. Combine safe adjacent scan-only passes
- Current: each optimiser pass scans every instruction separately.
- Change: only after measuring, combine compatible passes that do not depend on each other's mutations into one scan, preserving existing pass order effects.
- Expected win: fewer full instruction scans per chunk.
- Risk: highest risk in this list; pass ordering is observable through optimisation counts and bytecode shape.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 14. Process registerise candidates inline or with reusable storage
- Current: `OptimiserRegisterisePass` collects potential instruction indexes in a list, then loops them in `Complete`.
- Change: either process safe cases during scan, or keep current two-phase logic but reuse/grow scratch storage with fewer tuple writes.
- Expected win: less allocation and list traffic in expression-heavy code.
- Risk: overlapping removals and lookahead/lookbehind rules are easy to break; keep exact existing registerised bytecode counts.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 15. Reduce `Chunk.InsertInstructionsAt` adjustment cost
- Current: goto reorder inserts a copied range, then adjusts labels once and line numbers once per inserted instruction.
- Change: adjust line numbers once by inserted count, paired with batched removal if item 2 lands.
- Expected win: faster goto block reordering.
- Risk: line-number run lengths must still map to same source lines after reordering/removal.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 16. Avoid repeated label-name scans in `Chunk.AddLabel`
- Current: adding a label scans `LabelNames` for name equality and computes max label id with `Labels.Keys.Max(...)`.
- Change: add a small reverse lookup/next-label counter, or a specialised internal path for optimiser-created combined labels.
- Expected win: cheaper duplicate-label collapse when many labels exist.
- Risk: broader `Chunk` behaviour; avoid unless profiler shows label creation matters.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 17. Measure optimiser-only wall clock before code changes
- Current: benchmark table covers compile-only and optimise-on/off scenarios, but not isolated optimiser time per pass.
- Change: add temporary/local measurement or benchmark around `Optimiser.Optimise`, ideally per pass/chunk, before attempting speedups.
- Expected win: prevents optimising noise; ranks items above by actual cost.
- Risk: benchmark-only work can grow; keep it local or remove after choosing first implementation item.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## Non-wall-clock optimiser-output ideas

These may make generated bytecode faster or smaller. They do not target optimiser wall clock time directly, so do them after wall-clock items unless explicitly chosen.

## 18. Registerise property access before binary ops
- Current: `OptimiserRegisterisePass` notes that doing get/set property work first could remove `GET_LOCAL 0` and unlock more lhs/rhs binary registerise.
- Change: split or reorder just enough registerise logic to expose existing binary-op optimisations.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: pass-order changes can alter report counts; if only optimiser tests fail, stop and ask for review.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 19. Fold `GET_PROPERTY` followed by `SET_LOCAL`
- Current: code comment says a `GET_PROPERTY` followed by `SET_LOCAL` could store target local in the `GET_PROPERTY` packet.
- Change: add narrow pattern fold for `var a = obj.prop` style code where stack effects are proven unchanged.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: stack discipline around `POP` and property getter side effects must stay identical.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 20. Store binary-op result directly to following local
- Current: binary registerise has disabled TODO for folding following `SET_LOCAL` because operation internals need stack/local distinction.
- Change: only if VM semantics support it, let binary ops write to following local and remove `SET_LOCAL`.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: higher VM risk; add focused arithmetic/overload tests before attempting.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

## 21. Remove more `GET_LOCAL` chains for property/index ops
- Current: registerise handles many nearby `GET_LOCAL` patterns, but only specific shapes.
- Change: add more narrow pattern folds after measuring bytecode wins on samples.
- Expected win: smaller/faster generated bytecode, not optimiser wall clock.
- Risk: easy to remove a value still needed by stack consumers; ask before changing tests if optimiser counts differ.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not attempted.

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

## Benchmark baseline - MODERATE - 2026-07-04

Source: `BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2026-07-04-20-55-09-report.csv`

| Name                                           |             Mean |         StdDev |            Prev Mean dif |      Gen |   Allocated |
| :--------------------------------------------- | ---------------: | -------------: | -----------------------: | -------: | ----------: |
| CompileExecuteSplit.Execute                    |       216.900 μs |       5.330 μs |       -5.800 μs (-2.60%) |        - |       192 B |
| CompileExecuteSplitOptimiserOff.Execute        |       220.700 μs |       7.740 μs |      +11.200 μs (+5.35%) |        - |       192 B |
| Program.CompileVsExecute_All                   |    89,603.800 μs |   6,277.450 μs |     +685.500 μs (+0.77%) |        - |   136,528 B |
| Program.CompileVsExecute_CompileOnly           |   404,156.100 μs | 126,630.890 μs |  +26,759.200 μs (+7.09%) | 125.0000 | 1,004,802 B |
| Program.CompileVsExecute_DeepCloneOnly         |    11,562.700 μs |     116.530 μs |     -112.700 μs (-0.97%) |        - |    53,512 B |
| Program.CompileVsExecute_NewEngineOnly         |     8,858.100 μs |     208.130 μs |     -201.400 μs (-2.22%) |        - |    38,592 B |
| Program.CompileVsExecute_TokeniseOnly          |    13,940.600 μs |     274.490 μs |     +270.100 μs (+1.98%) |        - |    48,672 B |
| Program.Conditional_If                         |     9,952.000 μs |     228.510 μs |     +229.200 μs (+2.36%) |        - |    38,648 B |
| Program.Dissasm_Script                         |    65,788.100 μs |   1,332.140 μs |   -1,797.100 μs (-2.66%) |        - |    96,549 B |
| Program.Looping_Loop                           |    34,858.100 μs |   1,149.780 μs |     +253.000 μs (+0.73%) |        - |    44,280 B |
| Program.ScriptVsNativeFunctional_NativeMethods |   106,305.700 μs |   2,642.090 μs |   +2,615.300 μs (+2.52%) |        - |   154,744 B |
| Program.ScriptVsNativeFunctional_UloxMethods   |   299,108.900 μs |  46,655.340 μs |  -19,640.600 μs (-6.16%) |  15.6250 |   344,472 B |
| RunScriptThenUpdateMany.OptimisationsOff       | 6,859,557.700 μs |  78,766.910 μs | -144,509.800 μs (-2.06%) |        - |    72,692 B |
| RunScriptThenUpdateMany.OptimisationsOn        | 6,549,466.000 μs |  66,885.040 μs | -439,147.800 μs (-6.28%) |        - |    78,833 B |
