## Why
The user requested one Immersive Chefs Workshop bundle for RimWorld 1.6 and 1.7.
## What Changes
- Compile shared Immersive Chefs source independently for both runtimes.
- Adapt only demonstrated engine API/XML divergences through narrow compatibility seams.
- Verify native cooking, dining and cleaning on each exact target and publish truthful version metadata.
## Owner
Mod: Immersive Chefs (`fumblesneeze.immersivechefs`).
## Capabilities
### New Capabilities
- `immersive-chefs-version-compatibility`: Versioned release compatibility with unchanged cooking and dining behavior.
## Impact
Uses the repository tooling matrix in `exercise-multiversion-release`; product independence and absent-safe optional integrations remain mandatory.
