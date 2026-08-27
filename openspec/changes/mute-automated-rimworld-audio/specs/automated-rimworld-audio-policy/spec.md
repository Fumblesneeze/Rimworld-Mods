## ADDED Requirements

**Owner:** Repository tooling — automated RimWorld launch automation rooted at `scripts/Invoke-GatewaySmoke.ps1` and projected through `tools/RimWorldModding.Mcp`.

### Requirement: Automated RimWorld launches are silent by default
Every isolated RimWorld process launched by the Gateway smoke launcher, MCP `game_run_start`, or grouped `e2e_run_start` SHALL use its disposable RimWorld `Prefs.xml` to set master volume to `0` unless the caller explicitly enables audio. The launcher SHALL continue to set music volume to `0`, SHALL NOT use operating-system mixer mutation as the mute mechanism, and SHALL NOT read or change the user's normal audio preferences.

#### Scenario: Default direct Gateway launch is muted
- **WHEN** a caller runs the Gateway smoke launcher without the audio opt-in
- **THEN** the disposable `Prefs.xml` contains master volume `0` and music volume `0`, the owned RimWorld process uses those settings, and the user's normal `Prefs.xml` retains its original hash

#### Scenario: Default MCP game run is muted
- **WHEN** a caller invokes `game_run_start` without `enableAudio` or with `enableAudio=false`
- **THEN** the operation starts its isolated Gateway-backed RimWorld process with master volume `0` and records the muted mode in retained launch evidence

#### Scenario: Default grouped E2E run is muted
- **WHEN** a caller invokes `e2e_run_start` without `enableAudio` or with `enableAudio=false`
- **THEN** every selected group's child RimWorld process starts with master volume `0` and the dry-run and aggregate evidence identify the muted mode

### Requirement: Audio has one explicit launch-time opt-in
The Gateway smoke launcher SHALL accept `-EnableAudio`, and MCP `game_run_start` and `e2e_run_start` SHALL expose an `enableAudio` boolean argument. When enabled, the disposable `Prefs.xml` SHALL set master volume to `1` while retaining music volume `0`. The opt-in SHALL affect only the newly launched isolated process and SHALL be fixed for that process's launch contract.

#### Scenario: Direct Gateway audio opt-in
- **WHEN** a caller starts the Gateway smoke launcher with `-EnableAudio`
- **THEN** the disposable `Prefs.xml` contains master volume `1` and music volume `0`, and launch evidence identifies audio as enabled

#### Scenario: MCP game-run audio opt-in
- **WHEN** a caller invokes `game_run_start` with `enableAudio=true`
- **THEN** the leased launcher receives the explicit audio switch and reports master volume `1` for that isolated run

#### Scenario: MCP E2E audio opt-in
- **WHEN** a caller invokes `e2e_run_start` with `enableAudio=true`
- **THEN** the grouped runner records audio as enabled and forwards the explicit audio switch to every selected child launcher

### Requirement: Audio launch evidence is truthful and complete
Gateway smoke dry-run and completed results SHALL report whether audio was explicitly enabled, the effective master volume, and the music volume. Grouped E2E dry-run plans and aggregate/group results SHALL report the requested audio mode. Evidence SHALL describe the values written to the disposable preferences rather than infer operating-system playback state.

#### Scenario: Muted dry-run reports effective values
- **WHEN** a caller performs a default dry-run
- **THEN** the result reports audio disabled, master volume `0`, music volume `0`, and the exact disposable preference path

#### Scenario: Enabled dry-run reports effective values
- **WHEN** a caller performs a dry-run with audio enabled
- **THEN** the result reports audio enabled, master volume `1`, music volume `0`, and the exact disposable preference path
