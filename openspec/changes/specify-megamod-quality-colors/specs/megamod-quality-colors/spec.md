# Quality presentation

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S131; specification only, no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: Quality colors supplement existing item information
The feature SHALL color the seven existing quality categories on supported native item displays without changing their wording, meaning, availability or actions.

Supported coverage is full/short quality labels, quality annotations in mouseover/inspect/item-info
labels, inspection quality-description text, equipment/apparel/inventory rows in the native Gear
tab, and neutral item-name text in native trade/caravan/transport transfer lists using that display.
Item condition percentages shown beside quality receive the same quality color; this is not a
condition gradient. Taint, forced/locked labels, counts, names and existing warnings remain present.
Unrelated warning/status coloring takes precedence over decorative quality coloring. Items without
quality receive no invented Normal quality/color; a minified item's actual contained quality counts.
Quality does not determine other statistics, durability, trade permissions, sorting or selection.

#### Scenario: One item across supported displays
- **WHEN** a player inspects a damaged excellent-quality weapon, opens its information card, views it in Gear and then opens an ordinary trade or transfer list
- **THEN** its quality is consistently excellent-colored, adjacent displayed condition uses that color, and its name, percentage, count, value and permitted actions retain their native meaning

#### Scenario: Status remains authoritative
- **WHEN** the player views a non-tradeable item or forced, locked or tainted apparel
- **THEN** the restriction/status remains legible and effective, whole-row restriction coloring is not replaced by a decorative quality tint, and no extra drop/equip/trade action becomes available

#### Scenario: Missing and contained quality
- **WHEN** the player views a resource without quality and a minified good-quality building
- **THEN** the resource remains without a quality annotation or imposed Normal tint, while the building uses its contained good quality

### Requirement: The full inspect title is an explicit single-item preference
The feature SHALL offer Color full inspect title, initially off, separately from ordinary quality annotations.
When on, the complete inspect title of one selected quality-bearing item receives its quality tint,
subject to the same status precedence. Multiple selection, no selection and non-quality objects
retain their ordinary title treatment. The tint does not leak into buttons, neighboring text or
later windows. This preference does not recolor every item's complete name on every other surface.

#### Scenario: Toggle while viewing an item
- **WHEN** the player enables this preference while one quality-bearing item is selected
- **THEN** its full inspect title changes on the next display update, and disabling it restores the ordinary title while retaining the colored quality annotation

#### Scenario: Selection and neighboring controls
- **WHEN** the player switches to several items, a pawn or an object without quality and opens another window
- **THEN** no previous single-item title tint remains on that selection or unrelated controls

### Requirement: Six reproducible palettes and seven customizable colors are available
The feature SHALL provide the following six palettes, with Default initially selected, and independent editing of all seven colors within the palette.

Values below are normalized red/green/blue channels in the range 0–1; all preset opacity is 1.
Named palettes are immutable starting points; editing a copy never changes a built-in palette.
The source's names identify color schemes, not dependencies or universal accessibility guarantees.

| Palette | Awful | Poor | Normal | Good | Excellent | Masterwork | Legendary |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Default | 1,0,0 | 159/256,103/256,0 | 1,1,1 | 0,1,0 | .451,.671,1 | 89/128,33/64,95/128 | 1,.922,.016 |
| Legacy | 1,0,0 | 1,0,0 | 1,1,1 | 0,1,1 | 0,1,0 | 0,1,0 | 1,.922,.016 |
| RPG Style Inventory | .5,.5,.5 | .5,.5,.5 | 1,1,1 | 0,1,0 | 1,0,1 | 1,.922,.016 | 1,.5,0 |
| WoW | .41,.41,.41 | .62,.62,.62 | 1,1,1 | .12,1,1 | 0,.44,.87 | .64,.21,.93 | 1,.5,0 |
| Colorblind | 221/256,51/64,119/256 | 17/32,17/128,85/256 | 1,1,1 | 17/32,51/64,119/128 | 51/256,17/128,17/32 | 17/256,119/256,51/256 | 221/256,51/64,119/256 |
| FFXIV | .41,.41,.41 | .62,.62,.62 | 1,1,1 | 1,.5898438,.8632813 | .2416077,.7929688,.3406804 | 15/128,.3999634,1 | .5544863,.2177124,123/128 |

