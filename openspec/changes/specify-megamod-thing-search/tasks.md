## Owner and delivery state

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target **1.7 beta**.
Specification only. Every implementation and acceptance task remains unchecked. Artifact completeness
does not establish source parity, drop-in compatibility, performance or verified runtime behavior.

## 1. Resolve exact implementation boundaries

- [ ] 1.1 Resolve CF-Q1 exact native input/window/control/selection targets and document minimal semantic admission for every hook, effective foreign patches, startup warning/fallback and later repatch handling.
- [ ] 1.2 Resolve CF-Q2 shared native candidate/holder/knownness and invalidation contracts with S032/S021/S017/S018; document distinct consumer filters, keys, freshness, lifecycle and native fallback.
- [ ] 1.3 Complete the S121 library/catalogue contracts required by workspace save/open/exchange before implementing those dependent actions; enumerate supported query representations and exact binding rules.
- [ ] 1.4 Audit CF-Q3 actual external consumers and current provider types/codecs; document admitted mappings, no-loss import/export, package identities and source co-installation without spoofing.

## 2. Focused vertical TDD and implementation

- [ ] 2.1 RED/GREEN basic name/type/stack-range/material/zone and wrapped-object matching, including exclusion, group thresholds, deliberate disable versus unresolved clauses and duplicate physical results in the honest host tier.
- [ ] 2.2 RED/GREEN map scope/binding, removed-map/zone handling, failed-map atomic publication, valid zero versus unavailable and stable result identity/order; implement the minimum owned evaluator with S018 composition.
- [ ] 2.3 RED/GREEN the first shared candidate lifecycle/invalidation slice, paused/same-tick refresh, split/merge/holder transfer and teardown; implement without per-port full-map caches or stale eligibility decisions.
- [ ] 2.4 RED/GREEN workspace new/resume/find-like-selection intent and lifecycle, safe unsupported selection, exact material alternatives and current-game binding; implement native entry paths and bottom-right control.
- [ ] 2.5 RED/GREEN editor lock/import/clear/focus intent and the explicit New search exception; implement listing/scope/basic criterion/group defaults/controls and ordinary focused text clipboard behavior.
- [ ] 2.6 RED/GREEN stable result/snapshot states and action-time stale-target decisions; implement native highlight, left/Shift/drag/double/select-all/right navigation and Enter-to-first-current-result behavior.
- [ ] 2.7 RED/GREEN connected/detached windows, continued refresh after separated-editor close, close/reopen/replacement ownership and 60-tick nonpersistent refresh; implement cleanup without orphan registrations or cross-save references.
- [ ] 2.8 RED/GREEN common Show search control inheritance, dormant overrides, main-menu write rejection and next-redraw timing; integrate native global keybinding configuration and scope explanation.
- [ ] 2.9 After 1.3, RED/GREEN owned library working copies and supported bill/search exchange, exact-map retargeting, whole-transfer rejection and unchanged prior state on failure; implement direct owned composition.
- [ ] 2.10 After 1.4, RED/GREEN the narrow optional provider adapter's absence/shape drift, complete supported round trips and duplicate-receiver prevention; implement without eager foreign references.
- [ ] 2.11 RED/GREEN patch admission and atomic rollback/warning paths with compatible foreign owners, ambiguous targets, missing invalidation and correct native fallback in the honest Harmony/native tiers.

## 3. Scoped verification and review

- [ ] 3.1 Translate player-facing controls, statuses, scope labels, tooltips and warnings into all seven required languages; verify long labels, small windows, UI scaling and focused text input.
- [ ] 3.2 Run affected nonzero focused tests, owning build and package/dependency checks through typed operations; verify no source DLL/art, package spoof or Dev Gateway dependency is shipped.
- [ ] 3.3 Benchmark combined search/hauling/construction/production queries with native baselines and real churn, including update cost, allocations and memory; record measured budgets before optimization claims.
- [ ] 3.4 Independently review the runtime diff with the repository code-review skill, resolve findings with evidence and rerun only affected checks.
- [ ] 3.5 Complete semantic playability review: ordinary knowledge, native selection/capacity/job restrictions, honest stock-versus-search quantities and provider/dependency availability remain intact.

## 4. Final reviewed-build native acceptance

- [ ] 4.1 In a fresh minimized isolated game, perform actual shortcuts, typed edits, Enter, Shift/drag/double/select-all/right navigation, holder/off-map/stale-result and paired/detached workflows; personally inspect before/action/after views and retain build/input/result evidence.
- [ ] 4.2 Verify paused edit/manual refresh, continuous refresh timing, replacement/close cleanup, map removal and actual save/game switching; verify common visibility settings and native keybinding scope from main menu, map and loaded-game world view.
- [ ] 4.3 Run exact absent/present source/provider cases, admitted/changed optional adapters and unsupported imports; exercise native bill-to-search and search/count/hauling workflows without direct mutations manufacturing the expected result.
- [ ] 4.4 Apply the root acceptance gate: retain reviewed build/package identity, ordered mods, exact actions, personally inspected screenshots, supporting logs/config hashes and cleanup; update replacement coverage only for behavior actually verified on that build.
