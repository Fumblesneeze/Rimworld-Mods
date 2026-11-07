## Why

Ordinary steel furniture and power infrastructure should not sustain fires merely because their
materials are treated as combustible. S104 replaces No Burn Metal with explicit material and
infrastructure choices, while preserving independent fire hazards and the rest of material balance.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S104/P2-34, No Burn Metal** (`Unon.NoBurnMetal`). This change specifies later
work; it delivers no gameplay or accepted compatibility.

## What Changes

- Remove selected materials' contribution to objects' flammability and the ordinary flammability
  of selected conduits and switches. Preserve source defaults and discover other metallic materials
  with protection off initially. Already nonflammable contributions need no redundant toggle.
- Explain the boundaries: loose resources, floors, fuel consumption, direct damage and unrelated
  components are not blanket-protected just because an object contains metal.
- Provide searchable, provider-grouped choices through common global/save settings. Make bulk
  scope explicit, preserve inherited choices, and show whether configured behavior is available.
- Apply changes to existing objects without restart or reconstruction, with correct save switching
  and truthful native stat inspection. Do not restore stale captured values over other mods' work.
- Preserve independent material properties and compose directly with owned fire, damage, recipe
  and inspection features. Do not retain patches against our own implementations.

## Capabilities

### New Capabilities

- `megamod-metal-fire-resistance`: Material and infrastructure fire behavior, discoverable choices,
  inherited settings, truthful presentation, safe lifecycle and compatibility fallbacks.

### Modified Capabilities

None. Existing fire-response, structural-damage, recipe-order and inspection contracts continue to
use current game behavior; they do not acquire new immunity or a second settings system.

## Impact

Core is required. Harmony becomes required when the guarded native stat adapters are implemented.
Anomaly and the six advertised material providers are optional, only for their actual content; none
is needed for Core behavior. If guarded provider XML operations are shipped, XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) is required from that first use. No dependency
metadata or runtime package changes occur in this documentation slice.

The original provider owns the overlapping feature when active; our overlap remains inactive with
an explanation. We do not impersonate its package, assembly, public classes or global settings file.
Source settings are not silently imported. No code, binaries or artwork from the GPL-3.0 reference
are copied into the product. Research retains Unon/Nonu, Dr Zhivago and Antyhc attribution and exact
installed/source identities.
