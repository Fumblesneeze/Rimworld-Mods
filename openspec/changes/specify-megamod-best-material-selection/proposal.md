## Why

Players should be able to prefer a useful crafting material without repeatedly narrowing and
reopening a bill's ingredient filter. Use Best Materials supplies that workflow; specifying its
actual choices and limitations lets us integrate it with the bill browser, linked workbenches and
perishable-stock preference without silently changing the player's selected material.

## What Changes

- Specify independent per-bill automatic material preference, relevant single-stat and named
  weighted comparisons, separate ingredient-list sorting and contextual material information.
- Preserve ordinary recipe quantities, permissions, quality generation and the native search's
  reach. Make unavailable comparisons and unsupported recipes understandable.
- Compose explicit material locks, linked-list edits, bill snapshots, started iterations and
  perishable selection into one owned workflow. Correct shared mutable preferences, ambiguous
  display directions, lost inactive choices and unsafe missing-stat recovery.
- Record inspected source contracts, deliberate omissions and later questions. This change
  delivers **documentation only**; no gameplay, compatibility or performance is implemented.

## Capabilities

### New Capabilities

- `megamod-best-material-selection`: contextual bill material preference, comparison and sorting,
  physical selection boundaries, independent persistence, composition and safe unavailability.

### Modified Capabilities

None in the archived baseline. The in-progress S024 practical-supplies draft receives a narrow
composition clarification: an explicit automatic output-material preference precedes spoilage;
spoilage still orders equally preferred eligible materials and other ingredients.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), targeting
**RimWorld 1.7 beta**. Source: **S026 / P1-20**, Use Best Materials, `kathanon.UseBestMaterials`,
Workshop **2901561962**. Proposal, design, capability and tasks live here; provenance and retained
questions live in [research](../../../mods/megamod/docs/refinement/2026-11-02-use-best-materials-research.md)
and [coverage](../../../mods/megamod/docs/replacements/use-best-materials.md).

Future runtime touches bill UI/state and ingredient selection, composed directly with S009–S012,
S014, S017, S021 and S024. S059 Best Mix's separate owned change now specifies the general ingredient
criterion after this output-material choice and before spoilage. Harmony becomes
a **required** dependency when its types are used. The original source, Better Workbench Management,
Math! and Performance Fish are **optional external providers**, never requirements for this owned
workflow. Exact original-provider co-installation suppresses owned overlap with a warning. No
provider package identity is impersonated. No XML change or XML Extensions use is proposed here;
no dependency metadata changes now. Product packages remain independent of other repository mods
and of the development Gateway.
