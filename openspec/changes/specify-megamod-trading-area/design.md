## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
**Origin: original**, S040. Read the [proposal](proposal.md),
[spec](specs/megamod-trading-area/spec.md) and
[native research](../../../mods/megamod/docs/refinement/2026-11-03-trading-area-research.md).
S039 owns party eligibility, lifecycle and the visitor setting; this change owns the footprint,
editing and cell-level gathering contract. Both compose within one owned feature, without self-patches.

## Goals / Non-Goals

**Goals:** a dedicated editable area, understandable contextual controls, actual painted-cell
gathering, native movement/safety/roles, finite fallback and map/save isolation.

**Non-Goals:** a physical marker, generic allowed-area picker, new bottom tab, exclusive storage zone,
per-faction area priority system, renamed foreign area, combat confinement, arrival teleportation,
pricing/stock changes, source identity facade or automatic migration.

## Decisions

### Dedicated map-owned area and native designator context

Use native Area/BoolGrid/CellBoolDrawer capabilities where the exact beta lifecycle permits it.
The base Area is non-assignable; never derive behavior from Area_Allowed or Area_Home's assignability.
Keep one owned area identity per map, initially empty. Its lifecycle must be idempotent for new maps,
loads and repeated initialization, without consuming an allowed-area slot. Choose a guarded native
registration seam or map-owned host after resolving TA-Q3; do not casually append duplicate objects
from multiple callbacks or rewrite foreign AreaManager contents.

Use one Trading area designator entry in Architect's area tools. Its selected context supplies
Expand, Erase and Clear all commands, status readout and overlay through native extension surfaces.
The native Selector accepts Things, Zones and Plans, not Areas; do not claim that simply deriving
Area makes it selectable with native gizmos. A hidden Thing marker would reintroduce the omitted
source object, and a Zone would conflict with existing stockpiles/growing zones. A broad Selector
rewrite is unnecessary for the specified command-based entry point. Exact contextual layout/input
must be verified in the running game, including empty-area access and supported UI scales.

Static Def/control metadata belongs in XML where available. Runtime cell selection, edits, phase
checks and native duty integration require C#. Any added Architect list entry must preserve other
entries and ordering rules; never replace the whole category. XML patches require the owning target
spec, XML Extensions content/shape checks and its declared dependency. Optional Architect XML also
requires FindMod with packageId=true. Prefer an available native extension point to a patch.

### Commit edits as map-bound changes

An active stroke holds map identity and prospective cell changes. Preview it without publishing a
new party destination. Complete the native gesture to publish one committed revision; cancelling,
switching map, leaving the map view or unloading the game discards unfinished input. Revalidate the
map and cells at commit. No deferred callback can write into a newly selected map or previous save.

Expand adds eligible discovered in-bounds cells; Erase removes painted cells without requiring them
to remain standable/discovered. Clear all empties only this map's owned grid. Commit idempotent deltas
and notify both rendering and S039 routing. Native Area.Clear/Invert do not issue all the same cell
notifications as Set; resolve actual consumer invalidation before choosing bulk paths. No-op edits
must not force party jobs. Existing zones, home/roof/allowed areas and objects are never mutated.

No new preference is introduced. Footprint and editing actions are map entity state. The ordinary
visitor preference remains in S039's common settings model. Editing mode/overlay focus is transient
UI state; the selected tool is not persisted as another save's active editor.

### Separate painted intent from current party usability

The saved footprint can include walls, water or temporarily obstructed cells without clearing or
changing those objects. Candidate selection uses the actual party and current native standability,
reachability, danger, access and occupancy rules. Preserve existing native permissions; do not
invent access just because the player painted a cell. Recheck at the decision and before commitment.

Keep a still-usable current gathering cell to avoid churn. Otherwise select a usable actual painted
cell, not an average/bounding-box center. Candidate order is an incidental implementation choice;
the spec promises neither nearest travel nor evenly distributed parties. Disconnected painted
islands need not be joined by painted paths. All relevant party members must satisfy S039's route
requirements; checking only the trader cannot establish a valid caravan location.

Ordinary idle-cell choices additionally respect the member's native gathering range/role around the
chosen location. No uniform new radius or invented pawn-per-cell limit. Native occupancy permits
the behavior it actually supports; the feature does not reserve the whole area. A finite pass over
available candidates ends in valid waiting or explicit native fallback, never unbounded randomized
retry. Preserve a currently valid in-area wait when no new movement is needed. If the party cannot
gather there, release it through S039; do not display successful area use while it remains elsewhere.

### Ordinary gathering reaches painted cells

S039's incoming point alone is insufficient: Travel can report arrival within its radius, and
Defend wandering uses a point/radius rather than arbitrary area membership. The composed feature
must arrange a normal physical approach to a usable painted cell before treating ordinary idle
gathering as fulfilled. Do not teleport or increase speed to reach the footprint.
JobGiver_Wander.TryGiveJob can return Wait_Wander from nextMoveOrderIsWait before invoking
GetExactWanderDest. Restricting only the destination validator cannot establish this entry rule;
cover the early wait path after arrival, needs and delivery as well as destination selection.

