# Dedicated trading area research — 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
**Origin: original**, S040/P1-31. No original mod/package/license applies to this area feature.

## User intent and evidence boundary

The [intake](2026-11-01-handwritten-notes.md) records a spiritual successor to Trading Spot: draw a
dedicated fixed trading area through area tools, with its own gizmos, not a pawn allowed area.
"Fixed" does not mean an immutable footprint, fixed size or predetermined map coordinates.
The goal is control of traders' ordinary gathering, not a new trading economy or content pack.

S039's [documentation-first source research](2026-11-03-trading-spot-research.md) already establishes
the reference's marker, party roles and native lifecycle. That source cannot supply the new area's
geometry/UI contract. The decisions below are proposed owned behavior, explicitly distinguished
from user-confirmed intent and inspected vanilla mechanics. No new public mod is inferred as a source.

## Native references

Inspected **1.7.4995 rev309**, Assembly-CSharp SHA-256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
ILSpy **11.2.0.10050** output is retained under
`artifacts/Refinement/megamod-spec-20261103/trading-area/native`.

Read complete Area, AreaManager, Area_Home, Area_Allowed, Designator_AreaHome/Expand/Clear,
Designator_AreaAllowed, JobGiver_Wander, JobGiver_WanderNearDutyLocation,
JobGiver_WanderInGatheringArea and JobGiver_AIDefendPoint. Selected relevant portions of
Designator_ZoneAdd, Selector and Designator establish the UI boundaries below; this is not an
audit of every native selection/job path. An initial guessed type `Verse.AI.JobGiver_DefendPoint`
did not exist; the actual `RimWorld.JobGiver_AIDefendPoint` was then inspected.

Read the Defend and Follow entries in Core `Defs/DutyDefs/Duties_Misc.xml`, file SHA-256
`9BF1AA50460348E62D1A2F57A001B7086666839AB8824D488133B7F81967A88A`.
These are source XML and decompiled bodies, not finalized loaded Defs or live workflow evidence.

| Native fact | Consequence for design |
| --- | --- |
| Area stores a map-owned BoolGrid, ID, saved grid and lazy CellBoolDrawer; base AssignableAsAllowed returns false | Dedicated area representation can reuse native cell storage/overlay without becoming an allowed area |
| Area_Home and Area_Allowed both override AssignableAsAllowed to true | Do not reuse Home or a renamed allowed-area object for this feature |
| AreaManager creates a fixed set of starting areas, loads links, and publicly exposes AllAreas/Get; default allowed-area cap counts only Area_Allowed | Owned registration/load repair needs a proven idempotent map lifecycle seam; do not consume an allowed-area slot |
| Area.Set notifies pathfinder/regions; native Clear/Invert directly change the grid and dirty drawing | Bulk edits must issue the owned routing revision/invalidation; calling Clear alone cannot prove all consumers refreshed |
| Home-area designators support add/remove cell gestures, draw styles, measurements and overlay; no construction job | Reuse the native area-tool workflow, with separate owned cell eligibility |
| Native Home painting only checks bounds and current membership | Proposed discovered-cell-only admission is an owned decision, not a claim about vanilla Home behavior |
| ZoneAdd rejects cells belonging to another zone type, fogged cells and other zone restrictions | An ordinary exclusive Zone would prevent the desired overlay on existing stockpiles/growing zones; avoid claiming that representation is equivalent |
| Selector.Select accepts Thing, Zone or Plan, not Area | A plain Area cannot supply native selection gizmos automatically; use the dedicated designator's contextual command surface instead of a hidden marker or broad selection rewrite |
| Designator has DrawPanelReadout, DoExtraGuiControls and selection/input hooks | Candidate native surfaces for the area's readout and gizmos; exact layout/input/localization remains implementation evidence |
| JobGiver_Wander uses a point/radius, optional validator, native danger/path checks and wait jobs; valid current position can result in waiting | Radius-only point reassignment is insufficient to honor an irregular footprint or ensure entry before idling |
| WanderNearDutyLocation defaults radius 7 and waits 125–200 ticks; Defend XML supplies radius 8 and Sprint outside radius | Reference numbers, not new area settings or a promise that all finalized providers use these values |
| Defend duty puts combat and ordinary needs ahead of wandering; Follow has its own following branch and radius | Constrain only admitted ordinary gathering/idle decisions; never globally constrain defense, needs, follower roles or all WanderNearDutyLocation callers |
| AIDefendPoint chooses combat positions around duty focus/radius | Do not apply area containment to combat movement just because it uses the same duty focus |
| WanderInGatheringArea uses GatheringsUtility and is not established as the trader idle path | Do not choose a method merely because its name says gathering area |

