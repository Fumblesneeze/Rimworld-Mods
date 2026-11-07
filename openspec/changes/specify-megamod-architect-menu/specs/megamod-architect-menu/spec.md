# Architect menu

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target **1.7 beta**.
Future observable behavior only; implementation and research remain in the design/refinement files.

## ADDED Requirements

### Requirement: Faster browsing preserves the architect's ordinary choices
The mod SHALL provide an Optimize architect menu preference, enabled by default, that preserves the current game's available commands, category assignments, stable order, grouping, labels, colors, information and activation behavior while reducing redundant menu work.

Disabling optimization retains ordinary native browsing. Pagination is independently configured
and can remain enabled. This feature adds no buildings, unlocks, free placement, changed costs or
new bottom tab. Existing construction/replacement behavior composes through its own contracts.
Responsiveness claims require comparative measurements of actual large-menu browsing and input,
including update costs; internal reuse statistics alone are not evidence of a faster experience.

#### Scenario: Compare optimized and ordinary browsing
- **WHEN** the player opens the same category with optimization enabled and disabled
- **THEN** the same current commands, ordering, labels, costs and ordinary actions are available, except for independently configured pagination

#### Scenario: Turn off optimization while paging
- **WHEN** optimization is disabled while command pagination remains enabled
- **THEN** the menu uses ordinary evaluation for the same paged choices and page navigation remains functional

### Requirement: Availability remains current without waiting for simulation progress
The mod SHALL reflect changes relevant to architect availability by the next menu redraw, including while paused, or use ordinary evaluation when it cannot establish that reused results remain valid.

Relevant changes include research, required buildings appearing or disappearing, scenario rules,
difficulty restrictions, discoveries, DLC progression, faction/ideology context and provider-defined
visibility. Enabling god mode uses its native eligibility; leaving god mode immediately restores
ordinary eligibility. A previous visible command must not survive merely because time has not
advanced. Unknown provider behavior must not be replaced by stale visibility or an empty menu.

#### Scenario: A prerequisite disappears while paused
- **WHEN** a required building is removed through an applicable native player action while paused
- **THEN** dependent architect commands reflect the new eligibility on the next redraw without requiring the game to unpause

#### Scenario: Leave god mode
- **WHEN** the player turns off god mode after viewing commands that ordinary research does not allow
- **THEN** those commands return to their ordinary availability on the next redraw

#### Scenario: A provider has changing visibility rules
- **WHEN** a supported menu includes a command whose current visibility cannot safely be reused
- **THEN** the menu evaluates its current ordinary visibility rather than continuing to show a stale answer

### Requirement: Material suggestions and presentation describe the current command
The mod SHALL preserve native default-material selection and explicit material choices, with labels, icons, colors and material-count information describing the same current choice.

Resource-dependent suggestions use the native selection rules and current authoritative resource
view; this feature does not introduce a best-material policy. Explicitly selected material is not
overwritten by a changed stock count. Map switches, material changes, language changes and relevant
ideology/style changes must not leave another context's presentation behind. Pagination alone
does not reset a selected command's material, style or placement rotation.

#### Scenario: Select a material then change pages
- **WHEN** the player chooses a material for a construction command and browses another page
- **THEN** the active command retains that choice and its native placement behavior

#### Scenario: Switch to a map with different resources
- **WHEN** the player opens the architect menu on a different map
- **THEN** unchosen material suggestions and displayed quantities describe that map's authoritative resource view, without importing the previous map's choice or counts

#### Scenario: Change language or ideology presentation
- **WHEN** the active language or relevant ideology/style presentation changes
- **THEN** current labels and colors appear on the next redraw without stale text or colors from the former context

### Requirement: Ordering and combined commands retain their meaning
The mod SHALL preserve native stable ordering and complete combined-command behavior, including equal-order ties, dropdown contents, enabled/disabled state, tooltips and keyboard activation.

A paged command unit is one visible native command or combined command, including its full dropdown
or grouped membership. Paging must not split one combined command's actions across separate pages.
Special commands such as orders and terrain tools remain commands; they are not excluded because
they do not construct a building. Changed or custom grouping is evaluated normally when safe reuse
cannot be established. Command activation still follows current native eligibility and placement.

