## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), S100/P2-29, 1.7 beta.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-snap-out-research.md) pins public source
621ba2b953981ddbf1624c055995bb91d44e0fcd, selected 0.9.1.0 binary, XML and native 1.7.4998.
The source uses owned WorkGiver/jobs/interaction/memory, a global Wander_Sad XML edit and global
settings; its selected assembly has no Harmony patch set. Documentation phase only.

## Goals / Non-Goals

**Goals:** native work reachability, faithful source probabilities, bounded state transitions,
honest feedback, scoped settings, actual helper-memory consumer support and independent admission.

**Non-goals:** a general therapy system, new bottom tab, automatic rescue/recruitment, curing every
custom mental-state mechanic, modifying all sad wanderers, copying jobs/settings from an old save,
an independent pawn index or a claim of Multiplayer support from source attributes alone.

## Decisions

### Prefer owned jobs and current native pawn enumeration

Use a small owned WorkGiver_Scanner with the map's native pawn list. Do not inherit 1.7's
WorkGiver_Warden.PotentialWorkThingsGlobal unchanged: it returns only spawned colony slaves and
prisoners, despite source ShouldSkip allowing colony-only scans. Preserve source Warden priority
18 within normal work ordering; do not overrule player work priorities. Required capacities are
the union of source XML Manipulation/Talking and code Talking/Hearing/Moving. Reuse native
reservation/path/interaction checks. Check target type before any cast.

Capture the specific mental-state instance/episode at admission and revalidate at approach,
conversation start, completion and recovery. Natural recovery or replacement invalidates the
attempt; a same-def newer state is still a different episode. Use faction relationship independently
of temporary mental aggression. Source CanDo permits hostile uncaptured factions accidentally;
owned policy explicitly excludes them and respects colony-prisoner permission separately.

Represent selected compatibility exclusions by inspected native state identity/semantics, not
substring matches that reject unrelated names containing Fighter. Supported ordinary breaks use
native recovery; known care/forced-combat/infant/toddler states are excluded. Unknown custom state
semantics need an optional narrow adapter or an unavailable explanation, not a guess at a cure.
Combine owned Toddlers care and social-fight rules directly. No Harmony patches of owned jobs.

### Preserve probability semantics rather than the misleading raw score

Source DoFormula returns score/100, then compares a uniform 0–0.7 draw. Derive the effective chance
as Clamp01(score/70), with negative opinion penalty expressed arithmetically rather than a loop.
Read actual native NegotiationAbility and SocialImpact, so capacity/social modifiers compose once.
Keep the source's separate aggression gate Clamp01(SocialImpact × weight / 0.85). Use ordinary
simulation randomness, not same-tick seeds that couple unrelated conversations, and persist a
resolved outcome so loading cannot reroll it. Native skill learning supplies passion/learning effects.

The source field initializer/reset uses StunWeight .55 but ExposeData's missing-key default is .45.
Choose .55 consistently in the common definition; explicitly documented as resolving source drift.
The source AlwaysSucceed only sets the final random draw to zero: negative scores still fail and
the aggression gate is untouched. Owned debug override bypasses both rolls only after eligibility.

### Make the recovery lifecycle explicit

Source aggressive success recovers the old state then starts Wander_Sad; non-aggressive success
keeps the original state until a two-job delayed recovery. It clears all catharsis memories when
suppression is enabled, ignores TryInteractWith's return, and assumes a mental state remains during
the final job. Replace this with one owned, serializable attempt/recovery lifecycle. Never credit a
conversation whose required interaction was rejected. Do not interrupt the target's current job
before final validity checks solely to obtain a roll.

Honoring that return value also requires an explicit counselling-specific interaction contract.
Native PawnUtility.IsInteractionBlocked consults MentalStateDef.blockInteractionRecipientExcept;
Core Berserk and FireStartingSpree allow only Insult, and Slaughterer has an empty allowed list.
The source's ignored return hid this obstacle. Prefer guarded additive XML Extensions entries for
the owned calming InteractionDef in the inspected supported states' existing exception lists,
preserving every existing entry and adding no duplicate. Do not set the list to null, replace it
wholesale or globally return false from IsInteractionBlocked. Keep the owned interaction unavailable
for random social selection; only the validated counselling job invokes it. Other native/provider
blocks still apply. Optional states require their own package/Def/semantic checks. Missing/ambiguous
exception shapes disable counselling for that state with bounded per-patch diagnostics; no success
roll is a substitute for the admitted real interaction. The owned 30-base-XP interaction is an
intentional correction for source cases whose blocked interaction previously earned no such XP.

