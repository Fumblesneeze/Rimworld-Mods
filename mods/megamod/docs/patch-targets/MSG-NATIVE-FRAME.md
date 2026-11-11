# MSG-NATIVE-FRAME: one native message GUI pass

Owner `fumblesneeze.megamod`; S071. Scope, window-identity, registration-dataflow, consumer-veto and
outer-invocation timing helpers are implemented and host-tested, but unregistered. Native composition
and the translation adapter are being integrated; actual native resolver/context execution, complete
bar/UI integration and player-workflow verification remain open. This document does not authorize
central admission of the incomplete group.

## Consumed scope

`Verse.Messages.MessagesDoGUI():void`, public static, non-generic and parameterless. Prefix/finalizer
scope only: begin collecting actual admitted `Message.Draw` registrations, then finish/clear that
pass even when native or foreign code throws. No argument changes, loop replacement, fixed spacing,
message list mutation, native replay or speculative CalculateRect call.

Method existence/full callable shape plus Harmony's actual `__runOriginal` execution result govern
this scope itself. A skipped original cannot authorize a commit, even when a foreign prefix or
postfix performs admitted message draws; completion still clears that nonempty pass. An empty skipped
pass is likewise unsuccessful and harmless. No old window is guessed or translated. Each
actual registration is independently protected by MSG-NATIVE-DRAW. A nested pass invalidates both
passes rather than applying overlapping translations. Native exceptions must propagate unchanged;
owned observation/write failures disable only placement with a bounded diagnostic and restore any
attempted mutations. No process-global transient can survive the finalizer.

Implementation: `Source/Features/MessagePlacement/Harmony/MessageGuiScopePatch.cs`, candidate
`MSG-NATIVE-FRAME`, common early admission. Its two acquired OwnedHarmonyPatch receipts are rolled
back independently; only this owner's prefix/finalizer slots are acquired. Prefix and finalizer use
highest priority to surround ordinary foreign prefixes and observe an exception before ordinary
foreign finalizers suppress it. A null begin token means no owned pass. The finalizer always returns
the exception it received; owned failure reporting cannot throw into the native entry point. The
runtime adapter must begin on the main thread, finish/clear every returned token and enable this scope
only after the complete position/timing group is admitted. Explicit foreign ordering that defeats
these assumptions is rejected through effective finalizer ordering before enabling and at entry/exit.
Finalizer factories are rejected before owned installation could reexecute them for inspection.
Later invalidation disables observation with one failure report; cleanup still receives an unsuccessful
result for any existing token. Foreign original-skipping prefixes and ordinary later exception
suppression remain effective and are covered by owned Harmony fixtures.

`Core/MessageGuiPass.cs` collects one pass only. Completion after a native exception or explicit
invalidation never writes. Nested passes invalidate both scopes; repeated Message object identity
invalidates the group; stale completion cannot replay a finished group. Native order is retained.

## Exact native window identity

`MessageWindowBindings` checks the message's recorded rectangle, supplied callback's captured message
and rectangle, and one existing ImmediateWindow with the exact admitted callback method and captured
Message reference. No ID/hash guessing. A repaint must have registered the exact supplied Action and
rectangle. An input pass may retain the prior native Action, as current ImmediateWindow does; its
captured rectangle and windowRect are updated together with lastDrawRect using the freshly observed
native bounds. All original snapshots remain available for rollback. No callback is invoked or
replaced. Missing/not-yet-rendered windows are not fabricated; ambiguous matches invalidate the pass.
Field/method types and instance/writable relationships are checked before use. Every existing
callback invocation list is inspected for the known message identity. A combined callback containing
that identity is incompatible, whether its native entry is first or last and whether a separate
singleton window also matches. It is never treated as not yet rendered. Combined callbacks belonging
only to other messages remain outside this binding. Supplied combined callbacks are also unsupported.
The capture result distinguishes ordinary input before the first registered repaint from incompatible
or ambiguous binding; only the former may be skipped without fabricating a visible window. This
distinction depends on the separately required queued-registration contract.

## Required timing boundary before translation

