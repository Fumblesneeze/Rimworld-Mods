# Automatic lighting and equipment standby

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S052 observable draft; no gameplay implemented or verified.

## ADDED Requirements

### Requirement: LO01 Ordinary indoor lighting follows relevant room occupancy
The feature SHALL automatically suppress supported ordinary indoor lamps when their room has no
relevant occupant and restore their normal lighting when a relevant occupant enters. Relevant
occupants are tool-using pawns of any faction, plus animals when Count animals is enabled. A pawn
departing through a doorway does not keep its former room occupied solely by standing in or moving
into that doorway. Outdoors, doorway-only spaces and temporarily indeterminate rooms retain normal
lighting. Sun/growing lights, climate equipment, scheduled lights, landing beacons, generators and
unsupported special-purpose lights retain their native behavior.
Supported wall lamps follow the room they illuminate on their facing side, including after
rotation or relocation, rather than a different room on the opposite side of the wall.

#### Scenario: Last occupant leaves and another returns
- **WHEN** the last relevant pawn leaves a supported indoor room and its departure delay expires
- **THEN** its eligible lamps stop illuminating the room; a later relevant entrant restores ordinary lighting on the next simulation update if normal power and light conditions permit it

#### Scenario: Visitor counts as an occupant
- **WHEN** a visitor, prisoner or other tool-using pawn remains in the room after the colonist leaves
- **THEN** the room remains occupied for automatic lighting regardless of faction

#### Scenario: Doorway transition does not leave an empty room lit
- **WHEN** the departing pawn is in or moving into the doorway with no other relevant occupant inside
- **THEN** the former room can complete its departure delay without repeatedly toggling lamps as the pawn crosses

#### Scenario: Outdoors and special lighting
- **WHEN** an outdoor lamp, sun lamp, grow light or scheduled special-purpose light has no nearby pawn
- **THEN** occupancy automation does not change that device's native operation

#### Scenario: Room is being rebuilt
- **WHEN** construction temporarily makes the lamp's room indeterminate
- **THEN** the feature does not force it dark or stall play, and reevaluates it after the room becomes available

#### Scenario: Wall lamp faces one of two rooms
- **WHEN** a supported wall lamp faces an empty room while the room on the other side is occupied
- **THEN** it follows the empty illuminated room's shutdown rule; after rotation or relocation it follows the room it now illuminates, without retaining its previous room's decision

### Requirement: LO02 Sleep and animal choices have explicit occupancy effects
The feature SHALL turn eligible lamps off when all relevant occupants are asleep, unless Keep
lights on while sleeping is enabled. Sleep shutdown is immediate on the next simulation update,
without the departure delay. Waking restores ordinary lighting. Count animals defaults Off;
when enabled, awake animals keep a room lit and sleeping animals participate in the all-asleep
decision. Turning on sleep lighting does not keep an empty room lit indefinitely.

#### Scenario: Last awake occupant falls asleep
- **WHEN** the last awake relevant occupant falls asleep with sleep lighting disabled
- **THEN** eligible lamps stop illuminating on the next simulation update; another relevant occupant waking restores them

#### Scenario: Sleep lighting is enabled
- **WHEN** all occupants sleep with Keep lights on while sleeping enabled
- **THEN** lamps remain ordinarily lit, but later departure of every relevant occupant still starts the empty-room shutdown delay

#### Scenario: Animals ignored by default
- **WHEN** only animals remain after tool-using pawns leave and Count animals is Off
- **THEN** the room qualifies as empty for automatic lighting

#### Scenario: Animal sleep and wake when counted
- **WHEN** Count animals is On and a relevant animal is the room's last awake occupant
- **THEN** its sleep permits sleep shutdown and its waking restores ordinary lighting without waiting for a human pawn to move

### Requirement: LO03 Departure delays cannot override newer room decisions
The feature SHALL use a configurable departure delay, initially 1.5 simulation seconds, and apply
an eligible delayed shutdown no later than 0.5 simulation seconds after its deadline. Zero delay
means the next simulation update. Reentry, Keep On, disabling automation, a changed room or moving
the lamp cancels obsolete shutdown decisions. A new departure gets a new full delay. Changing the
configured delay restarts still-eligible pending departures using the new value; paused simulation
does not consume the delay.

