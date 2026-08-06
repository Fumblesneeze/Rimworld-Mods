## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Separate shared in-game test contract
The repository SHALL provide a host-safe `RimWorldDevGateway.IntegrationTesting` contract assembly containing `IntegrationTestAttribute`, `RunAt`, and assertion helpers without depending on NUnit, VSTest, Verse, Unity, Harmony, or a product mod. An integration-test method SHALL be public, static, parameterless, return `void`, and carry `[IntegrationTest(RunAt.<point>)]`. Version one SHALL define `MainMenuLoaded` and `PlayableMapLoaded` lifecycle points.

Gameplay product runtime assemblies SHALL NOT reference this contract or any assembly whose simple name starts with `RimWorldDevGateway`, compared case-insensitively. The owning Dev Gateway runtime and separately built test assemblies MAY reference the contract; test assemblies MAY also reference their owning product mod, RimWorld, and optional-mod assemblies, and MAY inspect live `DefDatabase<T>` values or Harmony patch metadata.

Before a product-owned smoke is admitted, the host SHALL retain a deterministic ordinal relative-path inventory of every ordinary package file with its byte length and SHA-256. It SHALL inspect every managed DLL's ECMA-335 metadata without loading or executing that assembly, retain its full assembly identity and every deterministically ordered assembly-reference row, reject every metadata-bearing DLL without an assembly definition, reject every package filename matching `RimWorldDevGateway*.dll` case-insensitively, and reject every managed reference whose simple name starts with `RimWorldDevGateway` case-insensitively. A selective list of expected product files or one forbidden contract filename SHALL NOT substitute for this complete package-boundary evidence.

#### Scenario: XML patch assertion is authored
- **WHEN** a developer writes a separately compiled method marked `[IntegrationTest(RunAt.MainMenuLoaded)]` that reads a real finalized Def
- **THEN** the method can assert the expected inherited or patched field through the shared assertion helpers without being discovered by the regular NUnit host suites

#### Scenario: Product package is built normally
- **WHEN** an ordinary Release product-mod build runs without integration-test staging
- **THEN** its complete file and managed-reference inventories prove that no integration-test assembly, `RimWorldDevGateway*.dll`, or managed reference to any `RimWorldDevGateway*` assembly is present in that product mod's package

#### Scenario: Product hides a Gateway assembly dependency behind another filename
- **WHEN** an ordinary product DLL or packaged managed helper has an ECMA-335 assembly reference whose simple name starts with `RimWorldDevGateway`, even though no packaged DLL has that name
- **THEN** metadata-only package inspection rejects the product smoke before RimWorld starts

### Requirement: Explicit startup gate and active-mod discovery
The in-game runner SHALL be disabled unless RimWorld starts with the exact `-devGatewayRunIntegrationTests` command-line flag. When enabled, it SHALL use active `ModContentPack` load folders to discover only bounded `DevIntegrationTests/*.integrationtests.json` manifests, validate that each manifest's owner equals its containing active mod, and load only the exact sibling `*.IntegrationTests.dll` named by that manifest. It SHALL load byte copies so source files are not locked and resolve dependencies only from already loaded game/mod assemblies or the same staged test directory. A dependency candidate SHALL match the requested normalized simple name, version, culture, and public-key token exactly. Discovery SHALL be deterministic, bounded, limited to active mods, traversal-safe, and isolated per manifest, assembly, type, and method. Assembly loading and all potentially mod-controlled reflection (`Assembly.GetTypes`, `Type.GetMethods`, metadata ordering, and `CustomAttributeData` inspection) SHALL run in one background discovery task at a time; the Unity update loop SHALL only start or poll that task and SHALL consume a completed result without waiting on an incomplete task. Test invocation alone SHALL remain on the Unity thread. Every admitted assembly source, including a source whose load fails, SHALL count toward the assembly-attempt limit. Hard aggregate assembly, type, method, test, failure, and discovery-work limits SHALL apply inside the worker as well as when results are admitted.

