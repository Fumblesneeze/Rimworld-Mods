# Searchable content-filter selectors

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: S020, **Cherry picker scroll bars**. Specification only; implementation and native acceptance
remain open. These selectors compose with the future S038 content-configuration feature. They do
not specify or deliver that feature's enable/disable, persistence or external compatibility behavior.

## ADDED Requirements

### Requirement: Contextual selectors preserve the available choices
The content-configuration page SHALL offer separate content-type and source-mod selectors in mod settings, each with a titled searchable list and a choice to remove only that filter.
Opening a selector preserves every choice supplied by the content page, including choices that
currently produce no results. Ordinary choices retain the host's order; filtering does not introduce
a new ranking. The unfiltered list places its All choice first. Choosing a type or source affects
only that view filter and combines with the page's other active filters.

#### Scenario: Select a content type
- **WHEN** the player opens the content-type selector and selects an available type
- **THEN** the page displays that type combined with its existing source and text filters
- **AND** the source filter, text query and enabled-content configuration do not change.

#### Scenario: Select a source and clear just that filter
- **WHEN** the player chooses a source, then opens the source selector and chooses All sources
- **THEN** the source restriction clears while the page's type and text filters remain in effect.

#### Scenario: A choice with no current results remains reachable
- **WHEN** another page filter excludes every entry belonging to a listed source
- **THEN** that source remains in the source selector
- **AND** selecting it shows the empty intersection without silently clearing the other filters.

### Requirement: Search matches every entered word and reports the result
Each selector SHALL match its complete displayed choice labels without regard to letter case, requiring every whitespace-separated search word to occur somewhere in a label.
Word order and leading, trailing or repeated spaces do not affect matching. The search is plain
substring matching, not a pattern language. Blank search shows all choices. The selector displays
the number of matching choices and the total number of choices, including All when present, and
an explicit no-matches message for an empty result. Search edits return scrolling to the start.

#### Scenario: Multiple words match without forming a phrase
- **WHEN** the source list contains Vanilla Furniture Expanded and the player searches `EXP furn`
- **THEN** that label matches, while a label containing only one of those words does not
- **AND** matching choices retain their relative order.

#### Scenario: Blank search restores the complete list
- **WHEN** the player clears a search or leaves only spaces
- **THEN** every choice is shown, the matching count equals the total, and scrolling starts at the top.

#### Scenario: No matching option
- **WHEN** no label contains all entered words
- **THEN** the list reports zero matches and offers no selectable result row
- **AND** the current page filters remain unchanged until a real choice is selected.

#### Scenario: Search uses full labels
- **WHEN** a narrow row visually shortens a long label and the player searches for a word in its hidden end
- **THEN** the full label is searched and the matching option remains selectable.

### Requirement: Resizing and scrolling keep every choice accessible
The selector SHALL be movable and resizable, using additional columns when sufficiently wide and vertical scrolling when choices exceed the available height.
Options read left to right across each row, then downward. Widening, narrowing or scrolling does
not change their relative order, meaning or selected filter. The heading, search, result count,
close control and a usable list remain accessible within the supported screen and UI scales.
Long labels remain identifiable through their full text on hover; this is a proposed readability
correction to the source's shortened labels.

#### Scenario: Long list
- **WHEN** the list contains more choices than fit at once
- **THEN** the player can scroll to and select the final choice without enlarging the window off-screen.

#### Scenario: Expand and contract columns
- **WHEN** the player widens a selector enough for more columns and then narrows it again
- **THEN** all choices remain in the same reading order and can still be reached
- **AND** the view does not remain stranded below the end of its content.

#### Scenario: Read a shortened label
- **WHEN** a label is shortened to fit a row
- **THEN** hovering reveals its complete displayed label, including any identity distinction
- **AND** resizing or viewing the full label does not select it.

### Requirement: Source names and icons identify the chosen source unambiguously
The source selector SHALL associate each choice, its filter and its icon with the same actual source, even when multiple sources share a display name.
Equal names receive a visible distinguishing identifier. Matching searches include that displayed
distinction. A source without an available icon uses a neutral presentation and remains selectable;
it never borrows another source's icon because their names match. All sources has a neutral
presentation. Content types do not require source icons. This identity rule is a proposed
correction to the inspected source's name-based selection and icon association.

#### Scenario: Equal source names
- **WHEN** two active sources share a name and the player selects one distinguished row
- **THEN** only that source supplies the source-filtered entries, combined with the other page filters
- **AND** the row's icon belongs to that source or is neutral.

#### Scenario: Search a distinguishing identifier
- **WHEN** equal source names have visible identifiers and the player searches one identifier
- **THEN** matching is based on the complete displayed labels and the corresponding source is identifiable.

