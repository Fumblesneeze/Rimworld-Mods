# RimWorld Dev Gateway

RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) is an intentionally dangerous, development-only RimWorld 1.6 mod. Whenever it is loaded, unrestricted in-process C# and uploaded-assembly execution are enabled. There is no safety switch, allowlist, confirmation dialog, or sandbox.

Use it only in a disposable isolated `-savedatafolder`. Uploaded code runs with the RimWorld process's full access, can corrupt game state or files, and cannot be unloaded from the AppDomain. Code that blocks the main thread can freeze the game even after the HTTP caller times out.

## Transport and threat model

The in-process server is EmbedIO 3.5.2 in managed-listener mode, bound only to a dynamically selected IPv4 `127.0.0.1` port. EmbedIO owns HTTP parsing and connection handling; the mod does not implement a raw HTTP parser. Kestrel is not used because current Kestrel expects the ASP.NET Core shared framework and a modern CoreCLR host, while RimWorld 1.6 loads `net48`-compatible mods in Unity Mono. EmbedIO supports the target runtime with a much smaller dependency graph.

The transport graph is pinned to EmbedIO 3.5.2, `Unosquare.Swan.Lite` 3.1.0, and `System.ValueTuple` 4.5.0. Direct in-process C# uses the separately pinned `Mono.CSharp` 4.0.0.143 package. The packaged runtime assemblies are exactly:

- `RimWorldDevGateway.dll`
- `RimWorldDevGateway.Contracts.dll`
- `RimWorldDevGateway.IntegrationTesting.dll`
- `EmbedIO.dll`
- `Mono.CSharp.dll`
- `Swan.Lite.dll`
- `System.ValueTuple.dll`

Every route requires a fresh 256-bit bearer token. The listener rejects non-loopback peers and requests containing an `Origin` header, so use the companion client, `curl.exe`, or another native HTTP client rather than browser JavaScript. Authentication limits accidental cross-process calls; it does not protect against another process running as the same Windows user that can read the manifest.

## Start and discover a session

The safest complete check is:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -DryRun -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -TimeoutSeconds 180
```

For an already running manual or isolated game, the companion client is an optional convenience for validated discovery, typed requests, screenshots, and host-SDK assembly compilation. Build it with:

```powershell
dotnet build .\tools\RimWorldDevGateway.Client\RimWorldDevGateway.Client.csproj -c Release
$gatewayClient = '.\artifacts\HostTools\Release\net480\RimWorldDevGateway.Client.exe'
```

The gateway atomically publishes `<active save-data folder>\DevGateway\current.json`. A normal game therefore uses:

```text
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\DevGateway\current.json
```

An isolated run uses `<run>\SavedData\DevGateway\current.json`. Select it with `--manifest` or set `RIMWORLD_DEV_GATEWAY_MANIFEST`. Add `--pid` whenever the launched process ID is known:

```powershell
$manifest = 'C:\path\to\run\SavedData\DevGateway\current.json'
$pidExpected = 12345
& $gatewayClient discover --manifest $manifest --pid $pidExpected -o json
& $gatewayClient status --manifest $manifest --pid $pidExpected -o json
```

The client validates API version, active state, token presence, exact PID and process start time, and an IPv4-loopback `/api/v1` base URL. `discover` deliberately omits the token from its output. Client exit codes are `0` success, `1` runtime/request failure, and `2` invalid usage; `--output table` is the default and `-o json` preserves API JSON.

For direct calls, load the sensitive manifest locally and supply a correlated request ID:

```powershell
$session = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
$authorization = "Authorization: Bearer $($session.token)"

curl.exe --silent --show-error --fail-with-body `
  -H $authorization `
  -H 'X-Request-Id: docs-status' `
  "$($session.baseUrl)/status"

curl.exe --silent --show-error --fail-with-body `
  -H $authorization `
  -H 'X-Request-Id: docs-speed' `
  -H 'Content-Type: application/json' `
  --data-raw '{"arguments":{"speed":"normal"}}' `
  "$($session.baseUrl)/actions/game.speed"

curl.exe --silent --show-error --fail-with-body `
  -H $authorization `
  -H 'X-Request-Id: docs-csharp' `
  -H 'Content-Type: text/plain; charset=utf-8' `
  --data-binary 'Current.ProgramState.ToString()' `
  "$($session.baseUrl)/executions/csharp"
```

Do not log, commit, or paste `current.json`; it is a live unrestricted-execution credential.

## API and client surface

All routes are below the manifest's `/api/v1` `baseUrl`.

| Route | Companion command | Purpose |
| --- | --- | --- |
| `GET /status` | `status` | Process/program/root, optional map/tick, pending dispatches, and danger flags |
| `GET /ui-state` | `ui-state` | Bounded window and selection snapshot |
| `GET /game-state` | direct HTTP | Developer/god mode, time, nullable map/camera, and bounded selection snapshot |
| `POST /game-state` | direct HTTP | Atomically set developer/god mode, pause, and native speed |
| `POST /camera` | direct HTTP | Set an absolute current-map center and/or native root size |
| `POST /things/query` | direct HTTP | Bounded current-map or current-view thing query |
| `GET /things/{handle}` | direct HTTP | Bounded thing/building/item/pawn inspection |
| `POST /selection` | direct HTTP | Atomic replace/add/remove/toggle/clear selection |
| `GET /logs?after=N&limit=N` | `logs --after N --limit N` | Cursor-based structured Unity/RimWorld logs |
| `POST /screenshots` | `screenshot --file PATH` | End-of-frame PNG capture |
| `POST /input/click` | `click --x N --y N [--button left|right|middle]` | Process-scoped client-coordinate click |
| `POST /input/drag` | `drag --start-x N --start-y N --end-x N --end-y N` | Process-scoped interpolated drag |
| `POST /input/keys` | `keys (--key K\|--text TEXT) [--modifiers Ctrl,Shift]` | Chord or Unicode text input |
| `GET /actions` | direct HTTP | Discover semantic actions, schemas, and availability |
| `POST /actions/{name}` | `action NAME [--arguments JSON]` | Invoke a semantic action on the main thread |
| `POST /executions/csharp` | direct HTTP | Evaluate raw UTF-8 C# in the stateful in-process REPL |
| `POST /executions/assembly` | `execute-source ...` | Compile on the host, upload, and invoke an assembly |
| `GET /automations` | `automations` | Discover named automations and availability |
| `POST /automations/{name}/runs` | `run NAME [--arguments JSON]` | Run synchronously and return a terminal automation run; `automations run` is an alias |
| `POST /defs/export` | direct HTTP | Bounded deterministic JSON snapshots from finalized live Def databases |
| `GET /integration-tests` | direct HTTP | Startup-gated in-game integration-test state and immutable results |
| `POST /dev-tools/actions/query` | direct HTTP | Discover bounded native debug-action leaves |
| `POST /dev-tools/actions/{handle}/invoke` | direct HTTP | Invoke an immediate action or activate its native pointer tool |
| `POST /dev-tools/spawn` | direct HTTP | Idempotent direct alias for the preflighted `quickstart.spawn` engine |
| `POST /gizmos/query` | direct HTTP | Discover selected/owned gizmos and architect designators |
| `POST /gizmos/{handle}/invoke` | direct HTTP | Invoke immediate/toggle or begin one typed interaction |
| `GET /interactions/current` | direct HTTP | Return the active typed interaction or `null` |
| `POST /interactions/{handle}/apply` | direct HTTP | Apply a thing/cell/cells/line/rectangle input |
| `POST /interactions/{handle}/cancel` | direct HTTP | Cancel only the matching active interaction |
| `POST /server/shutdown` | `shutdown` | Controlled listener shutdown and credential cleanup |

