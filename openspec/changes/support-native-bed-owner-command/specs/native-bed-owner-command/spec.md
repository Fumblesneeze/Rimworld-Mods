## ADDED Requirements

### Requirement: Invoke the original bed-owner command faithfully
The RimWorld Dev Gateway SHALL expose the exact native Command_SetBedOwnerType through its existing typed gizmo invocation and SHALL retain the exact newly opened native menu for minimized automation. It SHALL NOT generalize this adapter to arbitrary custom command subclasses.

#### Scenario: Open and use the vanilla owner menu
- **WHEN** a caller invokes a current enabled original bed-owner command for a selected bed
- **THEN** RimWorld opens its native colonist/prisoner/slave menu and choosing prisoners through the leased native menu changes the bed according to the game's room and ownership rules

#### Scenario: Preserve command safety boundaries
- **WHEN** a command is disabled, stale, or a custom unsupported subtype
- **THEN** the existing registry checks reject invocation and no fabricated input event or direct bed-state mutation is used
