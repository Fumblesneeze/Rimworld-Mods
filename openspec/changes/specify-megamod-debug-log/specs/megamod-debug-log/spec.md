# Debug log

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. These are observable requirements for future work, not implemented
behavior. Source evidence and technical admission plans belong in the adjacent design and research.

## ADDED Requirements

### Requirement: Diagnostic access follows the developer UI
The mod SHALL use the existing top developer-toolbar log entry and native log shortcut to open one diagnostic window, without adding a bottom tab, bottom-right button, or pause/main-menu log button.

These manual entry points are available with developer mode enabled, including at the main menu
and in a loaded game's map or world view. Turning developer mode off removes owned diagnostic
controls and announcements. Native automatic error-window behavior remains governed by native
preferences; this feature does not conceal an automatically reported startup failure.

#### Scenario: Open and close the log during play
- **WHEN** the player uses the top log entry twice with developer mode enabled
- **THEN** the first action opens the native diagnostic window and the second closes it, without opening another copy or a gameplay tab

#### Scenario: Developer mode is off
- **WHEN** developer mode is disabled
- **THEN** no owned manual log entry or diagnostic announcement is shown, and the game's ordinary error-reporting behavior remains available

### Requirement: Filters change the visible log without deleting evidence
The mod SHALL preserve the native Message, Warning and Error visibility controls, initially all shown in a new process, with each category identifiable independently of its color.

Filtering affects only the displayed list, not capture, file output, repeat counting, announcements,
or Copy all. Toggling a category back on reveals its retained records in native order. Hiding the
selected record's category clears its detail selection. Filter state and selection are transient
process/window state, not save settings; closing/reopening the window retains the filters.

#### Scenario: Hide and restore warnings
- **WHEN** the player hides warnings, another warning is recorded, and the player shows warnings again
- **THEN** retained old and new warnings reappear in order, with their full details and native repeat counts

#### Scenario: Hide the selected category
- **WHEN** the selected error is hidden by the Error filter
- **THEN** the list and details no longer present it as selected, without deleting it from the retained log

### Requirement: Inspection and copy retain complete native diagnostic content
The mod SHALL preserve native record selection, full message and stack-trace inspection, draggable detail-pane sizing, native trace-size controls, and Copy all of the currently retained log.

Short list previews and presentation colors must not truncate or rewrite copied text or full
details. Copy all includes records hidden by display filters, uses the native retained-record
format, and does not promise messages already evicted or suppressed. Clear empties the retained
log and selection and resets the logging interval, without deleting existing files or resetting
native once-only message identities. The native rolling history capacity remains unchanged; a
capture limit is not a larger scrollback history. With the inspected baseline this is 1,000 rows.

Native Auto-open and Pause on error controls retain their ordinary effects and native state,
without a second owned setting. Display filters/colors do not suppress those effects; explicit
muting and stopping of ordinary capture have the separate consequences specified below.

#### Scenario: Inspect a long filtered log
- **WHEN** the player selects a long retained message, resizes the detail pane, and copies all while another category is hidden
- **THEN** complete message text and trace are inspectable, and copied output includes hidden retained records without visual truncation or reformatted numbers

#### Scenario: Clear without deleting files
- **WHEN** the player clears the log after it has reached the configured capture limit
- **THEN** the retained list and selection clear and new eligible messages can be captured, while existing log-file content remains intact

#### Scenario: Filtered errors retain native pause behavior
- **WHEN** Error display is hidden, ordinary error capture is active, and native Pause on error is enabled during play
- **THEN** an eligible new error still pauses the game through the native behavior despite being hidden in the list

### Requirement: Severity colors affect presentation rather than source text
The mod SHALL provide separate Message, Warning and Error colors, defaulting to native white, yellow and native error red, and an Ignore embedded colors option enabled by default.

The palette is shared by log rows and owned diagnostic announcements. When Ignore embedded colors
is enabled, embedded color choices cannot override the severity color in those displays. Other
text, formatting and the original message/trace remain intact. Disabling the option permits native
embedded colors. Changing a palette or this option updates retained rows on the next redraw.
Color choices are opaque; category labels/icons continue to identify severity when colors coincide.

#### Scenario: A mod supplies a differently colored warning
- **WHEN** a warning contains an embedded green color and Ignore embedded colors is enabled
- **THEN** its diagnostic display uses the configured warning color, while copying it retains the original message content

