## Why

Grazers can exhaust individual plants even when a small amount of food would suffice. Graze up
offers persistent pasture, configurable buried-plant access and plant longevity; these need explicit
rules for the megamod, including finite food accounting and honest settings labels.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S043 / P1-34**, `[Kit] Graze up` (`kittahkhan.grazeup`, Workshop 2302739121).
This is **specification only**, with no implemented or accepted gameplay.

## What Changes

- Specify proportional plant consumption leaving configurable growth, with optional destructive
  species and the source's separate regrowing-plant and destruction nutrition bonuses.
- Retain configurable minimum growth and access through snow **and sand**, explicitly including
  the source's leafless-plant grazing and supported tool users eating rooted plants in place.
- Correct negative/zero-progress bites and invented nutrition; recheck actual plants before a bite
  and preserve ordinary diet, access, reservations, ingestion effects and harvesting.
- Specify configurable finite plant lifespan and a soft floor independently of grazing.
- Put all policy in shared global/per-save settings with accurate bonus/total labels and safe
  species-list persistence. Keep plant growth and age as ordinary saved world state.
- Compose nutrition/damage directly with [S087 Grazing Lands](../specify-megamod-grazing-lands/proposal.md),
  retaining separate plant-lifespan behavior, provider checks and later growth/longevity verification.

## Capabilities

### New Capabilities

- `megamod-grazing`: Growth-based bites, eligibility, destructive species, rooted ingestion and policy.
- `megamod-plant-lifespan`: Finite lifespan scaling, soft floor and independent shared policy.

### Modified Capabilities

None published. S087 revises this unarchived grazing draft in place and owns the one combined
amounts/damage contract; it must not stack patches against our own grazing or lifespan implementation.

## Impact

Future owning code: plant ingestion/eligibility and lifecycle adapters, shared settings, native
job preparation and optional-provider admission. Core and **Harmony (`brrainz.harmony`)** are required
for the proposed runtime hooks; declare Harmony when those hooks are implemented, never bundle it.
Original providers and DLC are optional. XML Extensions is not used by this design; if later XML
patches are needed it becomes required with guarded merge-preserving operations. The Dev Gateway
remains developer-only and never a runtime dependency.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-graze-up-research.md) records the selected
1.7 source and DLL, exact native comparison, formulas, public docs and consumer limits.
[Replacement coverage](../../../mods/megamod/docs/replacements/graze-up.md) distinguishes drafted,
omitted and not-yet-implemented behavior. No safe source-save migration or universal compatibility
claim is made.
