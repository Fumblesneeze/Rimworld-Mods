# S082 — Easy Upgrades (Continued) research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Read-only investigation on **2026-11-04**; not a live reproduction or runtime acceptance report.

## Identity and evidence order

1. [Primary Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=2904429048) first:
   Mlie's continuation of ToyWalrus/ToyWalruss's
   [original](https://steamcommunity.com/sharedfiles/filedetails/?id=2134505728). It describes
   building conversion and paid quality attempts; it also acknowledges a resource-consumption bug.
2. Installed About/Manifest/README/license and selected latest **1.6** XML/assemblies under
   `F:/Steam/steamapps/workshop/content/294100/2904429048`. SupportedVersions is 1.1–1.6, with no
   1.7 claim and no declared dependencies/loadAfter. Manifest1.6.3; author ToyWalrus.
3. Public [source](https://github.com/emipa606/EasyUpgrades), read-only checkout
   [b2170be74708814fa4520e0dfe83ce0005939b0a](https://github.com/emipa606/EasyUpgrades/tree/b2170be74708814fa4520e0dfe83ce0005939b0a).
   Selected binary decompiled offline, never executed. Its committed 1.6 DLL is byte-identical to
   the installed DLL. Runtime sources are 24 C# files; method bodies, settings, comp/designator/job
   paths and selected compiled counterparts were inspected, with Def names verified against XML.
4. Current native assembly and Core construction XML, then bounded installed consumer searches.
   No game launch, Workshop write, product implementation or normal-save/config mutation.

| Artifact | SHA-256 / identity |
| --- | --- |
| Installed and upstream committed 1.6 EasyUpgrades.dll | `378FF74A590A92C819C53E1404AD7488B11E9A739C107FD340B11B2B3DC37796` |
| Installed About.xml | `AE32D97C3ACC7C9C3BE75E70EAF372D378A668C31EA2409A7C808B7C3643F408` |
| Installed Manifest.xml | `24180BBCB1AA16F7681155110556D1FBA8E7F28B9FF1475C91620DDAA66967A9` |
| Current native Assembly-CSharp.dll, 1.7.4998 rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Assembly version1.6.0.0, file1.6.3, informational
`1.0.0+06e1fb7eb0b642b44d8c35e025cb71585aada9c4`; Release, IgnoresAccessChecksTo Assembly-CSharp.
This informational revision differs from checkout HEAD; byte identity does not prove source-build
reproducibility. Root MIT notice copyright2020 Mlie; assembly notice © ToyWalrus2026 MIT. Retain
applicable attribution/license notices for any future substantial reuse. No source/art/assembly has
been copied into the product. Foreign repository instruction files do not govern this work.

Local ignored inspection root: `artifacts/Refinement/megamod-spec-20261104/easy-upgrades/`:
`upstream/Source/EasyUpgrades`, `binary/EasyUpgrades`, `native/RimWorld.CompRefuelable.decompiled.cs`.
Current existing native extracts also read: `best-mix/native/RimWorld.WorkGiver_DoBill.decompiled.cs`
and `use-bedrolls/native/RimWorld.JobDriver_RemoveBuilding.decompiled.cs` under the same dated root.
The first attempted legacy Managed path was absent; actual beta DLL is at the game root. No missing
path result is counted as native evidence.

## Source building conversion

`CompUpgrade`/`CompDowngrade` expose player-building gizmos, resolve linkedThing and additional or
refund resource lists. Upgrade checks one and multiple research prerequisites; both comp helpers
replace WoodLog resource entries with actual Stuff when both endpoints are MadeFromStuff.
`Command_ModifyThing.ApplyDesignation` removes Uninstall/Deconstruct and opposite conversion orders;
`Designator_Upgrade` applies the same upgrade intent over visible eligible cells, with designate-all.
Downgrade is a contextual gizmo, not a second area designator. No bottom tab exists.

WorkGiver_Upgrade/Downgrade scan native designations, reject forbidden/burning/unreservable targets,
require Construction assignment and construct a Job. Upgrade gathers native listerThings candidates,
sorted by Manhattan distance, with itemAvailability precheck then reservation/reach tests. The
multi-stack per-Def accumulator overwrites its prior sum with the next take, risking a wrong third
stack quantity. The driver accepts only target reservation, then queues hauling/work. It does not
retain the base driver's designation/explosive failure checks because it replaces MakeNewToils.

`JobDriver_ModifyThing` uses source WorkToBuild clamped20..3000, ConstructionSpeed×1.3 per tick and
0.08×GlobalLearningFactor experience. It tracks placed Thing references, destroys each whole Thing,
captures workbench BillStack, refunds ceil(Fuel) of the first allowed fuel, removes ALL designations,
destroys old with WillReplace, constructs linkedThing using old Stuff, assigns player/fullHP,
re-adds bills, reconnects power, spawns and refunds the declared difference. It preserves neither
arbitrary comps nor quality/bed ownership/style explicitly. Its shadowed work fields are not Scribed
by its base class; only resourcesPlaced is added to ExposeData. Recovery is not an atomic contract.

These are source facts; actual wrong consumption, save loss or duplication remains a **risk inferred
from code**, not a reproduced bug. The draft does not promise faithful preservation of those defects.

### Exact installed XML route inventory

`AddCompToThingsWithComp.xml` contains six Core bidirectional pairs. The public prose's five-pair
list omits the installed stool/chair pair. Source prices and current unfinalized Core XML deltas agree
for the six baseline pairs below; Stuff volume and foreign patches still require finalized proof.

| Pair | Source additional / reverse refund | Source upgrade research | Current Core work, source → destination |
| --- | --- | --- | --- |
| FueledStove ↔ ElectricStove | 2 ComponentIndustrial | Electricity | 2000 → 2000 |
| HandTailoringBench ↔ ElectricTailoringBench | 50 Steel, 2 ComponentIndustrial | Electricity only | 2000 → 2500 |
| FueledSmithy ↔ ElectricSmithy | 3 ComponentIndustrial | Electricity only | 3000 → 3000 |
| WoodFiredGenerator ↔ ChemfuelPoweredGenerator | 1 ComponentIndustrial | Electricity | 2500 → 2500 |
| Door ↔ Autodoor | 40 Steel, 2 ComponentIndustrial | Autodoors | 850 → 1100 |
| Stool ↔ DiningChair | 20 WoodLog, resolved as actual Stuff | ComplexFurniture | 450 → 8000 |

Source downgrade commands have no research gate. Current Core destination tailoring also requires
ComplexClothing and smithing also Smithing; the owned contract requires all current prerequisites.
Stool/chair costStuffCount25/45; door/autodoor25/25; tailoring75/75 plus fixed electric materials.
Stoves cost80Steel, smithies100Steel, generators100Steel; electric/chemfuel components give the
listed differences. These are source XML comparisons, not a finalized active-mod export.

| Optional package / source XML | Directed route (↔ includes refund reverse; → has no source reverse) | Source added material | Source research |
| --- | --- | --- | --- |
| `dismarzero.VGP.VGPGardenFabrics` / VGP.xml | TableLoom ↔ TableElecLoom | 55Steel, 2components | Electricity, ComplexClothing |
| `dismarzero.VGP.VGPGardenGourmet` / VGP.xml | Oven ↔ ElectricOven | 65Steel, 2components | Electricity, CookingII |
| Same | HandCanningstove ↔ Canningstove | 20Steel | Electricity |
| Same | FueledCandyTable ↔ CandyTable | 20Steel | Electricity, CookingI |
| Same | GrindStone ↔ ElectricGrindStone | 40Steel, 2components | Electricity |
| `VanillaExpanded.VFEProduction` / VFEProduction.xml | TableButcher ↔ VFE_TableButcherElectric | 60Steel, 4components | Electricity |
| Same | DrugLab ↔ VFE_TableDrugLabElectric | 100Steel, 6components | Electricity, DrugProduction |
| Same | VFE_FueledSmelter ↔ ElectricSmelter | 2components | Electricity, Smithing |
| Same | TableStonecutter ↔ VFE_TableStonecutterElectric | 105Steel, 6components | Electricity, Stonecutting |
| `DankPyon.Medieval.Overhaul` / MedievalOverhaul.xml | DankPyon_BedLog → DankPyon_StrawBed | 5DankPyon_Straw | None specified |
| Same | DankPyon_StrawBed → DankPyon_Bed_Rustic_Green | 5DankPyon_Linen | None specified |
| Same | DankPyon_BedLogDouble → DankPyon_StrawBedDouble | 10DankPyon_Straw | None specified |
| Same | DankPyon_StrawBedDouble → DankPyon_Bed_RusticDouble_Green | 10DankPyon_Linen | None specified |
| Same | DankPyon_Trestle → DankPyon_SawTable | 10Steel, 1component | DankPyon_Lumber, Electricity |
| Same | DankPyon_SpinningWheel → DankPyon_ClothSpinner | 65Steel, 2components | ComplexClothing, Electricity |
| Same | DankPyon_Furnace → DankPyon_BlastFurnace | 100Steel, 2components | Electricity |
| Same | SimpleResearchBench → DankPyon_AdvancedResearchBench | 15DankPyon_IronIngot, 25DankPyon_ComponentBasic | DankPyon_Engineering, DankPyon_Alchemy |

17 optional route rows plus six Core pairs; **38 directed source conversions**. Optional sequences
use MayRequire and successAlways rather than full semantic admission. Some Medieval patches append
a new comps node to the ThingDef. Owned XML must merge into guarded existing nodes and preserve all
foreign entries. Optional source declarations are a research catalog, not our verified compatibility.

## Source quality reworking

CompIncreaseQuality and Designator_IncreaseQuality find CompQuality-bearing targets. Gizmos prioritize
player art/buildings, then factionless apparel/weapons. Apparel gizmo needs a recipe/bench name;
weapon gizmo does not, but area designator checks both. The area tool binds Command_ItemForbid.
Both gate the maximum quality on designation, not consistently through the work lifecycle.
Minified wrappers and installed art take different paths; JobDriver_IncreaseQuality can automatically
minify art and remove all designations, despite prose saying sculpture must be uninstalled first.

WorkGiverDefs: Construction for upgrade/downgrade/building quality, Tailoring for apparel, Crafting
for item, Art for art, all priorityInType100 and Manipulation. Apparel and art factories nevertheless
call CanDoWorkType(Crafting); owned behavior corrects this. Portable station lookup first uses
recipeMaker.recipeUsers then any building AllRecipes product, rejecting forbidden/burning/unreachable/
unreservable candidates but not explicitly checking research, skill or power/fuel. Base quality
giver checks target reservation/forbidden/burning and resources. The draft retains real manufacturing
route restrictions and S054 repair knowledge rather than upgrading unknown high-tech loot for free.

Cost helper unwraps minified Thing, chooses Stuff quantity unless a MadeFromStuff object also has
fixed costs (then highest count, first tie), or tech-level material and max(10, round(base-value/10/
material-value)). It multiplies by quality factor and positive bulkRecipeCount (else1), ceils and
min1. VFE Architect support is reflection by extension type NAME `StuffExtension_Cost`, reading
field `thingDef`; it is not a concrete package/type-safe provider proof. Owned mapping needs that
proof. Max quality Legendary; bases/material factors and full formulas are in the observable spec.

Success: baseChance×skill/14. Failure: baseFail+(20-skill)/20×0.15, except Awful0. One Unity random
draw tests success first then success+failure, so high success truncates actual failure mass.
At skill14 Normal, actual chances are85% /11.5% /3.5%, not simply85% /7% /8%.
Mech fixed skill is used first, overridden by pawn skills when available. Source lacks explicit
probability or effective-skill clamp except percent text; owned outcomes bound both.

Building work clamps WorkToBuild20..200; portable/art make-work divided by max(skill,.01), clamp1..200.
ConstructionSpeed or WorkSpeedGlobal×1.3 advances work;0.08×learning per active tick. Success raises
one quality, sets fullHP, clears graphic/styleGraphic and dirties mesh; failure lowers one quality
and subtracts rounded10% of newMaxHP; neutral adds10% and caps toMax. BonusXP starts from quality
index×80/40/50 respectively. Source uses ArtGenerationContext.Colony and formats the item name by
slicing before `(`, risking wrong names/exceptions; metadata preservation requires explicit handling.
It destroys entire recorded resource Things and removes ALL designations only after message success.
Only thingToWorkOn is Scribed; work and resourcesPlaced are not. Target/bench reservation fallback
can reserve only the target when the bench cannot be reserved. These risks drive explicit exact
accounting, cancellation/save, station and once-only outcome contracts; none was live reproduced.

Source global ModSettings: six `increase*QualityChance`, five `decrease*QualityChance`, six
`neededMaterials*Quality`, `maxUpgradableQuality`, with matching Reset/Scribe defaults. Sliders
0..1 /0..10 /0..6; higher rows hidden by max; Reset is global. There is no save inheritance.
Owned UI replaces this with common global/save/Use-global, scope-safe resets and contextual orders.

## Native facts and composition

- Current CompRefuelable.PostDestroy returns floor(Fuel) for qualifying single-fuel objects even
  under WillReplace; Refuel multiplies supplied units. Combined with source ceil(Fuel) manual return
  is a potential double refund. Native snippets are pinned; exact live destruction path remains EU-Q2.
- Current WorkGiver_DoBill checks fuel/operability, interaction reservation, allowed worker and
  recipe skill. Source bench-name search is weaker; an operational manufacturing route is needed.
- Current JobDriver_RemoveBuilding Scribes its own private work fields and uses interval work at
  ConstructionSpeed×1.7. Source shadows fields and supplies its own MakeNewToils and1.3 rate.
- S028 full-build/material-change contracts are not this cheaper conversion action. Preserve state
  through shared owned transfer rules, with no free quality roll/repair on conversion. S013 keeps
  unmet target/spent budget; rework achieving that target uses normal run closure/count clearing.
  Later deterioration shows unmet quality but needs an explicit new construction request, never an
  automatic rework/construction loop. S054 gates repair knowledge. S005 and
  S014/17/18/19/26 preserve group/bill accounting; quality reworking produces no extra physical item.
- Use native designation/lister/recipe data and existing shared facts, not another hauling/search
  cache. Specific invalidation obligations belong to the first concrete shared extension.

## Actual external consumers and boundaries

Installed text search over XML/C#/JSON for EasyUpgrades/package ID found the source versions,
RimPy catalog (`1847679158/db/db.json`, historical `toywalrus.easyupgrades`), a replacement catalog
(`3396308787/replacements.json`) and **Colony Manager Redux**. Bounded ASCII DLL search found only
EasyUpgrades' six own versioned DLLs. This does not exclude encoded/dynamic consumers elsewhere.

`ilyvion.colonymanagerredux`, Workshop3310027356, reads
`Integrations/EasyUpgrades/Common/Patches/ManagerStation.xml` under package-gated LoadFolders branches
for1.5/1.6/1.7. It adds BasicManagerStation↔ManagerStation (Def names CM_BasicManagerStation /
CM_ManagerStation), researchManagingSoftware, 25WoodLog +50Steel +1Silver additional/refund. Its
comments explicitly rely on Stuff substitution. The consumed comp/property/member identities are
listed in design. Those source classes are internal C# types but XML-reflected contracts nonetheless.
No manager-state transfer was inspected; this is a proven incoming definition consumer, not a proven
safe manager-station upgrade. A narrow optional route adapter is the candidate, no package spoof.

Source Def names: Upgrade/Downgrade; IncreaseQuality_Building/Apparel/Art/Item designations;
UpgradeThing/DowngradeThing and IncreaseQuality_Building/Crafting/Artistic jobs, corresponding work
givers. Public Command_ModifyThing.ApplyDesignation, designators, static EasyUpgrades chance helpers
and JobDriver_ModifyThing are exposed surfaces, but no incoming executable consumer was demonstrated.
Do not reproduce all names without need or promise source job/load compatibility. Settings/save import
is absent; future migration needs backup/atomicity/restoration and explicit postconditions.

## Deliberate draft decisions, balance and remaining questions

Preserve source quality options and one paid attempt, but preserve damage/quality on structural
conversion; require full destination eligibility; exact quantities; ordinary workstation operation;
safe sculpture handling; no free fuel; own-only cancellation; no negative/over-maxHP; actual chance
display; correct art/tailor work assignments; policy edits pause instead of changing a committed deal.
Source scale values remain provisional and explicit. No arbitrary balance penalty added.

The current XML comparators are new chair8000work/45Stuff, stool450work/25Stuff, stove2000work/
80Steel(+2components electric), and door850 versus autodoor1100work/(same25Stuff+40Steel+2components).
Conversion chair450 and rework≤200 are far cheaper labor than fresh manufacture; high skill makes
low tiers certain and the default ceiling permits Legendary. Full/neutral repair and quality-market
value can create strong value gain. Need actual finalized three-comparator balance measurements,
including cheap/expensive Stuff, normal manufacture, S028 reconstruction, high-tech fixed-cost gear,
market resale and mechs. These are documented risks, not measured profitability claims.

| ID | Open work retained for later |
| --- | --- |
| EU-Q1 | Settle measured labor/cost/quality/repair balance, largest-fixed and bulk-count formulas; current defaults are provisional |
| EU-Q2 | Prove current native state/fuel/commit/recovery/art/recipe lifecycle and each actual semantic target |
| EU-Q3 | Inspect optional providers, manager-station state and any real external facades/migration; no blanket support |
| EU-Q4 | Verify direct composition with quality budgets, replacement, repair knowledge, production/hauling and provider quality |
| EU-Q5 | Contextual UI, common settings, seven languages, focused RED/GREEN, independent implementation review and native acceptance |

All questions are recorded rather than interrupting the specification pass. Implementation and
finalized-game measurements remain pending. Review/schema results are recorded in the progress ledger.

Independent document review found two contract edges now explicitly specified: external destruction
recovers surviving delivery without recreating/refunding the target, and target-achieving rework uses
S013's normal count-clearing closure rather than retaining an exhausted run forever. Subsequent
deterioration does not auto-start construction. Implementation-only index/scan constraints were
removed from the observable spec and remain in design. No runtime evidence is claimed by these edits.
