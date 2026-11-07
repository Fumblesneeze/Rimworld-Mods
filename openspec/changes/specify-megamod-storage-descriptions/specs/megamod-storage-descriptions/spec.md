# Storage descriptions

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S098/P2-27. This is a specification draft, not implemented behavior. Corrections
and extensions below are owned choices, not claims of exact source parity.

## ADDED Requirements

### Requirement: Explain storage on existing description surfaces
The mod SHALL add one clearly separated storage-information section to supported storage buildings'
construction descriptions and ordinary information-card descriptions.
The section describes the building being considered or inspected, not its linked group's aggregate
capacity. Show the building type's supported capacity when no instance exists, and the particular
building's actual supported capacity when an instance is being inspected. Label any type-level
estimate or unresolved quality-dependent value rather than presenting it as an exact instance value.
Native shelves, small shelves and bookcases are included. Specialized storage can participate when
its real storage behavior is supported. Bedding, fuel, ammunition or ingredient settings alone do
not qualify a building as ordinary item storage. Stockpile zones and other nonbuilding entities do
not acquire a new description through this feature. Empty original prose does not suppress otherwise
valid storage information. Reading a description never changes storage or performs work.

#### Scenario: Compare before construction
- **WHEN** the player reads an ordinary shelf's construction description
- **THEN** its description includes storage capacity and supported item types before the player builds it.

#### Scenario: Inspect one linked shelf
- **WHEN** the player opens the information card for one member of a linked storage group
- **THEN** the summary describes that building rather than adding together every linked member.

#### Scenario: Bedding is not a warehouse
- **WHEN** a bed exposes settings for choosing its bedding but has no supported ordinary storage function
- **THEN** its description does not acquire a generic item-storage capacity summary.

#### Scenario: No original prose
- **WHEN** a supported storage building has no ordinary description text
- **THEN** its available storage information is still readable, without an empty leading paragraph or error.

### Requirement: Distinguish capacity from item quantity and refill policy
The summary SHALL state total physical storage capacity in its real unit and, where meaningful,
capacity per storage cell.
For ordinary shelves this means stack slots, not the number of individual items, items currently
stored or free slots. An ordinary two-cell shelf with three stack slots per cell shows six total
stack slots and three per cell. A native two-cell bookcase with capacity for ten books shows ten
books, not ten arbitrary stacks. For nonuniform storage, show the supported total and indicate that
per-cell capacity varies instead of multiplying a maximum by every occupied cell. Unsupported or
context-dependent values are marked unavailable or conditional, never invented as zero or an exact
rectangle-derived total. Native or provider physical limits, quality effects and supported capacity
configuration remain authoritative. Refill targets, current allowed-item selections and limits on
automatic filling do not change this physical maximum. If shown alongside it, such policy limits
must be separately labeled. This feature does not change physical limits or hauling behavior.

#### Scenario: Stack slots are not units
- **WHEN** six shelf slots can hold items with different stack sizes
- **THEN** the summary still says six stack slots and does not imply six individual items or a universal item count.

#### Scenario: Bookcase capacity
- **WHEN** the player inspects an ordinary native bookcase whose physical capacity is ten books
- **THEN** its summary states ten books and does not imply that other item types can occupy them.

#### Scenario: Nonuniform optional storage
- **WHEN** a supported storage provider gives three cells capacities of two, four and zero slots
- **THEN** the summary states six total slots and varying per-cell capacity, not twelve slots.

#### Scenario: Lower automatic refill limit
- **WHEN** a shelf's automatic filling is limited below its physical capacity
- **THEN** its physical capacity stays unchanged in the description and reading it does not alter that policy.

### Requirement: Summarize supported item types truthfully
The item summary SHALL describe what the storage can physically be configured to accept, not its
initial or current player-selected filter.
Use the actual supported restrictions. Do not infer unrestricted storage from a missing filter or
from the mere presence of one allowed type in every category. A known empty allowed set says None;
unknown support says unavailable. An unrestricted claim is valid only when all otherwise storable
item types are supported and no additional item-specific restriction is being hidden. Fully covered
categories can use their category name; partial categories are identified as partial, or expanded
into accurate narrower categories/types. Do not label a book-only filter as unrestricted Items.
Indicate additional condition, quality, size or other special restrictions that cannot be conveyed
by type names alone. Type eligibility is not a promise that every individual item or delivery is
currently acceptable. Keep current player policy in the existing storage controls.

#### Scenario: Empty filter
- **WHEN** an admitted storage building supports no item types
- **THEN** its supported-items summary says None, never Everything.

#### Scenario: A small subset of a broad category
- **WHEN** storage supports books but not the other items in their broad parent category
- **THEN** the summary identifies books or a clearly partial category rather than claiming the whole category.

#### Scenario: Special restrictions
- **WHEN** otherwise supported corpse types are subject to a large-corpse exclusion
- **THEN** the summary communicates that additional restriction and does not promise every corpse is acceptable.

#### Scenario: Player policy is narrower
- **WHEN** a player configures a general-purpose shelf to accept only medicine
- **THEN** its physical supported-types summary remains general-purpose while the existing storage controls still show the medicine-only policy.

