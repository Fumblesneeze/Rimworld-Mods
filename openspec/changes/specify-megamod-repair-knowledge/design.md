## Context

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S054/P1-47 replaces I Cant Fix That's repair-knowledge restriction.
This is documentation only. See [research](../../../mods/megamod/docs/refinement/2026-11-03-i-cant-fix-that-research.md)
for documentation-first provenance, complete current-source coverage, selected compiled comparisons,
native traces, installed-provider boundaries and hashes. No runtime compatibility is established.

Source package `Mlie.ICantFixThat`, Workshop2780971535, selected1.7 DLL, source revision
`7761227b5ab66950a724307daad0a2219b00e2ff`. Its six core files and eleven adapter families use
independent unconditional PatchAll calls. They do not provide our required semantic admission,
safe lifecycle or common settings. All12 source projects contain a kill-all-RimWorld build target;
none was built or run. Downloaded source and assemblies remain read-only inspection inputs.

## Goals / Non-Goals

Goals: one research policy, understandable refusals, explicit alternative-route semantics,
consistent settings, native behavior preservation, and safe conditional repair-provider adapters.

Non-goals: implementing repairs or maintenance absent from the game/provider; changing costs,
durability or work rates; restrictions on using salvaged equipment, recycling, construction or
medical care; importing source settings; pretending to provide the upstream assembly/package API.

## Decisions

### 1. One policy and a bounded production-knowledge graph

Use a single owned policy consumed directly by owned work and narrow external adapters. Build the
production-research relation from finalized definitions once for that content generation, beginning
with native recipe/user lists. No per-source Thing scan or repair-target cache. Read current research,
faction technology, difficulty, God mode and effective settings at decisions; do not cache a global
boolean that survives changes. Invalidate derived definition relations when supported content changes
occur and clear game-scoped references on teardown. Shared metadata consumers can reuse the same
finalized recipe graph rather than maintaining a second Ctrl+F/hauling index.

Source dictionary is last-applicable-recipe-wins. Its singular research field takes precedence over
plural and its bench fallback searches above Archotech. Native ProducedThingDef excludes all recipes
with multiple products or special products. Replace these accidents with RK02's OR between routes,
AND within a route, explicit multi-output enumeration and recipe research before bench fallback.
Use the legitimate user union, including ThingDef.recipes, rather than only RecipeDef.recipeUsers.

Do not use RecipeDef.AvailableNow wholesale: it includes non-research faction/ideology restrictions.
Only explicit research participates in manufacturing knowledge. Do not add a new bench-research
requirement to an explicitly researched recipe; fallback-only is the narrower interpretation of
upstream intent. Workbench availability on the map and ingredient/skill availability remain work
decisions, not learned knowledge. Building direct research and minimum tech remain additional checks.

Identify genuine manufacture, not repair/refurbishment returning an input. Admit dynamically produced
outputs only with an explicit proven output contract; do not guess at specialProducts. No identifiable
manufacturing route adds no fabricated research blocker, while other gates still apply. Ambiguous
provider routes need a recorded adapter/research decision under RK-Q2 before compatibility is claimed.
Avoid recursive recipe chasing: the fallback reads construction research for a legitimate bench,
not a recursively solved graph of every component ingredient.

### 2. Preserve work rules and correct maximum-tech regression

Native WorkGiver_Repair retains HomeArea, reservation, fire, deconstruction and repairability checks.
FixBrokenDownBuilding additionally retains actual broken state and available industrial component.
Drafted repair retains manipulation/work eligibility and an existing disabled NoPath result. Actor
scope is explicit: the colony categories included by source plus player-owned admitted automation;
do not apply player knowledge to another faction's work merely because a common helper is called.

Source also applies ThingDef.maxTechLevelToBuild, contradicting its author statement that higher tech
can repair lower tech. Core HoopstoneRing/GameOfUrBoard demonstrate the mismatch. Ignore this maximum
for repairs, but retain minimum tech, direct research and source's Difficulty.AllowedToBuild trap/
mortar/turret restriction, with distinct feedback. Native God mode bypasses only our extra policy.

### 3. Common settings, consistent defaults and contextual feedback

Work → Repair knowledge uses foundation's global/save rows and lifecycle. Ceiling Off, allowance1,
integer0..7 follow source fresh-object defaults; source's loaded-missing-key true/5 discrepancy is
deliberately not preserved. No separate singleton, foreign settings import or source keys. Resolve
effective changes at next offer/start/commit; inactive allowance is retained. Display current colony
and maximum level only with an active game. All seven baseline languages need labels/reasons.

