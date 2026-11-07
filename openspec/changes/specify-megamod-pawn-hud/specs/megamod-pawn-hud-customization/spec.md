# Pawn HUD customization

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. S127 observable draft; implementation and acceptance remain pending.

## ADDED Requirements

### Requirement: Configure the HUD through common scoped settings

HUD configuration SHALL use ordered **Interface → Pawn HUD → Presentation, Content, Appearance,
Presets, Compatibility** sections in mod settings. Each actual configuration value, including the
docked/floating layout collections, SHALL use the common Global default / Current save / Use global
model. Presentation and underlying-pane modification SHALL be one combined preference with one
inheritance choice, so independently inherited flags cannot produce an invalid combination.
Save values SHALL inherit the live global default until overridden; returning to inheritance
SHALL retain the dormant override. Independent layout controls SHALL edit only the chosen scope and
presentation. Changes SHALL take effect on the next visible refresh without restarting the game.

With no active game, all save controls and save-directed mutations, including layout editing,
reset and preset application, SHALL be disabled; global editing SHALL remain usable with a
clearly identified sample preview if no pawn can be previewed. A sample SHALL NOT mutate a game.
The reusable named-preset library SHALL be global-only because it is a collection for sharing
configurations across saves, not a live save preference; applying a preset SHALL still target an
explicit global or writable current-save scope. The view toggle and individual pawn policies are
gameplay/view state and SHALL NOT be folded into the layout's inheritance.

#### Scenario: Inherit and override a layout
- **WHEN** the player overrides the current save's docked layout, changes the global layout, and
  later checks Use global
- **THEN** the override is isolated while active, the inherited layout follows the latest global
  value, and unchecking Use global restores the retained override.

#### Scenario: Configure from the main menu
- **WHEN** Pawn HUD settings open with no active game
- **THEN** global preferences and the reusable library are editable, all save-targeted actions are
  disabled with reasons, and no previous or implicit save is changed.

### Requirement: Expose bounded presentation preferences

The following source-derived initial values and ranges SHALL be available through the common
settings UI. Dimensions are logical UI units; screen-fit protection SHALL keep controls accessible
without discarding a preferred value. Out-of-range or invalid input SHALL be rejected visibly
without partially applying related values.

| Preference | Initial value | Allowed values / relevance |
| --- | --- | --- |
| Refresh interval | 500 ms | 100–3000 ms, 100 ms steps |
| Presentation and underlying pane | Integrated | Integrated; Floating with modified inspect pane; Floating with native inspect pane |
| Show log in modified floating-mode inspect pane | On | Only relevant with floating HUD and modified pane |
| Inspect pane height | 232 | 165–800 |
| Inspect tab width / minimum tab slots | 85 / 7 | 72–150 / 6–12; actual visible tabs remain accessible |
| Show faction / ideology icon | On / On | Only where that information exists |
| Floating anchor | Top right | Nine corner/edge/center positions |
| Floating offsets | 0, 0 | Screen-bounded positioning at the current scale |
| Floating width / height | 320 / 416 | 100–1000 each, with screen-fit handling |
| Avoid letter overlap / spacing | On / 4 | Spacing 0–12; applies only to visible floating HUD overlap |
| Regular text size | 13 | 7–20 |
| Large / small size adjustment | +9 / -1 | 0 to +24 / -5 to 0 relative to regular; positive readable final sizes |
| Each text style's line spacing | 100% | 100–250% |
| Label / numeric-value width | 95 / 40 | 30–150 each; full text remains reachable |
| Show skill progress remainder | On | Does not change actual experience or numeric-format preferences |

Changing presentation SHALL preserve both layouts and dormant log preferences. Integrated always
includes the necessary pane modification; the two floating choices make the underlying pane's
behavior explicit. Changing a global presentation SHALL affect inheriting saves but SHALL NOT
rewrite a current-save override or its dormant value.

#### Scenario: Change presentation without losing a layout
- **WHEN** the player customizes both layouts and switches presentation modes
- **THEN** each layout reappears intact and the named presentation determines the underlying pane,
  without silently disabling the HUD or destroying the other layout.

#### Scenario: Mixed inheritance and presentation changes
- **WHEN** save A overrides presentation to Floating with native inspect pane, save B inherits the
  global presentation, and the global choice changes from Floating with modified pane to Integrated
- **THEN** A stays floating with its native pane and B becomes integrated, including after switching
  saves; checking Use global in A adopts Integrated, and unchecking restores A's former choice.

### Requirement: Customize semantic colors and bar styles

The player SHALL be able to edit the following color roles with a preview, color components,
opacity and RGBA entry. Defaults SHALL retain source-readable distinctions:

