# Corpse fire continuity

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S091. Observable draft; nothing implemented or accepted.

## ADDED Requirements

### Requirement: Continue an existing flame at the placed corpse
The feature SHALL preserve the existing flame's strength and origin when a pawn is still on ordinary
attached fire as it becomes a corpse and that corpse is successfully placed on a map. It SHALL use
that flame for the normal attempt to start fire at the corpse's actual placed cell, with no new ignition roll, fixed
replacement strength, intensity bonus or extra fire-damage event. The behavior SHALL apply without
a colonist, faction, age or species restriction when the ordinary death and fire paths support it.

#### Scenario: A burning pawn dies
- **WHEN** a burning human or animal dies and its corpse is placed in a cell that allows ordinary fire
- **THEN** fire continues at that placed corpse's cell using the prior flame strength and origin
- **AND** death does not extinguish it solely because the pawn became a corpse

#### Scenario: Corpse placement moves to a nearby cell
- **WHEN** a burning pawn's corpse is placed nearby because its original cell cannot accept it
- **THEN** the ordinary fire attempt uses the actual corpse location
- **AND** this feature does not start a second fire at the abandoned death location

#### Scenario: A different source deals the killing blow
- **WHEN** one actor started the pawn's fire and another actor or cause delivers the fatal damage
- **THEN** the continuing fire retains its original fire attribution rather than adopting the killing blow's source

### Requirement: Respect ordinary fire placement and death outcomes
The feature SHALL continue fire only for the supported successful on-map corpse-placement path.
It SHALL preserve the ordinary refusal to add fire where an existing fire or other cell conditions
prevent it, without increasing or replacing the existing fire. It SHALL create no flame for a pawn
whose fire was absent or extinguished before corpse creation. Deaths producing no placed corpse,
contained or off-map remains, failed placement and invalid flame strength SHALL create no owned fire.

#### Scenario: No remaining fire
- **WHEN** a pawn dies without attached fire, including after successful self-extinguishing or assistance
- **THEN** the feature starts no corpse fire
- **AND** a fire belonging to another pawn is not borrowed for that death

#### Scenario: Cell already contains fire or rejects a new one
- **WHEN** the placed corpse's cell already has a fire or ordinary placement refuses new fire
- **THEN** the feature does not add, enlarge or replace a fire to force continuity
- **AND** ordinary corpse placement and existing fire behavior continue

#### Scenario: No map corpse is placed
- **WHEN** a death creates no corpse, placement fails, or remains stay inside a container or off-map
- **THEN** the feature does not ignite an unrelated cell, holder or future-spawned corpse
- **AND** normal death/container outcomes remain intact

#### Scenario: Invalid captured flame strength
- **WHEN** the prior flame has a non-finite or non-positive strength
- **THEN** the feature declines that handoff without inventing a replacement strength
- **AND** it emits a bounded diagnostic while leaving ordinary death handling intact

### Requirement: Continue through ordinary fire lifecycle
The resulting fire SHALL be an ordinary fire at the corpse's cell. Normal spread, heat, damage,
burnout, weather, fire suppression and firefighter actions SHALL govern it. It SHALL not be attached
to a movable corpse, guaranteed to consume the corpse, immune to extinguishing, or restarted after
extinguishing. Ordinary corpse decay, hauling, resurrection and destruction SHALL remain unchanged
except for their normal interaction with that fire. No new settings, gameplay UI or save conversion
SHALL be introduced for this behavior.

#### Scenario: Fire is extinguished
- **WHEN** a firefighter or normal environmental effect puts out the continued fire
- **THEN** it goes out through the ordinary workflow
- **AND** the corpse does not recreate it on a later tick or after saving and loading

#### Scenario: Corpse is moved or resurrected
- **WHEN** normal gameplay moves the corpse or resurrects its pawn before the fire ends
- **THEN** the ordinary cell fire follows its own lifecycle rather than following the corpse or forcing an attached fire onto the revived pawn
- **AND** any subsequent ignition, including with Anomaly present, follows normal rules

#### Scenario: Save and reload an ongoing fire
- **WHEN** the player saves with an existing continued fire and reloads
- **THEN** the fire and corpse follow their ordinary saved states
- **AND** loading does not repeat the death handoff, duplicate the fire or reset its strength

### Requirement: Keep death handoffs independent and admitted
Each death's fire handoff SHALL be independent and occur at most once. Nested deaths, repeated
callbacks, skipped or interrupted death paths SHALL not transfer one pawn's fire to another or leave
a handoff for a later death. The feature SHALL activate only for its documented semantic prerequisites,
allow compatible changes and withhold affected hooks on missing or ambiguous prerequisites. Each
skipped enabled patch SHALL receive a bounded startup warning naming its target, reason, unavailable
owned behavior and fallback. An already-correct game or original provider SHALL not receive a
duplicate owned fire handoff.

#### Scenario: Closely related deaths
- **WHEN** two pawns die in the same event, including a death caused during another death's effects
- **THEN** each placed corpse uses only its own remaining flame strength and origin, if any
- **AND** a non-burning victim gains no fire because another victim was burning

#### Scenario: Death path is skipped or interrupted
- **WHEN** a supported provider prevents death, interrupts corpse creation, or invokes a later death after a failed attempt
- **THEN** no stale handoff ignites a later or unrelated corpse
- **AND** completed normal side effects are not repeated to recover a missing fire

#### Scenario: Native fix or original provider is present
- **WHEN** the game already performs this handoff correctly or Keep Burning, Corpse! is active
- **THEN** the owned overlap stays inactive with an explanation of the controlling behavior
- **AND** the same death is not repaired twice or given extra fire intensity

#### Scenario: Required semantics changed
- **WHEN** the required corpse-placement and fire data flow cannot be established, or installation fails
- **THEN** the affected owned group is withheld with the bounded startup warning
- **AND** unrelated features and foreign patches remain in control of their own behavior
