# Needle: all-mod option search reference

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
Inspected 2026-11-14. S141 is specification only; implementation remains paused.

## Sources and evidence

- [Needle Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3834934844), ZIPender,
  `ZIPender.Needle`; installed About reports 1.1.0 and 1.6/1.7 support, Harmony required.
- Public documentation lookup was attempted first; the web reader could not retrieve the exact
  Workshop page. The installed author's `About/About.xml` describes cross-mod option search,
  values/help/tabs/owner metadata and click-to-reveal/highlight. This retrieval limit says nothing
  about availability or compatibility.
- Read-only installed input: `F:/Steam/steamapps/workshop/content/294100/3834934844/`.
  Selected `1.7/Assemblies/Needle.dll` SHA256
  `7FBB6CE4E88EFB63A407E4277F96959761737D51BF3F94DAFB15BC95443D60F4`.
  Local decompilation: `artifacts/Refinement/2026-11-14-intake/needle-decompiled/`.
  No public source repository or license grant was found in the inspected package/search. Independently
  implement behavior; copy no source code, DLLs or assets. Hashes identify evidence, not runtime admission.

## Explicit scope correction

The earlier [2026-11-02 clarification](2026-11-01-handwritten-notes.md) covered owned setting names,
descriptions, feature names and categories. The initial S141 draft followed that narrower scope.
Asked whether to retain it or expand to foreign individual options, the user answered on 2026-11-14:

> I want it to search all mod options, not just ours or supported, just like needle does it

This **supersedes owned-only search and any supported-mod whitelist**. S047's active page-navigation
draft is reconciled in place: its page grouping/identity remains, while S141 extends the same native
chooser to individual options. A failed or custom page is a reported discovery gap, not an exclusion
from eligibility. The implementation pause remains in force.

## Observed implementation and selected adaptations

| Source surface | Observed behavior / boundary | Our decision |
| --- | --- | --- |
| `Indexer.ModsWithSettings` | Enumerates loaded mod handles with nonempty SettingsCategory, without package-specific search adapters | Enumerate all actual native/provider pages; preserve independent pages, not package/title deduplication |
| `StartupRead`, `Indexer` | Startup long-event read, queued work, per-page states and name-only fallback on GUI admission failure | Bounded automatic discovery and explicit refresh with visible progress/gaps; no ordinary-gameplay scan |
| `GhostDraw`, `GuiState` | Neutral hidden page draws; restores GUI/Event/debug state; closes capture-created windows; catches failures | Generic shape-guarded capture with guaranteed cleanup; no editor/reset/apply/write actions |
| `PageRead` | Follows observed tab callbacks and restores original path; depth/state/entry guards plus separate exploration and restoration draw thresholds | Bounded tab discovery; document measured budgets before code, report truncation instead of assuming complete coverage |
| `PageReader`, `PageRecorder` | Associates drawn text, tips, checkbox/slider/value geometry, occurrences and scroll contexts | Read displayed options without foreign settings-value reflection; keep last-observed provenance and explicit unknowns |
| `Query`, `SearchEngine`, `TextNorm` | AND terms, quotes, exclusions, mod/author/id/tab/def fields and weighted readable text | Keep terms/phrases/exclusions and mod/author/id/tab filters; label-first stable ranking, no copied scores or Def classifier |
| `Fuzzy`, `SearchOptions` | Default typo fallback; length 5–8 one edit, 9+ two; only absent exact matches qualify | Same bounded word-length policy, independently implemented; phrases/exclusions/fields remain literal |
| `NeedleSettings` | Tooltips, tabs, startup reading and typos default on; logging off; three-second highlight | Include help/tabs, default discovery and typo fallback, three-second highlight; no duplicate source preference inventory |
| `UI.Jump`, `ResultsView` | Opens the real page/tab, then resolves current observations for scroll/highlight | Reveal original foreign/owned controls; identity includes page/tab/occurrence, never cached coordinates alone |
| Native Mod options patches / optional ModMenuPlus adapter | Cross-mod option results in the chooser | Compose with S047 and preserve native access even when S047 sorting is off; optional adapters do not gate mod eligibility |

`PageRead` declares `MaxDraws = 108`, but that is not an enforced absolute cap: exploration checks
`draws >= 104`, while restoration navigation checks `draws >= 124`; callback steps can add draws.
Do not infer a safe total budget from the declared constant. Our budgets require measured contracts.

Source negation-only queries return no results; ours deliberately filters all entries for such a
query. Needle's `def:` classifier is not selected: S141 searches settings, not a general Def browser.
Do not copy its narrow character folding table as a universal Unicode contract.

The source restores GUI state, **not arbitrary foreign settings data**. Hidden drawing and tab
callbacks can have effects beyond captured GUI state; neutral input alone is not transactional
rollback. Our reader must report known mutation/restoration failures, bound further work and retain
normal access. A single callback cannot be safely forcibly interrupted on the Unity thread.
Real before/action/after observations are required for the chosen readers and normal edit lifecycle.

## Owned fast path and acceptance limits

Current `SettingsRenderer` registrations and `SettingDefinition` already provide owned labels/help,
stable ID, category, order, scope and application timing. Extend that path with search/reveal metadata
instead of drawing our own pages merely to recover known metadata. It is a fast path within universal
search, not a substitute for foreign discovery. Keep current owned values distinct from cached foreign
display text; retain normal foreign editing/persistence.

[The owning change](../../../../openspec/changes/specify-megamod-settings-search/proposal.md)
specifies query, generic capture, status, native reveal and lifecycle. Exact GUI/provider contracts,
budgets and representative real page scenarios remain implementation work. No runtime, performance,
provider compatibility or complete custom-control coverage claim follows from this inspection.
