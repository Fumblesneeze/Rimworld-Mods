## Why

Travelers carrying usable beds should be able to sleep in them and recover the same physical beds
afterward. Specify Use Bedrolls - Continued while correcting unsafe transfers, lost home assignments
and repeated deployment, and integrating its behavior with personal supplies and shared hauling limits.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S073 is a replacement reference, not a co-owner.

## What Changes

- Deploy an eligible carried, ground or genuinely spare shared portable bed for ordinary rest,
  preferring nearby indoor placements. Recover it through physical work after the rest episode.
  Preserve permissions, actual occupancy, capacity and manual changes throughout interruption/load.
- Redistribute spare carried beds on caravan entry using each recipient's needs and capacity without
  overriding the selected unloading mode or loading commitments. Keep world-caravan sleep native.
- Retain home-bed claims across travel by default under the composed S115 ownership policy. An
  optional release setting remembers left-behind claims per map/allowance and reclaims valid slots
  on return. Preserve multiple owners, other home maps and later applicable player choices.
- Add single/double bedroll bills at both tailoring benches using native products/materials/quality.
  Proposed work correction: 600/1100 base work, matching current construction anchors rather than
  the source's older 350/600 values. Do not change construction costs or bed stats.
- Put configuration in common global/save settings and entity actions on contextual bed/pawn controls.
  Preserve one provider, bounded compatibility fallback and safe physical recovery of owned state.

This is a **specification-only draft**. No gameplay, migration or compatibility is implemented or accepted.

## Capabilities

### New Capabilities

- `megamod-portable-beds`: rest eligibility, physical deployment/sharing/recovery, caravan distribution,
  personal-supply composition, common configuration and compatibility lifecycle.
- `megamod-home-bed-memory`: travel memory, valid slot reclaim and explicit player control.
- `megamod-bedroll-tailoring`: native bedroll production at tailoring benches with guarded content merging.

### Modified Capabilities

None. S062 remains the owner of personal supply targets and real carried counts; this change defines
temporary bed deployment as a separate retained commitment, not another carried unit or target.

## Impact

- Source **Memegoddess.UseBedrolls**, Workshop **3534169351**. Primary documentation, selected binary,
  MIT attribution, hashes and native evidence are in
  [research](../../../mods/megamod/docs/refinement/2026-11-04-use-bedrolls-research.md).
- Future required dependencies: **Harmony** and **XML Extensions** (`imranfish.xmlextensions`,
  Workshop2574315206). Original providers, other medical/ownership/content mods and DLC are optional.
- Compose directly with S021/S126 capacity, S062 personal supplies/field care, S061 roof-safe removal
  and native timetables. Preserve S060's exclusion of already-built installation from enclosure
  protection. [S115 ownership](../specify-megamod-bed-ownership/) supplies the common ownership policy
  and seat fallback after eligible deployment. S135 loading remains later research.
- No source identity spoofing, copied assembly, automatic source-save/settings import, private Thing
  index, new physical bed/art, dependency on another repository product or product Gateway dependency.
