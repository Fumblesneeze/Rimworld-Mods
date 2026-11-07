## Why

Players want visiting trade parties to gather where commerce belongs in their colony. Preserve
Trading Spot's gathering behavior while making it cooperate with danger, departure, deliveries and
the user's dedicated trading area instead of repeatedly forcing every party back to a marker.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Source S039, **[PL] Trading Spot**,
`tradingpost.programmerlily.com`, Workshop `3507108484`, is a read-only reference.

## What Changes

- Specify which visiting trade parties and optional ordinary visitors use the map's chosen trading
  destination, how their existing roles remain intact, and how already-present parties respond.
- Preserve ordinary spawn locations, physical travel, trading, cargo, prices, visit duration,
  danger reactions and departure. A destination preference cannot override a departure or emergency.
- Specify missing/unreachable destinations, map isolation, save/load, original-provider coexistence
  and the shared global/save visitor preference.
- Compose S027 visitor deliveries directly: couriers may leave gathering temporarily, return when
  still visiting, and follow ordinary departure when the visit ends.
- **BREAKING relative to the source:** the physical one-cell TradingSpot building, automatic
  destruction of an old marker and its construction-work setting are not adopted. The user chose
  a dedicated area, owned by the separately tracked **S040 original feature**.

This change delivers **specification only**. It owns the source-derived gathering policy; S040
owns the area footprint, designation controls, gizmos and detailed cell selection. The integrated
feature cannot be accepted before both contracts and their real player workflow are implemented.

## Capabilities

### New Capabilities

- `megamod-trader-gathering`: eligible visiting parties, preserved roles/lifecycle, preferred
  destination changes, visitor settings, delivery composition and compatibility fallback.

### Modified Capabilities

None. S027 retains delivery rules and the foundation retains UI/settings/admission contracts.

## Impact

- Future map-local party policy and narrow native lifecycle integration. Harmony becomes a required
  dependency only when the selected implementation uses its types; no source DLL is bundled.
- Original Trading Spot, Hospitality and other destination providers remain optional. No DLC is
  required. A cooperative optional route needs package/shape evidence; original co-installation
  withholds overlapping owned redirection, without unpatching or uninstalling it.
- No new bottom tab or source-marker assets. Global visitor preference goes in mod settings;
  area/entity state remains contextual under S040. No duplicated Thing cache.
- No XML modification is proposed by this gathering slice. Any future external Def/XML adaptation
  requires XML Extensions package/content guards and merge preservation, with its actual dependency
  declared in the implementing slice.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-trading-spot-research.md) and
  [replacement report](../../../mods/megamod/docs/replacements/trading-spot.md) record the full
  installed source, native lifecycle, defaults, source defects, real consumers and deliberate omissions.
