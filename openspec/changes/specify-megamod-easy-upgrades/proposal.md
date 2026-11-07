## Why

Colonists should be able to convert a supported building without discarding its useful structure,
and spend material and skilled work on a single attempt to improve an existing item's quality.
S082 selects Easy Upgrades (Continued), with explicit accounting, progression and preservation rules.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**; origin replacement **S082/P2-11**. Specification only.

## What Changes

- Offer researched building conversions with a visible additional-material or refund preview,
  preserving compatible choices and safely returning recoverable contents at completion.
- Offer one paid quality-reworking attempt on eligible buildings, weapons, apparel and sculpture;
  disclose improvement, deterioration and unchanged outcomes; the user-selected initial ceiling is
  Masterwork, with ordinary Legendary creation/acquisition unaffected.
- Correct source paths that can lose state, bypass work eligibility, consume the wrong physical
  quantity, duplicate fuel returns or continue after cancellation. Compose directly with owned
  replacement, quality construction, repair knowledge, production and hauling behavior.
- Keep contextual commands and Architect orders; put actual configuration in the common inherited
  global/save settings. Do not add a bottom tab or an automatic quality-grinding loop.
- Record all source conversion families, a demonstrated Colony Manager Redux consumer, migration
  limits and unsupported provider paths rather than claim universal replacement compatibility.

## Capabilities

### New Capabilities

- `megamod-building-conversion`: explicit differential-cost conversions, safe transfer, interruption,
  optional conversion boundaries and compatibility admission.
- `megamod-quality-reworking`: one-attempt quality jobs, material/work/outcome rules, shared settings
  and integration with existing production, repair and quality policies.

### Modified Capabilities

None. S028's full-build and material-replacement workflows retain their own prices and quality
rules. This adds a separately labeled conversion action, not a cheaper implicit replacement path.
Reworking that reaches a quality-construction target uses S013's existing achieved-target closure;
later rework deterioration does not automatically start a new construction run.

## Impact

Future feature-local work/orders and guarded declarative additions to existing content. Core and
**XML Extensions** (`imranfish.xmlextensions`, Workshop2574315206) are required when those additions
are implemented. Harmony becomes required only if actual native bridges use it; source inspection
does not establish a need to patch every quality-bearing object. Original Easy Upgrades, VGP,
VFE, Medieval Overhaul, Colony Manager Redux and DLC remain optional. No source assembly/assets,
Gateway, or other repository-mod runtime dependency is introduced.

No gameplay, package change, build or installation is delivered. See the
[research journal](../../../mods/megamod/docs/refinement/2026-11-04-easy-upgrades-research.md)
for evidence, deviations and unresolved work.
