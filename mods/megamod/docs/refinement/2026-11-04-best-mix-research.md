# S059 — Best Mix (Continued) research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S059/P1-53, the handwritten arrow to Best Mix, not a separate Random Materials
mod. Inspected 2026-11-04. Selected source/native/consumer research and observable draft complete;
independent source and composition reviews found no issues; strict validation passed. No implementation or runtime acceptance.

## Documentation and exact inputs

Read [Workshop 2195986094](https://steamcommunity.com/sharedfiles/filedetails/?id=2195986094),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2195986094), installed
About/README and [public repository](https://github.com/emipa606/BestMix) before source inspection.
Documentation advertises workbench ingredient criteria, per-bill overrides inheriting the bench,
stock scope/storage options, optional softness/electric protection, no pawn bills and 125-bill capacity.
The old broken-status sentence is struck through in About/README, followed by restoration credit to
Taranchuk and Kayedon; flattened web text must not be mistaken for current broken status. Comments
about spoilage or RIMMSqol are leads, not proof of installed failures. Changelog 1.6.2 fixes bill
handling; 1.6.1 corrects Soft Warm Beds detection. No 1.7 support is advertised.

Installed root `F:/Steam/steamapps/workshop/content/294100/2195986094`; package `Mlie.BestMix`, original
author pelador, maintainer Mlie. About/Manifest/file 1.6.2, assembly 1.0.0.0, informational commit
`63083c63cbda60fe95a379d1d30b2ca5d5adc524`. LoadFolders 1.6 selects root +1.6 +Assets; older versions use
LegacyAssets. Selected reference has Harmony required, CE and Soft Warm Beds load-after hints only.
Source project net48 references compile-only Krafs/Harmony plus MultiplayerAPI/VersionFromManifest.

| Input | SHA256 or revision |
| --- | --- |
| Installed 1.6/BestMix.dll | `1AD3F21A85C3D6C07ED490D21224FF0A28991396719F01FC7C97E781492F70C9` |
| About/About.xml | `8953CB9AA3D98AA7686552C59B14BBE627C2653F9CC9BF54D3576038E5D3B25B` |
| LoadFolders.xml | `E452AE27BB9EF79A765C9D59817F0AFC6C9594A248535B9275A1449CA6EC8753` |
| Public checkout | `05fe8ab8ed3ae184bf7d3569a46e18af60586da3` |
| Public checkout 1.6 DLL | Same hash as installed DLL |
| Native Assembly-CSharp 1.7.4998 rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| NiceBillTabExpansion 1.7 consumer | `E4FFD4EE3B9C79DA1E2767D5751BB7175032957D4E287AC0652E37C1D550F212` |

Ignored artifacts: `artifacts/Refinement/megamod-spec-20261104/best-mix/{upstream,compiled,native,
consumer-nice-bill}` and `consumer-hits.txt`. Read all 14 reconstructed behavior classes, assembly
metadata, current source project/ingredient patches and English strings. Public checkout bundles
the exact installed binary; that fact does not prove a fresh build from this commit. No upstream
build or game executed. Runtime evidence remains the selected decompilation. Other source files are
available for follow-up; do not claim every public source line has been reviewed.

License.md is MIT, copyright 2020 Mlie; selected assembly says pelador 2026 MIT, whereas the inherited
About/README description retains CC BY-NC-SA 4.0. Record both notices; no assertion that MIT clears
all inherited art/code provenance. Independently engineer behavior, do not redistribute source
assemblies/art or copy the old asset bundle. Future actual reuse needs resolved applicable notices.

## Complete selected implementation inventory

- **Controller/Settings:** common source-global ModSettings and version label. Visible defaults:
  AllowBestMix=true, AllowMealMakersOnly=false, mapStock=false, inStorage=true. No per-save settings.
  AllowBMBillMaxSet=true is persisted under typo `AllowDMBillMaxSet` but has no UI. Hidden persisted
  UseRadiusLimit=false, RadiusLimit=100, IncludeRegionLimiter=true and debug flags=false. Readonly
  DebugMaster/RadiusRestrict/adjBillBMPos default=false; useStock=true. These hide diagnostic/radius/
  placement controls; ordinary window does not expose them.
- **Initializer/CompProperties:** queue startup scan, add CompBestMix properties to Building
  subclasses with exact ITab_Bills type, avoiding its own duplicate properties. Runtime Def mutation,
  not XML. CE package `CETeam.CombatExtended`; softness recognizes `JPT.SoftWarmBeds` or
  `Mlie.JPTSoftWarmBeds`; independently resolves `StuffPower_Armor_Electric`/`Textile_Softness` with
  missing allowed. Package presence alone adds menu modes even when the stat is absent.
- **CompBestMix:** bench CurMode default DIS, saved BMixDebug and List<string> BillBMModes. Spawn/load
  normalizes null mode and prunes bill entries. Player-owned spawned valid bench gets Misc1 action
  gizmo, changing bench mode; inspect text shows current bench mode. Debug gizmo is behind the
  permanently-false DebugMaster. Menus are translated-label sorted and use upstream icons.
- **BMBillUtility:** per-bill entries are `uniqueLoadID;mode` strings. NON means inherit current bench
  mode, DIS means nearest. The bill menu labels NON `(No Change)` although it resets inheritance;
  bench `(No Change)` sets DIS, duplicating nearest instead of doing nothing. Set ignores its `set`
  argument, replaces entries for that bill, and does not clone state when bills are copied. GetBillBmTex
  returns a `...Bill` icon; GetBillBMMode returns NON for pawn/null/missing comp. SetBillBmVal opens
  the selector if a comp exists. Null/deleted bill and nullable lists require careful recovery.
- **Bill_DoInterface:** inserts AddBmGui at every Widgets.EndGroup call, assumes width arg3 and
  billStack field; draws a 24-pixel icon at width−174 on non-surgery bills. Does not check the feature's
  enable/meal-only settings, so presentation can remain when selection is disabled.
- **BillStack_DoListing:** if hidden AllowBMBillMaxSet, substitutes every short literal 15 with 125,
  logs error if none. Not scoped to production bench or tied to AllowBestMix. It changes UI listing,
  not AddBill or arbitrary external add/copy limits. No checked semantic target.
- **HarmonyPatching:** unguarded owner `com.Pelador.Rimworld.BestMix`, PatchAll.
- **Ingredient context patch:** TryFindBestBillIngredients prefix sets static curGiver; postfix clears
  it, no finalizer/nesting stack. Stale context after exceptions/nested calls is a static risk.
- **No-mix patch:** Priority.First prefix calls Sort and always sets alreadySorted=true, even if Sort
  did nothing for null context or medical bills. This can suppress native sorting outside active scope.
- **Mix patch:** replaces a SortBy call based on a six-instruction lookahead, fixed argument indices
  and eight-instruction skip, without structural guards or failure warning.
- **BestMixUtility.Sort:** with curGiver and not Bill_Medical, uses mode comparer or Shuffle. Invalid/
  disabled giver falls through DIS, sorting by Position distance anyway. Thus "off/nearest" does not
  necessarily preserve vanilla mixing's ingredient-value-then-distance order. List.Sort has no explicit
  stable/distance tie-break for other modes. Candidates come from native search; no full-radius search
  extension is wired in the selected patches.
- **Legacy helpers:** BMixRegionIsInRange, BMixFinishedStatus, BestMixValidator and debug-list helpers
  remain defined but have no selected internal call sites. Region/radius flags do not implement the
  old full-search behavior. Do not resurrect dead code as an advertised current feature.
- **MultiplayerSupport:** when MP.enabled, registers SetBMixBillMode and SetBMixMode; patches RndFloat
  with tick-seeded Rand.PushState/PopState. Actual Random uses availableThings.Shuffle, not the
  RndFloat comparator. Registered commands are evidence of intent, not deterministic random-selection
  or full multiplayer compatibility. Source debug toggling is not synced.

## Exact current criteria

Twenty-six ordinary modes include nearest; two optional modes bring the menu to 28. Source sorts
ingredient Things, not simulated finished-product stats. Values are per item unless noted; stack
count affects only fraction/stock. No custom ordered criteria or per-slot UI exists.

| Code | Selected code comparison |
| --- | --- |
| DIS | Squared Position-to-root distance ascending |
| DTR | TicksUntilRotAtCurrentTemp ascending; no rottable =>72,000,000 ticks |
| HPT / RHP | Greatest lost-HP fraction / greatest remaining-HP fraction; no HP treated as 0 |
| VLC / VLE | Current MarketValue lowest / highest |
| TMP / FRZ | Current AmbientTemperature highest / lowest |
| RND | Shuffle the current candidate list |
| BIT | Greatest stackLimit/max(1,stackCount), equivalent to least fill fraction for positive ordinary stacks |
| MST / LST | Greatest / least counted units of the candidate's ThingDef |
| BTY / UGY | Ingredient Beauty plus an explicitly present Stuff Beauty **factor**, highest / lowest |
| HVY / LGT | Ingredient Mass highest / lowest |
| FLM | Ingredient Flammability highest |
| PTB / PTS / PTH | StuffPower_Armor_Blunt/Sharp/Heat highest |
| PTE | Optional StuffPower_Armor_Electric highest, missing stat => all equal |
| INH / INC | StuffPower_Insulation_Heat/Cold highest |
| SOF | Optional Textile_Softness highest, missing stat => all equal |
| WSP / WBT | SharpDamageMultiplier/BluntDamageMultiplier highest |
| NUL / NUH | Ingredient Nutrition lowest / highest |

The beauty debug formatter instead adds a Stuff Beauty **offset**, so its displayed number disagrees
with actual sort. Do not describe source Beauty as finished-product beauty. Source damage/robust
comparison treats no-HP items differently from healthy HP items; explicit owned applicability rules
must avoid pretending an unsupported stat is a real zero. Native frozen/inactive rot currently returns
the same 72,000,000-tick sentinel; already-rotting active items return 0, ambient temperature is rounded.
This is current-temperature expiry, not Common Sense's remaining intrinsic rot-progress hours.

## Stock signal, data and native selection

Every stock comparison calls getStockAmount for both candidates, walking ThingsOfDef again each time.
Count stackCount for same ThingDef regardless Stuff/quality/condition. Default scope is strict squared
distance < bill radius squared from bench position; mapStock removes that radius for **counting**.
Default inStorage requires IsInValidStorage. Exclude CompForbiddable.Forbidden, burning and fogged;
there is no per-worker allowed-area/reach/reservation/ingredient-filter check on these totals. Only
spawned lister members count, not pawn inventories or container-only items. Candidate retrieval still
obeys the native bill search; mapStock does not authorize hauling map-wide. A candidate outside valid
storage can be consumed although absent from the stock total. Held Things can have different Map/
Position semantics; the current 1.7 container path must not dereference source assumptions blindly.

Native 1.7 WorkGiver_DoBill inspected: ingredient gathering can succeed early after pawn medicine,
autonomous inner-container, haul-source contents or regional candidates. SearchRadius and worker
permission/reservations remain; spawned candidates use strict distance bound. Regional search can
stop once a complete selection exists; selected Best Mix adds no complete map/radius optimum.
No-mix helper uses PositionHeld distance unless alreadySorted, groups by Def, supports split stacks,
fixed ingredients and whole-stack recipes; actual chosen counts matter. Mix helper normally sorts
ascending IngredientValueGetter.ValuePerUnitOf then distance, using ceil(remaining/value) and current
counts. Replacing that sort can change which resources and how much overage a recipe consumes; it is
not a pure presentation feature. Overlapping ingredient slots must not count physical units twice.

Native BillStack.MaxCount is 15 and DoListing gates AddBill on Count<15; AddBill itself simply appends.
Owned 125-bill extension must govern native/owned add, copy/paste and linked-list paths consistently while
retaining existing over-limit rows. S017 already explicitly allows a supported extension; it otherwise
specifies 15. Enlarging one UI constant is insufficient.

## Proven consumer and composition boundaries

Bounded case-insensitive ASCII/UTF16LE scan of Workshop DLL/C#/XML/JSON found own files, RimPy/
replacement metadata and **NiceBillTabExpansion 1.6/1.7**. Catalog confirms old `pelador.bestmix` and
continued `Mlie.BestMix`. No other executable consumer found in this scope; not universal absence.

Read full current 1.7 BestMixBillButton and BetterWorkbenchesBillButtonsPatch. It resolves
`BestMix.BMBillUtility` **by full type name, without package/assembly identity checks**, then two
static signatures: `Texture2D GetBillBmTex(Thing,Bill)` and `void SetBillBmVal(Thing,Bill)`.
The icon getter is used while rendering a non-surgery production bill; null hides the button. Click
invokes setter to open the selector, not to immediately choose a mode. Translation key
`BestMix.SelectModeDesc` supplies tooltip. One exception disables the consumer's button for session.
It does not read CompBestMix, legacy string storage or settings in these paths. These are concrete
facade candidates; a changed package ID alone need not break this UI. Source coexistence must avoid
two competing type bindings. S011 owns its integrated UI path directly, not a patch against S059.

S026 Use Best Materials already ranks the **finished output's** material for supported no-mix bills;
it does not own general ingredient choice. Its prior composition is explicit lock > output criterion
> Common Sense spoilage > distance. S059 general ingredient mode needs a single combined order,
including secondary ingredients, Random and Nearest, without altering an explicit output-material
lock or committed iteration. S014 whole linked bill state, S017 snapshots and S021/S024 physical
quantities/capacity/route ownership must carry the resulting choice once. Common Sense's linked-group
candidate addition remains distinct from ranking and must not be lost when an explicit mode wins.

## Selected draft and corrections

Owned change: [ingredient preferences](../../../../openspec/changes/specify-megamod-ingredient-preferences/proposal.md).
The draft retains 26 ordinary source criteria and two optional properties, with a new true Default
initial bench choice and bill inheritance. Explicit Nearest remains available; Default/off does not
replace native mixing value order. Native early-stop scope remains, not the dead helper's implied
full-radius optimum. Beauty becomes current ingredient beauty once; HP-inapplicable items trail
comparable items rather than count as damaged. Missing active provider properties hold work and
preserve intent, distinct from inherently inapplicable candidates.

One owned order: filter/material lock, S026 output criterion, explicit S059 ingredient criterion,
S024 remaining spoilage ties, native ties. Random is stack-based, attempt-consistent, UI-independent
and below locks/output ranking. Current-temperature expiry is deliberately different from S024's
intrinsic spoilage-hour key. The existing S026/S024 specs/designs now state this composition and keep
the linked-group candidate addition. No independent sort or duplicate physical quantity owner.

Stock retains source-like spawned-map counting; no independent cache. Shared/native facts provide
one type aggregation per selection, with actual permission separate. Inherited copies visibly bind
to destination default; linked benches share the source-selected default; started work keeps actual
portions and committed choices. The 125-bill capacity is a common production-list policy, deliberately excluding
medical/pawn lists and preserving over-limit rows when disabled. Five configuration options use
global/save inheritance rather than source-global state; hidden dead controls stay omitted.

Select the exact two-call external facade, not package/assembly/comp/settings spoofing. Original-
present behavior delegates only to an admitted actual source assembly or safely declines. CE and
Multiplayer were not found in the bounded installed About identity check; installed softness
continuation is `Mlie.JPTSoftWarmBeds` (Workshop 3006469889). Exact optional beta support remains open.
Source multiplayer helper evidence is insufficient; owned admitted sessions must synchronize actual
attempt decisions and host-effective config without overwriting client-local global defaults.

| ID | Outstanding work | Draft position |
| --- | --- | --- |
| MX-Q1 | Selected UI/default/meaning review | Default vs explicit Nearest, ingredient-only Beauty, condition applicability, linked defaults and production-only 125-bill capacity are explicit corrections |
| MX-Q2 | Stock/shared-data scope and cost | Source-like spawned units exclude container-only/pawn inventory; exact query/freshness/holder boundaries and combined profiling before code |
| MX-Q3 | External and multiplayer contracts | Two proven button methods selected; validate actual source delegation, optional stats and canonical multiplayer API/session behavior before support claims |
| MX-Q4 | Native admission, balance and acceptance | Effective mix/no-mix/count/material/capacity seams, RED/GREEN, review, real crafting/UI/copy/provider workflows and measured costs; all runtime tasks unchecked |

Static semantic pass must retain recipe value/rounding and physical units, distinguish stock signal
from reach, and avoid claims that random input or 125 bills improve balance/performance. Source/current
native comparisons are evidence of design constraints; finalized optional content and live work
remain future proof. No build, game launch or source/normal-save mutation occurred.
