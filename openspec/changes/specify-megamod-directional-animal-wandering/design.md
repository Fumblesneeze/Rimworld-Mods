## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
**Origin: original**, S066/P1-60. [Proposal](proposal.md),
[observable contract](specs/megamod-directional-animal-wandering/spec.md),
[research](../../../mods/megamod/docs/refinement/2026-11-04-directional-wandering-research.md).
This is a specification draft, not runtime implementation or accepted performance evidence.

Pathfinding Lab's selected core is a short directional walk and an adapter associating that route
with the actual native wander job/request. It excludes pen/player/duty providers and discards
one/two-step walks. The user's extension requires new contexts and valid short results, not copying
those exclusions or taking the entire experimental pathfinder. Native room/pen topology, active
hunting authority, job priority and actual follower costs remain the source of truth.

## Goals / Non-Goals

**Goals:** local direction-preserving idle movement, no unwanted herd center for confined animals,
S065 hunter cohesion, short/no-move termination, current physical boundaries, honest settings and
safe job/request lifecycle with native purposeful work unchanged.

**Non-goals:** a replacement world/general pathfinder, new pens, animal assignments or training,
feeding/hunt/haul jobs, special movement powers, population changes, perfect trajectory/save replay,
source API facade, benchmark speedup claims or runtime dependencies on repository-owned mods.

## Decisions

### One small movement policy with explicit contexts

Keep the pure bounded directional calculation separate from current game context, without a generic
pathfinding framework. Reuse or extract host-safe code under repository boundaries only when there
is a concrete shared consumer; megamod must never load Pathfinding Lab's assembly. Source provenance
does not waive spec → focused RED/GREEN → implementation → native acceptance.

Admit actual provider semantics, not a report string or a broad Idle/Fieldwork tag:

1. Higher-priority/native specialized work owns movement: needs/recovery, rope, Lord/duty, combat,
   feeding, pen shelter, map exit and orders. It is not a directional-wander candidate.
2. An authorized S065 idle companion uses its current hunter reference and one shared distance.
3. Ordinary idling in an enclosed native pen uses physical pen context and short lengths.
4. Otherwise a valid bounded non-doorway current room supplies confined context.
5. Otherwise exact supported ordinary factionless WanderAnywhere/WanderHerd semantics use wild
   context. Unconfined tame WanderColony retains native colony intent.

The exact native providers can still differ in urgency, cadence, expiry, danger, reports and
destination validators. Preserve these facts. Do not intercept every JobGiver_Wander subclass:
WanderInRoofedCellsInPen overrides destination logic and its root method throws; it is a separate
shelter action. WanderInPen can be marker-seeking outside an enclosure, and the player animal tree
also uses WanderAnywhere and WanderColony. A pen feature implemented only by patching WanderInPen
would miss most ordinary tame idle decisions. Native root-purpose and own S065 intent must be explicit.

### Bounded directional steering accepts short results

Retain a per-animal heading, bounded adjacent attempts and no repeated cell within one walk.
Reference behavior is forward heading with occasional ±45-degree drift, alternating wider turns
before reversal. At most eight unique directions are attempted at each step; the configured maximum
of ten steps caps a local plan at eighty neighbor probes, independent of map size. No repeated full
restart to achieve the selected minimum. A plan containing only its start means wait; any legal
one-or-more-step prefix can be used. Update the reference core's 3-step lower admission and the
adapter's four-cell cutoff deliberately, with meaningful public-rule tests.

Use ordinary game randomness; do not reseed the whole game or promise identical reference/random
trajectories. Heading can be transient and reset on load/map/context changes. Per-animal heading is
not a per-animal Thing/region cache. Preserve heading across successive compatible local walks;
blocked turns may change it. Roots and enclosure facts are current context, not permanent state.

Local steering does not run RandomWanderDestFor to find a distant endpoint after failure, or ask
the full pathfinder to repair a short idle route. It also does not replace native herd-root selection
or spatial topology maintenance with guessed geometry. Account for their costs separately; bounded
neighbor probes alone do not prove bounded total work or a measured performance improvement.

### Native spatial data supplies physical confinement

Prefer native Room/District/Region and AnimalPenManager/PenMarkerState facts. An enclosed pen's
ConnectedRegions can include connected rooms and passable doorways. Recognition uses physical
current membership, not merely AnimalPenUtility.GetCurrentPenOf: that helper also gates on Roamer
and marker acceptance, so it can return null while the animal is still physically contained.
Changing filters must leave ordinary relocation behavior intact without redefining walls.

