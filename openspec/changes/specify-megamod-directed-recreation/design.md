# Directed recreation design

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.

## Context

P1-02 maps to `KT.ChillOut`, not an invented overlap with another source. Steam retrieval returned
HTTP 429; installed author documentation and the linked public repository were read before source.
The repository's packaged 1.7 DLL exactly matches the installed DLL hash. Both current source and
decompiled installed control flow were inspected; matching packaged binaries do not alone prove
every source file built that binary. [Report](../../../mods/megamod/docs/replacements/chill-the-fork-out.md).

## Goals / Non-Goals

Goals: explicit ordinary recreation orders, safe lifecycle, common settings, optional Odyssey,
compatible external activity providers, and no permanently altered schedule.

Non-goals: forced recreation for every possible joy giver, new recreational content, a cheat that
ignores boredom or physical eligibility, automatic leisure policy, or wholesale source-code reuse.

## Decisions

1. Prefer the 1.7 float-menu provider surface for contextual options. Discover building activities
   from `JoyGiver_InteractBuilding` contracts, not a hardcoded building list. Validate any reflected
   `TryGivePlayJob(Pawn, Thing)` shape and external provider before invoking it. Do not guess that
   every recreation mod uses this contract. Multiple activities on one target require CH-Q2.
2. Use native ordered-job paths and revalidate at action time. The source creates a job during menu
   construction, clears queued jobs and orders it with `SatisfyingNeeds`; swimming uses `StartJob`
   directly. Preserve intended visible ordering, not unsafe stale job closures or bypasses.
3. The source writes `756559690` to `Job.count` as a private marker and globally post-processes
   `Pawn_TimetableTracker.CurrentAssignment`. Do not copy this collision-prone quantity sentinel.
   Track owned explicit orders with lifecycle-safe state and choose the narrowest scheduling hook.
   Installed 1.7 `JoyUtility.JoyTickCheckEnd` checks `ignoreJoyTimeAssignment` before rejecting an
   assignment that disallows recreation; it also retains ordinary gain/skill gain and, for standard
   jobs, finishes or advances at recreation above 0.9999. This is a narrower investigation lead,
   not permission to set an ignore flag permanently (which would also bypass later Sleep).
   Inspect each activity's caller before choosing a hook; a getter replacement can also
   change schedule outlines from the preceding CM change. Our owned features must compose directly.
4. Settings have inconsistent source defaults: field initialization is 0.6, missing-key loading
   uses 0.5. Installed 1.7 `Mod.GetSettings` delegates to `LoadedModManager.ReadModSettings`: no
   settings file (or no loaded settings object) returns a new instance, preserving 60%; a loaded
   settings record missing `fckICE` receives the serialized 50% fallback. CH-Q1 is resolved by this
   source trace. Our proposed **60%** default is consistent across new and missing entries.
   Source settings persist globally under `fckICE`; use our common descriptive key, no implicit import.
5. Odyssey support must require active `ludeon.rimworld.odyssey` and consumed swimming shapes/Defs,
   not installation alone. Keep the adapter absent-safe. Building recreation must work without it.
   Source 10°C only selects the cold explanation when native outdoor suitability also rejects;
   it is not an unconditional swimming cutoff and does not reject indoor water.
6. Suppress our directed-recreation feature with one bounded startup explanation when the original
   `KT.ChillOut` is active. Never remove the original's patches or mutate its configuration.
7. This work requires behavior, not a static Def change. XML does not express the ordered-job
   lifecycle; no new XML dependency or separate cache is justified. Shared native Def lookup may
   be indexed only with an explicit lifecycle if measurements warrant it.

## Target specs to complete before code

- A schedule/cancellation patch must identify the exact condition preventing the ordered activity
  during Work/Anything, while retaining other assignments, ordinary jobs and cancellation.
  Method existence alone cannot establish this behavior; inspect effective semantics and competing
  prefixes/transpilers. Do not require a pristine method or exact assembly hash.
- Any reflection adapter must check its consumed signature and result/eligibility contract.
- Optional swimming registration and job construction form an isolated feature group. A missing
  DLC or invalid shape cannot initialize DLC-specific paths or disable unrelated building orders.
- Document persistence and job-end/load cleanup hooks with their minimal predicates. Each actual
  patch needs a stable target document, test cases and foundation startup diagnostic before use.

## Risks / Trade-offs

- Native activity restrictions vary by provider → specify only supported contracts and test actual
  representative games rather than broad “all recreation” compatibility.
- Source schedule override affects every reader of CurrentAssignment → prefer narrow integration
  and preserve stored schedule display, including the CM outline's intent.
- Joy fullness and tolerance termination are native rules → inspect exact 1.7 paths, retain ordinary
  completion and document the observed boundaries, not an invented fixed recreation duration.
- Clearing the queue is visible source behavior → retain it for these immediate orders; do not add
  queue-preserving semantics without a separate observable amendment.
- No explicit source license was found in the inspected repository → independently implement;
  source/assembly access does not grant permission to copy assets or code wholesale.

## Migration Plan

No runtime changes yet. Later add common settings, native menu orders, then isolated schedule/swim
integration. Newly loaded saves inherit defaults. Source settings are untouched; migration would
need its own backup, atomicity and recovery specification. Invalid persisted orders end safely.

## Open Questions

- **CH-Q1 (resolved):** no loaded settings object uses 60%; a loaded record missing the field uses
  50%. See decision 4 and retained exact-game source evidence. No user decision is needed.
- **CH-Q2 (research):** enumerate actual 1.7 building providers, multiple activities at one target,
  relevant external consumers and native forbidden/danger rules for ordered recreation.
- **CH-Q3 (research):** exact activity-specific stopping and save/load behavior for reading, building
  recreation and swimming; extend observable edge scenarios before their implementation.
- **CH-Q4 (composition):** validate current-assignment display versus actual activity alongside the
  CM schedule outline; it must not require one owned feature patching the other's code.

These are recorded work items, not questions blocking the next source's research.
