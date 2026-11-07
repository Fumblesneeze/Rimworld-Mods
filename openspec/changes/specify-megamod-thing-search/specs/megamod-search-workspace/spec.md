# Search workspace

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
S033 Ctrl F - Continued. Specification only; no runtime behavior delivered.

## ADDED Requirements

### Requirement: Offer predictable entry points
The mod SHALL provide Ctrl+F for a new text search, Ctrl+Alt+F for a new search based on the selection, and Ctrl+Shift+F to reopen the current session's last search without clearing its criteria.
The base key defaults to F and is rebindable through native keybinding configuration; Shift takes
precedence if Alt is also held. New text search focuses an empty name field and initially shows
Not searched rather than an expensive unrequested full listing. Typing, deliberately refreshing
or accepting evaluates it; clearing previously entered text permits an unrestricted name match.
Opening an already open workspace focuses it without duplicate windows. A small bottom-right
search-view control toggles the workspace without resetting its query; no ordinary bottom tab is
added. With no loaded game it is unavailable. With a loaded game but no viewed map, an existing
explicit/all-maps query remains accessible; a new Current map search explains that a map is needed.

#### Scenario: Start and resume
- **WHEN** the player types a query, closes it and presses Ctrl+Shift+F during the same loaded game
- **THEN** its criteria and map choice return and its results are freshly evaluated; Ctrl+F instead starts a new empty name search.

#### Scenario: No viewed map
- **WHEN** a new search is requested from a loaded game's world view without a viewed map
- **THEN** the workspace explains the unavailable Current map scope and lets the player choose loaded maps without using a previous game's map.

### Requirement: Find things like the current selection
Find-like-selection SHALL construct an editable query from selected things' exact type/material pairs, or from the exact selected zone.
Different pairs are alternatives; within each pair, type and material are both required. Quality,
condition and stack quantity of the selected example are not implicitly copied as restrictions.
Wrapped examples use their represented type/material. A zone query binds to that actual map/zone.
With no usable selection, including selection of an unsupported plan, fall back to a focused new
name search without throwing or manufacturing a criterion. Unsupported objects mixed with usable
things are ignored with a visible explanation; supported alternatives remain intact.

#### Scenario: Select two materials
- **WHEN** wooden and steel chairs are selected and the player invokes find-like-selection
- **THEN** results can include either exact type/material pair across qualities, but do not include stone chairs solely because they share the chair type.

#### Scenario: Search a zone
- **WHEN** a stockpile is selected and find-like-selection is invoked
- **THEN** the search identifies that exact stockpile and lists matching known things at its held locations on that map.

### Requirement: Make edits deliberate and visible
The workspace SHALL expose listing, map scope, criteria/group controls, clear, refresh and editing lock with understandable labels and tooltips.
Unlocked edits update results immediately, including while paused. Lock prevents changing the current
criteria, listing and scope, including replacement through import or clear; it does not prevent
inspecting results, copying the query or refreshing it. Explicit New search shortcuts are an exception:
Ctrl+F or Ctrl+Alt+F starts a new unlocked working query without modifying the previous query's source
library entry or bill. Imported queries initially open locked and can be explicitly unlocked. Clearing an
unlocked query restores the ordinary default listing/current-map scope and empty name search;
it does not erase saved library entries. Query choices belong to this gameplay workspace rather
than mod settings or global/save inheritance. Standard text editing remains usable in text fields.

#### Scenario: Inspect an imported query
- **WHEN** a transferred query opens locked
- **THEN** the player can inspect, copy and refresh it but must unlock it before changing or replacing its criteria.

#### Scenario: Edit while paused
- **WHEN** a paused player changes an unlocked name or material criterion
- **THEN** results update for the new criterion without requiring a game tick or enabling continuous refresh.

#### Scenario: Start a new search from a locked query
- **WHEN** the player presses Ctrl+F while a transferred query is locked
- **THEN** a new unlocked empty name search becomes the working query and the transferred query's source remains unchanged; Ctrl+Alt+F similarly creates a new unlocked selection-based query.

### Requirement: Present stable understandable results
The results view SHALL show object names/icons, stack quantities, match/unit totals, map/holder location, search status and available actions in a scrollable resizable view.
Group by map, then sort by displayed object type name, material name and held location, with stable
ties so an unchanged refresh does not reshuffle rows. Within the same map and cell, tied rows keep
their order while those objects remain. Different maps or types sharing a label remain distinguishable.
Hover highlights the current map location or holder when known; an off-map result identifies its
map instead of highlighting an unrelated cell. Contained/unselectable results explain that navigation
locates them or their holder but does not directly select an inaccessible inner object. Long names
and all supported UI scales retain access to controls and complete details.

