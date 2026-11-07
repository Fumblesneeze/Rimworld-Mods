# Construction plant clearing

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S063/P1-57, Cut plants before building (Continued). Observable draft only.
PC-Q1–4 retain proposed corrections and later implementation/acceptance work.

## ADDED Requirements

### Requirement: Accepted construction creates advance orders only at its actual site

PC01. When advance clearing is on, newly accepted building, floor, installation or reinstallation work SHALL place appropriate ordinary plant orders on the plants actually obstructing that work.
The complete rotated footprint and the work's actual map apply. Merely sharing a cell is not an
obstruction. The feature SHALL NOT use a fixed plant-work threshold to decide which plants block
construction. Plants that ordinary construction can leave or remove without preparatory plant work
SHALL NOT receive unnecessary advance orders. The terrain-under-grower exception remains effective.
For owned planning across unexplored cells, the entire relevant footprint SHALL first be revealed
and successfully revalidated by that planning workflow. Until then, this feature SHALL NOT reveal
hidden plants through orders, counts, notices or worker actions. Its deferred first clearing decision
occurs at that successful reveal revalidation, using the current switch/preferences and ordinary
restrictions; this is not a rescan of unrelated old plans. Invalid or canceled plans create no orders.

Rejected placement, previews, sampling, selection changes and canceled gestures SHALL NOT issue
orders. Instant completed construction has no future work to prepare. A genuine installation plan
remains eligible even if the object's original construction is free or developer mode is enabled.

#### Scenario: Rotated building with several obstructions
- **WHEN** an accepted rotated multi-cell building has obstructing plants in different footprint cells
- **THEN** each eligible obstruction on that map receives at most one appropriate order and plants outside the footprint remain unchanged by this feature.

#### Scenario: Plant sharing a permitted floor cell
- **WHEN** a plant can coexist with the accepted floor under the ordinary grower/terrain rules
- **THEN** advance clearing does not mark it simply because it occupies that cell.

#### Scenario: Rejected placement or floor sampling
- **WHEN** placement is rejected, a preview is canceled, or the player samples a floor without placing work
- **THEN** this feature adds no plant orders, even if an older plan already occupies the sampled location.

#### Scenario: Instant construction versus actual installation
- **WHEN** one action completes a structure instantly and another creates a genuine installation plan
- **THEN** only the plan can trigger advance clearing, regardless of the installed object's original construction cost.

#### Scenario: Construction planned across fog
- **WHEN** owned construction planning accepts a footprint partly in unexplored terrain
- **THEN** no clearing orders, notices or plant-specific information expose the hidden contents; after full reveal and successful plan revalidation, its first clearing decision uses current conditions, while a canceled or invalidated plan creates none.

### Requirement: Roof clearing follows actual new roof-building orders

PC02. Newly accepted manual or ordinary automatic roof-building area additions SHALL create advance orders only for plants that obstruct roof construction in those newly included cells while advance clearing is on.
Already roofed cells, unchanged old area cells, cells excluded by a No roof choice, and failed
automatic roof-generation attempts SHALL NOT create new orders. The feature SHALL NOT extend a
roof area, invent support, create a roof, or treat a roof-removal area as roof-building work.
Unrevealed roof cells SHALL NOT expose hidden plants through this feature's orders or notices.

#### Scenario: Manually plan roof through a tree
- **WHEN** the player successfully adds an unroofed cell containing an eligible roof obstruction to Build roof
- **THEN** the plant receives an appropriate advance order while the roof still needs ordinary labor and support.

#### Scenario: Automatic room roofing excludes cells
- **WHEN** ordinary automatic roofing adds some cells but leaves a No roof cell and an already roofed cell unchanged
- **THEN** only actual new unroofed roof-building cells are considered for clearing.

#### Scenario: A later room fails automatic roofing
- **WHEN** a second room fails to acquire a roof area after a first room succeeded
- **THEN** no plants in either room are newly marked on behalf of the failed second attempt.

#### Scenario: Remove a roof before deconstruction
- **WHEN** roof-safe removal creates temporary Remove roof work
- **THEN** that work does not create plant-clearing orders or reverse the removal into Build roof.

### Requirement: Choose a real clearing action without manufacturing yield

