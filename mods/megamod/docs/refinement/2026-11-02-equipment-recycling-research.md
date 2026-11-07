# Original weapons/apparel recycling: refinement and evidence

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S022**, P1-17, **origin: original**. Inspected **2026-11-02**, sequentially after S021.
[Owning change](../../../../openspec/changes/specify-megamod-equipment-recycling/) specifies future work;
no gameplay implementation or native acceptance. S023 owns on-site actual melee destruction.

## Confirmed intent and reference status

The [interview record](2026-11-01-handwritten-notes.md) confirms weapons and apparel as sufficient
recycling scope. It does not select all Recycle This features, component refunds, rates, skills,
station rules or save/API parity. Recycle This and Destroy Item are research references only.
Micro Designations remains selected as S096 for other item-specific processing; compose directly.
Do not turn a reference source's settings or defects into purported user decisions.

Read public documentation first: [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3253550009),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3253550009) and
[official repository](https://github.com/emipa606/RecycleThis). The description advertises item
orders, bench-based recovery and disposal, vanilla-like returns/work, optional gizmos and complex
refunds. The September 2025 notes add a return-rate preference. Runtime analysis below comes from
the installed 1.7 assembly. The cached public page listed older version folders, but the downloaded
pinned checkout includes 1.7 and its main DLL matches the installed binary exactly.
Generic Steam warnings and author compatibility claims do not prove target-beta behavior.

## Pinned inspection inputs

| Input | Identity |
| --- | --- |
| Reference provider | Recycle This (Continued), author busted_bunny, continued by Mlie; `Mlie.RecycleThis`; Workshop 3253550009 |
| Installed location | `F:/Steam/steamapps/workshop/content/294100/3253550009` |
| Metadata | Supported versions 1.3–1.7, modVersion 1.7.0, Harmony dependency |
| About SHA-256 | `11F935B98A085B421668CD9AEF0DB85DB20D4544C6F621473D1008E2DE69FAE8` |
| Inspected DLL | `1.7/Assemblies/RecycleThis.dll`, assembly 1.7.0.0, file 1.7.0 |
| DLL SHA-256 | `BAA43B9E245B96908211750523177163DD6202CB3DAC35146F6591A672228B64` |
| Informational version | `1.0.0+025c97c6bd0d53f0bda93fe9d60bd08569217d2b` |
| Public checkout | `54ef9c39dc448154657a6f1cceb9cfc31991146c`; its 1.7 main DLL has the same SHA-256 as the installed DLL |
| License | Installed LICENSE.md: MIT, copyright 2020 Mlie; assembly also credits busted_bunny. Preserve applicable notices if substantial source is reused; no source assets/binaries selected for packaging |
| Native comparator | 1.7.4995 rev309, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Artifacts: `artifacts/Refinement/megamod-spec-20261102/recycling/{upstream,installed-decompiled,game}`.
Installed inspection covers recycling designator/workgiver/job, utility/station resolution, startup,
refund helper, settings/UI, reverse designator registration and relevant 1.7 Def/Patch XML. The
destroy job was not adopted or exhaustively analyzed here; that is not this original feature's scope.
Native inspection covers Thing.Smeltable/SmeltProducts, CostListCalculator, GenRecipe and RecipeWorker,
plus source XML for smelting recipes, work stats and three equipment examples. No game was launched.
The five installed 1.7 gameplay XML files also match their checkout counterparts byte for byte.
Public About.xml has SHA-256 `844239C68D16D6072E4E94772816504A0A42E55962C5B89CCE5589206FDA7594`,
which differs from installed metadata. Matching shipped DLL/XML bytes do not prove a reproducible
build from the source tree or full folder equivalence. Independent review prompted this provenance
correction; the downloaded tree and actual hashes take precedence over the cached web listing.

## What the reference actually does

- The designator accepts weapons/apparel only when stackLimit is below 2 and its usefulness test
  passes. That test uses fixed 25% rounded non-intricate adjusted costs or any explicit smeltProducts,
  ignoring the configurable return percentage and GiveComponents. Cell designation then checks
  only category/stackLimit, not the full usefulness test. DesignateThing removes **all** designations.
- Smeltable items use a bench listing SmeltWeapon or SmeltApparel. Other items use a bench listing
  Make_Apparel_BasicShirt or Make_Apparel_TribalA. The lists are built once from finalized-looking
  Def collections in a static constructor; runtime safety of that timing was not verified.
- It finds a reservable bench reachable from the item with Danger.None, allows a fuelable bench
  through UsableForBillsAfterFueling, and can return a native refuel job first. The item must be
  designated, nonburning, nonforbidden and reservable. Each job requests one item.
- The job reserves item/bench, carries to the bench, works, emits products at the pawn, then destroys
  the input. Work is 1600 from its JobDef extension. It uses the input's recipeMaker work-speed stat
  times bench WorkTableWorkSpeedFactor when present and above 0.01; otherwise speed is 1. It checks
  current bench usability during the job only on the smeltable branch. No explicit skill XP award
  appears in the inspected recycling job. Work counters are saved as workLeft/totalNeededWork.
- Refunds iterate CostListAdjusted(actual Stuff), randomly round cost × ReturnPercent × efficiency,
  and optionally omit intricate inputs. The job obtains efficiency from an input recipeMaker stat
  on the **item**, or 1. Explicit smeltProducts are then added at fixed quantities, unaffected by
  rate, efficiency or the intricate filter. No HP/quality multiplier appears in this path.
- It creates products before destroying the input, with no explicit recovery transaction. A throw
  midway through product creation/spawning could leave partial products and the original input;
  this is a code-derived risk, not an observed in-game defect.
- Defaults are ShowGizmo=true, GiveComponents=true, ReturnPercent=0.25; return slider 0–1. These
  are global settings. ReverseDesignatorDatabase.InitDesignators postfix appends both item gizmos
  when ShowGizmo is on, with Harmony owner RecycleThis.patch. No bottom tab.
- Installed XML adds **RecycleThis_Recycle** as a separate work type (natural priority 431), moves
  DoBillsSmelter into it, sets Crafting/Art natural priorities to 430/400, and enables specific
  Fabricor work types. This differs from the description's Crafting-worktype account. Both patch
  files also append the same designator classes to Orders; final duplicate handling was not tested.
- Utility CreateLists can mutate a recipeUsers list for a recipe named RecycleThis; no such recipe
  is present in the inspected 1.7 Def folder. Do not copy legacy mutation or assume it is needed.

## Native baseline and proposed economics

Native Thing.Smeltable rejects relics, then checks item and actual Stuff smeltability. Its default
SmeltProducts returns randomly rounded **25%** of adjusted costs only for non-intricate **smeltable**
materials, then adds fixed smeltProducts. The efficiency argument is unused in this base method.
CostListAdjusted combines declared costs and actual Stuff, converting material volume once; it is
not historical per-instance recipe ingredient provenance. Native SmeltWeapon/SmeltApparel require
one item, 1600 work and SmeltingSpeed. Both inspected speed stats are skill-independent: SmeltingSpeed
uses Sight weight 0.3/Manipulation 1, GeneralLaborSpeed Sight 0.5/Manipulation 1, with WorkSpeedGlobal.

The original feature proposes return of all ordinary construction materials, including cloth/wood/
stone, not just native smeltable materials; complex ingredients are initially excluded. Rate initially
25%, whole-percent range 0–100, applies only to adjusted construction costs. Explicit item salvage
remains fixed; complex exclusion applies to **both** sources. No HP, quality or skill yield multiplier.
These are deliberate choices, not source parity. Unknown custom recovery/content semantics stay
unsupported instead of fabricating outputs or using market value. Zero expected recovery is not
recycling; positive fractional recovery may legitimately round to zero and must be described.

| Source XML comparator | Declared input basis | Proposed default return per item | Labor comparison |
| --- | --- | --- | --- |
| Club, actual Wood/Steel/Stone Stuff | 40 ordinary Stuff units | 10 units of that actual ordinary-volume Stuff | Creation 1200 work; recycling 1600 baseline |
| T-shirt, actual cloth/leather | 40 ordinary Stuff units | 10 actual material units | Creation 1600 work; scrapping 1600 baseline |
| Autopistol | 30 steel + 2 industrial components | 7 or 8 steel, each 50%; no components initially | Creation 5000 work; smelting 1600 baseline |

Small-volume Stuff uses adjusted physical counts: 40 ordinary units at volume 0.1 becomes 400 items,
then 25% gives 100, not 10 or 1000. This is a formula comparator, not a claim every example accepts
every precious material. These are source/XML values, not measured finalized optional-mod values.
The default is lossy; raising recovery to 100% or returning scarce complex items changes progression
and requires explicit setting text and later playability checks. Do not claim no possible third-party
craft/refund exploit when modded manufacture costs or salvage rules differ from declared costs.

## Contracts and composition

The bounded Workshop/local-Mods literal search for RecycleThis in DLL/C#/XML files produced 54 paths
retained in consumer-paths.txt. Outside the provider, substantive hits are **[AV] Mechtech**, Workshop
3339795890, package `Veltaris.Mechtech`: its 1.5/1.6 LoadFolders conditionally selects RecycleThis patches on Mlie.RecycleThis;
Recycle_Patches.xml changes the named WorkGiver's canBeDoneByMechs, and AV_Mech_Scrapper conditionally
adds the named WorkType. This binding will not activate for our package. The inspected mod already
lists Crafting for that mech, but that alone does not establish our actual mech workflow or 1.7
support. No broad namespace/Def/save emulation is selected for this original feature. The scan cannot
rule out unknown dynamic consumers. Source settings keys and work-counter keys are not imported.

S023 destruction and S096 recipe orders share a direct per-item processing choice, preserving
unrelated orders. S021 follows exact quantities during ordinary hauling; it must not convert
personal supplies or inventory cargo into recycling authorizations. S014 linked bill lists and
S017 production accounting remain unchanged because these are item jobs, not invisible added bills.
S033 search and other consumers share existing item facts, with current per-pawn/bench checks.

The S023 lifecycle review clarified the shared quantity proposal: active/suspended processing
choices survive split/merge independently; Resume never activates other portions. Known native
consumption retires the corresponding order; count-only removal from marked/unmarked material
reduces outstanding authorization first. Ambiguous competing choices/suspension states block
remaining work for explicit resolution. One-unit recycling cannot safely infer this from the old
marked count or S021's cargo/kept policy alone. Both observable contracts now state these rules.

## Questions retained for later

| ID | Kind | Open work |
| --- | --- | --- |
| ER-Q1 | Proposed balance / UX | Review 25% adjustable construction return, complex recovery off, fixed salvage exception, protected relic/held eligibility, one-item work and explicit previews; measure costs and progression before accepting runtime behavior |
| ER-Q2 | Exact beta lifecycle | Prove native item/bench reachability, fuel/power, reservations, interrupted work, split/merge, quantity ownership, consumption/output transaction and durable failure recovery |
| ER-Q3 | External contracts / composition | Pin supported custom salvage and bench capabilities, source coexistence, actual mech eligibility and direct S023/S096 processing arbitration; no assumed original-provider migration |
| ER-Q4 | Implementation / acceptance | Individual target admission, focused tests, seven languages, shared-data cost, independent review and actual native designation/work/output/cancel/save/provider evidence |

No question interrupts the deep dive. Detailed design belongs in the design document; observable
rules, including provisional balance decisions, belong in the capability specification.
