# S071 prepared geometry and renderer consumption

Owner: `fumblesneeze.megamod`. Governing plan revision 17, following revision 12, 2026-11-08, supersedes the proposed
recache field-address/private-calculator production work. This is the selected design, not an
implemented geometry adapter or native acceptance. The first timing/argument handoff below is
host-tested and unregistered; the older production experiments were removed on2026-11-08.

## Required boundary

Clearance and the actual native bar must consume the same coherent prepared geometry in this GUI
invocation. `ColonistBarOnGUI` constructs rectangles from current `cachedDrawLocs` and `Size`
(`BaseSize * cachedScale`); frames consume the same native fields. Prove that consumption and
stability, not how a private calculator produced the data. Equal retained values, compatible
replacement preparation or skipped original calculation are not failures by themselves.

Capture actual root/bar, invocation token, logical viewport, view/visibility and renderer-admission
generation. Before native preparation, request its ordinary dirty invalidation on the first
root/bar/viewport/generation or after failed preparation. Native map/group invalidation still applies.
The first changed display must use the current layout. Never wait for the next repaint, require
numerical change, recalculate private layout, draw the bar twice, or maintain a pawn census/cache.

## Inspected ordering and selected commit point

Selected Assembly-CSharp 1.7.5000 rev1275 SHA-256:
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
Retained sources: `artifacts/Megamod/P0-B/messages-native-1.7.5000/`. Exact MapInterface and
RimWorld.WorldInterface were additionally decompiled read-only using the local ilspycmd.

1. Root applies UI scale before UIRoot. Base UIRoot collects messages, then invokes additional
   game-component/world GUI callbacks. `Message.Draw` registers native ImmediateWindows; it does
   not normally consume their callbacks there.
2. Play UIRoot invokes world UI, then map UI. World route requires world selected, active world
   camera, Playing and no screenshot filter. Map route requires a current map, DrawingMap and no
   screenshot filter. The bar additionally checks logical size and tile-picker visibility.
3. Actual `ColonistBarOnGUI` reads `Entries` and `ShowGroupFrames` before its entry loop. **After
   these preparation reads and before the first loop draw/input** is the selected prepared-state
   handoff. Observe its actual receiver/entries local/current fields, then revalidate queued message
   bindings and translate the stack once before any bar or message consumer uses that placement.
4. Bar handling includes `HandleClicks`, group-frame drawing, `DrawColonist`, weapon drawing and
   group-frame clicks. DrawColonist queries message overlap before drawing the portrait. Group
   frames can draw earlier, so the first overlap query is not the common commit barrier.
5. Later `WindowStackOnGUI` calls `ExtraOnGUI`, updates immediate-window membership, draws shadows
   and calls `WindowOnGUI` / `GUI.Window`. Its **entry before ExtraOnGUI/shadows** is the fallback
   barrier for proved absent bar contribution or explicitly unavailable automatic clearance.
   Failure to observe a bar invocation does not prove absence. The message callback itself is too late
   because native window geometry/background/input has already begun consuming the rectangle.

This requires splitting message collection completion from commit/cleanup. Keep native message
order, one native registration per message, all exact callback/window bindings and one whole-stack
translation. Early consumption before the selected barrier vetoes pending movement. Pending state
must be discarded on failed/repeated/nested invocation and outer cleanup. No deferred native work,
second renderer, replayed callback or retroactive undo of native interaction is permitted.

## GUI-local state and minimal admission

The snapshot contains only this invocation's actual consumed inputs: exact root/bar and list
identities; entry order, pawn/map/group identities; all location values; scale/size; visibility;
native extension inputs and S001's own effective presentation receipt. Values must be coherent and
finite, with matching entry/location correspondence. Validate the actual data even if identities
and counts did not change. No layout or per-pawn cache survives the invocation. A dimension memo
is permissible; it contains no geometry.

