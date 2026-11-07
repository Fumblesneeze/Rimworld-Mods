# Quick storage creation

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S107, Quick Stockpile Creation. Specification only; no implementation accepted.

## ADDED Requirements

### Requirement: Start storage from a selection of real items
The mod SHALL offer one contextual Create storage command for selected visible haulable items on the current map, preserving their ordinary commands.

Left-click starts a fresh stockpile tool for the distinct selected item types; right-click offers
content/category choices and supported building alternatives. It does not also open an unsolicited
menu on the direct left-click path. Mixed selections safely ignore objects that are not eligible
items, explain omitted entries and reject an empty result. Packed objects use their contained
storable type without unpacking them; the outer physical object supplies location and current storage.
An exact-type choice identifies the item type, not that sample's material, quality, condition or
other incidental state; a narrower state restriction requires an explicit supported choice.

#### Scenario: Several stacks and types
- **WHEN** the player selects two steel stacks and one wood stack and activates Create storage
- **THEN** the tool proposes a stockpile for steel and wood once each, without selecting all resources
- **AND** existing selected items remain where they were and unchanged

#### Scenario: Packed object and mixed selection
- **WHEN** the selection contains a packed chair, a haulable resource and an unrelated non-item object
- **THEN** the proposed type choices include the chair's storable type and resource, excluding the unrelated object
- **AND** the chair stays packed and its outer location is used for current-storage decisions

#### Scenario: Stale menu or empty selection
- **WHEN** selection or map changes before an open menu choice is activated, or no eligible item remains
- **THEN** the old choice changes nothing and explains that the player must reopen it from a valid selection
- **AND** the UI does not throw or act on a different map's selection

### Requirement: Derive meaningful category and state choices
The mod SHALL offer deduplicated categories shared by all eligible selected items, including every supported category ancestry, and an explicit selected-types choice even when no category is shared.

The universal root is not a content shortcut. A category choice includes its available descendants,
including supported modded types, restricted by the chosen storage's intrinsic abilities. A partial
category explains its usable subset; an empty choice is unavailable. Zone creation also offers
supported configurable state choices matching all selected items, such as clean/tainted apparel,
smeltable/non-smeltable, burnable/non-burnable and biocoded/non-biocoded equipment, or fresh/rotten
remains. Unrelated state restrictions are not guessed to be opposites. A fresh-only shortcut is not
offered for selected food which is destroyed by rotting and cannot remain in a rotten state.

#### Scenario: Shared category versus exact types
- **WHEN** selected hats and coats share Apparel and the player chooses that category
- **THEN** the new stockpile permits available apparel types, including supported modded apparel
- **AND** choosing selected types instead permits only the selected types

#### Scenario: Multiple category paths and absent ancestry
- **WHEN** items share a valid category through a secondary category assignment
- **THEN** it is offered once
- **WHEN** an eligible item has no category ancestry or a malformed category cycle
- **THEN** valid exact-type creation remains available without an unbounded traversal or invented category

#### Scenario: Tainted apparel
- **WHEN** all selected eligible apparel is tainted and the player chooses a tainted-apparel zone
- **THEN** the category's usable apparel types are allowed, tainted is allowed and clean is disallowed
- **AND** unrelated configurable states remain unrestricted and intrinsic/nonconfigurable restrictions remain intact

#### Scenario: Unknown special-state meaning
- **WHEN** the claimed state-only choice cannot be expressed reliably without rejecting unrelated states or admitting its opposite
- **THEN** that state variant is unavailable with an explanation
- **AND** ordinary exact-type and category choices remain usable where supported

### Requirement: Use one predictable initial priority rule
The mod SHALL initialize every quick-created zone or building with the same selection-order-independent priority rule.

The global built-in priority is Preferred; automatic higher-priority creation is on. For each
selected physical item, use the configured default if it has no current storage or promotion is
off; otherwise use one level above its current storage's effective priority, capped at Critical.
The creation priority is the highest of these per-item results. The tool shows that priority
before placement. A capped promotion reports once per committed command that it cannot exceed
Critical; browsing a menu does not repeat warnings. A source's low priority is not automatically
raised to at least Preferred when its actual promoted result is lower.

#### Scenario: Multiple source priorities
- **WHEN** selected items occupy Low and Important storage and promotion is on
- **THEN** the proposed priority is Critical regardless of which item was selected first or which creation route is used

#### Scenario: Unstored and low-priority items
- **WHEN** a Low-stored item is selected alone with promotion on and default Preferred
- **THEN** its new storage is Normal
- **WHEN** an unstored item is included too
- **THEN** the shared creation priority is Preferred

