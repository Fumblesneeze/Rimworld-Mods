## Why

Choosing a building material or floor should show how much stored material exists and what that
specific choice costs. The original menu annotation has ambiguous row matching and incomplete
cost handling; the user's shared-index requirement should make these counts consistent with the
same map data used by Ctrl+F and hauling without making their different filters identical.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S032; this change **specifies future behavior only**.

## What Changes

- Show stored/required material quantities on actual building-material and construction dropdown
  choices, including floors, with details for additional ingredients.
- Bind each count to its exact choice, preserving different costs for floors using the same
  resource. Use the actual adjusted cost and physical item units, including small-volume Stuff.
- Keep ordinary menu eligibility, ordering, selection and construction rules. Counts describe
  stored stock, not unreserved/reachable supply or the number of buildings guaranteed finishable.
- Refresh displayed stock after relevant changes and isolate maps/save lifecycles. Use the same
  shared candidate/index owner as S033 Ctrl+F and hauling, with a distinct stored-stock view.
- Extend annotations to resolvable native grid choices and nonterrain construction dropdowns,
  and isolate unsupported/utility rows. These are explicit corrections/extensions, not source claims.
- Keep the feature contextual; introduce no bottom tab, settings page or click-to-search action.

## Capabilities

### New Capabilities

- `megamod-buildable-material-counts`: correct contextual construction stock/cost information,
  freshness, unchanged actions and safe unavailable/co-installation behavior.

### Modified Capabilities

None. The foundation's shared Thing-data and patch-admission contracts apply without modification.

## Impact

- Reference **Show Buildable Material Count**, `BP.ShowBuildableMaterialCount`, Workshop
  **3245101540**. [Research](../../../mods/megamod/docs/refinement/2026-11-03-buildable-material-count-research.md)
  pins all nine decompiled files and relevant exact native 1.7.4995 inputs. No source DLL/code/art copying.
- Future hooks require **Harmony**, `brrainz.harmony` (Workshop 2009463077). No DLC or source mod is
  required. No XML modification is currently planned; XML Extensions becomes required if a later
  implementation adds its operations, with corresponding metadata and target contracts.
- Other menu/storage providers are **optional**, narrowly admitted integrations; their absence
  leaves native menus usable. No blanket Floating Menu or virtual-storage support is claimed.
- No source API/save migration or automatic source removal. Co-installed original annotations
  suppress the overlapping owned feature with a warning rather than duplicate labels.
- All implementation, target guards, tests, seven-language UI, measurements and native acceptance
  remain pending. The change does not alter resource amounts, costs, reservations or balance.
