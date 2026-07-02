# AGENTS.md

## Project overview

ULox is a C# implementation of a bytecode-interpreted scripting language for Unity, originally inspired by Lox from Crafting Interpreters. The repository also contains a Unity sample/playground project and a small VS Code syntax-highlighting extension for `.ulox` files.

## Repository layout

- `ulox/ulox.core/` - Core ULox runtime/compiler library. Targets `netstandard2.0`; Unity package content lives under `Package/Runtime/`.
- `ulox/ulox.core.tests/` - NUnit test project targeting `net6.0`. Includes C# tests and `.ulox` script samples/tests under `uloxs/`.
- `ulox/ulox.core.bench/` - BenchmarkDotNet benchmarks targeting `net6.0`.
- `ulox/ulox.core.profile/` and `ulox/ulox.tryitout/` - Profiling/try-out helper projects.
- `ulox-example/` - Unity example project using `com.ulox.core` from `ulox/ulox.core/Package` via `Packages/manifest.json`. Unity version noted in `ProjectSettings/ProjectVersion.txt` is `2021.3.25f1`.
- `ulox-vscode/` - VS Code language extension for ULox syntax highlighting.
- `.github/workflows/` - CI for .NET tests, Unity tests, benchmarks, and deployment.

## Coding and maintenance notes

- After completing a feature or task, run project tests only if there are unstaged code changes within `ulox/ulox.core/`.
- Whenever running ULox tests, always use `/skill:test-results`. That skill keeps the terminal noise low.
- Whenever running benchmarks, show the BenchmarkDotNet summary table to the user and append an ASCII-only copy of the table to `benchmark-results.txt` for local retrieval.
- Main runtime code is in namespace/assembly `ulox`; keep changes focused in `ulox/ulox.core/Package/Runtime/` when working on language/runtime behavior.
- When changing language behavior, update or add relevant tests in `ulox/ulox.core.tests/` and consider `.ulox` scripts in `ulox/ulox.core.tests/uloxs/Tests/` and samples in `ulox/ulox.core.tests/uloxs/Samples/`.
- The PR template explicitly reminds maintainers to add/update sample scripts and test scripts; follow that convention for language-facing changes.
- Prefer small, targeted changes. Do not refactor unrelated compiler, VM, optimizer, Unity sample, or extension code while completing a narrow request.

## Agent guidelines: leave untouched unless explicitly requested

- Do not modify generated/build/cache folders: `.git/`, `.vs/`, `ulox/**/bin/`, `ulox/**/obj/`, `ulox-example/Library/`, `ulox-example/Temp/`, `ulox-example/Build/`, `ulox-example/CodeCoverage/`, `Logs/`, or `UserSettings/`.
- Do not edit `ulox.backup/`; treat it as archival backup content.
- Do not modify Unity scene, prefab, material, texture, TextMesh Pro, or other serialized asset files under `ulox-example/Assets/` unless the task is specifically about the Unity example content. If an asset is changed, preserve/update its corresponding `.meta` file intentionally.
- Do not change `.github/workflows/` unless the task is specifically about CI/build automation.
- Do not alter `ulox-vscode/package-lock.json` except when intentionally updating the VS Code extension dependencies.
- Do not overwrite existing user changes. Check `git status --short` before editing and avoid files that are already modified unless the user asks to work on them.
- Do not change `ulox/ulox.core.bench/` or `ulox/ulox.core.profile/` or `ulox-example/` unless explicitly directed to do so.
