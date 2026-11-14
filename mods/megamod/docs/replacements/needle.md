# Needle — cross-mod settings search

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.

S141 reference: [Needle](https://steamcommunity.com/sharedfiles/filedetails/?id=3834934844),
ZIPender, `ZIPender.Needle`, installed 1.1.0. [Research](../refinement/2026-11-14-needle-research.md)
records read-only package inspection. The user's 2026-11-14 clarification selects individual
options across **all mods**, superseding the initial owned-only draft.

## Coverage

| Source idea | Selected result | Contract / status |
| --- | --- | --- |
| Cross-mod labels/help/values/tabs/owner search | Every available settings page eligible, no package whitelist or per-mod opt-in | SS-01/02; specified, unimplemented |
| Hidden page/tab discovery | Generic bounded capture with truthful per-page gaps; owned metadata fast path | SS-05/06/08; specified, unimplemented |
| Query terms/phrases/negation/fields/typos | Source-inspired matching; mod/author/id/tab filters, bounded typo fallback | SS-02; no copied scores; no Def classifier |
| Click opens page/tab and highlights option | Original control, current-layout scroll, three-second highlight; selection edits nothing | SS-03; specified, unimplemented |
| Mod options entry point | Shared native chooser query and distinct page/option results with S047 | SS-03/07; S047 sorting may be off |
| Displayed option values | Owned current scope/value; foreign last-observed text, normal owner persistence | SS-04; specified, unimplemented |
| Failures/refresh/lifecycle | Visible pending/partial/failed/unverified pages, ordinary access, explicit retry | SS-05/08; failed capture is not scope exclusion |

No foreign editor/store, source preference migration or dependency is introduced. Query/index/reveal
state is transient; the default-on enabled Boolean uses common global/save inheritance. Active
original Needle triggers the standard bounded warning and best-effort effective-off path while
retaining preferences; this is not a coexistence claim.

## Evidence and remaining work

Nothing implemented or accepted. [Proposal](../../../../openspec/changes/specify-megamod-settings-search/proposal.md),
[design](../../../../openspec/changes/specify-megamod-settings-search/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-settings-search/tasks.md) require exact
generic GUI research, focused RED/GREEN, independent review, real foreign/provider-native workflows
and personal observation. Test packages cannot become a supported-mod whitelist. All-mod eligibility
does not prove every custom control can be read; actual gaps must remain visible and unaccepted.
