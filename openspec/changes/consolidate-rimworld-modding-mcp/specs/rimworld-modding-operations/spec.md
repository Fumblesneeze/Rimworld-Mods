## ADDED Requirements
**Owner:** Repository tooling — `RimWorldModding.Mcp` at `tools/RimWorldModding.Mcp`.

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

#### Scenario: Build a registered metadata fixture before its focused suite
- **WHEN** a registered test suite reads compiled metadata from another repository test project
- **THEN** the suite builds that exact fixture in the selected configuration before executing so stale binaries cannot satisfy or fail the metadata contract

#### Scenario: Validate one product package
- **WHEN** a caller validates a distributable mod package
- **THEN** the result checks its declared allowlist and rejects source, symbols, caches, test, host, Gateway, compiler, optional-mod, RimWorld, Unity, or bundled Harmony assemblies as applicable

### Requirement: Presentation rendering is a typed offline operation
The MCP/CLI SHALL expose `presentation_render(packageId)` for one validated release profile. It SHALL
execute only the repository-contained PowerShell renderer declared by path and SHA-256 in that
profile's presentation manifest, with the exact manifest path, a bounded timeout, cancellation, and
bounded output. The operation SHALL work in an uncommitted authoring checkout or an isolated release
worktree, without launching RimWorld, changing Steam, installing a mod, or requiring a clean revision.
Publication preparation and synchronization SHALL retain their separate clean-revision gates.

#### Scenario: Rerender an authored presentation
- **WHEN** a caller selects a package whose presentation manifest pins an existing renderer
- **THEN** the operation invokes that renderer with the selected manifest and JSON output, returns its
  exit/duration/output evidence, and repeated unchanged input produces the same authored outputs

#### Scenario: Refuse an unreviewed or failed renderer
- **WHEN** the renderer is missing, outside the repository scripts directory, not PowerShell, changed
  from its pinned hash, or returns a failure
- **THEN** rendering fails with an actionable error and does not report successful presentation output

### Requirement: Successful mod builds install the local package by default
The `mod_build` operation SHALL refuse to run while RimWorld is active, build the selected profile's
project, and, only after a successful build, synchronize its positive-allowlist package into the
configured local RimWorld `Mods/<package-id>` directory. When no override is supplied it SHALL use
the repository's configured local RimWorld installation. Replacing an existing package SHALL not
create or retain a backup of the installed mod; temporary staging SHALL be outside the scanned Mods
directory and removed after synchronization. A failed build SHALL not change the installed package.

#### Scenario: A successful build updates the local mod automatically
- **WHEN** a caller runs `mod_build` for one canonical package with a successful Release build
- **THEN** the operation reports the build and installed package results, and the destination package
  contains the exact built allowlist without a retained install backup

#### Scenario: A failed build does not install partial output
- **WHEN** the selected project build fails
- **THEN** the operation returns the build failure and does not synchronize or replace the local package

### Requirement: New distributable mods use the canonical repository baseline
Repository guidance SHALL define one repeatable baseline for every new distributable mod: a
`mods/<ModName>` Zlepper project with author/package identity, generated About metadata, an owning
OpenSpec change, a positive-allowlist release profile, and test-layer selection appropriate to the
behavior. The guidance SHALL identify `mod_build` as the default build-and-install operation, with
`local_mod_sync` as the install-only equivalent; neither operation SHALL retain an install backup.

#### Scenario: A new mod is discoverable before its first release
- **WHEN** a contributor adds the standard project metadata, package ID, About source, and release profile
- **THEN** repository discovery and `release_profile_validate` report one canonical mod identity and its exact project/package inputs without requiring a Workshop item ID

#### Scenario: A new mod follows the default local development loop
- **WHEN** a contributor runs `mod_build` for the new mod's canonical package ID with no `modsRoot` override
- **THEN** a successful build installs the positive-allowlist package into the configured local `Mods/<package-id>` folder, stages outside the scanned Mods folder, and leaves no install backup