Routed JSON API envelopes use version 1 and report the request ID, `ok`, duration, and either `result` or a stable error. Every response also carries `X-Request-Id`, including authentication and transport-limit errors. A caller-provided ID must be 1–64 letters, digits, `-`, `_`, `.`, or `:`; otherwise the server generates one.

The version-one status result remains intentionally narrow: `developerOnly`, nullable `map`, `pendingDispatches`, `processId`, `programState`, `rootType`, nullable `tick`, `unrestrictedExecutionEnabled`, and `warning`. A non-null map has `Handle`, `Biome`, `Width`, and `Height`. The UI-state result contains `programState`, `rootType`, up to 256 selection entries (`Handle`, `Label`), and up to 128 window entries (`Handle`, `Type`, `Modal`). Richer developer controls live in the dedicated `/game-state`, `/camera`, `/things`, and `/selection` contracts; they do not silently widen `/status` or `/ui-state`. Versions come from the session manifest, and action schemas and availability come from their discovery routes.

Use the raw C# endpoint or a named automation when a verification needs state outside those stable DTOs. That escape hatch does not promote the returned data into the API contract; adding a new stable snapshot field requires an OpenSpec/API change.

Log results contain `Entries`, `OldestCursor`, `NewestCursor`, `HistoryEvicted`, and `PageTruncated`. Entries contain `Sequence`, `TimestampUtc`, `Severity`, `Message`, optional `Stack`, `Thread`, and optional `RequestId`. `HistoryEvicted` means the requested cursor is older than retained history. `PageTruncated` means more matching retained entries exist than fit the 500-entry or 3 MiB serialized-entry page. Resume a truncated read with the final returned entry's `Sequence` as `--after`; do not use the ring-wide `NewestCursor`, which could skip entries.

Useful client examples:

```powershell
& $gatewayClient ui-state --manifest $manifest --pid $pidExpected -o json
& $gatewayClient logs --after 0 --limit 100 --manifest $manifest --pid $pidExpected -o json
& $gatewayClient screenshot --file '.\artifacts\manual-gateway.png' --manifest $manifest --pid $pidExpected
& $gatewayClient action game.pause --arguments '{"paused":true}' --manifest $manifest --pid $pidExpected -o json
& $gatewayClient action game.speed --arguments '{"speed":"fast"}' --manifest $manifest --pid $pidExpected -o json
& $gatewayClient click --x 1000 --y 100 --manifest $manifest --pid $pidExpected -o json
& $gatewayClient keys --key Escape --manifest $manifest --pid $pidExpected -o json
```

Initial semantic actions are `game.pause` (optional boolean `paused`, omitted to toggle), `game.speed` (`paused`, `normal`, `fast`, `superfast`, or `ultrafast`), `window.accept`, `window.cancel`, and `debug.tool.cancel`. The last action is available only while a native `DebugTool` pointer action is active and clears that exact native tool instead of cancelling an unrelated window. Discovery is authoritative: an action may be present but unavailable at the current menu/window/game state. Prefer semantic actions to raw input. Raw input is Windows-only, revalidates the RimWorld PID/window/client bounds, and can optionally skip activation with `--no-activate`.

All raw click, drag, chord, and text injection runs on HTTP workers through one serialized input lane, not Unity's main thread. A click cannot interleave with an active drag, and request/server cancellation interrupts timed drag waits while retaining the lane through best-effort mouse/key release. This lets RimWorld process injected events without a long gesture freezing the game; live Verse and Unity state operations still go through the bounded dispatcher.

## Semantic game control and inspection

All operations in this section are ordinary authenticated HTTP requests and run through the Unity main-thread dispatcher. Request property names are case-sensitive camel case. Result DTO properties retain their .NET names in the JSON writer.

Read the complete control snapshot, then make an atomic partial mutation:

```powershell
curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'X-Request-Id: docs-game-state-read' `
  "$($session.baseUrl)/game-state"

curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'Content-Type: application/json' `
  -H 'X-Request-Id: docs-game-state-set' `
  --data-raw '{"devMode":true,"godMode":true,"speed":"Fast"}' `
  "$($session.baseUrl)/game-state"
```

`speed` is `Paused`, `Normal`, `Fast`, `Superfast`, or `Ultrafast`. God mode requires developer mode; one request may enable both. Disabling developer mode also disables god mode. Forced pause is reported and prevents a request from falsely claiming that the game resumed. `GET /game-state` also returns nullable `CurrentMapHandle` and `Camera`, plus up to 256 bounded selected-thing summaries.

Move the current-map camera with absolute coordinates and RimWorld's native `CameraDriver.RootSize`:

```json
{
  "mapHandle": "map-17",
  "center": { "x": 100, "z": 100 },
  "rootSize": 24
}
```

At least `center` or `rootSize` is required. A supplied map handle must still be current; cells and native root size are validated rather than silently clamped. The result contains before/after center, zoom label, root size, map dimensions, supported root-size range, and view rectangle.

Query map things without recursively serializing live Verse objects:

```powershell
$things = curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'Content-Type: application/json' `
  -H 'X-Request-Id: docs-things' `
  --data-raw '{"scope":"view","kinds":["pawn"],"labelContains":"Ada","limit":50}' `
  "$($session.baseUrl)/things/query" | ConvertFrom-Json

$handle = $things.result.Things[0].Handle
curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'X-Request-Id: docs-inspect' `
  "$($session.baseUrl)/things/$([uri]::EscapeDataString($handle))"
```

`scope` is `map` or `view`; view inclusion uses the thing's occupied rectangle and requires the current map camera, while map scope and selection do not. Exact list filters are `defNames`, `kinds`, `runtimeTypes`, `factionNames`, and `factionRelations`; scalar filters are case-insensitive `labelContains`, `fogged`, and `selected`. Each list has at most 64 values, `labelContains` at most 256 characters, and `limit` at most 1,000. Results are ordered by the exact primary `ThingID` handle, which is never truncated. Continue an exclusive page with `after` equal to the last returned handle when `PageTruncated` is true. `ExcludedUnaddressableCount` reports spawned objects such as transient motes without a ThingID and the defensive exclusion of an identity above the 256-character handle bound.

The primary handle remains valid only while that exact object is spawned on the current map in this game run. Inspection, selection, and gizmo owner/thing-target resolution also accept RimWorld's legacy `GetUniqueLoadID()` form returned as `LoadId`; they never resolve by label or Def name. Query results always expose the normalized primary handle. Inspection bounds description/inspect text, component names, and item/building/pawn details and returns per-field warnings when a mod getter failed or output was truncated.

Selection mutations use this body:

```json
{ "operation": "replace", "handles": ["Pawn_17", "Building_42"] }
```

Operations are `replace`, `add`, `remove`, `toggle`, and `clear`. There may be at most 200 handles, matching RimWorld 1.6's native `Selector.MaxNumSelected` capacity. `clear` rejects handles; other operations require handles except an empty `replace`, which clears selection. Every handle is resolved before mutation, duplicates are removed in caller order, and a stale member leaves the complete previous selection unchanged.

