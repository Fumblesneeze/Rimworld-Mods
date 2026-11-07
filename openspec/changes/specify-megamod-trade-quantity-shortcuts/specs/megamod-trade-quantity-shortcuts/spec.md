# Trade quantity shortcuts

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S095, replacement-derived. This document specifies observable behavior; nothing is implemented.
Numbers in worked trade examples are illustrative inputs, not changes to item prices or balance.

## ADDED Requirements

### Requirement: Contextual trade suggestions preserve ordinary control

The feature SHALL offer useful quantity shortcuts within supported interactive silver trade rows.
It SHALL change only the chosen row's pending quantity and ordinary derived totals. It MUST NOT
execute a trade, change other item selections, grant stock/currency or alter prices. Ordinary typing,
all/zero actions when no useful shortcut applies, Reset, Cancel and final acceptance SHALL remain
available with their ordinary effects. Native-layout single steps SHALL remain unchanged; Trade UI
Revised follows the explicit left-click exception below. It SHALL evaluate both directions independently
where both offer useful choices. Favor and unrelated transfer windows SHALL retain their controls.

#### Scenario: Suggestion updates the pending basket
- **WHEN** the player clicks an affordable-quantity shortcut for a trade row
- **THEN** that row's pending count and the dialog's corresponding totals update
- **AND** no physical item, silver or goodwill transfers until ordinary final acceptance.

#### Scenario: Ordinary interaction remains ordinary
- **WHEN** the player types a count, uses a native-layout single step, resets or cancels the trade
- **THEN** the ordinary action occurs without an unrelated calculated quantity taking its place
- **AND** cancellation transfers nothing.

#### Scenario: Unsupported currency or surface
- **WHEN** the player views a Favor trade, read-only/unwilling row, caravan-loading screen or other unrelated transfer
- **THEN** this feature leaves its controls unchanged and makes no silver-affordability promise.

### Requirement: Affordable quantities use the whole pending deal

For an already affordable deal, each offered direction SHALL select the farthest allowed quantity
change that leaves both parties able to pay the game's final silver amount, with other item rows
unchanged. It SHALL respect actual row stock/bounds, current prices, the zero-stop preference and
whole-deal rounding. If all permitted stock fits, the ordinary all action SHALL remain. A suggestion
MUST NOT lose an affordable unit or propose an unaffordable extra unit by rounding prices separately.

#### Scenario: Purchases account for other rows
- **WHEN** the colony has 10 silver, other selected rows cost a net 4, and a row offers more than two items at a buying price of 3
- **THEN** its maximum affordable purchase is two items, bringing the final cost to 10 rather than 13.

#### Scenario: Sales respect trader funds
- **WHEN** the trader has 10 silver, the rest of the basket is empty and the colony offers more than three items at a selling price of 3
- **THEN** its maximum affordable sale is three items, leaving the trader 1 silver.

#### Scenario: Whole-deal rounding permits a small purchase
- **WHEN** both parties have zero silver, the only row sells to the player at 0.2 per item, and the game's totals round 0.4 to zero and 0.6 to one
- **THEN** the affordable quantity is two items under those native rules, despite the zero silver balance.

#### Scenario: Full stock already fits
- **WHEN** the ordinary all action stays within both parties' funds and the permitted direction
- **THEN** the player retains that action rather than an unnecessary replacement promising a smaller amount.

### Requirement: Unaffordable deals get the smallest sufficient correction

If a party cannot afford the proposed deal, a correction shortcut SHALL make the smallest positive
change in the helpful direction on that row that makes both parties able to pay. It SHALL respect
stock and the zero-stop preference. A smaller change that still leaves a deficit MUST NOT be
presented as sufficient. If that row cannot repair the deal within those limits, no repair promise
SHALL be shown; manual changes and the game's ordinary warnings/confirmations remain available.

#### Scenario: Undo enough of an unaffordable purchase
- **WHEN** the colony has 10 silver, the trader has 20, and the pending purchase is four items priced at 4 with no other selections
- **THEN** the correction removes two pending purchases, leaving two items and a total cost of 8
- **AND** it does not stop after removing one and leave a cost of 12.

#### Scenario: Reduce an excessive sale
- **WHEN** the trader has 5 silver, the colony has 10, and the pending sale is five items priced at 2 with no other selections
- **THEN** the correction removes three from the pending sale, leaving a sale of two for 4
- **AND** it does not leave a sale of three that the trader still cannot afford.

