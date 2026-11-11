## Why

A definition-loading warning can identify a bad value without identifying the definition or file
being processed. S094 makes that context inspectable alongside the original diagnostic, so a player
can locate the affected content without guessing which mod caused the underlying problem.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S094/P2-23, Log After Def Error (Continued). Initial implementation
and bootstrap composition are implemented-unverified; remaining integration and native acceptance
obligations are open.

## What Changes

- Add the current definition name/type, source mod identity and XML file to accepted diagnostics
  emitted during definition conversion, with explicit unknown fields where provenance is unavailable.
- Cover both warnings and errors; the inspected continuation currently decorates warnings only.
  Identify the definition being processed, not an unproven responsible patch or mod.
- Keep one ordinary diagnostic occurrence with unchanged original text, severity and available
  trace. Context follows its capture, filtering, copy and announcement behavior through S035.
- Preserve correct context across nested/concurrent loads, exceptions and reloads without stale
  attribution. Add one enabled-by-default, process-wide setting with a documented scope exception.
- Reuse native diagnostic UI and top developer controls; do not add a menu/tab, upload logs,
  repair malformed content, suppress failures or migrate source configuration.

## Capabilities

### New Capabilities

- `megamod-definition-diagnostics`: truthful definition/source context attached to ordinary loading
  diagnostics, safe scope lifecycle, logging composition and independent compatibility admission.

### Modified Capabilities

None. S035's capture and presentation contract remains authoritative; this separately owned feature
composes directly with that channel instead of duplicating or patching the owned logger.

## Impact

Native definition conversion/logging adapters and one common global-only setting. Core and
external Harmony are required. Original Log After Def Error, its continuation,
Better Log, VersionFromManifest and HugsLib/Log Uploader are not dependencies. Active originals trigger
a bounded startup warning and best-effort ordinary context-feature off behavior, without rewriting
stored preferences or modifying the originals. Original API/consumer/facade and coexistence work is
excluded; effective native target admission and genuine integrations actually used remain required.
No DLC, XML Extensions operation, Thing cache, new content,
foreign DLL or Gateway dependency. Any later XML patch requires the repository's guarded XML
Extensions workflow and dependency at first use. Host tests and review do not accept the feature;
the reviewed native loading/select/copy/reload and target-admission workflows remain required.
