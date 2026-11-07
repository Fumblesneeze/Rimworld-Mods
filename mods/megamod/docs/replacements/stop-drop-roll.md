# Stop, Drop, And Roll! [BAL]

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**, S079/P2-08. Reference `balistafreak.StopDropAndRoll`, Workshop2362707956,
author balistafreak with Taranchuk coding credit. Root1.6 source/binary is the inspected reference;
the nested older copy is not another selected mod. Metadata supports1.2–1.6, not claimed1.7 support.

[Research](../refinement/2026-11-04-stop-drop-roll-research.md) records documentation-first inspection,
hashes, native methods/Defs, settings, actual source discrepancies and search limits. No source
license permitting redistribution was found; independently implement behavior without copying code,
assemblies or art. Source assembly version is1.0.0.0; exact byte identity is retained in research.

Owning [proposal](../../../../openspec/changes/specify-megamod-fire-response/proposal.md),
[observable spec](../../../../openspec/changes/specify-megamod-fire-response/specs/megamod-fire-response/spec.md),
[design](../../../../openspec/changes/specify-megamod-fire-response/design.md) and
[unchecked tasks](../../../../openspec/changes/specify-megamod-fire-response/tasks.md) specify future
work. Nothing is implemented or accepted by this documentation.

## Behavior inventory

| ID / requirement | Input/action → observable result | Default, timing and edge cases | Status/evidence |
| --- | --- | --- | --- |
| SR01 Independent scopes | Select feature/group policy → only that group's subsequent behavior changes | Stationary colonists on, other humanlikes/animals off; resistance all on; unclassified pawns unchanged | Source defaults retained, fallthrough corrected in draft |
| SR02 Concentrated self-extinguishing | Able burning pawn reaches its normal response → stationary native action instead of water/random movement |150 ticks, no additional10% selection gate; drafted response retains native priority; ongoing damage | Native job/tree and source prefixes; drafted |
| SR03 Incapacity and lifecycle | Downed/mandatory-bed, removed fire, interrupted/saved action → native safe lifecycle | No forced ability, duplicate record, new timer or persistent immunity | Native behavior plus explicit safeguard scenarios |
| SR04 Present body coverage | New ignition attempt → one eligible outer part sampled | Hands/feet/subparts exempt; legs included; ordinary flame-hit weights; exposed face and missing parts respected | Intentional correction of source leg/local-weight behavior |
| SR05 Independent garment resistance | Sampled part's worn coverage → one check per distinct garment | clamp(1-strength*(heat-threshold)); default4/25%; zero strength no resistance; no natural armor | Source formula and native Rand behavior; drafted with safe finite validation |
| SR06 Native fire rules | Blocked new ignition → no attached fire/job-stop/record; passing → native attachment | Direct/ongoing damage, immunity, water/rain/vacuum, spread and reignition retained; no extra apparel wear | Native attachment/fire evidence; effective-provider boundary still open |
| SR07 Common configuration | Settings edit/reset/load → correct labeled global/save scope | Eight rows, Use global, dormant override, next decision timing,0–10 strength,0–200% threshold, invalid recovery | Replaces source global-only per-draw write; seven languages required |
| SR08 Provider authority | Source active or required semantics incompatible → owned contribution inactive | Response pair atomic; source owns both branches; compatible foreign changes allowed; bounded warning | Target policy drafted, executable checks/native compatibility future |

## Interactions and composed result

S004 weather-ready arrivals determines actual worn clothing; ignition checks consume that current
clothing's effective protection, not a duplicated source inventory or cached rating. S075 uncapped
armor affects direct armor processing/effective stats; this feature neither replays that formula nor
uses direct-damage mitigation as a second ignition check. Owned composition uses direct policy calls,
never patches against megamod. Fire response retains normal action priority relative to other owned
needs, medicine and work behavior; no unsupported conflict is assumed.

No full-map Thing/fire scan or separate cache is needed. Use native pawn anatomy/apparel and real
fire/job targets. Shared search/hauling infrastructure is not a reason to create a new fire index.
Actual optional race/apparel/combat interactions need narrow package/Def/member-resolved adapters.
The source's CE caveat and comments are not evidence of compatibility or incompatibility.

## UI audit and relocation

| Source entry / purpose | Classification | Megamod destination and change |
| --- | --- | --- |
| Mod options: six booleans, IRS/MHA sliders, reset | Configuration | Combat / Fire response; grouped Stationary response and Ignition resistance; common inherited rows |
| Native ExtinguishSelf job report | Gameplay status | Native pawn activity report retained; no new window or animation |
| Source startup PatchAll | Technical lifecycle, no UI | Guarded per-patch admission and invariant bounded diagnostics; no new bottom/debug tab |

