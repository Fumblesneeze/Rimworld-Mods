## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S060 references Smarter Construction 1.7.0.0. Current source, all selected binary behavior classes,
native 1.7.4998 work/completion and three actual consumer families are recorded in
[research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-construction-research.md).
This is an independent design, not a port of source caches or transpilers. No runtime code here.

## Goals / Non-Goals

**Goals:** bounded automatic enclosure protection, physical safe-side completion, direct S058/S013/
S028 composition, correct map/worker identity, preserved material/work and bounded recovery.

**Non-Goals:** global optimal construction planner, every enclosure size or occupant, generic
pathfinding repair, minified installation, source namespace/assembly facades, copied source tests,
whole-provider compatibility, copied artwork or ordinary-game automation during specification.

## Decisions

### 1. One decision before actual irreversible completion

Place the core decision at the real completion boundary before Frame.CompleteConstruction consumes
resources, destroys the frame, creates quality/style and emits completion effects. Admission must
prove those calls have not already occurred and that deferral skips both completion and its normal
continuation exactly once. Do not target DisplayClass8_0, a b__1 name, fixed local numbers or the
first instruction text match. Resolve the actual closure/call/data flow under the exact runtime.

Evaluate candidate admission only for the actual finishing job. Current JobOnThing can return a
blocking-item handling job before FinishFrame; source filters any non-null result and can suppress
preparatory work. Preserve native blockers/clearing and validate final eligibility again. Predictive
selection is useful but cannot replace the fresh final check. All parts that promise protection,
defer completion and manage relocation form an atomic protection group. Ranking and stalled-travel
recovery are independent enhancements only where they can operate safely on their own.

Use the existing physical frame and native progress. Once full work is reached, waiting and moving
do not repeatedly execute the work/XP/failure tick. A permitted completion resumes the legitimate
completion path once; actual normal work still carries ordinary failure risk. A canceled or invalid
attempt preserves physical state. S013 only observes a real quality outcome; S028 keeps its existing
replacement transaction, contained items and costs while calling this decision directly when its
completion adds blocking cells. Compare old/new blocking footprints for replacement, not only the
new Def. This covers replacements constructed from resources; minified install-over retains the
explicit installation exclusion. No Harmony patch against owned construction code.

### 2. Bound geometry explicitly and distinguish it from pawn permissions

Retain source limits as named policy constants: small-region threshold 100 (strictly less is checked),
local-ranking distance 15 inclusive. Flood fill after hypothetically blocking the entire rotated
result footprint; four-way adjacency for regions and safe work positions, eight-way exterior
neighborhood for the local score. A blocked adjacent work cell with no traversable cardinal neighbor
remains inspectable. Never query out-of-map thing grids or treat an outside coordinate as a work cell.
Deduplicate visited cells and use bounded work; a large connected area ends this check without a
universal safety claim. Compare current physical topology, not named room/roof membership.

The normal path grid is a source of geometry, not proof that this pawn can reach or reserve a cell.
Choose from the small perimeter using native construction touch, reach, area/forbidden/reservation,
door and danger checks. Prefer nearest supported route distance; deterministic tie order. Validate
all occupied cells, including a builder standing in a non-origin cell. Before actual closure,
revalidate every affected worker/occupant and effective geometry. Ordinary player-faction animals
and mechs are protected; foreign pawns and loose goods remain outside the selected source promise.

The source does not compare every pre/post world path and does not establish universal isolation:
the bounded after-footprint small-space model is retained. Geometry providers can add a verified
usable exit to that decision; a nearby anchor alone is insufficient. The current explicit model
and whether to expose its policy limits remain CT-Q1, not silently broadened implementation choices.

### 3. Compose selection once, without summing arbitrary priorities

S058's category order remains first among eligible finishing candidates. Within a category, apply
the source-like local blocking-frame class, neighbor score when origin is currently walkable, then
native distance/discovery ties. Local means straight-line horizontal distance <=15 on an ordinary
map; supported providers must supply consistent route/world semantics rather than subtract unrelated
coordinates. Preserve other admitted provider priorities; unknown ordering contracts disable only
the affected ranking adapter. Candidate eligibility includes S013 worker restrictions. Source's
negative-result exemption is not permission to turn an invalid target into valid work.

Retain native listers/candidate providers, reservations, WorkGiver order and MaxPathDanger. Do not
globally replace GenClosest or reconstruct every job type. Source's custom finder only applies to
finish frames, despite a public comment mentioning delivery. The spec deliberately leaves delivery
order alone. Native blocking jobs, floor/roof jobs and installation keep their actual workflows.

### 4. Share facts and invalidate decisions by their real context

Use native topology, listers and the existing shared map data owner; no per-port Thing list or
map-blind static coordinate cache. Compute small local geometry on demand first. If profiling
justifies retained results, keys include map identity, full footprint/rotation and topology epoch;
cache only geometry. Read current occupants and worker-specific access separately. A previous
worker's EnclosesSelf/EnclosesThings result must never be reused for another worker.

Invalidate relevant spawn/despawn/destroy/rotation/terrain/door/footprint and provider passage
changes. Ensure all changed footprint cells are reflected before the next completion; source's
BuildingSpawned handler refreshes only the origin. Prefer the native freshness contract to an
unnecessary extra notification patch. Clear retained references on map/game teardown and rebuild
after load. No off-thread Verse/Unity reads, wall-clock random cache expiry or saved cache entries.
Planning throttle cannot turn unknown/freshness failure into permission to complete. Benchmark
combined consumers and update costs before claiming improvement.

### 5. Movement and continuation have one job owner

