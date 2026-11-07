# S054 — I Cant Fix That research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **P1-47**. Inspected 2026-11-03. Complete current-source read and selected compiled,
native/provider comparisons support the [observable draft](../../../../openspec/changes/specify-megamod-repair-knowledge/).
Independent review and typed strict validation passed. Source facts below are distinct from selected corrections; nothing is
implemented, and no runtime compatibility or acceptance is claimed.

## Documentation and exact inputs

Read [Workshop2780971535](https://steamcommunity.com/sharedfiles/filedetails/?id=2780971535),
[official source/README](https://github.com/emipa606/ICantFixThat), installed About/Manifest,
LoadFolders, MIT license and English keyed text. The stated intent is to restrict repairs of
buildings/apparel/weapons whose production the colony does not understand, with an additional
colony-tech-level option. Advertised integrations are research leads, not verified compatibility.

Installed package `Mlie.ICantFixThat`, Mlie, version1.7.0, supports1.3–1.7. Root
`F:/Steam/steamapps/workshop/content/294100/2780971535`. Required Harmony, loadAfter Harmony.
The1.7 base DLL SHA256 is
`79A30002226DF721B48D5D874BDD416609CB7A27DB0AE861E0E1F818278C32EB`, identical to the source checkout's
1.7 DLL. Source pinned at `7761227b5ab66950a724307daad0a2219b00e2ff`, retained read-only under ignored
`artifacts/Refinement/megamod-spec-20261103/i-cant-fix-that/source`; base DLL decompiled in sibling
`compiled`. Informational version names older commitdd660f241124f348cf10fd51d3b4dbd85c3f0a74,
not proof of the exact build's source state. MIT copyright2020 Mlie; retain notice for any reuse.

The core project targets net10.0, assembly/file1.7, Rimworld.Ref1.7.4995-unstable and Harmony3rc4.
All12 projects target net10.0; their framework/reference/build-target fields were inspected. VFEAncients
also has a hard compile reference to installed1.5 VFEAncients.dll; most adapters resolve reflection
targets. **Do not build upstream:** all12 BeforeTargets Clean/Build targets force-kill every RimWorld.exe
process. This was read only and never executed. Source outputs/assemblies remain inspection inputs,
not a deployable megamod dependency. No game, normal configuration or Workshop content changed.

## Read coverage

Fully read all six core C# files: Main, ICantFixThatMod, ICantFixThatSettings,
WorkGiver_Repair_HasJobOnThing, WorkGiver_FixBrokenDownBuilding_HasJobOnThing and
FloatMenuOptionProvider_DraftedRepair_GetSingleOptionFor. Fully read all23 adapter C# files: all29
current source files. Core, VFEAncients and RepairWorkbench projects were fully read, with structured
inspection of the relevant fields across all12 projects. Selected compiled core Main, Settings and
AssemblyInfo were fully read. All11 selected1.7 adapter assemblies were decompiled; target resolution,
gating and key bodies were cross-checked, including full VFEAncients/SimpleMending bodies. This is not
a claim to have fully read every generated compiled member. Artifacts live under the same ignored
research root: `compiled`, `adapters/<folder>`, `native`, `provider-vfea`, `provider-glittertech`.

## Core behavior found

- Startup builds a static dictionary from RecipeDef.ProducedThingDef to selected research list,
  logs its size and PatchAll with owner Mlie.ICantFixThat. Later recipes for a product overwrite
  earlier entries; it does not represent alternative production routes explicitly.
- Research choice: singular recipe researchPrerequisite first, then plural researchPrerequisites,
  then recipeUsers. The fallback initializes lowestTech=Archotech and selects research techLevel
  greater than that value; with normal enum levels it cannot choose a workbench. This is a static
  source defect candidate, not a reproduced player failure or desired replacement rule.
- CanRepair(Thing) forwards to ThingDef. God mode returns true. A cached unfinished research list
  refuses repair. Optional tech gate compares player faction Def tech level plus allowance against
  the item's tech level. Non-buildings otherwise pass. Buildings additionally enforce
  minTechLevelToBuild, maxTechLevelToBuild, IsResearchFinished and storyteller AllowedToBuild.
  The author explicitly states in a January2026 Workshop reply that higher tech can always repair
  lower tech; the maximum-tech building check contradicts that for Core primitive furniture.
- IsValidRepairer includes colonists, colony prisoners, slaves and colony mechs. Core ordinary
  repair and breakdown postfixes preserve native false and only add refusal for these actors.
  Failure reason is ICFT.NoKnowledge.reason. Drafted-repair menu postfix replaces any nonnull
  option when CanRepair fails, without that actor check; it can replace an already-disabled NoPath
  explanation. Native context is recorded below.
- Public GetDisallowedThings enumerates only keys of the recipe dictionary and calls CanRepair,
  not all building definitions. Bounded consumer search is recorded below.

Settings discrepancy: field initializers are AlsoBlockTechlevel=false and allowance1f, while
Scribe fallback defaults are true and5f. Native LoadedModManager.ReadModSettings returns newT for
an absent settings file (false/1); an existing ModSettings object with missing keys invokes ExposeData
(true/5). Keys are AlsoBlockTechlevel and AlsoBlockTechlevelLevel. No user settings were modified.
Global source UI has a checkbox and, when checked, an integer-rounded allowance slider from0 through
the final TechLevel enum index. With an active game it shows colony/max repairable tech labels.
No bottom tab or separate gameplay window was found in all29 source files. Owned settings use common
global/per-save inheritance and consistent false/1 defaults; no source singleton, source keys or
automatic settings import is selected.

## Conditional adapter inventory

LoadFolders1.7 selects root/1.7 plus these conditional compatibility folders. Gates below are exact
source declarations. Only the two providers described later were found in the installed Workshop
metadata scan; no blanket current1.7 compatibility follows from these names.

| Folder | Source IfModActive gates |
| --- | --- |
| FluffyBreakdowns | theeyeofbrows.fluffybreakdowns or fluffy.fluffybreakdowns |
| RepairWorkbench | Acruid.RepairBench |
| RepairableGear | SteveZero.RepairableGear |
| RepairItems | Moony.RepairItems |
| MiscWeaponRepair | Haplo.Miscellaneous.WeaponRepair |
| MendAndRecycle | notfood.MendAndRecycle |
| VFEAncients | VanillaExpanded.VFEA |
| LTSMaintenance | LimeTreeSnake.Maintenance |
| OgRepairYourGear | Og.Repair.Your.Gear |
| GlittertechExpansion | Ushanka.GlittertechExpansion |
| SimpleMending | ComfyCuddles.WithEuterpe |

Each adapter has its own static startup class, compatibility log and unconditional PatchAll owner.
These are implementation references, not an admission scheme to copy.

| Folder | Source behavior | Selected1.7 DLL SHA256 |
| --- | --- | --- |
| FluffyBreakdowns | WorkGiver_Maintenance.HasJobOnThing postfix; preserve false, colony actor and CanRepair | `3AD8754FA7298188E7097FC68AF47EACC8C037F1D04C27DD7866F0543B96CE06` |
| RepairWorkbench | FindDamagedItems(Pawn,Thing,Bill) result list filtered by CanRepair, no actor check; accidental VFEAncients namespace | `0A9E86FDAA658012CBC83A36959152DA34F23E69D9D254DFC7F4D6806BD3E404` |
| RepairableGear | IsRepairable(Thing) and CanBeMaintenanced(Thing) preserve false then CanRepair/reason, no actor context | `579D10D1D0BB80A79C6405A03960142BC8F05B0E2DEC5169388E6EFB583D9F82` |
| RepairItems | PawnCanRepairEver resolved without signature, bool result and Thing t; no actor check | `F139E1651B4E4FF3866ACB59AF4B9FCA12BCC3E80BCBC799256B493A3FC5B298` |
| MiscWeaponRepair | GetAvailableTwinThing(Pawn), reject unrepairable returned donor with null/reason | `D1BDDDD182B80A78470ACAD9FA803772DF572BF781FAA65778F9A25046F17D34` |
| MendAndRecycle | Bill.IsFixedOrAllowedIngredient Thing/ThingDef; four MendSimple/ComplexApparel/Weapon recipes; CanRepair on every ingredient, no reason/actor | `52F46967D10B1129DB58ED8A9F7BA3BF6F0086E9EE83593EFC6C009FAB91B771` |
| VFEAncients | Prefix on provider RepairItem, extension and first damaged stack1 weapon/apparel; false result and skip on missing knowledge | `C38A537333F97E523302EC5FE0E5F596C618BBF1B7795DE996A91AF22FFBB910` |
| LTSMaintenance | Apparel/weapon maintenance HasJobOnThing, native result and colony actor, nonnull target | `1BF2B0B04E64CF920BDFDF1B6950B638D4573B712F9BB2908E6601FA432B406C` |
| OgRepairYourGear | HasDurability resolved by name, bool Thing helper with no actor context | `B8E3B3985B922A87032502D8511DD0469EE69755E46549A17C9AC4B6057A1C51` |
| GlittertechExpansion | CompGlittertechRepairer.CanRepairThing(Thing), preserve false then CanRepair/reason | `7B9B4DD8F58D5809DBFC8E5E56D3C84B39A76EFF1EFA9515E24DA584DF89AD1C` |
| SimpleMending | RepairUtilities.TryGetRepairCost resolved by name, preserve false then CanRepair/reason; source class is misleadingly named CanRepairThing | `2736A51F089D26550F43E4E8D70EFC238E109DDF8A0632FF51DBF709A5C258CE` |

Exact namespaces/callable shapes and adaptation boundaries are in the owning design. Source helpers
without actor context can affect more than the intended colony action; actual callers must be
admitted. Source mending ingredient restrictions are not a sound distinction between the equipment
being repaired and its steel/cloth/component ingredients.

## Installed provider samples

**Vanilla Factions Expanded - Ancients**, `VanillaExpanded.VFEA`, Workshop2654846754, advertises1.3–1.5.
Selected1.5 DLL SHA256 `1D9B4C6B0509F8FB7389017A8AFCFB3A0D3783103473D714BB4D341C024B5132`.
Read included MendingPatches.cs and RecipeWorker_Mend.cs fully; decompiled the selected mending class
and compared critical RepairItem behavior. Its GenRecipe.MakeRecipeProducts prefix recognizes
RecipeExtension_Mend, picks the first damaged stack1 weapon/apparel, sets HP to maximum, returns it
as the product and skips ordinary product creation. Ingredient helpers distinguish gear/materials
and select repair cost from Stuff/cost/fallback tech. ConsumeIngredient despawns repaired singleton
gear and consumes ordinary materials. The source restriction's prefix-of-prefix refusal can leave
the outer recipe-products result unset; possible consumption/result risk is a static inference,
not a reproduced player bug. Our group must refuse safely before consumption and completion.

**Ushanka's Glittertech Expansion**, `Ushanka.GlittertechExpansion`, Workshop3522676478, advertises1.5–1.6.
Selected DLL SHA256 `BB945E032B4133636E21BFFDE2839A9167B9110CA888E7A784562C03D7787442`.
Read entire CompGlittertechRepairer.cs including map manager; compared selected decompilation.
Manager scans player repairable buildings/repairers every250ticks. Candidate check uses target not
destroyed, HP and radius; machine needs power and no stun. Working RepairTick adds1HP at configured
interval (default10) without rerunning CanRepairThing for the retained target. Started/stopped paths
change active/idle power and glow; stopping requests rescan. An owned adapter must recheck policy
before further HP changes, preserving these power/range/rate rules and keeping dynamic provider
demand intact. This does not establish S052/S053 power-controller compatibility.

Other nine source gate families were not found in the installed Workshop metadata scan. No source
advertisement is upgraded to a current provider identity or beta-compatibility claim. Optional
provider admission remains RK-Q2; missing packages are not reasons to invent a replacement provider.

## Native1.7 comparisons

Inspected installation1.7.4995rev309, Assembly-CSharp SHA256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
Retained targeted decompilation under `native`; hash identifies evidence only.

- Read WorkGiver_Repair, WorkGiver_FixBrokenDownBuilding, DraftedRepair provider, RepairUtility,
  JobDriver_Repair and JobDriver_FixBrokenDownBuilding. Ordinary repairs require repairable building,
  same faction, damage, appropriate home area/reservation/access/fire/designation eligibility.
  Breakdown work additionally needs actual broken state and available industrial component.
- DraftedRepair requires eligible selected pawn/mech/manipulation and construction ability, supports
  drafted single selection, returns NoPath when appropriate, otherwise orders normal Repair.
- Repair begins with an80-unit work countdown reduced each tick by ConstructionSpeed×1.7, so elapsed
  warmup depends on worker speed; it learns Construction and increments HP through native repair
  speed/work. Breakdown waits1000ticks, destroys the component then applies success/Notify_Repaired.
  Neither native job driver knows this source policy; selection checks alone do not cover revocation.
- Read Mod.GetSettings and LoadedModManager.ReadModSettings: absent file versus missing saved keys
  explains the false/1 versus true/5 split without touching user files.
- Read RecipeDef.ProducedThingDef, AllRecipeUsers and AvailableNow, BuildableDef.IsResearchFinished,
  TechLevel enum and Difficulty.AllowedToBuild. ProducedThingDef returns null for specialProducts or
  product count other than1. AllRecipeUsers includes direct users and reverse ThingDef.recipes.
  AvailableNow requires both singular/plural research and includes non-research faction/ideology
  conditions. IsResearchFinished checks all building research. TechLevel is Undefined0 through
  Archotech7. AllowedToBuild gates traps/mortars/turrets using difficulty flags.
- Core Buildings_Joy.xml: HoopstoneRing/GameOfUrBoard have maximum Neolithic construction tech;
  ChessTable/HorseshoesPin have minimum Medieval. This concretely exposes the source's maximum-tech
  repair regression; finalized modded definitions and live work remain future evidence.

## Public contracts and bounded consumer search

Public Main.CanRepair(Thing/ThingDef), IsValidRepairer(Pawn), GetDisallowedThings and the adapter's
public static readonly mutable MendingRecipesList are used by the source's own adapters. Settings
use the two global keys above; no owned entity save type was found. No XML PatchOperation is used.

Successful native rg scans covered5452 installed Workshop/local DLL paths for assembly/API names
and UTF16LE reflection strings. All51 matching DLL paths belonged to this source's own versions/
adapters; no external executable consumer was found. Successful text scans found own source metadata
and a RimPy database catalog record (Workshop1847679158), which is not an API consumer. Lists are
retained as `installed-dll-candidates.txt` and `consumer-dll-matches.txt`. Earlier full Node scans
timed out and supply no evidence. These are bounded installed searches, not proof of universal
absence or a promise to support uninspected reflection consumers. No speculative facade/package
spoofing or upstream settings migration is selected.

## Selected corrections and observable boundaries

The owning draft intentionally chooses:

1. Any manufacturing route's research suffices; all research within it is required. Bench research
   is fallback only when the recipe has no direct research, using every legitimate recipe user.
   Recognize identifiable multi-output manufacture; repair/recycling cannot teach manufacture.
2. No recipe means no invented research. Workbench presence, ingredients, pawn skill and ideology
   production restrictions do not erase learned research. Keep actual repair's own work rules.
3. Buildings retain direct research, minimum tech and source's difficulty restriction, but advancing
   past maximum construction tech does not remove repair knowledge.
4. Consistent default ceiling Off/allowance1, common global/save inheritance and contextual feedback,
   preserving existing native refusal. No new bottom tab; native God mode remains the debug bypass.
5. Scope colony actors and player-owned admitted automation, gate the repaired target rather than
   ordinary material ingredients, and recheck before further result/consumption with safe recovery.
6. Each external repair path has one coherent semantic admission group; missing is quiet, changed
   stays native with bounded warning. Source co-installation suppresses our overlapping group.
7. Shared definition metadata and direct owned policy calls; no duplicate Thing cache or internal
   source-mod patch stack. Native resource/work rates and foreign data stay intact.

These are draft decisions/corrections, not claims that the source already behaves this way.
S022 recycling/S023 destruction, production knowledge/browser and future medical treatment remain
distinct. There is no added item, recipe, durability, material cost, research project or provider
content. Core research progression is the comparator; finalized balance/job reachability and
unresearchable salvage behavior need the future semantic/live pass.

## Remaining work

RK-Q1 effective beta guards, lifecycle and safe interruption; RK-Q2 actual optional providers,
manufacturing classification and external consumers; RK-Q3 finalized progression comparisons;
RK-Q4 implementation, seven-language UI, focused RED/GREEN and final native acceptance. No game,
build, test or install was performed for this prose slice. Review/strict checks are tracked in the
[progress ledger](specification-progress.md); all runtime task boxes remain unchecked.
