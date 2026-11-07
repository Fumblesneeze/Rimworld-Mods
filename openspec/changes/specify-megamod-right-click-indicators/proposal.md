## Why

Players cannot readily tell which command buttons have right-click menus. Right-Click Gizmo
Indicator makes those menus discoverable; the owned replacement must retain that convenience
without stale markers, speculative command execution or hidden suppression of unrelated messages.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Queue **S110**, origin **replacement**. This change specifies future
behavior only; it delivers no runtime implementation or gameplay/visual acceptance.

## What Changes

- Mark ordinary command buttons whose current right-click route opens an options menu, including
  supported modded commands and allowed-area selection. A menu containing only disabled choices
  still counts; a disabled command does not gain an owned marker.
- Keep markers current for the actual command, selection, map and modifier keys. Do not reuse the
  first command of a type as permanent truth for every instance or future game.
- Keep the marker passive: drawing it must not create menus, perform actions, consume input,
  emit spurious messages or silence genuine messages. Compose directly with owned command providers.
- Preserve native build/dropdown markers, command behavior and menus; avoid duplicate indicators
  and collisions with command labels. Keep the source's explicit custom-drawn-gizmo limitation
  documented rather than claiming every foreign UI is supported.
- Retain guarded optional menu-provider support and original-provider co-installation policy.
  A real Hide/Restore menu remains a menu; no unrequested provider blacklist is introduced.

## Capabilities

### New Capabilities

- `megamod-context-menu-indicators`: current, passive, native-style right-click menu indicators
  on ordinary command buttons, with preserved input and scoped compatibility fallback.

### Modified Capabilities

None. Existing owned command features supply their menu facts through direct composition; their
underlying orders, hide controls, storage actions and Architect navigation keep their own contracts.

## Impact

- This change, [research](../../../mods/megamod/docs/refinement/2026-11-05-right-click-gizmo-indicator-research.md),
  [coverage](../../../mods/megamod/docs/replacements/right-click-gizmo-indicator.md) and the ledger.
- Required game1.7 beta. Harmony is required with the first Harmony implementation; never bundled.
  No XML change is selected. Any later XML Extensions use requires its dependency and guards in
  that owning slice. No hard DLC, original-provider, optional-menu-provider or Gateway dependency.
- Source `CheaterEater.RightClickGizmoIndicator`, Workshop `3750529401`, is a reference, not a
  dependency. If active, it owns the overlap and the owned indicator is disabled with a startup warning.
  `Taggerung.IAintBuildingThat` is an optional integration reference, not another adopted feature.
- No settings, save migration, new tab/toggle/debug button, private Thing index or copied texture.
  Existing native presentation remains the visual reference; runtime asset selection and final
  native visual review remain future gates. All seven baseline language workflows must remain usable.
