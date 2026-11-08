## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). This change delivers
the process and templates; individual replacements and original features require subsequent slices.

## ADDED Requirements

### Requirement: Evidence precedes a replacement specification
Each replacement SHALL start with read-only inspection of the identified upstream version and its
observable behavior, configuration, persistence behavior and dependencies. Record source
locations, package/Workshop identity, inspected build/hash, license/attribution, uncertainty, and
behavior evidence. Inventory all bottom-tab and other UI entry points and apply megamod's UI
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
- **AND** each departure from an upstream result appears in the replacement ledger with its player impact and possible conflicts

### Requirement: Replace specified behavior without original-mod compatibility work
Replacement work SHALL independently research and specify observable source behavior. Original-mod
API parity, consumer audits, compatibility facades, original saved-state import and drop-in or
coexistence compatibility work SHALL remain outside scope. The replacement ledger MUST NOT claim
those guarantees. Genuine external integrations actually used SHALL still satisfy the root
package/Def-resolution, absent-safety and shape-guard requirements. Game-target semantic admission
and normal persistence of megamod's own saves SHALL remain required.

#### Scenario: A replacement is based on Cherry Picker
- **WHEN** Cherry Picker behavior is selected for replacement
- **THEN** its observable behavior is researched and the chosen megamod behavior is specified
- **AND** the documented scope does not promise that other mods calling Cherry Picker APIs or reading its saved state will work with megamod

### Requirement: Warn and use ordinary feature-off behavior when an original is active
An active original provider SHALL trigger a bounded startup log warning and best-effort disablement
of the corresponding owned replacement features through their ordinary feature settings/effective-off
path. The effect and timing SHALL match manually switching those features off. This policy SHALL NOT
promise complete coexistence compatibility or suppression/rollback of every earlier effect. It MUST
NOT permanently rewrite stored global/save preferences or inheritance choices, modify the original,
or disable unrelated features and their required shared services. Installed-but-inactive originals
SHALL NOT trigger this policy. No full original-provider compatibility test matrix is required.

#### Scenario: The original provider remains loaded
- **WHEN** an original provider is active alongside megamod
- **THEN** a bounded startup log warning identifies that provider and the corresponding owned features subject to best-effort disablement
- **AND** those features use the same effective-off behavior and timing as a manual off choice, without rewriting the player's stored preferences
- **AND** the original provider is not disabled, edited, uninstalled or unpatched

#### Scenario: An unrelated feature shares supporting data
- **WHEN** an original provider triggers effective-off behavior for a replacement and another enabled feature uses the same shared service
- **THEN** the unrelated feature remains available with that service under its own settings and target requirements
- **AND** the original-provider rule does not switch off all megamod features

#### Scenario: Startup handling is not a compatibility promise
- **WHEN** the startup warning reports an active original and best-effort disablement
- **THEN** it does not report that the original and megamod are fully compatible or that all previously applied effects were undone
- **AND** documented limitations of the ordinary off choice still apply

#### Scenario: Original provider is installed but inactive
- **WHEN** an original mod exists on disk but is not in the active mod list
- **THEN** its presence alone does not disable our replacement
- **AND** retained global/save preferences and ordinary target-admission checks determine whether the replacement can activate

#### Scenario: Other mods change a target compatibly
- **WHEN** an unrelated active mod changes a target without breaking its documented semantic requirements
- **THEN** the original-provider suppression rule does not impose a general ban on other patches
- **AND** the feature remains eligible for its ordinary game-target semantic admission checks

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
source interactions, observable deviations and possible conflicts, own persistence, actual used integrations, omissions,
deferred work, and evidence links. Status SHALL distinguish candidate, analyzed, specified,
implemented-unverified, verified, deferred, and deliberately omitted behavior at feature granularity.

#### Scenario: Only part of an upstream mod is verified
- **WHEN** one upstream feature passes acceptance and other portions are absent or unverified
- **THEN** only the proven portion is marked verified and the remainder stays explicitly outstanding