PC03. For an eligible obstruction without an existing explicit plant order, the feature SHALL prefer ordinary tree extraction when Prefer digging up is enabled and that tree can actually be extracted; otherwise it SHALL choose harvest only if the plant is currently harvestable and harvesting removes the obstructing plant, and cutting in the remaining cases.
Prefer digging up starts **off**. An unavailable extraction option falls back to ordinary harvest
or cut at order creation; a valid extraction order awaiting a worker does not silently become cutting.
Current plant condition governs ordinary work at execution, including maturity, yield and failure.

Workers SHALL physically perform the normal work, with normal time, skills, access, reservations,
forbidden state, willingness and consequences. No extra skill/research permission, yield multiplier,
duplicated product, instant removal or guaranteed successful harvest is granted. An extracted tree
is the same physical tree in its ordinary transportable form; hauling it away remains real work.

#### Scenario: Harvest a removable mature plant
- **WHEN** the obstruction is harvestable and harvesting removes it, with extraction unavailable or disabled
- **THEN** a normal harvest order is created and the eventual worker obtains only ordinary yield.

#### Scenario: Mature berry bush survives harvesting
- **WHEN** an obstructing mature berry bush would survive a harvest-only order
- **THEN** the automatic order is cutting, retaining ordinary cutting yield without a separate duplicate harvest.

#### Scenario: Immature tree is not harvestable yet
- **WHEN** an immature obstruction cannot currently be harvested and extraction is unavailable or disabled
- **THEN** its automatic order is cutting rather than a harvest order that waits for growth, and it yields only what ordinary cutting permits at that condition.

#### Scenario: Extract a tree
- **WHEN** Prefer digging up is on and the ordinary tree is extractable
- **THEN** an extraction order is created, a qualified worker must do the work, and the resulting tree must still be physically moved before any remaining obstruction is cleared.

#### Scenario: No extractor is currently available
- **WHEN** an eligible extraction order has no currently available worker
- **THEN** it waits rather than becoming a cutting order or granting a builder new work permissions.

#### Scenario: Plant changes before work
- **WHEN** a marked plant becomes forbidden, unharvestable, inaccessible or claimed by another worker before work starts
- **THEN** normal eligibility governs the attempt and no earlier advance decision grants stale permission or creates products.

### Requirement: Existing plant orders and their independent lifecycle are preserved

PC04. Automatic placement-time ordering SHALL preserve existing explicit cut, harvest and extract orders and unrelated designations; it SHALL NOT blanket-cancel them to install its preferred choice.
A matching order is reused without duplication or progress reset. An incompatible existing order
retains precedence; an unresolved conflict is left unchanged with an inspectable reason. Incidental
construction and roof clearing SHALL NOT cut a tree while its explicit extraction order is active
or replace another active explicit plant action with a competing automatic action.

Created plant orders are independent ordinary orders. Canceling the construction/roof plan, turning
advance clearing off or changing preferences SHALL NOT cancel or rewrite them. The control's help
SHALL explain that remaining plant orders can be canceled separately. Explicit cancellation SHALL
stay canceled until another qualifying new placement; enabling the switch does not rescan old plans,
and later plant growth alone does not create new advance orders. If a preserved harvest leaves an
obstruction, ordinary later construction/manual work must clear it; this feature does not repeatedly
recreate a completed or canceled order. Native completion retains its ordinary plant consequences.

#### Scenario: Extraction already selected before roof work
- **WHEN** a tree already has Extract tree and the player adds a roof order over it
- **THEN** the extraction order remains and incidental roof clearing does not chop the tree instead.

#### Scenario: Preserve an explicit harvest choice
- **WHEN** a player-selected harvest order conflicts with the automatic preference to cut a perennial
- **THEN** the existing harvest remains; any surviving obstruction is not silently converted into a new cutting order by this feature.

#### Scenario: Repeat placement without deleting other marks
- **WHEN** accepted work encounters a plant already carrying the same clearing order and an unrelated mark
- **THEN** neither mark is removed or duplicated and work progress is not reset.

#### Scenario: Cancel the building after ordering plants
- **WHEN** the player cancels the building or roof plan before the plants are worked
- **THEN** the visible independent plant orders remain available for work or separate manual cancellation.

#### Scenario: Cancel a plant order or enable clearing late
- **WHEN** the player cancels a generated order or turns clearing on after plans already exist
- **THEN** no background scan recreates that order or creates orders for the old plans.

### Requirement: Special trees are excluded from automatic advance orders

