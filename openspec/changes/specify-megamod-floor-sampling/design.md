## Context

Sole owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
S136 documentation only. [Research](../../../mods/megamod/docs/refinement/2026-11-07-floor-pipette-research.md)
pins installed source, binary IL, public commit,1.7 load-folder fallback and native designator behavior.

## Goals / Non-Goals

**Goals:** one contextual sample→ordinary tool transition, honest availability/feedback and direct
S036 Architect/S038 content-selection composition.

**Non-goals:** new floors, general terrain cloning, paint copying, blueprint sampling, buried-layer
selection, research bypass, altering build costs, a new hotkey/settings framework or item index.

## Decisions

Use a small owned Designator with a shared pure current-cell assessment for hover, CanDesignateCell
and click. First validate current map/in-bounds/fog; only then inspect terrain and current Floors
category/build availability. Click repeats the full assessment rather than trusting a previous
hover. Keep source category scope explicit; arbitrary buildable terrain elsewhere is not silently
added. Read the current terrain grid, no map-wide scan/cache. Clear transient state on map/game exit.

Use existing Architect resolution and the native BuildCopyCommandUtility-compatible result, not a
new Designator_Build to evade rules. Native FindAllowedDesignator(BuildableDef,bool=true) recursively
resolves dropdowns and game rules, but its static cache only rechecks Visible on cache hits; it can
retain null/membership/rule decisions. S036's current evaluation/invalidation must cover genuinely
effective paused context changes. Consume only S038's applied policy: requested exclusion/category
changes stay pending until its save/reload boundary. Reuse that authority and clear/fallback when uncertain, rather
than another floor-permission cache. Effective rule/visibility must be checked before selecting.

Select the actual current command and active dropdown variant, then request S036's one-shot reveal.
Do not execute the build command's DesignateSingleCell or replay input to reveal it. Explicitly
consume the sample gesture across press/release so the newly selected construction tool cannot
receive the same click. Native DesignatorManager already keeps the original designator for
Finalize on mouse-down; test drag/release and custom draw styles instead of assuming all paths do.
Preserve native colored-floor ShowEyedropper behavior; it chooses color within a dropdown, not the
same general Floors sampler. No source Cancel hotkey: native Designator_Cancel defaults C and
already appears in Floors. Normal right-click/Cancel-mode exit remains.

Create an owned icon through the repository raster-art skill during implementation; no source
Oskar Potocki texture is copied or selected now. Review reference proportions, smallest readable UI
size, light/dark contexts and independent context-free visual acceptance in a real game. All seven
languages cover labels, generic fog/out-of-bounds rejection and ordinary unavailable explanations.
No global/save preference or new persistent state; this is a contextual action.

### Admission and declarative integration

FPT-FLOORS-ENTRY target document: unique Floors category/specialDesignatorClasses node, valid native
selection contracts and owned type available. XML Extensions checks nodes, appends only missing
owned entry and preserves all source/native/third-party siblings. Required dependency metadata in
same slice. Do not copy source bare PatchOperationAdd or empty Harmony.PatchAll. No Harmony patch is
currently selected; if an actually needed native adapter is later introduced, create per-patch
semantic predicates and focused tests first, declare Harmony and check effective/incoming code,
compatible foreign changes, repatch invalidation, atomic owned rollback and bounded warnings.

Gate sampler availability and selection together. Optional external bindings require canonical
package identity plus exact consumed shapes; XML content also FindMod packageId=true. Native
ordinary floors do not need a hardcoded list of DLC packages. Active ferny.floorpipettetool withholds
the owned entry/selection, never removes its patches/commands. Unreadable runtime candidate data
rejects that sample safely and visibly without per-frame logging; startup admission failure warns
once per skipped enabled patch. Preserve S036's independent ordinary/provider fallback.

## Risks / Trade-offs

- Source hover ignores fog and click relies on earlier admission → unified ordered checks and
  fog regression cases prevent information disclosure and stale selection.
- Category membership is deliberately source-like → custom floors moved elsewhere are unsupported
  samples, even if they can be built manually. Record FPT-Q2 rather than quietly expanding scope.
- Native static resolver cache can outlive changed rules → current S036/S038 authority, no speculative
  second cache and no “research completed once” permission shortcut.
- Clicking through after selection could designate work → native press/drag/release acceptance is
  mandatory, plus sampling/removal separation with S067 floor shortcuts.

## Migration Plan

No source settings or saved sampled-floor state. Future product deployment adds owned command only;
loading starts without a sample target. No source API/type/package impersonation. Any later save
migration requires its own backup, atomicity and failure restoration contract.

## Open Questions

FPT-Q1 inventory resolved. FPT-Q2 category/hotkey/one-shot reveal deviations, FPT-Q3 effective native
selection/provider/fog/input guards and FPT-Q4 implementation/localization/art/native acceptance remain.