| Roles | Initial appearance |
| --- | --- |
| Main text / disabled / neutral | White / medium gray / light gray |
| Critical / warning / good / excellent | Native readable red / yellow / cyan-green / green |
| Bar background / full / low / threshold | Dark gray / green / dark red / translucent black |
| Selector text / background / separators | White / dark gray / translucent light gray |
| Minor / major passion | Pale amber / stronger amber |
| Saturated / active skill | Pale red / pale yellow-green |
| Own / ally / independent / hostile faction | White / native ally / native neutral / native hostile colors |
| Wild / prisoner / slave | Brown-orange / orange / yellow-orange |

Each bar SHALL support low-to-full gradient, low-only, full-only and reversed-gradient presentation,
initially low-to-full unless its supported provider specifies another style. Invalid color input
SHALL leave the previous value intact. Colors SHALL NOT replace textual state or cause changes to
the underlying health/need/skill. Restoring a color or section SHALL affect only the chosen scope.

#### Scenario: Edit warning colors
- **WHEN** the player changes warning and critical colors or makes them identical
- **THEN** the preview and live HUD use those colors while warning text still distinguishes the
  conditions, and invalid RGBA input does not damage the previous configuration.

### Requirement: Edit ordered layouts and conditional content

The layout editor SHALL support horizontal/vertical groups, panels, rows, blank spacing,
separators and widgets. The player SHALL be able to add, remove and reorder entries, expand/collapse
the editor tree, allocate remaining height to a group/panel, and choose which of the four pawn
display groups may see each entry. At least one display group SHALL be selected for an enabled
entry; hiding an entry SHALL never become “visible to everyone” after saving. Editing one entry
SHALL NOT unexpectedly change another entry made from the same template.

The catalog SHALL include all HUD information/actions from the display capability, existing needs,
skills, training, pawn statistics and records, and admitted third-party widgets. Names and origin
SHALL be identifiable; missing catalog content SHALL not prevent editing the rest of the layout.
Selecting optional data SHALL not add that need, skill, training or statistic to a pawn. Rows with
no applicable content SHALL collapse; removal/reordering SHALL leave the remaining controls usable.

#### Scenario: Add a modded need and restrict its audience
- **WHEN** the player adds an admitted modded need to one row and enables it only for player creatures
- **THEN** only matching creatures that actually have the need show it; humanlikes and creatures
  without it do not acquire a fabricated bar, and the choice survives save/reload.

#### Scenario: Independent copies of an entry
- **WHEN** the player adds the same widget to two groups and changes only one's audience or bar style
- **THEN** the other entry retains its own choices through editing and persistence.

### Requirement: Reuse presets without ambiguity or silent data loss

The player SHALL be able to select the default layout, a compact variant, a classic grouped
variant, a detailed preview variant including traits, user presets and admitted provider presets.
These are independently authored configurations; no upstream image or implementation is required.
Optional hygiene/thirst variants SHALL appear only with the corresponding active provider and
available needs, not merely a matching display name.

Saving a preset SHALL allow integrated layout, floating layout or both, and optional dimensions,
tab sizing and text sizing. At least one layout SHALL be included. Applying it SHALL identify its
origin and explicit writable destination scope and show which layout/settings will change;
omitted layouts and preferences SHALL remain unchanged. Importing older supported presets SHALL
preserve their meaningful content, warn about unsupported entries and never execute arbitrary file
content. Presets with the same display name from different providers SHALL stay distinguishable.

Named user presets SHALL reject invalid/reserved names and path traversal. Overwriting or deleting
a user preset SHALL require confirmation within the game. Built-in/provider presets SHALL be
read-only; the player can save a separate user copy. Deleting a reusable preset SHALL not erase
layouts already applied to a save or global scope. Unsupported/missing provider entries SHALL
remain identifiable and restorable when the provider returns.

#### Scenario: Apply only a floating preset
- **WHEN** the player applies a preset containing only a floating layout and no text or size preferences
- **THEN** only that layout in the chosen scope changes; the integrated layout, fonts, dimensions,
  global defaults or other saves outside the chosen scope remain unchanged.

#### Scenario: Same name from two providers
- **WHEN** two active providers supply a preset named “Compact” and the player chooses one
- **THEN** its provider is visible before selection and that exact preset is retained across reload,
  without resolving to the first unrelated mod or the built-in Compact preset.

#### Scenario: Missing provider and failed import
- **WHEN** a provider disappears or a malformed preset fails validation
- **THEN** existing valid layouts remain usable, the missing/invalid content is explained and kept
  recoverable, and no unrelated preset or settings file is overwritten.

### Requirement: Support the published external widget behaviors

