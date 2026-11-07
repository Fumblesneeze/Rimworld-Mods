## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S095/P2-24. [Research](../../../mods/megamod/docs/refinement/2026-11-05-trade-quantity-shortcuts-research.md)
records the 1.5.1 source package/root assembly, native 1.7.4998 and optional Trade UI Revised 1.7
binary. Installed Max Buy, Max Sell advertises only 1.4–1.6. No 1.7 compatibility is accepted here.

Source calculates a proposed adjustment from current row stock, directional prices and whole-deal
silver. It also contains goodwill-target gifting absent from its public description. The useful
intent is retained; its duplicated arithmetic, inverse goodwill curve, global rectangle table and
unguarded patch installation are not implementation contracts.

## Goals / Non-Goals

**Goals:** explainable row-local suggestions, native economy and rounding, no unintended execution,
minimal gift quantities, common settings and independently admitted native/provider UI surfaces.

**Non-goals:** pricing/relation changes, automated trading, multi-row deal optimization, new trading
windows/tabs, expanding trader stock, changing gift eligibility, replacing DTI, whole-mod binary
parity, importing source settings or adding a general purpose trade cache/framework.

## Decisions

### Express quantities against one immutable deal snapshot

Read the native trade session and current tradeables on the game thread. Normalize each row into
colony-oriented buy/sell quantities, held stock, actual native bounds, prices and willingness. Keep
currency separate. Evaluate a candidate final row count with every other row fixed, applying the
same whole-deal accumulation order and currency conversion as the active game. Native
TradeDeal.UpdateCurrencyCount sums noncurrency CurTotalCurrencyCostForSource then delegates to
CurrencyTradeable.CostToInt, currently Mathf.RoundToInt. Do not round each row, substitute displayed
rounded prices, assume every currency subclass uses that conversion, or use funds clamped for
actual transfer as proof that the original proposed deal is affordable.

Candidate cost is piecewise across zero: removing a pending purchase refunds the buying price;
removing a pending sale removes its selling-price proceeds; a new transaction beyond zero uses its
own directional price. Prefer a small bounded monotone integer search with the native currency
projection over the source's hand-adjusted floor/ceil branches. Validate the largest feasible
balanced-deal quantity and its next neighbor; for debt repair validate the first feasible quantity
and its previous neighbor. Validate both parties' projected silver and the native row bounds.
Zero remaining silver can still permit a low-priced quantity that rounds to zero in the full deal.

Only finite, admitted positive marginal valuations and bounded integer quantities qualify for
calculated shortcuts. Free/invalid/nonmonotone provider prices keep ordinary controls. Avoid integer
overflow in bounds/deltas and floating-point stagnation in searches. A provider whose effective
cost depends on additional unmodeled state needs its own admitted projection or a safe fallback.
Never mutate real counts temporarily, call ResolveTrade, or execute a foreign sale callback to probe
a hypothetical quantity. No additional stock, cash, goodwill or player economic information is made up.

The source returns the first useful direction, preferring buy. Evaluate both meaningful controls
independently; the player chooses one. This is an intentional presentation extension, not executing
both. If selecting all available stock already fits, leave the ordinary all control. With the
cross-zero option off, calculate within the segment up to zero: a useful partial undo is allowed,
but reaching zero retains the zero action and no shortcut continues into a new direction. This
reflects source behavior more precisely than its broad setting label promising never to replace 0.

### Bind the suggestion to the actual button action

The native quantity renderer uses stopping points and distinct single/all/reset controls. Trade UI
Revised replaces the internal renderer, so original native IL alone cannot prove its live route.
Source hooks Widgets.Label globally by Rect and then AdjustTo/AdjustBy by mouse position and sign.
The stored original label is not checked. Avoid that mechanism: attach suggested text and the
matching adjustment at the admitted trade-row action boundary, using the actual trade/session/row,
direction and input action identity. Numeric editing, native-layout single steps, reset and other
transfer dialogs must not be captured merely because the pointer lies over a former button rectangle.
Trade UI Revised has a deliberate exception: an offered shortcut replaces its left-click step/
multiplier action, as the source integration does. Without an offer, left-click remains ordinary;
provider numeric entry and right-click all remain unchanged. Do not invent an undiscoverable alternate
gesture or claim that the replaced step is simultaneously preserved.