## Native developer actions and direct spawning

Native debug actions are ephemeral generated tree leaves. Query them narrowly, retain the returned handle, and invoke only an exact expected path:

```powershell
$debugActions = curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'Content-Type: application/json' `
  -H 'X-Request-Id: docs-debug-query' `
  --data-raw '{"search":"Log pathfinder state","categories":["Pathing"],"modes":["Immediate"],"limit":10}' `
  "$($session.baseUrl)/dev-tools/actions/query" | ConvertFrom-Json
```

Query filters are bounded `search`, `categories`, `allowedGameStates`, `modes`, `after`, and `limit` (maximum 1,000). Descriptors expose the node `RuntimeType`, delegate `SourceType`, and a bounded nullable `DiscoveryError`; a failing mod visibility/toggle getter makes only that descriptor unavailable. Handles fingerprint the exact path/source/action identity and are re-enumerated immediately before use. Discovery traverses only RimWorld's already materialized tree and deliberately does not call lazy `childGetter` submenus: potentially huge generated menus are returned as `Unsupported`, and direct spawning or raw C# is used for their parameterized operations. This prevents one generated menu from blocking the Unity thread or starving later top-level actions.

`Immediate` means that the exact action delegate returned; it does not claim that a dialog opened by that delegate has already been completed. Continue such a dialog through `window.accept`, `window.cancel`, raw input, or raw C#. `MapPointer`, `PawnPointer`, and `WorldPointer` actions activate RimWorld's exact native `DebugTool` and return `PointerRequired: true`; apply that native tool using `/input/click`, then use `debug.tool.cancel` when it must be abandoned. The gateway does not claim that the debug closure accepted a semantic cell because it actually reads `UI.MouseCell()` or `GenWorld.MouseTile()` on the pointer event. Disabled, stale, ambiguous, and unsupported leaves fail explicitly.

For named deterministic setup, `POST /dev-tools/spawn` accepts the same envelope and exact version-one arguments as `quickstart.spawn`:

```json
{
  "arguments": {
    "version": 1,
    "center": { "x": 100, "z": 100 },
    "items": [
      { "defName": "Steel", "count": 75, "offset": { "x": -2, "z": 0 } }
    ],
    "pawns": [
      { "kindDefName": "Colonist", "count": 1, "offset": { "x": 2, "z": 0 } }
    ]
  },
  "idempotencyKey": "docs-direct-spawn-v1"
}
```

It performs the same all-feasible preflight, native creation/spawn, bounds, result handles, warnings, progress, artifacts, and mutation ledger documented under `quickstart.spawn`. An equivalent retry returns the original terminal run. Reusing the key with changed arguments returns `idempotency_conflict`.

## Gizmos, targeting, placement, and drag

Gizmo discovery uses either the current selection or explicit current-map ThingID owners and may include architect categories without opening the architect UI:

```json
{
  "ownerScope": "selection",
  "architectCategoryDefNames": ["Zone", "Structure"],
  "limit": 200
}
```

Use `ownerScope: "explicitOwners"` with `ownerHandles` for explicit owners. The limits are 256 owners, 64 exact `DesignationCategoryDef` names, and 1,000 returned descriptors. Descriptors contain revision, source, owners, runtime type, bounded label/description, disabled state/reason, hotkey, group key, observed toggle state, interaction kind, and accepted inputs. A handle fingerprints the current map, owners, source, ordered list, and native identity. Invocation re-enumerates the list, so a map/selection/owner/order change returns `stale_gizmo_handle` instead of pressing a neighboring command.

`Immediate` and `Toggle` invocations complete directly; toggles report observable before/after state. `Target`, `Placement`, and `Drag` create the one active semantic interaction and return its handle, source handle, accepted input shapes, map handle, owners, and revision. Inspect it with `GET /interactions/current`, then submit exactly one matching shape:

```json
{ "kind": "thing", "thingHandle": "Pawn_17" }
```

```json
{ "kind": "cell", "cell": { "x": 101, "z": 99 } }
```

```json
{
  "kind": "line",
  "start": { "x": 100, "z": 100 },
  "end": { "x": 106, "z": 100 }
}
```

```json
{
  "kind": "rectangle",
  "cornerA": { "x": 100, "z": 100 },
  "cornerB": { "x": 103, "z": 102 }
}
```

`cells` accepts an explicit `cells` array. Lines use deterministic Bresenham expansion; rectangles are inclusive and filled; duplicates are removed; one interaction resolves to at most 4,096 unique cells. Every target is preflighted through the native targeter/designator, and the result distinguishes accepted and rejected targets. Cancel only the exact current handle. Starting another interaction returns `interaction_in_progress`; map, owner, or command-list changes make it stale.

Typed version-one placement covers designators whose complete meaning is a cell set. A wall/build designator that still needs a Stuff or rotation choice is not applied through this typed route; use a named `quickstart.spawn` descriptor or raw C# for that development setup. Zone/build adapters can be added later once their extra choices have explicit schemas. World targeting and mod-defined multi-stage GUI interactions remain unsupported.

The safe native adapters cover ordinary `Command_Action`, `Command_Toggle`, local `Command_Target`, and cell-based `Designator` operations. Group-only UI semantics, abilities/verbs/world targets, sliders, custom `GizmoOnGUI`, right-click menus, build material/rotation choice, and multi-stage targeting are intentionally `unsupported`; use raw C# or process-scoped input, then add a tested typed adapter when that workflow becomes recurring.

## Unrestricted C# and assembly execution

### Direct stateful C#

`POST /executions/csharp` is the lowest-friction escape hatch and does not require the companion client or host .NET SDK. Send raw UTF-8 source as `text/plain`, either without a charset or with `charset=utf-8`; another media type, charset, or malformed UTF-8 is rejected before main-thread dispatch. The route returns `Succeeded`, `ResultSet`, invariant-culture stringified `Value`, runtime `Type`, and bounded compiler `Diagnostics` in the normal JSON envelope.

The REPL is stateful for the life of one gateway session. Imports, declarations, and variables from a successful submission remain available to later submissions:

```powershell
curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'Content-Type: text/plain; charset=utf-8' `
  -H 'X-Request-Id: docs-csharp-declare' `
  --data-binary 'var gatewayCounter = 40;' `
  "$($session.baseUrl)/executions/csharp"

curl.exe --silent --show-error --fail-with-body `
  -H $authorization -H 'Content-Type: text/plain; charset=utf-8' `
  -H 'X-Request-Id: docs-csharp-read' `
  --data-binary 'gatewayCounter + 2' `
  "$($session.baseUrl)/executions/csharp"
```

At REPL creation the gateway initializes the framework namespaces first, probes whether the host has already imported `Verse.Current`, and adds `Assembly-CSharp` only when that host fallback is needed. It then references Unity, the gateway, and compatible loaded AppDomain assemblies once per simple assembly identity, and preloads `System`, common collections/LINQ/reflection/threading namespaces, `RimWorld`, `Verse`, `UnityEngine`, and `RimWorldDevGateway`. This ordering avoids Unity Mono importing `Assembly-CSharp` twice while still allowing direct game, gateway, EmbedIO, and compatible optional-mod access.