### Requirement: Replace only recognized obsolete storage information
The mod SHALL preserve unrelated description content, including paragraphs after a storage section,
translated prose, formatting and independently supplied nonstorage information.
A known obsolete storage-stat block can be replaced only when its identity and complete boundaries
are established. Replace just that block and present one current storage section. A coincidental
heading, unknown format or ambiguous boundary must not authorize deleting the rest of a description.
When an existing competing storage block cannot be safely separated, retain it, leave the new
competing section inactive for that surface and explain the unavailable integration diagnostically.
Nonstorage descriptions remain unchanged. Repeated openings never append another owned copy.

#### Scenario: Recognized block followed by other prose
- **WHEN** a supported description contains a recognized old storage-stat block followed by a warning about refrigeration
- **THEN** only the old block is replaced and the refrigeration warning remains.

#### Scenario: Similar heading in unrelated text
- **WHEN** a nonstorage description contains the words Storage stats in a paragraph
- **THEN** the description is preserved without truncation or a new storage section.

#### Scenario: Unknown block boundaries
- **WHEN** a competing storage section cannot be safely separated from surrounding text
- **THEN** its text remains intact and the owned competing section is withheld with a bounded diagnostic explanation.

#### Scenario: Reopen the same description
- **WHEN** the player repeatedly opens the same admitted description
- **THEN** it contains one owned storage section with no accumulating copies.

### Requirement: Keep storage descriptions current and readable
The mod SHALL reflect current admitted capacity and supported-filter facts on the next relevant
description display, including after supported provider configuration changes while paused.
Instance-specific facts cannot leak to another building, map, type preview or save. A change in
language, heading color or number formatting refreshes the presentation without restarting the game.
Use the current localized category/item labels and all seven baseline languages. Preserve number
meaning and units through common number formatting. Long lists and translated text remain readable
within the existing description surface; use its ordinary scrolling/wrapping or a clearly labeled
continuation into the information card, without silently hiding restrictions or overlapping controls.
No bottom tab, debug command or bottom-right toggle is added for this information.

#### Scenario: Supported capacity change while paused
- **WHEN** a supported provider's configuration changes a storage building's actual capacity while paused
- **THEN** the next relevant description shows the new capacity rather than a startup snapshot.

#### Scenario: Different instances and a type preview
- **WHEN** two supported buildings have different quality-dependent capacities and the player also opens their construction preview
- **THEN** each instance shows its own capacity and the preview labels its type-level assumption or conditional value.

#### Scenario: Long translated summary
- **WHEN** the player views a long supported-item list in a baseline language
- **THEN** all summary content is reachable and readable without text overlap, dropped restrictions or raw translation keys.

### Requirement: Configure the heading color through common settings
The mod SHALL expose Storage descriptions → Heading color through the common settings interface.
The built-in value is orange **#E5A84C**. Accept six-digit RGB hexadecimal colors with a leading #,
case-insensitively; normalize accepted values consistently. Incomplete or invalid edits leave the
last valid effective color in use and show validation feedback. Reset restores this same built-in
value in the selected scope, rather than a different undocumented color. Apply valid changes on
the next display. This is presentation configuration, not building-specific gameplay state.

Provide Global default, Current save and Use global through the common load/store/render path.
New or missing save values inherit dynamically. Inherited editors are inactive and dormant
overrides survive; clearing Use global restores the override or seeds it from the current effective
value. With no loaded game, save controls and their mutation callbacks are inactive. Changes to one
scope do not overwrite another save or global state. No source-settings import is implied.

#### Scenario: Invalid color edit
- **WHEN** the player enters an incomplete color or rich-text markup instead of a valid RGB color
- **THEN** descriptions retain the previous valid color and the setting reports the invalid input without changing unrelated text.

#### Scenario: Consistent reset
- **WHEN** the player resets an editable color override to its built-in value
- **THEN** that override becomes #E5A84C and does not silently reset the global default or another save.

#### Scenario: Inherit and switch saves
- **WHEN** a global color changes while one save inherits and another retains an explicit override
- **THEN** the inheriting save uses the new color, the overriding save keeps its color, and switching saves refreshes descriptions.

#### Scenario: Main menu settings
- **WHEN** settings are opened with no loaded game
- **THEN** only global color configuration is editable and no save-control callback can mutate an implicit or previously loaded save.

### Requirement: Preserve other providers and ordinary storage behavior
The mod SHALL activate only supported description contributions and preserve unrelated compatible
modifications.
With the original Storage Stats in Description provider active, withhold owned overlap and report
that choice without changing the provider. Another description provider's existing storage summary
requires an admitted combined presentation or deferral of the owned competing section on that
surface; its nonstorage information remains. Missing or changed optional storage behavior leaves
unknown facts visibly unavailable or the affected contribution inactive, with bounded diagnostics,
while independently supported descriptions continue. Startup warnings identify every skipped
enabled patch, its failed requirement, unavailable behavior and fallback. Display failures never
change storage capacity, filters, linking, refill policy, item contents or jobs.

#### Scenario: Original provider present
- **WHEN** the original provider is active alongside megamod
- **THEN** owned overlapping description generation is inactive with an explanation and the original provider is not removed or modified.

#### Scenario: Another description provider
- **WHEN** another active provider already supplies storage information on the construction surface and no admitted combined presentation exists
- **THEN** the owned competing section is withheld on that surface while unrelated provider details and independently supported information-card presentation remain.

#### Scenario: Changed specialized storage
- **WHEN** an optional provider's capacity semantics can no longer be determined safely
- **THEN** its unknown capacity is not shown as a guessed exact total, ordinary storage still works, and supported native shelf descriptions remain available.
