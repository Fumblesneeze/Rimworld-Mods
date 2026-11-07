# S065 — Bonded hunting companion research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S065/P1-60, researched 2026-11-04. No selected upstream source package/license,
version or source consumer API applies. Hunt for Me (`aRandomKiwi.HuntForMe`, Workshop 1593245720)
was explicitly rejected; none of its behavior is selected implicitly.
[Proposal](../../../../openspec/changes/specify-megamod-hunting-companions/proposal.md),
[design](../../../../openspec/changes/specify-megamod-hunting-companions/design.md),
[spec](../../../../openspec/changes/specify-megamod-hunting-companions/specs/megamod-hunting-companions/spec.md).

## Requirements provenance and proposed choices

The [interview record](2026-11-01-handwritten-notes.md), P1-60 and hunting-companion clarification,
confirms all qualifying bonded animals with **Attack + Haul** training, no extra Hunting training,
staying around the hunter, one claimant per shot/downed prey, finishing if necessary and hauling only
if a destination and capacity exist. Finishing is not conditional on haulability. No dog-only limit,
speed bonus, exact claimant policy, injury-attribution rule or companion distance was selected.
S066 separately owns Pathfinding Lab directional wandering and the short pen/room extension.

Proposed HC-Q1 choices: preserve native Master/followFieldwork permission, actual undrafted Hunt and
station contexts, actual effective hit plus same-episode delayed collapse, nearest legal claimant,
normal animal melee rather than human execution, dangerous-melee-death fallback, distance 3 cells and
2-game-hour shared pre-pickup budget. These are reviewable assumptions, not invented interview answers.

## Documentation first, then exact installed source