Mono.CSharp 4.0.0.143 uses a C# 6 baseline and has only partial C# 7 support; it is not current Roslyn, so do not rely on records, file-scoped namespaces, nullable-reference syntax, or other newer constructs. Use the assembly-upload path when newer compiler features or a conventional project are more useful.

There is no reset endpoint. Restart the disposable RimWorld process to discard REPL declarations and dynamically emitted assemblies. Compilation failures normally return HTTP success with `Succeeded: false` and diagnostics; an executing snippet can still throw, hang the main thread, mutate anything accessible to RimWorld, or terminate the process.

### Host-compiled assembly upload

`execute-source` creates a unique temporary `net48` project on the host, references `Assembly-CSharp*.dll`, `Unity*.dll`, and the gateway contract assembly, invokes the installed .NET SDK, uploads the resulting DLL, and removes the temporary directory. This path uses the host compiler; the in-game `Mono.CSharp.dll` exists only for the direct REPL endpoint, and Roslyn is not shipped in the mod.

The exact entry contract is a declared public static method taking and returning one string. The client defaults the method name to `Execute` and the request string to `{}`:

```csharp
using System.Threading;
using Verse;

namespace Inspect;

public static class Entry
{
    public static string Execute(string requestJson)
    {
        return "state=" + Current.ProgramState + ";thread=" + Thread.CurrentThread.ManagedThreadId;
    }
}
```

Compile, upload, and invoke it while the same game process remains running:

```powershell
& $gatewayClient execute-source .\Inspect.cs `
  --managed 'F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed' `
  --contract '.\artifacts\HostTools\Release\net480\RimWorldDevGateway.Contracts.dll' `
  --entry-type 'Inspect.Entry' `
  --entry-method Execute `
  --request-json '{"probe":"manual"}' `
  --manifest $manifest `
  --pid $pidExpected `
  -o json
```

The method runs through the Unity main-thread dispatcher. Its returned string is placed in the API result. The server only checks the signature and upload bound; it intentionally does not inspect, authorize, sandbox, or unload the assembly.

## Finalized Def export

Use `POST /defs/export` when an XML patch needs to target the actual values produced by one exact active mod list. RimWorld performs XML inheritance, conditional loading, PatchOperations, parsing, cross-reference resolution, and post-load processing before these objects reach `DefDatabase<T>`; the Gateway reads those final databases on the main thread rather than approximating that pipeline in a host test.

The filters are exact and intersect: fully qualified database type, `defName`, and declaring source package ID. Optional `fieldNames` selects exact projectable top-level fields; a missing/non-projectable requested name produces `requested_field_not_found`. Results sort by database type and ordinal Def name. A truncated response carries an opaque exclusive `NextCursor`; pass it back unchanged as `cursor` to continue. Version 1 encodes database type and `defName` as two separately canonical Base64/strict-UTF-8 segments, so newlines and Unicode round-trip without a separator ambiguity. Malformed, legacy, or non-canonical cursors are rejected, and a live Def with an unpaired-surrogate, empty, or overlong paging identity is omitted with `def_identity_not_reversible`.

```powershell
$request = @{
    format = 'json'
    defTypes = @('Verse.ThingDef')
    defNames = @('Steel')
    sourcePackageIds = @('ludeon.rimworld')
    fieldNames = @('label', 'statBases', 'modExtensions')
    pageSize = 1
} | ConvertTo-Json -Compress

curl.exe --silent --show-error --fail-with-body `
  -H $authorization `
  -H 'X-Request-Id: docs-def-steel' `
  -H 'Content-Type: application/json' `
  --data-raw $request `
  "$($session.baseUrl)/defs/export" `
  --output '.\artifacts\steel-finalized-def.json'
```

Each item contains its database and concrete runtime type, name, raw label, retained declaring package/mod/file metadata, a bounded field projection, and per-Def warnings. Nested Defs become typed name references, repeated objects become reference markers, `[Unsaved]` and static fields are omitted, and collections, strings, depth, nodes, reflection work, and serialized bytes are bounded. The field walker allows 512 projectable fields and counts at most 4,096 reflected members before static/`[Unsaved]`/duplicate exclusions. One `RuntimeType.GetFields` call still returns a declared-member array synchronously and cannot be preempted partway; cancellation and work accounting resume immediately after that CLR/Mono call.

Runtime strings allow 65,536 characters and recognized materialized collections allow 1,024 entries. Each complete top-level field key/value entry is measured with the real JSON writer against 1 MiB. Each complete item—including metadata and warnings—is measured against 8 MiB. A field overflow becomes a named limit marker; an aggregate item overflow retains a deterministic sorted field prefix and `$byteLimit`. The complete successful Def-export HTTP envelope is capped at 32 MiB and 500,000 serialized JSON nodes. If the next item would cross either cap, it is omitted, `Truncated` is set, and `NextCursor` points after the last included item so the omitted item begins the next page. Measurements include JSON escaping, structural node expansion, cursor, request ID, and envelope overhead; worst-case escaped surrogate code units and dense dictionary wrappers therefore cannot evade the response policy.

Def-export request JSON has no lower endpoint aggregate cap than the Gateway's 32 MiB transport request-body policy. Its work remains bounded by the schema's filter, selector, cursor, page-size, string-length, and source-scan limits; a schema-valid request larger than 64 KiB is deserialized and dispatched normally.

Only cancellation of the active request is propagated. An unrelated `OperationCanceledException` thrown by a mod is isolated like another field/database failure. Diagnostic formatting never reads arbitrary virtual exception `Message`/`ToString`, and it reads `Type.FullName`/`Name` only from the known runtime `Type` implementation. These bounds are Dev Gateway safety and patch-authoring payload policy, not limitations imposed by EmbedIO, Zlepper, Unity, RimWorld's Def loader, or the JSON format. Declaring package metadata is not patch authorship: RimWorld does not retain a complete history of which later PatchOperations touched each field.

The format is deliberately JSON and reports `IsCanonicalSourceXml: false`. RimWorld 1.6 has a public `DirectXmlSaver`, but it is not an inverse of Def loading: the unified patched XML document is discarded, while that saver reconstructs public and non-public runtime fields, omits source metadata marked `[Unsaved]`, can expand unsafe graphs, and has no cycle or response budget. An XML request therefore fails explicitly instead of presenting a lossy debug reconstruction as canonical patchable source.

## Startup-gated in-game integration tests

The installed Zlepper testing SDK remains a normal NUnit/VSTest compiler setup. It does not launch RimWorld, load active mods, apply XML patches, populate real Def databases, or run tests at a game lifecycle point. The Gateway supplies that missing integration tier without shipping NUnit into Unity.

Test assemblies reference the host-safe `RimWorldDevGateway.IntegrationTesting` contract. A test is a public static parameterless `void` method marked with one lifecycle point:

```csharp
using RimWorldDevGateway.IntegrationTesting;
using Verse;

public static class SteelPatchIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void SteelHasTheExpectedFinalizedValue()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        IntegrationAssert.NotNull(steel, "Core Steel must exist in the real post-load database.");
    }
}
```

