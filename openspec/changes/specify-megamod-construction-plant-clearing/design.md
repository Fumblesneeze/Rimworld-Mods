## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S063 replaces `Mlie.CutPlantsBeforeBuilding` behavior with independently engineered owned code.
Read the [proposal](proposal.md), [observable contract](specs/megamod-construction-plant-clearing/spec.md)
and [research](../../../mods/megamod/docs/refinement/2026-11-04-cut-plants-before-building-research.md).
This design is not implementation, a target-admission proof or native acceptance.

The selected installed/public 1.7 DLL agrees by hash. Twelve runtime source files and their compiled
counterparts expose four placement hooks, a play-settings hook and a global willingness hook.
Native 1.7 distinguishes accepted construction from sampling/tutorial rejection, actual roof-area
additions from stale intermediate cells, harvest from removal, and ordinary work from extraction.
The source erases all existing designations and stores its operational switch process-wide. Those
mechanisms are not requirements to retain.

## Goals / Non-Goals

**Goals:** accepted-site-only advance orders, current physical obstruction semantics, explicit order
precedence and lifecycle, source-like real labor and options, correct map context, common settings,
and direct composition with owned work. Preserve proven external contracts where practical.

**Non-Goals:** a permanent construction-parent planner, periodic whole-map scanning, automatic
clearance of every newly grown plant, custom plant yields, extra tree-sowing permission, a forestry
management tab, automatically canceling source jobs, importing source configuration, or claiming
all third-party map providers work. No new raster asset is selected in this documentation change.

## Decisions

### Observe committed work on its owning map

Use a small owned plant-order policy called directly by owned placement/replacement workflows and
by admitted native placement integrations. Inputs are the newly accepted work's real map, full
rotated footprint or newly accepted roof cells, and current plant facts. Capture context before
selection or designator state can change, then prove an actual new accepted plan/area transition.
Merely finding an old blueprint at the same location does not prove the callback succeeded.
Do not infer the target map from `Find.CurrentMap` when the work already supplies it.

Candidate native entry points are `Designator_Build.DesignateSingleCell`,
`Designator_Install.DesignateSingleCell`, manual BuildRoof area changes and
`AutoBuildRoofAreaSetter.TryGenerateAreaNow`. Prefer the narrowest proven committed transition over
duplicating each source postfix. Native `innerCells` can be stale on early returns and includes
cells not roofed by that call; `justRoofedCells` also needs an actual successful-call/delta contract,
not blind trust in another reusable buffer. A provider prefix that skips placement is not success.
Eyedropper/Floor Pipette selection, previews, forbidden placement, canceled gestures and instant
construction must produce no advance orders. A real installation plan is eligible independently
of the installed object's original `WorkToBuild` value or developer-mode state.
For S028 hidden-cell planning, use its existing full-footprint reveal/revalidation lifecycle before
the deferred first plant decision. Do not inspect/report hidden plants or start clearing based on
fogged contents. This is a direct call from the admitted deferred placement, not a new parent tracker
or periodic old-plan rescan. Use current settings and reject canceled/invalidated plans. Unrevealed
roof cells likewise cannot expose hidden plant orders/notices.

### Reuse native obstruction and real work

Native `GenConstruct.BlocksConstruction`/`FirstBlockingThing` and roof blocking semantics are the
starting point for actual obstruction. Do not carry over the source's `harvestWork >= 200` proxy.
Native 1.7 construction uses a strict comparison to dandelion work and a terrain-under-grower
exception; roof obstruction has its own flag. These are adapter facts, not new hard-coded balance
constants. Inspect all actual plant obstructions in the affected cells, not only `GetPlant`'s first
result. Unknown provider semantics fail closed at that provider boundary.

