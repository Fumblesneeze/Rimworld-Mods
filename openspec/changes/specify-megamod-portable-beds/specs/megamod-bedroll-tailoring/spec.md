# Bedroll tailoring

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S073 observable draft only. Portable-bed common compatibility rules apply.

## ADDED Requirements

### Requirement: Tailoring benches can produce ordinary portable bedrolls
BT01. Both hand and electric tailoring benches SHALL offer bills for one ordinary single bedroll and one ordinary double bedroll when the corresponding content is available and admitted.
Use native fabric/leather material categories, including eligible added materials. The inspected
base quantities are40 material units for the single and85 for the double, matching construction.
Ordinary unit-volume material uses40/85 physical pieces; other admitted material volumes and applicable
bill rules retain their normal conversion, rather than always consuming40/85 pieces.
Proposed base tailoring work is600 and1100 respectively, deliberately correcting the source's
older350/600 work values. Use ordinary general labor speed, Crafting for quality and its normal
quality range, native bill ingredients, unfinished work, station power/access and work eligibility.
Add no recipe-specific minimum skill or research; bench availability remains ordinary progression.

The result is the actual minified native bedroll with its chosen material, quality and style, not
a second item type. Preserve ordinary mass (base2.5/4.5), sleeping capacity, comfort, rest/healing,
hit points, construction recipes, market-value rules and caravan usefulness. No new healing boost,
trader injection, resource refund or quality reroll on deployment is added. Applicable material and
other admitted modifiers remain in effect; no hardcoded material whitelist replaces categories.

#### Scenario: Craft at either tailoring bench
- **WHEN** an eligible worker completes a single or double bedroll bill using ordinary unit-volume material at a functioning hand or electric tailoring bench
- **THEN**40 or85 physical pieces satisfy the base ingredient requirement and one minified native bedroll is produced with600 or1100 base work, modified by the ordinary material/work rules and Crafting-derived quality.

#### Scenario: Interrupt and resume
- **WHEN** the bill is interrupted and saved with unfinished work
- **THEN** its actual resources and progress survive normal resumption without another output, ingredient duplication or free quality retry.

#### Scenario: Added material and native use
- **WHEN** an admitted material belongs to a supported fabric/leather category and a bedroll is crafted from it
- **THEN** ordinary material selection and product effects apply, and the resulting item can be installed, packed and used by caravans as its native type permits.

#### Scenario: Different material volume and work
- **WHEN** an admitted fabric/leather material contributes less than one ordinary material unit per piece or changes crafting work
- **THEN** the recipe consumes the native converted physical count for40/85 base units and applies the material's normal work effect, rather than fixing all materials to the same piece count and duration.

### Requirement: Content integration preserves existing production choices
BT02. Tailoring additions SHALL require the actual bedrolls, benches, materials and production support, preserving unrelated recipes, supported categories, work choices and other providers' content.
Each product's complete addition is atomic and idempotent. Merge compatible station membership
without duplicate bills. An already supplied equivalent recipe is used once, not added again; an
incompatible existing production definition is preserved and the conflicting owned addition is
skipped with a bounded startup warning. Do not overwrite that provider's work or ingredient rules
merely to force these defaults. A missing single/double product disables only its dependent addition;
unsupported required recipe infrastructure leaves affected native construction available.

Bench bills use S012's integrated Add bill browser, S014/S017–S019 bill linking/management/worker rules,
and S026 material selection where applicable, without an extra bottom tab or settings-only switch
for individual bills. Existing source provider authority remains as specified for portable beds.
No automatic conversion of old source bills or missing recipes is claimed.

#### Scenario: Another provider adds a station
- **WHEN** compatible production already includes another permitted workbench
- **THEN** that station and its unrelated recipes remain, and the tailoring choices appear once each.

#### Scenario: Incompatible or missing content
- **WHEN** a provider changes a bedroll's production contract incompatibly or removes the double bedroll
- **THEN** its existing content is preserved, affected owned additions are skipped with a bounded explanation, and an independently admitted single recipe can remain available.
