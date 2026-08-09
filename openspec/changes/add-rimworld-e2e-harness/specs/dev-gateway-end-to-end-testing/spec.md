## ADDED Requirements

**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: E2E tests are separate attributed runtime fixtures
The repository SHALL provide a separately packaged `RimWorldDevGateway.EndToEndTesting` contract. Every E2E test SHALL be a concrete attributed test type with a globally stable test ID, one owning package ID, and the complete ordered active package sequence required by that test, excluding only the Gateway package which the host appends last. A non-Gateway owner MUST appear in that declared sequence. A Gateway-owned infrastructure test MUST declare `fumblesneeze.rimworlddevgateway` as owner, MUST rely on the one implicit final Gateway package, and MUST NOT list Gateway in the sequence. E2E projects and assemblies MUST remain outside ordinary NUnit/VSTest registration, product `Assemblies` directories, and release packages.

#### Scenario: A product declares an E2E workflow
- **WHEN** a test type implements the E2E contract and declares Core, Harmony, its owning product, and an optional integration in exact order
- **THEN** host discovery reports that stable test under exactly that non-Gateway mod group
- **THEN** the product's ordinary release artifact contains neither the E2E assembly nor a Gateway reference

#### Scenario: A test declaration is ambiguous
- **WHEN** a test ID is duplicated, a non-Gateway owner is absent from its package sequence, an owner is neither present nor the exact implicit Gateway owner, Gateway is listed explicitly, or its package sequence is empty, duplicated, missing, or unresolvable
- **THEN** discovery fails before launching RimWorld with an actionable validation error

#### Scenario: Gateway owns an infrastructure self-test
- **WHEN** a Gateway-owned test declares Core as its exact non-Gateway sequence and `fumblesneeze.rimworlddevgateway` as owner
- **THEN** discovery accepts the owner through the implicit final Gateway entry and the launched active list is exactly Core followed by Gateway

### Requirement: The host launches one isolated process per exact mod group
The host runner SHALL discover all selected E2E tests before launch, group them by ordinal package sequence, and start exactly one fresh isolated quickstart RimWorld process for each distinct group. The active list in that process SHALL equal the declared sequence followed by `fumblesneeze.rimworlddevgateway`; no downloaded-but-undeclared package may participate. Groups and tests SHALL execute in deterministic order and a process SHALL never be reused for another group.

The runner SHALL accept one or more exact stable test-ID filters independently from its exact mod-group filter. It SHALL validate every requested ID against the complete discovery plan before mutation, launch only groups containing a selected test, and pass each group's exact selected IDs through a restart-bound Gateway startup selection. Runtime discovery SHALL still validate the complete staged bundle and active package order, but SHALL admit and execute only those exact IDs. An unknown, duplicated, invalid, mismatched, unadmitted, or additionally executed ID SHALL fail closed. A run without test-ID filters retains the complete-group behavior for deliberate regression and release verification.

#### Scenario: One current scenario is selected from a mature group
- **WHEN** the operator supplies the stable ID of one E2E test whose exact mod group contains several already-accepted tests
- **THEN** the host launches that group once and the Gateway admits, executes, reports, and cleans exactly the selected test
- **THEN** no other test in that group is arranged or executed

#### Scenario: A requested test ID is not admitted
- **WHEN** a selected stable ID is unknown at host planning or absent after runtime bundle and active-mod validation
- **THEN** the run fails before test arrangement and never substitutes a same-group or similarly named test

#### Scenario: Three tests share two mod combinations
- **WHEN** two tests declare the same Core/Harmony/product sequence and a third also declares one optional mod
- **THEN** the runner starts two processes
- **THEN** the first matching process runs the two same-group tests sequentially and the second runs the optional-mod test

#### Scenario: Runtime order differs from discovery
- **WHEN** the actual loaded package set is missing, adds, or reorders any declared package
- **THEN** the Gateway loads no mismatched E2E test and the group fails before test arrangement

