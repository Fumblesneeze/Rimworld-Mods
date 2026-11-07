## Context

Owner: repository verification tooling (`tools/RimWorldModding.Mcp`). Installed 1.7.4984 rev1242 uses `RimWorld.exe`, root-level `Assembly-CSharp.dll` / `UnityStubs.dll`, and `net10.0` Waylith. Existing launchers assume `RimWorldWin64.exe`, `RimWorldWin64_Data/Managed`, and `net480`. The existing Zlepper SDK glob would import platform runtime DLLs if pointed blindly at the new root.

## Goals / Non-Goals

Goals: retain versioned build/launch selection, authenticated bounded Gateway control, real minimized screenshots and native command verification, exact process/build provenance and cleanup. Expose recurring behavior through the existing typed operation registry.

Non-goals: migrate unrelated products, change save data, download a replacement game, broaden optional compatibility claims, implement the pathfinding algorithm in the tooling port, or claim all Gateway scenarios work merely because startup succeeds.

## Decisions

Generated-map cleanup enumerates held contents as well as spawned things. It refuses before destroying a new holder if that holder contains any surviving baseline object; fixtures must restore such transfers explicitly. New items held by baseline pawns remain cleanup candidates.

- Select runtime from bounded `Version.txt` plus required file shape, never from a convenient surviving DLL alone. 1.6 selects Unity/Mono/net480; 1.7 selects Waylith/net10.0. Unknown versions and incomplete layouts fail closed. Hashes identify builds, not API compatibility.
- Upgrade the repository compiler SDK to installed stable .NET 10 while leaving project target frameworks unchanged unless explicitly opted in. A newer compiler can build the existing net480/net8 host contracts; global `TargetFramework` command-line overrides are rejected as an approach because they corrupt project-reference target selection.
- Give opted-in game projects explicit runtime-specific imports and a restricted game reference set. Never ship platform/game/Harmony-provider DLLs. Keep shared host-safe contracts independently targeted.
- Keep unchanged native APIs direct. Introduce small version-specific adapters only where compiler inspection or live behavior demonstrates drift. Prefer an existing native rendering/control path to simulated gameplay or desktop input.
- Preserve the 1.6 REPL/compiler route. If Mono.CSharp cannot execute on Waylith, use an explicitly versioned compiler backend with bounded inputs and the same authentication/dispatch/cancellation semantics; do not pretend a disabled evaluator passed smoke.
- Reuse the existing lease, configuration isolation, and artifact lifecycle. Runtime selection must feed builds, upload compiler, launch executable, test folders and identity evidence consistently.

## Risks / Trade-offs

- Beta API churn -> compile against exact installed assemblies and fail on unsupported native shapes; record version and hashes.
- Waylith screenshot/minimized loop differences -> test the actual rendered view and ordinary time advancement before product work.
- SDK/reference drift -> focused pure host tests for both layout choices plus real selected-runtime compile; no 1.6 live claim without its installation.
- Broad existing Gateway surface -> port compiler blockers and required verification path first, keep unexercised capabilities explicitly unaccepted.
- Profiling adapter may be unavailable on 1.7 -> inspect its actual installed shape and record a blocker instead of substituting synthetic pathfinder timing for native benchmark results.

## Migration Plan

Capture host RED/GREEN per slice; compile repository-local first; review scoped changes; deploy only the dev Gateway through the existing launcher; launch a fresh leased minimized process. Capture before/action/after native pawn/control observations and shutdown evidence. Product implementations follow in a separate owned change. Reverting opt-in imports leaves existing 1.6 target defaults intact; no normal user configuration migration is performed.

## Compiler port evidence

The focused .NET 10 evaluator RED throws MissingMethodException for the removed AppDomain.DefineDynamicAssembly overload. 1.7 therefore requires Microsoft.CodeAnalysis.CSharp.Scripting 4.11.0; 1.6 retains Mono.CSharp 4.0.0.143. A concrete separate `RimWorldDevGateway.Waylith.Unit` process runs evaluator contracts on .NET 10 with real game type metadata but no initialized game, active mods, Harmony installation or populated Def database. It is not live acceptance.

## Open Questions

Exact Waylith capture and evaluator behavior will be established from compilation and live probes. No compatibility claim precedes those observations.

## First-product follow-up

The requested large-map building session needs an explicit user-owned lifetime. Add optional
`prelaunchConfigDirectory` and `userPlaytest` arguments to `game_run_start`, reusing existing
fixture validation/copy and the existing lease/launcher. Default verification deadlines remain
unchanged. In user-playtest mode the hold ends only on actual user exit or explicit cancellation;
the launcher stays alive to sanitize credentials and retain normal-config/cleanup evidence.
It never deletes saves. A pure shared hold-decision helper is exercised by focused host tests.
No automatic background playtest may opt in without a user request. Product acceptance remains
a separate fresh disposable run; a user-owned session is not relabelled as accepted E2E evidence.

Large-map testing explicitly opts into a single-test generated-map isolation contract through a shared marker interface. The execution machine rejects mixed groups. The backend records baseline object/zone/designation identity sets, preserves natural roofs and existing environment, and removes only newly created disposable objects during cleanup. Native fixture-owned narrow terrain/roof edits are restored by deferred cleanup. No whole-map reset and no normal save reuse are allowed in that policy. Existing default empty-map isolation is unchanged. This avoids the observed quadratic engine deregistration cost when deleting hundreds of thousands of plants before measurement.

Expose the existing launcher's prelaunch XML-mod-settings fixture through `e2e_run_start` as an optional repository-artifacts-contained directory. Preserve its strict filename/XML/symlink bounds, forbid normal ModsConfig/Prefs replacement, and retain copied hashes in each disposable run. Dry-run reports the argument without launching or copying. The selected game layout must also govern package inspection: retain the pinned 1.6 assembly inventory; use an exact inspected 1.7 platform-name inventory, never wildcard System/Unity prefixes or arbitrary installed DLLs. No publication support is claimed by this read-only package check.
