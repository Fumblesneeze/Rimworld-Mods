## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S092/P2-21. [Research](../../../mods/megamod/docs/refinement/2026-11-05-repeat-conversion-research.md)
pins public documentation, all seven shipped C# source files, the selected 1.7 Release DLL, native
ability/job/save paths and optional provider XML. Documentation only; no native behavior accepted.

## Goals / Non-Goals

**Goals:** retain explicit conversion intent, submit one valid native cast at a time, preserve manual
orders and temporary waiting, cancel terminal assignments reliably, and apply inherited preferences.

**Non-goals:** choosing targets automatically, converting prisoners through warden work, granting
roles/abilities, changing certainty math or cooldowns, automating rituals, global ideology changes,
porting the optional content provider, or importing source saves/settings without an owned migration.

## Decisions

### Own assignment state rather than patch every Human definition

Use a small per-game collection of actual assignments, each tied to guide, target, assigned map,
intended Ideo and owned pending-job identity. This is primary gameplay state, not a copy of a map's
Thing list. Native pawns, roles, abilities and jobs remain the owners of their existing data. Avoid
all-pawn scans, duplicate pawn indexes or a feature-specific cache of derived role/health facts.
Use shared scheduling/invalidation where already owned; do not invent a framework solely for this feature.

The source adds CompProperties_ConvertAuto only to Human with unguarded PatchOperationAdd, then
dynamically attaches a Comp for other eligible pawns. Its GameComponent separately serializes a
trackedGuides list. Prefer one authoritative assignment store that covers eligible humanlike races
without XML injection or runtime mutation of pawn component lists. No XML is required by this design.
If implementation establishes a real declarative need, use XML Extensions package/content/node guards,
idempotent additive membership and the required dependency; preserve all existing components.

Persist intended ideology rather than reconstructing it from the current pawn on PostLoadInit.
Resolve references and remove invalid/duplicate entries after cross-reference resolution, without
casting during load. Derive scheduled participation from valid assignments; do not serialize an
independent stale registration list. Bind pending jobs to assignment identity, preserve that identity
through ordinary job saving, and recover no more than one admitted pending/current owned job on load.
If ownership cannot be established, decline automatic replay; never delete an uncertain manual job.

### Separate terminal validity from temporary cast readiness

Resolve actual assignment participants before dereferencing them. Evaluate terminal reasons first:
completion, death/destruction, departure from the assigned map, target cryptosleep, invalid target
kind/hostility, loss of colony ownership, intended ideology or supported ability. Then evaluate
temporary restrictions such as mental state, drafted/downed/crawling/non-mobile guide, LayDown,
target consciousness, sleep preference, cooldown, allowed area and safe reachability.

Native IsColonistPlayerControlled becomes false during a mental break. Separate enduring player
ownership from that temporary property so the source's 1.7 mental-break retention remains without
masking a simultaneous death, departure or faction loss. A target's non-hostile mental state also
waits; actual hostile target eligibility remains a terminal boundary. Validate full native target
parameters/effect checks rather than the source's partial copy of canTarget flags, which omits
neverTargetHostileFaction and other semantic fields. Native prisoner/slave exceptions remain native.

Source ShouldRemoveAutoConvertTarget returns false for null/destroyed references, omits target map
departure, and clears target fields without removing registrations. Ability-loss cleanup occurs
inside a branch whose CanUseConvertNow has already rejected a missing ability, so it is ordinarily
unreachable. These are static source defects to correct, not observed live reproductions.

### Submit and revalidate native jobs without accumulating automatic orders

Resolve Convert first, then the admitted VME_LeaderConvert alternative. The source searches direct
pawn abilities then role AbilitiesFor; preserve both ownership paths while checking current required
memes for either. Do not treat the existence of an AbilityDef as ownership. Re-resolve at decisions;
never store a stale Ability object across a role change or reload.