Resolve multiple markers in the same connected enclosure to one physical context. Do not persist
an invented animal→marker assignment. If no enclosed pen applies, use a non-doorway Room.ProperRoom
with current normal region identity. Roof, temperature, room role and psychological-outdoor flags
are separate; do not turn the entire map's outdoor room into confinement. Unknown/stale topology
cannot authorize a short path across a presumed wall; defer to a later valid current decision.

Native pen manager invalidates marker states after building spawn/despawn; PenMarkerState checks
door passability changes and lazily recalculates. Connected-district calculation also has a per-tick
cache. Reuse these owned/native facts and inspect full geometry/door/region invalidation before
relying on them. Do not copy mutable native collections into persistent per-animal snapshots. If
derived membership is justified, give it one map owner shared with actual consumers, an explicit
revision/invalidation contract and measured update/memory costs; no private flood fill per walk.

The proposed conservative idle rule rejects door/fence cells even when pen connectivity includes
them. Thus an idle walk need not traverse every connected part. Purposeful food/shelter/rope/follow
work still uses ordinary doors/flaps and their actual access/time costs. This retains the reference's
idle preference without pretending a native connected pen is several permanent pens. A breach,
open gate, room split/merge, marker removal or map disposal invalidates affected context; no stale
outline may confine subsequent ordinary work.

### Herd roots and moving hunter roots have distinct authority

Use the admitted native WanderHerd/WanderUtility root selection and effective radius, including
same race/faction, reachability and human avoidance. Do not add a global herd catalog. A wild animal
already outside its root circle takes ordinary regrouping. Native root selection is bounded but
not free; its candidate/reachability work belongs in combined measurements.

S065 hands its current hunter/episode permission directly to this planner. Use that feature's one
distance setting; do not reinterpret its 3-cell proposal as a step count. Check the hunter's current
map/position/authority before a continuation. A moving root can invalidate the next idle segment;
outside-distance behavior uses purposeful native following, not repeated shrinking circles or
unlimited replans. Other pets are not roots. No idle reservation can duplicate a hunting seat,
prey claim or actual cargo job. S065 retains needs suspension and all actual attack/pickup rules.

The S066 operational toggle selects idle style. When off or its own route is unavailable, a
supported ordinary near-master path can still serve S065. That fallback must itself satisfy S065's
priority/permission contract; merely invoking native following unconditionally would undo its
temporary need/recovery suspension. If safe fallback is unproven, the affected companion movement
path fails closed as specified, rather than claiming all companion behavior works.

### Real edge legality and physical follower costs remain authoritative

Use one current local-step validator composing native permissions, collision/destination claims,
physical cardinal/diagonal legality, selected conservative idle preferences, context boundary and
actual provider validator. Revalidate again before consuming an affected next step; the native
30-tick lookahead alone does not cover all changed enclosure/root/area/hazard semantics.

Do not infer that passing only a destination check proves every intermediate edge. Conversely, do
not treat idle room preferences as bans on purposeful travel. S024's enabled sheltered-walk budget
must use current native/composed movement costs when truncating a local walk, not the reference's
approximate path-total formula. S030 recovery and native roofed-pen shelter remain separate
decisions, including routes through exposure that their own escape contract allows.

The 0.5 animal toxic-environment-resistance preference is a native/reference idle threshold, not
general hazard immunity. Preserve actual local hazards and provider susceptibility. Optional
terrain/map/door/locomotion integrations require canonical identity and actual consumed semantics;
flying/swimming/water-seeking are ordinary fallback until explicitly supported. Never invent
movement capability to fit the local grid. Preserve pawn-specific terrain costs, injury/carry speed,
urgency and native collision waiting through the real follower.

### Treat planning, job association, path delivery and cancellation as one admitted route

A prospective route has exact pawn/map/context, start, ordered adjacent cells, current endpoint,
boundary/root and originating decision. Associate it only with the actual resulting owned/admitted
idle job, never a reused pooled object or any job sharing a label. Clear pending data after the
actual job result, including a native Wait/null, and on Job.Clear/replacement. Support partial
progress only from a matching actual cell along that same route; changed targets are not authority
to repurpose it.

Native GotoWander uses JobDriver_Goto's destination reservation and movement toil. Before path
delivery, validate request ownership, cell target, start/progress, end mode and customizer/provider
constraints. Resolve a request exactly once, retain correct native path ownership, and release
owned pooled/native allocations on every unsuccessful handoff. Path totals must truthfully reflect
the consumed native/composed request contract; reference arithmetic alone is insufficient.

