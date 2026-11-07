# S137 — Condition-driven medical bill filtering

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: **original**. User addition on 2026-11-05. This is the historical refinement intake, not an
implemented feature. **2026-11-07 follow-up:** the sequential deep dive is now drafted in the
[owning change](../../../../openspec/changes/specify-megamod-condition-medical-bills/) and
[research record](2026-11-07-condition-medical-bills-research.md). HBF-Q2 explicitly records the
native ancestor-availability discrepancy and proposed extension before implementation.

## Confirmed intent

With the Health tab open, using the compact condition display and an already-visible medical bill
pane (the user cites Compact Hediffs and Mint Menus), clicking a condition should filter the bill
recipe choices to operations relevant to that condition. This is a contextual way to find the
appropriate medical operation; the condition is the input to the filter.

User examples:

- Click an infection on a foot: offer an available **amputate leg** operation when removing that
  ancestor body part also removes the affected foot. Matching must account for transitive body-part
  effects, not just operations naming the exact clicked part.
- Click a bionic replacement: show its appropriate **remove part** operation.
- Click porcupine quills: show the **remove quills** operation.

The user requests a general solution where possible, heuristics where general matching is
insufficient, and a deliberate way to add explicit owned mappings for sensible exceptions that
neither general rules nor heuristics capture. Do not substitute a handful of hardcoded examples for
that broader discovery requirement, and do not assume every operation mentioning a part is relevant.

## Research and specification work queued

1. Inspect current native medical recipes/workers and actual loaded content to determine how
   removal, restoration, implantation, cure and other relevant effects can be identified without
   executing an operation. Distinguish known effects from inferred ones; study body-part ancestry
   and the actual eligible operation target, including ancestor removal.
2. Inspect the exact Compact Hediffs grouping/row-click behavior and the referenced Mint Menus
   health/bill surface. Determine what an aggregate row represents and how selection can coexist
   with existing row actions. Mint Menus is presently a named UI reference for this feature;
   selection of its entire unrelated feature set is not inferred from this request.
3. Specify a stable precedence for evidence-backed general matching, bounded heuristics and
   explicit mappings, including exclusions/corrections. Identify how optional content can contribute
   mappings safely and how uncertain/unsupported operations are presented. Keep mechanisms in
   design; express visible matches, exclusions and explanations in the observable spec.
4. Specify selection indication, changing/clearing the filter, no-match behavior, grouped conditions,
   changing pawn/body/health state, and interaction with search and existing recipe availability.
   These details are open, not invented user decisions. Filtering must not imply that clicking a
   condition itself orders or performs surgery or bypasses the ordinary medical workflow.
5. Compose directly with S007's owned compact health display and the integrated medical bill view,
   preserving actual external contracts where needed. Avoid patches against our own implementation,
   duplicate health/recipe caches or a new bottom tab. Selection belongs to the active pawn/window;
   it is contextual gameplay/UI state, not a global or save configuration setting.
6. Create a separately owned OpenSpec change with proposal, observable scenarios, design and tasks.
   Retain exact example/provider provenance and native verification cases, including ancestor
   amputation, bionic removal and quill removal, before any implementation.

## Tracking

- Queue **S137**; **HBF-Q1** records the research/design questions above for later, without a user
  interview or a premature compatibility claim.
- [Sequential ledger](specification-progress.md)
- [Compact Hediffs coverage](../replacements/compact-hediffs.md)
- Existing S089 healer-serum chooser selects a condition for an item treatment. It does not replace
  this requested filtering of ordinary medical bill recipes; inspect any interaction rather than
  conflating their workflows.
