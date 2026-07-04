# TODO

Use this shape when adding new todo items. Do one item at a time.

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

## Future session instructions

- Do not attempt any item marked `SKIP`.
- Do one todo item at a time; keep diff as small as possible.
- Only change compiler/desugar code unless item explicitly says otherwise.
- Do not optimise scanner or optimiser here.
- Preserve bytecode/runtime behaviour; expected output and existing tests must stay unchanged.
- Before each item, note baseline benchmark row(s) most relevant to that item.
- After each item, run ULox tests with `/skill:test-results` if runtime/compiler code changed.
- After each item, run MODERATE benchmarks with `/skill:benchmark-results` and add result/drift to that item's Notes.
- If benchmark does not improve beyond noise, revert code or mark item failed with result.
- If implementation needs broader refactor than todo describes, stop and ask before changing code.

## 2. Cache token line numbers during compile
- Current: `Compiler.EmitPacket` and `WriteConstantAt` call `TokenIterator.GetLineAndCharacter`, which linearly scans `LineLengths` for nearly every emitted bytecode packet.
- Change: cache line number per token index in `TokenIterator` as tokens advance, or keep a rolling line cursor; emit packets with cached current line.
- Expected win: removes O(lines) work from hot bytecode emission without changing emitted instructions or diagnostics.
- Risk: wrong run-length line metadata; cover with disassembly/diagnostic line tests plus full compiler tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only improved 573,048.1 -> 550,787.2 μs (-3.88%); runtime-heavy rows mixed/noisy.

## 3. Run desugar checks by token type instead of every step every token
- Current: `TokenIterator.Advance` calls every desugar step for every token, even though most steps match one token type.
- Change: build a `Dictionary<TokenType, IDesugarStep[]>` or switch dispatch for `STRING`, compound assignment tokens, `LOOP`, and context-sensitive `INIT`.
- Expected win: fewer virtual calls and branch chains per token while preserving desugar output.
- Risk: missing context-sensitive `INIT`; cover class init arg match tests and all desugar tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only improved 550,787.2 -> 430,819.7 μs (-21.78%); runtime-heavy rows mixed/noisy.

## 4. Replace `Regex.Unescape` in string interpolation desugar
- Current: every string token goes through `System.Text.RegularExpressions.Regex.Unescape`, including strings without escape sequences.
- Change: skip unescape when string contains no backslash; for escaped strings, use small direct unescape matching current scanner/compiler semantics.
- Expected win: avoids regex engine setup on common string literals.
- Risk: escape semantics drift; cover escaped string and interpolation tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only improved 430,819.7 -> 421,770.1 μs (-2.10%); runtime-heavy rows mixed/noisy.

## 5. Avoid scanner allocation for simple interpolated expressions
- Current: `StringInterpDesugar` creates a new `Scanner` and `Script` for every interpolation expression.
- Change: fast-path single identifier/member/index expressions by emitting tokens directly; keep scanner fallback for complex interpolation.
- Expected win: less allocation and rescanning for common `$"{name}"`-style interpolation while preserving complex cases.
- Risk: tokenization differences in fast path; cover simple and complex interpolation tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only 421,770.1 -> 420,187.7 μs (-0.38%); not a meaningful win, code reverted.

## 6. Reduce list mutation cost in loop desugar
- Current: `LoopDesugar` repeatedly uses `FindIndex`, `GetRange`, `RemoveRange`, and multiple `InsertRange` calls over the shared token list.
- Change: compute needed indexes once and replace the whole loop slice with one pre-sized token buffer.
- Expected win: fewer token list shifts and temporary enumerables for each `loop`.
- Risk: off-by-one brace placement; cover all `loop` and `for` desugar tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only regressed 420,187.7 -> 430,887.7 μs (+2.55%); code reverted.

## 7. Replace LINQ in desugar hot paths
- Current: `ClassInitArgMatchDesugar` and `LoopDesugar` use `Where`, `SelectMany`, and `Concat` while mutating token lists.
- Change: use simple loops with pre-sized `List<Token>` buffers.
- Expected win: fewer iterator allocations and less delegate overhead during compile.
- Risk: changed token order; cover class init arg and loop tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only improved 430,887.7 -> 396,860.9 μs (-7.90%); allocated 1,005,090 -> 1,004,802 B.

## 8. Cache chunk string constants during compile
- Current: `Chunk.AddConstant` linearly scans all constants for every string/double constant add.
- Change: maintain per-chunk dictionaries for string and double constant IDs inside `Chunk` or compiler-side during compile.
- Expected win: turns repeated identifier/property/global constant lookup from O(constants) to O(1).
- Risk: duplicate constants or changed constant IDs; cover bytecode/disassembly-sensitive tests and full compiler tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 9. Avoid add-then-read constant path for local declarations
- Current: `DeclareVariable` adds current identifier as a string constant, reads it back, then declares local by name.
- Change: when `scopeDepth > 0`, use `TokenIterator.PreviousToken.Literal` directly and skip constant add/read.
- Expected win: less constant table work for local variable declarations; emitted bytecode unchanged because locals do not need constants.
- Risk: global declaration path must still add constants; cover local/global var declaration tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 10. Use stack peek instead of LINQ `Last` for loop state
- Current: `BreakStatement` and `ContinueStatement` call `comp.LoopStates.Last()` on `Stack<LoopState>`.
- Change: use `comp.LoopStates.Peek()` once and reuse it.
- Expected win: avoids enumerating stack on every break/continue compile.
- Risk: none expected if same top loop state is used; cover nested loop break/continue tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only regressed 396,860.9 -> 402,963.0 μs (+1.54%); code reverted.

