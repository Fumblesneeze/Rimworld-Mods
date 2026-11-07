# Construction access

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S060/P1-54, Smarter Construction. Observable draft; nothing implemented.
Source-derived limits and proposed corrections remain subject to CT-Q1–4 review before code.

## ADDED Requirements

### Requirement: Automatic blocking construction preserves unfinished access

CT01. The feature SHALL check the small adjacent spaces described in CT02 before accepting and
immediately before completing an automatic supported frame whose finished result is impassable. It SHALL
defer completion when a checked space contains another player-faction pawn or an unfinished blueprint
or frame. Player pawns include animals and colony mechs, not only human colonists. The acting builder
alone follows the safe-side movement rule instead. Unrelated loose items, completed furniture,
visitors and hostile pawns SHALL NOT by themselves activate this source-derived protection.

Deferral SHALL retain the construction order, actual supplied resources and earned work, with an
inspectable reason. Other eligible work can continue. When the obstruction changes, ordinary work
selection SHALL reconsider the frame using current circumstances. This does not issue movement
orders to the other pawn or automatically finish, delete or cancel the enclosed work.
Preparatory clearing and delivery SHALL not be vetoed merely because the eventual closing step
needs a later enclosure check.

#### Scenario: Close a wall around another colonist
- **WHEN** finishing an automatic wall would leave another colonist in a checked small space
- **THEN** the wall remains unfinished with its materials and work intact, and its reason identifies the access problem.

#### Scenario: Protect an animal and unfinished work
- **WHEN** a checked space contains a player animal, colony mech, blueprint or frame
- **THEN** automatic closing completion waits even if no other human colonist is there.

#### Scenario: Empty pocket or loose goods
- **WHEN** a checked space contains only ordinary loose goods or completed furniture
- **THEN** this safeguard alone does not forbid completion or promise to protect later hauling access.

#### Scenario: Other pawn leaves
- **WHEN** the other protected pawn leaves and no protected unfinished work remains in the space
- **THEN** subsequent ordinary selection can resume the frame without requiring its recreation.

#### Scenario: Clear an obstruction before building
- **WHEN** the ordinary construction workflow first returns a permitted clearing job
- **THEN** the future completion safeguard does not suppress that preparatory action as though it were already finishing the wall.

### Requirement: Small-space limits are explicit and do not promise universal safety

CT02. The check SHALL consider the entire rotated completed footprint and its side-adjacent spaces
on the target map. Its source-derived small-space scope is connected traversable cells joined through
their north, south, east or west sides, after treating that footprint as blocked. A connected space
with fewer than **100 cells** is checked; reaching **100 cells** places it outside this bounded
protection. This is not a named-room, roof, temperature or general evacuation test. A diagonal-only
gap SHALL NOT count as a side connection for this check.

An adjacent currently unwalkable cell containing unfinished work SHALL also be protected if it has
no traversable side approach after completion. Out-of-map cells SHALL never become contents, work
positions or a fabricated escape route. Doors and supported special passages SHALL follow their
actual admitted movement meaning. The feature's explanation SHALL state the size/scope limit and
forced-order bypass; it SHALL NOT promise that large enclosures or every kind of occupant is safe.

#### Scenario: Boundary at one hundred cells
- **WHEN** otherwise comparable completion would bound a 99-cell space and a 100-cell space
- **THEN** the former receives small-space protection and the latter is outside this safeguard's stated coverage.

#### Scenario: Rotated large structure
- **WHEN** a multi-cell blocking result is rotated and would close an approach at its far corner
- **THEN** the complete rotated footprint participates, not only its origin cell.

#### Scenario: Only diagonal contact remains
- **WHEN** a small space has only a diagonal connection after the planned completion
- **THEN** that diagonal does not exempt it from this side-connected protection.

#### Scenario: Single inaccessible work cell at the edge
- **WHEN** a pending frame occupies a single blocked cell beside a map edge and loses its remaining side approach
- **THEN** it can be protected without examining nonexistent map contents or treating an outside cell as usable.

