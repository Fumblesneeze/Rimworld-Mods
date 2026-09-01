## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Explicitly named potentially maximizing process input
Every public Gateway surface that may restore or foreground a minimized RimWorld window—and may therefore cause Windows or Unity to present it maximized—SHALL include the literal warning `may-maximize-window` in its action, route, command, method, request type, step type, or serialized operation name. The direct HTTP routes SHALL be `POST /api/v1/input/may-maximize-window/click`, `POST /api/v1/input/may-maximize-window/drag`, and `POST /api/v1/input/may-maximize-window/keys`; the companion CLI SHALL expose matching `may-maximize-window-click`, `may-maximize-window-drag`, and `may-maximize-window-keys` commands; successful results SHALL identify the operation with the matching warning-bearing value. Ambiguous legacy names such as `/input/click`, `/input/drag`, `/input/keys`, `click`, `drag`, and `keys` SHALL NOT remain callable aliases. Help and usage failures for those legacy CLI names SHALL direct callers to the explicit replacements.

#### Scenario: Caller selects a desktop-disruptive input path
- **WHEN** a caller inspects the HTTP route, companion command, or typed E2E action before sending process input that can activate a minimized RimWorld window
- **THEN** its public name explicitly states `may-maximize-window` before any side effect occurs

#### Scenario: Caller uses an old ambiguous CLI command
- **WHEN** a caller invokes `click`, `drag`, or `keys`
- **THEN** the companion exits with invalid usage, names the corresponding `may-maximize-window-*` replacement, and sends no request

### Requirement: Process-scoped click input
`POST /api/v1/input/may-maximize-window/click` SHALL accept a mouse button and screen-local client-pixel position whose origin is the top-left of the rendered RimWorld client, validate that the current target window belongs to the running RimWorld PID and that the point lies within its current client bounds, and inject exactly one down/up click pair or return a capability/focus/bounds error. Foreground activation from a Gateway HTTP worker SHALL keep any temporary Windows input-queue attachment alive through the complete serialized click and detach it in cleanup. A click MAY retry one transient `focus_lost` before mouse-down; it SHALL record that reacquisition, SHALL NOT inject more than one button pair, and SHALL NOT retry after mouse-down. `GET /api/v1/ui-state` SHALL expose the corresponding current rendered-client width, height, and coordinate origin so callers do not infer coordinates from a scaled desktop capture.

#### Scenario: Click a visible RimWorld control
- **WHEN** the RimWorld window is available and an authenticated caller clicks a point within its client bounds
- **THEN** the input is targeted to that RimWorld process and the result records the resolved screen/client coordinates and button events

#### Scenario: Resolve coordinates from the rendered client
- **WHEN** an authenticated caller reads UI state before clicking or dragging
- **THEN** the returned client-area dimensions use the same top-left client-pixel coordinate space accepted by the input endpoints

#### Scenario: Window target changed
- **WHEN** the recorded window handle no longer belongs to the RimWorld PID at injection time
- **THEN** the gateway returns `target_window_mismatch` and injects no input

#### Scenario: Foreground transition is transient
- **WHEN** Windows releases the exact RimWorld foreground window after movement but before mouse-down on the first click attempt
- **THEN** the gateway reacquires the same PID-owned window once, records `focus_reacquire`, and injects one click pair or fails closed without a pair

### Requirement: Bounded drag input
`POST /api/v1/input/may-maximize-window/drag` SHALL accept in-bounds start and end client coordinates, mouse button, bounded duration, and bounded interpolation steps; revalidate target ownership and focus during the gesture; and release any pressed button if focus or ownership is lost. Its timed interpolation loop SHALL run outside Unity's main-thread dispatcher so waiting between native input events does not freeze the game or prevent it from consuming those events.

#### Scenario: Drag across the map
- **WHEN** an authenticated caller supplies valid start/end points and duration while the RimWorld window remains targeted
- **THEN** the gateway emits an ordered press, bounded movement sequence, and release and returns their timing ledger