At click, re-read/revalidate session, mode, row, bounds, prices, pending counts and provider selection.
If the offer changed since presentation, refresh without applying a different hidden quantity;
require a fresh explicit activation. Apply only the chosen row through native adjustment and its
ordinary change notification. Let the dialog refresh silver, mass, goodwill and other summaries.
Cancellation and acceptance remain native paths. Any temporary rendering/action scope is nested,
exception-safe and cleared on close/mode change. UI-scoped facts are discarded on session teardown.

Do not rebuild a separate map inventory or Ctrl+F index: TradeSession owns the authoritative stock
and pending counts. Reuse its facts; if repeated valuation needs memoization, key it to this dialog's
complete relevant revision and measure first. Invalidating quantity, currency, prices, goodwill,
faction, mode and provider changes is mandatory. No live references are saved or used off-thread.

### Predict gifts through the actual native rules

Target the next milestone strictly above current goodwill plus the current basket's native predicted
gain: 0, then 75, then 100 in the inspected game. These are native neutral/allied/maximum milestones,
not new balance settings. Revalidate their meanings if the native/provider relation system changes.
Choose the smallest additional positive count on the selected row whose full gift prediction reaches
that target. Include silver gifts and native prisoner valuation. Do not subtract independently
inverted goodwill curves or divide every gift by an ordinary item's sale price.

Native FactionGiftUtility has read-only GetBaseGoodwillChange(Thing,int,float,Faction) and
PostProcessedGoodwillChange(float,Faction) seams beneath GetGoodwillChange(List<Tradeable>,Faction).
They currently value a returned prisoner of the receiving faction twice, divide value by 40, then
apply the relationship curve from the **current** goodwill with integer truncation/caps. Prefer an
admitted read-only projection of those same seams for hypothetical counts, including any effective
modifications at the outer calculation boundary. If an outer patch changes valuation beyond that
projection, do not advertise a falsely exact target. Validate against the complete native preview;
never modify real tradeables to test a quantity. The copied source curve/cache is unnecessary.

Relation restrictions can make numeric goodwill unattainable; no target promise for null/permanent
enemy or otherwise locked factions. If available row stock cannot reach the next target, keep manual
gifting. Preserve all native acceptance/carrying checks at their real execution sites; do not run
message-producing checks during repaint or invent a new carry restriction where native trading has
none. The shortcut predicts goodwill, not every reason the transaction may later be refused.

### Settings, localization and external contracts

Two common definitions under Trading → Quantity shortcuts, in this order:

| ID | Built-in default | Effect |
| --- | --- | --- |
| trading.quantityShortcuts.crossZero | false | Allow a quantity shortcut to continue past a row's zero selection |
| trading.quantityShortcuts.forceDollar | false | Use $ instead of the localized money symbol in shortcut labels |

Both use global default/save override/Use global, including dormant overrides and main-menu mutation
guards. Apply changes on the next presentation/action; no separate source ModSettings or static
settings singleton. Pending quantities and gift mode are session gameplay state, never settings.
Translate clear quantity/direction/undo/goodwill tooltips and settings in all seven baseline locales;
numeric formatting composes with the owned readable-number feature without changing arithmetic.
Source emoticons alone do not explain goodwill targets; use a readable numeric target plus tooltip.
No bottom tab/right-side toggle is needed. Omit the source hidden middle-mouse logging gesture;
future explicit diagnostic actions must live in the top developer toolbar.

The bounded installed symbol scan found no external consumer of source public detector/helper
types or its Working flag. This is not a universal absence proof. Do not supply a speculative facade,
spoof Leoltron.MaxBuyMaxSell, or read/write its setting keys. A later actual consumer requires a
separately evidenced contract. No save payload exists for pending source suggestions.

### Admit native and external surfaces independently

These are candidate target families, **not implemented target documents or validators**. At the first
patch create stable-ID target records with exact consumed semantics, tests and native evidence links.