Opt-in projects set `RimWorldInGameIntegrationTest=true`, declare `RimWorldIntegrationTestOwnerPackageId`, and include a sibling `<assembly-base>.integrationtests.json` manifest naming the built `*.IntegrationTests.dll`. `-ActivePackageIds` is mandatory and is the complete nonempty ordered active package sequence, not just the test owner; omission is rejected instead of building every opted-in project. Reusable Gateway fixtures may use `requiredPackageIds`/`forbiddenPackageIds`. A product matrix can instead set `activePackageSetMode` to the literal `exact` and provide `activePackageIds`; it cannot also contain nonempty constraints. Exact mode compares count and position case-insensitively, so missing, extra, and reordered mods all exclude the project. Manifest top-level properties are a closed set and duplicates are invalid. The builder reads and validates each source manifest before building, selects against the complete sequence, and rejects source/output manifest or exact-sequence drift after the build. A standalone host build defaults to the repository's marker-owned `artifacts\InGameIntegrationTests\StagingMods` tree. Gateway smoke first validates and registers the complete dry-run plan, then asks the same builder to publish only those candidates beneath each owning active mod's versioned `DevIntegrationTests` folder for the exact process. Publication prepares a complete same-volume sibling stage before renaming it into place, preserves the prior complete owned stage on failure, and removes all exact marker-owned live candidates afterward even when later result or evidence validation fails. Every staged assembly must contribute at least one discovered descriptor with the same owner and exact full assembly identity. Test DLLs never go under `Assemblies`, because RimWorld eagerly loads every DLL there before a startup flag can gate discovery, and ordinary product builds do not stage them.

For each `-AdditionalModProjectPaths` product, smoke evidence inventories every deployed ordinary-package file by ordinal relative path with byte length and SHA-256. It reads ECMA-335 metadata from every managed package DLL without loading or executing product code, retaining the DLL's full identity and every ordinal reference row, including duplicates. A metadata-bearing DLL without an assembly definition (for example, a renamed netmodule) fails closed because it has no ordinary assembly identity to admit. The preflight rejects a case-insensitive `RimWorldDevGateway*.dll` filename anywhere in the package and also rejects a managed reference whose simple name begins `RimWorldDevGateway`, so renaming or omitting the Gateway DLL cannot hide a product dependency.

```powershell
.\scripts\Build-InGameIntegrationTests.ps1 `
  -ActivePackageIds 'ludeon.rimworld','fumblesneeze.rimworlddevgateway' `
  -RimWorldPath 'F:\Steam\steamapps\common\RimWorld' `
  -SteamModContentFolder 'F:\Steam\steamapps\workshop\content\294100' `
  -Configuration Release

.\scripts\Invoke-GatewaySmoke.ps1 `
  -RunIntegrationTests `
  -TimeoutSeconds 180

.\scripts\Invoke-GatewaySmoke.ps1 `
  -RunIntegrationTests `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|MainMenuLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ImmersiveChefsXmlProbeContainsItsFinalPatch' `
  -TimeoutSeconds 180
