## Why

Choosing a trade quantity currently means adjusting counts until the whole deal fits the available
silver. S095 adds contextual shortcuts for the largest affordable quantity, the smallest correction
to an unaffordable deal, and the additional gift quantity needed for the next goodwill milestone.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S095/P2-24, Max Buy, Max Sell. **Specification only.**

## What Changes

- Calculate a row's suggested quantity against the complete pending deal and the game's final
  silver total, including rounding, stock limits and the different prices for buying and selling.
- Distinguish undoing a pending selection from trading in the opposite direction. Keep the normal
  zero stopping point by default; retain an option to allow a shortcut to continue through zero.
- Offer the smallest additional gift from a row that reaches the next projected goodwill milestone,
  including existing gifts and the game's special valuation of returned prisoners.
- Keep the controls within the trade dialog, with clear tooltips and two shared global/save settings.
  Preserve manual quantities, native acceptance/refusal, reset, cancellation and final confirmation.
- Support an admitted Trade UI Revised surface while retaining its right-click quantity action.
  Other trade windows retain their own controls. Do not duplicate an active original provider.
- Correct source rounding/valuation weaknesses and brittle UI targeting without changing prices,
  faction relations, trade eligibility or the underlying gift economy.

## Capabilities

### New Capabilities

- `megamod-trade-quantity-shortcuts`: affordable quantity suggestions, minimal deal corrections,
  goodwill-target gifts, contextual controls, configuration and compatibility fallback.

### Modified Capabilities

None. Shared settings, localization and compatibility admission follow the foundation contract.

## Impact

Future trade-dialog adapters and deterministic quantity logic. Core and external Harmony are
required on implementation; Trade UI Revised (`hobtook.tradeui`) is an optional adapter target.
Max Buy, Max Sell and Dynamic Trade Interface are optional coexistence inputs, not dependencies.
No DLC, new Def, XML patch, map Thing cache, foreign assembly or Gateway dependency is required.
Any later XML Extensions use must add its required dependency and guarded merge policy in that
slice. No source configuration import, package impersonation, runtime code or gameplay acceptance
is delivered by this draft.
