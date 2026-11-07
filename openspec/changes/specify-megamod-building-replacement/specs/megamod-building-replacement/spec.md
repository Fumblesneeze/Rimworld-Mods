# Building replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Documentation-only contract for S028; no behavior is implemented or accepted.

## ADDED Requirements

### Requirement: Explicit material replacement orders
The player SHALL be able to select Replace Stuff in the Structure tools, choose a material and drag over eligible buildings or construction plans, or place the same building in the same orientation with a different material.
Ordinary choices use materials known on the current map and accepted by the target. Wood is the
initial selection when eligible; an unavailable choice never silently substitutes another material.
Player buildings and claimable neutral buildings qualify; ordering a neutral target claims it.
Enemy, unclaimable, non-material, incompatible-terrain and already conflicting upgrade targets do
not qualify. The preview explains rejection and counts each physical target once, including a
large building touched by several dragged cells. Normal access, research and placement restrictions
remain unless a specific planning requirement below relaxes that restriction.

#### Scenario: Replace a large building once
- **WHEN** the player drags a material order across several cells occupied by one eligible building
- **THEN** one replacement is ordered, one building's material cost is displayed and its current material remains until work completes

#### Scenario: Invalid target or repeated order
- **WHEN** the target rejects the material, belongs to an enemy or already has the same replacement order
- **THEN** the order is rejected with a reason and no second plan, resource obligation or ownership change is created

### Requirement: Material replacement preserves the building and pays for reconstruction
Material replacement SHALL keep the existing building, its enclosure and its compatible resident configuration while requiring new material and removal plus construction work.
New material quantity is the building's material count adjusted for that material's volume, rounded
as ordinary construction and at least one unit; fixed ingredients such as components are not
charged again. Removal work uses the old building/material's normal deconstruction work, including
the current native minimum **20** and maximum **3000** work, plus normal construction work for the
new material. Old material returns once using the building's ordinary deconstruction return rules,
not a universal refund percentage. Success consumes supplied new material, restores full HP and
rerolls any construction quality using the completing builder. Bills, ownership, storage choices,
temperature and other state that belongs to this same building are not reset by the material change.
The selected quality-construction rules still govern eligible workers and any finite retry budget.
Material completion produces exactly one quality result; quality automation does not add a second
roll to that completion. A below-target result can lead only to an ordinary, separately paid quality
cycle within the existing run's remaining allowance. Changing material does not restart that run.

#### Scenario: Material change on an owned bed
- **WHEN** a builder completes a valid material replacement on a bed
- **THEN** its material and displayed stats update, it has full HP and the builder's resulting quality, and its owner and bed-use choices remain
- **AND** the colony pays the new material and work and receives only the old material's normal deconstruction return

#### Scenario: Fixed components and quality retries
- **WHEN** a quality-targeted building with fixed components receives a material replacement
- **THEN** those components are neither charged nor refunded, and replacement does not reset or multiply the remaining quality retry allowance

#### Scenario: Material completion on an exhausted quality project
- **WHEN** a material replacement completes on a building whose quality project has exhausted its attempts
- **THEN** completion produces one builder-generated quality result and retains exhaustion, with no second roll or automatic fresh quality cycle

### Requirement: Retargeting and failure preserve only earned work and actual resources
Changing or canceling a replacement SHALL preserve the intact old building and account for actual supplied resources without duplicating them.
Changing a normal blueprint or ordinary unfinished construction to another material cancels its old
plan, returns its actual contents and creates a fresh plan without retaining assembly work. Changing
an in-progress material replacement returns its inputs and retains at most completed removal work;
assembly work for the previous material is lost. Selecting the original material cancels that
replacement. Construction failure in the assembly phase retains completed removal work, applies
ordinary failure losses to supplied new material and requires replacement inputs/work; removal
itself does not fail and consume material. Canceling returns remaining actual inputs. Missing or
destroyed original targets cancel the owned replacement and recover its remaining inputs without
recreating the old building or yielding its material again.

#### Scenario: Retarget after partial assembly
- **WHEN** the player changes a partly worked steel replacement to stone
- **THEN** supplied steel is returned, completed removal work is retained, and stone assembly starts without the steel assembly progress

#### Scenario: Failure and cancellation
- **WHEN** construction fails after removal work and the player subsequently cancels
- **THEN** the old building remains unchanged and the colony receives only the actual material left after ordinary failure losses, with no second refund

#### Scenario: Original destroyed during work
- **WHEN** the old building is destroyed or removed before its material replacement finishes
- **THEN** the replacement cannot finish against another building at that location and its remaining supplied inputs are recoverable

