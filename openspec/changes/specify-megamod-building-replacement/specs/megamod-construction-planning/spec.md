# Construction planning

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Documentation-only S028 contract; no gameplay accepted.

## ADDED Requirements

### Requirement: Planning over rock preserves normal removal work
A small bottom-right control SHALL enable planning positive-work buildings over unsmoothed mineable rock and unexplored cells, initially enabled for a new save unless its configured default says otherwise.
The control is available in map play, not as a new bottom tab or world-map action. Turning it off
returns new placement to ordinary restrictions without canceling existing plans. Over-rock orders
add necessary mining where no suitable existing mining/smoothing order already handles the rock.
Resources can be delivered before clearance, but the final building must wait for physical clearance.
Builders only perform mining if ordinary work/capability rules allow it; normal mining skill and ore
yield still apply. Already ordered suitable smoothing is completed instead of destroying a wall that
can coexist with the planned result. Unrelated smoothing/mining orders are not silently canceled.

#### Scenario: Builder cannot mine
- **WHEN** a building is planned over rock but its available builder cannot perform mining
- **THEN** the plan remains and waits for an eligible miner; building work does not erase the rock or bypass its ore-yield rules

#### Scenario: Compatible smoothing already ordered
- **WHEN** a smoothed wall would satisfy an existing smoothing order and coexist with the planned construction
- **THEN** that rock is smoothed rather than mined away merely to serve the construction plan

### Requirement: Fog planning neither reveals terrain nor bypasses revealed restrictions
Unexplored-cell planning SHALL show and allow selection/cancellation of the player's own plan without revealing hidden terrain, rooms or contents.
Research, visible placement information and duplicate-plan restrictions still apply. When relevant
cells are revealed, every affected plan is checked against its entire now-known footprint on its
own map. A revealed obstacle or invalid terrain leaves the order unable to proceed with an
explanation; it does not erase the obstacle or construct an invalid building. A plan invalidated by
reveal is canceled with actual supplied inputs recovered when no supported prerequisite can make
it valid. A suitable rock/bridge prerequisite follows this capability's normal planning rules.
Hidden terrain takes precedence over automatic support discovery and exact cost previews: no
hidden-cell bridge choice, support plan, terrain-specific rejection or inferred support cost is
shown before reveal. Display those support needs/costs as unknown. Retain a mixed-footprint order
as awaiting exploration and defer new support planning and construction until its relevant footprint
is known; do not spend inputs against an unassessed hidden support arrangement. On reveal, apply
normal full-footprint checks and then show/create any legal prerequisites and their actual costs.

#### Scenario: Discover an invalid footprint
- **WHEN** exploration reveals that one cell of a large planned building cannot support it and no supported bridge or rock-removal prerequisite can fix it
- **THEN** the plan is canceled with an explanation and its actual supplied resources are recovered, without revealing other unexplored cells

#### Scenario: Reveal on a different map
- **WHEN** fog is revealed on a map other than the one currently viewed
- **THEN** affected plans on that map are rechecked and plans on the viewed map are unchanged

#### Scenario: Mixed revealed and unexplored footprint
- **WHEN** a proposed building covers known ground and unexplored cells that happen to contain water
- **THEN** the preview marks hidden support costs unknown and the order waits for exploration without showing water-specific bridges or spending their materials
- **AND** revealing the footprint triggers normal validation and, if legal, the bridge plans and complete updated costs become visible

### Requirement: Automatic support uses buildable researched terrain
Placing or installing a building SHALL also plan necessary bridge-like support on an assessed, revealed footprint when a removable, researched and legally buildable support terrain can make the footprint valid.
Support is added only to cells that need it, reuses suitable existing support plans and must finish
before the supported construction can finish. Ordinary floors, permanent terrain transformations,
grow-soil conversions, gravship substructure and supported provider-specific ship-design terrain
are not automatically chosen as bridges. No arbitrary chain of terrain conversions is invented.
Building research, bridge research and both sets of placement restrictions remain effective.
An unavailable support choice rejects the unsupported building plan with a reason, without leaving
half of a newly attempted support/building arrangement behind.

#### Scenario: Build across mixed ground and water
- **WHEN** an eligible building footprint covers sufficient ground and bridgeable water
- **THEN** support is planned only on the necessary water cells, and construction waits for that support while keeping all normal costs

#### Scenario: No legal support
- **WHEN** every candidate is unresearched, unbuildable, unsuitable or excluded ship terrain
- **THEN** the unsupported placement is rejected without adding an invalid building or partial new bridge plans

