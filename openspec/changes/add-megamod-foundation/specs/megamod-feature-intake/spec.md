## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). This change delivers
the process and templates; individual replacements and original features require subsequent slices.

## ADDED Requirements

### Requirement: Evidence precedes a replacement specification
Each replacement SHALL start with read-only inspection of the identified upstream version and its
observable behavior, configuration, persistence, dependencies, and consumed contracts. Record source
locations, package/Workshop identity, inspected build/hash, license/attribution, uncertainty, and
consumer evidence. Inventory all bottom-tab and other UI entry points and apply megamod's UI
placement rules rather than copying upstream navigation. The OpenSpec SHALL state concrete inputs, player actions, results, edge cases,
defaults, balance/timing assumptions, and explicit omissions before tests or implementation.

#### Scenario: A request names Allow Tool
- **WHEN** a contributor receives only "implement Allow Tool functionality"
- **THEN** they first inventory the actual upstream behaviors and interactions and derive separate observable scenarios
- **AND** the mod name alone does not satisfy a feature specification or authorize a parity claim

### Requirement: Compose interactions inside owned behavior
When one selected source mod modifies another selected source mod, the analysis SHALL capture the
base behavior, modification, conditions/options, precedence, and conflicts. The feature specification
SHALL define the chosen combined behavior. Implementation MUST use ordinary owned code/composition,
not Harmony or equivalent runtime patches against megamod's own implementation.

#### Scenario: A selected add-on changes a selected base feature
- **WHEN** source B changes source A's behavior and both are being replaced
- **THEN** their reports link to one explicit composed behavior contract and its tests
- **AND** the native workflow produces that composed result through direct owned logic without an internal A-to-B patch layer

#### Scenario: Source interaction has conflicting defaults
- **WHEN** upstream load order or settings produce multiple possible results
- **THEN** the spec records the selected default and supported alternatives before implementation
- **AND** each departure from an upstream result appears in the replacement ledger with its compatibility impact

### Requirement: Preserve consumed interfaces deliberately
Replacement work SHALL inventory public/reflected type and assembly identities, method signatures,
Def names, events/lifecycle, package detection, settings/save keys, return and exception semantics,
and actual consumer expectations as applicable. Preserve supported contracts through a narrow facade
where practical. Document unsupported bindings and migration needs instead of spoofing upstream
identity or claiming unverified drop-in equivalence. Each feature SHALL define co-installation policy
so it cannot silently duplicate the upstream effect.

#### Scenario: A Cherry Picker consumer uses an external contract
- **WHEN** an inspected consumer binds to an upstream API, Def, saved key, or package identity
- **THEN** its exact contract is included in the replacement analysis and compatibility scenarios
- **AND** drop-in support is claimed only after that consumer works in the exact replacement-only run

#### Scenario: The original provider remains loaded
- **WHEN** an original provider is active alongside megamod
- **THEN** our conflicting replacement features remain inactive before they can change behavior or content
- **AND** a bounded startup log warning identifies the original provider, affected owned behavior and original-provider fallback
- **AND** unrelated megamod features remain available and the original provider is not disabled, edited, uninstalled or unpatched

#### Scenario: Original provider is installed but inactive
- **WHEN** an original mod exists on disk but is not in the active mod list
- **THEN** its presence alone does not disable our replacement
- **AND** ordinary target-admission checks still determine whether the replacement can activate

#### Scenario: Other mods change a target compatibly
- **WHEN** an unrelated active mod changes a target without breaking its documented semantic requirements
- **THEN** the original-provider suppression rule does not impose a general ban on other patches
- **AND** the feature remains eligible for its ordinary compatibility checks

### Requirement: Initial supported play starts with a new colony
Initial gameplay support SHALL cover colonies started with megamod in single-player without
Combat Extended. Multiplayer, CE compatibility and importing pre-megamod colonies or original-mod
save state SHALL remain outside initial support. This scope SHALL NOT waive ordinary persistence
and reload requirements for colonies created with megamod.

#### Scenario: Save a supported colony and resume it
- **WHEN** a colony started with megamod is saved and later loaded with its supported configuration
- **THEN** its specified gameplay state and settings persist through the ordinary save/load workflow
- **AND** the new-colony requirement does not demand a fresh colony on every session

#### Scenario: Upstream has an optional multiplayer or CE integration
- **WHEN** source research finds an upstream multiplayer or CE adapter
- **THEN** that finding is retained as reference material without promising or implementing initial support
- **AND** such research does not make a multiplayer, CE or migrated-save run an accepted initial configuration

### Requirement: Original features and optimizations follow the same acceptance process
Features without an upstream source SHALL be marked original and specify observable behavior before
focused RED/GREEN tests. Optimizations or deliberate changes SHALL record semantic deviations and
measured performance expectations before implementation. Every runtime slice MUST receive independent
review, relevant package checks, and personally observed native player-workflow verification on the
reviewed build under the root acceptance gate.

#### Scenario: An original or optimized feature is requested
- **WHEN** no upstream implementation exists, or an existing one is being intentionally changed
- **THEN** the spec states its own observable rules, assumptions, failure cases, and test/live verification plan
- **AND** tests, logs, startup probes, or direct state mutation alone cannot mark it accepted

### Requirement: Maintain an honest replacement ledger
The mod SHALL document every source whose functionality it replaces, including implemented portions,
source interactions, deviations and possible conflicts, preserved/broken contracts, omissions,
deferred work, and evidence links. Status SHALL distinguish candidate, analyzed, specified,
implemented-unverified, verified, deferred, and deliberately omitted behavior at feature granularity.

#### Scenario: Only part of an upstream mod is verified
- **WHEN** one upstream feature passes acceptance and other portions are absent or unverified
- **THEN** only the proven portion is marked verified and the remainder stays explicitly outstanding
