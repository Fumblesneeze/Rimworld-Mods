## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). This capability is the
inert scaffold, not gameplay acceptance.

## ADDED Requirements

### Requirement: Stable independent product identity
The scaffold SHALL use display name `Make Rimworld Great Again`, package ID `fumblesneeze.megamod`,
folder `mods/megamod`, author `Fumblesneeze`, and a pinned Zlepper ModSdk project targeting RimWorld
1.7 beta. It MUST NOT ship or depend on another repository-owned mod or the developer Gateway.

#### Scenario: Inspect a built scaffold
- **WHEN** the owning project is built against a supported 1.7 installation and its allowlisted package is validated
- **THEN** generated About metadata has the specified identity and 1.7 version, and the inventory contains only the owned assembly, About metadata, and baseline language catalogs
- **AND** no game, Workshop, Harmony, test, tooling, or Gateway assembly is bundled

### Requirement: Inert and truthfully documented initial state
The initial scaffold SHALL contain no runtime entry point, startup patch application, gameplay Defs,
shared caches, or implemented replacement claims. Its README SHALL distinguish the documented future
contracts from delivered features. Its release profile SHALL remain unpublished/private and its
development preview SHALL NOT be treated as release-approved art.

#### Scenario: Read the feature status
- **WHEN** a contributor opens the mod README and coverage index
- **THEN** they see that no source mod is yet replaced and that guard/cache implementations and live feature acceptance remain future work

### Requirement: Exact initial localization inventory
The scaffold SHALL include English, German, Spanish, French, ChineseSimplified, Russian, and Japanese
catalogs with the same empty key inventory. Each future feature MUST add contextual translations for
its exact keyed and DefInjected inventory and pass the repository's rendered-language acceptance gate.

#### Scenario: Validate catalogs without invented UI
- **WHEN** the scaffold's source and packaged language XML are inspected
- **THEN** all seven are well-formed LanguageData documents with zero keys and packaged bytes match source
- **AND** no translated feature or rendered UI is claimed from empty catalogs