### Requirement: The builder physically finishes from a permitted outside position

CT03. Automatic work SHALL defer final completion if only the acting builder would be enclosed,
or the builder stands anywhere in the completed footprint, and first move the builder to a permitted
side-adjacent construction position outside the checked small spaces and footprint. The position
must be reachable before completion and valid for performing the same work. Choose a nearest valid
position by supported ordinary route distance, with a stable tie choice. Recheck access and the
enclosure before finishing after the move. A builder already in a valid outside position need not
move merely because another safe position exists.

No suitable reachable position means waiting with a reason. Relocation SHALL respect the current
worker's ordinary allowed area, forbidden state, reservations, door and construction-danger rules;
it SHALL NOT teleport, ignore restrictions, displace another pawn or open an artificial passage.

#### Scenario: Finish from outside
- **WHEN** a builder alone occupies a protected pocket and can legally reach an outside construction position
- **THEN** the builder walks there before the frame becomes a finished wall.

#### Scenario: Safe-looking cell is inaccessible
- **WHEN** the only geometrically outside position is forbidden, reserved or unreachable for this builder
- **THEN** the wall waits rather than completing or moving the builder there by force.

#### Scenario: Builder inside a multi-cell footprint
- **WHEN** the builder stands in a non-origin cell that the finished structure will occupy
- **THEN** the builder must leave the whole footprint through a valid movement path before automatic completion.

#### Scenario: Conditions change during relocation
- **WHEN** another protected pawn enters or the selected route becomes invalid before completion
- **THEN** the renewed check defers the frame safely and does not trust the earlier movement plan.

### Requirement: Nearby constrained frames compose with ordinary work priorities

CT04. Automatic choice among eligible frames SHALL first preserve the enabled S058 categories:
ordinary structures, then terrain construction, then the ordinary spike trap. Within a category,
an impassable-result frame at a straight-line horizontal distance of at most **15 cells** from the
builder SHALL receive the local construction preference before other frames. Among those local
frames, prefer more currently unwalkable neighboring cells around the full footprint, including
corner neighbors; a cell is counted once. A target already on an unwalkable origin receives no
extra neighbor-count score. Remaining equal choices use ordinary distance and discovery order.
Without S058, this local preference applies within the remaining admitted ordinary priority policy.

Only currently permitted and reachable work participates. The preference SHALL NOT override a
player's forced target, work-type priorities, required skills or quality-builder eligibility, or
replace a valid job already underway. Material delivery, repairs, mining and deconstruction retain
their separate order. This is a local ordering rule, not a guarantee of the globally best build plan.

#### Scenario: Fill the constrained end first
- **WHEN** two eligible nearby wall frames share a category but one has more blocked neighbors
- **THEN** automatic finishing prefers that frame before the less constrained one.

#### Scenario: Ordinary category stays above a floor
- **WHEN** a valid ordinary structure and an impassable terrain frame both qualify for local preference with S058 enabled
- **THEN** the structure category wins before their local neighbor scores are compared.

#### Scenario: Exact local boundary
- **WHEN** equivalent blocking frames are 15 and 16 cells away
- **THEN** only the 15-cell frame receives this local preference; the other remains eligible ordinary work.

#### Scenario: Materials still need delivery
- **WHEN** a nearby preferred blueprint lacks materials while a lower-ranked frame can be finished
- **THEN** the preference does not invent a finish job or reorder the separate delivery workflow.

### Requirement: Explicit forced construction bypasses only these safeguards

CT05. An explicit player-forced construction action SHALL bypass this feature's enclosure veto,
safe-side relocation and automatic target preference for that action, allowing intentional enclosure.
Ordinary building rules, physical resources, skills, permissions and failure consequences still
apply. This bypass SHALL NOT become a permanent property of the frame or leak into later automatic
jobs. The feature's extra stalled-journey recovery SHALL not replace the game's handling of a forced
action. Unsupported external forcing tools SHALL not be presumed to use the same semantics.