#### Scenario: Critical and disabled promotion
- **WHEN** a selected item already occupies Critical storage with promotion on
- **THEN** new storage remains Critical and the committed command explains the cap once
- **WHEN** promotion is off
- **THEN** all quick-created storage uses the configured default, regardless of source storage

### Requirement: Paint fresh zones without modifying existing zones
The mod SHALL create separate stockpile zones only on currently eligible free cells, using ordinary map, fog, terrain and obstruction rules.

It does not resize, merge into, rename or replace an existing zone merely because it was selected,
touched or adjacent. Each connected accepted region becomes a new zone. Each new filter allows the
chosen types; configurable states start unrestricted except for any specifically chosen state
restriction. Native initial quality, condition and risk ranges and intrinsic/nonconfigurable limits
remain intact. This new-filter initialization never resets existing storage. Choices are committed
to native storage policy, not maintained by a continuously reapplied
template. Cancelled or wholly invalid placement creates nothing.

#### Scenario: Drag across an existing stockpile
- **WHEN** a quick-storage drag covers free cells and an existing weapon stockpile
- **THEN** only eligible free regions receive new zones and the weapon stockpile's cells, name and policy remain unchanged

#### Scenario: Separate free regions
- **WHEN** an accepted drag contains two disconnected free regions separated by unusable cells
- **THEN** each new region receives the chosen filter and priority
- **AND** no fogged or invalid cell is revealed or taken over

#### Scenario: Cancel before placement
- **WHEN** the player cancels a prepared zone tool without committing cells
- **THEN** no zone, filter change or item movement occurs

### Requirement: Name only newly created zones
The mod SHALL give each new zone a localized content-based name with a distinct numeric suffix beginning at1, optionally prefixed by a meaningful room role when the entire new zone occupies that room.

Single-type/category/state choices identify their contents; multiple unrelated types use a concise
multiple-items description. Generic rooms, open ground and zones spanning rooms omit the room role.
Names remain editable. Later drags, language/room changes or subsequent commands do not rename earlier
zones, including zones created earlier by the same still-selected tool.

#### Scenario: Two new food zones
- **WHEN** the player creates two zones for the same content in the same meaningful room context
- **THEN** their generated names identify that content and are distinct

#### Scenario: Preserve a manual rename
- **WHEN** the player renames a created zone and later continues using the creation tool
- **THEN** the earlier zone keeps the player's name

### Requirement: Create filtered native storage buildings
The mod SHALL offer the small and large shelf and small and large bookcase templates when enabled, present, supported, unlocked by ordinary research and capable of accepting every exact selected item in the chosen exact-type route.

Category routes can use only their intrinsically accepted subset and explain any narrowing. A
bookcase is not offered for an exact selection containing both books and steel. Shelf restrictions
on chunks, packed buildings, large corpses and any current provider restrictions remain in force.
No arbitrary modded container is added to the creation catalog merely because it stores things.

Materials are chosen through a contextual construction control from the building's legal materials,
with current availability shown. A legal unavailable material can be planned for later delivery;
availability is not a promise of sufficient or reachable stock. An earlier material choice cannot
force an illegal material onto another building or map. Rotation, costs, labor, research, style,
capacity and ordinary construction outcomes remain native.
New building filters use the same initial content/state policy as newly created zones, within the
building's intrinsic restrictions; they do not retain unrelated default item-type selections.
New storage starts independent; merely placing it near existing storage does not join a group or
overwrite that group's policy. Later explicit linking follows the ordinary linking contract.

#### Scenario: Mixed books and resources
- **WHEN** books and steel are selected for exact-type storage
- **THEN** bookcases are unavailable for that complete selection, while eligible shelves remain possible
- **AND** selecting only books exposes supported bookcase choices

#### Scenario: Material and construction
- **WHEN** the player chooses a legal material and places a quick shelf
- **THEN** an ordinary construction order uses that material and normal costs/work
- **AND** insufficient supplies leave ordinary construction waiting rather than grant free materials

#### Scenario: Native god mode was already enabled
- **WHEN** the player has explicitly enabled native god mode and places admitted quick storage
- **THEN** the native immediate-build result receives the same chosen filter and priority once
- **AND** this command does not enable god mode itself or change unrelated immediate construction

#### Scenario: Blueprint becomes a storage building
- **WHEN** workers complete a quick-created storage blueprint, including after saving and loading during construction
- **THEN** its chosen filter and priority remain on the completed building
- **AND** subsequent manual edits are retained through the native construction lifecycle rather than overwritten by the original template

