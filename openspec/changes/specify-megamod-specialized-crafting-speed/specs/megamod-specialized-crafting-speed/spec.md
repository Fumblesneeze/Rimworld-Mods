# Specialized crafting speed

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld1.7 beta.
Origin replacement **S122 De-generalize Work**. Specification only; no runtime acceptance claimed.

## ADDED Requirements

### Requirement: Separate speeds retain ordinary labor effects once

The feature SHALL provide inspectable smithing, tailoring and sculpting speeds. Each SHALL have a
neutral specialized contribution of100%, a final minimum of10%, and include the pawn's ordinary
general-labor effects once. A specialized bonus SHALL affect its own activity without granting a
bonus to the other two activities. The feature SHALL add no automatic crafting-skill speed curve.

#### Scenario: Neutral specialization
- **WHEN** a pawn with general labor speed80% and no specialized modifier performs an eligible tailoring recipe
- **THEN** its tailoring speed is80% and the crafting work progresses at that speed, with ordinary worktable effects still applied

#### Scenario: Independent specialization and lower boundary
- **WHEN** a pawn with general labor speed100% has a tailoring offset+50% and a sculpting offset that would reduce sculpting below10%
- **THEN** tailoring speed is150%, sculpting speed is10%, and smithing remains100% without a separate smithing modifier

### Requirement: Eligible recipe activity determines the specialized speed

Recipes currently using general-labor speed SHALL use tailoring for tailoring activity, sculpting
for sculpting activity, and smithing for smithing, smelting-effect or cooking-effect processing.
This includes standard component and advanced-component production when they still use general
labor. The processing rule deliberately retains the source's classification; it SHALL NOT turn
ordinary cooking or native smelting recipes into smithing when they already use a distinct speed.
The same classification SHALL apply to direct recipes and recipes associated with craftable items.

#### Scenario: A general-labor tailoring recipe
- **WHEN** a pawn starts an eligible clothing recipe whose activity is tailoring
- **THEN** its production progress uses tailoring speed while its ingredients and resulting clothing are unchanged

#### Scenario: General-labor processing
- **WHEN** an eligible general-labor processing recipe uses a cooking or smelting activity
- **THEN** it uses smithing speed, and the recipe's speed information identifies that choice rather than implying a Cooking-skill bonus

### Requirement: Inherited recipes respect the final activity and explicit overrides

Recipe classification SHALL use the recipe's effective activity and speed, including inherited
values. A child recipe's explicit activity or distinct speed SHALL take precedence over an
inherited classification. An unknown or ambiguous activity SHALL retain its existing behavior
with a startup explanation when an enabled proposed mapping cannot safely be applied.

#### Scenario: Activity and speed inherited separately
- **WHEN** a recipe inherits general-labor speed and defines tailoring activity itself
- **THEN** it receives the same tailoring speed as an equivalent recipe defining both values together

#### Scenario: A child changes its activity
- **WHEN** a recipe derived from a smithing recipe changes to sculpting while still using general-labor speed
- **THEN** it uses sculpting speed rather than accidentally inheriting the parent's smithing remapping

#### Scenario: A child already has its own speed
- **WHEN** a derived recipe supplies a distinct provider-specific speed
- **THEN** the base classification leaves that speed and its bonuses intact unless a separately admitted named integration covers it

### Requirement: Existing specialized and unclassified work remains unchanged

Native smelting, cooking, drug synthesis and any other distinct work speed SHALL remain in use
outside the named integration contract. General-labor recipes with other activities SHALL retain
general-labor speed. This feature SHALL NOT redefine which tasks are hauling, Basic work, smithing
or another Work-tab assignment.

#### Scenario: Native and bulk smelting
- **WHEN** a pawn performs an ordinary smelting recipe or S013 bulk smelting using native smelting speed
- **THEN** progress still uses native smelting speed, including its own capacity effects, rather than smithing speed

#### Scenario: Cooking and unrelated work
- **WHEN** a pawn cooks a meal using cooking speed or performs an unmapped general-labor recipe
- **THEN** the respective cooking or general-labor speed remains in effect and the Work-tab priorities are unchanged

### Requirement: Production and assignment contracts remain native

Changing work speed SHALL NOT change ingredient selection, work required, output quantity,
research, skill requirements, worker selection, product quality rules or bill completion rules.
The existing training rules SHALL continue to operate; time-based training can therefore award
less experience per product when the pawn completes it faster. No compensating XP grant is added.

#### Scenario: Faster work produces the same contracted product
- **WHEN** two otherwise equivalent eligible pawns make the same recipe with different specialized speeds
- **THEN** the faster pawn finishes sooner without cheaper ingredients, extra products, lower skill requirements or an automatic quality increase

#### Scenario: Time-based training remains time-based
- **WHEN** a faster pawn spends fewer ticks on a recipe that awards experience for time worked
- **THEN** training follows the shorter actual work time rather than an invented per-product compensation

### Requirement: Inspection explains the speed actually used

Pawn stat details and owned recipe inspection SHALL identify the effective specialized speed and
its contributing general and specialized effects. Descriptions SHALL distinguish speed from
quality. Changes to equipment, health or traits SHALL be reflected through ordinary stat updates,
without retaining stale feature-local results.

#### Scenario: Equipment changes a specialized bonus
- **WHEN** a pawn equips or removes an admitted tailoring-speed item and the player inspects its stats and starts tailoring
- **THEN** the displayed tailoring value and resulting work progress reflect the same changed bonus, while the explanation does not list ordinary labor effects twice

### Requirement: Owned bill and query features consume one result

Linked workbenches, bill management, recipe browsing and stat queries SHALL consume the same
effective recipe and pawn speeds as actual production. This feature SHALL NOT add a separate
production counter, worker policy, Thing inventory or independently refreshed stat cache.

#### Scenario: Linked benches and inspection agree
- **WHEN** the player runs a linked bill at two benches and inspects or queries the acting pawn's crafting speed
- **THEN** each job uses the pawn's same effective specialized speed plus that bench's ordinary effects, and bill progress/counting retains its existing shared rules
