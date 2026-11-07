# Map Thing searches

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
S033 Ctrl F - Continued. Specification only; no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: Explain what is being searched
The search SHALL offer Map things as its initial listing, with alternatives for Pawns, Items, Buildings, Plants, Natural objects, Haulable non-items, All ordinary things and Known contents.
Map things includes normally selectable map objects and known contained things. Pawns includes
spawned pawns of every faction/species, without an implicit colonist, age or health restriction.
Items means ordinary always-haulable objects; Haulable non-items covers other potentially haulable
objects. Buildings excludes natural rock; Plants includes normally selectable plants; Natural
objects covers natural rock formations. All ordinary things also includes ordinary unselectable
objects such as grass and filth, but excludes transient projectiles, effects and invisible engine
helpers. Known contents searches contents rather than adding their holders automatically.
All ordinary things includes known contents. The individual category listings otherwise search
spawned objects; the player can combine them with Known contents. Combining overlapping listings
does not duplicate results. These listing choices and all criteria remain visibly stated.

#### Scenario: Find a wild animal
- **WHEN** the player chooses Pawns without faction or species criteria
- **THEN** visible wild animals, enemies, colonists, children and downed pawns are eligible results on the chosen maps.

#### Scenario: Search both loose and carried items
- **WHEN** the player combines Items with Known contents
- **THEN** matching loose items and disclosed carried items are included once each, with their locations distinguished.

### Requirement: Search only ordinary player knowledge by default
Ordinary searches SHALL exclude fog-hidden objects and undisclosed contents, including unopened containers whose contents are not known to the player.
Forbidden, reserved, unreachable or non-player-owned objects remain searchable when known; being
found does not make them usable. A revealed holder permits its known contents to be searched
without requiring those contents to be individually selectable. Caravans away from loaded maps,
orbital traders and unknown world locations are outside these map listings. Enabling developer
or god mode does not broaden an ordinary search. Any future hidden-information search belongs to
an explicitly labeled, separate debug workflow entered through the top debug toolbar; it cannot
silently alter ordinary searches or production/hauling decisions.

#### Scenario: Hidden cargo and forbidden visible stock
- **WHEN** a search encounters an undisclosed casket and a visible forbidden steel stack
- **THEN** the casket's unknown contents are absent while the steel can appear with its ordinary restriction intact.

#### Scenario: Change developer state
- **WHEN** god mode is toggled while an ordinary search is open
- **THEN** its next refresh still follows ordinary visibility and does not reveal fog-hidden results.

### Requirement: Provide explicit basic matching criteria
The search SHALL support name text, exact object type, construction material, and zone criteria without requiring another mod.
Name text is a case-insensitive literal substring of the displayed object's name, not a regular
expression or a match on internal identifiers; empty text imposes no restriction after a search
is deliberately evaluated. Type selection uses the actual type identity even when labels match.
An optional inclusive stack-quantity range applies to each individual stack, with unrestricted
ends by default. Material choices include an exact material, a material category, any material,
and not made from material; construction plans use their chosen building material. Zone choices
include an exact zone, any stockpile, any growing zone, and any zone, based on the object's current
held location. Range inputs cannot silently replace a valid range with reversed or invalid bounds.
More specialized criteria require their own S121 catalogue contracts before support is claimed.

#### Scenario: Match names literally
- **WHEN** the player searches for mixed-case text or punctuation appearing in a visible object's name
- **THEN** case does not matter, punctuation is literal, and an unrelated internal identifier alone does not match.

#### Scenario: Restrict a stack rather than the total
- **WHEN** an exact-item criterion allows quantities 10 through 30 and matching stacks contain 5, 20 and 40
- **THEN** only the stack of 20 satisfies that criterion; the three stacks are not summed before applying the range.

#### Scenario: Distinguish materials and zones
- **WHEN** a material-category criterion and an exact-zone criterion are both required
- **THEN** only objects with an eligible material and a held location inside that particular zone match.

### Requirement: Treat wrapped objects as one physical result
Search criteria SHALL recognize the represented object inside a minified item or corpse without counting that inner object as an additional physical result.
Each basic criterion can match the wrapper or its represented object; inclusion accepts a match
on either, and exclusion rejects a match on either. A corpse therefore can be found by its dead
pawn's name and a minified building by its building type/material. Their row identifies the
corpse/minified state. Separate known possessions inside a holder remain distinct physical items;
the relationship is shown rather than pretending they are loose or directly selectable.

#### Scenario: Find a packed building
- **WHEN** a minified wooden bed matches the selected building and material
- **THEN** one minified-bed result appears, located at its actual holder or map position, without an extra inner-bed count.

