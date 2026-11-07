Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification only. All implementation and acceptance tasks remain unchecked.

## 0. Reconcile the user's updated references before implementation

Completed research evidence: [source comparison and decision record](../../../mods/megamod/docs/refinement/2026-11-03-hauling-composition-decisions.md),
with linked exact-source audits, independent review and strict validation. These checks complete
documentation tasks only; every implementation and native-acceptance task below remains open.

- [x] 0.1 Deep-dive Pick Up and Haul - Continued (`Zzyxzz.PickUpAndHaulReworked`, 3829549663), public docs first then source/selected 1.7 binary; replace legacy-only assumptions with pinned findings.
- [x] 0.2 Analyze Pick Up And Haul - Reworked (`Dreadarm.PickUpAndHaul`, 3813653998) as an alternative, including actual public contracts, capacity, personal supplies, loading, recovery and save policy; compare observable outcomes rather than infer quality from its age/name.
- [x] 0.3 Compare dd.Armful Haul (`dd.armfulhaul`, 3808100512) native jobs, hand-stack representation, capacity, coordination, interruption/save behavior, rendering and performance boundaries; investigate the stated PUAH incompatibility. Keep S126's required visible hauled stacks regardless of which hauling logic is selected, without importing extra independent capacity allowances.
- [x] 0.4 Inspect the selected 1.7 While You're Up reference (`patch1.7.whileyoureup`, 3832180338) where it intersects these batch implementations; retain original provenance and direct owned S125 composition. This narrow intersection does not complete S125's separate full feature analysis.
- [x] 0.5 Resolve PH-Q5 by documenting comparison evidence and chosen behavior, updating proposal/design/specs/coverage and consumed interfaces, and independently reviewing/validating the revised draft. S126 separately owns detailed visual scenarios and acceptance; no new-source completeness claim comes from legacy review.

## 1. Pin implementation contracts

- [ ] 1.1 Resolve or explicitly retain PH-Q1's combined capacity, starting threshold and ordinary heavy-item fallback interpretation; measure actual beta comparators including steel, small-volume currency, equipment, corpses and native pack/mech workers before changing any proposed rule.
- [ ] 1.2 Inspect exact beta work selection, transfer/merge/partial-acceptance, destination, reservation, interruption, save/holder and Gear/action paths; record missing provider capabilities and add observable cases before implementation.
- [ ] 1.3 Establish canonical optional provider IDs, consumed signatures/semantics and current compatibility policies for the real consumer table; preserve feasible public behavior without source assembly/package impersonation or guessed private facades.
- [ ] 1.4 Create each selected PH-T1–6 individual target document with executable admission, effective-code/repatch policy, atomic dependency group, rollback, bounded startup warning and safe saved-cargo fallback. Do not create a patch for an owned direct call.

## 2. Focused vertical RED/GREEN slices

- [ ] 2.1 Record focused RED/GREEN for combined mass/volume fits, existing supplies/hands, partial units, zero/invalid measurements, provider extra limits and capacity reductions in a host-safe calculation environment.
- [ ] 2.2 Record focused RED/GREEN for actual source/destination quantity allocation, mixed shelf slots, linked-member limits and competing commitments; preserve native per-pawn eligibility at use time.
- [ ] 2.3 Record focused RED/GREEN for finite route construction, source-derived distance rules, stable ties, forced-seed versus ordinary extras and urgent-only filtering; reject zero-progress loops.
- [ ] 2.4 Record focused RED/GREEN for successful/partial/failed transfer accounting, split/merge, personal versus cargo portions, unqualified consumption, destroyed/stale identities and no same-type recovery guesses.
- [ ] 2.5 Implement minimum physical collection and delivery through native action paths with owned additive Job/WorkGiver Defs; do not use source assemblies or blanket runtime pawn-Def mutation.
- [ ] 2.6 Implement physical destination grouping and unload/recovery before another batch or single-load fallback, handling partial storage, failed placement, occupied hands, draft-drop off/on, emergencies, duplicate scheduling, saved 600-tick unchanged-failure delay, early changed-condition/manual retry and no safe drop cell.
- [ ] 2.7 Implement saved cargo/kept state and current-holder recovery across save/load, caravan/map transfers, faction loss, death and source co-installation; verify no stale route authority or automatic foreign-save import.
- [ ] 2.8 Implement contextual Take and keep, release/drop and unload/retry with actual capacity limits, exact quantities, supported personal-provider protection and native eligibility; do not globally return unlimited pickup permission.
- [ ] 2.9 Implement the five common global/save preferences, inclusive threshold boundaries, dormant overrides, main-menu disabled callbacks and stated effect timing, preserving existing cargo recovery when a preference is switched off.

