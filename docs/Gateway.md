# RimWorld Dev Gateway

RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) is an intentionally dangerous, development-only RimWorld 1.6 mod. Whenever it is loaded, unrestricted in-process C# and uploaded-assembly execution are enabled. There is no safety switch, allowlist, confirmation dialog, or sandbox.

Use it only in a disposable isolated `-savedatafolder`. Uploaded code runs with the RimWorld process's full access, can corrupt game state or files, and cannot be unloaded from the AppDomain. Code that blocks the main thread can freeze the game even after the HTTP caller times out.

## Transport and threat model

The in-process server is EmbedIO 3.5.2 in managed-listener mode, bound only to a dynamically selected IPv4 `127.0.0.1` port. EmbedIO owns HTTP parsing and connection handling; the mod does not implement a raw HTTP parser. Kestrel is not used because current Kestrel expects the ASP.NET Core shared framework and a modern CoreCLR host, while RimWorld 1.6 loads `net48`-compatible mods in Unity Mono. EmbedIO supports the target runtime with a much smaller dependency graph.

The transport graph is pinned to EmbedIO 3.5.2, `Unosquare.Swan.Lite` 3.1.0, and `System.ValueTuple` 4.5.0. Direct in-process C# uses the separately pinned `Mono.CSharp` 4.0.0.143 package. The packaged runtime assemblies are exactly:

- `RimWorldDevGateway.dll`
- `RimWorldDevGateway.Contracts.dll`
- `RimWorldDevGateway.EndToEndTesting.dll`
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
| `GET /ui-state` | `ui-state` | Pause/speed, rendered-client bounds, and bounded window/selection snapshot |
| `GET /game-state` | direct HTTP | Developer/god mode, time, nullable map/camera, and bounded selection snapshot |
| `POST /game-state` | direct HTTP | Atomically set developer/god mode, pause, and native speed |
| `POST /camera` | direct HTTP | Set an absolute current-map center and/or native root size |
| `POST /things/query` | direct HTTP | Bounded current-map or current-view thing query |
| `GET /things/{handle}` | direct HTTP | Bounded thing/building/item/pawn inspection |
| `POST /selection` | direct HTTP | Atomic replace/add/remove/toggle/clear selection |
| `GET /logs?after=N&limit=N` | `logs --after N --limit N` | Cursor-based structured Unity/RimWorld logs |
| `POST /screenshots` | `screenshot --file PATH [--things H1,H2] [--padding N]` | End-of-frame full PNG or object-bounded crop |
| `POST /input/click` | `click --x N --y N [--button left|right|middle]` | Process-scoped screen-local client-coordinate click |
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
| `GET /end-to-end-tests` | direct HTTP | Startup-gated multi-frame E2E discovery, current step, durable terminal results, and screenshot artifacts |
| `POST /dev-tools/actions/query` | direct HTTP | Discover bounded native debug-action leaves |
| `POST /dev-tools/actions/{handle}/invoke` | direct HTTP | Invoke an immediate action or activate its native pointer tool |
| `POST /dev-tools/spawn` | direct HTTP | Idempotent direct alias for the preflighted `quickstart.spawn` engine |
| `POST /gizmos/query` | direct HTTP | Discover selected/owned gizmos and architect designators |
| `POST /gizmos/{handle}/invoke` | direct HTTP | Invoke immediate/toggle or begin one typed interaction |
| `GET /interactions/current` | direct HTTP | Return the active typed interaction or `null` |
| `POST /interactions/{handle}/apply` | direct HTTP | Apply a thing/cell/cells/line/rectangle input |
| `POST /interactions/{handle}/cancel` | direct HTTP | Cancel only the matching active interaction |
| `POST /server/shutdown` | `shutdown` | Flush the accepted response, then perform controlled listener and credential cleanup |

Routed JSON API envelopes use version 1 and report the request ID, `ok`, duration, and either `result` or a stable error. Every response also carries `X-Request-Id`, including authentication and transport-limit errors. A caller-provided ID must be 1–64 letters, digits, `-`, `_`, `.`, or `:`; otherwise the server generates one.

The version-one status result remains intentionally narrow: `developerOnly`, nullable `map`, `pendingDispatches`, `processId`, `programState`, `rootType`, nullable `tick`, `unrestrictedExecutionEnabled`, and `warning`. A non-null map has `Handle`, `Biome`, `Width`, and `Height`. The UI-state result contains `programState`, `rootType`, nullable effective `paused`, nullable native `speed`, nullable rendered `clientArea`, up to 256 selection entries (`Handle`, `Label`), and up to 128 window entries (`Handle`, `Type`, `Modal`). `clientArea` reports positive `Width`/`Height` and `CoordinateOrigin: TopLeft`; this is the exact client-pixel coordinate space accepted by `/input/click` and `/input/drag`, independent of desktop screenshot scaling. Richer developer controls live in the dedicated `/game-state`, `/camera`, `/things`, and `/selection` contracts. Versions come from the session manifest, and action schemas and availability come from their discovery routes.

Use the raw C# endpoint or a named automation when a verification needs state outside those stable DTOs. That escape hatch does not promote the returned data into the API contract; adding a new stable snapshot field requires an OpenSpec/API change.

`POST /screenshots` keeps `{}` as the full-frame request. An object-bounded request supplies `thingHandles` and optional `paddingPixels` (default 32), for example `{"thingHandles":["Pawn_42","Building_9"],"paddingPixels":24}`. At one end-of-frame main-thread operation, the Gateway enumerates the current map once, resolves every distinct exact handle, projects each complete occupied-cell footprint, unions the bounds, applies padding, clamps to the captured frame, and returns only that PNG crop. Resolution is atomic: malformed, stale, camera-unavailable, or wholly off-screen targets return a stable error without a partial image. The implementation captures only one full frame and destroys both full and partial textures on every success or failure path.

Log results contain `Entries`, `OldestCursor`, `NewestCursor`, `HistoryEvicted`, and `PageTruncated`. Entries contain `Sequence`, `TimestampUtc`, `Severity`, `Message`, optional `Stack`, `Thread`, and optional `RequestId`. `HistoryEvicted` means the requested cursor is older than retained history. `PageTruncated` means more matching retained entries exist than fit the 500-entry or 3 MiB serialized-entry page. Resume a truncated read with the final returned entry's `Sequence` as `--after`; do not use the ring-wide `NewestCursor`, which could skip entries.

Useful client examples:

```powershell
& $gatewayClient ui-state --manifest $manifest --pid $pidExpected -o json
& $gatewayClient logs --after 0 --limit 100 --manifest $manifest --pid $pidExpected -o json
& $gatewayClient screenshot --file '.\artifacts\manual-gateway.png' --manifest $manifest --pid $pidExpected
& $gatewayClient screenshot --file '.\artifacts\two-targets.png' --things 'Pawn_42,Building_9' --padding 24 --manifest $manifest --pid $pidExpected
& $gatewayClient action game.pause --arguments '{"paused":true}' --manifest $manifest --pid $pidExpected -o json
& $gatewayClient action game.speed --arguments '{"speed":"fast"}' --manifest $manifest --pid $pidExpected -o json
& $gatewayClient click --x 1000 --y 100 --manifest $manifest --pid $pidExpected -o json
& $gatewayClient keys --key Escape --manifest $manifest --pid $pidExpected -o json
```

Initial semantic actions are `game.pause` (optional boolean `paused`, omitted to toggle), `game.speed` (`paused`, `normal`, `fast`, `superfast`, or `ultrafast`), `window.accept`, `window.cancel`, and `debug.tool.cancel`. The last action is available only while a native `DebugTool` pointer action is active and clears that exact native tool instead of cancelling an unrelated window. Discovery is authoritative: an action may be present but unavailable at the current menu/window/game state. Prefer `GET/POST /game-state` for deterministic pause/speed control and use raw keyboard input when the native keybinding itself is the behavior under test. Raw input is Windows-only, revalidates the RimWorld PID/window/client bounds, and can optionally skip activation with `--no-activate`.

All raw click, drag, chord, and text injection runs on HTTP workers through one serialized input lane, not Unity's main thread. Windows activation temporarily joins the worker, prior-foreground, and exact RimWorld window input queues and retains that lease until the gesture finishes; cleanup always detaches them. A click may reacquire focus once if the first transition is lost before mouse-down, records `focus_reacquire` in its event ledger, and never retries after a button was pressed. A click cannot interleave with an active drag, and request/server cancellation interrupts timed drag waits while retaining the lane through best-effort mouse/key release. This lets RimWorld process injected events without a long gesture freezing the game; live Verse and Unity state operations still go through the bounded dispatcher.

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

Architect build descriptors also expose `BuildableDefName`, so automation can select a build command by the finalized Def identity instead of a localized label or category ordinal. Non-build gizmos return `null` for that field.

Use `ownerScope: "explicitOwners"` with `ownerHandles` for explicit owners. The limits are 256 owners, 64 exact `DesignationCategoryDef` names, and 1,000 returned descriptors. Descriptors contain revision, source, owners, runtime type, bounded label/description, disabled state/reason, hotkey, group key, observed toggle state, interaction kind, and accepted inputs. A handle fingerprints the current map, owners, source, ordered list, and native identity. Invocation re-enumerates the list, so a map/selection/owner/order change returns `stale_gizmo_handle` instead of pressing a neighboring command.

`Immediate` and `Toggle` invocations complete directly; toggles report observable before/after state. `Target`, `Placement`, and `Drag` create the one active semantic interaction and return its handle, source handle, accepted input shapes, map handle, owners, and revision. Inspect it with `GET /interactions/current`, then submit exactly one matching shape:

```json
{ "kind": "thing", "thingHandle": "Pawn_17" }
```

```json
{ "kind": "cell", "cell": { "x": 101, "z": 99 } }
```

For a native `Designator_Place`, the cell may include one exact cardinal orientation:

```json
{
  "kind": "cell",
  "cell": { "x": 101, "z": 99 },
  "rotation": "East"
}
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

Typed version-one placement covers designators whose complete meaning is a cell set plus, for `Designator_Place`, an optional `North`, `East`, `South`, or `West` orientation. The gateway initializes and configures the exact revalidated native place designator before its own preflight and designation; it does not rotate the resulting Thing afterward. Rotation on another input/designator and rotation of a non-rotatable placing Def fail closed. A build designator that still needs a Stuff choice is not applied through this typed route; use a named `quickstart.spawn` descriptor or raw C# for that development setup. World targeting and mod-defined multi-stage GUI interactions remain unsupported.

The safe native adapters cover ordinary `Command_Action`, `Command_Toggle`, local `Command_Target`, cell-based `Designator` operations, and exact cardinal rotation for native place designators. Group-only UI semantics, abilities/verbs/world targets, sliders, custom `GizmoOnGUI`, right-click menus, build material choice, and multi-stage targeting are intentionally `unsupported`; use raw C# or process-scoped input, then add a tested typed adapter when that workflow becomes recurring.

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

The named type must declare exactly one public static, non-generic method with the requested case-sensitive name. It returns `string` and may use the legacy `(string requestJson)` signature or the context-aware `(string requestJson, GatewayAssemblyExecutionContext context)` signature. The client defaults the method name to `Execute` and the request string to `{}`. Use the context-aware form when uploaded code needs request correlation, cooperative cancellation, or a session automation:

```csharp
using RimWorldDevGateway.Contracts;
using System.Threading;
using Verse;

namespace Inspect;

public static class Entry
{
    public static string Execute(
        string requestJson,
        GatewayAssemblyExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        context.RuntimeExtensions.RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "inspect.state",
                "1",
                "Reports the current game state.",
                mutating: false),
            (argumentsJson, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return "state=" + Current.ProgramState + ";arguments=" + argumentsJson;
            });
        return "request=" + context.RequestId +
            ";state=" + Current.ProgramState +
            ";thread=" + Thread.CurrentThread.ManagedThreadId;
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

The method runs through the Unity main-thread dispatcher. The API result contains `Value`, `Truncated`, and `OriginalUtf8Bytes`; `Value` is deterministically limited to 10 MiB UTF-8 including the truncation marker. This bound is chosen so worst-case sixfold JSON escaping still fits the transport's 64 MiB response ceiling. Context-aware code receives the dispatch cancellation token. A registered automation is process-session scoped, appears in `GET /automations`, receives at most 1 MiB of normalized argument JSON plus its automation cancellation token, and has the same 10 MiB result bound. Its metadata is limited to 64 schema entries, 64 prerequisites, 256 UTF-8 bytes per schema key, 4,096 UTF-8 bytes per schema value/prerequisite, and 512 KiB total. Empty/null or oversized metadata fails before registration. HTTP request/deadline cancellation is linked into the run token for cooperative started handlers; this is transport lifecycle handling, not a version-one remote-cancel API. The legacy one-string signature remains accepted but cannot observe cancellation after invocation. The server intentionally does not authorize, sandbox, or unload uploaded assemblies.