For admitted providers the HUD SHALL support the published five version-one widget forms:
an existing need with optional tooltip, a labeled value, a bar with value/thresholds, a selector,
and a custom drawn region with a declared height and whether it occupies space. Labels SHALL use
their localized text or stable identifier fallback. Values, fills, thresholds, style, tooltip,
hover and click behavior SHALL retain their documented meanings. Selectors SHALL be discoverable
in the editor alongside the other four forms; hovering SHALL invoke hover behavior and clicking
SHALL invoke click behavior, never the reverse. Standard external selectors SHALL show their
provided tooltips. Omitted text style SHALL use Regular for custom bars and Small for custom
values/selectors, following the published defaults; an explicitly supplied supported style SHALL
be honored. This intentionally corrects the inspected custom-bar implementation's omitted-style
behavior, which produces Small despite its documentation promising Regular.

A widget's absence for a particular pawn SHALL collapse it without an error. Unsupported versions,
missing required content, invalid/non-finite dimensions or callback failures SHALL make only the
affected widget unavailable with bounded explanation. A provider's read-only presentation SHALL
not gain permission to mutate a different pawn through stale context. Admission SHALL preserve
native permission and multiplayer restrictions for actions; unverified action support SHALL be
disabled rather than silently invoked unsynchronized.

This is a supported format/behavior contract, not an assertion that all upstream binaries, package
checks or private patches bind automatically. Such limitations SHALL be visible in compatibility
documentation before a drop-in claim is made.

#### Scenario: Selector callbacks and tooltip
- **WHEN** the player adds an admitted custom selector, hovers it and then clicks it
- **THEN** its label and tooltip appear, only its hover behavior occurs on hover, and its click
  behavior occurs once on activation for the intended subject.

#### Scenario: Invalid third-party height
- **WHEN** a custom region reports an invalid height or its required provider is unavailable
- **THEN** it is shown as unavailable in configuration without corrupting neighboring layout,
  blocking native inspection or erasing the saved reference.

#### Scenario: Published default text styles
- **WHEN** a provider supplies a custom bar, value and selector without text-style preferences
- **THEN** the bar uses Regular text and the value/selector use Small; an explicit supported style
  changes only the widget that declares it.

### Requirement: Persist and recover configuration safely

Global preferences, save overrides and reusable presets SHALL persist separately through the common
settings mechanism. Existing foreign RimHUD configuration SHALL NOT be edited or automatically
migrated. Any explicit supported import SHALL first retain a recoverable copy of the destination,
validate the whole proposed update and apply it atomically; failure SHALL restore the previous
destination and report that no successful import occurred. The source SHALL remain untouched.

Missing fields SHALL receive documented defaults/inheritance. Malformed or unsupported saved data
SHALL be retained for recovery while the affected section uses a safe default, with a bounded
diagnostic; a version change alone SHALL NOT reset unrelated settings. Reset SHALL name its target
scope and affected sections. Global reset SHALL restore the specified built-in defaults for those
sections, including built-in layouts/colors, without editing save overrides or the named-preset
library. Inheriting saves SHALL naturally follow those reset global values. Current-save reset
SHALL set Use global for the chosen sections while retaining all dormant overrides, including
layouts/colors; it SHALL NOT replace them with built-in values or clear the named-preset library.
Re-enabling an override SHALL restore that retained value. Other sections and saves' own overrides
SHALL remain unchanged. Native pawn policies SHALL
remain valid if the HUD is disabled or removed; the feature SHALL not require a separate game
content mod to keep those policies usable.

#### Scenario: Interrupted preset application
- **WHEN** writing an accepted import/preset update fails after validation
- **THEN** the prior destination is restored, the source is untouched and the player sees failure,
  not a success message or a partially replaced pair of layouts.

#### Scenario: Save switching and old preferences
- **WHEN** the player switches between an inherited save and an overridden save, or loads older HUD data
- **THEN** the appropriate effective preferences are used without leaking another save's state,
  and invalid fields are recoverable without resetting the entire HUD or changing pawn policies.

#### Scenario: Reset with customized global values
- **WHEN** global colors/layouts are customized and the current save contains a mixture of inherited
  and overridden values, then the player resets only that save's HUD appearance/content sections
- **THEN** those sections inherit the current customized globals, dormant overridden colors/layouts
  remain recoverable by unchecking Use global, and unrelated sections and saves are unchanged.

#### Scenario: Reset global appearance
- **WHEN** the player resets global HUD appearance to built-in defaults
- **THEN** inheriting saves use those defaults, active/dormant save overrides and all reusable
  presets remain intact, and the unrelated content/presentation sections are unchanged.
