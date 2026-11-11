Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at
`tools/RimWorldDevGateway.EndToEndHost`.

## ADDED Requirements

### Requirement: E2E scenarios declare one native XML loading mode
An E2E scenario SHALL use native default loading unless it explicitly declares legacy XML loading.
Legacy selection SHALL be limited to supported RimWorld 1.7 isolated E2E runs. The runner SHALL use
the game's native startup option before initial XML conversion, rather than modifying loader state
or calling a loader from test code. The public interface SHALL remain exact typed test/group selection
without accepting arbitrary command-line arguments.

#### Scenario: Existing default scenario
- **WHEN** an existing scenario omits a legacy declaration
- **THEN** its default group identity and native loader arguments remain unchanged
- **AND** it runs through the existing isolated E2E lifecycle without a legacy flag

#### Scenario: Explicit legacy scenario
- **WHEN** a supported 1.7 scenario declares legacy loading and is selected
- **THEN** its fresh minimized process starts with exactly one `-legacy-xml-deserializer` argument before XML loading
- **AND** its exact ordered mod list and all other isolation requirements remain in force

#### Scenario: Unsupported request
- **WHEN** a selected declaration is malformed or requests legacy loading for an unsupported runtime
- **THEN** preflight fails clearly before publishing fixtures or launching a game
- **AND** no arbitrary argument or silent default substitution is accepted

### Requirement: Different loading modes require separate exact groups
Scenarios with identical ordered packages but different declared XML modes SHALL be planned and
executed in separate processes. Exact selection SHALL retain full metadata validation and only the
selected group's package/fixture availability requirements. Mode SHALL never be represented as a
fake installed package, and XML fixture payload identity SHALL remain unchanged by mode selection.

#### Scenario: Same packages with two modes
- **WHEN** default and legacy scenarios declare the same ordered packages
- **THEN** discovery reports two distinguishable groups and their selected executions use separate processes
- **AND** existing default group IDs retain their meaning

#### Scenario: Select one fixture-backed variant
- **WHEN** only the legacy variant requiring a declared XML fixture is selected
- **THEN** the plan retains that fixture's exact owner, package and file identities for that variant
- **AND** unselected variants do not add availability requirements or execute in its process

### Requirement: Observed native mode controls runtime admission
The runtime SHALL compare each selected scenario's declared mode with the actual native process
mode before constructing or arranging that scenario. Compiled declarations and staged metadata
SHALL agree. Incompatible modes, unknown manifest schemas or inconsistent declarations SHALL fail
with a clear explanation; zero admitted tests SHALL never be reported as success.

#### Scenario: Wrong process mode
- **WHEN** a selected legacy scenario reaches a default process, or a selected default scenario reaches a legacy process
- **THEN** admission fails before that scenario's constructor or Arrange executes
- **AND** retained evidence identifies requested and observed modes

#### Scenario: Mixed bundle with one selected mode
- **WHEN** a valid staged bundle includes both modes but selection requests only the process-compatible tests
- **THEN** complete bundle metadata remains validated and only the compatible selected tests execute
- **AND** unselected opposite-mode tests do not cause a false mismatch failure

#### Scenario: Manifest conceals or changes the declaration
- **WHEN** staged metadata disagrees with the compiled declaration or claims legacy in the default-only schema
- **THEN** admission fails before scenario construction or Arrange
- **AND** an older runtime cannot silently accept a mode-bearing manifest as default

### Requirement: Planning and evidence expose the mode without weakening cleanup
Dry-run SHALL state each selected group's XML mode and the fixed launch choice without launching,
publishing fixtures or mutating a game. Live evidence SHALL retain requested and observed modes with
the existing package/build/fixture/process identities and action evidence. Mode selection SHALL keep
the current savedata/log isolation, normal-config hash checks, exact-child stop proof and leased
fixture cleanup/recovery rules on success, failure and cancellation. Existing destinations SHALL
never be overwritten or backed up for this mode selection; normal user configuration remains untouched.

#### Scenario: Compare default and legacy dry-runs
- **WHEN** the same fixture-backed package sequence is planned once per mode
- **THEN** both plans retain identical package/fixture identities and show their different native mode choices
- **AND** both report no game mutation and start no child process

#### Scenario: Legacy run fails or is cancelled
- **WHEN** a selected legacy workflow fails or is cancelled after staging or launch
- **THEN** the existing exact process and fixture ownership rules govern cleanup and retain unresolved obligations
- **AND** failure evidence preserves both the scenario outcome and any cleanup failure without claiming unproved restoration

### Requirement: Native verification proves loader use and player inspection
Acceptance of this tooling capability SHALL include a reviewed separate native legacy process that
loads declared XML inputs during actual startup and exercises the owning scenario's real native
player inspection workflow. The acting acceptance agent SHALL personally inspect causal screenshots
and relevant complete logs, retain mode/build/process/fixture identities and verify cleanup. A flag,
host test, dry-run or loader assertion alone SHALL not accept product behavior.

#### Scenario: Inspect actual legacy-loaded diagnostics
- **WHEN** the reviewed legacy process loads the declared diagnostic XML fixture and the player selects its retained native log records
- **THEN** the observed native details correspond to those actual loader records on the tested build
- **AND** evidence connects the prelaunch choice, observed native mode, player actions and visible result without synthetic loader calls
- **AND** the affected default workflow is also verified on the reviewed build while broader product obligations remain separate
