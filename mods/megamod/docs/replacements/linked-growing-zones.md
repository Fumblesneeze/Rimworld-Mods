# Linked growing zones

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Origin: **original**.
- Intake P1-06 / S006: user confirmed persistent sharing of crop, Allow sowing and Allow cutting.
  No external source mod/Workshop identity or upstream license applies.
- Inspected 2026-11-02: native **1.7.4995 rev309**, assembly SHA256
  `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`, from
  `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`. Native proprietary source was inspected
  read-only, not copied into product code. Local decompilation:
  `artifacts/Refinement/megamod-spec-20261102/linked-growing/game/`.
- Inspected `Zone_Growing`, `PlantToGrowSettableUtility`, `Verse.Command_SetPlantToGrow`,
  `PlantUtility.ValidPlantTypesForGrowers`/`CanSowOnGrower`, `WorkGiver_GrowerHarvest`, and sowing
  eligibility/blocker-clearing branches. The failed RimWorld-namespace command lookup is not evidence.
- User intent and native code are authoritative for this original feature. No existing mod behavior
  or external compatibility is inferred merely from a similar feature name.

## Behavior inventory

| ID | Action → observable result | Evidence / assumption | Owning specification | Status |
| --- | --- | --- | --- | --- |
| LG-01 | Link fields → persistent shared crop and two flags | User instruction; first-selected source follows linked-storage interaction as an explicit design choice | [Linked growing zones](../../../../openspec/changes/specify-megamod-linked-growing-zones/specs/megamod-linked-growing-zones/spec.md) | Specified only |
| LG-02 | Change crop → all members update | Native command already iterates temporary selected growers; extension persists membership | Same spec, synchronized controls | Specified only |
| LG-03 | Disable sowing/cutting → native work permission changes | `Zone_Growing.GetGizmos`, sow/harvest work givers; selected crop can still be harvested with cutting off | Same spec, work rules | Specified only |
| LG-04 | Choose crop → native valid choices and warnings | `Command_SetPlantToGrow`, `PlantUtility`; proposed incompatible-link rejection is an explicit extension | Same spec, crop eligibility | Specified only |
| LG-05 | Unlink/delete/resize/reload → preserve valid independent or shared choices | Original lifecycle requirements; native zone serialization is per-zone | Same spec, member and save lifecycle | Specified only |
| LG-06 | Missing saved crop → visible unresolved choice, sowing paused | Deliberate safeguard instead of native null-to-potato fallback | Same spec, lifecycle recovery | Specified only |

## Interactions and UI audit

Crop, sowing/cutting and link controls belong to the zone's contextual gizmos. Group name, membership
and Select linked belong to zone inspection/selection. No bottom tab or mod-settings row is needed:
these are entity-specific gameplay choices, not configuration. Independent zone defaults remain
native (both flags true, ordinary crop selection). Linked hydroponics has a separate intake entry;
it must compose with this policy explicitly, not patch megamod's own grouping methods. No altered
fertility, yields, skill thresholds, temperature, research or harvest speed is part of this feature.

## Contracts and persistence

| Inspected consumer | Contract | Treatment / limit |
| --- | --- | --- |
| `IPlantToGrowSettable` and native crop command | Crop get/set, grower cells/map, combined validity and information/warnings | Preserve via owned command integration; no unrestricted replacement list |
| Native grower work | `allowSow`, `allowCut`, chosen crop plus actual pawn/cell conditions | Keep native per-zone values coherent; no replacement scheduler |
| `Zone_Growing.ExposeData` | Crop Def plus two boolean save values | Preserve base values; add separate membership and explicit missing-crop recovery |
| Installed external writers/readers | Not yet inspected | LG-Q1; no drop-in or universal propagation claim |

Existing zones remain independent until a player links them. Migration from another provider is
not included. Disabled-feature/removal recovery is still LG-Q2, not a guaranteed safe uninstall.

## Implementation and coverage

Nothing implemented. All six rows await tests, target admission documents and final native workflows.
Core required; no source-provider dependency or co-installation concern for this original feature.
Optional grower/content providers need canonical-package and Def/member shape checks. Harmony or
XML Extensions becomes required when used. No declarative list replacement is planned.

## Verification and acceptance

Future workflows must use actual native zone selection, crop choice and toggles, then observe growers
sowing/harvesting under changed policy. Include duplicate-group multi-selection, changing crops with
sowing disabled, mature selected-crop harvest with cutting off, incompatible pollution choices,
unqualified growers, resize/delete/reload and missing content. Host policy tests cannot prove native
job behavior. Final reviewed-build runs follow root acceptance and retain before/action/after proof.
LG-Q1–3 remain in the [refinement ledger](../refinement/specification-progress.md).
