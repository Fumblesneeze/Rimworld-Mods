## Why

Visiting groups should arrive dressed for the map's conditions instead of predictably collapsing
before they can trade or leave. P1-05/P1-39 select Dress for the Weather (Continued), including
optional ordinary raider preparation and survival protection for all humanlike vacuum arrivals.

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This change specifies future behavior; it implements nothing yet.

## What Changes

- Prepare neutral incident groups for the arrival map's outdoor temperature, within faction,
  apparel, material, body and replacement restrictions.
- Specify pollution/toxic-weather protection and optional-DLC vacuum clothing separately from
  ordinary hot/cold clothing, with their eligibility and precedence visible in settings descriptions.
- Keep ordinary temperature/pollution preparation for raiders off by default; all-humanlike vacuum protection remains independent.
- Preserve two distinct clothing filters using shared global defaults and save overrides.
- Require bounded preparation, current comfort checks, no needless repeated replacement, and
  truthful fallback when legal clothing cannot protect a pawn.

## Capabilities

### New Capabilities

- `megamod-weather-ready-arrivals`: environment-appropriate apparel for newly arriving incident groups.

### Modified Capabilities

None. Foundation settings, startup admission and later staged generation are composed capabilities.

## Impact

Source: `Mlie.DressForTheWeather`, Workshop `3607724833`; see
[research and coverage](../../../mods/megamod/docs/replacements/dress-for-the-weather.md).
The original provider is not required. Harmony is required by the proposed arrival integration when
implemented. Biotech and Odyssey are **optional**: missing content removes only its hazard-specific
equipment path. An external spore-weather provider is optional and must supply its expected content.
Combat Extended compatibility is an upstream author claim, not verified megamod support.
No XML Extensions operation, new item, autonomous colonist outfit system or new bottom tab is proposed.


Decisions10/11 select survival gear over conflicting protected armor and broaden vacuum preparation
to every humanlike spawning on an applicable map, including supported Odyssey/SOS2 environments.
Existing NPC arrivals may receive protection; existing player-owned pawns must bring real gear.
Reuse S130's shared apparel facts without treating policy membership as adequate protection.
