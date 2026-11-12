## ADDED Requirements

Owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.

### Requirement: Inspect an exact owned caravan through native World controls

The Gateway SHALL expose typed named automations and matching E2E steps for a native World-view
round trip, exact player-caravan jump/selection, Items/Health tab commands and the actual observed
pawn-Health row button. It SHALL preserve native button visibility, disabled, modal and tutorial
restrictions. Player actions MUST NOT fabricate a dialog, set the detailed Health pawn, invoke a UI drawing
method to manufacture observation, guess root coordinates or broaden ordinary window input to
arbitrary transient windows. Input delivery remains distinct from personally observed native results.

#### Scenario: Open World and inspect the selected caravan
- **WHEN** an operator captures an admitted settled map baseline and requests World open
- **THEN** the displayed native World worker is invoked at most once and its actual before/after state is retained
- **AND** an exact current player-caravan capability selects and centers that object through the native game command
- **AND** its actual World inspect pane supplies the native Items/Health tab instances and tutorial-aware tab callback
- **AND** closing World with an unrelated main tab open refuses rather than silently activating twice

#### Scenario: Open the native detailed Health card
- **WHEN** the exact selected caravan's visible pawn-Health row is captured from a completed native layout and clicked
- **THEN** the existing minimized input lane delivers the captured button center through the actual native input path
- **AND** the capability binds current Game, World, UI, caravan, pawn, tab, transient-window instance and layout geometry
- **AND** the named pawn's resulting Health card is separately observed and captured
- **AND** a reused negative native window ID alone never authorizes input

#### Scenario: Preserve a valid detailed Health selection across tabs
- **WHEN** a pawn's Health detail is already open and the operator changes Items to Health for that same caravan
- **THEN** observation recognizes the retained detail instead of clicking its toggle again
- **AND** a workflow that needs a later fresh open first closes the detail through its actual row button and observes closure

#### Scenario: A neighbouring Health row is clipped
- **WHEN** one row button is fully visible and another has supported geometry outside the actual intersected clip
- **THEN** the visible row remains capturable and the clipped row is excluded
- **AND** unknown or invalid geometry still invalidates the enclosing observation

#### Scenario: Health detail drawing fails after the card returns
- **WHEN** the exact immediate-window contents callback or one of its postfixes fails after drawing the card
- **THEN** observation records that failure before the native window catches it and does not publish DetailVisible
- **AND** successful outer dispatch and pending-window processing cannot override the incomplete contents witness

#### Scenario: Refuse stale caravan controls
- **WHEN** scene, ownership, selected caravan, pawn membership, tab, window, scale, scroll, clipping, geometry or modal input changes
- **THEN** remaining pointer phases refuse before reaching another native control
- **AND** failed, skipped, ambiguous or incomplete enclosing native layout cannot publish a control capability
- **AND** cleanup preserves unrelated hot controls and reports ambiguous ownership honestly

### Requirement: Restore only the captured game's owned World-view state

The first World-view lease SHALL admit a fresh settled non-background map baseline with inactive
world camera, empty world selection and drag history, no open World pane/main tab and no active
world targeting or competing input. These are repository fixture limits, not native engine limits.
Capture SHALL remain passive and retain the complete fixed camera state, pose and touched native
selection/view flags. The same typed lease SHALL serve automation and existing E2E context cleanup.

#### Scenario: Restore after the native round trip
- **WHEN** the operator ends the same Game/World view lease after owned input has drained
- **THEN** cleanup restores the captured None mode, deactivates that exact camera, and restores its fixed fields and pose without advancing smoothing
- **AND** only owned selection entries are cleared and raw nullable selection/view fields are restored without lazy initialization
- **AND** native pane closure and a later frame's equality are observed before restoration is reported complete
- **AND** no simulation ticks, native sounds, tutorial opportunities or unrelated renderer caches are claimed rewound

#### Scenario: A replacement game reuses the persistent camera
- **WHEN** Game, World, map, UI, driver, config or owned state identity differs at restoration
- **THEN** cleanup refuses writes into departed or replacement state even if the camera driver object survived the transition
- **AND** its receipt distinguishes restored, skipped and unverifiable phases while independent fixture cleanup still runs

#### Scenario: Normal World frames accumulate camera history
- **WHEN** more than1024 native drag-history timestamps accumulate during the owned round trip without an active drag
- **THEN** restoration drains the exact owned history in bounded advances after deactivating its driver
- **AND** no frame-count ceiling or foreign-list replacement is used to claim restoration

