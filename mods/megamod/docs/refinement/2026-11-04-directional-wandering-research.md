# S066 — directional animal wandering research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
**Origin: original**, P1-60/interview addition, researched 2026-11-04. Reference: repository-owned
Pathfinding Lab, `fumblesneeze.pathfindinglab`; no selected Workshop source, external license/API
or source save migration. This document is evidence/refinement, separate from the observable spec.

## User decisions and proposed choices

The [interview record](2026-11-01-handwritten-notes.md) selects the experimental herd-wandering
update, also used around S065 hunters and for animals in pens/rooms. Confined animals use much
shorter walks, provisionally 2–3 adjacent steps, and do not use another animal as their center.
Wild herds retain their herd reference; companions use the hunter. Direction persists generally,
obstacles cause turns, and fewer steps or no movement are valid. A one-cell room must terminate
without endless searches, immediate job churn or interference with real work. This selects the
wandering feature, not the experimental routing system.

Proposed DW-Q1 choices: ordinary supported ground-animal contexts; active S065 companionship before
ordinary confinement, then enclosed pen, bounded current room, then unconfined wild wandering;
physical containment independent of pen acceptance filters; native room geometry without a roof
requirement; conservative door/fence-free idle paths; 3–10 wildlife/companion steps and 2–3 confined
steps; integer settings bounded to 1–10 steps; availability on. S065 still owns companion distance,
proposed 3 cells. These policies are not invented interview answers.

## Documentation first and pinned reference

Read Pathfinding Lab's [README](../../../PathfindingLab/README.md) and its
[proposal](../../../../openspec/changes/add-pathfinding-lab/proposal.md),
[design](../../../../openspec/changes/add-pathfinding-lab/design.md),
[wildlife contract](../../../../openspec/changes/add-pathfinding-lab/specs/pathfinding-lab-routing/spec.md)
and task/evidence distinctions before source. The documented selected subset is ordinary wildlife
3–10-step direction-preserving movement, native wait cadence and herd-circle bounds. Purposeful,
pen/duty and unsupported locomotion were excluded. Its broader routing/benchmark claims do not
prove this new adaptation. A later public documentation search surfaced historical announcements
and secondary pen guides; none was used as proof of current 1.7 technical behavior.

Read both complete implementation files and all three focused reference tests, without executing
the project or tests. SHA256 at inspection:

| Input | SHA256 |
| --- | --- |
| `mods/PathfindingLab/README.md` | `C20FD1B9CFC0F70B9550C14A0F58CB4D6C3C63EF340D81497A01EC6E6A135014` |
| `mods/PathfindingLab/Source/WildlifeWander.cs` | `110215E90289EAC9F800EC9C6698767C08D10F2D0E7636FEA3A2D3B1310A4B2F` |
| `mods/PathfindingLab/Source/Core/DirectionalWalk.cs` | `E74F0F5DA100EB71DE6AB833C4FA766C82E63400E70AF2D0333DEAD20D713FE5` |
| `tests/PathfindingLab.Unit/DirectionalWalkTests.cs` | `C32A25DF64F9CE1BDBD4C705333BF8B6ACFDE6C9BC84C1428E24D2059C34197D` |
| Core `Defs/ThinkTreeDefs/Animal.xml` | `D857FB17533F17F90DA48FAE355D5014F7A4F78AB3DCC6EE755A881BDA732121` |

The typed installation inspection reports **1.7.4998 rev1299**, root
`F:/Steam/steamapps/common/RimWorld`. Native assembly SHA256:
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Offline ILSpy output is retained under
`artifacts/Refinement/megamod-spec-20261104/directional-wandering/native/`.
The typed registry exposes live Gateway decompilation, not offline source inspection; no game
was launched. One guessed `RimWorld.AnimalPenEnclosureState` lookup failed; the actual consumed
type `Verse.PenMarkerState` was resolved from CompAnimalPenMarker and read successfully.

## Reference behavior and adaptation gaps