| Candidate | Minimal semantic contract | Failure domain |
| --- | --- | --- |
| TQS-ROW-ACTION | Actual trade row/direction/button action receives the shown final count through native adjustment and updates the same dialog; no capture of typing, single steps or unrelated transfers | Native shortcut presentation plus action, atomic together |
| TQS-COST | Actual directional row values aggregate and currency conversion predicts the same two balances as execution; relevant effective patches preserve this data flow | Monetary suggestions |
| TQS-GIFT | Current basket, faction, target milestone, native base valuation and final goodwill projection agree, including effective outer patches and relation restrictions | Gift suggestions |
| TQS-TRADEUI | Active hobtook.tradeui; admitted actual row renderer/input routes and any used field/property shapes; correct left-click quantity vs right-click all behavior in both columns/modes | Trade UI Revised presentation/action, atomic together |

If a seam is only read, record its shape/projection admission without inventing an unnecessary
Harmony patch. An actual argument rewrite must validate the relevant consuming code/data flow, not
just its signature. Inspect effective prefix/skip-original/transpiler behavior; every owned
transpiler validates its incoming stream on every application. Tolerate unrelated IL/foreign owners,
never use hash equality or an empty patch list as admission, and never execute foreign transpilers
speculatively. Initialize idempotently, roll back only newly added own members of the affected group,
and revalidate or deactivate after supported later repatching. Each skipped enabled patch emits one
bounded startup warning with owner/patch/spec/target/reason, unavailable behavior and fallback.

Trade UI Revised's inspected reflection surface is TradeUI.TradeUIParameters, public static Singleton
returning that type and public instance bool isDrawingColonyItems. Read only if actually needed after
canonical package detection and full shape admission. Its 1.7 left-click routes call AdjustBy;
right-click routes call AdjustTo and retain provider behavior. No hardcoded pixel offsets or eagerly
loaded optional type references. An unsupported provider renderer keeps its own complete controls;
do not draw half a replacement or claim compatibility based on the Workshop description.

With active leoltron.maxbuymaxsell, defer this whole owned feature with one bounded coexistence
warning; do not unpatch/remove the original. Dynamic Trade Interface has its own window and affordable
buttons; leave that window untouched. Its documented native-window fallback can use this feature
only when the actually opened native/admitted surface is verified. Package presence alone neither
forces a DTI adapter nor disables an otherwise admitted native route. Multiple replacing surfaces
must select one actual owner, or fall back safely. No duplicate implementation of owned features.

## Risks / Trade-offs

- **Beta and price-provider drift:** minimal effective admission, conservative fallback and separate
  exact native acceptance; source or host arithmetic alone is not compatibility evidence.
- **Tiny prices/rounding and gift integer steps:** neighbor validation against native totals, without
  per-row rounding or a hard-coded copied curve. Illustrative spec numbers are fixtures, not defaults.
- **Large catalogs:** bounded row search and current-session facts; no speculative optimization claim.
- **Provider UI changes:** separate atomic adapters and actual input identity; retain usable controls.

## Migration Plan

No source save/config migration. New saves inherit global settings; old games without entries do so
as well. Reopening a trade builds fresh session state. Disabling/removing the feature leaves native
trades/saves and original provider files intact. No Workshop writes or assembly reuse.

## Verification and Open Questions

MBMS-Q1–4 in [research](../../../mods/megamod/docs/refinement/2026-11-05-trade-quantity-shortcuts-research.md)
retain effective-price/gift admission, exact provider input routes, missing public source/license and
final native/localization evidence. Research proceeds without interviewing the user about source
behavior. Host Unit tests own bounded calculations; Harmony tests own synthetic admitted patches;
real sessions, game prices, factions and provider assemblies require fresh isolated game processes.
Final reviewed-build player workflows must adjust native rows, inspect changed totals/tooltips,
accept representative purchases/sales/gifts and observe delivered items/silver/goodwill. Include
cancel/reset, changed offer, absent/changed/present providers and save/global UI. A final semantic
balance pass must establish unchanged prices, stock, eligibility and relation reward, with no new
profitable loop or premature transaction. All implementation and live tasks remain unchecked.