The checked-in `scripts\Fixtures\GatewayAssemblyExecutionSmoke.cs` is the canonical live probe for context-aware upload and no-restart session registration.

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

One background persistence lane serializes and atomically writes the token-free `DevGateway\Sessions\<run-id>\integration-tests.json`. Attachment becomes visible only after its initial snapshot is durable. Before then `GET /integration-tests` returns retryable `503 integration_test_status_pending`; afterward it returns the exact last committed immutable snapshot under the same 4 MiB bound as the artifact. Gateway smoke retains the latest initial-pending response as `integration-tests-pending.json` and continues its bounded poll only for exact HTTP `503`, exact error code `integration_test_status_pending`, and case-sensitive JSON boolean `retryable: true`; missing, false, string, or differently cased retryability is fatal, as is pending after any ready response. The runner persists the exact running candidate before invocation and the exact terminal candidate before advancing. Its background operation absorbs a brief Windows destination-reader lock with a short atomic-replace retry and never sleeps Unity's thread. A later transient start/write failure retains and retries that same object, invokes or reruns nothing, and leaves the endpoint on the prior durable state. This preserves the current test name if synchronous code freezes the main thread without putting JSON serialization or disk I/O on Unity's thread.

Exception output is intentionally conservative. The runner never calls arbitrary `ToString`, `Message`, or `StackTrace`; only exact trusted assertion and standard wrapper types can contribute separately bounded message/stack components, and untrusted exceptions receive a fixed suppression marker. Components are truncated before credential redaction and diagnostic concatenation. A fresh isolated RimWorld PID is the only supported reset because test assemblies, Def databases, Harmony owners, a stuck discovery worker, and engine statics cannot be reliably unloaded, cancelled, or restored.

These tests are appropriate for final Def/PatchOperation assertions and `Harmony.GetPatchInfo` checks under selected mod combinations. They are diagnostic integration evidence. Passing them does not accept a player-visible feature: the separate native player action and personally observed before/action/after workflow in `AGENTS.md` still applies.

## Startup-gated multi-frame E2E tests

The E2E tier codifies repeatable player workflows that cannot finish in one lifecycle method. Marked test projects remain outside product packages and ordinary NUnit/VSTest registration, reference `RimWorldDevGateway.EndToEndTesting`, and declare one owner package. Each `[RimWorldEndToEndTest]` type provides a stable ID, its complete ordered non-Gateway active package list, per-test frame/tick/wall-clock deadlines, `Arrange`, and an iterator of typed action/wait/observation steps.

Use the grouped host runner:

```powershell
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TimeoutSeconds 300 -Output table
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId ludeon.rimworld -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TestId immersive-chefs.localization-rendering -Language German -Output json
```

The runner discovers installed package IDs from Core/DLC/local/Workshop metadata, passes large inventories and each exact child mod order through UTF-8 package-ID files, builds and deploys every repo-owned test owner before staging, and atomically publishes marker-owned bundles under each owner's `1.6\DevEndToEndTests`. It appends Gateway last, starts one fresh minimized isolated process per exact group, and runs same-group tests sequentially. `-Language` accepts one bounded RimWorld language-folder name and writes it only to that child's disposable `Prefs.xml`; dry-run, child, and aggregate evidence record the requested value, while omission keeps the English default. If Core lacks that locale, the launcher hash-leases the Gateway's metadata-only provider into Core language discovery without overwriting an installed provider, then removes only its verified file after the owned process stops. `language-provider.json` and `language-provider-cleanup.json` record that lifecycle. The provider enables exact product-catalog rendering but intentionally supplies no Gateway or Core translation. The child's save-data/artifact root uses a short `smoke-NNN` branch so long descriptive group IDs do not exceed Unity Mono's classic Windows path boundary. XML-invalid console controls are replaced only in JUnit projection; retained stderr remains byte-for-byte diagnostic evidence. The game never builds tests. `-devGatewayRunEndToEndTests` is the only discovery gate; without it the Gateway does not enumerate or load E2E bundles.

At runtime, discovery verifies the owner, complete real active order, assembly identity/hash, attribute metadata, and host manifest before byte-loading the test assembly. The execution state machine persists each admitted transition before invoking the next action. Native steps reuse Gateway time, selection, camera, gizmo, float-menu, screen-local input, and end-of-frame screenshot services. `GET /end-to-end-tests` returns retryable pending until the initial token-free snapshot is durable, then exposes discovery, the current test/step, and terminal results. Screenshots are persisted in the same session directory and referenced by the corresponding observation step.

`IncidentActionStep` is the host-safe semantic path for a real native incident. It resolves one exact loaded `IncidentDef`, optionally one exact loaded faction by its unmodified load ID, builds forced current-map storyteller parameters, and invokes that Def's real `IncidentWorker.TryExecute`. It does not enqueue a guessed incident, expose a generic test callback, or use desktop input; missing, ambiguous, rejected, or throwing state fails closed with bounded diagnostics.

`ArchitectCategoryActionStep` is the host-safe semantic path for retained build-menu evidence. It
accepts one exact category Def name and open/close intent, validates exactly one cached native tab,
then uses RimWorld's Architect main-button, inspected category-click, and current-tab escape paths.
It requires player control, exact category cardinality, the requested category selection, and the
post-action open/closed state. Missing or drifted shapes fail closed. The action never restores,
focuses, resizes, maximizes, or sends desktop input, so a minimized E2E run can capture the real
Architect menu without taking over the workstation.

The E2E state machine waits for `Game.PlayerHasControl` before capturing the destructive isolation baseline because RimWorld's native pause command rejects during the brief quickstart handoff. The wait uses the test deadline and fails as `player_control_not_ready` without cleaning an uncaptured baseline. The shared contract deliberately omits a generic synchronous callback action because Unity cannot preempt a blocking main-thread delegate; fixed-cost iterator-side fixture maintenance belongs after a persisted step boundary and is never player-action evidence.

Dynamically loaded tests can request `IEndToEndFloatMenuCatalog` from `IEndToEndContext`. `Query(actorRuntimeId, targetRuntimeId)` returns the current native options as shared-contract metadata containing the visible label, disabled state, and a callback-sensitive stable identity. Select one unambiguous enabled option from that metadata, then pass its stable identity to `FloatMenuActionStep`; do not reproduce Gateway hashes, retain RimWorld callbacks, or assign the resulting job directly.

Before and after every test, the process pauses and removes every constructed or natural roof—including overhead mountain—before destroying any map content, then removes all destroyable spawned Things/Pawns, zones, designations, selection, active interactions, and test-opened windows. After Thing removal it clears and verifies live messages, visible and delayed letters, and the active alert-readout cache so alert-sensitive scenarios start from a clean notification slate. This order prevents roof collapse or object removal from leaking notifications into later scenarios. Permanent non-destroyable map features such as quicktest steam geysers remain environment. Cleanup restores control/camera state and verifies that roofs, disposable content, and notification surfaces are empty; an unverifiable reset taints the process and skips later tests. The host continues with later independent mod groups, writes aggregate JSON/JUnit, shuts down the exact PID, sanitizes credentials, and removes only the exact leased test stage in `finally`.

The focused notification-isolation acceptance run is retained locally at `artifacts/EndToEndRuns/Grouped/20260806T204114758Z`. `gateway.notification-cleanup.seed` visibly displayed a native message, letter, and alert and also seeded a delayed letter; the following `gateway.notification-cleanup.verify` screenshot showed a clean notification surface while exact state inspection confirmed the delayed queue was empty. Both tests and final cleanup passed in the same minimized process.

E2E state and assertions remain supporting automation. Acceptance still requires the acting agent to inspect the exact run's native screenshots and confirm that the recorded player action caused the visible outcome; logs or a green endpoint alone are insufficient.

Each E2E screenshot is captured at end-of-frame and persisted before its step passes. A transient Unity capture failure, invalid encoded frame, capture timeout, or concurrent-capture lease race receives one fresh end-of-frame retry; deterministic target/request failures and artifact-persistence failures remain terminal so missing evidence is never reported as success.

E2E assemblies can query `IEndToEndGizmoCatalog` for stable gizmo identities, runtime types, native interaction kinds, and structured buildable Def names. A `GizmoActionStep` may set `expectRejected: true`; it passes only when native preflight rejects every supplied target, applies nothing, and the Gateway cancels the still-open semantic interaction. This makes invalid placement rules observable without turning an expected player-facing refusal into an infrastructure failure.

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

`scripts\Invoke-GatewaySmoke.ps1` defaults to only Core + Dev Gateway in an isolated run. It writes `SavedData\Config\Prefs.xml` with `runInBackground=True`, `volumeMusic=0`, fullscreen disabled, and a deterministic 1600×900 render size, then passes matching `-screen-*` arguments to Unity before requesting a minimized owned RimWorld window. Windows can expose Unity's new HWND in its normal state for a few milliseconds before applying the minimized request; the launcher does not maximize it or enforce its later state. Pass `-VisibleWindow` for hands-on or computer-use interaction. The desktop-owning `gateway-regression` scenario requests a normal visible window automatically. Dry-run and completed results identify `Prefs`, `RunInBackground`, `MusicVolume`, `RenderWidth`, `RenderHeight`, `Fullscreen`, `UnityWindowArguments`, the requested `LaunchWindowStyle`, effective visibility mode, and whether visibility was explicitly requested. Once the window has been created, the launcher does not query, classify, warn on, or enforce native window state, so a user restore or maximize cannot fail the run. The user's normal `Prefs.xml` is hashed before and after alongside `ModsConfig.xml`; either mutation fails the run. `-AdditionalModIds` validates and de-duplicates package IDs. If Harmony is present, the launcher moves only `brrainz.harmony` ahead of Core to honor Harmony's installed `loadBefore` contract, preserves every other caller package's relative order after Core, and keeps Gateway last. This lets the same harness load Harmony and a product mod without touching the normal mod list. For product evidence, `-AdditionalModProjectPaths` builds and deploys the exact repository product project before launch and records its assembly/About/XML-patch hashes; the ordinary deployed package is rejected if a test assembly or Gateway integration contract leaked into it. `-ExpectedIntegrationTests` names the exact owner, lifecycle, and fully qualified product test that must be discovered and pass. A non-Gateway expected owner is rejected unless the corresponding repository product project is supplied, so a stale preinstalled package cannot satisfy provenance. The script waits for the exact PID's manifest, reads it through a 64 KiB non-reparse bound, requires its exact active schema and JSON types, safe run ID, exact `http://127.0.0.1:<port>/api/v1` base URL, and matching PID plus process-start instant before using its token or paths. Scenario step request IDs are derived deterministically from the descriptive scenario and step names; valid short IDs remain readable, while longer compositions retain a readable prefix plus a SHA-256 suffix within the API's 64-character correlation bound. Every mode runs a harmless raw-C# health probe proving Mono.CSharp evaluation, live Unity/Gateway/EmbedIO access, and RimWorld's actual ordered loaded-mod list; only the explicit Gateway regression mutates and restores developer mode. It proves unauthenticated `401` and authenticated traffic through the real Unity Mono EmbedIO listener, checks all packaged DLLs including `Mono.CSharp.dll`, captures non-mutating state/log/Def/screenshot evidence, checks the Player log before setup, requests controlled shutdown, stops the exact owned PID, rescans the flushed log, and verifies the normal mod-list and preference hashes are unchanged.

The explicit `gateway-regression` scenario owns the desktop and raw-input probes. The click endpoint itself performs one bounded pre-button foreground reacquisition, and the launcher may submit a fresh request after a fail-closed response. In a non-interactive desktop session Windows may still deny foreground ownership; by default the regression records the gateway's explicit `focus_lost` safeguard as `ClickOutcome: focus-guard-rejected` rather than injecting into the wrong window. Pass `-RequireRawClick` together with that scenario for a dedicated interactive input run that must inject successfully.