#### Scenario: Intentionally seal a space
- **WHEN** the player forces an otherwise permitted frame to finish despite a protected small pocket
- **THEN** this safeguard permits the requested completion without its automatic relocation.

#### Scenario: Forced order is canceled
- **WHEN** the player cancels the forced action and the frame is later selected automatically
- **THEN** the ordinary enclosure checks apply again.

#### Scenario: Force cannot supply missing resources
- **WHEN** the player tries to force a frame whose ordinary requirements are unsatisfied
- **THEN** this bypass does not grant the resources, skill or access needed to build it.

### Requirement: Waiting movement and reload preserve one real construction

CT06. Deferral and relocation SHALL preserve one frame and its actual resource contents, earned
work, quality intent and construction choices. Completion costs, failure chances, final quality,
style, storage membership and ordinary completion consequences remain governed by the existing
construction workflow. A finished building and its completion effects SHALL occur once, only after
real permitted work. Waiting on a fully worked frame SHALL NOT repeatedly perform work, award skill
experience or roll extra construction failures merely because the safety check is retried.

Drafting, canceling, changing work assignments, despawning the target, death or map changes SHALL
cancel obsolete owned movement/resume intent. They SHALL not erase the frame or duplicate resources.
Save/load SHALL preserve legitimate unfinished work and re-evaluate current conditions before
resuming; it SHALL not duplicate a queued return job. Waiting on this feature SHALL not spend a
quality-rebuild attempt or replacement budget as if a building had been completed.

#### Scenario: Save during the outward walk
- **WHEN** the game is saved after the builder starts relocating but before completion
- **THEN** loading retains the same physical frame/materials and resumes or safely abandons the one pending continuation after checking current conditions.

#### Scenario: Draft before returning to the frame
- **WHEN** the player drafts the relocating builder
- **THEN** drafting takes effect and no stale automatic return overrides it or appears twice later.

#### Scenario: Fully worked but still blocked
- **WHEN** a completed-work frame must wait for another pawn to leave
- **THEN** repeated reconsideration grants no further work experience or completion/failure effects while waiting.

#### Scenario: Quality cycle waits for access
- **WHEN** a quality-controlled rebuild reaches an unsafe closing step
- **THEN** the access wait does not count as a failed quality result or consume another rebuild attempt.

### Requirement: Stalled construction journeys recover without retry loops

CT07. The feature SHALL perform a fresh progress check after continuous lack of movement for
**300 game ticks** while a worker is trying to travel during an owned automatic construction journey,
observed within the next **50 game ticks**. Legitimate door waiting, a supported level transfer, paused gameplay or
actual construction work SHALL NOT by themselves count as a stalled journey. If the journey is
truly stalled, release that attempt through ordinary job recovery and let work selection reconsider
current conditions. Do not teleport, complete the frame or immediately enqueue the same failed
journey repeatedly. Movement or a new journey resets the observation interval.

A failed relocation SHALL not repeatedly retry an unchanged unreachable position. Keep the frame
available for later legitimate work and reconsider when relevant access, geometry, pawn position or
explicit player orders change. This recovery is local to construction, not a general pathfinding fix.

#### Scenario: Route becomes blocked
- **WHEN** an automatic builder remains unable to move for the stated interval with no legitimate waiting state
- **THEN** the attempt ends once and ordinary scheduling can choose other work while preserving the frame.

#### Scenario: Working is not being stuck
- **WHEN** the builder stands still while performing valid construction
- **THEN** the journey watchdog does not cancel that work because 300 ticks elapsed.

#### Scenario: A supported transfer is in progress
- **WHEN** a level provider legitimately holds a builder in transit longer than the ordinary observation interval
- **THEN** the admitted integration does not treat that transfer alone as a construction stall.

