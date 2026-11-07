# Original feature: directional animal wandering

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
**Origin: original**, S066/P1-60 interview addition. User-selected reference: repository-owned
Pathfinding Lab's directional wildlife behavior, extended to hunting companions and confined animals.
No Workshop source, legacy migration or external consumed API is selected. Reference source and
tests were read without execution; exact hashes and native 1.7.4998 rev1299 evidence are in
[research](../refinement/2026-11-04-directional-wandering-research.md).

Status: research and observable draft independently reviewed and validated. **Nothing implemented.**
[Proposal](../../../../openspec/changes/specify-megamod-directional-animal-wandering/proposal.md),
[design](../../../../openspec/changes/specify-megamod-directional-animal-wandering/design.md),
[spec](../../../../openspec/changes/specify-megamod-directional-animal-wandering/specs/megamod-directional-animal-wandering/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-directional-animal-wandering/tasks.md).

## Behavior inventory and coverage

Every row is specified, not implemented; DW IDs refer to observable requirements in the linked spec.
Evidence is the user interview, complete selected reference files and targeted native inspection.

| ID | Observable action → result | Boundary / proposed default |
| --- | --- | --- |
| DW01 | Ordinary animal idles → select actual supported context | Wild / companion / enclosed pen / bounded room; purposeful and specialized work retains control |
| DW02 | Local walk begins → adjacent generally forward steps and turns | Wild/companion 3–10, confined 2–3; one-step/shorter/wait outcomes valid |
| DW03 | Wild herd idles or separates → bounded local movement or native regroup | Preserve current native herd selection/distance, no unrelated root |
| DW04 | Hunter waits/moves → companion idles nearby or physically regroups | S065 owns authority and one shared distance; other pets are never centers |
| DW05 | Animal is confined or boundary changes → current local enclosure | Physical pen/room, no herd/marker center, filters do not create walls |
| DW06 | Hazards/access/room choices apply → legal safe idle steps | Conservative no-door/fence idle preference; purposeful passage/shelter unaffected |
| DW07 | Trap or obstruction → shorter movement or normal waiting | No minimum-fill search, immediate failure churn, teleport or stale global route |
| DW08 | Load/disable/change map or work → current valid movement only | Discard stale local intent, preserve actual positions and purposeful work |
| DW09 | Tune/toggle → one inherited setting state | Bottom-right current-save toggle, common ranges/Use global, main-menu guards, seven languages |
| DW10 | Other owned/provider behavior → one movement authority | Shared native data, direct composition, minimal effective admission and explained fallback |

## Selected changes and interactions

The reference core rejects fewer than three requested steps, and its adapter discards one/two-step
results. Both deliberately change here. Pen/player contexts were excluded; the new selection and
confinement rules are original work. One-cell no-move behavior must be verified through actual
native job progression, not inferred from a bounded helper returning its starting cell.

S065 supplies hunter permission, needs suspension and actual prey/hauling jobs. The S066 toggle
changes only idle style; admitted ordinary following can remain. S024 room/shelter preferences and
S030 recovery compose directly. S043/S087 grazing, S021 hauling and native feeding/shelter/duties
retain purposeful routes. No internal patches between owned features or per-port spatial caches.

Native pen/room geometry defines confinement independently from pen acceptance filters. Proposed
idle doorway avoidance means an animal need not idle through every connected barn section; food,
sleep, safety and other legitimate jobs can still use the flap/door normally. Broken pens are not
made safe. DW-Q1 retains this policy, context precedence, classification and tuning for review.

## UI audit and relocation

| Reference surface | Actual purpose | Megamod selection |
| --- | --- | --- |
| Pathfinding Lab pawn gizmo and global experimental setting | Whole experimental router mode | Not imported; no pawn action for this global behavior |
| Local directional wandering on/off | Current-play behavior toggle | One small bottom-right button, current-save availability override through common settings |
| Availability defaults and walk lengths | Configuration | Animals / Wandering settings with Global default, Current save and Use global |
| Existing animal/pen/area controls | Entity-specific gameplay choices | Remain in native contextual surfaces, never per-entity mod-settings rows |

No new bottom tab. Seven-language text and actual rendered workflows remain unimplemented.
Main-menu save controls/callbacks are disabled; dormant overrides and save switching use the common
path. The source's global toggle and oversized-map controls are not copied or migrated.

## Contracts, omissions and compatibility

No Pathfinding Lab runtime dependency, assembly/type facade, settings/save import or drop-in
replacement claim. No upstream external consumers are selected; native job/follower/pen/room
semantics are researched inputs, not a promise to preserve private source APIs. The entire abstract
router, map-size changes and old optimization claims are omitted deliberately.

Flying/swimming/water-seeking, ropes/duties and special movement remain ordinary behavior until
explicitly supported. Compatible foreign patches are allowed; an unproven effective route declines
with a bounded startup warning and contextual explanation. Co-installation never changes another
mod's mode or removes its patches. Every actual patch needs its own target document and atomic
admission. Harmony/XML Extensions are required on their first actual use; no DLC or Gateway dependency.

Native spatial facts and the common map-data policy supply shared data. Per-animal heading/pending
intent is transient; new whole-map scans/flood fills/private caches are not justified by this draft.
No new animals, stat/yield/training/capacity benefit or artwork is added.

## Verification and remaining work

DW-Q1–5 track proposed policy, exact effective target/lifecycle and topology contracts, shared-data
costs, optional providers, UI and final native acceptance. Focused RED/GREEN, package/review,
actual waiting/movement/needs, geometry edits, save/interruptions, languages and performance evidence
are still required. Documentation inspection and historical Pathfinding Lab runs do not accept
this new feature or establish a speedup.

The draft has **10 requirements, 36 scenarios and 32 unchecked tasks**. Independent native/source
and composition reviews found no actionable findings. Typed strict validation passed 88 items with
zero failures; the separate PostHog telemetry error did not fail validation. Links and whitespace
passed across the 12 scoped documents. DW-Q1–5 and all runtime tasks remain open.