#### Scenario: Another world activity starts before restoration
- **WHEN** a foreign targeter, route planner, tile picker or drag is active before restoration
- **THEN** cleanup refuses the mode mutation and preserves that activity and its native state
- **AND** the failed receipt does not claim a restored view

#### Scenario: Cancel a captured view workflow
- **WHEN** cancellation or failure follows capture or an admitted native action
- **THEN** retained artifacts identify the owned view lease and actual attempted changes
- **AND** existing deferred cleanup drains only owned input and applies the remaining shared cleanup deadline
- **AND** timeout, failed later-frame observation or partial restoration cannot report complete cleanup
- **AND** missing native input dispatch consumes at most15s of the shared30s cleanup period, then reports incomplete input ownership while allowing at most10s independent view restoration and the remaining fixture cleanup
- **AND** a retained detailed Health selection may be cleared only during cleanup when its captured baseline was null, the exact same native tab and Game/World remain owned, and its current pawn belongs to a caravan selected by this lease; foreign detail is preserved and reported incomplete

#### Scenario: Share native layout boundaries with the Entry observer
- **WHEN** the caravan and Entry providers observe the same native event dispatch or pending-window processing
- **THEN** each uses a separate Harmony state declaring type and admits only the exact registered Gateway finalizers
- **AND** a foreign exception-suppressing finalizer or failed native body revokes observation publication

### Requirement: Click an exact observed Entry-menu control through native input

The Gateway SHALL capture the actual rendered Options and developer quicktest controls in a
settled Entry menu and click one current captured control through the existing minimized native
input lane. It MUST NOT fabricate a WindowStack owner, call a captured callback directly or
change developer mode to make a control available. A delivered gesture is separate from the
subsequent native window or new-game outcome.

#### Scenario: Open Options and create a disposable second game from Entry
- **WHEN** an authenticated operator captures an unobscured settled Entry menu and clicks its exact Options handle
- **THEN** native button input opens the ordinary Options window while the game remains minimized
- **AND** after native closing controls, a fresh capture may expose developer quicktest when developer mode is already enabled
- **AND** clicking that current control follows the native quicktest path; the caller separately waits for and observes the distinct playable game

#### Scenario: Refuse a stale or obscured Entry control
- **WHEN** the root, UI root, window stack, dispatcher, language, developer mode, extent, layout, native control identity or enabled state changes, or a game, long event, world view or blocking window appears
- **THEN** remaining input is refused before reaching another control
- **AND** the provider requires fresh completed native layout before each mouse phase, retains delivered phases and cleans only its owned input
- **AND** missing, ambiguous or unsupported controls never fall back to an ordinal, coordinate guess or callback invocation

#### Scenario: Preserve native input ownership across an Entry transition
- **WHEN** cancellation, a rejected release or a native scene transition occurs during an Entry gesture
- **THEN** the existing deferred input lifecycle retains partial results and attempts only provably owned release and pointer restoration
- **AND** an unrelated hot control is never treated as owned because it lacks a window mapping; ambiguous cleanup quarantines further injection

#### Scenario: Reject an observation invalidated after the menu returns
- **WHEN** a menu has returned but a later callback fails, replaces the layout event or changes dispatcher provenance before the enclosing native dispatch finishes
- **THEN** its controls remain provisional and cannot authorize the following mouse phase
- **AND** native OnGUI failures caught by the dispatcher invalidate the observation through the inspected pending-window cleanup path
- **AND** only intact enclosing completion, including successful native pending-window processing, publishes the current controls; skipped phases or unsupported observer/finalizer authority fail closed

#### Scenario: Retain a failed Entry capture run
- **WHEN** the capture handler rejects the layout or returns a malformed inventory
- **THEN** the E2E outcome retains the returned automation run ID, request ID, state, original error code and result artifact
- **AND** the original rejection code is preserved and the inventory observer is not invoked

### Requirement: Navigate an available ordinary bottom tab through its native button

The Gateway SHALL offer a typed desired-open-state action for one exact supported bottom tab in
a settled playable game. It SHALL preserve the native button's visibility, disabled state and
modal input restrictions, use its normal command, and report the observed result. It SHALL keep
the game minimized and preserve desktop focus and pointer. Controls within the opened tab remain
separate native player actions; opening a tab does not imply a product workflow succeeded.

