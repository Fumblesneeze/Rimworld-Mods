## Why

Travel should not silently erase a pawn's home bed, and assigning beds should make owners and
partners easy to find. Specify Sensible Bed Ownership together with the already drafted portable
bed behavior so travel, temporary sleep and deliberate player assignments follow one policy.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S115 is an external reference, not an owner.

## What Changes

- Retain one ordinary bed per map and, where supported, a separate substructure bed on that map;
  keep deathrest assignments distinct. Prefer a chosen local bed, otherwise the ground-based bed.
  Preserve legitimate off-map ownership for bedroom information without attempting remote sleep.
- Make search, partner ordering, contextual assignment, bulk assignment, unassignment and owner
  navigation available through ordinary assignment controls. Preserve restrictions and other owners.
- Extend legitimate sharing and relationship/status changes across recorded assignments; handle
  moving, packed, destroyed and reloaded beds without duplicate claims or stale ownership.
- Combine directly with S073: **retain home claims by default**, changing that draft's source-derived
  release default to false. Optional release uses its remembered-claim/reclaim controls. Temporary
  portable beds never erase a different map's home choice; applicable player choices supersede memory.
- Prefer a nearby permitted comfortable seat when ordinary rest would otherwise use the ground,
  after eligible portable-bed deployment. Retain normal comfort and rest effects, not new bonuses.
- Use shared settings and native context. Correct source-wide reservation shortcuts, fragile patch
  matching, global search state and unsafe partial/group assignment rather than reproducing them.

**Specification only.** No runtime behavior, migration, art or compatibility is delivered or accepted.

## Capabilities

### New Capabilities

- `megamod-bed-ownership`: map-specific ownership, gravship preference, contextual assignment,
  partner/lifecycle handling, seat-rest fallback and portable-bed composition.

### Modified Capabilities

None in the archived baseline. The unimplemented `specify-megamod-portable-beds` draft is corrected
in place for the shared release default, map-specific remembered intent and seat-rest ordering.

## Impact

- Source: **Sensible Bed Ownership**, `sensiblebedownership.1trickPwnyta`, Workshop3328702448,
  modVersion1.3.0, selected1.6 payload. Exact public/source/binary/native evidence and limitations:
  [research](../../../mods/megamod/docs/refinement/2026-11-06-sensible-bed-ownership-research.md).
- Future required dependency: Harmony when patches are implemented. Biotech, Royalty, Ideology,
  Odyssey and external assignment/content providers are optional; absent content grants no feature.
  No XML changes selected, so no new XML Extensions dependency for this slice.
- Direct composition with S073 portable beds/home memory, S062 physical supply commitments and
  existing safety rules. S135 caravan loading remains later research. Share native/map data.
- No source package spoofing, assembly/texture reuse, automatic legacy import, other repository-mod
  runtime dependency, Gateway dependency or blanket drop-in claim.
