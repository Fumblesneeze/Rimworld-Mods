## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Dedicated semantic game-state get and set
`GET /api/v1/game-state` SHALL return an immutable main-thread snapshot containing developer mode, god mode, pause state, native game speed, nullable current-map handle, nullable camera center/native zoom/view rectangle, and the bounded selected-thing summaries. `POST /api/v1/game-state` SHALL atomically set any supplied `devMode`, `godMode`, `paused`, or `speed` fields and return before/after snapshots. It SHALL reject an empty mutation and unknown fields.

God mode SHALL be enabled only while developer mode is enabled. A request MAY enable both together; a request that enables god mode while developer mode remains disabled SHALL fail before mutation. Disabling developer mode SHALL also disable god mode. Speed SHALL use `Paused`, `Normal`, `Fast`, `Superfast`, or `Ultrafast`; setting a non-paused speed SHALL unpause and setting `Paused` SHALL pause.

#### Scenario: Read and toggle development controls
- **WHEN** an authenticated caller gets game state and then posts `devMode: true` and `godMode: true`
- **THEN** both calls execute on the main thread and the mutation reports the exact before and after values

#### Scenario: Invalid god-mode transition
- **WHEN** developer mode is disabled and a mutation requests only `godMode: true`
- **THEN** the gateway returns `invalid_game_state` and changes neither flag

#### Scenario: Disable developer mode while god mode is active
- **WHEN** a mutation sets `devMode: false`
- **THEN** its after snapshot reports both developer mode and god mode disabled

### Requirement: Absolute camera movement and zoom
`POST /api/v1/camera` SHALL accept an optional absolute map center and/or native zoom value, require at least one, validate the current map handle when supplied, clamp neither an invalid cell nor an unsupported zoom silently, apply the complete change through `CameraDriver`, and return before/after camera state including the resulting view rectangle.

#### Scenario: Center and zoom on a thing
- **WHEN** a caller submits the current map handle, a valid center cell, and a supported native zoom
- **THEN** the camera moves and zooms deterministically and the after snapshot contains that thing's occupied cell in the view rectangle

#### Scenario: Camera request names an old map
- **WHEN** the request map handle differs from the current map
- **THEN** it returns `stale_map_handle` without moving or zooming the camera

### Requirement: Bounded current-map and current-view thing queries
`POST /api/v1/things/query` SHALL query only the current map with `scope` equal to `map` or `view`. View scope SHALL require the map camera and include a spawned thing when its occupied rectangle intersects the current camera view; map scope SHALL remain usable without a camera. The request MAY filter by exact Def names, coarse kinds, exact runtime type names, faction/relation, bounded case-insensitive label substring, fog state, and selected state. It SHALL cap each filter list at 64 entries, return at most 1,000 summaries, sort by stable-in-run ThingID handle, and paginate with an exclusive `after` handle while reporting `PageTruncated`.

Each summary SHALL contain only bounded scalar identity, label, Def/runtime type, coarse kind, current-map handle, position/occupied rectangle, rotation, stack count, faction, hit points, quality when present, fog/forbidden/selected state, and pawn flags. A primary ThingID within the 256-character handle bound SHALL be returned exactly and never truncated; an overlong or absent primary identity SHALL be excluded and counted in `ExcludedUnaddressableCount`. It SHALL NOT serialize live Thing or Pawn references.

A modded Thing whose summary getter throws during selection capture SHALL be omitted individually without erasing healthy selection summaries. The gateway SHALL record one bounded warning with the Thing handle when available, the current request ID, and the causal stack.

#### Scenario: List visible pawns matching a label
- **WHEN** a view query filters to pawns whose label contains a supplied substring
- **THEN** it returns only matching spawned pawns intersecting the view, ordered by handle and bounded by the requested limit

#### Scenario: Continue a truncated map query
- **WHEN** more matching things exist than the active limit
- **THEN** `PageTruncated` is true and submitting the final returned handle as `after` continues strictly after it without repeating an entry

#### Scenario: One selected modded Thing has a broken getter
- **WHEN** selection capture contains one Thing whose summary getter throws and another healthy Thing
- **THEN** the response retains the healthy summary and the correlated gateway log identifies the omitted Thing and causal exception

### Requirement: Bounded thing and pawn inspection
`GET /api/v1/things/{handle}` SHALL re-resolve a current-map thing by exact ThingID and return its query summary plus bounded description/inspect text, component type names, and applicable item, building, or pawn details. Pawn details SHALL remain scalar/bounded and include kind/race, faction, gender, biological/chronological age, dead/downed/drafted state, current job Def, skill levels, and bounded health-condition labels. Inspection SHALL reject missing, destroyed, off-map, or wrong-map handles and SHALL never recursively serialize game objects.