Evidence lives under `artifacts\GatewaySmoke\<UTC run id>` and includes the immutable aggregate `evidence-summary.json`, `SavedData\Config\ModsConfig.xml`, `Player.log`, build/status/UI/log/action/automation artifacts, the mandatory exact-process `gateway-screenshot.png`, shutdown/process/credential/integration-stage cleanup records, and aggregate `cleanup-status.json`. A named product scenario adds `scenario.json`, one result per C# step, and its declared screenshots. The explicit Gateway regression adds `execution.json`, `execution-state.json`, `execution-restore.json`, `flaui-evidence.json`, `click.json`, quickstart/semantic artifacts, before/move/zoom/restore camera JSON, settled game-state snapshots, and four camera PNGs. `main-menu.png` exists only when that regression's independent FlaUI desktop capture completed; an unavailable FlaUI capture remains explicit and is never replaced with a copy of the Gateway image. The evidence summary records configured and live RimWorld versions separately, the Gateway mod version, and each required deployed DLL's filename, complete managed assembly identity, byte length, and SHA-256. A product run additionally records the exact repository project and package root; the complete ordinary-package relative-path/length/SHA-256 inventory; every managed DLL's full identity and metadata-only reference inventory; the primary product assembly identity/length/SHA-256; About metadata hash; and every source/deployed XML patch relative path/length/hash. The isolated `ModsConfig.xml` and command result record the ordered Core/additional/gateway list. Each regression FlaUI child is time-bounded and terminated only through its retained process handle. Setup records service/connect attempts before invocation; cleanup reconciles observed disconnected/stopped state after uncertain responses and preserves a service that was already running and disconnected. Each cleanup component writes an explicit completed, failed, or not-required/not-requested status, and the aggregate retains primary verification plus process, credential, and stage-cleanup failures together rather than masking later cleanup failures. After the exact launched PID is confirmed dead, including after the force fallback, the host removes a matching `SavedData\DevGateway\current.json`, atomically removes credentials from a matching active `session.json`, and scans every retained run artifact for every observed bearer token. A normal controlled shutdown still leaves the runtime's `stopped` token-free tombstone unchanged.

Every admitted request also appends token-free started/terminal entries to `requests.jsonl` and atomically replaces `last-request.json` inside that session directory. While requests are active, the atomic record identifies the newest active admission in `started` state—even if a newer quick request completes while an older request remains hung; with no active request it contains the latest recorded terminal event. The host independently writes `last-host-request.json` before transmission. On failure it writes `failure-diagnostics.json` with the last request, process liveness/exit code, and classification; cleanup writes `process-cleanup.json`. Cleanup requests a normal close and waits 15 seconds. Only if the exact owned process handle remains alive does a bounded hidden PowerShell helper revalidate its process-start identity and call Windows `MiniDumpWriteDump` for native thread, handle, module, and process/thread data before retained-handle force fallback. The helper is terminated after 20 seconds, and every failed or partial capture is removed. The dump excludes full heap memory; after the process dies, credential cleanup discovers every recoverable token and scans its ASCII, UTF-16LE, and UTF-16BE byte forms. It retains the analyzable dump only after discovery and every scan succeeds with all tokens absent. Missing or malformed credential state, a failed scan, an unrecoverable token, or a found token deletes the dump fail-closed.

Add `-Quicktest` to wait for a playable map without running setup, moving the camera, selecting objects, toggling developer controls, connecting FlaUI, or injecting input. Select the comprehensive Gateway surface regression explicitly when those mutations are the subject under test:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -Scenario gateway-regression -TimeoutSeconds 300
```

Use the exclusive hang diagnostic only when validating the dump/force-cleanup path itself:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -HangDumpProbe -TimeoutSeconds 300 -Output json
```

That explicit probe starts minimized, waits for an ordinary playable quicktest, then submits one raw main-thread sleep and requires the real endpoint to return `response_timeout_after_start`. That expected transition enters ordinary exact-handle cleanup; the probe passes only when the live stall is classified, the owned process is terminated through the force fallback, a valid native dump is captured and retained after multi-encoding credential scans, every cleanup lane succeeds, and normal configuration hashes remain unchanged. It cannot be combined with a scenario, integration/E2E runner, interactive hold, Gateway regression, visible-window override, or skipped deployment, and never runs during an ordinary quicktest.

The `gateway-regression` scenario runs the checked-in `scripts\Fixtures\GatewayQuickstartSmoke.json` descriptor twice with one idempotency key, then gets/sets/restores developer mode, god mode, pause/speed, and camera; queries, inspects, and atomically selects spawned things; directly spawns and cleans up a disposable item and pawn; invokes a safe native debug action; and exercises a reversible native gizmo plus disposable architect interaction. It also opens the real `LudeonTK.EditWindow_Log`, retains `developer-log-before.png`, sends 24 correlated status requests whose diagnostics must appear in the Gateway ring buffer from non-Unity transport threads, proves a later main-thread game-state request completes while the window remains observable, retains `developer-log-after.png`, and closes the window. That developer-log probe uses only in-process Gateway observation and screenshots and never requires maximizing the RimWorld window. The scenario retains `quickstart.json`, `quickstart-replay.json`, `post-quickstart-status.json`, `post-quickstart-ui-state.json`, `post-quickstart-logs.json`, `developer-log-traffic.json`, correlated semantic artifacts, and API/FlaUI screenshots.

Named product scenarios are explicit descriptors in `scripts\Scenarios`. Version one validates required package IDs against the configured set before dry-run/launch and against RimWorld's actual live loaded set before setup, then runs ordered descriptor-local raw-C# and screenshot steps. Screenshot names are case-insensitively unique and must begin with `scenario-`, so a descriptor cannot overwrite standard evidence. Their migration status, exact non-Gateway groups, and required observable workflows live in [`ScenarioMigrationInventory.md`](ScenarioMigrationInventory.md). For example:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-caravan-dining `
  -InteractiveHoldSeconds 900 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -TimeoutSeconds 300
