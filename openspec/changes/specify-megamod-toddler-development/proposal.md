## Why

Toddlers should gradually become independent while still needing adult attention, protection and
care. Replacing Toddlers requires the whole childhood experience, with the user's required
temperature awareness, rather than just adding mobility or reproducing its conflicting care paths.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This change **specifies future behavior only**; it implements none.

## What Changes

- Specify S031 Toddlers: early-childhood learning, food, independent play, attention, crib use,
  adult care, clothing, mood, carrying, travel, kidnapping and abandoned-child arrivals/adoption.
- Include its functional baby clothing and developmental presentation. These support the selected
  mechanic; this does not broaden megamod into an unrelated content collection.
- Compose directly with S030's mandatory toddler temperature prevention and escape. Care, play,
  dressing and caravan handoffs must use that same safety policy.
- Correct documented source defects: unsafe drops, stale adult/child pairings, interrupted bath
  rewards, frame-dependent needs, ineffective food-mood suppression and overly broad suppression
  of foreign mental states. Record every intended deviation and provisional tuning value.
- Keep entity policies in existing contextual/Assign/Schedule controls and actual settings in the
  common global/save UI. Developer overrides belong in the top debug controls. Add no bottom tab.
- Specify optional hygiene, alien-race and clothing-taxonomy outcomes without claiming universal
  compatibility or silently activating code written for the original package identity.

## Capabilities

### New Capabilities

- `megamod-toddler-development`: lifecycle, learned abilities and developmental presentation.
- `megamod-toddler-care`: feeding, mess, play, loneliness, cribs and safe adult assistance.
- `megamod-toddler-apparel-mood`: clothing availability, dressing, protection and toddler episodes.
- `megamod-toddler-travel`: carrying, passengers, noncombatant vulnerability and refugee adoption.
- `megamod-toddler-integrations`: optional hygiene, alien development and clothing-provider outcomes.
- `megamod-toddler-configuration`: settings, entity controls, lifecycle, coexistence and unavailable behavior.

### Modified Capabilities

None. S030's drafted environmental and toddler safety contracts remain authoritative; this change
uses them without redefining their thresholds, reaction bounds or allowed-area recovery policy.

## Impact

- Source reference: **Toddlers**, `cyanobot.toddlers`, Workshop **2903359152**, pinned in
  [research](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-research.md).
  Original 1.6 support is not evidence of 1.7 support. No upstream code, assemblies or art are copied.
- Biotech is **required for this feature**, optional for unrelated megamod features. Without it,
  toddler behavior and controls are unavailable with an explanation; megamod can still load.
- Future Harmony integration requires **Harmony** (`brrainz.harmony`, Workshop 2009463077).
  Planned guarded XML operations require **XML Extensions** (`imranfish.xmlextensions`, 2574315206)
  as a required runtime dependency once those operations ship; neither dependency is added by prose.
- Dubs Bad Hygiene, Humanoid Alien Races, Progression: Attire and other content providers are
  **optional**, individually admitted integrations. Hospital, Altered Carbon, Celsius, GeneSeed
  and sound-provider details remain explicit research gates, not promised working adapters.
- The original provider and its unofficial ports are inspection inputs, not dependencies. No
  automatic source removal, source-save conversion, package impersonation or drop-in ABI claim.
- All implementation, tests, patch-target documents, native 1.7 compatibility, asset provenance,
  localization and live acceptance remain pending. Open technical questions are retained as tasks.
