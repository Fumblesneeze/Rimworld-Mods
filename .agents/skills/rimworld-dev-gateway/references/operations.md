# Gateway operations and failure guide

## Stable operational choices

- Transport: pinned EmbedIO on Unity Mono/.NET Framework-compatible assets; no ASP.NET Core shared
  framework and no custom HTTP parser.
- Execution: unrestricted raw-text Mono.CSharp endpoint is always available while the development
  mod is loaded. Uploaded assemblies are an optional escape hatch, not the only way to execute code.
- Security boundary: authenticated per-run bearer token, IPv4 loopback, conspicuous in-game warning,
  disposable savedata. This blocks accidental remote access, not same-user malicious code.
- Threading: HTTP workers parse/authenticate and enqueue. Verse/Unity work runs on the game main
  thread. End-of-frame screenshot work remains asynchronous to the request handler.
- State: Things, selections, gizmos, debug actions, and interactions are addressed by resolvable
  runtime/stable handles. Re-query after map, owner, selection, or command-list changes.

## Capability routing

Use `docs/Gateway.md` for exact current paths. The stable categories are:

- get/set developer mode, god mode, pause, speed; include speed in UI-state snapshots;
- map/on-screen Thing listing with filters, exact inspection, multi-selection, camera move/zoom;
- native debug-action discovery/invocation and Thing/Pawn spawning;
- gizmo discovery and immediate, toggle, placement, line, rectangle, and drag interactions;
- exact-PID screen-local mouse and keyboard input;
- full-frame, cell/object-bounded, and multi-object union screenshots;
- bounded Def export of finalized loaded Defs;
- raw text REPL, uploaded assemblies, named automations, integration tests, and E2E tests;
- controlled shutdown and credential-free session tombstones.

Do not guess fuzzy debug/gizmo labels. Require exact path, runtime kind, availability, and cardinality.
Pointer-required tools must be activated semantically and completed with exact-PID screen-local input.

## Launcher contract

Use the repository launch scripts rather than manually editing the user's config. A verification
launch should produce:

- Harmony-first exact mod order and Gateway last;
- unique short savedata/evidence paths and a dedicated log;
- the launcher's fixed windowed 1600×900 render size, minimized startup, background execution, and
  music volume zero;
- developer mode only after an explicit game-state action or scenario enables it for the workflow;
- process PID/start identity, product package hashes, loaded/configured mod order, request IDs,
  screenshots, logs, cleanup, and normal `ModsConfig.xml`/`Prefs.xml` hashes.

Maximization is never an input prerequisite. The user may maximize the window to watch; automation
must recalculate client-local coordinates and continue. Do not add a watchdog that crashes or closes
the process because its window state changed.

## Quicktest symptom guide

| Symptom | Likely cause | Correct response |
| --- | --- | --- |
| Startup flickers, pawn briefly moves, or fixtures appear | An implicit/default scenario is running | Launch with `-Quicktest` and omit `-Scenario`; run one explicitly selected scenario/test only |
| Camera endpoint reports success but view does not move | Stale state, wrong program boundary, or unobserved client | Re-query UI/camera state and capture before/after from the same PID; do not add desktop guesses |
| `Could not find player faction` | Quicktest reached a map before faction/player-control readiness | Wait for player faction and `Game.PlayerHasControl` before setup/actions |
| RimWorld freezes after an endpoint call | Main-thread blocking, recursive enumeration, or uncancellable user code | Preserve journal/log/dispatcher evidence; do not immediately retry; terminate only the exact PID if graceful shutdown cannot run |
| Integration test passes without the product mod active | The host loaded a test bundle outside its owner/exact matrix | Require the owner mod in the complete ordered active package set and validate again in the runtime |
| Screenshot lacks the inspect pane | Multiple Things were selected | Frame all related Things with the camera, then select only the evidence target before a full-frame capture |
| Pointer click works only maximized | Coordinates were desktop- or fixed-window-relative | Use screen-local input against current exact-PID client bounds |

## Main-thread robustness

- Never sleep or wait for network/process work on the Unity thread.
- Express multi-frame work as iterator steps with fixed per-frame cost and bounded deadlines.
- Snapshot collections before serialization; isolate failures per provider/Thing and continue within
  the declared budget.
- Persist admission/journal state before invoking arbitrary code.
- A deadline before start may cancel safely. A timeout after user code starts is cooperative only;
  report that it may still be running.
- Restore pressed input, active interaction, selection, camera, developer/god state, pause/speed, and
  every setting in failure cleanup.

## Evidence discipline

Use direct mutation only for arrangement/diagnosis. For the claim itself retain:

1. before state visible to a player;
2. the native player action or faithful semantic/input automation;
3. an intermediate action frame when causality could otherwise be ambiguous;
4. the resulting inspect panel, physical Thing/job/hediff/thought/building state;
5. exact build/mod/PID identity, clean relevant log, config restoration, and cleanup.

Never accept an endpoint merely because it returned `ok`, and never accept product behavior from a
diagnostic REPL assertion.