#### Scenario: No sufficient correction in this row
- **WHEN** no permitted count on the chosen row makes the deal affordable to both parties
- **THEN** the row makes no successful-repair claim and the player can adjust other rows normally.

### Requirement: Undoing and crossing zero use their proper prices

Undoing a pending purchase SHALL remove its buying-price cost; undoing a pending sale SHALL remove
its selling-price proceeds. Continuing beyond zero SHALL use the new direction's price. By default,
shortcuts MUST NOT cross zero. A partial undo that stops before zero is allowed; when zero is the
appropriate endpoint the ordinary zero action SHALL remain. Enabling the cross-zero preference
SHALL permit a combined undo/new transaction, clearly explained before activation.

#### Scenario: Partial undo is valued as an undo
- **WHEN** the player reduces a pending purchase for an item whose buying and selling prices differ
- **THEN** the suggested reduction refunds the removed buying-price cost, not hypothetical resale proceeds.

#### Scenario: Default zero boundary
- **WHEN** undoing a pending selection could also allow trading in the opposite direction and cross-zero is disabled
- **THEN** the control stops at or before zero and does not silently start an opposite transaction.

#### Scenario: Explicitly permit crossing zero
- **WHEN** cross-zero is enabled and the player activates a suggestion that reverses direction
- **THEN** the tooltip distinguishes the undone amount from the new purchase/sale
- **AND** the resulting whole deal uses the correct price for each part of the change.

### Requirement: Gifts reach the next predicted goodwill milestone

In supported gift mode, a row SHALL offer the smallest additional positive gift quantity that makes
the whole basket's native predicted goodwill reach the next milestone above the current projection:
0, then 75, then 100 in the baseline game. Existing gifts, current relations, native rounding and
special gift valuation SHALL be included. The suggestion SHALL identify the target and quantity,
without claiming a guaranteed relation change where native faction rules prevent it. Silver can be
a gift row. At the maximum, with an ineligible faction or insufficient row stock, ordinary gifting
SHALL remain without a misleading target shortcut. Final gifting remains an explicit native action.

#### Scenario: Existing gifts count toward the target
- **WHEN** the current basket already projects goodwill above zero but below 75
- **THEN** an offered target shortcut adds only the smallest quantity from that row needed to reach 75
- **AND** the existing gifts remain selected.

#### Scenario: Returned prisoner has its native value
- **WHEN** a gift row represents a prisoner from the receiving faction
- **THEN** its target suggestion uses the same special valuation as the game's complete gift preview
- **AND** it does not request extra prisoners by treating them as ordinary-price goods.

#### Scenario: Exact threshold and integer steps
- **WHEN** the current projection is exactly 75 and an additional quantity can reach 100
- **THEN** the next target is 100 and the suggestion is the smallest count whose native preview reaches it
- **AND** a count whose preview falls short is not presented as reaching the target.

#### Scenario: No achievable milestone
- **WHEN** goodwill is already at maximum, the faction cannot gain the promised goodwill, or available stock cannot reach the next target
- **THEN** no target-reaching shortcut is advertised, while ordinary permitted gift controls remain.

### Requirement: Suggestions stay truthful as the trade changes

The feature SHALL refresh after changes to basket quantities, stock, prices, currency, mode, faction
or goodwill. A click on a stale offer SHALL refresh it without silently applying a different count.
Invalid/non-finite prices, unsupported valuation rules or unsafe quantities SHALL fall back to
ordinary controls without overflow, hangs or changes to pending rows. Free items SHALL retain the
ordinary quantity actions. Closing/reopening or switching trade/gift mode MUST NOT reuse stale offers.

#### Scenario: Offer changes before activation
- **WHEN** the shown suggestion becomes stale before the player activates it
- **THEN** the dialog refreshes the offer and applies no changed quantity until a fresh activation.

#### Scenario: Unusable valuation
- **WHEN** a row has a zero, invalid or unsupported price for the proposed calculation
- **THEN** the ordinary row remains usable without a misleading calculated offer or repeated errors.

#### Scenario: Session changes
- **WHEN** the player closes a trade, opens another or switches gift mode
- **THEN** suggestions refer only to the current session and mode, with no old row action carried over.

### Requirement: Configuration and explanation use the common interface