Order selection: preserve an existing explicit cut/harvest/extract choice; otherwise skip Super
trees and admitted provider-protected plants, prefer available extraction if configured, then
harvest only when the plant is currently harvestable and harvesting removes it, otherwise cut.
An immature tree with HarvestDestroys is not sufficient: native harvest work rejects it while
HarvestableNow is false. Native cutting already handles
ordinary yield; do not harvest a perennial and cut it again just to produce the same yield twice.
Tree extraction creates the actual minified tree, whose subsequent hauling and installation use
ordinary systems. Do not import tree-sowing skill/research requirements into extraction merely
because those values appear in TreeBase. No extra work/yield multipliers or guaranteed outputs.

Free-standing native designations persist after a plan is canceled or the switch is turned off,
matching the source's simple lifecycle. State this in the UI; a player removes unwanted plant
orders explicitly. Repeated placement does not duplicate or reset them. No parent links, automatic
undo, retry queue or permanent cache is needed. Automatic additions must not call blanket
`RemoveAllDesignationsOn`. Native completion can perform its normal designation cleanup and plant
consequences; that is distinct from our placement-time mutation.

### Narrow the worker restriction and protect existing orders

Keep the configurable worker restriction independent of the advance-order toggle, but apply it
only to incidental plant clearing for construction, installation and roof building. Use the actual
worker's Plant Cutting assignment when it exists; missing work settings preserve ordinary worker
eligibility. The restriction never grants capability, overrides ideology, permits an unreachable
target, changes Growing harvest/sow eligibility, or adds an implicit forced-order exception.

Do not patch the global `PawnWillingToCutPlant_Job` as the source does: its native callers include
grower harvest/sowing and explicit cutting. Integrate the scoped decision with construction and
roof blocker selection and revalidate before incidental work begins. Honor active explicit plant
orders, especially extraction, before choosing a competing incidental cut. Native construction
already avoids extraction-marked trees; native roof clearing does not, so roof composition needs
its own admitted guard. No qualified worker means visible waiting, not silent permission changes.

### Operational state and common configuration

One per-game operational boolean applies across that game's maps and persists through save/load.
The corner control and map-view shortcut are its gameplay controls. Hide/disable them outside a
usable colony-map view, including world view; never consume the binding while text entry owns it.
Use a uniquely owned rebindable KeyBindingDef, default Backslash. Binding scope is native global
input configuration. Do not copy the source `TMB_` identity or source texture without a proven need.

Use the common settings load/resolve/render/store path:

| Preference | Built-in default | Scope and effect |
| --- | --- | --- |
| Start advance clearing enabled | true | Global-only initialization preference for new/missing per-game operational state; changing it never flips an existing game's state |
| Prefer digging up eligible trees | false | Global default + per-save override; next new automatic order |
| Restrict incidental clearing to Plant Cutting workers | true | Global default + per-save override; next incidental job selection and start |
| Show skipped special-tree notices | true | Global default + per-save override; subsequent placement notices |

The global-only initializer is an explicit settings-model exception: the current game's choice is
already the saved operational control, and a second save-local initialization preference cannot
initialize an already initialized game. Label it accordingly. The other rows use Use global,
Current save and Global default, preserve dormant overrides and disable save callbacks at the main
menu. Do not cancel or reset a job already physically underway merely because a preference changes.
No migration from the source's process-static toggle or `DefaultMode/OnlyCutters/DigUp/NoInfo` keys.

### Patch admission and failure groups

Before runtime implementation, create stable target documents from the mod template for every
actual Harmony/XML patch. The following are design boundaries, not existing validators:

| Group | Minimal consumed semantics | On failed admission |
| --- | --- | --- |
| Advance orders | Committed build/install/roof transition, target map/footprint, obstruction and order meanings, operational state/control wiring | Disable owned advance ordering and its control with a reason; retain ordinary game/foreign orders |
| Incidental worker/order policy | Actual construction/roof blocker path, worker identity, explicit-order checks and eligibility before work | Disable that owned restriction/precedence integration, explain fallback; leave admitted advance ordering usable |
| Optional map adapter | Canonical provider active, actual target map/level and coordinates, exact consumed members | Disable owned feature paths on unsupported provider maps, not unrelated native maps |