#### Scenario: Missing source icon
- **WHEN** the selected source has no usable icon
- **THEN** the row remains readable and selectable and selection still applies that exact source filter.

### Requirement: Selection and cancellation have distinct effects
Selecting an available row SHALL apply exactly that choice once and close the selector; closing without selection SHALL leave every page filter unchanged.
The close button, outside dismissal and Escape are cancellation paths. Search, resize, scrolling,
hovering and icons are navigation only. An unavailable choice is visibly unavailable with its
reason, cannot change the page and does not dismiss the selector as if selection succeeded.
No background click passes through to change enabled content. Showing unavailable choices clearly
and keeping them open on rejected selection are proposed corrections, not source parity claims.

#### Scenario: Exact choice after filtering and resizing
- **WHEN** the player searches, resizes, scrolls and then selects a matching row
- **THEN** the page applies that row's actual choice once and the selector closes
- **AND** no adjacent or formerly positioned row is selected.

#### Scenario: Cancel after searching
- **WHEN** the player changes selector search text and dismisses with Escape, outside click or the close button
- **THEN** the previous page filters and enabled-content choices remain unchanged.

#### Scenario: Unavailable choice
- **WHEN** the player tries to select a visibly unavailable row
- **THEN** its reason remains accessible, no selection is applied, and the selector stays open.

#### Scenario: Outside dismissal does not edit the page
- **WHEN** the player dismisses by clicking over a content-enable checkbox behind the selector
- **THEN** the selector closes without changing that checkbox or any content choice.

### Requirement: Navigation is transient and separate from settings changes
The selector SHALL start each opening with empty search text and scrolling at the top; its search, scroll and size SHALL NOT be saved as global defaults or per-save overrides.
Closing it does not clear the page's selected filters. Opening the other selector does not inherit
the first selector's search. The feature introduces no preference row, bottom tab or bottom-right
toggle. Browsing remains available at the main menu where the content page is available, while all
save-scoped editing remains disabled under the common settings rules. Changing enabled content
is a separate explicit action governed by S038, never a side effect of selecting a view filter.

#### Scenario: Reopen after choosing
- **WHEN** the player selects a source from a searched list and reopens that selector
- **THEN** the page retains the selected source but the selector starts with the complete list at the top.

#### Scenario: Switch selector types
- **WHEN** the player closes a searched type selector and opens the source selector
- **THEN** the source selector begins with no search and no inherited scroll position.

#### Scenario: Browse without a save
- **WHEN** the player opens the available content page from the main menu and uses either selector
- **THEN** browsing works without a loaded map or save
- **AND** it cannot create or modify a save override or mutate a previously loaded save.

### Requirement: Composition confines behavior to the owned content page
The selectors SHALL be directly composed into the owned content-configuration page and SHALL NOT replace unrelated dropdowns, add another content-management page or require the original source add-on.
Source add-on presence alone does not disable these owned controls: its selectors belong to its
original provider. Activation of the underlying content-configuration feature, including
coexistence with original Cherry Picker, is governed by S038. This change cannot enable a content
feature that S038 has withheld. No automatic foreign settings import or original-mod identity claim
is made by providing these selectors.

#### Scenario: Original add-on absent
- **WHEN** the supported owned content page is active without Cherry picker scroll bars installed
- **THEN** both owned selectors retain their search, navigation and exact selection behavior.

#### Scenario: Other menus
- **WHEN** the player opens an unrelated mod setting, bill menu or assignment dropdown
- **THEN** this feature does not change its menu, labels, selection or close behavior.

#### Scenario: Underlying feature unavailable
- **WHEN** the owned content feature has been withheld by its compatibility policy
- **THEN** these selectors do not reactivate it or present apparently working content-edit controls
- **AND** the owning page explains the unavailability without modifying content configuration.

### Requirement: Unavailable choices cannot apply a different filter
If the selector cannot establish a valid choice catalogue or the selected choice is no longer available, it SHALL preserve the current filters and configuration and explain the failure instead of selecting a substitute.
The player can close and reopen to obtain the current catalogue. An unavailable selector must not
disable unrelated working controls or change the underlying content configuration.

#### Scenario: Choice disappears before selection
- **WHEN** a displayed choice is no longer valid when clicked
- **THEN** no substitute or All filter is applied and the current configuration is unchanged
- **AND** the player receives an explanation and can reopen the list.

#### Scenario: Catalogue unavailable
- **WHEN** the choices cannot be obtained reliably
- **THEN** the page reports that the selector is unavailable instead of displaying a misleading empty catalogue
- **AND** no content or filter value changes.