Admit the effective renderer's relationship from those inputs to its actual draw/input arguments,
including the group-frame getters and selected visible extensions. Validate before commit and at
the actual consumption boundaries; do not use getters that recache/draw during read-only checks.
Late dirtying, in-place edits, changed receiver, argument substitution, repatch, changed routing,
another bar invocation or renewed layout after the handoff must not silently retain authority.
Before movement, reject unproved state. After native effects occurred, preserve them and stop
further owned movement; report the invalidation without claiming this run proved clearance.
Maintaining one stable geometry through all actual uses is a remaining integration obligation.

Harmony 3 inner rewriting follows ordinary transpilers, with optional persistence rewriting later.
Use root's C01 target-specific admission result for the consumed renderer operations/owned markers.
Do not infer final semantics from last ordinary transpiler order, execute foreign patches to probe
them, or ban unrelated owners/inner changes. No private calculator/address guard is a prerequisite.

## Existing code disposition

| Existing helper | Disposition under revision 12 |
| --- | --- |
| MessageBarLayoutPreparation | Retain dimension/admission identity, failure and reentrancy policy if useful; adapt its synchronous callback API to the native handoff, or replace that API. Its 23 cases prove only the old pure orchestration. |
| MessageBarEntriesGuard | Removed with its standalone fixture on2026-11-08. Exact same-receiver original recache then field return was stricter than actual coherent renderer consumption. Its17 cases remain historical evidence. |
| MessageBarExecutionPatch / MessageBarExecutionReceipt | Removed with their standalone fixture on2026-11-08. Original-call counts, calculator witness, run-original and scale observations were not required by the revised contract. Its13 reviewed cases remain historical evidence. |
| MessageBarBoundsReceipt | Retain Visible / Absent / Unavailable with read-only invocation-bound validation, captured at the selected later boundary. |
| MessageGuiEvent / MessageGuiPass / bridge | Retain invocation/reentrancy, exact surfaces, failure and rollback policy; separate collection completion from commit and outer cleanup. Existing completion-at-MessagesDoGUI expectations are superseded. |
| Message window binding / registration / draw / consumption observers | Retain necessary exact native behavior checks, subject to C01 and the later commit boundary. Consumer entry before commit still vetoes. |

Root's dependency search found no selected caller before retiring the three source files and two
standalone fixtures. Exact source/hash snapshots are retained under
`artifacts/Megamod/P0-B/retired-private-production-20261108/`. Current prepared, event and bridge
tests remain. Root owns cross-file leases, compilation and registration.

## Next vertical slice and remaining gates

### Revision 17: selected entry-call arguments, host slice only

`MessageBarPreparedPatch` remains the sole transpiler/publication owner. Its optional non-owning
`MessageBarEntryConsumptionPatch` describes the selected click/draw methods and rectangle value
type. Both supplied methods must be public instance calls returning void with
that value as their first parameter, each appearing exactly once after the injected handoff in the
incoming method's emitted instruction order. The participant adds no target slot, route, callback,
separate generation or runtime adapter. One emitted body and one C01 metadata snapshot serve timing
admission and selected-call argument admission; failure of only the latter retains timing/native work.

The public names deliberately say `EntryArgumentsAvailable`, `EntryArgumentsFailure` and
`CaptureEntryArgumentsAuthority`. They cover selected call-boundary arguments surviving the inspected
published inner effects, not full geometry authority. They do not prove source fields/Size produce
the rectangle, one local feeds both calls, execution/control-flow reaches the handoff first, GUI
invocation lifetime, current prepared values, or stability through later native consumption. A native
caller must not use this receipt alone to authorize automatic clearance. Frames, labels, weapons,
outlines and S001 still need their actual consumed relationships and runtime receipts.

The first honest RED invoked the native-shaped fixture with a real foreign inner prefix rewriting
the click rectangle. Native handling received changed values while the proposed entry authority
incorrectly remained available. Minimum GREEN checks the selected call operations through the
existing publication reader. Both click/draw rewrites now withhold only entry authority, preserve
native effects, and recover after removal/rebuild; old and denied receipts cannot gain authority.
Retained geometry plus an unrelated result rewrite remain eligible. Missing/unreadable participant
descriptions leave timing and native calls available. Handoff-only callers have no entry authority.
The fixture includes an actual value-type `ldloca`/constructor-call form; it does not depend on
`newobj` alone. This is host evidence, not a native geometry test.

