# Directional animal wandering

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
**Origin: original**, S066/P1-60 interview addition. Specification only; nothing implemented.
The user selected directional wandering, hunter-centered companions, shorter pen/room walks and
bounded shorter/no-move outcomes. Exact policies and defaults below remain proposals in DW-Q1–5.

## ADDED Requirements

### Requirement: Change ordinary animal idling without taking over purposeful work

DW01. Enabled directional wandering SHALL apply to supported ordinary ground-animal idle movement in wild, S065 companion and physically confined pen/room contexts.
This requires a spawned, mobile animal with no controlling mental state, duty, rope/caravan order,
combat, feeding, medical/rest, shelter/recovery or other purposeful activity. Flying, swimming,
water-seeking and specialized movement remain with their ordinary provider until their equivalent
behavior is specifically supported. Humanlike pawns and mechanoids are not included.

Proposed context precedence is active S065 idle companionship, then actual enclosed pen, then
bounded current room, then ordinary unconfined factionless wildlife. Other unconfined tame animals
retain normal colony wandering. A wild animal physically confined in a room uses confined movement
instead of trying to center on an animal outside. Admission never invents a new animal assignment.

Feeding/grazing, fleeing, shelter-seeking, following/regrouping, hunting, finishing, hauling, medical
rest, map exit, forced orders and native specialized duties keep their authority and purposeful
routes. A job does not become ordinary idling solely because its report says wandering. Enclosure
boundaries below constrain this idle behavior, not every real job the animal can perform.

#### Scenario: Hunger interrupts idling
- **WHEN** an idle animal ordinarily needs to eat and has a permitted food source beyond its next short walk
- **THEN** it can take the normal feeding route without being restricted to the short idle-walk length.

#### Scenario: Roped or specially controlled animal
- **WHEN** a handler ropes an animal, a caravan/duty controls it, or it starts flying or swimming
- **THEN** that control retains movement and no pending local idle walk overrides it.

#### Scenario: Different ordinary contexts
- **WHEN** unconfined wildlife, an active hunting companion, a confined animal and an unconfined non-companion pet are idle
- **THEN** the first three use their respective directional rules, while the pet keeps ordinary colony wandering.

### Requirement: Walk locally in a generally persistent direction

DW02. A directional idle walk SHALL consist of adjacent physical steps that generally continue the animal's recent heading, with occasional gentle variation and turns when obstructed.
The animal does not jump to a sampled distant destination, teleport, accelerate or ignore terrain
and locomotion costs. It does not revisit a cell within the same short walk to fill its requested
length; later walks can legitimately revisit places. Obstacles and boundaries can turn it around.

Proposed wildlife and companion walk lengths are **3–10 steps**; confined lengths are **2–3 steps**.
Choose within the effective range for each new walk. These are requested lengths, never minimum
success conditions: a legal one-step or two-step result remains usable, and no move remains valid.
Diagonal movement counts as one adjacent step and retains its ordinary movement cost. Walk length
is not the animal's movement speed, herd radius, hunter distance or time between idle decisions.

#### Scenario: Open ground
- **WHEN** an eligible animal idles repeatedly on unobstructed ground
- **THEN** its short walks show generally persistent direction with occasional variation, using ordinary movement timing.

#### Scenario: Boundary ahead
- **WHEN** its current direction reaches an obstacle or the current idle boundary
- **THEN** it turns to another legal adjacent direction or stops, without passing through the boundary.

#### Scenario: Only one legal step
- **WHEN** the requested confined walk is three steps but only one legal non-repeating step is available
- **THEN** the animal can take that step and wait afterward instead of rejecting it or looping to fill the request.

#### Scenario: Later return to a place
- **WHEN** another ordinary idle decision occurs after an earlier walk
- **THEN** cells visited by the earlier walk can be used again; no permanent trail of forbidden cells is created.

### Requirement: Wild herds retain their native reference and regrouping

DW03. Unconfined wild herd animals SHALL keep the ordinary eligible herd reference and effective herd distance while using directional movement within that boundary.
Do not select a hunter, unrelated species or physically unavailable herd mate merely to obtain a
center. Native herd-selection rules remain, including relevant faction, access and human-avoidance
rules; no new herd membership or global gathering order is introduced. Ordinary non-herd wildlife
wanders locally from itself without acquiring a herd-mate reference.

Within the herd boundary, outward steps turn or stop. An animal already outside the current herd
boundary can use its ordinary purposeful regrouping instead of an endless sequence of tiny local
steps. Loss/change of the reference requires a fresh current decision; an old walk never drags an
animal toward an absent or cross-map root. The native herd distance is not replaced by the configured
step count or S065's companion distance.