#### Scenario: Missing or changed building template
- **WHEN** one template is absent, locked, intrinsically unsuitable or has an unsupported creation/settings path
- **THEN** that choice is unavailable with the relevant explanation and other admitted templates and zone creation remain usable

### Requirement: Preview and commit the same bounded placement plan
The mod SHALL preview and place the same deterministic nonoverlapping building arrangement for the selected material, rotation and drag, including a one-cell gesture when it is valid.

The preview respects footprint and required interaction access. Existing buildings, frames,
blueprints and unrelated zones/items are not replaced to make room for this quick command; normal
construction can move loose obstructions only through its ordinary workflow. Previewing, cancelling
or switching tools never inserts physical items, changes world lookups or changes other building
definitions. Cancel or map/game change discards the pending plan and temporary material choice.
After a plan is selected its content/priority is fixed until changed or cancelled; moving the
original sample items does not silently retarget it. Each placement rechecks current validity.

#### Scenario: Multi-cell shelves and rotation
- **WHEN** the player rotates and drags two-cell shelves across a rectangle
- **THEN** the preview and committed orders use the same valid nonoverlapping footprints
- **AND** interaction access and occupied cells are not double-booked by the preview

#### Scenario: One cell and tool switching
- **WHEN** the player previews and commits one valid small shelf
- **THEN** the one-cell preview and created order agree
- **WHEN** the player switches to an ordinary building tool afterward
- **THEN** its normal preview and placement work without retained quick-storage state

#### Scenario: Map changes during planning
- **WHEN** a map switch, game unload or cancellation interrupts a prepared tool
- **THEN** no later click commits that plan to a new context and no world contents or definitions remain altered

#### Scenario: Some cells become invalid
- **WHEN** a cell becomes obstructed after preview but before placement
- **THEN** that footprint is skipped with feedback while successfully committed independent footprints retain their correct settings
- **AND** an individual failed creation does not leave an unconfigured owned order or roll back unrelated successful orders

### Requirement: Use common settings and native persistence
The mod SHALL expose initial priority, automatic priority promotion, shelf choices and bookcase choices through common global/save/Use global rows under Storage / Quick creation.

Built-in values are Preferred, on, on and on; legal priorities are Low, Normal, Preferred, Important
and Critical. Changes affect newly prepared commands, not existing storage or an already prepared
plan. Globals and save overrides persist independently, dormant overrides survive Use global, and
main-menu save editors and callbacks are disabled. Invalid persisted entries use their own built-in
defaults; missing save overrides inherit. Selected-item, material, pending-tool and undo state are
contextual gameplay/session state, not inherited settings. No source settings import is automatic.

#### Scenario: Save inheritance and main menu
- **WHEN** the player changes a global default while one save inherits and another overrides it
- **THEN** newly prepared commands use each save's effective choice after reload/switching
- **AND** the main menu permits global edits but no save or Use global mutation

#### Scenario: Existing filters remain editable
- **WHEN** the player changes or copies a created storage's policy, links/unlinks it, or reloads the game
- **THEN** native policy and S005 linking rules preserve those edits
- **AND** this feature does not reapply an old creation plan or overwrite S049 refill settings

### Requirement: Admit creation safely and keep its UI contextual
The mod SHALL expose only creation actions whose documented runtime conditions hold, with one bounded startup warning per skipped enabled patch identifying the unavailable behavior and ordinary fallback.

Compatible unrelated patches remain allowed. Original Quick Stockpile Creation owns overlap when
active; owned commands are then unavailable without unpatching the provider. Activation failure
or supported later invalidation withdraws only the affected owned group and its pending plans.
All labels, scope/priority/material explanations and unavailable reasons use the seven baseline
languages. No bottom tab, bottom-right toggle or automatic cheat behavior is added. Native manual
zone/building commands remain available. Diagnostic logging does not repeat per preview frame.

#### Scenario: Changed creation path
- **WHEN** required blueprint-setting semantics cannot be verified
- **THEN** quick-building creation is inactive with its bounded warning, while independently admitted zone creation and ordinary building tools still work

#### Scenario: Original provider and partial failure
- **WHEN** the original provider is active or an owned integration group fails partway through activation
- **THEN** no duplicate or half-configured owned command remains, its warning explains fallback, and foreign patches remain intact

#### Scenario: Localization and placement of controls
- **WHEN** the player uses quick storage in English, German, Spanish, French, Chinese Simplified, Russian or Japanese
- **THEN** contextual actions and settings explanations are readable without raw keys or English-only label trimming
- **AND** no separate bottom-bar management tab is required