```

The Immersive Chefs scenario clears the generated pawn's randomized inventory, then creates and selects a caravan containing a human pawn, a Simple meal with its plate embedded, and cutlery; opens the native Items tab; captures the ready scene where the plate is not yet a separate inventory row; then makes the pawn hungry and pauses. After native ingestion, the plate must become a visible inventory row beside the returned cutlery. It does not run the Gateway regression battery. Product-mod runs should pass `-ExpectedLogMarkers` for known initializer success messages; the smoke also rejects common structured mod/gateway exception signatures. If a regression post-log page reports `PageTruncated`, continue manually after its final returned sequence when the full range matters.

`immersive-chefs-kitchenware-fabrication` creates an ordinary crafting spot plus a machining table on a real charged battery power net, exact granite/steel/wood ingredient stacks, two capable workers, and two pawn-restricted one-shot production bills. It asks vanilla's finalized Crafting and Smithing `WorkGiver_DoBill` instances for the jobs and pauses after native job acceptance; it never calls recipe completion or creates the products itself. This distinction caught the inherited `requiredGiverWorkType=Crafting` defect that made every smithy and machining kitchenware recipe unstartable, and the recipe family now inherits the correct work-giver type.

`immersive-chefs-kitchenware-route-matrix` requires the actual Argonic Core, Vanilla Expanded Framework, and Expanded Materials - Masonry package chain. It creates three crafting spots and three fueled smithies, six restricted workers, and only the exact wood, adobe-brick, silver, and handle stacks required by the finalized recipes (four material for four plates, two for four cutlery settings, and six metal plus one wood for a cookware set). The arm step asks vanilla `WorkGiver_DoBill` for all six jobs and pauses without running recipe completion or synthesizing an output. This exercises fixed non-Stuff adobe alongside Stuff-aware soft and intermediate-metal paths.

`immersive-chefs-cooking-ware-selection` builds two sealed, mutually unreachable fueled-stove kitchens and a separate sealed starving requester. One real Simple-meal bill can reach Awful clean steel cookware and a clean steel plate plus Legendary dirty golden controls; the other can reach only dirty steel ware. The request step invokes vanilla's patched `JobGiver_GetFood.TryGiveJob` for the starving pawn and requires it to return no reachable food, rather than calling Immersive Chefs' internal urgency registry. The arm step then asks finalized `WorkGiver_DoBill` for both real jobs and pauses. It never creates a meal or invokes recipe completion directly.

`immersive-chefs-recipe-complexity` builds three sealed, mutually unreachable fueled-stove kitchens with matched healthy Cooking-20 pawns, identical clean normal-quality steel cookware and plates, and no linked assistants. The fixtures use the exact finalized `CookMealSimple`, `CookMealFine`, and `CookMealLavish` Defs; the arm step asks finalized `WorkGiver_DoBill` for each native one-shot bill and pauses after all three jobs are accepted. It neither creates products nor invokes recipe completion. Resume at one native speed and pause/select each spawned meal to compare the real player-visible completion order.

`immersive-chefs-cooperative-cooking` builds matched assisted and control kitchens, three Cooking-only pawns, exact one-shot Simple-meal bills, clean matched ware, and a powered sauce station linked only to the assisted stove. Its final setup step merely enables the configured feature and undrafts the pawns; it does not choose, start, end, or tick a pawn job and never manufactures a meal. Native scheduling must send the specialist to the linked station while the assisted lead collects ingredients, overlap the two jobs only while the lead cooks, leave the control kitchen unassisted, and release the specialist when the lead's exact job ends or a higher-priority player action intervenes.

`immersive-chefs-handheld-food-exclusions` builds two sealed concrete rooms with one healthy hungry colonist apiece, an exact Pemmican stack or packaged survival meal, and separate clean steel plate/cutlery controls. Its arm step asks vanilla `JobGiver_GetFood.TryGiveJob` for both jobs and requires each job to target the exact nearby fixture object before starting the native `Ingest` driver and pausing. It never calls `Thing.Ingested`, embeds a plate, or manufactures a post-ingestion state. Resume at a native speed and inspect the ordinary map and controls after both jobs finish.

`immersive-chefs-nutrient-paste-dining` builds a sealed concrete room containing a real powered vanilla nutrient-paste dispenser, its adjacent hopper, forbidden rice feedstock, one healthy hungry colonist, and exact clean Normal steel plate/cutlery controls. Its arm step asks vanilla `JobGiver_GetFood.TryGiveJob` for a native `Ingest` job and requires the target to be that exact operational dispenser before starting the job and pausing. It never calls `TryDispenseFood`, `Thing.Ingested`, or Immersive Chefs' ware-binding helpers. Resume at a native speed to observe the meal being dispensed, carried, consumed, and replaced by the used ware.

`immersive-chefs-imported-meal-plating` is passive after setup. It creates one unplated Simple-meal stack with three preserved Rice/quality/temperature/rot records, three clean Excellent steel plates, a fueled stove, and one Cooking-only pawn, selects the meal, and pauses. It never names or invokes the plating workgiver/job, calls `TryEmbedPlate`, or starts a pawn job. Resume through a native key or the Gateway game-speed control and observe the ordinary scheduler move the meal to the stove; the selected inspector must change from `Service ware: unplated` to `Bound plates: 3` while its existing ingredient and culinary fields remain visible.

`immersive-chefs.countertop-microwave-native-reheat` supersedes the removed legacy `immersive-chefs-microwave-reheating` descriptor. The product-owned E2E fixture creates a hungry drafted colonist, an initially frozen plated Simple meal, exact clean steel cutlery, and a powered countertop microwave. Typed native Undraft and time controls cause ordinary food seeking, carrying, reheating, and ingestion; retained screenshots and terminal assertions cover the active appliance toil, five-point quality loss, 60 °C output, and exact dirty ware return.

The accepted self-feeding microwave proof is `artifacts\GatewaySmoke\20260804T122413200Z`. The exact Core/Harmony/Immersive/Gateway process opened on the selected meal whose ordinary inspector visibly read `Culinary quality: Masterwork (80)` and `Meal temperature: Frozen (-5 °C)`. A semantic selection followed by the pawn's exact native Undraft gizmo and Normal speed caused the ordinary `Ingest` job; `microwave-final-visible-heating.png` visibly shows the pawn at the powered appliance while the pawn inspector reads `Carrying: Simple meal` and `Consuming simple meal`. After the job finished, the native Needs panel visibly showed `Excellent cooking +4`, `Steaming hot meal +2`, and `Proper place setting +1`, making the configured five-point loss observable as a Masterwork-to-Excellent band change and proving the hot output without reading component state. The exact returned plate and cutlery screenshots each visibly read `Cleanliness: dirty`. The harness then delivered the shutdown response, closed only PID 31404, sanitized credentials, cleaned its stage, found no final Player-log error, and preserved both normal configuration hashes.

The accepted Thermodynamics ownership proof is `artifacts\GatewaySmoke\20260805T095422163Z`, paired with the same reviewed assembly's minimized exact-loader run `artifacts\GatewaySmoke\20260805T094945767Z`. The exact Core/Harmony/Thermodynamics/Immersive/Gateway process passed the product-owned finalized-Def tests with Thermodynamics `DMicrowave` and `HeatMeal` present and `ImmersiveChefs_Microwave` absent. Through native screen-local clicks, RimWorld's own Options → Mod options → Immersive Chefs surface visibly replaced every temperature control with `Meal temperature and microwave: provided by Thermodynamics - Hot Meals`; the adjacent Thermodynamics settings page visibly retained heating speed, temperature diffusion, and temperature moodlet controls. Both runs completed final log scanning, exact-process shutdown, credential/test-stage cleanup, and unchanged normal mod-list and preference hashes. The inverse Core/Harmony/Immersive/Gateway run `artifacts\GatewaySmoke\20260805T094212355Z` passed the finalized fallback-microwave matrix when Thermodynamics was absent.

The accepted fallback-countertop proof is `artifacts\EndToEndRuns\Grouped\20260805T105514165Z`. In the exact Core/Harmony/Immersive/Gateway group, the dynamically loaded product E2E test used the finalized microwave build designator to place one appliance on a steel dining table and one on a machining table, while native preflight rejected a storage shelf and bare floor without creating anything. The retained screenshots visibly show both supports and selected appliances with the reviewed custom microwave sprite. The test then invoked the selected dining table's own native Deconstruct command; an ordinary construction pawn removed that exact underlying support, the original microwave became one nearby minified appliance, and the second supported microwave remained spawned. The agent inspected all three screenshots, the group passed all four admitted Immersive Chefs E2E tests, cleanup was untainted, and the marker-owned stage was removed. Blueprint/bed rejection and active-reheat interruption remain part of the broader gameplay acceptance task rather than this completed construction slice.

The current accepted orbital-meal purchase and unobtrusive-window proof is `artifacts\EndToEndRuns\WindowLaunchFinal2\20260805T161635116Z`. The exact Core/Harmony/Immersive/Gateway process generated a one-serving Fine meal through `ThingSetMaker_TraderStock`, placed that exact meal and its Poor steel plate in an orbital trader's native stock container, and opened RimWorld's trade dialog. A typed native trade action resolved that exact meal's current `Tradeable`, applied its native count setter plus dialog refresh from `0` to `1`, then invoked the exact `Dialog_Trade` Accept callback. That semantic path contains no Win32 mouse, keyboard, focus, restore, or maximize operation. The personally inspected 1600×900 frames visibly show the Fine-meal row change from `0` to `1`, the 20-silver charge, and the delivered selected meal's ordinary inspector reading `Bound plates: 1`. The test checkpoint confirms the original physical plate remained embedded exactly once.

During the earlier orbital-trade E2E run, RimWorld automatically restored and then maximized; the user confirmed this was not their window change. The external 20 ms exact-PID timeline at `artifacts\EndToEndRuns\WindowLaunchFinal\20260805T161025756Z` records the transition during that verification, but the retained evidence does not establish whether Windows, Unity, the fixture, or another game path triggered it. Neither the launcher nor the semantic trade path explicitly requests focus, restore, resize, or maximize, and a maximized window is not required for that in-process action. The corrected countertop fixture searches a fixed candidate grid and reaches its first player-action step in 1.45 seconds. The reviewed rerun's credential-free `window-timeline.json` contains exactly one native state for PID 19740 from first observation through shutdown: `ShowCommand=2`, `Iconic=true`, with the outer normal window rectangle `-7,0–1609,939` corresponding to the configured 1600×900 client and no restore or maximize transition. All five exact-group tests passed, the stage and credentials cleaned, and both normal configuration hashes remained unchanged. This timeline is external read-only acceptance evidence, not a launcher monitor; the launcher still never inspects or overrides a later user restore/maximize.

The accepted settlement-meal purchase proof is `artifacts\EndToEndRuns\Grouped\20260805T183131142Z`. In the exact Core/Harmony/Immersive/Gateway group, the fixture temporarily narrowed an existing neutral faction trader to native Fine-meal and Steel stock generators, created a real same-tile player caravan and settlement, and restored both finalized trader lists during cleanup. The new typed action resolved their exact live world-object IDs, verified RimWorld's own visit lookup targeted that settlement, and invoked the enabled `CaravanVisitUtility.TradeCommand`; it contains no translated selector, Win32 input, focus, restore, or maximize operation. Personally inspected frames visibly show the native settlement dialog with 500 caravan silver, Fine meal and Steel stock, then the Fine-meal transfer changing `0→1` and caravan food increasing `0→0.6` days before native Accept closes the dialog. The terminal checkpoint proves that the settlement released the meal, the caravan acquired that same Thing, and its original physical plate remains embedded exactly once. A separate isolated test created two settlements on the same tile, targeted the one RimWorld did not report as visited, observed the exact `settlement_trade_target_mismatch`, and retained a frame with zero trade dialogs. All seven same-group tests passed, the stage and credentials cleaned, final log scanning passed, and both normal configuration hashes remained unchanged. Its external read-only 20 ms timeline observed the exact PID's normal HWND creation state for 48 ms, then `ShowCommand=2`/`Iconic=true` without any later restore or maximize transition; both settlement actions occurred about 47 seconds after minimization.

The accepted Adaptive Meal Bill/Overcooked Meals proof pairs the exact finalized-loader run `artifacts\GatewaySmoke\20260805T205254586Z` with the native cooking E2E run `artifacts\EndToEndRuns\Grouped\20260805T205100848Z`. The loader run confirms the inspected assembly identities, exact upstream Harmony owners, concrete Advanced Fine classification, and one ware/culinary component on `OvercookedMeals_MealOvercooked`. In the E2E run, a real pawn accepted the adaptive Fine bill, hauled the eligible steel plate and cookware, and visibly worked at the stove before Overcooked Meals replaced the poisoned output on the second native attempt. Personally inspected frames show the selected survivor with Rice, `Bound plates: 1`, `Culinary quality: Awful (0)`, and `Meal temperature: Steaming Hot`; the returned selected steel cookware reads `Cleanliness: dirty`, and the rejected wood plate remains spawned. The process passed and cleaned after the E2E state machine waited for native player control instead of racing quickstart pause.

`immersive-chefs-meal-cooling-holders` is a passive three-way thermal comparison. Four small setup submissions create three identical plated 70 °C Simple meals, then enclose them in real roofed Rooms served by one charged power net: a 21 °C heater for the ambient control, a 5 °C cooler for the refrigerated control, and a -10 °C cooler for the frozen control. Setup never advances a tick, writes a Room temperature, calls the culinary cooling implementation, or changes game speed. Resume only through an ordinary native speed action, pause after the desired interval, and select the returned exact meal handles to compare their ordinary inspectors.

The accepted cooling proof is `artifacts\GatewaySmoke\20260804T131851410Z`. In the exact Core/Harmony/Immersive/Gateway run, the initial native inspector visibly showed the selected ambient meal at `SteamingHot (70 °C)`. One explicit Superfast action advanced from tick 35 to tick 5104 before an explicit pause. The three exact-meal screenshots then visibly reported `Warm (44.3 °C)`, `RoomTemperature (21 °C)`, and `Frozen (-2.5 °C)` for the ambient, refrigerator, and freezer controls, respectively. This observes both exponential progression and the faster refrigerator/freezer paths through player time control and native inspection rather than a synthetic cooling call.

`immersive-chefs-meal-serving-stack` is a passive per-serving split fixture. It creates one selected three-meal stack with ordered culinary records 20, 50, and 80, three exact embedded plates, three clean cutlery units, and one starving drafted diner inside a sealed enclosure. Setup never calls `SplitOff`, either culinary consumption hook, or a pawn job API. Use semantic selection and the pawn's native Undraft gizmo, resume normal time, and pause after one ordinary ingestion. The original meal handle remains the two-serving map stack, allowing its ordinary inspector to expose whether RimWorld's native split transferred only the consumed serving and plate.

The accepted serving-stack proof is `artifacts\GatewaySmoke\20260804T133540194Z`. Its ready screenshot visibly showed `Simple meal x3`, `Bound plates: 3`, and `Masterwork (80)`. The exact native Undraft toggle changed from true to false, then ordinary game time produced one ingestion. `meal-serving-stack-final-remainder.png` visibly shows the same selected meal handle as `Simple meal x2`, `Bound plates: 2`, and `Good (50)`, proving the Masterwork-80 serving and one exact plate split away while the next per-serving record remained intact. The exact four-mod launcher then passed final log scanning, controlled process shutdown, credential/stage cleanup, and both normal configuration hash checks.

`immersive-chefs-prepared-food-workflow` builds two sealed matched kitchens, one Cooking-20 prep chef, two drafted Cooking-10 comparison cooks, one real two-cycle preparation bill, and one pawn-restricted Simple-meal bill per cook. The prepared-input bill accepts only the output of the prep station; the control accepts only raw rice, and both receive matched clean Normal steel ware. The scenario undrafts only the prep chef and does not choose, start, end, or tick a job, construct recipe products, or modify rot progress. After the native prep bills visibly produce the prepared stack, use both cooks' native Undraft toggles while paused and resume one native speed to compare the ordinary cooking jobs.

`immersive-chefs-hidden-provenance` builds two paused sealed rooms with closer-left hidden-human and farther-right hidden-rice controls. One Cooking-only pawn has a native Simple-meal bill whose ordinary ingredient filter permits prepared food and rice but rejects human meat; one hungry diner has a native food policy with the corresponding restriction. The scenario does not choose or start either job. Resume through an ordinary game speed: the cook and diner must each skip the closer forbidden stack, consume the farther allowed stack, and leave exact source names absent from ordinary inspection.

The accepted hidden-provenance proof is `artifacts\GatewaySmoke\20260804T100456433Z`. RimWorld loaded exactly Core, Harmony, Immersive Chefs, and Gateway, and all 25 `PlayableMapLoaded` tests passed, including the real patched bill, food-policy, and vanilla ingredient-thought boundaries. After one explicit `Fast` speed action, the closer hidden-human prepared stack and meal remained while the farther hidden-rice controls were consumed; the cook produced a new Simple meal. `hidden-provenance-final-cooked-meal.png` visibly shows the selected product and its ordinary inspector reports `Ingredients: Nutrient paste meal` without exposing either exact hidden source. The launcher then closed only its exact PID, sanitized credentials, cleaned the staged test bundle, and preserved both normal configuration hashes.

`immersive-chefs-prepared-paste-dispensing` builds and visibly frames a sealed concrete room containing a real powered vanilla nutrient-paste dispenser, adjacent hopper/feedstock, and one selected capable operator, but no plate or cutlery. It has no arm step and never names the prepared-food Def or custom job: the operator must use RimWorld's native right-click float menu and choose `Dispense prepared cooking paste` during an explicit interactive hold. This preserves the player-order boundary while keeping no-hold invocations useful as fixture-only preflights.

`immersive-chefs-vnpe-prepared-paste` requires the exact installed Harmony, Vanilla Expanded Framework, Vanilla Nutrient Paste Expanded, and Immersive Chefs packages. It creates a real powered VNPE pipe network, tap, and vat, fills that native vat to 12 meals as an explicit scenario precondition, selects one capable operator, and pauses. It never invokes `TryDispenseFood`, constructs prepared output, or starts a job. Verification must right-click the real tap, choose the native Immersive Chefs float-menu option, resume through an ordinary game speed, inspect the resulting stack, and visibly compare the vat before and after.

`immersive-chefs-animal-caravan-dining` provides the matching animal-exclusion workflow. It selects a caravan with a fully fed, Plants-0 human escort and a starving Labrador retriever, plus an intentionally cold, poor, contaminated Simple meal whose clean plate is embedded and clean cutlery is loose. The escort keeps RimWorld's caravan simulation active but cannot be the eater or generate competing forage during the bounded observation window. Native caravan ingestion must remove the meal, return the plate unused, and leave the cutlery untouched. The loaded integration assertion separately proves exact Thing identity, unchanged safe sanitation, and absence of the mod's otherwise-guaranteed custom food poisoning; animals have no mood-memory workflow to inspect.

`immersive-chefs-animal-map-dining` exercises the same exclusion through an ordinary map ingest job. It places a named factionless wild raccoon, one plated Simple meal, and clean loose cutlery inside a small sealed fixture that prevents colonist hauling and competing animal food selection; captures the ready view while the plate remains embedded; then selects and arms the raccoon before pausing. Native wild-animal AI must choose and consume the meal without reserving or moving the cutlery; afterward the meal is gone and the exact still-clean plate is visible at the eating location. The supporting loaded integration assertion starts and ticks a real `JobDriver_Ingest` and checks its reservation boundary, exact Thing identity, unchanged sanitation and cutlery cell, and absence of custom food poisoning.

`immersive-chefs-storage-dishwashing` creates a sealed concrete fixture with mutually exclusive clean-only and dirty-only plate stockpiles, a dishwasher on a real charged battery power net, a hauling-only pawn, a drafted cleaning-only pawn, and clean, dirty, and player-forbidden dirty plates. It does not unpause or invoke any gameplay action automatically. Use native speed control to let the hauler route the two allowed plates, the selected plate's native Allow gizmo to lift its prohibition, and the cleaner's native Undraft gizmo to start `Doing dishes`; query exact Thing handles between those player actions when identity conservation matters.

`immersive-chefs-processor-dishwasher` creates a sealed powered domestic-dishwasher fixture for the real Processor Framework fill work giver. Three hauling-only pawns and separate dirty cookware, plate, and cutlery stacks begin inside; together they weigh 5.25 place settings. Setup starts no job and advances no cycle. Resume at Normal speed to let native hauling fill the save-persistent loading batch, then observe zero washing progress until capture and the same three Thing identities returning clean.

`immersive-chefs-processor-dishwasher-interruption` composes that passive fixture with one capable Basic-only switch worker. The interruption extension performs no flick, cycle tick, cancellation, or post-fixture ware mutation. Start the batch through ordinary hauling and native time, use `Designate toggle power` so the worker performs the switch, and compare the ordinary inspector before interruption, after advancing time without power, immediately after restoration, and after resumed progress. `Eject dishes` must return the same dirty Things with their exact Stuff, quality, and hit points.

`immersive-chefs-dubs-processor-dishwasher` composes that fixture with real hidden Dubs plumbing and a real connected small water tower initialized to exactly 10.0 L. It calls neither water-debit nor job APIs. During native hauling the full 5.25-setting loading batch must leave the tower unchanged; starting the captured cycle performs one 0.525 L debit, rendered as 9.5 L, and neither progress nor completion may debit it again.

`immersive-chefs-common-sense-cleanup` loads the exact Common Sense matrix and creates a selected self-diner, a drafted Doctor-capable nurse, a conscious hungry patient in a medical bed, two plated Simple meals with clean cutlery, and one powered dishwasher. It deliberately creates no ingest, patient-feeding, or cleanup job. Use the diner's native right-click meal action, then the nurse's native Undraft gizmo and patient context action. The scenario proves behavior only when the pawns visibly finish those ordinary jobs and the dishwasher's normal inspector contains both returned place settings.

`immersive-chefs-common-sense-no-route` is the inverse handoff workflow. It creates one selected Cleaning-capable diner with an exact plated Simple meal and cutlery inside a sealed concrete room, plus a charged power net whose dishwasher starts switched off. No reachable hand-washing source exists, and the scenario starts no ingest, flick, or cleaning job. Use the native `Consume simple meal` action and inspect the released dirty plate and cutlery before selecting the dishwasher and applying its native power-toggle designation. Once an ordinary pawn switches it on, normal Cleaning work must collect the same items without a Common Sense retry loop.

The pre-rename historical animal-exclusion behavior proof is `artifacts\GatewaySmoke\20260802T232842610Z`. Its ready screenshot visibly contains only the Simple meal and the then-current table-utensil item; after native unpause and caravan simulation, the retained FlaUI screenshot shows “Out of food,” the returned steel plate, and the untouched utensil item. Forage per day is visibly zero. The harness then passed its final flushed-log scan, stopped PID 18844, sanitized the session credential, and restored the normal mod-list hash. It proves the exclusion workflow that preceded the terminology/schema rename, not the current cutlery DefName.

The pre-rename historical regular-map animal proof is `artifacts\GatewaySmoke\20260803T010911816Z`. The arm step ends the factionless wild raccoon's setup-time job without choosing a replacement, starvation is then applied, and native input resumes play; the retained before/after views show the meal disappear while the returned white plate appears at the eating location and the adjacent yellow utensil item remains in its original cell. The harness completed its final log scan, stopped PID 34372 without force, sanitized the session credential, and restored the normal mod-list hash. The immediately preceding normal-UI inspection in `artifacts\GatewaySmoke\20260803T005506372Z` selected the returned item and visibly reported `Steel plate (normal)` and `Cleanliness: Clean`; the only subsequent scenario change prevented a pre-starvation replacement job. Complementary historical exact four-mod loaded-test proofs cover all 23 then-discovered tests: `artifacts\GatewaySmoke\20260803T010617429Z` passed all 12 `MainMenuLoaded` tests, including exact Harmony-owner installation, and `artifacts\GatewaySmoke\20260803T005219093Z` passed all 11 `PlayableMapLoaded` tests, including the animal chew-speed boundary with a distinguishable 1.25× plate and explicit clean flags for both exact wares. These artifacts do not prove the current cutlery DefName.

The accepted pre-rename cutlery-free colonist proof is `artifacts\GatewaySmoke\20260803T074425385Z`. RimWorld loaded exactly Core, Harmony, Immersive Chefs, and Gateway; all 12 `PlayableMapLoaded` tests passed, including the real `JobDriver_Ingest` assertion that restores the shared map's exact previous dirt thickness and forbiddance state. The explicit scenario then selected a colonist with a plated meal and no cutlery in a sealed concrete room. Native Space-key input resumed play; the retained normal game view shows that the meal disappeared and the returned plate plus vanilla dirt became visible at the eating location. The evidence-only replay `artifacts\GatewaySmoke\20260803T075129302Z` opened the selected pawn's native Needs panel after the same player action and visibly showed the old terminology. Both runs completed their final log scans, exact-process shutdown, credential and test-stage cleanup, and normal mod-list restoration; they remain historical evidence rather than proof of the renamed schema.

The accepted post-rename terminology and Gateway-input proof is split across two clean exact four-mod processes. `artifacts\GatewaySmoke\20260803T-cutlery-gateway-acceptance\20260803T082623203Z` passed all 12 `PlayableMapLoaded` tests and then visibly showed `Ate without cutlery -3` in the selected pawn's native Needs panel after ordinary ingestion. Its Gateway-only interaction ledger reports the 2560×1440 top-left client area, Space changing `paused/speed` from `true/Paused` to `false/Normal`, a screen-local native click, an explicit `Fast` speed transition, and restoration to `Paused`. `artifacts\GatewaySmoke\20260803T-cutlery-item-accepted-clean\20260803T092717053Z` rebuilt the reviewed final Gateway and product commits, spawned and selected the real `ImmersiveChefs_Cutlery` Def through stable semantic routes, and retained a normal rendered inspect view reading `Steel cutlery setting (normal)`, `Quality: Normal`, `Cleanliness: clean`, `Kitchen cleanliness: 50`, and `Dining comfort: 50 %`. In that same final-build process, Space again changed the UI snapshot from `true/Paused` to `false/Normal`, while a screen-local click at client `(365,1410)` changed the top native window to `MainTabWindow_Work`; the retained screenshot visibly shows the ordinary Work table. The harness then stopped PID 32144 normally, left a credential-free tombstone, sanitized retained credentials, reported no final Player.log error, and preserved the normal mod-list hash. This closes the cutlery terminology and screen-local input observations without relying on diagnostic REPL assertions.

The accepted patient-feeding proof is `artifacts\GatewaySmoke\20260803T-patient-feeding-ui-acceptance-02\20260803T115950850Z`. RimWorld loaded exactly Core, Harmony, Immersive Chefs, and Gateway. The explicit scenario created three real `FeedPatient` jobs: a cutlery-equipped nurse with a cold plated meal, a conscious no-cutlery patient, and an unconscious no-cutlery patient. `patient-microwave-heating.png` visibly shows the first nurse facing the powered microwave with the native progress effect while carrying the meal. `patient-feeding-completed.png` shows all three patients fed and the resulting returned ware beside the fixture. In `conscious-patient-needs-visible-03.png`, the selected conscious patient's native Needs panel visibly reports `Ate without cutlery -3`. `patient-feeding-dirt-visible.png` selects the native dirt at the two no-cutlery patient cells and the inspect panel reports `Dirt x2`. The clean run used the minimized, muted launcher default, briefly focused only the exact owned window for the player-visible Needs-tab click, minimized it again, completed final log scanning and exact-process shutdown, sanitized credentials, and preserved both normal configuration hashes.

The accepted storage and dishwasher proof uses `artifacts\GatewaySmoke\20260803T135842100Z` plus the exact-identity follow-up `artifacts\GatewaySmoke\20260803T140936243Z`. The first exact four-mod run visibly shows the clean and dirty plates in their mutually exclusive stockpiles while the forbidden dirty plate remains selected at the fixture, the undrafted cleaner carrying a plate with the native `Doing dishes` job, the powered dishwasher actively washing at 10%, and the cleaned plate merged into the clean-only stack. Its native Allow-gizmo response changes the selected target's forbidden state from true to false and the following normal rendered frame loses the red forbidden marker. In the follow-up, after that native Allow action and native Undraft action, `identity-03-cleaner-carrying.png` visibly shows the cleaner carrying `Steel plate (normal)` while performing `Doing dishes`; exact inspection still finds the other dirty plate spawned in the dirty-only stockpile and reports the formerly forbidden target as no longer spawned, establishing that the carried identity is the player-allowed plate rather than its already-allowed neighbor. Both runs completed final log scanning, exact-process shutdown, credential and integration-stage cleanup, and preserved the normal mod-list and preferences hashes.

The accepted Common Sense cleanup proof uses the final settled exact-matrix integration run `artifacts\GatewaySmoke\20260804T031205310Z` and the player-workflow run `artifacts\GatewaySmoke\20260804T024648209Z`. The former loaded exactly Core, Harmony, Common Sense, Immersive Chefs, and Gateway and passed the public `CommonSense.Settings.adv_cleaning_ingest` plus `CommonSense.Utility.IncapableOfCleaning(Pawn)` shape assertion at the main menu. In the latter, process-scoped clicks opened RimWorld's native `Consume simple meal` and `Prioritize feeding` menus; `common-sense-nurse-feeding-in-progress.png` visibly shows the selected nurse carrying the meal with the native `Feeding simple meal to Common Sense Patient` job. After both dining jobs, `common-sense-nurse-both-wares-washing.png` visibly shows the selected powered dishwasher washing with `Capacity: 2.5/16 place settings`, corresponding to two plates and two 0.25-equivalent cutlery settings. The observed and final package inventories record the same product SHA-256 `B7B52600F28C34AA8424534C9B62A492079E83E355122D8762C1EE425D4BB923`. The scenario never injected an ingest, feed, or dish-cleaning job, and both runs passed final log scanning, exact-process shutdown, credential cleanup, and normal configuration hash checks.

The accepted Common Sense no-route proof uses `artifacts\GatewaySmoke\20260804T041223064Z` for the unavailable-route half and the clean full-workflow rerun `artifacts\GatewaySmoke\20260804T042419660Z`. Both exact Core/Harmony/Common Sense/Immersive Chefs/Gateway processes used screen-local input to open and choose RimWorld's native `Consume simple meal` action. The first run's ordinary plate and cutlery inspectors visibly read `Cleanliness: dirty` while both items remained freely selectable on the sealed room floor. In the clean rerun, `common-sense-no-route-free-ware.png` again visibly shows the meal gone and both items released before any route was restored. After a native power-toggle designation, the pawn performed ordinary Basic and Cleaning work, both items disappeared into the dishwasher at 1.25 plate-equivalent capacity, and they reappeared beside it after the normal cycle. The scenario started none of those jobs; both runs passed their final log scans, exact-process shutdown, credential cleanup, integration-stage cleanup, and normal configuration hash checks. The first bounded hold expired only after the unavailable-route evidence, so it is not used for the restored-route claim. The post-review repository regression is `artifacts\TestResults\20260804T043805480Z-1916-cc4160e90538460b8c6c91da3c5bc405`: all 727 executable tests passed, with only the three documented Microsoft-net48 listener cases not executed.

The accepted restart-only dishwasher-capacity proof is `artifacts\GatewaySmoke\20260804T045018115Z`. Its exact Core/Harmony/Common Sense/Immersive Chefs/Gateway process passed the product-owned `DishwasherCapacitiesAreMaterializedAtDefFinalization` loaded test for both the 16-place domestic and 64-place industrial Defs. A screen-local player click then selected the ordinary dishwasher; `dishwasher-finalized-capacity-visible.png` visibly shows RimWorld's native inspector reporting `Capacity: 0/16 place settings` on that same reviewed build. The setting is now materialized once while finalized Defs initialize, and the optional Processor Framework adapter consumes that finalized value instead of applying the setting again. The run passed its final log scan, exact-process shutdown, credential and integration-stage cleanup, and normal configuration hash checks. The post-review repository regression is `artifacts\TestResults\20260804T045428330Z-31080-9777e0136208494caa235ac9d08f21aa`: all 728 executable tests passed, with only the three documented Microsoft-net48 listener cases not executed.

The accepted weighted Processor/Dubs batch proof is `artifacts\GatewaySmoke\pf-dubs-sealed-final-native-20260804T1646Z\20260804T164543474Z`. On the reviewed exact Core/Harmony/Processor Framework/Dubs/Immersive Chefs/Gateway build, ordinary Processor fill work by three hauling-only pawns visibly changed the selected appliance from idle to `Dishwasher: loading`, `Capacity: 5.25/16 place settings`, `3 stacks processing (3 slow)` while its real connected Dubs tower remained at 10.0 L. Advancing native time visibly changed the appliance to washing all three stacks and the tower once to 9.5 L. Completion returned the exact cookware, plate, and cutlery Thing handles with their distinct 219/220, 88/90, and 67/70 hit points; each normal inspector visibly read `Cleanliness: clean`, and the tower still read 9.5 L. The passive scenario injected no job, cycle progress, cleaning transition, or water debit. The launcher then completed its final log scan, exact-process shutdown, credential cleanup, and normal configuration hash checks.

The accepted Processor interruption proof is `artifacts\GatewaySmoke\pf-interruption-clean-accepted-20260804T1744Z\20260804T174532596Z`. The selected appliance visibly showed washing at 10% before a native power-toggle designation. An ordinary Basic worker switched it off; the inspector then showed `paused: no power`, the same 5.25/16 setting capacity, and the same three processing stacks after 1,093 additional game ticks. The worker restored power and the inspector immediately showed the unchanged 10%, then advanced to 20% under native time. Finally, the native `Eject dishes` gizmo returned only the original cookware, plate, and cutlery handles, visibly dirty at their original 219/220, 88/90, and 67/70 hit points. The interruption extension invoked no jobs, flicks, cycle ticks, ejection, or post-fixture ware mutations. The launcher completed its final log scan, controlled exact-process shutdown, credential and integration-stage cleanup, and both normal-configuration hash checks.

The accepted Hospitality guest proof is `artifacts\GatewaySmoke\20260803T165820108Z`. The explicit `immersive-chefs-hospitality-guest-dining` scenario loaded exactly Core, Harmony, Hospitality, Immersive Chefs, and Gateway, registered two fully initialized pawns through Hospitality's real map component and `CompGuest.Arrive`, and armed two native ingest jobs while paused. Player Space and speed-3 input completed both meals. A semantic map query then selected the exact spawned golden setting; `scenario-hospitality-returned-colony-cutlery-dirty-current.png` shows it on the map in the ordinary inspector with `Cleanliness: dirty`. A process-scoped click opened the fallback guest's native Gear tab; `scenario-hospitality-personal-cutlery-gear-current.png` shows the retained steel cutlery setting. The same loaded suite passed the active-Hospitality source-priority regression and dirty/wild-water personal-stack conservation regression. The run passed final Player.log scanning, exact-process shutdown, integration-stage and credential cleanup, and both normal configuration hash checks.

The accepted independent-child proof uses the exact-build visual run `artifacts\GatewaySmoke\20260803T180518207Z` and clean lifecycle rerun `artifacts\GatewaySmoke\20260803T181433734Z`. The explicit `immersive-chefs-independent-child-dining` scenario loaded exactly Core, Biotech, Harmony, Immersive Chefs, and Gateway, created a real child and the exact plated lavish-meal fixture, and armed only that fixture meal while paused. Process-scoped Space and `3` input changed the rendered game from paused to native superfast play; after ordinary ingestion, a map query found no lavish meal and found the exact returned golden plate and cutlery. The visual run's retained ordinary inspectors both read `Cleanliness: dirty`, while a process-scoped click opened the child's native Needs panel and visibly showed `Legendary cooking`, `Steaming hot meal`, and `Proper place setting`. Its host hold was interrupted only after those captures, so the clean rerun repeated the identical product assembly hash and native inputs, passed the same full-tree child/toddler loaded test without advancing the shared map, and retained passing final Player.log, exact-process shutdown, credential cleanup, integration-stage cleanup, and both normal configuration hashes.

The accepted first fabrication-path proof is the reviewed exact-build run `artifacts\GatewaySmoke\20260803T190127870Z`. The exact Core/Harmony/Immersive/Gateway run armed both real bills while paused; process-scoped Space and `3` input then completed ordinary hauling and work. A bounded map query found exactly one `Granite cookware set (excellent)` with `StuffDefName=BlocksGranite` and one `Steel cookware set (excellent)` with `StuffDefName=Steel`. The retained native inspectors visibly show the granite set as clean with Kitchen cleanliness 15, Kitchen comfort 10%, Cooking speed factor 60%, and Culinary quality modifier -6, while the steel set is clean with Kitchen cleanliness 50, Kitchen comfort 50%, Cooking speed factor 100%, and Culinary quality modifier +6. This closes the primitive-stone and vanilla-steel machining subset only; the remaining acquisition/material/equipment cases in gameplay task 2.6 stay open.

The accepted soft/adobe/smithy route proof is `artifacts\GatewaySmoke\20260803T193123650Z`, with the matching reviewed main-menu Def integration pass at `artifacts\GatewaySmoke\20260803T195141930Z`. The playable run loaded the exact Masonry dependency chain, then process-scoped Space and `3` input completed all six ordinary bills. The final map contained wooden plates x4, wooden cutlery x4, fixed-adobe plates x4, silver cookware x1, silver plates x4, and silver cutlery x4 after the corresponding exact allowed input stacks were consumed. Ordinary inspectors visibly showed the wooden plate's 10 cleanliness/30% comfort/80% eating-speed profile, adobe's 5/10%/65% profile, and masterwork silver cookware's 65 cleanliness/65% comfort/105% cooking speed/+14 culinary modifier. The separate settled-main-menu process passed both exact unit-cost/work-type and real optional-Masonry patch assertions; both processes cleaned up the exact owned PID and preserved the user's normal configuration hashes.

The accepted chef's-knife proof is `artifacts\GatewaySmoke\20260803T201902040Z`, with finalized-Def integration confirmation at `artifacts\GatewaySmoke\20260803T202947394Z`. Native Space and speed-3 input completed the real machining bill from its exact 30-steel fixture, producing one `Steel chef's knife set (excellent)`. Its ordinary inspector visibly reports Excellent quality, Kitchen cleanliness 50, Kitchen comfort 50%, Cooking speed factor 100%, and Culinary quality modifier +6. With the control pawn still visibly equipped with a normal steel knife, a native right-click exposed RimWorld 1.6's `Force equip` apparel action; after the native wear job, the Gear tab visibly retained both the ordinary weapon and the forced chef's knife in 1.6's utility `Equipment` group. The loaded Def test independently confirms `thingClass=Apparel`, waist/Belt layers, `equipmentType=None`, no `CompEquippable`, no mutable `CompSanitation`, the 30-unit recipe cost, and `TableMachining` recipe user. The playable process shut down cleanly, sanitized its credential, and preserved both normal configuration hashes.

