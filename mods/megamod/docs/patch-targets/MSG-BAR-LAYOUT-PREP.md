# MSG-BAR-LAYOUT-PREP: prepared bar state and render consumption

## Ownership and status

- Owner: `fumblesneeze.megamod`; S071 `megamod-message-placement`: current-layout clearance,
  absent bar, first resize/UI-scale/map/group display, unchanged interactions and S001 composition.
- Governing plan revision 17 follows revision 12, which supersedes mandatory recache-address/private-calculator production
  proof. The first timing/argument handoff is implemented in honest Harmony fixtures and remains
  **unregistered**. A bounded selected-entry-call argument participant now has host evidence; full
  native geometry, renderer-consumption admission and adapter integration remain unimplemented.
  No native acceptance.
- Scope: `Source/Features/MessagePlacement/BarBounds/`, owning Unit/Harmony fixtures, coordinated
  message timing bridge and later actual native scenarios. Root owns registration/index/project work.
- Research identity only: Assembly-CSharp 1.7.5000 rev1275 SHA-256
  `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
  No assembly hash, whole-body equality or foreign-owner ban is an admission predicate.

## Consumed native contract

The actual bar renderer and owned clearance must consume the same coherent prepared state in one
native GUI invocation. How a private calculator produced that state is not a consumed contract.

Inspected `RimWorld.ColonistBar.ColonistBarOnGUI():void`, public instance, non-generic:

1. Visibility rejects small logical viewports and active tile picker; the map/world caller adds view,
   screenshot and program-state routing. Layout events do not run the entry loop.
2. It reads `Entries` and `ShowGroupFrames` before the first entry loop.
3. Each entry rectangle uses that receiver's `cachedDrawLocs[i]` and `Size.x/y`; native Size uses
   BaseSize and Scale, whose native getter returns `cachedScale`. The paired entry local supplies
   the corresponding pawn/map/group and native interaction callbacks.
4. The rectangle feeds native click handling and portrait drawing; weapons derive from that
   rectangle. Group frames use current Entries, DrawLocs, Size and Scale through the native drawer.
5. DrawColonist calls GetEntryRectAlpha, which queries Messages.CollidesWithAnyMessage using message
   lastDrawRect. Frame drawing can happen before that query. Late movement would make consumption
   disagree even if the eventual message window looked correctly placed.

These relationships, the actual prepared values and renderer visibility need executable admission.
A compatible provider may retain values, replace calculation, skip an original or assign results
through another address while preserving them. Such behavior is not rejected merely for differing
from the original calculator. A no-op is relevant only when it leaves an unproved/invalid actual
state or a renderer mismatch; requiring every calculator to execute does not establish clearance.

## Proposed barrier and observed state

Select the actual bar path **after Entries/ShowGroupFrames preparation and before the first entry
loop draw/input**. Validate its actual receiver/local entries and the exact native fields consumed
by the effective renderer. This is a proposed narrow target insertion, not an installed patch.

At first root/bar/viewport/admission or after failure, request ordinary native invalidation before
those preparation reads. Current logical dimensions exist after UI.ApplyUIScale. Normal native
map/group invalidation remains in force. Preserve native entry/reorder/label bookkeeping; do not
copy its pawn gathering, force results, invoke collision queries for measurement, draw twice, or
add a UI.ApplyUIScale patch. Unchanged resulting coordinates can still be correct.

The GUI-local snapshot must carry reference identities AND consumed values: root/bar/invocation,
entry/location correspondence and order, pawn/map/group identities, all positions, scale/size,
visibility and the selected native/S001 visible-extension inputs. Finite coherent values are
required. In-place edits with unchanged list identity/count are still changes. A read-only
validation closure cannot call Entries, recache, draw, settings registration or an S001 refresh.

This is not a private cache. Only a dimension/admission identity memo may outlive the invocation;
no per-pawn index or historical rectangles. S001 supplies its current effective presentation by
direct owned call, with its own update timing and validation. Hidden/reduced native portrait parts
must not be unioned back into S001's result. Labels/backgrounds, weapons, selection/mood outlines
and separately visible empty group frames remain required.

## Message timing and failure boundary

The former early commit in MessagePlacementBridge.Complete is superseded. The bridge now seals
message collection and retains its native surface bindings for the explicit later
prepared-bar barrier. Native UIRoot performs game-component/world work after MessagesDoGUI and
before map/world bar rendering; snapshotting only at collection can be stale on that same display.

At the prepared-bar barrier validate the invocation/root/window/settings context, queued native
message bindings and prepared geometry, then perform one whole-stack translation before any
entry draw/input/collision consumer. Preserve newest-first native order and spacing, exact
ImmediateWindow callback identity, windowRect, captured rectangle and lastDrawRect together.
A repeated/early consumer or changed context before commit rejects pending movement.

For proved absent bar contribution, or explicitly unavailable automatic clearance, use
WindowStackOnGUI entry before ExtraOnGUI, immediate-window maintenance, shadows and WindowOnGUI.
Do not use the message callback as the fallback barrier: GUI.Window already consumed its rectangle.
Failure to observe a bar invocation is not proof of absence; resolve current native route/visibility,
otherwise expose Unavailable rather than invented empty bounds. Menu, hidden bar, tile picker,
screenshot filtering, world and map routing need explicit cases.

Snapshot/renderer relationships must remain valid through actual consumption. Guard the relevant
field/getter-to-rectangle and rectangle-to-draw/input relationships, including frame/extension
paths; do not reconstruct private production. Before the first commit, changed input can be
captured at the selected current handoff or rejected. After native effects have occurred, preserve
them, revoke further owned movement and report the invalidation; never rewind native drawing,
clicks or dismissal. This failure is not successful clearance evidence. No whole-GUI transaction
is claimed. Cleanup must discard pending state at the outer invocation boundary on every exit.

## Effective patches and remaining ambiguities

C01 revision 12 is a prerequisite for consumed-operation admission. Harmony 3 inner rewriting runs
after ordinary transpilers, with optional persistence rewriting later. Validate relevant effects
on native field/getter/argument uses and inserted observers using that shared contract; last
ordinary transpiler alone is insufficient. Compatible unrelated effects remain eligible. Unknown
relevant effects disable only the unproved contribution with a bounded reason; no speculative
foreign execution or blanket owner/inner-patch ban.

Concrete remaining proof obligations:

- actual bar receiver/entries local and cache/Size values reach the admitted draw/input operations;
- no unobserved in-place mutation, renewed preparation, receiver/argument substitution or second
  bar render changes that relationship between the selected handoff and consumption;
- native group-frame and label/weapon/outline paths use the measured effective extensions;
- message collection remains unconsumed until commit; absent/unavailable fallback precedes window
  ExtraOnGUI/shadows/GUI.Window, including input passes with existing ImmediateWindow callbacks;
- current view/routing and S001 preparation identify visible content, not merely installed providers.

These require narrowly scoped executable guards/runtime observations and native evidence. They do
not justify reinstating an unconditional private calculator or byref-address proof. If an isolated
address/call check later resolves a demonstrated consumed-state ambiguity, document that reason.

## Implemented first handoff, not complete renderer admission

`MessageBarPreparedPatch`, candidate MSG-BAR-PREPARED, accepts exact supplied synchronous instance
renderer/Entries/ShowGroupFrames methods. It finds one straight-line same-receiver Entries call
stored in a local, followed by one ShowGroupFrames call stored in another local, with no branch
entering that preparation sequence or overwritten entries local. It inserts observation after
the second store. The actual installed native anchors are IL001a/001f and IL0023/0028; offsets are
research evidence only. The predicate is not byte equality and permits unrelated surrounding work.
The getter declaring type may be a base of the renderer type: assignability preserves the same
actual receiver. Exact declaring-type equality is not needed. Static/abstract/generic and return/
argument shape restrictions, plus exact incoming same-receiver/local checks, still apply.

The marker receives the actual body receiver and effective locals, not values captured by a prefix
or reread getters. A later foreign prefix's receiver substitution is therefore observed. Inner
getter-result changes can be compatible: the same effective returned local is handed onward.
This narrow predicate does not yet prove every alternate consumer path or the whole native
field/entry-to-rectangle relationship; the remaining obligations above still gate runtime use.

Each actual transpiler execution revokes publication and emits a unique token. Refresh uses C01
ExpectedVersion/Inspect, the published version, exact owned final ordinary order and FailureFor
over the emitted marker/argument/token operations. The receipt uses the safe slot reader, not
GetPatchInfo. Actual inner skip/argument effects on the marker reject publication; unrelated
effects and supported readonly observations remain eligible. This is a normal synchronous target;
state-machine attributes are rejected. No callback/attribute constructor/factory is invoked as an
inspection probe. The reporting helper's readonly-observer rule is applied to this ordinary
handoff only, not to cleanup-sensitive Exit/finally guarantees. Arbitrary foreign global effects
or exceptions are not sandboxed. Callback failure preserves native behavior and faults the route.

An enabled marker reuses successful publication while the immutable incoming generation is still
the published identity. It does not clone/deserialize/sort provider metadata on unchanged renders.
Every transpiler execution still revokes that identity. An unadmitted generation retries refresh;
busy or not-yet-published metadata never grants it the prior generation's authority. Actual geometry,
root/context, embedded token and invocation checks remain per invocation. A bounded real-Harmony
fixture holds composition of an unrelated target while renders observe changed current values;
after relevant repatch it proves refusal under contention and recovery after release. This is a
behavior/inspection-work check, not a benchmark or a claim about the older observers' hot paths.

`MessagePlacementBridge.CommitPrepared(root,bounds,barrierIsCurrent)` rechecks its pending context,
draw/queue generations, all native bindings and both supplied validation predicates at the final
authorization boundary. An early failed handoff invalidates pending movement even if no native
collision query occurs. With no pending collection yet, bar activity is still sent to the active
MessageGuiEvent envelope, preventing later collection from moving already-consumed geometry.
The same notification outside an envelope is harmless; it never leaks into the next invocation
or rewinds completed effects. Outer-envelope cleanup discards pending references on all exits. Runtime
has removed the provisional early captureBar constructor argument and bar value from its frame;
no production caller used that argument. Its actual prepared provider remains absent. The reviewed
MSG-WINDOW-STACK fallback now supports only the exact Entry/no-Game context; central registration
and native acceptance remain pending.

Existing S071 draw/registration effect admission and outer receipt readers have their separately
reviewed C01 checks. This marker's publication check does not replace those boundaries or prove
native geometry.

### Revision 17 selected entry-call participant — host implemented, review pending

`MessageBarEntryConsumptionPatch` is an optional description consumed by the existing prepared
transpiler, not a second installed patch. It identifies the rectangle value type and click/draw
consumer methods. Each method must be public, instance, nongeneric/nonabstract, return void and
accept that rectangle value as its first parameter. One call to each must occur after the inserted
handoff in emitted instruction order; absent/ambiguous/earlier calls withhold only this participant.
The owner retains one actual incoming stream, expected version, body token, immutable generation,
publication and rollback receipt. It reuses the same C01 snapshot for timing and selected-call effects.

Timing `Available` and the existing callback remain handoff-only. `EntryArgumentsAvailable`,
`EntryArgumentsFailure` and `CaptureEntryArgumentsAuthority` separately describe whether published
inner effects can rewrite or skip the selected call operations. Relevant rectangle-argument changes
withhold that authority while native calls retain their actual values/effects and timing stays
available. Unrelated argument/result effects remain eligible. New composition revokes old receipts;
removing a rejected rewrite and successfully republishing permits only a fresh receipt. Participant
description failures do not obstruct native recomposition or disable a valid handoff.

This authority does **not** prove field/getter-to-rectangle production, that both arguments came
from the same prepared local, actual runtime control-flow/order, visible extension coverage or GUI
invocation/value stability. It must never be substituted for full `MessageBarBoundsReceipt`
validation or used alone to register automatic clearance. No new geometry capture, runtime callback
or renderer replay is implemented. These remaining relationships still need their own bounded work.

The first RED produced a real foreign inner rectangle rewrite and failed because entry authority
stayed true although native click handling consumed shifted coordinates. Minimum GREEN retains
the rewritten native effects while refusing entry authority. Five focused participant cases cover
both consumers and removal/repatch recovery, unrelated result rewriting with unchanged geometry,
and missing/unreadable participant descriptions. Existing handoff-only and bridge behavior also pass:
RED1/GREEN1, then affected GREEN39, none skipped. The fixture's actual IL contains a struct local
address followed by its constructor call, rather than requiring `newobj`. Exact source snapshots,
TRX/log paths, provider identity and before/delta/hash evidence are retained under
`artifacts/Megamod/P0-B/messages-entry-arguments-revision17/`. Independent review and native geometry
acceptance remain pending; no runtime/project/central registration was changed.

## Existing implementation disposition

### 2026-11-11 manual-positioning consumer barrier

The native runtime now supplies four exact native drawer methods as Consumers on the existing
MessageBarPreparedTargets: HandleClicks, DrawColonist, DrawGroupFrame and HandleGroupFrameClicks.
The single existing transpiler validates each occurs once. A graph walk from method entry with the
owned handoff removed must not reach any selected consumer. This proves dominance rather than
instruction order and accepts ordinary loop/switch flow. Branch destinations must resolve uniquely;
exception-region control flow is not currently admitted for this selected native method. Failure
preserves the whole incoming stream. The emitted handoff and consumer operations share the existing
C01 publication snapshot, generation revocation and owned rollback.

This authorizes the existing manual movement barrier only. It does not certify field-to-rectangle
geometry, visible extensions or full automatic clearance. A genuine bypass fixture initially
admitted unsafe order; the corrected batch rejects it while native consumption continues. A loop
counterexample retains values and one handoff. No second bar patch or provider replay is added.

`MessageBarLayoutPreparation` has 23 reviewed pure cases for dimension identity, callbacks and
revocation. Retain the useful memo/reentrancy policy, adapting or replacing its synchronous API to
fit the actual native handoff. Its receipt alone never proves current renderer consumption.

`MessageBarEntriesGuard` (MSG-BAR-ENTRIES, 17 cases) and `MessageBarExecutionPatch` /
`MessageBarExecutionReceipt` (MSG-BAR-EXECUTION, 13 cases, P1 receiver fix independently clear)
were unregistered historical experiments, removed with their standalone fixtures on2026-11-08.
Root's source/dependency search found no selected consumer; the exact removed files and hashes
remain under `artifacts/Megamod/P0-B/retired-private-production-20261108/`.
Their exact original call/nesting,
run-original and calculator-witness requirements are **not selected admission prerequisites**.
Their ordinary-patch reviews do not establish
Harmony 3 inner compatibility, actual prepared geometry or native behavior. No private-production
or out-field-address implementation follows merely to complete those old receipts.

Retain the Visible/Absent/Unavailable `MessageBarBoundsReceipt` concept, existing exact message
bindings, whole-stack calculation/rollback and outer invocation/early-consumer policy. Their
composition must split collection completion from later commit/cleanup. Tests that mandate early
bar capture or immediate MessagesDoGUI commit are superseded; meaningful failure/identity/native
interaction assertions remain required. The removed tests describe only the retired experiments;
current prepared-handoff, bridge and event behavior tests remain.

## Focused verification and native gate

The prepared observer revokes its old publication before inspecting a new composition. If provider
metadata cannot be read, its transpiler returns the incoming stream unchanged, with no owned handoff.
This must not prevent foreign patch removal. A later successful provider rebuild may publish a fresh
handoff; the old receipt never regains authority. The exact oversized-metadata regression records
actual removal failure on the prior implementation, followed by native-shaped consumption without
an owned callback and recovery on the corrected implementation. Focused RED1/GREEN1 and affected
prepared/bridge GREEN34 are retained under `artifacts/Megamod/P0-B/messages-prepared-metadata-fix/`.
These host checks do not accept native layout or gameplay.

The earlier timing RED used an intervening native-shaped GUI callback changing positions in place
after message collection but before the prepared-bar barrier. Assert no early movement, current
values at the barrier, one translation and aligned earliest bar collision/message-window geometry.
Use a pure timing seam first and explicit owned Harmony targets for actual call/argument boundaries.

Follow with retained unchanged geometry and compatible replacement preparation; list/value/context
mutation before and after handoff; absent-bar window fallback; unknown routing; early consumption;
repatch; same-instance resize/UI-scale/map/group changes; empty framed groups and effective S001.
Do not require numeric differences or original calculator execution in those cases.

The orchestrator must exercise the first changed display through native viewport/UI-scale/view/
group/settings actions, produce native messages and personally inspect label/weapon/frame/S001
clearance plus hover, click/target/quest and dismissal. Host fixtures, authored scenarios and
retained old reviews do not accept any feature. Manual fallback does not complete S071.
