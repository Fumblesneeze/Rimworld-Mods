## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S032; future **1.7 beta** behavior.
This display does not change construction costs, physical stock or ordinary action eligibility.

## ADDED Requirements

### Requirement: Construction choices show their own material quantities

The mod SHALL show stored quantity / required quantity alongside each supported building-material
choice and construction dropdown choice, including floors. Required quantity SHALL mean one placed
building or one floor cell, not every planned blueprint or the full current drag. A selectable
Stuff row SHALL show its chosen material; a fixed-cost row SHALL use its ordinary cost-icon material
when available, otherwise the largest-value required ingredient as the primary material. The
material identity SHALL be understandable from the row and its details. Each annotation SHALL
belong to that exact choice, even when two choices have the same label, icon or ingredient.

#### Scenario: A building's material menu
- **WHEN** a supported menu offers a wood version costing 5 wood and 120 wood is counted in storage
- **THEN** that choice shows 120/5 associated with wood while retaining the building's ordinary identity
- **AND** selecting it still selects that building with wood

#### Scenario: Two floors use the same resource
- **WHEN** one dropdown contains two floors costing 4 and 7 units of the same material, with 30 stored
- **THEN** their rows show 30/4 and 30/7 respectively
- **AND** neither row inherits the other's cost because their icons or material match

### Requirement: Quantities use actual adjusted construction costs

Required quantities SHALL use the active difficulty and the exact selected construction choice,
including Stuff volume conversion, ordinary rounding and fixed ingredients in the same material.
The displayed numerator and denominator SHALL use physical item counts consistently. Fixed
ingredient quantities already expressed in items SHALL not receive another volume conversion.
The display SHALL not invent or alter a recipe, research requirement or cost to simplify counting.

#### Scenario: Small-volume Stuff and an additional same-material cost
- **WHEN** a choice requires 5 ordinary units of Stuff with 0.1 volume per item plus 10 fixed items of that same material
- **THEN** its requirement is 60 physical items and is compared with the stored physical count
- **AND** a separate fixed ingredient cost of 25 such items remains 25 rather than becoming 250

#### Scenario: Active difficulty changes costs
- **WHEN** the active difficulty or a supported provider changes a choice's required quantities
- **THEN** its next displayed query uses the current actual costs
- **AND** a previously opened menu does not keep presenting the old requirement as current

### Requirement: Additional costs remain visible and shortage does not forbid planning

For choices requiring multiple materials, the mod SHALL retain the compact primary count and
provide contextual details listing every required material with its stored/required quantity.
The explanation SHALL identify the compact number as one material, not a complete affordability
verdict. Zero and insufficient stored quantities SHALL remain explicit for choices the ordinary
menu includes; this feature SHALL NOT hide, disable or reorder them solely because of shortage.
It SHALL not add new material choices that the native or admitted provider menu does not offer.

#### Scenario: Enough wood but missing components
- **WHEN** a choice has enough stored wood but also requires two components with none stored
- **THEN** its details show both the wood quantity and 0/2 components
- **AND** the wood count does not claim that the whole building is affordable or prevent ordinary blueprint planning

#### Scenario: A zero-stock planning choice
- **WHEN** an ordinary or developer menu includes a valid material with no counted stock
- **THEN** it shows 0/required and retains that menu's normal selection rules
- **AND** merely showing a count does not enable developer privileges or reveal hidden construction

### Requirement: Stored stock has a clear scope independent of builder eligibility

Stored quantity SHALL refer to the current map's ordinary counted-resource stock: eligible fresh
items in its storage locations, excluding fog-hidden stock, with physical stacks counted once.
Ordinary loose piles outside storage, pawn inventories/carry, off-map supplies and materials already
committed inside construction SHALL not be included merely because they exist. Forbidden or
reserved stored material SHALL remain in this raw stored total when ordinary resource accounting
includes it; the display SHALL explain that totals do not guarantee permission, reachability or
unreserved supply for a particular builder. Planned blueprints SHALL not subtract hypothetical
future consumption. A provider storage mechanism needs separately supported accounting.

#### Scenario: Stored and loose material coexist
- **WHEN** a map has 20 counted wood in storage and 15 loose wood outside storage
- **THEN** the displayed stored count is 20
- **AND** the loose pile can still be found or used by other features under their own ordinary rules

#### Scenario: Counted stock is inaccessible
- **WHEN** counted stored material is forbidden, reserved or behind a blocked route
- **THEN** it remains in the raw stored total where ordinary resource accounting includes it
- **AND** the tooltip explains the limitation and actual construction retains ordinary permission/path/reservation checks

### Requirement: Displayed quantities stay current across stock and map changes

