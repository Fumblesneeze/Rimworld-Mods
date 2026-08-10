## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Stable package identity
The gateway project, runtime package constant, About manifest, deployment folder, shared test contracts, exact-mod launch matrices, and current operational documentation SHALL use package ID `fumblesneeze.rimworlddevgateway` consistently. The project and generated About manifest SHALL expose only the public author `Fumblesneeze`, not a private full name. They SHALL NOT retain a superseded package identity. An explicitly labeled historical evidence record MAY preserve the identity actually used by that old run only when it also states that the run predates and cannot verify the current identity.

#### Scenario: Build and launch the gateway package
- **WHEN** a contributor builds and launches the gateway in an isolated exact-mod RimWorld process
- **THEN** the project metadata, runtime status, About manifest, deployed folder, and active mod list identify it as `fumblesneeze.rimworlddevgateway`
- **AND** the project and generated About manifest identify the author as `Fumblesneeze`
- **AND** no duplicate legacy gateway package is staged or activated

### Requirement: Independent developer-only mod
RimWorld Dev Gateway SHALL be a separately loadable developer-only mod, and Immersive Chefs SHALL NOT declare it as a required dependency, optional dependency, assembly reference, or load-order dependency.

#### Scenario: Immersive Chefs loads without the gateway
- **WHEN** RimWorld starts with Core, Harmony, and Immersive Chefs but without RimWorld Dev Gateway
- **THEN** Immersive Chefs loads and initializes without a missing-dependency warning for the gateway

#### Scenario: Gateway loads without Immersive Chefs
- **WHEN** RimWorld starts with Core and RimWorld Dev Gateway but without Immersive Chefs
- **THEN** the gateway starts its API without resolving any Immersive Chefs type

### Requirement: Loopback-only server
The gateway SHALL host its HTTP API with EmbedIO 3.5.2 and bind it exclusively to IPv4 loopback `127.0.0.1`, SHALL reject any non-loopback peer, and SHALL expose no setting or request that can bind it to a wildcard, LAN, or public interface.

#### Scenario: Server starts on loopback
- **WHEN** the gateway initializes successfully with its default dynamic-port configuration
- **THEN** its session manifest reports an `http://127.0.0.1:<port>` base URL and no non-loopback listener exists

#### Scenario: Non-loopback binding is requested
- **WHEN** configuration or an API request attempts to supply a non-loopback bind address
- **THEN** the gateway rejects the value and does not open that listener

### Requirement: Mono-compatible packaged transport and evaluator
The gateway SHALL use the managed .NET Standard 2.0 assets from pinned EmbedIO 3.5.2 instead of Kestrel or a hand-written TCP/HTTP parser and SHALL use the .NET Framework asset from pinned Mono.CSharp 4.0.0.143 for its raw REPL. The generated `net48` mod package SHALL deploy compatible `EmbedIO.dll`, `Swan.Lite.dll`, `System.ValueTuple.dll`, and `Mono.CSharp.dll` runtime assemblies next to the gateway assemblies. Acceptance SHALL include proof in RimWorld's actual Unity Mono host rather than only the host-side test runtime.

#### Scenario: Generated package is inspected
- **WHEN** the Release mod package is assembled
- **THEN** it contains the expected EmbedIO 3.5.2 `EmbedIO.dll`, Unosquare.Swan.Lite 3.1.0 `Swan.Lite.dll`, System.ValueTuple 4.5.0 compatibility DLL, and Mono.CSharp 4.0.0.143 `Mono.CSharp.dll`, and does not require `Microsoft.AspNetCore.App`, Kestrel, Watson.Core, CavemanTcp, or Roslyn

#### Scenario: Transport starts in the real game host
- **WHEN** RimWorld starts an isolated Core-plus-gateway mod list and the host sends an authenticated status request followed by a raw C# expression
- **THEN** Unity Mono loads the packaged transport and evaluator dependencies, both requests succeed through EmbedIO, and the game log contains no missing-assembly, type-load, missing-method, compiler-initialization, or dependency-resolution error for the gateway runtime

### Requirement: Per-run bearer authentication
The gateway SHALL generate a cryptographically random bearer token and run ID for each process run, SHALL require that bearer token on every API route, SHALL never write the token to the RimWorld log or an API error, and SHALL compare submitted tokens without data-dependent early exit.

