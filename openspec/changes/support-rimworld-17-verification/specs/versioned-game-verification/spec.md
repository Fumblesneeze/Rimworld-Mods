## ADDED Requirements

### Requirement: Explicit supported runtime selection
Repository verification tooling (`tools/RimWorldModding.Mcp`) SHALL own this capability. It SHALL resolve game version, executable, managed reference directory and target framework consistently before building, deploying or launching. Supported layouts are 1.6 Unity/Mono and 1.7 Waylith. Unknown versions, missing required files and malformed version input MUST fail without launching or changing normal configuration.

#### Scenario: Legacy layout
- **WHEN** an intact 1.6 installation is selected
- **THEN** resolution returns `RimWorldWin64.exe`, `RimWorldWin64_Data/Managed`, and `net480` without opting unrelated products into a newer framework.

#### Scenario: Beta layout
- **WHEN** an intact 1.7 installation is selected
- **THEN** resolution returns `RimWorld.exe`, the game root managed directory, and `net10.0`, and compilation excludes platform runtime DLLs from the explicit game-reference set.

#### Scenario: Incomplete or unsupported installation
- **WHEN** version input is unsupported or the selected executable, primary assembly or runtime-specific shim is absent
- **THEN** resolution reports the exact invalid installation and no deployment or game launch occurs.

### Requirement: Isolated versioned verification
The tooling SHALL preserve the existing authenticated loopback transport, bounded dispatch, exact-PID/start ownership, minimal ordered mod list, disposable savedata, dedicated logs, package independence and credential cleanup on both runtime paths. It MUST record the actual game version and tested package hashes. It MUST NOT edit the user's normal ModsConfig or preferences; before/after hashes SHALL verify this. Graceful shutdown MUST precede bounded exact-owned-process fallback. Installation MUST remain limited to canonical local package folders, never Workshop.

#### Scenario: Native beta workflow
- **WHEN** the reviewed Gateway is built and launched against 1.7 in a fresh minimized leased process
- **THEN** an authenticated native command changes observable player state, personally inspected before/action/after screenshots show that change, an ordinary pawn command can complete under native simulation, and shutdown retains token-free evidence plus unchanged normal configuration hashes.

#### Scenario: Runtime mismatch
- **WHEN** configured runtime identity and actual loaded game identity differ
- **THEN** the run fails rather than attributing its evidence to the configured build.

### Requirement: Runtime-correct code execution and test staging
Opted-in Gateway and game-facing test projects SHALL compile for the selected runtime without globally retargeting project references or unrelated products. Uploaded diagnostics SHALL use matching game references/framework. Every required runtime dependency SHALL be pinned and inventoried; product mods MUST remain independent of Gateway. A successful host build or diagnostic MUST NOT count as live gameplay acceptance.

#### Scenario: Beta diagnostic execution
- **WHEN** bounded code is submitted through the authenticated 1.7 Gateway
- **THEN** it executes on the required game thread with request correlation and bounded result handling, and any uploaded assembly uses the selected .NET 10 target.

#### Scenario: Synchronous REPL thread affinity
- **WHEN** a beta REPL submission contains `await`
- **THEN** it is rejected before executing any statement because the synchronous endpoint cannot safely resume asynchronous continuations on the game thread; multi-frame work uses dispatcher-backed automations instead. This is an execution-model restriction, not a sandbox: explicitly created tasks, threads or blocking user code remain unrestricted and unsafe.

#### Scenario: Version-specific test bundle
- **WHEN** an opted-in game-facing test is staged for 1.7
- **THEN** only its selected runtime bundle is discovered outside ordinary product Assemblies, with exact identity/hash checks and guaranteed lease cleanup.

### Requirement: Explicit isolated fixture settings and beta package inspection
The tooling SHALL accept an optional artifacts-contained prelaunch mod-settings directory on exact E2E operations, apply the existing bounded XML-copy policy only to disposable save data, and retain hashes. Normal configuration SHALL remain untouched. Package validation SHALL select the real runtime layout and exact platform assembly inventory, preserving rejection of undeclared product and Gateway dependencies.

#### Scenario: Oversized-map fixture configuration
- **WHEN** an exact E2E run names a valid contained mod-settings fixture
- **THEN** dry-run reports it without launch or copy, and a real run copies only allowed XML settings into its unique savedata before starting minimized and muted.
- **AND** shared read-only validation rejects malformed XML, DTDs, reserved configuration names, directories, reparse points and oversized inputs before build/discovery or copying.

#### Scenario: Invalid fixture or runtime dependency
- **WHEN** a fixture escapes artifacts or a beta product references an undeclared non-platform assembly
- **THEN** validation fails before acceptance and neither normal settings nor the installed Workshop content is changed.

### Requirement: Explicit generated-map fixture isolation
The tooling SHALL support a typed opt-in generated-map E2E fixture for large native worlds. It SHALL admit that policy only for a single selected test in a fresh disposable process. Default tests SHALL retain the existing empty-map reset. The generated-map policy SHALL preserve baseline Things, roofs, zones and designations instead of globally destroying them, clean newly created disposable Things and transient UI/notifications, and restore captured control/camera state. The fixture SHALL register restoration of its own terrain/roof/fog edits. Baseline natural objects removed within a fixture are disposable process environment, not claimed to have been restored; no later test may reuse that map.

#### Scenario: Large generated map comparison
- **WHEN** one explicitly opted-in generated-map test is selected through `e2e_run_start`
- **THEN** its native generated map reaches fixture arrangement without deleting every natural object, native player actions remain unchanged, cleanup removes run-created objects and restores control state, and the exact owned process is closed.
- **AND** the internal runner honors the typed operation's existing timeout range through 3600 seconds; a longer declared workload does not silently inherit the obsolete 600-second adapter ceiling.

#### Scenario: Mixed generated-map group
- **WHEN** a generated-map fixture is selected alongside another test in the same process
- **THEN** execution fails before destructive arrangement rather than weakening the later test's baseline.

### Requirement: Explicit user playtest lifetime
The typed `game_run_start` operation SHALL accept the same isolated prelaunch mod-settings fixture and an explicit `userPlaytest` flag, default false. Only a user-requested interactive playtest may set this flag. It SHALL launch minimized with the same exact-process and normal-configuration protections, but SHALL NOT automatically terminate a ready playtest on the verification hold deadline. The launcher SHALL remain to perform credential/process evidence cleanup after the user exits, or after an explicit exact-run cancellation. Saved games SHALL be retained. This mode is not disposable test acceptance.

#### Scenario: User continues building beyond the hold deadline
- **WHEN** a user-owned playtest remains alive past the normal verification hold deadline
- **THEN** the launcher continues observing without closing or focusing it, and retains the isolated save directory for the user.

#### Scenario: User exits or explicitly cancels
- **WHEN** the user exits the exact playtest process, or requests cancellation of its exact run
- **THEN** the launcher completes cleanup without deleting saves or changing normal configuration; normal user exit is not an unexpected verification failure.
