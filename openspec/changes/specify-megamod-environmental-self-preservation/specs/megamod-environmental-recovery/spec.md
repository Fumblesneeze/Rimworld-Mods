# Environmental recovery

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source S030, Brrr and Phew (Continued). Proposed behavior only; implementation and live acceptance open.

## ADDED Requirements

### Requirement: Early recovery applies to eligible colonists and animals
The mod SHALL offer enabled environmental recovery to awake, mobile, undrafted player-controlled
colonists, including colony slaves, and by default to player-owned animals. It SHALL preserve
urgent native duties, medical-rest priority and ordinary player orders; burning, downed, mentally
broken or sleeping pawns are not forced into a new recovery job. Animals following their trained
master are excluded while following. S065 hunting companions whose following is temporarily
suspended for a controlling need/recovery are not actively following during that suspension; their
otherwise-applicable recovery remains eligible without changing the saved follow choice. Visitors,
prisoners, wild animals, wild humans, mechanoids and
other non-colonist creatures are not automatically included. The toddler temperature capability
adds its separately specified early escape and idle protection.

#### Scenario: Eligible colonist or animal becomes injured
- **WHEN** an eligible colonist or player animal reaches an enabled injury threshold and ordinary job selection reaches environmental recovery
- **THEN** it chooses an eligible recovery activity if one exists, ahead of ordinary work or idle wandering
- **AND** the selected pawn's activity report identifies the actual recovery reason and resting, sleeping or swimming state

#### Scenario: Direct control or urgent care takes precedence
- **WHEN** a pawn is drafted, given an overriding native order, needs medical rest, is burning, downed or in a mental state
- **THEN** this feature does not displace that control, emergency or care behavior with autonomous environmental rest
- **AND** it does not teleport, revive or alter the pawn's condition to make recovery eligible

#### Scenario: Animal follows its master
- **WHEN** a trained player animal is currently following its master
- **THEN** this feature does not separate it from its master with a recovery job
- **AND** after following ends it becomes eligible under the same enabled injury rules

#### Scenario: Hunting companion temporarily yields following
- **WHEN** S065 suspends a hunting companion's active following for otherwise-eligible environmental recovery
- **THEN** the recovery uses this capability's existing enabled thresholds and priorities while native following stays suspended
- **AND** the saved follow choice is preserved, and ordinary actively following animals retain their exclusion.

### Requirement: Each enabled injury can independently request recovery
The mod SHALL start cold/heat response at or above **20% hypothermia/heatstroke**, and toxic,
blood-rain and supported breathing response at or above **10%** severity by default. Cold, heat,
toxic and general breathing responses default on; blood-rain response is on when its content is
available. Combat Extended smoke response is deferred outside initial support, with no initial control or adapter. These supported thresholds are configurable
from **1–100%**. Each supported enabled hazard SHALL be evaluated independently; the existence
of another hazard's content does not require the pawn to suffer from it. Where several enabled
causes qualify, one activity addresses their combined refuge requirements; no overlapping jobs
or forced disappearance of health conditions are introduced.

#### Scenario: Exactly at the threshold
- **WHEN** eligible hypothermia reaches 20% with the default cold response enabled
- **THEN** recovery can begin at that threshold
- **AND** 19.9% does not start this ordinary cold-recovery rule, without disabling native care or toddler early escape

#### Scenario: Vacuum without smoke or oxygen starvation
- **WHEN** a pawn reaches the supported vacuum threshold while unrelated smoke or heavy-air content is also installed
- **THEN** the vacuum response does not require smoke or oxygen starvation on that pawn
- **AND** disabling smoke response does not disable vacuum response

#### Scenario: Multiple current causes
- **WHEN** a pawn simultaneously qualifies for cold and toxic recovery
- **THEN** its selected destination must meet both causes' safety requirements
- **AND** one cause cannot send it to a refuge known to worsen the other

### Requirement: A refuge is physically usable and suitable for actual exposure
The mod SHALL choose a reachable, usable final refuge for the pawn, considering its current
thermal protection and all supported hazards there. Cold/heat recovery prefers conditions in
which the relevant injury can actually improve; a comfortable-range boundary that does not allow
improvement is not sufficient. Toxic refuge avoids applicable toxic terrain, pollution and airborne
exposure; blood-rain refuge stops blood-rain exposure; breathing refuge actually stops the supported
air, smoke or vacuum exposure. A roof alone is not proof of breathing safety. Ordinary dry refuge
prefers a valid bed, then a valid floor resting cell; unsuitable preferred beds do not exclude other
valid beds. Reservations, ownership, room permissions, doors and physical access remain respected.

