# Select Perishable

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
- Origin **replacement**, S114/P2-43. Source `Danoweb.SelectPerishable`, Danoweb,
  [Workshop3667956712](https://steamcommunity.com/sharedfiles/filedetails/?id=3667956712).
  Installed mod1.1 advertises1.6; DLL assembly/file1.0.0.0,9216bytes.
- SHA256 `61A2A3B2B409D14D1BFA5793D74310F7EE7C88C72309D37E533A4E9BC00C3F57`.
  [Research](../refinement/2026-11-06-select-perishable-research.md) records complete four-class
  inspection, native contracts, input hashes, public documentation and consumer limitations.
- No source/license located; independent behavior implementation planned, no source/DLL/art copy.

## Behavior inventory

All rows are **specified, not implemented or verified** in the
[owned contract](../../../../openspec/changes/specify-megamod-perishable-selection/specs/megamod-perishable-selection/spec.md).

| ID | Player action → result | Scope/defaults/edges | Evidence / requirement |
| --- | --- | --- | --- |
| PER01 | Open storage → one contextual bulk action | No unrelated filter-window leakage; existing controls retained | Source three patches/current ITab; PER01 |
| PER02 | Classify type → category and decay match |10 source category roots/descendants; positive deterioration OR native rot; no current-map/temperature test | IsPerishable/native metadata/Core comparators; PER02 |
| PER03 | Resolve storage → only applicable types | Intrinsic bounds/forced hidden; linked union; search does not limit; empty disabled | Intentional source correction; PER03 |
| PER04 | Mixed/all checkbox state → Select/Deselect set operation | Unrelated types, specials, ranges, priority and refill unchanged; no persistent mode | Source loop/native SetAllow; PER04 |
| PER05 | Edit linked policy → one shared result and preserved consequences | Physical limits, normal hauling, ordinary bill handling versus linked-row unresolved output/new-start blocking, save/copy/unlink | S005/S014/S049/S050/current native; PER05 |
| PER06 | Stale or failed action → no partial/wrong policy | Reopen stale context; supported rollback verified | Intentional safe mutation contract; PER06 |
| PER07 | Provider/shape drift → guarded fallback | Source overlap suppressed; seven languages; ordinary editing retained | Foundation/current targets; PER07 |

## Interactions and composed result

Compose directly with S005 linked storage, S014 linked workbenches, S049 refill and S050 storage presets. The helper edits
one current effective policy and leaves threshold/priority/range/special-state values intact. A
later preset or manual edit remains authoritative. Actual acceptance still respects physical
destination rules. No self-patches, duplicated map Thing index or independent per-member policy.
Ordinary unlinked bills retain native handling; linked rows retain an unsuitable output destination
as unresolved and stop new starts until resolved, without silently selecting another destination.
Existing native/shared definition facts are the classification input; no current inventory scan.

## UI audit and relocation

| Source entry point | Purpose | Owned placement / change |
| --- | --- | --- |
| Button in storage filter header; constructor increases tab size | Contextual bulk edit of storage checkboxes | Existing storage configuration, alongside presets; flexible layout and clear category/decay explanation |
| No settings, bottom tab, debug or view toggle | None | None added |

These are storage-specific gameplay choices, not mod configuration. No Use-global row, global
default or save override is introduced. Selecting all qualifying types does not loosen fresh/rotten
or other instance restrictions. Perishable includes deteriorating survival meals; medicine remains
outside the listed families. This is not a generic freezer preset or dynamic spoilage filter.

## Contracts consumed by mods or saves

| Evidence | Contract | Decision / verification |
| --- | --- | --- |
| Public IsPerishable/GetPerishableDefs; no external hit in bounded scan | Source assembly/type/signature | No facade/package spoof; revisit only for proven consumer |
| Native storage serialization | Resulting allowances and existing restrictions | Preserve native save/load/copy/unlink; no helper-state migration |
| Native/contextual filter callers | Owner/parent/filter, variant propagation, callbacks and bill/hauling consequences | Guarded shared mutation and actual native workflows required |

## Implementation and coverage

| Scope | Implemented | Deviations / conflicts | Missing / omitted |
| --- | --- | --- | --- |
| PER01–07 | Nothing | Intrinsic candidate bounds, empty-state correction, atomic scope-safe edits, no global flag leakage or per-frame full scan, seven languages | All code, target documents, tests and native verification |
| Classification scope | Nothing | Retains category/deterioration-or-rot contract; does not silently expand to medicine/all objects | Custom spoilage adapters only after actual inspection; refrigeration-only mode deliberately absent |
| Source co-installation | Nothing | Owned overlap disabled with warning, source untouched | Exact source-present acceptance |

Harmony is required with actual implementation use, never bundled. No XML mutation selected;
later XML Extensions use requires dependency and content/shape guards. No hard DLC/provider/other
product-mod/Gateway dependency. No source API compatibility or all-provider guarantee from static
inspection. Source preview is not an owned asset.

## Verification and acceptance

[Ledger](../refinement/specification-progress.md) tracks review/validation and PER-Q1–3. Future
acceptance requires native Select/Deselect actions and personally observed checkbox/policy results,
then actual hauling, ordinary bill feedback, linked-row unresolved-output/new-start blocking and
save/reload/copy/unlink in a fresh minimized isolated
reviewed-build game. Include restricted/linked storage, unchanged special/refill settings, empty
scope, stale/failure cases, source/provider-present runs and all seven languages. Retain causal
screenshots, exact build/mod order, config hashes and cleanup. No runtime task is accepted yet.