The game process SHALL never invoke `dotnet`, MSBuild, a source generator, or a compiler. A host-side repository command SHALL require a nonempty complete ordered active-package sequence on every invocation, discover only projects explicitly marked `RimWorldInGameIntegrationTest=true`, evaluate their package matrices against that sequence, build them before launch against the exact selected RimWorld and Workshop paths, and stage their output into marker-owned test-mod roots before the isolated launcher copies matching bundles into each owning mod's versioned `DevIntegrationTests` folder. Omitting the active-package argument SHALL fail before build or staging; there SHALL be no unfiltered "build every opted-in project" mode. Staging and cleanup SHALL be failure-atomic and SHALL never replace or delete an unowned directory.

A manifest MAY use reusable `requiredPackageIds`/`forbiddenPackageIds` constraints, or it MAY declare `activePackageSetMode: "exact"` plus a nonempty `activePackageIds` sequence. Exact mode SHALL be mutually exclusive with nonempty required/forbidden constraints and SHALL compare package IDs case-insensitively while preserving count and position. Both the host before build/stage and the Gateway runtime before exposing an assembly load source SHALL reject a missing, extra, or reordered package. The manifest schema SHALL reject unknown and duplicate top-level properties rather than silently ignoring them.

#### Scenario: Normal RimWorld startup
- **WHEN** the Gateway is loaded without the startup flag
- **THEN** it does not scan, load, discover, or execute any staged integration-test assembly and its status reports the runner as disabled

#### Scenario: Test project belongs to an inactive mod
- **WHEN** host staging is requested for an active package set that excludes the project's declared owner package ID
- **THEN** the project is not built or staged for that run and the in-game runner cannot discover it

#### Scenario: Host omits the complete active package sequence
- **WHEN** integration-test build or staging is requested without a nonempty `ActivePackageIds` value
- **THEN** the host rejects the request before invoking `dotnet` or writing a stage

#### Scenario: Exact product matrix is reordered or widened
- **WHEN** an exact-mode manifest names Core, Harmony, its product mod, and the Gateway in that order but the requested or live active package sequence has the same members in another order or contains another package
- **THEN** the host does not build or stage that project and the runtime does not expose its assembly for loading or invocation

#### Scenario: Focused product matrix excludes completed fixture suites
- **WHEN** an exact product or compatibility matrix matches one product test project but does not match the Gateway fixture project's own exact Core-plus-Gateway matrix
- **THEN** the host stages and validates only the matching product project, accepts its nonempty complete result snapshot without requiring unstaged Gateway fixture descriptors, and still requires all four exact Gateway fixtures whenever any Gateway fixture descriptor is present

#### Scenario: Manifest adds an undeclared field or repeats a field
- **WHEN** a staged integration-test manifest contains an unknown or duplicate top-level JSON property
- **THEN** validation records an invalid-manifest failure instead of deserializing a permissive approximation

#### Scenario: One staged assembly is invalid
- **WHEN** one matching staged DLL cannot load or contains an invalid test signature
- **THEN** the runner records a bounded discovery failure and continues discovering valid assemblies and tests

#### Scenario: Mod reflection blocks
- **WHEN** assembly loading, type enumeration, method enumeration, or attribute inspection does not return
- **THEN** the Unity update loop remains responsive while polling that one worker, no second discovery worker starts, and a fresh process remains the supported cancellation/reset boundary

#### Scenario: Marked method is async void
- **WHEN** a marked method has `AsyncStateMachineAttribute` and returns `void`
- **THEN** discovery records `invalid_async_void_test` without instantiating the marker attribute and never invokes the method; `Task`-returning methods remain invalid signatures

### Requirement: Once-per-lifecycle main-thread execution
The Gateway runtime host SHALL observe native RimWorld lifecycle state on the Unity main thread. `MainMenuLoaded` SHALL fire only after play data is loaded, no long event is active or waiting, the entry root, UI root, and window stack are present, and that complete condition has remained true into the following rendered frame; `PlayableMapLoaded` SHALL fire only while the game is playing with a current map. Each discovered test SHALL execute at most once per process at its declared point, in deterministic assembly/type/method order, on the Unity main thread.

