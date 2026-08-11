## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Unrestricted execution enabled by default
Unrestricted in-process execution SHALL be enabled whenever RimWorld Dev Gateway is loaded, including with default or absent settings. It SHALL require no restart-required enable switch, code allowlist, signature, per-call confirmation, predeclared automation, companion client, or assembly build beyond the per-run bearer token.

#### Scenario: First run with default settings
- **WHEN** the gateway starts without an existing settings file
- **THEN** authenticated status reports `unrestrictedExecutionEnabled: true` and the raw C# execution endpoint accepts valid work

#### Scenario: New verification need arises mid-run
- **WHEN** a developer needs a game operation not exposed by an existing endpoint or automation
- **THEN** the developer can submit raw C# directly to the running gateway without rebuilding the gateway, compiling a client-side assembly, or restarting RimWorld

### Requirement: Primary raw-text C# endpoint
`POST /api/v1/executions/csharp` SHALL accept the C# submission directly as a `text/plain` request body, permit only an absent charset or `charset=utf-8`, strictly decode UTF-8 without replacement characters, strip an optional UTF-8 BOM, and evaluate it through the main-thread dispatcher using bundled Mono.CSharp. This endpoint SHALL be the primary unrestricted-execution interface and SHALL NOT require the optional companion client.

#### Scenario: Evaluate an expression with direct HTTP
- **WHEN** an authenticated caller posts UTF-8 `text/plain` containing `1 + 2`
- **THEN** the normal API envelope reports a successful evaluation with `ResultSet: true`, `Value: "3"`, `Type: "System.Int32"`, and empty diagnostics

#### Scenario: Reject a JSON submission
- **WHEN** a caller posts the source using `application/json` instead of `text/plain`
- **THEN** the gateway returns `unsupported_media_type` before enqueueing evaluator work

#### Scenario: Reject malformed source bytes
- **WHEN** a `text/plain; charset=utf-8` body contains an invalid UTF-8 byte sequence
- **THEN** the gateway returns `invalid_utf8` before enqueueing evaluator work and does not alter REPL state

### Requirement: Serialized stateful REPL semantics
The gateway SHALL own one Mono.CSharp evaluator for the lifetime of a gateway run, serialize submissions, and evaluate each submission on the Unity main thread. Successful declarations, variables, methods, and `using` directives SHALL remain available to later submissions in the same run. The evaluator SHALL preload references and common namespaces for the gateway, RimWorld, Verse, Unity, framework, and compatible assemblies already loaded in the process.

#### Scenario: Reuse a declaration
- **WHEN** one submission successfully declares `var gatewayCounter = 40;` and a later submission evaluates `gatewayCounter + 2`
- **THEN** the later result is `42` without the caller resending the declaration

#### Scenario: Observe game-thread affinity
- **WHEN** a submission returns `Thread.CurrentThread.ManagedThreadId`
- **THEN** its value is the gateway's Unity main-thread identity rather than an EmbedIO worker-thread identity

#### Scenario: Concurrent callers submit work
- **WHEN** two authenticated raw evaluations are admitted concurrently
- **THEN** they are ordered by the bounded dispatcher/evaluator serialization and never execute evaluator state concurrently

### Requirement: REPL lifecycle and reset boundary
Version one SHALL keep evaluator state until controlled gateway shutdown or RimWorld process teardown and SHALL expose no independent state reset endpoint. A new gateway process SHALL construct a clean evaluator. A compile failure SHALL NOT implicitly create a new evaluator or erase earlier successful state.

#### Scenario: Compile error follows a valid declaration
- **WHEN** a successful declaration is followed by an invalid submission and then a valid expression using the earlier declaration
- **THEN** the invalid submission reports failure and the later expression can still use the earlier declaration

#### Scenario: Start a fresh gateway run
- **WHEN** the current gateway shuts down and RimWorld starts a new gateway run
- **THEN** declarations from the previous process are absent and the new session manifest identifies the fresh run

### Requirement: Bounded REPL source diagnostics and result
The raw route SHALL reject encoded source above 65,536 bytes, and the evaluator SHALL defensively reject decoded source above 65,536 characters. It SHALL cap compiler diagnostics at 16,384 characters, the invariant-culture result text at 65,536 characters, and runtime type text at 1,024 characters, using an explicit truncation marker for bounded textual fields. Its normal evaluation record SHALL expose `Succeeded`, `ResultSet`, optional `Value`, optional runtime `Type`, and `Diagnostics`, inside the standard bounded JSON envelope.

