# Common Sense

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S024 / P1-18**. Source: **Common Sense**, avilmask,
`avilmask.CommonSense`, Workshop **1561769193**. Inspected **2026-11-02**.

Read the [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=1561769193)
before the author's [public source](https://github.com/catgirlfighter/RimWorld_CommonSense).
The [research record](../refinement/2026-11-02-common-sense-research.md) pins public commit
`aed142c6121e104b78afe3b973e1677b1ff601e4`, installed 1.6 assembly version `1.0.9771.40664`,
SHA-256 `2BA4EC439242F3AE190DCE5226A59BCAFDC48C72CE822C11399B021CFB990BF5`, matching shipped
public assembly and ten metadata/XML files, inspected native 1.7 inputs and actual incoming consumers.
The installed source advertises through 1.6; none of this proves 1.7 compatibility or a reproducible
source build. No redistribution license grant was found: independently implement the behavior;
do not copy source code, assemblies or artwork.

## Behavior inventory

All rows are **analyzed; specified with retained gaps; not implemented**. Evidence is the pinned
source/native inspection above; actual in-game acceptance is absent.

| ID | Native action → proposed observable result | Defaults, boundaries and owning contract |
| --- | --- | --- |
| CS-B1 | Starting ordinary work → bounded physical preparation/cleanup, then the same valid work | Cleanup on; five filth objects before/between work; no forced/queued/urgent-care delay; [housekeeping](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-contextual-housekeeping/spec.md) |
| CS-B2 | Eating/social activity or completed tending → permitted cleanup at the appropriate time | Eating cleanup off; post-tend on with zero meaning all of one finite selection; no extra dispensing or postponed urgent care; housekeeping |
| CS-B3 | Selected building choice or ongoing precipitation → control cleanup or postpone snow/sand removal | Building choice true, control hidden by default, hiding retains choice; precipitation postponement on; housekeeping |
| CS-B4 | Gathering bill ingredients → capacity-limited batch and at most one useful storage detour | Both on; S021 shared physical budget and actual quantities; separate packing work; [supplies](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-practical-supplies/spec.md) |
| CS-B5 | Ingredient/food choice → favor eligible perishables without changing explicit recipes, policies or medicine selection | Both on; specified remaining-rot weighting, current radius/access, surgery native; supplies |
| CS-B6 | Meal merging or generating food → preserve relevant ingredient distinctions and optionally show stable inferred history | Separation on, odd-is-normal off, generated provenance off; never override actual history/native merge vetoes; supplies |
| CS-B7 | Inspect recipe/category → current readable information in existing surfaces | Recipe information on, category navigation native; no filter mutation; supplies |
| CS-B8 | Mark gear for unloading, interrupt carried personal supplies, or reach packing opportunity → physical movement with preserved intent | Manual unload/return on; protected items, capacity, no replay, safe cancellation; native packing trigger above 30%; supplies |
| CS-B9 | Seek recreation → eligible outdoors preference and finish a scheduled recreation interval | On; below 40% outdoors preference,95% completion/80% restart, native urgency and manual orders; [needs](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-practical-needs/spec.md) |
| CS-B10 | Social drink, meditation, drug policy or patient visit → actual permitted effects with less wasted recreation | On; specified timing, predicted joy, potential mood, recipient/worker eligibility and physical ingestion; needs |
| CS-B11 | Sleep or seek installed hygiene activity → optional gentle mood movement or appropriate need ordering | Sleep mood off, no anesthesia/cryptosleep or load catch-up; hygiene only when admitted, provider unchanged outside specified windows; needs |
| CS-B12 | Idle movement → optional shelter and considerate destination choices | Both off; actual immunity, safe bounded routes and destination ownership; [wandering](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-considerate-wandering/spec.md) |
| CS-B13 | Change preferences, load a save or activate another provider → clear effective behavior and safe state transitions | [Complete settings/lifecycle contract](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-common-sense-configuration/spec.md); no capacity bypass or startup-patch preference |

## Interactions and composed result

S021 owns combined inventory capacity, exact quantities and kept supplies; S024 consumes those rules.
S005 mixed storage contributes candidates subject to actual bill radius and storage acceptance.
S014 retains a started linked iteration's material, radius and worker choices through all ingredient
trips, cleaning and reload, with one shared quota and current physical eligibility checks.
S009–S012 own recipe browsing; Common Sense contributes read-only facts there. S002 manual recreation
takes precedence over optional opportunities. S029 drug policy, S030 toddler safety, S058/S062 care,
S026/S059 ingredient selection composes directly. S123 [routine personal meals](routine-meal-carrying.md)
now specifies the complete reserve quantity/retention policy through the same packing opportunity,
retaining S024's30% Food trigger and removing its communal entry guard; both remain unimplemented.
The later companion/pen wandering feature can reuse safety facts without changing its own job scope.
No patches against our own implementation or separate per-port Thing/path cache are selected.

## UI audit and relocation

