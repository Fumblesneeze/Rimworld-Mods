# Equipment power budgets

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S053 replacement draft; no implementation or acceptance claimed.

## ADDED Requirements

### Requirement: RP01 Each supported device has one power policy
The feature SHALL apply equipment budgets to player-owned electrical workbenches, research benches,
deep drills, scanners, televisions, powered doors, turrets, ordinary hydroponics and their supported
powered research/medical facilities. Optional equipment requires individually supported behavior.
Generators, batteries, fuel-only devices, continuous climate control and unknown autonomous machines
retain their ordinary behavior. A matching name or a power connection alone does not establish support.

For devices shared with LightsOut, LightsOut's actual-work activity rules SHALL feed one RePowered
rate calculation. RePowered costs take precedence over LightsOut percentages while equipment budgets
are enabled. Other LightsOut equipment and room lighting keep their existing rules. Disabling
equipment budgets returns shared devices to LightsOut rates and source-exclusive devices to native
rates, cancels this feature's work-start restriction and retains saved rate choices for re-enabling.
This intentional-disable fallback does not apply to a failed combined device group: failed rate or
activity admission leaves that device native under RP09, including suppression of LightsOut rate
changes, the additional forecast restriction and owned active/idle claims for that device.

#### Scenario: Shared workbench
- **WHEN** a supported bench starts and stops work with both owned source features enabled
- **THEN** it follows one activity state and one configured RePowered rate, without multiplying that result by LightsOut's rate

#### Scenario: Non-overlapping equipment
- **WHEN** ordinary room lamps, mech charging or ritual speakers operate alongside a RePowered bench
- **THEN** their LightsOut policies continue without acquiring the bench's active multiplier

#### Scenario: Disable equipment budgets
- **WHEN** the player disables equipment budgets while a bench and turret are active
- **THEN** the bench follows its LightsOut policy, the turret returns to native consumption, the additional work-start restriction ends and rate choices remain available for re-enabling

#### Scenario: Unsupported device or ownership change
- **WHEN** a generator, unknown autonomous machine or non-player building is encountered, or a supported building leaves player ownership
- **THEN** this equipment-budget feature leaves or restores its ordinary consumption and does not turn a consumer into a generator

### Requirement: RP02 Work and facility demand follows useful activity
The feature SHALL classify supported benches, research, drilling, scanning and television viewing
as active during actual use, including a pawn beginning work while already at the target. A queued
bill, reservation or approaching pawn alone does not activate ordinary work equipment. A supported
autonomous machine is active while actually progressing its work without a pawn. Linked research
facilities are active while any supported linked bench is researching; linked medical facilities
are active while any valid linked bed has an occupant. Ordinary hospital and medical-designated
beds use the same valid-link rule. Facilities keep native eligibility and benefits.

Activity changes SHALL take effect by the next simulation update, without an equipment shutdown
delay. Ending one user's activity does not idle equipment still needed by another user. Powered
readiness must remain available to start legitimate work; native disconnection, flick-off, breakdown,
EMP, fuel, ingredient, skill and other restrictions remain authoritative.

#### Scenario: Work begins at the target
- **WHEN** a pawn already beside an idle supported bench starts a valid bill
- **THEN** the bench enters active consumption as work begins and the pawn can progress normally

#### Scenario: Reservation without work
- **WHEN** a pawn reserves a scanner or research bench but is still walking or cancels before use
- **THEN** the device remains idle rather than charging active power for the reservation alone

#### Scenario: Shared viewers and facilities
- **WHEN** one of two TV viewers leaves, or one of two researchers using benches linked to a powered analyzer stops
- **THEN** the shared device stays active until the last relevant use ends

#### Scenario: Occupied medical bed
- **WHEN** a patient occupies a valid linked medical bed and later leaves
- **THEN** its powered medical facility becomes active and then idle, unless another linked bed remains occupied

#### Scenario: Autonomous work and waiting
- **WHEN** an admitted autonomous machine progresses a recipe and then waits for ingredients or output space without progressing
- **THEN** it uses the supported active and idle states respectively, preserving any independently required continuous demand

#### Scenario: Native refusal
- **WHEN** a bench is flicked off, broken or lacks required resources
- **THEN** standby readiness neither grants work permission nor hides the native reason

