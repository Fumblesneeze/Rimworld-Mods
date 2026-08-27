## Why

Automated RimWorld runs currently mute only music, so game, ambient, and UI sounds can still interrupt the workstation during routine MCP, Gateway, and E2E verification. These runs should be silent by default while preserving an explicit opt-in for sound implementation and testing.

## What Changes

- Make isolated automated RimWorld launches mute RimWorld's master volume by default in their disposable `Prefs.xml`.
- Add an explicit `enableAudio` MCP argument and matching launcher switch that restores master audio for sound-focused work.
- Apply the same policy to direct Gateway smoke runs, leased `game_run_start` processes, and grouped `e2e_run_start` processes.
- Record the requested audio mode and effective master volume in dry-run and completed evidence.
- Keep the user's normal RimWorld preferences unchanged and retain music volume at zero in both modes.
- Deliver repository-tooling behavior only; no packaged mod gameplay behavior changes.

## Capabilities

### New Capabilities

- `automated-rimworld-audio-policy`: Default-muted isolated launch preferences, explicit audio opt-in, propagation through MCP/Gateway/E2E entry points, and truthful evidence.

### Modified Capabilities

None.

## Impact

**Owner:** Repository tooling — automated RimWorld launch automation rooted at `scripts/Invoke-GatewaySmoke.ps1` and projected through `tools/RimWorldModding.Mcp`.

Affected components are the Gateway smoke launcher, grouped E2E runner, MCP operation contracts/planners, focused host tests, and launcher documentation. RimWorld 1.6 is required to exercise the live launch contract; there are no new required or optional external runtime dependencies. Product mods remain independent of the Gateway and MCP tooling.
