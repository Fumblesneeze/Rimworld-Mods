Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S093/P2-22, Labels on Floor (continued). Observable draft; nothing implemented or accepted in game.
The corrections and proposed defaults below are explicit owned choices, not additional user answers.

## ADDED Requirements

### Requirement: Automatic labels describe the current revealed room or zone
The feature SHALL display the current meaningful room purpose and zone name on the viewed map.
A growing zone without a custom label displays its selected crop. Other zones use their current
native name. Rooms with no meaningful purpose are omitted unless customized; a location which
the game does not recognize as a room remains ineligible. Names display in uppercase where the
writing system has case. Custom text takes precedence over automatic text. A room's custom name
does not change its actual purpose, score, ownership or use; zone labeling does not change its
filter, priority, crop or jobs. No label reveals fogged content, including in Smart placement.

#### Scenario: Purpose and crop change
- **WHEN** a revealed dining room becomes a hospital or the player changes a growing zone's crop
- **THEN** its automatic label updates to the current purpose or selected crop without reopening settings
- **AND** a custom label remains unchanged until the player clears it

#### Scenario: Generic and unavailable rooms
- **WHEN** a revealed generic room is customized and a different location has no eligible room
- **THEN** the customized room can display a label and the ineligible location cannot acquire one
- **AND** labeling either location does not create or change a room mechanic

#### Scenario: Fog and map boundaries
- **WHEN** an otherwise eligible room is still fogged, or a proposed zone label would cover fogged cells
- **THEN** no hidden room identity or label text is exposed there
- **AND** only the currently viewed map supplies displayed labels

### Requirement: Labels have contextual transactional editing
The feature SHALL provide a label editor through an Architect Orders action and a rebindable
hover shortcut, initially R. The hover shortcut opens only when no pawn is selected and no text
editor or higher-priority interaction has consumed it. A zone under the pointer is the default
target; when both a zone and eligible containing room exist, the editor also offers an explicit
choice of those two targets. This addition keeps a fully zone-covered room editable. The editor
remains accessible when labels are hidden, pauses ordinary simulation and identifies its target.

Each target has custom text, default/custom color, default/show/hide visibility, automatic/
horizontal/vertical orientation and whole-cell horizontal/vertical offsets from -10 through +10,
initially zero. These are entity state, not mod settings. Custom text is trimmed, single-line and
limited to 27 displayed text characters after uppercase conversion; invalid input explains the
problem and does not discard the previous valid draft. Clearing text restores automatic naming.
Reset appearance/name is an explicit confirmed action and remains part of the draft until Apply.

Apply or Enter commits all edits together. Cancel, Escape, close button, outside dismissal,
target switching and target loss discard uncommitted edits; target switching opens the other
target's saved values. Preview is optional but never becomes committed state by merely dismissing
the editor. A failed or conflicting commit leaves the prior saved values intact, explains the
failure and cannot replay on reload. Individual reset does not erase global/save preferences.

#### Scenario: Find either target without a new bottom tab
- **WHEN** the player invokes the action over a stockpile covering a room while labels are hidden
- **THEN** the editor identifies the stockpile and permits choosing the containing room
- **AND** neither the action nor the editor adds a bottom tab or edits global settings

#### Scenario: Apply a complete customization
- **WHEN** the player changes a label's text, color, visibility, orientation and offsets and presses Enter
- **THEN** all valid values become the saved customization together and the display refreshes
- **AND** an explicit white color remains an override of a different default color

#### Scenario: Every dismissal cancels
- **WHEN** the player previews a name or reset and then uses Cancel, Escape, close, outside dismissal or target switching
- **THEN** the original label and native zone name remain unchanged
- **AND** reopening the editor or reloading the save does not restore the discarded draft

#### Scenario: Invalid text or concurrent target change
- **WHEN** an edit exceeds the text limit, or the target is removed or externally renamed before Apply
- **THEN** invalid text cannot replace the valid draft, and a stale whole-target commit cannot overwrite newer state
- **AND** the player receives a clear explanation and can reopen the current target safely

### Requirement: Custom zone names remain coherent with ordinary renaming
The feature SHALL make an applied custom zone name the zone's ordinary visible name as well as
its floor label. While a custom name is active, ordinary native zone renaming updates that same
floor name. Appearance-only edits do not rename the zone. Clearing the custom name restores the
ordinary zone name remembered before customization and resumes automatic floor naming, including
the current crop for a growing zone. An external native rename while customized becomes the new
remembered ordinary name, so a later reset cannot undo that newer explicit rename. Room names
remain floor annotations only. Label color never changes the zone's native selection tint.

