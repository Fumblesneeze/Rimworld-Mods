# Initial nocturnal schedules

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld 1.7 beta**.
Origin S070 Automatic Night Owl replacement. Observable draft only; nothing implemented or accepted.

## ADDED Requirements

### Requirement: Qualifying player pawns receive one initial nocturnal schedule
The feature SHALL consider living player-faction humanlike pawns with supported schedules after starting arrival, a new map arrival, successful recruitment or successful enslavement, including a ready schedule on an unspawned recruit.
A Night Owl trait qualifies. Enabled optional criteria also qualify a pawn whose current ideology
prefers darkness or whose active genes dislike sunlight, when the corresponding DLC is available.
One or multiple matches have the same single result, subject to customization preservation below.
Prisoners, animals, mechs, subhumans, visitors outside the player faction and unselected starting
candidates do not receive this automation. Children with usable schedules retain normal care rules.

#### Scenario: Starting night owl
- **WHEN** a selected starting Night Owl pawn becomes available with an ordinary uncustomized schedule
- **THEN** it receives the configured initial nocturnal schedule once, whether arrival is standing, by pod or by supported gravship
- **AND** unselected starting candidates are unchanged

#### Scenario: Successful versus failed recruitment or enslavement
- **WHEN** recruitment or enslavement actually makes an eligible pawn player-owned with a ready schedule
- **THEN** the feature considers that pawn once without requiring a later map respawn
- **AND** a failed attempt, will reduction or recruitment into a foreign faction does not qualify it

#### Scenario: Actual darkness and sunlight behavior
- **WHEN** a pawn without Night Owl has an enabled darkness-preferring ideology or an active sunlight-disliking gene
- **THEN** it qualifies without requiring a particular displayed meme or xenotype name
- **AND** a suppressed gene, a name alone or unavailable DLC is insufficient

#### Scenario: Multiple or absent qualifying reasons
- **WHEN** a pawn has several qualifying reasons, or has none
- **THEN** several reasons produce one schedule decision while no reasons leave the schedule unchanged
- **AND** missing optional pawn information does not invalidate an independently qualifying Night Owl trait

### Requirement: The template is a complete native schedule with explicit time bounds
The feature SHALL use a 24-hour local schedule with Sleep during the configured consecutive window and Anything during all other hours.
The built-in start is **11:00** and duration **8 hours**, covering 11:00 up to but excluding 19:00.
Configuration accepts whole start hours **0–23** and whole durations **1–24 hours**. A window crossing
midnight continues cyclically. Invalid edits are rejected with an explanation and retain the last valid
window. This changes scheduled permissions, not sleep need, mood rules, movement or emergency behavior.

#### Scenario: Default window
- **WHEN** a qualifying uncustomized pawn is initialized with built-in settings
- **THEN** hours 11 through 18 display Sleep and the other sixteen hours display Anything
- **AND** normal work, recreation, care and emergency rules determine what the pawn actually does

#### Scenario: Midnight and full-day windows
- **WHEN** the initial window starts at 22:00 for 8 hours
- **THEN** Sleep covers 22:00 through 05:59, and Anything covers 06:00 through 21:59
- **AND** a duration of 24 hours covers the whole day regardless of start, while duration 1 covers exactly the selected hour

#### Scenario: Invalid configuration
- **WHEN** a proposed start or duration is outside its allowed whole-hour range
- **THEN** the edit is rejected visibly and neither global nor save configuration is partially changed

### Requirement: Customized schedules and later edits remain authoritative
The feature SHALL preserve a pawn's customized schedule instead of overwriting it on first consideration, and SHALL preserve all subsequent player edits after a schedule has been assigned or preserved.
A schedule differing from the ordinary initial pattern—Sleep 22:00–06:00, Anything otherwise—is
treated as custom. An observed manual edit, timetable paste or supported provider choice also protects
it even when its final values match that pattern. Historical baseline-looking choices made before
tracking existed cannot be inferred; this limit is explained in settings. Preserved pawns remain
decided, rather than being reset automatically after a later change.

#### Scenario: Custom schedule before first consideration
- **WHEN** a newly arriving or recruited qualifying pawn already has a nondefault schedule
- **THEN** every assignment remains unchanged and future automation continues to preserve that pawn
- **AND** a provider-created work or recreation block is not replaced with Anything

#### Scenario: Player returns to the baseline pattern
- **WHEN** a player edits or pastes a timetable while this feature is active, ending with the ordinary initial pattern
- **THEN** that explicit choice remains protected from later initial automation
- **AND** viewing or copying a timetable without assigning it does not by itself protect the recipient of no edit

#### Scenario: Edit after automatic assignment
- **WHEN** the player paints or pastes a different schedule after automatic initialization
- **THEN** those assignments survive save/load, map travel, leaving and rejoining the colony, and later template changes
- **AND** the player continues editing through ordinary Schedule controls without a separate per-pawn mod setting

### Requirement: Load and changing eligibility do not cause repeated scheduling
The feature SHALL restore its prior schedule decisions before considering loaded pawns and SHALL never treat a decided pawn as new merely because of a load, respawn or eligibility change.
When first added to an existing save, it can initialize a qualifying loaded pawn's baseline schedule;
customized schedules remain preserved. Existing off-map pawns wait for a supported lifecycle event.
Changing a trait, gene, ideology or configuration alone does not immediately reschedule anyone.
An undecided, unedited pawn can first qualify at a subsequent supported arrival/control event.

