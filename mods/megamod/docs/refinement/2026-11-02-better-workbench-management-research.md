# Better Workbench Management: source and native research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Queue **S017**, photo **P1-14**, origin **replacement**. Started **2026-11-02**.
This journal records evidence and remaining implementation research for the
[S017 draft](../../../../openspec/changes/specify-megamod-workbench-management/).
Earlier checkpoints below retain what was known at that point; the completed trace and decisions
follow them. No runtime implementation or compatibility acceptance is claimed.

## Documentation first

Read the [author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=935982361),
[latest change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/935982361) and
[source repository](https://github.com/Falconne/ImprovedWorkbenches/).
The description cautions that vanilla now includes some former features. It covers copying/linking,
bill navigation and overview conveniences, product counting choices and bench-wide worker restrictions.
The 2025 notes specifically remove custom renaming for 1.6, add worktable restrictions, remove HugsLib,
add Colony Groups/hauling-related counting support and add inventory counting disabled by default.
This is the documentation starting point, not a claim that every older advertised feature remains
distinct from native 1.7. Native parity must be mapped before specification.

## Exact inputs

- Read-only installed root: `F:/Steam/steamapps/workshop/content/294100/935982361`.
  **Better Workbench Management**, **Falconne**, package **falconne.BWM**, About **1.6.1.3**,
  supported **1.0–1.6**, required **Harmony**, Core/Harmony load-after hints. No declared 1.7 support.
- About SHA-256 **0883A31034258EB4AEF9839BEA976E50EE839C5639D3759ACF0BF2CD9AA1968F**.
- Inspected `1.6/Assemblies/ImprovedWorkbenches.dll` SHA-256
  **0913320230337B8482C3903454A3E1417BAFB6A7AA80A3703D936736CBA827D4**;
  assembly/file **1.6.1.3**, informational
  **1.6.1.3+e339a5ca077cefe8005be09579a729a812fe01c7**.
- Public source pinned **f8db7979e9885e0e723a110e56d702b68dbfc341**, dated
  2025-08-04T21:34:08+02:00. The DLL's informational commit could not be fetched from that repository
  (`not our ref`); do not claim identical revisions. Installed decompilation is authoritative for
  installed behavior, with public source used as explanatory evidence after comparison.
- Public repository has **GPL-3.0** LICENSE.txt. No license file appeared in the bounded installed
  license-name inventory. Record attribution/reuse decisions before any reuse; these documents do
  not copy the source implementation or assemblies into the product.
- Ignored artifacts: `artifacts/Refinement/megamod-spec-20261102/better-workbench-management/upstream`
  and `installed-decompiled`. The whole installed 1.6 assembly was decompiled into individual files;
  this does not mean all those files have been read yet.

## Completed source reads: settings and startup

Installed `ModSettings_ImprovedWorkbenches`, `Mod_ImprovedWorkbenches`, `Main` and AssemblyInfo read.

| Setting / saved key | Built-in default | Source role, pending complete call-site trace |
| --- | --- | --- |
| expandBillsTab | true | Automatically open selected workbench's Bills tab |
| enableDragToReorder | true | Bill overview drag reordering |
| mirrorSuspendedStatus | true | Include suspended status in source bill mirroring |
| dropOnFloorByDefault | false | Default new-bill delivery choice |
| countOutsideStockpiles | true | Extend eligible loose product counting |
| countInventory | false | Additional carried/inventory counting |
| countCarriedByNonHumans | true | Include colony animals/mechs/player-controlled subhumans in relevant counting paths |

All seven source fields are static global ModSettings. Our adopted settings must use the common
global/save model. The user's whole-list synchronization requirement takes precedence over any
source choice that would leave a linked row's suspension local; final composition must be explicit.
Worker restrictions and bill-specific extra-product/away choices are gameplay state, not settings.

Startup constructs Main and calls PatchAll under the source package identity, then loads settings.
World.FinalizeInit sets the WorldComponent storage and clears clipboard references. Main integrates
by type discovery with Outfitter, Project RimFactory, No Max Bills and Colony Groups; those source
checks are not our required canonical-package/shape adapters. Main.GetMaxBills returns 15 ordinarily,
125 when its NoMaxBills type probe succeeds. Bill deletion removes extended data and clipboard references.
No new bottom tab has been found in the inspected startup/settings paths; remaining UI paths still need reading.

## Completed source reads: extra state and counting

Read `ExtendedBillData`, `WorktableRestrictionData`, `WorktableRestrictionDataStorage`,
`RecipeWorkerCounter_CountProducts_Detour`, `RecipeWorkerCounter_GetCarriedCount_Patch`,
`CompPawnOriginalMap`, `PawnExtensions` and pawn spawn/despawn patches.

- Extended bill state contains **CountAway=false**, legacy **Name**, and **ProductAdditionalFilter**;
  saved keys `countAway`, `name`, `productFilter`. CloneFrom copies CountAway/filter and optionally Name.
  The surrounding storage/link/copy lifecycle has **not yet** been fully read.
- Bench restriction state saves enabled flag, named pawn or slave/mech/non-mech choices and skill
  range **0–20**; application chooses named pawn first, then slave, mech, non-mech, any pawn. Skill
  range is assigned for slave/non-mech/any, not named/mech branches. World storage is keyed by bench
  thing ID under the source-spelled `worktableRestrictionDictonary` key. UI/inheritance/removal
  behavior remains to be traced before treating this as a complete gameplay contract.
- CountProducts postfix starts from the native result and adds extra-product and selected default
  product contributions. It considers includeEquipped, non-human carriers, CountAway, selected slot
  group, inventory setting, resource-counter fast-path conditions and loose/minified items. It sums
  actual item stack counts and uses native CountValidThing in several branches. It also has a fast
  loose-resource path excluding invalid position, nonfresh, stored and fogged items.
- Away candidates come from living player-faction pawns across maps/caravans/traveling transporters
  whose recorded OriginMap equals the bill map. Their inventory/carry contents are counted, and worn/
  equipped contents when includeEquipped. This branch precedes selected-storage return logic.
  Exact reachability of choices, filtering, overlap/double-count risks and native 1.7 comparison are
  **research pending**, not yet accepted semantics or diagnosed live bugs.
- `CompPawnOriginalMap` saves an OriginMap reference for player-faction pawns. Leaving a player home
  records that map; spawning at a player home clears it. The installed root XML unconditionally adds
  this comp to BasePawn/comps. Our equivalent, if needed, requires guarded additive XML Extensions
  content checks and required dependency, or a justified narrower owned association design.
- The source GetCarriedCount helper reuses one static list assembled from free colonists and colony
  animals/mechs/player-controlled subhumans. Its actual Harmony registration and consumed native
  call shape still require confirmation; do not infer a functioning patch solely from a helper's code.

## Consumer search and initial concrete bindings

Bounded text/XML/C#/JSON and DLL literal searches across installed Workshop/local mods completed.
Matches include source files, mod-manager metadata/rules, a third-party layout harness's saved mod
lists, **No Max Bills Redux** and selected **Everybody Gets One - Continued**. Catalog/rule/mod-list
matches still need their individual context classification; existence alone is not a behavior consumer.
This is a bounded literal scan, not proof against dynamic or uninstalled bindings.

Read these concrete installed source files:

- Workshop **3530806680**, `ImprovedWorkbenches/BillCopyPaste.cs`: separate
  **SupportImprovedWorkbenches** assembly directly references **ImprovedWorkbenches** and postfixes
  `ExtendedBillDataStorage.MirrorBills`. It clones/imports the source bill's person-counter state into
  the destination. This is a real modification of another selected source. Our adopted S017/S018
  behavior must compose directly with S014's whole-list plan, not retain internal self-patches.
- Workshop **3526216885**, `1.6/Source/NoMaxBillsRedux/NoMaxBillsMimicry.cs` and relevant Core.cs:
  publishes `NoMaxBills.Patch_BillStack_DoListing` so BWM's type probe succeeds, then reflects
  `ImprovedWorkbenches.Main.GetMaxBills()` and replaces its 125 limit. This is a consumed reflected
  type/method contract; exact deployed package/hash and current-version behavior remain to inspect.
- S018's installed 1.7 DLL and older bridge DLLs contain BWM references, but the 1.7 binding has not
  yet been decompiled. A partial read of `Source/BillRepeatTypes.cs` establishes custom repeat-mode
  references only; it is not S018's completed sequential analysis.

## Next bounded work and recorded questions

- **BM-Q1:** read remaining copy/paste/link, bill list/config/navigation and restriction paths; map
  source conveniences against native 1.7, compose whole-list linking and document actual defaults/edges.
- **BM-Q2:** trace product-count UI eligibility, extra filters, away/carrier/dedup branches and native
  counting; specify one shared-data policy and concrete fixes only after source evidence is complete.
- **BM-Q3:** complete save/link/clipboard/removal lifecycle and actual consumer/provider contracts,
  including the installed 1.7 S018 adapter and No Max Bills Redux. No source-save migration or facade
  is promised by initial identity preservation.
- **BM-Q4:** enumerate per-patch semantic targets, guarded XML, settings/UI placement, seven-language
  behavior and focused test/native acceptance work; no code or game run in this research stage.

No user clarification is requested. Complete this source before advancing to S018's own change.

## Completed trace: clipboard, links and persistence

Read installed `BillCopyPaste`, `LinkedBillsSet`, `ExtendedBillDataStorage`, clone/ExposeData patches,
`BillStack_DoListing_Detour`, `Bill_DoInterface_Detour`, `BillConfig_DoWindowContents_Patch`,
`Bill_Production_DoConfigInterface_Detour`, `Bill_Production_RepeatInfoText_Detour`,
`BillRepeatModeUtility_MakeConfigFloatMenu_Detour`, `Bill_LabelCap_Detour`,
`BillConfig_PreClose_Detour`, `Dialog_ThingFilter`, `ITab_Bills_TabUpdate_Detour`, delete/destroy
patches, `Open_Bills_Tab_On_Select`, `Widgets_ButtonImage_Detour`, `Resources`,
`Detours/BillUtility_MakeNewBill_Detour` and English keyed language data.

- Clipboard holds **live Bill_Production references**, not snapshots. Empty-bench Copy returns
  without clearing old clipboard. Destination eligibility prunes deleted sources after its initial
  empty check; an emptied list can pass the remaining loop. Paste creates new recipe bills with
  null style precept, then mirrors choices. It checks capacity per row and can append only the
  portion fitting the limit. Paste Into accepts one other source and preserves destination recipe.
- Public `ExtendedBillDataStorage.MirrorBills(Bill_Production sourceBill, Bill_Production
  destinationBill, bool preserveTargetProduct, bool initialCopy = true)` copies ingredient allowances
  when product is not preserved or fixed allowed Def sets match. It always copies radius, delivery
  and paused state. Destination bench restriction takes precedence; otherwise worker/group choices
  copy initially, or when source bench is unrestricted. Suspended copies only with its preference.
  Count/repeat fields are conditional on a single ordinary product or stone-block/butchery counters;
  output quality/taint/equipped/material fields depend on both output shapes. Extended CountAway/filter
  always copy; legacy name copies only when product is not preserved. Native style/graphic variant
  and native playerCustomName are not copied by this mirror path. These omissions are not a complete
  editable-plan copy contract.
- Source links are lists of physical bill references. Linking appends to the parent's first set;
  no full-list ownership/atomic group merge. Removal from a two-member set removes the set. Mirroring
  happens on bill details draw and hovered-bill TabUpdate; UpdateAllLinkedBills uses first member.
  Overview repeat-mode changes are blocked for linked rows outside Details. They are not the user's
  shared whole-list quantities and must not be retained as internal self-patches.
- World storage saves linked sets and legacy integer-ID extended data. Current extended data is
  attached through Bill_Production.ExposeData; legacy migration looks up private bill loadID and
  clears the legacy dictionary after logging unmatched entries. Native clone copies extended state.
  Deleting production bills removes clipboard/extended/link membership. Destroying worktables
  removes restrictions and bills' extended state. Do not treat that as safe minification or migration
  for our stronger persistence contract; original public shape is recorded, not adopted wholesale.

## Completed trace: interface and worker policy

- Overview adds Copy all, Paste/Paste link, Paste Into, Break link, delivery toggle, row dragging and
  `(resume) current/target` text. Delivery switches any non-floor mode to floor, then to Best storage.
  Whole-row drag marking suppresses native image buttons through a global flag reset by production
  config drawing. Our scoped controls must not suppress unrelated UI or use selection as ownership.
- Details adds previous/next using the currently selected bench's bill stack, casts adjacent rows
  to production bills and does not wrap. Copy/Paste Into/Break link are added there. Owned navigation
  instead follows the dialog bill's actual owner, handles removed/unsupported rows and shared order.
- Restrict workbench checkbox only toggles stored enabled state. Window.PreClose for exact
  Dialog_BillConfig compares its bill worker fields to the currently selected bench's saved policy,
  then copies policy to every bill. MakeNewBill also uses current selection to apply a saved policy.
  This can target the wrong bench if selection changed. Proposed explicit owner-based application
  captures choices when enabled, propagates on accepted edit, preserves choices when disabled and
  becomes group-wide under whole-list linking. Native restrictions remain conjunctive with eligibility.
- Auto-open is **bills and storage**, not bills alone: one selected native worktable, stockpile or
  storage building; preserve a currently open compatible tab, else choose Storage/Bills. The source
  blanket-skips storage buildings if an active display name equals LWM's Deep Storage. We do not
  copy display-name detection or disable every native shelf merely because a provider is present.
- Source default-drop postfix affects newly made Bill_Production, not existing bills. Native 1.7
  already implements rename through playerCustomName/IRenameable and its Details button; no second
  rename field/UI is needed. Several older translation keys (installed counts, extra numeric UI,
  filter counters) have no current implementation in this inspected version and are not evidence
  for extra active features.

## Completed trace: counting and native 1.7 comparison

Count controls appear only for source single-product/no-special-products target bills. CountAway is
**not gated by Include equipped in the actual UI**, despite older description wording. Additional
filter starts with an all-types parent filter, suppresses special filters and does not expose its own
HP/quality ranges; it uses the bill's ranges. OK with no allowed types clears it; closing without OK
discards the local edit and reopens bill details.

Native inputs reused from S009/S014: `Verse.RecipeWorkerCounter`, `RimWorld.ResourceCounter`,
`Bill`, `Bill_Production`, `BillStack`, `ITab_Bills`, `Dialog_BillConfig`, `BillUtility`, inspected
**1.7.4995 rev309**, Assembly-CSharp SHA-256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**.

- Native GetCarriedCount iterates **SpawnedPawnsInFaction(Faction.OfPlayer)**, already including
  nonhuman direct carriers. BWM's helper expects a FreeColonistsSpawned call absent here. Its method
  also lacks a transpiler annotation/conventional name and searches a private helper with default
  GetMethod; these are static registration/shape concerns, not a proven live 1.6 bug. No such legacy
  carrier rewrite is needed on inspected 1.7.
- Native primary counting has resource-counter and detailed branches. ResourceCounter sums fresh,
  unfogged storage contents and updates every **204 ticks** plus explicit update paths. Detailed
  counting includes map items, minified contents, direct carry and haul sources, or selected storage.
  Include equipped additionally includes free-colonist equipment, apparel **and inventory**, even
  for selected storage. Native CountValidThings(List) increments once per qualifying Thing; native
  container/other paths sum stacks. Our explicit units contract corrects this branch discrepancy
  rather than making stacked additional products use a different unit.
- BWM adds primary extension twice if primary is also in ProductAdditionalFilter. With Include
  equipped, nonhuman extra-product equipment/apparel is visited in both AllPawnsToCount and
  AllNonHumanPawnsToCount. Inventory option also adds direct carry to a native result already
  including it in map scope. Resource detailed/minified additions can overlap native enumerations.
  These are source-level overlapping paths; no live symptom is claimed. The replacement computes
  one policy-consistent union, never baseline-plus-unguarded-extra sums.
- Loose resource source fast path checks freshness/fog; other filtered paths differ. Native detailed
  branch can include loose resource goods even when source preference is off. Draft explicitly
  normalizes that preference and resource freshness across all paths. This is a **proposed deviation**,
  not a claimed unchanged native rule. Likewise normalize physical units and minified multiplicity.
- Origin is recorded on despawn from a player home and cleared on spawn at any player home.
  Source away branch considers living player pawns with that origin before selected-storage return;
  inventory/carry count regardless of local CountInventory/nonhuman preferences, and worn/equipped
  only with Include equipped. Draft preserves that deliberate away override, while defining home
  transfer, invalid origins, death/faction and unavailable-container boundaries explicitly. Exact
  native holder/PawnsFinder coverage still needs implementation proof, especially cryptosleep.

## Concrete consumers and boundaries after binary inspection

| Input | Actual binding / result | Adopted boundary |
| --- | --- | --- |
| No Max Bills: Redux, `justharry.nomaxbillsredux`, Workshop 3526216885, About 1.1.0/1.6 | DLL SHA-256 FB9CF68F86D56533962702449719BE08E82529B2CECB4FDBE6C95B43A0BB5A76; decompiled reflected Main.GetMaxBills and 125-to-int.MaxValue transpiler match inspected source. Also exposes NoMaxBills type probe. | Real reflected contract; absent target is caught. No fake Main/125 sentinel. A supported adapter must make browser/list/paste/link capacity coherent; currently fifteen, provider beta acceptance open. |
| EGO older support assembly/source | Assembly-bound MirrorBills postfix clones person-counter state; conditional LoadFolders for falconne.BWM. | Selected modifier behavior must compose directly in S018; no package spoof to load an obsolete bridge. |
| EGO installed 1.7 main DLL | SHA-256 51259BBC102B2566066CFC956A9B7C6E7F74CB686F0433B33E2FF4A927C8C433. ImprovedWorkbenches_Patch probes short type BillConfig_DoWindowContents_Patch and **public static DrawFilters(Dialog_BillConfig __instance, Rect inRect)**, present in inspected BWM. OrPersonCount_Transpiler with noSurplus=true extends the TargetCount condition to TD_PersonCount and TD_XPerPerson, excluding TD_WithSurplusIng. 1.7 LoadFolders also names a conditional 1.7/ImprovedWorkbenches folder, but the installed 1.7 inventory contains only the main assembly. | Concrete UI-modification contract to compose directly in S018 after its mode analysis. Beta execution and the separately missing old MirrorBills bridge remain unverified. |
| Mod-manager db / communityRules, Workshop 1847679158 | Catalog entries for source/forks/translations; old uuugggg.everybodygetsone loadAfter falconne.bwm rule | Metadata ordering/catalog, not a direct API consumer. |
| LayoutHarness saved modlists, Workshop 3745434121 | falconne.bwm entries in rimsort_1/2/3.xml and sample_s2_03_n250.xml | Test/mod-list references, not a callable consumer. |

Consumer decompilations are ignored artifacts under `consumers/no-max-bills` and `consumers/everybody-17`.
Independent review caught and corrected an initial false claim that DrawFilters was absent. The
actual declaration and EGO's OrPersonCount_Transpiler helper were re-read; no runtime compatibility
claim follows from the now-correct source binding.
Outfitter/RimFactory/Colony Groups are source's optional calls outward; they are not evidence that those
providers consume our proposed replacement. Canonical identity/shape/actual beta workflow remains
BM-Q3 before any adapter claim. Source identity and source serialization are not automatically preserved.

## Draft decisions, explicitly distinct from source facts

Confirmed user requirements: whole-list synchronization, direct composition of selected add-ons,
observable specs, shared data, common settings, semantic patch admission and contextual UI routing.
Proposed refinements: copy-time snapshots, atomic full paste with previews, complete applicable
same-recipe choices, explicit cross-recipe transfer summary, actual-owner navigation/restrictions,
group-wide worker policy, unique unit totals, uniform resource scope/freshness and missing-choice recovery.
These improve source-derived behavior but have **not** received separate user approval or runtime proof.

Retain six meaningful source preferences, with nonhuman preference renamed/scoped to extra inventories
and equipment because native direct carrying is already broad. Omit mirrorSuspendedStatus: the user
requires full synchronization. Settings use common global/save inheritance; no source settings import.
Owned saved home associations avoid the source's unconditional BasePawn comp patch; no XML mutation
is currently planned. If that decision changes, required XML Extensions and merge-preserving guards
must be specified before code.

Outstanding work now:
- **BM-Q1:** validate proposed clipboard/transfer UX and exact 1.7 writable-choice/active-work boundaries;
  source behavior is now traced, not a request for the user to explain it.
- **BM-Q2:** prove unique stack/minified/container/away enumeration and freshness events, including
  cryptosleep, traveling transporters and other-map transitions; benchmark shared data with search/hauling.
- **BM-Q3:** inspect canonical optional providers and supported current contracts; S018 owns full
  person-count analysis. Source-save migration and a source-type facade remain deliberately unpromised.
- **BM-Q4:** turn target families into executable semantic guards, run focused RED/GREEN, independent
  code review, seven-language UI and final minimized native workflows. No implementation task is complete.
