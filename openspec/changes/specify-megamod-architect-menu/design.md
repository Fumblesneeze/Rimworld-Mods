## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
S036 Architect Menu Optimizer is a reference only. The [research inventory](../../../mods/megamod/docs/refinement/2026-11-03-architect-menu-optimizer-research.md)
records every runtime source file, settings/defaults, native reference bodies, provider evidence and
hashes. This change is documentation only; source inspection does not establish effective patch
semantics, measured speedup or native acceptance.

## Goals / Non-Goals

**Goals:** responsive native Architect with current choices, independent optional pages, complete
command groups, accessible search/layout, common settings and bounded diagnostics. Optimize only
where semantic reuse is justified and measure the resulting cost.

**Non-goals:** new building content or unlocks, material-quality selection, global command-menu
rewrites, a replacement bottom tab, source package/assembly emulation, settings migration, arbitrary
provider support, foreign patch removal or a speculative general cache framework.

## Decisions

### 1. Native behavior is the result contract

Retain native category identity, game designator restrictions, stable sorting, grouping/merging,
dropdown membership, material/style choice, disabled commands, placement and activation. Existing
native listers and native material-selection rules remain authoritative. A timer is not proof that
availability or presentation is current: paused research/prerequisite/provider changes must be
visible on the next redraw, or that path must evaluate normally.

Use concrete derived data owned by the Architect feature rather than reproducing a cache per source
class. Start with the smallest measured repeated work. Native evaluation is a valid fallback for
stateful PlaceWorkers, foreign command methods, unknown label/grouping dependencies or unavailable
invalidation. Reuse must key the dependencies actually consumed: map/session and Def generations,
research/prerequisites/difficulty/discovery/DLC state where applicable, material choice, ideology,
language, formatting, width/scale/font/wrapping and provider context. Do not retain live map objects
after teardown, serialize UI caches or access Verse/Unity objects off-thread.

Alternatives rejected: source 120/204-tick expiry as sole correctness rules, 60-frame color expiry
as proof of identity, and amortized stale availability as an ordinary option. Batching or expiry
may be internal strategies only when they still meet the observable freshness contract. Do not
promise all eight diagnostic domains will be cacheable on every target; unavailable domains report
why and use ordinary calculation.

### 2. Share material facts, not Architect-specific decisions

Compose directly with the shared map-owned Thing-data/native-resource view already specified for
S021 hauling, S032 material counts and S033 Ctrl F. Inspect native ResourceCounter/listers first;
do not create another global Thing enumeration or reservation/reachability cache for this source.
Native authoritative count refresh and more immediate native deltas are distinct from a stale owned
snapshot; retain the shared view's documented freshness rather than inventing resources.

Material suggestions remain a UI consumer using native rules; no Use Best Materials bill policy is
applied here. Explicit Stuff selection remains command state. S034 formatting is composed directly,
with layout invalidation when representation changes; diagnostics remain excluded. Text-measurement
memoization is UI data, not a second Thing index. Scope any generic game rendering hook to an admitted
Architect draw context and restore that context in guaranteed cleanup. Source-wide GizmoGrid and
Command rendering effects on unrelated windows are not part of this feature.

### 3. Form complete command units before paging

Native grouping precedes pagination. One unit is a visible command or complete combined command,
with member identity and activation semantics intact. Do not flatten dropdown children, discard
orders/terrain tools or use only the first member to prove eligibility. Preserve stable equal-order
ties. A future grouping optimization must account for dynamic merge/group semantics; otherwise use
native grouping on the full set before taking pages. Source raw-designator slicing was rejected
because it can split combined commands.

Keep command-page state per category and category-page state per current map browsing session.
It is transient, not settings or saved gameplay state. Query edits and explicit unique-match
activation are command-page reveal events; redraw is not. Explicit category opening reveals the
category button's page but restores that category's valid remembered command page, both with no
query and with an unchanged active query. Search still evaluates the complete native
collections and uses native category choice/highlighting. Reveal the first visible matching unit
in that chosen category, with first-page fallback if none is visible. A unique actionable match
reveals and activates exactly once through native action flow. Manual navigation thereafter stays
put until a new reveal event; never unlock hidden native matches.

