# TODO

## Compiler optimisation process

- Add/confirm tests around likely impacted compiler/desugar behavior before changing code.
- Do one todo item at a time; keep diff as small as possible.
- Only change compiler/desugar code unless item explicitly says otherwise.
- Do not optimise scanner or optimiser here.
- Preserve bytecode/runtime behaviour; expected output and existing tests must stay unchanged.
- If performance matters, run a baseline benchmark before changing code and add the relevant row(s) to the todo Notes.
- Make the change, then run ULox tests with `/skill:test-results` if runtime/compiler code changed.
- If tests fail, make no more than 2 implementation fix attempts, then stop and ask for help.
- If tests pass and performance matters, run MODERATE benchmarks with `/skill:benchmark-results`.
- Compare benchmark results to the recorded baseline and add result/drift to that item's Notes.
- If benchmark does not improve beyond noise, ask whether to revert, keep, or stop.
- When keeping performance changes, update the benchmark baseline in the todo before measuring the next performance item.
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