Use native command availability (GizmoDisabled), CanCast, CanQueueCast, full target eligibility and
CanApplyOn before automatic cast admission. Assignment itself remains possible during cooldown.
GizmoDisabled adds comp.GizmoDisabled, lord.AbilityAllowed, deathrest and applicable caster capability
restrictions which CanCast/CanApplyOn do not imply. In particular CompAbilityEffect_MustBeCapableOf
checks Social incapability only in GizmoDisabled for both inspected conversion abilities. Preserve
these restrictions; ready cooldown and a valid recipient do not authorize bypassing a disabled command.
Current CanQueueCast examines current/queued jobs for the same verb or ability group; source only calls
CanCast, allowing repeated TryTakeOrderedJob(..., requestQueueing:true) submissions while a different
job is current. Native requestQueueing starts immediately for an idle pawn and otherwise appends,
but its idle branch clears queued jobs. An owned submission adapter must preserve existing manual
orders even in that boundary case, and track at most one pending/current automatic conversion.

Use the native Ability.GetJob/job driver/verb for travel, reservations, warmup, effects, notifications
and cooldown propagation. Current Convert uses CastAbilityOnThingMelee and JobDriver_CastAbilityMelee;
it approaches Touch and checks CanApplyOn while travelling. The source's separate InteractionCell /
Danger.None reachability precheck does not replace native final travel. Keep that conservative safe
preflight and the effective allowed-area target-cell restriction.

Native job drivers do not prove all owned assignment conditions at effect time. Add the smallest
owned-job-scoped validation/cancellation seam necessary to enforce the observable contract, with
documented target predicates. Recheck owner/target/ideology/ability/meme/assignment generation,
temporary restrictions and effective settings before the effect. Do not reapply CanQueueCast in a
way that rejects the current owned job merely for occupying its own group. Preserve native in-progress
casting semantics while retaining all relevant non-queue command refusals, including component and
lord restrictions. Use an admitted equivalent predicate for this boundary, not wholesale suppression
of GizmoDisabled or classification by localized reason strings. Unknown provider command restrictions
must not be waived to make an owned cast proceed. Never directly invoke CompAbilityEffect_Convert.Apply or change certainty to
simulate successful conversion, and never waive a provider confirmation requirement silently.

Cancellation withdraws only proven owned pending work and stops unfinished owned casting before its
effect. Already-applied effects/cooldowns are not rolled back. Explicit player orders win and are
never restored over; a retained assignment can try only at a later normal check. Source cancellation
only clears the target field and leaves queued casts, an intentional owned correction. Exact group
coordination with S051 specialist work commands uses direct calls/ordinary ability ownership, not
patches against our own implementation; neither feature gets an extra cooldown or priority bypass.

### Preserve meaningful settings with one consistent default

Common settings path: Ideology / Repeated conversion; ordered timing, distance, waking, ritual.
Use the shared load/store/resolve/row renderer with Global default, Current save, Use global and
disabled inherited/main-menu mutation paths, including reset/import callbacks. Pawn targets are not
settings. Bound integer intervals/distances as specified and reject invalid edits; malformed stored
values recover to per-field built-in defaults with bounded diagnostics. New/missing entries inherit.

Source static maxDistance initializes to 1000 but Scribe uses 50 for a missing key in a loaded file.
Native LoadedModManager.ReadModSettings returns new T when no settings file exists; therefore a
fresh source install takes 1000. Preserve that fresh-install value consistently in the owned model
instead of silently switching between 1000 and 50. It can span most ordinary maps; the public FAQ's
proximity wording is not evidence for a smaller actual default. Document 50 as an upstream migration
quirk only. No source settings import is included.

Default checks are every 2500 game ticks, independently of native ability cooldown. First assignment
is considered next simulation tick; later readiness is reconsidered within the configured interval.
Use game-scoped scheduling rather than static tickNow leaking across sessions. There is no need to
visit every pawn every tick; process only active assignments and retain a bounded due schedule.
Interval changes affect the next decision without retroactive multiple checks or timer-driven casts.
Cheap invalidity/current-job checks precede pathfinding. No benchmark improvement is claimed yet.

