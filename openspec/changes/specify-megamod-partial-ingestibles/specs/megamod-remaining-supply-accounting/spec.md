# Remaining supply accounting

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S085 FoodTracker replacement. Observable draft only; no implementation or compatibility acceptance.

## ADDED Requirements

### Requirement: Keep individual portions through stack operations
Compatible remainders SHALL stack and split without changing their individual remaining amounts or
meaningful condition. Whole and partial items cannot become interchangeable full-value units.
Physical stack limits still count physical items, not whole-item equivalents. A half and quarter
item together are two physical items containing 0.75 of a full item; they do not become a new whole
item. Transfers preserve exactly the portions selected and leave the balance at the source.
The existing finite-taint feature also requires equal-chance selection without replacement. Combined
handling of unequal remainders and that selection rule stays unavailable until FTR-Q4 specifies one
selection/quote policy that preserves both contracts. A quote cannot promise one portion and deliver
another, expose hidden contamination or change future allocation merely from viewing or a failed
transfer. This draft does not override the finite-taint rule.

#### Scenario: Merge and split unequal leftovers
- **WHEN** compatible half and quarter items are merged, then one is split off
- **THEN** the two resulting items retain the original half and quarter amounts and their combined 0.75 total; neither becomes an average or a full item.

#### Scenario: Stack capacity is reached
- **WHEN** a stack reaches its normal physical-item limit with small remainders
- **THEN** it accepts no extra physical items merely because their combined remaining quantity is small.

#### Scenario: Unequal portions also use finite-taint selection
- **WHEN** a half and quarter item share a stack governed by fair finite-taint selection and their joint selection/quote contract is unresolved
- **THEN** the combined partial-transfer path is unavailable with an explanation; it neither rerolls a quoted portion nor silently overrides the existing selection rule.

### Requirement: Mass and carrying limits use actual remaining contents
Each supported remainder SHALL contribute its remaining fraction of the applicable whole item's
mass to inspection, carrying, hauling, inventory and travel totals. A selected subset uses that
subset's actual contents; unrelated unselected stock contributes nothing. Existing pawn/animal
carrying constraints and personal-supply reservations remain enforced. No second hidden inventory
or separate hauling capacity is introduced. Ordinary unconsumed items retain their usual mass.

#### Scenario: Unequal selected portions
- **WHEN** a pawn takes a half item from a half-and-quarter stack of otherwise identical items
- **THEN** its carried mass increases by half the applicable whole-item mass and the quarter remains at the source, with no capacity allowance based on the average.

#### Scenario: Combine hauling and meal reserves
- **WHEN** an owned hauling job also carries a reserved partial meal
- **THEN** both count toward the same carrying limit once and reserving the meal does not create more physical cargo.

### Requirement: Remaining value matches actual trades
Remaining contents SHALL scale the applicable whole item's market-value contribution, while
ordinary condition effects, negotiation, currency and buy/sell rules remain applicable. Trade totals
and actual delivered portions must agree in both directions. Partial items cannot sell as whole
items, become free because a calculation failed or create profit by splitting/merging. No additional
retail or freshness discount is introduced beyond existing rules and remaining quantity.

#### Scenario: Sell and buy selected leftovers
- **WHEN** the player selects unequal partial items to sell or buy
- **THEN** the displayed total and completed trade use the value of exactly those delivered portions under the ordinary trade modifiers.

#### Scenario: Reorganize stock without consuming it
- **WHEN** the player repeatedly splits and merges the same compatible stock before trading
- **THEN** its total remaining mass, nutrition and market value do not increase or decrease solely from reorganization, apart from normal display rounding.

### Requirement: Travel forecasts and consumption use the selected supplies
Caravan formation, splitting, transporter/portal loading and travel SHALL count only the actual
selected or carried portions. Food-duration forecasts retain applicable native eater eligibility,
food policies, hunger, grazing, breastfeeding, forage and journey assumptions while replacing full
quantities with remaining usable nutrition. They remain estimates, not a survival guarantee.
Repeated viewing is read-only and cannot consume supplies, reroll food amounts or change the world.
Instant travel consumption of a remainder grants only its remaining nutrition and supported effects.