### Requirement: RP03 Doors turrets and hydroponics have explicit activity
The feature SHALL charge powered doors their active rate while opening or closing and their idle
rate while stationary, including fully open held or blocked doors. It must preserve native movement,
obstruction, access and power-dependent opening rules. Turrets are active while they have a valid
current attack target, including aiming and reloading with that target, and idle when the target
ends; powered readiness remains available to acquire a target.

An ordinary hydroponics basin SHALL be active while any planted crop occupies it and idle when
empty, including plants that are mature, sleeping or temporarily unable to grow. This does not
change growth, fertility or crop eligibility. A basin with continuous lighting or another extra
service retains that service's required demand unless a supported combined policy separates it.

#### Scenario: Both door directions
- **WHEN** a powered door opens, remains fully open and then closes
- **THEN** it draws active power during each movement and idle power while stationary

#### Scenario: Held or obstructed door
- **WHEN** a door is held open or prevented from closing by an obstruction
- **THEN** it stays open under native rules and does not consume movement power indefinitely

#### Scenario: Turret target lifecycle
- **WHEN** an idle turret acquires a valid target, aims, reloads and later loses that target
- **THEN** it becomes active on acquisition, remains active while the target remains valid and returns to idle on loss

#### Scenario: Occupied and empty basin
- **WHEN** the last plant is removed from a basin and a new crop is later planted
- **THEN** its consumption changes to idle and back to active without changing the crop's normal requirements

#### Scenario: Self-lit basin
- **WHEN** a basin supplies light independently of crop occupancy
- **THEN** that lighting and its required power remain intact; absence of a supported combined policy leaves the basin's native power behavior intact with a startup explanation

### Requirement: RP04 Configured rates have explicit defaults and precedence
The feature SHALL enable equipment budgets by default, with generic idle consumption 10 W,
generic active consumption 2.5 times the device's ordinary active demand and moving-door consumption
10 times its ordinary powered demand. The vanilla preset option starts On and supplies these pairs:

| Building | Idle | Active |
| --- | --- | --- |
| Electric crematorium | 200 W | 750 W |
| Electric smelter | 400 W | 4500 W |
| High-tech research bench | 100 W | 1000 W |
| Ordinary hydroponics basin | 5 W | 75 W |

An explicit building-type override SHALL take precedence over a supported external rate definition,
then an enabled vanilla preset, then the generic category rate. Disabling presets exposes the next
applicable rate without deleting overrides or disabling hydroponics activity. The UI states the
selected rate source. Conflicting external definitions without an established precedence leave
that building on its native power behavior with one startup warning; an explicit valid player rate
can resolve a rate-only conflict, but cannot establish missing activity support.

Generic multipliers SHALL use current ordinary demand, including native research and supported
provider adjustments. Explicit W pairs and vanilla presets are absolute rate choices; their UI
states that research does not scale these selected values. Independent continuous provider costs
must not be erased by any rate choice. Native zero-consumption conditions remain zero. Otherwise
every electrical standby value below 0.01 W, including zero, uses the shared readiness floor, capped to ordinary
positive demand if it is smaller; active zero is not an accepted rate.

#### Scenario: Generic rates
- **WHEN** a supported generic device normally draws 100 W and has no override or preset
- **THEN** it draws 10 W idle and 250 W active; a powered door with that ordinary demand draws 1000 W while moving

#### Scenario: Preset precedence and reset
- **WHEN** the electric smelter has presets enabled and a player sets its type to 20 W idle and 2000 W active
- **THEN** the override wins; resetting that type restores 400 W idle and 4500 W active without altering other types

#### Scenario: External definition wins over preset
- **WHEN** an admitted external definition supplies a distinct smelter pair and no player override exists
- **THEN** that external pair takes precedence over the vanilla preset and the settings row identifies its source

#### Scenario: Research and absolute choices
- **WHEN** research changes a generic device's ordinary active demand from 100 W to 80 W
- **THEN** its default active rate becomes 200 W, while a type with an explicit 300 W active choice stays at that clearly labeled absolute value

#### Scenario: Zero standby and native zero
- **WHEN** the player sets standby to zero for an otherwise positive consumer, or native conditions already suppress consumption to zero
- **THEN** the first retains the documented readiness floor and the second stays at zero without phantom consumption

