# Implementation tasks — automatic self-tend permission

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification-only checkpoint. Every implementation task remains open; static analysis is not
RED/GREEN evidence or native acceptance.

## 1. Establish exact native and provider contracts

- [ ] 1.1 Resolve ST-Q1 on the implementation build: native permission/UI/save, effective Medical level, Doctor ability, map/holder/caravan population and actual player medical job selection; distinguish NPC-only paths.
- [ ] 1.2 Specify and test minimal no-patch feature admission and original-package suppression, including absent/changed native contracts and benign foreign modifications. Add per-patch targets/admission/dependencies only if actual implementation needs hooks.
- [ ] 1.3 Complete scoped consumer/provider audit and direct composition with owned compact Health, Toddlers and any then-specified Drug Response/Priority Treatment contracts; do not invent precedence or source facades.

## 2. Implement one observable policy behavior at a time

- [ ] 2.1 Focused Unit RED → GREEN for first assessment, inclusive threshold, aptitude-aware input and enable-only behavior, with meaningful boundaries and nonzero counts.
- [ ] 2.2 Focused Unit RED → GREEN for manual choice preservation, same-side changes, crossings, effective policy changes and equivalent inheritance no-ops.
- [ ] 2.3 Focused Unit RED → GREEN for per-pawn policy history through map/caravan transfer, absence during policy edits, temporary ineligibility and first/missing records.
- [ ] 2.4 Implement native collection/eligibility and game lifecycle wiring without duplicate Thing caches or static cross-game pawn references; use honest native fixtures/process tests for global state.
- [ ] 2.5 Prove initial-before-play, 600-tick ordinary bound and paused committed-policy timing; no side effects from merely rendering the settings window.

## 3. Shared configuration, persistence and presentation

- [ ] 3.1 Add both validated definitions through common global/save load/store/resolve/UI paths; prove inheritance, dormant overrides, main-menu callback protection and save switching.
- [ ] 3.2 Persist native permission and versioned assessment history without upstream migration; prove unchanged reload, changed-global reload, invalid-history suspension and dormant-history recovery.
- [ ] 3.3 Implement translated policy explanations, source-provider status and first-assessment/manual-choice caveats in all seven baseline languages; retain native contextual Health controls with no new tab/toggle.
- [ ] 3.4 Verify original-active suppression precedes every initialization/write path, bounded diagnostics and native-manual fallback; verify permission/history are not changed by a skipped feature.

## 4. Focused checks and independent review

- [ ] 4.1 Measure multi-map/caravan assessment allocation and latency with representative populations, temporary absences and map teardown; no optimization claim without measured evidence.
- [ ] 4.2 Run affected focused tests, owning build/package checks and strict OpenSpec validation; preserve no source/Gateway DLLs, references or dependencies and do not claim old source supports 1.7.
- [ ] 4.3 Independently review the scoped implementation and resolve findings; rerun only affected checks after fixes and retain durable RED/GREEN and reviewed build identity.

## 5. Final reviewed-build player acceptance

- [ ] 5.1 In a fresh minimized isolated game, use native settings and pawn Health controls to observe threshold/default changes, manual preservation and paused policy changes; inspect action/before/after evidence personally.
- [ ] 5.2 Exercise real eligible self-treatment through native work/jobs and observe pawn work, supplies and Health result; verify incapable/unassigned Doctor and restricted-care cases do not gain bypasses.
- [ ] 5.3 Observe real recruitment, map/caravan transfer, saved manual exceptions, inherited/overridden save switching and main-menu disabled controls; test secure slave/child/Toddlers and original-provider cases in exact optional-mod runs.
- [ ] 5.4 Complete root AGENTS.md acceptance and semantic balance pass on the reviewed build: record exact actions, observed effects, screenshots/logs, package identity, ordered mods, user-config hashes and cleanup; only then update replacement coverage and task states.