#### Scenario: A group has several additional mods and a long descriptive identity
- **WHEN** the host launches the exact group through a child PowerShell process
- **THEN** it passes the ordered additional package IDs through one UTF-8 file rather than repeating an array parameter on the command line
- **THEN** the runtime save-data branch uses a short group-independent name while the descriptive report retains the complete group identity
- **THEN** XML-invalid terminal control characters are replaced in JUnit projection without rewriting the retained stderr artifact

### Requirement: E2E loading is explicit and dynamic
The Gateway SHALL discover, validate, byte-load, and execute marker-owned E2E bundles below active mods only when the exact E2E startup flag is present. RimWorld SHALL never build test source. Bundle publication and cleanup SHALL be atomic and limited to exact owned staging directories, and every loaded assembly SHALL match the host-retained identity, hash, owner, and metadata-derived test list.

#### Scenario: Gateway starts normally
- **WHEN** the Gateway is loaded without the E2E startup flag
- **THEN** it neither enumerates E2E manifests nor loads an E2E assembly nor clears the playable map

#### Scenario: A staged bundle drifts after discovery
- **WHEN** a staged assembly's bytes, identity, attribute list, or owner differ from the host descriptor
- **THEN** the runtime rejects that bundle without executing any of its tests

### Requirement: E2E tests execute as bounded multi-frame workflows
The E2E contract SHALL separate main-thread fixture arrangement from an iterator of typed `act`, `wait`, and `observe` steps. Synchronous arrangement and iterator-side fixture maintenance SHALL use bounded candidate sets and fixed-cost setup operations; a map-wide sort/search, long-running generation loop, blocking delegate, or other setup that can cross Windows' hung-window interval SHALL instead be bounded or split across frames before it is admitted as an E2E fixture. The Gateway SHALL NOT expose a generic synchronous callback step as an action. The Gateway SHALL wait without capturing or clearing an isolation baseline until the quickstart game reports native player control, SHALL bound that wait by the test's declared deadline, SHALL advance the iterator without blocking frame rendering, SHALL execute Unity/Verse access only on the main thread, and SHALL enforce test-declared frame, game-tick, and wall-clock deadlines plus a host watchdog. Supported action steps SHALL include native gizmos selected from exact Thing owners and/or exact architect category Def names, exact float-menu orders, typed native settlement-trade, trade-dialog, registered exact-window confirmation, incident, and save/reload operations, pause/speed, selection, camera, and process-scoped input; supported observation steps SHALL include predicate assertions, full or object-bounded screenshots, and named checkpoints. The shared read-only gizmo catalog SHALL preserve the selected descriptor's observed toggle state and exact hotkey Def name when available, while retaining the original constructor contract for already-built E2E bundles.

A typed gizmo `Place` step MAY declare one exact cardinal rotation. The shared contract SHALL permit that option only for a cell-shaped placement step, and the runtime SHALL project it to the Gateway's native place-designator interaction rather than mutate a resulting Thing. Other gizmo interaction kinds SHALL reject the option during contract validation.

#### Scenario: An E2E test performs a rotated native placement
- **WHEN** a test selects one exact Architect place designator and applies it to one cell with `East`
- **THEN** the runtime passes the cardinal choice into the native placement interaction before preflight and designation
- **THEN** later waits, queries, and screenshots can observe the east-facing placed building

A typed Architect-category action SHALL accept one exact loaded `DesignationCategoryDef` and an
open/close intent. Opening SHALL use RimWorld's native Architect main-button activation path, select
the exact category through the current Architect tab's own cached category object and inspected
category-click method, and require the requested tab/category to be active afterward. Closing SHALL
use the native current-tab escape path and require Architect to be closed afterward. Missing player
control, missing or duplicate category tabs, method-shape drift, or a rejected tab transition SHALL
fail closed. The action SHALL NOT restore, focus, resize, maximize, or use process input. A later
screenshot and read-only architect designator query SHALL prove player-visible menu readability and
the exact buildable identities.

