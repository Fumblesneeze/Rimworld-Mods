# Cooking and slaughter recovery

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`); target **1.7 beta**.
Origin **original**, S118. The user accepted neutral level10 and approximately60% extra recovery at
mastered skills. The selected curve gives160.5406% at all20 and100% at all10 without changing animal
base yields. Remaining tuning and actual playability still require verification; nothing is implemented.

## ADDED Requirements

### Requirement: SY01 Make Cooking contribute to slaughter speed
The feature SHALL multiply S117's slaughter-speed contribution by a Cooking factor, before the
ordinary final speed minimum. By default Cooking0,10,20 SHALL contribute75%,100%,125%, with linear
interpolation between them. This SHALL use the same handling-start snapshot and coordinated wait
as S117, without duplicating its Animals/Melee/work/capacity factors or changing job eligibility.
An otherwise eligible worker without skills SHALL receive a neutral Cooking contribution.

#### Scenario: Neutral and skilled cooking
- **WHEN** otherwise identical handlers have Cooking0,10 and20 under default settings
- **THEN** their Cooking contributions to slaughter speed are75%,100% and125% respectively
- **AND** Cooking10 preserves S117's existing speed contribution.

#### Scenario: One stable attempt
- **WHEN** Cooking, health or configuration changes after handling begins
- **THEN** the current attempt retains its captured duration and the next attempt uses current facts
- **AND** the animal's wait and handler's progress remain coordinated.

#### Scenario: Worker without a usable skill tracker
- **WHEN** a native-eligible worker has no skills
- **THEN** this addition contributes100% speed and100% yield rather than inventing zero-level skills
- **AND** no new work permission or extra training experience is granted.

### Requirement: SY02 Earn recovery only through deliberate slaughter
The feature SHALL record a recovery factor only for the actual corpse produced by a successful
ordinary designated or automatically selected animal slaughter. It SHALL capture the handler's
skills and effective tuning when handling starts, and commit the result only when that attempt's
native final cut causes the target's death and produces the matching corpse. Mental-state kills,
hunting, companion finishing, euthanasia, combat, unrelated deaths and pre-existing unrecorded
corpses SHALL receive no inferred recovery adjustment.

#### Scenario: Deliberate slaughter completes
- **WHEN** the captured eligible attempt's final cut kills its intended animal and leaves its corpse
- **THEN** that corpse retains the captured recovery factor
- **AND** no extra meat, leather or other product appears merely because slaughter ended.

#### Scenario: Cancellation or another cause of death
- **WHEN** handling is cancelled or the animal dies from another cause before the intended final cut
- **THEN** the unfinished attempt grants no recovery record or extra products
- **AND** a new handler or target cannot inherit its partial work or captured factor.

#### Scenario: Slaughterer mental state and other kills
- **WHEN** a mental-state slaughterer, hunter, bonded companion, combatant or euthanasia operation kills an animal
- **THEN** no S118 recovery factor is earned
- **AND** S117's separately defined timing scope, including Cooking on its native slaughter jobs,
  does not imply eligibility for a yield bonus.

#### Scenario: Legacy or externally obtained corpse
- **WHEN** the player loads an old corpse or obtains one with no valid owned recovery record
- **THEN** its recovery factor is100%
- **AND** an execution wound, name, species or supposed former handler is not used to invent history.

### Requirement: SY03 Derive bounded recovery from all three skills
The feature SHALL derive recovery from Animals, Melee and Cooking while keeping work speed,
Manipulation, Sight and body size out of this additional recovery factor. Those factors SHALL
retain their ordinary timing or native output effects. For this feature's skill calculations,
levels below0 or above20 SHALL use the respective endpoint. With neutral level `N`, Animals `A`,
Melee `M`, Cooking contribution `C`, yield influence `I` and configured bounds, the rule SHALL be:

- Animals contribution: `a(level) = 0.64 + 0.09 × level`.
- Melee contribution: `m(level) = 0.95 + 0.0125 × level`.
- Relative proficiency: `P = a(A) × m(M) × C / (a(N) × m(N))`.
- Recorded recovery: `Y = 1 + I × (P - 1)`, limited to the configured minimum/maximum.

By default `N=10`, `I=50%`, minimum recovery50%, maximum recovery200%. Cooking's factor is the
piecewise-linear curve in SY01, using the same configurable neutral level. These are recovery
multipliers, not changes to any animal's base meat/leather values.

#### Scenario: Neutral all-round proficiency
- **WHEN** Animals, Melee and Cooking are all10 with default tuning
- **THEN** the recorded recovery factor is100%.

#### Scenario: Low and high all-round proficiency
- **WHEN** all three skills are0 or all are20 with default tuning
- **THEN** recorded recovery is approximately63.7723% or160.5406% respectively
- **AND** higher recovery can produce more than the unchanged native base amount.

#### Scenario: Cooking varies while handling skills are neutral
- **WHEN** Animals/Melee stay10 and Cooking is0 or20 under default tuning
- **THEN** recovery is87.5% or112.5% respectively
- **AND** Cooking still affects handling speed through its separate75% or125% contribution.

#### Scenario: Bounds and zero influence
- **WHEN** chosen tuning would put recovery outside its configured bounds
- **THEN** the factor stops at that bound
- **AND** setting yield influence to0% produces100% recovery for future attempts while retaining
  the separately configured Cooking contribution to speed.

#### Scenario: Faster work is not extra recovery
- **WHEN** two handlers have the same three skills/tuning but different global work speed or capacities
- **THEN** their captured S118 recovery factor is identical
- **AND** native handling restrictions, speed differences and the eventual butcher's own efficiency remain effective.

### Requirement: SY04 Apply recovery to actual meat and leather processing only
The feature SHALL multiply the corpse's normal meat and leather quantities by its captured factor
before ordinary final whole-item rounding during admitted butchering. It SHALL preserve current
carcass damage, missing parts, malnutrition, difficulty, the butcher's efficiency and other admitted
native output modifiers. It SHALL leave base values, trophies, discrete body parts, organs,
arbitrary extra products, corpse ingestion and other processing unchanged by this factor.

#### Scenario: Different slaughterer and butcher
- **WHEN** a corpse prepared with160.5406% recovery is butchered by an ordinary100%-efficient butcher
- **THEN** its native meat/leather quantities receive that captured recovery multiplier
- **AND** choosing a different butcher changes only the ordinary butcher contribution, without
  recalculating the deceased animal's preparation from that butcher's skills.

#### Scenario: Native injury and difficulty reductions
- **WHEN** an earned corpse has lost parts or has applicable injury, malnutrition or difficulty effects
- **THEN** recovery multiplies the remaining native quantities after those rules
- **AND** it restores no missing part, removed meat, health or freshness.

#### Scenario: Fractional product quantity
- **WHEN** normal processing would yield2.4 units before whole-item rounding and the recorded factor is150%
- **THEN** normal probabilistic rounding is applied once to3.6, producing3 or4 units with the ordinary fractional rule
- **AND** the feature does not first round2.4 and then multiply that already-rounded count.

#### Scenario: Discrete or absent products
- **WHEN** an eligible animal also yields a horn, discrete body part or another extra product, or has no leather output
- **THEN** only its normal meat/leather channels are adjusted
- **AND** no extra horn, organ, substitute leather or new product type is invented.

#### Scenario: Mixed processing inputs
- **WHEN** an admitted processing operation handles several corpses with different or absent recovery records
- **THEN** each corpse's own meat/leather quantities use only its own factor
- **AND** one prepared corpse does not multiply another ingredient or the whole recipe's unrelated outputs.

### Requirement: SY05 Preserve physical production and read-only inspection
The feature SHALL produce adjusted quantities through the ordinary recipe completion and ingredient
consumption workflow, without separate bonus drops, additional recipe iterations or duplicated
completion effects. Inspection and previews SHALL NOT consume a recovery record, generate physical
products, alter body parts or reroll an earned factor. Normal bills, storage, stack splitting and
carrying limits SHALL remain authoritative for the actual produced quantities.

#### Scenario: Complete a butcher bill
- **WHEN** a valid butcher bill finishes processing a recorded corpse
- **THEN** the corpse is consumed once and its actual adjusted products enter ordinary placement,
  bill accounting, storage and hauling
- **AND** no second bonus batch, iteration or slaughter experience is awarded.

#### Scenario: Inspect repeatedly then butcher
- **WHEN** the player repeatedly opens the corpse information or a recipe preview before processing
- **THEN** its captured factor and body remain unchanged and no product appears
- **AND** subsequent real processing still uses that factor exactly once in each applicable quantity calculation.

#### Scenario: Large product output
- **WHEN** adjusted output exceeds one stack or one hauler's capacity
- **THEN** ordinary physical splitting, placement and capacity restrictions apply
- **AND** no extra oversized storage stack or separate unlimited hauling inventory is introduced.

#### Scenario: Interrupted or unsupported processing
- **WHEN** processing stops before ordinary completion, or an alternate processor cannot establish the required quantity contract
- **THEN** this feature creates no standalone compensation products or replayed iteration
- **AND** unsupported processing retains its ordinary result with a bounded explanation of the unavailable adjustment.

### Requirement: SY06 Keep recovery attached to the earned corpse across its lifecycle
The feature SHALL preserve a valid captured factor through save/reload and ordinary corpse hauling,
storage, caravan/container transfer and map changes. Later configuration, skill changes, handler
death or departure SHALL not recalculate it. Consuming, destroying or replacing the corpse SHALL
not transfer its recovery to a different body; resurrection SHALL end that corpse's entitlement.
Invalid or unsupported saved recovery data SHALL use neutral native output without guessing.

#### Scenario: Store transfer and reload
- **WHEN** a recorded corpse is hauled, moved between containers/maps or carried in a caravan, then saved and reloaded
- **THEN** its original factor and preparation explanation remain associated with that same corpse
- **AND** another corpse, map or save cannot inherit the result.

#### Scenario: Later settings or handler change
- **WHEN** the original handler gains skill, dies or leaves, or the player lowers the current maximum recovery
- **THEN** an already-earned valid factor remains unchanged, even if above the new maximum
- **AND** the new tuning applies to future handling-start snapshots.

#### Scenario: Interrupted attempt across reload
- **WHEN** a valid in-progress deliberate slaughter is saved after its handling snapshot and reloaded
- **THEN** its captured tuning/proficiency and S117 remaining work are retained
- **AND** no recovery is committed until that same attempt produces its matching corpse.

#### Scenario: Resurrection or destruction
- **WHEN** a recorded corpse is destroyed, consumed or used to resurrect its pawn
- **THEN** its factor cannot benefit another corpse
- **AND** a later death is neutral unless a new qualifying deliberate slaughter earns a new record.

#### Scenario: Damaged record or unavailable feature
- **WHEN** saved recovery data is malformed/unsupported or the required owned processing cannot be admitted
- **THEN** ordinary native quantities are used and the unavailable adjustment is explained without repeated spam
- **AND** a valid dormant record is preserved while the corpse exists; restoring support can use it,
  but processing the corpse while unsupported does not promise a later compensating reward.

### Requirement: SY07 Provide shared inheritable tuning with explicit effect timing
The feature SHALL expose the following ordered controls in the shared mod-settings slaughter
section, each with editable Global default, Current save and Use global controls. New/missing save
entries SHALL inherit current globals; per-save controls SHALL be inactive while inheriting and
both per-save editing and Use global SHALL be inactive at the main menu. All setting changes SHALL
apply to future handling-start snapshots, not active attempts or earned corpses.

| Setting | Built-in default | Allowed values |
| --- | --- | --- |
| Neutral skill level for Cooking and yield comparison |10|Whole levels1–19|
| Cooking contribution at level0 |75%|10–100%|
| Cooking contribution at level20 |125%|100–200%|
| Yield influence |50%|0–100%|
| Minimum recovery |50%|10–100%|
| Maximum recovery |200%|100–300%|

Cooking SHALL interpolate between the configured level0 endpoint,100% at the neutral level and
the configured level20 endpoint. Bounds deliberately keep a positive Cooking contribution and
allow neutral recovery. Invalid edits SHALL retain the previous valid value; absent/invalid saved
values SHALL follow the common validated inheritance/default path with a bounded diagnostic for corruption.

#### Scenario: Inherit and override independently
- **WHEN** one save inherits tuning and another overrides it, then a global default changes
- **THEN** future attempts in the inheriting save use the new global and the overridden save retains its own choice
- **AND** changing one save does not write another save or the global value.

#### Scenario: Toggle Use global
- **WHEN** Use global is checked for an existing override and later unchecked
- **THEN** the dormant override is restored
- **AND** a first override starts from the current effective global value instead of an unrelated default.

#### Scenario: Main menu and loaded world view
- **WHEN** settings are opened at the main menu
- **THEN** globals remain editable, current-save actions are visibly and functionally disabled with a reason,
  and no previous save is presented as active
- **AND** in a loaded game's world view the same rows use that current save without requiring a selected map.

#### Scenario: Edit a curve or bound
- **WHEN** the player changes a valid endpoint, neutral level, influence or recovery bound
- **THEN** the explanation/preview reflects the effective curve for future attempts
- **AND** non-finite, out-of-range or fractional neutral-level edits are rejected without partial invalid tuning.

### Requirement: SY08 Explain preparation in existing contextual surfaces
The feature SHALL show the current predicted slaughter recovery and Cooking speed contribution in
ordinary pawn information, and the captured factor on a recorded corpse with its scope clearly
explained. Final output estimates SHALL identify remaining dependence on carcass condition and the
butcher; unknown quantities SHALL not be shown as fixed promises. Owned UI SHALL support English,
German, Spanish, French, Simplified Chinese, Russian and Japanese without a new bottom tab or cheat control.

#### Scenario: Inspect current proficiency and earned result
- **WHEN** the player inspects a handler and then one of its prepared corpses
- **THEN** the handler shows current predicted factors while the corpse shows its earned factor
- **AND** the corpse explanation identifies meat/leather scope and dependence on later butchering.

#### Scenario: Variable recipe preview
- **WHEN** the player inspects a butcher recipe without a determined corpse and butcher
- **THEN** the view labels the result input-dependent and keeps normal bill creation available
- **AND** any estimate with determined inputs includes the known factor without changing the native base values.

#### Scenario: Translated and unavailable information
- **WHEN** a baseline language is selected or processing support is unavailable
- **THEN** labels, settings, explanations and unavailable-state text remain readable in that language
- **AND** an inactive adjustment is not presented as a guaranteed current benefit.

### Requirement: SY09 Admit provenance processing and persistence as a coherent feature
The feature SHALL require admitted owned S117 timing and documented minimal contracts for its
capture, corpse persistence, quantity transformation and presentation. Original-provider selection
or unknown required semantics SHALL disable the affected S118 behavior rather than guess provenance
or multiply a foreign pipeline. Compatible changes SHALL be allowed. Failure SHALL preserve native
outcomes, retain valid dormant corpse records where possible, and issue one bounded startup warning
per skipped enabled patch with identity, failed condition, unavailable behavior and fallback.

#### Scenario: Original or unsupported provider
- **WHEN** S117 selects the original provider or a required slaughter/processing contract cannot be established
- **THEN** S118 adds neither its Cooking speed contribution nor newly earned recovery
- **AND** native/provider jobs remain available without an extra owned timing or product adjustment.

#### Scenario: Compatible product and timing modifications
- **WHEN** other patches preserve the required deliberate-death and native meat/leather quantity relationships
- **THEN** the owned feature can remain active and preserve their compatible modifiers
- **AND** admission does not require an exact game hash or absence of other patch owners.

#### Scenario: Guard exception or partial installation
- **WHEN** a required condition is false/unknown, checking throws or installation only partly succeeds
- **THEN** the affected owned group remains inactive and only newly installed owned patches are removed
- **AND** each skipped enabled patch has a bounded startup warning naming package, patch/spec,
  target, reason, unavailable behavior and native/provider fallback.

#### Scenario: Later invalidation or repeated initialization
- **WHEN** supported later changes invalidate a contract or initialization repeats
- **THEN** no duplicate Cooking factors, records, product multipliers or warnings accumulate
- **AND** foreign patches remain untouched; owned behavior resumes only after successful revalidation,
  with no replay of an earlier kill, consumed corpse or product delivery.