The accepted glitterworld-cookware acquisition proof is `artifacts\GatewaySmoke\20260803T212044960Z`, with finalized-Def integration confirmation at `artifacts\GatewaySmoke\20260803T212649741Z`. The exact Core/Harmony/Immersive/Gateway process created a powered comms console and orbital beacon, placed 5000 colony silver only in the beacon's trade area, and stocked one Good-or-better set in an exotic orbital trader's directly held inventory. RimWorld's native trade dialog visibly showed `Glitterworld cookware set (good)` before purchase; its native buy control changed the order to one set and the silver balance by -1554, and native Accept plus time advancement caused the orbital delivery. Only then did an exact map query find the set in colony ownership. Its ordinary inspector visibly reports Good quality, self-cleaning, Kitchen cleanliness 100, Kitchen comfort 90%, Cooking speed factor 135%, and Culinary quality modifier +18%. The scenario never spawns the product for the player, so the observation exercises the real trade and delivery boundary rather than synthesizing its result. The independent settled-main-menu test confirms that the finalized item is buyable Spacer-tech content with generated-stock commonality, a fixed self-cleaning glitterworld material profile, and no manufacturing recipe.

The accepted clean-preference and urgent-fallback proof uses the detailed inspector run `artifacts\GatewaySmoke\20260803T214717060Z` plus the clean exact repeat `artifacts\GatewaySmoke\20260803T215433358Z`. Both Core/Harmony/Immersive/Gateway runs visibly began with both native bills armed and paused, and process-scoped Space and `3` input completed both ordinary cooking jobs. In the left kitchen, the initially clean Awful steel cookware moved from its supply cell to the stove and became dirty after use, while the Legendary golden dirty cookware and plate remained in their original cells despite their much stronger material and craftsmanship scores. In the right kitchen, its only dirty steel cookware moved to the stove, became the used set, and the resulting ordinary Simple-meal inspector visibly reports one bound plate and Good culinary quality. Exact map identities and positions separately confirm two meals and the three spawned cookware controls. The first run's outer shell timeout detached its launcher after the retained observations, so its exact process was closed separately; the 90-second repeat reproduced the same native outcomes and then passed the launcher's final Player.log scan, exact-process shutdown, credential and stage cleanup, and both normal configuration hash checks. This closes clean-first selection and the urgent dirty-ware path without directly registering urgency or synthesizing a product.

