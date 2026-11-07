## Why

Improved breathing, blood pumping and blood filtration stop benefiting consciousness at 100% in
the inspected base game. Consciousness Patch lets those improvements matter; the megamod needs
explicit contribution rules, a working optional input limit and clear composition with health ceilings.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S046 / P1-38**, Consciousness Patch (`Vex.Consciousness`, Workshop 2913127696).
This change **specifies future behavior only**. No gameplay is implemented or accepted.

## What Changes

- Allow the three physiological capacities above 100% to contribute to consciousness, retaining
  source default weights of 20% blood pumping, 20% breathing and 10% blood filtration.
- Specify multiplicative relative contributions and their effects below 100%, not an additive
  percentage-point bonus or immunity to damaged organs.
- Implement the documented optional common limit on each input capacity, default off with a
  dormant 300% limit; correct the source's malformed cap-on transformation.
- Put all five policy values in common global/save settings with clear language and coherent
  application to existing pawns, replacing stale startup-inlined policy and missing installed labels.
- Preserve native brain/pain, later condition/gene effects, capacity presentation and health rules.
  Compose directly with S045: malnutrition's final ceilings still constrain the improved result.
- Guard the precise physiological contribution behavior and preserve ordinary fallback when an
  original or unsupported provider owns it; do not duplicate the calculation or patch owned code.

## Capabilities

### New Capabilities

- `megamod-physiological-consciousness`: Physiological contributions, input limits, scoped policy,
  native health consequences and condition-ceiling composition.

### Modified Capabilities

None. S045's existing ceilings remain unchanged; this capability explicitly supplies the preceding
worker result. Its pending composition notes will link the reconciled contract.

## Impact

Future owning code: one narrow admitted native worker integration, common settings and native
capacity/health refresh. Core and **Harmony (`brrainz.harmony`)** required for the implemented hook;
declare Harmony when implemented, never bundle it. DLC, enhanced-organ providers and the source mod
are optional. S045's XML Extensions requirement belongs to its separate XML feature; no XML operation
is used here. Original Consciousness Patch suppresses the owned overlap without foreign unpatching.
No source/private Harmony/Dev Gateway assembly is shipped and no repository product dependency is added.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-physiological-consciousness-research.md)
pins complete source, matching installed DLL and native IL, settings, cap-path defect, static balance
comparators and external-consumer limits. No blanket organ/health-provider or beta loadability claim
is implied by source support metadata or this draft.
