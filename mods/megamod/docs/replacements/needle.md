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
| Hidden page/tab discovery | Generic bounded capture with truthful per-page gaps; owned metadata fast path | SS-05/06/08; automatic root-layout component verified; full tabs/provider reading and production registration pending |
| Query terms/phrases/negation/fields/typos | Source-inspired matching; mod/author/id/tab filters, bounded typo fallback | SS-02; detached query component implemented, integrated UI/index pending; no copied scores or Def classifier |
| Click opens page/tab and highlights option | Original control, current-layout scroll, three-second highlight; selection edits nothing | SS-03; specified, unimplemented |
| Mod options entry point | Shared native chooser query and distinct page/option results with S047 | SS-03/07; S047 sorting may be off |
| Displayed option values | Owned current scope/value; foreign last-observed text, normal owner persistence | SS-04; specified, unimplemented |
| Failures/refresh/lifecycle | Visible pending/partial/failed/unverified pages, ordinary access, explicit retry | SS-05/08; failed capture is not scope exclusion |

No foreign editor/store, source preference migration or dependency is introduced. Query/index/reveal
state is transient; the default-on enabled Boolean uses common global/save inheritance. Active
original Needle triggers the standard bounded warning and best-effort effective-off path while
retaining preferences; this is not a coexistence claim.

## Evidence and remaining work

The detached query, native recording/read-isolation components, composed single-callback reader
and completed native page catalog are implemented; scoped English
component workflows and their limits are recorded in the implementation ledger and tasks. The
listing-bounds component has a reviewed 22-step English workflow: offscreen label/help observation,
unchanged value/viewport, then native scrolling and editing of the original checkbox. The catalog
has a reviewed112-step English workflow using actual XML Extensions generated pages: equal titles
and packages retain independent opening/editing/persistence and stale descriptor invalidation.
Its twelve isolated diagnostic phases support bounded enumeration and publication; provider hidden
drawing remains pending. The automatic root-layout driver now has a reviewed112-step English
workflow with16 root-inspected captures: every actual page is attempted, cancellation retains
successful observations, refresh and constructor reentry preserve page identity, and an open
provider window remains unchanged. PUAH and both provider pages retain native editing/persistence.
Window-opening provider reads fail explicitly, leaving their normal opening routes usable. This
does not accept hidden provider option coverage. Full tab discovery, production activation,
the integrated search UI and original-control reveal remain unimplemented or unaccepted. [Proposal](../../../../openspec/changes/specify-megamod-settings-search/proposal.md),
[design](../../../../openspec/changes/specify-megamod-settings-search/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-settings-search/tasks.md) require exact
generic GUI research, focused RED/GREEN, independent review, real foreign/provider-native workflows
and personal observation. Test packages cannot become a supported-mod whitelist. All-mod eligibility
does not prove every custom control can be read; actual gaps must remain visible and unaccepted.
