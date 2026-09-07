## ADDED Requirements

**Owning mod:** Guest Bed Gizmo (`fumblesneeze.guestbedgizmo`) at `mods/GuestBedGizmo`.

### Requirement: Hospitality and Ideology activation is optional and shape guarded

Guest Bed Gizmo SHALL activate its integration only when canonical package IDs `Ludeon.RimWorld.Ideology` and `Orion.Hospitality` are in the real active mod list and the supported loaded assembly, guest-bed type, swap method, legacy-toggle identity, translation, icon, and gizmo patch surfaces all match the inspected RimWorld 1.6 contract. Compatibility MUST depend on those consumed interfaces and assets, never on an allowlisted module version ID, file hash, or assembly version. Binary fingerprints MAY be retained as diagnostic evidence only. It MUST have no compile-time Hospitality reference and MUST NOT install its bed-gizmo Harmony patch when either prerequisite is absent or Hospitality is incompatible.

#### Scenario: Hospitality is absent
- **WHEN** Harmony, Core, Ideology, and Guest Bed Gizmo load without `Orion.Hospitality`
- **THEN** ordinary beds retain vanilla's three-choice owner command and Guest Bed Gizmo installs no bed-gizmo patch or Hospitality-specific behavior

#### Scenario: Ideology is absent
- **WHEN** Hospitality and Guest Bed Gizmo load without `Ludeon.RimWorld.Ideology`
- **THEN** Hospitality's legacy guest-bed control remains untouched and Guest Bed Gizmo installs no bed-gizmo patch

#### Scenario: Supported Hospitality is active
- **WHEN** Ideology and a Hospitality Continued 1.6 package with compatible bed interfaces are active before Guest Bed Gizmo
- **THEN** Guest Bed Gizmo validates one supported adapter and installs its `Building_Bed.GetGizmos` patch exactly once after Hospitality

#### Scenario: Hospitality is rebuilt without changing the consumed bed interfaces
- **WHEN** Hospitality has a different module version ID, file hash, or assembly version but preserves all consumed bed interfaces and assets
- **THEN** the adapter remains eligible and the native owner command still offers guest conversion without adding the new binary to an allowlist

#### Scenario: Hospitality removes its legacy guest toggle or bed-swap interface
- **WHEN** an upstream change removes a consumed legacy-toggle or swap interface, including replacement of the old bed UI that makes this integration obsolete
- **THEN** Guest Bed Gizmo leaves the upstream bed controls intact, installs no partial integration, and reports the specific missing interface once

#### Scenario: Hospitality shape changed
- **WHEN** `Orion.Hospitality` is active but any required type, method, action identity, translation, icon, or return shape differs
- **THEN** Guest Bed Gizmo installs no partial integration, leaves Hospitality behavior untouched, and emits one bounded actionable warning

### Requirement: One unified bed-owner command includes guests

While the supported Hospitality adapter is active, every eligible player-controlled humanlike bed SHALL expose one native-style bed-owner command. Activating it MUST open one ordinary float menu containing exactly `For colonists`, `For prisoners`, `For slaves`, and `For guests` in that order, using the existing vanilla and Hospitality localized labels and icons.

#### Scenario: Player opens owner menu on a vanilla bed
- **WHEN** the player selects an eligible ordinary bed and activates its bed-owner command
- **THEN** one float menu visibly presents the four ownership choices in the specified order

#### Scenario: Player opens owner menu on a guest bed
- **WHEN** the player selects an existing Hospitality guest bed and activates its enabled `For guests` owner command
- **THEN** the same four-choice menu opens from that command

### Requirement: Hospitality legacy toggle is removed without collateral gizmo loss

While the adapter is active, Guest Bed Gizmo SHALL remove only Hospitality's exact standalone `For guests` toggle from ordinary-bed gizmos. It MUST retain vanilla bed actions and Hospitality's guest-bed rental, attractiveness, copying, and other unrelated gizmos.

#### Scenario: Ordinary bed gizmos are enumerated
- **WHEN** the player selects an eligible ordinary bed with Hospitality and Guest Bed Gizmo active
- **THEN** the gizmo bar contains the unified owner command and no separate Hospitality guest toggle

#### Scenario: Guest bed gizmos are enumerated
- **WHEN** the player selects a Hospitality guest bed
- **THEN** its unified owner command is enabled and labeled `For guests`, its rental/attractiveness controls remain available, and no separate guest toggle appears