Recovery outside the pawn's allowed area is permitted by default, with a configurable restriction;
this exception only relaxes the allowed-area boundary. It does not unlock doors, ignore physical
reachability, steal a bed, remove a pen boundary or authorize a dangerous destination. Choosing a
refuge SHALL NOT change another pawn's bed ownership. No requirement creates a safe location when
the colony has not provided one.

#### Scenario: Preferred bed is unsuitable
- **WHEN** the pawn's usual bed is too hot, reserved by someone else or exposed, but another permitted bed is suitable
- **THEN** the unsuitable bed is skipped and the other valid bed can be used
- **AND** no existing owner is displaced to make the choice possible

#### Scenario: Only a floor refuge is usable
- **WHEN** no valid bed exists and a safe reachable floor resting cell is available
- **THEN** the pawn travels to that actual cell and rests using ordinary floor-rest behavior
- **AND** safety is checked for that cell, not merely another cell in the same room

#### Scenario: Roofed vacuum or polluted shelter
- **WHEN** a roofed candidate still exposes the pawn to the triggering vacuum or toxic hazard
- **THEN** it is rejected as a recovery refuge for that hazard
- **AND** the feature does not label shelter as safe solely because it has a roof

#### Scenario: Allowed-area preference
- **WHEN** the only otherwise suitable refuge is outside the pawn's allowed area
- **THEN** default recovery can use it if native physical access permits
- **AND** turning off outside-area recovery excludes it without moving the area's boundary or changing anyone's assignment

#### Scenario: No usable refuge
- **WHEN** a bounded search finds no physically usable suitable refuge, including a blocked one-cell room
- **THEN** no invalid movement or lying-down order is issued and ordinary care remains available
- **AND** unchanged failure is retried no more often than every 500 game ticks, with changed access or hazard facts allowing an earlier attempt at the next safety evaluation
- **AND** failure does not freeze the game, generate a per-tick warning or claim the pawn is recovering safely

### Requirement: Heat response can use real safe swimming
The mod SHALL retain swimming as an optional-content heat response for colonists who can perform
the game's swimming activity. When Odyssey swimming is available, a valid cooling swim within
**29 cells** is considered before dry refuge, matching the source's local-swim preference. The
whole activity must satisfy ordinary swimming access and applicable hazard rules, including other
current recovery causes. It SHALL use physical travel and swimming with the game's cooling
behavior, not grant a cure merely for selecting water. Animals and unsupported life stages are not
granted swimming ability. Without a suitable swim, ordinary dry-refuge selection remains available.

#### Scenario: Native cooling swim is available
- **WHEN** a heat-injured eligible colonist has a safe reachable swimming activity within 29 cells
- **THEN** the colonist physically travels and swims, with heatstroke changing through that actual native activity
- **AND** its report describes cooling by swimming rather than lying down

#### Scenario: Swimming unavailable or unsuitable
- **WHEN** Odyssey is absent, the pawn cannot swim, water is unsafe or no valid local route exists
- **THEN** heat recovery proceeds to suitable dry refuge without requiring Royalty or granting a new ability
- **AND** no health condition is removed just because a water cell was found

### Requirement: Recovery follows actual causes and changing surroundings
The mod SHALL let ordinary rest, sleep, comfort and health rules determine recovery; it adds no
new healing rate, immunity, free treatment or material resource. It SHALL continue resting until
the enabled causes that prompted or subsequently joined recovery are gone, unless an ordinary
interruption occurs. Unrelated disabled conditions do not prolong the job. Disabling a response
stops waiting for that cause without deleting the condition. During owned travel/rest, changed
refuge/access/hazard facts SHALL be rechecked within **250 game ticks**; a failed check cancels or
replans the affected activity, never converts an unknown location into a certified safe one.

#### Scenario: Triggering condition clears
- **WHEN** the cold condition that prompted recovery disappears through normal health rules
- **THEN** the pawn can finish recovery even if an unrelated disabled condition remains
- **AND** that unrelated condition is neither treated nor erased by this feature

#### Scenario: Smoke recovery completes
- **WHEN** supported enabled smoke exposure prompted recovery and the smoke condition remains
- **THEN** it remains a recovery cause even if the pawn has no cold, heat or vacuum injury
- **AND** when it clears, it no longer keeps the pawn resting

#### Scenario: Shelter deteriorates during recovery
- **WHEN** a heater fails, roof disappears, smoke arrives or a refuge otherwise becomes unsuitable
- **THEN** the pawn stops relying on that refuge within the safety-evaluation interval and seeks another valid option
- **AND** if no option exists, ordinary care can take over without endless failed movement orders

