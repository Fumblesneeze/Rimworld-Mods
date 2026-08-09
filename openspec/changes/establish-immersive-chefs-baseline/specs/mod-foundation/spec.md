## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Repository and mod layout
The repository SHALL keep OpenSpec artifacts at repository root and SHALL keep every playable RimWorld mod below `mods/<ModName>`.

#### Scenario: Locate the Immersive Chefs contract and implementation
- **WHEN** a contributor opens the repository
- **THEN** the contributor finds OpenSpec configuration and changes under root `openspec/` and the owned mod under `mods/ImmersiveChefs`

### Requirement: Loadable RimWorld 1.6 package
The build SHALL produce a RimWorld 1.6 mod package named Immersive Chefs with package ID `fumblesneeze.immersivechefs`, a generated `About/About.xml`, and its game assembly under `1.6/Assemblies`. The MSBuild package identity, runtime package constant, generated About manifest, deployment folder, current operational documentation, and exact-mod verification matrices SHALL use that identity consistently and SHALL NOT retain a superseded package identity. An explicitly labeled historical evidence record MAY preserve the identity actually used by that old run only when it also states that the run predates and cannot verify the current identity.

#### Scenario: Build the distributable mod
- **WHEN** a contributor invokes the documented mod build command against a valid RimWorld 1.6 installation
- **THEN** the output folder contains valid metadata and `1.6/Assemblies/ImmersiveChefs.dll`
- **AND** the project, runtime constant, About manifest, deployment folder, and verification manifest all identify the package as `fumblesneeze.immersivechefs`

### Requirement: Minimal hard dependency surface
Immersive Chefs SHALL declare Harmony package `brrainz.harmony` as its only required third-party mod, SHALL load after supported optional integrations when present, and SHALL NOT bundle Harmony or optional-mod assemblies.

#### Scenario: Inspect generated dependency metadata
- **WHEN** the mod package is built
- **THEN** `About.xml` lists Harmony as required, treats all other integrations as optional load-order hints, and the mod output contains no `0Harmony.dll` or optional integration DLL

### Requirement: Idempotent startup
Immersive Chefs SHALL initialize its Harmony owner and compatibility snapshot once per game load and SHALL emit a stable startup log marker containing its package ID.

#### Scenario: Load the baseline in RimWorld
- **WHEN** RimWorld loads Immersive Chefs with its hard dependencies
- **THEN** the Player log contains exactly one `[ImmersiveChefs] Initialized fumblesneeze.immersivechefs` marker and startup completes without an Immersive Chefs error

### Requirement: Optional integration discovery
Immersive Chefs SHALL expose a side-effect-free public compatibility catalog that accepts loaded package IDs, matches them case-insensitively, de-duplicates them, and reports a snapshot for every recognized optional integration without loading optional assemblies.

The recognized catalog SHALL include:

- Royalty: `ludeon.rimworld.royalty`
- Processor Framework: `syrchalis.processor.framework`
- Expanded Materials - Metals: `argon.expandedmaterials.metals`
- Expanded Materials - Masonry: `argon.expandedmaterials.masonry`
- ABS Polymer: `mlie.simplysublimeabspolymer`
- Dubs Bad Hygiene: `dubwise.dubsbadhygiene`
- Gastronomy: `orion.gastronomy`
- Variety Matters Improved Redux: `evyatar108.varietymattersimprovedredux`
- Vanilla Food Variety Expanded: `vanillaexpanded.vanillafoodvarietyexpanded`
- Vanilla Expanded Framework: `oskarpotocki.vanillafactionsexpanded.core`
- Vanilla Nutrient Paste Expanded: `vanillaexpanded.vnutriente`

#### Scenario: Detect installed optional integrations
- **WHEN** the catalog receives mixed-case and duplicate loaded package IDs for Dubs Bad Hygiene and Processor Framework
- **THEN** it reports those two integrations as active exactly once and reports every other recognized integration as inactive

#### Scenario: Ignore unknown mods safely
- **WHEN** the catalog receives unknown package IDs or no package IDs
- **THEN** it returns a complete inactive snapshot for recognized integrations without throwing or resolving any optional assembly

### Requirement: Current local dependency inventory
The repository SHALL record locally verified package IDs, Workshop IDs, RimWorld support, activation state, transitive dependencies, useful Def/API evidence, and suitability decisions separately from runtime detection.

#### Scenario: Review an integration decision
- **WHEN** a contributor evaluates a planned integration
- **THEN** the inventory explains whether the downloaded mod supports RimWorld 1.6 and why it is included, excluded, or requires an adapter

### Requirement: Unsupported material packages stay excluded
Immersive Chefs SHALL NOT treat `Argon.VMEuP` or `Argon.ExpandedMaterials.Stones` as supported integrations because the downloaded packages declare support only through RimWorld 1.4; generic material recipes SHALL remain able to accept compatible modded Stuff without naming those packages.

#### Scenario: Outdated plastics and porcelain mods are downloaded
- **WHEN** the compatibility catalog is constructed on this machine
- **THEN** the outdated Expanded Materials plastics and porcelain package IDs are absent from the recognized integration set

### Requirement: Portable local paths
Build and verification entry points SHALL accept a caller-provided RimWorld path and Steam Workshop content path while defaulting to the verified `F:\Steam` installation on this machine.

#### Scenario: Build on another Steam library
- **WHEN** a contributor supplies valid alternate `RimWorldPath` and `SteamModContentFolder` MSBuild properties
- **THEN** the build resolves game and Harmony references from those locations without editing project files
