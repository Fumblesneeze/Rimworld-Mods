## Why

The user wants to see the stacks a pawn is actually hauling, including during movement, rather
than discover a batch only in an inventory list. The display must remain truthful as cargo changes
and must not grant extra carrying capacity.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: selected replacement subset, **S126 / dd.Armful Haul**, `dd.armfulhaul`, Workshop
**3808100512**, installed version **0.4.2**. **Specification only:** no gameplay implementation or
native acceptance is delivered. [Source audit](../../../mods/megamod/docs/refinement/2026-11-03-armful-haul-research.md).

## What Changes

- Show actual hauled stacks on supported moving haulers, with contextual quantities and individual
  item inspection. Keep personal supplies distinct from delivery cargo, including mixed stacks.
- Update presentation after actual pickup, partial delivery, transfer, interruption and loading a
  save. Do not show planned pickups as held cargo or disappear retained cargo after a failed drop.
- Compose directly with S021's physical cargo and capacity rules. Reuse native item appearance;
  do not adopt Armful's independent per-stack allowance to obtain its visual effect.
- Propose extra-stack drawing on by default, a small bottom-right view toggle, and its default
  through common global/save settings. Contextual cargo information remains available when drawing
  is off. No new bottom tab; no pawn-specific cargo choice in mod settings.
- Preserve native single-item/pawn carrying presentation and avoid duplicate cargo pictures or
  commands. Unsupported drawing falls back visibly and warns at startup without modifying cargo.
- Record broader Armful hauling, construction, ingredients, loading and prison-food behavior for
  later selection; this change does not silently adopt them. Initial pile spacing is adaptive;
  upstream's extreme spacing slider is not selected pending visual evidence.

## Capabilities

### New Capabilities

- `megamod-visible-haul-cargo`: accurate carried-stack drawing, contextual inspection, lifecycle
  freshness, view controls and safe compatibility fallback.

### Modified Capabilities

None in archived capabilities. S021 owns physical cargo, recovery and capacity; S125 owns
opportunistic hauling; S133/S135 own later loading/work-priority composition. This capability
presents their actual supported cargo and does not extend their work eligibility.

## Impact

Future work affects pawn carrying visuals, contextual cargo selection/inspection and common
settings. Harmony is a **required future runtime dependency** for the selected native drawing/UI
seams. Armful, PUAH forks, custom renderers, custom inventories and storage providers are **optional
external providers**, not required dependencies. No upstream assembly or raster asset ships.
No XML mutation is currently selected; any later XML Extensions use requires its dependency and
guarded, merge-preserving operations. No repository-owned runtime dependency or Gateway reference.

Co-installation must select one renderer for overlapping cargo without altering other mods'
patches or changing physical ownership. No source package impersonation, binary facade, active-job
or settings migration is promised. All patch target specs, focused tests, independent code review,
native player workflows and live visual acceptance remain future work.
