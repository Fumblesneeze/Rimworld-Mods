# Visible hauled cargo

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). **1.7 beta**.
S126 selects Armful-inspired cargo presentation; broader source hauling is a comparison input.
Specification only. S021 owns physical quantities, capacity, work and recovery. The view toggle,
adaptive layout and compact cargo list below are proposed refinements of the confirmed requirement
to see hauled stacks on moving pawns.

## ADDED Requirements

### Requirement: Show actual held delivery cargo
With extra cargo drawing enabled, the mod SHALL show a carried pile representing the stacks a
supported hauler actually holds for delivery, including while moving. Each held cargo stack
contributes its item appearance once; a stack already pictured in the pawn's hands is not pictured
again. Planned, reserved, already delivered or destroyed goods do not appear as held cargo.
Personal supplies are not portrayed as delivery cargo. If a physical stack mixes personal and
hauled units, its cargo representation and cargo quantity refer only to the hauled portion.

#### Scenario: Pick up two different item types
- **WHEN** an eligible hauler physically picks up steel and wood for delivery and walks away
- **THEN** both held types are represented on that hauler during movement, with no duplicate of the ordinary hand picture.

#### Scenario: Plan succeeds but pickup does not
- **WHEN** a planned extra pickup fails because another pawn has taken the goods
- **THEN** those goods never appear in the hauler's pile or held-cargo list.

#### Scenario: Mixed personal medicine and cargo
- **WHEN** a pawn holds two kept medicine units and five hauled units in one physical stack
- **THEN** the cargo view reports five hauled units, identifies the two retained personal units separately in its details, and does not depict seven units as delivery cargo.

#### Scenario: Personal supplies only
- **WHEN** a pawn carries only personal meals, medicine or equipment
- **THEN** no extra delivery-cargo pile or populated hauled-cargo list is invented for those supplies.

### Requirement: Cargo remains readable as the pawn moves
The mod SHALL use the actual items' recognizable appearance, including applicable material, color
and stack appearance, while preserving the pawn's ordinary carrying position and movement.
Quality and condition remain available in item details even when their ordinary graphics do not
distinguish them. The pile adapts to cargo count and supported pawn size without extending across
unrelated nearby tiles, making the pawn unrecognizable or covering unrelated controls. Pictures
need not show one object per unit; exact quantities belong in the cargo information. At distant
zoom, a compact pile is sufficient; closer inspection exposes individual cargo details.
No adjustable pile-spacing setting is included initially.

#### Scenario: Turn while carrying a mixed pile
- **WHEN** a supported hauler moves and turns through the ordinary facing directions with several cargo types
- **THEN** the pile stays attached to that pawn, preserves recognizable item appearances and does not leave pictures behind on the map.

#### Scenario: Inspect at different zoom levels
- **WHEN** the player views the moving hauler at ordinary close and distant play zooms
- **THEN** the pile remains associated with that hauler, the pawn remains recognizable, and exact stack details remain accessible through selection.

#### Scenario: Similar items have different quality
- **WHEN** two hauled weapons share an ordinary graphic but differ in quality or condition
- **THEN** neither is visually substituted with another item and their separate details identify the correct quality and condition.

#### Scenario: Many light stacks
- **WHEN** an allowed load contains many small stacks
- **THEN** the pile uses a compact arrangement and all actual cargo stacks remain separately inspectable without requiring off-screen controls.

### Requirement: Inspect every actual cargo stack contextually
Selecting a hauler with cargo SHALL expose a contextual **Hauled cargo** control and a concise
current cargo summary. The control opens a scrollable list showing each physical cargo stack's
item, hauled quantity and applicable material, quality and condition, with access to its ordinary
item information. Identical-looking separate stacks remain separate entries. Mixed personal and
cargo quantities are clearly labeled. The list preserves the ordinary carried-item selection and
other pawn commands; it does not promise a future delivery order. This is pawn gameplay
information, not a mod setting or a new bottom tab. Entries disappear when no cargo remains.

#### Scenario: Two identical separate stacks
- **WHEN** the player opens Hauled cargo for a pawn holding two separate identical-looking stacks
- **THEN** both stacks are listed with their own current quantities and each opens information for that actual stack.

#### Scenario: Cargo exceeds the list height
- **WHEN** more stacks are held than fit in the contextual list
- **THEN** scrolling reaches every entry without covering or disabling unrelated pawn commands.

#### Scenario: Click after delivery changed the list
- **WHEN** a listed stack is delivered or removed before the player activates its entry
- **THEN** the view refreshes without selecting a different same-type item or presenting the old stack as still held.

#### Scenario: Other pawn information is extended
- **WHEN** another supported mod adds pawn status text or commands
- **THEN** the cargo summary/list remains available without deleting, duplicating or depending on the wording of that unrelated information.

### Requirement: Follow actual cargo changes and recovery
The mod SHALL refresh cargo pictures and information by the next visible update after actual
pickup, split, merge, partial delivery, consumption, drop, transfer or destruction. An interrupted
job does not by itself erase retained cargo from presentation. Failed placement leaves the
retained goods visible and described as held; nearby recovery placement is not called successful
storage delivery. Loading a save or returning to a map reconstructs presentation from actual
current cargo without phantom goods or pictures on the previous holder.

#### Scenario: Partial delivery
- **WHEN** a destination accepts two of five hauled units
- **THEN** the destination contains the two delivered units and the hauler's display/list reports only the three still held for delivery.

#### Scenario: Interrupt and fail to drop
- **WHEN** hauling is interrupted and safe placement fails so the cargo remains held
- **THEN** the held-cargo view retains the actual remaining stacks and exposes their pending recovery status without inventing a successful delivery.