#### Scenario: Apply and reset a growing-zone name
- **WHEN** the player names a growing zone HERBS through the label editor, changes its crop, then clears its custom name
- **THEN** the native name and floor label first show HERBS, the custom label survives the crop change,
  and reset restores its remembered native name while the floor label shows the current crop

#### Scenario: Native rename remains authoritative
- **WHEN** a customized stockpile is renamed through the ordinary native rename control
- **THEN** its floor name follows that rename and later clearing the custom floor name does not restore an older native name
- **AND** changing only text color leaves the stockpile's normal tint and storage rules unchanged

### Requirement: Placement respects geometry and deliberate orientation
The feature SHALL offer Classic and Smart placement. Classic uses a suitable bottom or left
edge, preferring the edge with more usable contiguous space and horizontal placement on a tie.
Smart prefers clearer space in the area, penalizes obstructed room locations and falls back to
Classic if no better valid position fits; bedroom bed-foot and hospital-door proximity are favorable,
while prison-door proximity is unfavorable. These are preferences, never permission to draw through
invalid space. Automatic orientation considers horizontal and vertical fits. A forced orientation
never silently changes to the other direction.

Automatic label footprints fit entirely within revealed cells of their own area, not merely its
bounding rectangle, and remain between the configured minimum and maximum size. Labels which
cannot fit at minimum size are omitted. Manual offsets apply after placement and can intentionally
move a label outside its area, but never outside the map or into fog; an invalid offset placement
is omitted without deleting its customization. A partially visible label remains drawn while its
footprint intersects the view. Relevant geometry, furnishings, crop and naming changes refresh
placement even when the area's total cell count stays the same.

#### Scenario: Irregular and tiny areas
- **WHEN** an L-shaped zone, disconnected zone or tiny room cannot contain a complete label at minimum size
- **THEN** the label uses another valid fitting position or is omitted
- **AND** it does not bridge a hole, expose fog or shrink below the configured minimum

#### Scenario: Forced direction and offset
- **WHEN** horizontal orientation cannot fit but vertical could, or an offset would move the label out of bounds
- **THEN** the label is omitted with its customization retained
- **AND** changing back to automatic or a valid offset allows it to return

#### Scenario: Smart placement and geometry refresh
- **WHEN** the player changes furnishings or exchanges zone cells while keeping the same number of cells
- **THEN** Smart placement uses the new shape and obstacles, with a valid Classic fallback if needed
- **AND** camera panning does not make a partially visible label disappear merely because its origin is off-screen

### Requirement: Display controls have clear precedence and overlap behavior
The feature SHALL expose one small bottom-right control and rebindable shortcut, initially L,
cycling Visible, On top, Hidden, then Visible. Visible places text at floor level; On top makes it
readable over map objects. The control identifies the current mode. Its action changes the current
save's display-mode override through the common settings path, leaving global defaults and other
saves unchanged. Returning that row to Use global resumes inheritance. No display action is taken
at the main menu, in world view, while typing or during a gravship cutscene.

Hidden, fog protection, the zoom limit, screenshot hiding and invalid placement always win over
an individual Show choice. Otherwise individual show/hide overrides the applicable room/zone and
growing/stockpile preference; Default follows those preferences. With overlapping rendered room
and zone labels, Hide room (default) suppresses only the intersecting room label, Hide zone
suppresses only the intersecting zone label, and Show both retains both. Same-kind overlaps are
not suppressed by this setting. Merely containing a zone does not hide a room label.

#### Scenario: Current-save visibility cycle
- **WHEN** a player cycles the bottom-right control from Visible through On top and Hidden
- **THEN** text layering and visibility follow those modes and the control shows the state
- **AND** another save and the global default retain their previous choices

#### Scenario: Individual visibility and stronger limits
- **WHEN** a zone is explicitly Show while zone labels are generally off
- **THEN** it is displayed if otherwise eligible, but remains absent in Hidden mode, fog, excessive zoom or an active hidden screenshot workflow

#### Scenario: Actual overlap only
- **WHEN** a room contains a zone whose rendered label does not intersect the room's rendered label
- **THEN** both remain visible even under Hide room
- **AND** moving the zone label to overlap invokes the selected room/zone overlap preference

### Requirement: Presentation preferences use common inheritance and readable text
The feature SHALL use the common global default, Current save and Use global model for all
presentation preferences. New or missing save entries use global; inherited values follow later
global edits. Dormant overrides survive Use global, and all save mutation controls are disabled at
the main menu. Valid changes affect the current display without restart; resets use the same scope
and never erase individual labels. Disable subordinate zone controls without erasing their values.

Built-in defaults and allowed values are:

| Preference | Built-in default | Values |
| --- | --- | --- |
| Display mode | Visible | Visible / On top / Hidden |
| Font style | Classic | Classic, regular/light/bold monospace, medieval, typewriter; preview each available style |
| Default text color | White | Valid color selection; opacity is controlled separately |
| Opacity | 75% | Whole percentages, 1–100 |
| Maximum text scale | 1.0 | 0.1–5.0 |
| Minimum text scale | 0.2 | 0.1–1.0, never greater than maximum |
| Furthest visible zoom | Furthest | Available native zoom categories |
| Room / zone / growing / stockpile labels | All on | On / off; growing and stockpile preferences depend on zone display |
| Placement | Classic | Classic / Smart |
| Room-zone overlap | Hide room | Hide room / Hide zone / Show both |
| Hide during supported screenshots | Off | On / off |

Reject invalid edits without changing prior effective values. Missing or invalid stored fields
recover to documented defaults, and an invalid minimum/maximum pair recovers both together to
0.2/1.0 with one bounded diagnostic. Fresh creation, missing-field loading and reset agree.
Minimum and maximum form one paired setting with one Use global choice and two value controls;
they inherit, override, reset and restore a dormant override together. Every scope validates the
complete pair. A global edit cannot combine with half of a save override or rewrite that override.
Custom color uses the same opacity. Missing optional fonts fall back to a readable supplied style
with a bounded explanation. Text in the seven baseline UI languages, including accented Latin and
Cyrillic, and supported Chinese, Japanese kana and Korean labels must remain legible through font
fallback. Unsupported characters retain a visible replacement marker rather than silently vanishing;
their saved text is preserved. This is not a promise of complete Unicode coverage or translation
of player-entered names. Owned controls and explanations are localized in all seven baseline languages.

#### Scenario: Inheritance and main-menu protection
- **WHEN** the global color changes for an inheriting save and an overriding save
- **THEN** only the inheriting effective color follows that change, with individual colors still taking precedence
- **AND** at the main menu only global edits can be applied, including through reset actions

#### Scenario: Consistent defaults and invalid ranges
- **WHEN** preferences are created, missing on load, reset, or contain an invalid scale pair
- **THEN** documented defaults are used consistently, with no minimum larger than the maximum
- **AND** rejected interactive edits retain their previous displayed and saved values

#### Scenario: Temporarily disable zone labels
- **WHEN** the player disables and later re-enables the general zone preference
- **THEN** the prior growing and stockpile choices are restored rather than both becoming off

#### Scenario: Scale bounds inherit together
- **WHEN** a save overrides its size pair to 0.8/1.0 and the global pair changes to 0.2/0.5
- **THEN** the save keeps 0.8/1.0; choosing Use global applies the complete 0.2/0.5 pair
- **AND** unchecking Use global or reloading the overriding save restores the valid 0.8/1.0 pair, never 0.8/0.5

#### Scenario: Mixed scripts and missing resources
- **WHEN** a label combines Latin, Cyrillic and supported East Asian text, or its preferred font is unavailable
- **THEN** supported text remains readable through admitted fallbacks and any unsupported character has a visible marker
- **AND** the saved name is not shortened or corrupted by a missing glyph or resource

### Requirement: Label state survives ordinary map lifecycle without leaking between games
The feature SHALL save each actual customization independently, preserving the difference between
Default and an explicit value, including white color, Show, Hide and direction. Zone state follows
the same native zone through resizing and reload, and is discarded after actual deletion rather
than copied to a newly created unrelated zone. A room customization follows the cell at which it
was first created. A split retains it with that cell; a temporary absence of a room there leaves
the record dormant until a valid revealed room returns or the map is removed.

If rooms with customizations merge, the earliest-created customization is displayed; other records
remain dormant and return to their own anchor regions after a later split. Editing the merged room
edits its displayed record. Confirmed Reset for the merged room clears all its contained records,
so hidden older names cannot unexpectedly return after that reset. Loading, changing maps, changing
language or loading another game rebuilds the correct current presentation without stale names,
fonts, positions or drafts. Hidden mode does not erase labels.

#### Scenario: Reload explicit overrides
- **WHEN** the player saves and reloads a white label under a nonwhite default with forced vertical orientation
- **THEN** its white override and direction survive, while a different Default label still inherits

#### Scenario: Split, merge and reset
- **WHEN** two customized rooms merge, later split, then merge again and the player applies Reset
- **THEN** the earliest-created record wins during the first merge and each anchor regains its record after splitting
- **AND** the confirmed merged-room reset prevents either old record returning after another split

#### Scenario: Delete and recreate a zone
- **WHEN** a customized zone is resized, deleted and replaced by a newly created zone in the same cells
- **THEN** resizing preserves the old customization, but the new zone does not inherit the deleted zone's identity or label