- Core `Build` accepts 3–10 requested steps; it retains a heading, usually continues forward and
  occasionally drifts by 45 degrees. Each step tests at most eight headings, never revisits a cell
  within that walk, and returns the starting cell alone when trapped. Heading reverses on blockage.
- Runtime restricts admission to exact WanderAnywhere/WanderHerd providers, factionless spawned
  animals, no downed/dead/mental state, flight, swimming, water-seeking, duty or Lord. Experimental
  activation is global and separate from megamod's selected behavior.
- Herd roots come from the native provider; being outside the radius uses ordinary regrouping.
  Local walks use the herd circle as a hard boundary. Non-herd walks start at the animal itself.
- **Adaptation required:** the runtime drops every path with fewer than four cells, including the
  start. It therefore discards valid one/two-step results, and the core rejects a requested two-step
  walk. Both must change for the user's confined-animal contract; this is not an unchanged port.
- Destination, job association and PathFinder request handoff are separate hooks. Job pooling is
  handled through Job.Clear and re-association. Pending paths and headings use weak transient state;
  loaded uncached idle jobs fail/rethink. Exceptions release allocated native paths.
- Local checks refuse doors, fences, collisions, forbidden/access-invalid cells, bad diagonal
  corners, hazards, sunlight sensitivity, pollution/rot stink below animal resistance 0.5, reserved
  destinations and provider-invalid cells. Real native movement continues through the follower.
  The source's approximate path total is not proof of every effective movement cost/provider rule.
- Existing hooks mostly check callable existence, not megamod's effective semantic admission.
  They are reference mechanisms, not approved target documents or compatibility proofs.
- Three host tests cover an open walk, blocked/herd-boundary turns without repeated cells, and
  eight rejected directions for a trapped walk. They do not test pens, rooms, two-step support,
  native interruptions, moving hunters, load or megamod acceptance.

## Native providers and spatial truth

Complete small types read: JobGiver_Wander, WanderAnywhere, WanderHerd, WanderCurrentRoom,
WanderInPen, WanderInRoofedCellsInPen, WanderRoped, WanderColony, WanderUtility, WanderRoomUtility,
AnimalPenEnclosureCalculator, AnimalPenConnectedDistrictsCalculator, AnimalPenEnclosureStateCalculator,
AnimalPenManager, PenMarkerState and JobDriver_Goto. Targeted sections read: AnimalPenUtility,
CompAnimalPenMarker, Room, RCellFinder and Pawn_PathFollower. Near-master/FollowClose evidence is in
the adjacent [S065 research](2026-11-04-hunting-companions-research.md). RegionAndRoomQuery and
JobDriver_Wait were extracted as supporting inputs; extraction alone is not a full behavior audit.

| Context | Inspected behavior | Consequence for this draft |
| --- | --- | --- |
| Ordinary wander | Alternates native wait/move, uses actual provider urgency/expiry/report; stationary selection can wait | Preserve cadence; returning the current cell during an already-running wander alone is insufficient proof against churn |
| Wild herd | Same race/faction, reachable candidate, avoids humans within 15 cells, native sampled candidate selection; radius 5 | Preserve native herd selection, no new private herd list or hunter substituted as wild root |
| Anywhere | Constructor radius 7, walk, 125–200 wait; Animal.xml commonly overrides wait to 120–240 | Walk length is not radius or duration; retain effective provider timing |
| Current room | Radius 7, amble, 125–200 wait, same-room validator except missing/doorway root | New confined context must prove a real bounded room; do not treat any map cell as a room prison |
| Pen idle | Radius 10; on map-edge district can select a suitable marker; player-roamer tree uses this only 10% of ordinary selections | Pen movement is not simply a global replacement of one provider; preserve unconfined marker-seeking |
| Roofed pen movement | Separate override checks active conditions, enclosed pen, roofed cells and samples up to four connected regions | Purposeful shelter-seeking remains separate, never trapped by a short-walk limit |
| Roped animal | Separate rope-root/radius 6 and current rope checks | Ropes/caravans remain outside ordinary idle steering |