#### Scenario: Change colors with retained messages
- **WHEN** the player changes the Error color or disables Ignore embedded colors
- **THEN** existing and future error rows use the effective presentation on the next redraw without recapturing the messages

#### Scenario: Permit embedded announcement colors
- **WHEN** Ignore embedded colors is off and an eligible announced warning contains a native embedded color
- **THEN** both its log display and announcement summary permit that color, without changing the raw captured text

### Requirement: Muting is an explicit loss of future diagnostics
The mod SHALL provide separate global Mute messages, Mute warnings and Mute errors controls, all off by default, clearly distinguished from the window's visibility filters.

Muting applies to future eligible messages through RimWorld's standard message/warning/error
reporting channel, including mod calls through that channel. A muted occurrence is not recorded
in that channel's retained log or file output, does not count toward the owned capture limit, and
does not trigger its ordinary announcement, auto-open or pause-on-error consequences. Already
retained/file-written messages remain. Re-enabling capture does not recover lost occurrences or
reset native once-only identities. Independent direct file writers and unrelated host channels
are outside this control, and the UI explains this boundary.
Required startup warnings and the limit-reached control notice are the explicit exceptions below.

#### Scenario: Muting differs from filtering
- **WHEN** warnings are muted and a new eligible ordinary warning is emitted
- **THEN** it cannot be recovered by showing warnings or Copy all, while earlier retained warnings remain available

#### Scenario: Resume a muted category
- **WHEN** the player unmutes errors and a new otherwise eligible error occurs
- **THEN** normal capture and native error consequences resume, without replaying errors discarded while muted

### Requirement: Capture limits are separate from retained history
The mod SHALL offer global capture-limit modes Game policy, Custom and Unlimited, defaulting to Game policy; Custom has an initial value of 2,000 and accepts whole numbers from 1 through 10,000.

Game policy leaves the current game's stopping policy intact and does not infer a functional limit
from a number found in the game. Custom admits the first N eligible ordinary occurrences in the
controlled channel during an interval, then stops further ordinary capture to that channel's log
and file and records one limit-reached notice. Repeats count as occurrences even if combined into
one displayed row; muted or natively rejected once-only calls do not count. Required startup
diagnostics are exempt. Unlimited removes the count-based stop for this channel, not ordinary
mute settings, once-only rules or the rolling history bound.

On capturing the Nth ordinary occurrence, Custom records exactly one warning-category control
notice in the retained log and file, explaining the limit and how to resume. That notice bypasses
owned muting and the exhausted limit, does not count as an ordinary occurrence, and produces no
announcement, sound, automatic opening or pause. Display filtering can hide it and native rolling
retention can evict it; Copy all includes it while retained. Further discarded occurrences do not
produce additional notices in that interval.

The active mode, any stopped state, and the distinction from retained history are visible in the
diagnostic UI. Changing mode or the active custom value starts a fresh interval and resumes the
controlled channel without clearing history. The top diagnostic window's Resume logging action
and Clear also start a new interval. Restart starts a new interval; save switching does not.
Invalid custom input preserves the last valid value and explains the allowed range.

#### Scenario: Reach a small custom limit
- **WHEN** Custom is 3 and four eligible ordinary occurrences arrive in a fresh interval
- **THEN** the first three are captured, the fourth is not, and one limit-reached notice and stopped status are available

#### Scenario: The limit notice survives muted warnings
- **WHEN** the third accepted ordinary message reaches Custom=3 while warnings are muted and Warning announcements are enabled
- **THEN** one warning-category limit notice still reaches retained history and the file, without another counted occurrence, announcement, sound, auto-open or pause

#### Scenario: Resume and preserve history
- **WHEN** the player resumes a stopped log or changes its active limit
- **THEN** new eligible messages are captured in a fresh interval without deleting the preceding records or log file

#### Scenario: Unlimited is not unlimited scrollback
- **WHEN** Unlimited is active and more unique records arrive than the native retained-history capacity
- **THEN** count-based stopping does not occur in the controlled channel, while older rows can still leave the rolling window history

#### Scenario: Reject an invalid custom limit
- **WHEN** the player enters zero, a fraction, or a value greater than 10,000
- **THEN** the last valid custom limit remains active and the UI explains the integer range

### Requirement: Required compatibility warnings survive owned logging controls
The mod SHALL always record its bounded startup warnings about skipped enabled patches and logging-feature admission failures despite its own mute settings or capture limit.