Preserve native false and already-disabled menu results. Add a reason only where our policy actually
refuses an otherwise eligible action. Filter candidate collections without turning one excluded
target into a repeated failure loop. There is no source bottom tab or per-entity configuration here;
native repair/work surfaces remain contextual and God mode remains top-debug-only.

### 4. Complete lifecycle admission, not isolated helper hooks

Native repair gradually mutates HitPoints in JobDriver_Repair. Breakdown repair consumes a component
before its success/result step in JobDriver_FixBrokenDownBuilding. A selection postfix alone cannot
enforce RK06 after settings change. A repair-path atomic group must admit offer/start and every
relevant result/consumption seam before any of its restriction patches is installed. For continuous
repair, recheck before the next HP increment; for breakdown/mending, before irreversible consumption
and output. Preserve legitimate earlier work and ordinary safe interruption, not a synthetic refund.

VFEAncients' inspected1.5 mending RepairItem prefix sets target HP and returns it, while ingredient
consumption preserves the gear and consumes materials. The source restriction prefixes this prefix
and can return false without setting the outer recipe-products result. This is a static lifecycle
risk, not a reproduced bug. Do not copy it. Prove an earlier safe refusal plus completion guard or
leave the whole provider branch native. Glittertech's selected working target is not rechecked by
CanRepairThing at each RepairTick; its admitted group needs a current-policy check before repair.

### 5. Optional provider inventory and admission boundaries

These package gates and targets are source contracts, not promised current1.7 support. Qualify
method shapes and caller context before using them; helper names alone do not prove actual repair.
The policy gates target equipment, not arbitrary material ingredients. Preserve native/provider
false, cost, range, power, work, donor and recovery behavior.

| Source folder / package gate | Source target | Owned adaptation boundary |
| --- | --- | --- |
| FluffyBreakdowns / theeyeofbrows.fluffybreakdowns or fluffy.fluffybreakdowns | Fluffy_Breakdowns.WorkGiver_Maintenance.HasJobOnThing(Pawn,Thing,bool) | Colony maintenance plus work lifecycle; verify actual installed identity before admitting either alias |
| RepairWorkbench / Acruid.RepairBench | Repair.WorkGiver_Repair.FindDamagedItems(Pawn,Thing,Bill) | Filter List<Thing> without blocking other candidates; preserve actor scope and completion |
| RepairableGear / SteveZero.RepairableGear | RepairableGear.ThingExtensions.IsRepairable(Thing), CanBeMaintenanced(Thing) | Helpers have no actor context; trace actual callers and avoid foreign-faction/global UI effects |
| RepairItems / Moony.RepairItems | MoonyRepair.RepairUtility.PawnCanRepairEver | Source resolves by name; exact signature and actor/lifecycle still require admission |
| MiscWeaponRepair / Haplo.Miscellaneous.WeaponRepair | WeaponRepair.CompWeaponRepairTwo2One.GetAvailableTwinThing(Pawn) | Identify repaired target and donor; filter safely without losing target or consuming donor on refusal |
| MendAndRecycle / notfood.MendAndRecycle | Bill.IsFixedOrAllowedIngredient(Thing/ThingDef), recipe names MendSimpleApparel/MendComplexApparel/MendSimpleWeapon/MendComplexWeapon | Source gates every ingredient; owned check distinguishes repaired equipment from materials and excludes recycling |
| VFEAncients / VanillaExpanded.VFEA | VFEAncients.HarmonyPatches.MendingPatches.RepairItem, RecipeExtension_Mend | Safe pre-consumption group; no hard provider reference or copied prefix-of-prefix behavior |
| LTSMaintenance / LimeTreeSnake.Maintenance | Maintenance.WorkGivers.WorkGiverDoApparelsMaintenance and WorkGiverDoWeaponsMaintenance.HasJobOnThing(Pawn,Thing,bool) | Colony maintenance; preserve provider eligibility and prove ongoing-work check |
| OgRepairYourGear / Og.Repair.Your.Gear | OgRepairGear.WorkGiver_OgRepairGear.HasDurability | Helper usage/actual repaired target, actor and completion must be proved |
| GlittertechExpansion / Ushanka.GlittertechExpansion | USH_GE.CompGlittertechRepairer.CanRepairThing(Thing) | Player-owned automation with ongoing RepairTick guard, native power/range/rate and safe reselection |
| SimpleMending / ComfyCuddles.WithEuterpe | ComfyCuddlesWithEuterpe.RepairUtilities.TryGetRepairCost | Cost helper name does not prove repair commitment; full consumer/lifecycle admission required |

Only VFEAncients (advertises through1.5) and GlittertechExpansion (through1.6) were found among these
installed Workshop packages. Their source and critical selected compiled behavior were inspected.
Other providers remain source-advertised gates; no installed/current1.7 claim. Missing optional
packages are quiet. Present incompatible shapes warn and leave the provider's complete path native.
Future owned repair code calls the policy directly; do not preserve an internal Harmony stack.