#### Scenario: Submission does not compile
- **WHEN** Mono.CSharp reports a compile error or incomplete submission within the source limit
- **THEN** the request completes with `ok: true`, `Succeeded: false`, no result value, and bounded compiler diagnostics when Mono.CSharp supplies them

#### Scenario: Source exceeds its evaluator limit
- **WHEN** encoded raw source exceeds 65,536 bytes or decoded source exceeds the evaluator's 65,536-character defensive limit
- **THEN** the gateway returns `csharp_source_too_large` without evaluating any prefix of it

#### Scenario: Expression produces a large textual value
- **WHEN** a successful expression's invariant textual representation exceeds 65,536 characters
- **THEN** the returned `Value` stays within the limit and ends with an explicit truncation marker

### Requirement: Arbitrary loaded-process access
Raw C# SHALL be permitted to use direct references or reflection against compatible RimWorld, Verse, Unity, framework, gateway, and optional-mod assemblies loaded in the process. It SHALL have the same operating-system authority as RimWorld and SHALL NOT claim to be sandboxed.

#### Scenario: Inspect and mutate game state
- **WHEN** authenticated raw C# reads a RimWorld property and performs a reversible game-state mutation
- **THEN** both operations run in-process on the main thread and the result can report the observed state without a gateway rebuild

### Requirement: Mono.CSharp packaged runtime and in-game proof
The gateway SHALL pin Mono.CSharp 4.0.0.143, package its compatible `Mono.CSharp.dll` beside the mod assembly, and SHALL NOT require Roslyn or the host .NET SDK in the Unity process. Acceptance SHALL prove evaluator initialization, state preservation, result/diagnostic handling, and game-state access inside RimWorld's actual Unity Mono host.

#### Scenario: Generated package is inspected
- **WHEN** the Release gateway package is assembled
- **THEN** it contains the expected Mono.CSharp 4.0.0.143 `Mono.CSharp.dll` and contains no Roslyn compiler assemblies

#### Scenario: Raw execution runs in Unity Mono
- **WHEN** an isolated Core-plus-gateway game receives two correlated raw submissions, with the second using state declared by the first
- **THEN** both succeed in the live RimWorld process and the game log contains no missing-assembly, type-load, missing-method, compiler-initialization, or dependency-resolution error for Mono.CSharp

### Requirement: Honest timeout and cancellation semantics
The gateway SHALL skip a raw or uploaded execution whose deadline passes before it starts, signal cooperative cancellation after a started execution exceeds its response deadline where the execution form can observe cancellation, and explicitly report that already-running unrestricted main-thread code cannot be forcibly preempted safely and may continue or freeze the game.

#### Scenario: Raw evaluation times out after start
- **WHEN** raw code does not return before its response deadline
- **THEN** the caller receives `response_timeout_after_start` and correlated logs record that the operation may still be running without widening the version-one status schema

### Requirement: Optional uploaded-assembly fallback
The gateway SHALL retain `POST /api/v1/executions/assembly` as an optional-to-call fallback for cases where Mono.CSharp's language surface or reference model is insufficient. It SHALL accept a bounded compatible .NET assembly payload, exact public static entry type and method, and bounded arguments; load it into the current AppDomain; and invoke only the declared entry point through the main-thread dispatcher. Availability or use of this fallback SHALL NOT be a prerequisite for the raw endpoint. The named type SHALL declare exactly one public static non-generic method with the requested case-sensitive name. That method SHALL return `string` and use either the legacy `(string requestJson)` signature or the context-aware `(string requestJson, GatewayAssemblyExecutionContext context)` signature; any overload ambiguity or other signature SHALL fail closed as `invalid_entry_point`.