Native PenMarkerState lazily caches connected regions, checks passable/impassable door transitions,
and AnimalPenManager rebuilds marker states after building changes. RoamerCanPass depends on current
free passage or the door's roamerCanOpen capability; pens can include connected rooms. Current-pen
lookup additionally requires Roamer, an accepting marker and reachability, so it alone cannot prove
physical containment of every animal after a filter change. The new contract separates geometry
from acceptance/relocation orders, using native facts without inventing permanent assignments.

Room.ProperRoom excludes map-edge rooms and requires a normal district; roof/temperature/role are
different properties. A bounded unroofed courtyard can therefore be a room; a roof over open terrain
does not make it one. Missing, doorway or changing topology must not become a stale confinement.
No new pen/room flood fill or per-animal whole-map topology cache is justified.

RCellFinder's ordinary destination search relaxes preferences and eventually tries larger radii,
even dropping some validator/access preferences in later fallbacks. That is not the selected
trapped-local-walk policy. Keep intentional purposeful recovery/regroup routing separate from
idle fallback instead of inheriting these relaxed searches into every blocked animal.

## Real movement, composition and limitations

JobDriver_Goto reserves the destination and uses native Toils_Goto. Pawn_PathFollower charges
actual movement/terrain/building/urgency costs, checks collisions/doors/corners and periodically
requests a new path. A direct short path still needs exact current job/request identity, owned
path lifecycle and revalidation before affected movement; native periodic checks alone do not
prove timely room/root/hazard invalidation. Do not treat a failed local path as authorization for
a global route to its stale endpoint.

S024 considerate room choices and optional shelter rules compose directly; already-sheltered walks
retain its cost budget. S030 recovery and native roofed-pen shelter take priority and can take
their permitted purposeful routes. S065 owns temporary following suspension and actual hunting
authority. S043/S087 grazing-related features, S021 hauling and other feeding/work jobs retain
their own routing and resource outcomes. The walker changes neither nutrition nor training/yields.

No source external consumers, runtime facade, source-settings import or performance guarantee is
claimed. Co-installation with Pathfinding Lab or another movement replacer must admit the effective
path, not reject solely for an owner/package presence or silently change the other mod's mode.
Native spatial listers/caches and the common map data policy remain authoritative. All measured
combined costs, actual large-population movement, special provider support and native acceptance
remain DW-Q2–5 work. No runtime code/Defs/assets, tests, build/install, game launch or normal-save
mutation occurred.

## Review and validation checkpoint

The owned draft contains **10 requirements, 36 observable scenarios and 32 unchecked tasks**.
An independent source/native review found no actionable P1/P2 issues and confirmed the four
reference-file hashes. A separate composition review found no issues across S024/S030 safety,
S064/S065 hunting/fallback, S043 grazing, S021 cargo and the shared settings/UI/data/admission
rules. S087 is explicitly queued. S065's spec/design/tasks/coverage now state the admitted
ordinary near-hunter fallback when directional style is disabled or unavailable, preserving
the same current permissions and temporary needs suspension.

Strict typed OpenSpec validation passed **88 items, zero failures**, exit code 0. The CLI also
reported a PostHog telemetry transport error, separate from successful schema validation.
All **12 scoped Markdown documents** passed local-link and whitespace checks. The queue contains
**67 reviewed drafts / 136 entries / 287 unique question IDs**. DW-Q1–5 remain open; no runtime
task or old Pathfinding Lab evidence is claimed as megamod acceptance.

Semantic balance pass: wild/herd, room and pen source comparators retain distinct native cadence,
urgency and radius; short step counts change idle excursions rather than movement speed or a
follow radius. Actual native movement costs, hazards, training, nutrition and purposeful access
remain. Conservative idle doorway avoidance can keep an animal on one side while idling, so real
food/shelter/flap workflows are explicit acceptance tasks. No new content, capacity, yield or
combat immunity is granted. Default lengths, context precedence and doorway policy are proposed;
finalized active-mod movement and combined performance remain unverified.

Next: **S067 Add Deconstruct Button To Floor Category**. No code/Defs/assets, build/install,
gameplay tests, game launch, source execution, Workshop edit or normal-save/config mutation occurred.
