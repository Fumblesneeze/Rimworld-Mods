# Labels on Floor (continued) — replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S093/P2-22; source canonical `falconne.labelsonfloor`, Workshop 3546003256, selected DLL 1.7.5.0.
**Status: independently reviewed and strictly validated observable draft; implementation and native/visual acceptance pending.**

## Specified coverage

Automatic revealed room/zone/crop labels; contextual names, colors, visibility, direction and
offsets; Classic and Smart placement; overlap, zoom, font and opacity preferences; inherited
global/save controls; bottom-right visibility cycle; transactional Orders/hover editor; current
native zone names; room/zone persistence; optional Odyssey transfer and screenshot hiding.

[Proposal](../../../../openspec/changes/specify-megamod-floor-labels/proposal.md),
[observable contract](../../../../openspec/changes/specify-megamod-floor-labels/specs/megamod-floor-labels/spec.md),
[design](../../../../openspec/changes/specify-megamod-floor-labels/design.md),
[tasks](../../../../openspec/changes/specify-megamod-floor-labels/tasks.md),
[public/source/binary/native research](../refinement/2026-11-05-floor-labels-research.md).

## Deviations and contracts

- Current view control moves to the bottom-right; presentation preferences use common inheritance.
  Individual label edits remain contextual. No new bottom tab; source R/L gestures are retained as
  rebindable defaults, and the editor can choose a room beneath its zone.
- Correct close/dismiss preview leakage, stale native zone names, explicit-white reload loss,
  mismatched creation/load defaults and destructive subtype toggling. Draft reset is cancellable.
- Honor forced direction and minimum size; reject invalid footprints/fog; refresh equal-count
  reshapes and draw partially visible labels. Smart placement preserves useful preferences, not
  the source's exact chunk weights or accidental bounding-box layout.
- Retain anchor-based room naming with deterministic merge/split/reset and native zone transfer
  identity. Preserve text/style/visibility through reload and actual successful gravship movement;
  do not change which zones or cells the game transfers.
- Keep readable fallback and visible missing-character markers. Six source style roles are covered;
  no particular foreign font resource is selected for reuse or licensed by this document.
- Minimum/maximum text scale inherit as one pair, preventing incompatible combinations after a
  global change. Original provider presence disables owned overlap while retaining owned records;
  resumption reconciles native renames before displaying or resetting a retained custom name,
  while appearance-only records retain automatic crop/name text. No package/type
  spoofing, source save migration, legacy manager destruction or unproven font/API facade. A bounded
  installed scan found recommendation metadata but no concrete callable consumer.
- Progress Renderer is an optional, package/shape-admitted capture integration candidate. Its
  archived source supports the consumed getter; current installed 1.7 compatibility remains unproven.
  Native screenshot support and base labels do not depend on that provider or Odyssey.

## Not implemented or accepted

**Nothing implemented.** No gameplay changes, full Unicode promise, generalized external font
framework, arbitrary screenshot-provider support, source-removal guarantee or performance claim.
LFL-Q1–5 retain runtime targets, lifecycle, exact providers, fonts/localization and native evidence.

Future acceptance must personally exercise edit/apply/cancel/reset, native room/zone/crop changes,
overlap/zoom/fog, save/map/language switching, settings scope and the exact admitted travel/capture
workflows. Actual text at multiple zooms/backgrounds/scripts requires the asset skill's independent
live visual gate. Passing tests or loading labels alone does not prove those player workflows.
[Progress ledger](../refinement/specification-progress.md).