The editor offers a color wheel, full-range lightness control, available named game color swatches,
current/new previews, RGB and opacity values 0–255, hue 0–360 with endpoints equivalent,
saturation/lightness 0–100%, and six/eight-digit hexadecimal color with optional #.
Six digits mean full opacity. Channel conversion rounds to the nearest displayable channel value;
editing one representation updates the others without repeated conversion drift. Opacity is
honored consistently on admitted quality-colored surfaces. Neutral labels and a neutral full-text
tooltip keep quality identifiable even for identical, low-contrast or transparent custom colors.

#### Scenario: Restore a preset after customization
- **WHEN** the player changes Poor, saves the palette and later selects Default again
- **THEN** all seven colors return to the listed Default values and the full-title preference is unchanged

#### Scenario: Consistent input and full lightness range
- **WHEN** the player selects a swatch or enters a valid RGB, HSL or hexadecimal color and adjusts lightness through 0%, 50% and 100%
- **THEN** all previews and input representations describe the same color, including black and white endpoints, in a language using either a dot or comma decimal separator

#### Scenario: Transparent and duplicate colors
- **WHEN** two qualities have identical colors or the player chooses zero opacity
- **THEN** the selected opacity is applied consistently and the quality remains identifiable through neutral editor labels and neutral full-text hover information

#### Scenario: Invalid input and cancelled editing
- **WHEN** the player enters incomplete, nonfinite, malformed or out-of-range input, or cancels/closes a color editor without saving
- **THEN** no invalid color is committed, invalid input is explained, the last valid preview is retained and cancellation leaves the committed palette unchanged

### Requirement: Palette settings use the common global and save model
The feature SHALL expose Quality colors under ordered Interface settings with the shared loading, storage and rendering behavior.
The seven-color palette is one collection setting so a preset is a complete coherent choice; it
has Use global, Current save and Global default controls. Color full inspect title is a separate
boolean row with those same controls. New/missing save entries use global. Inherited values follow
later global changes; explicit overrides affect only their save. Use global retains a dormant
override, restoring it when unchecked or seeding the first override from the current global value.
At the main menu, Use global and all save-value editing/preset/reset/save controls and callbacks
are inactive. In a loaded save, Use global remains clickable while checked; only the inherited
save-value editor and its preset/reset/save callbacks are disabled. Global controls remain usable
in both cases. No bottom tab or operational toggle is added.

Changing a preset or multiple colors operates on a preview for the explicitly selected scope;
Save commits the complete valid palette together, Cancel commits nothing. The previous value is
retained for recovery; a failed commit restores it and verifies restoration instead of leaving a
partial palette. A stale editor from another save/scope cannot commit. Missing/corrupt individual
loaded entries use their built-in Default color with a bounded diagnostic, preserving valid entries.
All owned controls, explanations and tooltips are translated in the seven baseline languages.

#### Scenario: Global and save isolation
- **WHEN** the player edits the global palette with one inherited save and another overridden save
- **THEN** the inherited save follows the change, the override does not, and saving/reloading or switching saves preserves each scope and its full-title preference

#### Scenario: Main-menu and dormant override controls
- **WHEN** the player opens settings without a game or checks Use global in a loaded save
- **THEN** no save can be edited from the main menu, and in the loaded save the inherited value editor is disabled but Use global remains clickable; global controls stay usable and unchecking Use global restores the saved dormant palette

#### Scenario: Whole-palette commit and stale editor
- **WHEN** a preset commit fails partway or a color editor remains open across a save/scope change
- **THEN** the previous complete palette is retained/restored and verified, no other save/global state is changed, and the failed or stale operation is explained

#### Scenario: Repair only damaged settings
- **WHEN** a saved palette contains a missing or invalid color alongside valid custom colors
- **THEN** only the damaged entry falls back to its built-in Default, valid customization remains intact and the repair is reported once