#### Scenario: Inspect a spawned pawn
- **WHEN** the caller inspects a pawn handle returned by a query
- **THEN** the response identifies the same pawn and returns bounded pawn/job/skill/health summaries without a cyclic object graph

#### Scenario: Thing was destroyed after discovery
- **WHEN** a previously returned handle no longer resolves on the current map
- **THEN** inspection returns `thing_not_found` rather than inspecting a different thing with a similar label or Def

### Requirement: Atomic semantic selection
`POST /api/v1/selection` SHALL accept `replace`, `add`, `remove`, `toggle`, or `clear` plus at most 200 exact thing handles, matching RimWorld 1.6's native selector capacity. It SHALL resolve every handle before mutation without requiring a camera, preserve caller order after removing duplicates, and use RimWorld's selector on the main thread. It SHALL return before/after bounded selection summaries. `clear` SHALL reject handles; all other operations except a valid empty `replace` SHALL require at least one handle.

#### Scenario: Select several queried things
- **WHEN** a caller replaces selection with three valid handles
- **THEN** all three things become selected in caller order and the returned after list contains exactly those handles

#### Scenario: One requested handle is stale
- **WHEN** an add request contains valid handles and one missing handle
- **THEN** it returns `thing_not_found` and leaves the complete prior selection unchanged

### Requirement: Discoverable native developer actions
`POST /api/v1/dev-tools/actions/query` SHALL enumerate bounded already-materialized leaf nodes from RimWorld's generated debug-action tree and return stable-in-run handles, full path, label, category, allowed game states, separate node runtime/delegate source types, bounded nullable discovery error, and invocation mode. It SHALL isolate a throwing mod-supplied descriptor getter to that descriptor. It SHALL traverse existing nodes breadth-first and SHALL NOT invoke lazy `childGetter` submenus during discovery; an unexpanded generated submenu SHALL be reported as `Unsupported` so it cannot freeze the Unity thread or starve later top-level actions. It SHALL support bounded path/label substring, category, game-state, and invocation-mode filters, sort by full path then handle, return at most 1,000 descriptors, and paginate with an exclusive handle.

`POST /api/v1/dev-tools/actions/{handle}/invoke` SHALL re-enumerate and fingerprint the exact node immediately before use. An immediate node SHALL run directly; `completed` SHALL mean its delegate returned, not that a dialog opened by it was subsequently completed. A native `ToolMap`, `ToolMapForPawns`, or `ToolWorld` node SHALL activate the exact native `DebugTool` and return its mode plus `pointerRequired: true`; the caller SHALL use the existing process-scoped input endpoint for the native click because RimWorld's tool closure reads `UI.MouseCell()` or `GenWorld.MouseTile()` and exposes no coordinate parameter, and MAY cancel it through the discoverable `debug.tool.cancel` semantic action. A stale, ambiguous, disabled, or unsupported node SHALL fail explicitly and SHALL NOT invoke another debug action. Known target operations MAY use separately specified typed adapters, but the gateway SHALL NOT claim that a generic debug-tool closure accepted a semantic cell when it did not.

#### Scenario: Apply an immediate debug action
- **WHEN** a caller invokes a currently available immediate action returned by discovery
- **THEN** the exact leaf action runs on the main thread and the result reports its path and completion

#### Scenario: Begin a map debug tool
- **WHEN** a caller invokes a supported map-cell debug tool
- **THEN** the exact native debug tool becomes active and the response reports `pointerRequired: true` so the caller can apply it with the process-scoped click endpoint

### Requirement: Deterministic direct developer spawning
`POST /api/v1/dev-tools/spawn` SHALL be a direct semantic alias for the tested version-one `quickstart.spawn` descriptor and SHALL accept its caller-idempotent bounded batch of named building, item, and pawn entries. The batch SHALL use an optional absolute `center` plus bounded per-entry `offset`; building/item entries SHALL include `defName`, count, optional `stuff` and quality, buildings MAY request `powerOn`, and pawn entries SHALL include `kindDefName` and count. The request SHALL retain the quickstart limits of 64 entries per section, 256 combined building/pawn instances, 5,000 units per item entry, and 32 pawns per entry.

The gateway SHALL resolve every Def, Stuff, quality, count, resulting cell, and placement prerequisite before mutation, use RimWorld's native creation/generation/spawn APIs, and return exact spawned handles, positions, warnings, and a mutation ledger. Equivalent retries with the same idempotency key SHALL return the original result; a changed request with the same key SHALL return `idempotency_conflict`.

