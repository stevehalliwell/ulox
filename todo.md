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

Source: `BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2026-07-04-15-56-45-report.csv`

| Name                                           |             Mean |         StdDev |            Prev Mean dif |      Gen |   Allocated |
| :--------------------------------------------- | ---------------: | -------------: | -----------------------: | -------: | ----------: |
| CompileExecuteSplit.Execute                    |       212.300 μs |       3.930 μs |       +0.900 μs (+0.43%) |        - |       192 B |
| CompileExecuteSplitOptimiserOff.Execute        |       220.700 μs |       6.310 μs |       -4.000 μs (-1.78%) |        - |       192 B |
| Program.CompileVsExecute_All                   |    93,649.500 μs |   8,492.680 μs |   -1,070.600 μs (-1.13%) |        - |   136,856 B |
| Program.CompileVsExecute_CompileOnly           |   573,048.100 μs | 120,373.820 μs |  -37,088.100 μs (-6.08%) | 125.0000 | 1,005,130 B |
| Program.CompileVsExecute_DeepCloneOnly         |    11,716.700 μs |     181.490 μs |      -54.400 μs (-0.46%) |        - |    53,512 B |
| Program.CompileVsExecute_NewEngineOnly         |     9,084.100 μs |     313.890 μs |      +46.900 μs (+0.52%) |        - |    38,592 B |
| Program.CompileVsExecute_TokeniseOnly          |    14,000.500 μs |     466.690 μs |     -126.400 μs (-0.89%) |        - |    48,672 B |
| Program.Conditional_If                         |     9,410.300 μs |     273.660 μs |     -209.100 μs (-2.17%) |        - |    38,648 B |
| Program.Dissasm_Script                         |    68,099.300 μs |   2,579.580 μs |     -472.100 μs (-0.69%) |        - |    96,549 B |
| Program.Looping_Loop                           |    34,734.000 μs |   1,225.600 μs |     -540.700 μs (-1.53%) |        - |    44,280 B |
| Program.ScriptVsNativeFunctional_NativeMethods |   117,394.400 μs |   9,466.490 μs |   +4,655.600 μs (+4.13%) |        - |   154,784 B |
| Program.ScriptVsNativeFunctional_UloxMethods   |   365,199.900 μs |  64,514.910 μs |   +9,752.200 μs (+2.74%) |  15.6250 |   345,520 B |
| RunScriptThenUpdateMany.OptimisationsOff       | 6,719,192.700 μs |  58,691.420 μs | -254,558.200 μs (-3.65%) |        - |    72,732 B |
| RunScriptThenUpdateMany.OptimisationsOn        | 6,372,104.700 μs |  49,325.100 μs | -307,632.200 μs (-4.61%) |        - |    78,873 B |

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
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 4. Replace `Regex.Unescape` in string interpolation desugar
- Current: every string token goes through `System.Text.RegularExpressions.Regex.Unescape`, including strings without escape sequences.
- Change: skip unescape when string contains no backslash; for escaped strings, use small direct unescape matching current scanner/compiler semantics.
- Expected win: avoids regex engine setup on common string literals.
- Risk: escape semantics drift; cover escaped string and interpolation tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 5. Avoid scanner allocation for simple interpolated expressions
- Current: `StringInterpDesugar` creates a new `Scanner` and `Script` for every interpolation expression.
- Change: fast-path single identifier/member/index expressions by emitting tokens directly; keep scanner fallback for complex interpolation.
- Expected win: less allocation and rescanning for common `$"{name}"`-style interpolation while preserving complex cases.
- Risk: tokenization differences in fast path; cover simple and complex interpolation tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 6. Reduce list mutation cost in loop desugar
- Current: `LoopDesugar` repeatedly uses `FindIndex`, `GetRange`, `RemoveRange`, and multiple `InsertRange` calls over the shared token list.
- Change: compute needed indexes once and replace the whole loop slice with one pre-sized token buffer.
- Expected win: fewer token list shifts and temporary enumerables for each `loop`.
- Risk: off-by-one brace placement; cover all `loop` and `for` desugar tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 7. Replace LINQ in desugar hot paths
- Current: `ClassInitArgMatchDesugar` and `LoopDesugar` use `Where`, `SelectMany`, and `Concat` while mutating token lists.
- Change: use simple loops with pre-sized `List<Token>` buffers.
- Expected win: fewer iterator allocations and less delegate overhead during compile.
- Risk: changed token order; cover class init arg and loop tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

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
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

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
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

## 19. Reuse compiler desugar step instances
- Current: every `TokenIterator` construction allocates four desugar step objects.
- Change: make stateless desugar steps static/shared; allocate only context-bearing state if ever needed.
- Expected win: less per-compile allocation.
- Risk: hidden mutable state in future desugar steps; keep current stateless contract or document it. Cover all desugar tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
- Notes: not started.

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