After a completed relevant stock change, the next normal redraw/query of an open menu SHALL show
the current count or explicitly unavailable information, rather than a silently stale number.
This includes hauling into/out of storage, movement, consumption, destruction, split/merge, fog or
freshness changes and supported provider transfers. Reopening, switching maps, loading a save or
returning from the world view SHALL use only the current map's stock and choices. Without a map,
the feature SHALL leave ordinary controls usable and not reuse a previous colony's numbers.

#### Scenario: A hauler brings in the loose pile
- **WHEN** a pawn actually hauls the 15 loose wood into the storage containing 20 while the menu is open
- **THEN** the next normal menu redraw shows 35 stored wood
- **AND** carrying the pile on the way there does not count it twice or prematurely as stored

#### Scenario: Another colony has different stock
- **WHEN** the player changes from a map with 35 wood to one with 8 and opens its construction choices
- **THEN** the second map shows 8, with no counts or menu context from the first
- **AND** reloading the save rebuilds current display information rather than restoring stale menu totals

### Requirement: Search and hauling agree about stock changes without sharing filters

A pile's current quantity and location SHALL remain consistent wherever material counts, Ctrl+F
search or hauling reports those facts. Stored counts SHALL apply the scope above without imposing
it on search or hauling. Relevant changes SHALL be reflected consistently rather than leave one
feature describing an earlier location or quantity as current. This consistency SHALL not turn
a count into a reservation, change search filters or add a click-to-search action.

#### Scenario: One pile moves through different uses
- **WHEN** search finds a loose pile, a hauler picks it up and then deposits it in storage
- **THEN** each feature reflects the same current pile identity/location under its own eligibility rules
- **AND** stored counts increase only upon eligible storage membership while search and hauling keep their distinct scopes

### Requirement: Native presentation and construction actions remain intact

Annotations SHALL use the existing contextual menus, with no additional bottom tab or standalone
settings window. Native grid menus SHALL expose equivalent counts and full ingredient details in
their option tooltips without replacing the grid with a list. Original labels, icons, visible/
disabled state, ordering, selection, close behavior and nonconstruction utility entries SHALL
remain usable. Reopening or redrawing SHALL not repeatedly prepend the annotation. Counts and
explanations SHALL remain readable with long localized names and large quantities.

#### Scenario: Grid floors and an eyedropper
- **WHEN** a native floor grid includes construction choices and an eyedropper utility
- **THEN** hovering a floor exposes its correct quantities while the utility remains an ordinary eyedropper without invented costs
- **AND** choosing either retains its ordinary action and menu behavior

#### Scenario: Repeated opens preserve labels
- **WHEN** the same menu is opened, refreshed and reopened repeatedly
- **THEN** each supported row has one annotation and a readable original name
- **AND** no duplicate count prefix or unrelated menu modification accumulates

### Requirement: Unsupported rows and integrations fail locally and truthfully

A no-cost choice SHALL remain usable without a fabricated material ratio. A row whose identity or
cost cannot be established SHALL retain its ordinary presentation; identifiable rows with an
unsupported stock source SHALL explain that the count is unavailable rather than show invented
zero. A utility, nonconstruction or malformed entry SHALL not suppress later valid annotations.
Ordinary compatible modded materials/buildables SHALL work from their actual supported properties,
not a hard-coded material-name list. Custom menu/storage providers SHALL require explicit admitted
support; incompatible enabled patches SHALL be skipped with bounded startup warnings naming the
missing behavior and fallback. Unrelated compatible changes or other patch owners alone SHALL not
cause rejection.

#### Scenario: One mixed-menu entry cannot be counted
- **WHEN** a dropdown has a utility, a no-cost item or an unsupported row followed by a normal floor
- **THEN** the first entries retain useful ordinary behavior and the valid floor still receives its correct annotation
- **AND** unsupported stock is not mislabeled as zero

#### Scenario: A required menu boundary changed
- **WHEN** an enabled menu integration cannot establish which construction choice an option represents
- **THEN** the affected annotation integration stays inactive with a startup explanation and the menu still selects normally
- **AND** a compatible unrelated patch does not disable otherwise supported annotations

### Requirement: Original-provider coexistence and persistence stay explicit

The owned display SHALL remain inactive with a bounded warning when the original Show Buildable
Material Count provider is active, rather than duplicate prefixes. It SHALL not remove
or unpatch the original. Transient menu/count state SHALL not be stored as colony gameplay state;
the feature SHALL not modify saves, resources or construction to preserve annotations. Foreign API
identity or safe source removal SHALL not be claimed without separate consumer evidence.

#### Scenario: Both providers are selected
- **WHEN** the player loads the original provider with megamod
- **THEN** only the original annotation path remains active for the overlap and startup explains the owned display's inactivity
- **AND** unrelated megamod features remain available without automatically changing the mod list