Use native movement plus one explicit, cancelable continuation tied to the frame/worker and original
construction choices. Do not copy only CurJob.def and target, force ignoreForbidden or enqueue a
fresh duplicate on every completion tick. Preserve native reservations/metadata and release only
owned reservations on cancellation. Drafting, target destruction, job replacement and map changes
clear stale intent; the actual frame/resources remain native state. Reload either resumes the one
valid intent or returns safely to ordinary scheduling, without lost work or fabricated results.

Source timing is a useful starting point for a scoped travel watchdog: 300 game ticks of observed
no movement, checked at intervals no larger than 50. Track actual active journey identity and exclude
valid door/provider transit and work states. Pause contributes no game ticks. One failed attempt
must not immediately recreate itself against unchanged circumstances. Retain a bounded suppression
reason until relevant geometry/access/position/order changes; avoid an indefinitely growing history
or a universal pathfinding service. Native explicit forced work is outside this added watchdog.
Do not adopt source's pawn-position cache across different jobs or its blind same-cell verdict.

### 6. External consumers are real but are not a small callable facade

The inspection found three families, detailed with hashes in research:

- MultiFloors (`telardo.MultiFloors`) conditionally loads a strongly bound source-assembly adapter
  only with `dhultgren.smarterconstruction`; it bypasses source enclosure checks on nonzero levels.
- Vehicle Map Framework (`OELS.VehicleMapFramework`) package-gates string-targeted patches of source
  priority/walkability and compiler-generated finder lambdas, plus source Harmony ordering.
- As above, So below II (`astryl.AsAboveSoBelow2`) resolves source detector/finder/pawn-cache names and
  patches their results/scopes/lambda distance to account for level links and transit.

These consumers depend on internal algorithm/IL shape, not a narrow behavior API. Copying all old
types, compiler closures or a strong source assembly just to accept those patches would preserve the
defects and recreate an internal patch stack. No such facade or package spoof is selected. Document
these unsatisfied bindings explicitly. Provide narrow owned optional geometry/distance/transit
adapters after canonical package and member/Def semantics are verified. MultiFloors and VMF samples
are 1.6 evidence, not beta-compatible implementations. AASB's 1.7 sample is not live acceptance.
Unknown provider semantics disable the affected enhancement with a bounded fallback, never invent
a route or leave the entire colony's ordinary construction blocked.

No source API means no automatic inherited multiplayer compatibility. Any synchronization work must
be independently specified and proved. Source co-installation disables owned overlap without foreign
unpatching or config edits; the source's remaining behavior is not claimed to follow owned rules.

### 7. Target admission and presentation

Create actual target documents and executable validators before code. Candidate groups:

| Group | Minimal consumed semantics / fallback |
| --- | --- |
| CT-PROTECT | Actual finish target, complete rotated footprint, current geometry/occupants, pre-consumption completion decision and exactly-once continuation; whole protection group inactive if unsafe |
| CT-MOVE | Native permitted position/movement, reservation and cancel/save lifecycle; part of protection's atomic support, ordinary work recoverable |
| CT-RANK | Actual finish-frame scanner/category and candidate/priority/reach path; ordinary ranking if unavailable |
| CT-PROGRESS | Active travel identity and real waiting/progress states; ordinary native job recovery if unsupported |
| CT-DATA | Only required shared freshness notifications, full map/footprint lifecycle; native/on-demand fallback |
| CT-PROVIDER | Canonical optional package plus real geometry/exit/distance/transit semantics; no enhancement on unsupported affected geometry |

Validate effective flow, relevant skip-original/foreign-transpiler interactions and current incoming
streams, tolerating benign changes and compatible owners. Unknown or ambiguous matches, exceptions
and install failures disable the group and roll back only newly owned hooks. Every transpiler returns
its input unchanged on failure; never execute foreign transpilers speculatively. Supported late
repatch invalidation must withdraw stale admission. Startup logs identify each skipped enabled patch,
target/spec ID, failed requirement, unavailable behavior and ordinary fallback without tick spam.

Use ordinary construction context and inspect explanations; no new setting/navigation framework.
Any future parameter setting follows common global/save controls, not source readonly fields.
Translate actual help/wait/recovery text into seven baseline languages. No source icons are selected;
future raster work still requires the asset workflow. The source has no bottom tab or player settings
screen to relocate. Any later diagnostic controls belong in the top debug toolbar.

## Risks / Trade-offs

- Small-region cap and selected occupant scope leave deliberate gaps → explicit 99/100, map-edge,
  multi-cell and unprotected-occupant scenarios; no blanket anti-trapping claim.
- Safety can delay walls and quality/replacement work → retain materials/progress, visible reason,
  native forced bypass, no retry XP/failure farming and current-state reconsideration.
- Multiple workers or asynchronous native topology refresh can race closure → fresh final decision,
  full-footprint invalidation and exact native acceptance with concurrent builders.
- Source consumers modify internal IL → no false facade promise; narrow owned optional adapters and
  exact provider-present testing, preserving ordinary fallback where unsupported.
- Local ordering can increase walking or delay beds → S058 category composition and measured native,
  source and owned comparisons; source performance numbers are not our evidence.

## Migration Plan

No deployment, normal-save editing or source cache/settings migration. Implement native physical
state and safe interruption first, then one vertical geometry/selection/movement slice at a time.
Recovery must work when an enhancement later becomes unavailable. No safe-uninstall promise for
future custom job state without a separate verified migration/removal contract. TDD, independent
review, package checks and final causal native workflows follow root AGENTS.md.

## Open Questions

CT-Q1–4 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain policy
scope/controls, exact completion/recovery semantics, optional consumers and measured gameplay/runtime
acceptance. All implementation and acceptance tasks remain unchecked.