PC05. Advance clearing SHALL skip special trees classified with the game's protected special-tree group, including available Anima, Gauranlen and Harbinger trees, and admitted equivalent provider protections.
The exclusion applies to automatic cutting, harvesting and extraction. Missing DLC or optional
content must not create a dependency. Existing explicit orders remain governed by their ordinary
rules. This exclusion is not general immunity from manual orders, incidental native construction,
damage or other mods; the UI SHALL describe it specifically as an advance-order exclusion.

Show skipped special-tree notices starts **on**. When on, a placement action encountering excluded
trees SHALL give one bounded contextual notice identifying the skipped targets or locations; a drag
must not flood messages per cell. Turning notices off suppresses the notice, not the exclusion.

#### Scenario: Special tree in a new footprint
- **WHEN** accepted construction encounters a protected special tree while Prefer digging up is on
- **THEN** no automatic extract, harvest or cut order is added for it and the enabled notice explains the skip.

#### Scenario: Silent exclusion
- **WHEN** skipped-tree notices are disabled
- **THEN** protected trees remain excluded while placement produces no skipped-tree notice.

#### Scenario: Missing expansion and multiple skipped trees
- **WHEN** a drag crosses several available special trees and another expansion's tree does not exist
- **THEN** the available protected trees are skipped with at most one notice for that action and the missing tree does not cause an error.

### Requirement: Incidental clearing can require a Plant Cutting assignment

PC06. Restrict incidental clearing to Plant Cutting workers SHALL start **on** and SHALL govern incidental clearing for construction, installation and roof building independently of the advance-order switch.
A worker with assignable work settings must have Plant Cutting active to perform that incidental
plant work. A worker without such settings retains ordinary eligibility. The rule never grants
capabilities, overrides willingness or forces a work assignment. Turning it off permits ordinary
incidental clearing under the remaining rules; it does not disable advance orders.

The restriction SHALL NOT change eligibility for ordinary Growing harvest, sowing or explicit plant
orders. A forced construction order has no implicit exemption. If no permitted worker can clear the
obstruction, the construction/roof work remains pending with a visible reason and the player can
change assignments, disable this restriction or issue appropriate manual work. Preference changes
affect new selection and not-yet-started incidental work, without resetting work already underway.

#### Scenario: Builder has construction but no Plant Cutting
- **WHEN** that builder encounters a plant blocking construction with the restriction on
- **THEN** the builder does not cut it incidentally and the pending work explains the worker restriction.

#### Scenario: Toggle advance ordering off
- **WHEN** advance clearing is off but the worker restriction remains on
- **THEN** new plans gain no advance orders while builders still need the required Plant Cutting assignment for incidental clearing.

#### Scenario: Grower without Plant Cutting
- **WHEN** a grower can ordinarily harvest or sow but lacks Plant Cutting
- **THEN** this construction-specific restriction does not remove that ordinary Growing eligibility.

#### Scenario: Forced construction and workers without assignment controls
- **WHEN** the player forces construction for a worker lacking the required assignment, or an otherwise eligible worker has no assignable work settings
- **THEN** the first worker remains restricted and the second retains ordinary eligibility without invented work settings.

### Requirement: Operational control is visible and saved with its game

PC07. Advance clearing SHALL have one visible small bottom-right on/off control in colony-map view, with a tooltip explaining its current state, independent remaining orders and the configured shortcut.
The rebindable shortcut defaults to **Backslash** and performs exactly one toggle per activation.
Both controls affect the current game's maps and save their state with that game. They SHALL be
inactive at the main menu and in world view, and the shortcut SHALL NOT consume text-entry input.
No new bottom tab or configuration window masquerading as a gameplay tab is added.

New or missing operational state starts from the current global Start advance clearing enabled
preference, built-in **on**. Once initialized, later changes to that initializer SHALL NOT flip the
game's state. Switching saves SHALL restore each game's own state rather than inheriting the last
opened game's switch. Unavailable integration SHALL visibly disable the control with a reason.

#### Scenario: Use the corner button and reload
- **WHEN** the player disables advance clearing, saves and reloads
- **THEN** it remains off and the visible state and shortcut tooltip agree.

#### Scenario: Switch games and change the initialization preference
- **WHEN** one game is off, another is on and the global initialization preference changes
- **THEN** loading either existing game restores its own state and a newly initialized game uses the changed preference.

#### Scenario: World view or active text input
- **WHEN** the player presses Backslash in world view, at the main menu or while entering text
- **THEN** this feature does not silently change the game's clearing state.