Each warning identifies the patch and target specification, target, failed prerequisite, unavailable
behavior and fallback. These warnings are still ordinary warning-category records for display
filtering and copy; the feature does not delete them or exempt them from native rolling eviction.
They must reach the startup log file even if hidden in the window. Repeated redraws, callbacks or
failed notification attempts must not recursively generate more copies of the same warning.

#### Scenario: A patch fails while warnings are muted
- **WHEN** an enabled patch fails its startup prerequisite check while Mute warnings is on and the ordinary custom limit is exhausted
- **THEN** its single compatibility warning is written to the startup log and can be inspected while retained, without resuming unrelated ordinary capture

### Requirement: Optional announcements belong to the top debug controls
The mod SHALL provide separate announcement preferences for Messages, Warnings and Errors, defaulting to off, off and on respectively, with announcement sound enabled by default.

Announcements appear beside the top debug-log control only during a loaded game with developer
mode on. They show severity, the count of eligible occurrences since acknowledgement, and a latest
message summary of at most two logical lines, with either common newline convention handled
correctly. A long line can be visually clipped; full text remains in retained log details. Muted
ordinary messages never announce. Hidden window categories can still announce. Incoming events
while announcements are unavailable are logged normally but are not replayed as announcements later.

Each severity can sound at most once per real-time second; all further occurrences update its count
and latest summary without extra sound. The sounds correspond to native reject, neutral-event and
negative-event sounds for Message, Warning and Error. No diagnostic messages are injected into the
ordinary top-left gameplay message feed. Announcement processing must not itself create recursive
log or sound storms.

#### Scenario: Repeated errors while playing
- **WHEN** several accepted errors arrive within one real-time second with developer mode and Error announcements enabled
- **THEN** their diagnostic count and latest summary update at the top, with at most one Error sound and no new gameplay-message feed entries

#### Scenario: Enter developer mode after earlier errors
- **WHEN** errors occurred with developer mode off and the player later enables it
- **THEN** retained errors remain inspectable, but past occurrences are not replayed as new announcement sounds or counts

### Requirement: Announcement acknowledgement selects evidence safely
The mod SHALL let a player activate a severity announcement to open the log, show that category, select its latest retained announced record and acknowledge that severity's count.

If that record is no longer retained, the window explains that its details are unavailable instead
of selecting an unrelated record or implying that the full file was searched. Clear acknowledges
all announcement counts; ordinary close/reopen does not. Leaving the game, disabling developer
mode or disabling a severity's announcements clears its pending announcements without clearing logs.

#### Scenario: Open a hidden announced error
- **WHEN** the player activates an Error announcement while the Error list filter is off
- **THEN** the log opens with Errors shown, selects the latest retained announced error and clears that announcement count

#### Scenario: The announced record has left history
- **WHEN** the player activates an announcement whose latest record has been evicted
- **THEN** the log explains the missing retained details and acknowledges the count without presenting another record as that event

### Requirement: Harmony diagnostic-file suppression preserves existing evidence
The mod SHALL offer a global Suppress Harmony diagnostic-file writes option, off by default, whose scope is Harmony's diagnostic-file output rather than RimWorld's ordinary log.

Enabling it preserves all existing files and content, and suppresses future direct and buffered
Harmony diagnostic output after admission. Pending buffered diagnostic text at activation is
discarded with the scope explained by the setting; messages suppressed during the enabled period
must not reappear when disabled. Suppression must not accumulate an unbounded hidden buffer, change
other mods' debug flags, disable their patches, or modify unrelated log writers. If the installed
provider's output paths cannot be isolated safely, the option is unavailable with a startup warning.

#### Scenario: Suppress and restore diagnostic writes
- **WHEN** the player enables this option with an existing Harmony log, triggers direct and buffered diagnostics, then disables it
- **THEN** old file contents survive, suppressed diagnostics do not appear later, and subsequent diagnostics can be written normally

#### Scenario: The provider cannot be controlled safely
- **WHEN** the installed provider changes the required diagnostic output behavior
- **THEN** suppression remains inactive with a clear reason, and ordinary RimWorld logging and other mods' patching continue

### Requirement: Developer fallback text can remain readable
The mod SHALL offer a Disable pseudo-translation preference, off by default, which leaves missing-translation fallback text readable when developer mode would otherwise decorate it.

The option does not supply a translation, suppress missing-key diagnostics, modify actual
translations, or change text outside that native fallback behavior. Changes apply to subsequent
text rendering without requiring a new game.

