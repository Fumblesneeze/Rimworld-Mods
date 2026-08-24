## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: One repository-owned MCP and CLI surface
The repository SHALL provide one C# executable named `RimWorldModding.Mcp` that exposes the same typed operation registry through local stdio MCP and a command-line interface. It MUST target the repository-pinned .NET SDK, publish as a single-file executable, reserve stdout for protocol or requested results, send diagnostics to stderr, and return CLI exit code `0` for success, `2` for invalid usage/validation, and `1` for runtime failure. CLI operations MUST support deterministic `json` and `table` output.

#### Scenario: One operation works through both transports
- **WHEN** a caller lists and invokes repository status through MCP and through the CLI
- **THEN** both surfaces expose the same operation name, input contract, normalized result, and success semantics without calling a separate implementation

#### Scenario: Invalid CLI input is machine-distinguishable
- **WHEN** a caller submits malformed JSON or an unsupported output format
- **THEN** the CLI writes an actionable diagnostic to stderr, emits no misleading success payload, and exits with code `2`

### Requirement: Trusted repository configuration starts the local server
The repository SHALL contain `.codex/config.toml` configuring a required stdio MCP server with explicit repository working directory, source-built `serve` arguments, bounded startup/tool timeouts, and write-aware approval policy. The server MUST validate that its configured root contains the expected repository markers and MUST NOT search upward into or operate on another repository.

#### Scenario: A new trusted local session discovers tools
- **WHEN** Codex starts a trusted session rooted at this repository and loads its project configuration
- **THEN** it launches the stdio server from the declared working directory and discovers the repository operation catalog without user-global MCP configuration

### Requirement: Operations are typed, discoverable, cancellable, and bounded
Every operation SHALL declare a stable name, concise description, typed input/output, risk class, timeout, cancellation behavior, and evidence policy. Long-running work SHALL return or retain a run ID, exact child-process identity, bounded progress, durable result location, and status/cancel lifecycle. The server MUST NOT expose arbitrary shell execution, arbitrary filesystem mutation, or a combined diagnostic/mutation Gateway tool.

#### Scenario: Caller discovers safety and lifecycle before invocation
- **WHEN** a caller lists operations
- **THEN** each descriptor identifies whether it reads, writes the workspace, mutates external state, or performs destructive local state and whether it returns a long-running run lease

#### Scenario: A cancelled run cannot kill an unrelated process
- **WHEN** a caller cancels a running operation by run ID
- **THEN** the server signals and, if required, terminates only the child process whose PID and start identity are retained by that run

### Requirement: Repeated automation graduates into the operation catalog
Repository process guidance SHALL require agents to search the MCP catalog before adding RimWorld automation. A workflow repeated across requests or mods, copied into another script, responsible for structured parsing, long-lived processes, safety boundaries, or durable evidence MUST receive an owning OpenSpec requirement, public-behavior tests, and a typed MCP/CLI operation instead of another agent-facing script. One-off read-only exploration MAY remain a shell or raw diagnostic, but it MUST NOT become the documented reusable path.

#### Scenario: A second mod needs an existing workflow
- **WHEN** release, test, package, Gateway, or evidence orchestration would otherwise be copied and renamed for another mod
- **THEN** the change extends the universal profile or typed operation and does not add a mod-specific public script

### Requirement: Migration adapters are explicit and temporary
The MCP/CLI MAY invoke an existing script or host as an internal adapter only when its operation owns typed validation, output parsing, cancellation, evidence, and safety semantics. Documentation SHALL name the MCP/CLI as the public entry point, and each retained adapter SHALL have a tracked parity/removal task. New product-specific publication policy MUST NOT be embedded in an adapter.

#### Scenario: Existing grouped runner remains an engine
- **WHEN** an MCP E2E operation uses the proven grouped runner during migration
- **THEN** callers receive the MCP operation contract and evidence result while the adapter path remains an internal implementation detail with a tracked migration owner
