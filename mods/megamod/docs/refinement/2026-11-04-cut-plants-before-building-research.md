# S063 — Cut plants before building (Continued) research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Inspected **2026-11-04**, sequentially after S062. This is static source/native research, not a live
reproduction or compatibility acceptance. No gameplay implementation, source build or game launch.
[Proposal](../../../../openspec/changes/specify-megamod-construction-plant-clearing/proposal.md),
[design](../../../../openspec/changes/specify-megamod-construction-plant-clearing/design.md) and
[observable contract](../../../../openspec/changes/specify-megamod-construction-plant-clearing/specs/megamod-construction-plant-clearing/spec.md)
separate intent, implementation analysis and selected behavior.

## Documentation first and provenance

Attempted the [continued Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3286376165)
and [changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3286376165) first. The
description returned a region-unavailable Community Hub and the changelog could not be retrieved;
an English-parameter retry did not supply the page. Successful primary fallbacks were the
[original Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=1539025677),
[public repository](https://github.com/emipa606/CutPlantsBeforeBuilding), and installed/public README
and About text, read before source analysis. No full continued changelog history is claimed.

Documentation advertises advance orders at construction, installation and roof sites, a corner
toggle with Backslash, special-tree exclusion, default state, optional digging-up and a Plant Cutting
worker restriction. Source also exposes a notice-suppression preference. Public compatibility banners
and generic removed-item warnings are not runtime evidence. The cached public web view was older
than the local repository revision; the selected installed artifacts determine the analyzed version.

| Input | Exact inspected identity |
| --- | --- |
| Continued provider | `Mlie.CutPlantsBeforeBuilding`, Workshop3286376165; About supports1.0–1.7 and requires/loads after Harmony |
| Original provider | `tammybee.cutplantsbeforebuilding`, Workshop1539025677, tammybee / 民火; verified in retained Source/About.xml; B19 item1232208162 is historical provenance only |
| Installed selection | `F:/Steam/steamapps/workshop/content/294100/3286376165/1.7/Assemblies/CutPlantsBeforeBuilding.dll`; no LoadFolders.xml exists |
| Version | Mod/file1.7.0, assembly1.7.0.0, informational `1.0.0+45e0d02ba42e4680e79ffe073155d8bd0d5c0a37` |
| Selected DLL SHA256 | `6BFF6A8477A27FF12FDE99596A25164866519C28898D7B99449B766091B64568` |
| About.xml SHA256 | `5FF9B3EF7F58B60502DAB7977CF524386D8306AA0C97E0238904A7C0940FBF87` |
| Public revision | `cbf33efcc1842f869adefc09d178cf8c723b46f2`; bundled1.7 DLL matches installed, not proof of a fresh source build |
| Native game | 1.7.4998 rev1299, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`; SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| License | Installed/public LICENSE names CC BY-NC-SA4.0 and MIT, copyright2017TammyBee; do not flatten this into an unconditional MIT-only claim. No source, binary or texture copied |

Local ignored evidence is `artifacts/Refinement/megamod-spec-20261104/cut-plants-before-building/`:
`upstream`, `compiled`, `native`, `consumer` and `consumer-hits.json`. Twelve runtime source files
and all selected compiled counterparts were read: Main, Util, settings/mod classes, texture/key
holders, and the six Harmony patch classes. Public source and selected compiled behavior agree on
the paths below. The installed package has KeyBinding XML, metadata and seven language folders;
no gameplay PatchOperation XML was found. VersionFromManifest is a display helper, not adopted.
The inspected public project uses net10.0, a1.7.4985 unstable reference and Harmony3 rc3, and has a
broad taskkill build/clean target. It was never built or executed. Its policy files are reference
data, not instructions for this repository. Failed exact-type lookups left empty diagnostic files;
only successfully reconstructed native types are evidence.

## Complete source behavior inventory

### UI, state and preferences

`Main` initializes a static `AutoDesignatePlantsCutMode` once from `DefaultMode`, then unguardedly
patches everything under owner `com.tammybee.cutplantsbeforebuilding`. The switch is process-wide,
not saved per game/map. Editing DefaultMode does not reinitialize it immediately; loading another
save in the same process does not restore an independent saved switch.

The mod settings window contains DefaultMode=true, OnlyCutters=true, DigUp=false and NoInfo=false,
all global with those exact save keys. It also displays a version label. NoInfo suppresses skipped
special-tree messages. The ordinary-settings category is hard-coded English. A play-settings prefix
adds a corner icon, localized on/off tooltip and key label. `TMB_ToggleAutoDesignatePlantsCutMode`
inherits GameKeyBinding with Backslash; key handling runs before the worldView early return, so
world-view hotkey toggling is possible without its icon. High/low tick sounds accompany key changes.
There is no bottom main tab, debugger window or forestry management menu. The source icon is
`UI/Buttons/AutoDesignatePlantsCutMode`; no art has been selected or copied for megamod.

### Construction and installation

Postfixes on `Designator_Build.DesignateSingleCell` and `Designator_Install.DesignateSingleCell`
forward the cell, placing rotation, PlacingDef/StuffDef and **Find.CurrentMap** to Util. Util returns
for godMode or zero WorkToBuild. Otherwise it walks the full rotated ThingDef footprint, or a1x1
terrain footprint. It does not confirm that the callback accepted a new blueprint/install plan or
that a plant is an actual obstruction. No parent plan or later plant monitoring is retained.

Native `Designator_Build` can return early for a tutorial veto or eyedropper action. Its normal
noninstant branch places a blueprint; instant/god-mode branches complete the terrain/structure.
Therefore a generic postfix can mark plants even without accepted work. Installation uses an actual
install/reinstall blueprint and is not equivalent to the object's original WorkToBuild. Native
`DesignatorManager.Deselect()` clears the selected designator, not the selected Thing; that call
alone is **not evidence of an installation null-reference bug**. Actual map/selection context still
must be captured and validated against effective foreign patches.

### Candidate, action and lifecycle

Util gets only the first `Plant` at a cell and requires `harvestWork >= 200`. Super-category trees
are skipped; unless NoInfo, each encounter sends a RejectInput message. This categorical exclusion
includes suitable modded Super trees, not merely three named Defs. It also prevents auto-extraction.
Otherwise Util removes **all designations on the plant**, then chooses:

1. DigUp plus a minifiable tree → ExtractTree.
2. Otherwise HarvestableNow → HarvestPlant.
3. Otherwise → CutPlant.

There is no test that harvesting removes the obstruction. No worker availability, forbidden state
or reachability test is done at order creation; native jobs must handle execution. Existing manual
extract/harvest/cut and unrelated designations can be erased. There is no ownership distinction,
parent cancellation cleanup, enable-time rescan or ongoing growth scan. Generated native orders
remain after canceling a construction plan or disabling the switch. Repeated triggering replaces
orders; settings affect future triggers rather than revising every existing plant.

### Roof paths

Manual BuildRoof postfix checks for any roof-interfering plant then calls the same first-plant/200
helper. The automatic-roof postfix walks private `innerCells`, applies the same roof-interference
test and calls Util. These roof calls bypass the godMode/WorkToBuild guard in DesignatePlants.
Source code does not confirm new BuildRoof area transitions, exclude existing roofs/NoRoof cells,
or establish that `innerCells` belongs to a successful current call.

Native `AutoBuildRoofAreaSetter.TryGenerateAreaNow` has early room/border/size/faction returns before
clearing innerCells. Later, innerCells includes room cells and certain roof-holder footprints, while
only some cells enter actual BuildRoof after current-roof, NoRoof and holder-range tests. Thus stale
prior-room state and current-but-uncommitted cells are concrete static risks. The native method's
`justRoofedCells` buffer also cannot be blindly trusted after an early return. Native manual roof
designation enables BuildRoof and disables NoRoof; its own plant warning is separate from source
special-tree messages. Owned notice handling must not flood per-cell duplicates.

### Worker restriction

The global postfix on `PlantUtility.PawnWillingToCutPlant_Job` preserves native false; when OnlyCutters
is on, it changes true to false if `pawn.workSettings?.WorkIsActive(PlantCutting) == false`. Missing
work settings leave the result alone. It is not conditioned on AutoDesignatePlantsCutMode, target
ownership or incidental construction, and has no forced-job exemption. It never enables a work type.

Inspected native callers include GenConstruct blocker clearing, RoofUtility, WorkGiver_PlantsCut,
GrowerHarvest and several GrowerSow clearing paths. This means the global source restriction reaches
ordinary Growing behavior too. The owned contract intentionally narrows it to construction/install/
roof incidental clearing while keeping its independent option and default.

## Native work and semantic comparisons

Inspected successful decompilations: build/install/roof designators, DesignatorManager, auto roof
setter, PlantUtility, Plant/PlantProperties, WorkGiver_PlantsCut, WorkGiver_ExtractTree and its
RemoveBuilding base, extraction designator, PlantWork/PlantCut/PlantHarvest/ExtractTree job drivers,
WorkGiver_BuildRoof and RoofUtility. Reused the same exact-game GenConstruct evidence from S061 and
GrowerSow/GrowerHarvest evidence from S058. No dynamic execution or finalized Def export occurred.

- Native construction blocks on plants whose work is **greater than dandelion work**, with a
  terrain-under-grower exception; the source's200-inclusive rule is not the same. Roof obstruction
  follows `interferesWithRoof` independently. Enumerating first plant only is insufficient for
  multiple supported plant-like occupants.
- Native cutting and harvesting share PlantWork's real work, yield/failure, stats and physical
  product placement. Harvest reduces a perennial's growth instead of removing it; PlantCut's done
  step destroys the remaining target. So a single ordinary cut can both obtain normal yield and
  clear a berry bush. A separate harvest-then-cut loop is unnecessary and can misstate costs/yield.
- Trees can leave ordinary stumps. Cut-mode completion can queue/designate them and native blight
  behavior can propagate cutting. Those native consequences are not evidence that our placement
  policy should scan/rewrite unrelated plants. Ordinary stumps need not obstruct construction.
- ExtractTree has PlantCutting work type, priority110 in source XML, real PlantWorkSpeed labor and
  Plants experience, reservations, forbidden/touch checks and a minified-tree result. Its designator
  requires a minifiable tree and removes competing cut/harvest marks. TreeBase's Plants6/TreeSowing
  requirements are for **sowing**, not a discovered extraction unlock. No invented extraction gate.
- GenConstruct avoids cutting a tree marked ExtractTree; RoofUtility's incidental cutting does not
  inspect that designation. Selected order precedence must therefore cover the roof path explicitly.

Static Core provenance comparators (not finalized active-mod values or elapsed gameplay time):

| Plant | Source work / yield | Relevant consequence |
| --- | --- | --- |
| Grass |40 work, no harvest product | Not a normal construction blocker under the native comparison |
| Dandelions | Inherited200 work, no harvest product | Source200-inclusive rule marks it; native strict threshold does not require that preparatory work |
| Berry bush |250 work,10 base berries, harvestAfterGrowth0.30 | Harvest alone leaves the plant; ordinary cut can clear it and use normal yield rules |
| Oak |1400 work,46 base wood; TreeBase minified form and chopped stump | Real work/yield/extraction and subsequent physical transport remain unchanged |
| Chopped stump |90 work, inherited4 base wood | Ordinary consequence of chopping; no new unconditional stump-clearing rule |

The draft changes advance-order selection and worker policy, not these stats, plant growth, research,
building recipes or work costs. Actual skill-adjusted work/yield, map restrictions and optional
provider effects must be measured in future native acceptance. No speed/performance claim follows
from static inspection.

## Actual external bindings

A bounded installed text scan found source-package/replacement metadata. A separate ASCII/UTF16
name scan of **5374 installed Workshop DLLs** completed without read errors. Besides the source's
versioned DLLs, it found Vehicle Map Framework1.6. This is a bounded literal scan, not proof that
dynamic names or every possible external consumer have been found.

| Consumer | Inspected contract |
| --- | --- |
| `OELS.VehicleMapFramework`, Workshop3426502333, installed mod1.6.673 | About and LoadFolders advertise1.5/1.6 only; no installed1.7 selection is claimed |
| Consumer DLL | `1.6/Assemblies/VehicleMapFramework.dll`, SHA256 `98C07524A064AB10FEA7E82547B06E75F41CAD0D9785911E1C5251F9DED9973D` |
| Activation | ModCompat checks canonical `Mlie.CutPlantsBeforeBuilding`; startup category is `VMF_Patches_CutPlantsBeforeBuilding` |
| Actual patch | String type `CutPlantsBeforeBuilding.Util`, method `DesignatePlants`, Normal; Prefix(ref Map map) changes map to FocusedVehicle.CurrentLevel when a vehicle is focused |

The consumer patch attribute was successfully decoded with the appropriate Harmony references;
the initial unresolved-attribute output was not treated as the full contract. This is a real incoming
binding to an internal helper, but a same-named facade would not activate the package-gated consumer.
No source package spoof is acceptable. Owned map propagation fixes the underlying ambiguity; future
supported vehicle integration needs a narrow admitted adapter and exact provider-present workflows.
The installed1.6 evidence does not establish compatibility on1.7.

## Selected departures, composition and remaining work

The observable contract selects actual accepted work/obstructions instead of incidental callbacks,
non-destructive order coexistence, perennial cutting instead of ineffective harvest-only clearing,
roof respect for extraction, and construction-scoped OnlyCutters. It preserves independent plant
orders after plan cancellation, normal jobs/yields, special-tree exclusion and the source preference
defaults. Special-tree exclusion is not a new prohibition on all ordinary manual/incidental cutting.
Independent source review identified the missing current-harvestability condition in the initial
draft. PC03 now requires both HarvestableNow behavior and removal for automatic harvest, otherwise
cut; its immature-obstruction scenario and tasks cover the distinction. Host composition inspection
also added S028 fog privacy and the deferred first clearing decision on successful full-footprint
reveal/revalidation. This reuses S028's existing workflow rather than adding an old-plan scan.

The switch becomes per-game persisted and map-view-only; world-view hidden toggling and process-state
leakage are omitted. The initializer is explicitly global-only because the game's operational switch
already owns its current state. Remaining preferences use the common inherited global/save renderer.
No bottom tab is introduced. Source settings keys, source icon/key Def and runtime package identity
are not imported. UI/behavior differences are declared in replacement coverage.

S006 retains crop/work semantics, S008 explicit orders, S013 retry budgets, S028 resources/contents,
S058 priorities, S060 completion access and S061 roof-removal safety. Direct calls to one owned policy
replace internal mod-to-mod patching. S136 Floor Pipette sampling must not clear plants before any
actual construction. Native stores are sufficient for this bounded placement use; no independent
cache or scan service is justified. Shared derived facts, if actually needed, use the common map index.

PC-Q1 retains proposed behavior/default review; PC-Q2 effective1.7 transitions, worker/order guards
and target documents; PC-Q3 supported map/protection providers and actual consumers; PC-Q4 localization,
combined cost and native acceptance. Implementation tasks remain unchecked. Public documentation,
source hashes, static guards and valid Markdown cannot accept player-visible behavior.

## Review checkpoint

The source-review P2 above was corrected and its resolution independently rechecked without further
findings. A separate composition review found no issues in S006/S008/S013/S028/S058/S060/S061 and
foundation UI/settings/admission/data interactions. Nine requirements and37 scenarios remain a draft;
all28 tasks remain unchecked. Typed strict validation passed85 items with no failures, and the eight
scoped Markdown documents passed local-link/whitespace checks. No runtime acceptance is implied.