#### Scenario: Reentry before expiry
- **WHEN** a relevant pawn returns before a pending shutdown expires and leaves again later
- **THEN** the old deadline cannot extinguish the occupied room and the later departure receives the full current delay

#### Scenario: Delay changes during a pending departure
- **WHEN** the player changes the delay while an eligible room is awaiting shutdown
- **THEN** its pending interval restarts with the new value; older or longer pending intervals cannot block other rooms

#### Scenario: Pause and zero delay
- **WHEN** play is paused during a departure delay, or the configured delay is zero
- **THEN** pausing preserves the remaining simulation time, and zero delay applies on the next simulation update without a new waiting interval

#### Scenario: Lamp moves before the old deadline
- **WHEN** a lamp is moved, uninstalled, destroyed or transferred before its former room's shutdown
- **THEN** that old decision cannot affect the moved lamp, a replacement building or another map

### Requirement: LO04 Keep On is contextual lamp state with native limits
The feature SHALL offer each player-controllable supported lamp a contextual Keep On choice,
initially Off. Keep On exempts that lamp from occupancy and sleep shutdown, without changing other
lamps. It does not grant power, override manual flicking, breakdowns, schedules or provider limits.
The feature SHALL preserve normal color, darklight, radius and other native lamp controls.

#### Scenario: Keep one lamp on
- **WHEN** the player enables Keep On for one lamp in an empty or sleeping room
- **THEN** that lamp resumes its normal permitted glow and avoids pending automatic shutdowns while other lamps retain their own choices

#### Scenario: Return a lamp to automation
- **WHEN** the player clears Keep On while the room already qualifies for shutdown
- **THEN** the lamp immediately follows the current room rule on the next simulation update, without waiting for another pawn transition

#### Scenario: Native power or switch prevents light
- **WHEN** Keep On is enabled but the lamp is manually off, unpowered, broken or otherwise refused by its provider
- **THEN** it stays subject to that refusal and automation does not create a flick job or manufacture power

#### Scenario: Change lamp appearance while automatically dark
- **WHEN** the player changes a supported lamp's color or darklight choice while occupancy has suppressed it
- **THEN** the choice is retained and is visible when the lamp next lights, with its normal radius and research restrictions

### Requirement: LO05 Attended equipment is active for actual use
The feature SHALL apply standby to supported idle bill workbenches, research benches, deep drills
and televisions, and active consumption while they are in use. Readiness needed to start a valid
native job remains available. Work begun by a pawn already at its destination is treated like work
begun after walking there. Completion, interruption or cancellation returns equipment to standby
only when no other user or ongoing process needs it; this transition has no lamp departure delay.

#### Scenario: Worker starts and finishes an ordinary bill
- **WHEN** a pawn prepares and performs a valid bill at an otherwise idle supported bench
- **THEN** the bench is available through the normal work path, draws active consumption during use and returns to standby after its last use ends

#### Scenario: Research or drilling begins without travel
- **WHEN** a pawn already at a research bench or deep drill starts its valid job
- **THEN** the device becomes active without requiring the pawn to leave and reenter its interaction position

#### Scenario: Job is interrupted
- **WHEN** the only worker stops using a bench because its job is completed, cancelled or interrupted
- **THEN** the bench returns to standby without retaining a permanent active state

#### Scenario: Multiple television viewers
- **WHEN** one of several television viewers leaves
- **THEN** the television remains active for the others and returns to standby only after the final viewer stops

#### Scenario: No electricity is available
- **WHEN** a supported bench has no usable power supply
- **THEN** standby does not make the work valid, create free work, or cause repeated futile work assignments that the normal power rules would prevent

### Requirement: LO06 Autonomous equipment follows its real ongoing demand
The feature SHALL preserve ongoing demand for the supported equipment below, including necessary
startup readiness, while applying standby after the demand ends. Room emptiness and a worker's
departure do not turn an ongoing process into idle work. Normal progress, resource prerequisites,
occupant safety, outputs, capacity and cancellation remain native. Optional DLC absence removes
only the relevant equipment branch.