#### Scenario: A bed has no vanilla owner command
- **WHEN** Hospitality contributes its legacy guest toggle to a bed for which vanilla emits no `Command_SetBedOwnerType`, including a human-baby crib
- **THEN** Guest Bed Gizmo preserves the legacy toggle and does not remove guest control without a unified replacement

### Requirement: Guest and vanilla ownership conversions use native behavior

Choosing `For guests` SHALL invoke Hospitality's validated bed-swap operation for each selected owner-command-eligible bed after preflighting every required replacement. Choosing colonist, prisoner, or slave SHALL invoke RimWorld's public `SetBedOwnerTypeByInterface` behavior. Guest-to-vanilla swaps MUST be deferred into RimWorld's resulting owner-assignment commit, after any native confirmation is accepted, and MUST cover every guest bed in RimWorld's complete affected-bed closure. Cancelling a confirmation or failing any preflight MUST leave every bed unchanged. Conversion MUST preserve every upstream-supported building property, including Stuff, hit points, quality, art, paint, style, position, rotation, and faction.

#### Scenario: Player converts a vanilla bed to a guest bed
- **WHEN** the player selects a constructed vanilla bed, opens the unified owner command, and chooses `For guests`
- **THEN** the bed is replaced by Hospitality's corresponding guest-bed Thing at the same location and visibly exposes the enabled `For guests` unified command

#### Scenario: Player converts a guest bed to a prisoner bed
- **WHEN** the player opens that guest bed's unified owner command and chooses `For prisoners`
- **THEN** Hospitality restores the corresponding vanilla bed and RimWorld visibly marks the replacement for prisoners through its native owner-type path

#### Scenario: Prisoner room preflight rejects the action
- **WHEN** the player chooses `For prisoners` on a guest bed whose room fails vanilla `RoomCanBePrisonCell`
- **THEN** RimWorld shows its native `CommandBedSetForPrisonersFailOutdoors` rejection and Guest Bed Gizmo performs no swap or owner-type change

#### Scenario: Player cancels an owner-removal warning
- **WHEN** a vanilla owner-type choice opens RimWorld's owner-removal confirmation and the player cancels it
- **THEN** no selected or room-propagated guest bed is swapped and no owner type changes

#### Scenario: Prisoner ownership expands to an unselected guest bed
- **WHEN** the player chooses `For prisoners` on one selected guest bed in a proper room containing another unselected guest bed
- **THEN** both beds in RimWorld's affected-bed closure become corresponding vanilla prisoner beds and the unselected bed does not become a guest/prisoner hybrid

#### Scenario: Player converts mixed selected beds
- **WHEN** the player selects multiple eligible ordinary and guest beds and chooses one owner type from the unified command
- **THEN** each selected bed converts once, every replacement remains selected, and no duplicate or orphan building remains

#### Scenario: Mixed selection contains unsupported beds
- **WHEN** a unified owner command is invoked while animal, human-baby, foreign-faction, or otherwise owner-command-ineligible beds are also selected
- **THEN** Guest Bed Gizmo excludes those unsupported selections from its conversion set and preflights every supported replacement before the first swap

#### Scenario: Conversion result is ambiguous
- **WHEN** an upstream swap does not produce exactly one corresponding replacement at the source bed's map root cell
- **THEN** Guest Bed Gizmo stops that action with a bounded diagnostic and does not guess or apply an owner type to another Thing

### Requirement: Live acceptance proves the native player workflow

The reviewed Release package SHALL be accepted only after the acting agent uses the actual bed-owner command in a fresh isolated RimWorld process and personally observes the menu and both conversion directions. Supporting host tests, patch inspection, Def inspection, logs, API state, or direct mutation MUST NOT replace that workflow.

#### Scenario: Final present-Hospitality acceptance
- **WHEN** the acting agent selects a real bed, opens the unified command, captures the four-choice menu, chooses guests, observes the guest replacement, then chooses a vanilla owner type on the reviewed build
- **THEN** retained exact-process before/menu/guest/vanilla screenshots causally show one unified command, both conversions, preserved related gizmos, and no relevant warnings or errors

#### Scenario: Final absence acceptance
- **WHEN** the reviewed product loads in a separate fresh process without Hospitality, with Dev Gateway available for native automation, and a player selects an ordinary bed
- **THEN** the observed bed UI remains vanilla, the process log is clean of product errors, configuration hashes match, and exact-PID cleanup completes
