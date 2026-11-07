## Why

A dragged area alone does not tell the player how many cells their selected order can affect.
Designations Tooltip's replacement should show both quantities accurately, including 1.7 drawing
shapes, while leaving the underlying designation action unchanged.

## What Changes

- Specify an affected-cells / candidate-cells preview at the center of supported designation drags,
  with affected-only Classic mode and optional counts on small drags.
- Preserve source defaults: Classic off, small-drag counts on. Use the common global-default and
  per-save override settings path, with safe main-menu behavior and immediate display updates.
- Count the selected shape's actual cells and accepted subset, including zero affected cells;
  distinguish this preview from object counts, resource yields and eventual job completion.
- Keep native outlines, measurements, selection rules and commitment unchanged. Correct stale paired
  counts, long-label centering and rendering-state leakage rather than reproducing source defects.
- Apply per-patch semantic admission and bounded diagnostics, preserve native fallback, and compose
  directly with owned designation tools. Record the actual Vehicle Map Framework ordering contract
  without claiming untested beta support or impersonating the source patch owner.

This change **specifies later work only**, origin **replacement**, S016 / P1-13 / P1-36. Zero-count
and shape semantics follow inspected installed code/native 1.7 behavior where source help/comments
are inaccurate. Settings inheritance and display corrections follow the user's megamod rules.

## Capabilities

### New Capabilities

- `megamod-designation-preview`: Accurate contextual affected/total cell counts with scoped display
  preferences, unchanged designation actions and safe compatibility fallback.

### Modified Capabilities

None. Owned Allow Tool functionality composes with this display through its existing action contract.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
See [research](../../../mods/megamod/docs/refinement/2026-11-02-designations-tooltip-research.md) and
[coverage](../../../mods/megamod/docs/replacements/designations-tooltip.md).

Future work touches designation preview rendering and two common settings rows. **Harmony**
(`brrainz.harmony`, Workshop `2009463077`) is **required** when this patch implementation is added.
No XML Extensions operation is proposed by this feature. The source provider is not required;
when active, our overlapping display is withheld. Vehicle Map Framework is an **optional** external
integration needing canonical-package and shape/order validation for a supported version.

No new bottom tab, resource/research/balance change, private map cache, upstream settings import,
custom gameplay save migration, product-to-product dependency or Gateway dependency is introduced.
All source and runtime assemblies remain read-only research inputs. No runtime code, metadata,
active mod list or package changes in this documentation slice.
