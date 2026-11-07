## Why

Large arrivals and new maps can interrupt play while all their contents are generated at once.
S078 asks to distribute that preparation while revealing raids and trader groups only when the
whole group is ready, and to make map/encounter loading responsive as well.

## What Changes

- Specify incremental preparation for raids, trader caravans, new maps and encounters, without
  revealing partially prepared groups or destinations.
- Preserve the intended event, strength, goods, relationships, arrival behavior and completed-map
  content. Performance must not come from removing work or weakening an encounter.
- Account for each event once, retain the caller's outcome, and define safe pause, save, exit,
  invalid-target and failure behavior rather than treating queued preparation as a completed arrival.
- Keep progress in the existing play/loading context, without a bottom tab, new game mechanic,
  future-event preview or speculative stockpile of generated pawns.
- Record unresolved timing/overload and persistence choices explicitly before implementation.
  This draft does not invent a measured time budget or promise universal compatibility.

## Capabilities

### New Capabilities

- `megamod-generation-smoothing`: Responsive preparation and complete publication of arrivals and
  destinations, preserving their gameplay and lifecycle outcomes.

### Modified Capabilities

None.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **original**, photo/interview P2-07. There is no source mod to replace or emulate.
This change specifies future behavior only; no runtime behavior or performance is delivered.

Harmony is required when the implementation first uses it. DLC and external generation/content
providers are optional and need exact supported-path admission; unsupported paths retain their
provider's generation. No XML mutation is proposed, so this feature adds no XML Extensions use.
The developer Gateway and Circinus remain verification-only, never product dependencies.

Affected systems include incident callers/accounting, pawn/stock generation, arrival publication,
map/encounter loading and save/teardown boundaries. Existing owned arrival-clothing, provisions and
trading-area behavior composes directly before publication. Shared search/hauling sees only usable
published content. No package spoof, foreign assembly copy or per-feature map Thing cache.

[Research](../../../mods/megamod/docs/refinement/2026-11-04-generation-smoothing-research.md) separates
native facts from proposals and remaining gates; [coverage](../../../mods/megamod/docs/replacements/generation-smoothing.md)
tracks the full requested scope and future evidence.


Decisions12/19 require background preparation while ordinary simulation continues, with raid,
ambush and hostile-entry notices only after complete valid generation. Discard results whose source
or entry intent vanished, preserving existing pawns/property. The rough1–2-second real-time estimate
is contextual, never a deadline or code/test assertion.