#### Scenario: A minimized visual catalog opens the real Production menu
- **WHEN** a focused product E2E test opens the exact Production Architect category through the typed action
- **THEN** the current native build menu is visibly open while the isolated RimWorld process remains minimized
- **THEN** the exact category designator catalog contains the product buildings and a retained screenshot proves their labels/icons are readable
- **THEN** the typed close action leaves no Architect tab open without foreground input

A typed trade-dialog adjustment SHALL require exactly one open native `Dialog_Trade`, resolve exactly one current `Tradeable` containing the supplied exact Thing runtime ID, validate the requested nonzero bounded delta against that tradeable's native minimum and maximum, invoke its native nonpublic count setter, and invoke the current dialog's native count-changed refresh. Typed acceptance SHALL re-resolve exactly one current dialog and invoke the exact native Accept callback generated by that RimWorld build. Neither action SHALL restore, focus, resize, or otherwise mutate the native window; neither SHALL set private count fields directly, call `TradeDeal.ResolveTrade` itself, close the dialog independently, or silently fall back to pixel input. Missing, ambiguous, stale, out-of-range, or version-drifted members SHALL fail closed. A later wait/assertion and screenshot SHALL prove the native dialog and trade deal produced the expected observable result.

A typed optional-mod dialog confirmation SHALL expose only the expected exact open-window type through the host-safe test contract. The Gateway SHALL retain the private allowlisted adapter shape, including exact assembly identity, confirmation callback, and argument fields; the test bundle SHALL NOT supply callback or field names and an unregistered window SHALL have no reflective fallback. The adapter SHALL require exactly one matching open window, a void-returning callback, and assignable exact argument values, SHALL close the dialog before invoking the callback when that is the inspected native button order, and SHALL fail closed on any identity, cardinality, or shape drift. It SHALL neither restore, focus, resize, nor foreground RimWorld. A later wait/assertion and screenshot SHALL prove the ordinary dialog action produced the claimed player-visible result.

The shared host-safe contract SHALL expose a read-only native float-menu catalog service through the E2E context. A query SHALL return each current actor/target option's visible label, disabled state, and callback-sensitive stable identity without exposing or retaining its RimWorld callback. Tests SHALL invoke the chosen result only through the typed float-menu action step, and the Gateway SHALL re-query and require exactly one enabled stable-identity match before running the native callback.

A typed native incident action SHALL require a playable current map and one exact loaded `IncidentDef` name. An optional faction load ID SHALL resolve to exactly one currently loaded faction without imposing positivity or player-faction assumptions. The Gateway SHALL construct RimWorld's current native storyteller parameters, mark the incident forced, apply the exact faction when supplied, and invoke the real `IncidentWorker.TryExecute`. A missing map, Def, or faction, a false native result, or an exception SHALL fail closed with bounded token-safe diagnostics. The action SHALL NOT expose an arbitrary callback, queue a guessed storyteller event, use translated labels, or substitute desktop input.

A typed native save-and-reload action SHALL accept only a canonical Windows leaf save name bounded to preserve room beneath the 255-code-unit component limit for RimWorld's extension and recovery suffixes. It SHALL reject whitespace normalization, trailing dot/space aliases, DOS device basenames, path separators, invalid filename characters, and an existing isolated save. From a player-controlled playable game with saving enabled, the Gateway SHALL call RimWorld's native save operation once, require the exact resulting save to exist and be nonempty, and then call RimWorld's native load operation once. The action SHALL complete only after `Current.Game` is a different instance, player control and a current map return, and no long event is active or queued. Native exceptions SHALL fail closed without tokens or arbitrary exception text. The attributed test SHALL register deferred cleanup for the exact isolated save, reacquire objects by stable game identity after replacement, and retain before/after screenshots or checkpoints; frame and wall-clock limits remain authoritative when loading resets game ticks.