| Equipment | Demand retained |
| --- | --- |
| Rechargeable equipment | Recharging until ready; a new discharge starts another charging period |
| Band nodes | Tuning and sustained service to a tuned mechanitor |
| Gene assemblers and processors | Valid current assembly work and facilities required by any ongoing assembly |
| Gene extractors | Selected/entering subject readiness and extraction until finished or cancelled |
| Growth vats | Selected/entering pawn or embryo readiness and occupied growth until stopped |
| Mech chargers | Valid attached charging use |
| Mech gestators | Native preparation/forming/completed phases, with forming treated as active even without an attending pawn |
| Subcore scanners | Subject entry and ongoing scanning until contents are ejected or work ends |
| Wastepack atomizers | Accepted waste still awaiting or undergoing processing |
| Ritual loudspeakers | Real native ritual demand and its ordinary powered contribution |
| Admitted android printers and pawn vats | Provider-reported autonomous production until its native end/cancellation |

#### Scenario: Gestation continues after the worker leaves
- **WHEN** a mech gestator is actively forming after the attending pawn departs or the save is loaded
- **THEN** it retains the appropriate active demand and normal progress instead of receiving an idle discount merely because no pawn is attending

#### Scenario: Shared gene processor still needed
- **WHEN** one gene assembly ends while another still requires the same processor
- **THEN** the processor remains available for the remaining work and enters standby only after all applicable demand ends

#### Scenario: Entering or occupied equipment
- **WHEN** a valid selected subject enters an extractor, vat or scanner and the activity later finishes or is cancelled
- **THEN** normal readiness and occupied work remain possible, and the empty unneeded device returns to standby without losing, ejecting or harming its occupant through this feature

#### Scenario: Charge completes and restarts
- **WHEN** supported rechargeable equipment finishes charging and is later discharged, or a mech begins a valid charging session
- **THEN** consumption follows the corresponding ready/charging state while charge duration and normal power requirements remain unchanged

#### Scenario: Band node supplies ongoing service
- **WHEN** a band node finishes tuning and remains assigned to its mechanitor
- **THEN** it remains active for that service rather than treating completed tuning as absence of demand

#### Scenario: Atomizer processes its last wastepack
- **WHEN** an atomizer receives waste and later finishes or ejects its last accepted waste
- **THEN** it is active while processing is still required and returns to standby when empty

#### Scenario: Ritual display is read
- **WHEN** the player previews or reviews a ritual outcome while speakers are in use
- **THEN** viewing the result does not switch speakers off, change their contribution or interrupt the ritual

#### Scenario: Unknown continuous equipment
- **WHEN** a hydroponic grow light, furnace, climate fan, management computer, gestalt engine or another unadmitted continuous device appears to resemble a supported lamp or bench
- **THEN** the feature leaves its native service intact instead of applying an occupancy or attended-work rule based only on resemblance

### Requirement: LO07 Resource rates preserve native availability and costs
The feature SHALL use the composed [RePowered equipment budgets](../../../specify-megamod-repowered/specs/megamod-repowered/spec.md)
for devices admitted to that enabled policy, with LightsOut providing actual-use activity for
overlapping work equipment. That device receives one rate calculation. Lighting and equipment
outside that policy use the LightsOut rules below; disabling RePowered restores those fallback
rules for shared equipment. Failed combined device admission takes precedence over that fallback:
the affected device keeps native consumption, with neither an owned discount nor added work-start
restriction, and inspection identifies the unavailable policy. The percentage controls explicitly
identify their normal fallback scope.

The feature SHALL default to 0% standby consumption and 100% active consumption in that fallback scope. Supported idle
electrical equipment with otherwise positive available demand consumes the greater of 0.01 W or
its native demand for the same phase multiplied by the standby percentage. An automatically dark
ordinary electrical lamp consumes 0.01 W. The floor is capped to ordinary positive demand when that
demand is smaller than 0.01 W. Native zero demand stays zero; absent electricity, manual
off state and other native refusals are not overridden. Active supported work, TV, charging and
ritual equipment uses its native same-phase demand multiplied once by the active percentage;
active lamps retain native demand. Native research reductions and valid external adjustments
remain effective. Generation, fuel, materials, work speed and outputs are unchanged.

Standby percentage accepts integers 0–100; active percentage accepts integers at least 100. The
electrical floor is an explicit source-derived compatibility value, not a promise of literal zero
draw. An optional gas adapter requires independently established units and consumption direction;
it must never generate or replenish fuel through a standby calculation.

