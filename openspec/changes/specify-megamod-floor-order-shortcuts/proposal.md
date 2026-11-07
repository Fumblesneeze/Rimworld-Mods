## Why

Floor planning should offer the existing Deconstruct command without requiring a category switch,
and general Orders should offer the existing Remove Floor command. S067 adds these two navigation
shortcuts while preserving what each native tool can actually do.

## What Changes

- Specify both additions supplied by Add Deconstruct Button To Floor Category, including its
  later Remove Floor addition, as observable native Architect workflows.
- Preserve original command locations, target restrictions, designation/cancellation and real work.
  Deconstruct remains a building tool; Remove Floor remains distinct from Remove Foundation.
- Merge each addition independently and avoid adding an already supplied equivalent entry.
- Propose mouse-only access for the added Orders Remove Floor entry. In inspected 1.7, it shares
  a key with wood harvesting; native rendering assigns that key to the first displayed claimant.
  Keep the existing Floors binding and other commands' bindings unchanged.
- Compose directly with S036 Architect presentation and S061 removal policies. Record missing
  content, original-provider coexistence and unavailable-correction fallbacks explicitly.

This change delivers **documentation only**. Implementation, tests and native acceptance remain open.

## Capabilities

### New Capabilities

- `megamod-floor-order-shortcuts`: Existing floor/building commands in additional Architect
  categories, contextual keyboard behavior, additive admission and native action preservation.

### Modified Capabilities

None. Related owned drafts retain their existing behavior and consume these native commands.

## Impact

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, targeting
**RimWorld 1.7 beta**. Source: `geodesicdragon.deconstructfloorcategory`, Workshop `2905837332`,
installed version 1.1.4. [Research](../../../mods/megamod/docs/refinement/2026-11-04-floor-order-shortcuts-research.md)
records primary documentation, complete XML inspection and the proposed binding deviation.

XML Extensions (`imranfish.xmlextensions`, Workshop `2574315206`) is **required** when implementing
the guarded XML additions; the inspected installation's 1.7 loading remains unverified. Harmony
is required on eager use for the contextual binding adapter. Neither dependency is added by this
documentation change. The original source is optional and never bundled or impersonated. No DLC,
other repository product mod or Dev Gateway runtime dependency; no custom art or new Thing cache.
