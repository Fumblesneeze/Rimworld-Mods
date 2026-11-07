## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S065/P1-60. [Proposal](proposal.md),
[observable spec](specs/megamod-hunting-companions/spec.md),
[research](../../../mods/megamod/docs/refinement/2026-11-04-hunting-companions-research.md).
The user selected all bonded Attack + Haul companions, one current prey claimant, finishing even
without haulability, actual corpse retrieval and hunter-centered directional wandering. No selected
source mod or consumed source API exists; Hunt for Me was expressly rejected.

Native 1.7 has distinct Bond relations, assigned Master, followFieldwork/followDrafted flags and
current learned trainables. Attack is Release; Odyssey AttackTarget is separate. Ordinary following
uses a broad Fieldwork job tag, not proof of an active hunt. Native hunting reserves its victim and
can reserve/unforbid its actual corpse, while normal animal hauling is probabilistic and separate.
Replacing those systems wholesale or issuing PredatorHunt would introduce unrelated attack/eating
behavior. This draft creates a scoped handoff through their actual lifecycle instead.

## Goals / Non-Goals

**Goals:** all eligible companions, faithful actual-hunt attribution, bounded single claimant,
real animal attacks and retrieval, explicit interrupted recovery, native permissions/needs,
shared data and safe integration with S064/S066/S021/S049.

**Non-Goals:** autonomous live-prey selection, dog-only content, a new trainable, pack inventory,
damage/speed/yield boosts, global attack/finish behavior changes, animal combat drafting, automatic
butchery, source-package impersonation or a runtime dependency on Pathfinding Lab. S066 owns the
directional walker and confinement extension; no new pathfinding subsystem is designed here.

## Decisions

### Reuse native animal choices and training truth

Use canonical Bond relation, current learned Release and Haul, active trainability, assigned
RespectedMaster and followFieldwork. Existing native prerequisites remain, including Obedience/
Tameness, without adding a third new training gate. CanBeMaster allows a bond to satisfy ordinary
handling-skill eligibility; do not invent a new Animals skill minimum. Avoid hard-coded breed lists:
compatible trainability effects, including admitted DLC effects, can change the current answer.

Reusing Master and fieldwork permission is proposed HC-Q1 policy. Preserve existing false/default
choices and expose why an otherwise bonded trained animal is not assigned. The alternative, a
second independent companion-owner/toggle model, would conflict with native follow decisions and
duplicate state. Do not create it without changing the observable contract. Entity controls remain
in native Animals/training UI; no per-pawn global/save setting.

Eligibility also consumes actual faction/map/spawn state, needs, restraint/lord duties, capacity,
training decay, access and allowed areas. Distinguish a bond from current obedience to a master;
do not write either relation merely to make a handoff legal. One animal cannot follow multiple
masters or run multiple assistance jobs even if modded data contains several bonds.

### Distinguish a hunting episode from a generic fieldwork tag

Use explicit owned S064 intent or an admitted native JobDriver_Hunt/current victim boundary.
Do not infer hunting from lastJobTag, a job label or merely carrying a gun. Native manual Hunt can
participate without a station. S008 drafted hunting stays separate; ordinary tame/mine/defend jobs
are not companion hunting. Native player-issued animal attacks keep their own authority.

Keep the minimal episode identity: hunter, originating native job or owned station intent, target,
map, qualifying hit/recovery epoch and current permission. A successful handoff can retain the
origin's target/station authority after natural human job completion. Mark that completion explicitly;
explicit interruption/drafting, changed target permissions and invalid hunter still revoke new
actions. Starting another hunt must not overwrite an already assigned animal's prior quarry.
Keep the originating native Hunt designation as live-prey authorization after handoff; player removal
revokes it even when the human is now doing another job. Distinguish natural death/designation cleanup
from player cancellation so lawful corpse retrieval continues. Station-originated authority instead
uses its actual current station/filter intent; do not manufacture a native Hunt designation.

### Observe actual hit and downing/death without changing damage

Bullet.Impact carries launcher, intended target, actual hit and DamageInfo, but can apply multiple
damage packets. Pawn.PostApplyDamage and health state change can nest with Kill, and bleeding state
changes need not carry a current damage instigator. Native HuntJobUtility recognizes only an actual
JobDriver_Hunt instigator; it is not a general delayed-hit or animal-finisher ownership contract.

Prefer narrow observation at admitted damage/state boundaries. Record an effective ranged hit only
for the current intended quarry and actual hunter; use a scoped before/result observation where
necessary so a shot that downs/kills during the damage call is not missed. Defer the decision until
the real state/corpse outcome is available, deduplicate nested notifications, and revalidate before
starting any job. No changes to damage arguments, RNG, health state or kill credit just to trigger work.
Non-projectile/status weapons need proven result attribution; unknown shapes remain unsupported
with ordinary hunter fallback. Fully blocked/missed hits are not inferred successful from animation.

HC03's delayed-collapse choice is explicitly authorization by a still-active wounded-quarry episode,
not a scientific attribution of which wound caused death. It includes a later contributing injury
while that episode remains current. Recovery/cancellation clears old-hit authority; renewed downing
needs a fresh qualifying hit. Do not scan all injured animals or reconstruct hit truth from history
text, proximity or last-attacker alone. Persist the minimal valid epoch for save/load.