#### Scenario: Configured standby and active rates
- **WHEN** a supported device outside enabled RePowered budgets would normally consume 100 W in the relevant phase, with standby 50% and active 150%
- **THEN** it consumes 50 W in that idle phase and 150 W in that active phase; the same demand is not multiplied twice

#### Scenario: Default near-zero electrical standby
- **WHEN** a supported powered consumer outside enabled RePowered budgets with positive normal demand at least 0.01 W enters default 0% standby
- **THEN** it retains the documented 0.01 W electrical draw and normal power availability checks

#### Scenario: Research or native suppression changes demand
- **WHEN** research reduces the device's normal power requirement, or native conditions suppress its consumption to zero
- **THEN** the rate uses the reduced native baseline, and zero remains zero rather than acquiring a phantom draw or production

#### Scenario: Active lamps and excluded devices
- **WHEN** the player increases Active work consumption above 100%
- **THEN** supported work/TV/charging/ritual equipment outside enabled RePowered budgets uses that rate, while ordinary active lamps and excluded continuous devices keep their native consumption

#### Scenario: Shared equipment uses one configured budget
- **WHEN** a workbench is admitted to enabled RePowered budgets while LightsOut percentages differ
- **THEN** its actual-use activity is shared, its rate follows RePowered once, and disabling those budgets restores the applicable LightsOut rates without a second multiplier

#### Scenario: Failed combined admission keeps native consumption
- **WHEN** a shared smelter cannot admit its enabled RePowered rate/activity group
- **THEN** it stays native with the bounded warning and unavailable inspection status, without falling through to LightsOut rates or retaining the added work-start forecast

#### Scenario: Unproved gas behavior
- **WHEN** the installed gas provider does not establish the required supported consumption behavior
- **THEN** that branch retains native gas use and lighting with a startup explanation, rather than applying an electrical value or an unverified fuel-saving claim

### Requirement: LO08 Settings and operational controls use their intended scope
The feature SHALL provide common global/default-save configuration for automatic lighting On,
keep lights on while sleeping Off, count animals Off, departure delay 1.5 seconds, standby 0% and
active work 100%, in ordered Lighting and standby sections, with rate labels identifying their
fallback scope outside enabled RePowered equipment budgets. Each row shows Global default,
Current save and Use global. Missing entries inherit the current global value; checking Use global
retains a dormant override. The main menu disables all save editing and save mutation callbacks.
Invalid, nonfinite, negative or unsupported-range input retains the last valid value with an
explanation. A valid settings change reevaluates affected activity by the next simulation update.

One small bottom-right control SHALL select automatic lighting On, Off or Use configured value
for the current game across its loaded maps, without changing global defaults. New games and
existing saves missing this live choice start at Use configured value. Its choice is saved with
that game. It controls only lighting automation; equipment standby continues independently.
Keep On remains contextual building state. No new bottom tab is added; optional diagnostics belong
in the top developer interface and cannot create unbounded recurring log output.

#### Scenario: Inheritance and dormant override
- **WHEN** a save inherits a changed global delay, or the player unchecks Use global after previously saving an override
- **THEN** the inherited save follows the current global delay, and unchecking restores its dormant override or seeds a first override from the current global value

#### Scenario: Main-menu save controls
- **WHEN** settings are opened without an active game
- **THEN** global controls remain usable while Current save, Use global, resets and other save-specific mutations are disabled with a reason

#### Scenario: Operational lighting off
- **WHEN** the player chooses Off at bottom right
- **THEN** lamps return to their ordinary permitted behavior, old shutdowns are cancelled and equipment standby remains active without changing the global lighting default

#### Scenario: Restore configured lighting behavior
- **WHEN** the player chooses Use configured value or loads another save
- **THEN** the appropriate save's configured value or saved live choice is used, with no leakage from the previous game

#### Scenario: New or missing live choice respects a disabled default
- **WHEN** automatic lighting is globally configured Off and a new game or existing save has no stored live selector choice or save override
- **THEN** the selector starts at Use configured value and automatic lighting remains Off rather than being forced On

#### Scenario: Invalid rate or delay
- **WHEN** an input is outside the displayed supported range, below the permitted minimum or not a finite number
- **THEN** it is rejected with an explanation and the last valid behavior remains in effect without an immediate or indefinite accidental shutdown