S039's native Travel arrival check permits nearness rather than exact arrival. Therefore the owned
ordinary-gathering decision must bring a supported trader/idle member into a usable painted cell;
standing outside within the ordinary arrival radius is not sufficient fulfillment of S040.
Independent native review also highlighted JobGiver_Wander.TryGiveJob's early Wait_Wander return
when nextMoveOrderIsWait applies, before GetExactWanderDest is called. A destination-validator-only
adapter cannot prove entry; TA-Q2 and task 2.6 explicitly cover this branch after arrival/needs/delivery.

## Proposed defaults and boundaries

- **One dedicated area per map, initially empty.** Painting multiple islands edits one footprint;
  there are no per-faction areas, named-area lists, priorities or manual party assignments in scope.
- **Architect area tools → Trading area**, with contextual Expand, Erase and Clear all gizmos and a
  readout. The dedicated command remains reachable when the footprint is empty. No bottom tab,
  bottom-right global toggle, generic allowed-area picker or hidden marker required.
- Drawing is free and immediate, available without research. Add discovered in-bounds cells;
  existing walls, terrain and zones remain. Painting is intent, not proof of current reachability.
  The same cell may belong to storage/growing/home/roof/allowed areas without altering those states.
- Prefer the current valid gathering location. Otherwise choose an actually painted usable cell;
  never use an unpainted centroid or bounding-box hole. No global-shortest-route claim. Relevant
  party members retain actual permissions, and their presence does not reserve the entire area.
- Normal idle destinations for supported gathering members stay in usable footprint cells within
  their ordinary gathering range. Followers retain native following; paths and temporary needs,
  S027 deliveries, combat/rescue and departures may leave. This is not an invisible wall.
- No arbitrary minimum area size or numerical pawn-per-cell capacity. A one-cell footprint may be
  usable under native movement/occupancy rules; waiting must terminate normally, not resample forever.
  If a member has no compatible idle candidate, retain legitimate safe waiting or yield to native
  recovery rather than fabricate a reachable cell. If the party has no usable gathering location,
  S039 native fallback applies. Crowding does not grant exclusive ownership of every painted cell.
- Area changes take effect after a completed edit, under S039's next ordinary decision rule.
  Cancellation of an unfinished gesture leaves the committed footprint unchanged. Clearing means
  empty area/native fallback; it does not delete unrelated zones, objects or source markers.
- State and edits belong to the current map/save. They are not preferences and use no global/save
  inheritance controls. S039's ordinary-visitor preference remains the sole related actual setting.
- The UI distinguishes empty footprint, drawing without a current party, per-party unusability and
  inactive integration. It does not promise universally reachable cells before evaluating a party
  or expose hidden map information. No repeated warnings or alerts when ordinary fallback suffices.

These are specification proposals. One-area scope, exact gestures and idle-membership expectations
are open to later refinement; do not mislabel them as confirmed interview answers. S039 provides
eligibility/phase/fallback and S027 provides delivery ownership through direct owned composition.

## Contracts, balance and implementation limits

There is no independent source assembly, saved type or consumed API to preserve for this original
feature. S039 documents legacy TradingSpot consumers; the area does not invent that ThingDef or
spoof its package. Native/modded stockpiles, zones and allowed areas remain intact. Exact Architect
providers and native graph/idle shapes still require admission and live evidence.

Area editing changes where ordinary parties wait and can affect travel/exposure; it does not add
trade goods, raise prices, prolong visits, restrict emergency escape or grant building/access rights.
Free drawing reflects an area order rather than a physical product. No new item economy, work
requirement, research tier, radius or carrying multiplier is introduced.

Use map-owned cell data and existing native Lords, not another shared Thing cache. Any transient
usable-cell set needs terrain/door/occupancy/party-access invalidation and use-time native checks;
do not persist a pawn-specific permission result. Runtime integration must follow the common minimal
semantic admission, incoming-transpiler validation and bounded failure diagnostics. No patch ID or
performance result is claimed before choosing the real implementation.

| ID | Remaining work |
| --- | --- |
| TA-Q1 | Verify proposed one-area/UI/painting defaults in the full native workflow; exact draw-style, overlay/gizmo layout and seven-language text; new art only under the asset skill |
| TA-Q2 | Exact idle-entry/cell-selection semantics and native safety/occupancy/party permissions, including one cell, holes, disconnected areas, crowding and all-supported-members fallback |
| TA-Q3 | Idempotent map registration/save/load and editor cancellation, area revision/invalidation, beta map lifecycle and guarded Architect/provider interaction |
| TA-Q4 | Focused RED/GREEN, reviewed-build native acceptance and measured large-area/multiple-party cost; no runtime claim from static source/UI seams |

No code, assets, runtime metadata, user saves/configuration or Workshop content changed. No game ran.
