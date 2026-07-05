# TODO

## Compiler/VM optimisation process

- Add/confirm tests around likely impacted compiler/desugar/VM behaviour before changing code.
- Do one todo item at a time; keep diff as small as possible.
- Only change the area named by the todo item. Do not refactor unrelated compiler, optimiser, VM, library, or benchmark code.
- Preserve bytecode/runtime behaviour; expected output and existing tests must stay unchanged.
- If performance matters, run a baseline benchmark before changing code and add the relevant row(s) to the todo Notes.
- Make the change, then run ULox tests with `/skill:test-results` if runtime/compiler code changed.
- If tests fail, make no more than 2 implementation fix attempts, then stop and ask for help.
- If tests pass and performance matters, run MODERATE benchmarks with `/skill:benchmark-results`.
- Compare benchmark results to the recorded baseline and add result/drift to that item's Notes.
- If benchmark does not improve beyond noise, ask whether to revert, keep, or stop.
- When keeping performance changes, update the benchmark baseline in the todo before measuring the next performance item.
- If implementation needs broader refactor than todo describes, stop and ask before changing code.
- Never commit implementation/code changes until the user explicitly says to keep/commit after seeing test and benchmark results.
- When user says to keep an item, update status, update baseline if requested, then commit.
- Do not attempt any item marked `SKIP`.
- When moving to a new item, regardless of keep or revert, update the todo. Todo-only commits are OK; implementation commits require explicit user approval.

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

## VM execution performance TODOs

Goal: make VM execution take fewer cycles for same program behaviour. For these items, performance matters: run and record a baseline benchmark before each code change, then update the baseline in this todo when keeping a change.

Current baseline (MODERATE, 2026-07-05): tests 775 passed, 0 failed. Benchmark summary: `CompileExecuteSplit.Execute` 220.300 μs; `Program.Looping_Loop` 34,035.200 μs; `Program.Conditional_If` 9,662.600 μs; `Program.ScriptVsNativeFunctional_NativeMethods` 106,262.300 μs; `Program.ScriptVsNativeFunctional_UloxMethods` 283,317.200 μs; `RunScriptThenUpdateMany.OptimisationsOff` 6,699,146.400 μs; `RunScriptThenUpdateMany.OptimisationsOn` 6,474,061.500 μs.

Future-session start point: do item 2 first. Keep benchmark suite cost low; prefer existing rows and tiny added benchmarks over broad new MODERATE work.

## 2. Add focused VM execution baselines
- Current: existing benchmarks cover some execution paths, but VM hot spots are mixed with compile/setup and broad scripts.
- Change: add or choose minimal BenchmarkDotNet rows for arithmetic loops, function calls, property get/set, method invoke, list/map index ops, and global access.
- Expected win: prevents guessing; gives each VM change a matching measurement.
- Risk: benchmark-only work can grow; keep rows tiny and reuse existing benchmark harness.

### Status
- [x] Attempted
- [x] Tested
- [x] Improved and Committed
- Notes: added LITE-only `VMFocusedBaselines` rows for arithmetic locals, function calls, property get/set, method invoke, list index ops, map index ops, and global access. Existing MODERATE rows still useful: `RunScriptThenUpdateMany.OptimisationsOn`, `CompileExecuteSplit.Execute`, `ScriptVsNativeFunctional_NativeMethods`, `ScriptVsNativeFunctional_UloxMethods`, `Looping_Loop`, `Conditional_If`. Build passed. LITE benchmark passed 2026-07-05. Focused baseline rows: `ArithmeticLocals` 297,007.000 μs; `FunctionCalls` 290,903.300 μs; `PropertyGetSet` 291,462.400 μs; `MethodInvoke` 388,153.300 μs; `ListIndexOps` 274,655.800 μs; `MapIndexOps` 298,107.200 μs; `GlobalAccess` 244,249.900 μs.

## 3. Resolve jump targets before execution
- Current: `GOTO` and `GOTO_IF_FALSE` decode a `Label` and call `chunk.GetLabelPosition`, which does a dictionary lookup during execution.
- Change: after compile/optimise, rewrite jump packets to hold direct instruction targets or add cached target fields/side-table.
- Expected win: fewer hash lookups and less label decoding in loops and branches.
- Risk: labels move during optimisation; resolve only after all instruction/label mutations.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: SKIP per user; labels are preferred even if direct jump targets might be faster.