#### Scenario: Select one leftover for a caravan
- **WHEN** the player loads only a quarter meal while other leftovers stay on the map
- **THEN** supply totals and the food estimate count that quarter alone; actual travel eating cannot grant a full meal.

#### Scenario: Reopen and revise a transfer dialog
- **WHEN** selection quantities change or a different caravan/loading dialog is opened
- **THEN** displayed quantities follow the current selection, without another dialog's supplies or duplicate pawn inventory entries, and viewing does not change any actual food.

#### Scenario: Pawns have different diets or travel food sources
- **WHEN** a travel group contains incompatible diets, grazing animals or breastfed babies
- **THEN** the remaining food is considered under the applicable normal eligibility and travel assumptions, rather than being treated as universally edible.

### Requirement: Partial ingredients cannot create whole products or supplies
Recipe and supply consumers SHALL distinguish physical item counts from usable remaining amounts.
A nutrition-based recipe can use supported remaining nutrition and must consume the corresponding
amount. A count-based recipe that cannot account for fractions excludes remainders with an
explanation, rather than spending one tiny remainder as a whole ingredient. Personal stock,
automatic refilling and medical-dose targets count usable amounts when their target means whole
meals/doses; storage stack limits still use their specified physical-item thresholds. These paths
do not broaden ingredient, storage or medical permissions.

#### Scenario: Cook with remaining nutrition
- **WHEN** an eligible recipe needs one whole item's worth of nutrition and only half remains
- **THEN** that half provides half the requirement, and another permitted source is required before the normal product can be made.

#### Scenario: Count-only recipe or personal supply target
- **WHEN** a recipe demands one indivisible item, or a pawn's supply target is one full meal
- **THEN** an unsupported partial ingredient cannot satisfy the recipe, and half a meal does not satisfy the full-meal supply target by itself.

### Requirement: Show quantities in existing contextual surfaces
Inspection SHALL identify a partial item and show remaining portion, usable nutrition where
applicable, mass and value. A mixed stack distinguishes physical count, remaining total and
individual portions; rounding is presentation only. Inventory, trade and travel views show exact
selected totals or explain a varying per-item amount with accessible detail. They do not substitute
an unexplained 'varies' for the amount the player is about to transfer. Existing useful stat
explanations and modifiers remain visible. All added text is provided in English, German, Spanish,
French, Simplified Chinese, Russian and Japanese. Entity quantities are saved item state, not
global/per-save configuration; no new bottom tab or per-item settings row is added.

#### Scenario: Inspect and transfer a mixed stack
- **WHEN** the player inspects a half-and-quarter stack and selects a portion for transfer
- **THEN** the view distinguishes two physical items, their individual remaining amounts and combined total, and the transfer total describes the portion that will actually move.

#### Scenario: Localized displays and small positive values
- **WHEN** a baseline language is selected or a remainder is smaller than ordinary two-decimal display precision
- **THEN** labels and explanations remain readable and the UI indicates a positive remainder instead of implying it is empty or free.

### Requirement: Keep owned features and supported providers consistent
Owned consumers SHALL use the same current remaining quantities and item condition across search,
hauling, storage, personal supplies, poisoning, drug response, treatment and inventory/travel.
Eligibility is checked
again when the actual action occurs. Completing one action cannot consume a portion already spent
by another; cached display data cannot authorize stale stock. Unsupported external integrations are
isolated and explained without claiming universal drop-in compatibility. On loss of required
accounting support, existing partial items retain their state and affected actions are unavailable
until safe handling is restored; ordinary whole stock remains usable.

#### Scenario: A displayed meal is consumed before pickup
- **WHEN** search or hauling identified a partial meal that another pawn consumes first
- **THEN** a later pickup rechecks current availability and does not create the stale amount or a second copy.

#### Scenario: Transfer or accounting support is unavailable
- **WHEN** safe accounting for a required transfer path is unavailable
- **THEN** that path does not move or price existing partial stock as whole items, explains the limitation, and retains the stock for recovery.
