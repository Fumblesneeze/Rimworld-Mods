# Monument quest labels

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S101/P2-31 replacement, observable draft only; implementation and native visual acceptance pending.

## ADDED Requirements

### Requirement: A placed monument displays its actual ongoing quest information

With monument labels enabled, the system SHALL display the current name of each known, player-visible
ongoing quest associated with a placed native monument marker, together with its applicable current
construction or preservation obligation. Names SHALL preserve the quest's spelling and case.
The association SHALL follow the actual marker and quest, including replacement markers, rather
than unrelated digits or the ordering of marker tags. A packed marker, placement preview, unrelated
building or ordinary ruin SHALL NOT receive a placed-monument label.

Multiple markers or multiple ongoing quests SHALL retain their own associations. Duplicate links to
the same obligation SHALL NOT duplicate the line. Unknown, hidden, unaccepted or ended quests SHALL
NOT be presented as active known obligations. A known ongoing quest with an unsupported/ambiguous
deadline SHALL keep its identifiable name and an explicit **Timing unavailable — see quest** cue;
the display SHALL NOT guess a deadline or report that demolition is safe.

#### Scenario: Place a quest monument
- **WHEN** the player places the marker for an accepted monument quest
- **THEN** its quest name and applicable obligation appear beside that placed monument without selecting it
- **AND** the packed marker and placement preview do not leave labels at earlier locations

#### Scenario: Replacement marker and reordered tags
- **WHEN** a replacement marker is placed or unrelated metadata changes the order of its quest associations
- **THEN** it displays the same actual quest obligation and remaining deadline
- **AND** no unrelated quest is selected merely because a tag contains its number

#### Scenario: Multiple monuments and quests
- **WHEN** the map contains multiple quest monuments or a marker has more than one genuine ongoing quest obligation
- **THEN** each label identifies the relevant quest and displays its own obligations without duplicate lines
- **AND** another monument's deadline is not substituted

#### Scenario: Missing or unsupported association
- **WHEN** a marker has no known ongoing quest, or its known quest has an ambiguous deadline
- **THEN** the first case has no quest label and the second has the known name with the timing-unavailable cue
- **AND** other valid monuments remain labelled without repeated error messages

### Requirement: Build, Protect and Keep reflect active obligations and actual remaining time

The system SHALL distinguish **Build** (time to finish construction), **Protect** (active threatened
protection after construction) and **Keep** (active retention obligation after construction). It SHALL
support native basic monument, timed-protection monument, monument decree and change-royal-heir
workflows. A decree's ordinary retention phase SHALL be labelled Keep even when it begins on the
same construction-completed event as a Protect obligation elsewhere.

Only started, still-active obligations SHALL supply running countdowns. A future/inactive, completed,
disabled or unrelated quest timer SHALL NOT be used. Remaining time SHALL match the actual quest
deadline with native localized whole-period formatting, without seconds, decimals or years, following
the source's display granularity. Positive time below the smallest displayed period SHALL read
**Less than 1 hour**, not a misleading finished state. A due obligation awaiting the game's ordinary
transition SHALL show **Due now**, not a negative interval or a claim that it is safe to demolish.
Confirmed untimed active obligations SHALL be identified as having no timed deadline; unsupported
timing SHALL use the unavailable cue instead.

For a native monument decree, the initial construction deadline and later forgotten/expiry deadline
SHALL remain distinct. After the initial construction deadline has passed while the decree remains
buildable, the label SHALL read **Build: overdue** and show the separately identified **Decree
forgotten in** countdown when that later timer is active. It SHALL NOT reset Build to the later
expiry timer, retain Due now indefinitely or treat this known overdue phase as untimed/unsupported.
This status follows actual native state; the normal 15-day construction and 80-day forgotten limits
are native baseline examples, not new fixed durations imposed by the overlay.

#### Scenario: Finish construction and begin protection
- **WHEN** the game recognizes a timed-protection monument as complete
- **THEN** Build is replaced by the actual started Protect countdown
- **AND** a pre-existing but previously inactive protection timer was not shown as running before completion

