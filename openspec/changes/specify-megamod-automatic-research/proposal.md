## Why

Researchers should be able to continue useful work when the player has not chosen a project.
Independently specify Research Whatever's automatic selection, study exhaustion and completion
notifications while preserving deliberate research choices and the game's progression restrictions.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Origin replacement, **S120/P2-50**, Research Whatever by avilmask/avil,
package `avilmask.ResearchWhatever`, Workshop2552092060.

## What Changes

- Give player research benches a persistent, initially enabled automatic-selection control.
  An idle eligible researcher chooses an available project requiring the least remaining work;
  existing player/provider choices always take precedence.
- Preserve exclusion and low-priority research policies, ordinary prerequisites and actual bench
  requirements. Correct the source's total-cost tie selection and premature choices at unusable benches.
- Automatically select suitable Anomaly research when positive study knowledge has no selected
  destination. Preserve category precedence, current study rewards and unfinished local discoveries.
- Pause exhausted study only through an available, reversible contextual control. Preserve the
  latest source's captured-entity resume correction; do not hide a second blocking switch.
- Offer native dialog, letter, quiet message or silent completion presentation through the common
  inherited settings UI. Leave independent story/unlock notifications and research consequences intact.
- Preserve optional content through guarded additive policies, one selected provider and minimal
  semantic target checks. Use native research/study data and directly compose owned UI features.

**Specification only.** [Research](../../../mods/megamod/docs/refinement/2026-11-06-research-whatever-research.md)
retains the selected1.6 binary/public source, current1.7 contracts and intentional corrections.
No gameplay, migration, package changes or live compatibility is delivered by this draft.

## Capabilities

### New Capabilities

- `megamod-automatic-research`: contextual bench autonomy, eligible remaining-work selection,
  Anomaly routing and resumable exhaustion, common completion presentation and guarded composition.

### Modified Capabilities

None. Existing message placement, contextual links, research UI and future search consume current
native state; their requirements do not need another internal patch layer.

## Impact

- Planned Harmony use requires `brrainz.harmony`. Guarded declarative bench/policy additions require
  **XML Extensions** (`imranfish.xmlextensions`, Workshop2574315206); neither library is bundled.
- Anomaly, Vanilla Gravship Expanded and Researchable Stat Upgrades are optional content inputs.
  No required source mod, DLC, other repository product or Dev Gateway dependency.
- Bench choices remain entity state; the completion mode uses shared global/save settings. No new
  bottom tab, research tree, queue, persistent project index, copied assembly or source identity spoof.
- Original-present selects the original and disables the overlapping owned feature. Public extension
  bindings and source-save migration are documented limits until actual consumers justify adapters.