Clamping after content change preserves the numeric page where valid, otherwise the last valid page.
Mode/page-size edits reset the corresponding pages. Map/game changes discard all page/context state.
Closing/reopening preserves valid pages but allows native search reset. Page actions must not cancel
the selected tool, change its material/style/rotation, re-run activation or place a blueprint.

### 4. Category content, navigation and height form one admitted surface

Keep native ordering, disabled category buttons and preferred-column placement. Compute page rows,
navigation, search/style areas and actual window height together, not an independent WinHeight
prefix that assumes another transpiler succeeded. A missing semantic anchor disables the entire
owned category paging surface and leaves the original stream/window intact.

Both configured page sizes are upper bounds. Measure available layout after reserving navigation
and native controls, use a positive fitting effective capacity, then derive pages consistently.
Clamp after capacity changes. If even one unit plus navigation cannot fit, disable that surface
with a visible reason and retain ordinary layout. Do not overwrite saved sizes or silently drop
units. Avoid copying source fixed 26/32/28-pixel formulas into an assumed provider layout. Exact
native coordinate seams, preferred-column gaps and tiny viewport behavior remain AM-Q1/Q3 research.

### 5. Five normal preferences, ephemeral developer controls

Register Optimize architect menu (true), Command pages (false), Commands per page (48, 12–120),
Category pages (false), Categories per page (20, 6–40) in the common Interface / Architect section.
Each row uses shared global/save load/store/resolve/UI, dormant overrides, validation and reset
semantics. Preserve invalid-input last values. Size editor availability follows the pagination mode
for that scope, plus inheritance for save editors. Use global remains available for a loaded save
even while its page mode is off; no-save callbacks and controls are disabled, including bulk reset.

Source independent optimization switches become temporary top-debug bypasses, not nineteen copied
settings. Developer state is off initially, does not persist and resets when developer mode ends or
the game exits. Diagnostics identify admitted/bypassed/unavailable domains, show counts without
claiming speedup and offer bounded refresh. Refresh clears derived data and counters, preserving
valid pages/preferences/tool state; data refresh and navigation reset are separate operations.
No unrestricted diagnostic overlay survives into normal play. Provide all seven baseline languages.

### 6. Package-gated optional adapters with semantic checks

Original `MRK.architectmenuoptimizer` co-installation suppresses owned overlapping enhancements as
a group. Do not change its settings, remove its patches or impersonate its package. Explain the
unavailable feature and startup fallback through the foundation diagnostic path, including when
owned ordinary logging suppression is configured.

For `ferny.BetterArchitect`, the inspected optimizer expects property Settings while the supplied
provider has static field settings. Do not port that adapter unchanged. Inspect the exact active
provider and consume only validated member shapes/lifecycle. The supplied reference is under 1.6
with only a 1.6 LoadFolders entry; its advertised 1.7 label alone does not prove beta selection.
Provider submenu ownership means command pagination is unavailable without an explicit adapter;
category pagination/other optimizations are independent where separately admitted. Dubs Mint Menus
and Scrollable Gizmos remain future verification boundaries, not inherited compatibility claims.

No demonstrated external callable optimizer consumer was found in the bounded text scan. Better
Architect's public required-item relationship still prevents claiming a package drop-in. Resolve
real binary/reflection consumers before adding any facade; preserve no speculative API surface.

### 7. Candidate target families become per-patch documents before implementation

The table is an investigation map, not completed admission. Each actual patch needs its own stable
target document, scenario, executable predicate, ordering/repatch policy, tests and eventual live
evidence. No unguarded PatchAll or blanket rejection of foreign owners is allowed.

