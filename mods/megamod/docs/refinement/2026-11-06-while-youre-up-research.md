# S125 — While You're Up full-source refinement

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Research2026-11-06, documentation only. This completes the full-source pass following the
[narrow S021 intersection](2026-11-03-while-youre-up-hauling-intersection.md). No native gameplay,
performance, source binary compatibility or completed implementation is claimed.

## Provenance and reading order

Read the selected [modern Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3832180338)
and installed About/English documentation first, then the [original Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2034960453)
and [public source](https://github.com/CodeOptimist/rimworld-while-youre-up). The web reader could not
open the modern page; a direct read-only HTML fetch succeeded and is retained locally. Installed
DLL decompilation fills behavior gaps; public descriptions are claims, not beta acceptance.

| Input | Identity |
| --- | --- |
| Selected port | While You're Up/顺路搬运, xiaopu, `patch1.7.whileyoureup`, Workshop3832180338; declares1.6/1.7, Harmony required; no About modVersion |
| Selected DLL | `1.7/Assemblies/WhileYoureUp.dll`,112128bytes, SHA256 `867B0C299677621C03888204FDD1045DBF64EBA257FBBBAA38F42C4647F2E1F2`, assembly1.7.0.0 |
| Selected About |11267bytes, SHA256 `D0B24A68FD7D3CCE2391C6629C88C87F8DE48481B9D2AC6FB5D7B76017854F54` |
| LoadFolders | root plus matching1.6/1.7 branch; SHA256 `CB32EDECA49B44725B8628E25C6FDC7514D992EA598B638E1C9581D55A08E7F2` |
| Original | CodeOptimist/Christopher S. Galpin, `CodeOptimist.JobsOfOpportunity`,2034960453; installed1.5DLL113152bytes, SHA256 `2252ED2E24B5A7C9D5A43247278A76A01E37933E0C021B109A602CBCE86133E2` |
| Public provenance | Commit `1c54e7c31e2d3d167a94e3723891a6bf7cdfe536`;8Source files plus LICENSE/NOTICE retained; not represented as identical to selected port |
| License/credit | Original AGPL3.0-or-later NOTICE; port credits original and3516529053 temporary1.6 patch. Independent behavior specification only; no upstream code/art/DLL copied into product |

Local inputs/manifests and modern HTML: `artifacts/Refinement/megamod-spec-20261106/while-youre-up/`.
The unchanged selected DLL permits reuse of its earlier read-only extraction under
`artifacts/Refinement/megamod-spec-20261103/while-youre-up/installed-project/`.
Read all main `WhileYoureUp/Mod.cs` behavior/settings/patch branches, `ModCompat`, filter/reverse-patch
and loading-context helpers, GUI numeric/enum helpers, transpiler insertion/finalization, weak
dictionary and deferred cleanup helpers. Remaining generic formatting/diagnostic helpers carry no
additional selected gameplay feature; do not claim every upstream library method is a product API.
Public source is retained for provenance/comparison, not substituted for the selected DLL.

Native1.7 `Pawn_JobTracker.TryOpportunisticJob` was read from the retained food-tracker native extract;
`WorkGiver_ConstructDeliverResources` freshly extracted offline in this evidence directory. Current
game identity is recorded in the evidence manifest. Native selection already has age/work/player/
forced/target gates and30/.5,50/.6,1.7/1.0,25-region thresholds. It can route some non-slot containers;
the selected port's custom storage loop scans slot groups, so broader native container support is
not automatically a feature of this port. Construction has explicit forced/resource/enroute and
cleanup handling that an owned hook must preserve.

## Source behavior inventory

| Surface | Inspected behavior and boundaries |
| --- | --- |
| General hook | Replaces native candidate search after existing prefix gates when Enabled; no current detour/legacy cargo; excludes four caravan-preparation jobs and bleed rate>.001 |
| Pending target | Normally targetA; DoBill uses its first targetQueueB item where present. A bill-supply attempt is tried before general bleeding exclusion |
| Search | Native haulables order; start/item and store/job proximity gates; ×2 soft expansion in Default/Pathfinding, hard limits in Vanilla; failed final route stops Default but Pathfinding continues |
| Budget | Geometric checks plus selected region or path check;1.7 total and1.0 added legs; actual path implementation treats TotalCost0 as failed without checking same-cell success |
| Destination | Descending storage priority; candidate cells near integer midpoint of resource and target; partial randomized cell inspection when accurate search requested; stockpile/type filters |
| Construction | Hook after FindAvailableNearbyResources; largest available resource stack, work-tag check, current need; without PUAH require stack greater than need; then common supply detour |
| Bill supply | Selected queued ingredient stack, surplus without PUAH, automatic haul check; common supply detour; pre-existing inventory resource rejected |
| Supply detour | At least2units fit mass; store strictly nearer work by squared straight-line distance; optional PUAH job or native HaulToCell fallback; no actual whole-supply-route budget |
| Equal priority | Same-priority different SlotGroup allowed; source temporarily lowers source storage priority getter and appends its goods to haulable candidates while PUAH plans |
| Extra goods | Supply batch uses same selected SlotGroup. Equal-priority/bleeding additionally checks current frame need or DoBill queued Defs; ordinary higher-priority supply batch can include unrelated goods |
| Inventory opportunities | Integrates only legacy PUAH shapes; sums pickup legs, first-planned delivery then nearest successive destinations and return; total budget checked as batch grows |
| Delivery | Nearest remembered destination, grouped by physical SlotGroup, then category-index/defName ordering; destination memory per ThingDef; stale cargo reference can substitute entire first same-Def inventory stack |
| Replacement store | Same SlotGroup automatically allowed; different group tested against new relative straight-line budget; default dumpIfStoreFilledAndAltsInopportune=true may cause external PUAH drop |
| Lifecycle | Weak per-pawn detour table; clear on finish/queue clear/scanner check, remove on pawn destroy; inventory opportunity cooldown5 rendered frames, same-frame loop marker; no serialized opportunity lifetime |
| Reports | Native hauling report augmented with on-way-to/closer-to target, inventory delivery described as efficient |
| Capacity extension | Optional port addition, off/default1, UI1–10; substitutes first35f in MassUtility.Capacity. It does not consult main Enabled and affects other users of capacity |

The original description's grouped unloading and route claims are useful intent; source per-Def
memory, first-destination budget approximation, unknown shape checks and partial-stream risks do
not prove these properties for every item/provider. The earlier intersection already identified
mixed personal/cargo and carry-limit risks; S021's corrections remain authoritative.

## Configuration, UI, persistence and helper audit

Source settings are global ModSettings, not per-save. Internal pages are Opportunity, Advanced,
Supplies & Ingredients, optional PUAH and the port's Carry Capacity. There is no source MainButtonDef
or injected bottom gameplay tab. Numeric helper ranges are float0–999 and percent0–10 (0–1000%);
integer entry has no corresponding explicit validated maximum. Advanced-page visibility does not
turn threshold application on/off. Selected source ships English, German, Japanese, Korean and two
Chinese variants; this does not meet the owning product's seven-language requirement by itself.

| Source key | Built-in default |
| --- | --- |
| Enabled / UsePickUpAndHaulPlus |true /true |
| DrawSpecialHauls / Opportunity_TweakVanilla |false /false |
| Opportunity_PathChecker |Default |
| MaxStartToThing / MaxStartToThingPctOrigTrip |30 /.5 |
| MaxStoreToJob / MaxStoreToJobPctOrigTrip |50 /.6 |
| MaxTotalTripPctOrigTrip / MaxNewLegsPctOrigTrip |1.7 /1.0 |
| Two RegionLookCount values |25 each |
| Opportunity_ToStockpiles / Opportunity_AutoBuildings |true /true |
| HaulBeforeCarry_Supplies / Bills / ToEqualPriority |true /true /true |
| HaulBeforeCarry_ToStockpiles / AutoBuildings |true /true |
| Bills_NeedsInitForCs |true; initialization/foreign-setting coordination flag |
| CarryCapacityMultiplier_Enabled / Multiplier |false /1 |

Filters enumerate Building_Storage and concrete subclasses, group by ModContentPack display name,
then find providers again by that name. General automatic allows all except `lwm.deepstorage`.
Supply automatic allows only these source package families:

`jangodsoul.simplestorage.ref`, `ogliss.thewhitecrayon.quarry`, `rimfridge.kv.rw`, `ludeon.rimworld`,
`lwm.deepstorage`, `mlie.displaycases`, `mlie.eggincubator`, `sixdd.littlestorage2`,
`mlie.extendedstorage`, `mlie.tobesdiningroom`, `mlie.fireextinguisher`, `solaris.furniturebase`,
`skullywag.extendedstorage`, `vanillaexpanded.vfespacer`, `buddy1913.expandedstorageboxes`,
`im.skye.rimworld.deepstorageplus`, `jangodsoul.simplestorage`, `mlie.functionalvanillaexpandedprops`,
`primitivestorage.velcroboy333`, `proxyer.smallshelf`, `vanillaexpanded.vfecore`,
`vanillaexpanded.vfeart`, `vanillaexpanded.vfefarming`, `vanillaexpanded.vfesecurity`.

Automatic displays a dummy copy rebuilt each draw; clicking it has no durable effect. Manual uses
the stored filter; returning after mod changes retains its previous choices. Missing filters seed
defaults. The opportunity search widget is not visibly assigned the separate filter passed to its
tree, while the supply widget explicitly is. Owned lists must have working search and visibly
read-only automatic rows, without preserving that wiring discrepancy.

NonThingFilter/Listing_TreeNonThingFilter are native ThingFilter/tree reverse-patch substitutes;
the latter bypasses hidden-item handling and matches building labels as strings. LoadingContext
temporarily changes Scribe mode and suppresses any `Could not load reference to` error while active;
its Log patch stays installed. No general suppression of missing-reference errors is adopted.
Owned stable identities retain missing selections visibly instead. Source reset invokes ExposeData
in inactive mode, resets widgets and rebuilding defaults; scoped owned reset must be explicit.

Debug visualization syncs with native drawOpportunisticJobs on settings load/save/dialog construction.
Colors are red/green general, magenta/cyan supply, yellow rejected alternative; selected source draws
600-tick flashes, sometimes repeated three times. Owned developer controls retain useful meaning,
not repeated drawing or persisted global debug-state coupling. Main Enabled becomes a current-play
bottom-right toggle with a global-only initial default through the common renderer. It seeds new
games and existing saves lacking operational state; a per-save seed after initialization would have
no effect, so this scope exception is explicit. All other configuration uses common inheritance rows.

## External contracts and concrete consumers

Fresh bounded installed C#/XML search excluding both While You're Up packages found no external text
matches for their package/type names. DLL metadata-string search found SmartMedicine and
VehicleMapFramework; their retained bodies were inspected, then freshly extracted consumer types
confirmed the same bindings (Smart Medicine1.7 and Vehicle Map Framework1.6). This search is
bounded, not a universal absence claim or a complete runtime provider matrix.

| Consumer/provider | Actual binding | Owned decision |
| --- | --- | --- |
| Selected port→legacy PUAH | CompHauledToInventory/takenToInventory/GetHashSet; WorkGiver HasJobOnThing/JobOnThing/TryFindBestBetterStoreCellFor/AllocateThingAtStoreTarget or Cell/skipCells; inventory/unload drivers and two named Defs | Continued has candidate names; Reworked/Armful do not. Name presence is not semantic proof; no legacy HashSet facade over exact owned portions |
| Selected port→Common Sense | Reflected CommonSense.Settings.hauling_over_bills; settings initialization/draw can disable/write the other mod's choice | One owned bill decision; external provider wins affected scope until cooperation; never edit foreign settings |
| Selected port→HugsLib | Dialog_VanillaModSettings constructor, selectedMod and DoWindowContents, otherwise native settings equivalents | No requirement for HugsLib; common owned settings UI |
| Smart Medicine Continued3526251680 | FixWhileYoureUp looks for WhileYoureUp.Utils.MyDistance and replaces an unspawned target with PositionHeld | That Utils type is absent from selected port. Historical hook is not an active consumer of its Mod methods; native holder-aware positions still required |
| Vehicle Map Framework3426502333 | ModCompat recognizes CodeOptimist.JobsOfOpportunity or zsbk.patch16.whileyoureup; category patch targets Mod.TryFindBestBetterStoreCellFor_MidwayToTarget, prefix changes map to thing.MapHeld | Selected patch1.7 ID not recognized there; map substitution alone doesn't prove cross-map route support. No spoofing; own optional adapter later |
| Old save/settings names | JobsOfOpportunity.ModCompat.Settings derives Mod.Settings; global labels/filter keys | Source migration alias, not a broad public API. No automatic import or removal-safe-save claim |

Source `PatchAll` is unguarded. Construction's12-instruction local-load search falls back to the
nearest load even when expected type is unproved; late injection failure returns GetFinalCodes,
which includes already queued insertions. Transpiler helper mutates labels and promotes branches;
it does not establish atomic admission. Other PUAH call-stack/cache cleanup uses postfix rather
than guaranteed finally. These are inspected risks, not a claim that a specific live crash occurred.
Every owned actual patch instead needs a semantic target document, incoming-stream validation,
atomic groups and bounded startup warnings. Hashes here only identify inputs.

## Selected differences and composition

The new [OpenSpec change](../../../../openspec/changes/specify-megamod-opportunistic-hauling/)
keeps useful routing/search/default intent, but explicitly changes:

- Shared S021 exact quantities, current physical destination space, actual capacity and grouped
  delivery replace per-Def memory, source HashSet substitution and extra allowances.
- One saved parent-attempt lifetime replaces frame counters; finite search handles zero limits;
  both purpose branches respect bleeding, forced/queued work and emergencies.
- Every actual delivery/replacement cell counts against the same budget; supply has a proposed
  whole-trip budget and useful-surplus test. The source supplies branch lacks these complete bounds.
- Same-priority movement is explicit and only for permitted pending-work supplies. Active S021 is
  reconciled; ordinary hauling remains strictly better-storage. No temporary priority mutation.
- Active S024 adopts the single combined bill preference and S125 resource-to-work route benefit,
  replacing its source room/worker-distance gate; its ingredient collection and bill authority remain.
- Automatic destination policy uses supported capacity/handling instead of a frozen24-package list.
  Manual choices stay stable and cannot bypass unknown storage contracts.
- UI follows common global/save, bottom-right operational toggle and top debug decision tree.
  No capacity multiplier, foreign settings writes, automatic source import or source code reuse.

## Questions and acceptance still open

WY-Q1: full-source research now drafted; exact beta effective-hook/parent continuation semantics and
all patch target documents remain implementation research. WY-Q2: actual optional provider support,
external contract/facade scope, non-slot/cross-level routing and package metadata. WY-Q3: finite
search cost, zero-path behavior, lifecycle invalidation and shared-index performance. WY-Q4: playability
of source defaults plus proposed supply surplus/budgets, manual storage defaults and full scope UI.
WY-Q5: personally observed native action/result evidence for selected modes, supply/equal-priority,
interruption/load, provider-present/absent and seven languages. No user interview is needed now.

Semantic pass: no new resource, capacity, work ability, research bypass or consumption discount.
Relocation spends movement and handling to reduce future trips; it cannot indefinitely postpone
the pending bill or substitute its materials. Compare actual native opportunistic hauling, S021
ordinary batching and selected source behavior with equivalent trips before accepting savings.
This document's static pass does not satisfy that live balance/performance gate.