```

Use `-Quicktest -RunIntegrationTests` to exercise `PlayableMapLoaded`. Add the intentionally narrow `-IntegrationFailureProbe` switch only for the Gateway's own fixture: it passes `-devGatewayForceIntegrationTestFailure`, expects the first map test to fail, requires the following map test to pass, polls again to prove the stage does not rerun, and then continues the normal authenticated smoke to prove server responsiveness.

Reflection is adequate for the deliberately small static-method contract, so version one does not add a source generator. Build-time project opt-in and manifest validation provide deterministic discovery without making the game compile source or relying on generated registration code.

The runner is completely disabled without `-devGatewayRunIntegrationTests`: it does not enumerate manifests or load test assemblies. When enabled, it resolves manifests only through active mods' version/load folders, preserves their actual loaded order, validates owner and package-matrix constraints, and rechecks an exact manifest sequence before exposing its named sibling DLL for byte loading. Dependency resolution requires an exact normalized assembly name, version, culture, and public-key token. The update loop admits one source at a time and polls one background discovery task; DLL loading plus `GetTypes`, `GetMethods`, metadata ordering, and `CustomAttributeData` inspection never run on Unity's thread. Failed loads still consume the 64-source attempt limit. A worker blocked inside mod reflection leaves the game responsive but cannot be safely cancelled; restart the isolated process to reset it. `MainMenuLoaded` waits for loaded play data, no pending long event, an entry root, UI root, and window stack, then one additional stable frame. `PlayableMapLoaded` requires a playing game and current map. Valid tests run on the Unity main thread, once per process and lifecycle point, in deterministic order; async-void and Task-returning methods are rejected, and each synchronous failure is isolated so later tests and the server continue.

One background persistence lane serializes and atomically writes the token-free `DevGateway\Sessions\<run-id>\integration-tests.json`. Attachment becomes visible only after its initial snapshot is durable. Before then `GET /integration-tests` returns retryable `503 integration_test_status_pending`; afterward it returns the exact last committed immutable snapshot under the same 4 MiB bound as the artifact. Gateway smoke retains the latest initial-pending response as `integration-tests-pending.json` and continues its bounded poll only for exact HTTP `503`, exact error code `integration_test_status_pending`, and case-sensitive JSON boolean `retryable: true`; missing, false, string, or differently cased retryability is fatal, as is pending after any ready response. The runner persists the exact running candidate before invocation and the exact terminal candidate before advancing. A transient start/write failure retains and retries that same object, invokes or reruns nothing, and leaves the endpoint on the prior durable state. This preserves the current test name if synchronous code freezes the main thread without putting JSON serialization or disk I/O on Unity's thread.

Exception output is intentionally conservative. The runner never calls arbitrary `ToString`, `Message`, or `StackTrace`; only exact trusted assertion and standard wrapper types can contribute separately bounded message/stack components, and untrusted exceptions receive a fixed suppression marker. Components are truncated before credential redaction and diagnostic concatenation. A fresh isolated RimWorld PID is the only supported reset because test assemblies, Def databases, Harmony owners, a stuck discovery worker, and engine statics cannot be reliably unloaded, cancelled, or restored.

These tests are appropriate for final Def/PatchOperation assertions and `Harmony.GetPatchInfo` checks under selected mod combinations. They are diagnostic integration evidence. Passing them does not accept a player-visible feature: the separate native player action and personally observed before/action/after workflow in `AGENTS.md` still applies.

## Automations and quickstart

Use discovery rather than assuming an automation is available:

```powershell
& $gatewayClient automations --manifest $manifest --pid $pidExpected -o json
& $gatewayClient run quickstart.spawn --arguments '{"version":1}' --idempotency-key empty-scene --manifest $manifest --pid $pidExpected -o json
```

The convenience form reads the arguments object from a file and invokes the same automation:

```powershell
& $gatewayClient quickstart .\scenario.json --idempotency-key chefs-smoke-v1 --manifest $manifest --pid $pidExpected -o json
```

The version-1 descriptor rejects unknown fields. A complete example is:

```json
{
  "version": 1,
  "center": { "x": 100, "z": 100 },
  "clearRadius": 4,
  "buildings": [
    {
      "defName": "TableMachining",
      "stuff": "Steel",
      "count": 1,
      "offset": { "x": 2, "z": 0 },
      "powerOn": true
    }
  ],
  "items": [
    { "defName": "ComponentIndustrial", "count": 20, "offset": { "x": -2, "z": 0 } }
  ],
  "pawns": [
    { "kindDefName": "Colonist", "count": 1, "offset": { "x": 0, "z": 2 } }
  ],
  "research": ["Electricity"],
  "gameConditions": [
    { "defName": "Eclipse", "durationTicks": 1200 }
  ]
}
```

`center` defaults to the map center. Building and item entries accept optional `stuff`, `quality`, and `offset`; building entries additionally accept `powerOn`. Supported qualities are `Awful`, `Poor`, `Normal`, `Good`, `Excellent`, `Masterwork`, and `Legendary`. Research entries may also use `{ "defName": "..." }`, and a game condition defaults to 60,000 ticks.

`quickstart.spawn` requires `ProgramState.Playing`, a current map, and no active long event. It parses and resolves all feasible Defs and cells before mutation, clears the bounded area, spawns/configures the scene, completes research, starts conditions, and tries to center the camera. A camera failure is a non-fatal `camera_focus_failed` warning. The result reports `Center`, every `ResolvedDefinition`, each spawned object's handle/category/Def/stuff/count/quality/position, warnings, and an ordered mutation ledger.

Inspect `GET /automations` for the live schema and availability. Version-one invocation is synchronous: `POST /automations/{name}/runs` waits for the main-thread handler and returns a terminal `succeeded`, `failed`, `cancelled`, or `timed-out` run with completed bounded progress/artifacts, result or error, and mutation ledger. An idempotency key replays that same terminal run ID and result and rejects reuse with different arguments.

Version one has no HTTP route for listing runs, looking up a run, polling or streaming incremental progress, or remotely cancelling a run. The registry has internal lifecycle and cooperative-cancellation mechanics, but those are not a remote contract. Add any such route through a future OpenSpec/API change.

Use the raw C# endpoint for one-off verification logic. Use a host-compiled uploaded assembly only when the older REPL language surface or reference model is insufficient. Add a named built-in automation when the flow is reusable and has a tested, discoverable contract. Product mods may own scenario JSON, but must not reference the gateway assembly.

## Current bounds

- Request headers: 16 KiB total; request body: 32 MiB.
- Network admission: 8 concurrent requests; excess requests receive retryable `503 gateway_busy`.
- Main-thread dispatcher: 128 pending items; at most one operation starts in each update or end-of-frame phase per frame.
- API response deadline: 15 seconds, except native debug-action discovery, which uses 60 seconds for its first bounded tree materialization; invocation uses the normal deadline. A disconnected HTTP client cancels queued work before it starts. A post-start timeout immediately logs that the operation may still be running and observes any later completion or fault, but already-started unrestricted code cannot be forcibly preempted.
- Direct C#: 65,536 encoded request bytes and 65,536 decoded source characters, 16,384 diagnostic characters, 65,536 result characters, and 1,024 runtime-type characters per submission; declarations and emitted assemblies persist for the session.
- Uploaded assembly: 16 MiB each. There is currently no per-run byte/rate quota, and loaded assemblies remain until RimWorld exits; restart disposable sessions during long campaigns.
- Default JSON serialization: depth 16, 100,000 nodes, and 4 MiB of UTF-8 output. Endpoint-specific serializers can impose a documented override. The transport response bound (including PNGs) is 64 MiB; the companion client rejects responses above 32 MiB.
- Finalized Def export policy: request JSON uses the 32 MiB transport body ceiling with no lower endpoint aggregate cap; schema-level cardinality and string limits still apply. Responses allow 32 requested Defs per page; 64 exact top-level field selectors; 512 projectable fields; 4,096 reflected members counted before exclusions; projection depth 8; 1,024 entries per recognized materialized collection; 65,536 characters per runtime string; 4,096 projected nodes; 1 MiB per complete field entry; 8 MiB per complete Def item; 500,000 serialized JSON nodes and 32 MiB for the complete success envelope; 1,024 database-type candidates; and 100,000 Def candidates per request. Paging stops before the next item would cross either envelope cap and returns a continuation cursor. Nested Defs are references and XML is unsupported in version one. These are Gateway policy values, not framework limits.
- In-game integration tests: 64 loaded assemblies, 128 valid tests/results, 64 discovery/infrastructure failures, 256 types per assembly, 128 methods per type, 8,192 aggregate discovery steps, and a 4 MiB immutable snapshot; synchronous test code runs on the main thread and cannot be preempted.
- Nested automation request parsing: 32 MiB of characters, depth 32, and 100,000 values, within the transport's 32 MiB request-byte bound.
- Log ring: 2,000 entries, with 8 KiB messages and 32 KiB stacks; reads clamp to 500 entries and a 3 MiB serialized `Entries` page even though the client accepts `--limit` through 1,000. `PageTruncated` tells the caller to continue after the final returned sequence.
- Screenshot: one capture at a time, 64 MiB server-side PNG bound; the companion client's response bound is 32 MiB.
- Drag: at most 10 seconds and 256 interpolation steps (the client additionally accepts at most 1,000, so the server's 256-step limit is authoritative).
- Text input: 4,096 characters; key chords: at most four unique modifiers.
- Game-state selection snapshot: 256 current-map things. Atomic selection requests: 200 handles, matching RimWorld 1.6's native simultaneous-selection capacity.
- Thing queries: 64 values per list filter, 256-character label filter, 1,000 results per page; inspection text 8,192 characters per field, 64 component types, 64 skills, 128 health conditions, and 64 warnings.
- Native debug actions: 256-character search, 64 category/game-state filter values, 1,000 results per page, and 10,000 bounded already-materialized source candidates per discovery pass. Lazy generated submenus are surfaced as unsupported and are never expanded by discovery.
- Gizmos: 256 explicit or selected owners, 64 architect categories, 1,000 returned descriptors, bounded retained handle registrations, one active semantic interaction, and 4,096 unique cells per apply.
- Internal automation history: 128 runs, with 256 progress records and 64 artifacts retained per run; version one does not expose that history through list/get APIs.
- Quickstart: at most 64 entries per section, clear radius 20, offsets ±64 cells, Def names 128 characters, item count 5,000 per entry, pawn count 32 per entry, and 256 combined building/pawn instances. Game conditions are limited to 3,600,000 ticks.

### Where the limits come from

We chose every numeric limit in this section as application policy; none is imposed by EmbedIO, Unity, RimWorld, Zlepper ModSdk, HTTP, or JSON. EmbedIO owns the HTTP protocol handling, while the Gateway separately rejects work before it can allocate or monopolize Unity's process. The selected 32 MiB request and 64 MiB response ceilings are tunable outer safety boundaries, not discovered framework maxima. The ordinary 4 MiB JSON ceiling is only a default for small control/status DTOs, and an endpoint with a documented bounded serializer may override it.

The Def-export values deliberately favor patch authoring over tiny payloads. A reflection inventory of the installed RimWorld 1.6 `Verse.ThingDef` inheritance chain found 276 declared instance/static fields and 253 projectable instance fields after static and `[Unsaved]` exclusions (`ThingDef` 198, `BuildableDef` 48, `Def` 7). The 512-field limit therefore provides roughly two times the observed Core headroom, while the separate 4,096-member work limit protects against hostile modded hierarchies before exclusions. The 1 MiB field, 8 MiB Def, 500,000-node envelope, and 32 MiB envelope values are intentionally generous, tunable payload policy: the page has room for several very large Def items while remaining at half the transport ceiling, dense dictionary/list DTO expansion stays bounded, and byte/node-aware continuation avoids rejecting an otherwise valid multi-page export. They are not claims about the largest legal RimWorld Def.

The integration-test snapshot remains at 4 MiB because its shape is fixed and much smaller: at most 64 assembly records, 128 descriptors/results, 64 failures, two lifecycle records, and bounded 128/256/1,024-character identity, message, and stack fields. Its ceiling covers the worst bounded record set including JSON escaping; it is unrelated to Def export and does not constrain general endpoint requests. Any future increase should change the owning OpenSpec contract and boundary tests together rather than being presented as a framework requirement.

## Isolated smoke evidence and cleanup

`scripts\Invoke-GatewaySmoke.ps1` defaults to only Core + Dev Gateway in an isolated run. `-AdditionalModIds` inserts validated, de-duplicated package IDs between Core and the gateway, which lets the same harness load Harmony and a product mod without touching the normal mod list. For product evidence, `-AdditionalModProjectPaths` builds and deploys the exact repository product project before launch and records its assembly/About/XML-patch hashes; the ordinary deployed package is rejected if a test assembly or Gateway integration contract leaked into it. `-ExpectedIntegrationTests` names the exact owner, lifecycle, and fully qualified product test that must be discovered and pass. A non-Gateway expected owner is rejected unless the corresponding repository product project is supplied, so a stale preinstalled package cannot satisfy provenance. The script waits for the exact PID's manifest, reads it through a 64 KiB non-reparse bound, requires its exact active schema and JSON types, safe run ID, exact `http://127.0.0.1:<port>/api/v1` base URL, and matching PID plus process-start instant before using its token or paths. Every mode runs a harmless raw-C# health probe proving Mono.CSharp evaluation, live Unity/Gateway/EmbedIO access, and RimWorld's actual ordered loaded-mod list; only the explicit Gateway regression mutates and restores developer mode. It proves unauthenticated `401` and authenticated traffic through the real Unity Mono EmbedIO listener, checks all packaged DLLs including `Mono.CSharp.dll`, captures non-mutating state/log/Def/screenshot evidence, checks the Player log before setup, requests controlled shutdown, stops the exact owned PID, rescans the flushed log, and verifies the normal mod-list hash is unchanged.

