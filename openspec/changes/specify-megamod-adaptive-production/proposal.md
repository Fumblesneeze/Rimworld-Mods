## Why

Production targets should follow the colony or another deliberately selected population without
constant bill edits. A separate ingredient-surplus trigger can automate processing while making its
limits clear: it decides when work starts, not how much stock a completed batch must leave.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, queue **S018**, **Everybody Gets One - Continued**, `Memegoddess.EverybodyGetsOne`.
This change delivers **specification only**; all implementation and gameplay acceptance remain open.

## What Changes

- Specify population-plus-offset and quantity-per-match targets, current totals, signed offsets,
  pause/resume behavior and explicit mode-switch semantics.
- Specify the bill's contextual matching editor, default population, map scopes, ordinary query
  combinations, result preview, complete cancel and recoverable missing conditions. Keep a shared
  query engine with S033/S121 rather than one search cache per replacement.
- Compose matching-pawn equipped counts with S017's unique product accounting and S014's whole-list
  plans directly. Copy the complete choice snapshot; never copy in-progress physical work.
- Specify a strict ingredient-stock trigger for one-entry recipes, including ordinary costs,
  batch overshoot and completion/collection of already committed work.
- Deliberately correct partial/unknown results being treated as counts, camera-dependent production,
  signed-control inconsistency and incomplete editor cancellation. These are proposed refinements.
- Retain optional, faithfully translated search exchange with TD Find Lib; explicitly leave wider
  catalogue coverage to S121 and foreign-save/binary compatibility unpromised.

## Capabilities

### New Capabilities

- `megamod-population-production`: adaptive targets, contextual matching, stock-carrier composition,
  shared plans, persistence, exchange and safe failure.
- `megamod-surplus-production`: one-ingredient stock triggers, threshold editing, ordinary work and
  committed-output recovery.

### Modified Capabilities

None in archived capabilities. Companion draft clarifications in S014/S017 point to this composition;
their existing capabilities remain owned by their respective changes, all under the same mod owner.

## Impact

Future implementation affects bill mode selection/details/overview, production start decisions,
query persistence/exchange and shared count data. It introduces no new bottom tab or configuration
preference. Bill choices belong to entity gameplay UI; future ordinary shared-library configuration
follows the common global/save settings contract.

Required future runtime dependency: **Harmony**. The independently owned query behavior does not
require the source mod or TD Find Lib. **TD Find Lib - Continued** (`Memegoddess.TDFindLib`) is an
optional, package- and shape-guarded exchange provider; its own required dependencies remain its
responsibility. No source assemblies are shipped. The three new owned repeat-mode Defs are additive
content, not edits to existing Defs; no XML patch is selected. Any later XML mutation requires XML
Extensions (`imranfish.xmlextensions`, Workshop 2574315206) and guarded merge-preserving operations.

Original EGO active means one overlapping implementation, with ours withheld and owned saved work
recoverable. No package spoof, automatic source-settings/save conversion or current compatibility
claim. [Source evidence and open questions](../../../mods/megamod/docs/refinement/2026-11-02-everybody-gets-one-research.md)
retain exact identities, public documentation, native comparisons and EG-Q1–4.