#### Scenario: Inspect untranslated configuration text
- **WHEN** developer mode is enabled, this preference is on, and a fallback string would be pseudo-translated
- **THEN** the ordinary fallback wording is shown, with placeholders and available translations preserved

### Requirement: Configuration uses common scope controls with explicit process exceptions
The mod SHALL place configuration in the ordered Diagnostics / Log settings sections Presentation, Announcements, Capture and Developer text, using the common settings loading, storage and UI path.

Colors, Ignore embedded colors, announcement preferences/sound and Disable pseudo-translation
use global defaults plus per-save overrides, new/missing save entries inheriting by default.
Each applicable row shows Use global, Current save and Global default. The save editor is disabled
while inheriting; both save editor and Use global are disabled at the main menu in appearance and
mutation callbacks. Loaded world view still has the current save. Enabling inheritance preserves
the dormant override; disabling it restores that value or seeds it from the current global value.
Save switching and reload resolve only that save's settings; leaving play restores global values.

Mute controls, capture-limit mode/value, and Harmony-file suppression are explicitly global-only:
they govern process-wide/startup evidence shared across saves, so loading a colony must not change
or resume the capture policy. Their common rows explain the exception and offer no save override.
They apply from initialization or the next eligible event after a valid edit. Presentation and
developer-text edits apply on next redraw, announcements to subsequent occurrences.

#### Scenario: Change a global color with two saves
- **WHEN** a global Error color changes and the player switches between an inheriting save and a save with its own Error color
- **THEN** the first uses the new global color and the second retains its override, without changing capture policy or interval

#### Scenario: Configure from the main menu
- **WHEN** Log settings are opened without a loaded game
- **THEN** global controls remain usable, all ordinary save controls and callbacks are disabled, and global-only capture rows explain their process scope

#### Scenario: Restore a dormant preference
- **WHEN** a save preference is changed to Use global and later changed back
- **THEN** its stored override returns, or the first override starts at the current effective global value if none existed

### Requirement: Reset operations respect their stated scope
The mod SHALL provide Global Restore defaults and Current-save Use global for all operations with distinct effects.

Global Restore defaults restores every global Log preference to its built-in value, including
global-only capture controls, without altering any save override. Capture-policy changes follow
the interval rules above. Current-save Use global for all changes inheritance for all applicable
Log rows, preserves dormant overrides and leaves global-only controls untouched. It resolves current
global values, not built-in defaults, and is disabled without a loaded game.

#### Scenario: Reset a save with nonstandard global preferences
- **WHEN** the player selects Current-save Use global for all while global colors and capture policy differ from their built-in defaults
- **THEN** the save uses those current global colors, dormant overrides survive, and the global capture policy and interval are unchanged

### Requirement: Independent providers and changed game versions fail visibly
The mod SHALL activate each enabled logging behavior only when its documented semantic prerequisites hold, tolerating unrelated game edits and compatible other patches.

Unsupported behavior remains inactive with one bounded startup warning per skipped enabled patch,
while independent admitted behavior and native logging remain usable. A control whose behavior
cannot be provided explains its unavailability instead of silently claiming success. When the
original Better Log package is active, owned overlapping Log enhancements are suppressed as a
group with a clear warning; no source removal, settings import, foreign patch removal or package
identity impersonation occurs. Native filters and native log entry remain native. No universal
third-party compatibility or automatic repair of errors is promised.

#### Scenario: The capture path changes while inspection still works
- **WHEN** capture-limit prerequisites fail but native inspection and color presentation remain compatible
- **THEN** the limit control is unavailable with a startup explanation, while inspecting and coloring ordinary native logs remains usable

#### Scenario: Original provider is co-installed
- **WHEN** Better Log is active alongside the megamod
- **THEN** owned overlapping Log enhancements remain inactive with a warning, and the source configuration and patches are left untouched

### Requirement: Diagnostic probes are finite and explicitly requested
The mod SHALL offer a developer-only Emit sample error action in the top debug output menu that produces one clearly labeled synthetic error per activation through the ordinary logging path.

The action obeys active mute/limit/announcement rules and does not repair game state. It creates
no automatic startup errors or persistent stream of errors. The source's unlimited rainbow-error
spam toggle is deliberately omitted.

#### Scenario: Emit a sample while testing diagnostics
- **WHEN** the player activates Emit sample error once with ordinary error capture enabled
- **THEN** one labeled sample occurrence enters the ordinary diagnostic workflow and no continuing emission is scheduled