The primary [Alpha 16 announcement](https://ludeon.com/blog/2016/12/rimworld-alpha-16-wanderlust-released/)
(Tynan Sylvester, 2016-12-20) describes separate drafted and hunting/taming follow choices. It establishes
historical control intent only; it does not prove current 1.7 implementation. Search results containing
third-party husbandry guides were not used as current technical evidence. This original feature has
no selected Workshop implementation to reverse engineer.

Typed installation inspection reports **1.7.4998 rev1299**, root
`F:/Steam/steamapps/common/RimWorld`; current `Assembly-CSharp.dll` SHA256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Read-only bounded reconstructions are in
`artifacts/Refinement/megamod-spec-20261104/hunting-companions/native/`.
The registry's decompile operation needs a live Gateway; existing offline ILSpy was used instead.
No game method, source project, assembly initializer or simulation was executed.

Inspected complete small selected types: ThinkNode_ConditionalShouldFollowMaster,
JobGiver_AIFollowMaster/AIFollowPawn/AIDefendMaster, JobGiver_WanderNearMaster, JobDriver_FollowClose,
JobGiver_Haul, PawnColumnWorker_FollowFieldwork, TrainableUtility, HuntJobUtility, Bullet,
ExecutionUtility, JobDriver_AttackMelee and JobDriver_PredatorHunt. Targeted sections, not full-class
audits: Pawn_TrainingTracker, Pawn_PlayerSettings, Pawn_RelationsTracker, RelationsUtility, Pawn,
Pawn_HealthTracker, Corpse, Pawn_CarryTracker, HaulAIUtility, Toils_Haul, Toils_Combat and MassUtility.
S064's inspected native JobDriver_Hunt/WorkGiver_HunterHunt evidence remains the hunter comparison.
Core XML: TrainableDefs/Trainables.xml, ThinkTreeDefs/Animal.xml and the animal comparators below.
Two guessed animal filenames were absent; actual CatGroup/CowGroup/WargGroup files were located
before inspection. No missing path is treated as source evidence.

## Training, bond and control findings

| Input | Inspected current behavior | Consequence |
| --- | --- | --- |
| Core Attack | Def Release, Intermediate trainability, 2 steps, Obedience prerequisite | User's Attack is not Odyssey AttackTarget |
| Core Haul | Advanced trainability, 7 steps, min body size 0.40, Obedience prerequisite | Existing abilities suffice; do not add training or hard-code breeds |
| Guard/Obedience | Intermediate, 3 steps, Tameness prerequisite | Retain native prerequisites without inventing another condition |
| HasLearned | Rechecks current trainability before learned flag | Ability can become unavailable without deleting old progress |
| Training completion | Obedience can assign the trainer as master when none exists | Assigned master and bond need not be the same pawn |
| Master and follow | Native saved master; followFieldwork and followDrafted default false; changing master can interrupt a follow job | Preserve existing choices, explain proposed permission gate |
| RespectedMaster | Requires animal and master in player faction | A stale master reference alone is not current authority |
| CanBeMaster | Bond can satisfy handling skill; prisoner/spawn and special dryad rules remain | Do not add a general Animals skill minimum |
| Bond formation | Actual relation; several existing bond counts affect odds; normal animal death/human death reactions remain | No new bonding action or rewritten bond consequences |

The proposed permission gate is additional player workflow, explicitly not user-confirmed. It uses
current controls so a bonded pet does not silently abandon another assigned master or an explicit
following-off choice. The actual user requirement that every qualifying animal can participate and
that no new Hunting/AttackTarget training is required remains central.

## Following and job priority findings

ShouldFollowMaster uses RespectedMaster plus followDrafted/drafted or followFieldwork/lastJobTag
Fieldwork and reachability. It also has a carried-away hostile captor branch; that is not a hunting
episode. Native follow radius is 3 unreleased / 50 released, with 200-tick expiry; near-master wander
radius 3 and idle interval 125–200. FollowClose also uses 1.2 distance hysteresis, periodic path checks,
native sight/nearby tests and physical movement. These are native observations, not wholesale new
constants adopted into the spec. Only proposed 3-cell distance is selected as the initial anchor.

Animal.xml has immediate danger/queued/forced/rope/lord priorities, then master defense/following,
then medical/allowed-area/temperature and basic needs branches. Ordinary trained hauling is later,
behind a 1.5-hour mean-time chance and Manipulation/Haul checks. A generic fieldwork tag can outlive
or include unrelated work; it must not authorize our attack. Adding an unconditional follower above
needs is unsafe. The owned path must yield as specified; merely ending its job would allow native
followFieldwork/lastJobTag to recruit it again. Temporarily suspend composed following for that
companion while the controlling need/recovery wins, preserve saved choices, and let S030 consume the
same effective authority. S030's ordinary actively-following exclusion remains. Its draft/spec and
design now state this narrow composition explicitly; no source or runtime behavior was changed.

S066 reference-only source reconfirmed:

- `mods/PathfindingLab/Source/WildlifeWander.cs`, SHA256
  `110215E90289EAC9F800EC9C6698767C08D10F2D0E7636FEA3A2D3B1310A4B2F`.
- `mods/PathfindingLab/Source/Core/DirectionalWalk.cs`, SHA256
  `E74F0F5DA100EB71DE6AB833C4FA766C82E63400E70AF2D0333DEAD20D713FE5`.

The current adapter accepts factionless ordinary wander/herd animals, not player companions, and
the core accepts 3–10 steps. It tests bounded adjacent headings and retains direction; the adapter
rejects very short paths. These are adaptation seams for S066, not a working companion integration.
No entire experimental pathfinder, runtime dependency or private per-hunter cache is selected.

## Shot, finishing and corpse findings

Bullet.Impact records launcher/intended/actual hit and applies DamageInfo, potentially followed by
extra damage packets. Health changes can down or kill within nested damage handling; bleeding/state
changes can lack a current instigator. Pawn.PostApplyDamage returns early after health handling if
dead. An observer must account for immediate death and exact episode identity rather than merely
run afterward and assume an active living target still exists.

Native HuntJobUtility.WasKilledByHunter requires a damage instigator whose current driver is
JobDriver_Hunt with that victim. Pawn.Kill creates the actual corpse and may reserve it for that
hunter, otherwise apply outside-home forbidding. S064's hunt driver later obtains/unforbids/reserves
that corpse for actual transport. Therefore companion handoff must reconcile both live-prey and
corpse claims. It cannot blindly reserve a duplicate or suppress the human before the animal starts.

ExecutionUtility performs an armor-ignoring execution cut and forces death if necessary; that is
not an animal's ordinary attack. PredatorHunt uses real melee but intentionally continues into
eating, so cannot be reused wholesale. AttackMelee/FollowAndMeleeAttack can attack a downed target
only with appropriate job intent; the general path otherwise stops, and it does not by itself stop
attacking when a previously downed prey recovers. Current authorization must guard every new attack.
The selected proposed behavior is actual animal melee, preserving wounds/yield and rejecting
dangerous-melee-death finishing. No precise meat-loss multiplier is asserted from these targeted reads.

The 2-hour budget corresponds to 5000 game ticks, using the native hunter/predator attempt scale as
a provisional anchor, not a claimed native companion timer. It bounds pre-pickup assistance across
reassignment; actual cargo recovery is not a timer that destroys a held corpse.

## Hauling and measured static comparators

Native JobGiver_Haul checks reservation/reachability, forbidden state, Manipulation, burning,
positive carry-stack space and storage. Pawn_CarryTracker limits count by current CarryingCapacity/
VolumePerUnit and stack limit. MassUtility's inventory capacity is BodySize×35 only for eligible
tool users/pack animals. A non-pack hauling dog can have zero inventory capacity without losing
its native single-load carry. These are distinct: S021 explicitly preserves ordinary heavy/single-load
fallback after owned cargo is unloaded. S049 automatic demand still governs corpse storage delivery.

Static Core ThingDef values, not finalized active-mod comparison or a new breed balance change:

| Animal | Body size / trainability | Movement / base value / pack flag | Meaning |
| --- | --- | --- | --- |
| Husky | 0.86 / Advanced | 5.0 / 250 / no local pack flag | A familiar Attack+Haul candidate; do not require pack inventory |
| Elephant | 4.0 / Advanced | 4.8 / 600 / packAnimal=true | Larger trained non-dog candidate; current capacity still measured at use |
| Warg | 1.4 / Intermediate | 5.0 / 450 / no local pack flag | Ordinary attack capability is not ordinary Haul qualification; admitted trainability changes can matter |

Trainables require real training effort and maintenance; companionship does not give free skill,
create food or remove normal hunger. Proposed normal melee adds real time/risk and can damage the
carcass; no careful-slaughter or S117/S118 bonus. Finalized abilities/body tools/health/carrying,
native needs, destination limits and actual multi-animal hunt outcomes remain acceptance work.

## Composition, contracts and evidence limits

No source API/save identity to preserve or copy. S064 owns station filters/return and passes current
authority directly; S066 owns directional movement; S008 owns drafted hunting; S021/S049 own actual
loads/destinations; S033/native listers own common facts; S126 presents actual batch cargo when used.
S117/S118 deliberate slaughter remains distinct. Unknown animal/weapon/storage/map APIs need canonical
package/shape adapters and provider-present acceptance, not guessed all-mod compatibility.

The S064 return contract now explicitly permits the human to reassess once an admitted companion
takes responsibility, without waiting for delivery. The delegated job still retains its originating
station's current filter/pause/removal authority. For native manual hunting, removing the living
prey's Hunt order cancels delegated finishing even after the human has moved on; routine designation
cleanup at death is distinct and must not cancel lawful corpse retrieval. Exact callback/ordering
proof belongs to HC-Q2 and future focused tests, not this observable policy decision.

HC-Q1–5 retain proposed control/attribution/finish/tuning, effective hit/outcome/holder seams, movement/
needs/shared-data composition, UI/localization and native verification. All runtime tasks remain open.
No runtime code/Defs/assets, build/install, gameplay tests, game launch, normal save/config change,
Workshop edit or source-project execution occurred. Static reads cannot prove working companion AI.

## Review and validation checkpoint

The owned draft contains **10 requirements, 41 scenarios and 33 unchecked tasks**. Independent native
review found no actionable P1/P2 issues. A separate independent composition review found no issues
across S008/S021/S030/S033/S049/S064/S126 or the explicit boundaries with queued S066/S117/S118.
Neither review accepts runtime behavior. Native callback ordering, effective target admission,
animal scheduling and observed finishing/delivery remain implementation and acceptance work.

Host refinement made the S030 temporary follow suspension and S064 post-handoff return contracts
explicit in both owning drafts and replacement ledgers. Manual Hunt cancellation and death cleanup
now have distinct observable outcomes. No saved native permission or source code was modified.

Strict typed OpenSpec validation passed **87 items, zero failures**, exit code 0. The CLI also
reported a PostHog telemetry transport timeout, which did not fail schema validation. Local-link
and whitespace checks passed across **15 scoped Markdown documents**. The queue now contains
**66 reviewed drafts / 136 entries / 282 unique question IDs**. HC-Q1–5 remain open.

The semantic balance pass retains actual training, movement, hunger/rest, attack timing, wounds,
corpse yield and carrying/storage limits. Husky, elephant and warg source comparisons document
different trainability/body/pack constraints; they are not finalized active-mod or live measurements.
The proposed 3-cell distance and 2-hour assistance budget remain explicit tuning assumptions.
No free training, extra capacity, slaughter yield or combat immunity is introduced. All runtime
tasks remain unchecked. Next: **S066 directional hunter/pen/room animal wandering**.
