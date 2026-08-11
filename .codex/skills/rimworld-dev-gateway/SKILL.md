---
name: rimworld-dev-gateway
description: Operate, diagnose, and extend this repository's RimWorld Dev Gateway and isolated game launchers. Use for authenticated raw C# execution, exact-PID UI/input/camera/gizmo/debug-action control, screenshots, quicktest/scenario automation, integration/E2E execution, launcher window behavior, game freezes, or missing gateway capabilities during live mod verification.
---

# RimWorld Dev Gateway

Follow root `AGENTS.md` and the repo-local `rimworld-mod-development` skill. Read `docs/Gateway.md`
for the current routes and command syntax. Read
[references/operations.md](references/operations.md) when launching a process, using raw desktop
input, diagnosing a live failure, or retaining live evidence; a route-only code change does not need
that operational reference.

## Preserve the boundary

- Keep the Gateway development-only, authenticated, IPv4-loopback-bound, and unrestricted whenever
  it is loaded. Do not add a restart toggle, allowlist, per-call confirmation, or companion-client
  requirement.
- Keep the raw-text Mono.CSharp REPL directly callable over HTTP. A host client may improve
  ergonomics, but it must never be required to submit source text and receive the result.
- Use pinned EmbedIO/Swan compatible with Unity Mono. Do not add Kestrel/ASP.NET Core, reintroduce a
  hand-written HTTP parser, or assume copying Kestrel assemblies recreates its shared framework.
- Never reference, hint, or package the Gateway from a product mod. Gateway/test assemblies remain
  dynamically staged and absent from product `Assemblies/`.
- Treat unrestricted execution honestly: it can hang, corrupt, or terminate the game. Isolation and
  cooperative cancellation reduce operational risk; they are not a sandbox.

## Choose the narrowest faithful control

Prefer these seams in order:

1. Typed semantic reads/actions for game state, camera, selection, Things, native debug actions,
   gizmos, interactions, screenshots, and test runners.
2. A versioned named automation for repeatable setup or control involving several bounded steps.
3. Exact-PID screen-local mouse/keyboard input for native UI or pointer tools that lack a semantic
   action. This needs an explicitly visible desktop-owning run (`-VisibleWindow`, or the regression
   scenario that requests visibility), may foreground/reacquire focus, and must not be used during an
   unobtrusive minimized run. Never maximize the window merely to make coordinates work.
4. Raw C# for one-off discovery or a diagnostic mutation.

Direct setup may arrange disposable preconditions. It must not manufacture the product outcome used
for gameplay acceptance. Drive and observe the real bill, job, gizmo, designator, ingestion,
construction, trade, or other native player path.

## Launch without disturbing the user

- Refuse to reuse an existing RimWorld process. Use a unique `-savedatafolder`, dedicated log, exact
  package order, PID/start identity, and pre/post normal-config hashes.
- Put Harmony before Core, product/optional mods after Core in their exact declared order, and the
  Gateway last.
- Launch windowed and minimized, with `runInBackground=true` and music volume zero. Do not steal
  focus, maximize the window, or treat the user manually resizing/maximizing it as a failure.
- Bind screenshots and raw input to the exact launched PID and current client bounds. Re-query bounds
  before input; release every pressed key/button in cleanup.
- Request graceful shutdown, then use only an exact-PID bounded fallback. Restore preferences,
  control state, staging, credentials, and only run-owned objects.

## Keep quicktest and scenarios explicit

`-quicktest` creates a playable colony; it must not implicitly run a verification suite, move pawns,
move the camera, spawn fixtures, or flicker through test cases. For a plain quickstart, pass
`-Quicktest` and omit `-Scenario`; the evidence labels that mode `none`, but `none` is not a scenario
file or valid `-Scenario` argument. Every setup mutation must belong to an explicitly requested named
automation/test and its mutation ledger.

Wait for `ProgramState.Playing`, `Game.PlayerHasControl`, a valid player faction, and any required
map lifecycle boundary before destructive reset or native actions. A map object existing during
asynchronous quickstart does not make faction, pause, camera, storyteller, or job paths ready.

For repeatable tests, let the E2E runner own reset and grouping. Remove roofs—including overhead
mountain—before clearing disposable map state, then clear messages, letters, alert readout, windows,
selection, interactions, designations, and zones. A failed reset taints that process; skip later tests.

## Extend the Gateway safely

When a recurring operation is hard to control or observe:

1. Add or amend the Gateway-owned OpenSpec scenario.
2. Add focused host RED tests for request/response, dispatch, cancellation, stale handles, and
   cleanup. Add a live gateway scenario when Unity behavior is involved.
3. Implement a typed route or versioned automation with bounded enumeration and exact capability
   handles. Persist request admission before invoking game code.
4. Marshal all Verse/Unity work to the main thread. Never perform network I/O, blocking waits,
   recursive provider enumeration, or unbounded serialization there.
5. Isolate exceptions from each modded Thing/provider. Return token-safe bounded errors and keep the
   listener/session lifecycle retryable after individual failures.
6. Use the new capability in a real in-game workflow and personally inspect the visible result.

Do not invent small payload/count ceilings and present them as framework limits. Identify whether a
bound comes from EmbedIO/Unity/RimWorld, an observed workload inventory, or repository safety policy.
Measure representative worst cases, leave justified headroom, make large diagnostic payloads pageable,
and record the rationale in OpenSpec/docs with boundary tests. Endpoint-specific bounds may override a
small-DTO default; Def export must remain useful for real modded Defs.

UI state must include current pause/speed state. Screen-local input must use the process client area,
not desktop assumptions. Screenshot APIs should support full-frame evidence and object/cell bounding
boxes; frame related actors first, then select only the single Thing whose inspect pane must be shown.

## Diagnose before relaunching

On a freeze/crash, stop repeating the same startup. Correlate the exact PID, request journal,
dispatcher state, last durable request, native developer console, and flushed `Player.log`. Distinguish
queue timeout, response timeout after start, Unity exception, process exit, and a still-running
uncancellable REPL. Preserve diagnostics before exact-PID cleanup.

Gateway JSON, logs, direct state reads, and `ok` responses are supporting evidence only. Final
acceptance requires a player action and personally observed game behavior on the reviewed build.
