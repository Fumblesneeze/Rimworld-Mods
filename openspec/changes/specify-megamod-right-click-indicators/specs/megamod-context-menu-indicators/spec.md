# Right-click menu indicators

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S110: future observable contract, not implementation or visual acceptance.

## ADDED Requirements

### Requirement: Mark available right-click menus on ordinary commands

The feature SHALL add a small extra-options marker to supported ordinary command buttons when
right-clicking the current enabled button opens an options menu. Coverage SHALL include native
order commands, plan and zone visibility menus, allowed-area selectors, owned commands and admitted
modded commands using the ordinary command surface. An empty menu, a disabled command or a
right-click action that does not open a menu SHALL receive no added marker. A menu whose choices
are all disabled still SHALL count as a menu. Existing native markers SHALL remain under their
own rules, including native build/dropdown indicators; this feature SHALL not remove those markers.

#### Scenario: Ordinary order menu
- **WHEN** an enabled native order command has a right-click options menu
- **THEN** its button displays the extra-options marker before the player opens the menu
- **AND** right-click opens that ordinary menu with its original choices.

#### Scenario: No options or only a direct action
- **WHEN** right-clicking a supported enabled command opens no options menu
- **THEN** the feature adds no marker, even if right-click performs another action.

#### Scenario: Disabled command versus disabled choices
- **WHEN** the command itself is disabled
- **THEN** no owned marker advertises its unavailable right-click menu
- **AND** an enabled command opening a menu of disabled explanations still receives a marker.

#### Scenario: Allowed-area picker
- **WHEN** right-clicking an enabled Expand or Clear allowed-area command can open its area picker
- **THEN** it receives a marker, including when the picker offers only area management
- **AND** the picker remains subject to the normal interaction restrictions.

#### Scenario: Plans and zones
- **WHEN** a current plan, stockpile, growing-zone or available fishing-zone command opens its native visibility menu
- **THEN** the command is marked without changing which plans or zones are visible.

### Requirement: Follow the current button and selection

The added marker SHALL describe the actual displayed command and its current right-click route,
not the first command previously seen with the same appearance or kind. It SHALL update on the
next normal display refresh after relevant selection, availability, map, modifier-key or provider
state changes. A grouped selection SHALL be judged by the menu the displayed group command
actually opens, not by an unrelated menu on another selected item. Loading or switching games
SHALL not carry another context's answer forward.

#### Scenario: Similar commands have different menus
- **WHEN** two instances of the same command kind differ in whether right-click opens a menu
- **THEN** each displayed button is marked according to its own current behavior, regardless of which was viewed first.

#### Scenario: A command changes while visible
- **WHEN** a supported command gains or loses its menu, or becomes disabled or enabled
- **THEN** the added marker appears or disappears at the next normal display refresh without requiring a restart or reselection.

#### Scenario: Modifier-gated provider menu
- **WHEN** an admitted provider shows its menu only while a configured modifier combination is held
- **THEN** the marker tracks that condition as keys are pressed and released
- **AND** it does not claim the menu is always present after the first successful use.

#### Scenario: Grouped menu routing
- **WHEN** the displayed group command opens a combined options menu for the selection
- **THEN** one marker describes that actual menu and selecting an option retains the ordinary group behavior
- **AND** another selected item's menu alone does not produce a marker when the displayed command's right-click route opens no menu.

#### Scenario: Map or game changes
- **WHEN** the player changes maps, loads a save or starts another game
- **THEN** newly displayed markers reflect the new context and do not retain the previous selection's availability.

### Requirement: Discoverability must remain passive

Showing or hovering a command and its marker SHALL not open or close menus, select an order,
change a zone/filter/plan, issue a job, spend resources, make a sound or create a message on behalf
of the indicator. It MUST NOT suppress a legitimate message, archive entry or sound merely to hide
an inspection side effect. The marker SHALL have no separate action or input interception; left
click, right click, hotkeys, tooltips and menu actions SHALL keep their existing behavior. If added
information cannot be determined safely for a supported integration, the affected marker SHALL
be unavailable rather than make a speculative change to the game.

#### Scenario: Repeated display and hover
- **WHEN** the player repeatedly views or hovers commands with menus, including owned quick-storage commands
- **THEN** the indicator creates no menu, order, filter edit, sound or warning message
- **AND** it does not change the selection or the result of a later intentional click.

#### Scenario: Genuine rejection remains visible
- **WHEN** the player deliberately uses a command that produces a normal rejection or information message
- **THEN** the original message, applicable archive entry and sound remain available under their usual rules
- **AND** the indicator does not mute them.

#### Scenario: Actual menu action
- **WHEN** the player right-clicks a marked command and selects an option
- **THEN** the original action runs with its normal targets, permissions, costs and group behavior
- **AND** viewing the marker has neither pre-executed that action nor caused it to run twice.

#### Scenario: Unsafe information provider
- **WHEN** a menu provider cannot supply safe current availability for the indicator
- **THEN** only the affected added marker is omitted with the applicable bounded compatibility diagnostic
- **AND** the native/provider button and actual input path remain available under their original rules.

