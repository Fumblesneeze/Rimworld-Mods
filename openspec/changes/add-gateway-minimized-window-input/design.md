## Context

Owner: repository tooling RimWorld Dev Gateway (`mods/RimWorldDevGateway`). P0 megamod uses ordinary
native settings, policy and log controls. Existing `ModSettingsActionStep` can open a page in play;
generic MayMaximizeWindow input requires a visible desktop-owning run. Neither direct Store writes
nor a window-opening screenshot proves checkbox/text behavior.

Read-only 1.7.5000 research is retained under
`artifacts/Implementation/megamod-p0/native-1.7.5000/` for `RimWorldWaylith.GlfwInputBridge`,
`RimWorldWaylith.Imgui.ImguiDispatch`, `UnityEngine.InputSystem.InputSystem`, `Waylith.Input.InputState`,
`UnityEngine.Event` and `UnityStubs.Gui.IGuiBackend`. The real GLFW bridge calls public
`InputSystem.QueueMouseMove/QueueMouseButton/QueueMouseScroll/QueueKeyChange/QueueTextInput`.
The actual IMGUI dispatcher performs layout/input passes and reads frame-held modifiers. This is
source evidence of a candidate seam, not proof that queued events survive the minimized frame loop.

## Goals / Non-Goals

Goals: exact-dialog native input for real controls; no desktop disturbance; shared typed automation
and E2E operation; faithful native focus/disabled-state/validation; cleanup and explicit partial results.
Non-goals: a product testing hook, a general widget accessibility tree, bypassing modal hit tests,
background OS input, supporting uninspected 1.6 input internals, or accepting gameplay from an API result.

## Decisions

- Reuse the engine input queue used by its window bridge, capability-detected by the full consumed
  member shapes. Do not create a second GUI dispatcher or temporarily replace product outcomes.
  Exact game hashes identify research/builds, not sole runtime admission predicates.
- Expose a named versioned operation through the existing Gateway mutation/automation and E2E
  surfaces. Inspect the current run/step lifecycle before selecting asynchronous plumbing; reuse
  it rather than introducing a parallel job queue. A queued request is not a completed gesture.
- Existing UI-state window labels encode only index/type and can be reused by a replacement; they
  are not exact-instance handles. Retain the actual Window reference with a session token, native ID,
  stack identity, bounds, scale and rendered pixel dimensions. Validate native GetWindowAt/GetsInput
  before each phase; initially reject immediate windows. Never reinterpret a stale screen index.
- Distinguish framebuffer pixels from GUI units and OS client pixels. The inspected bridge feeds
  bottom-left native input while IMGUI uses DisplayHeight to flip it back; UI scale acts through the
  GUI matrix. Convert once against the captured rendered extent and check that extent stays valid
  while minimized. Do not assume OS client pixels equal framebuffer pixels.
- Start with one real click consumer and vertical host RED/GREEN. Add key/text and scroll phases as
  required by the native settings scenario. Preserve frame separation where native held modifiers,
  focus or key transitions require it. Main-thread calls enqueue bounded work; never wait there for
  future frames. Exceptions and cancellation release owned presses even after the dialog disappears.
- Inspection shows Queue* mutates controls immediately, rather than retaining a deferred event list.
  The next InputSystem.BeginFrame clears transient press/release flags. Gateway Update runs after
  ImguiDispatch.DispatchInput, so ordinary dispatcher timing would lose those edges. Use a narrowly
  shape-guarded pre-dispatch hook for pending phases and a post-dispatch observation of consumption;
  that observation does not prove a widget outcome. The native loop dispatches input before skipping
  minimized rendering with RunInBackground enabled; verify this live before acceptance.
- DispatchKeyboard processes characters before editing shortcuts and snapshots held modifiers once.
  Separate focus click, held Ctrl+A shortcut, release, then text across dispatches. Probe raw
  Keyboard/Mouse controls for incompatible held inputs, not Input.GetKey which masks text-captured
  keys. Verify text focus and the native char subscription before sending characters.