### Requirement: Formatting remains correct and current
The feature SHALL preserve visible text and actions through localization, shortening and quality/palette changes without requiring a game tick.
Colored spans remain well formed when shortened, keep distinct occurrences independent, and never
expose formatting markup or recolor unrelated text with the same word. Full neutral hover text
remains available on shortened supported labels. Language and palette/save changes update the
next display; a real item's quality/condition change updates its next display too. Empty/null
labels and unknown quality data fall back to the underlying display safely. Unrelated text rendering
and other mods' meaningful spans remain unchanged. No accumulating item references survive teardown.

#### Scenario: Localized repeated words and narrow labels
- **WHEN** a localized item name contains a quality word also used in its annotation and the row is too narrow for the full label
- **THEN** only the intended spans receive their proper colors, shortening remains readable without broken markup and hovering reveals the full neutral information

#### Scenario: Paused changes and real item updates
- **WHEN** the game is paused and the player saves a new palette, changes language/save, or views an item whose actual quality or condition has changed
- **THEN** the next display uses current values without waiting for ticks, stale colors or a restart

#### Scenario: Other content stays independent
- **WHEN** identical plain text appears in two differently colored contexts or an unrelated window draws after the item label
- **THEN** each keeps its own appearance, with no inherited tint, malformed text or changed click behavior

### Requirement: Owned presentation shares one quality meaning
The feature SHALL supply the same effective quality palette directly to owned item presentations that adopt quality colors, including S132's inventory presentation.
Other meanings such as condition bars, danger colors and cargo state remain distinct and keep
their owning contracts. Selecting the RPG Style Inventory palette does not require that external
mod. This feature adds no world items, resources, stats, jobs, carrying capacity or equipment rules.
Existing native trade, gear and item-info interactions remain the real controls.

#### Scenario: Shared owned surfaces
- **WHEN** the player changes the effective quality palette and opens the owned Gear presentation and a native trade/item-info display
- **THEN** the same quality uses the same palette while condition bars, warnings, cargo state and item actions retain their separate meaning

#### Scenario: Cosmetic changes do not grant actions
- **WHEN** the player colors a locked item, a non-tradeable item or an inventory stack
- **THEN** item availability, mass, capacity, quantity, value, sorting and permitted actions are unchanged

### Requirement: Compatibility failures are visible and isolated
Every enabled quality-display patch SHALL have a documented target spec checked at startup and operate only when its minimal required behavior is proven.
Compatible modifications do not disqualify a target merely because another mod patched it. Missing,
ambiguous or changed required behavior, checking errors and installation failures leave the affected
patch or inseparable group inactive, restore only its own partial changes, retain the underlying
display and emit one bounded startup warning identifying patch/spec, target, reason, unavailable
behavior and fallback. A later invalidation has the same safe fallback without repeated log spam.

With original DawnsGlow.qualcolor active, owned overlapping coloration is withheld with warning;
foreign patches/settings remain untouched. Provider absence is normal. No automatic import of
source settings, fake source identity or claim that package-bound external integrations still work
is made. Dynamic Trade Interface's source-specific color bridge and Nice Inventory Tab's source
palette-copy button are documented as not supplied by this change; their independent behavior
continues and any native color effects apply only where actually admitted. No new failure is induced
by manufacturing provider discovery. Unsupported external surfaces are not claimed as covered.

#### Scenario: Compatible and incompatible targets
- **WHEN** startup finds an unrelated compatible edit on one target and removal of required label semantics on another
- **THEN** the proven target can be colored, the incompatible target retains its underlying display with the specified warning, and neither foreign patch is removed

#### Scenario: Original provider and external consumer discovery
- **WHEN** the original provider is active, or only the megamod and a source-package-bound external consumer are active
- **THEN** the original retains overlapping authority in the first case, while the second case does not pretend that its old palette bridge was found; other supported display behavior remains independently available

#### Scenario: Partial admission failure
- **WHEN** installation of an inseparable coloring/shortening group fails after adding one owned hook
- **THEN** that group's newly added hooks are rolled back, the underlying readable display remains, and unrelated admitted features continue