Each test invocation SHALL be exception-isolated. Assertion failures, target exceptions, discovery failures, start/end UTC timestamps, duration, lifecycle point, owning package ID, assembly identity, and test name SHALL be retained in an immutable bounded result snapshot. One background persistence lane SHALL serialize and atomically write a token-free `integration-tests.json` snapshot in the current Gateway session directory before invoking each test and after every terminal transition, so neither serialization nor disk I/O blocks Unity and a synchronous test hang leaves the exact current test and running state available to host diagnostics. Session attachment SHALL be transactional and SHALL commit only after the exact initial snapshot is durable. The runner SHALL NOT discover, invoke, or advance past a running or terminal transition until that exact immutable candidate commits. The background operation SHALL absorb a brief destination-file reader with a short bounded atomic-replace retry before reporting failure; it SHALL NOT sleep the Unity thread. A transient start/write failure SHALL retain and retry the same candidate without rerunning a test or manufacturing an `infrastructure-failed` result. At most one persistence operation SHALL be active. A failing test SHALL not stop the Gateway server or later tests. Since synchronous game-thread code cannot be safely preempted, the contract SHALL diagnose elapsed time but SHALL NOT claim hard cancellation of a hung test.

#### Scenario: Main menu becomes ready
- **WHEN** the startup flag is enabled and RimWorld first reaches a settled main menu
- **THEN** all valid `MainMenuLoaded` tests run once on that main thread after real Def loading and Harmony initialization, and repeated frames do not rerun them

#### Scenario: One test fails
- **WHEN** a test throws an assertion exception
- **THEN** its full bounded failure is recorded, subsequent tests still run, and the Gateway remains responsive

#### Scenario: Durable write fails transiently
- **WHEN** attachment, a running transition, or a terminal transition cannot be persisted
- **THEN** the update loop remains responsive, the same immutable candidate is retried, no test is invoked or rerun while it is uncommitted, and later progress resumes only after that candidate commits

#### Scenario: Snapshot is briefly open by an external reader
- **WHEN** a process temporarily holds the current `integration-tests.json` without Windows delete sharing while the background lane commits the next candidate
- **THEN** the lane retries atomic replacement for a short bounded interval without blocking Unity, commits the exact candidate after the reader releases it, and emits no false runtime failure

### Requirement: Integration-test result endpoint and fresh-process isolation
`GET /api/v1/integration-tests` SHALL return whether the runner is enabled, discovered assemblies/tests, lifecycle-point states, and bounded terminal results without exposing live reflection objects. Before the initial attachment commit it SHALL return retryable `503 integration_test_status_pending`; afterward it SHALL expose only the last durably committed snapshot object. The response and artifact SHALL share one 4 MiB aggregate serialization bound and SHALL represent that same immutable committed state. Exception formatting SHALL never call arbitrary exception `Message`, `StackTrace`, or `ToString`: it MAY read bounded `Message` and `StackTrace` only for an exact allow-list including `IntegrationTestAssertionException` and safe standard wrappers after invocation unwrapping, SHALL use a fixed suppression marker for any other exception, SHALL read type text only from the known runtime `Type` implementation, SHALL truncate raw components before redaction or concatenation, and SHALL redact the current bearer token from endpoint, artifact, and diagnostic strings. Results SHALL be available after their durable commit so host verification can retain them with the exact PID, process start, build, and mod list.

The supported reset boundary SHALL be a fresh isolated RimWorld process. The runner SHALL NOT attempt to unload assemblies, clear all RimWorld static state, or rerun a lifecycle suite in the same process. Def/XML and Harmony assertions are integration evidence, but they SHALL NOT replace the repository's required player-action and observable-behavior acceptance for game-facing features.

#### Scenario: Host collects a completed main-menu suite
- **WHEN** the host polls the authenticated result endpoint after main-menu readiness
- **THEN** it can persist a token-free result document tied to the exact isolated process and fail the run when any discovery or test result failed

#### Scenario: A product gameplay test passes
- **WHEN** an in-game test proves a Def value or Harmony patch is present
- **THEN** that result supports integration diagnosis but does not by itself accept player-visible product behavior without the separate observed player workflow
