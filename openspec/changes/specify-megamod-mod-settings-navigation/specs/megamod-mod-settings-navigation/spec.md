# Mod settings navigation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, S047 Mod Options Sort. Observable draft; no implemented behavior claim.

## ADDED Requirements

### Requirement: MO-01 Every available settings page keeps its own identity
The mod SHALL organize the settings pages available through the supported native/provider Mods
options list, preserving independently openable pages even when their titles, authors or owning mod
are the same. Choosing a page SHALL open that page's own controls. The chooser SHALL NOT change
mod activation, load order or gameplay settings merely by listing, sorting or searching them.

#### Scenario: Several pages belong to one mod
- **WHEN** one mod or settings provider supplies multiple independent settings pages
- **THEN** every page remains separately discoverable and opens its own controls.

#### Scenario: Duplicate titles remain distinguishable
- **WHEN** two independent pages have the same displayed title
- **THEN** both remain listed in a stable order, with available owner/page identity details that
  distinguish them without changing the provider's settings labels.

#### Scenario: Browsing does not edit settings
- **WHEN** the player searches, changes sorting or inspects an entry without opening and editing it
- **THEN** no gameplay setting, global default, save override, mod activation or load order is changed.

### Requirement: MO-02 Author grouping and name sorting remain selectable
The mod SHALL provide author-group and name sorting. Author mode and grouping single-entry authors
SHALL both start enabled after game launch. In author mode, settings entries share their normalized
author heading; entries sort by settings title within each group. Single-entry groups move to
Various Authors when that option is enabled, and Various Authors appears last. Group sizes SHALL
use the complete available page list before search filtering. Name mode omits group headings and
shows author attribution on each entry. Equal titles SHALL retain a stable tie order.

#### Scenario: An author has several settings pages
- **WHEN** author sorting is selected and one author has several available pages
- **THEN** those pages share an author heading and are ordered by displayed settings title.

#### Scenario: Single-entry authors are collected last
- **WHEN** singleton grouping is enabled and several authors each have exactly one available page
- **THEN** those pages appear in the final Various Authors group, each retaining visible attribution.

#### Scenario: Disabling singleton grouping reveals their headings
- **WHEN** the player disables grouping single-entry authors
- **THEN** those entries appear under their own author/group headings in ordinary alphabetical order.

#### Scenario: Search does not change group membership
- **WHEN** an author has several pages but only one matches the current search
- **THEN** the result stays under that author's group rather than moving to Various Authors.

#### Scenario: Name mode preserves the remembered group choice
- **WHEN** the player switches to name sorting and then returns to author sorting
- **THEN** name mode lists pages alphabetically without group headings, and returning restores the
  previous singleton-grouping choice without changing any settings values.

### Requirement: MO-03 Special groups preserve attribution and handle missing metadata
The mod SHALL retain the source's visible group conventions: mod names containing both Vanilla and
Expanded share the Vanilla Expanded group; otherwise author text containing sarg uses Sarg Bjornson,
then author text containing helixien uses Helixien. These comparisons ignore case. Positively
identified HugsLib-generated pages use the HugsLib-generated group before those conventions.
Each group remains subject to the singleton rule. Original author information SHALL remain available
for search and identity inspection. Missing information SHALL use an Unknown author fallback and
SHALL NOT by itself identify a page as HugsLib-generated.

#### Scenario: A source family alias does not erase authorship
- **WHEN** a page is grouped under Vanilla Expanded or a recognized author alias
- **THEN** its original author information remains searchable and inspectable.

#### Scenario: Modern HugsLib pages have ordinary metadata
- **WHEN** a supported HugsLib-generated page also provides a mod name and author
- **THEN** it is still recognized as provider-generated, can share the HugsLib group and retains
  its available original owner information.

#### Scenario: An unknown page has no metadata
- **WHEN** an otherwise supported settings page lacks content/author information
- **THEN** it remains reachable with an Unknown author fallback, without falsely becoming HugsLib.

#### Scenario: A special group contains only one page
- **WHEN** a normalized special group has only one available page and singleton grouping is enabled
- **THEN** that page appears under Various Authors while retaining its normalized and raw identity
  for search and inspection.

### Requirement: MO-04 Search includes titles, mod names, authors and groups
The mod SHALL perform case-insensitive substring search across the displayed settings title, mod
name, original author information, normalized author and displayed group. An empty query SHALL
show all available entries. Filtering SHALL preserve independent matches and current grouping/order
without editing the pages or searching opaque values inside unrelated settings controls.

#### Scenario: Search by an author hidden by grouping
- **WHEN** the player searches for an original author whose page is displayed under Various Authors
  or a special family heading
- **THEN** that page is still found in its current group.

#### Scenario: Search by mod name rather than settings title
- **WHEN** a page's settings title differs from its mod name and the player searches the mod name
- **THEN** the page appears as a match.

#### Scenario: Group search follows the displayed view
- **WHEN** a singleton page is displayed under Various Authors and the player searches that label
- **THEN** it matches in author mode while that grouping is enabled; in name mode or with singleton
  grouping disabled, the label alone does not match unless it occurs in another searchable field.
