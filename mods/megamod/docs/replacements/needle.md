# Needle — selected owned-settings search

## Identity and provenance

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S141, selected implementation reference: [Needle](https://steamcommunity.com/sharedfiles/filedetails/?id=3834934844),
ZIPender, `ZIPender.Needle`, installed 1.1.0. [Research](../refinement/2026-11-14-needle-research.md)
records the documentation retrieval limit, primary installed documentation, read-only decompilation,
hash and missing license grant. Independent implementation; no code/assembly/art copying.

## Behavior inventory and UI placement

| Source idea | Selected owned result | Contract / status |
| --- | --- | --- |
| Find an individual option by label/help/tab context | Search owned name, description, feature and category across sections | SS-01/02; specified, unimplemented |
| Results show a setting and its value | Distinct feature/category path and current common scope/value/reason | SS-04; specified, unimplemented |
| Click result to open/scroll/highlight | Reveal the existing real row without editing it; three-second highlight | SS-03; specified, unimplemented |
| Search from Mod options | One search within megamod's existing structured settings page, no bottom tab | SS-05/07; specified, unimplemented |
| Startup page/tab discovery | Use owned registration metadata; no hidden draws or tab callbacks to index | SS-06; intentional implementation difference |

## Interactions and persistence

S047 keeps its other-mod page/title/author search. This capability covers only megamod options,
following the existing user clarification; foreign individual settings, source fuzzy/negative/field
queries and Def lookup are not selected. Foundation C02/C10 own registration, values, scope and UI.
No second loader/store or cached gameplay permission is created. Queries/reveal state are transient;
only the default-on search-enabled preference uses common global/save inheritance and NextDisplay.

Active original Needle receives a bounded startup warning and best-effort ordinary owned-search-off
handling, preserving stored preferences. S047 and common editing remain independent. No source API,
save migration or coexistence compatibility is promised. Harmony remains the existing required
dependency; Needle and the Dev Gateway are not product dependencies.

## Implementation and verification

Nothing implemented or accepted. [Proposal](../../../../openspec/changes/specify-megamod-settings-search/proposal.md),
[design](../../../../openspec/changes/specify-megamod-settings-search/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-settings-search/tasks.md) retain exact native
input/layout research, focused RED/GREEN, independent review, seven-language scoped navigation and
root's fresh minimized native action/result gate. Documentation checks do not accept game behavior.