Distraction must have an independently releasable, serialized owned contribution. Do not simply
apply a full-duration native StunFor then forget it when the helper's job ends. Establish exact
native stun/immunity/overlap semantics and a cancellable scoped effect before implementation;
cancel/completion/load-invalidity releases our contribution by the next game update and preserves
pre-existing/concurrent foreign stun duration and attribution. Do not restore an old shared
countdown snapshot or call a blanket stop-stun that erases later effects. If that ownership cannot
be safely admitted, aggressive counselling remains inactive with an explanation. No long stun
without its associated active conversation; normal interrupted attempts still gain no reward/cooldown.

Design recovery around one addressed episode, one reward, one native end notification and at most
one warranted recovery memory. A harmless owned time-alone phase must suspend old harmful actions
without changing global Wander_Sad eligibility. Preserve the original causedByMood/recovery-thought
semantics across that phase. Decide the minimal native hook/state subclass during implementation
against real 1.7 state/job behavior; prove no duplicate PostEnd, recovery notification or catharsis.
Do not clear all queued jobs or an unrelated new mental state. Preserve foreign/native side effects
unless a documented narrow compatibility contract requires otherwise.

Use bounded destination selection: admissible own bed, then one local fallback, then finish in
place if neither is possible. Revalidate destination and episode after travel; no endless search,
bed requirement for visitors or assumption that a room on another map is reachable. Natural expiry,
downing, death and map departure invalidate only our transient control. Save remaining phase time,
reward-delivery state and owned cooldown; do not serialize a second native mental-state object.
Per-episode identity across load and the exact recovery hook are SO-Q1 implementation gates.

Use a separate recipient cooldown expiry rather than source lastAssignedInteractTime, which other
warden work also uses. Capture effective gameplay settings once for an admitted attempt and compute
expiry on resolved failure. Successful recovery itself excludes further attempts; do not add a
new six-hour penalty to an unrelated later break. Failed non-aggressive retreat preserves the
source's integer-roll 66% and 1,750–3,499-tick duration with safe access and cancellation.

### Settings, state and UI remain separate

Common definition IDs under social.counselling describe defaults, ranges, order and tooltips from
the observable spec. Use shared global/save load/store/render/effective resolution and retain dormant
overrides. General eligibility options precede chance/time advanced controls. The expanded/collapsed
advanced section is presentation state; do not serialize it as a gameplay setting. Original global
keys are provenance, not an automatic import contract. Top debug diagnostics/guaranteed-outcome
controls are session-only exceptions because they are development/cheat actions, not game rules.

Native contextual orders, work status/progress, messages, speech and mood memories are the only
ordinary surfaces. Add precise blocked reasons and distinguish start of time alone from final
recovery. English, German, Spanish, French, ChineseSimplified, Russian and Japanese ship together;
do not copy upstream English speech wholesale without attribution if reused.

### Preserve the demonstrated external memory contract narrowly

Character Development 3770112044 / ferny.characterdevelopment defines WQE_WantHelpSnapOut gated by
MayRequire=weilbyte.snapout, referencing SnapOut_HelpedSomeoneThought; its worker checks that memory
and its memory postfix completes matching active wants. Preserve that ThoughtDef name when owned
provider selection permits it, including +3/0.5-day/one-copy/Psychopath behavior. Do not manufacture
the memory for failed counselling or grant a replacement-specific extra want reward.

Preserving only the name is insufficient: the author's want is filtered out without the original
package. A narrow optional XML Extensions adapter must admit the provider package and exact raw
want shape, then redirect only that existing want's prerequisite to the selected owned provider
when the original is absent. This is an intentional scalar gate change, not replacement of the
want collection or its reward/eligibility. Check raw-XML filtering/patch order before choosing the
operation; if the prerequisite is removed before ordinary PatchOperations can access it, use a
documented admitted pre-filter adapter or a provider-supported alternative, never silently duplicate
the full want. SO-Q3 holds that timing decision. Ensure the want is unavailable if owned runtime
admission subsequently fails; a pre-load package gate alone cannot guarantee runtime availability.