The optional ritual preference checks repeat-penalty state, not overall ritual availability.
Current Precept_Ritual.RepeatPenaltyActive uses its native 1,200,000-tick window when applicable.
Match Conversion to Convert/Moralist, and the admitted VME_LeaderConversion to
VME_LeaderConvert/VME_ExaltedLeader using validated role/group/ritual links. The source checks only
the Core Conversion Def even for leader conversion. Correct that association; do not add an owned
20-day timer or automate rituals. Missing ritual means no pause. Native changes to its penalty stay native.

### Keep UI local and admission narrow

Append an owned contextual command to the guide's existing gizmos, with target/cancel state and a
concise waiting reason. Use an owned command's rendering instead of a global Command.GizmoOnGUIInt
patch identified by translated description. No bottom tab or bottom-right global assignment toggle:
this controls a specific pawn. No borrowed VFE CheckAuto texture or source raster is selected here.
Future new raster work follows the mandatory asset/live visual gate. Localize controls, settings,
disabled reasons and status in all seven baseline languages; test compact/multi-pawn layouts.

Before any hook, register a stable patch-target document and executable minimal predicate. A
GetGizmos additive observer needs its consumed callable/result shape, not exact IL or absence of
other owners. Any hook enforcing a pending job's effect boundary must prove that the checked
assignment and target feed that actual effect, including skip-original and effective transpiler
semantics. Method existence alone is insufficient there. Incoming streams are checked each time;
mismatch returns unchanged input. Do not replay foreign transpilers as probes.

Install atomic owned groups idempotently; missing/unknown predicates, validator exceptions or
installation failure leave the group inactive and roll back only newly installed owned hooks.
Revalidate supported later repatches. Log one bounded startup warning per skipped enabled patch
with package/patch/spec ID, target, failed condition, unavailable behavior and fallback. Compatible
foreign modifications remain allowed. An active canonical linnun.keepconverting suppresses the
owned scheduler/UI whenever the original provider is loaded; explain provider control rather than false missing
functionality. Never unpatch the source or import its state implicitly.

## Risks / Trade-offs

- Native source queueing can accumulate casts -> current/queued group checks plus one explicit
  owned job and effect-time revalidation. Prove cancellation/manual-order boundaries in game.
- Multiple guides can select the same target -> preserve independent assignments and ordinary
  reservations; each rechecks the target's ideology before effects, with no invented global lock.
- Source/public behavior diverges -> document selected 1.7 facts and intended corrections explicitly;
  do not claim the original has been live-tested or that every beta/provider combination is compatible.
- Optional leader content can change -> canonical `vanillaexpanded.vmemese` and consumed Def/role/
  meme/group/job/ritual shape checks, isolated adapter, separate exact-provider acceptance. Do not
  grant roles or patch the provider to make an unsupported shape resemble Core.

## Migration Plan

New games and first enablement start unassigned. Owned save state uses validated references and
intended ideology; no automatic import of source CompConvertAuto/convertTarget, trackedGuides or
static settings. Source saves can contain source-specific types, so removal is not promised clean
or silently repaired. A future importer needs an owned spec for backup, atomicity, rollback and
postconditions before use. No user's normal save/configuration is touched during development.

Core/Harmony are required on implementation. Ideology is feature-required but whole-product optional;
no Ideology means no scheduler/controls. VME and its framework are optional content providers. No
XML currently required; XML Extensions becomes required if used. No source DLL, private Harmony,
Gateway or dependency on another repository-owned runtime mod may enter the product package.

## Open Questions

KCV-Q1–5 retain exact effective job/cancellation/save semantics, full native/provider admission,
settings/UI behavior, S051/original-provider composition, finalized balance and native acceptance.
The draft fixes observable decisions; these are implementation/evidence obligations, not a request
for the user to explain the existing mod.