- Do not call ResetAllInput as ordinary cleanup: it would clear unrelated input. Capture prior pointer
  state, require neutral incompatible buttons/modifiers, track injected ownership, restore temporary
  state through the same engine path. Do not erase actual changed settings or closed windows.
- Bound the first text gesture to1024UTF-16code units and one gesture at a time as repository safety
  policy, not an engine limit. This exceeds the immediate numeric/search fields while limiting one
  request's dispatch work. Partition text into frame-bounded phases; reject invalid surrogate input.
  Expand only with a measured consumer and updated tests/specification if needed.
- Extend the existing native settings navigation readiness check to settled entry scene or settled
  player-controlled game. Keep other action types' player-control checks unchanged. The E2E runner's
  current loaded-map lifecycle may need a separately scoped menu workflow; do not fake Current.Game.

Host policy/queue-state tests belong in an existing honest Gateway environment, preferably the current
1.7 `RimWorldDevGateway.Waylith.Unit` where applicable. Do not initialize a real mod or representative
Def database in host tests. Actual UI event delivery belongs in an isolated current1.7 native run.

## Risks / Trade-offs

- Engine resets queued input before dispatch → inspect InputRuntime/frame order and prove a real click
  on a stock native control before building more consumers; revise the adapter from that evidence.
- Click and release coalescing or lost modifiers → test phased scheduling, focus and cleanup through
  actual native text editing, including cancellation and a window-changing callback.
- Window scaling/clipping drift → bind current dimensions and verify at distinct supported UI scales;
  reject mismatched context instead of clicking elsewhere.
- Native input dispatcher swallows callback exceptions → retain actual dispatched state and logs;
  a request result cannot promise a control outcome without observation.
- An injection or cleanup callback can fail after changing input → record uncertain dispatch and
  attempt both owned button release and pointer restoration. A failed cleanup quarantines the native
  input lane instead of admitting another gesture or retrying every frame. Retain partial state and
  the cleanup failure; controlled recovery or exact-process teardown is needed before reuse.
- Main-menu tests outlive a game reset → inspect Gateway runner lifetime, retain exact process ownership,
  and use a supported menu transition without replacing global game state through diagnostic mutation.

## Delivery and remaining research

This change is authorized supporting tooling for the active implementation goal. Register and test
the exact operation, then independently review and personally verify it in a fresh minimized native
run. Product part acceptance stays separate. Removing the capability leaves existing desktop routes
unchanged; do not introduce product dependencies or alter the user's normal mod configuration.
Open implementation questions: exact input-queue consumption point; GUI scale mapping; asynchronous
operation reuse; safe neutral-input probe; runner lifetime for menu transitions. Resolve by source
inspection and bounded development runs, not by asking the user to explain engine behavior.

Read-only followup resolved the source-level phase/identity questions above. Gateway's existing
automation Execute handler currently marks a run complete synchronously; it cannot return a pending
gesture as success or wait on the game thread. Extend its retained run lifecycle with frame-stepped
completion/cancellation and expose that lifecycle through typed routes, or use an existing equivalent
asynchronous operation after inspection. E2E already polls IGatewayEndToEndStepOperation. Register
cleanup immediately because timeouts can abandon polling. The runtime host survives scenes, but
existing map isolation does not; a menu workflow must explicitly preserve/restore its native loaded
game or use an owned menu mode. These integration and live-proof obligations remain open.

The first deferred lifecycle implementation extends the existing automation registry: a handler can
return IGatewayDeferredAutomationOperation; Advance polls each original-thread operation once and
retains pending state until cleanup reports completion. Cancellation/deadline intent stays latched,
partial results survive, and no other thread's operation runs on the caller. Existing history capacity
also bounds active admission; terminal entries may be evicted around pending ones. There is no second
public queue. This host-tested/reviewed lifecycle still needs runtime Advance and typed status/cancel
wiring. Native hooks must check cancellation before injection as well as at the later Update poll.

