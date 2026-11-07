## Why

Players should be able to inspect a prospective colonist before answering a wanderer offer without
losing the event's medical context, deadline or consequences. Wander Joiner Plus supplies that intent,
but its current letter replacement can discard native event details; its older public code describes
a different incident-interception approach.

## Owner

Exactly one owner: **Make Rimworld Great Again**, package `fumblesneeze.megamod`,
`mods/megamod`, targeting **RimWorld 1.7 beta**. Queue **S109**, origin **replacement**.
This change specifies future behavior only; no gameplay implementation or acceptance is delivered.

## What Changes

- Add visible work restrictions, traits and passions plus a native pawn-information action to
  supported wanderer offers. Preserve the original identity, context, choices and remaining time.
- Cover ordinary walk-in offers and Royalty's paralytic-abasia offers separately. Receiving a pod
  and consenting to membership are different events; inspection must not postpone needed care.
- Preserve native acceptance, refusal, expiry, charity and arrival rules. Do not extend S108's
  rescuee-specific untimed request or refusal policy to these existing timed letters.
- Preserve hidden provider state for an admitted Vanilla Events Expanded traitor offer without
  revealing it. Record the inspected provider's generation-dispatch gap as research, not a promise
  that every installed version works or permission to manufacture traitors ourselves. If that route
  is unavailable, use the same presentation fallback for otherwise indistinguishable walk-in offers.
- Avoid duplicate prompts, unrelated joiner interception, global letter-class replacement and
  accidental changes to emergency arrivals such as the man in black.
- Document the upstream saved letter type as an unsatisfied migration contract. No automatic
  source-save migration or source assembly/package identity emulation is promised.

## Capabilities

### New Capabilities

- `megamod-wanderer-join-offers`: contextual inspection of identified wanderer offers while
  preserving their native or admitted provider decisions, timing, visibility and lifecycle.

### Modified Capabilities

None. S108 rescuee consent remains a separate contract; only ordinary information formatting may
be composed directly. No patches against owned code.

## Impact

- [Research](../../../mods/megamod/docs/refinement/2026-11-05-wander-joins-plus-research.md),
  [coverage](../../../mods/megamod/docs/replacements/wander-joins-plus.md), this change and the
  sequential ledger. All implementation and native verification tasks remain open.
- Required game: RimWorld 1.7 beta. Harmony becomes required with the first Harmony implementation;
  no bundled Harmony or upstream assembly. No XML change is selected by this design; any later
  `XmlExtensions.*` use requires XML Extensions and its guarded-content contract in that slice.
- Royalty (`Ludeon.RimWorld.Royalty`) supplies the abasia event and is optional. Ideology
  (`Ludeon.RimWorld.Ideology`) and Biotech (`Ludeon.RimWorld.Biotech`) remain optional native context.
  Vanilla Events Expanded (`VanillaExpanded.VEE`, Workshop `1938420742`) is an optional provider,
  retaining its own required framework. Neither it nor Humanoid Alien Races is required for Core.
- `Ogliss.WanderJoinsPlus` (`2177016016`) is a read-only reference. If active, it owns the overlap
  and the owned enhancement is disabled with the documented startup warning; no foreign unpatching.
- Existing contextual letters and info cards; no new bottom tab, debug control, configuration
  setting, private pawn index, content Def or art. Seven baseline languages remain required.