- **AND** raw and normalized author search remain available in both modes, and returning to author
  mode restores the remembered singleton choice using the complete available page list.

#### Scenario: Clear or unmatched search
- **WHEN** a query has no matches and the player then clears it
- **THEN** the chooser first shows an empty-result state and then restores all available pages in
  the chosen order, without stale empty headings or unreachable results.

### Requirement: MO-05 Browsing state and readable controls stay with the settings chooser
The mod SHALL place these navigation controls in the existing Mods options chooser. Sort/group
choices SHALL persist across chooser openings within the same running game process and reset to
their enabled defaults on restart; they are transient browsing state and SHALL NOT be written as
global or save configuration. Search SHALL reset on a fresh opening. Available icons, readable
titles, full-title/author details and localized controls SHALL remain usable at supported UI scales.
No new gameplay bottom tab, debug button or colony-view toggle SHALL be added.

#### Scenario: Reopening preserves sorting but clears search
- **WHEN** the player changes sorting, enters a query, closes the chooser and reopens it
- **THEN** the sort/group choices remain, the new chooser has an empty query and no save/global
  setting was written to retain those browsing choices.

#### Scenario: Restart returns to source defaults
- **WHEN** the game is restarted after using name sorting or ungrouped authors
- **THEN** the chooser starts in author mode with singleton grouping enabled.

#### Scenario: A title is long or an icon is missing
- **WHEN** an entry's full title does not fit or no usable icon is supplied
- **THEN** the entry remains readable and selectable, with full identity details available and no
  broken image preventing access; supported language/scale changes keep controls reachable.

#### Scenario: The megamod keeps one structured settings entry
- **WHEN** the player selects Make Rimworld Great Again
- **THEN** its one entry opens the structured common settings pages, with normal Global default,
  Current save and Use global behavior and main-menu save protections, rather than a collection of
  fake settings entries for the original mods whose features it replaces.

### Requirement: MO-06 Native and provider settings retain their own editing lifecycle
The mod SHALL open native settings through their normal page and supported provider settings through
their provider's own page. Successful edits and closing SHALL retain that owner's normal persistence.
An optional provider's absence or unsupported behavior SHALL NOT make unrelated native settings
unusable. The chooser SHALL NOT replace an unavailable provider page with an empty or misleading
native page, silently claim an edit succeeded, reset foreign defaults or import source settings.

#### Scenario: A native settings page saves normally
- **WHEN** the player opens a native mod page, edits one of its settings and closes it normally
- **THEN** that same owner's controls and ordinary persistence handle the edit.

#### Scenario: HugsLib opens the selected settings pack
- **WHEN** the player selects a supported HugsLib-generated entry
- **THEN** the provider opens the corresponding pack's real settings page, including when another
  pack shares its mod name or package.

#### Scenario: HugsLib is absent
- **WHEN** no supported HugsLib provider is active
- **THEN** native pages remain searchable and openable without requiring that optional library.

#### Scenario: A provider route is unavailable
- **WHEN** a provider-generated page cannot be safely opened through the owned chooser
- **THEN** the affected integration is unavailable with an explanation, its established provider
  menu route is preserved, and unrelated native pages remain usable; the chooser does not present
  an empty substitute page as working settings.

### Requirement: MO-07 Compatibility failures leave settings accessible
The mod SHALL enable the chooser integration only for supported native/provider behavior, preserving
compatible added entries. Unsupported required menu behavior SHALL leave the owned overlap unavailable
with a bounded startup warning identifying the skipped change, reason and retained native/provider
navigation. Active original Mod Options Sort SHALL instead trigger the foundation's best-effort
ordinary feature-off handling with a bounded startup warning, unchanged stored preferences and no
modification of the original. It SHALL NOT promise source-menu usability or coexistence compatibility;
an inactive installed original SHALL NOT trigger this policy. Recoverable per-entry metadata, icon or
opening failures SHALL be isolated without repeated per-frame warnings or loss of unrelated pages.

#### Scenario: Another provider contributes a compatible page
- **WHEN** another supported provider adds a settings entry before or during an admitted chooser
  refresh
- **THEN** that page remains available with its owner identity and opening behavior.

#### Scenario: The original sorter is active
- **WHEN** original Mod Options Sort is active
- **THEN** one bounded startup warning names the source and best-effort handling through the ordinary chooser feature-off path
- **AND** its effect and timing match manually off, stored preferences remain intact and the independently registered common settings page is not removed
- **AND** no original-provider navigation or coexistence guarantee is made.

#### Scenario: Required chooser behavior is unsupported
- **WHEN** required menu behavior cannot be supported without losing another settings route
- **THEN** the owned chooser integration stays unavailable as a coherent whole and leaves existing
  native/provider navigation in place with an explanatory startup warning.

#### Scenario: One entry fails while other entries work
- **WHEN** one page has recoverable metadata/icon or opening failure
- **THEN** the chooser explains the affected entry's limitation, keeps unrelated entries usable
  and reports the failure without a repeated per-frame log flood.
