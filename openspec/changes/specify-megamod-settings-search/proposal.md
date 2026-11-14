## Why

The user clarified on 2026-11-14 that settings search must find individual options across **all
mods**, like Needle, without requiring a supported-mod list. This supersedes the initial owned-only
draft and extends the earlier name/help/feature/category requirement. Finding only a mod's settings
page is insufficient: selecting an option must reveal its original control.

## What Changes

- Specify S141 as cross-mod individual-option search in native Options → Mod options, composed with
  S047's page grouping/navigation and usable when S047 sorting is disabled.
- Discover all available active-mod settings pages automatically, including independent
  provider-generated pages, without package allowlists, per-mod adapters or registration prerequisites.
- Use Needle-style bounded page drawing and tab discovery for foreign controls; use existing
  registration metadata directly for megamod's own options.
- Search readable labels, help, displayed values, tabs, mod/page names, authors and package IDs;
  retain owned feature/category context. Reveal and highlight the original control without editing it.
- Report pending, incomplete and failed discovery honestly, preserving normal page access.
  A failed capture is a coverage gap, not a reason to redefine the scope as supported mods only.

## Capabilities

### New Capabilities
- `megamod-settings-search`: universal option discovery, query/results, native navigation, value
  provenance, lifecycle and failure handling.

### Modified Capabilities
The still-active S047 draft `megamod-mod-settings-navigation` is amended in its owning change to
compose its page-level search with S141 option results. Its former exclusion of opaque foreign
controls is superseded; grouping, distinct page identities and native persistence remain authoritative.
No duplicate capability delta is introduced here.

## Impact

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
This is specification work only; implementation remains paused. Common settings metadata, native
chooser integration and generic GUI capture will be affected later. Harmony remains the existing
dependency. No Needle binary/API dependency, source-settings migration, XML Extensions operation
or Gateway product dependency is introduced. Source inspection informs independent implementation.