### Requirement: Configuration uses common scope-aware settings

PC08. Configuration SHALL appear in an ordered Construction / Plant clearing section of common mod settings, with clearly labeled built-in defaults and effect timing.
Prefer digging up (off), Restrict incidental clearing to Plant Cutting workers (on), and Show skipped
special-tree notices (on) SHALL use Global default, Current save and Use global controls. New/missing
entries inherit the current global value. An inherited save control is inactive; disabling inheritance
restores a dormant override or seeds the first one from the effective value. Editing a save override
SHALL NOT change globals or another save. At the main menu, save editors and Use global actions,
including mutation callbacks, SHALL be disabled while global editing remains available.

Start advance clearing enabled (on) is explicitly **global-only**: it initializes a game's missing
operational state, whose later save-local choice already belongs to the corner control. The UI SHALL
explain this exception and not offer a misleading save-local initializer. Key rebinding retains the
game's global input configuration. Changed action preferences affect newly generated orders; notices
affect subsequent placements; worker timing follows PC06. Existing orders are not rewritten.
All labels, reasons, tooltips and scope explanations SHALL be localized in the seven baseline languages.

#### Scenario: Independent save preference
- **WHEN** one save overrides Prefer digging up and another inherits a global change
- **THEN** the overridden save retains its value and the inherited save uses the changed global value for its next new order.

#### Scenario: Main-menu configuration
- **WHEN** mod settings are opened without an active game
- **THEN** global controls work, all save-local mutations are disabled and no previous save is changed.

#### Scenario: Dormant override and old plant orders
- **WHEN** a player checks Use global and later restores an override while existing plant orders are present
- **THEN** the override is retained/restored correctly and the old orders are not rewritten.

### Requirement: Owned work composes once and unsupported integrations explain their fallback

PC09. Plant clearing SHALL compose once with owned placement, quality retries, replacements and roof work, preserving their existing costs, retry budgets, contents, physical resources, work priorities and final access/safety checks.
Preparing plants does not itself finish construction or satisfy another feature's safety check.
Growing-area permissions retain their ordinary scope and manual plant orders retain precedence.
If a tree awaiting extraction becomes blighted, owned automatic blight cutting SHALL preserve the
extraction choice by neither adding a competing cut nor selecting its preferred cutting for that tree
while extraction remains ordered. Normal extraction eligibility still applies; cancelling extraction
alone does not trigger blight automation for an already existing infection.
Search and hauling SHALL observe the same actual plants, resulting products and extracted trees;
clearing must not manufacture duplicate quantities, stock or invisible cargo.

If an original provider is active, the overlapping owned feature SHALL be inactive with an explanation,
leaving foreign behavior and orders untouched. Supported compatible patches SHALL NOT be rejected
merely for being present. Unavailable required behavior SHALL leave the affected owned group inactive
and emit a bounded startup warning identifying the skipped patch/target requirement, reason, unavailable
behavior and fallback. Admission of unrelated groups can remain effective. Unsupported external map
contexts SHALL NOT redirect orders onto the visible parent map; explain unavailable clearing there
and retain ordinary manual work. No source-package identity or unverified 1.7 compatibility is implied.

#### Scenario: Quality retry with an existing plant order
- **WHEN** an authorized quality cycle creates new construction at a site already carrying a plant order
- **THEN** the order is reused once while construction budgets, spent resources and remaining safety conditions are unchanged.

#### Scenario: Blight after choosing extraction
- **WHEN** advance clearing has created or preserved an extraction order and the tree becomes blighted
- **THEN** owned automatic blight cutting leaves that action intact without a competing automatic cut or blight-priority cut choice
- **AND** ordinary extraction eligibility and explicit manual cutting remain available, without automatic catch-up solely from cancelling extraction.

#### Scenario: Original provider co-installed
- **WHEN** either original Cut plants before building provider is active
- **THEN** owned overlapping clearing is disabled with a reason and no foreign patches, preferences or orders are removed.

#### Scenario: Compatible versus changed placement behavior
- **WHEN** another patch preserves the required accepted-placement behavior, or changes it so success can no longer be established
- **THEN** the compatible case can remain enabled and the unsupported case skips the affected group with the specified startup explanation.

#### Scenario: Vehicle or other external map cannot be resolved
- **WHEN** the player's target is an unsupported external map context
- **THEN** no plants on another map are marked and the player receives the unavailable-feature explanation while ordinary manual work remains available.