The accepted exact-complexity proof is `artifacts\GatewaySmoke\20260803T222808778Z`, with post-review base and Vanilla Cooking Expanded finalized-runtime matrices at `artifacts\GatewaySmoke\20260803T224423685Z` and `artifacts\GatewaySmoke\20260803T224501097Z`. In the visible exact Core/Harmony/Immersive/Gateway process, one Normal-speed run exposed the selected `MealSimple` at 22:28:56 UTC while the other cooks were still working, `MealFine` at 22:29:08, and `MealLavish` at 22:29:27. Their ordinary inspectors visibly identify Simple, Fine, and Lavish meals, each with one bound plate; no scenario step creates a meal or calls recipe completion. Both settled-main-menu matrices independently prove the finalized Harmony work boundary has exactly one Immersive Chefs postfix, applies 0.75x/2x/3x to every exact vanilla single/bulk and meat/vegetarian recipe named by the contract, leaves survival meals and pemmican unchanged, and—when the actual Vanilla Expanded Framework and Vanilla Cooking Expanded dependency chain is loaded—leaves `VCE_CookBakeSimple` at 1x. The VCE Def is absent rather than invented in the base matrix. All three accepted processes completed final log scanning, exact-process shutdown, credential/stage cleanup, and both normal configuration hash checks.

The accepted cooperative-cooking proof is split across focused exact Core/Harmony/Immersive/Gateway runs. `artifacts\GatewaySmoke\20260804T062639211Z` visibly shows the specialist leave for the powered linked sauce station during the lead's ingredient collection and then report `Manning linked kitchen station` while both leads perform their native cooking jobs. In `artifacts\GatewaySmoke\20260804T063410528Z`, the assisted meal appeared at tick 622 and the matched control at tick 674; their ordinary inspectors report culinary scores 47 and 36, respectively. That run exposed a real pooled-job identity defect when the specialist stayed claimed after the lead began wandering. The fix retains both the native `Job` reference and its `loadID`; `artifacts\GatewaySmoke\20260804T065346937Z` uses the rebuilt assembly and visibly shows the specialist `Wandering` after both meals finish. Finally, `artifacts\GatewaySmoke\20260804T070444196Z` invokes the specialist's native Draft toggle: the ordinary pawn panel changes from `Manning linked kitchen station` to `Watching for targets`, while the selected lead continues to report `Cooking simple meal`. The scenarios never directly advance work or construct meal products; the bounded launcher owns final log scanning, exact-process shutdown, credential/test-stage cleanup, and normal configuration restoration.

The accepted handheld-food exclusion proof is `artifacts\GatewaySmoke\20260803T230951294Z`, with finalized-Def confirmation at `artifacts\GatewaySmoke\20260803T225448600Z`. The reviewed exact Core/Harmony/Immersive/Gateway scenario asked vanilla's food chooser for the exact Pemmican and packaged-survival-meal objects, then a process-scoped Normal-speed action let both native `Ingest` jobs finish. The retained ordinary map and inspectors visibly show Pemmican reduced from x50 to x30, the packaged meal absent, both rooms free of dirt or a returned dish, and all four adjacent steel plate/cutlery controls still in their original cells with `Cleanliness: clean`. The settled main-menu integration test independently confirms that both finalized excluded-food Defs are outside meal coverage and contain neither embedded-ware nor culinary/temperature components. The visible run passed final Player.log scanning, exact-process shutdown, credential/stage cleanup, and both normal configuration hash checks.