#### Scenario: Herd mate remains nearby
- **WHEN** a wild herd animal is within its current permitted herd distance
- **THEN** its local steps stay within that boundary and turn inward when needed.

#### Scenario: Herd reference moves away
- **WHEN** the current reference is no longer nearby enough for local herd idling
- **THEN** ordinary permitted regrouping can take over without a speed bonus or a local-search loop.

#### Scenario: No eligible herd mate
- **WHEN** the native herd decision supplies no other eligible animal
- **THEN** the animal can idle around its own valid position rather than attaching to an unrelated pawn.

### Requirement: Hunting companions use their hunter rather than another animal

DW04. A companion currently authorized by S065 SHALL use its actual hunter as the reference for nearby directional idling.
S065's one common companion-distance setting governs this behavior, initially proposed **3 cells**;
there is no duplicate radius setting here. Other pets never become substitute centers. The current
hunter's movement and access can end a local walk and require ordinary physical regrouping. The
hunter need not wait for every companion, and the animal gains no ability to pass a blocked door,
fence or forbidden route. If it cannot rejoin, it waits/yields without endless retry or teleportation.

Actual prey approach, finishing and retrieval retain S065 authority and purposeful movement; this
walker does not grant another target, claim, carrying allowance or attack. S065's temporary yielding
to needs and its saved follow choices remain effective. Turning directional wandering off changes
idle style only: supported ordinary near-hunter movement can continue under S065, with the same
permissions, needs and handoff rules. It never enables an otherwise unavailable companion feature.

#### Scenario: Several companions at a station
- **WHEN** a hunter waits at an S064 station with several eligible S065 companions
- **THEN** each idles near the hunter with ordinary collision/access rules, without following a chain of other pets.

#### Scenario: Hunter leaves the nearby area
- **WHEN** the hunter moves beyond the companion's effective distance during an idle walk
- **THEN** the animal abandons stale idle steps and can physically regroup using the current hunter and normal routing.

#### Scenario: Prey assistance starts
- **WHEN** S065 grants one companion the actual prey claim
- **THEN** its purposeful approach/finishing/retrieval takes over without a short-walk limit or a second claim from this feature.

#### Scenario: Directional style is disabled
- **WHEN** directional wandering is turned off while S065 remains otherwise supported and enabled
- **THEN** ordinary near-hunter movement remains possible and saved animal permissions are unchanged.

### Requirement: Confined animals roam within their real pen or room

DW05. Ordinary confined animals SHALL use short local walks from their own positions, staying within their current physical enclosure without choosing another herd animal or pen marker as a center.
Proposed pen scope is a currently enclosed native pen containing the animal, including its connected
parts. If no such pen applies, a bounded, non-doorway current room qualifies; a roof is not required,
and a roof over otherwise open terrain alone does not create confinement. A pen's animal acceptance
filter is a management choice, not a wall: changing it does not teleport the animal or immediately
erase physical confinement while ordinary relocation is pending. Allowed areas/access still apply.

Multiple markers in one connected enclosure do not create duplicate wandering jobs or permanent
per-animal pen assignments. A pen spanning connected rooms is the broader physical context, but
local idle steps still obey the conservative door policy below. Local wandering does not promise
to visit every part of an enclosure or find food/shelter by itself.

A broken/removed enclosure or altered room boundary invalidates the old local plan and requires
a fresh context. An animal outside a valid enclosure is not artificially imprisoned inside the
old outline. Native pen-marker approach, roaming, escape, shelter and relocation remain separate
normal behavior when applicable; this feature does not make an open pen safe.

#### Scenario: Confined herd species
- **WHEN** a herd animal is inside an enclosed pen or bounded room and another herd animal is outside
- **THEN** ordinary idle steps stay local inside its enclosure and do not center on the outside animal.

#### Scenario: Unroofed room and roofed open ground
- **WHEN** one animal occupies a bounded unroofed courtyard and another stands beneath a roof on open terrain
- **THEN** only the bounded space qualifies as room confinement solely from that geometry.

#### Scenario: Pen filter changes
- **WHEN** a marker stops accepting an animal that is still physically inside the enclosed pen
- **THEN** its ordinary local idling remains confined until other legitimate movement takes over; no ghost transfer or permanent reassignment occurs.

#### Scenario: Fence breach or room merge
- **WHEN** a fence/wall is removed or a passage changes the current enclosure during a planned walk
- **THEN** the old boundary is re-evaluated before further affected idle steps and subsequent behavior uses current geometry.

### Requirement: Idle safety and access remain real and current