### Requirement: Bridge preference and cost feedback cover actual planned support
Bridge selection SHALL follow the player's effective preference order among legal researched candidates on an assessed footprint, considering all ingredients and the actual quantity of new support in that footprint.
For cells with the same support need and candidate set, prefer the first candidate whose combined
support cost is available after existing commitments and the planned building's inputs; otherwise
use the first legal researched preference and show its shortages. Different support needs can use
different legal bridges. Shared resource commitments are not spent twice. A zero-cost legal bridge
is not rejected solely for costing nothing. No fixed ten-tile stock threshold or first-ingredient-only
affordability estimate is used. Preview combines building and support costs, includes every required
ingredient once and distinguishes shortages from legal placement. Low stock can leave a valid plan
waiting for future supplies; it does not create supplies or reserve unreachable items as collected.

#### Scenario: Multi-ingredient support is unaffordable
- **WHEN** the preferred bridge has enough of its first ingredient but lacks another ingredient for the required footprint, and a lower preference can be fully supplied
- **THEN** the lower preference is selected and the preview shows its full combined building/support cost

#### Scenario: No candidate fully supplied
- **WHEN** legal researched bridges exist but none can be fully supplied
- **THEN** the first legal preference is planned with accurate shortages, and work waits for actual materials

### Requirement: Removing support handles dependent plans consistently
Canceling or removing planned support SHALL cancel and refund unfinished plans that cannot remain valid without it, independent of whether those plans are selected.
Only actual supplied inputs return. Other supported plans, already valid buildings and unrelated
nearby work remain governed by ordinary game rules. A support construction failure that leaves a
valid retry order does not unnecessarily cancel dependent work. Removing a top plan does not
silently demolish finished support or cancel support also needed by another plan.

#### Scenario: Cancel selected dependent work's bridge
- **WHEN** the player cancels a bridge needed by an unfinished building, whether that building is selected or not
- **THEN** the invalid dependent construction is canceled and its actual inputs are recoverable, with no duplicate refunds

#### Scenario: Failed support can be rebuilt
- **WHEN** bridge construction fails but leaves a valid ordinary retry plan
- **THEN** the dependent building continues waiting for that support instead of being discarded solely because a frame failed

### Requirement: Construction access remains physically safe
Supported construction and mining SHALL permit valid corner-touch work and compatible overlapping plans without treating temporary passers-by as permanent blockers.
This does not grant movement through walls, ignore reservations or allow unrelated incompatible
objects to overlap. Completion waits or uses ordinary safe movement when a pawn would otherwise
be trapped by the new building; it never deletes, crushes or teleports a passer-by to force success.
Mined products use a legal nearby placement on the miner's side of a separating wall, remaining
physically present when immediate placement cannot succeed. Ordinary deconstruct-for-construction
work outside an admitted replacement remains available. A replacement frame is not a finished wall
for attaching another structure; valid existing attachments survive a compatible replacement.

#### Scenario: Pawn crosses construction footprint
- **WHEN** a pawn temporarily crosses a safe building footprint as construction finishes
- **THEN** construction can complete through normal safe game behavior without treating the pawn as a permanent obstacle or losing the pawn

#### Scenario: Tight corner with no safe placement
- **WHEN** corner mining or construction has no legal reachable work/product position
- **THEN** it waits or fails visibly without infinite retry, remote ore placement or movement through a wall

### Requirement: Construction preferences use common settings and save isolation
Actual planning configuration SHALL use the common mod-settings controls for Global default, Current save and Use global.
Configuration comprises the default over-rock/fog control state and ordered bridge preferences;
missing/new save entries inherit global defaults. A current-play control state is saved separately
as gameplay state and is not silently reset by editing the default. New maps in the same save use
that save's control state. Bridge-order changes affect new planning, not already committed material
choices. Missing provider bridges are inactive retained preference entries; new eligible bridges
are appended deterministically without disturbing retained order. Checking Use global retains the
dormant override. At the main menu all save controls and save-changing callbacks are inactive;
global defaults remain editable. Save switching never imports the previous save's state.

#### Scenario: Global change and existing plans
- **WHEN** the player changes global bridge order while a save inherits it
- **THEN** new plans use the changed order and existing planned bridges keep their committed choices

#### Scenario: Main menu and save switching
- **WHEN** settings are opened without a loaded game and a different save is subsequently loaded
- **THEN** save editors were disabled at the menu and the loaded save uses its own inherited/overridden preferences and saved operational control state
