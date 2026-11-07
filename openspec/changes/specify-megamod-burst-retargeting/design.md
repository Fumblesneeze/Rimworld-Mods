## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Source S081 `Spray.Transfer`, Workshop3490194365. Documentation first, full two-class binary and
current native shooting/turret/search paths inspected; [research](../../../mods/megamod/docs/refinement/2026-11-04-spray-transfer-research.md).
No code executed or runtime work accepted.

## Goals / Non-Goals

**Goals:** redirect remaining automatic ranged burst shots when a pawn goes down, preserve actual
weapon rules and explicit control, and constrain any bridge by semantic evidence.

**Non-goals:** more shots/damage/range, homing in-flight projectiles, new targeting AI, a combat
framework, a new toggle/settings screen, or blanket support for every custom Verb subclass.

## Decisions

### One feature-local decision at the actual shot boundary

Source prepends `SprayShift(this, ref currentTarget)` to discovered `TryCastNextBurstShot` methods.
It tests old targetPawn non-Mobile; pawn caster requires drafter/FireAtWill and CurJob.targetA!=target;
non-pawn caster skips those tests. It searches through casterIAttackTargetSearcher with only
NeedLOSToAll|NeedThreat|NeedAutoTargetable. It has no shot-type/burst-size/availability guard,
turret hold/forced-target check, null CurJob guard or semantic target check.

Use a combat feature-local policy with actual firing Verb, caster/searcher, current target,
burst state and current explicit-order authority. Restrict to true multi-shot ranged firing, not
the entire Verb universe. Preserve target-bound jobs, including S008/S064/S065 hunting/finishing,
through direct owned context rather than self-patches. No hard-coded player-only rule: caster's
existing firing controller and hostility govern; pawn without an applicable controller remains native.
Turrets need native hold-fire, forced-target, manning and target validator context. No source blind
cast/CurJob dereference or speculative compatibility type names.

Candidate resolution must use the actual firing Verb. Native BestShootTargetFromCurrentPosition
uses searcher.CurrentEffectiveVerb, not the method's `verb` argument. Prove identity/equivalence
or use a narrowly adapted search context that faithfully supplies the actual weapon and original
searcher's authority; never borrow an alternate weapon's reach. Native attackTargetsCache is already
the correct candidate index. No duplicate map scans/cache, static retained candidate or saved target queue.
Use live native selection preferences, including weighted scoring where native chooses randomly;
do not replace them with nearest enemy or a frozen selection snapshot. Revalidate the returned
candidate's actual CanHit/permissions before committing; generic search fallback is not proof.

### Preserve complete native shot accounting and feedback

Current `Verb.WarmupComplete` sets burst count/state and immediately calls TryCastNextBurstShot.
`VerbTick` calls it when the next-shot interval expires; it resets a despawned caster or stunned pawn.
TryCastNextBurstShot snapshots currentTarget **before** Available/TryCastShot; success performs
effects/target notifications/fuel and decrements shots, failure zeros remaining shots. Remaining
shots set interval/stance; final shot sets idle/cooldown, callback and per-burst fuel.

A candidate bridge must therefore place the transfer before the relevant snapshot/shot consumption,
retain current availability, and preserve all downstream effects/accounting. Do not simply append
a postfix after firing or mutate a target after the method has already captured it for notifications.
Validate incoming/effective code, including foreign skip-original/argument/target changes. No actual
bridge is selected just because the source prepends IL. A narrow prefix is preferable if complete
effective semantics can be proven; otherwise a guarded structural transpiler is justified by that
specific boundary, not copied insertion-at-index0.

Synchronize only necessary actual aiming/feedback state under the admitted native path; never
rewrite pawn job targets or turret forcedTarget to manufacture automatic authority. Turret
currentTargetInt and Verb.currentTarget are distinct in source; prove visible aim and callback
coherence before supporting that path. If a dependent path cannot be safely synchronized, leave
its whole transfer inactive. Native ongoing-burst save fields currentTarget/burstShotsLeft/interval
remain authoritative; no new persistent transfer state or replay job is needed unless proven otherwise.

No candidate => no target write and preserve native shot behavior. FireAtWilloff/HoldFireon means
no transfer, not an invented instant cease-fire rule. No new retargeting delay/angle cap is chosen;
source benefit is retaining the burst. Profile/measure only if claiming performance later.

### Preserve native turret and weapon selection semantics