DW06. Every directional step SHALL obey current physical movement, permitted area/access, ordinary collision, legal diagonal-corner and destination-reservation rules.
No idle step bashes a fence/door or claims another pawn's reserved destination. Proposed conservative
idle policy avoids entering doorway and fence cells, even when a door is open; purposeful travel
through a permitted door or animal flap is unaffected. This is an idle preference, not a new wall
or a restriction on feeding, following, shelter or other legitimate jobs.

Known traps/fire, dangerous or wander-avoided terrain, deadly danger and danger exceeding the
animal's effective idle tolerance are excluded. Relevant sunlight sensitivity and native pollution/
rot-stink avoidance remain effective; proposed ordinary animal pollution/rot-stink avoidance uses
the inspected native **less than 50%** toxic-environment-resistance threshold. This does not grant
immunity at that threshold or override S024/S030's actual susceptibility/recovery decisions.

When S024 preferences are enabled, respect its applicable destination-room choices, shelter
protection and already-sheltered movement budget. A purposeful move to shelter or S030 recovery
can cross the exposure its own rules permit; it is not rejected as an unsafe idle walk. Actual
injury, food, sleep, training and environmental consequences remain unchanged.

#### Scenario: Door beside an idle animal
- **WHEN** a legal food or shelter route passes through a doorway beside a confined animal
- **THEN** its ordinary idle walk does not enter the doorway, while the appropriate purposeful job can use it normally.

#### Scenario: Blocked diagonal or occupied cell
- **WHEN** a diagonal would cut an illegal corner or another pawn/reservation blocks the next cell
- **THEN** the animal turns, shortens or waits without clipping through the blocker or stealing the destination.

#### Scenario: Susceptible animal and pollution
- **WHEN** an animal below the effective ordinary avoidance threshold considers a polluted or rot-stink cell
- **THEN** that cell is not selected merely to complete an idle walk, while actual immunity and enabled shelter/recovery rules are assessed independently.

#### Scenario: Shelter beyond a short local walk
- **WHEN** native pen shelter behavior, enabled S024 shelter choice or S030 recovery selects an eligible destination farther away
- **THEN** that permitted purposeful movement retains its own route and takes precedence over idle confinement/length preferences.

### Requirement: Trapped or invalidated walks terminate without repeated job churn

DW07. A blocked local idle decision SHALL finish in bounded work, choosing a shorter legal walk or normal waiting without repeatedly searching until the requested length succeeds.
One-cell rooms, fully occupied neighbors, narrow passages and newly blocked paths are ordinary
outcomes. No recursive restart, repeated immediate failure/reschedule, distant idle destination
search or forced relocation is used to satisfy a minimum. Waiting retains the effective ordinary
idle cadence and remains interruptible by normal work/needs/orders; no arbitrary long lockout is added.

A path that becomes invalid is stopped before the next affected step. Reconsider current context
and priorities once; absent a new controlling job, wait before another ordinary idle attempt.
Relevant geometry/permission changes are usable at later ordinary decisions without clearing an
unrelated global cache. Crowded animals do not continuously displace each other to force movement.

#### Scenario: One-cell room
- **WHEN** an eligible animal has no legal neighboring cell in a one-cell room
- **THEN** it stays there and waits at normal cadence, with no endless search, repeated immediate job failures or loss of response to normal needs.

#### Scenario: Narrow two-cell enclosure
- **WHEN** the enclosure contains fewer reachable distinct cells than the configured walk length
- **THEN** the animal uses the available short movement or waits instead of bouncing within one walk to fill the count.

#### Scenario: Block after selection
- **WHEN** construction or another obstruction invalidates a selected next step before the animal takes it
- **THEN** the animal stops/reassesses and waits if still idle, without requesting a distant route to the stale endpoint.

#### Scenario: New order while waiting
- **WHEN** an animal waiting after a failed local attempt receives a legitimate higher-priority order or need
- **THEN** that work can proceed immediately under its ordinary rules without waiting for a special wandering retry lockout.

### Requirement: Loading and changing contexts do not resume stale movement

DW08. Save/load, map changes and current job/authority changes SHALL preserve the animal's real position and valid work without replaying obsolete idle steps.
Transient heading and an unfinished local plan need not reproduce the exact pre-save trajectory.
On load, an old local walk without current validated context can end once and wait/reconsider;
it must not teleport, replay movement, repeatedly fail or force a global route to its former target.
Existing valid purposeful work is not canceled merely because this walker lacks a local plan.

Death, despawn, faction/context changes, missing hunter, changed enclosure, feature disable or lost
integration clear stale idle authority. Another animal, reused job or newly loaded save must not
inherit the old path/reference. Disabling the feature safely returns later idle decisions to
their supported ordinary providers without changing positions, enclosures or other mods' settings.

