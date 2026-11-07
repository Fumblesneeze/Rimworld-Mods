# Toddler temperature safety

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
User amendment P1-24 to S030; full toddler-life-stage replacement is separately owned by S031.
Proposed behavior only; source investigation does not independently reproduce the reported bug.

## ADDED Requirements

### Requirement: Idle toddlers avoid dangerous temperatures before injury
The mod SHALL include temperature prevention for player-colony toddlers supplied by the supported
owned life stage or an admitted optional toddler provider. Autonomous idle wandering and newly
chosen autonomous play destinations/routes SHALL avoid temperatures outside that toddler's actual
safe range, considering current clothing, traits and supported local heating/cooling. For the
inspected base-game rule, safe range is the pawn's comfortable range extended by **10°C at each
end**; a provider replacing that rule requires its own admitted interpretation. This protection
does not wait for a hypothermia/heatstroke threshold, impose a fixed age-only Celsius range or
grant immunity. Ordinary allowed-area and physical access rules apply to idle/play selection.

#### Scenario: Warm room beside a dangerously cold yard
- **WHEN** a mobile toddler idles or chooses autonomous play with a dangerously cold destination available nearby
- **THEN** that destination and a route unnecessarily through that danger are not chosen
- **AND** safe alternatives remain available without requiring the toddler to become injured first

#### Scenario: Clothing or local heat changes safety
- **WHEN** the toddler's protection changes or supported local heating changes a candidate cell's temperature
- **THEN** the next idle/play decision uses the new actual safe range and temperature
- **AND** another toddler with different protection can legitimately have different valid destinations

### Requirement: Mobile toddlers escape unsafe surroundings early
The mod SHALL evaluate current toddler temperature during autonomous idle/play and owned safety
movement at least every **250 game ticks**. If surroundings become thermally unsafe, a mobile,
awake, nondowned toddler not under overriding player, emergency, carrying or medical control
SHALL interrupt autonomous idle/play and seek a reachable safe location before waiting for the
ordinary adult injury threshold. This early movement is not blocked merely by low food or play.
Prefer a location allowing any existing temperature injury to improve; if none exists, a reachable
location inside the safe range can stop further thermal exposure without promising recovery.
Safe arrival SHALL end the escape movement whether or not an injury remains. Ordinary recovery
can follow only when its separate eligibility and suitable-refuge conditions hold; otherwise
ordinary care and safe activities resume without an injury-based hold or repeated escape selection
from an already safe location. The escape does not itself impose sleep or claim a cure.

Outside-area escape uses the common recovery outside-area preference, default on. The selected
escape route can traverse the existing unsafe starting area to reach safety, with ordinary native
physical and emergency path restrictions; this is not a general change to all game pathfinding.
Automatic return to an allowed area SHALL NOT undo a successful escape or safe caregiver placement
by sending the toddler back into dangerous temperature. Until a thermally safe permitted return
destination and route exist, it remains at the safe refuge subject to ordinary emergency, care and
explicit player control. This temporary safety precedence does not edit the assigned area or
authorize new idle/play excursions beyond it. Return can resume after the area/access becomes safe.

#### Scenario: Heater fails before injury appears
- **WHEN** a mobile toddler's idle/play location becomes too cold after heating fails
- **THEN** within 250 game ticks it can choose a physically reachable safe destination and begin moving there
- **AND** it does not wait for 20% hypothermia or an adult rescue when it can leave itself

#### Scenario: Only nonworsening shelter exists
- **WHEN** a toddler above the configured injury-response threshold can reach a thermally safe location but no location that currently permits injury recovery
- **THEN** it can move out of the harmful temperature without being reported as cured
- **AND** safe arrival ends escape movement and returns to ordinary care or safe activity, without starting an ineligible recovery hold or repeatedly escaping from the already safe location
- **AND** remaining health conditions persist, and later suitable recovery remains possible under its own eligibility rules

#### Scenario: Safe refuge outside an unsafe assigned area
- **WHEN** self-escape or an adult rescue places a toddler safely outside its allowed area while every return destination or route in that area remains thermally unsafe
- **THEN** automatic area-return does not send it back into that danger or trigger an escape-and-return loop
- **AND** it stays at the refuge without changing the area assignment until a safe permitted return is available or overriding player/emergency/care control takes over
- **AND** after the area becomes safe, normal permitted return can resume

#### Scenario: Escape must cross the current cold room
- **WHEN** the toddler starts in a cold room and the only ordinary accessible route to safe shelter crosses that room
- **THEN** the route is not rejected solely because its starting portion has the exposure being escaped
- **AND** this exception does not permit a fresh idle/play trip through avoidable thermal danger

### Requirement: Real mobility and caregiver rescue remain authoritative
The mod SHALL respect the toddler's actual crawling/walking, door, crib, path, carrying and
reservation constraints. A crawler cannot open a closed door or leave a crib it cannot normally
leave merely because safety is desired. For no reachable destination, the feature SHALL end a
bounded search without an invalid order, repeated zero-distance walking or an endless loop;
unchanged no-route attempts occur no more often than every **500 game ticks**, with changed
access/hazard facts allowing an earlier attempt at the next 250-tick safety evaluation. Native or
supported caregiver rescue remains available. A caregiver's ongoing pickup/carry action takes
precedence over independent escape; self-escape must not repeatedly undo a completed safe rescue.

#### Scenario: Crawler blocked by a closed door
- **WHEN** a crawler cannot open the only door to safe shelter
- **THEN** it does not gain door-opening ability and the no-route result leaves caregiver rescue available
- **AND** a mobile walker that can legally use that door can independently take the same route

#### Scenario: Single-cell room or trapped crib
- **WHEN** the toddler is confined to one unsafe cell or cannot leave its crib
- **THEN** the game continues normally without an endless search, movement loop or teleport
- **AND** an adult who can physically reach and carry it remains able to perform ordinary rescue

#### Scenario: Adult rescue is already underway
- **WHEN** an adult has begun the supported pickup/carry action for the toddler
- **THEN** the toddler does not start a competing escape that repeatedly defeats that action
- **AND** after safe placement, idle/play prevention avoids immediately wandering back into the dangerous temperature

### Requirement: Toddler safety composes once and fails explicitly
The mod SHALL implement the owned toddler and environmental behavior as one composed rule,
without duplicate movements or provider-specific guesses about age. Supported external toddler
behavior uses actual life-stage and mobility rules; absent toddler content leaves this branch
dormant. Unsupported active toddler or temperature semantics withhold the affected owned safety
behavior with a bounded startup warning; other supported adult/animal behavior remains available.
Original Brrr and Phew co-installation can leave the separately admitted toddler prevention/escape
active only when it does not duplicate source/provider control; otherwise that branch is also
withheld explicitly. No requirement changes growth, learning, feeding, crib mechanics or the
caregiver work system beyond this temperature composition.

#### Scenario: Owned toddler life stage or supported external provider
- **WHEN** a player-colony toddler is supplied by the owned S031 behavior or a supported admitted provider
- **THEN** the same prevention and escape outcome applies once using its actual movement capabilities
- **AND** baby, adult and unrelated age-based pawn types are not silently treated as that life stage

#### Scenario: Missing or changed toddler support
- **WHEN** toddler content is absent or an active provider's required safety/mobility behavior is incompatible
- **THEN** absent content produces no toddler feature, while incompatible enabled support produces a startup warning naming the missing behavior and ordinary fallback
- **AND** no complete Toddlers replacement or 1.7 compatibility is claimed merely because adult environmental recovery works
