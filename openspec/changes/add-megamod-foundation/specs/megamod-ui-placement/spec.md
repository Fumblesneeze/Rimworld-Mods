## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). This change specifies
UI placement and audit rules only; the scaffold has no settings, toolbar controls, or bottom tabs.

## ADDED Requirements

### Requirement: Audit upstream UI and apply megamod placement rules
Every source analysis SHALL inventory bottom-tab additions and other entry points, their actual
purpose, and injected/dynamic as well as XML-defined controls. Megamod's classification SHALL take
precedence over upstream placement. Mixed-purpose windows SHALL be split/classified by function;
wrapping settings, an override, or a toggle in a window does not justify a bottom tab. Record chosen
placement, rationale, relocation and player navigation changes in the source report. This audit SHALL
NOT require preserving source APIs, checking their consumers or providing a compatibility facade.

#### Scenario: A source mod adds a bottom tab
- **WHEN** its behavior is analyzed for replacement
- **THEN** its actual commands/configuration/mechanics are classified before UI implementation
- **AND** its MainButtonDef or equivalent injected tab is not blindly copied

### Requirement: Put debug and cheat overrides in the top developer toolbar
Functionality intended for debugging or unrestricted cheats/overrides SHALL be exposed in the top
debug toolbar alongside native developer controls and only while developer mode is enabled.
Classification SHALL depend on bypassing sensible game mechanics, not whether an override is
cosmetic or presented as a window. It MUST NOT occupy an ordinary bottom gameplay tab.

#### Scenario: A character editor overrides appearance
- **WHEN** a replacement can directly change a character outside normal game mechanics
- **THEN** its entry point is a top debug control in developer mode
- **AND** disabling developer mode hides that entry point and no bottom gameplay tab remains

### Requirement: Entity-specific choices are gameplay state rather than settings
Choices specific to a pawn, area/zone, bill, building, or other gameplay entity SHALL be classified
as gameplay state, not mod configuration. Persisting them in a save MUST NOT move them into the mod
settings menu or global-default/per-save configuration model. Use appropriate native contextual
controls or a gameplay management window. Unrestricted overrides still follow the debug/cheat rule.

#### Scenario: Assign a policy to a particular pawn or area
- **WHEN** a player changes a choice attached to that entity
- **THEN** the interaction uses its contextual gameplay UI or the relevant mechanic's management window
- **AND** it is not presented as a per-save mod setting or an inherited global configuration row

### Requirement: Put configuration in structured mod settings with explicit scope
All configuration SHALL reside in the mod settings menu, including save-specific configuration.
The menu SHALL use a documented stable feature/category order, clear section boundaries, meaningful
localized headings, and explicit Global default versus Current save labels. Each setting SHALL
specify persistence, defaults, effect timing, and interactions. Save overrides MUST NOT modify global
defaults or another save; inherited settings intentionally resolve the current global default.

#### Scenario: Configure a feature with global and save options
- **WHEN** the player opens the mod settings menu during a loaded game
- **THEN** the feature's options appear in the specified section/order with clearly distinguished scopes
- **AND** changing a current-save option affects only that save while global changes follow their documented global semantics

### Requirement: Global defaults with per-save inheritance are the standard model
Every setting SHALL provide an editable global default and an optional per-save override unless a
documented reason makes this impossible or nonsensical. New/missing per-save entries SHALL default
to Use global. Effective resolution SHALL select the current global value while inheriting and the
save's override otherwise, with the declared built-in default if no global value has been stored.
Changing the global default SHALL affect inheriting saves when resolved, not existing overrides;
the feature SHALL specify when a resolved change takes effect in gameplay.

#### Scenario: Global default changes with two different save policies
- **WHEN** one save uses global and another has an override, and the player changes the global default
- **THEN** the first save resolves the new global value and the second continues resolving its override
- **AND** a new save or newly introduced setting inherits the global default without a copied stale value

#### Scenario: Switch inheritance without losing an override
- **WHEN** the player checks Use global for a setting with an existing save override
- **THEN** the effective value switches to global while the dormant override is retained
- **AND** unchecking Use global restores that override; if no override has ever existed, it starts from the current effective global value

### Requirement: One shared loading storage resolution and rendering path
All features SHALL use the mod's common settings loading, effective-value resolution, storage, and UI
rendering path instead of independent per-feature implementations. Stable setting identities, typed
values/defaults, validation, scope/exemption metadata, and presentation order SHALL describe each
setting. Store global defaults in global mod configuration and each save's inheritance choice and
override in that save. Reload and save-switch behavior SHALL preserve those distinctions.

#### Scenario: Persist inherited and overridden values
- **WHEN** values and Use global choices are saved and the game is reloaded or switched
- **THEN** the common loader restores each save's own choices and overrides and resolves against the current global defaults
- **AND** no feature-specific loader or UI silently applies a different inheritance policy

### Requirement: Original-provider effective-off handling preserves stored choices
Startup detection of an active original provider SHALL make a best-effort attempt to disable the
corresponding owned features with the same behavior and timing as their ordinary manual off choice.
This effective-off handling MUST NOT rewrite stored global defaults, per-save overrides or Use global
choices. Its startup warning SHALL identify the affected features without promising full coexistence
compatibility or rollback beyond the ordinary off behavior. This rule does not waive game-target
admission or safeguards for external integrations actually used.