### Requirement: LO09 Persistence and information describe current behavior
The feature SHALL preserve each lamp's Keep On choice, the current game's operational selector and
remaining valid departure delays across save/load, while reconstructing activity from current
occupants, jobs, charging and processes. Moving, replacing or removing buildings and maps must not
leave controls affecting unrelated objects. Inspect information SHALL indicate automatic standby
and truthful consumption without removing native work, fuel, power-network or provider details.
No upstream settings/save data is imported or rewritten automatically.

#### Scenario: Save during a departure delay
- **WHEN** a save is loaded with an eligible pending lamp shutdown
- **THEN** its remaining simulation delay is preserved, subject to current occupancy and Keep On, without an old timer extinguishing an occupied room

#### Scenario: Save during active work or sleep
- **WHEN** a save containing an active bench, viewer, autonomous process and sleeping room is loaded
- **THEN** each resumes the appropriate observable activity/lighting policy without needing an unrelated pawn movement to repair it

#### Scenario: Move or rebuild rooms and buildings
- **WHEN** a lamp or supported building changes map or its room is split or merged
- **THEN** it follows its current location and activity, retaining its own valid Keep On choice while obsolete location state cannot control it

#### Scenario: Inspect a standby building
- **WHEN** the player selects supported idle equipment
- **THEN** its inspection reports standby and actual consumption while retaining unrelated native work, storage, fuel and provider information

#### Scenario: Existing upstream configuration
- **WHEN** the player starts using this feature with an existing LightsOut or HugsLib configuration
- **THEN** owned settings use their documented defaults/inheritance and do not silently import, overwrite or delete that upstream configuration or its saved building state

### Requirement: LO10 Supported integrations fail clearly and preserve unrelated behavior
The feature SHALL activate only supported game/content branches. Missing optional content leaves
unrelated branches usable; changed or unprovable required behavior leaves the affected branch
native and reports one bounded startup warning identifying the skipped change, failed prerequisite,
unavailable behavior and fallback. Compatible unrelated modifications do not by themselves disable
the feature. Supporting a building preserves its existing properties, categories and other
extensions without duplicate controls or partial resource, light and heat behavior.

When original LightsOut, external Turn It On and Off - RePowered, RePowerReborn or original Turn It
On and Off is active, this owned
replacement group SHALL remain inactive with one clear startup explanation, preserving that
provider's behavior. Within the megamod S052 and S053 instead use the direct composition in LO07
and RP01–04, without self-patching. Supported description integrations SHALL display
the owned rate/phase result and refresh when relevant settings change; unsupported integrations
are documented rather than represented as exact drop-in compatibility.

#### Scenario: Optional provider absent or changed
- **WHEN** an optional light/equipment provider is absent, or present with unsupported behavior
- **THEN** absence creates no failed optional feature, while a changed enabled branch stays native with a bounded startup warning; ordinary supported lamps and benches remain available

#### Scenario: Compatible unrelated modification
- **WHEN** another mod modifies unrelated behavior without invalidating this feature's required behavior
- **THEN** that modification alone does not disqualify supported automatic lighting or standby

#### Scenario: Existing building extensions
- **WHEN** supported building content already contains other extensions or this feature is initialized again
- **THEN** those existing entries and unrelated fields are retained, with one usable owned control and no duplicated or half-active behavior

#### Scenario: Original power controller co-installed
- **WHEN** original LightsOut, external RePowered, RePowerReborn or original Turn It On and Off is active
- **THEN** owned lighting and standby are suppressed as one group with a startup explanation, without uninstalling, changing or unpatching the provider

#### Scenario: Supported description consumer
- **WHEN** an admitted Relevant Stats In Description integration shows a supported building before and after a rate or research change
- **THEN** its displayed consumption estimates reflect current owned behavior, distinguish applicable phases and do not retain a stale range or apply rates to unsupported interaction-cell buildings

#### Scenario: Consumer depends on absent source internals
- **WHEN** an upstream-only startup optimization or unsupported description integration cannot attach to the replacement
- **THEN** the feature does not pretend the source package is present; the documented integration remains unavailable, truthful owned inspection remains available and unrelated consumer behavior is left intact
