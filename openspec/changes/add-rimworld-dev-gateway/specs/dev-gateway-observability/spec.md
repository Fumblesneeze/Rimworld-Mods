## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Structured status snapshot
`GET /api/v1/status` SHALL return an immutable main-thread version-one snapshot with these fields: `developerOnly`, nullable `map`, `pendingDispatches`, `processId`, `programState`, `rootType`, nullable `tick`, `unrestrictedExecutionEnabled`, and `warning`. A non-null `map` SHALL contain only `Handle`, `Biome`, `Width`, and `Height`. The version-one status contract SHALL NOT imply game/mod-version, pause/speed, window-geometry, uploaded-assembly-count, per-operation, raw-input, or automation-health fields.

State outside this deliberately narrow contract MAY be inspected through the unrestricted raw-C# endpoint or a discoverable named automation. Adding another stable status field requires a later OpenSpec/API contract change rather than relying on an undocumented implementation detail.

#### Scenario: Status at the main menu
- **WHEN** an authenticated caller requests status while RimWorld is at the main menu
- **THEN** the result reports the current `processId`, program/root state, developer warning and execution flags, a nullable `tick`, `map: null`, and the current dispatcher pending count without dereferencing map-only state

#### Scenario: Status in a colony
- **WHEN** an authenticated caller requests status with a playable map loaded
- **THEN** the result reports the current tick and the map's stable-in-run handle, biome, width, and height without returning live Verse objects

### Requirement: Structured UI-state snapshot
`GET /api/v1/ui-state` SHALL return a main-thread version-one snapshot with exactly the stable baseline fields `programState`, `rootType`, `selection`, and `windows`. `selection` SHALL contain at most 256 entries with `Handle` and `Label`; `windows` SHALL contain at most 128 entries with `Handle`, `Type`, and `Modal`. The snapshot SHALL NOT walk or recursively serialize live Verse/Unity object graphs.

Map/view state, client geometry, pause/speed, semantic-action discovery, and richer selected-object details are not part of the version-one UI-state schema. Callers SHALL use `GET /api/v1/actions` for action discovery and MAY use raw C# or a named automation for additional inspection; promoting such data into this DTO requires a later OpenSpec/API contract change.

#### Scenario: Inspect an open inspect pane
- **WHEN** a pawn or building is selected and an authenticated caller requests UI state
- **THEN** the result identifies the selection by stable-in-run handle and label and reports the bounded window stack without promising inspect-pane internals

#### Scenario: UI state contains a cyclic game graph
- **WHEN** a selected object references a map, faction, and other cyclic Verse objects
- **THEN** the response contains only the selection handle/label and window handle/type/modal fields and completes without traversing the cyclic graph

### Requirement: Cursor-based structured logs
The gateway SHALL capture Unity and RimWorld log events emitted after gateway startup in a 2,000-entry sequence-numbered ring buffer. `GET /api/v1/logs` SHALL support an exclusive `after` sequence, clamp `limit` to 1–500 entries, and bound the serialized `Entries` array to 3 MiB. Each returned entry SHALL contain `Sequence`, `TimestampUtc`, `Severity`, a message bounded to 8 KiB of UTF-8, an optional stack bounded to 32 KiB of UTF-8, `Thread`, and an optional correlated `RequestId`.

The read result SHALL expose `Entries`, `OldestCursor`, `NewestCursor`, `HistoryEvicted`, and `PageTruncated`. `HistoryEvicted` SHALL mean the requested cursor predates retained history. `PageTruncated` SHALL mean additional matching retained entries were omitted because either the count or aggregate byte bound was reached. To continue a truncated page without skipping entries, callers SHALL pass the final returned entry's `Sequence` as the next exclusive `after` cursor rather than the ring-wide `NewestCursor`.

#### Scenario: Retrieve new logs after a cursor
- **WHEN** the caller requests logs after sequence 100 and entries 101 through 103 exist
- **THEN** the response returns those entries in ascending order, reports `PageTruncated: false`, and the final returned entry has sequence 103

#### Scenario: Aggregate page bound is reached
- **WHEN** more matching retained entries exist than fit within either 500 entries or the 3 MiB serialized-entry budget
- **THEN** the response returns the largest ascending prefix within the active bound and reports `PageTruncated: true` so the caller can resume after the final returned sequence

#### Scenario: Requested cursor was evicted
- **WHEN** the caller's `after` sequence predates the oldest retained ring entry
- **THEN** the response reports `HistoryEvicted: true` and starts with the oldest retained entry independently of whether the returned page is also truncated

### Requirement: Correlated operation logging
Every gateway request and main-thread operation SHALL carry one request ID through admission, dispatch, progress, completion, timeout, and exception logging, while authentication failures SHALL be logged without token material.

#### Scenario: Raw execution throws
- **WHEN** a raw C# submission throws during request `exec-7`
- **THEN** both the error response and captured exception log contain request ID `exec-7` and neither contains the bearer token

### Requirement: End-of-frame screenshots
`POST /api/v1/screenshots` SHALL capture the current RimWorld client at end-of-frame on the main thread, encode a valid PNG, return capture dimensions and request metadata, and release temporary Unity resources. The endpoint SHALL enforce bounded concurrent captures, dimensions, response size, and timeout.

#### Scenario: Capture a playable scene
- **WHEN** an authenticated caller requests a screenshot while a map is rendered
- **THEN** the response provides a decodable PNG for that frame with matching width, height, and request ID metadata

#### Scenario: Capture capacity is exhausted
- **WHEN** a screenshot request arrives while the configured capture concurrency is occupied
- **THEN** the gateway returns a retryable `capture_busy` error instead of allocating another full-frame texture
