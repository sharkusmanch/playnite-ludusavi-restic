# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

---

## 5. C# Conventions

This project targets **.NET Framework 4.6.2**. C# 8+ features (nullable reference types, switch expressions, using declarations) are not available.

**Build**
- Use `MSBuild.exe` (Visual Studio / VS Build Tools), not `dotnet build`. The `dotnet` CLI does not generate `.g.cs` XAML code-behind files for WPF on .NET Framework — the build will succeed but the view won't work.
- This project uses **SDK-style `.csproj`** (`Microsoft.NET.Sdk.WindowsDesktop`). New `.cs` files are auto-included by default. Do not add `<Compile Include>` entries manually.
- When adding NuGet packages, only use versions that ship a `net462` (or `net461`/`net45`) target folder. Don't rely on `netstandard2.0` fallbacks — they can silently break at runtime inside Playnite's AppDomain.

**Naming** (Section 5 naming rules take priority over "match existing style" — don't perpetuate violations in new code)
- Classes, methods, properties, enums: `PascalCase`
- Local variables and parameters: `camelCase`
- Private fields (instance and static): `_camelCase` (e.g., `_api`, `_rng`)
- Exception: the logger is `logger` (no underscore) — this is the Playnite SDK convention
- Constants: `PascalCase`
- Interfaces: `IPascalCase`

**Exception handling**
- Catch only exceptions you can handle meaningfully. **Always log them before re-throwing or recovering.**
- Use `throw;` (not `throw ex;`) to preserve the original stack trace.
- Don't use exceptions for control flow; guard with defensive checks instead.
- Exception: cleanup code (deleting temp files in `finally` or `OnApplicationStopped`) may use empty `catch { }` — add a comment explaining why silence is acceptable.
- For batch operations (processing N games), catch and log per-item failures so one bad game doesn't abort the rest. This is not "error handling for impossible scenarios" — it's expected partial failure.

**Resource management**
- Wrap `IDisposable` objects in `using` blocks.
- Don't leave streams, file handles, or HTTP clients undisposed.
- GDI+/`System.Drawing` objects (`Image`, `Bitmap`, `Graphics`) must be disposed — memory leaks are silent and cumulative. Always use `using` or explicit `Dispose()` in a `finally` block.

**Async**
- No `async void` except for event handlers.
- Don't block async with `.Result` or `.Wait()` — deadlock risk on UI threads.

**Access modifiers**
- Default to `private`. Only widen visibility when there's an explicit reason.
- Prefer interfaces for service dependencies.

---

## 6. Playnite Plugin Rules

**Logging**
- Always declare: `private static readonly ILogger logger = LogManager.GetLogger();`
- Use `logger.Debug/Info/Warn/Error` — never `Console.WriteLine` or `Debug.WriteLine`.

**Threading**
- The Playnite SDK is **not thread-safe** for UI and view operations.
- Never modify UI objects from a background thread.
- Actual UI manipulation (dialogs, view navigation) must always be marshalled via `Application.Current.Dispatcher.Invoke(...)`.

**Database**
- Wrap bulk game updates in `PlayniteApi.Database.BufferedUpdate()` to avoid excessive change events.
- Games reference Platforms, Series, etc. by ID list — update through the correct collection (e.g., `api.Database.Platforms.Update()`), not by modifying the game's ID list alone.

**Settings**
- Settings are serialized to JSON automatically via `LoadPluginSettings<T>()` / `SavePluginSettings()`.
- Use `[DontSerialize]` (Playnite SDK attribute) to exclude non-persistent properties.
- Implement `BeginEdit`, `CancelEdit`, `EndEdit`, `VerifySettings` on the settings view model.

**Assembly references**
- Only reference `PlayniteSDK` NuGet package. Never reference `Playnite.dll` or `Playnite.Common.dll` directly — causes load failures.
- Avoid NuGet packages whose versions conflict with Playnite's bundled assemblies.

**Paths**
- Never hardcode Playnite-internal paths. Use `api.Paths.ConfigurationPath`, `api.Paths.ExtensionsDataPath`, etc. for portability between installed and portable Playnite.
- Temp files go to `Path.GetTempPath()`, not plugin config paths, to avoid permission issues.

**Lifecycle**
- Don't access `PlayniteApi.Database` before `OnApplicationStarted()` fires; data may not be ready.
- Clean up timers, file watchers, and background threads in `OnApplicationStopped()`.

**Playnite SDK type quirks**
- `Game.Playtime` and `Game.PlayCount` are `ulong` — cast explicitly to `long`/`int` when assigning to DTOs or doing arithmetic. No implicit conversion exists.
- `Game.CompletionStatusId` defaults to `Guid.Empty`, not `null`. Guard with `!= Guid.Empty` before using it. `game.CompletionStatus?.Name` is nullable — access safely.
- `Game.Platforms`, `Game.Genres`, `Game.Tags`, etc. are `List<T>` — check for `null` before iterating; they're not initialized to empty lists by default.