#### Scenario: A dropdown crosses a potential page boundary
- **WHEN** a native dropdown or combined command is near the end of a page
- **THEN** it appears as one complete command on one page, with all of its ordinary choices/actions intact

#### Scenario: Commands change order or grouping
- **WHEN** a provider changes command order, label, grouping or enabled state
- **THEN** the next display reflects that change and activating a command performs the current ordinary action, not a previous group's action

### Requirement: Command-list pages are optional and complete
The mod SHALL offer command-list pagination, off by default, with 48 visible command units per page by default and a configurable whole-number range of 12 through 120.

The effective page capacity is the configured size limited to what fits in the current viewport,
as described under layout. When more units exist than that capacity, the current page contains at most that many units
plus a distinct Next page control showing current page and total pages, both one-based. Next page
advances and wraps from the last page to the first. The navigation control is not counted as a
command unit and cannot designate cells or become a construction order. When the list fits on
one page, show the ordinary list without a page control. All pages together contain every current
visible command unit exactly once in its ordinary stable order.

#### Scenario: Page through 50 visible commands
- **WHEN** command-list pagination is enabled at 48, the viewport fits that capacity and a category has 50 visible units
- **THEN** page 1 contains the first 48 and page 2 the remaining 2, with Next page wrapping back to page 1 and no missing or repeated unit

#### Scenario: A small category needs no pages
- **WHEN** a category has no more visible units than the effective page capacity
- **THEN** its full ordinary list is displayed without a page-navigation command

### Requirement: Category pages preserve category identity and native restrictions
The mod SHALL offer category-button pagination independently, off by default, with 20 buttons per page by default and a configurable whole-number range of 6 through 40.

The effective category-page capacity is the configured size limited by the current viewport.
Paging uses the current native category sequence, including native unavailable/dimmed category
buttons, with native ordering and preferred-column behavior within each page. A separate Next
page row appears only when needed and shows current/total pages; it wraps at the end. Search and
style controls remain accessible below the category rows and navigation, and the window fits
its actual displayed rows without covering those controls. A category remains the same category
when moved between pages; a disabled category does not become selectable through paging.

#### Scenario: Browse 21 categories
- **WHEN** category pagination is enabled at 20, the viewport fits that capacity and there are 21 current category buttons
- **THEN** the first page shows 20 buttons and the second the remaining button, with usable navigation, search and native category selection

#### Scenario: An unavailable category is paged into view
- **WHEN** the current page includes a category whose native prerequisites are unmet
- **THEN** its ordinary unavailable appearance and selection rejection remain effective

### Requirement: Native search reaches commands beyond the current pages
The mod SHALL preserve the native search matching/highlighting and search scope across the complete category and command collections, independently of the currently displayed pages.

When a search edit makes the native search choose an open category, show the category page containing
it and the first command page with a visible match in that category, if any. If there is no visible
match there, use its first command page without inventing a match or bypassing native restrictions.
The native search's chosen category and matching rules are not redefined by pagination. Nonmatching
commands retain the native dimming behavior; this is not a new filtered-only search mode.

Native acceptance of a unique actionable search match reveals its page and activates the same
command once. A hidden or otherwise ineligible match is not made usable. Manual page navigation
after a search edit keeps the query and must not snap back on each redraw. A later query change or
explicit unique-match activation can reveal the relevant page again.

#### Scenario: Search finds an off-page command
- **WHEN** the current page lacks a visible match and a search edit chooses a category containing one on another page
- **THEN** the appropriate category page and first matching command page are shown with native highlighting

#### Scenario: Activate a unique match on another page
- **WHEN** the player uses the native accept action for a unique actionable match outside the displayed command page
- **THEN** that page is revealed and the ordinary command is activated once without removing its placement restrictions

#### Scenario: Browse after searching
- **WHEN** the player manually advances pages after a search edit
- **THEN** the query remains active and the chosen page stays displayed until another explicit navigation or search-reveal event

### Requirement: Page navigation does not cancel an active tool
The mod SHALL treat page movement as browsing, without automatically selecting a different category, cancelling an active native tool, resetting its material/style/rotation or placing anything.

The previously selected category's command panel can remain open while its category button is
on another page. Explicitly opening a category reveals its category page, while restoring that
category's valid remembered command page. This applies both without a query and with an unchanged
active query; merely reopening a category is not a command-page search-reveal event. Command-page state is
remembered separately for each category during the current map's browsing session. Closing and
reopening Architect or changing categories preserves valid remembered pages; ordinary native
search reset/focus behavior on reopen remains native.