Additional inspected provider facts: dispatcher and ImGuiHost live in RimWorld.dll, not
Assembly-CSharp.dll. ImGuiHost exposes a public float DisplayHeight and no corresponding width
property. Compare it with captured rendered Screen dimensions before injecting; do not infer a
valid minimized extent. Ordinary nonnegative Window.ID, exact object/stack, current geometry, native
GetWindowAt and GetsInput are distinct checks. GetWindowAt consumes GUI units. These facts and the
input-port phase RED/GREEN receipts are retained under artifacts/Implementation/gateway-native-window-input/;
neither the host state machine nor source inspection proves actual minimized event delivery.

The inspected `WindowStack.Add` assigns `uniqueWindowID++` from a zero-initialized static counter;
the first ordinary window therefore has ID zero. Immediate windows use negative IDs and are also
excluded by their runtime type. Capture and per-phase validation share that eligibility rule; zero
must not be treated as an uninitialized ID. Exact reference/stack identity, current native ID and
the opaque session handle remain required independently of this numeric eligibility check.

### Post-layout mouse-event guard

Source inspection found a gap not exercised by the successful basic-checkbox run: `RunInputPass`
calls `RunPass(layout)` before `RunPass(mouseDown/mouseUp/mouseDrag)`. During layout,
`WindowStack.WindowStackOnGUI` calls `ExtraOnGUI` and then recopies the current window list. A callback
can replace a captured window at the same rectangle, and the next native mouse event can then route
to the replacement through `GUI.DispatchInputToWindows`. Checking immediately after QueueMouseButton
occurs before this layout; observing the outer dispatcher afterward is too late to prevent delivery.
This is a source-confirmed safety gap, not evidence of a historical wrong click.

The additional target predicate is the declared private nongeneric instance void
`ImguiDispatch.Dispatch(Vector2,bool)` event boundary. The inspected caller creates Event.current
after the layout pass, then this method begins native window/game event delivery. Resolve the exact
declaring type, accessibility, parameters, static/generic state and return shape. Install an owning
prefix/postfix on this boundary and remove those exact methods during shutdown (thirteen owned bindings
in total). Restrict the gate to the current outer dispatch frame, exact active operation and dispatcher,
and owned left-mouse event; leave layout, repaint and unrelated native events unchanged. Revalidate
the captured instance/stack/geometry/hit target, event type, rendered point, edge ownership and
cancellation immediately before native delivery. Nested outer dispatch while this gesture is active
is rejected because it could replay the same raw edges.

Record event delivery only when its own original executes. The outer DispatchInput returning normally
cannot certify a blocked or unobserved mouse pass. Feed this receipt to the existing click core, which
reports native_input_not_dispatched and performs owned release/restoration after rejection. No second
queue, gameplay setter or rollback is added. Pure gate/core tests establish receipt and cleanup policy;
the native scenario `gateway.native-window-layout-replacement` must demonstrate replacement during
both press and release layout, untouched replacement controls, and a subsequent independent click.
The new native seam and callback timing require fresh reviewed live evidence.

The caller's release tail also requires a guard: `DispatchInput` clears `GUIUtility.hotControl`
after RunInputPass(mouseUp), even when the inner event was suppressed. During ordinary Harmony
rebuild, locate its unique static void(int) hotControl setter, the zero argument, preceding
RunInputPass(mouseUp, button, point, modifiers) call, matching button reload and zero-button branch
to the instruction after the setter. Replace only that setter call with an equal-stack-effect
Gateway guard; retain labels and exception markers. Leave ordinary releases unchanged. For an
owned release without observed delivery, skip this reset so the caller cannot clear replacement
ownership. Do not throw an artificial exception through the native dispatcher's catch/log path.

Normal rebuild observers on both mouse targets invalidate earlier publication receipts synchronously.
As for the reset contract, wait for new immutable shared metadata, read under Harmony's dictionary
lock, and use only the inspected metadata-only native sorter. Require both observers terminal among
transpilers and the event guard terminal among prefixes; reject factories and downstream infixes.
Compatible earlier prefixes/transpilers remain permitted when the required tail snippet is retained.
Check authority before queueing and again at event admission. A later priority tie or explicit
HarmonyAfter prefix cannot inherit the old admission. An active-gesture rebuild aborts further input.
For the exact active dispatch frame, check current authority and provider availability before trusting
even absent or unowned capture state: a rebuild during layout can move a new prefix ahead of capture
and change its classification. Reject that invocation and retain a failed delivery receipt for owned
cleanup. Invocations outside the active frame and proven unowned passes keep their native routing.

