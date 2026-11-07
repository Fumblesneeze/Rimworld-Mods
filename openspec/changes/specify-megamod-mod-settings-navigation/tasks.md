# Implementation tasks — mod settings navigation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification only; all implementation and acceptance tasks remain open.

## 1. Establish native and provider contracts

- [ ] 1.1 Publish stable per-patch target documents for completed settings-entry discovery, native chooser/open/write behavior, effective foreign interactions and coherent fallback.
- [ ] 1.2 Inspect actual beta HugsLib/fork selection and proxy/opening/injection contracts; define package-ID/shape-guarded optional adapters and prove preservation of existing provider access on failure.
- [ ] 1.3 Record exact title/author/group and duplicate-page identities, late injection/language refresh and original-sorter suppression without package spoof or speculative API facade.

## 2. Vertical chooser behavior

- [ ] 2.1 Focused RED → minimum GREEN for author/name sorting, source group precedence, singleton counts before search, stable duplicate ties and missing author handling; retain nonzero evidence.
- [ ] 2.2 Focused RED → GREEN for raw/normalized author, title/mod/group substring search, empty/no-match results, singleton membership stability and distinct provider packs.
- [ ] 2.3 Implement the smallest admitted native chooser integration with exact page identities, native/provider-owned open paths and no foreign settings-value reflection or mutation during browsing.
- [ ] 2.4 Test benign foreign additions, unsupported required semantics, failed install, repeated initialization, owned-only rollback and later revalidation; validate incoming streams on every invocation if transpiling.
- [ ] 2.5 Test native/provider click routing and errors, absent/changed HugsLib, working original-sorter access versus unsupported pre-existing source failures, GUI-state recovery and bounded diagnostics.

## 3. State, UI and lifecycle

- [ ] 3.1 Implement transient author/group defaults and process-session retention, fresh-query reset, immediate reordering and no global/save/foreign-settings writes from browsing.
- [ ] 3.2 Implement all seven languages, readable native-like cards/reflow, missing icons, full identity details and empty/unavailable states; keep controls in the existing settings chooser.
- [ ] 3.3 Verify one megamod entry opens the structured common settings page and retains its global/save/main-menu guards, without recreating per-source fake settings pages.
- [ ] 3.4 Verify provider-completed list refresh, language changes, query/scroll recovery, duplicate titles and dialog teardown without stale static Mod references or per-frame rediscovery.
- [ ] 3.5 Preserve native/provider edit-and-close persistence and verify no bulk reset/import/default probing; keep advertised-only highlighting explicitly unimplemented unless separately specified.

## 4. Review, package and performance

- [ ] 4.1 Run affected focused honest-tier tests and owning build/package checks, declaring Harmony only when implemented and keeping HugsLib optional with no bundled source/framework/Gateway assemblies.
- [ ] 4.2 Independently review the implementation, resolve findings and rerun affected checks with durable RED/GREEN and the exact reviewed build identity.
- [ ] 4.3 Measure representative large settings-list open/search/refresh cost; use native collections and dialog-local projections without a game Thing index or per-frame provider reflection.
- [ ] 4.4 Complete semantic playability review for unchanged gameplay settings, preserved provider access, required/optional metadata and any explicitly omitted coverage.

## 5. Final reviewed-build native acceptance

- [ ] 5.1 In a fresh minimized isolated game exercise native Options → Mods → search/sort/group → choose/edit/close and inspect causal results, including main-menu and loaded-save/world views.
- [ ] 5.2 Exercise native/provider duplicate pages, source aliases, missing metadata/icons, long translated labels/scales, reopen/restart, exact HugsLib-present/absent and original/changed-provider fallbacks.
- [ ] 5.3 Complete root AGENTS.md acceptance on the reviewed build with personally inspected action/result evidence, exact ordered mods/package, supporting logs, normal-configuration protection and cleanup before closing gameplay tasks.
