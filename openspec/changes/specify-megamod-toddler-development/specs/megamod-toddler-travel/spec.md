## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.

## ADDED Requirements

### Requirement: Toddlers travel as dependent passengers

Awake toddlers SHALL be carryable for ordinary eligible rescue, caravan, shuttle and transport
workflows without pretending they are injured. A mobile toddler SHALL be able to reach a gathering
place when its actual route and capability permit; adult carrying SHALL remain available for
departure. Toddlers SHALL NOT gather items, carry other pawns, rope animals, reload, perform care
or otherwise act as caravan workers. Passenger assignments SHALL remain unique through repeated
selection, removal, cancellation and late joining. Native carrier capacity, access and destination
rules SHALL remain; toddler carrying SHALL not create extra item-hauling capacity.

#### Scenario: An awake toddler joins a caravan
- **WHEN** the player includes a mobile toddler and a suitable adult in a valid caravan
- **THEN** the child is prepared and transported as one passenger through actual movement/carrying
- **AND** it receives no injury and does not start gathering cargo

#### Scenario: Departure changes
- **WHEN** the player removes and re-adds the child, cancels departure or its carrier becomes unavailable
- **THEN** only the current passenger assignment remains and ordinary clear failure/reassignment behavior applies
- **AND** safety is not bypassed merely because a gathering spot is closer

### Requirement: Putting down a carried pawn reports the actual result

An undrafted player-controlled carrier holding a pawn SHALL have a contextual Drop carried pawn
command. It SHALL place the pawn only at a valid nearby destination, respecting reservations and
child safety. If no suitable placement exists, the carrier SHALL retain the pawn and the command
SHALL explain the failure. Repeated input SHALL not duplicate, lose or falsely report placing it.

#### Scenario: The carrier is surrounded
- **WHEN** the player orders a carried toddler put down with no valid nearby cell
- **THEN** the child remains held and the player receives a useful unavailable-placement explanation
- **AND** an ordinary attempt can succeed after a safe space becomes available

### Requirement: Noncombatant status does not create invulnerability

Toddlers SHALL not receive ordinary fighting, hostile-approach, sapper, ambush or turret jobs and
SHALL not count as ordinary combat threats. Damage, battlefield hazards and predator hunting SHALL
remain possible. Hostile kidnappers SHALL be able to select an awake toddler where ordinary victim
faction, reachability, reservation, distance and exit rules permit. Other victim restrictions and
third-party job intentions SHALL not be discarded to obtain that eligibility.

#### Scenario: An awake vulnerable toddler
- **WHEN** a hostile eligible kidnapper can reach an unprotected toddler and exit the map
- **THEN** it can attempt actual pickup and escape despite the toddler being awake
- **AND** walls, lost access, changed eligibility and interception still prevent or interrupt the attempt normally

#### Scenario: Battlefield danger
- **WHEN** a toddler is exposed to a damaging hazard or a hunting predator
- **THEN** ordinary health consequences remain possible even though the toddler does not fight as a combatant

### Requirement: Abandoned-child crashes preserve the arrival story and ordinary eligibility

The ordinary baby-refugee crash event SHALL remain conditional on children being allowed and
ordinary event eligibility. An arriving infant or toddler SHALL survive as a non-player child,
without automatic recruitment or prisoner status. Its chronological age SHALL initially equal
biological age. The traumatic-crash mood effect SHALL be -20 for 6 days with no stacking.
There SHALL be a 50% chance to attempt including a dead parent when a valid missing/untracked
parent slot exists; a generated parent SHALL share the child's faction and xenotype, have the
matching relation and arrive as a real corpse in the same pod. Existing tracked parents SHALL not
be overwritten. This feature SHALL not increase the event's ordinary frequency.

#### Scenario: A crash with a parent
- **WHEN** an eligible baby-refugee crash selects a valid deceased-parent outcome
- **THEN** the pod contains the living child and related dead parent, and the child receives the stated trauma
- **AND** the child is neither instantly recruited nor made a prisoner

### Requirement: Abandoned children can wait safely and be adopted

An abandoned child SHALL initially remain near the arrival rather than immediately leave the map:
a suitable anchor within 12 cells and ordinary wandering within 3 cells of it, subject to actual
mobility and S030 safety. Necessary food, rest and safe escape SHALL take precedence over staying
near that anchor. Self-feeding SHALL require its learned ability; an incapable child SHALL retain
ordinary adult rescue/care opportunities. An eligible abandoned non-colonist toddler with no adult
of its faction present SHALL offer ordinary contextual adoption, preserving native restrictions
and family relations. Adoption SHALL end abandoned-child control immediately.
An unadopted living arrival that ages beyond the dependent Baby stage SHALL become a factionless
wild person through the ordinary applicable behavior. Death, departure, rescue, adoption and
save/load SHALL not leave stale control, erase an adopted pawn or stop unrelated new jobs.

#### Scenario: Adoption after a crash
- **WHEN** the player uses the eligible child's contextual Adopt action
- **THEN** the child joins through the ordinary adoption workflow and is no longer controlled as an abandoned arrival
- **AND** family relations, health and possessions survive adoption and reload

#### Scenario: Arrival location becomes unsafe
- **WHEN** dangerous temperatures develop near the arrival anchor
- **THEN** a capable child seeks safety according to S030 and an incapable child remains eligible for rescue
- **AND** the loiter rule does not force it back into danger

#### Scenario: An unadopted child grows up
- **WHEN** the still-abandoned living child leaves the dependent life stage
- **THEN** it becomes a factionless wild person once and releases the old arrival behavior
- **AND** a child already adopted or removed is not transformed by a stale arrival record
