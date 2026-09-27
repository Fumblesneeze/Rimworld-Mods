## Why

Edge-centered selection must be tested through native pointer hit testing, not direct selection by ID.
Existing desktop input would disturb the user and does not work faithfully with minimized RimWorld.

## What Changes

- Add a bounded fractional map-coordinate left click through the native Selector input path.
- Expose the same action to the authenticated semantic-action API/MCP and typed E2E steps.
- Retain before/after selection and restore temporary input state without moving the desktop cursor.

## Capabilities

### New Capabilities
- `gateway-map-pointer-selection`: minimized native fractional map selection.

### Modified Capabilities
None.

## Impact

Owner: repository tooling **RimWorld Dev Gateway** (`mods/RimWorldDevGateway`).
Shared E2E contracts and Gateway tests are affected. Gateway's existing required Harmony dependency
is unchanged. Product mods acquire no Gateway dependency.
This delivers developer control, not gameplay behavior.
