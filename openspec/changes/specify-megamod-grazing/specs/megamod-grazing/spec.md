# Grazing

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S043 / Graze up, revised by S087's [combined consumption contract](../../../specify-megamod-grazing-lands/specs/megamod-grazing-plant-damage/spec.md).
Specification only; no implementation or accepted gameplay.

## ADDED Requirements

### Requirement: Grazing consumes growth while leaving a living plant by default
The mod SHALL let an eligible grazer eat only the nutrition it requests from a supported rooted
plant. Growth-consumed plants SHALL lose growth proportionally and retain at least **8% growth**
unless selected-species destruction or S087's pasture-loss rule consumes them. Crop-health plants
SHALL instead follow S087's finite health rule. Smaller growth bites SHALL leave more growth.
Plant age SHALL remain unchanged; subsequent growth and survival SHALL follow ordinary conditions.

#### Scenario: Small bite from mature grass
- **WHEN** a supported fully grown grass plant has 0.50 base nutrition and a grazer requests 0.10 under default policy
- **THEN** the grazer receives 0.10 nutrition and the same plant remains at 80% growth with unchanged age

#### Scenario: Exhaust available grazing
- **WHEN** a non-destructive grazer requests at least 0.46 nutrition from that mature grass
- **THEN** the bite supplies 0.46 and leaves the same plant at 8% unless the single default 5% pasture-loss roll destroys it; either result grants only 0.46

#### Scenario: Grazing does not cause winter regrowth
- **WHEN** a retained plant cannot currently grow because of ordinary temperature, light or other conditions
- **THEN** grazing does not remove those restrictions or reset its age to prolong its life

### Requirement: Plant nutrition and regrowing-plant bonuses are explicit
The mod SHALL use S087's single nutrition allowance: nominal edible harvest nutrition for plants
with a positive edible yield, or the plant's own nutrition otherwise, multiplied by a configurable
**100%** default scalar. Growth-consumed plants that survive normal harvesting SHALL receive an
additional **250%**, giving **350% total**, before S087's cost for edible-yield consumption. These
controls SHALL NOT change harvested item definitions or harvesting work; ordinary harvest output
SHALL reflect the plant's remaining native growth and health without an extra grazing deduction.

#### Scenario: Regrowing berry bush
- **WHEN** a full-grown regrowing bush yields ten food items of 0.05 nutrition and a grazer requests at least 0.805 with default settings
- **THEN** it receives 0.805 and leaves the bush at 8% growth, without spawning harvested berries

#### Scenario: Hay crop uses the composed crop rule
- **WHEN** a grazer requests at least 0.45 from a mature full-health hay crop yielding eighteen items of 0.05 nutrition under defaults
- **THEN** it receives 0.45 and the crop is consumed through S087's health rule, without an additional 0.276 growth bite or hay items

#### Scenario: Nutrition scalar changes future bites
- **WHEN** the player commits a different grazing nutrition scalar
- **THEN** later bites use it without changing existing plant growth or granting food merely for editing settings

### Requirement: Destructive species remove plants only under the defined hunger condition
The mod SHALL have an empty destructive-species selection by default. This selection SHALL apply
only to growth-consumed plants, not S087's crop-health branch. A selected species SHALL destroy a
supported growth plant only when normal harvesting destroys that plant and requested nutrition
is strictly greater than the available non-destructive bite. A destructive bite SHALL supply the
plant's scaled nutrition multiplied by its current growth plus a configurable **50% full-growth
equivalent** bonus by default, capped to requested nutrition. Regrowing harvest plants SHALL retain
the growth rule even for selected species; only S087's separately applicable pasture-loss rule
can destroy a non-yielding regrowing plant at depletion through this combined grazing policy.

#### Scenario: Listed species requires more than the remaining bite
- **WHEN** a listed species requests 0.60 from full-grown 0.50-nutrition grass under defaults
- **THEN** the plant is consumed once and supplies 0.60, without dropping a second food item

#### Scenario: Equality does not trigger species destruction
- **WHEN** that species requests exactly the 0.46 available non-destructively
- **THEN** the species rule does not destroy it; it supplies 0.46 and follows only the ordinary S087 pasture-loss chance at 8%

#### Scenario: Destruction bonus has a finite cap
- **WHEN** that species requests 1.00 from the same full-grown grass
- **THEN** destruction supplies only 0.75 under the default 50% bonus

#### Scenario: Listed species eats a regrowing bush
- **WHEN** a selected destructive species grazes a plant that survives ordinary harvesting
- **THEN** species selection does not destroy it; its growth falls proportionally to the actual bite, and it remains at least at the remainder unless it has no positive harvest yield and the ordinary S087 pasture-loss roll destroys it