#### Scenario: Browse category pages while placing
- **WHEN** the player advances the category page while a construction tool is selected
- **THEN** the tool and open category remain active without creating a blueprint or selecting another category

#### Scenario: Return to a previously browsed category
- **WHEN** the player leaves a category on page 2 and returns during the same map session with unchanged contents
- **THEN** page 2 is restored without changing command state or repeating a previous activation

#### Scenario: Return to a category during an unchanged search
- **WHEN** the player manually browsed to page 2 after a search edit, leaves the category and reopens it with the same query
- **THEN** its category button is revealed and page 2 is restored with native query highlighting, until a new search edit or explicit unique-match activation requests a different page

### Requirement: Page state recovers from content and lifecycle changes
The mod SHALL keep page numbers within the current valid range when contents change and clear transient page/search-derived presentation state when the map or game changes.

Preserve the current numeric page where it remains valid; otherwise clamp to the last valid page,
using the first page for an empty or single-page list. A page-size edit or changing that pagination
mode resets its corresponding pages to the first page. Disabling a pagination mode restores its
full list without cancelling the active tool. Page positions are not written to save files, and
previous-game commands must not reappear after loading another game or returning to the main menu.

#### Scenario: The final page loses its contents
- **WHEN** visibility or category changes reduce the total number of pages below the current page
- **THEN** the nearest valid earlier page is shown with correct current/total text and no stranded empty page

#### Scenario: Load another game
- **WHEN** the player switches maps or loads another save
- **THEN** transient page positions start at the first page and displayed commands belong only to the new active context

### Requirement: Text and controls remain coherent after layout changes
The mod SHALL lay out architect text and hit areas for the current language, text formatting, scale, available width and native or admitted provider layout.

Changing those inputs must not reuse dimensions from an incompatible earlier display. Native
tooltips/full information remain usable when a label is abbreviated. Page controls must not cover
search, style selection, building details or command hit areas. This feature does not install
unrelated global command-menu layout changes merely to optimize Architect.

Configured page sizes are upper bounds. If a paged surface cannot fit that many units in the
available area, reduce its effective capacity to a positive fitting count and recalculate pages
without changing the saved preference. Keep navigation and every unit reachable. If even one
unit plus navigation cannot fit, use the ordinary surface and explain that paging is unavailable
at this size. Restoring space restores the configured capacity where it fits.

#### Scenario: Resize or change UI scale
- **WHEN** the available architect area or UI scale changes with paging enabled
- **THEN** controls and labels are laid out for the new dimensions and clicking the visible page/search/command controls still invokes the indicated action

#### Scenario: A configured page is too large for the viewport
- **WHEN** the chosen page size exceeds what the current area can display
- **THEN** paging uses a smaller fitting capacity with correct page counts, or explicitly falls back when even one unit cannot fit, without changing the saved size or losing commands

### Requirement: Common settings separate preference from browsing state
The mod SHALL place normal preferences in Interface / Architect, ordered as Optimization, Command pages and Category pages, using the common global-default/per-save-override model.

The five settings are Optimize architect menu=true, Command pages=false, Commands per page=48,
Category pages=false and Categories per page=20, with the ranges above. Each has Use global,
Current save and Global default controls; new/missing save entries inherit. Save editors are inactive
while inheriting, and both save editor and Use global are disabled at the main menu in appearance
and callbacks. A loaded game in world view still has save scope. Overrides never write globals or
other saves; enabling inheritance preserves dormant overrides, and first override creation uses
the current effective value. Saved settings apply on load; edits take effect on the next redraw.

Invalid page-size input preserves the last valid value with an explanation. Page-size editors are
inactive while the pagination mode for their scope is off, retaining their values: the global
editor follows the global mode and the save editor follows that save's effective mode in addition
to its inheritance restriction. The Use global checkbox still follows the ordinary loaded-save
rule independently of that mode. Global Restore
defaults changes only global settings. Current-save Use global for all restores inheritance for
these five preferences, preserves dormant overrides and uses current globals rather than built-ins;
it is disabled without a loaded game. Page position, active tool and category selection are transient
browsing/gameplay state and are not exposed as save-override settings.

