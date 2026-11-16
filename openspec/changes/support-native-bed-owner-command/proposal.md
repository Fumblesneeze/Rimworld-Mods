## Why
The Guest Bed Gizmo 1.7 absence workflow exposed that the Gateway cannot invoke RimWorld's original bed-owner command, preventing faithful verification when Hospitality is absent.

## What Changes
- Recognize the exact native `Verse.Command_SetBedOwnerType` as an immediate gizmo action.
- Invoke its inspected native input method and lease its newly opened FloatMenu through the existing typed gizmo surface.

## Owner
Repository tooling: RimWorld Dev Gateway.

## Capabilities
### New Capabilities
- `native-bed-owner-command`: Native owner-menu action and observation through the existing Gateway gizmo API.

## Impact
Development tooling only. No product dependency or package change; no new endpoint or desktop input.
