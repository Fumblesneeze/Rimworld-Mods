# Hunting companion tasks

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Original S065; documentation only. No implementation or acceptance task is checked.
Implement one observable slice at a time through focused RED → minimum GREEN → refactor, using
honest test environments and durable nonzero evidence before reviewed-build native acceptance.

## 1. Resolve policy and native seams

- [ ] 1.1 Review HC-Q1 proposed native master/follow permissions, delayed-collapse attribution, normal melee finish, hazard fallback, nearest claimant and 3-cell/2-hour defaults; update scenarios for changed decisions.
- [ ] 1.2 Complete current 1.7 hit/down/death/bleeding/corpse/outcome and job-completion/interruption seams, including duplicate packet ordering and native corpse reservations.
- [ ] 1.3 Pin ordinary animal needs/duty priority, permission/training/area rules, native single-load versus S021 pack capacity and exact S049 destination admission.
- [ ] 1.4 Create stable per-patch target documents, effective semantic predicates, atomic handoff/recovery groups, repatch handling and warning/fallback tests before any hook.

## 2. Accompanying and permissions through vertical TDD

- [ ] 2.1 RED/GREEN current Bond/Attack/Haul qualification, all permitted companions, master/follow choices, learned versus wanted training, trainability changes and no Odyssey AttackTarget requirement.
- [ ] 2.2 RED/GREEN native Hunt versus generic Fieldwork/drafted S008 contexts, station phases, one animal/hunter assignment and ordinary non-companion following preservation.
- [ ] 2.3 Compose S066 directional following with the hunter as root and shared distance, legal purposeful regrouping, shorter/no-move outcomes and admitted ordinary style when S066 is off/unavailable, retaining needs suspension without a Pathfinding Lab dependency.
- [ ] 2.4 RED/GREEN needs/medical rest/temperature/duty/rope/player-order priority and uninterrupted existing cargo; prove temporary composed follow suspension prevents native recapture and permits S030 recovery without mutating saved follow choices.

## 3. Attribution and atomic handoff through vertical TDD

- [ ] 3.1 RED/GREEN effective actual ranged hits on current intended quarry, immediate/downstream state changes, absorbed/missed/collateral shots, direct death and supported provider status effects.
- [ ] 3.2 RED/GREEN delayed collapse, contributing injuries without false kill credit, canceled/retargeted hunts, recovery clearing old-hit authority and save-preserved valid attribution.
- [ ] 3.3 RED/GREEN nearest eligible claimant/stable ties, finishing independent of hauling, one actor/target, originating versus foreign reservations and actual corpse transition.
- [ ] 3.4 RED/GREEN atomic reserve/start/hunter suppression, failed-start rollback, repeated notices and no double finishing/carrying or replay of already fired projectiles.
- [ ] 3.5 RED/GREEN interrupted claimant reassignment, fixed shared pre-pickup deadline and bounded failed-candidate reconsideration without extending the attempt on load/context changes.

## 4. Animal finishing and actual retrieval through vertical TDD

- [ ] 4.1 Implement only scoped downed-prey melee intent through native tools/cooldowns/damage, with current authorization before each new attack; no execution-cut, predator-eating, target substitution or yield bonus.
- [ ] 4.2 RED/GREEN recovery/protection/faction/fog/forbidden/station cancellation, unavailable body tools, dangerous melee-death fallback, direct death during approach and absent corpse.
- [ ] 4.3 RED/GREEN one actual hunt outcome and actual participants' damage/kill consequences, independent S008/S117/S118 scopes and no repeated hunter tale/record from handoff retries.
- [ ] 4.4 RED/GREEN physical corpse pickup with native Manipulation/carry-volume limits, non-pack dogs, actual batch mass/volume when used and required unloading before ordinary fallback.
- [ ] 4.5 RED/GREEN finisher-to-retriever transfer, no capacity/destination, S049 automatic demand/incoming claims and player forbid after initial corpse unforbid.
- [ ] 4.6 RED/GREEN destination invalidation, actual delivery/physical recovery, carrier death/incapacity and no deletion/duplication/cross-map recovery or second capacity allowance.

## 5. Lifecycle, UI and compatibility

- [ ] 5.1 RED/GREEN natural hunter completion versus explicit interruption, native living-prey Hunt cancellation versus death cleanup, new hunt while old retrieval continues, reassessment/rejoin, changed master/bond/training and map/game teardown.
- [ ] 5.2 RED/GREEN save/load of every live/prey/corpse/held/delivered phase, fixed deadline, missing identities, conflicting holders and disabled/admission-failed saved attacks.
- [ ] 5.3 Implement contextual Animals/training activity/reasons and common availability/distance/budget settings with scoped persistence, dormant overrides, finite input validation, main-menu callback guards and seven-language text.
- [ ] 5.4 Connect native relations/listers and S033 common map facts with minimal fresh owned claim state; measure combined costs and avoid private whole-map caches/scans.
- [ ] 5.5 RED/GREEN compatible foreign patches, effective skip-original/incoming changes, invalid targets/exceptions, idempotence, atomic owned rollback and bounded per-patch startup diagnostics.
- [ ] 5.6 Verify proven optional provider boundaries, additive guarded Animal decision changes, preserved trainable order, required Harmony/XML Extensions at first use and package/Gateway independence.
- [ ] 5.7 Run focused nonzero tests, owning build/package checks and typed OpenSpec validation; obtain independent implementation review, resolve findings and rerun affected checks.

## 6. Reviewed-build native acceptance

- [ ] 6.1 In a fresh minimized isolated game, use native training/master/follow controls and Hunt/station orders; observe multiple permitted companions and actual wandering/regrouping/needs behavior.
- [ ] 6.2 Through actual hunter shots, observe immediate downing, direct death and delayed collapse, one claimant, human handoff, real animal attacks and exact corpse retrieval; personally inspect causal before/action/after views.
- [ ] 6.3 Observe non-pack single-load retrieval, insufficient capacity, no storage/demand, different finisher/retriever, existing cargo and actual destination-change recovery without duplicate loads.
- [ ] 6.4 Exercise recovery, tame/forbid/filter/pause changes, dangerous prey, drafting, needs/duties, one-cell blockage, failed starts/timeouts and hunter/animal incapacity through native workflows.
- [ ] 6.5 Exercise saves during hit context/approach/finishing/carry/delivery, changed maps/permissions/admission and retained cargo; verify main-menu/inherited settings and all baseline languages.
- [ ] 6.6 Run exact admitted provider-present cases and final balance/playability review of finalized animal trainability, attacks, body/carry/needs costs, hunting risk/yields and measured combined performance; accept no compatibility/performance claim from static inspection.
- [ ] 6.7 Satisfy root AGENTS.md final acceptance on the reviewed build: exact build and ordered mods, real native actions, personal observations, causal captures, normal-config hashes and cleanup; check only tasks whose evidence is complete.
