## Why

Make Rimworld Great Again has many common settings. The previously agreed search across setting
names, descriptions, feature names and categories needs an observable contract. Needle provides a
useful reference for finding an individual option and navigating directly to its existing control.

## What Changes

- Specify S141, Needle-inspired search within megamod's one structured settings page.
- Search all registered owned settings, including sections outside the current viewport; show the
  matching setting, category/feature path, current scope/value and unavailable reason where relevant.
- Reveal and highlight the actual setting without changing it, with current save/inheritance guards.
- Use owned registration metadata rather than replaying settings draws to discover controls.
- Keep S047's other-mod page-name/author chooser separate. Individual options inside other mods,
  Needle's source APIs, hidden page crawling and source-settings migration are outside this draft.

## Capabilities

### New Capabilities
- `megamod-settings-search`: localized option search, result navigation, live scope/value presentation,
  lifecycle and failure behavior within the common settings UI.

### Modified Capabilities
None. This gives the 2026-11-02 owned-settings search decision its own contract; S047 still handles
page navigation without inspecting opaque foreign controls.

## Impact

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
This change specifies later behavior only; it delivers no runtime implementation or acceptance.
It extends the owned common settings renderer/metadata and composes directly with S047. Harmony is
already a required product dependency; no additional external runtime dependency, XML Extensions
operation, Needle dependency or Gateway product reference is introduced. Needle is read-only
research inspiration. Other requested additions remain separate intake entries.
