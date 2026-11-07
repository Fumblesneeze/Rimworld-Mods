## Why

The installed RimWorld beta is 1.7.4984 on Waylith/.NET 10, while the development Gateway and isolated launchers assume Unity/Mono 1.6. The requested pathfinding experiment needs real, minimized 1.7 player workflows before any performance result can be trusted.

## What Changes

- Owner: repository verification tooling (`tools/RimWorldModding.Mcp`), including its dev-only Gateway runtime and launcher adapters. This change owns no product gameplay.
- Resolve the supported game version, executable, managed directory, and compilation target explicitly; reject unknown or incomplete installations before deployment or launch.
- Add opt-in 1.7 compilation and Gateway runtime adapters while preserving the 1.6 build path. Do not silently retarget unrelated mods.
- Carry runtime identity through typed operations, isolated launching, uploaded-code compilation, and evidence.
- Verify authenticated control, screenshots, native pawn commands, shutdown, and untouched normal configuration on the exact beta build.
- Keep the subsequent experimental pathfinder in its own mod-owned change. Giant-map generation and benchmark claims are not accepted by this tooling port.
- Follow-up: version-correct product package inspection and explicit isolated E2E prelaunch mod-settings fixtures, required by the first beta product. These do not authorize publication or change normal configuration.

## Capabilities

### New Capabilities

- `versioned-game-verification`: version-aware builds and isolated Gateway-backed native verification for the inspected 1.6 and 1.7 layouts.

### Modified Capabilities

None; existing changes remain unarchived and no canonical capabilities are present in `openspec/specs`.

## Impact

Affected code: repository SDK selection, opt-in game build imports, Gateway adapters, host compiled-code client, MCP operations, launcher internals, and focused host/live tests. Existing authentication, process ownership, package separation, cleanup and evidence contracts are unchanged. Gateway requires its pinned transport/compiler dependencies and installed Harmony where its inspected runtime needs it; no product gains a Gateway dependency. Circinus remains an optional profiling integration and is required only for later benchmark runs that claim its measurements. Preserving 1.6 source/build support is not a claim of fresh 1.6 live acceptance without a matching installation.
