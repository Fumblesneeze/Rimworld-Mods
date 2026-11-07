## Why

S055 selects Meals On Wheels - Continued so a hungry colonist can use reachable carried food when
ordinary food selection finds nothing. The replacement must complete a real transfer and meal,
respect diets and other work, and compose safely with our shared hauling/inventory ownership.

## What Changes

- Specify fallback food sharing from other colony pawns, preserving existing native animal access
  and every successful ordinary food choice.
- Retain the source's near-spoilage priority, with complete candidate inspection and predictable
  food/distance ranking instead of arbitrary pawn/inventory order.
- Specify an optional-content passenger-shuttle fallback using a real retrieval workflow; searching
  must never unload food or return an untransferred remainder as the usable portion.
- Preserve native eating permissions, physical quantities, shared capacity and interruption safety;
  integrate hauling, food condition and later meal carrying through direct owned policies.
- Record the actual Immersive Chefs consumer and limits of source/save compatibility; add no tab or
  inert sample setting.

## Capabilities

### New Capabilities

- `megamod-shared-carried-food`: Fallback food discovery, safe borrowing/retrieval, native eating
  and direct shared-inventory composition.

### Modified Capabilities

None. S021's cargo attribution, S024's food preferences, S041's taint quantities and S123's future
meal carrying retain their contracts; this change defines their food-sharing intersection.

## Impact

One owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Specification only; no runtime implementation or acceptance.

Core and Harmony (`brrainz.harmony`) are required when runtime patches are implemented. Odyssey and
its passenger-shuttle content are optional; pawn sharing must work without them. External inventory
and food providers remain optional, shape-admitted integrations. No product dependency on another
repository mod or Dev Gateway, no source assemblies/identities shipped and no automatic migration.
No XML mutation is planned; actual later XML Extensions use requires its hard dependency and guards.
