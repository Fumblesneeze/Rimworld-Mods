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
The host smoke command SHALL be able to launch RimWorld with an isolated save-data folder, `-quicktest`, and a minimal explicit mod list consisting of Core, zero or more caller-supplied additional package IDs, and the gateway. With no Harmony package, Core SHALL be first; when caller-supplied `brrainz.harmony` is present, Harmony SHALL be first and Core second, matching Harmony's installed `loadBefore` contract. Caller-supplied packages other than Harmony SHALL retain their relative order after Core, and the gateway SHALL remain last. The host SHALL discover and authenticate through the session manifest; verify through the raw endpoint that RimWorld actually loaded that ordered list; accept optional expected target-mod log markers; reject common mod/gateway load or initialization exception signatures; and wait with bounded diagnostics for both gateway readiness and a playable map before invoking any main-thread Def export, integration verification, or scenario setup. With no additional package IDs, the default SHALL remain Core plus the gateway. This command SHALL use direct HTTP and SHALL NOT require the optional companion client.

#### Scenario: Quickstart reaches a playable map
- **WHEN** the host launches a supported RimWorld build with valid mod paths, optional additional package IDs, and a scenario descriptor
- **THEN** it records and verifies the actual isolated ordered mod list, obtains authenticated status for the exact launched PID, and starts setup only after that process reports a playable map

#### Scenario: Harmony participates in an isolated launch
- **WHEN** the caller includes `brrainz.harmony` among additional package IDs
- **THEN** the isolated active order starts with Harmony followed by Core, preserves every other caller package's relative order after Core, and leaves the gateway last

#### Scenario: Quicktest never reaches a map
- **WHEN** the launched process exits, logs a mod error, or exceeds the readiness deadline before a playable map exists
- **THEN** the host fails with the PID, gateway/log evidence, isolated configuration, and reason and does not alter the user's normal mod configuration

### Requirement: Deterministic duplicate Workshop package resolution
When an explicitly requested active package ID occurs in more than one immediate Steam Workshop item, the isolated launcher SHALL NOT leave RimWorld to select an arbitrary copy. It SHALL inspect each matching `About/About.xml` read-only and proceed only when exactly one copy declares support for the current RimWorld major version. Before process start it SHALL publish that selected item as a marker-owned temporary physical copy directly beneath RimWorld's local `Mods` directory, so RimWorld's local-mod precedence is deterministic without weakening the Gateway's integration-test prohibition on reparse-point traversal. Dry-run SHALL report the selected Workshop item, source, and planned local path without publishing it. The launcher SHALL retain the publication and cleanup evidence, remove only its exact owned copy and marker after the exact game process is confirmed stopped, and leave the Workshop source unchanged. If no unique current-version candidate exists, or ownership/cleanup validation fails, the launcher SHALL fail closed rather than selecting, replacing, or recursively deleting an unowned path.

#### Scenario: One current and one legacy copy are installed
- **WHEN** an active package ID resolves to multiple Workshop items and exactly one declares the running RimWorld major version
- **THEN** dry-run identifies that exact item, the real run loads it through an owned local physical copy, and shutdown removes only that copy and its marker after the exact process exits

#### Scenario: Duplicate current copies are ambiguous
- **WHEN** two or more matching Workshop items declare the running RimWorld major version, or none does
- **THEN** the launcher rejects the configuration before process start and reports all relevant Workshop item IDs without modifying Workshop content

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
- **THEN** the host clears the generated pawn's randomized inventory, creates and selects a caravan containing only the intended covered meal with its exact plate embedded and cutlery, shows the native Items tab without a separate pre-ingestion plate row, captures the ready scene, arms the pawn's hunger, and pauses so native ingestion can be observed returning that plate as a new inventory row

#### Scenario: Animal caravan exclusion setup is selected
- **WHEN** the exact mod list includes `fumblesneeze.immersivechefs` and the caller selects `immersive-chefs-animal-caravan-dining`
- **THEN** the host creates a caravan with a fully fed Plants-0 human escort, a starving Labrador retriever, an intentionally cold, poor, contaminated plated meal, and loose cutlery; captures the native Items scene without a separate plate row; and pauses so native animal ingestion can be observed returning the unused plate while leaving cutlery untouched and without competing caravan forage

#### Scenario: Map animal exclusion setup is selected
- **WHEN** the exact mod list includes `fumblesneeze.immersivechefs` and the caller selects `immersive-chefs-animal-map-dining`
- **THEN** the host places a hungry factionless wild raccoon beside an intentionally cold, poor, contaminated plated meal and reachable loose cutlery in a sealed fixture on the current map; captures the ready scene without a separate plate; and pauses so the animal's native ingest job can be observed returning the clean plate at the eating location without reserving, collecting, dirtying, or moving the cutlery and without colonist hauling or competing animal food selection

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

### Requirement: Native E2E save and reload action
The attributed E2E contract SHALL expose a typed save-and-reload action that accepts only a safe leaf save name. The Gateway SHALL invoke RimWorld's native `GameDataSaveLoader.SaveGame` and `GameDataSaveLoader.LoadGame` operations on the main thread only from a player-controlled playable game. It SHALL fail when native saving is disabled, refuse to overwrite an existing isolated save, and verify the new file is nonempty before loading it. Completion SHALL require a different `Current.Game` instance, restored player control and current playable map, and no current or queued long event. Frame and wall-clock deadlines SHALL remain authoritative because loading may reset the game-tick clock. The attributed test SHALL own and delete its exact isolated save through deferred cleanup.