#### Scenario: Loading or disabling recovery
- **WHEN** a save is loaded with an owned recovery job, or its relevant response is disabled
- **THEN** current settings, target availability and actual health are re-evaluated before continuing
- **AND** stale targets or disabled-only causes release the owned job without modifying ordinary health, ownership or player orders

### Requirement: Ordinary food and configured recreation interruptions remain available
The mod SHALL retain the source's needs trade-off: ordinary recovery does not begin, or ends at
its next needs check, when the pawn is below its native hungry threshold; for colonists it also
does so below **12.5% recreation** when recreation interruption is enabled (default on). The
recreation threshold is configurable from **1–100%**. These exits return control to ordinary needs
and care selection; they do not guarantee safety, force food/recreation into existence, or grant
permission for a new owned outdoor break in danger. Animals have no recreation-interruption test.
The separate toddler escape is movement to safety and is not blocked merely by low food or play.

#### Scenario: Hungry while resting
- **WHEN** a recovering pawn becomes hungry by the game's ordinary threshold
- **THEN** the recovery job yields to ordinary job selection so food can be sought
- **AND** if the injury remains, recovery can be considered again when its eligibility conditions are satisfied

#### Scenario: Recreation option differs between saves
- **WHEN** one save permits low-recreation interruption and another overrides it off
- **THEN** only the first yields environmental rest for that reason
- **AND** neither save changes the other save's choice or the pawn's ordinary recreation gain

### Requirement: Common settings preserve every environmental preference
The mod SHALL place configuration under ordered **Pawn behavior → Environment** sections for
responses/thresholds, animal and allowed-area policy, needs interruptions, and outdoor breaks.
All genuine configuration uses the common **Use global**, **Current save**, **Global default**
controls and persistence, including the blood-rain switch and threshold. New saves inherit current
global values; dormant overrides survive switching back to inheritance. Save controls are disabled
at the main menu and while inheriting as applicable. Optional-content choices retain their stored
values when unavailable and explain their inactive state. Changes apply at the next relevant
evaluation; no source-specific settings are imported automatically. No new bottom tab is added.

#### Scenario: Global defaults and save overrides
- **WHEN** an inherited save's global cold threshold changes, or a save has its own explicit threshold
- **THEN** the inherited save uses the current global value and the overridden save keeps its own
- **AND** switching saves does not retain the previous save's effective values

#### Scenario: Main menu and unavailable content
- **WHEN** settings open without an active save or without blood-rain content
- **THEN** current-save controls cannot mutate a save, and unavailable response controls explain their inactive content dependency
- **AND** opening the window does not erase stored blood-rain preferences or prevent editing unrelated global defaults

#### Scenario: Persist and reset numeric values
- **WHEN** the player changes blood-rain or another response preference, saves settings and later reloads
- **THEN** that scope's values and inheritance choice are retained
- **AND** a clearly labeled reset of numeric values restores the documented numeric defaults only in the selected editable scope, leaving response switches and other scopes unchanged

### Requirement: Provider support and startup failures are explicit and isolated
The mod SHALL activate optional environmental behavior only for available supported content,
and explain enabled behavior withheld by an incompatible target/provider in bounded startup logs.
Compatible unrelated patches are allowed. The core feature works without optional DLC, Toddlers,
More Events, Combat Extended or Vanilla Temperature Expanded. For supported local-temperature
providers, refuge and toddler/outdoor decisions use the temperature the pawn actually experiences,
including local heating/cooling exactly once. Unknown active environmental semantics are not
silently presented as verified safety. An active original Brrr and Phew withholds owned overlapping
recovery/outdoor jobs with an explanation; it is not automatically removed or unpatched.

#### Scenario: Supported local heating
- **WHEN** an admitted local-temperature provider makes a cell safe or unsafe for a pawn
- **THEN** refuge, outdoor and toddler temperature decisions reflect that effective temperature
- **AND** the heating effect is not counted twice or ignored because the enclosing room has a different temperature

#### Scenario: Optional provider absent or changed
- **WHEN** an optional provider is absent, or its active exposure/safety behavior cannot be understood safely
- **THEN** absence leaves its branch dormant while incompatible enabled behavior is withheld with a startup warning naming the unavailable behavior and fallback
- **AND** independent supported branches remain available; a missing provider is not replaced by a similarly named condition from another source

#### Scenario: Original active or patch prerequisite missing
- **WHEN** the original is active, or a prerequisite for an enabled owned job is missing or ambiguous
- **THEN** the overlapping or affected owned behavior is withheld and a bounded startup warning identifies the reason and remaining native/source behavior
- **AND** other owners are untouched, there are no duplicate recovery jobs, and compatible foreign changes alone do not cause rejection