#### Scenario: Unchanged failed position
- **WHEN** the same relocation remains inaccessible after an attempt fails
- **THEN** automatic retries do not create a move/resume loop until relevant circumstances change.

### Requirement: Geometry and optional providers have explicit boundaries

CT08. Unaffected construction workflows SHALL retain their existing behavior. These include ordinary
nonblocking frames, standard floor/roof work and installation of minified buildings. This feature
does not promise enclosure prevention for every construction-like action. Owned quality rebuilds
use this decision directly at their actual closing
step. Owned replacements constructed from resources SHALL use it when completion adds a newly
blocking footprint; installation of an already-built minified replacement remains outside this scope.
Unchanged already-blocking replacement space SHALL not be treated as a newly closed passage. The
replacement's materials, contents, damage and interrupted-work recovery remain authoritative.

Optional robots, vehicles, alternate levels, stairs and special passages SHALL receive enhanced
protection only where their real movement and completion semantics are supported. A valid supported
alternate exit prevents a false trapped-space verdict; a mere nearby portal or provider name does
not prove an exit usable by the affected pawn. Unavailable provider support SHALL leave that affected
enhancement inactive with an explanation, rather than invent a route or permanently forbid normal
construction. Ordinary supported maps and unrelated feature parts remain usable.

#### Scenario: Install a packed structure
- **WHEN** the player installs a minified building
- **THEN** its ordinary installation proceeds without a false promise that these frame checks protect it.

#### Scenario: Owned replacement adds a blocking extension
- **WHEN** an owned replacement constructed from resources will newly close cells around protected unfinished work
- **THEN** its final closing step uses the same access decision while retaining its existing physical replacement state.

#### Scenario: Real alternate exit
- **WHEN** an admitted level provider offers a currently usable exit from the candidate pocket
- **THEN** the pocket is not falsely treated as sealed solely because its local floor is small.

#### Scenario: Unknown alternate geometry
- **WHEN** a provider's route or completion contract cannot be supported
- **THEN** that enhancement declines with a bounded explanation and does not claim safety or create a fictitious passage.

### Requirement: Controls current facts and startup fallback remain honest

CT09. The feature SHALL use existing construction designations, contextual forced actions and
inspect information, without a new bottom tab, settings screen or view toggle. This source-derived
draft selects no user-adjustable configuration or persistent source-settings import. The stated
100-cell, 15-cell and recovery limits SHALL be documented, with any future configuration using the
common global/save settings path. Information viewing SHALL not move workers or perform construction.

New selections and final checks SHALL use the correct map, footprint, acting worker and current
occupants/access. Identical coordinates on another map or a previous worker's result SHALL not
control this decision. Changed geometry and newly completed buildings SHALL affect subsequent checks;
loading or returning to a map SHALL not revive old decisions.

Each enabled integration SHALL check its documented startup requirements. Unsupported integration
stays inactive with a bounded warning identifying the skipped behavior, reason and remaining ordinary
workflow. Compatible unrelated modifications SHALL not alone disable it. If source Smarter
Construction is active, owned overlap SHALL remain inactive with a source-control explanation;
never apply two relocation or completion decisions. No source package/assembly impersonation,
automatic save migration, general safe-removal or unverified multiplayer support is promised.

#### Scenario: Two maps share coordinates
- **WHEN** two frames occupy identical coordinates on maps with different neighbors
- **THEN** each receives its own current geometry and occupants decision.

#### Scenario: Another worker takes over
- **WHEN** a second builder approaches a frame previously checked for the first
- **THEN** self-enclosure and permitted movement are evaluated for the second builder rather than reused from the first.

#### Scenario: Completion integration fails admission
- **WHEN** the current game cannot support the required completion decision
- **THEN** the enhancement stays inactive, logs its missing protection and leaves the ordinary workflow and existing physical work recoverable.

#### Scenario: Original source is active
- **WHEN** Smarter Construction is co-installed and active
- **THEN** source ownership is stated and the owned overlapping relocation, ranking and completion behavior does not also run.