#### Scenario: Inherited and overridden page sizes differ
- **WHEN** the global command-page size changes while one save inherits and another has an override
- **THEN** the inheriting save uses the new size while the other retains its own size, with page state reset only where the effective pagination configuration changed

#### Scenario: Configure from the main menu
- **WHEN** Architect settings are opened with no loaded game
- **THEN** global preferences can be edited, save controls and callbacks cannot mutate any save, and disabled pagination-size editors retain their values

#### Scenario: Restore save inheritance
- **WHEN** Current-save Use global for all is used while global preferences differ from built-ins
- **THEN** the save follows those current globals and its dormant overrides survive without changing global settings

### Requirement: Developer diagnostics are separate from normal settings
The mod SHALL offer developer-only architect diagnostics through the top debug tools, off by default, with independent temporary bypasses for availability, ordering, labels, colors, material suggestions, text layout, grouping and width optimizations.

Diagnostics identify admitted, bypassed and unavailable work, and show measured reuse/recalculation
counts without claiming those counts prove a speedup. A Refresh architect data action discards
owned derived menu data and resets diagnostic counters without changing gameplay choices, ordinary
preferences or valid page positions. The next redraw recomputes current presentation. Diagnostics
and bypasses last only for the developer session and reset on developer-mode disable or game exit;
they are not persistent global/save configuration. No statistics overlay remains in ordinary play.
There is no exposed setting that deliberately permits stale availability while work is deferred.

#### Scenario: Isolate a suspected optimization
- **WHEN** a developer temporarily bypasses label optimization through the top debug tools
- **THEN** labels use current ordinary calculation while other admitted optimizations and pagination remain available

#### Scenario: Refresh data and exit developer mode
- **WHEN** a developer refreshes architect data, then disables developer mode
- **THEN** the menu recomputes without changing gameplay state or valid pages, and diagnostic views/bypasses stop without persisting into ordinary play

### Requirement: Other menu providers keep ownership of their interfaces
The mod SHALL preserve an active external provider's command identities and UI when its behavior can be admitted safely, and disable only the affected owned optimization or pagination surface when it cannot.

An external replacement of the building submenu keeps that submenu; owned command-list paging is
unavailable there unless a specific integration supports it. Category paging and independent
optimizations can remain active where separately admitted. Provider edits to categories, ordering
or visibility must be reflected on the next redraw through a validated integration or ordinary
evaluation. Original Architect Menu Optimizer co-installation suppresses owned overlapping menu
enhancements as a group with a startup warning, without changing the original's configuration.

No provider package, save or binary identity is impersonated, and no settings are automatically
imported. Unverified compatibility claims from source documentation remain unverified for megamod.

#### Scenario: A provider replaces the submenu
- **WHEN** an external menu owns a building-submenu layout without an admitted paging integration
- **THEN** that menu remains usable, the owned command-page setting explains its unavailability there, and separately admitted category/navigation behavior remains available

#### Scenario: The original optimizer is active
- **WHEN** the original Architect Menu Optimizer package is active
- **THEN** owned overlapping enhancements remain inactive with a warning and the original's patches/settings remain untouched

### Requirement: Unsupported targets leave usable native or provider behavior
The mod SHALL check documented semantic prerequisites before applying each enabled patch and skip only its affected patch or atomic behavior group when those prerequisites are false or unknown.

Compatible other patches and unrelated code edits are allowed. A skipped enabled patch produces
one bounded startup warning naming its target specification, target, reason, unavailable behavior
and fallback. Pagination content, navigation and window-size changes must activate or fall back
together; unsupported changes must not leave a shortened window with inaccessible commands.
Runtime loss of valid derived results uses current ordinary evaluation rather than stale or
invented data. No foreign patches are removed and no warnings repeat on every frame.

#### Scenario: A category-layout assumption changes
- **WHEN** category pagination can no longer provide both its content and correctly sized navigation/search area
- **THEN** that whole pagination surface remains inactive, the ordinary category menu remains usable, and one warning per skipped enabled patch explains why

#### Scenario: An unrelated compatible patch exists
- **WHEN** another mod changes an unrelated part of the target while the required architect behavior still holds
- **THEN** the owned feature remains eligible instead of being rejected solely because another patch owner exists