Owned invalidation must prevent a native global re-request from pursuing the stale idle endpoint.
End the affected idle attempt once, release its claims and re-enter ordinary priority selection;
if still idle, wait at the effective provider cadence. Native TryGiveJob behaves differently when
already running GotoWander, so merely returning pawn.Position or PawnPath.NotFound is not proof of
bounded waiting. Test the real failure/arrival/job-override sequence before choosing hooks. Urgent
work bypasses this ordinary idle wait through native priority, not a separate long cooldown.

Save no live paths, native regions, pooled jobs or map references as serialized cache. Loaded local
jobs without a current admitted plan may terminate once and wait/rethink. Preserve valid purposeful
jobs and actual positions. Clear context on despawn/death/map unload/game switch; another save or
reused job must never inherit it. No saved direction or exact random-trajectory migration is promised.

### One settings path and one operational state

Definitions: availability on; shared wildlife/companion range 3–10; confined range 2–3. Each bound
is an integer 1–10 and a range edit is atomic with min ≤ max. These are proposed tuning bounds;
shorter actual paths still remain valid. The small bottom-right button writes only the current-save
availability override through common storage; the settings row displays the same value and Use global
state. No second operational boolean, per-animal config, global Pathfinding Lab toggle or source
settings import. At main menu disable all save callbacks, including button/shortcut routes when
there is no game. Dormant overrides, global edits, save switching and seven languages follow the
foundation. Label the button as idle behavior, not whole-game pathfinding or a speedup promise.

### Per-patch semantic target admission

Before implementation, create a stable target document per actual Harmony/XML patch and link its
observable scenario. Prefer direct owned calls for S024/S030/S065; never patch our own ports. Any
declarative tree insertion is additive, XML-Extensions guarded and preserves unrelated branches.
XML Extensions becomes a required runtime dependency at first use; Harmony at first eager use.

| Atomic route/group | Minimal effective facts to prove | Failure behavior |
| --- | --- | --- |
| Context/destination | Correct actual provider, current root/boundary/validator, native wait and job decoration | Leave that ordinary provider intact; no partial local-destination replacement |
| Job/request/follower | Exact resulting job and path request, successful physical delivery, actual costs, next-step revalidation, failure/wait/reuse lifecycle | Entire affected directional route inactive, including pending continuations; supported ordinary movement remains |
| Boundary/current safety | Real current native pen/room/permission/provider state and invalidation | Reject stale local work without manufacturing confinement or movement |
| Optional provider | Active canonical package, expected consumed Def/API and effective movement/context semantics | Only unproven provider route declines with contextual explanation |

Check effective skip-original and incoming-transpiler behavior, not just original IL or target
existence. Every transpiler validates its actual input on every run and returns it unchanged on
failure. No speculative foreign-transpiler replay. Hashes identify researched/build artifacts,
not compatibility. False/unknown predicates, exceptions and installation failures leave the atomic
route inactive and remove only newly installed owned patches in that group. Idempotence/repatch
handling must preserve foreign owners and produce one bounded startup warning per skipped enabled
patch with spec/target/reason/unavailable behavior/fallback. Explicitly disabled routes are not
failed candidates. Another planner's package/owner presence alone is not failure; effective ownership
and semantics decide cooperation. Never change that mod's mode or silently unpatch it.

## Risks / Trade-offs

- Short confined walks can appear calmer but do not prove less total work → measure context,
  planning, invalidation, idle waits, actual jobs and shared updates together.
- Native room/pen caches have different invalidation paths → current native facts and targeted
  topology/provider tests, no per-animal flood fill or stale serialized membership.
- Conservative doorway avoidance may keep idling on one side of a barn flap → explicitly idle
  only; food/shelter/other purposeful travel remains available and verified.
- Missing plans after load or native job pooling can cause repeated failure → exact ownership,
  once-only termination and normal waiting, including already-running GotoWander cases.
- Different planners may both replace movement → effective semantic admission and one active
  route, not blanket package rejection or stealing another mod's global setting.

## Migration Plan

No upstream settings/save import or pen/animal assignment rewrite. New saves inherit common defaults;
existing animals/rooms/pens remain actual game state. Transient plans are discarded safely on disable
or reload; saved actual jobs/positions follow the scoped recovery contract. Future owned save-schema
migration needs explicit backup/atomic recovery and verified postconditions. No safe-uninstall or
provider compatibility claim precedes native acceptance.

## Open Questions

DW-Q1–5 retain proposed context/door/tuning policy, effective native job/follower and topology seams,
shared data and combined costs, exact optional providers/UI/lifecycle, and reviewed-build native
movement/needs/geometry acceptance. Record unresolved research here; do not ask the user to explain
existing native or source-mod behavior.