### One claim and one physical target through handoff

Use the same owned claim boundary as S064 plus native reservation semantics, not an independent
companion reservation universe. Resolve live prey to its actual corpse once; preserve whether the
native hunter already reserved that corpse during Pawn.Kill. Transfer only compatible originating
claims. A foreign job's reservation is a real conflict, not something to unreserve globally.

Nearest legal approach with stable ties is proposed claimant policy. Select a finisher independently
of destination/carry capacity; after death, prefer the current eligible carrier, then another current
companion when necessary. Start and reservation transfer form one atomic owned handoff: do not suppress
the human attack/finish/carry path until an actual animal action has been admitted, and restore normal
human handling if that start fails. Once transferred, suppress only the originating owned/native
hunter continuation for that exact quarry. Already-fired projectiles and unrelated gunfire remain real.

Phases are accompanying → approaching downed prey → actual melee → corpse pickup/delivery →
ordinary reassessment/rejoin. Direct-death can begin at corpse pickup. The current actor/target and
physical holder determine recovery, not an old phase label. Reassignment first reconciles and ends
the previous claim; no concurrently active finisher/carrier. Finish/readiness checks are committed
on the Unity thread. A shared pre-pickup deadline, proposed 2 game hours = 5000 ticks, bounds stuck
or ineffective finishing and remains fixed through claimant changes and load. Post-pickup transport
uses physical recovery, not deadline-triggered cargo deletion. No recursive retry on failure.

### Animal melee is not human execution or predator feeding

Use actual melee tools, reach, cooldowns and damage through admitted native combat. Ordinary
FollowAndMeleeAttack stops at a downed target unless the job carries explicit kill-incapacitated
intent; apply that only within our current authorized job. The ordinary attack driver alone would
continue chasing after recovery and may have unrelated chaining/door-attack behavior. Keep explicit
downed-state/current-permission checks before each new hit and prohibit such target substitution.

Do not call ExecutionUtility's armor-ignoring cut/forced Kill for an animal or reuse PredatorHunt's
feeding tail. Human hunting's 180-tick execution and S008's 60-tick Finish off remain distinct. Actual
melee can damage body parts and corpse yield; there is no careful-slaughter flag or repair of wounds.
The native DangerousInMelee death action blocks automatic close finishing as proposed safety policy,
returning it to ordinary hunter ranged handling. Never remove that death action or invent immunity.
Known unsafe corpse handling, burning, unavailable destination or missing corpse still defer safely.

Keep actual attacker/kill consequences. Need an admitted once-only hunting outcome bridge where
native hunter completion would otherwise record twice or fail to recognize animal assistance;
do not globally change HuntJobUtility to pretend every companion attack was a human hunt. Capture
the actual source before designing those exact hooks (HC-Q3). S117/S118 slaughter work remains separate.

### Carry capacity and storage demand follow existing owned policies

Native Pawn_CarryTracker limits a held load by CarryingCapacity / VolumePerUnit and stack limit.
MassUtility's pack-inventory capacity is different and can be zero for a hauling dog. Preserve S021's
ordinary single-load fallback; do not reject all non-pack companions or grant them inventory. An
actual body/Manipulation loss can stop hauling even when training remains learned. Prefer one ordinary
corpse job; no opportunistic duplicates from another quarry without independent authorization.

If an admitted batch route participates, use S021's common mass/volume accounting, existing cargo
and explicit fallback-unload requirement. Corpses are normally excluded from batching but still
eligible for ordinary retrieval. Keep S126's relevant real-load presentation when that route applies;
native held corpse display suffices for native single-load work. Never manufacture a visual-only copy.

Use S049 automatic-refill admission for this delivery, not its production-output bypass. Destination
filters/priority/demand, incoming quantities, access and physical capacity are checked before pickup
and placement. Preserve independent player forbids after the initial hunting corpse-unforbid action.
Unsupported storage/custom capacity paths are not assumed unlimited. Emergency cargo recovery keeps
S049's distinct recovery semantics without converting every ordinary retrieval into an exemption.

### S066 supplies movement; needs and commands retain priority

S066 consumes one common companion distance setting, proposed 3 cells (native unreleased-follow and
wander anchor), and uses the hunter as root. Confined 2–3-step idle walks are unrelated tuning. Native
follow has purposeful regrouping and near-master wandering; idle/no-move is valid in narrow geometry.
No idle walker supplies a route to prey, storage or another map. S066 must preserve actual edge legality
and not import Pathfinding Lab's experimental activation/foreign owner or runtime assembly.
S066's on/off control selects idle style. An admitted ordinary near-master fallback can preserve
S065 companionship when that style is disabled/unavailable, but must share the same temporary
following suspension and current permission. Do not claim fallback from native invocation alone.