## 4. Cache stack base in VM hot loop
- Current: local/register access repeatedly reads `_currentCallFrame.StackStart` and adds operand bytes for every local operand.
- Change: cache stack base in `Run` for straight-line execution and refresh it only after call/return/frame changes.
- Expected win: less field access and repeated addition on arithmetic/local-heavy bytecode.
- Risk: stale stack base after `CALL`, `RETURN`, `NATIVE_TYPE`, init-chain frame pushes, or yield paths.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: attempted local `stackStart` cache in `Run` for direct `GET_LOCAL`/`SET_LOCAL`, refreshed after return/call/native call/invoke/native type. Tests passed: 775/775. LITE benchmark after change: `VMFocusedBaselines.ArithmeticLocals` 291,211.000 μs vs 297,007.000 μs baseline (-1.95%); `Program.Looping_Loop` 34,858.100 μs vs 34,548.300 μs prior LITE (+0.90%); result looked within noise. Reverted per user; no VM code kept.

## 5. Split common registerised arithmetic fast paths
- Current: arithmetic ops call `Pop2OrLocals`, branch on `NOT_LOCAL_BYTE` for each operand, then separately handle target local write.
- Change: add narrow fast paths for common fully-registerised numeric ops, or specialised opcodes only if measurement says branch cost matters.
- Expected win: fewer branches and stack operations in optimised arithmetic-heavy code.
- Risk: opcode explosion; keep only measured hot shapes.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: SKIP per user; avoid specialised arithmetic fast paths/opcodes for now.

## 6. Avoid stack push when result is only assigned then popped
- Current: many ops still push a result even when bytecode immediately stores to a local/property or the value is discarded.
- Change: add compile peepholes or VM op variants for statement-position assignment/cleanup paths that write target without pushing dead stack values.
- Expected win: fewer stack writes and fewer cleanup `POP`s for assignments and arithmetic updates.
- Risk: stack discipline is fragile; start with one proven bytecode pattern.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: attempted VM lookahead for assigned arithmetic/compare/index result followed by `POP 1`: write target, advance IP over pop, skip result push. Added focused optimiser test during attempt proving `ADD -> POP 1` assignment pattern and result. Tests passed: 776/776. MODERATE benchmark after change: `RunScriptThenUpdateMany.OptimisationsOn` 6,642,003.400 μs vs current baseline 6,474,061.500 μs (+2.59% slower); `CompileExecuteSplit.Execute` 211.500 μs vs 220.300 μs (-4.00% faster); `Program.Looping_Loop` 36,286.000 μs vs 34,035.200 μs (+6.61% slower). Reverted per user; no code kept.

## 7. Add class field slot storage for fixed fields
- Current: instance fields live in `Table`, so `GET_FIELD`, `SET_FIELD`, and many property paths pay hashed dictionary lookup.
- Change: give class-declared fields stable indexes and store instance field values in an array/list; keep `Table` only for dynamic fields.
- Expected win: faster property/field access on normal class instances.
- Risk: broad instance layout change; dynamic objects, frozen/read-only behaviour, and serialisation may rely on `Fields` table.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: user asked to skip measure-first and try implementation. Added fixed-field slot attempt, tests passed 776/776, benchmark looked like noise (`RunScriptThenUpdateMany.OptimisationsOn` -0.71%, compile/all allocations slightly up, added complexity). Reverted per user; code was harder to work with for noise-level benefit. Do not reattempt without explicit request.

## 8. Cache property and method lookup per call site
- Current: `GET_PROPERTY`/`INVOKE` read name constants then look in fields/method tables every time.
- Change: cache receiver type plus field slot/method `Value` for monomorphic call sites, with fallback when receiver type changes.
- Expected win: faster repeated method calls and property reads in object-heavy code.
- Risk: cache invalidation for dynamic fields/classes; do not attempt before field-slot work or clear fallback rules.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: baseline method/property benchmark first. User chose keep, but complexity is a concern; try only with clear fallback rules. Do not mutate semantics for dynamic objects. Good first version: cache only method lookup for `INVOKE` on `UserTypeInternal`/stable `FromUserType`; fallback to current dictionary path when receiver type differs. Attempted per-instruction `INVOKE` method cache keyed by receiver class, preserving field lookup first. Tests passed: 775/775. LITE benchmark showed no clear win: `VMFocusedBaselines.MethodInvoke` 379,091.600 μs vs 377,778.800 μs baseline (+0.35%); `VMFocusedBaselines.PropertyGetSet` 291,706.100 μs vs 295,895.400 μs baseline (-1.42%, unrelated/noise); `ScriptVsNativeFunctional_UloxMethods` 279,145.600 μs vs 288,962.700 μs (-3.40%, high StdDev/noise). Reverted per user; no VM code kept.

