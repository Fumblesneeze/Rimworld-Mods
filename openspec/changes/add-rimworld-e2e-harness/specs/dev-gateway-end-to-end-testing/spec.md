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

#### Scenario: Three tests share two mod combinations
- **WHEN** two tests declare the same Core/Harmony/product sequence and a third also declares one optional mod
- **THEN** the runner starts two processes
- **THEN** the first matching process runs the two same-group tests sequentially and the second runs the optional-mod test

#### Scenario: Runtime order differs from discovery
- **WHEN** the actual loaded package set is missing, adds, or reorders any declared package
- **THEN** the Gateway loads no mismatched E2E test and the group fails before test arrangement

### Requirement: E2E loading is explicit and dynamic
The Gateway SHALL discover, validate, byte-load, and execute marker-owned E2E bundles below active mods only when the exact E2E startup flag is present. RimWorld SHALL never build test source. Bundle publication and cleanup SHALL be atomic and limited to exact owned staging directories, and every loaded assembly SHALL match the host-retained identity, hash, owner, and metadata-derived test list.

#### Scenario: Gateway starts normally
- **WHEN** the Gateway is loaded without the E2E startup flag
- **THEN** it neither enumerates E2E manifests nor loads an E2E assembly nor clears the playable map

#### Scenario: A staged bundle drifts after discovery
- **WHEN** a staged assembly's bytes, identity, attribute list, or owner differ from the host descriptor
- **THEN** the runtime rejects that bundle without executing any of its tests

### Requirement: E2E tests execute as bounded multi-frame workflows
The E2E contract SHALL separate main-thread fixture arrangement from an iterator of typed `act`, `wait`, and `observe` steps. The Gateway SHALL advance the iterator without blocking frame rendering, SHALL execute Unity/Verse access only on the main thread, and SHALL enforce test-declared frame, game-tick, and wall-clock deadlines plus a host watchdog. Supported action steps SHALL include native gizmos selected from exact Thing owners and/or exact architect category Def names, exact float-menu orders, pause/speed, selection, camera, and process-scoped input; supported observation steps SHALL include predicate assertions, full or object-bounded screenshots, and named checkpoints.

The shared host-safe contract SHALL expose a read-only native float-menu catalog service through the E2E context. A query SHALL return each current actor/target option's visible label, disabled state, and callback-sensitive stable identity without exposing or retaining its RimWorld callback. Tests SHALL invoke the chosen result only through the typed float-menu action step, and the Gateway SHALL re-query and require exactly one enabled stable-identity match before running the native callback.

#### Scenario: A pawn must finish a real job
- **WHEN** arrangement creates a drafted pawn and fixtures, an action step invokes the native Undraft command, and a wait step watches the ordinary job outcome
- **THEN** rendered frames and game ticks continue while the pawn's normal scheduler and job driver run
- **THEN** the test resumes at its observable assertions only after the predicate succeeds or its declared deadline fails

#### Scenario: Test code attempts no player action
- **WHEN** a fixture directly constructs the claimed end state but records no native action between its before and after observations
- **THEN** the result cannot satisfy the player-workflow acceptance classification even if its state assertion passes

#### Scenario: A test discovers a native ingestion option
- **WHEN** a dynamically loaded product test queries the current pawn and meal through the shared float-menu catalog
- **THEN** it can select one unambiguous enabled visible option and submit the returned stable identity without referencing Gateway implementation assemblies
- **THEN** a stale, disabled, missing, or ambiguous option fails closed without assigning a synthetic ingestion job

### Requirement: The map is empty and verified between tests
Before the first test and in guaranteed cleanup after every test, the runner SHALL pause the game and remove all destroyable spawned map Things and Pawns, jobs, zones, designations, selections, active interactions, test-opened windows, and registered scenario-owned world objects. Permanent non-destroyable map features such as steam geysers are part of the map environment, MUST NOT be destroyed, and MUST be excluded from the disposable-state emptiness check. The runner SHALL restore developer/god mode, speed, camera, and pressed input to the process baseline and verify the disposable map is empty before arranging the next test. Tests SHALL be able to register additional cleanup actions for mod-specific global state.

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
An assertion, test exception, unsupported native action, stale handle, or ordinary test timeout SHALL fail only the current test when map reset remains trustworthy. Each failure SHALL retain the current step, bounded causal exception, live job/target and selection checkpoint when available, final screenshot, log cursor page, and cleanup result. A test failure MUST NOT be converted into a process success merely because later cleanup passed.

#### Scenario: One test fails and cleanup succeeds
- **WHEN** the first test in a group fails an observable assertion and the reset returns to a verified empty map
- **THEN** the second test still runs
- **THEN** the aggregate contains one failed and one independently evaluated result

#### Scenario: RimWorld exits during a test
- **WHEN** the exact process exits before terminal persistence
- **THEN** the host synthesizes explicit aborted results for the active and unrun tests and retains the flushed process log and cleanup status

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
