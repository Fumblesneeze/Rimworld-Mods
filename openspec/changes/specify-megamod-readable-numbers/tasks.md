## Owner and delivery state

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target **1.7 beta**.
Specification only. All implementation/acceptance tasks remain unchecked; source inspection and
artifact completeness do not establish performance, universal UI support or live compatibility.

## 1. Resolve implementation contracts

- [ ] 1.1 Resolve RN-Q1 current native quantity/measurement/draw/input/name/date/history/quest targets and effective foreign-patch interactions; record stable minimal semantic target specs and atomic group/fallback policy for every planned hook.
- [ ] 1.2 Resolve RN-Q2 deliberately used optional HUD/readout/date/profiler contracts; define admitted package/type/representation shapes and absent/changed behavior without eager references or package spoofing. Exclude original Readable Numbers API/consumer/facade research and coexistence work.
- [ ] 1.3 Specify concrete source-representation/context provenance, structured styled quantities and generic ambiguity boundaries; establish the shared formatter contract with owned S127/S033/S032/production displays.

## 2. Vertical TDD and implementation

- [ ] 2.1 RED/GREEN digit-preserving grouping, punctuation, sign/unit/precision handling, per-number thresholds and small-decimal rules with mixed-label and None-mode cases; implement the minimum pure formatter.
- [ ] 2.2 RED/GREEN literal overlapping exclusions, exact punctuation/backslash/bar persistence, duplicate/empty rejection and whole-list inheritance; implement through common configuration.
- [ ] 2.3 RED/GREEN atomic composite punctuation inheritance/validation, dormant custom values across presets/reload, global/save switching, disabled main-menu writes, global default reset, current-save Use global for all and next-redraw application; implement common settings and live previews.
- [ ] 2.4 RED/GREEN identifier/input/date/debug/markup protection, same-string/different-context ambiguity, repeated-format stability, malformed/long input and expansion failure; implement safe context-aware formatting without changing source data.
- [ ] 2.5 RED/GREEN scoped nested/exception restoration and coherent hook admission/rollback with benign foreign changes and unsafe label-use changes; implement owned native hooks without unconditional PatchAll or an eagerly loaded incompatible backend wrapper.
- [ ] 2.6 RED/GREEN current representation/width invalidation across setting, exclusion, language and game changes; implement one bounded text-result cache only if justified, independent of Thing candidate storage.
- [ ] 2.7 Integrate native quest reward translation/measurement, map quantity labels and ordinary supported UI through the same formatter; retain full-value access and native actions/selection/caret behavior.
- [ ] 2.8 RED/GREEN external RimHUD precision/styles, Enable/exclusion/small-decimal consistency and owned direct composition; implement the admitted adapter without rewriting all periods in markup.
- [ ] 2.9 RED/GREEN admitted Toggleable Readouts refresh/raw-value preservation, Dawn date exclusion and profiler context protection with missing/changed providers; implement only inspected optional contracts.
- [ ] 2.10 RED/GREEN active-source startup warnings and best-effort ordinary formatter-off behavior, inactive-source handling and retained preferences, plus safely isolated generic-path failure and other owned actions; no source coexistence guarantee. Implement developer-only diagnostics without routine log spam.

## 3. Scoped review and checks

- [ ] 3.1 Translate settings, examples, errors and warnings into all seven required languages; verify long text, punctuation glyphs, UI scale and rich-text preservation through actual supported displays.
- [ ] 3.2 Run focused nonzero honest-tier tests, owning build and package checks through typed operations; verify required Harmony metadata and no copied source DLL/art, foreign identity or Dev Gateway dependency.
- [ ] 3.3 Measure combined text draw/measure/update cost, allocations and bounded cache memory with real mixed labels/settings changes and owned consumers; document measured budgets before optimization claims.
- [ ] 3.4 Independently review the implementation with the code-review skill, resolve findings with evidence and rerun affected checks.
- [ ] 3.5 Complete semantic playability review: quantities, precision, input parsing, names, dates, costs/bills, selection and provider actions remain unchanged apart from specified display punctuation.

## 4. Final reviewed-build acceptance

- [ ] 4.1 In a fresh minimized isolated game, open actual resource/stat/quest/map displays and settings, change formats/thresholds/exclusions and inspect before/action/after text and layout; include mixed-length values, styled decimals, long labels and full-value access.
- [ ] 4.2 Exercise native numeric/name input, selection/copy, history/date/file metadata, resets, inherited/overridden settings and real save/main-menu switching; personally verify unchanged input/data and correct effective presentation.
- [ ] 4.3 Run exact deliberately used provider absent/present/shape-drift cases and owned HUD/search/count/bill composition; observe native user workflows rather than directly constructing formatted expected results. Limit original-source coverage to warning/ordinary-off behavior without a coexistence compatibility matrix.
- [ ] 4.4 Apply the root acceptance gate on the reviewed build: retain build/package identity, ordered mods, actions, personally inspected screenshots, supporting logs/config hashes and cleanup; update coverage only for actually verified behavior.