#### Scenario: Authenticated request
- **WHEN** a caller sends the current token as `Authorization: Bearer <token>` to a documented endpoint
- **THEN** the gateway authenticates the request and proceeds to endpoint validation

#### Scenario: Missing or stale token
- **WHEN** a caller omits the authorization header or uses a token from a previous process run
- **THEN** the gateway returns a structured `unauthorized` error without revealing any part of the current token

### Requirement: Discoverable session manifest
After its listener is ready, the gateway SHALL atomically publish a token-bearing session manifest and current-session locator beneath the active RimWorld save-data folder. The manifest SHALL contain API version, loopback URL, bearer token, process identity, run identity, game and mod versions, start time, and a developer-only warning.

#### Scenario: Host discovers a running gateway
- **WHEN** the listener has started and the host reads `DevGateway/current.json`
- **THEN** the referenced manifest describes the same live RimWorld process and provides the credentials needed for an authenticated status request

#### Scenario: Stale session is found on startup
- **WHEN** a manifest refers to a PID and process-start identity that is no longer live
- **THEN** the gateway removes its credential and retains at most a credential-free stale-session record

### Requirement: Versioned API and stable envelopes
The initial API SHALL use the exact route prefix `/api/v1`. Every JSON response SHALL contain `apiVersion` equal to `1`, a validated caller-supplied or generated `requestId`, `ok`, and `durationMs`, plus exactly one of `result` or a structured `error` containing stable `code`, bounded `message`, optional bounded `details`, and `retryable`.

#### Scenario: Successful version-one request
- **WHEN** an authenticated caller invokes a valid `/api/v1` endpoint and supplies `X-Request-Id: smoke-42`
- **THEN** the response envelope has `apiVersion: "1"`, `requestId: "smoke-42"`, `ok: true`, a `result`, and no `error`

#### Scenario: Unsupported API version
- **WHEN** an authenticated caller requests `/api/v2/status`
- **THEN** the gateway returns `unsupported_api_version` and identifies `/api/v1` as supported

### Requirement: Main-thread dispatch with bounded waits
The gateway SHALL execute all live Unity and RimWorld state access through a bounded main-thread dispatcher queue. Each operation SHALL carry its request ID, deadline, and HTTP-client cancellation signal; SHALL be skipped if its deadline expires or its caller disconnects before start; and SHALL return a distinct structured timeout result if the caller's response deadline expires after work starts. The runtime SHALL start at most one queued operation per Unity dispatcher phase and frame. A post-start timeout SHALL immediately log that the correlated operation may still be running. An operation that later finishes SHALL still be observed and SHALL emit a bounded request-correlated diagnostic identifying late success, cancellation, or the full internal exception and stack.

#### Scenario: Queued operation expires
- **WHEN** a request reaches its deadline while still waiting in the dispatcher queue
- **THEN** the operation does not touch game state and returns `timed_out_before_start`

#### Scenario: Operation response exceeds its bound
- **WHEN** an operation has started but no result is available by the response deadline
- **THEN** the network request returns `response_timeout_after_start` with the same request ID, signals cooperative cancellation, and immediately logs that the operation may still be running

#### Scenario: Caller disconnects before queued work starts
- **WHEN** the HTTP client cancels a request while its game operation is still waiting in the dispatcher
- **THEN** the cancellation reaches that operation and it never touches live game state later

#### Scenario: Several expensive operations are queued
- **WHEN** multiple admitted main-thread operations are ready during one Unity update phase
- **THEN** the runtime starts only the first operation in that phase and leaves the remainder queued for later frames

#### Scenario: Timed-out work faults later
- **WHEN** the HTTP response has returned `response_timeout_after_start` and that operation later throws
- **THEN** the exception is observed and the internal gateway log records its full stack, original request ID, and elapsed timing without attempting a second HTTP response

### Requirement: Crash and hang diagnostic trail
For every authenticated request admitted to the router, the gateway SHALL append a token-free `started` record and a terminal `completed` or `failed` record under the run directory and SHALL atomically replace `last-request.json`. While requests remain active, that atomic record SHALL identify the newest admitted active request in `started` state; when none remain active, it SHALL identify the most recently recorded terminal request. The isolated host smoke SHALL independently record its last outbound request before transmission, report whether the owned process exited and its exit code or remained alive after a timeout, and distinguish a suspected main-thread hang from an ordinary verification failure. Cleanup SHALL request graceful close and wait before termination; if the exact owned PID remains alive, the host SHALL attempt an external process dump and record `captured`, `failed`, `timed-out`, or `unavailable` before exact-PID force fallback.