### Requirement: Construction use is suspended without destroying other work
Replacement SHALL leave a building ordinarily usable while only planned, and suspend incompatible use once physical replacement work starts.
Workbenches stop starting or continuing incompatible bill work; partial production, committed
ingredients, counters and whole-list relationships remain governed by the owned linked-workbench
contract. Turrets do not start new attacks while being worked on. A replacing bed is not newly
selected for rest, and its ownership is retained rather than automatically claiming another bed.
Work that would invalidate an occupied bed waits until its occupants can leave through normal game
behavior; an occupied medical bed is not forcibly removed. Existing committed activity must settle
safely before the incompatible construction transition. Cancellation restores use when other
ordinary conditions permit it. This does not turn off every building function: a retained wall
still seals the room and a cooler still follows its ordinary operating requirements.

#### Scenario: Workbench with partial production
- **WHEN** physical replacement begins while a workbench has partially completed bill work
- **THEN** that work is safely suspended or retained for an eligible linked continuation, without erasing ingredients, crediting a product or restarting its production count

#### Scenario: Occupied medical bed
- **WHEN** an upgrade would remove a bed still occupied by a patient
- **THEN** completion waits with a visible reason until the bed is safely free; the patient and ownership are not lost or teleported

### Requirement: Different-building upgrades have an unambiguous target
The player SHALL be able to place a supported replacement building over one eligible existing building and keep the old building until the new one can be safely completed.
Supported families include native replacement relationships and the examined wall/fence/door,
cooler, constructed-bed, table, plant-growing, battery and like power-generator families. The
source-derived ordinary groups include dressers, end tables, lights, wall lights, research benches,
sun lamps, columns, fueled generators, televisions, solar generators, conduits, shelves and the
corresponding art/smelting/smithing/stonecutting/butchering/drug/machining/stove/tailoring/fabrication/
campfire/cremation/refining stations. A one-member group does not invent an alternative building.
Optional content only extends groups when its actual buildings and necessary state handling are
supported. Eligibility is not permission to ignore research, skill, allowed material, footprint,
placement or provider restrictions. Free special-purpose targets such as sleeping spots or
nonconstructible vacuum checkpoints do not erase existing buildings through this workflow.

An order identifies one old building. If a larger footprint overlaps several replaceable buildings,
it is rejected with an explanation instead of choosing one or demolishing the others. Compatible
coexisting objects such as an allowed conduit or wall attachment are not automatically removed.
The player can use ordinary separate deconstruction for unsupported arrangements. Reinstalling an
existing item consumes that physical item and follows native installation costs, not a free newly
created copy or an invented full-build material bill. Its actual HP, quality and existing
configuration remain authoritative. Target-building choices can transfer only where compatible
without overwriting that installed item's existing choices or silently merging/detaching groups.
A conflict between consequential existing choices rejects the install-over order before either
object is removed; the player can resolve it through the ordinary explicit configuration/linking
workflows. An empty destination field alone does not authorize overriding a group relationship.

#### Scenario: Upgrade a fueled stove
- **WHEN** the player places an eligible electric stove over one fueled stove
- **THEN** the old stove remains until completion, the new stove requires its full ordinary construction inputs plus old-stove removal work, and the old stove returns normal deconstruction materials once

#### Scenario: Ambiguous footprint or forbidden upgrade
- **WHEN** a proposed large building overlaps two eligible old buildings, or the requested target fails research or special placement rules
- **THEN** the replacement is unavailable with a reason and neither old building is removed

#### Scenario: Install an already-built item
- **WHEN** the player orders a supported minified building installed over an eligible old building
- **THEN** the actual selected item is installed through ordinary installation work and the old building is removed only on successful completion, without duplicating the selected item
- **AND** the installed item's actual quality, HP and compatible existing configuration remain unchanged

#### Scenario: Install workbench from another linked group
- **WHEN** a minified workbench belonging to group B is ordered installed over a workbench belonging to group A
- **THEN** the conflicting install-over order is refused before removal, both groups retain their lists and accounting, and the player receives an explanation rather than an implicit group merge

### Requirement: Upgrade transfer preserves defined choices without inventing supplies
A completed upgrade SHALL transfer supported player choices once and preserve recoverable physical contents while respecting what the new building can actually do.
Transfer includes bills and their owned worker/material/count policies, storage filters/priority,
linked storage/workbench membership, cooler target temperature, bed medical/prisoner status and
eligible owners, and supported growing-building crop choice. Existing destination restrictions
still limit effective storage and crop eligibility. A retained but unavailable bill remains visibly
unavailable, not deleted or silently changed to another recipe. Structural loss of recipe/provider
support blocks new work on that row throughout a linked group until support is restored or the
incompatible member is explicitly removed. Already committed physical work follows the shared
production rules; it does not authorize a new iteration on the structurally invalid group.
A bed unable to retain all owners
or their eligibility rejects the upgrade before removal and explains the conflict. An incompatible
crop choice is retained as unavailable until the player chooses a valid crop; it is not planted.

