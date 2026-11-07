# Quality-dependent apparel durability

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement S106, Quality Based Durability. This is a specification for later implementation.

## ADDED Requirements

### Requirement: Quality changes apparel damage loss
The mod SHALL multiply incoming positive damage to eligible quality-bearing clothing and armor by the current quality's configured loss multiplier once, before ordinary item-specific damage handling and rounding.

Eligible items have ordinary hit-point durability; worn and loose apparel from every faction and
supported content source qualify. Weapons, buildings, pawns and qualityless apparel do not qualify.
The built-in defaults are Awful150%, Poor125%, Normal100%, Good85%, Excellent70%, Masterwork50%,
Legendary25%. These percentages describe damage loss, so lower values mean better durability.

#### Scenario: Default quality comparison
- **WHEN** otherwise equivalent eligible items at each quality receive20 points of incoming damage
  with no further modifiers or absorption and enough remaining health
- **THEN** their condition losses are respectively30,25,20,17,14,10 and5 hit points
- **AND** the feature does not change their maximum health or quality

#### Scenario: Worn and loose items share the rule
- **WHEN** an eligible coat is damaged while worn, carried, on the ground or worn by a corpse
- **THEN** the same current-quality multiplier applies if the ordinary damage event reaches that coat
- **AND** faction and ownership do not create an exemption or an additional damage event

#### Scenario: Unsupported item categories remain ordinary
- **WHEN** damage reaches a weapon, building, pawn, qualityless garment or item without hit-point durability
- **THEN** this feature leaves that item's damage handling unchanged
- **AND** an unknown quality leaves its damage unchanged rather than assigning a guessed quality tier

### Requirement: Include the actual damage and wear paths
The mod SHALL apply the quality rule to ordinary apparel damage, including combat, fire, daily wearing, environmental deterioration and clothing damage caused by the wearer's violent death, while preserving when those events occur.

This is not a combat-only feature. It adds no new deterioration opportunities, wear timers or damage
on loading. It does not intercept repairs, direct condition assignments, consumption, recycling or
other removal which does not deal ordinary damage to the apparel.

#### Scenario: Daily and outdoor wear
- **WHEN** an eligible Legendary garment receives an ordinary1-point daily-wear or outdoor-deterioration event
- **THEN** its incoming loss becomes0.25 before ordinary rounding
- **AND** the game may lose zero or one hit point for that event using its ordinary fractional handling
- **AND** neither the opportunity frequency nor the underlying wear/deterioration rate is increased

#### Scenario: Clothing damage on death
- **WHEN** a wearer's violent death causes the game to damage an eligible garment
- **THEN** the quality multiplier applies once to the amount the game chose for that garment
- **AND** taint, corpse creation, notifications and any other ordinary death effects remain ordinary

#### Scenario: Repair and removal retain their own rules
- **WHEN** a repair restores condition or a recipe consumes or removes an item without dealing ordinary damage
- **THEN** this feature neither scales the restoration nor prevents that consumption or removal
- **AND** it does not grant a repair action or alter work, material or research requirements

#### Scenario: Item destruction by repeated attacks
- **WHEN** a pawn deliberately destroys an eligible apparel item through repeated melee damage
- **THEN** each actual damage event uses the same quality multiplier as other damage
- **AND** no second durability multiplier is added by the destruction command

#### Scenario: No-loss apparel cannot trap a destruction worker
- **WHEN** on-site melee destruction of0%-loss apparel completes ten attacks without a new lower
  target health or smaller marked quantity
- **THEN** the existing destruction safety rule stops and suspends that attempt with an ineffective-attacks reason
- **AND** unchanged automatic work scans, interruptions or reload do not restart or refresh its allowance
- **AND** only explicit Retry or a genuine change in attack capability or target susceptibility permits another attempt;
  the feature never substitutes forced damage or direct deletion

### Requirement: Preserve ordinary protection and numerical consequences
The mod SHALL preserve the ordinary downstream damage rules, absorption, destruction, armor protection and fractional handling after applying its single apparel-loss contribution.

