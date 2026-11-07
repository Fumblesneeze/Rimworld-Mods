# S095 — Max Buy, Max Sell research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
P2-24. Documentation/source analysis on **2026-11-05**, not runtime acceptance.
[Change](../../../../openspec/changes/specify-megamod-trade-quantity-shortcuts/),
[coverage](../replacements/trade-quantity-shortcuts.md), [ledger](specification-progress.md).

## Provenance and inspection order

1. [Max Buy, Max Sell Workshop page](https://steamcommunity.com/workshop/filedetails/?id=2889812094):
   contextual affordable/repair quantities and Trade UI Revised compatibility. The description does
   not mention gifting. A player reports excessive-sale correction leaving one extra unit; that
   report is a research lead, not accepted reproduction. The author's DTI comment points to similar
   buttons, not a confirmed adapter. Sharedfiles access returned 429; the workshop/filedetails route
   was readable. Generic Steam page banners are not proof of mod removal/incompatibility.
2. Installed About, entire package file inventory, both language catalogs and all behavior classes
   in the sole root DLL were inspected read-only. No source project, public-source link or license
   file accompanies this package. Bounded public searches for Leoltron/MaxBuyMaxSell and the detector
   found no source repository; a guessed repository URL returned 404. Source availability and reuse
   terms remain unknown. Independently implement the observable contract; do not copy DLL/art/textures.
3. [Trade UI Revised documentation](https://steamcommunity.com/workshop/filedetails/?id=2683622537)
   describes two columns and right-click full quantities and names Max Buy, Max Sell compatibility.
   Its linked [source repository](https://github.com/patrickdevarney/RimworldTradeUI) is public; the
   Workshop author permits modifications with credit. Exact installed 1.7 renderer/input code was
   decompiled to establish the consumed shape, not assumed identical to the repository branch.
4. [DTI documentation](https://steamcommunity.com/workshop/filedetails/?id=3020706506) describes its
   own affordable buttons and choosing the original window by Ctrl/opening preferences. Its linked
   [repository](https://github.com/Zeracronius/DynamicTradeInterface) has an MIT license. Only this
   coexistence boundary is in S095; its whole UI/source implementation is not adopted or accepted.
5. Targeted native decompilation: TradeDeal, Tradeable, Transferable, TransferableUIUtility,
   Dialog_Trade, Planet.FactionGiftUtility and DiplomacyTuning in **1.7.4998 rev1299**.

| Input | Inspected identity |
| --- | --- |
| Source | Max Buy, Max Sell, Leoltron; package spelling `Leoltron.MaxBuyMaxSell`, canonical `leoltron.maxbuymaxsell`; Workshop `2889812094`; About modVersion **1.5.1**, supported **1.4/1.5/1.6**, Harmony required |
| Source DLL | `F:/Steam/steamapps/workshop/content/294100/2889812094/Assemblies/Leoltron.MaxBuyMaxSell.dll`; assembly/file **1.0.0.0**, informational **1.0.0+10343039c8c21606dcf295211da952c4a8de11b1**, Release; no version folders/LoadFolders |
| DLL SHA-256 | `E31835C03B6721C6658E1732DFE91215BB0C44AC149B96818DC56A30ABA300F0` |
| About SHA-256 | `16891CCE8C408F6B282F087A38ED8BB9AFF9658939CF721EFCC32D7B2F46707B` |
| Optional UI | `hobtook.tradeui`, Hob Took, Workshop `2683622537`; About supports 1.3–1.7, Harmony required; LoadFolders chooses root + `v1.7`; `v1.7/Assemblies/TradeUI.dll`, assembly/file **1.0.0.0** |
| UI DLL SHA-256 | `7B14A8C001CC02D92B9D28ACD1DA9C6701725039EF1A346CC315DE051019AC53` |
| DTI identity | `zeracronius.dynamictradeinterface`, Zeracronius, Workshop `3020706506`; installed About/LoadFolders support 1.7 and select root + `1.7`; no exact binary integration claim |
| Native DLL SHA-256 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Hashes are provenance only, not compatibility predicates. Local decompilation remains ignored under
`artifacts/Refinement/megamod-spec-20261105/max-buy-max-sell/`: `binary/`, `native/`, `trade-ui/`.
No foreign assembly was executed/built or copied into a product. Selected module/shape identity must
be renewed at implementation/admission time; no current 1.7 live run is claimed for the old source.

## Observed compiled behavior

### Monetary quantities

`TransferableUIUtilityPatch.Prefix` runs for interactive, non-read-only, willing Tradeable rows in
silver sessions. Ordinary mode excludes currency rows. It sums all noncurrency current costs and
uses Mathf.RoundToInt to project colony/trader funds; passes pre/post funds, directional prices,
post-deal counts and pending quantities that can be undone to `MaxBuyMaxSellDetector.TryReplace`.

The detector first tries the buy direction, then sell, returning one replacement. If a party has a
deficit it attempts just enough correction; otherwise it attempts the affordable maximum. Undoing
selected sales uses sale price and undoing purchases uses purchase price. Crossing zero is optional.
Branches generally require more than one post-deal item and positive remaining cash; full affordable
stock keeps ordinary all controls. Repeated manual floor/round adjustments are not symmetric.

**Concrete source arithmetic counterexample**, not an observed live game: colony starts with 10,
trader with 5, pending sale of five at 2 each, no other rows. Cost is -10; trader post balance -5;
colony post balance 20. The first undo-sale branch takes floor(5/2)=2. Its increment condition uses
colony pre + round(cost + 2×salePrice) = 10 + round(-6) = 4, so it does not increment. It returns
undo two, leaving sale three costing the trader 6 and still a deficit of 1. Undo three is the minimum
valid correction. This independently supports the report's class of issue for these inputs.

`TradeableDealParams.CurrencyColonyHasPostTrade` uses trader pre-funds in its property, but no actual
UI caller was found using that helper; do not conflate this unused error with the active counterexample.
Current source finite/zero-price and extreme-quantity guards are insufficient for a general solver.

### Gift quantities

`PrefixGiftMode` applies to gift rows including silver. It calculates current goodwill plus native
GetGoodwillChange for existing gifts, then selects the next strict threshold: below0 →0, below75 →75,
below100 →100; none afterward. Source labels are `:|`, `:)`, `>M`; tooltip adds quantity/value/target.

It subtracts the value of existing gifts from an inverse estimate of required gift value, then takes
ceil(remainingValue / **ordinary row sale price**). Existing prisoner gifts from the receiving faction
get ×2, but the candidate row's divisor omits that factor. For an illustrative native baseline at
goodwill -2 and a returned prisoner priced at 40, one prisoner contributes 80 gift value and reaches
zero; the source can demand two and hide the offer when only one exists. No game fixture was run.

`GoodwillHelper` duplicates/inverts the goodwill curve from zero, uses a cached 75-entry array and
2000-iteration binary search, and subtracts independently quantized estimates for positive starting
relations. It uses 40 value per point below zero, 160 above75. Those are source calculation details,
not new owned balance knobs. There is no final native minimality check, robust null/locked-faction
guard or complete invalid-price guard. Positive gift adjustment is intentional: native gift mode
reverses PositiveCountDirection to Destination, unlike ordinary trade's Source direction.

### UI, state and settings

- Source startup calls unguarded PatchAll under owner `Leoltron.MaxBuyMaxSell`.
- Before row drawing it populates a static Rect-keyed replacement dictionary. A global Widgets.Label
  prefix substitutes text by rectangle without checking the stored original label. Adjustment hooks
  compare the transferable, pointer rectangle and sign. Vanilla uses AdjustTo; Trade UI Revised uses
  AdjustBy. Only a postfix clears the table; Working toggles have no guaranteed exception cleanup.
- Trade UI reflection reads `TradeUI.TradeUIParameters.Singleton` and `isDrawingColonyItems`; missing
  property/field shapes are not safely admitted. It calculates fixed column-specific pixel offsets.
- Source two-column 1.7 code skips native DoCountAdjustInterfaceInternal while a Dialog_Trade is open.
  Its own left-click/drag cases call AdjustBy; right-click/drag cases call AdjustTo full bounds. The
  public Singleton getter returns TradeUIParameters and the instance field is bool. No source-type
  consumer was found in this renderer; this is a source consumer of Trade UI, not the reverse.
- Global source settings: `replaceZeros=false`, `forceDollarSignInMaxBuyMaxSellButtons=false`.
  The former's wording says not to replace reset0; implementation can still replace it with a partial
  undo before zero. The owned label states the actual crossing boundary clearly. Both owned settings
  gain the shared per-save inheritance model; no original keys are imported.
- Source UI is trade-row buttons plus mod settings, no bottom tab. Holding middle mouse over a row
  enables repeated diagnostic input/result logging without a developer-mode guard. Deliberately omit
  this hidden gesture; any later explicit debug action follows top-debug placement.
- Only English/Russian source catalogs ship. Owned baseline languages remain all seven.

## Native boundaries and deliberate composition

Native TradeDeal.UpdateCurrencyCount sums CurTotalCurrencyCostForSource and calls the currency row's
virtual CostToInt (baseline RoundToInt). Transferable.AdjustTo clamps against the actual row range.
DoCountAdjustInterface maintains zero stopping points; smaller ranges can use a combined single
button. Do not blindly require two or more items or place a replacement at a nonexistent outer rect.

Native Dialog_Trade's final action checks trader funds and can ask the player to confirm an unpaid
excess sale. TradeDeal also checks colony funds and other native refusal conditions before execution.
LimitCurrencyCountToFunds clamps the actual silver transfer; that is not an affordability oracle.
Owned suggestions preserve ordinary manual excess-sale confirmation, never prohibit it globally.

Native gift calculations use selected counts capped to held stock, sale price, ×2 for returned
same-faction prisoners, value/40, the current goodwill curve, integer truncation and caps. The curve
starts at the faction's **current** goodwill, not zero. Dialog_Trade's gift-toggle eligibility checks
faction presence/permanent-enemy status and excludes Favor. Other gift entry points and effective
faction restrictions need explicit admission; no source assumption guarantees goodwill can change.
CheckCanCarryGift is a separate native routine and was not called in the inspected Dialog_Trade or
TradeDeal bodies. Preserve checks at their actual workflow sites; do not claim it runs on every trade
or invoke its rejection messages during preview.

Owned design uses one current-session valuation and direct row actions; monetary and gift checks
stay separate. No map/hauling/Thing-search cache is introduced. Readable Numbers may format output
but must not supply rounded input prices. All common settings and diagnostics compose directly,
without self-patching. No Def/XML edit is needed.

## Contracts, consumers and omissions

The public detector takes seven floats and four integer counts and returns a nullable replacement;
GoodwillHelper exposes threshold/value helpers and TransferableUIUtilityPatch exposes static bool
Working. Internal patch machinery supports the source implementation; TradeBalanceState,
TradeActionType and MaxBuyMaxSellReplacement are public types, even though no external consumer
was found by this scan.
No Def, gameplay-save component, persistent suggestion, content addition or recipe was found.

A bounded `rg -a -l` search of installed Workshop DLL/C#/XML for `Leoltron.MaxBuyMaxSell`,
`IMaxBuyMaxSellDetector` and `MaxBuyMaxSellDetector` found only the source DLL/About. This discovers
ordinary metadata/text references, not all dynamically built/reflected strings or UTF-16 package
checks. It does not prove universal non-consumption. No facade is justified by current evidence;
retain the package/assembly identity gap and inspect any actual consumer before claiming drop-in.

With the original package active, defer the owned feature with a bounded warning rather than let
both replace buttons. Trade UI Revised is optional and shape-admitted; DTI's own controls remain
its responsibility. No entire-provider UI import or speculative harmony-owner priority chain.
No source setting migration, hidden logging gesture, inverse-curve cache, broad widget substitution,
price rebalance, stock changes or source API compatibility claim.

## Open research and acceptance questions

| ID | Remaining work | Resolution path |
| --- | --- | --- |
| MBMS-Q1 | Exact effective monetary/gift projection, outer patches, relation locks and bounded hypothetical evaluation | Before coding, inspect the selected 1.7/provider semantics; target specs and honest tests, native complete-preview comparison; no real-count probe mutation |
| MBMS-Q2 | Native/Trade UI Revised real input boundaries, drag/right-click, layout and DTI/native-window coexistence | Separate admitted adapters, exact provider identity and native player workflows; skip unsupported shape without guessing offsets |
| MBMS-Q3 | Public original source/license and any actual external API/settings consumer | Bounded further evidence if needed; independent implementation, attribution and no package spoofing/implicit import |
| MBMS-Q4 | Seven-language/tooltips, save inheritance, rounding/gift edge fixtures and reviewed-build acceptance | Focused TDD then exact native trades/gifts/settings, personally observed stock/silver/goodwill and cleanup; no arithmetic-only acceptance |

## Semantic balance pass and status

This is quantity assistance for the existing economy. Baseline 0/75/100 milestones and actual native
gift valuation are preserved; no market value, trade-price multiplier, stock, carrying capacity,
research, skill, goodwill reward or recipe is changed. Fixture prices above illustrate arithmetic,
not balance decisions. Native manual selection and final restrictions remain authoritative. The
largest affordable amount is not a claim that every independently disallowed transaction can execute.

Draft authored; independent review and strict validation are recorded in the ledger after completion.
No host test, build, install, game launch, normal save/config edit, Workshop mutation or runtime
acceptance occurred. All code, native validation and compatibility evidence remain future work.
