# Message placement

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld 1.7 beta**.
Origin replacement, S071 Better Message Placement (Continued). **Manual runtime, settings controls
and bounded preview are partially implemented with bounded reviewed English native verification;
automatic clearance and full native acceptance
remain unfinished.** Numerical layout choices below remain the specified defaults.

## ADDED Requirements

### Requirement: Ordinary feature-off preserves message preferences
Message placement SHALL have a common global/save Boolean enable setting, initially enabled,
applied on the next display. Disabling SHALL retain both stored offsets and inheritance choices
while leaving native message placement and interactions available. An active original provider
SHALL use this same effective-off setting without overwriting requested preferences.

#### Scenario: Disable and reenable manual placement
- **WHEN** the player disables placement with nonzero offsets and then reenables it
- **THEN** native placement is used while disabled and the retained offsets apply on the next display after reenablement
- **AND** disabling is not reported as a failed patch or an unavailable GUI context

#### Scenario: Start with placement disabled and enable it later
- **WHEN** placement is effectively off at startup, including an active-original effective-off reason
- **THEN** its native patch group is not attempted and disabled placement produces no failed-patch warning
- **AND** a later ordinary effective enablement may admit the group and apply retained offsets on the next display

#### Scenario: Incremental manual placement is explicitly bounded
- **WHEN** the manual-positioning implementation is available before automatic bar geometry integration
- **THEN** settings visibly explain that automatic clearance is unavailable
- **AND** admitted manual offsets and viewport fitting remain usable without claiming bar clearance or changing the stored pair
- **AND** the implementation milestone does not satisfy automatic-placement, preview or native interaction acceptance obligations

### Requirement: Automatic placement follows the visible colonist bar
Owned short-message placement SHALL begin at the ordinary native position, including its active
lesson adjustment once, and move down only as needed to put the first message's padded display bounds
at least **4 UI units** below the lowest visible colonist-bar content, whether or not its background
is visible. Native background visibility SHALL remain unchanged. The bar includes visible rows,
labels, weapons, group frames and supported mood/selection outlines. With zero manual offsets and
enough viewport space, messages SHALL clear that content. Hidden content SHALL reserve no space.

#### Scenario: Visible bar has several rows and extended content
- **WHEN** a native short message appears with its native background visible or hidden below a visible supported bar with labels, weapons or group frames
- **THEN** its first padded display bounds clear the lowest visible bar content by at least four UI units when space permits
- **AND** relocation does not make a normally hidden background visible
- **AND** the ordinary native position is kept if it is already lower

#### Scenario: Current bar layout changes
- **WHEN** rows, visible groups, UI scale or viewport size change before messages are displayed
- **THEN** placement reflects the current visible layout on that display, including the first display after the change
- **AND** it does not temporarily use the previous layout's reserve

#### Scenario: Owned portrait filtering and weapon presentation compose with placement
- **WHEN** the owned colonist-bar presentation hides or reduces entries, or displays an enlarged or downward-offset weapon
- **THEN** message clearance follows the parts actually displayed, including the weapon's displayed extent
- **AND** hidden portraits and overlays reserve no space; visible names, frames and remaining content still do
- **AND** placement does not change the bar's chosen filter, weapon settings or display-update timing

#### Scenario: The bar is not visible
- **WHEN** messages are displayed at the main menu or in a play view where the bar is hidden, suppressed or has no visible content
- **THEN** no space is reserved for the bar
- **AND** the native position and any active lesson adjustment remain the starting point

#### Scenario: World view and lessons retain their own context
- **WHEN** the player switches between map and world views or a lesson changes its message position
- **THEN** placement follows the bar actually visible in that view and preserves the lesson's native adjustment once
- **AND** a visible world-view bar is not ignored merely because no map is selected

### Requirement: Manual offsets are relative to automatic placement and use the visible UI
The feature SHALL offer horizontal and vertical offsets as one scoped position pair, each initially
**0%**, each accepting finite values from **−100% through +100%**. Horizontal uses current viewport
width and vertical uses current viewport height, after UI scaling. Offsets SHALL apply once after
automatic placement, before viewport fitting. Negative values move left/up; manual choices can
intentionally overlap the bar. Invalid edits SHALL preserve the previous pair; invalid stored pairs
SHALL use 0/0 in their existing scope with an explanation, without modifying another scope.

#### Scenario: Positive and negative offsets are applied once
- **WHEN** the player sets horizontal +10% and vertical −10%
- **THEN** the preferred position moves right by one tenth of visible UI width and up by one tenth of visible UI height
- **AND** it is fitted to the viewport afterward; upward adjustment is allowed to overlap the bar

