## ADDED Requirements

### Requirement: Game targets use major and minor only
Repository tooling RimWorldModding.Mcp SHALL select games solely by major/minor, such as 1.6 and 1.7. Catalogs, profiles and download arguments SHALL reject patch/revision numbers, Steam build IDs, depot-manifest IDs and game-file hash pins. Downloading SHALL use the declared rolling branch, external owned cache paths, bounded processes and credential-free repository evidence. Observed full versions MAY be retained in run evidence but MUST NOT gate builds, launches or publication.

#### Scenario: Agent acquires two game versions
- **WHEN** the owning user authenticates and the agent requests 1.6 and 1.7 in separate external directories
- **THEN** each rolling branch downloads into its owned cache, remembered login is reused, and the normal Steam installation remains intact

#### Scenario: Steam updates a supported line
- **WHEN** game files, patch numbers or revision numbers change while the major/minor remains the same
- **THEN** validation accepts the cache without a catalog hash or manifest update and an explicit download can refresh it

#### Scenario: Version file differs from live runtime
- **WHEN** Version.txt and the live Gateway report different patches or revisions within the same major/minor
- **THEN** launch validation accepts them and records observations without creating a runtime revision pin

#### Scenario: Forbidden game pin
- **WHEN** a caller adds a full-version, runtime-revision, Steam-build, depot-manifest or game-hash field
- **THEN** central operation admission and strict schemas reject it before external effects, including non-download operations

#### Scenario: Wrong major or minor
- **WHEN** the selected cache or live runtime has a different major/minor
- **THEN** validation rejects it without falling back to the normal installation

### Requirement: A mod declares its matrix once
The release profile SHALL name supported compile targets and a development target. Builds SHALL isolate intermediate/output/reference roots per target and merge exactly one product assembly for every declared compatibility folder. Generated About versions, package validation and Workshop version tags SHALL agree with that declaration.

#### Scenario: Host verification selects the installed Harmony provider
- **WHEN** the selected game target uses a version-specific Harmony directory and the legacy Current directory is absent
- **THEN** typed `test_run` accepts an explicit existing absolute Harmony DLL path, forwards it as one argument to its owned adapter, and retains the adapter's assembly-identity evidence; missing or relative paths are rejected before execution

#### Scenario: Both target builds succeed
- **WHEN** a profile declares 1.6 and 1.7 and each independent build succeeds
- **THEN** the combined allowlisted package contains both version folders and metadata lists exactly those versions

#### Scenario: Runtime framework facades differ
- **WHEN** the 1.6 Mono game directory contains framework facades with different type-forwarding identities
- **THEN** the Gateway explicitly opts into the .NET Framework reference pack for system types and the exact game directory for game APIs; product builds retain their existing Mono API contract, including runtime-provided Index/HashCode APIs that the reference pack lacks

#### Scenario: Invalid matrix
- **WHEN** a target is missing, repeated, unsupported, or collides with another compatibility folder
- **THEN** validation fails before download, compilation, installation or publication

#### Scenario: Publication preserves the tested matrix
- **WHEN** both target native workflows have been reviewed and package upload is requested
- **THEN** publication uploads the exact combined package without compiling against the publisher runtime

#### Scenario: Major/minor target native verification
- **WHEN** the agent requests `release_target_test` with a declared target and explicit workflow IDs
- **THEN** the runner validates the owned major/minor external cache, installs the frozen combined candidate, compiles only the test/Gateway infrastructure, and retains unchanged product hashes with native action/result evidence

#### Scenario: Wrong product verification
- **WHEN** a selected project has another owner or a selected native group omits the requested product
- **THEN** the runner refuses before installing or launching; opted-in verification projects compile against the frozen target DLL instead of rebuilding the product project

#### Scenario: Build receipt drift
- **WHEN** publication sees a dirty build, a different revision, profile, catalog, target list or package digest
- **THEN** it refuses publication until a clean matching Release matrix has been built