#### Scenario: Transfer to another holder
- **WHEN** actual cargo transfers from one pawn to another supported holder
- **THEN** it disappears from the previous pawn's presentation and appears only on the current supported hauler when it is still delivery cargo.

#### Scenario: Save during hauling
- **WHEN** the player saves while a pawn holds multiple cargo stacks and reloads that save
- **THEN** the pile, quantities and item details describe the actual restored cargo with no duplicate items or stale pending pickups.

#### Scenario: Destroy or consume part of a mixed stack
- **WHEN** ordinary play removes units from a stack containing personal supplies and cargo
- **THEN** the display follows the surviving personal/cargo quantities determined by the hauling rules and never recreates removed units.

#### Scenario: Leave and revisit a map
- **WHEN** a hauler leaves the viewed map and later returns with a changed load
- **THEN** no pile remains at the old position and the returning presentation reflects the current load.

### Requirement: Presentation never changes hauling mechanics
Drawing, opening cargo information or changing the view SHALL NOT change item counts, carrying
allowance, reservations, work priorities, destinations, personal keep choices, delivery timing or
the result of an in-progress transfer. It does not grant training, hauling permission or support
for otherwise unsupported jobs. The display serves all worker classes and ordinary, urgent,
opportunistic or loading workflows admitted by their owning hauling feature. Native carrying of a
baby, rescued pawn or other non-cargo entity remains unchanged and is never hidden or displaced
to make room for a cargo picture.

#### Scenario: Toggle at full capacity
- **WHEN** the player turns extra cargo drawing on or off while a hauler is full
- **THEN** its held goods and remaining carrying allowance do not change and no additional pickup becomes possible because of the toggle.

#### Scenario: Supported animal or mech hauler
- **WHEN** a trained animal or eligible mech performs a hauling workflow supported by the hauling feature
- **THEN** its actual cargo receives the same truthful presentation without gaining new work or capacity from this display feature.

#### Scenario: Carry a rescued pawn
- **WHEN** a pawn carries a rescued pawn while delivery cargo is also retained
- **THEN** the rescued pawn's native picture and physical handling remain intact; cargo details remain accurate, and extra cargo drawing yields where necessary to preserve that carrying presentation.

### Requirement: View controls use common defaults and save overrides
Extra cargo drawing SHALL be enabled by default. A small bottom-right **Show hauled stacks**
toggle changes only the current save's effective view preference and immediately updates the
view. Using that toggle creates or updates the current-save override; it never writes the global
default. The same preference appears in ordered **Hauling → Cargo display** settings with
**Global default**, **Use global** and **Current save** controls. There is no second hidden toggle
state. New or missing save entries inherit the current global default. Checking Use global retains
the dormant override; unchecking restores it or seeds it from the current effective value.
The per-save editor is inactive while inheriting, and all save controls are inactive at the main
menu. Contextual cargo information remains available when extra pictures are off. UI text and
disabled-state explanations are localized in all seven repository languages.

#### Scenario: Toggle an inherited save
- **WHEN** an inheriting save currently shows extra stacks and the player turns off the bottom-right view toggle
- **THEN** that save gains an off override, the pictures change immediately, global defaults remain unchanged, and settings show the same effective state.

#### Scenario: Inherited and overridden saves
- **WHEN** the global default changes while one save inherits and another has an override
- **THEN** the inheriting save follows the new default and the overridden save retains its own choice when loaded.

#### Scenario: Restore inheritance and the dormant choice
- **WHEN** the player checks Use global and later unchecks it
- **THEN** the inherited view applies while checked and the previously saved override returns when unchecked.

#### Scenario: Main menu settings
- **WHEN** settings are opened with no active save
- **THEN** Global default remains editable, both Use global and Current save are visibly inactive with an explanation, and no previous or implicit save is changed.

#### Scenario: Pictures disabled
- **WHEN** extra cargo drawing is off while a pawn holds several cargo stacks
- **THEN** native carrying presentation remains and the complete cargo summary/list still exposes all actual held cargo.

### Requirement: Unsupported presentation fails without affecting cargo
If a required drawing or inspection behavior cannot be supported safely, the mod SHALL withhold
only the affected presentation enhancement, retain available ordinary carrying/item information
and leave physical hauling unchanged. It records a bounded startup warning for each skipped
enabled enhancement, identifying its documented target, failed condition, unavailable behavior
and fallback. Compatible changes by other mods are allowed. Known overlapping source renderers
use a declared single-provider policy instead of drawing duplicate piles; no other mod is removed
or disabled automatically. A view unavailable because of compatibility is visibly distinguished
from a user preference turned off.

#### Scenario: Drawing unavailable but cargo inspection works
- **WHEN** the game or a provider no longer supports the required extra-stack drawing behavior
- **THEN** ordinary carrying remains, available cargo inspection stays accurate, the view control explains the drawing limitation, and startup logs identify the skipped drawing enhancement.

#### Scenario: Compatible foreign change
- **WHEN** another mod changes unrelated pawn drawing or status behavior while the required cargo presentation remains supported
- **THEN** the compatible enhancement remains available without duplicate pictures or lost foreign information.

#### Scenario: Source Armful is also active
- **WHEN** source Armful already supplies the overlapping carried-stack presentation
- **THEN** the owned overlapping enhancement is withheld with a startup explanation, without changing either provider's cargo, copying source settings or installing duplicate stack pictures or commands.

#### Scenario: External provider disappears between sessions
- **WHEN** a save is loaded without a previously used external renderer or cargo provider
- **THEN** the display uses only the actual supported current cargo, retains its saved view preference and reports unavailable enhancements without manufacturing or migrating items.