Event ownership must not be inferred again from fields that another prefix can change. A separate
published-first prefix captures the exact per-invocation operation/dispatcher/event reference and
button/type/raw-type/point/drawing values in Harmony state. The published-last guard uses this immutable
ownership record; changing button, drawing or Event.current cannot turn the owned mouse pass into an
unrelated pass that bypasses validation. Compatible prefixes may run between capture and guard.
Normal layout, repaint and unowned input retain their normal routing.

The EventDispatch body itself has a stricter, specifically developer-tool safety limit: its incoming
effective instruction stream must preserve the original delivery body. Compare operation/member/literal
identity, branch destinations, locals/arguments and exception regions, normalizing labels, short forms
and inert nops. Earlier no-op stream transformations remain supported; unproven body changes disable this
native-input capability rather than declaring the game or the other mod incompatible. This is not the
megamod product patch policy or a general foreign-owner ban, and no assembly/build hash pins admission.
The outer release-tail observer still uses its local semantic snippet predicate.

Read the original event body with an explicitly constructed MethodCopier and its empty transpiler list,
guarding the consumed constructor/Finalize signatures. In the inspected Harmony 3 implementation,
PatchProcessor.GetOriginalInstructions resolves installed factories even when zero transpilers are
requested, so it is not an allowed inspection shortcut. Do not use GetCurrentInstructions either.
Published metadata must place the entry capture first and final guard last among prefixes, and the
stream observer last among transpilers; unsupported downstream infixes/factories remain rejected.

Cleanup recognizes a preserved foreign hot control only for a rejected owned release when the captured
window is gone and the control is mapped to exactly one different current ordinary window in the same
stack. It leaves that control alone; unproven ownership remains an explicit cleanup failure. Existing
owned button-latch checks still apply. The release-layout fixture establishes a native named text
control and its capture during replacement layout as an adversarial precondition, then observes that
the same control ID remains active after rejection. Only after the preservation assertion/capture does
the fixture release its own control and test independent click recovery. This does not manufacture
the preservation result or bypass product UI behavior.

Retained automation runs use authenticated GET /automation-runs/{id} and POST
/automation-runs/{id}/cancel. The repository exposes gateway_automation_status and
gateway_automation_cancel with separate game-run and automation-run identities; the companion uses
automations status/cancel ID. Both ID boundaries accept one ASCII literal segment of at most200
characters, a repository request bound exceeding generated GUID identities. Cancellation returns
only intent and the run identity, avoiding the result's size/depth in this acknowledgement; only a
later terminal status receipt establishes cleanup completion. Status retains the start route's same
64MiB/16-depth/100000-node response budget so any admitted result remains inspectable. A generic
automation admitted by HTTP carries its own deadline equal to the existing response budget
(normally15 seconds), independent of the completed request's linked token lifetime. Synchronous
handlers retain cooperative request cancellation. Disposing a successful initial request does not
itself cancel pending work; explicit cancellation or the retained deadline does. Test clocks prove
deadline ownership; none of these timings assert gameplay generation performance.

### Effective raw-input reset boundary

The fresh minimized diagnostic run `20261107T154254754Z-b95d97b157fd4e17b604ee0869ce9796`
observed the native dispatcher but no invocation of the tiny `InputSystem.BeginFrame` wrapper hook.
Inlining into an already compiled caller is a plausible cause, not a proven JIT diagnosis. The
provider correctly remained unavailable; metadata and a registered patch were not reset evidence.

