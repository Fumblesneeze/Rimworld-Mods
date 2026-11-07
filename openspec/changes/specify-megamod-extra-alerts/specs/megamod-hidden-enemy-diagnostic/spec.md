# Hidden-enemy diagnostic

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S084 specification only. This is explicitly a developer information override, not ordinary play.

## ADDED Requirements

### Requirement: Hidden information is available only through explicit developer controls
The hidden-enemy diagnostic SHALL be accessible from the **top debug toolbar** only while developer
mode is enabled and a game is loaded. It is not a normal alert, bottom tab, bottom-right play toggle
or ordinary mod-settings preference. Merely enabling developer mode does not open it or announce
hidden enemies. The player explicitly opens/refreshes the diagnostic to request this information.

Its open state is transient to the current game session, initially closed. Disabling developer mode,
leaving the game or switching/loading another save closes and clears it, including callbacks and
hover text. It does not reappear automatically. This operational diagnostic state is not a global/
per-save configuration setting; no persistent hidden-enemy preference is added.

#### Scenario: Normal play
- **WHEN** developer mode is off and hidden hostiles are present
- **THEN** no owned control, count, name, notification or cached tooltip reveals them

#### Scenario: Explicit developer request
- **WHEN** developer mode is enabled and the player opens the top-toolbar diagnostic
- **THEN** the requested hidden-enemy information appears without changing ordinary alert preferences

#### Scenario: Disable or switch save
- **WHEN** developer mode is disabled or another save loads while the diagnostic is open
- **THEN** its view and cached information disappear and reopening requires a fresh explicit request

### Requirement: The diagnostic reports presence without changing the world
The diagnostic SHALL list current living, spawned, non-downed pawns hostile to the player that are
fogged or otherwise hidden from the player, grouped by loaded map with distinct counts and names.
State plainly that this exposes normally hidden information. A pawn becoming visible, downed, dead
or absent leaves the next refreshed diagnostic. Visible enemies remain governed by ordinary alerts.

It performs no reveal/unfog, targeting, selection of hidden pawns, damage, orders or world mutation.
Do not promise native click-to-locate for hidden targets. Missing/changed provider visibility semantics
make only that unsupported diagnostic portion unavailable with an explanation; no guessed locations.
When original Extra Alerts is active, it owns the overlapping hidden-information feature and the
owned diagnostic is unavailable with a provider explanation, without changing the original's controls.
All added debug text is localized in the seven baseline languages.

#### Scenario: Hidden hostile becomes visible
- **WHEN** a hidden hostile emerges normally while the diagnostic is open
- **THEN** the next explicit refresh removes it from the hidden list and ordinary enemy reporting follows its own settings

#### Scenario: Presence without location override
- **WHEN** the player reads the diagnostic's hidden-enemy list
- **THEN** the listed enemies remain hidden in the world and no hidden target is selected or ordered against