### Requirement: Grazing eligibility distinguishes usable food from ordinary harvest readiness
The mod SHALL require a supported edible, non-burning plant with positive usable nutrition, growth
at least **40%** by default and strictly above the effective remaining-growth limit. This global
grazing minimum SHALL apply to trees as well as other plants, replacing their ordinary grazing growth
thresholds. Leafless state and not being ready for harvest SHALL NOT by themselves prevent grazing.
The eater's normal diet, access, food policy, reservations and reachability SHALL still apply.

#### Scenario: Growth below the threshold
- **WHEN** an edible plant is at 39% growth with the default 40% minimum
- **THEN** it is not selected for grazing and gives no food through this feature

#### Scenario: Leafless but otherwise eligible
- **WHEN** an edible leafless plant meets the growth/nutrition requirements and the grazer can normally eat its plant type
- **THEN** leaflessness alone does not prevent grazing, without curing the condition or changing survival rules

#### Scenario: Edible does not override diet or access
- **WHEN** a plant meets the growth threshold but the eater cannot eat that kind of plant, cannot reach it, or is forbidden access
- **THEN** this feature does not grant a grazing job or bypass the relevant restriction

#### Scenario: Tree growth follows the same threshold
- **WHEN** a tree-eating creature encounters an otherwise eligible tree below the configured grazing minimum
- **THEN** the tree remains unavailable for grazing until the minimum is met

### Requirement: Buried-plant access explicitly covers snow and sand
The mod SHALL allow otherwise eligible grazing through snow and sand by default. With this policy
off, grazing SHALL be unavailable when either burial depth exceeds that plant's ordinary hide depth.
The option SHALL be labeled to describe both materials. It SHALL NOT remove snow/sand, change
movement costs or make buried plants grow under unsuitable conditions.

#### Scenario: Buried grazing enabled
- **WHEN** an otherwise eligible plant lies under snow or sand deeper than its normal hide depth
- **THEN** a capable grazer can eat it in place under the default buried-access policy

#### Scenario: Buried grazing disabled
- **WHEN** buried access is off and snow or sand exceeds the plant's hide depth
- **THEN** the plant is unavailable; equality with the hide depth alone does not exclude it

### Requirement: Unavailable or invalid bites cannot invent food or repeat without progress
The mod SHALL recheck current food availability before a bite takes effect. No usable nutrition,
an invalid quantity, an already depleted/removed target or no requested food SHALL produce no bite,
no growth increase, no artificial nutrition and no destructive fallback. The pawn SHALL finish or
seek valid food normally, without repeatedly selecting the unchanged unusable plant or gaining
successful-ingestion effects from rejected attempts.

#### Scenario: Minimum equals remaining growth
- **WHEN** both limits are 40% and a plant is exactly at 40%
- **THEN** the plant supplies no food and is not repeatedly selected; it becomes eligible only after positive available growth returns

#### Scenario: Another grazer eats first
- **WHEN** a selected plant is depleted below eligibility or destroyed before another pawn finishes its approach
- **THEN** the second pawn gains no food or successful-ingestion effects from that plant and releases the attempt normally

#### Scenario: No hunger or invalid nutrition
- **WHEN** the prospective eater requests no food or the plant has invalid/nonpositive usable nutrition
- **THEN** the attempt does not consume the plant, increase growth or credit an invented food amount

#### Scenario: Settings change before the bite
- **WHEN** a committed policy edit makes an approached plant unavailable
- **THEN** the bite uses the current policy and cancels or reselects without consuming stale quantities

### Requirement: Tool users graze rooted plants in place through ordinary ingestion
The mod SHALL allow supported tool users whose diet permits plant grazing to eat the rooted plant
in place using ordinary approach, reservation, chewing and ingestion effects. It SHALL NOT pick up
the plant, collect extra rooted plants into inventory, or move it to a table. Ordinary harvested-food
jobs and harvesting SHALL retain their native behavior. Cancellation SHALL release the job normally.

#### Scenario: Supported tool user grazes
- **WHEN** an eligible tool user obtains a valid rooted-plant eating job
- **THEN** they approach and chew at the plant, receive one bite result and leave its specified remainder or destruction result

#### Scenario: Cancellation before consumption
- **WHEN** the player interrupts the approach or the target becomes inaccessible before the bite
- **THEN** no plant consumption is credited and the pawn and reservation are not left stuck