| Source surface | Purpose | Megamod destination / change |
| --- | --- | --- |
| Global mod-settings sections | Preferences and limits | Common ordered feature sections, Global default / Current save / Use global; no source standalone settings implementation |
| Selected building cleaning gizmo | This building's gameplay choice | Contextual building control; hide preference never erases the saved choice |
| Gear tab and optional inventory tabs | This pawn/item's unload intent | Native contextual controls with quantity, Cancel and held/Resume state; optional tabs need admitted adapters |
| Ingredient filter/category information | Read-only explanation/navigation | Native information and integrated Add bill browser; no extra bottom tab |
| Cooled storage room label | Intended room classification | Native room information, preserving competing roles |

Source has no dedicated bottom tab to retain. Global/save preferences use the common load/store/
resolve/render path, dormant overrides and disabled main-menu save controls/callbacks. Apply at the
specified next safe decision; do not restart consumed ingestion or production. Entity choices are
saved gameplay state, not settings. All seven baseline languages and real UI workflows remain work.

## Contracts consumed by other mods or saves

| Consumer / inspected binding | Disposition and required proof |
| --- | --- |
| While You're Up reads/writes CommonSense.Settings.hauling_over_bills and source settings | No writable global facade for save-specific preferences; its active enabled detour wins, unknown state withholds ours; never edit its preferences |
| As above, So below II transpiles private Hauling_Opportunity distance calls | Private source body is not recreated; admit an actual route adapter or skip unsupported cross-level detours |
| SimpleBabyCarry orders before source Harmony owner on StartJob | Preserve actual baby/job safety through admitted explicit ordering, without spoofing the source owner |
| Glittertech Expansion probes assembly-qualified settings for a warning | Probe not reproduced; disappearing warning does not prove a custom fabricator is safe |
| Cash Register disables opportunistic prefixes | Preserve actual job opt-out; no source type binding required |
| PUAH documents direct HaulToInventory workgiver use | S021 direct owned composition replaces the dependency |
| Repository ImmersiveChefs probes exact source package/assembly, setting and cleaning helper | Incoming binding unsupported here; no repository runtime dependency; any consumer change has a separately owned contract |

Research lists canonical package IDs and concrete evidence. Optional hygiene, RPG/Awesome inventory
and Simple Sidearms source adapters need exact package/member/lifecycle admission before support.
No blanket assembly, Def, job or save drop-in compatibility is claimed. Source comp/job/global-setting
keys are not silently imported. A future importer/removal workflow requires its own backup, atomic
conversion, rollback and verified postconditions. Owned save/load retains actual state without replay.

## Implementation and coverage

**Implemented: nothing.** All CS-B1–13 are specification work. [Proposal](../../../../openspec/changes/specify-megamod-common-sense/proposal.md)
records intent; [design](../../../../openspec/changes/specify-megamod-common-sense/design.md) records technical
boundaries; [tasks](../../../../openspec/changes/specify-megamod-common-sense/tasks.md) remain unchecked.

Deliberate deviations include exact total cleaning budgets, protected explicit/urgent work, one
detour, mandatory capacity, safe inventory recovery, destination bedroom ownership, actual 0..1
hazard immunity, stable inferred meal history, read-only summaries, independent polite default,
sleep midpoint clamp, simpler explicit social minimum and preserving hygiene behavior outside the
selected windows. Disabling recreation completion fully restores native behavior instead of the
source's retained 80% threshold. Manual unload quantities retain prior protection and cancellation
cannot cancel pre-existing S021 cargo; blocked saved orders remain preserved for explicit Resume.
No source whole-driver copies, duplicate packing XML edit, dummy meal recipe,
global preference mutation or capacity bypass. These are proposed owned contracts, not upstream facts.

S125's full opportunity draft now governs the single owned bill-storage detour and its canonical
Hauling > Opportunities preference. S024 provides navigation to that control, not a second editor.
Actual selected-material surplus, resource-to-work improvement and whole-trip bounds replace the
source room/worker-distance gate; only S125 authorizes work-bound equal-priority relocation.

Original Common Sense active withholds owned overlap; preserve cargo and hold explicit unload orders
for Resume. External While You're Up's overlapping detour follows the rule above. Missing/changed optional
integrations disable only their unsupported behavior. Runtime work will require Harmony and XML
Extensions, guarded additive Def/extension/component changes and one intentional packing-threshold
scalar replacement; no whole-list replacement is intended. Shared facts remain map-owned with current
pawn-specific eligibility checked at use. Exact patch targets, code, tests and live evidence do not
exist yet; CS-T1–8 in design are candidate groups, not finished admission checks.

## Verification and acceptance

The [progress ledger](../refinement/specification-progress.md) records strict artifact validation and
independent specification review. CS-Q1–4 retain balance, native lifecycle, provider/composition and
implementation work. No build, runtime test, game launch or compatibility acceptance is implied.

Future reviewed-build evidence must include real cleaning/bills/care, physical inventory transfers,
actual food/drug/recreation effects, constrained wandering, source/provider-present and absent cases,
settings/save transitions and seven-language controls. Preserve exact mod lists, action/result views,
build identity and cleanup. Labor, available food, skill/research/work eligibility, urgency and
shared-data performance require measured native acceptance, not source-derived assumptions alone.
