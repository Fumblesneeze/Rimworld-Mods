# Personal Bugfixes

A local collection of repairs for Fumblesneeze's own RimWorld games. Package ID: `fumblesneeze.personalbugfixes`. It is not intended for distribution and has no Workshop item or publication profile.

Requires RimWorld 1.6 and Harmony. Other mods are optional; load this mod after the ones it repairs. It adds no research, items or settings.

## Included repair

- [Exploration discovery list bounds](Fixes/ExplorationDiscoveryList.md): prevents a stale discovery list in Rimworld Exploration Mode from breaking pawn registration, including the baby-to-crib workflow observed with Toddlers.

## Defensive activation

Each fix checks its optional package, resolves the expected member signatures, and matches only the relevant IL fragment. Assembly IDs are diagnostic information, not compatibility hashes. The fix then reproduces the fault against disposable data using a copy of the actual target method. A passing original assertion leaves the method alone. Unknown code, conflicting patches or an inconclusive probe also leave it alone and emit a warning.

After applying a fix, startup verifies the installed IL and reruns the behavioral assertion against the patched copy. Failed postconditions remove only that fix's Harmony owner. Every fix logs `Absent`, `NotRequired`, `Incompatible`, `Applied` or `Failed` with its target and decision stage under `[Personal Bugfixes]`. Check warnings after game/mod updates. These checks describe startup state; they do not monitor patches installed later.

## Development

On a first checkout, E2E discovery needs an installed package. Use typed `game_run_start` with `packageIds: ["brrainz.harmony", "fumblesneeze.personalbugfixes"]`, `projectPaths: ["mods/PersonalBugfixes/PersonalBugfixes.csproj"]` and audio disabled for an initial disposable absent-target run. Close its exact lease through `run_cancel` before the E2E runs. This builds/deploys only the local package; it does not enable the mod in the normal game configuration.

Repository-local build: `dotnet build mods/PersonalBugfixes/PersonalBugfixes.csproj -c Release`. Use the repository's typed `test_run` with suite `PersonalBugfixes.Unit` or `PersonalBugfixes.Harmony`. The isolated `e2e_run_start` operation discovers the owning project `tests/PersonalBugfixes.EndToEndTests/PersonalBugfixes.EndToEndTests.csproj`; its exact tests are `personal-bugfixes.absent-target` and `personal-bugfixes.exploration-childcare`. The runner builds/deploys into the canonical local package-ID folder, uses disposable save/configuration data, and retains evidence. It requires the installed optional mods for the childcare test. No release/publication operation is configured for this personal mod.

New fixes implement `IPersonalFix`, have a distinct owner and startup registration, and ship an author-facing Markdown report under `Fixes/`. Add focused tests for supported failure, upstream repair, structural drift, inconclusive probes, installed-change verification and rollback. A host test is supporting evidence; acceptance requires a reviewed-build native player workflow and personally inspected screenshots in an isolated game.

Removing this mod leaves Exploration's ordinary saved discovery flags intact. It does not repair feature deletion/reordering, null/uninitialized lists, or unrelated childcare errors.
