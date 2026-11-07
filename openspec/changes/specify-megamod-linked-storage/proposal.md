## Why

Players can keep shelf settings synchronized but must maintain ordinary stockpile zones separately.
The requested original feature extends the same interaction to zones and mixed shelf/zone groups,
so one warehouse can have one coherent storage policy.

## What Changes

- Specify linking, shared filters and priority, unlinking, group identification and persistence.
- Preserve each member's physical storage restrictions, even when a linked ground stockpile can
  accept things its shelves cannot.
- Compose later presets and refill controls with this group rather than introducing parallel links.
- Record native 1.7 behavior and remaining integration research separately from player requirements.

This change delivers specification documents only. No gameplay behavior is implemented or verified.

## Capabilities

### New Capabilities

- `megamod-linked-storage`: Native-style shared storage policies for stockpiles and shelves.

### Modified Capabilities

None. The foundation's settings, admission and shared-data contracts remain applicable.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: original, intake P1-06 / S005. Affects storage selection, hauling and bill destination use.
Core is required. No third-party source mod is required. Harmony becomes required if the eventual
implementation uses its types; XML Extensions becomes required if any of its operations are used.
Optional storage providers require individually inspected adapters; this proposal makes no blanket
compatibility claim. Growing-zone and workbench linking have separate owning changes.
