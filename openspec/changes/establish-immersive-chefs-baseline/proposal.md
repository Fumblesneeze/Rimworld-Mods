## Why

Immersive Chefs needs a reproducible RimWorld 1.6 foundation before its interdependent cooking, dining, hygiene, and compatibility systems can be implemented safely. This change establishes that foundation against the game and Workshop mods installed on this machine, while preserving the full gameplay vision in a separate planned OpenSpec change.

## What Changes

- Create a repository-level OpenSpec workspace and keep all RimWorld mods below `mods/`.
- Add the loadable `mods/ImmersiveChefs` mod for RimWorld 1.6 with Harmony and XML Extensions as its hard mod dependencies.
- Use the recovered RimWorld ModSdk/testing approach where it is compatible with the current game; preserve an explicit modern-.NET test runner where the framework cannot load current RimWorld assemblies reliably.
- Add a small public compatibility boundary that detects supported optional mods by package ID without hard assembly references.
- Add repeatable build, test, deploy, minimal-mod-list launch, log inspection, and desktop smoke-verification commands.
- Distill the verified workflow into a repo-local `rimworld-mod-development` skill covering C#/Harmony compatibility, Def/XML authoring and patching, tests, and in-game verification.
- Inventory locally downloaded integrations and record exact package IDs, versions, load-order expectations, and confidence in OpenSpec artifacts.
- Install the OpenSpec, TDD, code-review, and FlaUI workflows repo-locally.
- Create a second, planned OpenSpec change containing the complete Immersive Chefs gameplay and settings contract; gameplay systems themselves are intentionally out of scope for this baseline.

## Capabilities

### New Capabilities

- `mod-foundation`: Owns the repository layout, RimWorld metadata, build output, startup behavior, required Harmony/XML Extensions dependencies, and optional-mod discovery for `mods/ImmersiveChefs`.
- `developer-verification`: Owns out-of-process tests, a reversible minimal-mod-list RimWorld launch and FlaUI/log smoke check, and the repo-local development/verification skill for `mods/ImmersiveChefs`.

### Modified Capabilities

None. This is a new repository.

## Impact

- Adds root OpenSpec and Codex workflow files.
- Adds `mods/ImmersiveChefs`, its C# projects, game metadata, scripts, and tests.
- Reads the installed RimWorld game at `F:\Steam\steamapps\common\RimWorld` and downloaded Workshop metadata at `F:\Steam\steamapps\workshop\content\294100`.
- Uses an isolated RimWorld `-savedatafolder` for smoke verification and never modifies the user's normal mod configuration or saves.
- Does not add hard references to optional integration assemblies and does not implement cooking gameplay in this change.

## Affected Mods

- **Immersive Chefs** (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`: new loadable/testable baseline only; cooking gameplay is specified separately for later implementation.
