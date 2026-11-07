## 1. Initial scaffold and contracts

- [x] 1.1 Establish the single owner, 1.7 beta target, delivery boundary, and five capability specs, including source-mod composition, guarded merge-preserving XML, and UI placement/settings scope.
- [x] 1.2 Create the pinned inert product project, generated metadata, private release profile, baseline catalogs, and solution/README registration.
- [x] 1.3 Add mod-local AGENTS instructions, replacement/patch templates, coverage index, shared-data guidance, and honest verification roadmap.

## 2. Scaffold verification

- [x] 2.1 Validate OpenSpec with the typed operation, validate the release profile, and build Release.
- [ ] 2.2 Complete the canonical allowlisted package validation and assembly-independence gate. Currently blocked by the repository's 1.7 platform inventory drift on installed build 1.7.4995 rev309: missing declarations `RimWorldWaylith.PrepatcherCompat`, `Waylith.Injection`; no longer present `Ktx`, `gli`. Do not weaken the tooling gate in this mod-owned change.
- [x] 2.3 Inspect the seven exact empty catalog inventories/source-package parity and confirm the scaffold has no runtime entry points, patches, Defs, or claimed features.
- [x] 2.4 Complete independent scoped review, resolve findings, and perform the final semantic/playability pass; retain local evidence and document that no gameplay is accepted. Local record: `artifacts/ManualVerification/megamod-foundation-20261101/review-and-verification.md` and `checks.json`.

## 3. Subsequent runtime slices — deliberately not delivered by this scaffold

Execution is coordinated by the [parallel implementation tasks](../plan-megamod-parallel-implementation/tasks.md)
and [roadmap](../plan-megamod-parallel-implementation/roadmap.md). Implementers supply focused tests,
reviewed code and native scenarios; the orchestrator personally runs and observes final integrated
acceptance after all chunks in the part are ready. These foundation tasks remain obligations and
cannot be checked merely because the plan is written or a chunk is handed off. Recheck task 2.2's
historical tooling blocker against the current gate at implementation start.

- [ ] 3.1 Analyze the first chosen source set or original feature; resolve cross-source modifications into a composed specification and record external contracts, deviations, and co-installation policy.
- [ ] 3.2 Implement its first behavior with focused RED/GREEN evidence and the appropriate honest test tier; implement any required target-spec admission/warning mechanism with positive, negative, compatible-other-patch, and failure-isolation cases.
- [ ] 3.3 For XML work, implement XML Extensions package/content guards and merge-preserving edits; verify absent/changed targets, existing third-party entries, duplicates, and any explicitly intended list replacement.
- [ ] 3.4 When concrete consumers need common Thing data, implement one shared owner and native fallback with lifecycle/freshness tests and combined-consumer performance evidence.
- [ ] 3.5 For the first UI slice, audit upstream tabs and separate entity-specific gameplay state from configuration; implement classified entry points and a common settings load/store/resolve/renderer with global defaults, per-save overrides, Use global rows, documented exceptions, main-menu disabling, and active-save switching.
- [ ] 3.6 Independently review the runtime slice, rebuild, then personally exercise and observe its native player workflow in fresh minimized Gateway-backed 1.7 runs under the root AGENTS acceptance gate; include classified UI entry points, developer mode on/off, scoped settings/main-menu/save-switch cases where applicable and retain exact-build/mod-list/action/screenshot/log/config/cleanup evidence before marking feature coverage verified.
- [ ] 3.7 Implement and verify original-provider suppression before each conflicting feature's earliest activation phase, including declarative changes where applicable; cover loaded versus installed-only providers, bounded warnings, unrelated features and shared services, and preservation of original behavior. Initial support is new-colony single-player without CE; ordinary megamod save/load remains required.

Sections 1–2 define the requested initial deliverable; the canonical package gate remains pending
the separately owned tooling inventory repair. Section 3 remains open until separately
specified features exist; none of its runtime requirements can be accepted from scaffold checks.