#### Scenario: Retain a basic, decree or heir monument
- **WHEN** construction enters the native basic, decree or change-royal-heir retention phase
- **THEN** the label reads Keep with that phase's actual remaining time
- **AND** it does not relabel a decree as Protect merely because construction completion started its timer

#### Scenario: Other quest timers coexist
- **WHEN** the same quest contains unrelated delays or more than one verified current monument obligation
- **THEN** unrelated delays are ignored and each verified obligation remains identified
- **AND** a timer is not selected merely because it was the last one created

#### Scenario: Deadline boundary and paused time
- **WHEN** positive remaining time falls below an hour, then reaches its due tick, or the player pauses
- **THEN** the display progresses from Less than 1 hour to Due now, never a negative countdown
- **AND** pausing holds the remaining game time while leaving the current label readable

#### Scenario: A decree is finished late
- **WHEN** an unfinished native monument decree passes its initial 15-day deadline and remains active
- **THEN** the display changes to Build: overdue with its separate remaining time until the 80-day forgotten limit
- **AND** completing construction later replaces both construction/forgotten lines with the newly active Keep obligation, without changing native consequences

#### Scenario: An overdue decree is forgotten
- **WHEN** the game's later forgotten deadline ends an unfinished decree
- **THEN** its active annotation disappears with the quest's actual end
- **AND** the overlay neither extends the deadline nor declares the monument completed

### Requirement: Labels remain current through map and quest lifecycle changes

Labels SHALL reflect marker placement, relocation/rotation, replacement, destruction, quest naming,
phase changes and termination by the next rendered map update after the game has made that change
available. Reading the label SHALL NOT complete or tick a quest on its own. Countdown text SHALL
refresh when its displayed interval changes, including while the map is viewed at a different game
speed. Save/load and switching games/maps SHALL reconstruct labels from current native state,
including when loaded paused, without retaining another game's marker references or stale text.

Only the currently rendered local map SHALL show its monument labels. World, travel/world overlays,
main menu and unrelated colony maps SHALL not show labels from another surface. A destroyed or
unplaced marker SHALL immediately lose its label; a historical/failed/completed quest SHALL lose
its active label even if the physical monument remains. Labels SHALL NOT reveal hidden quest
information, unknown markers or text positioned in unrevealed terrain.

#### Scenario: Switch colony and world views
- **WHEN** the player switches from a colony to another map, the world view or a shuttle travel view
- **THEN** only the appropriate current local map's labels remain visible
- **AND** returning shows current information without needing game time to advance

#### Scenario: End a quest or destroy a marker
- **WHEN** the quest ends, fails or becomes historical, or its marker is destroyed/unplaced
- **THEN** its former active labels disappear on the next map update
- **AND** a later replacement uses its current association rather than retaining the old label position

#### Scenario: Load paused or start another game
- **WHEN** a save is loaded paused or a different game is started in the same application
- **THEN** labels are built from that game's current markers, quests and obligations before presentation
- **AND** no earlier game's labels or countdowns carry over

### Requirement: Monument annotations are readable and do not obstruct ordinary play

The quest-name block SHALL prefer the map-north side of the monument footprint and its obligation
block the map-south side, approximately one cell outside the footprint as in the source. Default
presentation SHALL use white text at **50% opacity**, with uniform scaling in the source's
**0.5–1.2** range when it fits. Actual rotated footprint and full rendered text bounds SHALL govern
placement. At edges or with long/localized names, the system SHALL use bounded nearby placement,
wrapping or an explicit ellipsis instead of distorted glyphs, text outside the map or illegible
compression below the minimum. Full quest information remains available through ordinary native
quest/marker controls; the label itself adds no gameplay click action.

Labels SHALL remain readable for the seven baseline UI languages, including supported Cyrillic,
Chinese and Japanese text; unsupported characters SHALL have a visible fallback rather than silently
becoming spaces. Rendering SHALL NOT force names to uppercase or reinterpret their content as
markup commands. Offscreen culling SHALL keep a partly visible label when its visible text is in the
viewport. Competing monument labels SHALL be placed without obscuring one another where space allows;
if a readable in-bounds unfogged placement is unavailable, omit the affected block and retain native
information access. Existing floor/room/zone labels and their settings SHALL remain independent.