The initial no-colony Entry fallback is specified separately in
[MSG-WINDOW-STACK](MSG-WINDOW-STACK.md). It commits at actual WindowStack entry before ExtraOnGUI,
shadows and window drawing; it does not infer bar absence in gameplay or complete automatic clearance.

The scope and identity helper do not prove native queued-window timing. Before this group can move
anything, independently admit the effective native registration and caller ordering: registration
stores the rectangle/callback for later WindowStackOnGUI; MessagesDoGUI completes before bar collision
and window drawing/input consume this pass. Inspect actual incoming streams on repatching, preserving
them unchanged on unsupported semantics. Current source proof is 1.7.5000 Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`; this hash is evidence, never
an admission predicate. The bounded invocation/consumer observation below supplies one caller-order
prerequisite; it does not prove arbitrary unobserved consumers or replace native composition checks.

### MSG-NATIVE-QUEUE: registration relationships only

`Harmony/MessageRegistrationGuard.cs` observes the effective incoming IL of these two resolved native
instance methods without changing instructions, labels, exception blocks, native arguments or calls:

- Public `Verse.WindowStack.ImmediateWindow(int, UnityEngine.Rect, Verse.WindowLayer, Action, bool,
  bool, float, Action, bool):void`.
- Private `Verse.WindowStack.AddNewImmediateWindow` with the same parameters/return.

The native resolver must select these exact symbols and `Verse.Window.windowRect` /
`Verse.ImmediateWindow.doWindowFunc`. The helper checks non-generic instance/public-private callable
shapes, the identical parameter lists and writable instance field/type relationships. Each method
must store its rectangle argument and drawing callback argument exactly once on the same identifiable
window receiver. The public method must forward those two arguments unchanged to the private method
on the same WindowStack instance. The window's field stores may be separated by unrelated work;
native `dup` and ordinary local aliases are supported. No concrete local index, native ID rule, loop,
message stride or whole-body hash is pinned.

Provenance is deliberately local to straight-line IL blocks; unknown merges do not prove an alias.
Writes/address exposure of the consumed instance/rectangle/callback arguments cannot retain their
original provenance. A local whose address is exposed anywhere in either analyzed method never
regains trusted provenance in that method: a retained address can mutate it after reassignment or
across a block boundary. Ordinary aliases with no address exposure remain supported. A known direct invocation of
the drawing callback argument during registration is rejected. The observer publishes a new opaque
generation on every relevant repatch, including rejection. Consumers must capture a non-null
generation and recheck reference identity before commit. Availability also requires both acquired
observers still present and last in the effective transpiler ordering; factories are not speculatively
executed to discover that order. Two independent owned receipts are rolled back even if one fails.
Compatible unrelated prefixes, no-ops and local aliases remain effective, including after rollback.

The registration observer now retains each incoming body's published version and relevant operations.
Every authority decision inspects both native targets in one C01 publication interval, requiring the
matching versions, owned receipts, ordering and preserved rectangle/callback stores plus AddNew
forwarding. It returns the incoming stream unchanged on metadata failure, with authority revoked.
Safe owned-slot readers never execute foreign factories or construct their attributes. Independent
review is CLEAR after39 affected RegistrationGuard/Bridge checks, including actual inner skips on
both store paths and AddNew forwarding. Retained hashes/RED/GREEN evidence:
artifacts/Megamod/P0-B/messages-queue-effective-handoff.md. Native geometry and composition remain open.

**This proves necessary local registration relationships only.** It does not prove that the stores
execute on every path, that an opaque helper/prefix/postfix never invokes a callback, or that callers
render in the required order. Runtime exact-window binding still has to verify the actual result and
revalidate capture identity at group commit. Before translation can be enabled, caller timing and the
scoped early-consumption observer below must compose with that actual result validation. The observer
invalidates active contributions on early consumer entry without suppressing native work; it is not
a universal observer of arbitrary mod UI.
No absence-of-arbitrary-calls assertion or blanket foreign-owner rejection establishes timing safety.

### MSG-NATIVE-CONSUMPTION: active-pass consumer veto only

`Harmony/MessageConsumptionPatch.cs` takes the structurally resolved `MessageDrawShape`, exact native
collision-query MethodInfo, an `Action` bound to the current pass's `Invalidate`, and a failure sink.
It exposes `Candidate`, `Available` and `Enable()`. It acquires two independent owned prefix receipts:

- The exact parameterless instance void callback in `MessageDrawShape.Callback`. In inspected
  1.7.5000 this is `Verse.Message/<>c__DisplayClass44_0.<Draw>b__0`; generated spelling is evidence,
  never a lookup or admission predicate. The callback and captured fields must share a declaring type.
- Public static `Verse.Messages.CollidesWithAnyMessage(UnityEngine.Rect, out float):bool`. Its first
  parameter must be the same value type as the admitted captured rectangle; the second is explicitly
  out float. Generic/abstract or incompatible signatures cannot acquire these observers.

These read-only entry observers need callable identity/shape, not body-pattern restrictions: entry
itself conservatively vetoes the pending contribution, including before the first observed message
and when another prefix later suppresses the consumer body. They neither invoke/replace callbacks
nor touch arguments, return/out values, windows, messages or native exceptions. Bind invalidation to
the main-thread MessageGuiEvent coordinator when composing the outer boundary below; its direct
MessageGuiPass invalidation additionally covers pending collection. Outside any owned invocation it
is a no-op. No surface collection or cache is owned here. Installation is dormant until the entire
placement group is admitted/enabled.

Both prefixes must remain present exactly once and first in their effective prefix order. Ordinary
compatible foreign prefixes remain supported, including original-skipping ones; forced earlier
prefixes revoke authority. Prefix factories are rejected before installation/order inspection could
speculatively reexecute them. Availability is rechecked on entry; observation/order failures disable
the helper and report once without escaping through native code. The runtime composition must also
recheck `Available` before a group commit, because an earlier foreign prefix can throw before this
observer executes and a foreign finalizer can suppress that exception. Owned rollback attempts both
acquired receipts and removes only this helper's routes/slots.

**Outstanding:** this helper alone cannot detect consumption before the message pass starts; the
outer coordinator below retains that observation. Neither helper establishes freshness of callback
metadata across a MessageDraw repatch, main-thread runtime wiring, whole-window binding/revalidation
or the placement result. Those gates remain required; `Available` alone never authorizes placement.
There is no central registration.

## MSG-NATIVE-EVENT: bounded outer-invocation timing

Targets are the exact declared public instance parameterless void overrides
`Verse.UIRoot_Entry.UIRootOnGUI` and `RimWorld.UIRoot_Play.UIRootOnGUI`. They must share the exact
resolved `Verse.UIRoot.UIRootOnGUI` base definition, and the observed receiver must be the exact
selected native root type. A further overriding subtype is not covered merely because it later
calls one of these base implementations. No generated-name lookup, body/stride hash or blanket
foreign-owner exclusion is used.

`Harmony/MessageGuiEventPatch.cs` acquires a prefix/finalizer pair for each override, leaving the base
method unpatched. Its candidate is `MSG-NATIVE-EVENT`, governed by this target document. Each pair
observes an invocation envelope, not proof that its original body
ran. Prefix ordering must start the envelope before other prefixes; first-finalizer cleanup prevents
later foreign finalizers from reusing it. The original native call, arguments, event handling and
exception flow remain untouched. An opaque token per invocation avoids treating frameCount or the
reused/mutable Event.current object as an event identity. Entry/play forwarding to unpatched base
UIRootOnGUI must not create a second envelope. Nested invocations, unknown receiver/current context,
stale tokens or repeated collection attempts cannot supply fresh placement authority. If a pass
already completed, later reentry/repetition preserves its completed native effects and only rejects
further owned commits for that invocation. Observer factories are rejected before owned installation
or effective-order inspection could execute them. Owned rollback attempts all four receipts and
removes only this helper's routes. Order/context failures disable the helper and report once.

`Core/MessageGuiEvent.cs` binds directly to MessageGuiPass: remember known callback/collision
entry before collection, invalidate pending collection, and allow ordinary consumption after its
prepared-state commit. Collection completion only seals; it never moves native surfaces. No active
outer envelope means no owned message pass. Existing MessagesDoGUI scope still
requires its actual original execution and success; the outer envelope does not replace that proof.
Runtime composition must capture/check current root, main-thread/event context and all relevant
admission generations before commit. End/exception cleanup discards pending state; unrelated native
exceptions after an already committed/displayed pass cannot be retroactively treated as permission or
used to undo rendered input state. A compatible foreign caller may invoke actual MessagesDoGUI while
skipping the outer original; the observed envelope remains valid, whereas a skipped MessagesDoGUI
original never permits movement. This bounded runtime-order rule does not claim whole-caller/body
proof, absence of arbitrary external consumers, or all-event atomicity.

Prepared-bar activity before MessagesDoGUI begins must also reach the active envelope, even though
the bridge has no pending collection yet. It vetoes later movement without requiring a collision
query. Calls outside an envelope remain harmless; calls after completed effects do not undo them.
The actual-provider regression retains an explicit sentinel proving the early call occurred, then
removes its foreign prefix and verifies recovery in the next invocation.

Public composition is explicit: `MessageGuiEvent.Enter/Exit` return/consume opaque Envelope tokens;
`BeginMessages/CompleteMessages` own and seal one Collection backed by the existing pass;
`CommitPrepared` commits its supplied later geometry at most once; `NotifyConsumption`
receives both admitted consumer observations. `MessageGuiEventPatch` accepts the exact base/override
methods, coordinator, current-context predicate, bounded failure callback and optional exact-envelope
exit notification. Exit notification runs after coordinator invalidation, including exceptional exit,
and clears the bridge's pending references. Candidate admission and
Enable are separate. Runtime must supply the exact current root/main-thread/native GUI predicate and
check it, every helper's availability and captured metadata/generations before completion. Concrete
native projection is authored below but remains unregistered/unverified. Native surface writes must
remain direct field updates, without callbacks/reentrant native consumption during writes; the outer
envelope cannot retroactively invalidate consumed geometry.

## Unregistered native composition

`Harmony/MessagePlacementBridge` directly composes the reviewed helpers with one existing pass. Its
group installs queue and draw observers, then a deferred consumer candidate, the outer event/message
scopes and final publication. The callback only becomes known after the draw observer inspects actual
incoming IL. Deferred installation therefore constructs its binding/consumer, validates the complete
consumer target before acquiring its receipts, and relies on the common coordinator to roll back all
attempted candidates if it fails. The entire group stays dormant until final publication. No nested
admission coordinator or second message collection is introduced.

`Runtime/MessagePlacementRuntime` resolves exact installed Message, Messages, WindowStack, Window,
ImmediateWindow and Entry/Play/base UIRoot members. Its forwarding signature matches native
ImmediateWindow exactly, with stack receiver and final Message identity. Native registration executes
once before owned observation; its exceptions propagate and it is never retried. The root-owned route
candidate rejects another owned route and the original Mlie.BetterMessagePlacement provider. Missing
required symbols/settings registration produce an unavailable resolver candidate, not invented targets.

Admission/construction require the native main thread. Runtime context requires UnityData's main-thread
check, a current native GUI event, exact Find.UIRoot and its WindowStack. Each actual message collection
captures logical UI width/height, repaint classification, current Game/common lifecycle receipt/save
binding/settings revision and effective offset pair. It retains native window enumeration, not a copied
window registry or stale list wrapper. Precommit checks reject changed roots/stacks/viewport/event
classification/settings/save ownership and changed draw-shape/queue generations. Compatible repatches
with unchanged captured members may recover on the next pass; changed members cannot silently reuse
old callback bindings. No Harmony rebuild occurs from the draw path.

Captured windows now expose IsCurrent validation of exact membership/callback/message identities and
all original rectangles, including combined/duplicate callbacks introduced after capture. Batch
prevalidation checks every surface before its first write; failure does not restore over a foreign
mutation when no owned write happened. Final authority must remain pending through that validation
and be checked immediately before the first write. The adversarial host case invoking an admitted
collision consumer during live-window re-enumeration exposed premature completion and now passes:
the collection/pass remain pending through all prevalidation, then recheck token/envelope authority
after the final read-only context callback. A consumer called by that callback also vetoes movement.
Once authorized, writes only set the three admitted fields;
no native callback, drawing, recache or external validation is permitted between those writes.

The narrowly shared `MessageBarBoundsReceipt` contract distinguishes Visible (finite logical Bottom),
Absent and Unavailable (localized ReasonKey), with read-only IsCurrent validation. The former early
Func<UIRoot,MessageBarBoundsReceipt> constructor argument is removed: capture during MessagesDoGUI
cannot certify later native geometry. The bridge retains sealed bindings until CommitPrepared is
called at the admitted later handoff. Its optional MessageBarPreparedPatch/capture pair currently
has a real Harmony fixture consumer only. Runtime now supplies the reviewed Entry/no-Game fallback
at the first WindowStack consumer (see MSG-WINDOW-STACK), but remains unregistered and supplies no
actual bar provider. Precommit validation must never recache/re-render/re-run S001. Native automatic
clearance and S001 effective bounds remain required final behavior; a manual-only milestone does not
replace them. See MSG-BAR-LAYOUT-PREP for the precise timing-only admission and remaining C01 migrations.

## Verification

2026-11-11: the existing bridge accepts an ordinary effective-enable callback. False at collection
entry performs no owned collection/movement; disablement is quiet and does not fault admission.
The same callback is checked at collection completion and final commit. Native message/window
registration, callback execution and outer cleanup continue. Reenablement uses the retained offset
pair in the next invocation. A real host failure demonstrated movement while disabled, followed by
focused fixes and a disabled/native → enabled/moved bridge case. Current runtime supplies the common
`messages.enabled` effective value. Native action evidence remains the orchestrator's gate.

C01's Event reader now uses immutable Prefixes/Finalizers from one safe publication snapshot for
each target's ordering/factory/ownership decision, with fresh projections for sorting and every
OwnedHarmonyPatch reader. Receipt projections include only the acquired owner's registrations,
so a later unrelated foreign factory cannot prevent owned rollback. An actual RED constructed6
foreign parameter attributes during validation; the corrected Event/Bridge suite passed34 tests
and independent review. Both parameter- and method-attribute inspection have zero constructions;
actual provider composition retains its normal behavior. Acquired-envelope cleanup remains
independent of current metadata admission. Scope now uses the same safe reader; its prefix check
requires membership, not sorting, and only finalizer factories prevent its finalizer-order proof.
After acquisition, completion is independently attempted in finally even if metadata inspection
throws. Native exceptions retain their identity and reporting is bounded to one failure. A real
post-acquisition foreign repatch reproduced the cleanup failure in four cases; all four passed after
the fix, with60 affected checks passing and an independent CLEAR recheck. Consumption now uses one
safe snapshot per target decision too, preserving its prefix-factory rejection and first-prefix
requirement. Its receipt readers project only owned prefixes. The actual two-case attribute
construction regression changed RED→GREEN, and56 affected checks passed with independent CLEAR.
Retained evidence: artifacts/Megamod/P0-B/messages-scope-cleanup-fix-handoff.md and
artifacts/Megamod/P0-B/messages-consumption-reader-handoff.md.
Safe role metadata does not prove body-effect compatibility or native integration.

Pure host fixtures cover exact message/window/callback correlation and three-rectangle mutation /
restoration. Owned Harmony tests must cover prefix/finalizer original exception preservation, empty
skipped-original passes, nested passes, repeated initialization and owned-only rollback. Current outer
fixtures additionally cover exact entry/play base forwarding, consumer-before-collection veto, unknown
receivers/context failures, foreign observer ordering/factories, native exception cleanup, reentry,
and a compatible foreign replacement caller. Pure coordinator fixtures cover stale/abandoned tokens,
repeated collection and bounded reporting. These are host fixtures with acquired Harmony patches,
not native UIRoot or Unity event execution. The native
orchestrator must use actual settings actions, display real gameplay messages, then hover/left-click/
right-dismiss at their relocated positions across repaint/input, bar-present/absent and resize cases.
Retain before/action/after captures and complete cleanup; no fixture, startup log or installed hook
accepts this feature. All frame/timing/native tasks remain open.