## 9. Avoid `BoundMethod` allocation on method property access
- Current: `GET_PROPERTY` creates a new `BoundMethod` object when a method is read from an instance.
- Change: add narrow VM lookahead for immediate `GET_PROPERTY` + `CALL` method-call shapes, or compile missed call sites to `INVOKE`; do not change escaped method values.
- Expected win: less allocation and GC for immediate method calls currently creating `BoundMethod`.
- Risk: bound method identity/lifetime may be observable; only skip allocation when call is immediate.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: direct `obj.Method()` already emits `INVOKE`; missed zero-arg shape is `(obj.Method)()`, which emits `GET_PROPERTY` then `CALL`. Added focused `VMFocusedBaselines.BoundMethodImmediateCall` row. Baseline LITE: `BoundMethodImmediateCall` 388,163.000 μs, 48,368 B allocated. Added narrow VM lookahead for non-registerised `GET_PROPERTY` followed immediately by zero-arg `CALL`; skips `BoundMethod` allocation only when receiver is an instance, field does not shadow method, class method exists. Escaped/stored/passed method values still use `BoundMethod`. Added immediate-call test. Tests passed: 776/776. LITE after change: `BoundMethodImmediateCall` 366,305.400 μs (-5.63%), 368 B allocated; `MethodInvoke` 378,858.000 μs vs 374,349.600 μs baseline (+1.20%, likely noise); `PropertyGetSet` 299,512.900 μs vs 294,623.400 μs baseline (+1.66%, likely noise).

## 10. Replace open-upvalue `LinkedList` with simpler storage
- Current: open upvalues use `LinkedList<Value>`, allocating nodes and pointer-chasing while capturing/closing closures.
- Change: use a small sorted list/array of open upvalues, or reuse nodes, because open upvalue counts are usually tiny.
- Expected win: fewer allocations and better locality for closure-heavy code.
- Risk: capture order and `CloseUpvalues` semantics must remain exact.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: baseline closure/upvalue benchmark first. User chose keep. Current fields/methods: `openUpvalues` in `VM.cs`, `CaptureUpvalue`, `CloseUpvalues`. Minimal replacement likely `List<Value>` kept sorted by upvalue index descending. Must preserve reuse of same upvalue for same stack index.

## 11. Reduce native-call frame overhead
- Current: native calls push a synthetic call frame and execute a `NATIVE_CALL` packet to invoke the delegate.
- Change: call native delegates directly from `PushCallFrameFromValue` or a dedicated native-call path while preserving return handling.
- Expected win: fewer frame pushes, instruction dispatches, and stack shuffles for native-heavy scripts.
- Risk: yield/return behaviour and native error reporting may depend on call frames.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: baseline native-list/native-math benchmark first. User chose keep. Likely files: `VM.cs`, `CallFrame.cs`, native library classes. Preserve `SetNativeReturn`, `GetArg`, exception wrapping, and stack cleanup exactly. First safe attempt may be direct native invocation with a temporary lightweight current native-call context rather than full frame removal.

## 12. Specialise native list/map index op paths
- Current: `GET_INDEX`, `SET_INDEX`, and `COUNT_OF` go through `INativeCollection` interface checks/calls.
- Change: add direct `NativeListInstance` and `NativeMapInstance` branches before interface fallback.
- Expected win: fewer interface dispatches for common collection operations.
- Risk: small win only; keep fallback for other native collections.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: SKIP per user; do not specialise native list/map index interface paths.

## 13. Trim allocations in native list helpers
- Current: native list helpers allocate in places such as `OrderBy(...).ToArray()` and create `Random` per `Shuffle`.
- Change: use `Array.Sort` on the existing key array and shared/per-VM random for shuffle.
- Expected win: less allocation and less overhead in list-heavy scripts.
- Risk: ordering stability and random determinism may change; confirm expected behaviour first.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: baseline native-list benchmark first. User chose keep. Likely file: `NativeListClass.cs`. `OrderBy` already has array of `(Value val, int index)` then LINQ allocates another array; `Array.Sort(orderByArr, comparer)` can avoid LINQ. For `Shuffle`, ask before changing randomness/determinism; `new Random()` per call may be acceptable unless benchmarked hot.

## 14. Add a fast `FastStack` copy/reset path for VM cloning
- Current: `CopyStackFrom` loops and pushes each value/frame, paying capacity checks per element.
- Change: add `FastStack.CopyFrom`/bulk copy and optional capacity pre-grow.
- Expected win: faster test runner/VM-class clone paths and fewer resizes.
- Risk: must not expose unused array slots or stale references when clear mode is enabled.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: SKIP per user; VM cloning not target runtime perf now.

## 15. Replace hot `Table.Set` double lookup
- Current: `Table.Set` does `ContainsKey` then assigns, causing two dictionary lookups for existing keys.
- Change: use `TryGetValue`/`CollectionsMarshal` where available, or accept assignment after one existence check in callers that already proved existence.
- Expected win: cheaper global/property/map updates.
- Risk: target framework is `netstandard2.0`; no newer APIs unless guarded.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: baseline global/property update benchmark first. User chose keep. `CollectionsMarshal` is not available for `netstandard2.0`; simplest candidate is adding `SetExistingUnchecked`/`SetAfterContains` only for callers that already checked existence, or changing callers to one `Get` plus assignment helper. Avoid changing `Table.Set` semantics for missing keys.