### Requirement: Isolated RimWorld and grouped E2E runs retain native-workflow evidence
The MCP/CLI SHALL start isolated game, smoke, integration, and grouped E2E work through exact executable and PID/start identity, unique savedata, explicit ordered mod list, dedicated logs, and durable evidence. It MUST keep automated processes minimized, use the Gateway for semantic control where available, hash the normal configuration before and after, request graceful shutdown, and restore or discard mutated isolated state. Gateway-based native player workflows SHALL satisfy live acceptance without requiring a separate run that omits the Gateway. Product dependency metadata, package contents, and assembly references SHALL independently exclude the Gateway. An operation result MUST distinguish supporting automation success from personally reviewed live acceptance.

#### Scenario: Start a grouped native workflow
- **WHEN** a caller selects an exact E2E group and test IDs
- **THEN** one isolated process executes the native player actions, retains correlated before/action/after screenshots and request IDs, and returns evidence that remains unaccepted until a reviewer records visual inspection

#### Scenario: Scope E2E discovery to an owning project
- **WHEN** a caller passes an optional repository-contained `.csproj` path to the E2E run operation
- **THEN** the operation builds and validates E2E assembly metadata only for that marked project, an unrelated project declaring an unresolvable package cannot block the selection, and a path outside the repository or without a `.csproj` extension is rejected with exit code `2`

#### Scenario: Cancel after the launcher has exited
- **WHEN** the launcher process has exited but the exact leased RimWorld PID and process-start identity remain live
- **THEN** cancellation still requests bounded graceful completion and terminates only that exact orphaned game process before releasing the lease

### Requirement: Live Gateway diagnostics and mutations are separate
The MCP/CLI SHALL expose authenticated health/status and typed diagnostic Gateway operations separately from raw C# diagnostics and explicit mutations. It SHALL resolve only a live Gateway instance whose PID/start identity and bearer material belong to the caller-selected isolated run, pass cancellation, bound input/output, redact bearer values, and retain request IDs. Mutation tools MUST state that direct mutation is setup/diagnostic behavior and cannot prove a player workflow.

#### Scenario: Read live state without exposing credentials
- **WHEN** a caller queries game state for an active isolated run
- **THEN** the operation authenticates to that run, returns the bounded Gateway response and request ID, and stores neither the bearer token nor live `current.json` in durable output

#### Scenario: Capture the exact leased game's rendered view
- **WHEN** a caller invokes `gateway_screenshot` with an exact ready run ID
- **THEN** the operation uses the existing authenticated Gateway capture, saves a uniquely named PNG beneath that run's evidence directory, and returns the run ID, game PID/start identity, path, byte count, and SHA-256 without returning credentials or changing game state
- **AND** a missing or non-ready run is rejected rather than selecting another process; the PNG remains supporting evidence until personally reviewed with its associated player action

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

#### Scenario: Local package synchronization does not retain an install backup
- **WHEN** `local_mod_sync` replaces an existing product copy
- **THEN** it stages outside the `Mods` directory, leaves only the selected package under `Mods/<package-id>`, and reports no retained install backup
### Requirement: Typed diagnostic usability
The tooling component SHALL expose `gateway_logs` with validated exclusive cursor and page limit; `gateway_errors` with bounded filtering/paging and exact error detail; `gateway_methods` with bounded method discovery; `gateway_decompile` selecting original or explicitly mutating current merged code; and `gateway_error_report` exporting an error and its provenance beneath the exact run's evidence directory. These operations SHALL reuse exact leased-process authentication, cancellation and request correlation. C# decompilation SHALL occur on the host from exact MVID/token-validated assembly inputs and retain provenance. No browser UI or managed hot-patch lifecycle is required in this slice.

#### Scenario: Continue chronological log pages
- **WHEN** a caller supplies the last returned sequence and a limit
- **THEN** the MCP/CLI forwards both values and returns later logs instead of the default initial page

#### Scenario: Inspect and export an error
- **WHEN** a caller selects an error from its exact run
- **THEN** typed tools return grouped occurrences and attributed frames and can retain a credential-free Markdown/JSON report with process and request identity

#### Scenario: Decompile an exact overloaded method
- **WHEN** a caller selects an exact method handle
- **THEN** host decompilation returns that token's C# and input hash, rejects changed module identity, and labels merged output as reconstructed current code without historical crash-line precision
