## Why

A large mod list makes settings difficult to find by title alone. Mod Options Sort supplies useful
author grouping and search; the megamod should retain that navigation while keeping every settings
page with its actual owner and handling missing metadata or optional providers safely.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld1.7 beta**.
Origin: replacement, **S047 / P1-40, P2-30**, Mod Options Sort (`Superniquito.ModOptionsSort`,2910865748).
This change **specifies future behavior only**; no runtime implementation or compatibility acceptance.

## What Changes

- Sort existing mod-settings entries by author groups or by name, with optional grouping of single
  entries, special source group conventions and correctly identified HugsLib-generated menus.
- Search titles, mod names, visible groups and original author information without losing independent
  settings entries that happen to share a title or package.
- Preserve the actual native/provider settings page and save-on-close lifecycle; avoid importing
  arbitrary foreign controls into the megamod's own settings schema.
- Keep sort controls as transient browsing state with source defaults; localize labels and provide
  safe missing-metadata/icon behavior and bounded diagnostics for unavailable integrations.
- Keep one megamod entry opening its structured settings, with common global/save controls inside it.
  Do not recreate a list of legacy source settings pages or add a gameplay tab.
- Explicitly record the stale advertised yellow/blue highlights as absent from selected1.7 code,
  without claiming replacement coverage for an unproven feature.

## Capabilities

### New Capabilities

- `megamod-mod-settings-navigation`: Complete settings-entry grouping, search, identity, provider
  navigation, transient browsing state and graceful unavailable-provider behavior.

### Modified Capabilities

None. Foundation global/save settings and UI-placement contracts remain authoritative for owned pages;
this change specifies navigation of the outer native settings chooser. The 2026-11-14 S141 decision
extends that chooser with individual-option discovery across all mods, without a supported-mod
whitelist. This active draft is reconciled with S141's query/reveal contract; page identity, grouping
and original owner persistence remain here.

## Impact

Future code: guarded native chooser integration, per-dialog metadata projection and narrow optional
HugsLib adapter. Core and **Harmony (`brrainz.harmony`)** required when implemented. HugsLib and its
consumers are optional; no DLC or XML Extensions operation required. Active original Mod Options Sort
triggers a bounded startup warning and best-effort ordinary feature-off handling for the owned chooser,
without modifying the original or stored preferences. No original API/drop-in or coexistence work is
included; required native and deliberately used HugsLib navigation checks remain.
No source assembly, private Harmony, Gateway or repository product dependency is added.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-mod-options-sort-research.md) records full
selected1.7 decompilation, native1.7 behavior, the inspected older HugsLib contract, source defects,
unknown reuse license and explicit coverage gaps. No blanket fork/beta compatibility is implied.