Focused RED1/GREEN1 and affected GREEN39 (entry participant, prepared and bridge fixtures) are retained
at `artifacts/Megamod/P0-B/messages-entry-arguments-revision17/`, including before/source hashes,
actual TRX/log references and the review diff. Independent review remains pending at handoff. No
Runtime adapter, Bridge, C01, project link, registration or native acceptance changed in this slice.

The first late bridge slice now retains sealed message bindings until an explicit prepared barrier.
`MessageBarPreparedPatch` observes the actual receiver, returned Entries local and effective
ShowGroupFrames local, after their stores. Its C01 publication checks cover that injected handoff
and compiled generation. The host bridge consumes a supplied bounds receipt and cleans pending
references at exact outer-envelope exit. This does not establish real native geometry, renderer
consumption/stability or first-display invalidation. Runtime no longer captures bar bounds during
MessagesDoGUI. It now supplies the reviewed WindowStack fallback only for the exact Entry/no-Game
context; the actual bar provider and central registration remain absent.

The explicit Harmony bridge fixture now performs the intervening same-list change and actual
prepared-method call before its first consumer; it no longer asserts movement at collection end.
An early bar handoff before collection finishes vetoes later movement even without a collision
query. Native exceptions after collection abort pending state and a later invocation recovers.
The new patch permits a compatible getter-result replacement and observes receiver substitution
at the body handoff. Actual inner skipping of the owned marker rejects publication; unrelated
skips and readonly observations remain eligible. These are host contracts, not native acceptance.

Review corrections retain bar activity in the active envelope even before message collection
exists. The next invocation recovers and outside-envelope calls remain harmless. Successful C01
marker admission is reused only for its unchanged immutable incoming generation; relevant repatch
revokes it, while busy/unpublished new generations retry without borrowing previous authority.
Current values/context still travel through each handoff. Inherited getters are eligible when their
declaring type accepts the actual renderer receiver; exact declaring-type equality is unnecessary.

The completed first RED used native-shaped collection queuing messages; an intervening GUI callback changes the bar's
layout in place; the later prepared-state barrier must place from the new actual geometry, before
the first bar collision/input/draw consumer. Assert no earlier translation and exactly one commit.
Use an honest pure timing seam, then explicit owned Harmony fixtures for the real boundary/argument
relationship. No fake Verse game. Preserve useful prior rollback/duplicate/exception assertions;
replace tests that mandate early bar capture or immediate message-scope commit/cleanup.

The pure MessageGuiEvent cases now distinguish collection sealing from CommitPrepared and pending
repetition from completed effects. The existing real-Harmony bridge composition case now runs its
explicit late native-shaped bar method, retaining callback/rectangle identity assertions.
MessageGuiPassTests.NativeObservationsAreCommittedTogetherOnlyAfterSuccessfulPassCompletion remains
useful at the actual commit boundary; its batching rule itself is not superseded.

Next focused cases: unchanged retained geometry accepted; original calculator skipped but actual
prepared render state coherent accepted; list replacement/in-place edit before commit rejected or
recaptured at the correct handoff; change between handoff and consumed operation loses authority;
absent-bar fallback before ExtraOnGUI; unknown routing unavailable; early collision/window consumer;
empty framed group; repatch; first resize/UI-scale/map/group change. Concrete native actions must
then verify first-display positions and unchanged hover/click/dismiss behavior on reviewed code.

Full visible content remains required: labels and their backgrounds, actual weapons, group frames,
selection/mood outlines, native visibility/screenshots/tile picker, and S001 hidden/reduced parts,
weapon scale/offset and update timing through direct owned composition. Do not union hidden native
portraits back into S001's result. No manual-only final substitute, native acceptance or completed
feature is claimed. UI/preview and seven-language obligations remain assigned separately.
