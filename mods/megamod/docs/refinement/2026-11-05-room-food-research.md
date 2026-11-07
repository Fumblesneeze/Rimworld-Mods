# S111 — Room Food research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Origin **replacement**. Inspected **2026-11-05**. Documentation/source research only; no gameplay acceptance.
[Change](../../../../openspec/changes/specify-megamod-room-food/) and [coverage](../replacements/room-food.md).

## Public documentation first and exact provenance

Read [Workshop3530729319](https://steamcommunity.com/sharedfiles/filedetails/?id=3530729319) first,
then its linked [author repository](https://github.com/TheLordDrake/RimWorld-RoomFood). The author
describes choosing food placed in a nearby dining room and using food in a patient's/prisoner's
room. The public page suggests RimFridge, not a required dependency. Its reply about Vanilla
Nutrient Paste Expanded explicitly says it was not tested; that is not compatibility acceptance.

- Installed title **Room Food**, author **TheLordDrake**, original by **Uuugggg**; package
  **TheLordDrake.RoomFood**, Workshop **3530729319**, advertised **1.6 only**, required Harmony.
  Installed root `F:/Steam/steamapps/workshop/content/294100/3530729319`.
- Selected unversioned `Assemblies/Room_Food.dll`: **9,216 bytes**, SHA-256
  `91175F0AA1A7BF3EE612FDB7004546EAFB45B27F1BBBE37A196ADDB45E58EBEB`;
  assembly/file version **1.0.0.0**, assembly name **Room_Food**, MVID
  **ae14d135-551d-461a-a5b0-a7089dcefa03**. Configuration attribute is empty; debug log call sites
  are absent in the selected binary. This is not a1.7 build/support claim.
- Public master commit **feb77b45d98dc578462ba9fded8a157db41a5fd8**, **2025-07-20T18:02:51Z**.
  All four runtime source files and all selected runtime classes were read and compared. The
  compiled behavior agrees with the public source after absent conditional debug calls are allowed.
  No public DLL or reproducible-build identity is claimed.
- Public project targets net4.7.2 with Krafs.Rimworld.Ref1.6.4523, publicizer2.1.0 and Harmony2.*
  excluded from runtime assets. Public development About still identifies `DEBUuugggg.RoomFood`
  and1.5; release About identifies `Uuugggg.RoomFood` through1.5. Public LoadFolders ends at1.5 and
  README links the original [Workshop1339148170](https://steamcommunity.com/sharedfiles/filedetails/?id=1339148170).
  Installed About/PublishedFileId, not stale public packaging, establish this continued source's identity.
- Installed package contains About/preview/publication ID, one assembly, one English keyed file and
  a mod icon. No installed LoadFolders, Defs, XML patches, source tree or license file. No license
  file appears in the inspected public tree either; public access is not permission to reuse code/art.
  Independently implement the observable behavior and retain source attribution.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/room-food/`. Retains Workshop HTML,
public commit/tree and raw text manifest, selected complete binary decompilation, assembly metadata,
six source/native input identities, seven native type extracts and bounded installed consumer search.
No target assembly was executed; installed content, normal saves/configuration and assets were not changed.

## Complete behavior inventory

| Source file / class | Actual behavior | Selected owned treatment |
| --- | --- | --- |
| FoodFinder.Postfix | HarmonyPriority.Last (0) postfix on SpawnedFoodSearchInnerScan; replaces a non-null or null result only when FindRoomFood returns a Thing | Guarded ordinary food-selection composition, not an unguarded postfix copy |
| FindRoomFood eligibility | Getter = traverseParams.pawn or eater; getter.IsFreeColonist and humanlike eater; map taken from eater | Preserve scope; explicitly require current same-map participants and ordinary request context |
| Different getter/eater | Uses eater.GetDistrict; rejects missing district/room and IsHuge; requires no table/hospital role | Recipient's whole enclosed room; ordinary bedroom and prisoner care included |
| Same getter/eater | Base radius maxDistanceToTable; expand to Euclidean root-to-existing-result distance if greater | Retain default/range and one-time expansion, clearly explained |
| Table test | Any artificial Building with SurfaceType.Eat, Danger.None, non-huge/non-prison room and any FoodSourceNotPlantOrTree in its district | Current allowed reachable eating surface with actual eligible food, not just a broad lister hit |
| Table discovery | ClosestThingReachable(root,map,BuildingArtificial,OnCell,traverseParams,radius,tableValidator) | Nearest qualifying safe permitted surface, stable tie, current native access semantics |
| Food discovery | Concatenate selected district regions' FoodSourceNotPlantOrTree; ClosestThing_Global centered on eater with caller validator and FoodOptimality based on root Manhattan distance | Whole room, preserve supplied domain, actual getter reachability and request limits; serving-side location for supported dispensers |
| Mod | Verse.Mod settings, Harmony owner Uuugggg.rimworld.Room_Food.main, PatchAll; category TD.RoomFood | Owned guarded registration and common settings; do not impersonate source owner |
| Settings | Global int maxDistanceToTable =100; slider1–200 cast to int; Scribe key maxDistanceToTable default100 | Shared inherited global/save row; no automatic source configuration import |
| DebugLog / Log | Conditional DEBUG wrapper over Verse.Log.Message; debug source constructor enables Harmony.DEBUG | No adopted global debug logging switch |
| English AutoEnglish | Only TD.RoomFood; distance slider text is hardcoded English | All seven baseline languages through common UI |
| Compiler support attribute | IgnoresAccessChecksToAttribute generated for publicized references | Build artifact, not a gameplay contract or API to copy |

Source uses highest FoodOptimality inside the district, with nearer eater distance as a tie-breaker
through GenClosest. It is not simply nearest-food selection. Table selection stops at the first
table whose district has any broad food-source entry: if none passes the actual validator, the
source returns the original result rather than trying another table. No persistent food cache,
save component, custom job, bottom tab, Def or runtime optional-mod adapter was found.

## Current native1.7 facts and concrete gaps

Game **1.7.5000 rev1275**. `Assembly-CSharp.dll`: **15,636,480 bytes**, SHA-256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
`Version.txt`:18 bytes, SHA-256
`BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A`.
Static research only; method presence in this beta does not admit the source's1.6 patch.

| Inspected native contract | Consequence |
| --- | --- |
| FoodUtility.SpawnedFoodSearchInnerScan(Pawn,IntVec3,List<Thing>,PathEndMode,TraverseParms,float,Predicate<Thing>) | Candidate domain is explicit; Manhattan root-distance bound, FoodOptimality, pawn.Map.reachability.CanReach, Spawned and validator are separate checks |
| FoodUtility.BestFoodSourceOnMap | Builds food permission predicate; source request includes/excludes plants; humanlike/mech branch uses inner scan; later allowed harvest may replace the selected source; assigns actual final ingestible Def |
| Ordinary food predicate | Checks eater food/diet/title rules, nutrition, freshness/corpse/drug permissions, forbidden/social propriety and quantities/reservations; generic non-dispenser path does not itself perform CanReach |
| Native dispenser predicate | Checks allowed/full/empty policy, manipulation, paste eligibility, faction, forbidden, power, feedstock where required, standable interaction cell, social propriety and interaction reachability |
| TryFindBestFoodSourceFor | Early non-player/soon-rotting carried-food returns; map search; personal/pack inventory comparison and fallbacks; never promise map room food overrides all inventory choices |
| JobGiver_GetFood | Normal Ingest, dispenser refill/refallback and TakeFromOtherInventory paths remain native; a failed empty dispenser refill can request another map search with dispensers excluded |
| WorkGiver_FeedPatient | Native recipient-care gates; canRefillDispenser false, inventory/pack access allowed, corpses allowed, allowVenerated true with ordinary two-pass handling |
| WorkGiver_Warden_Feed | Native prisoner eligibility; canRefillDispenser false, inventory allowed, pack inventory false, corpses false; creates native FeedPatient job |
| GenClosest.ClosestThing_Global | Checks spawn/haulable inventory state, Euclidean center-distance, validator and priority; does not independently ensure reachability |
| Room / District | Room has multiple districts/regions; IsHuge is RegionCount>60. ProperRoom excludes map-edge-connected space and requires a normal region; it is not a roof requirement |

Concrete source-derived risks, not observed live bug reports:

1. The postfix neither receives nor preserves `searchSet`/`peMode`. Broad district enumeration can
   admit a source intentionally excluded by the caller; the passed predicate is not the whole contract.
2. A reachable table plus accepted generic food predicate does not establish access to the chosen
   food. The replacement search omits the inner scan's independent path check.
3. Nearest table with unusable food masks later useful rooms. Whole-room intent is narrowed to one
   district; footprint-based room lookup can miss wall-serving sources. More than60 engine regions
   disables even a valid large room, while a small outdoor region can pass the source's non-huge test.
4. Source food range uses eater-centered Euclidean distance while native inner scan uses getter-root
   Manhattan distance. These differ for caregiver jobs and limited callers.
5. The public description does not describe inventory early-return/outer comparison or later harvest
   handling. Preserve those boundaries instead of promising every meal always comes from a room.

## Owned choices, composition and semantic playability

RF01–RF08 specify the adopted behavior and deliberate corrections. Whole current enclosed room,
actual eligibility before choosing a room, preserved candidate/path/radius constraints and native
serving-side location are proposed corrections, not claims that upstream already does them. Table
distance100/range1–200 and one-time extension to already found farther food are source-derived.
New invalid-value recovery100 and shared inheritance are explicit owned choices. Source global
configuration is untouched; no pawn/entity settings are added.

- **S024 Common Sense:** current-spoilage contribution occurs once within the room comparison.
- **S057 Preserved Food:** its finite food preference still applies within the selected room; room
  priority can deliberately choose stocked preserved food over a better fresh meal outside. Native
  early inventory and later comparison stay intact. Trader provisions and caravan preferences are unrelated.
- **S055 Meals On Wheels:** borrowing remains after ordinary selection, including room preference,
  returns no source. A successful room result is not displaced by another pawn's inventory.
- **S021/S041/S123 and shared Ctrl+F data:** query shared current facts/native listers; no independent
  stock count, cargo split, capacity exemption, inventory refilling or new scan/cache per port.
- **Medical features / Drug Response:** this is nutrition selection, not a medicine or hemogen policy.
  Patient and warden permission differences remain meaningful. No assumed conflict or merged drug UI.

Comparator review: native map search preserves all actual access/food gates but does not provide
room priority; source1.6 provides priority but narrows room data and omits independent access checks;
owned S024/S057 contribute food scores rather than room selection. The selected combination changes
which existing food is used, not production costs, research, nutrition, mood, spoilage, portion sizes
or hauling capacity. It can trade food quality/freshness for deliberate local stocking. Measure travel
and food-choice outcomes before claiming a performance or playability improvement. No finalized Def,
live job or runtime compatibility acceptance is inferred from this static semantic comparison.

## External contracts, coexistence and UI

Read-only installed search across `.cs`, `.xml`, `.json` and DLL byte strings for Room_Food and both
RoomFood IDs found the source itself, a Workshop database entry, a replacement catalogue and four
layout-harness mod lists. Exact hits/hashes are retained. These are package metadata/list consumers,
not demonstrated calls to FoodFinder or dependencies on a source assembly facade. Reflection,
obfuscation, noninstalled consumers and other encodings remain outside this bounded negative result.

Public `Room_Food.Mod.settings`, Settings.maxDistanceToTable and FoodFinder.FindRoomFood are possible
surface area, not evidence of an actual consumer. Do not preserve speculative facades. Source global
Scribe key is not an owned save migration. No source runtime save type was found.

| Source surface | Owned decision |
| --- | --- |
| Room Food mod-settings category / distance slider | Common Food settings row, Global default/Current save/Use global, translated explanation including extension |
| Automatic food choice | Existing self-feeding and caregiver jobs; no new tab, gizmo or toggle |
| Original package/Harmony owner | Active original/continued/dev provider disables owned overlap with warning; never edit/unpatch source or spoof ID |
| RimFridge suggestion / untested paste-network comment | Neither is required or accepted as compatible. Native exposed food works only through actual native contracts; special providers need independent shape/access inspection |
| Source settings file and icon | No automatic import, no copy or selection of source art |

Harmony is required with actual future patch use, never bundled. No XML change is selected; future
XML Extensions usage requires dependency and guarded merge-preserving operations. Product has no
hard DLC, Room Food, refrigerator, network or Gateway dependency. Every future patch requires its
own executable semantic target predicate and bounded failure behavior as documented in design.

## Open questions and acceptance state

| ID | Scope | Remaining work |
| --- | --- | --- |
| RF-Q1 | Effective targets and providers | Implementation beta hooks/callers, source domain/path semantics, enclosure/serving boundaries, optional contracts and later repatches |
| RF-Q2 | Composition and cost | Direct S024/S055/S057/S123 selection, shared data, representative travel/food-choice/update/memory measurements |
| RF-Q3 | Verification | Focused TDD, common settings in seven languages, source/optional-provider cases and reviewed-build native feeding workflows |

All runtime tasks remain unchecked. Independent behavior/composition and provenance reviews found
no issues; strict typed validation passed133 items with zero failures. The refinement ledger records
the checkpoint and evidence checks. No code, build, installation, game launch, source
settings mutation, image work, commit or publication is part of this draft.
