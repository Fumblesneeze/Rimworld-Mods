## Why

S054 selects I Cant Fix That so salvaging advanced equipment does not automatically give the colony
the knowledge to repair it. The replacement needs a consistent rule across building repairs,
breakdowns, maintenance, mending and admitted automated repair, without accidentally consuming
equipment or preventing unrelated work.

## What Changes

- Specify colony research knowledge, alternative production routes, workbench research fallback
  and an optional additional technology ceiling for repairs.
- Preserve ordinary work eligibility, costs, speed and success chances; add clear contextual
  refusals and safe cancellation when eligibility changes.
- Correct recipe-order dependence, missing workbench fallback, inconsistent settings defaults and
  the loss of lower-tech repair ability caused by maximum construction-tech restrictions.
- Apply the same target-only rule to supported maintenance, donor-item and mending integrations,
  with explicit scope for colony workers and automation. Recycling and destruction stay separate.
- Use common global/per-save settings and native contextual explanations, with no new bottom tab.

## Capabilities

### New Capabilities

- `megamod-repair-knowledge`: Research- and technology-limited repair eligibility, settings,
  contextual feedback, safe work lifecycle and optional repair-provider composition.

### Modified Capabilities

None. Existing foundation, recycling, destruction, production and construction contracts retain
their own scopes; future owned repair workflows call the same policy directly.

## Impact

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Documentation only; no gameplay implementation or acceptance.

Future runtime requires Core and Harmony (`brrainz.harmony`) for admitted patches. All external
repair/maintenance providers are optional, independently package/content/shape-admitted branches;
none is required to restrict ordinary building repairs. No XML mutation is currently planned. If
one becomes necessary, its first guarded use requires XML Extensions (`imranfish.xmlextensions`,
Workshop2574315206), a target spec and merge-preserving operations in that slice.

No upstream assembly, package identity, settings data or artwork is shipped. No repository-mod or
Dev Gateway dependency is introduced. See the [source research](../../../mods/megamod/docs/refinement/2026-11-03-i-cant-fix-that-research.md).