It does not directly change armor rating, coverage, penetration, wearer injury or maximum health.
Longer-lasting armor can naturally remain available to protect its wearer for longer. It imposes
no minimum one-point loss, artificial remaining-health floor or separate saved damage remainder.
Zero incoming damage stays zero. A configured0% prevents loss through these damage events, not
every way an item can disappear. A100% setting contributes no change.

#### Scenario: Fractional losses remain probabilistic
- **WHEN** repeated eligible1-point events use a25% multiplier without other changes
- **THEN** individual events can cause zero or one hit point of loss under ordinary rounding
- **AND** the feature neither guarantees one point each time nor promises one loss exactly every fourth event

#### Scenario: Other damage rules retain their place
- **WHEN** a10-point event reaches eligible Masterwork apparel and an ordinary subsequent item rule doubles damage
- **THEN** the quality contribution first makes the incoming amount5 and that later rule can make it10
- **AND** ordinary absorption can still prevent loss and lethal resulting damage can still destroy the item

#### Scenario: No separate protection bonus
- **WHEN** a wearer is attacked while using eligible armor
- **THEN** this feature changes only the damage sent to the armor item
- **AND** the normal protection calculation still decides the wearer's injury using the ordinary armor rules

#### Scenario: Zero and neutral settings
- **WHEN** eligible apparel has a configured0% loss multiplier
- **THEN** ordinary incoming damage produces no condition loss through this feature's supported path
- **AND** direct consumption or removal remains possible
- **WHEN** the multiplier is100% or the incoming amount is zero
- **THEN** this feature leaves that incoming amount unchanged

#### Scenario: Invalid event arithmetic falls back safely
- **WHEN** a damage amount or its scaled result cannot be handled as a valid finite ordinary damage amount
- **THEN** the feature leaves that event to ordinary handling without contributing a replacement amount
- **AND** records one bounded diagnostic for the failing rule without repeating it on every hit

### Requirement: Explain the current item's loss rate
The mod SHALL add one clearly labeled quality damage-loss entry to eligible individual apparel information while the feature is active, preserving the item's existing information.

The entry shows the current quality's effective percentage, explains that lower means less loss,
names combat and ordinary wear/deterioration scope, and distinguishes damage loss from maximum
health and wearer protection. It is a multiplier before ordinary damage handling, not an exact
lifetime forecast. Abstract definitions and ineligible items receive no guessed item-specific rate.

#### Scenario: Inspect a current garment
- **WHEN** the player opens information for an eligible Excellent coat with the built-in settings
- **THEN** the additional entry shows70% damage loss and explains its scope
- **AND** the existing health, protection, material and other entries remain available

#### Scenario: Current state replaces stale information
- **WHEN** the item's quality or effective setting changes and the player next inspects it
- **THEN** the entry reflects the current quality and current save's effective multiplier
- **AND** inspecting another item or switching saves does not reuse the previous item's rate

#### Scenario: No misleading entry
- **WHEN** the player inspects an abstract recipe/product definition, a qualityless item or an item
  without hit-point durability, or the owned feature is unavailable
- **THEN** no owned entry claims a quality-loss multiplier is active for that item

### Requirement: Use common inherited configuration
The mod SHALL expose the seven quality multipliers in ordered mod settings under Equipment / Apparel durability, each using the common Global default, Current save and Use global controls.

Each quality accepts finite0%–300%, initially using its listed built-in default; it need not remain
monotonic if the player deliberately changes the tiers. Changes affect subsequent damage and
inspection, not previous condition loss. Each reset states its scope. Global reset restores the
seven built-in defaults; current-save reset restores inheritance and clears that section's saved
overrides. These controls add no bottom tab or bottom-right toggle.

#### Scenario: Inherited and overridden values
- **WHEN** the player changes a quality's global default
- **THEN** inheriting saves use the new value, while saves overriding that quality keep their own value
- **AND** each save and each quality's choice remain independent through reload and save switching

#### Scenario: Toggle inheritance without losing a dormant choice
- **WHEN** the player enables Use global for a quality with a saved override
- **THEN** the save editor becomes inactive and its dormant override is retained
- **WHEN** the player disables Use global again
- **THEN** the saved override is restored, or a first override is seeded from the current global value