#### Scenario: Positive standby below the floor
- **WHEN** a normally 100 W bench is configured for 0.001 W standby
- **THEN** its effective standby is 0.01 W and the UI explains the floor rather than bypassing it because the entered number is positive

#### Scenario: Blackout and recovery at minimum standby
- **WHEN** usable supply disappears and later returns for a supported bench configured at zero or positive sub-floor standby
- **THEN** it cannot work through the blackout solely because its draw is tiny, and it can resume legitimate work after native power returns

#### Scenario: Unresolved external rate conflict
- **WHEN** two admitted rate sources disagree for the same type without a supported precedence
- **THEN** that type stays native with a bounded explanation until the player supplies a valid resolving rate, while unrelated equipment remains supported

### Requirement: RP05 Additional work-start checks account for the real net demand
The feature SHALL default Prevent underpowered work starts to On. Before a supported electrical
bill starts, it considers the extra consumption of changing that bench from its current idle draw
to its configured active draw, any newly required shared facilities, other ongoing network demands
and other starts already admitted at that moment. Each affected power network is accounted for
separately, using only its connected devices, generation and usable batteries. The bench's network
and any network serving a facility natively required for that work must each pass. A nonnegative
projected balance passes; otherwise usable connected battery reserves must cover that network's deficit for the
configured reserve horizon, initially 8⅓ simulation seconds, equivalent to 500 normal simulation
updates. Equality at the reserve boundary permits starting. The horizon is a common scoped setting.
Native-optional facilities do not become prerequisites: an unavailable one supplies no benefit and
does not block work. An optional facility on another circuit is still accounted for there, but its
forecast shortfall alone does not block this bench; native power determines whether its benefit
remains available. All newly active loads on the bench's own circuit affect its forecast, including
optional facilities sharing that circuit. No reserve is promised across disconnected networks.
Network splits, merges or disconnections before actual work starts invalidate the old forecast and
release its pending admissions; a fresh decision uses the current networks.

This extra check SHALL never grant native work permission, apply to a bench that can work without
electricity, reject a merely informational bill listing, or preempt already running work. Native
power loss can still interrupt it. Rejected automatic work waits; a direct attempt explains the
power shortage and required budget. Restored supply allows an ordinary retry without a manual reset.
Disabling this check leaves ordinary power behavior and configured rates in force.

#### Scenario: Incremental rather than full active draw
- **WHEN** a bench changes from 10 W idle to 250 W active on a network with 240 W spare and no batteries
- **THEN** its start is permitted because the additional 240 W is covered

#### Scenario: Battery horizon
- **WHEN** the projected network has a deficit and connected usable reserves cover exactly the configured horizon
- **THEN** the start is permitted; reserves below that boundary reject it with the power-shortage explanation

#### Scenario: Competing starts and shared facilities
- **WHEN** two jobs seek the same spare power, including a facility that both would use
- **THEN** each decision includes already admitted demand, counts the shared facility only once and does not promise the same reserve twice

#### Scenario: Facility on a separate circuit
- **WHEN** the bench's network can support a start but a linked facility's separate network cannot cover its new demand
- **THEN** the bench's batteries are not counted for that facility; a natively required facility prevents the start, while an optional one does not, and its native power state determines whether it provides its benefit

#### Scenario: Networks change before committed work
- **WHEN** a switch or construction splits or merges the networks after a candidate start was forecast but before work begins
- **THEN** the old pending allocation is discarded and the actual start is checked against the new network membership without duplicate demand or inaccessible battery reserves

#### Scenario: Existing deficit and later recovery
- **WHEN** other loads already exceed generation and a new bill lacks sufficient reserves, then generation recovers
- **THEN** the bill waits without consuming ingredients or leaving a phantom demand, and becomes eligible through its normal retry path

#### Scenario: Existing work and native blackout
- **WHEN** reserves fall below the added forecast threshold during a running bill
- **THEN** this extra check does not cancel it, while a real native power loss can still stop it normally

#### Scenario: Unpowered work and disabled check
- **WHEN** a bench permits work without electricity, or the player disables the extra work-start check
- **THEN** this feature adds no forecast refusal and all ordinary work conditions still apply

