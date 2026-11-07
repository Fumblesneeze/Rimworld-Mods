## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S116 references arl85's Show gender/age in caravan form dialog, selected 1.7 DLL1.2.0.0.
[Research](../../../mods/megamod/docs/refinement/2026-11-06-caravan-pawn-details-research.md)
records the exact public commit, installed hashes, complete small patch, current native paths and
TinyTweaks overlap. This is design only; no patch or compatibility is accepted.

The source postfixes `TransferableOneWay.Label`, not a caravan window. This also reaches splitting
and transporter loading, native label search, the Name comparer and existing row tooltips. Native
`Tradeable_Pawn.Label` already supplies the equivalent single-pawn information in two-way trade.
Native animal grouping admits a biological-age difference of at most one year within other
restrictions, so grouped members can have different completed ages. Source `AnyThing` describes
only the first member. Current native input: 1.7.5000rev1275; hashes identify research, not admission.

## Goals / Non-Goals

**Goals:** passive, localized and truthful pawn details in the actual one-way transfer row; preserved
transfer controls and native grouping; graceful provider/data fallback; reuse native presentation.

**Non-Goals:** new sorting/filtering language, pregnancy display, transfer or animal-selection
changes, pawn age/gender mutation, new tab/toggle/settings, source identity emulation, provider
code copying, migration, a separate pawn index or claims of tested 1.7 compatibility.

## Decisions

### One shared row description

Prefer one admitted postfix at the native one-way label getter, preserving its incoming result.
Build the summary from the row's available pawn members; retain the native base name, and append
the source/native species label only for an individually named nonhuman pawn. Use native localized
gender labels, including ordinary gender-none. Use completed biological years from the native
integer-age contract, not chronological years or floating rounding near a birthday.

For a row of pawns with a common gender, show a single completed age or minimum–maximum completed
ages. Add an explanation to the native row tooltip for grouped ranges: available members of this
row, not an age guarantee for whichever subset the native transfer later chooses. Preserve other
tooltip content. If required data is unavailable, the row includes non-pawns, or a provider groups
incompatible genders, retain the incoming label without invented aggregate facts. A source-selected
provider keeps its own presentation; the owned range correction is inactive in that case.

Alternative: append the first pawn's facts exactly as the source does. Rejected for grouped ages.
Alternative: split rows by age or choose different animals. Rejected: it changes transfer mechanics.
Do not use suffix-string parsing to detect ownership; names and translations can contain the same
text. Do not catch and claim to repair native getter failures before the postfix executes.

### Native UI, search and lifecycle

Keep `TransferableOneWayWidget` and `TransferableUIUtility` in charge of row layout, controls and
existing cache. The enhanced label feeds the native quick-search and Name comparer, preserving
their current query language and lexical ordering. Do not advertise a numeric age sorter. Preserve
icon, info-card and transfer count hit areas; the full summary remains readable in the row tooltip
when a narrow row clips its label. Keep native category/market-value/default choices unchanged.

Read current row facts when the label is rebuilt. Reopening the dialog or its ordinary native
cache refresh must make display, filtering and sorting use current descriptions. Do not promise
continuous hidden-row birthday resorting while a native cache is unchanged. Reuse the widget's
existing invalidation, adding a guarded native refresh only if implementation proves necessary.
No map-wide scan, per-feature pawn index, persisted derived summary or new background polling.
Measure large grouped rows before selecting any additional cache; share any justified derivation
with owned transfer presentation, keyed to row membership and relevant pawn facts, never per port.
S133 transporter and S135 caravan loading work must call the same owned presentation directly.

### Optional providers and consumed contracts

Select the original provider when `arl85.ShowGenderAgeCaravanFormDialog` is active; suppress only
the owned addition. Public TinyTweaks has package `Mlie.XNDTinyTweaks`, Harmony owner
`XeoNovaDan.TinyTweaks` and bool `TinyTweaksSettings.ShowGenderAgeCaravanFormDialog` (default true).
The inspected public version advertises through1.6 and is not installed here. With its overlap
enabled, suppress ours; with an admitted false flag, ours can operate. Unknown type/member/state
fails the affected owned addition closed. Respect subsequent overlap-setting changes before
rendering a refreshed description. Prefer a small admitted ownership check over patch churn.
Do not edit foreign settings, unpatch foreign owners or repair duplicates already made by two
foreign providers. Other name modifiers are allowed when their output and row contract are usable.

No callable consumer was found in the bounded source/assembly-name scan. RimPy's database stores
the package identity as catalog metadata, not an API binding. Do not spoof source package, assembly,
Harmony owner, static field or nested patch class. Later proven consumers need a narrow separately
specified adapter. Source requires Harmony; actual owned Harmony use must add the proper dependency.
No DLC, XML mutation or XML Extensions operation is selected. No runtime dependency on another
repository product, upstream DLL or Gateway. Native localized fields need no copied dictionaries;
owned tooltip/explanation text must support all seven baseline languages.

### Target admission

Before implementation, create actual stable-ID patch-target documents and link them to the spec,
validators and tests. Proposed atomic group: row description plus required full-summary tooltip.
Check the getter's callable shape, row-member/pawn data contracts and the result actually consumed.
A pure append postfix does not require an exact original IL body or proof the original ran when a
compatible skip-original prefix returns a valid label. Empty native rows are not ordinary valid
label-getter inputs; do not invoke the getter speculatively to test them.

Any tooltip/cache hook needs its own minimal contextual contract: the actual one-way row, tooltip
composition or refresh behavior used. If an argument is changed, validate its required effective
use; if a transpiler is eventually necessary, validate its actual incoming stream each time and
return it unchanged on failure. Do not execute foreign transpilers as probes. Hashes, exact game
versions and absence of foreign owners are not admission predicates.

Failure/unknown/exception/install failure leaves the affected group inactive and rolls back only
newly installed owned patches. Emit one bounded startup warning per skipped enabled patch with
package/patch/spec ID, target, failed condition, unavailable details and native/provider fallback.
Use idempotent startup and supported later invalidation/revalidation; never remove foreign patches.
Optional missing providers are not errors. No per-label/tick warning stream.

## Risks / Trade-offs

- Ranges can be mistaken for selected-subset promises → explicit tooltip and unchanged counts.
- Long names/translations can crowd controls → preserve native hit areas and full hover text.
- Existing native caches can delay current summaries → documented ordinary refresh/reopen boundary;
  test real query/sort/display consistency there, not synthetic getter calls alone.
- Optional TinyTweaks public shape may differ at implementation → exact package/shape guards,
  runtime-setting transitions and source-present tests; public inspection is not acceptance.
- Extra labels influence native search/name ordering → preserve that actual source effect and test
  it; no new category or numeric-age sorting advertised.
- Missing standalone license → independent behavior implementation; do not copy code or art.

## Migration Plan

No owned save/settings keys or migration. Labels derive from current native rows. Disabling/removing
the owned feature restores ordinary/provider presentation without changing pawns, groups, selected
quantities or saves. Install/deploy/runtime acceptance remain later tasks; none performed for draft.

## Open Questions

CPD-Q1–3 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain
effective admission/provider versions, bounded summary/layout/cache measurements and future focused
TDD plus reviewed-build native acceptance. These are research/verification tasks, not interviews.