#### Scenario: Switch saves and languages
- **WHEN** the player loads another game or changes the interface language
- **THEN** labels and automatic translated names reflect that game's current entities and language
- **AND** no prior game's draft, removed map or cached text appears

### Requirement: Optional travel and screenshot behavior follows the actual workflow
The feature SHALL preserve supported room and stockpile/growing-zone customizations through native
Odyssey gravship travel and save/load while in transit. A room record travels only when its anchor
travels; a zone record follows the zone actually transferred by the game, including native handling
of a zone partially intersecting the ship. Labels do not change which cells, zones or buildings
travel. After landing, automatic placement is recomputed in the destination; explicit horizontal/
vertical direction and offsets remain relative to destination map axes. No departure-map ghost
label or destination duplication is produced. Cutscenes never display these labels.

When Hide during supported screenshots is on, labels are absent while native screenshot mode or
an admitted Progress Renderer capture is active and resume afterward without changing saved
visibility. With the option off, ordinary label visibility applies. An unavailable or incompatible
optional capture integration does not claim to hide its captures, warns once if enabled for a
present provider, and leaves native screenshot handling and ordinary labels usable. Odyssey or
Progress Renderer absence is normal and does not generate a missing-provider failure warning.

#### Scenario: Gravship transfer and transit reload
- **WHEN** customized rooms and native transferred zones launch, the game is saved in transit, and the ship lands rotated on another map
- **THEN** their customizations appear only on the correct destination entities with recomputed placement
- **AND** forced direction and offsets retain their map-axis meanings, while untransferred anchors remain on the departure map

#### Scenario: Screenshots restore the prior view
- **WHEN** the player enters and exits native screenshot mode or starts and finishes an admitted provider capture with hiding enabled
- **THEN** labels are absent during that workflow and afterward return according to the unchanged current visibility preferences
- **AND** no extra capture, file output or camera change is initiated by the label feature

#### Scenario: Unsupported optional capture
- **WHEN** an installed capture provider cannot supply an admitted active-capture indication
- **THEN** its integration is marked unavailable with a bounded explanation, without suppressing ordinary labels forever
- **AND** native screenshot hiding still works when enabled

### Requirement: Compatibility failure preserves ordinary game behavior and saved intent
The feature SHALL apply each game integration only after its documented minimal target conditions
are checked at startup. Compatible unrelated edits and other patches are allowed. An unknown,
changed or failed condition leaves the affected integration or declared group inactive, preserves
native drawing/input/zone/travel behavior and saved customizations, and logs one bounded startup
warning per skipped enabled patch naming the affected behavior and fallback. Later supported
repatches are rechecked; failed installation rolls back only newly added owned work.

When original Labels on Floor is active, owned overlapping rendering, input and automatic native
rename synchronization are inactive, with one clear explanation; existing owned label records are
retained. Before resuming a zone label with an active custom-name override, reconcile its native name against the last name
applied or observed by the owned feature. A different current native name wins, updates the custom
floor name and becomes its remembered reset baseline; retain the other appearance choices. Records
with appearance-only overrides keep automatic naming and do not acquire custom text on resumption. Deleted
or unresolved entities cannot resume against a different zone. No source package is impersonated
or removed, and no source save/settings are silently
imported. External label/font APIs and arbitrary screenshot providers are not claimed as drop-in
compatible without their own consumed-contract evidence. Other megamod features remain usable.

#### Scenario: Benign edits and unsafe target changes
- **WHEN** another patch makes a compatible unrelated change, or instead removes a required integration condition
- **THEN** the compatible case remains eligible, while the unsafe integration stays inactive with its stated native fallback and bounded startup warning
- **AND** neither case removes another mod's patches or modifies native storage, crop or room mechanics

#### Scenario: Original provider and owned records
- **WHEN** original Labels on Floor is active beside megamod
- **THEN** only the original supplies overlapping labels and input, and owned records remain saved without migrating or overwriting source state
- **AND** deactivating the source in a later supported session permits owned records to resume after current compatibility checks

#### Scenario: Resume after a rename while suppressed
- **WHEN** an owned FOOD zone label is suppressed by source co-installation, renamed natively to MEDICINE,
  saved and reloaded, and later resumes after source deactivation
- **THEN** the floor label follows the current MEDICINE name while retaining its owned appearance choices
- **AND** clearing its custom name keeps MEDICINE as the ordinary zone name rather than restoring FOOD or an older baseline

#### Scenario: Appearance-only records remain automatically named
- **WHEN** a growing zone has only a custom color, is renamed natively while owned labels are suppressed,
  and later resumes after save/reload and source deactivation
- **THEN** it retains the custom color and displays its current crop rather than acquiring a custom zone-name label
- **AND** a later crop change still updates the floor text automatically
