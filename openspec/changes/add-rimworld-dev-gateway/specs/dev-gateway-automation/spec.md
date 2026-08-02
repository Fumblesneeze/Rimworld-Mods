## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Discoverable named automation registry
`GET /api/v1/automations` SHALL expose every built-in and session-registered automation's stable name, version, description, bounded argument schema, prerequisites, availability, unavailable reason, and mutating/read-only classification.

#### Scenario: Discover automations on a playable map
- **WHEN** an authenticated caller lists automations after a map is ready
- **THEN** the response includes `quickstart.spawn` with its current version, schema, prerequisites, and available state

### Requirement: Synchronous correlated automation runs
`POST /api/v1/automations/{name}/runs` SHALL dispatch the named automation to the main thread, wait for its handler to finish, and return a terminal run representation in the same HTTP response. The returned run SHALL have a stable run ID and one of `succeeded`, `failed`, `cancelled`, or `timed-out` as its state, together with bounded completed progress/artifact records, timings, result or error, and any mutation ledger supplied by the automation.

The registry MAY use `queued`, `running`, or `cancel-requested` internally while the synchronous call is in progress, but version one SHALL NOT expose those as an incremental remote lifecycle.

#### Scenario: Named automation succeeds
- **WHEN** an authenticated caller starts an available automation with valid arguments
- **THEN** the POST completes with a terminal `succeeded` run that identifies the run and records its completed progress, mutations, warnings, timings, result, and artifacts

#### Scenario: Named automation fails in its handler
- **WHEN** an automation handler reports a known failure after recording progress or mutations
- **THEN** the POST completes with a terminal `failed` run containing the bounded completed records and error instead of exposing a remotely pollable intermediate state

### Requirement: Version-one automation lifecycle boundary
Version one SHALL expose discovery through `GET /api/v1/automations` and synchronous invocation through `POST /api/v1/automations/{name}/runs`. Run listing, run lookup, incremental progress polling or streaming, and remote run cancellation SHALL remain reserved for a future OpenSpec/API change and SHALL NOT be promised by the version-one HTTP surface.

#### Scenario: Caller needs incremental progress or cancellation
- **WHEN** a client needs to observe or cancel a still-running automation remotely
- **THEN** it treats that capability as unavailable in version one rather than assuming an undocumented list, get, stream, poll, or cancel route

### Requirement: Idempotent mutating automation requests
Mutating automation runs SHALL accept a caller idempotency key, retain its bounded result for the run, and return the existing run when the same key and equivalent arguments are submitted again. Reusing a key with different arguments SHALL fail.

#### Scenario: Quickstart retry after a client timeout
- **WHEN** the host repeats an equivalent `quickstart.spawn` request with the same idempotency key
- **THEN** the gateway returns the original run instead of spawning duplicate buildings or items

#### Scenario: Idempotency key is reused differently
- **WHEN** a caller reuses an existing key with different automation arguments
- **THEN** the gateway returns `idempotency_conflict` before starting new work

### Requirement: Quickstart launch and readiness flow
The host smoke command SHALL be able to launch RimWorld with an isolated save-data folder, `-quicktest`, and a minimal explicit mod list consisting of Core, zero or more caller-supplied additional package IDs, and the gateway in that order; discover and authenticate through the session manifest; verify through the raw endpoint that RimWorld actually loaded that ordered list; accept optional expected target-mod log markers; reject common mod/gateway load or initialization exception signatures; and wait with bounded diagnostics for both gateway readiness and a playable map before invoking setup. With no additional package IDs, the default SHALL remain Core plus the gateway. This command SHALL use direct HTTP and SHALL NOT require the optional companion client.

#### Scenario: Quickstart reaches a playable map
- **WHEN** the host launches a supported RimWorld build with valid mod paths, optional additional package IDs, and a scenario descriptor
- **THEN** it records and verifies the actual isolated ordered mod list, obtains authenticated status for the exact launched PID, and starts setup only after that process reports a playable map

#### Scenario: Quicktest never reaches a map
- **WHEN** the launched process exits, logs a mod error, or exceeds the readiness deadline before a playable map exists
- **THEN** the host fails with the PID, gateway/log evidence, isolated configuration, and reason and does not alter the user's normal mod configuration

