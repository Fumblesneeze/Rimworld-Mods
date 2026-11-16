## ADDED Requirements

### Requirement: Exact Steam targets live outside the repository
Repository tooling `RimWorldModding.Mcp` SHALL expose typed acquisition using pinned DepotDownloader, external cache paths, bounded owned processes and no credential-bearing durable repository output. Branch discovery SHALL be onboarding only; publication builds SHALL consume reviewed exact depot/file identities.

#### Scenario: Agent acquires two game versions
- **WHEN** the owning user authenticates and the agent requests 1.6 and 1.7 in separate external directories
- **THEN** the existing Steam installation remains intact and each acquired target records its exact manifests, game version and approved hashes

#### Scenario: Target cache changed
- **WHEN** a file or manifest differs from a pinned target
- **THEN** the build refuses that cache rather than falling back to another installation

#### Scenario: Additional game input
- **WHEN** an undeclared file appears outside the explicit Mods, downloader-state, marker and inventory paths
- **THEN** exact-target validation rejects the changed file set even if all previously approved hashes still match

### Requirement: A mod declares its matrix once
The release profile SHALL name supported compile targets and a development target. Builds SHALL isolate intermediate/output/reference roots per target and merge exactly one product assembly for every declared compatibility folder. Generated About versions, package validation and Workshop version tags SHALL agree with that declaration.

#### Scenario: Both target builds succeed
- **WHEN** a profile declares 1.6 and 1.7 and each independent build succeeds
- **THEN** the combined allowlisted package contains both version folders and metadata lists exactly those versions

#### Scenario: Runtime framework facades differ
- **WHEN** the 1.6 Mono game directory contains framework facades with different type-forwarding identities
- **THEN** opted-in builds use the .NET Framework reference pack for system types and the exact game directory for game APIs, avoiding mixed framework references

#### Scenario: Invalid matrix
- **WHEN** a target is missing, repeated, unsupported, or collides with another compatibility folder
- **THEN** validation fails before download, compilation, installation or publication

#### Scenario: Publication preserves the tested matrix
- **WHEN** both target native workflows have been reviewed and package upload is requested
- **THEN** publication uploads the exact combined package without compiling against the publisher runtime

#### Scenario: Exact target native verification
- **WHEN** the agent requests `release_target_test` with a declared target and explicit workflow IDs
- **THEN** the runner validates the exact external cache, installs the frozen combined candidate, compiles only the test/Gateway infrastructure, and retains unchanged product hashes with native action/result evidence

#### Scenario: Wrong product verification
- **WHEN** a selected project has another owner or a selected native group omits the requested product
- **THEN** the runner refuses before installing or launching; opted-in verification projects compile against the frozen target DLL instead of rebuilding the product project

#### Scenario: Build receipt drift
- **WHEN** publication sees a dirty build, a different revision, profile, catalog, target list or package digest
- **THEN** it refuses publication until a clean matching Release matrix has been built