The accepted vanilla nutrient-paste dining proof uses `artifacts\GatewaySmoke\20260803T234936916Z` for the carried-meal observation and the clean lifecycle run `artifacts\GatewaySmoke\20260803T235402783Z` for the completed result. Both exact Core/Harmony/Immersive/Gateway processes asked vanilla's food chooser for an `Ingest` job targeting the exact real powered dispenser. In the first run, a process-scoped Normal-speed action reached the native carrying phase; `nutrient-paste-carried-mid-job-centered.png` visibly shows the pawn holding a nutrient paste meal beside the dispenser while the ordinary pawn inspector reports `Carrying: Nutrient paste meal` and `Consuming nutrient paste meal`. In the second run, a process-scoped Superfast action completed ingestion; the meal count reached zero, and `nutrient-paste-returned-plate-dirty-centered.png` plus `nutrient-paste-returned-cutlery-dirty-centered.png` visibly identify the exact returned Normal steel items with `Cleanliness: dirty`. The final run passed the flushed Player.log scan, exact-process shutdown, credential/stage cleanup, and both normal configuration hash checks.

The accepted prepared-paste proof is `artifacts\GatewaySmoke\20260804T002340916Z`. The reviewed exact Core/Harmony/Immersive/Gateway scenario visibly opened on a powered dispenser/hopper fixture with one selected operator and no serving ware. A process-scoped native right-click exposed `Dispense prepared cooking paste`; selecting that exact float-menu option started `ImmersiveChefs_DispensePreparedPaste`, and a semantic Normal-speed action completed the ordinary job. `prepared-paste-completed-inspector.png` visibly shows the resulting `Prepared ingredients x18` stack at the dispenser and the ordinary inspector reads `Preparation quality: 20/100`, `Source: nutrient paste`, and `Nutrition per unit: 0.05`. Supporting exact inspection identified the same stack as `ImmersiveChefs_PreparedFood42485`. The run passed its flushed Player.log scan, exact-process shutdown, credential/stage cleanup, and both normal configuration hash checks.

The accepted prep-station schedulability proof is `artifacts\GatewaySmoke\20260804T074951657Z`. The preceding exploratory run `20260804T073415033Z` visibly left the unrestricted Cooking-only chef wandering; finalized workgiver diagnostics showed vanilla `DoBillsCook` scans only its two stove Defs, exposing that `requiredGiverWorkType=Cooking` did not make the custom station scannable. After the dedicated native `WorkGiver_DoBill` Def was added, the clean exact Core/Harmony/Immersive/Gateway run passed `PreparedFoodWorkGiverTargetsOnlyThePrepStation`; `prepared-workflow-fixed-prep-in-progress.png` visibly shows the selected chef carrying Rice x10 and reporting `Preparing ingredients`, and `prepared-workflow-produced-stack-inspector.png` visibly shows the two real bills' combined `Prepared ingredients x20` with preparation quality 100/100, RawRice provenance, and 0.05 nutrition per unit. A later matched workflow exposed the visible-source eligibility bug: `artifacts\GatewaySmoke\20260804T102304021Z` left the prepared cook wandering while the raw cook completed, and the exact loaded-game assertion failed in `artifacts\GatewaySmoke\20260804T102918415Z`. After source expansion was limited to hidden provenance, `artifacts\GatewaySmoke\20260804T103124132Z` passed that loaded assertion and `artifacts\GatewaySmoke\20260804T103221524Z\prepared-meal-first-raw-cook-active.png` visibly shows the prepared meal selected while the raw cook is still performing native `DoBill`; the ordinary meal inspector retains Rice, one bound plate, culinary quality 43, and steaming-hot temperature. At equal temperature, supporting live measurements recorded prepared/raw rot deltas of approximately 61060.77/15259.65, or 4.001x, and the retained ordinary inspectors visibly report the much shorter prepared-food spoilage time. The clean current-build repeat `artifacts\GatewaySmoke\20260804T111449262Z` reproduced the prepared-first behavior and completed final log scanning, correlated `202 Accepted` shutdown, exact-process exit 0, credential cleanup, and both normal configuration hash checks. The save/load half is closed separately below.

The accepted prepared-food persistence proof is `artifacts\GatewaySmoke\20260804T114002600Z`. Its passive scenario directly arranged only the persistent fixture and contained no save, load, Scribe, or `GameDataSaveLoader` call. The player workflow used process-scoped screen-local input to open RimWorld's Escape menu, click the native Save button, type `ImmersiveChefsPreparedRoundTrip`, click Save, reopen the menu, click its newly available native Load button, and select the visible save row. Gateway UI state observed the real transition through `MapInitializing` and back to `Playing`. `scenario-prepared-food-before-save.png` shows the ordinary pre-save inspector with `Prepared ingredients x7`, 60/60 hit points, quality 83/100, `RawRice, AgaveFruit`, nutrition 0.05, and active spoilage; `native-save-dialog-02.png` and `native-load-dialog.png` retain the two native dialogs; `prepared-food-after-native-reload.png` visibly shows the reconstructed selected stack with the same count, hit points, quality, sources, nutrition, and active spoilage. In the same exact Core/Harmony/Immersive/Gateway process, `PreparedFoodRoundTripsThroughTheRealScribePipeline` passed through RimWorld's actual deep Scribe path and independently retained both contribution records, source counts and craftsmanship, hidden-source flag, dietary flags, poison snapshot, preparer ID, stack count, and exact rot progress. The run delivered its final correlated shutdown response, exited code 0 without force, removed credentials and test stages, and preserved both normal configuration hashes. The earlier valid scenario exposed an overlong composed screenshot request ID at `artifacts\GatewaySmoke\20260804T113558123Z`; the general deterministic request-ID derivation now permits descriptive scenario and step names without exceeding the established 64-character correlation contract.

The accepted VNPE prepared-paste proof is `artifacts\GatewaySmoke\20260804T013057645Z`. The exact Core/Harmony/VEF/VNPE/Immersive/Gateway process visibly showed the real connected vat at 12 stored meals before any order. Gateway screen-local clicks selected the operator, opened the tap's native right-click menu, and chose `Dispense prepared cooking paste`; Normal speed completed the ordinary pawn job. `vnpe-prepared-output-inspected.png` visibly shows `Prepared ingredients x18`, `Preparation quality: 20/100`, `Source: nutrient paste`, and `Nutrition per unit: 0.05`, while `vnpe-vat-after-dispense.png` visibly shows 11 stored meals. The run passed the final log scan, exact-process shutdown, credential cleanup, and unchanged normal configuration hashes. Final current-build companion run `artifacts\GatewaySmoke\20260804T020012905Z` loaded the same exact matrix and passed the finalized Def/type/declared-override, native Harmony owner, live Off-setting, package inventory, and no-hard-reference integration contract.

The review follow-up `artifacts\GatewaySmoke\20260803T123106052Z` closes the negative player-visible cases that state probes cannot accept. After the same three native jobs completed, `conscious-patient-needs-visible.png` again shows `Ate without cutlery -3`. The selected pawns' native Needs panels in `unconscious-patient-needs-visible.png`, `cutlery-nurse-needs-visible.png`, `conscious-nurse-needs-visible.png`, and `unconscious-nurse-needs-visible.png` visibly omit that memory; `cutlery-patient-needs-visible.png` likewise shows the cutlery-equipped patient received normal dining feedback without the missing-cutlery thought. These screenshots establish that the consequence belongs only to the conscious patient actually fed without cutlery, rather than to the nurse or an unconscious recipient.

The accepted unobtrusive-launch proof uses three clean exact Core-plus-Gateway runs. `artifacts\GatewaySmoke\launcher-final-minimized-muted\20260803T111321048Z` natively observed PID 29700 as minimized, retained isolated `runInBackground=True` and `volumeMusic=0`, and recorded ticks advancing `172→475` over five seconds through authenticated requests without activating the window. `artifacts\GatewaySmoke\launcher-final-visible-options\20260803T111459443Z` natively observed explicit `-VisibleWindow` as Normal and retains ordinary Options screenshots visibly showing `Run in background` checked and `Music volume: 0%`. `artifacts\GatewaySmoke\launcher-final-auto-visible-regression\20260803T111701296Z` proves `gateway-regression` selected Normal visibility automatically with `VisibleWindowRequested=false`, injected its exact-PID raw click, completed the semantic regression/restoration, and shut down cleanly. All three runs retained equal before/after hashes for the user's normal `ModsConfig.xml` and `Prefs.xml`.

The accepted intentional window-change regression is `artifacts\GatewaySmoke\launcher-user-window-change-final\20260803T143327162Z`. The real launcher requested `Minimized`, then the exact owned window was deliberately maximized while both RimWorld and the launcher host remained alive. That controlled regression is distinct from the later orbital-trade run in which RimWorld maximized automatically. Together they support the simpler contract: the launcher performs no post-start window-state probe, and window changes are neither classified nor enforced.

The shutdown hardening behind the final run used compile-failing RED evidence at `artifacts\TestResults\20260803T091351638Z-46764-43f9c376222f408095d03b27677cd3ba` for the retained host lifecycle and `artifacts\TestResults\20260803T092140018Z-32508-e85372fce2e848af8e6598bd2e1e778e` for recovery-safe log markers. A later real run exposed a distinct response race: the Gateway journal recorded `202 Accepted`, but listener cancellation could close the connection before the caller received the body. The transport-completion handoff began with compile-failing RED `artifacts\TestResults\20260804T110629705Z-41504-d6a1cd15ce7f4c13a696b6034fa5175c`; focused GREENs are `20260804T110745086Z-45452-89606b3191664f51ade779f8d888f972` and `20260804T110837813Z-47648-2d022232d212424f8c5353b35efe72be`, and the complete Gateway suite passed 567/567 at `artifacts\TestResults\20260804T111023785Z-26228-647bd22ccec048499ab66a838a45192f`. Real Unity/Mono proofs `artifacts\GatewaySmoke\20260804T111407749Z` and `artifacts\GatewaySmoke\20260804T111449262Z` both delivered the complete correlated shutdown body before teardown; the latter then exited code 0 and passed every cleanup lane. The event-driven handoff runs only after the output stream is written and flushed and does not delay shutdown by a guessed timeout. Window maximize/restore changes after startup are never inspected, classified, or enforced and are unrelated to this transport fix.

The accepted foreground-input hardening run is `artifacts\GatewaySmoke\20260804T014438227Z`. Its exact Core/Gateway process hit the real transient path: `click.json` records one `focus_reacquire` followed by one mouse down/up pair, `ClickOutcome` is `injected`, and exact-PID, credential, integration-stage, and normal-config cleanup all passed. The corresponding Release suite is `artifacts\TestResults\20260804T015141775Z-31332-786d2c6b292f442f8ecd7c7a2ef5f6f9`: all 560 executable tests passed with only the three documented Microsoft-net48 listener cases not executed.

The accepted imported-meal plating run is `artifacts\GatewaySmoke\20260804T034902447Z`. The exact Core/Harmony/Immersive Chefs/Gateway process passed the product-owned finalized JobDef/WorkGiver/Harmony test. Its ready screenshot visibly shows `Simple meal x3`, Rice, quality 73, 42 °C, rot progress, and `Service ware: unplated`; after the sole game-time resume action, `imported-meal-plating-progress.png` visibly shows the same selected stack at the fueled stove with `Bound plates: 3`, Rice, and quality 73. Final log, process, credential, integration-stage, and normal-config cleanup all passed. The preceding rejected load `20260804T034454797Z` exposed and preserved the Harmony `t` parameter-name mismatch before the final exact-signature regression was added. The post-review repository regression is `artifacts\TestResults\20260804T040229734Z-36508-6f322648d1e64f10ae784f1c113b7255`: all 726 executable tests passed, with only the three documented Microsoft-net48 listener cases not executed.

The accepted main-menu integration proof is `artifacts\GatewaySmoke\20260802T074054731Z`. In a fresh Core-plus-Gateway process, RimWorld reached its settled interactive main menu, the finalized Steel Def export contained the real Gateway XML-patch marker with no projection warning/truncation, and both exact `MainMenuLoaded` fixtures passed. Unauthenticated access returned `401`, raw unrestricted execution and raw click injection succeeded, the endpoint and persisted integration snapshots matched, shutdown left a token-free stopped session, the exact PID died, the owned test stage was removed, and `evidence-summary.json` records identical before/after normal mod-list SHA-256 values of `7B516628E3EFB1813848D0CD100C2DD3A7853EC14EE6ADBA52EFC2163B980957`. The retained PNGs show RimWorld's normal interactive main menu with no error dialog.