## 11. Replace enum duplicate list scan with set
- Current: `EnumTypeDeclarationCompliette` checks `_enumKeys.Contains` on a `List<string>` for each enum key.
- Change: keep a `HashSet<string>` for duplicate detection; keep list only if order is needed.
- Expected win: O(1) duplicate checks for large enums.
- Risk: comparer must match existing string equality; cover duplicate enum key tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 12. Replace class field list membership with set
- Current: `ResolveNameLookupOpCode`, `DoesCurrentClassHaveMatchingField`, and init arg desugar use `Fields.Contains` over a list.
- Change: add set-backed membership in `TypeInfoEntry` while preserving existing ordered `Fields` list API.
- Expected win: faster field resolution and class init desugar for classes with many fields/mixins.
- Risk: mixin field propagation must keep set and list in sync; cover class field, mixin, and init arg tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 13. Cache generated unique-name counters per chunk/prefix
- Current: `ChunkUniqueName` counts constants with LINQ and `Contains(prefix)` every time a unique local/test name is needed.
- Change: track a per-compiler-state dictionary of prefix counters.
- Expected win: removes repeated full constant-table scans during loop/test desugar and anonymous naming.
- Risk: generated names differ; behavior should remain same unless tests assert exact internal names. Cover disassembly/name-sensitive tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 14. Replace label-name lookup scan with dictionary
- Current: `Chunk.AddLabel(HashedString, int)` scans `LabelNames` and computes `Max` over labels for each new label.
- Change: track `Dictionary<HashedString, Label>` plus a monotonically increasing next-label id.
- Expected win: O(1) label creation for branch-heavy code.
- Risk: label ID allocation must remain stable enough for tests/disassembly; cover goto/label, if, loop, match, and testset compile tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 15. Replace declaration/statement compilette dictionaries with arrays
- Current: compiler dispatch for declarations/statements uses `Dictionary<TokenType, ICompilette>`.
- Change: use `ICompilette[]` indexed by `(int)TokenType`, matching Pratt parser rule storage.
- Expected win: faster dispatch in parse hot path with no behavior change.
- Risk: enum size/index assumptions; cover all declaration and statement tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 16. Remove `ICompilette` wrappers for compiler-local static actions
- Current: `CompiletteAction` wraps static compiler actions in heap objects and interface calls.
- Change: store `Action<Compiler>` arrays directly for declaration/statement dispatch; keep custom compilette objects only where state is required.
- Expected win: fewer interface calls and setup allocations.
- Risk: stateful compilettes (`class`, `testset`, `testcase`, `enum`) must still work; cover declarations and tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 17. Replace Pratt parse rule objects with struct table
- Current: Pratt rules are `IParseRule` objects with virtual `Prefix`/`Infix` calls; invalid rules throw `NotImplementedException`.
- Change: store `struct` entries with nullable delegates and precedence in an array; call delegates directly and throw compiler errors for missing prefix/infix.
- Expected win: less virtual dispatch in expression parsing and fewer setup allocations.
- Risk: invalid expression diagnostics may change; cover expression parse error tests.

### Status
- SKIP
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 18. Cache current compiler state in statement/expression hot paths
- Current: many helpers repeatedly call `CurrentCompilerState`/`CurrentChunk`, each peeking `_compilerStates`.
- Change: in tight methods (`EndScope`, `ResolveNameLookupOpCode`, emit helpers), cache state/chunk once and pass/use local variables.
- Expected win: fewer stack peeks and property calls during compilation.
- Risk: stale state around nested function/type compile boundaries; apply only within methods that do not push/pop state. Cover nested functions/classes.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only 402,963.0 -> 403,842.2 μs (+0.22%); not a meaningful win, code reverted.

## 19. Reuse compiler desugar step instances
- Current: every `TokenIterator` construction allocates four desugar step objects.
- Change: make stateless desugar steps static/shared; allocate only context-bearing state if ever needed.
- Expected win: less per-compile allocation.
- Risk: hidden mutable state in future desugar steps; keep current stateless contract or document it. Cover all desugar tests.

### Status
- [x] Attempted
- [x] Tested
- [ ] Improved and Committed
- Notes: tests passed (771/771). MODERATE benchmark compile-only improved 403,842.2 -> 382,638.9 μs (-5.25%); allocated 1,004,802 -> 1,004,674 B; code reverted by request.

## 20. Pre-size compiler collections from token count
- Current: chunks, compiler state stacks, and desugar output buffers use fixed small starting capacities and grow as scripts compile.
- Change: use token count heuristics to set initial capacities for instructions/constants/temp token buffers where cheap.
- Expected win: fewer list reallocations for large scripts.
- Risk: over-allocation on tiny scripts; keep conservative caps. Cover compile tests and allocation benchmark.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.