The explicit `gateway-regression` scenario owns the desktop and raw-input probes. Its click probe retries foreground activation. In a non-interactive desktop session Windows may still deny foreground ownership; by default the regression records the gateway's explicit `focus_lost` safeguard as `ClickOutcome: focus-guard-rejected` rather than injecting into the wrong window. Pass `-RequireRawClick` together with that scenario for a dedicated interactive input run that must inject successfully.

Evidence lives under `artifacts\GatewaySmoke\<UTC run id>` and includes the immutable aggregate `evidence-summary.json`, `SavedData\Config\ModsConfig.xml`, `Player.log`, build/status/UI/log/action/automation artifacts, the mandatory exact-process `gateway-screenshot.png`, shutdown/process/credential/integration-stage cleanup records, and aggregate `cleanup-status.json`. A named product scenario adds `scenario.json`, one result per C# step, and its declared screenshots. The explicit Gateway regression adds `execution.json`, `execution-state.json`, `execution-restore.json`, `flaui-evidence.json`, `click.json`, quickstart/semantic artifacts, before/move/zoom/restore camera JSON, settled game-state snapshots, and four camera PNGs. `main-menu.png` exists only when that regression's independent FlaUI desktop capture completed; an unavailable FlaUI capture remains explicit and is never replaced with a copy of the Gateway image. The evidence summary records configured and live RimWorld versions separately, the Gateway mod version, and each required deployed DLL's filename, complete managed assembly identity, byte length, and SHA-256. A product run additionally records the exact repository project and package root; the complete ordinary-package relative-path/length/SHA-256 inventory; every managed DLL's full identity and metadata-only reference inventory; the primary product assembly identity/length/SHA-256; About metadata hash; and every source/deployed XML patch relative path/length/hash. The isolated `ModsConfig.xml` and command result record the ordered Core/additional/gateway list. Each regression FlaUI child is time-bounded and terminated only through its retained process handle. Setup records service/connect attempts before invocation; cleanup reconciles observed disconnected/stopped state after uncertain responses and preserves a service that was already running and disconnected. Each cleanup component writes an explicit completed, failed, or not-required/not-requested status, and the aggregate retains primary verification plus process, credential, and stage-cleanup failures together rather than masking later cleanup failures. After the exact launched PID is confirmed dead, including after the force fallback, the host removes a matching `SavedData\DevGateway\current.json`, atomically removes credentials from a matching active `session.json`, and scans every retained run artifact for every observed bearer token. A normal controlled shutdown still leaves the runtime's `stopped` token-free tombstone unchanged.

Every admitted request also appends token-free started/terminal entries to `requests.jsonl` and atomically replaces `last-request.json` inside that session directory. While requests are active, the atomic record identifies the newest active admission in `started` state—even if a newer quick request completes while an older request remains hung; with no active request it contains the latest recorded terminal event. The host independently writes `last-host-request.json` before transmission. On failure it writes `failure-diagnostics.json` with the last request, process liveness/exit code, and classification; cleanup writes `process-cleanup.json`. Cleanup requests a normal close and waits 15 seconds. Only if the exact owned PID remains alive does it attempt `RimWorldWin64-hang.dmp` through the Windows `comsvcs` dumper (recording an explicit unavailable/failed/timed-out result) before exact-PID force fallback. Because a full-memory dump can itself contain the unrestricted bearer token, credential cleanup deletes that transient dump after the PID dies instead of retaining it.

Add `-Quicktest` to wait for a playable map without running setup, moving the camera, selecting objects, toggling developer controls, connecting FlaUI, or injecting input. Select the comprehensive Gateway surface regression explicitly when those mutations are the subject under test:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -Scenario gateway-regression -TimeoutSeconds 300
```

The `gateway-regression` scenario runs the checked-in `scripts\Fixtures\GatewayQuickstartSmoke.json` descriptor twice with one idempotency key, then gets/sets/restores developer mode, god mode, pause/speed, and camera; queries, inspects, and atomically selects spawned things; directly spawns and cleans up a disposable item and pawn; invokes a safe native debug action; and exercises a reversible native gizmo plus disposable architect interaction. It retains `quickstart.json`, `quickstart-replay.json`, `post-quickstart-status.json`, `post-quickstart-ui-state.json`, `post-quickstart-logs.json`, correlated semantic artifacts, and API/FlaUI screenshots.

Named product scenarios are explicit descriptors in `scripts\Scenarios`. Version one validates required package IDs against the configured set before dry-run/launch and against RimWorld's actual live loaded set before setup, then runs ordered descriptor-local raw-C# and screenshot steps. Screenshot names are case-insensitively unique and must begin with `scenario-`, so a descriptor cannot overwrite standard evidence. For example:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-caravan-dining `
  -InteractiveHoldSeconds 900 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -TimeoutSeconds 300