#### Scenario: Spawn a steel object and two pawns
- **WHEN** a valid request names a stuffable building or item Def, Steel, a valid PawnKindDef, feasible center-relative cells, and an idempotency key
- **THEN** all entries are created, spawned, and returned by handle only after the whole request preflights successfully

#### Scenario: Spawn request contains an unknown Def
- **WHEN** any entry names a missing ThingDef or PawnKindDef
- **THEN** the request fails with `def_not_found` before spawning another valid entry

### Requirement: Bounded gizmo and architect-designator discovery
`POST /api/v1/gizmos/query` SHALL enumerate gizmos for the current selection or at most 256 explicit current-map owner handles, and SHALL optionally enumerate resolved designators for at most 64 exact architect category Def names. It SHALL return at most 1,000 stable-in-run descriptors containing handle/revision, source, owner handles, runtime type, label/description, disabled state/reason, hotkey, group key, observed toggle state, and interaction kind `immediate`, `toggle`, `target`, `placement`, `drag`, or `unsupported`.

The handle SHALL fingerprint the current map, owners/source, runtime type, stable order, and relevant command/designator identity. Invocation SHALL re-enumerate and compare that fingerprint so selection changes, map changes, owner destruction, command-list changes, or reordered ambiguous commands produce `stale_gizmo_handle` rather than applying a neighboring command.

A modded owner whose `GetGizmos()` or reverse-designator enumeration throws SHALL be skipped individually without erasing descriptors from healthy owners. The gateway SHALL record one bounded warning with the owner handle, current request ID, and causal stack.

#### Scenario: List gizmos for a selected building
- **WHEN** a building is selected and the caller queries selection gizmos
- **THEN** the result describes its currently visible commands, including disabled reasons and whether each completes immediately or requires a typed interaction

#### Scenario: List wall and zone designators
- **WHEN** the caller queries applicable architect categories
- **THEN** resolved build/zone designators report placement or drag semantics without opening the architect UI

#### Scenario: One gizmo owner throws
- **WHEN** a multi-owner query reaches one modded owner whose gizmo enumeration throws and one healthy owner
- **THEN** the healthy owner's descriptors remain in the response and a correlated warning identifies the failed owner without failing the whole query

### Requirement: Immediate, toggle, target, placement, and drag gizmo application
`POST /api/v1/gizmos/{handle}/invoke` SHALL revalidate the descriptor and run an enabled immediate or toggle gizmo directly, returning before/after toggle state when observable. A target, placement, or drag gizmo SHALL create exactly one active semantic interaction and return its stable handle, source handle, accepted input shapes, and map/owner/revision fingerprint. Unsupported or disabled gizmos SHALL fail explicitly without calling `ProcessInput`.

`GET /api/v1/interactions/current` SHALL return the active interaction or null. `POST /api/v1/interactions/{handle}/apply` SHALL accept exactly one matching thing, cell, explicit cell set, line, or rectangle input; resolve shapes to at most 4,096 unique cells; preflight every target using the native targeter/designator validator; then invoke the captured native callback or designator and report accepted/rejected targets plus completion state. `POST /api/v1/interactions/{handle}/cancel` SHALL cancel only the matching interaction. Native world-target commands and mod-defined multi-stage interactions SHALL be reported as unsupported until a typed adapter exists; native world debug tools remain available through the debug-action activation plus process-scoped pointer workflow.

Only one interaction MAY be active. Starting another SHALL return `interaction_in_progress`. Map changes, lost/destroyed owners, changed command fingerprints, or native cancellation SHALL return `stale_interaction` and clear it. The gateway SHALL NOT synthesize `Event.current`, guess a mod-defined callback, or silently use pixel input.

#### Scenario: Press an ordinary command button
- **WHEN** an enabled `Command_Action` descriptor is invoked
- **THEN** its exact action runs once and the response is terminal without an active interaction

#### Scenario: Toggle a command
- **WHEN** an enabled `Command_Toggle` is invoked
- **THEN** its exact toggle action runs once and the result reports observable before/after toggle state

#### Scenario: Target a thing with a command
- **WHEN** a target gizmo starts an interaction and the caller applies a valid queried thing handle
- **THEN** the captured native target callback receives that exact thing and the interaction completes

#### Scenario: Drag a wall or zone
- **WHEN** a drag designator interaction receives a valid bounded line or rectangle
- **THEN** the gateway deterministically expands and preflights the cells, invokes the native multi-cell designator, and returns the affected cells

#### Scenario: Custom gizmo cannot be classified safely
- **WHEN** a mod-defined Gizmo exposes no supported semantic contract
- **THEN** discovery reports `unsupported` with its runtime type and invocation refuses it, while raw C# remains available without a restart