Constrain only the exact admitted ordinary gathering/idle path for the eligible party and member.
Preserve combat/needs branches, carrier Follow and chattel Escort, and S027 courier ownership.
The inspected JobGiver_WanderNearDutyLocation is broadly reused; a global validator replacement
would change unrelated pawns. An owned duty variant or narrow native adapter must preserve the
real finalized role graph. JobGiver_WanderInGatheringArea's name alone is not proof it controls
traders. Do not implement a generic allowed-area/path-cost override to force footprint containment.

Paths, followers, needs, delivery, rescue and exits can cross outside. If a current ordinary idle
target was erased, replace it at S039's next legitimate gathering decision. A protected job still
owns its normal completion/cancellation. An area edit cannot restart the party's timer or gifts.

### Data ownership and costs

Save only the owned map footprint/identity and any required schema data. S039 separately owns the
minimal party baseline needed to release its redirection. Rebuild transient candidate/drawing state
after load; current phase, roles, path permissions and occupants are never durable eligibility facts.
On map/game removal clear references and pending editors. New maps start empty; movement of pawns,
buildings or caravans does not copy cells to another map. Exact beta map transforms need evidence
before supporting a footprint transfer; do not infer transfer from equal coordinates.

Use native Lord lists and area cells directly. This is not a consumer needing another Thing index.
Only add derived candidate data when measured cost warrants it, with explicit terrain/edifice/door,
reservation/occupancy, party-access and footprint revision invalidation. Changes to shared facts
still use the mod's one shared data path; use-time native checks remain authoritative. Keep all
Verse/Unity work on the game thread. Measure large footprints and multiple parties before claiming
an optimization; bounded candidate enumeration must not become every-tick full-map pathfinding.

### Admission and inactive behavior

Before any actual patch, add its stable-ID target document and executable validator with scenario,
tests and evidence links. Candidate seams include registration/load, designator context, committed
edits and exact ordinary idle-destination selection. An observer needs only the callable shape it
consumes; altering a destination requires the actual argument/result/field use and stage semantics.
Check original and effective behavior, skip-original prefixes and relevant transpilers. Every
transpiler validates its actual incoming stream on every execution and returns it unchanged on
failure. Do not probe by repeatedly running foreign transpilers. Revalidate supported later repatches.

Treat S039 redirection and required S040 cell-level behavior as one atomic routing capability for
each supported party path: if footprint containment cannot be admitted, do not silently downgrade
to the old point/radius promise. Keep the independent area editor/readout available if safe. A false,
unknown or throwing check/install disables the affected route and rolls back only newly installed
owned hooks. Never unpatch another owner. Emit one bounded startup warning per skipped enabled patch
with package/patch/spec ID, target, reason, unavailable behavior and fallback. Benign foreign patches
or unrelated code changes are allowed; exact hashes/versions are evidence, not admission predicates.

Optional providers require canonical package and consumed type/member/Def/ownership checks. Original
PL Trading Spot suppresses overlapping routing under S039; the area's cells remain editable and
visibly inactive. Unsupported guest systems retain their own control. Native area tools/foreign
zones remain intact when one optional UI integration fails. No marker, package or DLL spoof.

## Risks / Trade-offs

- **Area is drawn but party never actually enters** → verify physical entry and later idle targets,
  not merely changed Lord fields or a static overlay.
- **Narrow/crowded footprint creates retry loops** → finite candidate handling and native waiting/
  fallback; test one cell and no usable cell without inventing a minimum size.
- **Area-as-zone steals storage cells** → independent non-assignable area state and overlap scenarios.
- **Editing during a map switch targets another map** → bind gestures and callbacks to exact map
  identity; cancel stale input before it can commit.
- **Duty constraints leak into combat or other pawns** → exact party/stage/member admission and
  preservation tests for native Follow/Escort, needs, deliveries and departures.

## Migration Plan

New or missing owned map state starts empty. Existing owned footprints round-trip with their maps.
No source marker conversion, foreign allowed-area import, copied name identity or automatic provider
removal. A later opt-in import requires its own backup/atomicity/restoration contract. Safe mod
removal and unsupported schema conversion are not claimed by this draft.

## Verification and Open Questions

TA-Q1–4 retain the proposed UI defaults, exact live seams, map lifecycle, measured costs and acceptance.
Pure cell-set/stroke/policy calculations fit Unit; owned patch-shape tests fit Harmony; lightweight
Def fixtures fit Defs. Real AreaManager, finalized duties, native pathfinding and active providers
require isolated RimWorld, not a fabricated host Def database.

After focused RED/GREEN and independent review, use native area tools to paint/edit/cancel/clear,
then observe actual incident-created parties arriving and idling within the footprint. Include holes,
blocked/narrow/disconnected/crowded areas, overlap with storage, S027 delivery, emergency exit,
save/load and map switch. Inspect before/action/after screenshots on the reviewed build. Record
build/mod identity, native actions, supporting logs, config hashes and cleanup. Verify seven-language
text and real gizmo input; any new raster assets also require the asset skill's independent live gate.