Use `RimWorldWaylith.Bootstrap.OnPrePollEvents(Waylith.EngineApplication)` as the effective reset
observation boundary. The inspected private static void callback is registered on the engine's
pre-poll event, before GLFW processes fresh input. It calls the input reset and then optionally
`WindowsImeBridge.BeginFrame`; the latter performs IME housekeeping rather than queueing raw input
in the inspected build. Exact declaring types, static/accessibility/generic/parameter/return shapes
are required. An owning last-priority stream observer validates the incoming instructions during
ordinary Harmony rebuilding: exactly one `InputSystem.BeginFrame` call must dominate every normal
return and the optional IME phase, without a loop through the reset. Unrelated instructions and
branches remain supported; unsupported exception-flow shapes fail closed. Do not execute foreign
transpilers or patch factories merely to inspect a candidate stream.

Last priority alone does not prove that this observer is terminal: explicit ordering constraints
and later priority ties can place another transpiler after it. Each observer invocation synchronously
invalidates the old reset admission and edge ownership before examining its incoming stream. It
records the preceding shared metadata publication and waits for a new publication after Harmony
has constructed and installed the replacement. Read the shared dictionary under Harmony's own
dictionary lock; use the exact immutable serialized byte-array identity as the publication receipt.
Deserialize that captured snapshot and require the observer to be the unique final transpiler in
the inspected Harmony 3 metadata-only `PatchSorter.Sort()` order, with the Gateway owner. That
provider subsequently applies `MethodCreator.AddInfixes` after all transpilers; inner hooks can
therefore skip or alter the reset call without entering the wrapper or changing outer `__runOriginal`.
Reject any published boundary `innerprefixes`, `innerpostfixes` or `innerfinalizers` until their
downstream transformation is separately proven. Require the exact public instance `Patch[]` field
shapes for those metadata arrays. This does not prohibit ordinary compatible prefixes, postfixes,
finalizers or earlier transpilers. Reject declared patch factories without invoking them.
Never use `GetSortedPatchMethods`, `Patch.GetMethod`
or a foreign-transpiler inspection run for this proof. A failed rebuild, absent/new/unproven
publication, later transpiler, unsupported stream, or skipped boundary cannot retain old admission.
Cache the order proof only for that publication; a new observation is required for another one.
Runtime resolution checks the consumed Harmony state field, sorter, deserializer and patch metadata
callable shapes. These internals are restricted to the 1.7 provider; the 1.6 path stays absent-safe
and reports unsupported. The metadata-only behavior is grounded in inspected provider source;
pure host tests do not initialize Harmony or establish native patch execution.

Only the proven caller's completed original plus cleared raw button/key edges and scroll advance
the owned-input generation. Keep the existing
wrapper hooks as skip/exception guards when that wrapper is actually invoked, without advancing a
second generation. A skipped caller, observed skipped wrapper, throwing reset, unsupported incoming
stream, or failed raw-state postcondition disables admission and preserves explicit cleanup/error
reporting. Never set observation state manually to bypass an unavailable provider. Shutdown removes
each exact owned hook on both targets. Source/host checks support this change; a fresh reviewed
native run still has to prove that the selected callback actually executes and drives a control.

Native E2E capture/click steps use the same registered automations as MCP/CLI. A narrow driver
decorator delegates all other steps unchanged. Capture returns exact opaque identity and GUI/pixel
geometry to the scenario; click retains its request/run receipt and remains pending until the
registry reports terminal cleanup. Capture retains the bounded descriptor inventory and request/run
identity even when the scenario rejects the observed inventory, so an absent-window assertion does
not erase its own diagnostic evidence. The E2E context registers cancellation cleanup before an input
run begins, polls it across frames, and refuses reuse when cleanup cannot be proved. No scenario
setter or separately implemented input queue substitutes for this path. The operation uses the
same15-second bounded deadline as the ordinary automation entry point; the surrounding test still
has its own frame/tick/wall-clock budget. Host receipts establish wiring only; actual native
checkbox captures and context-change cases remain required on the reviewed build.

### Pointer-sensitive popup reproduction and proposed workflow lifetime

