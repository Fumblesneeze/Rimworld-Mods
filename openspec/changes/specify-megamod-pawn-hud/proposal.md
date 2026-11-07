## Why

Selecting a pawn should expose its condition, capabilities and current work without repeatedly
opening several tabs. S127 adopts RimHUD's configurable summary and contextual shortcuts while
preserving native controls, trustworthy information and the megamod's common settings rules.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This change **specifies later work only**; no runtime implementation,
external compatibility or gameplay acceptance is delivered.

## What Changes

- Specify docked and floating selected-pawn summaries, conditional widgets, health/mental warnings,
  detailed tooltips, skill progress, animal training, current work and native policy/tab shortcuts.
- Preserve native selection, inspect buttons, tabs, letters and tutorial access. Correct stale
  action targets, false shutdown information, hidden letters, swapped custom-selector callbacks
  and whole-HUD failure cascades identified in source analysis.
- Put layout, appearance, refresh and preset configuration in structured mod settings with common
  global defaults, save overrides and Use global controls. Keep the operational view toggle at
  bottom right and diagnostics in the top debug toolbar. Pawn policy choices remain pawn state.
- Specify editable layouts, safe named preset reuse and the published five widget kinds. Preserve
  supported external formats and callbacks; identify package/assembly and private-patch consumers
  that cannot be satisfied by a format adapter alone. Do not claim universal drop-in compatibility.
- Compose owned number formatting, need overflow, compact health, cargo details and subsequent
  Passion Icons/Gear features directly. S128 remains a separate source change; S137's condition
  filter remains a separate medical-bill feature rather than a HUD click side effect.
- Keep original RimHUD authoritative when co-installed; withhold the overlapping owned HUD with
  a startup warning. No foreign configuration edits, forced mod removal or package-ID spoofing.

## Capabilities

### New Capabilities

- `megamod-pawn-hud`: selected-pawn information, warnings, contextual actions, placement and fallback.
- `megamod-pawn-hud-customization`: layout/theme settings, presets, external widgets and persistence.

### Modified Capabilities

None. Related active drafts remain their owners' contracts; this change consumes them directly.

## Impact

- Documentation under this change, the S127 research note, replacement coverage and refinement ledger.
- Future runtime requires the selected 1.7 game and Harmony when hooks are implemented. No DLC is
  required; DLC information is conditional. Original RimHUD and provider mods are optional inputs,
  never bundled dependencies. Dubs Bad Hygiene needs/presets, Vanilla Skills Expanded, Useful Marks,
  Vehicle Framework/SmashTools and Pawn Rules need individual package/shape admission; Multiplayer is deferred;
  their source advertisement or installed presence is not an acceptance result.
- No XML Def mutation is selected. If a later adapter uses XML Extensions, that implementation
  must add its required dependency and guarded, merge-preserving contract in the same slice.
- No source code/assets or upstream DLL redistribution; no published license file was found in
  the inspected source tree. Independently implement the observable contracts, retain attribution.
- Tests, target documents, seven-language UI, focused independent review and final minimized native
  in-game workflows remain unchecked implementation tasks. No dependency on another repository mod
  or the developer Gateway is introduced.