Animal.xml places normal master following above several medical/basic-needs branches. Adding another
unconditional following job there could starve them. Ensure owned accompanying yields to the stated
needs/duties before issuing a continuation; a public insertion tag alone does not prove correct
priority. Pin the actual permission/need decision boundary and observable interruption scenarios.
S030 excludes animals while following; do not globally remove that rule. A controlling need/duty
temporarily suspends the composed following authority for this owned companion, including native
ShouldFollowMaster scheduling. Merely ending our job is insufficient: the native fieldwork flag/tag
could immediately recruit it again. Preserve the saved master/follow choice and expose the temporary
yield in the shared admission decision consumed by S030/native following. Evaluate otherwise-relevant
needs/recovery without treating this temporary follow suspension as a permanent permission change.
Ordinary needs/job priority and completion govern resumption. Other native following remains under
its own contract. No blanket interruption of real carrying jobs or global skip-follow patch.

### Shared state, lifecycle and settings

Use native relations/training/master data and map listers/common S033 Thing facts. Store only
minimum additional episode/claim/deadline state; no per-hunter full animal/corpse caches or tick scans.
Invalidate on current relationship/training/faction/map/permission/target changes. Shared candidates
never cache the actual pawn-specific authorization or destination acceptance as permanently true.
No off-thread Verse access. Measure total index maintenance/query/memory costs before optimization claims.

Save stable owned identities and phase, resolving/reconciling with native jobs/holders on load;
never replay damage/death/spawn to repair an old token. Disabled/admission-failed saved intent stops
before attacks/pickups. Existing cargo remains in its real holder or ordinary lawful recovery path.
Map unload/game switch clears transient indexes and foreign-game references. A normal actor death
keeps native bond reactions, contents and notifications; do not rescue cargo by duplicating it.

Availability on, distance 3 cells and budget 2 hours use the common load/store/resolve/render settings
path. Entity permission remains native. Positive finite numeric validation also handles overflow;
budget applies only to new opportunities, distance to next idle/regroup decision. Preserve dormant
save overrides and block every save callback at the main menu. Seven baseline languages and actual
Animals/training/status/native action workflows remain implementation tasks.

### Per-patch target admission and additive definitions

Create a stable target document for every actual Harmony/XML patch before implementation. Prefer
owned declarative jobs and additive native extensions, but do not assume Animal_PreMain automatically
provides the required need/work precedence. Any XML mutation uses XML Extensions node/content guards,
external package-ID checks when relevant, and makes XML Extensions a hard dependency at first use.
Do not replace Animal's whole tree or reorder native trainables; native trainable data warns that
reordering can corrupt save associations. There is no need for a new Hunting trainable.

| Group | Minimal effective meaning needed | Failure |
| --- | --- | --- |
| Animal accompanying | Current native permission/training and correct needs/duty precedence; supported S066 movement or admitted ordinary style fallback | Affected owned following inactive; retain native behavior and explain availability |
| Attribution and transfer | Actual ranged hit/current quarry, down/death epoch, real corpse/claims and atomic human-to-animal transfer | No new handoff or suppressed hunter continuation; ordinary hunter remains |
| Finish/retrieve/recovery | Current downed authority, actual melee, exclusive actor, corpse identity, native/composed capacity, safe save/holder recovery | Fail the complete assistance group closed, including held saved attacks; preserve physical cargo |
| Optional provider | Canonical package/Def identity and actual consumed damage/map/capacity/storage lifecycle | Disable only unproven provider path with fallback, never guess or duplicate actions |

Hash/version evidence is not a compatibility gate; compatible foreign patches and benign changes
remain allowed. Examine required effective semantics, skip-original behavior and actual incoming
transpiler flow; every transpiler validates each incoming stream and returns it unchanged on failure.
No speculative foreign-transpiler replay. Unknown predicates/exceptions/install failures atomically
roll back only newly owned patches in the affected group. Idempotent startup and supported repatch
revalidation emit one bounded warning per skipped enabled patch with target/spec/reason/behavior/
fallback. Native method existence alone cannot prove correct prey argument use or corpse transfer.
Harmony is required on first eager use. No source mod, DLC, Pathfinding Lab or Gateway dependency.

## Risks / Trade-offs

- Native follow permission adds an explicit player choice → proposed HC-Q1, explain assignment and
  retain all qualifying animals without another training prerequisite.
- Delayed collapse has mixed injury causes → specify authorization rather than exclusive kill credit.
- Real animal melee can lower yield or be ineffective → preserve normal consequences and finite budget;
  no covert execution or damage boost.
- Two actors and native corpse reservations → atomic transfer plus one actual target/holder and fallback.
- Carry volume versus pack mass → use S021's established distinction, never create capacity.
- Follower priority can suppress needs → verify live interruption rather than trusting XML position.

## Migration Plan

No upstream import or automatic rewrite of native master/follow/trainable state. Existing games
receive common availability defaults but preserve actual entity permissions. No retroactive hit or
old-corpse queue. Future owned saved-state schema migrations need explicit backup/atomic recovery
and verified postconditions. Safe uninstall and all-provider compatibility are not claimed.

## Open Questions

HC-Q1–5 retain proposed permission/attribution/selection/tuning, exact native/provider target and
outcome seams, movement/needs/hauling composition, contextual localization and future native
acceptance. Resolve during research/review; do not ask the user to explain an existing mod.
