# Implementation tasks — automatic baby names

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification only. All implementation and acceptance tasks remain open.

## 1. Resolve native boundaries and patch admission

- [ ] 1.1 Resolve exact generation/birth/adoption/load/manual naming seams for confirmed pending names, parent/xenotype timing, stillbirths, suppressed letters and effective provider behavior.
- [ ] 1.2 Establish a coherent pre-publication name/letter transaction with per-event snapshot, owned-only restoration, name-change detection and verified postconditions before enabling automation.
- [ ] 1.3 Publish stable per-patch target specs and test minimal effective admission, benign foreign patches, incoming transpiler checks where used, idempotent startup, atomic rollback, later repatch invalidation and per-skipped-patch warnings.
- [ ] 1.4 Implement Biotech availability, optional Ideology content/shape admission and original-source suppression with exact absent/present/changed-provider boundaries; do not spoof source identity.

## 2. Vertical naming behavior

- [ ] 2.1 Focused Unit RED → GREEN for pending/success/manual-change transitions, duplicate events, deliberate Baby, ambiguous legacy state and language-independent owned identity; reject zero-test runs.
- [ ] 2.2 Focused Unit RED → GREEN for ordered parental fallbacks, prepared surnames, parent-entry versus distinct-surname distributions, missing parent names and usable generated fallback.
- [ ] 2.3 Verify native full-name generation and actual parent/Ideology marriage-role behavior in the honest game tier, including same-gender pairs, differing ideologies, unknown precepts and unsupported name types.
- [ ] 2.4 Implement admitted successful assignment and safe generator/commit failures without altering birth outcomes, ownership, parentage, native deadlines or foreign births.
- [ ] 2.5 Implement one-time native-collection reconciliation across maps/containers/world/caravans/deceased babies with conservative legacy recovery, saved provenance, deduplication and teardown/discard cleanup.

## 3. Notifications and common settings

- [ ] 3.1 Implement successful birth/adoption paragraph and action handling without translated-caption identity or temporary pawn-name swapping; preserve foreign information/actions, target, history and failure fallback.
- [ ] 3.2 Implement no-required-prompt, Jump/Close/archive and reload behavior while preserving voluntary native rename windows, missing/dead targets and already named adoptees.
- [ ] 3.3 Add the common scoped surname row, accurate fallbacks, dormant inheritance, invalid-value behavior, requested/effective DLC fallback and main-menu mutation guards.
- [ ] 3.4 Supply and verify all seven baseline languages for settings, availability, fallbacks and name announcements; keep individual names on native contextual surfaces.
- [ ] 3.5 Verify save switching, partial/corrupt state, repeated initialization, manual names and feature disablement without source-config import or a separate pawn/Thing cache.

## 4. Focused review and package checks

- [ ] 4.1 Run affected focused tests, owning build/package checks and strict OpenSpec validation; preserve nonzero RED/GREEN evidence and keep source/private Harmony/Gateway assemblies out of the product.
- [ ] 4.2 Independently review the scoped implementation and resolve findings; rerun affected checks and retain the exact reviewed build/package identity.
- [ ] 4.3 Measure bounded initialization/event work and complete semantic playability review: unchanged birth/development/health, ordinary naming deadlines and correct DLC/provider availability.

## 5. Final reviewed-build native acceptance

- [ ] 5.1 In a fresh minimized isolated game, observe ordinary birth, native notice/Jump/Close/archive and voluntary rename through actual player workflows; personally inspect causal before/action/after evidence.
- [ ] 5.2 Exercise actual adoption, stillbirth and supported no-letter/growth-vat paths plus pending-name reload (including S031 toddlers and supported alien stages), intentional Baby and cross-language/save transitions; do not manufacture the claimed final name via setup code.
- [ ] 5.3 Verify DLC/source/name/letter-provider cases in exact separate active-mod runs, including effective guard failure and preserved native naming; report unsupported cases instead of claiming universal compatibility.
- [ ] 5.4 Complete root AGENTS.md acceptance on the reviewed build with player actions, observed names/notices, screenshots/logs, ordered mods, package identity, normal-config hashes and cleanup before closing gameplay tasks or replacement coverage.