#### Scenario: Open Assign for a policy workflow
- **WHEN** an operator requests the closed, visible and enabled Assign tab while the game accepts bottom-bar input
- **THEN** the ordinary Assign window opens through its native button command
- **AND** the operation retains the requested tab and observed before/after states
- **AND** a repeated open request leaves it open without activating the button again

#### Scenario: Preserve an unavailable button's restrictions
- **WHEN** the target tab is missing, hidden, disabled, unsupported, blocked by a modal window or requested outside settled player control
- **THEN** the action explicitly refuses without opening a window or invoking a replacement control
- **AND** another open tab and unrelated windows remain unchanged

#### Scenario: Close only the requested tab
- **WHEN** an operator requests a supported tab closed while another tab is open
- **THEN** the request observes that its target is already closed and does not close the other tab
- **AND** closing the actual target, when permitted, uses its native button command once

#### Scenario: Native command does not reach the requested state
- **WHEN** the native command throws, replaces the game/UI context or leaves a different tab state
- **THEN** the action reports failure with attempted and known observed state separated
- **AND** it does not retry a toggle or undo unrelated accepted native effects

#### Scenario: Cancel tab navigation before dispatch
- **WHEN** cancellation arrives before the native tab command is dispatched
- **THEN** the command is not invoked
- **AND** cancellation after a committed native result does not fabricate its rollback

### Requirement: Operate the exact native dialog without disturbing the desktop

The Gateway SHALL offer bounded click, key/text and scroll operations for one identified current
native dialog on a supported runtime. Operations SHALL use the game's input path and preserve its
hit testing, disabled controls, focus and callbacks. They MUST NOT restore, foreground or maximize
the game, move the desktop pointer, or call a product setting setter in place of its control.

#### Scenario: Change a native checkbox in a minimized game
- **WHEN** an authenticated operator clicks an enabled checkbox in the identified current dialog
- **THEN** the native control changes through its normal callback and the next captured view shows the change
- **AND** the game stays minimized and desktop focus/pointer remain unchanged

#### Scenario: Disabled control remains disabled
- **WHEN** an operator clicks an inactive per-save checkbox at the main menu
- **THEN** it remains unchanged and the action does not bypass the control's restriction

#### Scenario: Enter text through the selected field
- **WHEN** the operator focuses a native text field and submits text or an editing key sequence
- **THEN** normal focus, selection and validation rules determine the visible result
- **AND** another field or underlying game command does not receive an unintended gesture

#### Scenario: Replace one captured focused native text field
- **WHEN** the operator clicks a writable single-line native field, captures its exact focused-control handle and requests bounded replacement text
- **THEN** native Ctrl+A, Backspace and text-input events replace it through its ordinary field path while the game remains minimized
- **AND** empty text and printable BMP names/numeric-invalid strings are delivered without direct editor/value mutation, clipboard use or arbitrary keyboard input
- **AND** the initial repository operation limit is128 characters and control/format/unassigned characters, line/paragraph separators and surrogate code units are rejected before input; this is not an engine text-length claim

#### Scenario: A focused field changes during text delivery
- **WHEN** focus, editor instance/rectangle, owning native window or geometry changes, including during the layout immediately before a key/text event
- **THEN** remaining replacement input is refused before reaching another field or an underlying game command
- **AND** retained evidence separates already delivered chord/clear/characters from undelivered input and confirmed owned cleanup

#### Scenario: Text replacement is cancelled with owned input pending
- **WHEN** cancellation or failure interrupts a replacement while a key or character remains owned
- **THEN** cleanup releases only those owned keys/removes only the exactly owned unconsumed character, leaving accepted native edits and unrelated input intact
- **AND** ambiguous ownership reports incomplete cleanup and quarantines further injection instead of clearing all input

#### Scenario: Reset authority changes while text keys are held
- **WHEN** the inspected reset body is rebuilt or reset verification fails during text input
- **THEN** all text-key receipts are revoked and later same-shaped foreign edges cannot inherit ownership
- **AND** cleanup independently attempts every still-provably-owned key and character, retaining all failures without releasing ambiguous input

#### Scenario: Retain cleanup failures in the final text result
- **WHEN** text cleanup fails after an action rejection or cancellation
- **THEN** its final receipt retains the original action failure separately from up to four ordered cleanup-failure entries covering the fixed three keys and character queue
- **AND** entries contain only bounded exception types and known failure codes, without arbitrary exception messages, stacks, Data or error-code payloads
- **AND** larger unexpected aggregates explicitly report truncation without changing incomplete-cleanup or quarantine behavior

