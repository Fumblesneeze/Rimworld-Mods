# S096 — Micro Designations research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Replacement P2-25. Inspected2026-11-05. [Draft](../../../../openspec/changes/specify-megamod-item-recipe-orders/)
is observable intent, not implemented behavior. Source facts, static risk findings and selected
corrections are distinguished below. Independent document re-reviews and strict validation passed;
the [ledger](specification-progress.md) records corrections and evidence. No game or source build was run.

## Documentation and pinned inputs

Read the [author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=1877972690)
before code. It describes item-specific recipe orders, modded recipe discovery, initially visible
actions with hiding preferences and configurable icons/hotkeys. It acknowledges stale corpse
eligibility until a failed attempt. A comment reports repeated pickup/drop with Medieval Overhaul's
bill-work callback; this is a report, not a verified current compatibility failure.

The page links [public source](https://github.com/catgirlfighter/RimWorld_MicroDesignations), pinned
at **ff1bdd4f83d1c9e98741f80ba581100de37f4f13**. Selected source is `Source/MicroDesignations16`;
other version directories are historical. Read README, change.log, selected project and compiled
source paths. Latest change.log says1.2.0–1.6. No LICENSE/COPYING was found in installed package or
checkout; public visibility does not grant reuse. Independently implement behavior, retain attribution,
and resolve any future code/art reuse separately. Never ship the inspected DLL or old bundled Harmony.

| Identity | Inspected value |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1877972690` |
| About name / author | Micro Designations / avilmask; Steam author avil |
| Package spelling / canonical | `avilmask.MicroDesignations` / `avilmask.microdesignations` |
| Advertised support / dependency | 1.0–1.6; Harmony required; no1.7 declaration |
| Selected input | `1.6/Assemblies/MicroDesignations.dll`; assembly/file1.0.0.0; no About modVersion |
| DLL SHA256 | `47744342D700E867CDB1E5C8CC8094EE77AD11A8C0718F8E761058B53CC1ECFC` |
| About SHA256 | `F8DEECF862645991067107892749DF7B4F7FD6901B16DD0C80C3CE8B1A668B57` |
| Source binary comparison | Public checkout's1.6 DLL has the same SHA256 as installed |
| Native target | RimWorld1.7.4998rev1299, Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

No LoadFolders was found;1.6 is an explicitly selected research reference, not a claim about which
folder a1.7 loader accepts. Read About, selected XML/English/Japanese catalogs and all selected
compiled types. Complete decompilation, clone and targeted native types remain ignored under
`artifacts/Refinement/megamod-spec-20261105/micro-designations/{binary,upstream,native}`.
Source project confirms the active compile list, including DesignatorPatches; the old
InspectGizmoGridPatches body is commented out and supplies no active hook.

## Discovery and item UI

`CrossReferences.Populate` scans recipes unless `DefExtention.Ignore`. It requires some recipe
users typed BuildableDef, one ingredient entry, and only the **first** allowed ingredient Def's
stackLimit below2. It does not prove all allowed items are singleton or that the recipe consumes
one physical unit. ThingDef inherits BuildableDef, so that test alone does not establish a building.
It enumerates fixedIngredientFilter.AllowedThingDefs and attaches ApplicableDesignation comps where
comps is nonnull. Multiple users/repeated population can add duplicates.

For each user it chooses the first fixed-bill-giver workgiver of WorkGiver_DoBill or a subclass,
gets/creates recipeName+`Designation`, then creates a MicroWorkGiverDef named
baseWorkgiverName+`_`+recipeName+`_DesignationWorkGiver`. It copies workType/capacities, sets
priorityInType to base minus5 and appends to workGiversByPriority without sorting. For subclassed
workgivers it requires RecipeJobDef matched by exact recipe defName first, then workerClass; missing
job mapping logs and skips. Designations/comps can exist even if workgiver construction fails.

ReverseDesignatorDatabase.InitDesignators repeats the loose eligibility scan without Ignore, but
requires a matching designation. Designator_MicroRecipe creates recipe-labeled selected-item
commands with facility icon/Stuff and optional DesignatorHotKey. Its action designates directly,
not selecting a drag tool. Cell/drag methods exist, choosing the top eligible Thing per cell and
rejecting fog/out-of-bounds; no exposed Architect Orders registration was found. Do not claim an
Orders tool from class inheritance alone.

`init_Command_Action` returns null when no cached built facility exists. That is **not action
suppression**: DesignatorPatches returns true when this result is null, allowing native
CreateReverseDesignationGizmo to call CanDesignateThing and create an ordinary command. With the
visibility flags off, an eligible missing-bench item can therefore receive a pending order; only
the custom facility-icon command is absent. Verified native1.6 source and current1.7.4998 Designator
fallback agree. FindBuilding takes the first recipe-user type with a colony building, not nearest;
an existing building makes cachedResearched true, otherwise it tests any user's construction research.
CanDesignateThing requires spawned ThingWithComps, applicable comp, no identical designation and
cached Allowed. Allowed manually checks disallowed special filters, not the full effective ingredient
and material plan. Cache refresh follows selection change or failed job, not every item/facility/
research change. Settings.ResetSelectTick uses wall-clock components with hour multiplier360000,
creating possible collisions; it is not a trustworthy state revision.

DesignateThing **RemoveAllDesignationsOn** the target before adding its own. Owned composition will
replace only a processing choice, preserving unrelated orders. ApplicableDesignationThingComp's
cached nullable Allowed has no save method; saved designations still reference dynamic Def identities.

## Work, effects and lifecycle

WorkGiver_MicroRecipe finds spawned designation targets. HasJob checks item reservation and scans
colony facilities for forbid/reservation, IBillGiver, CurrentlyUsableForBills and reachable interaction
cell at **Danger.Deadly**. JobOnThing additionally rejects burning/forbidden input. It does not
repeat the full native recipe skill, availability and ingredient scheduler checks. Sorting returns0
for all fueled/unfuelable candidates, so normal stations are not actually distance-sorted despite
the changelog's closest-bench claim. Empty-fuel candidates can return native refuel jobs, but early
CurrentlyUsableForBills filtering can prevent that; if every candidate is unusable, the selected
bench can remain null before subsequent helpers. These are static findings, not reproduced runs.

It clears obstructing bench items, constructs the selected job with that bench, a one-item target
queue and **countQueue=[1]**, and creates recipe.MakeNewBill. Production bills increment repeatCount
and use DropOnFloor. That bill has no billStack and is absent from the bench list. Native DoBill's
driver still reserves bench, interaction and queued ingredients; do not misreport all reservations
as missing. The source bypass is in the workgiver selection/eligibility path.

Global Bill.DeletedOrDereferenced prefix skips the original for **every** null-stack bill, returning
bill.deleted. Job.ExposeData prefix deep-saves **billSaved** when the job's bill is null or has no
stack, again not limited to source jobs. EndCurrentJob transpiler finds CleanupCurrentJob and injects
UnmarkDesignation with hardcoded local3. It removes the designation for a surviving target; on any
non-success resets eligibility and re-adds it if CanDesignateThing succeeds. This can retry unchanged
failures indefinitely and does not supply an explicit suspension/completion ledger.

Native recipe processing grants its real products/skill/effects; the source does not define a custom
yield policy. One successful nonconsuming operation is intended to clear its designation. Actual
provider callback compatibility remains a separate research/verification requirement.

## Settings, patches and XML

The three static fields are explicitly initialized false: hide_unresearched, hide_empty, hide_inactive.
Scribe missing-key defaults are **true,false,false**. Current native Mod.GetSettings and
LoadedModManager.ReadModSettings confirm absent file→new settings object→false,false,false, while
loading an existing record with absent research key takes true. No normal settings file was read or
changed. Selected replacement uses consistent false,false,false and common global/save inheritance.
The first visibility check calls RecipeDef.AvailableNow, covering ideology/faction prerequisites as
well as research despite its research-only label. The owned label makes that existing scope explicit.
Source exposes three global checkboxes plus raw-English BetterLoading warning; catalogs are English
and Japanese only. No new bottom tab was found.

Constructor reflects ThingFilter.disallowedSpecialFilters and throws if missing, then unguarded
PatchAll owner **net.avilmask.rimworld.mod.MicroDesignations**. Startup normally populates after
StaticConstructorOnStartupUtility.CallAll. Type-only detection of
BetterLoading.Stage.InitialLoad.StageRunStaticCctors:PreCallAll changes this to CrossReferences'
static constructor. ReverseDesignatorDatabase patch reflects desList and throws if missing.
Selector Select/Deselect/Clear reset the wall-clock cache; Designator.CreateReverseDesignationGizmo
prefix returns the Action_Designator command where available. Work Tab adapter probes
WorkTab.PawnPriorityTracker.get_Item and redirects ref WorkGiverDef to the underlying workgiver,
without canonical package/signature admission. No executable semantic startup check scheme exists.

Selected XML declares **17** designation presets: flesh/mech butchery, cremation, apparel/weapon
burning, generic/five named stone cuts, slag, weapon/apparel smelting/destruction and RecycleApparel.
Icons include slaughter/recycle/burn/chisel. The first11 bind Deconstruct or TogglePower; named stone
and RecycleApparel presets have no explicit binding. Several actions on one item can share a key.

RecipeJobs.xml uses vanilla conditional/add patches when MendSimpleApparel or RecycleApparel exists,
creating RecipeJobDef mappings for MendAndRecycle.RecipeWorkerWithJob_Mend/Mend and
RecipeWorkerWithJob_Recycle/Recycle. It does not check package/type shapes. The commented Ignore
example is not an active XML modification. Owned external XML needs XML Extensions package/content
guards, additive merge and the required dependency; no copying of this weaker admission.

## Native1.7 comparisons and balance

Targeted decompiled WorkGiver_DoBill, JobDriver_DoBill, Bill, Toils_Recipe, RecipeDef, IngredientCount,
Mod, LoadedModManager and Designator were read at the relevant methods. WorkGiver_DoBill checks
UsableForBillsAfterFueling, reserves interaction/bench, refuels, checks required work type,
PawnAllowedToStartAnew and FirstSkillRequirementPawnDoesntSatisfy. IsUsableIngredient applies
Bill.IsFixedOrAllowedIngredient and a matching ingredient filter. Fresh Bill copies
defaultIngredientFilter; hard filters and explicitly configurable defaults are not identical.
Bill.DeletedOrDereferenced directly dereferences billStack.billGiver, explaining the source bypass
but not justifying a global replacement.

IngredientCount.CountRequiredOfFor uses ceiling(count or Worker.GetIngredientCount divided by
IngredientValueGetter.ValuePerUnitOf). A single ingredient entry may therefore require many units.
RecipeDef.AvailableNow checks research plus faction/ideology availability. AllRecipeUsers includes
both direct recipe users and reverse building recipe lists. The native driver carries/places actual
queued inputs and runs Toils_Recipe work/completion. Completion makes products, consumes ingredients,
notifies iteration/records/tales/quests and places products; preview cannot safely execute it.

Read relevant Core recipe/building XML. Current examples, not new fixed balance settings:

| Operation | Current native comparator |
| --- | --- |
| Stone cutting | One chunk;1600 work, GeneralLaborSpeed, Crafting learn factor0, special stone products |
| Steel slag | One ChunkSlagSteel or allowed mech slag;400 work, SmeltingSpeed;15Steel |
| Weapon/apparel smelting | One equipment unit;1600 work, SmeltingSpeed, actual smelted products |
| Weapon/apparel destruction | One unit;400 work, no products; default ingredient choices exclude valuables but are not identical to hard eligibility |
| Combined SmeltOrDestroyThing | Listed on current smelter alongside older recipes; recipe workAmount400 destruction or smeltingWorkAmount1600 for smeltable items, corresponding actual products |
| Flesh butchery | One eligible corpse;450 work, ButcheryFleshSpeed, Cooking and ButcheryFleshEfficiency; rot restrictions, animal default choice |
| Mech corpse processing | Shred300 or smash450 work; Crafting/efficiency; hard mech eligibility and separate friendly-corpse defaults |

S022's25%/1600-work recycling is separate; do not apply it to native smelting. S023 remains actual
melee disposal. S015 bulk smelting does not become a one-unit recipe. S054's repair knowledge applies
to any admitted mending action. S052/S053 must receive real activity and keep facility power/fuel.
No cost/research/skill shortcut is intended. Finalized native/provider progression, job reachability
and actual output comparisons remain MDR-Q3, not accepted by XML inspection alone.

## Public contracts and bounded consumers

Public source types include RecipeJobDef:Def (jobName,workerClass), DefExtention:DefModExtension
(Ignore), DesignatorHotKey:DefModExtension (hotKey), MicroWorkGiverDef:WorkGiverDef (workGiverDef,
recipeDef,designationDef,jobDef), plus action/comp/settings classes. Source XML consumes the first
three. Dynamic designation/workgiver names, billSaved, three setting keys and source type identity
can matter to saves/consumers; no implicit owned migration or alias is selected.

A completed bounded `rg -a -l -i` scan of installed Workshop `.cs/.dll/.xml` for MicroDesignations
and RecipeJobMend/RecipeJobRecycle found own versions, Simple Recycling's compatibility XML and
LayoutHarness sample modlists under3745434121. The latter are evaluation modlists, not an executable
API binding. No other executable consumer was found by that normal-string scan; it is not an
exhaustive UTF16/reflection or all-internet absence claim.

**Simple Recycling**, Workshop2079862064, `lanilor.simplerecycling`, Lanilor/Proxyer, About1.5.4,
supports1.1–1.5. Read About, LoadFolders and
`MicroDesign/Patches/SimpleRecycling_RecipeJobs_MD_patch.xml`. Its1.3/1.4 LoadFolders gates
MicroDesign on `avilmask.MicroDesignations`;1.5 selects only1.5, so this is a dormant historical
adapter in its latest explicit folder. XML creates **MicroDesignations.NeolithicRecipeJobDef** and
**MicroDesignations.SimpleRecipeJobDef**, neither present in selected1.6 Micro Designations. This
is an actual historical consumed binding, not a working modern contract to emulate. Its Steam page
returned a regional hub error during this pass; local public metadata/patches are the evidence.

Installed About searches for source gate families notfood.MendAndRecycle, Fluffy.WorkTab,
BetterLoading and dankpyon.medieval.overhaul returned dependencies/mentions but no exact providers
among matches. Better Work Tab is a different installed package, not proof of the source WorkTab
adapter. The inspected deprecated Medieval Overhaul page describes an old1.3–1.4 release; it does
not identify a current installed affected build. No optional-provider compatibility is accepted.

## Selected corrections, omissions and remaining work

The owned draft deliberately replaces loose first-Def/count checks with exact one-unit eligibility;
supports safe stack quantities; preserves unrelated orders; gives nearest eligible bench and native
worker/fuel rules; keeps valid context and scoped save/completion state; refreshes real conditions;
retains planned missing-bench orders with explicit waiting reasons; uses consistent inherited
settings with a clearer colony-availability label; resolves ambiguous keys;
and suspends unsupported failures instead of looping. These are corrections/extensions, not source
parity claims. Exact costs/consequences and normal interrupted-work behavior remain native.

No public Architect Orders tool, arbitrary multi-input job support, missing historical API facade,
source settings/save migration, unverified custom worker or BetterLoading patch is silently promised.
MDR-Q1–5 record exact targets/context, external contracts, finalized comparators, shared lifecycle and
reviewed native acceptance. Nothing implemented; all implementation tasks remain unchecked.
