## Why

Ordinary attacks should be poor substitutes for breaching equipment against structural barriers.
Specify the selected Walls are solid behavior while preserving other damage modifiers, making its
scope understandable and correcting unsafe exemption arithmetic and inherited breaching changes.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S072 is a replacement reference, not a co-owner.

## What Changes

- Add configurable ordinary cutting/piercing, blunt, projectile and explosion damage multipliers
  for passable and impassable buildings. Default to walls, doors and mineable barriers; an option
  extends ordinary reductions to other buildings. Preserve independent damage modifiers.
- Use the installed source's explosion behavior as the baseline, expressed as relative multipliers
  of 0.4 for passable and 0.25 for impassable buildings. Record the disagreement with its description.
- Retain enhanced crushing for suitable blunt weapon heads, natural-rock-family projectile immunity
  as an initially disabled option, and half mining damage against constructed nonmineable walls.
  Preserve dedicated breaching attacks and ordinary rock/ore mining.
- Make already selected dig-enabled exit behavior attack blockers instead of mining them, retaining
  native eligible ranged breaching, reservation/wait behavior and exit selection.
- Use common global/save settings applied to subsequent damage, without restarting or changing
  existing hit points. Add no bottom tab, debug tool or operational toggle.
- Preserve optional source integration coverage through separately admitted adapters. Keep one
  provider when the original is enabled, and isolate unsupported contributions with startup warnings.

This is a **specification-only draft**. No runtime functionality or accepted compatibility is delivered.
The semantic scope and normalized factors are explicit proposed corrections, retained for review.

## Capabilities

### New Capabilities

- `megamod-structural-damage`: structural damage families, crushing and breaching, mining and exit
  behavior, scoped settings, optional content and coherent compatibility fallback.

### Modified Capabilities

None. Composition preserves existing social-fight, item-destruction and roof-safe-removal contracts.

## Impact

- Source **Victor.WallsAreSolid**, Workshop **2896548513**, inspected 1.6 XML/root binary selected
  statically for 1.7. Identity, hashes, primary documentation and evidence limits are in
  [research](../../../mods/megamod/docs/refinement/2026-11-04-walls-are-solid-research.md).
- Future runtime dependencies: **required** Harmony and XML Extensions (`imranfish.xmlextensions`,
  Workshop2574315206) for guarded declarative content and native integration. Original source,
  optional content/combat/AI providers and DLC remain **optional**. Their presence is not acceptance.
- Recognize native weapon-head attacks without changing their damage identity; fixed mining XML
  contributions require atomic content guards and merge preservation. Save-specific factors require
  dynamic policy rather than global Def edits.
- No copied source assembly, source identity spoofing, automatic settings import, private Thing cache,
  new item/building/recipe, runtime dependency on another repository mod or product Gateway dependency.
