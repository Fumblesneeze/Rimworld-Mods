## Why

Large content lists should remain practical to navigate. The content-type and source-mod filters in
our Cherry Picker replacement need searchable, scrollable selectors that preserve the intended
selection and remain readable when the player widens the window.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, queue **S020**, **Cherry picker scroll bars**, `lucius.cherryscroll`.
**Specification only:** no runtime behavior or in-game acceptance is delivered.

## What Changes

- Specify both selectors, case-insensitive all-word search, visible matching counts, empty results,
  scrolling, resizing into columns, source icons and exact selection/cancellation behavior.
- Compose them directly into the future owned content-configuration page. This selected add-on
  receives its own change; broader Cherry Picker behavior and contracts remain queued as S038.
- Propose identity-safe source selection when names collide, readable full labels and visibly
  unavailable choices, correcting limitations found in the source without inventing new settings.
- Keep browsing transient and separate from changing enabled content. No extra bottom tab, generic
  dropdown replacement or patch against our own implementation.

## Capabilities

### New Capabilities

- `megamod-content-filter-selectors`: contextual searchable content-type/source selectors,
  responsive navigation, exact choice identity and safe composition with content configuration.

### Modified Capabilities

None. S038's full content-removal/configuration contract is not drafted by this change.

## Impact

Future work affects the megamod content-configuration UI only. It changes no item availability,
research, costs, jobs or saves by browsing. Actual content edits, their scope and their application
timing belong to S038. Selector navigation is transient UI state, not a global/save setting.

This owned UI slice needs no new external runtime dependency or Harmony/XML patch. The source's
Harmony and Cherry Picker requirements describe its adapter, not a requirement to patch that mod
again. If integration later needs a patch, its owning target spec and semantic admission must precede
implementation; XML Extensions becomes required if an XML Extensions operation is introduced.
No external adapter, original package impersonation or automatic source settings migration is promised.
The source add-on can remain attached to its original provider without modifying our owned controls;
S038 must settle coexistence of the underlying content configuration before activation.

[Research and recorded questions](../../../mods/megamod/docs/refinement/2026-11-02-cherry-picker-scrollbars-research.md)
retain provenance, consumed callbacks, proposed corrections and remaining native verification.