### Requirement: RP06 Settings are searchable scoped and recoverable
The feature SHALL use the common settings system in an ordered Power → Equipment budgets section.
General rows contain equipment budgets On, idle 10 W, active multiplier 2.5, door multiplier 10,
vanilla presets On, prevent underpowered starts On and reserve horizon 8⅓ simulation seconds.
Idle accepts finite values at least zero; active W, multipliers and horizon require finite positive
values. Values that cannot be represented safely are rejected. Valid changes take effect by the
next simulation update; invalid edits retain the last valid value and explain the restriction.

Searchable device rows SHALL show the building label, source mod, supported activity, derived rate
source and idle/active W, with a clear choice between automatic rates and an explicit pair. Search
matches label, internal identifier or source-mod name without case sensitivity. Each row uses
Global default, Current save and Use global; the explicit pair is one atomic setting. Reset affects
only the chosen row and scope, restoring automatic selection; feature reset affects only this
feature in the chosen scope and explains its extent. Filtering rows never deletes hidden choices.

Missing entries SHALL inherit current global values. Use global retains dormant save overrides;
unchecking restores them or seeds the current value. Main-menu save controls and mutation callbacks
are disabled. Choices for temporarily absent building types remain saved and visibly identifiable
as unavailable, without creating content or granting support. Configuration applies to a type;
per-building native flick/door controls remain contextual gameplay state. No new bottom tab is added.

#### Scenario: Find and edit a type
- **WHEN** the player searches by a mod name, sets a type's idle/active pair and clears the search
- **THEN** only that type and chosen scope change, with other hidden overrides retained

#### Scenario: Global inheritance and dormant pair
- **WHEN** a save inherits a changed global pair, then restores its previously stored override
- **THEN** it first follows the current global value and then its own complete dormant pair, without changing another save

#### Scenario: Main-menu editing
- **WHEN** the player opens these settings at the main menu
- **THEN** global editing remains usable and all save controls, Use global, save resets and their mutation callbacks are inactive with a reason

#### Scenario: Missing content and recovery
- **WHEN** a building provider is removed and later restored
- **THEN** its stored choices survive absence, are marked unavailable during it and become usable again only for an admitted matching building type

#### Scenario: Invalid or incomplete pair
- **WHEN** an edit supplies a negative idle value, zero active value, nonfinite number or only half a valid pair
- **THEN** the effective pair remains unchanged and the UI identifies the invalid edit

#### Scenario: Scoped reset
- **WHEN** the player resets one save-specific device row
- **THEN** that row returns to automatic selection in that save while global defaults and all other rows remain intact

### Requirement: RP07 State and information remain correct across views and saves
The feature SHALL operate on every loaded map, including world view and maps not currently shown.
New, removed, moved, destroyed or transferred equipment is evaluated from its current location and
state. Loading or switching saves reconstructs activity and power budgets without carrying another
game's transient users or admissions. Viewing settings or inspection must not itself activate work.

Inspection SHALL show idle/active state, actual draw, configured idle/active rates and applicable
rate source without removing native power, work, fuel or provider information. Temporary no-power,
disabled and unsupported states must not be presented as ordinary powered standby. Supported
description integrations show the same effective values and refresh after settings/research changes.

#### Scenario: Work while viewing the world
- **WHEN** the player opens world view or another colony while work starts or stops on a loaded map
- **THEN** equipment on that map updates its state and network cost normally

#### Scenario: Move or remove a device
- **WHEN** a powered device is moved between maps or removed during work
- **THEN** its old map retains no demand or references to it and its new state is evaluated without affecting unrelated devices

#### Scenario: Load and save switching
- **WHEN** the player reloads an ongoing research job or opens another save
- **THEN** current work and settings determine consumption, with no idle interval caused solely by missing an old start event or leaked state from the other save

#### Scenario: Inspect and description refresh
- **WHEN** the player changes a device's effective override or generic research-adjusted rate and views its inspection and an admitted description consumer
- **THEN** both show current applicable values without stale ranges or activation caused by viewing them

