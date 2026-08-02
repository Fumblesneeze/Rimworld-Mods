## Why

Reliable RimWorld mod verification needs game-native visibility and control that desktop UI automation cannot provide consistently. A separate, explicitly developer-only gateway mod will expose a stable local API for inspecting and driving a running game, executing one-off verification logic, and creating repeatable scenario quickstarts without coupling shipping gameplay mods to test infrastructure.

## What Changes

- Add the independently loadable **RimWorld Dev Gateway** mod with a versioned HTTP API bound only to loopback, hosted by pinned EmbedIO 3.5.2 and its managed .NET Standard 2.0 runtime dependency rather than a hand-written HTTP parser.
- Authenticate every request with a newly generated per-run bearer token recorded in a per-run session manifest.
- Expose structured game status, selected UI state, recent log events, screenshots, and bounded operation results.
- Provide process-scoped click, drag, and key-input endpoints; keep timed native drag delays off Unity's main thread so the game can render and consume injected events during the gesture.
- Add a stable semantic game-control surface for developer/god mode, pause and speed, camera movement/zoom, map and view thing queries, bounded thing inspection, and atomic selection changes.
- Discover and invoke native debug actions, including deterministic direct spawning of things and pawns, without depending on screen coordinates.
- Discover selected-thing and architect gizmos and apply immediate, toggle, target, placement, and drag interactions through stable-in-run handles and explicit interaction phases.
- Provide unrestricted in-process execution, enabled by default for this strict development-only mod, through a primary stateful raw-C# REPL endpoint backed by bundled Mono.CSharp and run on the game main thread.
- Retain uploaded compiled assemblies and a companion host compiler/client only as optional fallbacks for code that exceeds Mono.CSharp's language/runtime capabilities; neither is required for normal REPL use.
- Add named automation support, including a quickstart flow that may use RimWorld `-quicktest` followed by an idempotent spawn/setup automation for mod-specific test scenes.
- Add a bounded, filterable post-load Def export so developers can inspect the exact finalized Def objects produced by the active mod list when authoring XML patches.
- Add an opt-in in-game integration-test contract and runner that discovers separately staged test assemblies for active mods and executes them once at declared RimWorld lifecycle points.
- Add prominent development-only warnings, controlled shutdown, request correlation, timeouts, and stable JSON result/error envelopes.
- Implement these gateway capabilities in this change; this is not merely a future product specification.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`, repository path `mods/RimWorldDevGateway`; newly implemented by this change.
- **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`, repository path `mods/ImmersiveChefs`; unaffected and must never depend on RimWorld Dev Gateway.

## Capabilities

### New Capabilities

- `dev-gateway-service`: Loopback service lifecycle, per-run authentication, API versioning, dispatch, bounded responses, warnings, and controlled shutdown.
- `dev-gateway-observability`: Structured status, UI-state, log, and screenshot retrieval.
- `dev-gateway-input`: General click, drag, and key-input operations executed safely within the running game.
- `dev-gateway-game-control`: Semantic game state/control, thing discovery and inspection, selection, native developer actions, spawning, gizmos, and target/placement interactions.
- `dev-gateway-execution`: Unrestricted, default-enabled in-process execution through a stateful raw-text Mono.CSharp REPL, with uploaded assemblies and host-side snippet compilation as optional fallbacks.
- `dev-gateway-automation`: Discoverable named automations and repeatable quickstart/spawn scenario setup.
- `dev-gateway-def-export`: Deterministic, bounded JSON snapshots of the live post-load Def databases with source-mod metadata and safe graph handling.
- `dev-gateway-integration-testing`: Startup-gated discovery, lifecycle execution, result reporting, and host-side staging of separate in-game integration-test assemblies.

### Modified Capabilities

None.

## Impact

- Adds a second mod under `mods/RimWorldDevGateway` with package ID `fumblesneeze.rimworlddevgateway`, packaged `EmbedIO.dll`, `Swan.Lite.dll`, `System.ValueTuple.dll`, and `Mono.CSharp.dll` runtime dependencies that must be proven inside RimWorld's Unity Mono host, plus an optional companion host-side client/compiler tool.
- Introduces a loopback-only HTTP surface and an intentionally unrestricted in-process execution boundary; the mod is unsuitable for normal play and must advertise that fact in metadata, startup logs, UI, manifest, and API status.
- Adds fast contract/unit tests plus startup-gated in-game Def/XML/Harmony integration tests and scenario verification. The gateway is test infrastructure only and is not a required, optional, or load-order dependency of Immersive Chefs; product test assemblies may reference the test contract but are staged separately and never ship in a product package.