No source bottom tab exists. Actual settings remain mod settings; there is no per-pawn gameplay
choice or operational view toggle to relocate. Save controls and resets are disabled at main menu,
per-save editors disabled while inheriting, globals editable. A reset names one scope. New/missing
save entries inherit dynamically; overrides persist independently and remain dormant on returning
to inheritance. Changes affect the next choice/attempt; ongoing action/attempt is not restarted.

Defaults and ranges are source-derived. Nonfinite/corrupt numeric input recovers to its default;
finite out-of-range values clamp. Seven-language labels explain percentage display, independent
feature scopes, native colonist classification and direct damage. No automatic source import.

## Contracts consumed by other mods or saves

| Contract / evidence | Preserve, adapt or unsupported |
| --- | --- |
| Native BurningResponse, ExtinguishSelf job and target/record lifecycle | Preserve native priority,150-tick action, saved job/fire and ordinary failure; guard loaded effective path |
| Native TryAttachFire and CanEverAttachFire | Preserve eligibility and no-existing-fire gate plus size/instigator/dependent effects; narrow owned veto only for supported new attachment |
| Source public GetApparelIgnitionChance(float) | Raw unclamped return is different from owned bounded probability; no consumer found in bounded text search, no binary facade promised |
| Source public ApparelsOnBodyPart(Pawn,BodyPartRecord), settings and Mod.settings | Source assembly/type bindings are not supplied by a new type; actual compiled/reflection consumers still SR-Q3 |
| Source package/Harmony owner | Do not spoof or unpatch; when active it owns both features |
| Source settings keys IRS/MHA and six enable fields | Proven global schema; no automatic import or overwrite; owned settings use shared schema |
| Native game/save entities | No custom source job, fire or save component to migrate; owned design also uses native objects |
| RimPy entry and LayoutHarness example mod lists | Identity/listing references found, not actual helper/API consumers |

Text search is not a compiled-consumer audit or drop-in guarantee. Future migration needs backup,
atomicity, restoration and verified postconditions. Inactivation/removal changes future decisions;
existing native jobs/fires remain normal saved objects, without bulk state edits.

## Implementation and coverage

| Feature | Implemented | Deviation / possible conflict | Missing |
| --- | --- | --- | --- |
| Stationary response | Nothing | Positive group scope; native priority preserved; paired water/self decision must admit together | Exact effective tree/job/drafted/cleanup paths and native acceptance |
| Ignition resistance | Nothing | Legs restored; native flame-hit weights; safe missing/custom body and no-op fallback; current actual coverage | SR-Q1 balance/distribution review, SR-Q2 attachment/provider gate and tests |
| Settings/UI | Nothing | Global/save inheritance and validation replace source global options/write-on-draw | Common implementation, scope/load/reset cases and seven-language native UI |
| Compatibility | Nothing | Source authority prevents double checks; no foreign assembly/type identity | SR-Q3 actual helper/race/apparel/combat consumers and separate present/changed-provider runs |

Harmony is required with implementation. No XML mutation or new XML Extensions use is selected;
DLC and optional content remain optional. No source assembly/assets, old bundled Harmony or Gateway
may enter the product package. No measured performance or universal combat compatibility claim.

## Verification and acceptance

Independent document review found no issues. Typed strict validation passed101 items, zero failed;
eight scoped documents passed links/whitespace checks. The draft has8 requirements,24 scenarios
and19 unchecked tasks; SR-Q1–4 remain open. Future focused host tests cover derived probabilities, group/settings rules and
failure decisions; exact isolated game tests cover native Defs, Harmony, jobs and fire.

Final acceptance must personally observe actual incendiary exposure and subsequent in-place
extinguishing near water, actual prevented/passing ignition with covered/uncovered clothing, retained
direct damage, assistance/environment, incapacity, settings/save and provider outcomes on the reviewed
build. Native player weapon/action paths cause the event; direct fire spawning or synthetic record
mutation is not acceptance. Retain causal before/action/after views, build/mod identity, logs,
configuration hashes and cleanup under root AGENTS.md.

Static comparators are recorded for flak vest, recon/marine armor and cloth/synthread/devilstrand
dusters. Finalized values/coverage and the deliberate leg/weight distribution change require the
balance pass. Source defaults can make covered parts very resistant while leaving uncovered face
and direct damage relevant; do not advertise complete fireproofing from one garment.
