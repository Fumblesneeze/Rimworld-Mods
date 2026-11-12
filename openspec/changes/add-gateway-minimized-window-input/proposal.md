## Why

Megamod's native settings, policy and log workflows require real checkbox and text-field input.
The current Gateway can open those dialogs, but its generic input route requires a visible desktop
window. This prevents faithful minimized acceptance of the controls themselves.

## What Changes

Add a single native held-move drag for exact captured windows, exposing the actual MouseDrag event
through existing named automation and E2E input ownership for native sliders. Ordinary clicks remain
unchanged; native drag delivery and the visible control result require separate evidence.

The unaccepted native caravan slice also guards late native contents failures, preserves visible
Health rows beside clipped neighbours, drains frame-grown owned camera history incrementally,
refuses restoration that could cancel foreign world activities, and partitions the existing
cleanup deadline so lost input dispatch cannot consume fixture cleanup time.

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
- Add a bounded native World-view round trip, exact player-caravan selection and Items/Health
  navigation, with one observed pawn-Health row button on the existing minimized input lane.
  Capture and restore the exact same game's camera/view state through a retained cleanup lease.
  This is a Gateway prerequisite for caravan observation; it does not accept product ingestion.

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