#### Scenario: Unity blocks during an admitted request
- **WHEN** a request is recorded as started but Unity cannot finish it or answer another route before the host deadline
- **THEN** the evidence bundle names the request ID, method, path, start time, host timeout phase, process liveness, and dump outcome without requiring a gateway health response

#### Scenario: RimWorld crashes instead of hanging
- **WHEN** the exact launched process exits unexpectedly during verification
- **THEN** the evidence classifies a process exit, records the available exit code and last request, and does not report the process as a live hang

### Requirement: Published transport and resource bounds
The gateway SHALL publish and enforce finite limits for headers, request bodies, raw-C# source/result/diagnostics, optional assembly payloads, concurrent requests, dispatcher depth, response size, and endpoint timeouts, and SHALL reject unsupported transfer encodings and over-limit requests before enqueueing game work.

#### Scenario: Request body exceeds its limit
- **WHEN** an authenticated caller declares or sends a body larger than the published endpoint limit
- **THEN** the gateway returns a structured `request_too_large` error and no dispatcher command is enqueued

#### Scenario: Dispatcher queue is full
- **WHEN** an authenticated request arrives while the dispatcher is at its published capacity
- **THEN** the gateway returns a retryable `gateway_busy` error instead of growing the queue

### Requirement: Controlled shutdown and cleanup
The authenticated `POST /api/v1/server/shutdown` route and RimWorld process teardown SHALL stop accepting requests, cancel work that has not started, complete bounded response cleanup, dispose the listener, erase live credentials, and leave a credential-free stopped-session record. The route SHALL write and flush its `202 Accepted` response before it requests runtime teardown; listener cancellation SHALL NOT race and abort an already accepted shutdown response. If listener join, locator removal, tombstone creation, or another owned cleanup step fails, the runtime SHALL remain in a retryable stopping state, retain the corresponding transport/session ownership and active claim, reject an overlapping restart, and finalize the stopped state only after a later cleanup attempt succeeds.

#### Scenario: Authenticated shutdown
- **WHEN** a caller invokes the shutdown route with the current bearer token
- **THEN** the gateway delivers the complete correlated `202 Accepted` acknowledgement before closing the listener, removes `DevGateway/current.json`, and removes the bearer token from the run record

#### Scenario: Tombstone replacement is briefly blocked
- **WHEN** the owned run manifest has a transient Windows sharing violation while shutdown atomically replaces it with the stopped tombstone
- **THEN** the session manager classifies the exact sharing/lock failure and retains ownership; the Unity host retries once per rendered frame for a short bounded interval without sleeping, then keeps ownership while throttling later attempts; retained/recovered diagnostics use fixed bounded markers rather than formatting exception objects; cleanup completes without an error when the lock clears, and a persistent lock cannot permit an overlapping restart

#### Scenario: Cleanup fails before ownership is released
- **WHEN** listener join or current-locator removal fails during shutdown
- **THEN** the runtime retains the exact owned resource and session claim, rejects a new start, and permits a later stop attempt to finish cleanup without losing the original handle

#### Scenario: RimWorld exits without an API shutdown
- **WHEN** Unity invokes application teardown while the gateway is active
- **THEN** the same listener and credential cleanup runs without blocking process exit indefinitely

#### Scenario: Host verification finishes while RimWorld is still open
- **WHEN** the isolated smoke has completed its API shutdown and the exact launched RimWorld process remains alive
- **THEN** the host requests a normal main-window close and waits before considering dump capture and exact-PID force fallback

### Requirement: Redundant danger warnings
The mod SHALL state that its raw REPL and optional assembly fallback permit unrestricted code execution and are only for trusted local development in its About metadata, startup log, settings/about UI, session manifest, API status, and visible in-game gateway indicator. The status response SHALL report both `developerOnly: true` and `unrestrictedExecutionEnabled: true`.

#### Scenario: Developer checks that the gateway is active
- **WHEN** the mod is loaded and the developer views either the in-game indicator or authenticated status
- **THEN** the unrestricted-execution warning and enabled state are unambiguous