### Requirement: RP08 Supported external activity is explicit and absent-safe
The feature SHALL support admitted external activity descriptions for actual work, valid reservation
or scheduled availability. Reservation-based activity applies only when explicitly declared for that
device and ends when its last valid reservation ends. Scheduled-only activity follows its permitted
schedule. When a schedule is combined with work or reservation, the schedule is an additional gate;
outside it the device cannot be kept active by those signals. Native continuous obligations and
refusals still apply. Unsupported or contradictory activity descriptions leave the device native
with a clear startup explanation rather than silently charging permanent idle power.

For admitted Doors Expanded and Vanilla Vehicles Expanded doors, both movement directions and native
obstructions SHALL remain correct. Admitted Project RimFactory equipment must preserve its speed,
range, output handling and additional power costs; it must not depend on the language of inspection
text. No whole-provider support is inferred from these limited adapters. Missing optional providers
do not disable ordinary equipment. Legacy definition packs requiring the original controller are
documented as unsupported co-installations until their dependency/loading contract is explicitly
adapted; the megamod does not claim that package identity.

#### Scenario: Declared reservation lifecycle
- **WHEN** an admitted reservation-driven device gains and loses its last valid reservation
- **THEN** it changes between its active and idle rates without requiring an unrelated pawn work callback

#### Scenario: Schedule combined with work
- **WHEN** a supported scheduled work device has an active user while its schedule changes from permitted to closed
- **THEN** the schedule prevents active operation and its native closed behavior remains authoritative

#### Scenario: Unknown activity declaration
- **WHEN** external content supplies rates but no supported way to determine the required activity
- **THEN** that device stays native with one startup explanation instead of silently becoming permanently idle

#### Scenario: Optional factory or moving garage
- **WHEN** an admitted factory machine works with a speed/range power surcharge, or an admitted garage door changes between open and closed forms
- **THEN** activity and the applicable costs remain correct through that workflow, without losing provider surcharges or retaining stale demand on the replaced form

#### Scenario: Original-dependent definition pack
- **WHEN** a player tries to use an unchanged pack whose metadata or definitions require the original RePowered implementation
- **THEN** documentation identifies that unmet contract and no exact drop-in claim or package spoof hides it

### Requirement: RP09 Incompatibility leaves a coherent documented fallback
The feature SHALL apply only after its required game and content behavior is established. A missing,
changed, ambiguous or failed prerequisite skips the affected coherent device group and produces one
bounded startup warning identifying the change, failed condition, unavailable behavior and native
fallback. Compatible unrelated modifications alone do not disable it. Activity, rates, work admission
and information for a skipped group must agree; it cannot keep a forecast restriction while its
rate policy is absent. Unrelated supported groups remain available.
This failed-group native fallback overrides LightsOut's ordinary rate fallback for the same device.
Inspection states that owned power management is unavailable and reports native consumption, rather
than presenting a LightsOut discount or a RePowered forecast as still active.

Original LightsOut, external RePowered, RePowerReborn or the original Turn It On and Off SHALL
suppress the owned combined lighting/equipment power group with a clear startup explanation.
The feature does not uninstall, alter or remove those providers. No upstream or HugsLib settings
are imported, deleted or rewritten automatically. Diagnostics are bounded; optional verbose
inspection belongs in developer controls, not a gameplay bottom tab or recurring error flood.

#### Scenario: Effective behavior no longer supports a patch
- **WHEN** a required activity or consumption behavior is missing or changed by the game or another mod
- **THEN** its device group retains native operation with the bounded warning and no half-applied rate or work restriction

#### Scenario: Failed admission differs from deliberate disable
- **WHEN** the player disables valid equipment budgets, or instead a smelter's enabled rate/activity group fails admission
- **THEN** deliberate disable restores the smelter's applicable LightsOut rates, whereas admission failure leaves it native with the warning, unavailable status and no owned forecast restriction or LightsOut rate adjustment

#### Scenario: Compatible unrelated patch
- **WHEN** another mod changes an unrelated part of a supported method without invalidating the required behavior
- **THEN** this alone does not prevent the equipment group from being used

#### Scenario: External power controller
- **WHEN** one of the named original power controllers is active
- **THEN** owned lighting and equipment power management stay inactive together with a startup explanation, preserving the external controller

#### Scenario: Existing source configuration
- **WHEN** a game starts with old RePowered or HugsLib settings present
- **THEN** those files remain untouched and owned settings follow their documented defaults and inheritance