#### Scenario: Inspect carried equipment
- **WHEN** a matching carried item is hovered in the list
- **THEN** its holder/location is identified and any map highlight points to that holder, without falsely showing the item as lying on the ground.

### Requirement: Use ordinary selection and navigation
Result actions SHALL use ordinary game selection/camera behavior and respect the current native selection limit and restrictions.
Left-click selects a selectable spawned result, clearing prior selection unless Shift is held;
clicking an already selected result also locates it. Shift-left-click toggles its selection, and
dragging extends the chosen select/deselect action across results on the viewed map. Double-left-click
selects matching results of that exact object type on the viewed map, regardless of material.
Select all adds eligible results on the viewed map only. Right-click locates a result or known
holder, and right-drag locates successive hovered results without repeating jumps within one row.
Unselectable/contained results use location navigation instead of forced selection. A single
off-map selection/navigation can switch to its loaded map; bulk/drag selection never accumulates
an impossible cross-map selection. Tooltips state **Shift** for additive/toggle selection.

#### Scenario: Select matching stacks
- **WHEN** the player Shift-selects and drags through visible selectable results on one map
- **THEN** the intended rows are added/removed through normal selection, with the native selection limit retained.

#### Scenario: Select all across a multi-map result
- **WHEN** results include two colonies and the player uses Select all while viewing one
- **THEN** only that map's selectable results are added, and the list remains clear that other-map matches were not selected.

### Requirement: Accept navigates to a current first result
Enter in the search workspace SHALL refresh/evaluate the query and locate its first valid result in displayed order, selecting it only if ordinary selection permits.
It leaves the workspace available for further search. A focused child control that consumes Enter
first completes its own native editing action. An empty successful result gives a non-error no-match
indication; an unavailable result explains why it cannot navigate. Before any result action, the
target's existence, visibility, map and location are checked again. A moved target is located at its
current known position; a destroyed, newly hidden or no-longer-matching target is not selected and
causes the results to be refreshed rather than acting on a stale row.

#### Scenario: Accept a search
- **WHEN** the player finishes entering a name and accepts the search
- **THEN** the first current result is located and selected if possible, without closing the search workspace.

#### Scenario: A result vanishes before the click
- **WHEN** a displayed target is destroyed or becomes hidden before the player clicks it
- **THEN** no invalid selection or hidden-location jump occurs and the view refreshes/explains the changed result.

### Requirement: Separate the result view intentionally
The player SHALL be able to keep the results view open after closing the editor, with that separation visibly indicated and off initially.
Closing a connected editor closes both views. Closing the results view closes its editor and stops
its continuous refresh. A separated result view offers an explicit Reopen editor action, including
the source-compatible right-click on its separation control. Reopening edits the same current query.
Moving/resizing either view does not reset the query, and opening the same workspace twice does
not create duplicate refresh work. Escape first releases an active query text field, then closes
the focused view according to these connected/separated rules.

#### Scenario: Watch results while playing
- **WHEN** the player enables Keep results open and continuous refresh, then closes the editor
- **THEN** the result view remains usable and keeps refreshing subsequent changes at its stated interval while the editor is closed; reopening the editor returns to its same query.

#### Scenario: Close the last result view
- **WHEN** a continuously refreshing result view is closed
- **THEN** its editor also closes and it performs no further automatic refreshes until explicitly enabled again.

### Requirement: Refresh at stated boundaries
Manual refresh SHALL always evaluate current facts, even while paused or after an earlier evaluation in the same game tick.
Continuous refresh is initially off and is a clearly indicated workspace toggle. When enabled,
refresh at most one in-game second after relevant changes while time advances; at normal speed
this corresponds to 60 game ticks, not a promise of wall-clock timing at higher speeds or during
lag. While paused, continuous refresh does not run and the UI explains this; edits/manual refresh
still work. Reopening, accepted criterion/scope edits and Enter request fresh results. Without
continuous refresh, display the last evaluation time/status so a snapshot is not presented as live.
Replacing a query stops refresh of the old query and applies the workspace's current toggle to
the new one once. Closing only a separated editor preserves the results view's refresh work;
closing the results view or leaving the game releases all that workspace's refresh work.

#### Scenario: Pause a watching search
- **WHEN** a continuous search is paused and an object changes through another normal paused action
- **THEN** the list remains a labeled snapshot until edited/refreshed, and manual refresh sees the change immediately.