### Requirement: Explicit quicktest scenario boundary
Launching with `-Quicktest` and no scenario SHALL wait for a playable isolated map without running scenario mutations or the Gateway surface-regression battery. It SHALL still run a harmless raw-C# health probe that proves Mono.CSharp evaluation, live Unity/Gateway/EmbedIO access, and the actual ordered loaded-mod list. In particular, the default quicktest SHALL NOT spawn temporary things or pawns, change selection, invoke debug actions or gizmos, apply designators, move or zoom the camera, toggle developer/god mode through the REPL, connect FlaUI, or inject raw input. The former comprehensive surface exercise SHALL run only when the caller explicitly selects the reserved `gateway-regression` scenario.

The host SHALL also accept an explicit named scenario descriptor from `scripts/Scenarios`. A descriptor SHALL declare schema version one, its matching name, required loaded package IDs, and ordered steps. Version one host scenario steps SHALL support raw C# source files and exact-process screenshots, resolve source files within the descriptor directory, reject missing required package IDs against the configured set before dry-run/launch and again against the actual live loaded set before setup, require case-insensitively unique scenario-prefixed screenshot names so standard evidence cannot be overwritten, retain a per-step result artifact, and execute no descriptor when the scenario option is omitted. After scenario execution or an interactive hold, the host SHALL stop the exact owned process and rescan the flushed Player log before reporting success.

#### Scenario: Ordinary quicktest stays quiet
- **WHEN** the caller launches with `-Quicktest` and omits `-Scenario`
- **THEN** the host reaches a playable map, proves raw C# health and the actual loaded mod list without mutation, and does not run the Gateway regression or any named setup descriptor

#### Scenario: Gateway regression is explicit
- **WHEN** the caller launches with `-Quicktest -Scenario gateway-regression`
- **THEN** the host runs the comprehensive spawn, selection, developer-action, gizmo, designator, camera, desktop, and raw-input regression and retains its evidence

#### Scenario: Caravan dining setup is selected
- **WHEN** the exact mod list includes `fumblesneeze.immersivechefs` and the caller selects `immersive-chefs-caravan-dining`
- **THEN** the host clears the generated pawn's randomized inventory, creates and selects a caravan containing only the intended covered meal with its exact plate embedded and silverware, shows the native Items tab without a separate pre-ingestion plate row, captures the ready scene, arms the pawn's hunger, and pauses so native ingestion can be observed returning that plate as a new inventory row

### Requirement: Declarative quickstart spawn setup
The built-in `quickstart.spawn` automation SHALL accept a version-one descriptor with required `version: 1`, optional `center: { x, z }`, optional `clearRadius`, and optional `buildings`, `items`, `pawns`, `research`, and `gameConditions` arrays. Building entries SHALL accept `defName`, optional `stuff`, `count`, `quality`, `offset: { x, z }`, and `powerOn`; item entries SHALL use the same shape without `powerOn`; pawn entries SHALL accept `kindDefName`, `count`, and `offset`; research SHALL accept either a Def-name string or `{ defName }`; and game-condition entries SHALL accept `defName` and `durationTicks`.

The automation SHALL cap each section at 64 entries, total spawned buildings and pawns at 256, an item entry at 5,000 units, a pawn entry at 32 pawns, each offset at 64 cells per axis, clearing at radius 20, and a condition at 3,600,000 ticks. It SHALL resolve all named Defs, Stuff, qualities, cells, and feasible prerequisites before mutation; use deterministic radial placement; clear only a bounded non-pawn area; spawn/configure content through game APIs; configure requested power state, research, and conditions; center the camera; and return resolved Defs, stable handles, positions, warnings, and a mutation ledger.

#### Scenario: Prepare a building verification scene
- **WHEN** a descriptor requests a supported map, powered named buildings, specified Stuff, and item stacks using valid Def names
- **THEN** the automation creates the requested scene near the resolved target cell and returns handles and positions for verification

#### Scenario: Descriptor exceeds a published bound
- **WHEN** an entry count, offset, clear radius, item or pawn count, or condition duration exceeds its version-one limit
- **THEN** preflight returns a stable limit error identifying the descriptor path and performs no feasible mutation