#### Scenario: Existing save name is rejected
- **WHEN** an E2E test requests a save leaf that already exists in its isolated save-data folder
- **THEN** the action fails before saving or loading and leaves the existing file unchanged

#### Scenario: Visible object survives native save and reload
- **WHEN** a focused E2E test records a visible Thing, invokes the typed save-and-reload action, reacquires that Thing by stable game identity, and observes it after load
- **THEN** the test continues only after the replacement game and playable map settle and can retain before/after screenshots and checkpoints for the same observable object

### Requirement: Exact native E2E window acceptance
The attributed E2E contract SHALL expose a typed exact-window accept action that requires player control and exactly one open window with the declared runtime type. The Gateway SHALL invoke that window's native `OnAcceptKeyPressed` path on the Unity thread under an accept-key event, restore the prior Unity event in guaranteed cleanup, and report success only after the exact window closes. It SHALL remain usable from a minimized background launch without process-scoped keyboard injection.

#### Scenario: Background E2E confirms a native message box
- **WHEN** a minimized E2E run names the sole open `Verse.Dialog_MessageBox` and requests exact-window acceptance
- **THEN** the native accept callback runs, the dialog closes, foreground focus is unchanged, and the action records the accepted runtime type

#### Scenario: Exact accept target is absent or ambiguous
- **WHEN** no open window or more than one open window has the declared runtime type
- **THEN** the action fails before invoking any window callback

### Requirement: Path-safe durable Gateway artifacts
The Gateway SHALL atomically commit session manifests, request-journal snapshots, integration-test snapshots, and E2E snapshots without deriving the temporary leaf from the complete destination filename. Its short unique sibling temporary leaf SHALL keep the complete temporary path within the legacy Windows path limit whenever the final artifact path itself fits that limit. It SHALL establish exclusive ownership before cleanup, leave a colliding sibling untouched, and retry name collisions only for a bounded count. A failed E2E attachment SHALL remain retryable without discovery or execution, but a deterministic temporary-path overflow SHALL NOT trap a healthy run in an endless pending loop.

#### Scenario: Deep isolated evidence root remains attachable
- **WHEN** an isolated launcher places the session beneath a deep artifact root whose final Gateway artifact paths fit the legacy Windows path limit but appending a GUID suffix to their full filenames would exceed it
- **THEN** each writer uses a shorter unique sibling temporary leaf, the session and initial E2E snapshot commit atomically, request journaling remains available, and the authenticated E2E endpoint advances beyond its retryable pending response

#### Scenario: Temporary sibling collision is not ownership
- **WHEN** a generated short temporary leaf already belongs to another file
- **THEN** the writer leaves that file unchanged, selects another leaf within a bounded retry count, and removes only the sibling it exclusively created

### Requirement: Normal mod configuration preservation
Automated launch SHALL use an isolated save-data folder and isolated `ModsConfig.xml`. If a future host operation is explicitly configured to modify the normal `ModsConfig.xml`, it SHALL back up and hash the exact file first, restore it in `finally`, and verify the restored hash before reporting success.

#### Scenario: Isolated quickstart completes
- **WHEN** the normal RimWorld configuration has a known hash and a quickstart run succeeds or fails
- **THEN** the normal configuration retains the same hash and the evidence identifies the isolated configuration used

### Requirement: Unobtrusive background launch
The Gateway host launcher SHALL write an isolated `Prefs.xml` that sets RimWorld's `runInBackground` preference to `True`, `volumeMusic` to `0`, and a deterministic windowed render size, and SHALL pass the matching explicit windowed size to Unity before it creates the owned game window. The default size SHALL be 1600x900 with fullscreen disabled. The launcher SHALL start the owned game process minimized by default so Unity does not first create a foreground full-desktop surface and only minimize it afterward. It SHALL hash the user's normal `Prefs.xml` before and after every dry or real run, report both hashes, and fail a real run if that file changed. The dry-run and completed-run results SHALL identify the isolated preference file, music volume, render size, fullscreen mode, Unity window arguments, and effective requested launch window style. After starting the owned process, the launcher SHALL NOT query, compare, classify, warn on, or enforce native window state because the user may restore or maximize the window at any time. Process ownership, readiness, and health checks remain authoritative. A caller MAY explicitly request a normal visible window when desktop UI or computer-use interaction is required, and the `gateway-regression` scenario SHALL select that visible style automatically because it owns FlaUI and raw-input probes.

#### Scenario: Quiet product verification continues in the background
- **WHEN** the caller starts a default, quicktest, or product-specific Gateway run without requesting desktop UI interaction
- **THEN** the isolated preference enables background execution with music muted, Unity receives matching windowed 1600x900 startup arguments before creating its window, the process is requested to start minimized, the Gateway remains available for semantic control and observation, and the user's normal preferences retain their original hash

#### Scenario: Minimized startup does not flash a full-desktop window
- **WHEN** Unity creates the exact owned window during a default background launch
- **THEN** fullscreen is already disabled at the Unity startup boundary, the first observed native window is not a foreground full-desktop normal/maximized surface, and no later window-state monitor is installed

#### Scenario: User changes the window after minimized launch
- **WHEN** the launcher requests its default minimized style and the user restores or maximizes the exact owned RimWorld window at any later point
- **THEN** the launcher performs no window-state probe and the healthy run continues through normal verification and cleanup

#### Scenario: Desktop interaction requires a visible window
- **WHEN** the caller requests a visible window or selects `gateway-regression`
- **THEN** the owned RimWorld process starts with a normal visible window while retaining the isolated background-execution preference