A product settings run (`20261107T185106527Z-39148f2aff064cf186b0be82b3760021`) dispatched both
mouse phases at the visible selector button but found no FloatMenu in the next assertion. It retained
neither the pre-click pointer nor menu creation/closure, so it cannot establish whether the button
failed to activate or a successfully created popup subsequently closed. Source inspection found a
specific lifetime mismatch: the successful click calls RestorePointer immediately after release;
ordinary FloatMenu defaults vanishIfMouseDistant=true and closes/cancels once the pointer is more
than 95 GUI units beyond its bounds expanded by 5. Scroll-view transforms map that run's click into
the visible button; no coordinate or product-menu override is justified by the retained evidence.

The Gateway-only `gateway.native-window-float-menu` regression uses a real Widgets.ButtonText
inside an equivalent scroll view to create an ordinary FloatMenu from its actual button callback.
Passive creation/closure callbacks retain exact identity, frames and pointer positions; a checkpoint
and screenshot precede terminal assertions. Button activation and menu persistence are separate
checks. The old pointer must lie outside the native distance threshold to reproduce this case;
the scenario records and verifies that precondition without moving the pointer or changing popup
behavior. Only its own windows are cleaned up. Host tests cannot establish this lifecycle.

Native reproduction `20261107T190444973Z-b2380515117242448fdb7da433120f3f` confirmed the mechanism:
the real button activated once, opened menu ID1 at frame979 with pointer (577,252), then the menu
closed in the same frame's layout with restored pointer (51,560), no selection, and distance
578.9309. Root personally inspected both causal captures. The correction is an explicit opaque workflow pointer scope
on the existing input lane. Preserve standalone version 1 immediate restoration. A scoped successful
click may transfer pointer ownership to the workflow after button/GUI cleanup; failure or cancellation
restores that gesture's previous point. The E2E context owns final restoration to the original point.
Typed begin/end and scoped-click contracts must distinguish gesture cleanup/ownership transfer from
workflow restoration, reject foreign pointer ownership, and close honestly on expiry/shutdown.
Use the explicit workflow deadline, not an unrelated short timeout. Do not add a parallel queue,
arbitrary delay, FloatMenu-specific exception, or product behavior change. Native RED was retained
before implementation; reviewed native GREEN remains required.

The shared E2E begin step derives its absolute deadline from the already-running scenario's actual
StartedUtc plus MaxWallClockSeconds. It registers scope restoration before passing the handle to a
scenario callback. The automation retains a native-pointer-scope artifact (scopeHandle/deadlineUtc)
before a post-admission cancellation can hide the result. Deferred scenario cleanup recovers that
exact handle even if receive throws, begin is cancelled late, or the scenario times out. A successful
gesture receipt never substitutes for end-scope pointerRestored evidence. The provider owns at most
one scope plus its bounded last terminal receipt; no scope allows a parallel gesture lane.

Automation names are native-pointer-scope.begin/end (version1), with deadlineUtc as an ISO round-trip
timestamp and an opaque scopeHandle. Native-window.click version2 requires pointerScopeHandle;
version1 keeps its old argument and restoration contract. A retained click receipt adds
pointerScopeHandle, pointerScopeState=retained and pointerRestored=false, while cleanupComplete
means the gesture's button/GUI cleanup and pointer transfer completed. Failed/cancelled gesture
cleanup reports gesture-restored only after readback. End reports scopeHandle, deadlineUtc,
pointerScopeState=closed, cleanupComplete and pointerRestored; failed ownership/restoration reports
cleanup-incomplete. End remains callable when new input admission is unavailable. Expiry requests
stop on any owned pending gesture, then restores the workflow pointer after that gesture's cleanup.
Main-thread Update and native dispatch tick this same provider lifetime; Advance only polls receipts.
Shutdown retains both gesture and scope cleanup evidence. The E2E driver retains a proved terminal
end receipt so later deferred cleanup does not re-open or re-end that old scope after another workflow.