#### Scenario: Resolution and UI scale change
- **WHEN** the same valid percentages are used after a window-size, resolution or UI-scale change
- **THEN** the movement is recomputed from the current visible UI dimensions
- **AND** the saved percentages are unchanged

#### Scenario: Invalid pair cannot partially overwrite valid settings
- **WHEN** an edit contains an out-of-range or nonnumeric offset
- **THEN** neither coordinate of that edit replaces the previous valid pair
- **AND** malformed loaded data uses the explained 0/0 fallback only in its own scope, preserving other saves and global defaults

### Requirement: Viewport fitting keeps native stack order and defines overflow
The feature SHALL fit the whole native message stack inside the viewport on each axis where its
extent fits, using a single translation and retaining native relative positions. A stack too wide
SHALL begin at the left edge; a stack too tall SHALL keep its newest message vertically inside the
viewport when that message fits, or align that message's top with the viewport otherwise. Overflow
SHALL follow ordinary clipping without changing text, spacing, order, lifetime or archive policy.
Fitting takes precedence over bar clearance and can produce overlap when space is insufficient.

#### Scenario: Extreme offset would hide a stack that fits
- **WHEN** a valid offset would place some of a stack outside the viewport and the native stack fits
- **THEN** the complete stack is moved just enough to fit on each affected axis
- **AND** its stored offsets and relative message positions remain unchanged

#### Scenario: The stack fits the screen but not the space below the bar
- **WHEN** automatic placement leaves insufficient space below the bar for a stack that fits the viewport
- **THEN** fitting keeps that stack on screen even if it overlaps the bar
- **AND** no message is reordered, shortened, expired or dismissed to make space

#### Scenario: A long message or tall stack cannot fit
- **WHEN** the native message arrangement exceeds viewport width or height
- **THEN** excess width begins at the left edge and excess height prioritizes the newest message as specified
- **AND** older messages keep their native relative positions rather than being piled onto one clamped position
- **AND** overflowing content can clip; the feature adds no text wrapping, scroll window or early removal

### Requirement: Relocated messages retain native behavior and interactions
The feature SHALL change only native short-message position. Text, types, posting eligibility,
ordering, spacing, expiry/fading, refresh, sound, background visibility and archival behavior SHALL remain native. Background,
text, hover response and click target SHALL agree at the displayed position. Letters, alerts, logs,
combat text and other notification surfaces SHALL be unaffected.

#### Scenario: Native gameplay produces and retires messages
- **WHEN** ordinary gameplay produces a message, repeats it, or lets it expire
- **THEN** it has the same content, posting/refresh, sound, lifetime and archive outcome as without owned relocation
- **AND** only the displayed position changes

#### Scenario: Native target and quest interactions follow the displayed message
- **WHEN** the player hovers or left-clicks a relocated message with a native target or quest action
- **THEN** the normal highlight or target/quest navigation occurs from the visible message's hitbox
- **AND** the old position does not remain a second interactive copy

#### Scenario: Dismissal and bar overlap remain coherent
- **WHEN** the player right-clicks a relocated message during play or its rectangle overlaps a portrait
- **THEN** ordinary dismissal and the native portrait-overlap response use its displayed position
- **AND** moving a message does not create duplicates or move letters, alerts or logs

### Requirement: Position configuration uses common scopes and existing settings navigation
The position pair SHALL use the common **Interface / Messages** settings section with **Use global**,
**Current save** and **Global default** controls. Both coordinates inherit together; new/missing save
values use the current global default. Inheritance SHALL retain a dormant override, restored when
unchecked or initially seeded from the effective global pair. A save edit SHALL never change another
save or global data. Valid changes SHALL affect the next display without reposting messages.

#### Scenario: Inheritance and dormant overrides preserve scope
- **WHEN** an inherited save follows a global change, an overridden save is edited, or Use global is toggled
- **THEN** only the appropriate effective pair changes; dormant overrides are retained and restored
- **AND** save/reload and save switching recover each save's own inheritance and pair

#### Scenario: Main-menu controls cannot mutate a save
- **WHEN** settings open with no active game
- **THEN** global controls remain usable and save controls, Use global and save reset are visibly disabled with an explanation
- **AND** their actions cannot write to a previous or implicit save

#### Scenario: Reset targets only the named scope
- **WHEN** the player resets an enabled current-save override or the global default
- **THEN** only that scope's pair becomes 0/0 and Use global is unchanged
- **AND** save reset is unavailable while inheriting; resetting globals affects only saves that inherit them

