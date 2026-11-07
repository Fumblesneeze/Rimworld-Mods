# Directional animal wandering tasks

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Original S066; specification only. All implementation/acceptance tasks remain unchecked.
Work through one observable slice at a time with focused RED → minimum GREEN → refactor,
nonzero evidence in honest environments, independent review and reviewed-build native acceptance.

## 1. Resolve policy and native integration

- [ ] 1.1 Review DW-Q1 proposed context precedence, bounded-room/physical-pen classification, conservative doorway rule and tuning limits; update observable scenarios for changed decisions.
- [ ] 1.2 Complete exact effective native wander/wait/job/follower contracts, current-room/pen/door invalidation and S065 ordinary-follow fallback; do not infer admission from a class name or old source hash.
- [ ] 1.3 Create stable per-patch target documents covering destination/job/request/next-step/wait/reuse lifecycle as an atomic route, compatible effective changes and safe explained fallback.

## 2. Local steering through vertical TDD

- [ ] 2.1 RED/GREEN adjacent direction-preserving walks, obstacle/circle turns, normal variation and no repeated cell within one walk; keep heading state separate from shared spatial facts.
- [ ] 2.2 RED/GREEN one/two-step requested and actual results, no-move single-cell traps, narrow passages, map edges and bounded candidate probes without minimum-length retries.
- [ ] 2.3 RED/GREEN native physical edge/corner/access/collision/reservation rules and conservative idle doorway/fence policy; prove purposeful routes remain independent.
- [ ] 2.4 RED/GREEN current danger/sunlight/pollution/rot-stink predicates, effective susceptibility and S024 destination/sheltered-cost composition without suppressing S030 or native shelter escape.

## 3. Context and priority slices

- [ ] 3.1 Integrate admitted ordinary wildlife/herd providers with native reference/radius and outside-radius regrouping; prove no new herd membership or per-animal whole-map candidate cache.
- [ ] 3.2 Integrate S065 hunter-root idle movement, moving-root invalidation, shared distance, purposeful regroup/assistance, needs suspension and supported ordinary style when S066 is disabled.
- [ ] 3.3 RED/GREEN physical enclosed-pen membership, multiple markers/connected rooms, non-roamer occupancy, changed acceptance filters and current marker/topology/door state.
- [ ] 3.4 RED/GREEN bounded non-doorway room recognition, unroofed courtyard versus roofed open terrain, wild animals in rooms, tiny spaces and room split/merge/breach behavior.
- [ ] 3.5 Preserve native provider urgency/cadence/expiry/reports, pen marker approach and roofed shelter; prove ropes/Lords/mental states/flight/swimming/water-seeking and unconfined tame colony behavior retain their intended provider.
- [ ] 3.6 Exercise actual priority selection for food, rest, hazards, S043/S087 grazing, S021 cargo, player orders and other purpose-driven work without idle recapture or changed resource outcomes.

## 4. Native job, path and lifecycle slices

- [ ] 4.1 RED/GREEN actual decision→job association, Wait/null results, pooled job reuse, exact request identity, start/progress/endpoint and unknown customizer rejection.
- [ ] 4.2 Implement physical local path delivery through native reservation/toil/follower handling; verify truthful costs, exactly-once resolution and guaranteed path/native allocation cleanup on errors.
- [ ] 4.3 RED/GREEN changes before the next affected step: geometry, hunter, enclosure, permission, danger and collision; stop stale paths and forbid global repair to obsolete idle endpoints.
- [ ] 4.4 RED/GREEN native arrival/failure/reselection including an already-running GotoWander; prove one bounded reassessment then ordinary wait, with urgent work still able to interrupt.
- [ ] 4.5 RED/GREEN save/load without transient plans, disable/enable, death/despawn, context/faction/map changes and game teardown; preserve actual position and valid purposeful jobs.

## 5. Shared settings, compatibility and verification preparation

- [ ] 5.1 Implement the common availability/range definitions and atomic validation, live inheritance/dormant overrides, next-walk application, save switching and main-menu mutation guards.
- [ ] 5.2 Implement the one bottom-right operational button as a current-save availability override, contextual unavailable reasons and seven-language settings/tooltips; no second toggle state or new tab.
- [ ] 5.3 Use native/shared pen/room/candidate data; retain minimal transient animal/job state and measure topology refresh, planning, revalidation and memory costs with actual combined consumers.
- [ ] 5.4 RED/GREEN effective target drift, skip-original/incoming changes, benign foreign patches, validator/install exceptions, idempotence/repatch and owned-only atomic rollback/warning contents.
- [ ] 5.5 Inspect concrete optional movement/terrain/door/map providers before adapters; test supported cooperation or affected-route decline without modifying another mod's mode. Keep XML changes additive and declare actual runtime dependencies at first use.
- [ ] 5.6 Run focused nonzero tests, owning build/package checks and typed OpenSpec validation; obtain independent implementation review, resolve findings and rerun only affected checks.

## 6. Final native acceptance on the reviewed build

- [ ] 6.1 In a fresh minimized isolated game, observe normal wildlife direction/turns/herd regroup and real feeding/fleeing; retain before/action/after views from that exact build.
- [ ] 6.2 Use native pen/room construction and animal controls to observe 2–3-step and shorter walks, single-cell/two-cell blockage, unroofed enclosure, filter changes, connected barn/flap and real food/shelter routes.
- [ ] 6.3 Through actual hunting/station orders, observe S065 companions use the hunter root, move when the hunter moves, yield to needs and perform real prey/corpse work without local-length restrictions.
- [ ] 6.4 Change walls/gates/areas/conditions during actual movement and observe timely stop/reassessment, ordinary waits and higher-priority interruption without clipping, loops or forced relocation.
- [ ] 6.5 Exercise save/load, map/context changes, in-play toggle, inherited/overridden settings, main-menu disabled callbacks and all baseline languages through native UI.
- [ ] 6.6 Verify exact admitted provider-present cases and measure combined work/memory/real job progress at representative animal populations; compare equivalent scenarios without claiming old Pathfinding Lab evidence or summed timings prove a whole-game speedup.
- [ ] 6.7 Complete final balance/playability review of effective move timing, waiting, hazards, confinement, access and purposeful job reachability; update proposed values if observed behavior requires it.
- [ ] 6.8 Satisfy root AGENTS.md final acceptance: reviewed build/package identity, exact ordered mods, real player actions, personal observations/causal captures, normal-config hashes and cleanup; check only tasks whose required evidence exists.