Physical stored items and unfinished work are not deleted, duplicated or granted extra capacity.
They remain where valid or are returned through normal physical placement before the old building
can be removed. Consumed fuel, battery charge, quality and arbitrary provider state are not copied
as free new supplies. New-building quality and HP follow ordinary completion and frame-damage rules;
this differs from full repair during a material change. Installing an already-built item instead
retains that item's actual HP/quality and obeys the configuration-conflict rule above.
An external building requiring unsupported
state transfer is unavailable for the owned upgrade, rather than offered with silent state loss.
Failure/cancellation before successful replacement leaves the old building and its choices intact.

#### Scenario: Upgrade a linked shelf into smaller compatible storage
- **WHEN** a supported linked storage upgrade completes
- **THEN** its link, filters and priority remain, its new physical capacity and restrictions apply, and excess contents remain physically recoverable without being counted as extra storage space

#### Scenario: Upgrade a linked workbench with unavailable recipes
- **WHEN** the new workbench cannot perform a retained bill
- **THEN** the bill's order, settings and committed progress remain, but new work on that row is blocked throughout the group until support is restored or the incompatible member is explicitly removed
- **AND** unaffected rows remain usable and any already committed physical work follows the shared production rules without granting a fresh iteration

#### Scenario: Ownership or provider transfer cannot be preserved
- **WHEN** an upgrade would lose a bed owner or unsupported external building state
- **THEN** the order is rejected before destroying the original, with an explanation of the unsupported transfer

### Requirement: Automatic rebuilding restores the destroyed building's supported configuration
An ordinary automatic rebuild SHALL remember supported configuration only for the rebuilding order created by that destruction.
Native automatic-rebuild enablement, player ownership, home-area, destruction, research and valid
placement conditions remain. Rebuilding still requires ordinary resources and work. Restore bills
and their owned policies, storage filters/priority and explicit linked membership, heater/cooler
target temperatures and supported growing-building crop choice. As already specified for quality
construction, an ordinary native rebuild after unrelated destruction inherits enablement and target
and starts a fresh **finite** quality run. A retry/recovery within an already initiated owned quality
cycle instead retains that cycle's spent budget; failure or reload does not replenish it.
This feature does not restore old fuel,
battery charge, old quality, destroyed contents or bed ownership through an otherwise unrelated
rebuild. Any unsupported external state is reported rather than silently described as restored.

Canceling the rebuild abandons that remembered restoration. Construction failure and save/load
retain it for the same rebuilding order. A manually placed building later occupying the same cell
does not inherit an abandoned building's bills, links or choices. Ordinary deliberate deconstruction
does not schedule an automatic rebuild through this feature.

#### Scenario: Automatic rebuild after destruction
- **WHEN** a configured eligible heater is destroyed and its automatic rebuilding order completes
- **THEN** the new heater has the remembered target temperature after consuming normal construction inputs and work

#### Scenario: Unrelated destruction differs from retry recovery
- **WHEN** combat destroys a quality-targeted building and native automatic rebuilding creates its new plan
- **THEN** the plan inherits the target and enablement with the ordinary fresh finite quality allowance
- **AND** a construction failure or reload within that rebuilding run preserves its already spent attempts instead of starting another run

#### Scenario: Canceled rebuild and unrelated later construction
- **WHEN** the player cancels an automatic workbench rebuild and later builds a different bench at that location
- **THEN** the later bench does not inherit the canceled restoration's bills, links or quality attempts

#### Scenario: Failure and reload
- **WHEN** a linked storage rebuild fails during construction and is saved and reloaded before successful completion
- **THEN** its surviving rebuild order retains the intended group and rules without recreating destroyed contents or restoring them twice

### Requirement: Replacement survives interruption and admits only supported integrations
Owned replacement SHALL preserve exact outstanding work, resources and configuration across save/load, cancellation and feature/provider changes, and leave unsupported actions unavailable with a bounded explanation.
A failed completion must leave either the intact old building with recoverable supplied inputs or
the completed new building with correctly transferred state; it must not lose both, duplicate
returns, repeat a quality roll on reload or apply state to another building. Source-mod save data
is not automatically imported. Activating an original Replace Stuff provider withholds overlapping
new owned actions and content offers; existing owned orders/devices remain recoverable without
replaying work through both providers. Other mods being present or modifying construction does not
alone disable compatibility. Unsupported required behavior produces a startup warning identifying
the skipped feature/patch and fallback; ordinary unaffected construction remains available.

#### Scenario: Save during material replacement
- **WHEN** the player reloads a save containing a partially completed replacement
- **THEN** the same original building, selected new material, earned work and actual supplied inputs remain, with no extra return or quality reroll before completion

#### Scenario: Original or incompatible provider becomes active
- **WHEN** startup detects an original overlapping provider or a changed construction behavior that cannot safely support an enabled owned patch
- **THEN** it warns about the unavailable owned behavior, offers no duplicate new action, and preserves existing owned work and contents for supported completion or cancellation recovery

#### Scenario: Compatible foreign construction changes
- **WHEN** another mod changes unrelated construction behavior while the required replacement behavior remains supported
- **THEN** owned replacement stays available and preserves that mod's applicable placement, cost and completion rules