#### Scenario: Load an existing game for the first time
- **WHEN** this feature first encounters a loaded qualifying pawn with no prior feature decision
- **THEN** an ordinary baseline schedule can receive the initial template while a custom schedule is preserved
- **AND** an indistinguishable historical baseline choice is treated as baseline, as explained in settings

#### Scenario: Reload after a prior decision
- **WHEN** a pawn already assigned or preserved by the feature is loaded or arrives on another map
- **THEN** its restored schedule remains unchanged, including edits made since that decision

#### Scenario: Later qualification on an undecided pawn
- **WHEN** a previously unqualified, unedited pawn acquires a qualifying reason
- **THEN** its timetable does not immediately change
- **AND** its first subsequent supported arrival/control event can initialize it if its schedule still qualifies for initialization

#### Scenario: Lose eligibility or change settings after initialization
- **WHEN** a decided pawn loses Night Owl, changes ideology or genes, or the player changes optional criteria or the sleep window
- **THEN** its existing schedule is retained without reversing or reapplying the prior template

### Requirement: Automatic writes are atomic and failures preserve the schedule
The feature SHALL retain the original complete timetable and prior decision as a rollback snapshot before an automatic change, apply the full change together, and verify the resulting assignments and decision.
Failure restores the original timetable and decision, explains that the operation was unavailable,
and does not report success or retry repeatedly. Malformed or unreadable known history is not treated
as a fresh unhandled pawn. Native schedule editing remains available.

#### Scenario: Assignment cannot be completed
- **WHEN** an automatic assignment fails before its schedule and decision are successfully committed
- **THEN** all prior hourly assignments and the prior decision are restored together
- **AND** the failure is explained without leaving a partial day or repeated work-time warnings

#### Scenario: Prior history is unreadable
- **WHEN** existing feature history cannot be read reliably for an affected save or pawn
- **THEN** affected automatic decisions are unavailable with a bounded explanation and existing schedules remain untouched
- **AND** this does not prevent the player from using ordinary Schedule controls

### Requirement: Configuration uses the shared settings model and native gameplay surfaces
The feature SHALL expose Include darkness-preferring ideologies, Include sunlight-disliking genes and Initial sleep window in Pawns / Initial schedules using the common settings model.
Both optional criteria default **on** consistently. Start and duration form one scoped window value.
Every row shows Global default, Current save and Use global; new save choices inherit live globals.
Accepted changes affect future undecided candidates only. No extra bottom tab, debug command,
operational toggle or per-pawn inheritance control is introduced. All new text uses seven baseline languages.

#### Scenario: Inheritance and dormant override
- **WHEN** a save uses global settings
- **THEN** it follows current global values, its own editor is inactive and any former override remains stored
- **AND** unchecking Use global restores that override or seeds a first override from the effective global value, without changing other saves

#### Scenario: Main menu and save switching
- **WHEN** settings are opened at the main menu
- **THEN** all save-specific controls and mutation actions are inactive with an explanation while available global controls remain usable
- **AND** entering or switching saves uses that save's own preference and prior pawn decisions

#### Scenario: Optional DLC unavailable
- **WHEN** the required DLC or supported content for one optional criterion is unavailable
- **THEN** its row explains the unavailable behavior and retains its stored value without claiming that criterion is active
- **AND** independently supported qualification and native schedules remain usable

#### Scenario: Schedule and child care remain native
- **WHEN** an initialized pawn's timetable is viewed, or a child's assigned bedtime is used by care behavior
- **THEN** existing Schedule and schedule-intent displays use the same actual assignments
- **AND** temporary directed recreation, medical needs and safe child care do not rewrite the schedule or count as new initialization

### Requirement: Compatibility admits supported behavior and identifies the active provider
The feature SHALL apply automatic writes only when their documented lifecycle, timetable, history and choice-preservation conditions are satisfied, allowing compatible changes from other mods.
Optional criteria can be unavailable independently; required core preservation failures leave owned
automatic writing inactive. Each skipped enabled patch produces one bounded startup warning naming
the failed condition, unavailable behavior and native fallback. Original-provider authority and
unsupported integrations are explained without discarding preferences or changing another mod's settings.

#### Scenario: Core or optional admission failure
- **WHEN** a required core condition fails, or only an optional eligibility criterion becomes unsupported
- **THEN** the affected owned behavior remains inactive with the specified explanation
- **AND** existing timetables remain untouched while ordinary Schedule controls and unrelated admitted behavior remain usable

#### Scenario: Original Automatic Night Owl is active
- **WHEN** the original selected source is active alongside the megamod
- **THEN** it alone supplies the overlapping automation and owned preferences remain dormant
- **AND** settings explain that the source governs schedules with its own overwrite/default behavior, rather than promising owned preservation corrections

#### Scenario: Optional custom schedule provider
- **WHEN** a supported provider supplies a custom initial schedule or an explicit schedule choice
- **THEN** that choice is preserved without a second automatic template or rejection merely because the provider exists
- **AND** unsupported provider paths are left to that provider with their scope documented, without a blanket compatibility claim