#### Scenario: Focus is lost mid-drag
- **WHEN** another process takes focus during an injected drag that requires foreground focus
- **THEN** the gateway stops movement, attempts to release the injected button, and returns `focus_lost`

#### Scenario: A drag has a visible duration
- **WHEN** a caller requests a multi-step drag lasting several seconds
- **THEN** the native input worker performs the bounded waits while Unity's main thread remains available to update and render the game

### Requirement: Key, chord, and text input
`POST /api/v1/input/may-maximize-window/keys` SHALL accept a bounded single key press, modifier chord, or text value, validate each requested key, target only the RimWorld process, release injected keys and modifiers after success or failure, and return the resolved event ledger.

#### Scenario: Press a RimWorld key binding
- **WHEN** an authenticated caller sends a valid space-key request while a colony is running
- **THEN** the gateway injects the key down/up sequence and reports the resulting pause state when observable

#### Scenario: Unsupported key is requested
- **WHEN** a caller submits a key name the input backend cannot resolve
- **THEN** the gateway returns `unsupported_key` and emits no partial chord

### Requirement: Raw input is serialized, off-main-thread, and cancellation-safe
Click, drag, key-chord, and text requests SHALL execute outside Unity's main-thread dispatcher through one serialized input lane per gateway runtime. No later raw-input request may inject an event until the earlier sequence has completed or attempted cleanup. HTTP-client disconnect and server-shutdown cancellation SHALL cancel a request waiting for that lane and SHALL interrupt a timed drag delay; if cancellation or failure occurs after any mouse button, key, modifier, or Unicode character was pressed, the gateway SHALL retain the lane while it attempts the corresponding release.

#### Scenario: A click arrives during a timed drag
- **WHEN** one request is holding a mouse button during a timed drag and another request submits a click
- **THEN** the click injects no event until the drag releases or completes its failure cleanup

#### Scenario: A client disconnects during a long drag
- **WHEN** request cancellation occurs after mouse-down while the drag is waiting between interpolated moves
- **THEN** the wait ends promptly, mouse-up is attempted exactly before another input sequence may begin, and the cancelled request does not continue the remaining moves

### Requirement: Discoverable semantic actions
`GET /api/v1/actions` SHALL expose each registered semantic action's name, description, argument schema, availability, and unavailable reason. `POST /api/v1/actions/{name}` SHALL invoke a registered, versioned semantic action on the main thread. Initial actions SHALL cover common pause, speed, window cancel/confirm, and native debug-tool cancellation operations. `debug.tool.cancel` SHALL be available only while `DebugTools.curTool` is active and SHALL clear that tool without invoking an unrelated top window. Action discovery SHALL remain separate from the deliberately narrow `/api/v1/ui-state` snapshot.

#### Scenario: Invoke an available pause action
- **WHEN** `game.pause` is advertised as available and an authenticated caller invokes it
- **THEN** the action executes directly on the main thread and the result records the before and after pause state

#### Scenario: Invoke an unavailable action
- **WHEN** a caller invokes a map-only action from the main menu
- **THEN** the gateway returns `action_unavailable` with the current prerequisite failure and does not simulate raw input

#### Scenario: Cancel a native pointer debug tool
- **WHEN** a map/pawn/world debug action has activated RimWorld's native `DebugTool` and the caller invokes `debug.tool.cancel`
- **THEN** that exact native tool is cleared and the result records active before and inactive after state

### Requirement: Explicit unsupported input behavior
Raw-input requests SHALL fail explicitly when the platform backend is unavailable, while semantic actions and unrestricted raw-C# execution remain usable. Version-one status SHALL NOT promise input-backend or window/client/DPI-geometry fields; a caller MAY use the raw-C# escape hatch for additional inspection, and any stable capability-discovery DTO requires a future OpenSpec/API change.

#### Scenario: Raw input backend is unsupported
- **WHEN** the gateway runs on a platform for which no raw-input backend is implemented
- **THEN** a click request returns `input_backend_unavailable` without disabling semantic actions or raw-C# execution