XML Extensions becomes required at first XmlExtensions.* use. Use FindMod packageId=true plus
node/field/content guards; preserve all unrelated attributes/children/wants. The memory consumer is
optional and shape-checked; its absence must not prevent counselling. Original presence suppresses
our overlapping Defs and work before loading, and suppresses this adapter. No fake package or
blanket SnapOut assembly/type facade.

VRE Android's actual selected binary checks the original package, reflects SnapOut.SnapCheck's
private static incompatDef list and adds VREA_Reformatting/VREA_SolarFlared. Without the original,
that path will not run. Implement equivalent package/Def/shape-resolved exclusions directly in our
owned eligibility policy; do not spoof the original package or reproduce a private mutable list
merely to trigger its startup reflection. On an active provider's changed exclusion contract,
disable counselling of its affected android population with one bounded warning, preserving ordinary
non-android counselling. Validate that fallback identification itself has a supported contract.

School & Learning's installed source reflects SnapOut.SnapDefOf.CalmDownJob to order intervention,
then falls back to a move/wait when missing. No DLL exists in the inspected package, so this is a
source-only integration lead, not an active binary compatibility claim. Its 900-tick teacher restore
is shorter than our default 1,250-tick conversation; preserve no facade until a selected compiled
consumer and its restoration lifecycle are inspected. Keep this explicit unsupported boundary in
coverage rather than silently claiming drop-in compatibility.

### Target admission and shared resources

Owned C#/Defs do not require blanket Harmony. For every actual native/optional hook, document a
stable target spec and executable minimal admission before use: work contextual dispatch if needed,
state/recovery ownership, selective catharsis handling and provider gating. Match only required
signatures/semantics, original and effective behavior; account for skip-original prefixes. Any
transpiler must validate its incoming stream every run and return it unchanged on failure. Permit
benign edits and foreign compatible patches; revalidate supported later repatching. On failure,
disable the declared atomic owned group, roll back only newly installed owned hooks, and emit one
bounded startup warning per skipped enabled patch with ID/target/reason/behavior/fallback. No hashes
or empty Harmony-owner lists as admission policy. Queued jobs check current availability too.

Use native map pawn/capacity/relationship data and shared owned health/work services, without caching
pair-specific chance or eligibility. No separate Thing or pawn search index. Keep transient state
bounded to participating pawns, prune dead/removed references, clear map/game resources at teardown
and rebuild necessary transient bindings after load. Verse/Unity access stays on the game thread.

## Risks / Trade-offs

- Higher effective success than a raw score suggests → preserve 0.7/0.85 denominators and verify
  representative low/normal/capped stats; no undocumented rebalance.
- Recovery has native side effects and provider-specific mental states → explicit episode lifecycle,
  ordinary end effects and admission; reject unsupported semantics rather than broad forced clearing.
- 1.7 source scan misses ordinary colonists → own native-list scanner and colony-only native scenario.
- Source says Multiplayer supported → retain as a research fact, but require exact-provider tests,
  simulation-safe random/state/settings synchronization before claiming owned multiplayer support.
- Optional want depends on early XML gating → timing and runtime-availability proof before acceptance.

## Migration Plan

No automatic source-save/settings migration. New owned saves persist only the described state.
Original active jobs, assembly/type references and settings files are not claimed drop-in portable.
Any later import requires its own backed-up atomic conversion, failure restoration and verified
postconditions. Co-installation leaves original state alone. No data writes, install or game run now.

## Open Questions

SO-Q1: exact 1.7 work/context/recovery admission and durable episode identity; prove interruption,
natural recovery, load and alternate mental-state effects without losing unrelated state.
SO-Q2: finalized negotiation/social-impact/catharsis/work comparators and focused native playability.
SO-Q3: Character Development raw-XML timing, guarded want availability and any further real consumers;
Multiplayer remains unverified until its explicit scope and synchronization are specified/tested.
SO-Q4: complete seven-language UI/settings/debug lifecycle, performance and reviewed native evidence.
