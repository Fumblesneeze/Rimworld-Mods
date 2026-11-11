# MSG-NATIVE-DRAW: observe the native short-message window

Owner `fumblesneeze.megamod`; S071 `specify-megamod-message-placement`, coherent message geometry,
unchanged callbacks, full-stack fitting and guarded fallback. This is a Harmony fallback: no XML or
public message-layout extension exists on the inspected draw boundary. No owned megamod code is patched.

## Target and predicates

`Verse.Message.Draw(int,int):void`, public non-generic instance method, current game assembly.
The incoming stream must contain one `CalculateRect(float,float):UnityEngine.Rect` result stored in
a captured rectangle field; that same field supplies the rectangle of one native
`Verse.WindowStack.ImmediateWindow(int,Rect,WindowLayer,Action,bool,bool,float,Action,bool):void` call.
The supplied Action must belong to the same capture object, which contains this exact Message.
The adapter replaces only that registration call with an exact forwarding call plus observation.
It does not calculate positions, invoke callbacks, rewrite text or bypass any native draw method.

The validator checks complete call signatures and captured dataflow/cardinality on the actual incoming
stream, tolerates local numbering/nops/unrelated instructions and returns the input untouched if
missing/ambiguous. Another owner alone is not a rejection. The owned observer must remain the last
ordinary transpiler; that ordering alone does not prove the final Harmony3 stream. Admission must
also retain the consumed message/capture/rectangle, delegate and forwarding relationships against
relevant inner effects, including a skipped or rewritten emitted Forward call. Compatible unrelated
effects and supported observations remain eligible. Factory transpilers whose
methods cannot be inspected without running foreign code are unsupported. Each repatch revokes the
previous admission before validation; the runtime checks current publication before collecting.
The observer uses minimum transpiler priority so ordinary compatible transpilers installed later
are still validated before observation. An explicit foreign after-order remains unavailable because
its final stream cannot be established by this observer. Native forwarding remains intact.

The target and its owned-slot receipt now use safe immutable publication metadata, exact published
versions and emitted-body generations. Recomposition invalidates previous authority before inspecting
the incoming stream. Relevant inner effects must preserve the consumed operations; unrelated edits
and supported observations remain eligible. Inspection constructs no foreign attributes and executes
no factories. Existing C01 snapshots suffice without a new shared export or whole-method fingerprint.
Unavailable or oversized metadata returns the incoming stream unchanged and leaves admission revoked,
so foreign removal and public instruction inspection remain usable. A later genuine composition may
recover. Independent review is CLEAR after35 affected Draw/Bridge checks, including the actual
oversized-registration RED/GREEN. Exact local evidence and hashes are retained in
artifacts/Megamod/P0-B/messages-draw-metadata-fix-handoff.md. Native registration, geometry and player
workflows remain open; these checks do not accept S071.

Observation additionally requires an exact native callback/window identity and equal native
Message.lastDrawRect and supplied rectangle. A skipped original contributes no captured window.
Only observed message windows are eligible, never unrelated immediate windows.

## Native timing and scope

Current 1.7.5000 rev1275 evidence: Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
Evidence decompilation: `artifacts/Megamod/P0-B/messages-native-1.7.5000/` (local, ignored).
`UIRoot_Play.UIRootOnGUI` calls base/messages, then map/world/bar, then WindowStackOnGUI. Native
ImmediateWindow registers on repaint and reuses its window/callback on input passes. The callback
captures the padded rectangle; moving only windowRect would leave that capture and lastDrawRect
incoherent. The eventual frame adapter therefore translates all three together, before window drawing,
and uses actual collected bounds/order instead of pinning the 26-unit spacing or lesson handling.
Main-thread only; no recursive sizing/draw pass. Message frame scope and timing require their own
admission before this observer can drive translation. This target alone is not live placement.

## Admission and failure

Early phase, candidate `MSG-NATIVE-DRAW`, common AdmissionCoordinator/OwnedHarmonyPatch receipt.
An invalid incoming stream or install exception rejects the owned contribution and rolls back only
the newly installed owned slot. Later incompatible repatching makes the observer unavailable.
Native registration/callback remain authoritative. Example common startup warning:
`[fumblesneeze.megamod] Patch MSG-NATIVE-DRAW not applied; target=Verse.Message.Draw;
targetSpec=MSG-NATIVE-DRAW; reason=missing or ambiguous captured native window rectangle;
unavailable=message placement; fallback=native placement`.
Original provider `Mlie.BetterMessagePlacement` suppresses the complete owned position group before
registration; central integration owns this check. No new dependency or XML contribution.

## Evidence and remaining gates

Focused owned Harmony fixtures must prove exact forwarding/native callback preservation, compatible
foreign edits, absent/ambiguous dataflow, later repatch invalidation and owned-only rollback. Fixtures
are host types, not loaded game content. Source shape is inspected; actual game/UI acceptance remains
open. The final native workflow must change offsets through common settings and observe placement,
hover/left-click/right-dismiss at the moved rectangle, bar-present/hidden states, input/repaint
continuity and exception cleanup on the reviewed build. Pure host success never accepts S071.
