# Common Sense feature configuration and coexistence

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024 specification only. This capability governs the four sibling behavior capabilities and does
not claim that the original mod is compatible with the beta.

## ADDED Requirements

### Requirement: Preferences have a complete ordered inventory
The mod SHALL present these preferences in ordered feature sections using the following initial defaults, with clear descriptions of their effects and any unavailable dependency.

| Section | Preference | Initial default |
| --- | --- | --- |
| Housekeeping | Clean before work; Clean after tending; Clean between bill stages | Each on |
| Housekeeping | Clean before eating/social relaxation; Show building cleaning control | Each off |
| Housekeeping | Before-work filth limit; Between-stage/eating filth limit; Post-tending filth limit | 5; 5; 0 meaning all currently eligible |
| Housekeeping | Postpone snow/sand clearing during precipitation | On |
| Supplies and work | Collect bill ingredients in batches | On |
| Supplies and work | Navigation to Bill-supply storage detours in Hauling > Opportunities | Single S125 setting, initially on; no duplicate editor or whole-stack promise |
| Supplies and work | Prefer perishable ingredients; Prefer perishable meals | Each on |
| Supplies and work | Return interrupted inventory items; Show manual unload controls | Each on |
| Supplies and work | Limit recreation snack quantities; Permit policy-allowed inventory drugs | Each on |
| Food information | Separate meals with distinct ingredient concerns | On |
| Food information | Treat odd ingredients as ordinary; Infer generated meal ingredients | Each off |
| Food information | Expanded ingredient information | On |
| Recreation and care | Prefer outdoor recreation; Consider potential mood for scheduled drugs | Each on |
| Recreation and care | Patient treats; Complete recreation before repeating; Social-drink economy; Meditation economy | Each on |
| Recreation and care | Sleeping mood movement | Off |
| Idle movement | Weather-aware wandering; Considerate room destinations | Each off |

Filth limits accept nonnegative whole numbers up to 1,000,000,000 as in the source UI; zero is labeled
All currently eligible, not disabled. Every pass is finite regardless of this value. Other specified
thresholds are named behavior policies, not hidden magic values. Item category information and the
single food-packing trigger correction have no separate source preference. The source's hidden
path approximation and startup patch-mode options are not exposed. Shared capacity is mandatory.

#### Scenario: New installation
- **WHEN** the player opens the settings for the first time
- **THEN** it shows these defaults in named sections without a second competing hauling-capacity preference.

#### Scenario: Zero cleaning limit
- **WHEN** a cleaning limit is set to zero
- **THEN** the UI explains that it means one pass over all currently eligible filth, not unlimited repeated cleaning.

### Requirement: All preferences use common global and save inheritance
Every listed preference SHALL use the common Global default, Current save and Use global controls, with new or missing save entries inheriting the current global value.
An inherited row disables its save editor. Checking Use global retains a dormant override; unchecking
restores it or seeds a first override from the effective global value. At the main menu, both the save
editor and Use global are disabled in presentation and mutation callbacks, with an explanation.
Changing a global default updates inheriting saves but not explicit overrides. Loading another save
does not reuse the previous save's decisions. No source global-settings file is silently imported.
Building cleaning choices, unload quantities and recreation-completion state are entity gameplay
state and do not become separate settings rows.

#### Scenario: Save override and global change
- **WHEN** one save overrides eating cleanup off and another inherits it, and the global default changes on
- **THEN** only the inheriting save adopts the new value while the overridden save remains off.

#### Scenario: Main-menu editing
- **WHEN** settings are opened without an active game
- **THEN** only global defaults can be changed and no callback modifies an implicit or previously loaded save.

