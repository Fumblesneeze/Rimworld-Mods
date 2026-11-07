# Malnutrition consciousness ceilings

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: S045 Consciously Malnourished replacement. Draft only; no implementation or acceptance.

## ADDED Requirements

### Requirement: Early malnutrition limits maximum consciousness
Ordinary trivial, minor, moderate and severe Malnutrition SHALL impose Consciousness ceilings of
95%, 90%, 80% and 70% respectively instead of their flat consciousness deductions. Their ordinary
severity intervals SHALL remain 0–below20%, 20–below40%, 40–below60% and 60–below80%. A stronger
supported existing ceiling SHALL remain effective. These ceilings SHALL apply to all supported
pawns with that condition, regardless of faction or species, without creating a food need or
condition for a pawn that does not otherwise have one.

#### Scenario: Otherwise healthy pawn progresses through stages
- **WHEN** an otherwise 100%-conscious pawn naturally reaches trivial, minor, moderate and severe malnutrition
- **THEN** its consciousness is limited to 95%, 90%, 80% and 70% at the respective stage boundaries
- **AND** no additional flat malnutrition consciousness deduction is applied

#### Scenario: Non-colonist or animal
- **WHEN** a supported prisoner, visitor, enemy or animal suffers the same ordinary condition
- **THEN** the same stage ceiling applies without changing ownership, food eligibility or other health rules

#### Scenario: Stronger existing ceiling
- **WHEN** another supported health rule already limits consciousness to 50% during minor malnutrition
- **THEN** the 90% malnutrition ceiling does not raise or loosen that 50% limit

### Requirement: A ceiling neither subtracts from low capacity nor restores it
The feature SHALL remove only the specified flat malnutrition deduction; it SHALL not raise low
consciousness to the ceiling, heal injuries or override native wakefulness/downing rules. Other
capacity effects SHALL continue to operate. An otherwise boosted pawn SHALL still face the absolute malnutrition ceiling,
even when a flat deduction would have left it above 100%.

#### Scenario: Impaired pawn at 60 percent
- **WHEN** a pawn whose other current effects produce 60% consciousness gains trivial malnutrition with no additional capacity factors
- **THEN** it remains at 60% rather than falling to 55% or being raised to 95%

#### Scenario: Very low consciousness
- **WHEN** other unchanged effects leave 4% consciousness and the pawn gains trivial malnutrition
- **THEN** this malnutrition stage does not subtract the additional five percentage points that would reduce it to zero
- **AND** ordinary unconsciousness and any other lethal conditions remain effective

#### Scenario: Native wakefulness threshold
- **WHEN** other unchanged effects leave 34% consciousness and an otherwise wake-capable pawn gains trivial malnutrition
- **THEN** it remains at 34% rather than falling to 29%, staying above the native 30% wakefulness threshold
- **AND** native deactivation, pain shock, movement and other downing rules still apply

#### Scenario: Boosted pawn
- **WHEN** other effects would provide 120% consciousness and the pawn gains trivial malnutrition
- **THEN** its consciousness is capped at 95%, not left at 115%

### Requirement: Preserve extreme starvation and ordinary recovery
The feature SHALL leave extreme malnutrition and all unrelated hunger/health effects unchanged.
In ordinary Core behavior extreme begins at 80% severity, caps consciousness at 10% and remains
life-threatening; lethal severity remains 100%, subject to native death-prevention rules. Food
consumption, hunger-rate changes, severity progression/recovery, mood, social effects and other
causes of downing/death SHALL retain ordinary supported behavior. Feeding SHALL not instantly cure
malnutrition or restore unrelated lost capacities.

#### Scenario: Extreme and lethal starvation
- **WHEN** ordinary starvation reaches extreme and continues toward lethal severity
- **THEN** the existing 10% extreme ceiling and ordinary lethal-severity/death-prevention behavior remain
- **AND** the earlier-stage change grants no immunity to starvation

#### Scenario: Feeding recovery
- **WHEN** the player feeds an affected pawn through the ordinary eating or patient-feeding workflow
- **THEN** food and severity recover by ordinary rules and each remaining stage uses its applicable ceiling
- **AND** unrelated wounds and health effects are neither removed nor healed by this feature

#### Scenario: Unrelated condition effects
- **WHEN** a supported provider adds a separate capacity or other effect to an ordinary malnutrition stage
- **THEN** that effect remains while only the intended flat consciousness deduction is replaced

### Requirement: Preserve native state and presentation
Existing condition identity, severity and pawn health SHALL persist across save/load. The ordinary
Health and capacity inspection surfaces SHALL show the actual ceiling and resulting consciousness;
no new tab, toggle, individual override or configuration setting SHALL be introduced. Names and
descriptions SHALL use the ordinary localized game surfaces. Loading SHALL not reset malnutrition,
resurrect a pawn or create a second condition.

#### Scenario: Reload an affected pawn
- **WHEN** a save with minor malnutrition is opened under the supported feature
- **THEN** the same condition/severity remains and ordinary inspection reflects the applicable 90% ceiling
- **AND** no duplicate condition, reset severity or healing occurs

#### Scenario: Inspect actual effect
- **WHEN** the player opens the affected pawn's health/capacity details
- **THEN** the display agrees with the actual maximum and current consciousness
- **AND** the player does not need a separate mod window to see that information

### Requirement: Unsupported health models remain unchanged
The owned four-stage conversion SHALL be unavailable together if the required ordinary malnutrition
stages or consciousness behavior are unavailable or ambiguous, leaving the prior health model intact.
Compatible unrelated changes SHALL remain supported and preserved. Each skipped enabled patch SHALL
have one bounded startup warning identifying the failed compatibility check, unavailable effect and
fallback. Active original Consciously Malnourished SHALL suppress the overlapping owned feature
without changing the original provider. Unrelated megamod features SHALL remain usable.

#### Scenario: Alternative malnutrition model
- **WHEN** a provider replaces the ordinary stages with a different progression that has no supported integration
- **THEN** none of the four owned ceilings is partially imposed and the provider's capacities/stages remain unchanged
- **AND** startup warnings explain why the owned behavior is unavailable

#### Scenario: Compatible extra data or already matching limits
- **WHEN** another mod adds unrelated condition information or already supplies the same supported ceilings
- **THEN** unrelated information is retained and the effect is neither duplicated nor rejected merely because another mod contributed

#### Scenario: Original source is active
- **WHEN** the original Consciously Malnourished mod is active
- **THEN** the owned overlap remains inactive with an availability diagnostic
- **AND** its source behavior and unrelated features are left untouched
