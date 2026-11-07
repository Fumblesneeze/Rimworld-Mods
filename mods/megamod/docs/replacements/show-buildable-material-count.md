# Show Buildable Material Count

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; origin: replacement.
- S032, **Show Buildable Material Count**, `BP.ShowBuildableMaterialCount`, Workshop **3245101540**;
  About author BustedPretex / Workshop BustedPretext. Inspected 2026-11-03, selected installed 1.7
  DLL, advertised 1.5/1.6/1.7. The 1.6/1.7 DLLs are byte-identical; that is not live beta acceptance.
- [Research and exact hashes](../refinement/2026-11-03-buildable-material-count-research.md) record
  public documentation first, all eight behavioral/support types plus AssemblyInfo, relevant native
  1.7.4995 methods and bounded consumers. No source/license grant located; no copied code/DLL/art.
- [Owned change](../../../../openspec/changes/specify-megamod-buildable-material-counts/proposal.md)
  specifies future behavior only. **Nothing is implemented or accepted.**

## Behavior inventory

| ID | Input/action → observable result | Scope / edge cases | Evidence / owning contract | Status |
| --- | --- | --- | --- | --- |
| BC-01 | Open building material menu → stored/required for that actual choice | Original action/identity retained, physical units and additional same-material costs | Source Buildables; [capability](../../../../openspec/changes/specify-megamod-buildable-material-counts/specs/megamod-buildable-material-counts/spec.md) | Drafted |
| BC-02 | Open floor/construction dropdown → each row's own quantities | Same-resource floors with different prices, multiple ingredients, dominant cost versus first entry, utilities | Source Floors + exact native dropdown/cost helpers; capability | Drafted |
| BC-03 | Hover a native grid choice → equivalent quantity details | Preserve grid/eyedropper, long localized names and original actions | Native 1.7 dropdown; explicit owned extension in capability | Drafted |
| BC-04 | Haul/change stock → current open-menu counts | Stored-resource scope, loose/carry/fog/freshness/holder/minified distinction, no pawn eligibility promise | Native ResourceCounter; capability | Drafted |
| BC-05 | Search/haul/query stock → consistent current pile facts | Same shared map source, different filters; map/save lifecycle and safe fallback | User clarification and [design](../../../../openspec/changes/specify-megamod-buildable-material-counts/design.md) | Drafted |
| BC-06 | Encounter unsupported/co-installed provider → usable ordinary menu | Unknown is not zero, local row failures, no duplicate annotation, bounded warnings | Source hooks and intended corrected admission; capability | Drafted |

## Interactions and composed result

P1-25's arrow to Ctrl+F means shared index/cache ownership, **not** a click-to-search command.
Use the same map-owned Thing facts as S033 Ctrl+F and S021/S125 hauling; keep counted-storage scope
distinct from search and pawn eligibility. S017 bill counts can share material facts but retain
their own counting policies. No self-patching stack or per-source duplicate caches.
Selected Architect/menu/Floor Pipette work must retain native selection/placement and exact row
identity; no universal custom-menu support before its actual contract is inspected.

## UI audit

| Source entry point | Function | Owned placement / difference |
| --- | --- | --- |
| Building material FloatMenu | Text quantity prefix on each matching material | Same contextual menu, exact choice binding, full ingredients and clear stored-stock meaning |
| Terrain dropdown FloatMenu | Prefix based on matching resource icon | Exact terrain row, no same-material cost mixup; isolate utility/unsupported rows |
| No source grid hook | Native grid currently has no annotation path here | Owned extension through existing option tooltips; preserve grid/eyedropper |
| No source settings/debug/bottom tab | Startup-only annotation behavior | No new settings or tab justified; future configurable preference would use common settings |

No persistent operational toggle or entity policy is added. New explanatory text needs all seven
languages. Do not infer desired hiding/colors from third-party comments or add a new search action.

## External and saved contracts

| Consumer / evidence | Binding | Treatment / limit |
| --- | --- | --- |
| Use This Instead installed replacements.json | Recommends this package for old StuffCount | Metadata identity only; no source API facade or package impersonation |
| Original public BuildDict/buildElement/patch types | Public declarations, no runtime text consumer found in bounded audit | Preserve behavior independently; binary/reflected consumers remain BC-Q3 before any API promise |
| Custom menu or digital/virtual storage | Workshop comments and possible future provider boundaries | Optional inspected/admitted support only; no assumed compatible storage accounting |
| Saved games | Source has no observed Scribe/settings state; menu flags/dictionary are static transient | Owned menu/count state is not saved; no universal safe source-removal/migration claim |

Harmony is required when hooks ship; no DLC/source mod dependency. No XML is currently planned.
If original `BP.ShowBuildableMaterialCount` is active, owned overlap stays inactive with a warning;
never unpatch it or change the user's mod list. Compatible unrelated patches remain allowed.

## Deviations, omissions and coverage

Correct exact cost association, extra ingredients, volume conversion and difficulty/provider changes;
do not match by icon/text alone. Replace process-global menu flags and per-menu snapshots with
scoped associations and current shared data. Add native grid and resolvable nonterrain dropdown
coverage deliberately. Unknown/uncounted/unsupported data is explicit rather than fake zero.
No-cost/utility rows stay ordinary, and failures do not stop annotation of later supported entries.

Stored counts do not reserve material, subtract plans, promise access or alter construction. The
primary number is one ingredient, with the complete list in details. No whole-mod API/migration,
all-virtual-storage or all-third-party-menu compatibility claim. No optimization is accepted yet.
BC-Q1–4 track remaining effective-target/provider/freshness/measurement and native acceptance work.

## Verification and acceptance

All [implementation tasks](../../../../openspec/changes/specify-megamod-buildable-material-counts/tasks.md)
remain unchecked. Exact source/native reads establish the draft, not loaded behavior. Future proof
requires focused RED/GREEN, semantic guards, reviewed package, actual Architect selection and pawn
hauling/construction, personally inspected before/action/after views, supported-provider groups,
language layout and combined-consumer measurements under the root acceptance gate.
No runtime/product build, game launch, user-save/config or Workshop modification occurred here.
Independent source and contract reviews are complete; the source return/continue description was
corrected and rechecked. Strict typed OpenSpec validation passed **54 items, 0 failed**, and scoped
links/whitespace passed. This accepts the documentation checkpoint, not any gameplay task.