### Requirement: Preference changes preserve current physical state
Preference changes SHALL affect the next safe decision for the relevant behavior without replaying completed work, abandoning physical cargo or reversing player orders.
Disabling optional cleaning stops selecting additional filth and resumes a still-valid parent task at
a safe boundary. Disabling batching retains collected ingredients and completes safe delivery/recovery
without collecting new batches through that option. Ingestion and production already performed are
never repeated or refunded by a toggle. Hiding unload/building controls preserves existing state and
leaves cancellation/recovery accessible contextually. Changes to stacking or generated provenance
do not rewrite existing stacks or history. Turning recreation completion off releases its automatic
preference and restores native recreation/hygiene selection without a leftover 80% threshold;
reenabling derives coherent state from the current need and retained save state.

#### Scenario: Disable batching while carrying ingredients
- **WHEN** the player disables batching after collection has begun
- **THEN** actual ingredients remain accounted for and are delivered or safely recovered once without new batch collection.

#### Scenario: Hide controls with a pending order
- **WHEN** manual unload controls are hidden while an item still has an order
- **THEN** that order and its contextual cancellation remain available rather than becoming invisible unchangeable work.

#### Scenario: Recreation completion disabled
- **WHEN** the player disables recreation completion
- **THEN** ordinary recreation selection uses native thresholds and the optional hygiene scheduling rule no longer changes provider choices.

### Requirement: One admitted provider owns each overlapping behavior
If original Common Sense is active, overlapping owned behavior SHALL remain unavailable with an explanation and bounded startup diagnostics, without removing the original mod or changing its preferences.
Previously owned pending side work is held safely; retain physical items, damage/history and saved
choices while ordinary valid parent jobs can continue. Restoring support does not replay old optional
actions. Pending explicit unload orders require the player to resume them after the overlap clears.
Ordinary preferences can be edited for later use, clearly distinguished from active behavior.
Loading and saving while support is unavailable still preserves dormant owned orders, remaining
quantities and entity choices. Admission gates their execution, not whether the save retains them.

An active external While You're Up bill-detour feature takes that opportunity; owned duplicate detours remain
off whenever that original provider is loaded, without an initial cooperative-original exception.
Withhold only the owned overlapping detour and leave normal bill collection usable. Do not write either provider's
settings. Missing or changed optional providers disable only their affected integration, with a
reason and ordinary fallback. Compatible unrelated modifications do not disqualify a feature.

#### Scenario: Original source installed alongside a pending order
- **WHEN** a save with owned unload orders opens with Common Sense active
- **THEN** the owned actions remain held, physical items are preserved and source-overlap status is shown
- **AND** removing the source does not silently execute old unload orders before explicit resume.

#### Scenario: Save again while support is blocked
- **WHEN** the player loads a save with a held two-unit unload order, saves again while support remains unavailable, then restores support and reloads
- **THEN** the same remaining two units and entity choices are retained for inspection and explicit Resume, without execution or loss during the blocked run.

Owned S024 and S125 instead share one bill-supply preference and parent-attempt decision under
S125's useful-surplus and whole-trip contract; neither schedules a duplicate around the other.

#### Scenario: Another detour provider
- **WHEN** an external While You're Up supplies the overlapping bill-hauling opportunity
- **THEN** only one detour is scheduled and neither mod's stored preferences are automatically changed.

### Requirement: UI and compatibility claims remain specific
These features SHALL use structured mod settings, existing work/bill/item information, Gear controls and contextual building state, without adding a bottom-bar tab solely for preferences or toggles.
Text, disabled reasons and controls are available in all seven baseline languages. Source API/save
emulation and general drop-in compatibility are not claimed by these behavior specifications.
Unsupported foreign hooks do not silently gain access to a different behavior under the original
identity. An unavailable integration leaves normal valid gameplay accessible and warns once on
startup for each skipped enabled patch, identifying the lost behavior and fallback. Unsupported
saved owned actions remain safe and recoverable; no unsupported source save conversion is attempted.

#### Scenario: Incompatible cleaning provider
- **WHEN** the actual cleaning permission or continuation behavior cannot be supported
- **THEN** the affected optional cleaning route stays unavailable with an explanation while the normal valid work remains usable.

#### Scenario: No extra tab
- **WHEN** the player configures these features or changes one bench's cleaning choice
- **THEN** settings and that bench's contextual controls provide the workflow without a new bottom tab.
