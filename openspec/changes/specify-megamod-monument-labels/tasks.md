# Implementation tasks — not performed by this specification draft

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
All tasks remain unchecked; MHL-Q1–3 retain research/implementation/native acceptance gates.

## 1. Native contracts and admission

- [ ] 1.1 Inspect generated native basic, timed-protection, decree and heir quests, including replacement markers and active/disabled/multiple delays; finalize exact association/role contracts and observable fallback scenarios.
- [ ] 1.2 Document and implement minimal executable target specs for every actual native/UI/optional hook, with focused RED/GREEN benign/changed/ambiguous/effective shapes, incoming streams if used, idempotence, atomic owned rollback and bounded per-patch warnings.
- [ ] 1.3 Verify Royalty/content absence and original-provider suppression, and classify runtime dependencies from the actual implementation without shipping source assemblies or atlas assets.

## 2. One observable behavior at a time

- [ ] 2.1 RED/GREEN deterministic marker/quest/objective projection, exact association, duplicate/multiple obligations and known-name/unknown-timing fallback in the appropriate honest test tier.
- [ ] 2.2 RED/GREEN active countdown projection and localized boundary text, with inactive/future/disabled delays, due/untimed cases, decree penalty versus forgotten deadlines and overdue/late-completion states, and differing Keep/Protect semantics; keep quest mutation outside the feature.
- [ ] 2.3 Implement map-owned native-list observation, paused first-render refresh, replacement/rotation/phase/quest cleanup and save/game/map lifecycle without a second Thing index or global permanent text cache.
- [ ] 2.4 Implement the monument producer using shared owned S093 rendering resources, measured text/rotated footprint bounds, glyph fallback, bounded placement/collision handling and viewport/fog checks.
- [ ] 2.5 Implement the small bottom-right toggle and common Display → Map labels → Monuments preference, including menu restrictions, inherited/dormant overrides, immediate paused response and seven-language text.
- [ ] 2.6 Verify owned resource eviction/disposal, game-thread access and changed-provider fallback; measure ordinary and many-monument workloads alongside floor labels before claiming optimization.

## 3. Focused checks and independent review

- [ ] 3.1 Run affected nonzero host tests, owning build, package/dependency checks and typed OpenSpec validation; retain focused RED/GREEN and exact build identity.
- [ ] 3.2 Independently review the scoped implementation and any selected/generated raster resources under the required asset process; resolve all findings and rerun affected checks.
- [ ] 3.3 Complete semantic/balance review against finalized native basic/protect/decree/heir quests: unchanged deadlines, construction, access, rewards, penalties and native information controls.

## 4. Final reviewed-build native acceptance

- [ ] 4.1 In a fresh minimized isolated Gateway-controlled game, personally observe real quest acceptance/marker placement, native construction completion and Build→Protect/Keep transitions, including decree deadline→overdue→late completion/Keep and forgotten expiry, with corresponding quest/inspect information.
- [ ] 4.2 Observe countdown boundaries, real quest/marker removal, replacement marker placement, rotation, map/world/shuttle view changes, paused load/save switching and bottom-right/common-settings actions with causal before/action/after views.
- [ ] 4.3 Inspect actual rendered text at materially different zooms, map/fog edges, adjacent monuments, long and seven-language names, partial viewports and coexisting floor labels; verify ordinary selection/orders still work.
- [ ] 4.4 Verify separate content-absent/original-present/changed-contract cases and retain exact build/mod list, personally inspected screenshots, actions, logs, normal configuration hashes and cleanup. Check tasks and replacement state only after the full root live gate; startup or static render tests alone do not accept the feature.
