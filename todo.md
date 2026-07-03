# Scanner optimisation TODO

Review scope: `Scanner.cs`, `StringIterator.cs`, `Token.cs`, `TokenisedScript.cs`, scanner callers/benchmarks.

## 1. Replace `StringIterator` scan path with direct string/index locals
- Current scanner calls through `StringIterator.Peek()`, `SafeRead()`, `CurrentChar`, `Advance()`, `IsAtEnd()` for each char.
- Keep line length tracking, but scan with local `source`, `length`, `index`, `current` inside `Scanner.Scan`.
- Expected win: fewer method/property calls, fewer bounds-style checks, less object state traffic.
- Risk: line/character accounting changes; cover with scanner tests.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 2. Avoid per-token substring unless parser needs literal
- Current scanner allocates `Substring` for every identifier and number.
- Numbers/identifiers need literal later, keywords only need token type. For keywords, emit literal only if compiler uses it; likely can emit `null`.
- Expected win: fewer allocations and copies on keyword-heavy code.
- Risk: any compiler/desugar path expecting keyword `Literal`; grep before change.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 3. Replace keyword `Dictionary<string, TokenType>` 
- Current identifier path creates string then dictionary lookup.
- See if we can replace the raw dictionary with a compile time or immutable dictionary before attempting, if chain or switch
- Use identifier span/range and switch by length + chars (`if`, `var`, `return`, etc.). Only allocate literal once token type known.
- Expected win: no dictionary hash/lookup, can avoid keyword string allocation.
- Risk: more manual code; generate none. Keep boring switch.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 4. Use `Substring` for strings instead of `StringBuilder` when no escapes processed
- Current string scan appends every char into reused `StringBuilder`.
- Since scanner only detects escaped quote and does not unescape content, find closing quote then one `Substring`.
- Expected win: fewer per-char calls and no builder append loop cost.
- Risk: preserve escaped quote behavior and unterminated-string error location.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 5. Pre-size token list from source length
- Current `TokenStartingCapacity = 500` fixed capacity.
- Use cheap heuristic like `Math.Max(16, source.Length / 4)` or keep 500 minimum for existing behavior.
- Expected win: fewer `List<Token>` resizes on large scripts, less over-allocation on tiny scripts if minimum reduced.
- Risk: tiny. Benchmark both heuristics.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 6. Reuse `LineLengths` storage or return `List<int>` directly
- Current `StringIterator` owns `List<int>`, then `ToArray()` allocates copy in `TokenisedScript`.
- Make `TokenisedScript.LineLengths` `List<int>` or let scanner manage/reuse array/list.
- Expected win: remove one allocation/copy per scan.
- Risk: compiler `GetLineAndCharacter` only indexes/Length; small signature change.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 7. Collapse direct-symbol dispatch into table/switch fast path only after measuring
- Current `Consume()` calls `ConsumeDirectSymbol()` before digit/alpha tests.
- Possible direct char switch in one loop can remove extra method calls and repeated `CurrentChar` property reads.
- Expected win: small unless scanner hot in profile.
- Risk: large diff for small gain; do after items above.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed

## 8. Benchmark scanner with allocation columns before/after each item
- Existing benchmark: `CompileVsExecute_TokeniseOnly`.
- Add/adjust only if needed to isolate large/tiny/string-heavy/comment-heavy scripts.
- Expected win: prevents guessing.
- Risk: benchmark sprawl; keep one parameterized benchmark if added.

### Status
- [ ] Attempted
- [ ] Tested
- [ ] Improved and Committed
