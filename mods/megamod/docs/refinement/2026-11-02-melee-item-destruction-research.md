# Original on-site melee item destruction: refinement and evidence

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S023**, P1-17, **origin: original**. Inspected **2026-11-02**, after S022.
[Owning change](../../../../openspec/changes/specify-megamod-melee-item-destruction/) is specification
work only; no implementation, game run, balance acceptance or runtime compatibility claim.

## Confirmed scope

The user explicitly chose destruction **where the item lies**, **items with HP only**, and repeated
**actual melee attacks** until destruction. No work-timer deletion or direct HP subtraction based
on DPS. Buildings, plants and living pawns are outside the item order; HP-less items are not deleted.
S022 owns weapons/apparel recycling. S096 Micro Designations supplies other processing actions and
must compose directly, not remain a patch to our implementation. Destroy Item/Recycle This are
research references, not full-source parity selections. See the [interview](2026-11-01-handwritten-notes.md).

Public documentation was read first: [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2423311270)
and [official repository](https://github.com/GarwelGarwel/DestroyItem), alongside installed README.
The reference advertises Hauling-assigned disposal, HP/DPS/labor-based duration, multiple workers,
corpse/embryo thoughts and hazardous waste effects. Its HP-less instant disposal directly conflicts
with the requested feature. Source analysis, not the advertised wording, establishes its actual path.

## Pinned inputs and inspected scope

| Input | Identity |
| --- | --- |
| Reference | Destroy Item, Garwel, `Garwel.DestroyItem`, Workshop 2423311270 |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2423311270` |
| About SHA-256 | `7FC578F5670B2F39F576BB8573B4E5543033F76C27E94BCE25FDD3E346751E08` |
| Declared support | 1.2–1.7; no declared Harmony dependency |
| Main DLL | `1.7/Assemblies/DestroyItem.dll`, assembly/file 1.5.0.0, configuration Debug |
| DLL SHA-256 | `77F08CEE4D001B88B39192A0A1145584364ADFD7728F5AD9671EC305E59577C6` |
| Informational version | `1.5.0+26cc3c993ba8c1dd568560669611021575e6d0a8` |
| Public checkout | `53706807c41e2e2ba8ba89e495258aafe35adc4f`; 1.7 DLL, About and all eight installed C# files match bytes |
| License | MIT, copyright 2021 Garwel; preserve notices if substantial source is later reused |
| Native comparison | 1.7.4995 rev309; Assembly-CSharp SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Artifact root: `artifacts/Refinement/megamod-spec-20261102/item-destruction` contains public checkout,
installed decompilation, selected native classes and the bounded consumer scan. All eight source
classes were read, plus relevant Job/WorkGiver/Orders/Damage XML; the compiled job was checked for
the actual timed-damage/final-destruction behavior. This is input correspondence, not reproducible
build or live behavior proof. Native Thing methods are also retained in sibling `recycling/game`.

## Reference behavior and deliberate non-parity

- Eligibility is destroyable ThingCategory.Item, with no useHitPoints requirement. Orders supports
  dragging, a source X binding and contextual reverse designators. Its map component checks/registers
  the designator on every GUI pass. UI strings are hard-coded English; no bottom tab.
- WorkGiver_DestroyItem belongs to Hauling, priorityInType 50, requires Manipulation. It scans
  spawned designations and permits the configured simultaneous reservations. The job fails when
  missing/despawned/forbidden or no longer designated, walks to Touch and remains there.
- Every **60 ticks** the job computes MeleeDPS × GeneralLaborSpeed × destructionSpeed, or full
  remaining HP in instant mode. Nonlethal steps call TakeDamage using Damage_Destruction. Lethal
  steps set HP to zero, call CompDissolution.TriggerDissolutionEvent(stackCount) if present or
  Destroy() otherwise, and increment a custom ItemsDestroyed record. No attack verb is invoked.
- Source defaults: destructionSpeed 1, instantDestruction false, maxDestroyers 2. UI speed ranges
  0.1–10 through logarithmic steps; workers 1–8. Global save keys use those same names. These are
  not adopted: actual melee governs timing and native single-worker item ownership is proposed.
- On work initialization it grants bespoke humanlike-corpse/embryo memories, including colony-wide
  corpse knowledge. Starting/cancelling/restarting can enter that code again; these memories are
  not a confirmed user request. Our feature preserves native attack/destruction consequences and
  adds no copied source thoughts or records. It does not promise filth-free disposal.
- Orders insertion is an unguarded append. The job's damage Def/custom deletion path and repeated
  GUI registration are not implementation templates for the requested real attacks.

## Native melee and destruction contracts

Inspected JobDriver_AttackMelee, Toils_Combat, Pawn_MeleeVerbs, Verb_MeleeAttack,
Verb_MeleeAttackDamage, DamageWorker, Thing.TakeDamage/Kill/Destroy/SplitOff, Corpse and CompDissolution.

| Native boundary | Relevant fact / implication |
| --- | --- |
| JobDriver_AttackMelee | Uses FollowAndMeleeAttack and TryMeleeAttack, but can auto-target nearby identical Things (radius 4) after finishing and optionally retarget a blocking building. Those unrelated target behaviors must not enter our item order |
| FollowAndMeleeAttack | Can path with Danger.Deadly and invokes a callback while in touch range; ordinary disposal must retain its own safe work/access checks rather than assume combat pursuit is appropriate |
| TryMeleeAttack | Checks FullBodyBusy, kind melee ability and verb usability. Returns true after invoking TryStartCastOn without propagating that call's result: a true return is not proof of a completed attack or damage |
| Melee selection | Native body/equipment/apparel/hediff and occasional terrain melee verbs are considered. Preserve supported actual melee rules; do not pick a ranged verb, swap gear secretly or impose highest-DPS-only attacks |
| Verb_MeleeAttack | Immobile items have native non-miss chance 1 and dodge 0; the ordinary Melee XP branch requires a non-immobile target. No extra training reward is needed for item disposal |
| Verb_MeleeAttackDamage | Native damage varies 0.8–1.2 of adjusted melee amount, includes actual source/quality/tool and optional extra damages, and calls target.TakeDamage; do not recreate this arithmetic as our damage timer |
| Thing.TakeDamage | Applies damage multipliers, pre/post callbacks, damage result, records/quest signals, possible filth/ignition and instigator effects. Bypassing it loses more than visible HP |
| DamageWorker | HP-using health-harming targets lose actual damage, with corpse factors where applicable; HP exhaustion calls Kill. A native stack is one HP-bearing Thing, not a new per-unit work budget |
| Thing.Kill | Calls Destroy(KillFinalize), distinct from a direct default Destroy(Vanish) call |
| Corpse destruction | Native destruction can destroy the dead pawn's equipment/inventory/apparel and notifies native ideology/ownership. No automatic stripping or safe salvage is implied |
| CompDissolution | Lethal health-harming pre-damage triggers map dissolution effects for current stack count. A separate manual post-kill dissolution call would risk duplicate effects |
| SplitOff | Native split copies current HP to the new portion. Never heal either portion or damage a whole mixed stack and then recreate its unmarked units |

Source/XML comparators, not finalized optional-mod measurements: WoodLog declares 150 HP and inherits
HP-using ResourceBase; Silver explicitly disables HP; HumanEmbryo declares 50 HP; Wastepack inherits
HP use and supplies gas-on-damage plus dissolution/pollution components. MinifiedThing is explicitly
an HP-using **Item** wrapper. The proposal includes that loose wrapper under the literal item rule,
while excluding installed buildings; MD-Q1 retains this classification for later review. Normal
contained-object consequences remain, just as for a corpse. Compatible custom items use actual
category/HP/damage capabilities, not a list of named commodities.

## Consumers, owned composition and unresolved work

The bounded DLL/C#/XML literal scan found 26 paths for canonical package/designation/workgiver/job
identities. Non-provider hits are VEF's **own** AnimalBehaviours.WorkGiver_DestroyItem and historical
copies: inspected current XML and class use CompDestroyThisItem, DestroyableObjects_MapComponent and
VEF_DestroyItem, not Garwel's designation/job. These are name collisions, not demonstrated source
consumers. No external binding is claimed preserved; this scan cannot rule out dynamic consumers.

No source package/assembly/Def/save compatibility facade is selected. Original Garwel.DestroyItem
present suspends overlapping owned attacks. Recycle This's bench disposal differs and is not itself
evidence to disable this entire on-site feature; prevent two processing authorities on the same
item and admit any required external-order adapter narrowly. S022/S096 compose directly. Shared
quantity identity and map facts integrate with S021/S033; no parallel per-feature Thing cache.

Proposed safety/playability choices: one worker per item; normal Hauling eligibility plus ability
to perform melee; no animal work grant; exact quantities and same-cell separation of mixed stacks;
pause after **10 completed attack cycles** without a new lower observed HP or reduced authorized
quantity, with explicit retry and no same-condition automatic restart. Native cooldown/warmup does
not count as a failed cycle. A non-busy attack that cannot start ends with a reason. These are named
policy constants, not damage/speed cheats; the feature adds no new global/save preference.

Independent lifecycle review refined the shared S022/S023 quantity contract: keep active, suspended,
unmarked and differently ordered portions distinct through splitting/merging. Only currently
attack-authorized units can be isolated for attacks; Resume/Retry cannot spread across a merged
stack. If ordinary consumption identifies only a count, retire destructive authorization first;
ambiguous competing states require visible suspension and explicit quantity resolution. These are
proposed safety rules, not facts attributed to the reference. The ineffective-cycle count also
survives unchanged interruption/reload and stack movement; nine failed cycles do not become zero.
Source/provenance and native consequence review found no additional issues. Implementation proof
remains outstanding, including future S096 transitions.

| ID | Kind | Open work retained for later |
| --- | --- | --- |
| MD-Q1 | Proposed classification / UX | Loose minified wrappers and contained-item consequences; one worker, Hauling placement, same-cell isolation, 10 ineffective cycles and readable status/explicit retry |
| MD-Q2 | Exact native action lifecycle | Prove selected melee cast start/completion, cooldown, zero-progress observation, interruption/cancel before next strike and all damage/destruction callbacks on exact beta |
| MD-Q3 | Quantity / provider composition | Per-portion suspension, conservative unspecified consumption, safe split/merge isolation, external orders, direct S022/S096 arbitration and supported melee/item effects without source emulation |
| MD-Q4 | Implementation / acceptance | Each actual semantic target, focused tests, seven-language UI, shared-data costs, independent review and native visible swings/HP/destruction/effects/save/provider proof |

Questions do not interrupt specification. There is no claim of safe clean disposal, blanket provider
compatibility, combat balance equivalence across mods or completion based only on a damage query.