#### Scenario: Save during a local walk
- **WHEN** the game loads with an unfinished local idle walk but no current valid plan
- **THEN** the animal remains at its saved position and resumes through a bounded current decision rather than repeatedly pursuing the old endpoint.

#### Scenario: Map or hunter changes
- **WHEN** an animal changes maps or loses its companion authority while idle
- **THEN** no previous-map cells or missing hunter remain movement targets.

#### Scenario: Reused job or another save
- **WHEN** later gameplay reuses an ordinary job or opens a different save
- **THEN** it cannot inherit another animal's local route or enclosure state.

### Requirement: Expose tuning through shared settings and an appropriate play toggle

DW09. Common Animals / Wandering settings SHALL provide availability default on, wildlife/companion length default 3–10 steps and confined length default 2–3 steps.
Each range accepts whole numbers **1–10**, with minimum no greater than maximum. Reject invalid,
fractional, reversed or overflowing input with an explanation and preserve the prior valid values.
Length changes apply to new walks, never extend a live walk or turn the minimum into a success
requirement. Companion distance remains S065's single setting. Entity animal/pen/area choices remain
in their existing native gameplay controls, not per-entity mod-settings rows.

Every setting uses the common Global default / Current save / Use global path. New/missing saves
inherit live global defaults. Use global preserves dormant overrides and disables their editors;
unchecking restores or seeds them. Main-menu save editors, inheritance controls and every save
mutation callback are disabled. Global changes never overwrite explicit save overrides.

A small **bottom-right** Directional animal wandering button shows the current effective state.
Toggling it writes only this save's availability override through the same common path, leaving
global defaults and other saves intact; returning to inheritance uses Use global. No duplicate
toggle state, pawn gizmo, new bottom tab or whole-pathfinder mode is introduced. The button is
disabled with a reason when required movement support is unavailable, without changing the saved
preference. Settings, tooltips and unavailable explanations support all seven baseline languages.

#### Scenario: Adjust confined length
- **WHEN** the player changes the valid confined range while an animal is already moving
- **THEN** its next idle walk uses the new range while the current walk still ends normally or stops for changed safety.

#### Scenario: Invalid range
- **WHEN** a range is reversed, fractional or outside 1–10
- **THEN** the previous valid range remains and the UI explains why the edit was rejected.

#### Scenario: In-play toggle and inherited default
- **WHEN** the player toggles directional wandering in a save that inherited its global availability
- **THEN** only that save gains the corresponding explicit override; Use global can restore inheritance and other saves/globals stay unchanged.

#### Scenario: Main menu and dormant override
- **WHEN** the player opens settings with no loaded save or re-enables Use global in a loaded save
- **THEN** main-menu save mutations are impossible, and the loaded save retains its dormant override while using the current global value.

### Requirement: Compose one movement decision with supported native and optional behavior

DW10. Directional movement SHALL compose directly with S024/S030 safety, S064/S065 hunting and native pen/room facts, using current shared data rather than separate source-feature scans or conflicting movement orders.
There is one actual movement/work authority at a time. S021 hauling, S043/S087 grazing and other
purposeful features retain their own resource/cargo behavior. No altered nutrition, growth, yields,
training, speed, capacity, animal population or full experimental pathfinding system is included.

Compatible species and harmless foreign patches remain usable. Unknown required movement/provider
behavior leaves only the affected route inactive, with a contextual limitation and one bounded
startup warning per skipped enabled patch identifying the target requirement, failed condition,
unavailable behavior and fallback. Do not reject merely because another patch exists, remove
foreign patches, silently change another mod's settings or run two planners for the same walk.
Partial admission must not suppress ordinary destinations without a working safe movement path.
XML changes preserve unrelated decision branches and content. No source package impersonation,
all-mod compatibility or whole-game performance claim follows from this specification.

#### Scenario: Compatible and unsupported providers
- **WHEN** ordinary compatible modded animals use supported ground movement while a different provider changes required path semantics
- **THEN** compatible paths remain available and only the unsupported route falls back with the required explanation.

#### Scenario: Incomplete movement support
- **WHEN** local destination selection is available but safe delivery/revalidation of that path is not
- **THEN** the affected directional route stays inactive as a unit, without stranding an animal after suppressing its normal movement.

#### Scenario: Other planner is present
- **WHEN** another enabled mod participates in the same movement path
- **THEN** the required effective behavior is checked; supported cooperation works, otherwise the affected owned route declines safely without changing the other mod's mode.