#### Scenario: An E2E workflow crosses the native persistence boundary
- **WHEN** a focused product test observes a visible object, saves and reloads through the typed action, reacquires it by stable identity, and observes it again
- **THEN** the Gateway performs one native save and load without overwriting existing data and resumes the iterator only in the settled replacement game

#### Scenario: A requested save name aliases or already exists
- **WHEN** the requested leaf is noncanonical, reserved by Windows, over the documented component bound, or already exists in the isolated save folder
- **THEN** the action fails before loading and does not overwrite or reinterpret the target

#### Scenario: A pawn must finish a real job
- **WHEN** arrangement creates a drafted pawn and fixtures, an action step invokes the native Undraft command, and a wait step watches the ordinary job outcome
- **THEN** rendered frames and game ticks continue while the pawn's normal scheduler and job driver run
- **THEN** the test resumes at its observable assertions only after the predicate succeeds or its declared deadline fails

#### Scenario: Fixture arrangement searches for clear map cells
- **WHEN** a test needs a small fixed number of separated fixture locations on the disposable map
- **THEN** arrangement scans a bounded deterministic candidate set, returns control before the native window is treated as hung, and leaves multi-frame waiting to typed steps

#### Scenario: Quickstart has a map before it grants player control
- **WHEN** an E2E bundle is admitted while the generated map exists but `Game.PlayerHasControl` is still false
- **THEN** the runner leaves the map untouched and retries readiness on later frames without recording an isolation failure
- **THEN** readiness that remains false beyond the test deadline fails as `player_control_not_ready`, taints that process, and runs no unowned cleanup against an uncaptured baseline

#### Scenario: A fixture needs a just-in-time deterministic precondition
- **WHEN** iterator-side fixed-cost setup adjusts bounded test-only state after a persisted wait boundary and before native completion
- **THEN** no synthetic action is recorded and acceptance still requires a separate native player action plus visible observation
- **THEN** arbitrary synchronous callback steps remain unavailable because their wall-clock deadline cannot preempt a blocked Unity main thread

#### Scenario: Test code attempts no player action
- **WHEN** a fixture directly constructs the claimed end state but records no native action between its before and after observations
- **THEN** the result cannot satisfy the player-workflow acceptance classification even if its state assertion passes

#### Scenario: A test discovers a native ingestion option
- **WHEN** a dynamically loaded product test queries the current pawn and meal through the shared float-menu catalog
- **THEN** it can select one unambiguous enabled visible option and submit the returned stable identity without referencing Gateway implementation assemblies
- **THEN** a stale, disabled, missing, or ambiguous option fails closed without assigning a synthetic ingestion job

#### Scenario: A minimized orbital trade dialog is operated
- **WHEN** a test adjusts the exact trader-owned meal by one within its native transfer range and invokes the native Accept callback while the isolated RimWorld process remains minimized
- **THEN** the native trade deal records and executes that exact purchase while the Gateway neither restores nor foregrounds the window

#### Scenario: A minimized optional-mod confirmation dialog is operated
- **WHEN** a test opens Replimat's registered survival-batch dialog through its native terminal gizmo and confirms its default one-meal value
- **THEN** the exact native callback creates one packaged survival meal while the Gateway exposes no generic callback surface and neither restores nor foregrounds the process

#### Scenario: A caravan opens settlement trade without desktop input
- **WHEN** a test identifies one player caravan and one visited settlement by their exact live world-object IDs
- **THEN** the Gateway requires both exact objects at the same tile, obtains RimWorld's native `CaravanVisitUtility.TradeCommand`, requires its enabled action shape, and invokes that command to open exactly one settlement `Dialog_Trade`
- **THEN** missing, ambiguous, wrong-type, wrong-tile, nonvisited same-tile target, disabled, or changed command shapes fail closed without using translated labels, pixel input, focus, restore, or maximize operations
- **THEN** a negative E2E step MAY require one exact expected failure code, passes only when that code is observed, and retains both the expected and observed codes without converting any other failure or successful open into a passing result