#### Scenario: A captured layout is changed before native execution
- **WHEN** another callback replaces or changes the captured layout event or its drawing/type/raw-type state, or layout execution/guard authority is lost
- **THEN** it cannot authorize the following native key pass
- **AND** owned cleanup runs without redirecting that key to a field

#### Scenario: Replace text within an existing pointer workflow
- **WHEN** a healthy owned pointer scope surrounds native text replacement
- **THEN** the text action preserves its pointer position and uses the same current-window/focused-control checks
- **AND** scope expiry or ending stops replacement and waits for owned key cleanup before restoring the pointer

### Requirement: Reject stale context and unsupported input before mutation

The Gateway MUST bind each operation to the exact process and native dialog instance, current
client bounds and permitted input shape. A replaced, obscured or closed dialog, invalid coordinate,
unsupported runtime, or active incompatible gesture SHALL produce an explicit rejection without
pressing a nearby control. It SHALL revalidate context before each pending gesture phase.

#### Scenario: A dialog is replaced before the click starts
- **WHEN** the requested dialog closes and another instance opens before dispatch
- **THEN** the stale operation is rejected and the new dialog receives no input

#### Scenario: Layout replaces the dialog immediately before a mouse event
- **WHEN** a layout callback replaces, moves or obscures the target after its mouse press or release was queued
- **THEN** the pending mouse event is rejected before the replacement or underlying game receives it
- **AND** the operation reports the undelivered phase and completes only its owned cleanup
- **AND** an already established control owned by the replacement keeps its input capture
- **AND** a fresh click using the replacement's own captured handle can proceed after successful cleanup

#### Scenario: The game lacks the inspected input capability
- **WHEN** the current runtime cannot supply the required native input operations
- **THEN** discovery reports the capability unavailable and invocation performs no desktop fallback

#### Scenario: Native pixel origin changes between supported beta runtimes
- **WHEN** native mouse projection uses an admitted height-minus-y or height-minus-one-minus-y mapping
- **THEN** captured-window input uses that mapping's inverse and reaches the exact requested rendered coordinate
- **AND** an unrecognized mapping is unavailable rather than using a guessed offset
- **AND** a later changed effective projection is refused before mouse delivery if its coordinates differ.

#### Scenario: Another component changes a prepared mouse event
- **WHEN** another runtime component changes the prepared mouse event before its native delivery
- **THEN** the Gateway rejects the changed owned gesture instead of forwarding it as unrelated input
- **AND** its cleanup preserves unrelated input ownership

### Requirement: Bound and clean up input while reporting actual outcomes

Operations MUST remain cancellable between bounded phases, release only their injected held inputs
on completion/failure/cancellation, and restore temporary pointer/input state without undoing real
accepted UI results. They SHALL reject incompatible preexisting held input rather than releasing
the user's controls. Result evidence SHALL distinguish request acceptance, dispatched input,
completion, rejection and partial failure. A successful dispatch SHALL NOT claim the requested
gameplay outcome occurred.

#### Scenario: Native dispatch completes without confirmed mouse delivery
- **WHEN** a click fails because its owned native mouse pass was not confirmed
- **THEN** its retained receipt distinguishes queued input edges, observed outer dispatch, captured
  native and owned mouse passes, and the owned pass admission/delivery result
- **AND** absent observations remain false or absent rather than implying the control changed
- **AND** the same failure and cleanup rules apply without retrying or bypassing native controls.

#### Scenario: Cancellation after a press
- **WHEN** cancellation arrives after an injected press and before the gesture completes
- **THEN** the owned press is released, no unrelated control is cleared, and the partial result is recorded
- **AND** a later independent operation is not blocked by a leaked gesture

#### Scenario: A callback changes the window during a gesture
- **WHEN** an input callback closes or replaces the target dialog
- **THEN** remaining target-dependent input stops and owned input cleanup still occurs
- **AND** already committed native results are retained and reported rather than fabricated as rolled back

#### Scenario: Continue a native popup workflow
- **WHEN** a successful click in an input workflow opens an ordinary pointer-sensitive menu
- **THEN** the menu remains available for observation and follow-up native player input
- **AND** finishing the individual click does not move the pointer away and dismiss the accepted result
- **AND** ending, cancelling or timing out the workflow restores only its owned temporary pointer state and reports incomplete cleanup honestly
- **AND** a failed or cancelled gesture releases its owned held input without modifying the menu's normal behavior