Current turret TryFindNewTarget uses the manning pawn when appropriate, includes smoke for direct
fire, non-burning for incendiary, thick-roof restrictions for mortar and IsValidTarget filters for
prisoners/machine allegiance/player animals. Source's generic three flags omit these contexts.
For automatic transfer use the shooter's actual native target policy, not a blanket LOS rule that
would incorrectly restrict supported overhead fire. Keep current native/final shot hit tests,
range and availability authoritative. Unknown custom beam/chain/ability/vehicle/CE/MVCF paths are
unclaimed until an exact provider adapter and causal workflow exist. An installed mod name alone
is not a reason to disable unrelated admitted weapons.

One native exception must not be copied into transfer unfiltered: TryFindNewTarget first has a
50% branch for hostile overhead fire that selects an in-range colonist building directly, before
the ordinary threat/auto-targetability/IsValidTarget search. SPT03 intentionally keeps the source's
threat and auto-targetable transfer restrictions. Native preferences apply **within** that eligible
set; calling TryFindNewTarget alone is not proof of eligibility. Do not let the bombardment branch
bypass those predicates. Ordinary native bombardment outside transfer remains unmodified. A direct
native result can only be accepted after every actual-weapon, turret and transfer predicate passes;
otherwise use an admitted filtered selection path or leave the target unchanged.

### Patch admission and provider ownership

Harmony is a required dependency when implemented, ownerfumblesneeze.megamod, no bundled0Harmony.
Source metadata lists Harmony only as loadAfter, while DLL eagerly references it and public
Workshop requires it; do not copy that dependency omission. No XML patch is currently selected.

Candidate target families **SPT-SHOT** (scheduled shot target use/accounting) and **SPT-TURRET**
(authority/selection/feedback, only if actual patches needed) require concrete per-patch target
documents linked to validator/tests/native evidence before code. Deduplicate exact resolved methods;
do not patch every type by same method-name lookup. Shape guards cover callable signature, intended
current target flow to actual shot and target notifications, state/count/interval/callback ordering,
and relevant native selection/authority contracts. Exact bytes/version/full-text identity and absence
of other owners are not predicates. Compatible foreign changes remain admitted.

Document startup ordering, effective chain, later repatch invalidation and atomic dependent group.
Every transpiler validates actual incoming instructions each run, returns them unchanged on failure,
and never speculatively re-executes foreign transpilers. False/unknown/exception/install failure
leaves the affected transfer inactive and rolls back only newly owned patches. Bounded invariant
startup warning per skipped enabled patch includes package/patch/spec/target/predicate/behavior/fallback.
No per-shot log spam, unguarded PatchAll, speculative foreign patching or internal feature patches.

Active `Spray.Transfer` owns all overlap; no source setting introspection or automatic unpatch.
Source public `SprayTransferPatch.SprayShift(Verb, ref LocalTargetInfo)` and transpiler names are
potential bindings, not proven consumed APIs. Bounded installed text/ASCII-DLL search found only
source identity/assembly; computed/reflected consumers still require inspection when claimed.
No DLL/package alias or fabricated drop-in guarantee. Comments about CE/RunAndGun/Milira are leads,
not verified compatibility reports; no installed Milira provider identity was established by text search.

## Risks / Trade-offs

- Improved remaining-shot utilization is a real combat advantage without more nominal damage.
  Compare native automatic burst, owned automatic burst and explicit targeted burst with matching
  weapon/ammunition/targets for final balance. No measured advantage is claimed from source alone.
- Restricting to true ranged bursts and preserving forced turrets corrects broader source behavior;
  these are explicit SPT-Q1 draft choices. No arbitrary angle/timing limiter is invented.
- Searcher weapon mismatch can choose an unreachable target; actual-Verb validation is mandatory.
- UI/notification state can disagree with projectiles when only one target field is changed;
  atomic admitted path and native observation, not installed Harmony count, prove this behavior.

## Migration Plan

No source settings or custom save state exist to import. Preserve native ongoing burst serialization;
no modified normal saves/configs. Future package deployment through typed operations only. Source
provider presence is a runtime selection decision, not permission to uninstall it.

## Open Questions

- **SPT-Q1:** Final supported attack-family boundary and balance review of retained no-extra-warmup
  transfer, multi-shot scope and explicit pawn/turret authority corrections.
- **SPT-Q2:** Actual1.7 effective shot/selection/feedback contracts, availability ordering, native
  save continuation and concrete patch validators, including multi-verb and manned turrets.
- **SPT-Q3:** Actual incoming/reflected consumers and requested special-weapon/provider adapters;
  no CE, MVCF, RunAndGun or Milira support claim from public comments or broad type scanning.
- **SPT-Q4:** Focused RED/GREEN, package/localization, reviewed-build native shooting/control/save/
  provider evidence and final playability assessment. No runtime tasks accepted.