```

The Immersive Chefs scenario clears the generated pawn's randomized inventory, then creates and selects a caravan containing a human pawn, a Simple meal with its plate embedded, and silverware; opens the native Items tab; captures the ready scene where the plate is not yet a separate inventory row; then makes the pawn hungry and pauses. After native ingestion, the plate must become a visible inventory row beside the returned silverware. It does not run the Gateway regression battery. Product-mod runs should pass `-ExpectedLogMarkers` for known initializer success messages; the smoke also rejects common structured mod/gateway exception signatures. If a regression post-log page reports `PageTruncated`, continue manually after its final returned sequence when the full range matters.

`immersive-chefs-animal-caravan-dining` provides the matching animal-exclusion workflow. It selects a caravan with a fully fed, Plants-0 human escort and a starving Labrador retriever, plus an intentionally cold, poor, contaminated Simple meal whose clean plate is embedded and clean silverware is loose. The escort keeps RimWorld's caravan simulation active but cannot be the eater or generate competing forage during the bounded observation window. Native caravan ingestion must remove the meal, return the plate unused, and leave the silverware untouched. The loaded integration assertion separately proves exact Thing identity, unchanged safe sanitation, and absence of the mod's otherwise-guaranteed custom food poisoning; animals have no mood-memory workflow to inspect.

The accepted animal-exclusion behavior proof is `artifacts\GatewaySmoke\20260802T232842610Z`. Its ready screenshot visibly contains only the Simple meal and steel silverware; after native unpause and caravan simulation, the retained FlaUI screenshot shows “Out of food,” the returned steel plate, and the untouched steel silverware. Forage per day is visibly zero. The harness then passed its final flushed-log scan, stopped PID 18844, sanitized the session credential, and restored the normal mod-list hash.

The accepted main-menu integration proof is `artifacts\GatewaySmoke\20260802T074054731Z`. In a fresh Core-plus-Gateway process, RimWorld reached its settled interactive main menu, the finalized Steel Def export contained the real Gateway XML-patch marker with no projection warning/truncation, and both exact `MainMenuLoaded` fixtures passed. Unauthenticated access returned `401`, raw unrestricted execution and raw click injection succeeded, the endpoint and persisted integration snapshots matched, shutdown left a token-free stopped session, the exact PID died, the owned test stage was removed, and `evidence-summary.json` records identical before/after normal mod-list SHA-256 values of `7B516628E3EFB1813848D0CD100C2DD3A7853EC14EE6ADBA52EFC2163B980957`. The retained PNGs show RimWorld's normal interactive main menu with no error dialog.

The accepted Immersive Chefs product integration proof is `artifacts\GatewaySmoke\20260802T111514801Z`. The harness built and deployed the repository's exact Immersive Chefs project, then RimWorld reported the exact ordered active and loaded set Core, Harmony, Immersive Chefs, and Gateway. Discovery attributed `ImmersiveChefs.InGame.IntegrationTests` to the active `fumblesneeze.immersivechefs` content pack; its constructor/active-set assertion and finalized-Steel XML assertion both passed. The same process's bounded Def export contains both `patched-by-immersive-chefs-xml` and `patched-by-gateway-xml`, and the personally inspected exact-process Gateway PNG shows a normal main menu without an error dialog. `evidence-summary.json` retains the complete ordinary product-package inventory and managed references, the exact expected product test, equal normal configuration hashes, a successfully injected raw player click, and completed process/credential/stage/FlaUI-service cleanup. The inverse proof is `artifacts\GatewaySmoke\20260802T111650331Z`: with only Core and Gateway active, the bundle and runtime snapshot contain only the Gateway-owned test assembly, no result is attributed to Immersive Chefs, and finalized Steel contains no Immersive Chefs marker.

The accepted playable-map semantic and failure-isolation proof is `artifacts\GatewaySmoke\20260802T073643956Z`. RimWorld loaded exactly Core plus Gateway, reached a playable Quicktest map, retained the unrestricted-execution warning and integration status overlay, and remained responsive after the exact deliberate map fixture failed while the later map fixture passed. Raw C# proved main-thread state/restoration; direct spawn replay reused one run and cleanup removed only returned handles; thing query/inspection/selection, a real native debug action, pawn drafting `false→true→false`, and two Plan-designator drags completed with correlated request evidence and cleanup. The camera visibly panned from the quickstart colony at `(125,125)` to a distant queried object at `(195,40)`, zoomed `24→19`, and restored `(125,125)@24`; the four retained rendered frames visibly agree with the settled game-state captures. The mutation responses now carry the same exact center, root size, and view rectangle as those post-frame snapshots rather than RimWorld's stale once-per-frame `CurrentViewRect` cache. Controlled shutdown sanitized credentials, removed the exact integration-test stage, left no owned RimWorld PID, and preserved the same normal mod-list hash. `evidence-summary.json` durably records both identical point-in-time hashes and all final cleanup paths without the bearer token. The harness issued no pawn movement order: changing pawn positions between frames are normal unpaused pawn activity, while the separate disposable spawn probe creates and deletes its own pawn.

### Freeze retrospective

The apparent repeated startup freezes were not a broad UI test suite. Earlier debug-action discovery recursively evaluated RimWorld's lazy generated submenus on Unity's main thread: retained runs measured 18.693 s, 37.811 s, and 39.961 s for that one request. Breadth-first discovery now refuses to invoke lazy child getters and the four subsequent clean processes measured 252–278 ms. The runtime also now starts at most one queued operation per Unity phase/frame and observes any operation that completes or faults after its HTTP timeout. The pawn that briefly appeared at another cell was a second disposable pawn spawned six cells left and six cells up from the fixture center, then deleted after its handle was verified. Separately, the fixture pawn's draft gizmo was toggled `false→true→false`. Neither step issued a movement order or pawn job.

For a manual session, stop the listener cleanly before closing the game:

```powershell
& $gatewayClient shutdown --manifest $manifest --pid $pidExpected -o json
```

If the game crashes, treat any old manifest as sensitive until the next startup scrubs it. Confirm its PID/start identity is stale; do not blindly reuse its token.

The smoke retains its evidence directory and the explicitly deployed `RimWorld\Mods\fumblesneeze.rimworlddevgateway` package; it does not enable that package in the user's normal mod list. Remove that exact local package folder separately if the development installation itself is no longer wanted.

## Extending the gateway

1. Update the `add-rimworld-dev-gateway` OpenSpec capability and tasks for the new observable contract.
2. Add host-safe request/manifest DTOs under `shared\RimWorldDevGateway.Contracts` only when they are shared with the client.
3. Capture a focused red test under `tests\RimWorldDevGateway.Tests`, then implement the narrow service. Keep all Verse/Unity access behind `GatewayDispatcher`; use `DispatchPhase.EndOfFrame` for rendering.
4. Expose the service through `GatewayApiServices` and `GatewayApiRouter`, then wire the production adapter in `GatewayRuntimeBootstrap`. For semantic actions extend the registry/Verse operations; for a reusable flow register a versioned automation and publish its schema and availability.
5. Extend `GatewayCliApp` only when a typed host command is more useful than direct HTTP. Preserve manifest/PID validation, bearer redaction, bounded I/O, exit codes, and temporary cleanup.
6. Run focused tests, `Invoke-Tests.ps1`, a zero-warning Release build, package inspection, strict OpenSpec validation, and the isolated Unity Mono smoke. Any added runtime DLL must be pinned, packaged, and proven by a real authenticated in-game request.
7. Keep the gateway absent from Immersive Chefs metadata, references, and release artifacts.