#### Scenario: Configuration remains readable in ordinary navigation
- **WHEN** the player opens settings in any of the seven baseline languages
- **THEN** position, scope, percentage, reset, overlap/overflow and unavailable-provider explanations are readable there
- **AND** the feature adds no bottom tab, bottom-right toggle, debug control or per-pawn setting

### Requirement: Preview shows effective positioning without posting a message
Settings SHALL include a clearly labeled, scaled viewport preview with a sample message stack and
the current bar reserve. It SHALL show effective current-save placement during play and global
placement at the main menu, naming that scope. It SHALL refresh for effective coordinate, reset,
inheritance, view and size changes without adding live messages, archive entries, sounds, targets or
quest actions. Closing settings SHALL remove the preview without clearing real messages.

#### Scenario: Preview follows independent changes and inheritance
- **WHEN** effective horizontal and vertical changes cancel numerically, or reset/inheritance changes the effective pair
- **THEN** the preview still updates to the actual pair
- **AND** editing a global value under a save override preserves that save's effective preview

#### Scenario: Preview never impersonates gameplay
- **WHEN** the player repeatedly adjusts settings, switches saves or opens them at the main menu
- **THEN** the labeled preview shows that context without creating a live notification, sound, history record or actionable target
- **AND** closing settings removes only the preview, leaving ordinary messages intact

#### Scenario: Incremental preview remains honest about unavailable clearance
- **WHEN** manual placement is admitted but automatic bar geometry is not integrated
- **THEN** the labeled sample diagram uses the current effective offsets, viewport and native anchor with an explicit automatic-clearance-unavailable explanation
- **AND** it invents no bar reserve; disabled or unadmitted placement shows its explanation without drawing an active placement sample
- **AND** this milestone does not accept current bar reserve, native message interactions or full preview coverage

### Requirement: Compatibility selects one provider and fails only unsupported contributions
The feature SHALL apply only where required message-placement behavior can be verified, allowing
unrelated changes and compatible other patches. Failed checks or installation SHALL leave the affected
owned contribution wholly inactive and preserve native/provider messages and preferences. Each skipped
enabled patch SHALL produce one bounded startup warning identifying the patch, target/spec, reason,
unavailable behavior and fallback. Unsupported automatic bar clearance SHALL be explained separately
from supported manual positioning. Repeated draws SHALL not flood the log.

#### Scenario: Compatible changes retain owned placement
- **WHEN** another patch or unrelated game edit preserves the required placement and native interaction behavior
- **THEN** owned placement remains eligible
- **AND** supported owned mood-bar content participates directly in the same clearance result

#### Scenario: Position admission fails without partial relocation
- **WHEN** required placement behavior is missing, ambiguous, changed incompatibly or fails installation or later revalidation
- **THEN** the entire affected owned position contribution remains inactive, with no horizontal-only or vertical-only result
- **AND** the bounded diagnostic names native/provider placement as fallback; unrelated features and foreign patches remain intact

#### Scenario: Target admission does not run another mod's behavior
- **WHEN** owned placement checks the required effective game or deliberately used integration behavior
- **THEN** checking alone does not run that mod's gameplay or initialization behavior
- **AND** its ordinary operation remains unchanged; an unavailable check disables only the affected owned contribution with its bounded diagnostic

#### Scenario: Uncertain display order preserves completed native effects
- **WHEN** message drawing or interaction is attempted before a safe placement update, or an unexpected repeated or nested update makes the remaining display order uncertain
- **THEN** further affected owned movement is skipped with a bounded diagnostic and ordinary message behavior continues
- **AND** already completed display and native interaction effects are preserved rather than undone; the feature does not promise retroactive atomicity for an entire UI update

#### Scenario: Automatic clearance is unavailable for a bar provider
- **WHEN** native positioning remains supported but the active bar's visible bounds cannot be verified
- **THEN** admitted manual offsets and viewport fitting use the native anchor without an invented bar reserve
- **AND** settings/preview identify automatic clearance as unavailable; stored preferences remain unchanged

#### Scenario: The original mod is active
- **WHEN** Mlie.BetterMessagePlacement is active
- **THEN** a bounded startup warning identifies best-effort handling through the ordinary owned placement-off path, with the same behavior, timing and preview availability as manually off
- **AND** stored owned preferences and source configuration remain unchanged; inactive installed sources do not trigger this handling
- **AND** unrelated features remain available and no source-only placement, rollback of earlier effects or coexistence compatibility is promised