### Requirement: Compose criteria without hidden changes
The editor SHALL support include/exclude, explicit enable/disable, All, Any, nested groups and an At least N group mode.
New searches begin in All mode; a newly added nested group begins in Any mode, and first choosing
At least N starts at two. Adding a type/material/exact-zone criterion requests an explicit choice
before enabling it rather than silently choosing an arbitrary item, material or zone.
All requires every enabled child; Any requires at least one; At least N requires at least the
entered positive whole number. An empty All group matches, whereas empty Any or At least N does
not. A threshold larger than the enabled-child count produces no matches and is explained rather
than silently reduced. Excluding a group negates its combined match. Deliberately disabled criteria
are shown and skipped; an unresolved or failed enabled criterion is an error, not a disabled one.

#### Scenario: Combine alternatives and a restriction
- **WHEN** a search requires either wood or steel and excludes objects in a specified stockpile
- **THEN** only wood/steel results outside that stockpile match, regardless of criterion order.

#### Scenario: Distinguish disabled from unresolved
- **WHEN** a player deliberately disables one criterion but another enabled criterion has lost its required content
- **THEN** the deliberate disable is respected and the unresolved criterion prevents a valid result until repaired or explicitly removed/disabled.

### Requirement: Make map scope explicit and stable
The player SHALL choose Current map, All loaded maps or a nonempty explicit set of loaded maps.
Current map follows the viewed map at the next evaluation; All loaded maps includes currently
loaded maps, not ungenerated world tiles. Explicit maps retain their identity across view changes.
An exact-zone criterion is bound to that zone on its own map and visibly constrains the search to
that map; a same-named zone elsewhere is not a substitute. Conflicting map-specific criteria are
explained as incompatible. A removed required map or zone makes the affected search unavailable
until repaired, without silently falling back to the viewed map. In world view, an existing
explicit/all-maps search can still evaluate loaded maps; Current map requires a viewed map.

#### Scenario: Follow the viewed colony
- **WHEN** the player changes from colony A to colony B and refreshes a Current map search
- **THEN** the results are for B; a search explicitly pinned to A still searches A.

#### Scenario: Lose a selected zone or map
- **WHEN** the selected zone is deleted or its map is removed
- **THEN** the search reports the missing target and does not substitute another zone/map or show an unrestricted result.

### Requirement: Count physical matches honestly
Results SHALL distinguish the number of matched objects/stacks from the total physical units in those matches.
A stack contributes its actual quantity, while an unstacked pawn/building contributes one.
Counts are not nutrition, market value, storage capacity or reservable supply. Overlapping categories,
nested holder discovery and map/holder transfers cannot count the same physical thing twice.
Each result identifies its map and, when contained, its holder/location. A successful search with
no matches displays zero; a search not yet run, unavailable search and failed evaluation have
distinct states without a misleading zero or apparently complete partial total.

#### Scenario: Count two stacks
- **WHEN** a successful search matches steel stacks of 12 and 8
- **THEN** it shows two matched stacks/objects and twenty physical units.

#### Scenario: Move a stack into a pawn's inventory
- **WHEN** a loose matching stack becomes carried and the combined loose/known-contents search refreshes
- **THEN** the stack appears at its new holder once, with its current quantity and no stale loose duplicate.

### Requirement: Keep shared facts consistent across consumers
At each completed evaluation the search SHALL use current map facts while retaining its own visibility and listing rules.
Relevant creation, destruction, movement, holder/map transfer, stack split/merge, name, material,
zone and other supported criterion changes are reflected at the specified refresh boundary.
Search, construction stock information and hauling agree on the same object's current identity,
quantity and location when evaluated at the same boundary. Their totals can legitimately differ:
search can include loose/carried/forbidden stock, construction counts only its specified stored
resources, and a hauler still needs ordinary capacity, access, reservation and destination eligibility.
No search or selection action creates a hauling job or bypasses those rules.

#### Scenario: Compare found stock with usable stock
- **WHEN** known steel is divided among storage, loose ground and a pawn's inventory
- **THEN** the search can show the chosen categories, construction reports only qualifying stored units, and hauling independently honors its actual eligibility rules.

### Requirement: Fail an evaluation visibly without broadening it
An unavailable criterion, provider or map evaluation SHALL make the search visibly unavailable with a useful reason and no authoritative total or enabled result actions for an incomplete evaluation.
The player's query remains intact for repair. The UI can retain a clearly labeled previous snapshot
for context, but cannot present it as current or let it drive actions. Ordinary searches in other
workspaces/consumers continue if unaffected. Recovery requires a successful complete evaluation;
missing clauses are never silently discarded, including when importing a query.

#### Scenario: One searched map cannot be evaluated
- **WHEN** evaluation succeeds for one selected map but fails for another
- **THEN** the UI reports an incomplete search, does not publish the first map's subtotal as the complete result, and preserves the selected maps and criteria.
