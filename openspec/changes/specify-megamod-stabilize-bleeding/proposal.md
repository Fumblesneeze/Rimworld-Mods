## Why

An emergency order should stop blood loss without spending time on unrelated treatment or collecting
medicine. S080 selects Stabilize Bleeding's advertised purpose; the inspected binary does not itself
filter the general treatment routine to bleeding conditions, so copying it would not establish that purpose.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. Origin replacement, S080/P2-09. This change **specifies future behavior
only**; implementation, tests and native acceptance remain outstanding.

## What Changes

- Add an explicit contextual emergency order for a single capable doctor, drafted or undrafted,
  to tend currently bleeding, treatable conditions at the patient's location without medicine.
- Retain the source's broader actual patient scope, including downed outsiders and eligible mobile
  colony patients/self-treatment. Clearly explain unavailable actions and preserve medical capability.
- Select the greatest current blood-loss contribution first; include fresh missing-part wounds,
  exclude non-bleeding conditions, and stop when no permitted useful bleeding treatment remains.
- Retain ordinary unmedicated work, quality and consequences; avoid repeated ineffective treatment,
  unrelated patient-order cancellation, duplicate rewards and replay after save/load.
- Compose directly with individual care, self-tend permission and medical work. Keep Combat Extended
  stabilization distinct: its medicine-consuming temporary effect is not this ordinary tending order.
- Record source discrepancies, external identities, co-installation fallback and later questions.

## Capabilities

### New Capabilities

- `megamod-stabilize-bleeding`: explicit bleeding-only field tending, permissions, prioritization,
  actual treatment costs/effects, cancellation, persistence, medical composition and compatibility.

### Modified Capabilities

None. S062's condition permissions remain authoritative; its ordinary batching order is not applied
to this separately named explicit emergency order. Its pending composition note is clarified here.

## Impact

Future owning code belongs beside the medical feature and uses native contextual extensions, jobs,
health and reservations. No separate settings, map cache, tab, source-package impersonation or save
import is proposed. Core is required; the selected source and other medical providers are optional
inspection/integration inputs, not dependencies. No DLC is required for the base action.

Prefer native public extension points. Harmony becomes required only if actually used; XML Extensions
becomes required if guarded XML patches are needed. A new owned JobDef alone does not require either.
Every actual patch still requires executable semantic admission and a documented target spec.
No product dependency/reference/load hint on another repository mod or the Dev Gateway is permitted.

Evidence and unresolved decisions: [research](../../../mods/megamod/docs/refinement/2026-11-04-stabilize-bleeding-research.md).