#### Scenario: Harvest and ordinary item eating
- **WHEN** a pawn harvests a crop or eats harvested berries as an item
- **THEN** these grazing rules do not replace harvest output/work or the normal item-eating workflow

### Requirement: Grazing configuration uses common scoped settings with honest values
The mod SHALL present grazing policy in **Mod settings → Animals and plants → Grazing**, using
common Global default, Use global and Current save controls. Source-derived defaults and supported
ranges SHALL be: buried access on; minimum growth **40%, 5–95%**; remainder **8%, 1% through the
current minimum**; nutrition scalar **100%, 10–1000%**; regrowing extra **250%, 100–1000% extra**;
destructive extra **50%, 0–1000% full-growth equivalent**; destructive species empty. S087 adds
pasture loss **5%, 0–100%** and crop-consumption cost **200%, 100–500%** to this same settings section.
Labels SHALL distinguish extra from total, and explain ordinary growth, harvest and hunger limits.

#### Scenario: Inherited save and dormant override
- **WHEN** a new save inherits global grazing policy, then the player overrides an option and later toggles Use global on and off
- **THEN** it follows the live global value while inheriting, disables the per-save editor then, and restores its dormant override afterward

#### Scenario: Coupled limits after a global edit
- **WHEN** a save's requested remainder exceeds its effective minimum because of a global edit
- **THEN** the effective remainder is capped to that minimum and clearly shown, without overwriting its stored override or another scope's value; raising the minimum restores the requested remainder when valid

#### Scenario: Main menu and invalid input
- **WHEN** settings are opened without a loaded game or an invalid numeric edit is attempted
- **THEN** only global controls can mutate state at the main menu, and invalid edits are rejected; loaded invalid stored numbers are safely bounded or defaulted with a diagnostic instead of reaching grazing

#### Scenario: Bonus labels are accurate
- **WHEN** default regrowing and destruction bonuses are displayed
- **THEN** the player sees that +250% means 350% total before any edible-yield consumption cost, and +50% destruction means half a full-growth allowance before the hunger cap, only on the growth-consumed branch

### Requirement: Species selections and scoped resets preserve intent
The mod SHALL offer searchable, ordered plant/tree-eating species with distinguishable identities,
including supported modded species. The destructive-species policy SHALL be a scope-owned set,
without duplicate entries. Temporarily unavailable selections SHALL remain visible as inactive
choices and SHALL NOT be erased by rendering or loading. An explicit global reset SHALL restore
built-in global defaults; a current-save reset SHALL restore inheritance and clear dormant overrides
only for this feature in that save. All new UI SHALL use the seven baseline languages.

#### Scenario: Species temporarily disappears
- **WHEN** a previously selected species is unavailable and the player opens settings
- **THEN** its stored choice remains inactive until it returns or is explicitly removed/reset, without being mapped to a similarly named species

#### Scenario: Scope-specific reset
- **WHEN** the player resets only current-save grazing settings
- **THEN** that save returns to inheritance with its grazing overrides cleared, while global values, other features and other saves remain unchanged

#### Scenario: Global reset preserves explicit save policy
- **WHEN** the player resets global grazing defaults
- **THEN** inherited saves follow the defaults and explicit save overrides remain stored and effective subject to the coupled-limit rule

### Requirement: Native state and compatible providers retain one coherent result
The mod SHALL retain native plant growth, age and ordinary successful-ingestion effects without
duplicating them. Existing plants SHALL use current committed policy on their next relevant action,
without conversion or automatic upstream-settings import. Unsupported or competing provider paths
SHALL fall back as a complete affected behavior group, with bounded startup warnings for each skipped
enabled patch and an explanatory settings status. Compatible foreign modifications SHALL NOT be
rejected merely for existing.

#### Scenario: Save and reload a grazed plant
- **WHEN** the player saves and reloads a retained plant and valid owned settings
- **THEN** its saved growth/age and scoped policy persist without replaying the bite or granting food again

#### Scenario: Original provider active
- **WHEN** original Graze up or Grazing Lands is active
- **THEN** owned overlapping grazing remains inactive with an explanation, leaving the original and unrelated owned features untouched

#### Scenario: Unsupported target semantics
- **WHEN** a required grazing target's consumed behavior is missing or ambiguous
- **THEN** the affected grazing group is withheld without partial widened eligibility, with logs identifying each skipped enabled patch, failed requirement, lost behavior and fallback

#### Scenario: Existing estimates remain estimates
- **WHEN** the player consults an existing pen food or nutrition display which does not model these grazing policies
- **THEN** it is not presented as an exact new stocking forecast; grazing itself uses the specified actual bite rules rather than that estimate