The context-aware signature SHALL receive the correlated request ID, the dispatch operation's cooperative cancellation token, and a host-safe extension interface. The version-one extension interface SHALL allow uploaded code to register a uniquely named session-scoped automation, including its name, version, description, mutating flag, string-valued argument schema, prerequisites, and a handler receiving normalized argument JSON plus the automation cancellation token. Registration metadata SHALL permit at most 64 schema entries and 64 prerequisites; schema keys SHALL be at most 256 UTF-8 bytes, schema values and prerequisites at most 4,096 UTF-8 bytes each, and all descriptor metadata together at most 512 KiB UTF-8. Registration SHALL reject empty/null or over-limit metadata before mutating the registry. These per-registration ceilings keep a hostile uploaded enumerable bounded and one descriptor below the existing discovery-response budget. Registration SHALL take effect without restarting RimWorld, SHALL remain visible through automation discovery for that process only, and SHALL use the existing automation registry's duplicate-name, execution, and cancellation behavior. The synchronous automation route SHALL link its dispatch/request/deadline token into the run token so a cooperative started handler observes transport cancellation even though version one exposes no separate remote-cancel route.

An uploaded entry result SHALL be returned as `Value`, `Truncated`, and `OriginalUtf8Bytes`. `Value` SHALL contain at most 10 MiB of UTF-8 including `\n[truncated]` when truncation occurs, SHALL never split a UTF-16 surrogate pair, and SHALL be deterministic for the same input. Ten MiB is derived from the existing 64 MiB response ceiling: worst-case JSON escaping expands a one-byte control character to six bytes, leaving bounded envelope overhead. Session-automation arguments SHALL normalize to at most 1 MiB of JSON before entering uploaded code, and its string result SHALL use the same 10 MiB normalization. The legacy entry signature remains supported for existing host snippets but cannot observe cancellation after invocation; the context-aware signature can.

#### Scenario: Execute a valid fallback assembly
- **WHEN** an authenticated caller uploads a compatible assembly whose exact declared public static entry point returns normally
- **THEN** the gateway invokes it on the Unity main thread and returns its bounded correlated result

#### Scenario: Context-aware assembly registers a session automation
- **WHEN** a context-aware uploaded entry registers a valid session automation and returns
- **THEN** the response carries its correlated bounded result, automation discovery reports the registration as session-scoped, and the caller can run it without restarting the game

#### Scenario: Started context-aware execution is cancelled
- **WHEN** a context-aware uploaded entry is running and the HTTP request or response deadline cancels its dispatch operation
- **THEN** its context token is cancelled and an observed `OperationCanceledException` is preserved as cancellation rather than rewritten as an entry-point failure

#### Scenario: Entry contract is invalid
- **WHEN** the requested type or method is absent, non-static, ambiguous, generic, or has an unsupported signature
- **THEN** the gateway returns `invalid_entry_point` and does not invoke another method

### Requirement: Optional companion compiler and client
The companion host tool SHALL remain available to compile a supplied C# source file into a uniquely named .NET Framework-compatible fallback assembly using the installed host .NET SDK and explicit RimWorld, Unity, and gateway-contract references, then upload it. The tool SHALL be optional for raw REPL use, and the mod package SHALL NOT bundle Roslyn.

#### Scenario: Use newer host compiler syntax
- **WHEN** a developer deliberately selects the fallback for source supported by the host SDK but not Mono.CSharp
- **THEN** the optional tool compiles in an isolated temporary project, uploads the assembly, returns the correlated result, and removes the temporary project

#### Scenario: No companion tool is installed
- **WHEN** a developer has only an HTTP client and a live session manifest
- **THEN** the developer can still perform unrestricted stateful C# evaluation through `/api/v1/executions/csharp`

### Requirement: Optional assembly size bound and accounting
For the assembly fallback, the gateway SHALL reject an individual decoded assembly payload above 16 MiB and SHALL expose a monotonic upload count. It SHALL explain that loaded assemblies cannot be unloaded from the default AppDomain, but SHALL NOT impose an upload-rate or hard per-run total that forces a developer to restart the unrestricted gateway.

#### Scenario: Individual assembly is oversized
- **WHEN** a fallback upload decodes to more than 16 MiB
- **THEN** the gateway returns `assembly_too_large`, does not load the assembly, and leaves the upload count unchanged

#### Scenario: Many compatible fallback assemblies are uploaded
- **WHEN** the caller repeatedly submits individually valid assemblies during a long developer run
- **THEN** the gateway continues accepting them and increments the upload count without requiring a restart