#### Scenario: Rotated or edge monument
- **WHEN** the player places a rotated monument near the map edge
- **THEN** annotations follow its actual footprint and remain on the map in revealed space
- **AND** any block that cannot be placed readably is omitted without a malformed mesh or off-map text

#### Scenario: Long localized name
- **WHEN** a quest name is long or contains supported accented, Cyrillic or East Asian text
- **THEN** it remains legible through wrapping/fallback or an explicit abbreviated label
- **AND** its original full name and normal quest access remain unchanged

#### Scenario: Partial viewport and adjacent monuments
- **WHEN** a label's starting point leaves the viewport while some text remains visible, or monuments are close together
- **THEN** visible text is not culled solely because its starting point is outside
- **AND** placement avoids overlapping monument text where possible without changing another feature's label state

### Requirement: The view toggle uses common settings and has no gameplay effects

The system SHALL offer a small bottom-right **Monument labels** toggle, enabled by default, and the
same setting under **Display → Map labels → Monuments**. The toggle SHALL write the current save's
explicit override through the common settings path, leaving the global default and other saves
unchanged. The settings row SHALL show Global default, Current save and Use global; new/missing
entries inherit, inherited save controls are inactive, dormant overrides survive Use global, and
main-menu save controls/Use global are always inactive. Changes SHALL take effect immediately,
including while paused. No new bottom tab, debug command, monument editor or separate appearance
settings panel SHALL be added by this slice.

When Royalty/native monument content is unavailable, the feature SHALL explain its unavailability
without breaking other megamod functionality. Global preference remains editable for future games;
the unavailable in-game toggle SHALL not pretend to enable missing content. Hiding labels SHALL
NOT cancel quests, alter timers, allow destruction, change rewards/penalties, rename native objects,
change construction blueprints/jobs or affect the independent floor-label display mode.

#### Scenario: Toggle while paused
- **WHEN** the player hides or shows monument labels with the bottom-right control while paused
- **THEN** the map display changes immediately and the corresponding save setting shows the explicit override
- **AND** quest progress and floor-label visibility remain unchanged

#### Scenario: Inheritance and main menu
- **WHEN** the player changes a global default, restores Use global, switches saves or opens settings at the main menu
- **THEN** inherited values follow the current global default and explicit save overrides remain isolated
- **AND** dormant overrides are retained and main-menu save edits are disabled

#### Scenario: Content is absent
- **WHEN** Royalty/native monument content is unavailable
- **THEN** no monument overlay is created and its unavailable state is explained
- **AND** other megamod features and editing the global display preference continue to work

### Requirement: Unsupported providers fail without interfering with native quests

With original Monument Highlight active, owned overlapping labels SHALL be inactive with a bounded
explanation; the original SHALL remain untouched. Compatible unrelated game changes SHALL remain
allowed. Each enabled external patch SHALL apply only when its documented minimal behavior contract
holds. Failed or ambiguous admission SHALL leave its affected patch/atomic owned feature inactive,
retain native quest/marker access and log one bounded startup warning per skipped enabled patch
identifying the failed condition, unavailable display and fallback. No foreign patch SHALL be removed.

Unsupported custom quest or timing content SHALL degrade per marker/obligation where it can safely
be isolated, preserving supported labels elsewhere. It SHALL NOT disable native quest progress or
produce repeated per-frame/tick errors. No source API, font-provider, save-schema or arbitrary custom
quest compatibility SHALL be claimed merely because the source exposed public classes.

#### Scenario: Co-install the original
- **WHEN** original Monument Highlight is active
- **THEN** it is the only provider of the overlapping labels and the owned feature reports inactivity
- **AND** its settings, assemblies and rendering remain untouched

#### Scenario: One unsupported quest
- **WHEN** one monument has unsupported timing while another uses a supported native quest
- **THEN** the unsupported label uses the known-name/unavailable fallback and the supported label continues
- **AND** native quest selection, deadlines and rewards remain unchanged for both

#### Scenario: Native hook changes
- **WHEN** a required rendering or observation contract is incompatible, or only an unrelated benign edit is present
- **THEN** the incompatible owned contribution stays inactive with a bounded warning while the benign edit remains supported
- **AND** ordinary quest/marker information remains usable without removal of any foreign patch