### Requirement: Fit the existing command presentation

Added markers SHALL use the native extra-options visual language, remain inside their command
button and be distinguishable at ordinary and compact command sizes. Use the native top-right
placement where free; when a label or counter occupies that space, place the marker in a clear
corner inside the same button without covering existing text. Existing equivalent indicators
SHALL not receive another owned copy. Markers SHALL preserve command tint, highlights, labels,
tooltips and neighboring controls, including after a drawing failure. They SHALL require no new
bottom tab, bottom-right toggle, debug button, configuration page or click target. All seven
baseline language layouts SHALL remain usable without untranslated added text.

#### Scenario: Normal and compact commands
- **WHEN** a supported command is drawn at normal or compact size or with a supported UI scale
- **THEN** its marker remains readable within that button and does not obscure its action, label or adjacent command.

#### Scenario: Existing native marker
- **WHEN** a build-material or dropdown button already displays its native extra-options indicator
- **THEN** this feature preserves that indicator and adds no second equivalent marker
- **AND** the native button retains its original material/dropdown behavior and availability rules.

#### Scenario: Counter and translated label
- **WHEN** a supported command has a top-right counter, a long translated label or a highlight
- **THEN** the marker uses clear space in the same button and leaves that information legible
- **AND** the native tooltip and command hit area remain unchanged.

#### Scenario: Compatible UI appearance change
- **WHEN** an admitted UI appearance change alters the native extra-options marker
- **THEN** the owned indicator follows the resolved native appearance without copying a separate incompatible image
- **AND** other command colors and highlights remain intact.

### Requirement: Compose with actual providers without inventing settings

The feature SHALL be active without configuration when its target conditions hold. It SHALL add
no saved gameplay state or global/save settings. Owned commands SHALL expose their current menu
capability directly as part of their own behavior. A genuine optional Hide/Restore menu SHALL count
as a menu; no provider-specific blacklist is selected by this change. Entirely custom-drawn gizmos
outside the ordinary command surface, including the native mechanitor control-group display, are
outside this replacement's coverage unless separately specified. That omission SHALL be documented
without claiming their menus are absent. If the original indicator mod is active, it SHALL own the
overlap and the owned indicator SHALL remain inactive with a bounded startup explanation.

#### Scenario: Owned quick-storage and order commands
- **WHEN** an owned command offers its specified right-click choices
- **THEN** it uses the same current indicator policy without a second prompt or duplicate marker
- **AND** its storage, order or other gameplay rules remain those of its owning feature.

#### Scenario: Optional Hide or Restore menu
- **WHEN** an admitted optional provider adds an actual Hide or Restore menu to an otherwise ordinary command
- **THEN** the menu is indicated under the same availability rules, even if this marks many buttons
- **AND** the feature does not hide or disable that provider's menu.

#### Scenario: Custom-drawn display
- **WHEN** a mechanitor control-group display or an unadmitted custom UI uses its own drawing surface
- **THEN** the feature leaves that surface and its original interactions unchanged
- **AND** its missing owned marker is documented as unsupported presentation, not as evidence that no menu exists.

#### Scenario: Original provider present
- **WHEN** CheaterEater.RightClickGizmoIndicator is active
- **THEN** the owned overlapping indicator stays inactive with a startup explanation
- **AND** it does not remove, unpatch or duplicate the original provider.

### Requirement: Admit compatible behavior and isolate failures

Each enabled patch SHALL check its documented minimal semantic prerequisites before application.
Unrelated code changes and compatible foreign patches SHALL be permitted. Missing, changed or
ambiguous required behavior, validation exceptions and installation failures SHALL disable only
the affected patch or declared atomic group. Each skipped enabled patch SHALL produce one bounded
startup warning identifying package, patch/spec, target, failed condition, unavailable indication
and original-button fallback. Repeated registration MUST NOT duplicate drawing or warnings. Later
supported changes SHALL invalidate affected availability until revalidation without removing
foreign behavior. Drawing or provider failures MUST NOT prevent unrelated commands from rendering
or leak altered visual/input state into another control; runtime diagnostics SHALL remain bounded.

#### Scenario: Compatible foreign modification
- **WHEN** a foreign change preserves the required current-menu and drawing behavior
- **THEN** the indicator can coexist with it and preserves its menu choices and presentation.

#### Scenario: Required assumption fails
- **WHEN** an enabled target no longer satisfies a required condition or checking/installation fails
- **THEN** the affected indication remains inactive and its bounded startup warning names the lost enhancement and fallback
- **AND** the original command and unrelated compatible indicators remain intact.

#### Scenario: Repeated startup or later invalidation
- **WHEN** registration repeats or a supported later modification changes an admitted target
- **THEN** there is no duplicate marker or warning, and affected indication waits for revalidation
- **AND** foreign patches, active menus and original command behavior are not removed or replayed.

#### Scenario: One marker fails to draw
- **WHEN** an added marker or its availability information fails at runtime
- **THEN** the failure does not block unrelated buttons or change their color, input or tooltip behavior
- **AND** it does not produce a warning or exception on every subsequent frame.
