## Why

Workers should choose sensible targets within their assigned work: patients needing care, animals
needing training, productive growing locations and useful construction. SmarterDoctors covers all
of these, but its medical arithmetic and undocumented rescue change require explicit decisions.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S058/P1-52, SmarterDoctors (Continued), `zal.smarterdoctors`, Workshop3744377058.
**Specification only; no gameplay delivered.**
[Research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-doctors-research.md) records exact
inputs, source discrepancies, current native contracts and the bounded consumer search.

## What Changes

- Specify automatic target ordering for tending, patient feeding, animal training, sowing,
  harvesting, cutting plants, finishing construction frames and rescue, within existing work rules.
- Preserve distinct disease/bleeding urgency, choose the worst disease per patient and retain native
  permissions, medicine/food choice, training prerequisites, construction costs and harvest batches.
- Prefer fertility first and hay last only for equal fertility; keep native growing buildings covered.
- Keep existing contextual controls and job/save state, without another menu or per-feature cache.
- **BREAKING relative to source parity:** omit automatic non-faction rescue and old rejection
  overrides; keep native rescue eligibility. Correct lost medical differences, reversed disease
  comparisons and arbitrary scalar category crossover. No package/assembly impersonation.
- Apply the user-selected imminent-danger priority across medical classes, shared directly with
  S134; retain ordinary eligibility and protected care without claiming perfect survival prediction.

## Capabilities

### New Capabilities

- `megamod-smarter-work-priorities`: observable target ordering, work/eligibility boundaries,
  direct composition, lifecycle and unavailable-feature fallback for the full selected source scope.

### Modified Capabilities

None. Existing work/health/food/housekeeping contracts remain; this change consumes them at selection.

## Impact

Future implementation affects only megamod's automatic work selection. RimWorld1.7 and Harmony are
required when implemented. DLCs, optional trainables and other mods are absent-safe inputs, not
dependencies. Source mods are read-only references. No runtime dependency on other repository mods.

No static Def change or XML patch is selected. A later XmlExtensions operation would require XML
Extensions (`imranfish.xmlextensions`, Workshop2574315206), content/node admission and merge-preserving
changes. Do not ship upstream code/data/assemblies or the Dev Gateway. Implementation, finalized
balance, external compatibility and native player-workflow acceptance remain open.
