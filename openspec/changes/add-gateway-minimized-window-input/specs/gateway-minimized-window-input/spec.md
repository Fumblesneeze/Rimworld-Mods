## ADDED Requirements

Owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.

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
