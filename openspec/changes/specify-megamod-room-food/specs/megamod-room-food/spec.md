# Room food preference

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S111/P2-40**. Draft only; no implementation or acceptance. Source-derived
defaults and proposed corrections below are explicit draft choices, not newly confirmed user preferences.

## ADDED Requirements

### Requirement: RF01 Room preference has a narrow feeding scope
During ordinary on-map food selection, the feature SHALL add room preference when the food-getter
is a free colonist and the eater is humanlike. It SHALL NOT change hunger, work priority, who can
feed whom, food quantities or the action's permissions. Animal eaters, slaves/prisoners choosing
their own food, foreign visitors, world-caravan food choice and routine inventory refilling receive
no added room preference. A player order to consume a particular item keeps that item; a prioritized
ordinary feeding job can still choose room food for its recipient.

#### Scenario: Colonist chooses an ordinary meal
- **WHEN** a free colonist's ordinary eating job searches the map for food
- **THEN** eligible dining-room food participates through RF02 without creating an extra eating job

#### Scenario: Free colonist feeds another humanlike
- **WHEN** a free colonist performs an allowed ordinary feeding job for a patient or prisoner
- **THEN** recipient-room preference participates through RF03 while the job retains its normal eligibility and priority

#### Scenario: Other eater or activity
- **WHEN** an animal, visitor or self-feeding prisoner selects food, a caravan eats, or a colonist refills carried supplies
- **THEN** this feature adds no room preference to that activity

#### Scenario: Explicit item versus feeding order
- **WHEN** the player orders consumption of a specific meal or instead prioritizes feeding a patient
- **THEN** the first order keeps its selected meal and the second uses ordinary feeding with recipient-room preference

### Requirement: RF02 Self-feeding prefers the nearest usable dining room
Within the table-search range, the feature SHALL choose the nearest accessible, permitted table
whose location is safe for the getter and whose enclosed room contains at least one currently
eligible map food source. A normal building providing an eating surface counts; a special Dining
Room designation, assigned seat or chair is not required. Prison rooms do not qualify for this
self-feeding preference. Distance to a table is straight-line horizontal cell distance from the
getter; exactly equal table distances permit either qualifying room without random selection on
every query. The room's food wins the map-food comparison even if an otherwise eligible food
outside is higher quality or scores better. Within that room, RF05 determines the food choice.

The whole room as currently recognized by the game participates, including connected portions
that the game treats as one room. Open outdoor expanses do not qualify; an enclosed courtyard can
qualify without a roof. There is no separate opaque room-size exclusion. A nearer room containing
only ineligible food SHALL be skipped rather than masking a farther qualifying room. With no
qualifying room, ordinary selection remains authoritative. This preference does not force the pawn
to eat at the table that identified the room or change native seating/table use.

#### Scenario: Simple meal in a dining room
- **WHEN** the nearest qualifying table's room contains an allowed simple meal and a lavish meal is outside
- **THEN** the simple meal is the preferred map source, even if the lavish meal has a higher ordinary score

#### Scenario: Nearest room has only unusable food
- **WHEN** the nearest table's room contains only forbidden or inaccessible food and a farther qualifying room has an eligible meal
- **THEN** the farther room is preferred rather than stopping at the first table

#### Scenario: No qualifying room
- **WHEN** every table is outside the range, inaccessible, unsafe, in a prison room or without eligible food
- **THEN** ordinary map-food selection remains unchanged, including an ordinary no-food result

#### Scenario: One connected room
- **WHEN** the game treats two connected portions as one enclosed room and an accessible permitted table and meal are in different portions
- **THEN** that meal can qualify without an additional restriction to the table's portion or an opaque room-size cutoff

#### Scenario: Outdoor and unroofed spaces
- **WHEN** one table is in an open outdoor expanse and another is in an enclosed unroofed courtyard
- **THEN** only the courtyard can qualify as a room, subject to all other requirements

#### Scenario: Eating place remains native
- **WHEN** a colonist obtains room-preferred food
- **THEN** normal eating-place and seat selection still apply; room preference neither reserves that room's table nor guarantees eating there

### Requirement: RF03 Caregivers prefer the recipient's room
When getter and eater differ, the feature SHALL prefer eligible food in the eater's current
enclosed room without requiring a table, hospital room role or dining-room search. This includes
ordinary care in a bedroom and permitted prisoner feeding. The eater's requirements and the
getter's ability to fetch and deliver the food both remain binding. Food in a different prison
room gains no permission from this preference. If the recipient has no enclosed room or its room
has no eligible source, ordinary feeding selection SHALL continue. The table-search setting does
not limit this branch; the feeding action's own search and access limits still apply.

