# S058 — SmarterDoctors (Continued) research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S058/P1-52. Inspected 2026-11-04. Documentation, selected binary, included source,
original source, native selection contracts and bounded consumer search inspected. Observable draft
independently reviewed and strictly validated; no implementation, live compatibility or gameplay
acceptance claimed.

## Documentation, identity and provenance

Read the [continued Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3744377058),
[original page](https://steamcommunity.com/sharedfiles/filedetails/?id=2324533248) and
[original repository](https://github.com/maarxx/SmarterDoctors) before filling source gaps. The
continued page initially returned a regional-unavailability response; a later read returned its
description. Generic Steam removal/incompatibility banners are not evidence of installed behavior.
The continued description repeats the original's medical, feeding, training, farming, construction
and rescue scope. Comments questioning effectiveness or comparing medical mods are research leads,
not verified conflicts. No settings or bottom tab are advertised or present in selected code.

Continued author **Zaljerem**, original **Maarx**; package `zal.smarterdoctors`, Workshop3744377058,
installed root `F:/Steam/steamapps/workshop/content/294100/3744377058`. About version1.0.1 advertises
**1.6 only** and requires Harmony. No LoadFolders.xml exists; inspect the 1.6 assembly as a reference,
not a supported 1.7 build. Changelog1.0.0 (2026-06-13) ports1.4 to1.6;1.0.1 (2026-06-21) fixes new
training types, crediting JoeOwnage67. Assembly version0.0.0.0, net48.

| Input | Identity |
| --- | --- |
| Selected 1.6/Assemblies/SmarterDoctors.dll SHA256 | `0E4FB6312F285E431C0F3517B283700FB365C44F16FE23D0DEBE96000B32ED8A` |
| Included 1.6/Source/HarmonyPatches.cs SHA256 | `E0E013F54A4EB0A8C56AF5087AEB1A66B6B7217E65D9080267DF90E09AA4DFC4` |
| About/About.xml SHA256 | `4CACA145499670398B5DF5E41D9902C3AE6CB0214110A81C902840BF99DF2355` |
| Original Git revision | `ec30dfb78fa8bf1494541e7e9cdc1142e0ebcd7e` |
| Native Assembly-CSharp1.7.4998rev1299 SHA256 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Ignored evidence: `artifacts/Refinement/megamod-spec-20261104/smarter-doctors/`, with `compiled`,
`original-source`, `native` and `consumer-hits.txt`. Read the entire included behavior file and all
five reconstructed behavior classes, assembly metadata, project, About/changelog and original README.
The complete original-to-included behavior diff adds Comfort, null-next-training handling and unknown
training fallback; otherwise behavior matches. The continued project references Krafs1.6 and Harmony
2.4.2 without shipping the latter. No upstream build executed. Decompiler could not render the
Prioritized attribute's getter enum; included source supplies that declaration. No license file or
reuse grant was found in inspected roots/headers. Independent implementation only; do not copy
source, binaries or artwork.

## Complete selected behavior

Main installs unguarded Harmony owner `com.github.harmony.rimworld.maarx.smarterdoctors`. Two base
WorkGiver_Scanner postfixes force Prioritized=true for eight scanner kinds and replace only a zero
GetPriority result. A third postfix broadens WorkGiver_RescueDowned.HasJobOnThing after false.

| Work | Exact source rule |
| --- | --- |
| Tend | HasImmunizableNotImmuneHediff => float(10^20) plus maximum visible not-immune Immunity−Severity; otherwise finite blood-loss death => float(10^10) minus ticks; otherwise0 |
| Feed patient | Food level exactly0 and first Malnutrition present => its severity; otherwise0 |
| Train | Native next training; Tameness500+(10−steps), Haul400+steps, Rescue300+steps, Release200+steps, Obedience100+steps, Comfort50+steps, unknown25+steps; no next =>0 |
| Cut plants | Exact Plant_Haygrass80; everything else100 |
| Sow and grower-harvest | New FertilityGrid(target.Map).FertilityAt(cell), minus1 if worker-map growing zone's chosen crop is Plant_Haygrass |
| Finish construction frames | Exact TrapSpike25, TerrainDef50, other buildables100 |
| Rescue | IsColonist50, AnimalOrWildMan25, other pawn0 |

The additional rescue postfix preserves an existing true result, otherwise repeats old rescue rules
**without the faction restriction**. It excludes a Tame designation, non-downed, in-bed/charging,
unreservable, enemies within40cells and caravan-taking targets. Babies require medical rest and a
safe bed; others use RestUtility.FindBedFor with social properness off. It does not update ShouldSkip
or JobOnThing, and can overturn another mod's deliberate false result. This undocumented eligibility
change is separate from the advertised colonists-before-animals ordering.

## Differences, defects and selected corrections

- `float(10^20) + ordinary disease gap` erases the gap: bounded numeric inspection gives the same
  float for ±0.5. `float(10^10)−100` and minus200 also coincide. Native approximate comparison adds
  further ties. These are source-analysis findings, not live failure claims. Preserve distinct
  ordering directly rather than treating enormous magnitudes as a contract.
- Source takes the **largest** immunity lead and adds it to a higher-is-better score. Even without
  lost precision that favors safer disease and ignores a more dangerous concurrent disease.
  Draft selects the smallest immunity-minus-severity gap per patient and ascending across patients.
- Public wording says fatal disease, but code accepts visible naturally immunizable disease without
  a lethality filter. Draft retains that broader membership explicitly, including nonfatal disease,
  and disease-before-bleeding class precedence provisionally. It is **not optimal medical triage**;
  SD-Q1 records emergency and later medical-feature policy review before implementation.
- Feeding only prioritizes currently empty-food patients with malnutrition, not every patient with
  residual malnutrition. Retain this threshold; no forced feeding or change to food choice.
- Training category order is real source behavior absent from the broad description. Retain it as
  ordered categories, then steps, rather than allow very large modded step counts to cross categories.
  It does not reorder one animal's requested training or skip prerequisites.
- Haygrass minus1 is a fertility penalty, not a tie-break: rich hay1.4 becomes0.4 and loses to other
  crop0.7. Draft follows documented fertility-first intent, using hay only for equal fertility.
  Use actual harvested plant, requested crop for sowing, and support native growing buildings too.
- New FertilityGrid per candidate allocates a wrapper; current native constructor stores only Map,
  not a whole initialized grid. Reuse native map fertility access; do not falsely claim a full-grid
  allocation. Current building fertility and pollution are included.
- Frame ranking does not govern material delivery, blueprints, repairs or deconstruction. Lower
  priority floors/traps remain buildable and may be forced explicitly. No resource/work-cost changes.
- Keep advertised rescue ordering, **omit the undocumented false-to-true rescue override**. Native
  eligible rescue and explicit player orders remain. This is an intentional loss of automatic
  non-faction rescue, not a claim of unchanged source parity; SD-Q2 retains safe-extension selection.

## Current native contracts inspected

WorkGiver_Scanner defaults Prioritized=false/GetPriority=0. JobGiver_Work keeps enabled work types,
priorityInType groups and emergency/explicit orders; it validates forbidden/HasJob before target
selection. Prioritized Thing selection uses GenClosest global reachable search; larger priority wins,
with approximate equality and horizontal squared distance. Cell selection uses greater priority and
exact equality/distance. A new comparator must not lose distinctions through either path or mix its
scores with unrelated non-neutral provider scores. Existing compatible hooks alone are not a reason
to refuse admission. Current work selection is not permission to interrupt a running job.

Tend preserves laying/self-tend, medical permission, reservations, mutant-care and aggression checks,
then chooses ordinary medicine. HasImmunizableNotImmuneHediff excludes injuries/missing parts and
requires **Visible**, natural immunity possibility and not fully immune. Thus hidden-disease mismatch
was considered but is **not found for this native build**. Its helper and source delta selection still
need one consistent population. No fatality filter is present.

FeedPatient uses SpawnedHungryPawns, excludes self/babies and respects workgiver species, ShouldBeFed,
warden division, reservations, food restrictions and available food. Its normal food search already
permits inventory/pack-animal supplies. This feature ranks patients, not meals; S055/S057 own meal
selection boundaries. A foodless pawn is not an eligible patient and must not throw in ranking.

Training uses same-faction animals, existing next wanted/trainable task, skill/interaction/food and
too-recent gates. It may return a gather-food job before training. Current GetSteps is internal;
NextTrainableToTrain uses native ordered wanted/CanBeTrained entries. Core trainables have steps:
Tameness5, Obedience (label guard)3, Release (attack)2, Rescue2, Haul7. Comfort is not in inspected Core
Trainables.xml; treat it absent-safe, not a hard DLC/provider requirement. No new trainable is added.

Grower candidates include zones and Building_PlantGrower. Sow retains grow season, crop, skills,
vacuum/room and blocking-work checks. Harvest retains mature/yield/permission/reservation checks.
Harvest JobOnCell creates a same-room local queue over40 radial cells, up to2400work after its anchor,
and distance-sorts queues with at least3entries. Ranking the anchor **does not promise every plant in
the batch is fertility-sorted or that the anchor is physically harvested first**. Retain native batch
behavior. Relevant contracts were read; not every unrelated body in large utility classes was read.

FinishFrames requires own-faction completed-resource frames, valid touch, obstruction handling and
CanConstruct. Rescue ShouldSkip still checks downed same-faction pawns. Current CanRescueNow uses
same faction for automatic work, WantsToBeRescued, forbidden state, enemies within25cells unless
forced, reservations/reach. WantsToBeRescued also excludes deactivation, forbidden medical mutants,
some mental states, slaughter targets and downed ritual roles. Old source fallback can bypass these.
FindBed matches the old source call for ordinary patients; keeping native eligibility avoids copying
an obsolete40cell gate or claiming neutral-only behavior the source does not actually enforce.

## Consumers, settings, UI, persistence and composition

Bounded case-insensitive ASCII/UTF16LE scan of installed Workshop DLL/C#/XML/JSON for SmarterDoctors
found only its own files and RimPy/replacement-catalog metadata (Workshop1847679158/3396308787).
Inspected catalog hits map original `maarx.smarterdoctors` to continued `zal.smarterdoctors`.
No executable caller found in this scope; not proof of universal absence. Internal Main,
Computations and patch types/public static compute methods, assembly name and Harmony owner are
potential reflection surfaces, but no compatibility facade/package spoof is justified by this scan.

No own settings, saved state, Defs, XML mutations, language files, window, gizmo or bottom tab.
Use existing work, health, training and growing controls; those are entity gameplay state. Retain
automatic rules without inventing switches. Later genuine tuning uses shared global/save settings.
Keep native candidate listers/current eligibility. No additional persistent pawn/Thing cache or
serialized priority values; shared index consumers reuse established ownership where needed.

S024 cleanup yields to continuing care; S042 self-tending initiation, S056 emergency surgery,
S062 Smart Medicine, S124 Drug Response and S134 Priority Treatment remain distinct. Apply S058
ordering once when automatic work actually selects among eligible targets; do not create treatment,
drug orders, suppress emergencies or alter explicit choices. Later research must specify any actual
overlap directly rather than patch owned behavior. S008 urgent work, S006 linked growing and
S060/S061 construction sources retain their own eligibility/operation scope; no inferred conflict.

If original or continued source is active, suppress the entire owned S058 overlap and explain that
the source supplies its own rules. Other incompatible branches fall back independently, with each
work family's selection/priority participation admitted atomically and bounded startup warnings.
No foreign unpatching, assembly bundling, source configuration import or broad drop-in promise.

## Remaining questions and acceptance boundary

| ID | Outstanding work | Draft position |
| --- | --- | --- |
| SD-Q1 | Medical class balance and S062/S124/S134 composition | Retain visible incomplete-immunity disease before finite bleeding before other tending; smallest disease gap first; nonfatal disease included explicitly; no invented emergency override |
| SD-Q2 | Undocumented automatic non-faction rescue | Omit unsafe false-to-true fallback; native automatic/forced rescue remains; only revisit through a separately safe explicit contract |
| SD-Q3 | Effective beta/provider selection and shared-data cost | Confirm actual comparison/override seams, dynamic foreign ordering, current listers/fertility/training and benign-patch tolerance before code |
| SD-Q4 | Implementation and causal acceptance | Honest RED/GREEN, target specs, package/review and real doctor/feeder/trainer/grower/builder/rescuer workflows on reviewed build |

Final balance must compare delay of bleeding care, nonfatal disease promotion, animal training
starvation, hay/floor/trap postponement, walking cost and batch behavior. No claimed improvement in
survival, throughput or performance without measurements. No runtime task is complete here.

## Reviewed checkpoint

Independent scoped review found no issues. The draft has **10 requirements, 41 scenarios and
24 unchecked implementation tasks**. Typed strict validation passed **80 items, zero failed** after
making SW03's normative verb visible on its first line; no behavioral change was needed. Eight
scoped Markdown documents passed local-link and whitespace checks.

The semantic pass compares ordinary native distance selection, documented source priorities and
actual selected arithmetic: disease gaps/bleeding times must remain distinct; native training steps
and prerequisites stay meaningful; fertility1.4 hay outranks0.7 other crop while equal fertility
defers hay; supplied bed/floor/spike-trap examples preserve all costs. Medical class safety,
concentrated-training fairness and sustained low-priority work still need finalized gameplay
measurement. No new loot, costs, research unlocks or work-speed bonuses are introduced. This review
is specification acceptance only, not runtime acceptance or a performance claim.
