# Owned settings search

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S141, Needle-inspired selected subset. Specification only; no runtime acceptance.

## ADDED Requirements

### Requirement: SS-01 Search covers owned setting names and context
The common settings page SHALL provide search across every registered owned setting's localized name, description/help, feature name and category name, independently of the currently visible section or scroll position.
Meaningful options within composite settings retain a route to their actual owning editor. Disabled
or temporarily unavailable registered settings remain discoverable with their reason; search never
enables a feature. Decorative text is not presented as an editable setting. S047 remains responsible
for other mods' page-name/author search; their individual opaque controls are outside this capability.

#### Scenario: The matching word exists only in help
- **WHEN** the player searches for a term present in an owned setting's description but not its name
- **THEN** the setting is found with its name and feature/category path, including outside the current section.

#### Scenario: Feature and category locate their settings
- **WHEN** the player searches a feature or category name
- **THEN** its matching registered settings appear without creating fake source-mod pages.

#### Scenario: A feature is unavailable
- **WHEN** a matching setting belongs to a disabled feature or lacks a required current capability
- **THEN** the result explains that state and remains discoverable without turning the feature on.

### Requirement: SS-02 Matching and clearing are predictable
Search SHALL use case-insensitive substring matching over readable localized text, treating whitespace-separated terms as AND conditions and a quoted phrase as one contiguous term.
Terms can match different fields of the same setting. Repeated whitespace is ignored; rich-text
formatting does not become search content. An unmatched opening quote treats the remaining text as
one phrase. Empty input restores ordinary category browsing. Name matches rank before matches only
in help or path; ties preserve common category/row order and stable identity. Search has no hidden
result cap. The first implementation has no fuzzy or field-operator syntax.

#### Scenario: Terms span label and description
- **WHEN** one query term matches a setting name and another matches that same setting's help
- **THEN** the setting matches, while a setting missing either term does not.

#### Scenario: Literal phrase and non-Latin text
- **WHEN** the player searches a quoted phrase or an exact Chinese, Japanese or Cyrillic substring
- **THEN** the corresponding localized text matches without requiring English or splitting it into Latin words.

#### Scenario: Empty results and clear
- **WHEN** no setting matches and the player clears the query
- **THEN** an explicit no-results state is replaced by ordinary complete category browsing without any setting change.

### Requirement: SS-03 Results reveal the real control without editing it
Selecting a result SHALL reveal its current containing section, scroll the actual setting into view and highlight its row for three real elapsed seconds after reveal without changing a value, inheritance choice or feature activation.
Duplicate names remain distinct through feature/category context. Navigation does not send an input
to an old screen coordinate. A setting removed or hidden by a later context change instead produces
an explanatory unavailable result and, where still valid, opens its containing section. A new user
navigation replaces the old reveal intent without repeated scrolling or focus stealing.

#### Scenario: Jump to a setting below the viewport
- **WHEN** the player selects a result in another section below the current viewport
- **THEN** that exact existing row becomes visible and briefly highlighted; its controls keep their values.

#### Scenario: Identical option labels
- **WHEN** two features each expose an option called Enabled and the player selects one result
- **THEN** the result's distinct feature/category identity reveals only the intended row.

#### Scenario: The result becomes stale
- **WHEN** the current save or dynamic setting list changes before a result is selected
- **THEN** navigation resolves against the current context or explains that the target is unavailable, with no stale edit or previous-save access.

### Requirement: SS-04 Search respects common value and scope semantics
Results SHALL show current values and scope through the common settings path, distinguishing Global default, Current save, inherited choices and effective-off or unavailable status without maintaining separate setting truth.
Unknown/unreadable values are labeled unavailable rather than replaced with false or zero. Selecting
and editing the real row uses its existing application timing, validation, save persistence and
dormant override behavior. At the main menu no last-save value is shown as current; all save controls
and callbacks remain disabled. Searching and selecting alone perform no settings writes.

#### Scenario: Global and save values differ
- **WHEN** a matching setting has a global-on value and current-save-off override
- **THEN** the result identifies both scopes accurately and the revealed row retains that exact override.

#### Scenario: Effective-off retains requested on
- **WHEN** original-provider handling makes a requested-on feature effectively off
- **THEN** search explains the effective state while preserving and displaying the stored/inherited request.

#### Scenario: Main-menu browsing
- **WHEN** the player searches and opens a setting without a loaded game
- **THEN** global editing remains usable and Current save/Use global remain disabled with a reason and no stale save data.

### Requirement: SS-05 Search state follows the current view and language
The query, result selection and highlight SHALL be transient settings-view state, cleared on close and never stored as global defaults or per-save overrides.
Language, registration, scope or value changes update the matching text and displayed state before
the next interaction. World-view access uses the current game without requiring a selected map.
Search and results remain usable in all seven baseline languages and supported UI scales, with
full labels/help available when text does not fit.

#### Scenario: Reopen and change saves
- **WHEN** settings is reopened after closing or another save is loaded
- **THEN** search uses current metadata and values with no inherited query, stale highlight or departed-save value.

#### Scenario: Change language
- **WHEN** the active language changes and the settings page is opened again
- **THEN** queries match the newly displayed labels/help and results reveal their newly measured controls.

### Requirement: SS-06 Discovery is read-only and failure preserves browsing
Building or querying the search index SHALL NOT execute hidden settings-page draws, tab/editor/reset callbacks or gameplay actions to discover values and controls.
There is no recurring search scan during ordinary gameplay outside the settings view. A malformed
entry is isolated with an honest unavailable result and bounded diagnostic; a search failure leaves
ordinary manual settings navigation and editing available. No setting or source configuration is
reset, migrated or rewritten by discovery or failure recovery.

#### Scenario: Discovery encounters a failing entry
- **WHEN** one entry cannot supply valid searchable metadata or a current display value
- **THEN** unrelated settings remain searchable and ordinary browsing remains available, without repeated per-frame warnings.

#### Scenario: Search has never been opened
- **WHEN** the player plays normally without opening settings search
- **THEN** this feature does not repeatedly traverse settings pages or invoke their controls in the background.

### Requirement: SS-07 Search activation uses the ordinary settings path
Settings search SHALL default on through one common GlobalAndSave Boolean in Interface/navigation, applying at the next display and preserving the foundation's inheritance, dormant override and main-menu guards.
Turning it off hides search and restores manual browsing without affecting S047's page chooser or
any feature values. An active original `ZIPender.Needle` triggers a bounded startup warning and
best-effort ordinary search-off handling, retaining stored preferences; an inactive installed copy
does not. This establishes no original-mod API, save or coexistence compatibility.

#### Scenario: Turn search off
- **WHEN** the player disables the owned search setting
- **THEN** manual settings browsing remains usable and all other feature values and scopes are retained.

#### Scenario: Original provider is active
- **WHEN** startup detects active Needle
- **THEN** the owned search follows its ordinary effective-off path with one bounded explanation, preserving stored preferences and the independently registered common settings page.