#### Scenario: Descriptor contains an unknown Def
- **WHEN** preflight cannot resolve a requested building or item Def
- **THEN** the run fails with `def_not_found` before performing feasible scenario mutations and identifies the descriptor path

### Requirement: Quickstart evidence bundle
Before invoking setup, the quickstart flow SHALL retain the initial authenticated responses and capture the current log cursor. After successful setup and its idempotent replay, it SHALL collect fresh authenticated status, UI state, a bounded log page after that pre-quickstart cursor, a post-setup screenshot, both automation responses, isolated mod list, game/mod versions, PID, run ID, and request IDs into a host-side evidence directory. The post-setup JSON files SHALL be named `post-quickstart-status.json`, `post-quickstart-ui-state.json`, and `post-quickstart-logs.json`.

#### Scenario: Scenario setup completes
- **WHEN** `quickstart.spawn` reports success
- **THEN** the host writes an evidence bundle sufficient to identify the exact process, ordered Core/additional/gateway configuration, spawned objects, post-setup state and visible scene, correlated log page, and idempotent replay

### Requirement: Truthful redundant desktop evidence
The isolated host smoke SHALL require a nonempty authenticated Gateway end-of-frame PNG from the exact launched process. When the explicit `gateway-regression` scenario is selected, a separate FlaUI desktop/window capture MAY supplement that image but SHALL NOT be the only visual evidence. That scenario SHALL write `flaui-evidence.json` with an explicit `completed` or `unavailable` state, bounded failure detail, window count when available, and the actual FlaUI screenshot path only when that file exists and is nonempty. It SHALL NOT copy or relabel the Gateway PNG as a FlaUI screenshot. A quiet or product-specific scenario SHALL NOT connect FlaUI or inject its regression click merely to produce redundant evidence.

When the mandatory Gateway PNG already exists, a bounded FlaUI window-list or screenshot failure SHALL be retained as `unavailable` and SHALL NOT abort the remaining exact-PID input probe or cleanup. The same FlaUI failure SHALL remain fatal when the mandatory Gateway PNG is absent or empty. Every FlaUI child command SHALL have a host-enforced timeout and SHALL be terminated only through its retained process handle; failure to confirm termination SHALL remain fatal. The host SHALL observe service state before setup, record service-start and connection attempts before invoking their side effects, preserve a pre-existing disconnected service, and reconcile an attempted connection to observed `disconnected` state and an owned service start to observed `stopped` state even when a command times out or returns malformed output.

#### Scenario: Redundant FlaUI screenshot is cancelled
- **WHEN** the exact-process Gateway PNG is retained but FlaUI cancels its separate desktop screenshot
- **THEN** the host records `flaui-evidence.json` as `unavailable`, leaves the FlaUI screenshot path null, continues the exact-PID input probe, and still performs normal cleanup

#### Scenario: No visual evidence source succeeds
- **WHEN** FlaUI capture fails and the mandatory authenticated Gateway PNG is missing or empty
- **THEN** the smoke fails instead of reporting visual evidence or fabricating a fallback screenshot

### Requirement: Product-mod-neutral automation boundary
Built-in gateway automations SHALL resolve optional content by package ID and Def name at runtime and SHALL NOT compile against Immersive Chefs or Workshop mod assemblies. Product-mod scenario descriptors SHALL remain data consumed by the host and gateway rather than a dependency from the product mod to the gateway.

#### Scenario: Verify a different mod
- **WHEN** quickstart is given a descriptor containing valid Defs from another loaded target mod
- **THEN** it resolves and spawns those Defs without loading any Immersive Chefs assembly or adapter

### Requirement: Normal mod configuration preservation
Automated launch SHALL use an isolated save-data folder and isolated `ModsConfig.xml`. If a future host operation is explicitly configured to modify the normal `ModsConfig.xml`, it SHALL back up and hash the exact file first, restore it in `finally`, and verify the restored hash before reporting success.

#### Scenario: Isolated quickstart completes
- **WHEN** the normal RimWorld configuration has a known hash and a quickstart run succeeds or fails
- **THEN** the normal configuration retains the same hash and the evidence identifies the isolated configuration used