### 6. Per-patch semantic target documents and rollback

Create stable RK target documents before implementation. Candidate families:

| Family | Minimal predicate beyond callable shape | Failure boundary |
| --- | --- | --- |
| Core repair/breakdown offer | Result means native eligibility for supplied pawn/target; preserve false and reason | Respective complete offer/start/commit group |
| Drafted repair menu | Result/action belongs to clicked repairable target; enabled/disabled reason and selected actor available | Ordinary repair group if required for coherent coverage |
| Continuous repair | Actual target HP write and owning job/actor, cancellation before write | Complete ordinary/provider repair path |
| Breakdown commit | Actual component consumption and repair-success/result chain, cancellation before both | Complete breakdown path |
| Definition/research/settings lifecycle | Callback changes exactly the relevant facts/metadata; effective refresh semantics | Dependent policy group if correctness cannot otherwise be maintained |
| Optional provider selection/commit | Active canonical package, actual content, method/field/caller shapes, target identity and safe refusal before side effects | That provider's complete owned restriction group |

Validate effective semantics, compatible prefix skipping and transpiler effects, tolerating unrelated
instructions and other owners. Do not use exact hashes, full-body equality or absence of foreign
patches as compatibility tests. Every transpiler validates its incoming stream and returns it intact
on mismatch; never speculatively execute foreign transpilers. Later supported repatching invalidates
and safely revalidates the complete group. Installation failure rolls back only newly added owned
patches. One bounded startup warning per skipped enabled patch contains package/patch/spec ID,
target, predicate/reason, unavailable behavior and true native/provider fallback. No tick spam.

If source Mlie.ICantFixThat is active, suppress the whole overlapping owned restriction, not the
external patches. Future shape validation must include case-normalized canonical package identity.
Core and Harmony are required when implemented. Providers and DLC content are optional. No XML
mutation is planned; if needed, first use requires XML Extensions metadata, package/content/node
guards and merge-preserving operations. Never runtime-edit declarative Def lists as a shortcut.

### 7. External contracts and persistence

Source public Main.CanRepair(Thing/ThingDef), IsValidRepairer(Pawn), GetDisallowedThings and the
MendAndRecycle MendingRecipesList are consumed internally by its adapters. Successful bounded
installed binary/text searches found no external executable consumer, only own assemblies and a
RimPy catalog entry. This does not prove universal absence. Do not recreate speculative assembly/
type/package identities; add a narrow facade only for a demonstrated supported consumer.
GetDisallowedThings enumerates only source dictionary keys, not a reliable all-equipment contract.

No source entity save type was found. Common owned configuration persists normally; no source-data
migration, automatic file edit or whole-source drop-in claim. Disable/uninstall adds no new item
type or mutation of gear; ordinary already-applied HP/resource consequences remain. A later explicit
migration would need its own backup, atomicity, failure restoration and postcondition contract.

## Risks / Trade-offs

- Unknown manufacture route → no invented research; optional ceiling remains useful for uncraftable
  advanced gear. Document this limit rather than silently making salvage permanently unrepairable.
- Recipe/bench alternatives and ignoring maximum construction tech intentionally loosen several
  source accidents → explicit RK02/RK03 scenarios and finalized content comparisons before acceptance.
- Optional helpers lack actor or commitment context → inspect exact providers; unsupported branches
  stay wholly native rather than claiming parity from an installed postfix.
- Knowledge changes mid-job → no late material loss or free result; native recovery workflows are
  mandatory acceptance, especially breakdown, donor and VFE-style mending.
- Source blanket difficulty restriction may surprise players → distinct feedback and documented
  retained behavior, not an unexplained research error.
- Beta and later repatching drift → per-patch semantic admission, bounded warnings, coherent fallback.

## Migration Plan

No runtime/data migration in this change. Implement vertical slices only after their relevant
research gaps are resolved; create target docs/tests with each real patch. Review the exact build,
verify dependencies/package contents, then personally exercise native workflows in a fresh minimized
Gateway run. Roll back an owned failure group to native/provider behavior without touching foreign
patches or source data. All implementation tasks remain unchecked until root acceptance is satisfied.

## Open Questions

RK-Q1: effective beta admission for offer/start/commit, safe interruption and later repatching.
RK-Q2: exact current optional-provider identities/callers/production classification and actual consumers.
RK-Q3: finalized content/progression comparisons for the selected route/default/building corrections.
RK-Q4: future RED/GREEN, seven-language settings/feedback, reviewed-build native acceptance and cleanup.
These are retained work items, not questions asking the user to explain existing mods.