| Candidate family | Minimum semantics to establish / fallback |
| --- | --- |
| Availability, label, color, material getter interception | Consumed callable/result shape, effective original/skip behavior and all reused dependencies; otherwise normal result evaluation |
| Native grouping/order and width/text seams | Exact incoming sort/group/render data flow, cardinality and context; compatible foreign changes allowed; unknown stateful command behavior evaluated normally |
| Command pagination/search activation | Full resolved collection, complete grouping, native visibility/actionability, one-shot activation and separate navigation; fall back as an atomic command-page surface |
| Category pagination/layout/search | Coupled content/row/window/navigation semantics and preferred columns; validate incoming streams, rollback newly installed owned group patches on failure |
| Lifecycle/invalidation callbacks | Existence/shape may suffice for pure observation, but prove the event covers the dependency and execution is not skipped; ordinary evaluation when current state cannot be known |
| Optional provider | Canonical active package plus consumed type/member/Def identity, static/instance shape and lifecycle; disable only adapter/surface on drift |

Every transpiler validates its actual incoming stream and returns it unchanged on failure. Do not
run foreign transpilers speculatively. False/unknown predicates, exceptions and installation failures
produce one bounded startup warning per skipped enabled patch, identifying target/spec/reason and
fallback; explicit user disablement is not a failed patch. Repatch invalidation must disable or
revalidate affected owned work without modifying foreign patches. No XML change is currently needed;
any later declarative addition follows XML Extensions content/package/merge rules and dependency.

### 8. Verify correctness and responsiveness separately

Host unit tests cover deterministic page partitions/clamps/reveal events, setting resolution and
cache-key/lifetime decisions. Only explicitly owned patches belong in Harmony tests, with cleanup;
native loaded Defs/providers and menu actions require a fresh isolated game. No synthetic loaded
mod or replayed foreign patch stack substitutes for the appropriate environment.

Use the repository performance skill and Circinus conventions when implementation exists. Inspect
the typed benchmark surface before adding orchestration. Measure actual native opening/redraw,
search, page movement, material selection, scale/language changes and map/provider invalidation,
paused and running, with small and large catalogs. Include cold/warm cost, updates, allocations and
retained memory, and combined material-count/search/hauling consumers. Separate attributable samples
from independent whole-workload comparisons; no arbitrary paired subtraction or cache-hit-as-speed
claim. No numeric speedup threshold is invented without a baseline. Profilers/Gateway remain
verification-only dependencies outside the product package.

Final reviewed-build acceptance uses native clicks/typing/placement and personally inspected causal
captures: off-page search, full grouped actions, paused prerequisite changes, material persistence,
category controls at small viewports, settings/save/world/main-menu transitions, developer on/off
and each admitted provider. Direct mutation may prepare a fixture but cannot demonstrate a player
result. No live run is required to accept this prose-only draft; all gameplay tasks remain open.

## Risks / Trade-offs

- Unknown command dependencies → ordinary calculation; correctness may reduce available speedup.
- Group-before-page can cost more than source slicing → preserve actions, then measure safe reuse.
- Foreign replacement UI may bypass native targets → separately admitted adapter or usable fallback.
- Large page preferences can exceed viewport → effective fitting capacity without preference mutation.
- Beta engine and provider drift → exact evidence, semantic checks and causal native re-verification.
- Source license unknown → independent implementation; no source/art/binary redistribution.

## Migration Plan

No migration or deployment occurs here. Future first settings entries use owned stable IDs; missing
save entries inherit. Never import source configuration automatically or persist live menu objects.
Disabling/removing the feature restores ordinary menus; unsupported groups fall back atomically.
Product packages must not contain source binaries, profilers, Gateway or another repository mod.

## Open Questions

AM-Q1–4 are recorded in the [sequential ledger](../../../mods/megamod/docs/refinement/specification-progress.md):
effective native admission, provider/consumer/license evidence, shared-data/layout/performance work,
and future implementation/acceptance. They are research tasks for later, not questions asking the
user to explain the source mod.