Admission is not a reservation of the physical pointer: immediately before the deferred first press,
read its actual position again after target/neutral-input validation and at the injection entry before
claiming or queueing input. Separate lifetime checks from pointer-position ownership; comparing the
scope's expected position to itself is not an ownership check. Foreign movement rejects the gesture
without overwriting or restoring that new position. Scope restoration is also reentry-guarded because
QueueMouseMove invokes synchronous callbacks before updating its position. Recursive cleanup observes
a pending result until the outer restoration finishes readback and publishes one terminal receipt.
Shutdown has a different obligation: any still-owned nonterminal scope supplies an explicit
native_input_shutdown_incomplete snapshot, even during expiry restoration with no pending gesture
or end automation. Application.Quit can invoke quitting synchronously from a pointer-event callback;
null would incorrectly mean no owned cleanup. This snapshot does not publish or replace the scope's
terminal end result. Nested end/poll stays pending, while shutdown retains ownership until a later
attempt observes the outer restoration's authoritative receipt.
The E2E runner retains completed step evidence providers until test cleanup, so a scope begun in an
earlier successful step still records its end-run identity/restoration receipt if a later step fails.
This needs no new public protocol or global history of pointer scopes.

### Native developer-log inspection

The inspected 1.7.5000 `DebugWindowsOpener.DrawButtons` and keyboard path both invoke its private
instance void `ToggleLogWindow()`. That command removes the ordinary `LudeonTK.EditWindow_Log`
when present, otherwise adds it through WindowStack. `UIRoot` owns the live `debugWindowOpener`.
Use that same command through a callable-shape-checked narrow adapter; do not call the auto-open
error path, set its pending flag, clear diagnostics or manufacture an error. Availability is
explicitly unverified on 1.6. All native state reads and invocation require the game thread.

Register `native-log.set-open` in the existing automation registry with version1 and a Boolean open
state. It requires finalized play data, an idle long-event queue, the live UI/stack and developer
mode already enabled. Match the command's exact log-window runtime type and reject duplicate or
derived-window ambiguity. Compare the requested state before invocation,
invoke once only when needed, then require the actual requested state. Retain before/open/changed
receipt and request identity; a callback that fails to open the log cannot report success. An
unsupported command, native exception or changed context does not trigger another toggle.
Before-state, attempted invocation and observed after-state are separate retained artifacts before
postcondition/cancellation completion checks; absent after-observation stays unknown after a throw.
`NativeDeveloperLogActionStep` uses that same registry path and includes those artifacts even when
the operation has no successful result. Scenarios capture the visible log and
restore their previous state through native actions; host tests only establish policy/wiring.

### Native bottom-tab navigation prerequisite

S029 requires native Game construction to exercise presets, followed by Assign → Manage drug
policies to inspect and edit the resulting policy. The dialog's New/Duplicate actions do not call
the preset factory. Existing native window clicks can operate the Assign header, but no typed
Gateway action currently opens that tab. Extend this owning change with one bounded action rather
than opening a policy dialog directly or creating a script that sets the active tab.

Use version1 automation native-main-tab.set-open, typed gateway_main_tab projection and
NativeMainTabActionStep through the existing automation/E2E driver. Inputs are exact defName and
Boolean open. Resolve afresh on the Unity thread from the current UIRoot_Play.mainButtonsRoot's
actual allButtonsInOrder list; its snapshot can differ from DefDatabase after content changes.
Shape-check that narrow list/member and support native toggle-tab workers only for the initial
slice. Require current game/player control, settled long events, visible game UI, a live UI/stack,
WindowStack.GetsInput(null), and the selected worker's Visible/Disabled predicates. The latter
conditions normally guard DoButton but are not all enforced by InterfaceTryActivate itself.

Compare actual target OpenTab identity before invocation. A satisfied request performs no toggle;
a close request cannot close an unrelated open tab. When needed invoke the existing worker command
once, observe the current target state/context and report truthful before/attempted/after evidence.
Missing/ambiguous/unsupported targets, changed context, callback failure or a failed postcondition
never trigger a second toggle or direct WindowStack/SetCurrentTab fallback. Reuse existing request
cancellation and main-thread dispatch, with no new input lane, HTTP router or background queue.
Manage policies remains an ordinary captured-window click; no product reference enters Gateway.

Exact1.7 source references and proposed leases are retained in the P0-C native UI research. Native
button semantics and the minimized action/result still require independent review and live evidence;
host tests must not fabricate an active mod or a populated game Def database.
