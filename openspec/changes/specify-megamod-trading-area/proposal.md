## Why

A point surrounded by unrestricted wandering does not give the player enough control over where
visiting traders gather. The user wants a dedicated trading area drawn through area tools, with its
own contextual gizmos, rather than assigning traders to a pawn allowed area.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Origin: original**, S040/P1-31 and the user's interview clarifications.
S039 [PL] Trading Spot is the separately specified gathering reference, not a runtime dependency.

## What Changes

- Specify one editable map-local Trading area, initially empty, with additive painting, cell
  erasure and clearing through an area-tool entry point and contextual gizmos.
- Show the footprint and usable/inactive status without changing existing zones, roofs, buildings,
  allowed areas or pawn assignments. No marker, construction bill, resource cost or research gate.
- Make actual painted cells govern eligible parties' ordinary gathering and idle destinations,
  while preserving S039's native roles, outside-footprint paths, deliveries and emergency exits.
- Define blocked, narrow, disconnected, crowded and empty footprints without unreachable-target
  loops, forced movement permissions or repeated destination churn.
- Retain area cells per map/save; expose disabled integration honestly while preserving editable
  area data. Do not replace another provider's marker or configuration.

This delivers **specification only**. One area per map, initial emptiness, overlapping existing
zones, discovered-cell painting and the precise gizmo set are proposed defaults recorded in the
spec, not additional statements attributed to the user. "Fixed" means a dedicated area type; the
player can edit its footprint. Both S039 and S040 are required for the complete owned feature.

## Capabilities

### New Capabilities

- `megamod-trading-area`: dedicated area editing, contextual visibility, painted-cell gathering,
  usable-cell selection, map/save lifecycle and inactive-state behavior.

### Modified Capabilities

None. S039 retains party eligibility/lifecycle and visitor settings; S027 retains finite deliveries.

## Impact

- Future map-owned area data and native designator controls, composed directly with S039's routing.
  Core required; no DLC or upstream mod required. Original Trading Spot and guest providers optional.
- No new bottom tab, mod preference, global default for map cells or ordinary allowed-area entry.
- Prefer native area/designator extension points. Harmony becomes required if actually used;
  any XML Extensions operation requires its dependency and guarded merge-preserving integration.
  No current runtime metadata or XML is changed by this documentation slice.
- No duplicate Thing cache, source DLL, source marker identity, automatic migration or external API
  compatibility promise. Seven-language UI and any new raster artwork need their normal live gates.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-trading-area-research.md) records native
  seams and assumptions; [coverage](../../../mods/megamod/docs/replacements/trading-area.md) tracks
  this original feature independently of source S039.