#### Scenario: Patient food without a table
- **WHEN** an eligible feeding job serves a patient in a bedroom containing a permitted meal and no table
- **THEN** that meal is preferred over otherwise better map food outside the patient's room

#### Scenario: Prisoner food remains socially permitted
- **WHEN** a warden feeds a prisoner whose room contains an allowed meal while another prison room contains other food
- **THEN** preference uses the recipient's room without granting access to food the action normally forbids

#### Scenario: Empty room or outdoor patient
- **WHEN** a patient is outdoors or has no eligible food in its room
- **THEN** the caregiver retains ordinary food selection rather than failing merely because room preference found nothing

#### Scenario: Recipient and getter differ
- **WHEN** a caregiver can eat a room meal that the recipient's respected diet or policy forbids
- **THEN** the meal is rejected for that feeding job regardless of the caregiver's own diet

### Requirement: RF04 Every preferred source remains usable by the action
Room preference SHALL preserve the current food search's allowed source set, nutrition, freshness,
diet, food policy, ideology/title rules, minimum food quality, corpse/drug permissions, social
propriety, forbidden state, reservations and search limits. The getter must be able to reach and
use the actual source through the action's normal path and danger rules. Being in the selected
room never grants permission. Supported dispensers retain their power, feedstock, interaction and
refilling constraints; a source is located where its food is actually obtained, including the
serving side of a dispenser built into a wall. Room preference does not add harvesting, hunting,
container unloading, medical drug administration or a new special-substance consumption route.

#### Scenario: Reachable table but unreachable food
- **WHEN** a table is reachable but its room's only food cannot be reached or touched under the feeding action's rules
- **THEN** that food is rejected and ordinary or another qualifying room's food remains available

#### Scenario: Reserved or newly forbidden meal
- **WHEN** another pawn reserves the needed portion or the meal becomes forbidden before the getter can use it
- **THEN** native action validation prevents improper use and permits ordinary recovery without overriding the reservation or prohibition

#### Scenario: Corpse or nutritious drug
- **WHEN** a selected room contains a corpse or a nutritious drug
- **THEN** it is considered only if that ordinary feeding action permits it; room preference never promotes a nonfood drug or hemogen request into a meal

#### Scenario: Dispenser serving a room
- **WHEN** an admitted dispenser serves the selected room from its interaction side and the actual feeding action permits using it
- **THEN** it can supply room food while retaining power, food-stock and interaction requirements
- **AND** an action that forbids refilling cannot select an empty dispenser on a promise of refilling

#### Scenario: Restricted candidate search
- **WHEN** an ordinary search intentionally excludes an otherwise desirable room food source or imposes a shorter search limit
- **THEN** room preference does not reintroduce the excluded source or extend that action's food-search limit

### Requirement: RF05 Food preferences compose in one selection
Within the selected room, food SHALL use ordinary eater-specific comparison, including distance
from the getter. Enabled Common Sense spoilage preference and preserved-food preference contribute
once under their own contracts. Room preference outranks a better map-food score outside the room;
it does not alter those contributions within it. Ordinary choices completed before map comparison,
including carried food about to rot, remain intact. After selecting the map source, native comparison
with eligible personal inventory remains intact, so room preference is not a guarantee against
carried food. Ordinary harvest handling remains governed by the requesting action.

Shared carried-food borrowing runs only if the resulting ordinary selection has no source. Room
preference SHALL NOT borrow, unload, reserve, move or consume food during a search. Hauling cargo,
routine carrying, actual nutrition, ingredients, poisoning history, mood and quantities retain
their existing owners and effects.

#### Scenario: Fresh and preserved food in one room
- **WHEN** the chosen room contains multiple eligible foods with spoilage and preservation preferences enabled
- **THEN** each preference contributes once to the within-room comparison and the chosen food keeps its real effects

#### Scenario: Preserved food stocked beside the table
- **WHEN** an eligible preserved meal is in the nearest qualifying room and an ordinary fresh meal is outside
- **THEN** the preserved meal wins the map-food comparison despite the separate preference for ordinary meals

#### Scenario: Carried food remains usable
- **WHEN** the ordinary process selects carried food before searching the map or later prefers it over the room's map source
- **THEN** the carried source remains selected without being replaced by room preference

#### Scenario: Shared-food fallback
- **WHEN** ordinary selection with room preference succeeds in one search and finds nothing in another
- **THEN** borrowing cannot displace the successful source and is considered only in the failed search under its own eligibility rules

#### Scenario: Repeated queries
- **WHEN** the game evaluates possible food repeatedly without performing a feeding action
- **THEN** no food is created, transferred, consumed or reserved, and no food policy or supply quantity changes because of those queries