#### Scenario: Main-menu scope and reset
- **WHEN** settings are opened without a current save
- **THEN** global editing and global reset remain usable
- **AND** save editors, Use global and current-save reset are visibly inactive and cannot mutate any save
- **WHEN** current-save reset is used with a save open
- **THEN** only that save's apparel-durability section returns to inheritance

#### Scenario: Invalid configuration is isolated
- **WHEN** an interactive edit is outside0%–300% or is not finite
- **THEN** it is rejected with an explanation and the previous valid value remains
- **WHEN** a stored global value or an explicitly selected save-override value is invalid or missing
- **THEN** only that entry falls back to its built-in quality default; other valid entries remain intact
- **AND** a save with no override continues to inherit its current global default

#### Scenario: No retroactive healing or implicit import
- **WHEN** settings change, the feature is enabled or removed, a save loads, or an item's quality is reworked
- **THEN** this feature does not rescale, heal or re-damage its existing condition or maximum health
- **AND** future events use the current applicable multiplier
- **AND** it does not silently import or rewrite the original mod's settings

### Requirement: Compose with providers and fail visibly
The mod SHALL enable the owned damage and explanatory behavior only when their documented compatibility conditions hold, preserving compatible third-party changes and ordinary fallback behavior.

The original Quality Based Durability provider takes ownership of the overlap when active; the
owned damage and item entry then remain inactive. No other mod is removed or unpatched. Unknown
or incompatible damage semantics leave the affected owned behavior inactive. Each skipped enabled
patch produces one bounded startup warning naming its failed condition, unavailable behavior and
fallback. Explicitly disabled features and absent unrequested optional adapters are not failures.

#### Scenario: Original provider is active
- **WHEN** the game starts with the original provider enabled
- **THEN** only that provider contributes its durability behavior and item entry
- **AND** the owned overlapping patches are skipped with their bounded warnings and settings explain the ownership
- **AND** the original provider's settings and patches are left untouched

#### Scenario: Compatible changes and unsupported replacements
- **WHEN** other mods make compatible changes that preserve the required damage and information behavior
- **THEN** those changes alone do not prevent activation
- **WHEN** a required condition is false, unknown or cannot be validated
- **THEN** the owned feature remains inactive, its warning identifies ordinary fallback, and no misleading owned rate is shown

#### Scenario: No half-active feature
- **WHEN** activation fails after only part of the owned feature was installed, or supported later changes invalidate its conditions
- **THEN** the owned contribution and its active-rate entry are withdrawn together
- **AND** unrelated behavior and foreign patches remain intact without repeated damage or condition changes

#### Scenario: Modded apparel uses admitted ordinary behavior
- **WHEN** supported modded apparel has real quality and hit-point durability and follows the admitted damage path
- **THEN** it receives the same quality-loss rule without requiring a hardcoded item list
- **AND** a custom damage path that has not been shown to support the rule is not claimed compatible

### Requirement: Keep presentation clear and work bounded
The mod SHALL provide the settings, item explanation, inactive reasons and validation feedback in English, German, Spanish, French, Chinese Simplified, Russian and Japanese, using the existing settings and item-information surfaces.

The feature adds no new research, recipes, equipment or resource costs, no periodic item scan and
no delay to ordinary work for rebuilding a private item list. Quality and settings are read when
needed; stale inspection must not become stale gameplay.

#### Scenario: Localized and contextual presentation
- **WHEN** the player inspects eligible apparel or edits settings in any baseline language
- **THEN** quality labels, percentages, scope, inheritance, reset and inactive explanations are readable
- **AND** no raw translation keys, extra bottom tab or debug-only configuration surface appears

#### Scenario: Loading and transfers preserve condition
- **WHEN** apparel is dropped, equipped, transferred between maps or carried through a save/reload
- **THEN** this feature adds no damage, healing, new item record or repeated initialization multiplier
- **AND** the next ordinary damage event uses its then-current quality and effective save settings
