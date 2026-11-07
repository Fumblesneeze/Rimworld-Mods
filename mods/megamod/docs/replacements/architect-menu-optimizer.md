# Architect Menu Optimizer

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin: **replacement**, S036, handwritten P1-28. Reference: **Architect Menu Optimizer** by
  Marukun, `MRK.architectmenuoptimizer`, [Workshop 3697920753](https://steamcommunity.com/sharedfiles/filedetails/?id=3697920753).
- Inspected 2026-11-03: selected 1.7 DLL version 1.0.0.0,
  SHA-256 `FE69A90623DD643C6424A9E5F296074B5EAFF1843407BF8313E93FB633041772`.
- [Research](../refinement/2026-11-03-architect-menu-optimizer-research.md) records read-only paths,
  all 39 runtime files, settings, public documentation, native/provider evidence and hashes.
  Source publication/license remains unknown; no upstream code, DLL or texture is copied.
- Status: **analyzed; specification drafted with gaps**. Nothing implemented or verified in game.

## Behavior inventory

All rows belong to the [Architect menu capability](../../../../openspec/changes/specify-megamod-architect-menu/specs/megamod-architect-menu/spec.md).
The requirement/scenario names below identify observable contracts, not implementation evidence.

| ID | Input/action → result | Defaults, timing and edges | Evidence / owning scenario | Status |
| --- | --- | --- | --- | --- |
| AM-01 | Open/redraw Architect → current ordinary commands with less redundant work | Optimization true; no changed costs/unlocks; measurable benefit required | Source cache/getter inventory; Compare optimized and ordinary browsing | Specified |
| AM-02 | Research, prerequisite, context or provider change → current eligibility/presentation | Next redraw, including paused; unknown reuse evaluates normally | Native Visible/material/label/color; A prerequisite disappears while paused | Specified |
| AM-03 | Select material/style then browse → native choice retained | Default suggestion remains native; no best-material rule | Native Designator_Build; Select a material then change pages | Specified |
| AM-04 | Browse commands → stable native complete groups/dropdowns | Group before paging, no split actions; orders/terrain included | Native grid/Command; A dropdown crosses a potential page boundary | Specified, deliberate source correction |
| AM-05 | Enable command pages → complete next/wrap browsing | Off; 48 units, range 12–120; configured upper bound, navigation extra | Source pagination; Page through 50 visible commands | Specified |
| AM-06 | Enable category pages → native buttons and usable search/style controls | Off; 20 buttons, range 6–40; disabled categories retained | Source/native category layout; Browse 21 categories | Specified |
| AM-07 | Search/accept off-page match → reveal, highlight and native action once | Entire collection; native dimming; manual paging not undone every redraw | Native search/force action; Activate a unique match on another page | Specified, deliberate extension |
| AM-08 | Navigate/reopen/change content → valid transient page state | Preserve tool, per-category pages; clamp/reset rules, no serialized references | Source page state/native tool flow; Return to a previously browsed category | Specified |
| AM-09 | Resize/reformat → fitting controls and matching hit areas | Reduce effective capacity without overwriting settings; ordinary fallback if no room | Layout gaps and native reference; A configured page is too large for the viewport | Specified correction |
| AM-10 | Edit common preferences → scoped effective configuration | Five rows, Use global, dormant overrides, disabled no-save controls | User settings policy; Configure from the main menu | Specified deviation |
| AM-11 | Use developer tools → temporary bypass/counts/refresh | Top only; off, no persistence; refresh preserves valid pages | Source SettingsUI/DebugOverlay; Refresh data and exit developer mode | Specified deviation |
| AM-12 | Provider/target unavailable → usable ordinary/provider surface | Atomic page surfaces, bounded warnings, no foreign-owner veto | Source adapter/native seams; A category-layout assumption changes | Specified correction |

## Interactions and composed result

- S032 material counts, S033 search and S021 hauling share authoritative map/material facts; this
  feature does not introduce another Thing scan or cache. Architect eligibility is still its own
  native decision. S034 number formatting participates directly in presentation/layout invalidation.
- S013 QualityBuilder, S028 Replace Stuff, future S038 Cherry Picker and S136 Floor Pipette Tool
  retain their separately specified construction/content/selection semantics. When owned, calls
  compose directly; no Harmony layer patches our other features. No Floor Pipette behavior is
  inferred from its name here.
- Native material suggestion is distinct from S026 bill ingredient preference. Paging cannot
  override research, remove physical costs or choose better materials on the player's behalf.
- External menu replacement retains its UI; no broad provider compatibility is claimed. Required
  startup warnings use the protected owned diagnostic path, even with Better Log suppression.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Ordinary global settings form with six cache switches and tuning ages | Configuration | Interface / Architect, five common global/save preference rows; tuning internals are not duplicated as normal settings |
| Experimental switches and debugMode | Diagnostics / optimization experiments | Top developer tools, temporary independent bypasses and counters; no ordinary overlay |
| Clear all caches and positive message | Diagnostic refresh | Top developer action, recompute data/counters while preserving valid pages and choices |
| Page-switch designator / category row | Ordinary contextual browsing | Existing Architect surfaces, not a new bottom tab; page control cannot designate |

Global/save preferences apply next redraw. Main-menu save/Use global/reset callbacks are disabled;
world view with a loaded game retains save scope. Global defaults and save inheritance reset
separately. Size editors also follow their own scope's pagination mode. Page/tool/category state is
transient browsing/gameplay state, not a setting. Developer session state is an explicit nonpersistent
exception because diagnostic bypasses must not silently affect ordinary games.

## Contracts consumed by other mods or saves

| Consumer/evidence | Contract | Preserve / unsupported / verification |
| --- | --- | --- |
| Better Architect public required-items list | References original Workshop provider | Not satisfied by megamod package; no spoof/drop-in claim |
| Optimizer outbound Better Architect adapter | Expects Settings property; supplied provider uses settings field and owns submenu | Reinspect exact active package, shape-guard narrow adapter; no source adapter parity claim |
| Installed bounded C#/XML/JSON scan | No external callable optimizer binding demonstrated | Binary/reflection consumers remain AM-Q2; no speculative helper facade |
| Source Settings global keys | Nineteen source fields, no save component | No automatic import; own five-row settings and independent save scope |
| Native command/category identities | Grouping, search, materials, activation and provider objects | Preserve through native calls; acceptance requires real menu/placement workflows |

Current source offers no gameplay Def/save migration requirement discovered in this audit. Removal
or feature disablement must leave ordinary menus available. Any future migration would need its own
backup/atomicity/restoration contract; none is promised here.

## Implementation and coverage

| Portion | Implemented | Deviation/conflict | Remaining evidence |
| --- | --- | --- | --- |
| Optimization, freshness and presentation | Nothing | No stale availability mode, blanket patch-owner veto or unrelated global-menu effects | Effective targets, deterministic tests, comparative workload and native results |
| Command/category paging | Nothing | Complete groups, full search routing, actual viewport fitting and atomic layout | Native action/result, small viewport, providers and page lifecycle |
| Settings/diagnostics | Nothing | Common save inheritance; top-only temporary debug controls | Seven languages, disabled callbacks, save/world/dev transitions |
| Source co-installation | Nothing | Suppress owned overlaps with warning; do not mutate original | Exact installed-source run and startup diagnostics |
| Optional providers | Nothing | Keep owned surfaces independent, unavailable submenu paging explained | Current Better Architect beta load/member shapes, Dubs and Scrollable Gizmos evidence |

Harmony becomes required when eagerly used. No source/provider runtime dependency is added merely
because it is a reference. No XML patch is currently proposed; any later XML uses required XML
Extensions, content/package guards and merge-preserving behavior. No benchmark or Gateway runtime
dependency may enter the package.

See [proposal](../../../../openspec/changes/specify-megamod-architect-menu/proposal.md),
[design](../../../../openspec/changes/specify-megamod-architect-menu/design.md),
[tasks](../../../../openspec/changes/specify-megamod-architect-menu/tasks.md) and
[progress/questions](../refinement/specification-progress.md). **AM-Q1–4 remain open.** Runtime
target documents, tests, build identity, performance evidence and causal live acceptance are all
future work; a validated draft does not establish any of them.