## 3. Composition, adapters and shared data

- [ ] 3.1 Compose S008 urgency, S005 linked storage and applicable S049 acceptance policies directly; preserve urgent lifetime and prevent ordinary fallback from bypassing owned refill limits. Once S125's full contract exists, compose its and S024's constrained opportunity through one parent-job decision without duplicate detours or changing production/material choices.
- [ ] 3.2 Use the shared map Thing subsystem/native listers for Ctrl+F, counts and hauling with actual invalidation at mutation/use boundaries; remove redundant full-list copies/static skip sets only after focused behavior is green.
- [ ] 3.3 Implement and verify the optional original Allow Tool delegate and owned-cargo urgency bridge as one PH-T6 admission group: canonical identity, compatible delegate ownership, automatic cleanup versus explicit cancellation, partial pickup, failed pre-cleared placement, nearby dropped markers, mixed loose-stack quantity display and bounded batch/fallback pickups, held Gear cancellation, save/reload and provider loss. Add each actual semantic target, failure/rollback checks and quantity-safe ordinary fallback; never activate a delegate-only partial adapter or promote cancelled units through a whole-Thing marker. Leave unsupported consumers explicitly unsupported.
- [ ] 3.4 For every promised custom mass/bulk/storage adapter, verify actual provider-present and provider-absent/changed behavior in the correct environment. A missing helper or active package alone is not a capacity proof.
- [ ] 3.5 Test selected patches for benign foreign edits, changed argument use, ambiguous flows, skip-original effects, repeated initialization, incoming transpiler revalidation, atomic rollback and warning contents in honest test tiers.
- [ ] 3.6 Benchmark combined search/count/hauling workload, invalidation cost, allocations and memory against the native/source comparators before claiming optimization; keep final job eligibility current.

## 4. UI, review and final native acceptance

- [ ] 4.1 Complete seven-language Gear quantities/reasons/actions, job text and common settings controls; verify supported UI scales and unavailable actions without adding a bottom tab or new work column.
- [ ] 4.2 Run affected typed builds, focused regressions, package/dependency checks and strict OpenSpec validation. Any selected XML mutation requires XML Extensions dependency and guarded merge preservation.
- [ ] 4.3 Independently review the scoped implementation, resolve findings and rerun only affected checks; complete the semantic capacity, priority, storage, personal-supply and heavy-item playability pass.
- [ ] 4.4 On the reviewed build in fresh isolated minimized RimWorld, perform native ordinary and urgent mixed-item hauls and observe real pickups/deliveries with measured existing loads, partial counts, destination limits and multiple haulers.
- [ ] 4.5 Perform native keep/release, draft/undraft with both drop preferences, emergency interruption, grouped storage edits/removal, occupied-hand and failed-drop/retry workflows; personally inspect item conservation, correct kept quantities, saved delay/change/manual-retry timing and no repeated immediate recollection.
- [ ] 4.6 Verify native save/reload, map/caravan transitions, supported animal/mech workers, main-menu/inherited/overridden settings and each claimed original/provider-present adapter scenario; keep unsupported cases unaccepted.
- [ ] 4.7 Retain tested build identity, ordered mods, causal native actions and screenshots, supporting logs, config hashes and cleanup. Check gameplay tasks and replacement status only when root final player-workflow evidence exists.