#### Scenario: Replace an automatically refreshed query
- **WHEN** a new query replaces an old one while continuous refresh is enabled
- **THEN** only the new query continues refreshing and the old query stops consuming refresh work.

### Requirement: Isolate working searches from saves and library entries
The current workspace SHALL be temporary gameplay state retained only within the current loaded-game session.
Closing/reopening retains its query; leaving/loading/reloading a game clears its query, results,
map/zone references, selection references and refresh/separation state. Working queries and window
positions are not promised as save data. Explicitly saved reusable searches belong to the separately
specified common search library: reopening one creates an independent working copy and changes do
not rewrite that library entry until the player explicitly saves it. Map/entity references from
another game require an explicit binding choice or an unavailable state, never name-based substitution.

#### Scenario: Switch games
- **WHEN** the player leaves one game and loads another before reopening search
- **THEN** no old map, zone, results or automatic refresh survives; the new workspace starts with default temporary state.

### Requirement: Configure the entry control through common settings
Show search control SHALL be a boolean setting with built-in default true under an ordered Search section using the common Global default, Current save and Use global controls.
New/missing save entries inherit global. A saved override affects only that save; re-enabling Use
global retains the dormant override. Apply effective changes on the next normal UI redraw without
closing an open workspace or disabling keyboard access. At the main menu all save controls and
their write paths are disabled; the global control remains editable. Keybinding assignment uses
native global keybinding configuration as a documented scope exception, because keyboard mappings
are player input preferences already managed by the game. Search criteria, map choices and the
current refresh/separation toggles are workspace operations, not inherited configuration.

#### Scenario: Hide only the control in one save
- **WHEN** the player disables inheritance and turns Show search control off in the current save
- **THEN** that save's bottom-right control disappears on redraw while keyboard access, global default and other saves remain unchanged.

#### Scenario: Configure from the main menu
- **WHEN** Search settings are opened without a loaded game
- **THEN** the global visibility default is editable and every save-specific control/write action is visibly unavailable with an explanation.

### Requirement: Exchange complete supported queries
The workspace SHALL expose the common library's save/open/copy/paste and admitted provider receive/send actions only for supported query representations.
A supported transfer preserves listing, criterion values, include/disable/group semantics and
declared map-binding intent, using an independent copy. Explicit destination-map retargeting is
shown before use; it cannot silently erase an exact-zone restriction. Unsupported clauses, missing
content, unresolved required bindings or malformed input reject the entire transfer with a reason,
leaving the current query and saved entries intact. Copy/paste in a focused text field remains text
editing; whole-query commands have explicit controls. An external provider is optional: its absence
or incompatible version leaves owned search and owned library operations usable and hides/disables
only that adapter's commands. S121 owns the full reusable-library/predicate/external-codec contracts;
this workspace cannot claim unsupported catalogue or foreign-save compatibility from a partial import.

#### Scenario: Import an unsupported clause
- **WHEN** a copied or externally received query contains a criterion the owned system cannot represent
- **THEN** import explains the unsupported criterion and changes neither the working search nor any saved entry.

#### Scenario: Open a production query as a search
- **WHEN** a supported owned bill query is explicitly sent to the workspace
- **THEN** a locked independent copy opens with the stated bill-map scope, and later search edits do not alter the bill's population rule.

### Requirement: Keep admission failures and source co-installation local
When required search integration cannot safely run, the mod SHALL leave that affected search behavior inactive and log one bounded startup warning identifying the failed target, reason, unavailable behavior and fallback.
The presence of another compatible patch alone is not a failure. If the original Ctrl F provider
is active, suppress the overlapping owned entry points/workspace and explain the choice; do not
remove the original, duplicate shortcuts or suppress independent owned stock/hauling/query consumers.
An incompatible optional exchange provider disables only its adapter. Shared-data failure uses a
correct current native query when available, otherwise the explicit unavailable-search state; stale
or partial data is not a fallback. These failures do not damage saved library entries or settings.

#### Scenario: Both search providers are installed
- **WHEN** startup detects the original Ctrl F provider alongside the owned feature
- **THEN** one workspace implementation handles its overlapping shortcuts, a bounded warning identifies the owned suppression, and unrelated owned consumers remain available.

#### Scenario: A required data hook is incompatible
- **WHEN** the owned search cannot prove fresh data through its admitted path
- **THEN** it uses its correct native fallback or reports search unavailable, with an actionable startup warning rather than fabricated zero results.
