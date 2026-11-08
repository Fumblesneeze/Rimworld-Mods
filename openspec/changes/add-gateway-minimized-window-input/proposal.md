## Why

Megamod's native settings, policy and log workflows require real checkbox and text-field input.
The current Gateway can open those dialogs, but its generic input route requires a visible desktop
window. This prevents faithful minimized acceptance of the controls themselves.

## What Changes

- Add bounded input to an exact current native dialog through the inspected 1.7 engine input path,
  without moving the desktop pointer, changing focus or restoring the game window.
- Preserve native hit testing, disabled controls, focus, text editing and ordinary callbacks.
- Expose the capability through the existing authenticated typed MCP/CLI and E2E surfaces, with
  stale-context rejection, cancellation and release of only injected input.
- Permit opening the native mod-settings page from a settled main menu as well as a loaded game.
- Expose the native developer-log toolbar command so minimized verification can inspect the actual console without clearing diagnostics or synthesizing errors.
- Open or close a supported ordinary bottom tab through its native button command, preserving
  visibility, disabled-state and modal input restrictions. This supplies the missing Assign entry
  path for the drug-policy workflow; controls inside the tab still use native input.

## Capabilities

### New Capabilities
- `gateway-minimized-window-input`: exact-dialog native input and main-menu settings navigation.

### Modified Capabilities
None. Existing disruptive desktop-input actions retain their explicit names and behavior.

## Impact

Exactly one owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.
Shared E2E contracts, Gateway host/live tests and the existing repository automation projection
are affected. The existing required Harmony dependency is unchanged. The 1.7 engine input provider
is capability-detected and unavailable on unsupported runtimes; there is no new external mod dependency.
Product mods acquire no Gateway reference. This delivers development control, not gameplay behavior.