Two settings SHALL appear under Trading → Quantity shortcuts: allow crossing zero, default false;
force the $ symbol instead of the localized money symbol, default false. Both SHALL use common
Global default, Current save and Use global controls. New/missing saves inherit; inherited per-save
editors are inactive; main-menu save editors and Use global are inactive. Re-enabling an override
restores its dormant value or seeds a first override from the current effective value. Global edits
SHALL affect inheriting saves without replacing overrides; changes apply to subsequent offers.

Tooltips SHALL describe direction, quantity and whether an offer maximizes an affordable trade,
repairs a deficit, undoes a selection or targets goodwill. A combined undo/crossing SHALL be clear.
All labels, tooltips, scope controls and fallback explanations SHALL support English, German,
Spanish, French, ChineseSimplified, Russian and Japanese. No new bottom tab, view toggle or hidden
middle-mouse diagnostic gesture SHALL be added. Pending trade choices are contextual state.

#### Scenario: Inheritance and save switching
- **WHEN** the player changes a global preference, then switches between an inheriting save and an overridden save
- **THEN** the first uses the new global value and the second retains its own value after reload.

#### Scenario: Main menu cannot edit a save
- **WHEN** settings are opened without an active game
- **THEN** global controls work while Current save and Use global controls cannot mutate any save.

#### Scenario: Understand the suggested action
- **WHEN** the player hovers a shortcut in a supported language
- **THEN** its tooltip explains the pending change and target, with readable counts/money and no change to the underlying calculation.

### Requirement: Preserve optional trade surfaces and original ownership

An admitted Trade UI Revised interface SHALL offer equivalent left-click shortcuts in its correct
column while preserving its ordinary right-click full-quantity action and numeric entry. When a
shortcut is offered, it SHALL replace that provider's left-click single-step/multiplier action;
when no shortcut is offered, its ordinary left-click step SHALL remain. This is an explicit exception
to native-layout single-step preservation, matching the source integration. Missing or incompatible
optional surfaces SHALL retain their own controls without partial replacements. With original Max
Buy, Max Sell active, this owned feature SHALL defer with one bounded coexistence warning. Dynamic
Trade Interface's own window SHALL remain untouched; an independently supported native fallback
window can still use this feature. No external mod SHALL be removed, impersonated or unpatched.

#### Scenario: Supported two-column interface
- **WHEN** the player uses left-click then right-click on the corresponding Trade UI Revised quantity controls
- **THEN** left-click applies the shown affordable/correction/gift suggestion when available
- **AND** that offered action replaces the usual left-click step, while right-click retains the provider's full-quantity behavior and numeric entry remains available.

#### Scenario: Provider has no useful suggestion
- **WHEN** no quantity shortcut is offered on a Trade UI Revised row
- **THEN** its left-click retains the ordinary step/multiplier action, with right-click and numeric entry unchanged.

#### Scenario: Original source is also active
- **WHEN** original Max Buy, Max Sell is enabled alongside this feature
- **THEN** the original owns the behavior and a bounded warning explains why this owned feature is inactive.

#### Scenario: Different trade-window owner
- **WHEN** the player opens Dynamic Trade Interface's own window or an unsupported replacement surface
- **THEN** its controls remain intact and this feature does not add a second competing action
- **AND** a separately admitted native fallback window is not disabled solely by that package's presence.

### Requirement: Compatibility failures preserve native trading

Every patch SHALL apply only when its documented target requirements hold. Compatible unrelated
changes or other patches alone MUST NOT cause rejection. Failed checks, changed effective behavior,
ambiguous targets or installation failure SHALL leave the affected shortcut group inactive and its
fallback controls usable. Each skipped enabled patch SHALL produce one bounded startup warning
identifying the owner, patch/spec, target, failed condition, lost behavior and fallback. Repeated
initialization MUST NOT duplicate actions; supported later target changes SHALL trigger revalidation
or deactivation. Failure MUST NOT leave a new label bound to an old quantity action.

#### Scenario: Relevant target no longer matches
- **WHEN** a required quantity/cost/gift or provider behavior no longer satisfies its target requirements
- **THEN** the affected shortcut group stays inactive, ordinary trading remains usable and the startup warning explains the fallback.

#### Scenario: Compatible changes coexist
- **WHEN** another patch changes unrelated presentation while the required behavior still holds
- **THEN** its presence alone does not disable the feature or remove that patch.

#### Scenario: Partial installation or later invalidation
- **WHEN** installation fails midway, initialization repeats or a supported later change invalidates an active target
- **THEN** no duplicate or partially paired label/action remains and only the affected owned behavior is rolled back or deactivated.
