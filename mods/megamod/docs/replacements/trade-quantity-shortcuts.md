# Max Buy, Max Sell — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**, S095/P2-24. Source **Max Buy, Max Sell**, Leoltron,
`Leoltron.MaxBuyMaxSell` / canonical `leoltron.maxbuymaxsell`, Workshop **2889812094**.
Installed About **1.5.1**, root assembly **1.0.0.0**, supported **1.4–1.6** only.
[Research](../refinement/2026-11-05-trade-quantity-shortcuts-research.md) records exact hashes,
documentation, full binary analysis, optional UI/native comparisons and unknown original source/license.
**Nothing implemented or accepted.** No package-ID or binary drop-in claim.

## Behavior inventory

Every row is **specified, not implemented** in the
[observable contract](../../../../openspec/changes/specify-megamod-trade-quantity-shortcuts/specs/megamod-trade-quantity-shortcuts/spec.md).

| ID | Player workflow and result | Options / deliberate correction | Requirement |
| --- | --- | --- | --- |
| TQS-01 | Click a row shortcut → update only its pending quantity and native totals | No automatic acceptance, new stock, changed prices or unrelated transfer effects | Contextual trade suggestions preserve ordinary control |
| TQS-02 | Choose an affordable maximum → largest count both sides can fund in the full basket | Whole-deal rounding, stock bounds, zero-silver fractional cases; native all when sufficient | Affordable quantities use the whole pending deal |
| TQS-03 | Correct an unaffordable proposal → smallest count change that clears the deficit | Fix source excessive-sale off-by-one; no false repair when impossible | Unaffordable deals get the smallest sufficient correction |
| TQS-04 | Undo a selection or cross zero → correct directional prices | Crossing defaults false; partial undo allowed; both useful directions can be offered | Undoing and crossing zero use their proper prices |
| TQS-05 | In gift mode choose target → minimum extra row count to native 0/75/100 milestone | Existing gifts/current relations/prisoner value included; no unattainable promise | Gifts reach the next predicted goodwill milestone |
| TQS-06 | Stock/price/session changes → refresh truthful offers | Stale click does not apply a different amount; invalid/free valuation keeps ordinary UI | Suggestions stay truthful as the trade changes |
| TQS-07 | Configure cross-zero/symbol and read localized tooltips | Two false defaults, shared save inheritance, all seven locales | Configuration and explanation use the common interface |
| TQS-08 | Use optional trade UI/original provider → one admitted owner | Trade UI Revised right-click preserved; DTI own window untouched; original deferral | Preserve optional trade surfaces and original ownership |
| TQS-09 | Unsupported target → usable original controls and bounded warning | Atomic label/action installation, benign patches allowed, later revalidation | Compatibility failures preserve native trading |

## UI audit and settings

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Native all/zero trade-row controls | Select an ordinary trade quantity | Existing trade dialog, clear quantity/repair/gift tooltip; no new bottom tab |
| Trade UI Revised single-arrow controls | Same action in two columns | Offered left-click shortcut deliberately replaces its step/multiplier; absent offer retains ordinary step; numeric entry and right-click full quantity preserved |
| Mod settings | Two configuration preferences | Trading → Quantity shortcuts; Global default / Current save / Use global, both built-in false |
| Hidden middle-mouse logging | Debug arithmetic | Omitted; any future explicit diagnostic action belongs in top developer controls |

Current save is disabled while inheriting; at the main menu both save editor and Use global are
inactive. Dormant overrides survive inheritance toggles; first override seeds from effective global.
Changes affect the next offer. Counts/mode are contextual trade state, never configuration. Owned
English, German, Spanish, French, ChineseSimplified, Russian and Japanese replace the source's
English/Russian-only coverage. Clear target numbers/tooltips replace unexplained gift emoticons.

## Interactions and external contracts

| Surface / evidence | Policy and limitations |
| --- | --- |
| Original active `leoltron.maxbuymaxsell` | Defer this feature with one coexistence warning; no removal/unpatching |
| `hobtook.tradeui` consumed Singleton/column field and actual renderer | Optional package/shape-gated adapter; exact effective left/right action semantics and live workflow required |
| `zeracronius.dynamictradeinterface` own window | Leave its own affordable buttons intact; an admitted native fallback can still use the owned feature |
| Source public detector/helpers/Working | No external caller found by bounded installed symbol scan; no speculative facade, assembly or package impersonation |
| Source global keys | No implicit migration/import; owned common settings and save inheritance |
| Native quantities/prices/goodwill | Preserve execution/refusals/manual oversell confirmation; projection must match the complete native preview |

Compose formatting/settings/diagnostics directly with owned features. Use native TradeSession facts;
no per-mod Thing cache or shared-map index is needed. Core and external Harmony are required when
implemented; optional UI packages are not hard dependencies. No DLC or XML patch is required. Any
future XML operation needs the ordinary target/merge guards and XML Extensions dependency at first
use. Product never ships upstream assemblies or depends on the Dev Gateway.

## Implementation, omissions and verification

Proposal, design, capability and unchecked tasks live in the
[change](../../../../openspec/changes/specify-megamod-trade-quantity-shortcuts/).
All TQS behaviors lack implementation, executable target records, focused RED/GREEN and final live
evidence. Intentionally omit hidden debug logging, copied inverse goodwill calculations, broad widget
replacement, source settings import and speculative public API compatibility. No price/reward rebalance.

MBMS-Q1–4 retain exact effective projection, optional UI, provenance and acceptance work. Document
review/strict validation are tracked in the [ledger](../refinement/specification-progress.md); they
do not accept runtime behavior. Final reviewed-build native trades/gifts must visibly demonstrate
pending quantity → accepted transaction → actual stock/silver/goodwill, plus refusals, cancellation,
settings/locales and separate provider groups, with exact-process evidence and cleanup. Balance
review must confirm unchanged underlying economy and no execution caused by preview.
