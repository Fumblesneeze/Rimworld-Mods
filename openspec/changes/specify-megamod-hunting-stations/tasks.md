# Hunting station tasks

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Original S064; documentation only. All implementation/verification tasks remain unchecked.
Use vertical focused RED → minimum GREEN → refactor, honest test tiers and retained nonzero evidence.

## 1. Resolve the implementation contract

- [ ] 1.1 Review HS-Q1 proposed seat balance, initial permissions, station/target choice, work allocation, departure/return and retry policy; update observable scenarios for every changed assumption.
- [ ] 1.2 Complete HS-Q2 pure potential-species descriptors across current1.7 multi-biome/coast/pollution/habitat/climate/migration/rare-arrival routes, with bounded unknown-provider coverage and no RNG/quest disclosure.
- [ ] 1.3 Pin adult-species grouping and exact native worker/weapon/ideology/approach/attack/death/transport/load contracts; retain initial-only range/aggression distinctions and current authorization guards.
- [ ] 1.4 Write stable per-patch target documents, minimal effective predicates, atomic safe-lifecycle groups, supported later repatch handling and warning/fallback scenarios before runtime hooks.

## 2. Station and contextual filter through vertical TDD

- [ ] 2.1 Define owned spot/seat/job/work data declaratively; verify revealed instant-spot placement, S028 seat rock/fog planning/privacy/reveal recovery, S063 deferred clearing, real construction/material/quality/minification and absent-safe dependencies without changing unrelated lists.
- [ ] 2.2 RED/GREEN map possible/observed species, absent seasonal/rare species, permanent map/DLC exclusions, variant deduplication, exact-zero retaliation defaults versus positive/unknown values, persistence of player choices, hidden-state privacy and unavailable/restored saved choices.
- [ ] 2.3 RED/GREEN individual/group/all/none/search behavior, adult-size boundary, regrouping without permission changes, new-species disallow and independent station persistence.
- [ ] 2.4 Implement contextual range/status/pause/filter and common radius/cutoff/retry settings with inheritance, dormant overrides, main-menu callback guards, input validation and seven-language text.
- [ ] 2.5 Follow the asset skill to create/select/verify the marker and raised seat, all required material/damage/cardinal states and seated/departing pawn alignment; retain independent visual review and live zoom evidence at acceptance.

## 3. Waiting and acquisition through vertical TDD

- [ ] 3.1 RED/GREEN ordinary Hunting worker/equipment/capacity rules, manual Hunt precedence, no station/empty filter, nearest reachable free station, actual arrival and one current assignment.
- [ ] 3.2 RED/GREEN bounded waiting and needs/priority/forced interruption, with no free XP/rest or abandoned claims; retain named60/250-tick responsiveness bounds.
- [ ] 3.3 RED/GREEN inclusive40-cell geometry, no station LOS, revealed behind-wall prey, fog exclusion, same-map scope, allowed/protected/owned/aggressive checks and legal departure/approach.
- [ ] 3.4 RED/GREEN stable nearest eligible prey and atomic competing station/native claims; revalidate at start and yield safely for blocked one-cell positions without a retry loop.
- [ ] 3.5 Connect native/common map facts with shared freshness and candidate fields; avoid per-station whole-map scans, stale permission caches or off-thread live object access.

## 4. Hunt, corpse and recovery through vertical TDD

- [ ] 4.1 Implement scoped station-only hunt intent without synthetic public Hunt designations or global ignore-designation bypass; RED/GREEN physical departure and preserved weapon/ammo/approach behavior.
- [ ] 4.2 RED/GREEN continuing range-independent pursuit, native timeout, retaliation, safe/dangerous downed completion and mid-action filter/faction/tame/fog/forbidden/access cancellation before new attacks/blows.
- [ ] 4.3 RED/GREEN actual corpse identity, ordinary yield/unforbid semantics, S021 capacity and S049 storage admission, no valid destination and single physical transport/recovery.
- [ ] 4.4 RED/GREEN work-aware physical return, no away-from-station target chaining, failed station/prey delay, zero-delay bounded retry and explicit manual Hunt independence.
- [ ] 4.5 RED/GREEN save/load phase/target/timer/claim reconciliation, pause/delete/move/minify/reinstall, map/game switch and missing/admission-failed work while preserving physical prey/cargo.
- [ ] 4.6 Compose S008 drafting takeover and S065/S066 admitted companion/wandering seams directly when those features exist; prove one finisher/carrier and interrupted handoff recovery without modifying internal ports through patches.

## 5. Admission, package and review

- [ ] 5.1 RED/GREEN compatible foreign changes, absent/changed targets, effective skipped originals, incoming-transpiler validation, repeated startup, exceptions and owned-group rollback with bounded warnings.
- [ ] 5.2 Verify exact optional wildlife/weapon/map adapters or their documented coverage/fallback; no unproven all-provider compatibility or source identity spoof.
- [ ] 5.3 Verify Harmony dependency at first eager use, XML Extensions at first actual XML patch, additive content, product/Gateway independence and positive package allowlist.
- [ ] 5.4 Run focused nonzero tests, owning build/package checks and typed OpenSpec validation, then independent scoped implementation review; resolve findings and rerun affected checks.

## 6. Reviewed-build native acceptance

- [ ] 6.1 In a fresh minimized isolated process, place/build/configure both forms through native UI, including S028 seat fog planning/reveal validation and S063 deferred clearing, and observe actual waiting only after legal completion, empty-filter behavior, species groups and possible-but-absent species.
- [ ] 6.2 Observe behind-wall/in-range detection, fog privacy, radius boundary, real departure and native shooting/retaliation/finishing through player actions; personally inspect before/action/after views.
- [ ] 6.3 Observe actual corpse carry/delivery or retained blocked corpse, return after ordinary reassessment, multiple hunters/stations, protected prey and manual Hunt/drafted-party precedence.
- [ ] 6.4 Exercise needs, commands, pause/filter/protection changes, one-cell blocked positions, timeout/retry, save/reload, station move/destruction and map switching; prove no duplicate kill/load or stale return.
- [ ] 6.5 Verify separately admitted provider-present and companion cases, settings inheritance/main-menu restrictions, all seven languages and required live asset/material/damage/zoom workflows.
- [ ] 6.6 Apply final balance review to finalized seat/material/quality comparators, work allocation, hunting risk/yield/capacity and measured combined index/query/update/memory cost; make no optimization claim from source inspection.
- [ ] 6.7 Satisfy root AGENTS.md final acceptance on the reviewed build: native actions and personal observations, exact build/mod list, retained causal captures, normal-config hashes and cleanup; update only tasks with complete required evidence.
