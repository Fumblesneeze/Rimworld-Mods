## Why

Repeated rotation inputs make furniture placement tedious, but automatic facing must respect a
player's deliberate adjustment. The megamod should orient seats and supported facilities sensibly
while preserving manual rotation until the cursor enters another cell.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S048 / P1-42**, Auto-Facing Chairs (`scarf.autofacingchairs`, Workshop3783758781),
with the user's explicit same-cell manual-rotation requirement. This change **specifies future
behavior only**; no implementation or live compatibility acceptance is delivered.

## What Changes

- Suggest chair facing from actual chair-use positions and adjacent eating surfaces, including
  built furniture, blueprints and frames.
- Retain the source's one-way facility orientation assistance, deterministic tie handling and
  unchanged orientation when target requirements conflict. Placing a bed near its facility is not
  a reason to rotate that bed.
- Preserve every manual adjustment within the current cell, including subcell mouse motion and
  repeated updates; entering another cell permits a new suggestion.
- Support ordinary new-build, install and reinstall placement consistently. Install/reinstall is
  an explicit extension beyond the inspected source's build-only hooks.
- Preserve actual native placement legality, blueprint orientation, work/cost, item identity and
  facility-link rules. A helpful preview is not a promise of a usable facility bonus.
- Move the source's Chairs only option, default off, into common global/save settings. Keep cursor
  rotation as transient placement state with native controls and no extra gameplay tab.
- Guard the precise placement/rotation semantics and preserve native/manual fallback, compatible
  content and unrelated patches; use direct owned composition with the Architect/builder features.

## Capabilities

### New Capabilities

- `megamod-furniture-orientation`: Contextual seat/facility orientation, same-cell manual control,
  ordinary placement lifecycle, common scope configuration and compatibility fallback.

### Modified Capabilities

None. Existing Architect and replacement-building contracts retain their command eligibility and
native placement behavior; this feature supplies the orientation suggestion for supported paths.

## Impact

Future owning code: narrow native placement integration, bounded native/shared spatial queries and
one common settings row. Core and **Harmony (`brrainz.harmony`)** required when implemented. DLC,
modded furniture and the source provider are optional. An independent C# implementation needs no
FSharp.Core runtime dependency, XML patch or XML Extensions usage for this feature. Do not copy
source/library/Gateway assemblies, spoof source identity or introduce a repository-mod dependency.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-auto-facing-chairs-research.md) records
complete source/decompilation, the misleading binary revision, native1.7 seams, source coverage and
remaining proof obligations. Same-cell suppression already exists in inspected source; this draft
does not label it a reproduced upstream bug or claim blanket CE/beta/provider compatibility.