Admission checks effective required behavior, not exact hashes, offsets, all-body equality or the
absence of other Harmony owners. If a prefix/transpiler can skip/remove the required action, verify
the effective transition; a signature alone is insufficient. Any transpiler validates its actual
incoming stream on every run and returns it untouched on failure. Do not invoke foreign
transpilers repeatedly to speculate about compatibility. Revalidation after supported later patch
changes uses the same predicates. Initialization is idempotent; failed installation rolls back
only newly owned patches in its declared group. A skipped enabled patch produces one bounded
startup warning naming patch/spec/target, predicate, missing behavior and fallback. No tick spam.

Original `tammybee.cutplantsbeforebuilding` or continued `Mlie.CutPlantsBeforeBuilding` active means
the owned overlapping feature is disabled with a clear reason. Do not modify foreign settings,
unpatch foreign owners, remove native orders, or spoof those package identities. Compatibility
corrections in this spec do not promise to constrain a running source provider's behavior.

### Actual consumer and shared systems

The installed 1.6 Vehicle Map Framework (`OELS.VehicleMapFramework`, Workshop3426502333) checks
the continued package ID and patches `CutPlantsBeforeBuilding.Util.DesignatePlants` to replace its
`ref Map map` with the focused vehicle's current level. Its installed About/LoadFolders advertises
1.5/1.6, not 1.7. Recreating the internal method without the expected package would not activate
that consumer. No facade/spoof is selected. Correct explicit map propagation is owned behavior;
any future vehicle adapter needs a supported provider version and separate native acceptance.

S006/S008 retain ordinary growing permissions and manual plant orders. S013/S028 call the same
accepted-work policy when their own authorized cycle/replacement creates new work; retry budgets,
old buildings, supplied resources and contents remain unchanged. S058 work priorities select
among eligible jobs; S060 completion access and S061 roof-removal safety remain later independent
checks. Roof-removal areas are not roof-building additions. S136 sampling supplies no build order.
Use native thing/designation/area stores; any genuinely shared derived facts belong to the one
map index also used by search/hauling. No separate scanning worker, stale permission cache or
off-thread Verse access. Benchmark combined consumers before claiming optimization.

## Risks / Trade-offs

- Native obstruction and harvesting semantics can change in beta → minimal shape/semantic checks,
  updated provenance and actual action/result tests, not a version allowlist.
- Existing explicit harvest can leave a perennial in place → preserve that player choice; explain
  remaining construction obstruction and allow ordinary follow-up/manual cutting. Do not rewrite it.
- Special-tree exclusion is only advance-order policy → do not advertise general tree immunity;
  ordinary manual/construction behavior and other providers remain separate.
- Cancellation retains free-standing plant orders → state the consequence visibly; avoid a hidden
  parent planner or automatic removal of orders a player may still want.
- Scoped OnlyCutters and map-only shortcut intentionally differ from source → record deviations
  and test growers, forced construction, world view, save switches and provider coexistence.

## Migration Plan

No runtime deployment or automatic import in this draft. Implement one vertical behavior at a time
with honest RED/GREEN, package checks and independent review, then verify native workflows in a
fresh minimized isolated game on the reviewed build. Keep source providers read-only and absent
from the primary acceptance run; use explicit separate provider-present runs for compatibility.
Rollback of owned integration must retain ordinary native plant orders and not mutate another save.
Safe mid-save removal or migration of source configuration is not claimed.

## Later composition with automatic blight cutting

S069 automatic blight cutting composes directly with existing ExtractTree orders: it adds no competing
cut and excludes extraction-marked trees from its preferred cut selection. Extraction keeps native
eligibility; cancelling it alone is not a new infection trigger. Verify extraction→infection and
mixed extraction/cut cases through the owning S069 native workflow, without self-patches.

## Open Questions

PC-Q1–4 in the refinement ledger retain policy review, effective beta transition/admission work,
external-map/provider contracts and focused performance/localization/native acceptance. Questions
do not block this documentation stage and do not excuse unchecked implementation tasks.