#### Scenario: Retain a preference while an original is active
- **WHEN** a feature has a stored enabled preference and an active original triggers startup handling
- **THEN** its best-effort effective-off behavior matches the ordinary manual off choice
- **AND** saving settings preserves the stored global value, save override and inheritance choice unless the player explicitly edits them

#### Scenario: Resume ordinary settings after the original becomes inactive
- **WHEN** a later startup has no active original for that feature
- **THEN** the retained global/save choices resolve normally, subject to target admission
- **AND** the earlier automatic handling has not permanently changed those choices

#### Scenario: Edit a retained preference while automatic disablement applies
- **WHEN** the player opens settings for a feature automatically disabled by an active original
- **THEN** its preference controls retain their ordinary scope and show the stored or inherited requested choice with an explanation that the feature is effectively off
- **AND** explicitly editing that preference changes only the requested choice; it does not bypass the startup disablement
- **AND** switching inheritance without a previous override seeds from the requested global default rather than the automatically disabled value

### Requirement: Render the same three-part setting row
For every non-exempt setting the common renderer SHALL display a localized Use global checkbox,
Current save control, and Global default control with clear labels. The per-save control SHALL be
inactive while Use global is checked and always inactive at the main menu. Use global itself SHALL
be inactive without an active save, while the global-default control remains editable. Disabled
callbacks SHALL enforce the same policy as the visual controls.

#### Scenario: Edit a row in a loaded save
- **WHEN** Use global is checked
- **THEN** the row displays all three controls, disables the per-save editor and shows the effective inherited value clearly
- **AND** unchecking Use global enables the per-save editor without disabling the global-default editor

#### Scenario: Render a row at the main menu
- **WHEN** the settings menu is opened without an active save
- **THEN** the Use global checkbox and per-save editor are inactive, the row explains that save scope is unavailable, and the global-default editor stays active
- **AND** no stale save values are presented as an active override

### Requirement: Settings inheritance exceptions require a reason
A setting that cannot sensibly support the standard inheritance model SHALL document its concrete
reason, actual scope, persistence, and UI behavior in its feature spec and definition metadata. It
SHALL still use the common settings path, clearly label unavailable scopes, and honor the main-menu
restriction for save-scoped values. Convenience is not a sufficient exemption.

#### Scenario: A feature needs a save-only value
- **WHEN** its spec explains why a global default cannot meaningfully represent that value
- **THEN** the common renderer presents its documented exception without a misleading functional Use global control
- **AND** the save-only editor remains disabled at the main menu and the exception is testable

### Requirement: Save-specific settings are disabled without an active game
When the mod settings menu is opened from the main menu, all current-save controls SHALL be visibly
disabled with a localized reason. Their callbacks, reset/import actions, and other mutation paths
MUST also reject save-scoped writes. The UI MUST NOT show stale last-save values as active settings,
create an implicit save, or mutate a previous game. Global controls SHALL remain usable.

#### Scenario: Open settings from the main menu
- **WHEN** no game is loaded and the player opens megamod settings
- **THEN** global settings are editable and all current-save controls/actions are disabled with an explanation
- **AND** no previous save or global value is changed by attempting a save-specific action

#### Scenario: Switch saves or open settings in world view
- **WHEN** the player loads a different save or opens settings in a loaded game's world view
- **THEN** the controls bind to the active save and enable according to the feature contract, without depending on a selected map or retaining the previous save's state

#### Scenario: Save-context observation becomes unavailable
- **WHEN** the mod cannot reliably observe the current save after a lifecycle callback fails
- **THEN** current-save settings and dependent save-scoped behavior become unavailable
- **AND** stored overrides are retained, global configuration remains available, and one diagnostic explains the failure

### Requirement: Reserve bottom tabs for gameplay control windows

A new bottom-bar tab SHALL be used only for a window that adds an ordinary game mechanic for the
player to control. Debug/cheat functions, configuration, and simple toggles MUST use their assigned
surfaces instead. Each qualifying feature SHALL specify the native entry point, window actions,
eligibility, and observable gameplay outcome before creating a tab.

#### Scenario: Review an ordinary gameplay control window
- **WHEN** a window lets the player manage a specified game mechanic through normal gameplay rules
- **THEN** a bottom tab is eligible and its native control workflow is verified
- **AND** unrelated settings/overrides are separated into their required surfaces

### Requirement: Put simple view and behavior toggles at bottom right
A control whose sole action is enabling/disabling a current-play view or behavior SHALL use a small
bottom-right control, analogous to vanilla automatic rebuilding, rather than a bottom tab. Debug
overrides still belong in the debug toolbar. Configuration of defaults/parameters belongs in mod
settings; persistence of an operational toggle alone does not change its control classification.

#### Scenario: Toggle a view or automatic behavior
- **WHEN** the player uses a feature that only turns a view/behavior on or off
- **THEN** a bottom-right control shows its current state and toggles the specified behavior directly
- **AND** the feature does not add a bottom tab or require opening a configuration window just to toggle it
