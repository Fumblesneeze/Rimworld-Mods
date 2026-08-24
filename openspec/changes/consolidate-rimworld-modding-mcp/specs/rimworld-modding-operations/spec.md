## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Repository and mod discovery is canonical
The MCP/CLI SHALL discover mods from their project and About metadata, associate OpenSpec changes and test projects, locate validated release profiles, and report stable package/display/project identities. It MUST reject duplicate package IDs, mismatched profile/About/project identities, unknown profile properties, and paths escaping the repository.

#### Scenario: List all operable mods
- **WHEN** a caller requests repository status
- **THEN** the result lists each discovered mod with package ID, project, distribution class, release-profile state, focused test projects, and owning active changes without guessing from folder names alone

### Requirement: Focused specification, build, test, package, and evidence tools
The MCP/CLI SHALL provide typed operations for strict OpenSpec validation, project/mod builds, focused test selection, product-package validation, and evidence inspection. Inputs MUST select exact changes, projects, groups, or test IDs; default feature work MUST NOT silently run the guarded full suite. Results SHALL preserve command identity, exit code, duration, bounded output, and artifact paths and MUST reject zero-test success.

#### Scenario: Run one focused test project
- **WHEN** a caller starts a test operation with one exact project and optional test filter
- **THEN** the operation runs that selection, reports passed/failed/skipped counts and result path, and fails if no test executed

#### Scenario: Validate one product package
- **WHEN** a caller validates a distributable mod package
- **THEN** the result checks its declared allowlist and rejects source, symbols, caches, test, host, Gateway, compiler, optional-mod, RimWorld, Unity, or bundled Harmony assemblies as applicable

### Requirement: Isolated RimWorld and grouped E2E runs retain native-workflow evidence
The MCP/CLI SHALL start isolated game, smoke, integration, and grouped E2E work through exact executable and PID/start identity, unique savedata, explicit ordered mod list, dedicated logs, and durable evidence. It MUST keep automated processes minimized, use the Gateway for semantic control where available, preserve the product-only no-Gateway exception, hash the normal configuration before and after, request graceful shutdown, and restore or discard mutated isolated state. An operation result MUST distinguish supporting automation success from personally reviewed live acceptance.

#### Scenario: Start a grouped native workflow
- **WHEN** a caller selects an exact E2E group and test IDs
- **THEN** one isolated process executes the native player actions, retains correlated before/action/after screenshots and request IDs, and returns evidence that remains unaccepted until a reviewer records visual inspection

#### Scenario: Cancel after the launcher has exited
- **WHEN** the launcher process has exited but the exact leased RimWorld PID and process-start identity remain live
- **THEN** cancellation still requests bounded graceful completion and terminates only that exact orphaned game process before releasing the lease

### Requirement: Live Gateway diagnostics and mutations are separate
The MCP/CLI SHALL expose authenticated health/status and typed diagnostic Gateway operations separately from raw C# diagnostics and explicit mutations. It SHALL resolve only a live Gateway instance whose PID/start identity and bearer material belong to the caller-selected isolated run, pass cancellation, bound input/output, redact bearer values, and retain request IDs. Mutation tools MUST state that direct mutation is setup/diagnostic behavior and cannot prove a player workflow.

#### Scenario: Read live state without exposing credentials
- **WHEN** a caller queries game state for an active isolated run
- **THEN** the operation authenticates to that run, returns the bounded Gateway response and request ID, and stores neither the bearer token nor live `current.json` in durable output

#### Scenario: Send raw mutating C# deliberately
- **WHEN** a caller explicitly selects the raw mutation operation and submits bounded C#
- **THEN** the result labels the action as mutation, journals it to the exact run, and does not promote its post-state to gameplay acceptance evidence

### Requirement: Normal mod-list management is transactional
The MCP/CLI SHALL inspect, enable, disable, and restore entries in the user's normal RimWorld `ModsConfig.xml` only through explicit operations. Before a write it MUST refuse when an unowned RimWorld process is active, resolve the exact file, record its hash, create an exact timestamped backup, parse XML, preserve unrelated order and values, write atomically, reparse, and report the after hash. On failure it MUST restore the backup. A package SHALL appear at most once and an anchor package MUST be resolved exactly.

#### Scenario: Enable a local mod after Hospitality
- **WHEN** a caller enables `fumblesneeze.guestbedgizmo` anchored after `orion.hospitality`
- **THEN** the exact normal config gains one entry after the anchor, all other entries remain in order, and the result includes backup plus before/after hashes

#### Scenario: Failed config validation restores original bytes
- **WHEN** a mod-list write produces invalid XML or loses an unrelated entry
- **THEN** the operation restores the exact backup and returns runtime failure without leaving a partially written config

#### Scenario: Local package backups are not scanned as mods
- **WHEN** local package synchronization replaces an existing product copy
- **THEN** its staged and retained prior copies live under a validated sibling recovery root outside the `Mods` directory and the installed package alone remains under `Mods/<package-id>`