#### Scenario: A native trader caravan incident exercises a compatibility boundary

- **WHEN** a test supplies exact `TraderCaravanArrival` and the exact load ID of one eligible non-player faction
- **THEN** the Gateway invokes RimWorld's native incident worker once with forced current-map parameters and that exact faction
- **THEN** the test waits for ordinary caravan entry and observes the resulting pawns and arrival letter, while missing, ambiguous, rejected, or version-drifted state fails closed without a generic callback or synthetic caravan

### Requirement: The map is empty and verified between tests
After native player control becomes available, before the first test and in guaranteed cleanup after every test, the runner SHALL pause the game and remove every roof cell, including constructed roofs and overhead mountain, before it destroys any map content. It SHALL then remove all destroyable spawned map Things and Pawns, jobs, zones, designations, selections, active interactions, test-opened windows, and registered scenario-owned world objects. After removal has finished, the runner SHALL clear every current live message, visible or delayed letter, and active alert-readout entry, and SHALL verify those notification surfaces are empty before arranging the next test. Clearing SHALL use RimWorld's public removal path where one exists; any required private delayed-letter or alert collection shape SHALL stay exact and Gateway-owned and SHALL fail closed on version drift. Newly arranged scenario conditions MAY generate fresh alerts normally. Permanent non-destroyable map features such as steam geysers are part of the map environment, MUST NOT be destroyed, and MUST be excluded from the disposable-state emptiness check. The runner SHALL restore developer/god mode, speed, camera, and pressed input to the process baseline and verify both that no roof remains and that the disposable map and notification slate are empty before arranging the next test. Tests SHALL be able to register additional cleanup actions for mod-specific global state.

#### Scenario: A generated map contains unsupported natural and constructed roofs
- **WHEN** the runner prepares or cleans a test on a map containing constructed roofs or overhead mountain
- **THEN** every roof is removed before any wall, support, pawn, item, or other map Thing is destroyed
- **THEN** no roof-collapse damage, alert, or leaked death contaminates the next test

#### Scenario: Initial removal or a completed test leaves player notifications
- **WHEN** map removal produces messages, visible or delayed letters, or active alert-readout entries before the first test or between sequential tests
- **THEN** notification cleanup runs after the map-content phases and clears every current entry
- **THEN** the empty-baseline verification fails closed unless all three notification surfaces are empty
- **THEN** a later alert-sensitive scenario starts from a clean slate and can observe only alerts generated by its own arranged conditions

#### Scenario: A test fails after spawning fixtures
- **WHEN** an assertion throws while pawns, buildings, items, filth, zones, or jobs remain
- **THEN** cleanup still removes them and verifies the empty baseline before the next test starts

#### Scenario: Reset cannot prove isolation
- **WHEN** cleanup throws or any tracked/spawned fixture, interaction, or pressed input remains
- **THEN** the current test records an infrastructure failure, the process is tainted, and later tests in that group are skipped rather than run against contaminated state

#### Scenario: A quicktest map contains a permanent feature
- **WHEN** the generated map contains a non-destroyable steam geyser or equivalent permanent map Thing
- **THEN** reset preserves that feature while still removing and verifying the absence of every destroyable disposable fixture

### Requirement: Failures are isolated and diagnostically complete
An assertion, test exception, unsupported native action, stale handle, or ordinary test timeout SHALL fail only the current test when map reset remains trustworthy. Each failure SHALL retain the current step, bounded causal exception, live job/target and selection checkpoint when available, final screenshot, log cursor page, and cleanup result. Before any failure snapshot becomes persistable, the runtime SHALL redact the exact live session credential from every failure field and cap the failure message to the same 8 KiB UTF-8 diagnostic-message budget used by Gateway log entries; identity and stack fields SHALL remain separately bounded. A test failure MUST NOT be converted into a process success merely because later cleanup passed.