### Requirement: RF06 Table-search distance uses common inherited settings
The feature SHALL expose **Nearby-table search distance** in the Food section of mod settings,
with built-in default **100 cells** and integer range **1–200 inclusive**. This is the normal
straight-line search radius for self-feeding tables. If ordinary map selection already found food
farther away, the table search extends to that food's straight-line distance. It does not grow
recursively after selecting room food and does not expand the action's food-search limit. The row's
explanation SHALL disclose this extension and that recipient-room feeding is unaffected.

Use the common Global default, Current save and Use global controls. New/missing save entries use
the current global value; an override affects only that save. Checking Use global preserves a
dormant override; unchecking restores it or seeds the first override from the current global value.
The save editor is inactive while inheriting. All save controls and callbacks are inactive at the
main menu; the global editor remains usable. Valid changes apply to the next selection, without
cancelling a valid ongoing job. Invalid stored values recover to 100 with a bounded diagnostic;
invalid edits cannot be committed. No separate bottom tab, operational toggle or source-settings
file is introduced. All seven baseline languages SHALL explain the same behavior.

#### Scenario: Default radius and boundary
- **WHEN** no save override exists and ordinary map food has not extended the default radius
- **THEN** a qualifying table exactly 100 cells away is in range and one farther away is not

#### Scenario: Existing farther food extends the search
- **WHEN** the effective setting is 100 and ordinary map selection found food 150 cells away
- **THEN** qualifying tables up to 150 cells away can be considered, with no further expansion caused by the chosen room's food

#### Scenario: Inheritance and dormant override
- **WHEN** one save inherits and another retains an override while the global value changes
- **THEN** only the inherited effective value follows the change; switching Use global restores or retains overrides as described

#### Scenario: Main menu and malformed value
- **WHEN** settings are opened without an active save or a stored distance is invalid
- **THEN** save controls cannot mutate a prior or implicit game, and invalid stored distance recovers to 100 without breaking feeding

### Requirement: RF07 Current rooms and food remain authoritative
Subsequent selections SHALL reflect current rooms, doors, tables, source availability, recipient
location, policies and access. Existing valid jobs are not repeatedly redirected merely because a
preferred room changes. An invalid target uses ordinary cancellation/reselection, without an
immediate endless retry of the same unusable source. Save/load, map removal, a different save and
a new game SHALL NOT retain a previous room winner or pawn-specific availability. Query work
remains bounded by the current map and search and creates no recurring full-map scan between food
requests. The feature adds no saved food, room assignment or job state.

#### Scenario: Room changes
- **WHEN** a wall or doorway changes the game's room boundaries or the preferred table is removed
- **THEN** the next selection uses the new layout while a still-valid feeding job can finish normally

#### Scenario: Recipient moves or food disappears
- **WHEN** a new feeding search occurs after the recipient moved rooms or the old room's food was removed
- **THEN** the current room and sources determine the result without stale preference or a rapid failure loop

#### Scenario: Save or map switch
- **WHEN** a save is reloaded, another map is entered or a new game starts
- **THEN** selections use that game's current food, rooms and effective settings rather than old references or winners

### Requirement: RF08 Unavailable preference preserves ordinary feeding
An enabled unavailable owned patch SHALL remain inactive with a bounded startup warning identifying
the affected behavior, failed prerequisite and ordinary-selection fallback. Compatible unrelated
changes alone SHALL NOT disable room preference. If an active original Room Food provider owns
the overlap, owned room preference SHALL be inactive with a warning, leaving the provider untouched.
Missing optional refrigerators or other food providers are quiet; an unavailable requested adapter
leaves that provider's ordinary behavior intact without claiming room-aware support. Repeated
initialization or later incompatibility SHALL NOT duplicate preference or partially alter feeding.
Unavailable or removed room preference leaves physical food, ongoing native jobs and save data usable.

#### Scenario: Unsupported game behavior
- **WHEN** a required food-selection prerequisite cannot be established at startup
- **THEN** the affected owned patch is skipped with one bounded explanatory warning and ordinary feeding continues

#### Scenario: Compatible other change
- **WHEN** another feature changes an unrelated part of food search while the required behavior remains true
- **THEN** room preference remains eligible rather than being refused solely because another mod also changed that method

#### Scenario: Original provider active
- **WHEN** an original Room Food provider is active
- **THEN** owned room preference is inactive with a warning and no second preference or source-settings mutation occurs

#### Scenario: Optional absence or changed provider
- **WHEN** an optional provider is absent or a requested provider integration no longer has its required behavior
- **THEN** absence is quiet, failed requested integration is explained once, and ordinary food selection remains available

#### Scenario: Reinitialization, later incompatibility or removal
- **WHEN** initialization repeats, an effective prerequisite later becomes false or room preference is removed from a save
- **THEN** no duplicate preference remains; affected owned behavior stops safely without removing other mods' work or invalidating physical food and native jobs
