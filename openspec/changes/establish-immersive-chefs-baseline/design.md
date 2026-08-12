## Context

The repository started empty. A June 11, 2026 prototype proved that RimWorld managed assemblies and Harmony can be exercised outside the game, but it used a hand-built `net8.0` harness and did not validate the purpose-built Zlepper SDK. A new isolated spike against RimWorld `1.6.4871 rev590` proved that `Zlepper.RimWorld.ModSdk/0.0.9` builds a `net480` mod and `Zlepper.RimWorld.ModSdk.Testing/0.0.9` runs NUnit tests after overriding its obsolete test adapter.

The installed Workshop library contains the requested frameworks and compatibility targets. Harmony is the only dependency needed to load the baseline. Processor Framework, Expanded Materials, Dubs Bad Hygiene, Gastronomy, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, Royalty, and material providers remain optional.

The owning mod is Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`. Generated distributable files live under ignored `artifacts/Mods/fumblesneeze.immersivechefs`; a deliberate deploy copies only that generated package to RimWorld's local `Mods` folder.

## Goals / Non-Goals

**Goals:**

- Produce a loadable RimWorld 1.6 package using the recovered and now-validated ModSdk family.
- Prove public mod behavior through fast tests that cannot silently pass with zero discovered tests.
- Keep optional integrations discoverable without compile-time references or assembly loading.
- Verify the generated package in a real RimWorld process with an isolated minimal mod list, FlaUI window evidence, and a dedicated log.
- Turn proven procedures into a concise repo-local RimWorld development skill.

**Non-Goals:**

- Implement cookware, dining items, cleaning jobs, appliances, prep, linked stations, meal state, expectations, or compatibility patches.
- Prove Unity lifecycle, map/job behavior, or optional integration APIs in the external test runner.
- Modify the user's normal RimWorld `ModsConfig.xml`, preferences, or saves.
- Design food waste or food preservation; both remain explicitly deferred gameplay topics.

## Decisions

### Use Zlepper ModSdk with explicit compatibility overrides

The production project uses `Zlepper.RimWorld.ModSdk/0.0.9` and its default `net480` target. The test project uses `Zlepper.RimWorld.ModSdk.Testing/0.0.9`, overrides `NUnit3TestAdapter` to `6.2.0`, and keeps the default `net480` runner for the baseline's pure catalog behavior.

Both `RimWorldPath` and `RimWorldManagedPath` are set explicitly. Zlepper computes its default managed path before a project-level RimWorld path override, so overriding only `RimWorldPath` leaves a stale `C:` reference. `SteamModContentFolder` is also explicit because this machine uses `F:`.

Alternative considered: copy the earlier manual multi-target project. It remains useful for future Harmony IL tests under `net8.0`, but using it alone would ignore the user's requested framework and duplicate ModSdk's metadata/reference/deploy behavior.

### Assert that tests were actually discovered

`scripts/Invoke-Tests.ps1` runs `dotnet test` with a TRX result, parses the result counters, and fails if executed tests are zero (including all-ignored selections). Its contract is:

- inputs: configuration, RimWorld path, Workshop path, and `table|json` output;
- exit `0`: at least one test ran and all passed;
- exit `1`: restore/build/test/runtime failure or failed test;
- exit `2`: a path or semantic argument rejected after PowerShell parameter binding.

Values rejected by a `ValidateSet` attribute never enter the script and therefore use PowerShell's own nonzero parameter-binding exit, normally `1`.

This guard is required because the Testing SDK's pinned adapter `4.2.1` crashes while enumerating installed .NET 10 yet `dotnet test` can still exit successfully with no tests.

Alternative considered: trust `dotnet test`'s exit code. The isolated spike demonstrated that this creates a false green.

### Keep the domain boundary independent of Verse

`IntegrationCatalog.Detect(IEnumerable<string>)` is the small public boundary. It normalizes loaded package IDs with `StringComparer.OrdinalIgnoreCase` and returns an immutable snapshot keyed by a fixed integration enum. It does not inspect Workshop files, use reflection, or resolve optional assemblies.

A thin RimWorld `Mod` entry point obtains `LoadedModManager.RunningModsListForReading`, invokes the catalog, calls one Harmony `PatchAll`, and writes the stable startup marker. Tests call the catalog through its public interface; they do not mock internal collaborators.

Alternative considered: one adapter class per optional mod in the baseline. There is no implemented integration behavior yet, so those types would be speculative and could accidentally create hard assembly dependencies.

### Separate source, generated package, and game deployment

Source lives under `mods/ImmersiveChefs`; ModSdk output lives under `artifacts/Mods`. `Directory.Build.targets` redirects ModSdk's automatic copy to an ignored artifact directory unless the caller passes `DeployToGame=true`. The smoke script is the documented path that opts into copying `fumblesneeze.immersivechefs` to the game's local `Mods` folder.

Alternative considered: commit generated DLLs and `About.xml`. Generated files are reproducible, noisy, and machine-version-specific, so the repository keeps source plus verification instead.

### Use an isolated RimWorld profile for in-game verification

`scripts/Invoke-RimWorldSmoke.ps1` refuses to start when another RimWorld process exists, builds with `DeployToGame=true`, creates a unique ignored artifact directory, and launches the game with:

- `-savedatafolder=<artifact>/SavedData`
- `-logFile <artifact>/Player.log`
- a generated `ModsConfig.xml` containing Harmony, Core, and Immersive Chefs only.

It records the exact launched PID, starts and connects FlaUI to that PID, verifies the process's native main-window handle/title, captures a screenshot, checks the dedicated log for the startup marker and mod-scoped errors, then disconnects and terminates only the recorded process in `finally`. RimWorld's Unity canvas can yield an empty FlaUI element/window tree, so PID-bound connection, the native window, screenshot, and log are the evidence set. The script never writes the normal LocalLow configuration.

Alternative considered: back up and replace the user's active `ModsConfig.xml`. An isolated profile provides stronger failure safety and better evidence isolation.

### Preserve dependency facts outside runtime code

The OpenSpec dependency contract is authoritative. Machine-specific inventories of currently installed Workshop metadata, assembly versions, and API/Def caveats are local audit artifacts and are not committed. Runtime code knows only canonical package IDs. This prevents downloaded-but-inactive or obsolete material mods from becoming accidental supported dependencies.

### Generate the skill only after verification

The skill-creator scaffold produces `.agents/skills/rimworld-mod-development` with a concise `SKILL.md`, agent metadata, and three focused references: Harmony/compatibility, Def/XML authoring and patching, and testing/in-game verification. It links to repository scripts instead of duplicating their implementations. `quick_validate.py` and a context-minimal forward test validate the result.

## Risks / Trade-offs

- **Zlepper 0.0.9 is old and pins stale test tooling** -> Pin the adapter override, parse TRX counters, document the exact framework limitation, and retain the proven manual `net8.0` pattern for future Harmony IL tests.
- **ModSdk defaults to writing into the live game Mods folder** -> Redirect normal builds in `Directory.Build.targets`; require explicit `DeployToGame=true` for the owned package.
- **A `net480` external runner cannot safely exercise every RimWorld 1.6 method** -> Test pure public mod behavior externally and reserve Unity/Harmony integration for the in-game tier or a future clean `net8.0` harness.
- **Unity UI exposes little accessibility structure** -> Treat process-bound window discovery plus screenshot and log markers as the baseline UI proof; do not claim element-level gameplay verification.
- **Package IDs vary in casing between metadata and `ModsConfig.xml`** -> Match with ordinal case-insensitive semantics and test mixed casing.
- **Processor Framework destroys inputs and creates a fixed Stuff-less output** -> Record it as a future custom adapter/patch decision; do not promise that its stock processor can preserve dirty dish identity or quality.
- **Optional mods update independently** -> Keep their assemblies out of the baseline and validate reflection/patch adapters against locally installed versions when those gameplay changes begin.

## Migration Plan

1. Add repository configuration, projects, metadata inputs, documentation, and ignored artifact paths.
2. Drive the compatibility catalog through RED/GREEN vertical tests.
3. Build and test without deploying to the game; assert non-zero test discovery.
4. Run the isolated smoke script, collect its screenshot/log evidence, and confirm the normal RimWorld configuration hash is unchanged.
5. Create and validate the repo-local RimWorld skill from those proven commands.

Rollback removes `F:\Steam\steamapps\common\RimWorld\Mods\fumblesneeze.immersivechefs`, which contains only this generated package. Source and ignored evidence remain recoverable in the repository; normal RimWorld user data requires no rollback because it is never modified.

## Open Questions

None block the baseline. Gameplay architecture questions, including dish identity through processors, station concurrency, save migration, and food waste, are captured in the separate planned gameplay change.
