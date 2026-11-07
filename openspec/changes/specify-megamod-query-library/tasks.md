# Implementation tasks

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Specification only. No implementation or acceptance task is complete.

## 1. Concrete admission and model boundaries

- [ ] 1.1 Revalidate exact game/source inputs and the127-entry mapping; resolve TDL-Q1 effective observations and distinguish direct native/owned calls from genuinely required hooks.
- [ ] 1.2 Resolve TDL-Q2 repeated-part applicability and stable content/category/thought-stage rules; add concrete positive, repeated-part, absent-species and ambiguous-content scenarios before implementation.
- [ ] 1.3 Resolve TDL-Q3 schema/version/size/depth budgets and durable recovery semantics; document player-visible limits before codec/persistence work.
- [ ] 1.4 Author individual semantic target specs for each actual patch/adapter and coherent failure group; include incoming/effective behavior, benign edits, later revalidation, rollback and bounded startup warnings. Add Harmony/XML Extensions dependency declarations only with actual use.

## 2. Ordinary criteria in vertical slices

- [ ] 2.1 Focused RED/GREEN for common inclusive/unbounded ranges, invalid/undefined values, wrapper/inner identity, unresolved versus disabled, negation and full-query failure; reuse S033/S018 rules.
- [ ] 2.2 Implement and test object/stat/quality/category/apparel masks/forbidden/mineable/door/HP/art/content-source and potential-recoverable criteria, with real displayed-stat applicability and no action side effects.
- [ ] 2.3 Implement and test plant/decay/temperature/deterioration criteria, units and native observations, including missing rot estimates and static product versus current harvest distinctions.
- [ ] 2.4 Implement and test area/zone/camera/faction/nearby/contained/reserved relationships; cover map edges, radius0, known-container traversal, cycles and stable map/entity bindings.
- [ ] 2.5 Implement and test designation/Can designate queries without issuing orders; prove None means no eligible supported action and querying does not mutate selection/game state.
- [ ] 2.6 Implement and test pawn property/skill/trait/thought/need/capacity/temperature/activity/work/quest criteria, including colony versus generic custody, hidden information, skill-above20, inclusive effect-ordered thought ranges with both persisted bounds and missing trackers.
- [ ] 2.7 Implement and test health severity/category/part occurrence/missing ancestor rules; include two unequal occurrences, hidden hediffs, ambiguous applicability and species lacking the part.
- [ ] 2.8 Implement and test gear/ability/social/schedule/guest/prisoner observations, nonempty All, unsupported active effects, blank schedule templates and room-location versus custody semantics.
- [ ] 2.9 Implement and test exact-policy and policy-item/category observations, addiction/dependency/joy/schedule/carry distinctions, stable identity and direct owned-policy composition.
- [ ] 2.10 Implement and test age/gender/time-record/race/animal-product/progress/training/bond criteria; distinguish theoretical output from current fullness, worker yield and hunting eligibility.

## 3. Expansion and optional observations

- [ ] 3.1 Implement and test Royalty criteria with exact faction/title choices, suspended obligations, missing titles, full permit copy/save fidelity and current observational food availability.
- [ ] 3.2 Implement and test Ideology criteria, known styles/precepts/roles, no-role/no-ideology cases, xenotype preference persistence and tree current/desired distinctions.
- [ ] 3.3 Implement and test Biotech mechanitor/mech/overseer queries and shortcuts, explicit class choice, nonempty All, current membership and feral-delay observations.
- [ ] 3.4 Implement and test xenotype/gene collections across pawn/embryo/pack/container/assembler, origin/activity/None truth tables, visibility, empty eligible holders, provider-specific unsupported meaning and reentrant evaluation.
- [ ] 3.5 Implement and test Odyssey observations, unique weapons, connected fuel/support/cooldown and undefined/missing denominators without changing gravship state.
- [ ] 3.6 Resolve TDL-Q4 against actual supported CAI/LTO providers; implement only admitted visibility/membership adapters, absent/disabled/missing-state behavior and stable groups. Keep RealFOW unsupported and FloatSubMenu presentation optional.

## 4. Library and external exchange

- [ ] 4.1 Implement complete independent definition/use/working/result boundaries; RED/GREEN every kind's copy/save/round-trip operands and S018/S033 complete cancellation/scope behavior.
- [ ] 4.2 Implement atomic create/edit/save/cancel/dirty-close, lock callbacks, rename/collision/unique copies, reorder/cross-group copy/move/delete and nested-cycle rejection with focused tests.
- [ ] 4.3 Implement versioned global template persistence, save-scoped binding placeholders, corrupt-input preservation, recoverable previous version, atomic commit/postcondition checks and restoration after failure.
- [ ] 4.4 Implement bounded inert owned clipboard/file codecs and complete query/group/library import preview; test all-or-nothing failures, unsupported stored data, focused text editing, unchanged numeric semantics and no raw-payload logging or Scribe interference.
- [ ] 4.5 Implement narrow actual-library provider/receiver adapters; test source tags, transfer shapes, CanReceive/lifetime races, independent clones, registration cleanup, exact conversion rejection and source-present policies with actual S018/S033 consumers.
- [ ] 4.6 Author34 ordinary and6 separately gated developer examples; verify complete criteria/scopes, deliberate binding, literal thresholds, current identities, explicit Add examples, collisions and no startup recreation/alerts/jobs.
- [ ] 4.7 Implement seven-language contextual/settings entry points and common Only available preference; test all list sizes, Shift mode, selected absent choice, main-menu callbacks, world view, dormant overrides and save switching.

## 5. Developer inspection

- [ ] 5.1 Resolve TDL-Q5 with a concrete proven non-mutating observation inventory and documented traversal limits; never invoke arbitrary getters to test their safety.
- [ ] 5.2 Implement separate top-toolbar debug workspace/collection and explicit private/hidden choices; test rejection by ordinary consumers and no god-mode leakage into production/search.
- [ ] 5.3 Implement admitted chains/comparisons with cycle/depth/visit bounds, unsupported-path retention and no authoritative partial result; cover imported definitions, null intermediates, finite collections and no mutation while paused.
- [ ] 5.4 Admit the optional HAR example only after exact package/observation/side-effect inspection and active-provider evidence; retain CE as deferred and unavailable throughout initial support.

## 6. Review and final evidence

- [ ] 6.1 Run focused honest-tier tests and affected build/package checks, reject zero-test runs, verify required/optional metadata and product/Gateway independence; retain exact evidence identities.
- [ ] 6.2 Exercise each actual target's positive/negative/ambiguous/effective foreign-patch cases, repeated initialization, install failure, owned-only rollback, warnings and later revalidation.
- [ ] 6.3 Obtain independent scoped code review, resolve findings and rerun affected checks. Measure shared query/search/hauling/production candidate/update/memory costs on the reviewed implementation.
- [ ] 6.4 Complete semantic playability review of disclosed knowledge, observation side effects, map/quantity scopes and actual bill consequences; sample thresholds do not grant work or change costs/yields.
- [ ] 6.5 Satisfy the root final acceptance gate on the reviewed build through minimized isolated native query/library/bill UI workflows: edit/copy/import/save/cancel, real changed game state and refresh, repeated parts, containers/maps, provider/DLC-present and absent cases, debug separation, settings/languages and reload. Personally inspect causal before/action/after evidence, retain ordered mods/build/actions/screenshots/logs, normal-config hashes and cleanup; only then update runtime coverage/tasks.
