# Wall temperature devices

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Documentation-only S028 contract; no asset or gameplay acceptance.

## ADDED Requirements

### Requirement: Wall devices coexist with walls without free demolition
Players SHALL be able to construct ordinary one-cell and wide one-by-two-cell wall coolers and vents in the appropriate native build menus, including over compatible walls.
The wall and device remain distinct physical buildings; placement must not delete the wall, its
material, compatible attachments or an unrelated plan. Walls can also be constructed/replaced
beneath a supported device where physically valid. The wide footprint spans a two-cell-thick wall
in its facing direction, rather than becoming a two-unit appliance. Freestanding placement remains
allowed, consistent with the inspected source; it does not grant a cooler the sealing properties
of a wall. Rotation and placement previews show the actual footprint and exterior operating sides.

#### Scenario: Cooler on a retained wall
- **WHEN** the player constructs an eligible wall cooler over a wall
- **THEN** both wall and cooler remain, the wall continues providing its normal enclosure and the cooler's operating sides match its preview

#### Scenario: Wide device through a thick wall
- **WHEN** the player rotates and constructs a wide device through two compatible wall cells
- **THEN** its operating sides are beyond the two ends of that thickness and no middle wall is silently removed

### Requirement: Thermal behavior retains native limitations
Wall devices SHALL use ordinary cooler temperature/power behavior and vent opening/closing behavior, with their correct exterior operating sides.
A wide cooler moves heat from its cold side to the hot side, with the same output, power demand,
temperature target and efficiency rules as its ordinary counterpart. A vent equalizes temperature
through its footprint while open and stops that exchange when closed, with applicable native gas
and vacuum behavior. A cooler itself is not an airtight wall or vacuum barrier. Vent sealing and
gas/vacuum effects follow the native vent, including any supported enclosing walls. Blocked-port
placement is allowed for ordinary and wall coolers/vents, but blockage still prevents operation
where native rules require a clear side; previews or inspection explain that condition. Placement
permission does not imply successful cooling or a new gas/vacuum mechanic.

#### Scenario: Cooler has blocked exhaust
- **WHEN** the player builds a cooler with an obstructed operating side
- **THEN** placement is possible, blockage is visible in feedback and the cooler does not bypass the ordinary thermal limitation

#### Scenario: Open and close a wide vent
- **WHEN** the player opens and then closes a wide vent separating two rooms
- **THEN** temperature exchange occurs through the correct exterior sides while open and stops through that vent when closed, without doubling its output

### Requirement: Devices retain ordinary construction economy
Wall-device construction SHALL inherit the corresponding ordinary cooler or vent's effective costs, research, skill, work, HP and operational output rather than gaining free construction or a wide-device multiplier.
Current Core anchors are cooler **90 steel, 3 components, 1600 work, HP100, Construction5,
Air conditioning research, 200W** and vent **30 steel, 400 work, HP100, Complex furniture research,
no electricity**. Both widths use their corresponding ordinary device's values. Applicable native
or supported mod changes to those base values carry through; costs/output are not frozen to an
obsolete beta snapshot. The building's actual full inputs must be supplied. Wide reach changes the
placement niche without granting doubled cooling, free research or a special resource return.

#### Scenario: Wide cooler resource cost
- **WHEN** a wide cooler is built under the inspected Core values
- **THEN** it consumes 90 steel and 3 components with 1600 work and the ordinary research/skill requirements, operating at the same 200W and cooling output as the one-cell cooler

#### Scenario: Supported base-cost change
- **WHEN** another supported mod changes ordinary cooler construction costs
- **THEN** both wall-cooler widths show and require the corresponding changed costs, without duplicating a cost entry

### Requirement: Visibility preferences affect offers and preserve other menus
Settings SHALL separately allow hiding wall coolers/vents and hiding ordinary coolers/vents, both false by default, through the common global/default and save-override controls.
Changes apply to build offers when the menu refreshes, not to existing buildings, plans or temperature
controls. Main-menu save editors and Use global are inactive; global editors remain usable. Build
menus retain unrelated categories, dropdown order and third-party entries. Where a supported
material-choice interface needs individual device offers, only the affected wall-device group is
expanded. Related commands are added without duplicates. No bottom tab is introduced; normal
temperature/flick choices remain contextual building state. Instant replacement/construction remains
restricted to ordinary developer/god-mode access, never an ordinary settings or bottom-right cheat.

#### Scenario: Hide a family with buildings already present
- **WHEN** the player hides wall devices in the current save
- **THEN** new offers for that family disappear while existing devices and plans remain usable and unrelated build groups retain their organization

#### Scenario: Shared material submenu
- **WHEN** a supported provider presents material choices inside a building dropdown
- **THEN** wall-device choices remain usable without unpacking unrelated dropdowns or changing their order

### Requirement: Missing content and original providers do not cause destructive partial content
Wall-device and related-command additions SHALL preserve other content and activate only when their required base content and behavior are supported.
Absent optional content produces no broken offer. A missing required shape leaves that affected
addition unavailable with a bounded startup warning and preserves existing unrelated lists and
buildings. An active original provider owns new overlapping offers; the owned version does not add
duplicate definitions or replacement actions. Existing owned devices remain identifiable and
recoverable, rather than being silently converted or deleted. Original-provider saves are not
automatically migrated and removing the owned mod while devices remain is not claimed safe.

#### Scenario: Provider changes a required device definition
- **WHEN** a required base device or related-command target is absent or incompatible at startup
- **THEN** the affected addition is skipped with a warning, and unrelated definitions and command-list entries remain intact

#### Scenario: Original provider co-installed
- **WHEN** the original Replace Stuff provider supplies the same device family
- **THEN** there is one selected provider for new offers, no duplicate source identity is introduced, and already-owned devices are preserved for supported use or removal