#### Scenario: One test fails and cleanup succeeds
- **WHEN** the first test in a group fails an observable assertion and the reset returns to a verified empty map
- **THEN** the second test still runs
- **THEN** the aggregate contains one failed and one independently evaluated result

#### Scenario: RimWorld exits during a test
- **WHEN** the exact process exits before terminal persistence
- **THEN** the host synthesizes explicit aborted results for the active and unrun tests and retains the flushed process log and cleanup status

#### Scenario: A mod returns hostile native rejection text
- **WHEN** a native player action returns an oversized rejection reason containing the live bearer credential
- **THEN** the persisted failure retains a useful deterministic prefix, replaces the credential, fits the 8 KiB UTF-8 message budget, and never invokes arbitrary exception formatting

### Requirement: Results are durable and automation-friendly
The runner SHALL emit one aggregate JSON result, one JUnit XML result, and table or JSON console output. Results SHALL identify the game version, exact PID/start identity, complete ordered mods, product/Gateway/E2E assembly identities and hashes, test and step timings, game ticks, assertions, screenshots, logs, cleanup, and before/after normal configuration hashes. Exit code `0` SHALL mean every selected test and cleanup passed, `1` SHALL mean a test/runtime/infrastructure failure, and `2` SHALL mean invalid usage or discovery/selection failure.

#### Scenario: A CI run succeeds
- **WHEN** every selected group and test passes and every process, credential, stage, input, and configuration cleanup succeeds
- **THEN** the command exits `0` and its JSON/JUnit artifacts contain the same deterministic terminal results

#### Scenario: No test matches a filter
- **WHEN** a caller supplies a test or group filter that selects zero tests
- **THEN** the command exits `2`, launches no process, and reports the unmatched filter

### Requirement: Existing gameplay scenarios migrate without weakening evidence
Manual Gateway scenarios SHALL be migrated into product-owned E2E tests by player-visible behavior. A descriptor MAY remain for interactive exploration, but duplicate automated orchestration SHALL be deleted after its E2E replacement proves the same or stronger native action and observable result on the reviewed build. The migration inventory SHALL track every existing scenario as pending, converted, interactive-only, or retired.

#### Scenario: Adverse meal outcome becomes the tracer test
- **WHEN** the first Immersive Chefs E2E test arranges an awful frozen meal with dirty cookware, plate, and cutlery and then invokes native undrafting and ordinary game time
- **THEN** the real ingestion job consumes the meal, returns the ware dirty, applies the visible awful/frozen/dirty dining thoughts, and produces food poisoning under a deterministic configured risk
- **THEN** the result retains before/action/after screenshots and the next test begins from an empty map

### Requirement: The repository workflow treats E2E as a distinct acceptance tier
`AGENTS.md`, the repo-local RimWorld development skill, README, and testing documentation SHALL explain when to use host tests, startup integration tests, E2E tests, interactive acceptance, and performance tests. A gameplay change with a repeatable multi-frame workflow SHALL add or update an E2E test, while the acting agent SHALL still personally inspect the reviewed build's native action and observable screenshots before accepting new behavior.

#### Scenario: A future agent changes a pawn job patch
- **WHEN** the change can be exercised through a bounded playable-map workflow
- **THEN** the workflow requires focused host coverage, a matching E2E regression, reviewed-build E2E execution, and personal inspection of retained behavior evidence

#### Scenario: RimWorld or a supported third-party mod updates
- **WHEN** a game or mod update may have changed internal methods, Defs, jobs, UI actions, or Harmony targets without an obvious load error
- **THEN** the documented maintenance workflow runs the relevant exact E2E groups in quick succession and uses their aggregate plus personally inspected behavior evidence to locate regressions