#### Scenario: Preserve standalone click behavior
- **WHEN** a caller requests a standalone click without opening a pointer workflow
- **THEN** the click restores its temporary pointer position on completion as before
- **AND** an already-owned pointer workflow cannot be silently borrowed by an unrelated standalone request

#### Scenario: End a pointer workflow after unrelated input
- **WHEN** another input source changes the pointer before workflow cleanup
- **THEN** cleanup does not overwrite that new pointer position
- **AND** the workflow reports incomplete ownership cleanup instead of claiming restoration

#### Scenario: A pointer workflow expires or is interrupted during opening
- **WHEN** a workflow reaches its declared deadline or its opening request is interrupted after ownership was assigned
- **THEN** it admits no further owned gestures and retains enough identity to finish its cleanup
- **AND** scenario cleanup still restores its owned pointer state even if the scenario callback fails
- **AND** repeated cleanup does not move the pointer a second time

### Requirement: Share typed control and retain causal evidence

The authenticated repository MCP/CLI and executable E2E steps SHALL use the same input capability
and rejection/cleanup rules. Admission and outcome records SHALL retain exact process/window and
input identity without credentials. Native acceptance MUST include personally inspected
before/action/after captures and observed native control behavior on the reviewed build.

#### Scenario: Reproduce a settings-control workflow
- **WHEN** a registered E2E scenario operates a native control through the typed capability
- **THEN** its input and observed result can be traced to the exact process and reviewed build
- **AND** a passing endpoint or state query alone does not mark that workflow accepted

#### Scenario: Inspect and cancel an accepted multi-frame operation
- **WHEN** an operation returns while its native gesture is still pending
- **THEN** its retained run identity can be inspected and cancelled through the authenticated typed tools
- **AND** inspection does not repeat the gesture and cancellation reports a request until owned cleanup completes
- **AND** an unknown or expired run is reported explicitly rather than starting another operation

#### Scenario: A caller stops polling a pending gesture
- **WHEN** the initial request has returned and no later status request arrives
- **THEN** the operation's own deadline still stops further target-dependent input and initiates owned cleanup
- **AND** disposing the completed HTTP request cannot remove that deadline

### Requirement: Open native mod settings in a settled main menu

The existing native mod-settings navigation capability SHALL support a settled main menu without
a loaded game, as well as settled play. It MUST select the exact active mod and preserve its native
settings window lifecycle; unavailable or ambiguous targets SHALL fail without opening another mod.

#### Scenario: Open global settings before loading a colony
- **WHEN** the operator opens the identified active mod's settings from the settled main menu
- **THEN** the native page appears with that mod's own main-menu restrictions intact
- **AND** no colony or artificial loaded-game state is created to make the action succeed

### Requirement: Observe an explicitly expected input rejection

An E2E click step SHALL support an optional exact expected rejection code so a negative native workflow can
continue to visible observation. It SHALL pass that step only when the admitted operation fails
with that exact code and confirms owned input cleanup. Unexpected success, another error, missing
cleanup evidence, cancellation or timeout SHALL remain a failure. Retain the actual operation
state, error, request identity and partial input receipt even when the rejection was expected.

#### Scenario: Continue after a safely rejected stale click
- **WHEN** a scenario expects the exact stale-context rejection and the admitted click returns it with confirmed cleanup
- **THEN** the scenario can inspect and capture the unchanged replacement window
- **AND** success or a different failure cannot masquerade as the expected rejection

### Requirement: Inspect the native developer log through its ordinary command

The typed automation and E2E surface SHALL open or close the native developer log through the
same command used by its toolbar button. It SHALL require developer mode and a settled native UI,
report the observed open state, and preserve an already matching state without toggling it.
It MUST NOT clear log entries, change developer mode or manufacture a diagnostic as an opening
side effect. Unsupported native command shape or ambiguous log windows SHALL reject the action.
Recorded before-state, attempted command and available after-observation SHALL survive a later
cancellation or failure; the result MUST NOT suggest that an interrupted command made no change.

#### Scenario: Capture the native log during verification
- **WHEN** a scenario requests the log open while developer mode is already enabled
- **THEN** the actual native log appears for a subsequent screenshot and retains its existing entries
- **AND** requesting open again leaves that same log open rather than closing it

#### Scenario: Cancellation follows a native log command
- **WHEN** cancellation arrives after the native command changed the window state
- **THEN** the cancelled result retains the before-state, attempted command and available after-observation
- **AND** the caller can identify a possible applied change without repeating the command
