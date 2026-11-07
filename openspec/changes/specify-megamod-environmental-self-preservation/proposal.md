## Why

Colonists and animals should react to environmental injury before ordinary work keeps them in
danger, and mobile toddlers should avoid dangerous temperatures and get themselves to safety.
Brrr and Phew supplies the baseline intent, but its actual destination, recovery and provider
behavior needs explicit contracts rather than copying its bugs or relying on its name.

## What Changes

- Specify cold, heat, toxic, blood-rain and supported breathing-hazard responses, including animals,
  reachable refuge, actual exposure, ordinary recovery and food/recreation interruptions.
- Treat independent hazards independently, recheck changed refuges, prevent invalid destinations
  and bed theft, and use real native swimming when it is a suitable cooling activity.
- Specify short safe outdoor breaks, source-derived need/duration values and contextual reports.
- Include the user's required toddler prevention and escape behavior directly, preserving mobility,
  crib/door restrictions and adult rescue when a toddler cannot reach safety. Full Toddlers remains S031.
- Put configuration in the common global/save settings UI, persist all values, and document optional
  providers and source co-installation. No new bottom tab or source-identity impersonation.
- Record source differences, actual incoming consumer bindings and deferred research. This change
  **specifies future behavior only**; no gameplay implementation or acceptance is delivered.

## Capabilities

### New Capabilities

- `megamod-environmental-recovery`: early hazard response, physical refuge and recovery, ordinary
  interruptions, settings, optional providers and startup fallback.
- `megamod-outdoor-breaks`: safe walking/skygazing breaks for low outdoors need with explicit
  durations, completion, reports and ordinary schedule/needs constraints.
- `megamod-toddler-temperature-safety`: proactive idle temperature avoidance, early escape,
  actual toddler mobility and bounded no-route/caregiver behavior.

### Modified Capabilities

None. Direct composition uses the existing foundation settings/admission rules and respects S002
directed recreation and S024 Common Sense; no patches against owned features are retained.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source **S030 / P1-24**, `Mlie.BrrandPhew`, Workshop **2195938471**; see
[research](../../../mods/megamod/docs/refinement/2026-11-02-brrr-and-phew-research.md).

Core recovery does not require the original mod or any DLC. **Anomaly**, **Odyssey**, **Biotech**
(toddler life-stage features), **Toddlers**, **More Events** and **Vanilla
Temperature Expanded** are optional providers for their specific branches, with exact admission
and native proof still required. Owned S031 toddler behavior will compose directly when implemented.
Original Brrr and Phew co-installation withholds overlapping owned recovery/outdoor behavior.

Future integration touches native job selection, rest/swimming and temperature/hazard queries.
Harmony becomes required when eagerly used. Any XML Extensions operations used for guarded
additive ThinkTree/content edits require `imranfish.xmlextensions` (Workshop **2574315206**) in the
same implementation slice. No runtime dependency metadata is changed by this draft. Other
repository products and the Dev Gateway remain absent from product dependencies and packages.
