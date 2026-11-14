# Needle: owned settings search reference

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
Inspected 2026-11-14. S141 is a selected implementation reference, not a promise of full source parity.
No runtime code, build, installation or game run is part of this documentation task.

## Sources and evidence

- [Needle Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3834934844), ZIPender,
  `ZIPender.Needle`; installed About reports 1.1.0 and 1.6/1.7 support, Harmony required.
- Public documentation lookup was attempted first; the web reader could not retrieve the exact
  Workshop page. The installed author's `About/About.xml` supplies the primary description: searches
  individual settings, values, help, tabs and owner metadata; click reveals/highlights a real option.
  This retrieval limit is not evidence that the mod is unavailable or incompatible.
- Read-only installed input: `F:/Steam/steamapps/workshop/content/294100/3834934844/`.
  Selected `1.7/Assemblies/Needle.dll` SHA256
  `7FBB6CE4E88EFB63A407E4277F96959761737D51BF3F94DAFB15BC95443D60F4`.
  Local decompilation is retained under `artifacts/Refinement/2026-11-14-intake/needle-decompiled/`.
  No public source repository or license grant was found in the inspected package/search; do not
  copy implementation, assemblies or assets. Hashes are evidence identity, not runtime checks.

## Observed implementation and selected adaptations

| Source surface | Observed behavior / boundary | Our decision |
| --- | --- | --- |
| `Search.Query`, `SearchEngine`, `TextNorm` | Terms/quotes/negation, field filters, weighted names/help/values/owner text, normalized readable strings | Keep terms/phrases and readable localized matching; search owned name/help/feature/category, deterministic name-first ranking |
| `Fuzzy`, `SearchOptions` | Default typo fallback; terms of 5–8 allow one edit, 9+ two; only absent exact matches qualify | Not selected for initial owned-search scope; no copied score or distance implementation |
| `NeedleSettings` | Tooltips, tab reading, startup reading and typos default on; logging off; highlight defaults three seconds | Descriptions always searched; three-second result highlight; no crawler/logging preference inventory |
| `StartupRead`, `Indexer`, `PageRead`, `GhostDraw` | Startup long event reads pages; hidden draws and tab callbacks discover controls, restoring GUI/tab state | Direct owned registration metadata; no synthetic foreign/owned UI replay for indexing |
| `UI.Jump`, `ResultsView` | Result opens page/tab, uses current observed layout and highlights a matched option | Reveal actual common row by stable setting identity, using current layout; no editor invocation |
| Native Mods options patches / optional ModMenuPlus adapter | Extends chooser with cross-mod option results | Keep S047 chooser independent; foreign individual-option search is not included in this draft |

This is focused inspection of the selected behavior and its key implementation alternatives, not
a complete compatibility audit of all source adapters. The decompiled GUI restoration cannot by
itself prove arbitrary third-party callbacks are free of side effects. Owned metadata avoids that
question entirely. No original-provider API/consumer audit or coexistence matrix is required.

## Existing decisions and current code

The [2026-11-02 clarification](2026-11-01-handwritten-notes.md) explicitly requests setting names,
descriptions, feature and category names. S047's MO-04 searches page/title/author/group and excludes
opaque foreign controls; that is a distinct capability. The current `SettingsRenderer` already owns
all row registrations, localized labels/help and drawing. `SettingDefinition` supplies stable ID,
category, order, scope and application timing. It lacks a search projection/reveal contract.
Retain this metadata in the existing registration path; avoid a second settings truth or index built
by calling draw delegates. Dynamic/composite rows need explicit identities and current status.

The 2026-11-14 scope question distinguishes owned options from Needle's broader foreign-options
search. This draft follows the already agreed owned-only scope unless the user explicitly expands it.
Needle's discovery implementation is an evaluated alternative, not a runtime dependency.

## Follow-through

[Owned change](../../../../openspec/changes/specify-megamod-settings-search/proposal.md) specifies
matching, current results, safe reveal, common scope, languages, lifecycle and failure behavior.
Implementation tasks remain unchecked; exact native text/reveal boundaries and focused UI acceptance
must be supplied later. No timing/performance or native compatibility claim follows from this research.