The accepted Immersive Chefs product integration proof is `artifacts\GatewaySmoke\20260802T111514801Z`. This historical run predates the `fumblesneeze.*` package-identity migration and therefore cannot evidence the current stable IDs: its retained active-mod list, deployment paths, and test owner truthfully use `fumblesneeze.immersivechefs` and `fumblesneeze.rimworlddevgateway`. The harness built and deployed the repository's exact Immersive Chefs project, then RimWorld reported the exact ordered active and loaded set Core, Harmony, Immersive Chefs, and Gateway. Discovery attributed `ImmersiveChefs.InGame.IntegrationTests` to the active `fumblesneeze.immersivechefs` content pack; its constructor/active-set assertion and finalized-Steel XML assertion both passed. The same process's bounded Def export contains both `patched-by-immersive-chefs-xml` and `patched-by-gateway-xml`, and the personally inspected exact-process Gateway PNG shows a normal main menu without an error dialog. `evidence-summary.json` retains the complete ordinary product-package inventory and managed references, the exact expected product test, equal normal configuration hashes, a successfully injected raw player click, and completed process/credential/stage/FlaUI-service cleanup. The inverse proof is `artifacts\GatewaySmoke\20260802T111650331Z`: with only Core and Gateway active under the former ID, the bundle and runtime snapshot contain only the Gateway-owned test assembly, no result is attributed to Immersive Chefs, and finalized Steel contains no Immersive Chefs marker.

The accepted playable-map semantic and failure-isolation proof is `artifacts\GatewaySmoke\20260802T073643956Z`. RimWorld loaded exactly Core plus Gateway, reached a playable Quicktest map, retained the unrestricted-execution warning and integration status overlay, and remained responsive after the exact deliberate map fixture failed while the later map fixture passed. Raw C# proved main-thread state/restoration; direct spawn replay reused one run and cleanup removed only returned handles; thing query/inspection/selection, a real native debug action, pawn drafting `false→true→false`, and two Plan-designator drags completed with correlated request evidence and cleanup. The camera visibly panned from the quickstart colony at `(125,125)` to a distant queried object at `(195,40)`, zoomed `24→19`, and restored `(125,125)@24`; the four retained rendered frames visibly agree with the settled game-state captures. The mutation responses now carry the same exact center, root size, and view rectangle as those post-frame snapshots rather than RimWorld's stale once-per-frame `CurrentViewRect` cache. Controlled shutdown sanitized credentials, removed the exact integration-test stage, left no owned RimWorld PID, and preserved the same normal mod-list hash. `evidence-summary.json` durably records both identical point-in-time hashes and all final cleanup paths without the bearer token. The harness issued no pawn movement order: changing pawn positions between frames are normal unpaused pawn activity, while the separate disposable spawn probe creates and deletes its own pawn.

The accepted object-bounded screenshot proof is `artifacts\GatewaySmoke\object-bounded-20260804T1442Z\20260804T144328765Z`. The passive, minimized Core-plus-Gateway Quicktest used no named scenario or setup mutations. A current-view query found existing colonists; the exact handles `Human1007` (Boland) and `Human1010` (Sally) were selected and sent through the companion client's `--things` path with 48 pixels of padding. The unchanged full frame is 2560×1440 and 8,758,441 bytes; the decoded target crop is 486×456 and 434,746 bytes. `object-crop-two-pawns.png` visibly contains both exact pawns and their native selection brackets while excluding the top bar, bottom controls, and most unrelated map area. The retained request, target query, selection response, PNG hashes, observation, and post-capture log page report zero matching errors. The harness then delivered controlled shutdown, closed exact PID 35692, sanitized credentials, cleaned its stage, retained a token-free tombstone, and preserved both normal configuration hashes.

The first accepted product-owned E2E compatibility proof is `artifacts\EndToEndRuns\LegacyUnplatedAccepted\20260805T022636270Z`. One minimized exact Core/Harmony/Immersive Chefs/Gateway process dynamically loaded only `ImmersiveChefs.EndToEndTests`, selected a debug/mod-style Simple meal with zero embedded plates, invoked RimWorld's native Consume float-menu callback, advanced into the real `Ingest` toil, and completed ordinary ingestion. The personally inspected object-bounded frames visibly show the meal before the order, the diner working at it, and the diner afterward with no returned plate or cutlery. The durable terminal checkpoint independently records the original meal destroyed and zero service ware on the map, carried, or in the diner's inventory. All 14 player-action/observation steps passed, the leased test stage and credentials were removed, the exact process closed, Player.log had no matching errors, and both normal configuration hashes were preserved.

The accepted plate-material proof is `artifacts\EndToEndRuns\PlateMaterialRejectedAlternatives4\20260805T033802343Z`. The same exact mod group dynamically loaded the product-owned tier test, created four sealed kitchens with unplated Simple/Fine/Lavish meals and deliberately stocked wood, granite, steel, and silver plates, then resumed ordinary Cooking work. The personally inspected frames show all four stocked fixtures, cooks operating every stove, and four finished meals dropped afterward. The terminal checkpoint records the exact physical Stuff retained inside each meal—Simple/WoodLog, Simple/BlocksGranite, Fine/Steel, and Lavish/Silver—and separately proves the Fine room left WoodLog plus BlocksGranite spawned while the Lavish room left Steel spawned. The separate finalized-Def run `artifacts\GatewaySmoke\20260805T030941109Z` passed the exact product main-menu test against the loaded XML and Stuff database; both runs removed their leased test stages and credentials, closed only their owned process, and preserved normal configuration.

The accepted complete RimCuisine/No-Vanilla proof is `artifacts\EndToEndRuns\Grouped\20260805T171604862Z`. Its one exact process loaded Core, Harmony, Processor Framework, all four RimCuisine 2 modules, No Vanilla Meals, Immersive Chefs, and Gateway, then ran all three compatibility tests sequentially. Two Cooking-only colonists used ordinary `DoBill` jobs at fueled stoves: the ordinary UI visibly shows `Thin pottage x5` with `Bound plates: 5` and `Extravagant meal x10` with `Bound plates: 10`, while the latter's ingredient line contains only upstream cassowary egg, agave fruit, corn, and raw fungus. The selected returned cookware in both kitchens visibly reads `Cleanliness: dirty`. A native orbital trade dialog then showed generated canned meal, hardtack, dried meat, wine, raw barley, and cupcake stock with enabled purchase controls, accepted all six transfers, and delivered them; a capable colonist separately made cigarettes through RimCuisine's native DrugLab bill. Ordinary inspectors for all seven retained products omitted Immersive Chefs meal state, while the terminal checkpoint records each exact product as uncovered with neither embedded ware nor culinary state. Removed vanilla meal Defs remained absent and the surviving stale bulk recipes `CookSimpleMealBulk`, `RC2_CookFineMealBulk`, and `RC2_CookLavishMealBulk` retained the native multiplier. Finally, another native orbital trade purchased an upstream-generated pizza: the delivered selected pizza visibly reads `Bound plates: 1`, and the checkpoint proves the same physical Poor Silver plate transferred exactly once. The run passed final log scanning and cleaned the exact process, credential, Workshop override, and leased test stage while preserving both normal configuration hashes.

The accepted Fast Meals/Meals on Wheels/Prioritize proof pairs `artifacts\GatewaySmoke\20260805T214356163Z` with `artifacts\EndToEndRuns\Grouped\20260805T215753247Z`. The exact loaded integration run validates the inspected optional assembly identities, exact `FoodUtility.TryFindBestFoodSourceFor` target and low-priority Meals on Wheels owner, Prioritize startup type/ledger/trader postfix owner, finalized preferability and offsets, and Fast meal coverage. The three sequential E2E tests then visibly show a diner carrying and consuming another pawn's exact plated Simple meal before the same wooden plate and cutlery return dirty; a second diner carrying and consuming the farther perishable Fast meal while nearer survival meals and Pemmican remain unchanged, again returning the exact setting dirty; and RimWorld's native `TraderCaravanArrival` creating a full caravan and arrival letter after the controlled humanlike trader entered the upstream compensation boundary foodless. The checkpoint records that same trader receiving `Pemmican:52` from the supported upstream postfix. All tests passed with final log, exact-process, credential, stage, and normal-configuration cleanup.

The accepted VCE/Fried/Adaptive/Overcooked proof pairs `artifacts\GatewaySmoke\20260806T024227225Z` with `artifacts\EndToEndRuns\Grouped\20260806T024836606Z`. The exact finalized-Def process loaded Core, Harmony, VEF, VCE base/Bakery/Haute/Stews/Fishing/Sushi, Fried, Adaptive Meal Bill, Overcooked Meals, Immersive Chefs, and Gateway; every declared recipe and final meal matched its package-attributed registry, every actual `WorkAmountForStuff` reflected the Harmony multiplier, non-meal Bakery/canning/condiment products remained untouched, and VEF retained its processor components. In the grouped E2E process, five cooks entered ordinary native `DoBill` jobs. Personally inspected frames show Simple, Fine, and Lavish bakes with two bound plates each, the Gourmet fritter with one, every used cookware set dirty, and Adaptive's live-selected Fine-veg product surviving Overcooked replacement with its one physical plate exactly once. The canned-meat inspector has no Immersive Chefs fields and its clean control plate/cookware remain on the map. The terminal checkpoint records actual work `337.5/1350/3600/4500`, all per-serving Stuff bindings as Wood/Wood, Steel/Steel, Silver/Silver, and Silver, exact untouched lower-tier stack counts, and zero ware lifecycle for canning. The runner retained a minimized launch contract and used no external focus, restore, maximize, mouse, or keyboard operation.

The accepted Replimat failed-dispense proof is `artifacts\EndToEndRuns\Grouped\20260806T054052469Z`. One minimized exact Core/Harmony/Replimat/Replimat Meals/Dubs Bad Hygiene/Common Sense/Immersive Chefs/Gateway process ran both the rejection and established success cases sequentially. The rejection fixture supplied exactly `0.5` feedstock and started two ordinary hungry diners with separate clean steel settings. The acting agent inspected the frames showing the target already in its native `Consuming replicated meal` job, the nearer diner visibly carrying the sole Ramen, and the target's exact returned plate and cutlery each reading `Cleanliness: clean` in the ordinary inspector. The terminal checkpoint records the first native dispense reducing feedstock to `0.0327103138`, the rejected dispense consuming nothing further, no second meal, no remaining dining session, and the same physical ware clean. The successful companion case still visibly produces, carries, and consumes Ramen before returning its exact ware dirty. Both tests, final log scanning, exact-process shutdown, credential/stage cleanup, and normal configuration checks passed without foreground input or a maximize request.

The accepted Replimat exclusion proof is the reviewed four-test rerun `artifacts\EndToEndRuns\Grouped\20260806T061201358Z`, paired with exact loaded-main-menu assertions at `artifacts\GatewaySmoke\20260806T061059475Z`. In the ordinary animal workflow, a selected raccoon visibly reports `Consuming kibble` from the Replimat feeder's native loose `Kibble x75`; the consumed stack falls to 67 while the individually inspected steel plate and cutlery controls remain clean. In the separate player workflow, the terminal's enabled native `Batch survival meals` gizmo opens the real `Batch replicate survival meals` dialog, and a registered exact-window action confirms one Packaged survival meal. Its ordinary inspector identifies the selected upstream product while individually inspected golden plate and cutlery controls remain clean. The Gateway registration privately binds the exact Replimat assembly/type/argument fields/void callback; the shared E2E contract supplies only the expected window type, and unregistered or drifted shapes fail closed. The exact process stayed minimized and neither path used focus, restore, resize, maximize, mouse, or keyboard input.

The accepted Replimat returned-setting cleanup proof is the strengthened exact-group run `artifacts\EndToEndRuns\Grouped\20260806T065124501Z`, paired with the reviewed loaded-main-menu run `artifacts\GatewaySmoke\20260806T065013055Z`. The native terminal path still visibly collects service ware before producing and carrying Ramen, while the checkpoint observes that exact plate and cutlery spawned dirty at completed ingestion. The selected Cleaning-capable diner then visibly carries `Steel plate (normal)` with current `Doing dishes` and `Queued: Doing dishes`; the test requires the queued job to target the other exact returned item and the same dishwasher, distinguishing Common Sense's committed two-item handoff from ordinary one-at-a-time Cleaning work. The next inspected frame selects the powered, Dubs-connected appliance beside its real water tower and visibly reports `Dishwasher: loading` plus `Capacity: 1.25/16 place settings`. Exact holder assertions confirm that same physical plate and cutlery are the two dirty admitted Things. All four group tests passed sequentially, the process stayed minimized, and final log, exact-process, credential/stage, and normal-configuration cleanup passed without foreground input or a maximize request.

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
