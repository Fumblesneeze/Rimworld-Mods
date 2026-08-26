## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Explicit host-test environments

The repository SHALL execute unpatched domain tests, Harmony-enabled tests, and Def-database tests in separate testhost processes. The ordinary domain suite SHALL NOT load Harmony, construct RimWorld `Mod` classes, populate `LoadedModManager`, or populate representative Def databases implicitly. A grouped run SHALL continue through every selected project, retain each result, and fail when any project executes zero tests or reports a failure.

#### Scenario: Run ordinary domain tests

- **WHEN** the unpatched Immersive Chefs suite starts
- **THEN** no running mods are registered, the Harmony assembly is not loaded, and the representative Def database is empty

#### Scenario: Run all Immersive Chefs host tests

- **WHEN** a contributor selects the grouped Immersive Chefs suite
- **THEN** the guarded runner invokes the unpatched, Harmony, and Def projects independently and rejects zero-test results from each

### Requirement: Scoped Harmony test patches

The Harmony test environment SHALL reference the configured installed Harmony assembly, retain and verify its name/version/MVID/SHA-256 evidence, apply only explicitly requested patches under a unique test owner ID, run non-parallel, and remove those patches in guaranteed cleanup. Failed patch acquisition SHALL attempt owner rollback, and failed disposal SHALL remain retryable.

#### Scenario: Exercise one patched method

- **WHEN** the focused Harmony capability test enters its patch scope
- **THEN** the target exhibits the patched public behavior inside the scope and its original behavior both before the scope and after cleanup

### Requirement: Scoped test Def database

The Def test environment SHALL support constructed minimal test-only `Def` instances, run non-parallel, and take exclusive ownership of the entire matching `DefDatabase<T>` for one scope only when that database is initially empty. Code inside the scope SHALL NOT add unrelated entries outside the scope's fixture set. Guaranteed cleanup SHALL clear the entire exclusively owned generic database. The scope SHALL refuse a nonempty database without mutating name or short-hash lookup state, because the external CLR cannot faithfully rebuild RimWorld's Unity-Mono short-hash cache. It SHALL NOT claim that this exercises RimWorld XML loading.

#### Scenario: Register a specific test Def

- **WHEN** a fixture constructs and registers a named test Def
- **THEN** `DefDatabase<T>.GetNamedSilentFail` returns that exact instance inside the scope and the name is absent again after cleanup

#### Scenario: Register several required test Defs atomically

- **WHEN** one focused host test constructs and registers several lightweight Def fixtures of the same type in one scope
- **THEN** every named fixture resolves to its exact instance inside that scope and every registered name is absent again after cleanup

#### Scenario: Reject a polluted Def process

- **WHEN** a fixture attempts scoped registration while the matching Def database already contains entries
- **THEN** registration fails before mutation and existing name and short-hash lookups remain unchanged

#### Scenario: Require XML-loaded Def behavior

- **WHEN** a test depends on RimWorld XML inheritance, cross-references, or PatchOperations
- **THEN** it uses the real loader in an isolated RimWorld run rather than `DirectXmlToObject`, `DirectXmlLoader`, or a repository-built substitute loader in the NUnit process

### Requirement: Honest optional-mod boundary

Host tests SHALL distinguish simulated package presence, referenced assembly shape, explicit Harmony patches, test-only Defs, and a genuinely loaded mod. Tests requiring another mod's normal constructor/static initialization, complete patch set, real Def XML/cross-references, or PatchOperations SHALL run in an isolated RimWorld process and SHALL NOT be described as unit tests.

#### Scenario: Test package-ID-dependent domain behavior

- **WHEN** behavior depends only on whether a package ID is active
- **THEN** the unit test passes an explicit package set to the public domain boundary without loading the external mod

#### Scenario: Claim behavior against a real external Def or patch

- **WHEN** acceptance depends on an installed mod's real Defs, XML patches, or runtime initialization
- **THEN** the final evidence comes from that mod being active in an isolated RimWorld run rather than from a fabricated host environment

### Requirement: Product-owned in-game integration tests

Immersive Chefs SHALL own a separately compiled startup-gated integration-test assembly whose manifest owner is exactly `fumblesneeze.immersivechefs`. Its manifest SHALL use exact active-package mode for the ordered sequence Harmony, Core, XML Extensions, Immersive Chefs, and Dev Gateway. The host builder SHALL select and stage that assembly only when the complete requested sequence matches every package and position, and the Dev Gateway SHALL re-evaluate the same exact sequence from the real active `ModContentPack` order before exposing the assembly for load or invocation through the active Immersive Chefs content pack. The ordinary Immersive Chefs package SHALL contain neither that test assembly, any case-insensitive `RimWorldDevGateway*.dll` filename, a metadata-bearing DLL without an assembly definition, nor a managed AssemblyRef whose simple name starts with `RimWorldDevGateway` case-insensitively. Its smoke evidence SHALL retain every ordinary package file by ordinal relative path/length/SHA-256 and every managed DLL's identity and complete metadata-only AssemblyRef-row inventory.

Every staged integration-test assembly admitted as smoke evidence SHALL contribute at least one discovered test descriptor with the same owning package ID and exact assembly identity. Merely loading an empty assembly SHALL not count as product-mod test evidence.

#### Scenario: Run Gateway self-tests without Immersive Chefs

- **WHEN** RimWorld starts with only Core and the Dev Gateway active and integration tests enabled
- **THEN** the Immersive Chefs integration-test project is neither staged nor discovered and no result is attributed to `fumblesneeze.immersivechefs`

#### Scenario: Prove the loaded Immersive Chefs XML patch

- **WHEN** RimWorld reaches its settled main menu with the exact ordered active set Harmony, Core, XML Extensions, Immersive Chefs, and Dev Gateway
- **THEN** the Gateway discovers the exact Immersive Chefs-owned test assembly from the active Immersive Chefs mod, runs its `MainMenuLoaded` tests, observes that the Immersive Chefs constructor initialized, and reads the expected Immersive Chefs XML-patch marker from the finalized live Core Steel `ThingDef`

#### Scenario: Product test assembly has no tests

- **WHEN** a staged assembly is admitted but contributes no discovered descriptor with its owning package ID and exact full assembly identity
- **THEN** the smoke fails instead of treating assembly discovery alone as proof that the loaded product mod was tested
