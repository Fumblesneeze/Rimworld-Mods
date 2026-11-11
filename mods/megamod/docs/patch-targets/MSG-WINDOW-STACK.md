# MSG-WINDOW-STACK: message placement before native window consumption

Owner: fumblesneeze.megamod; S071 megamod-message-placement. Initial implementation slice only;
unregistered and not accepted in game. Root owns target-index/bootstrap registration.

## Native boundary

Target the resolved public, synchronous, non-generic instance
`Verse.WindowStack.WindowStackOnGUI():void`, with no parameters. Retained1.7.5000 source is
`artifacts/Megamod/P0-B/messages-native-1.7.5000/Verse.WindowStack.cs`; research assembly SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` is not an admission predicate.
The body copies windows, calls ExtraOnGUI, updates immediate-window membership, draws shadows and
calls WindowOnGUI. Entry is the fallback barrier before any of those native body consumers.

A highest-priority void prefix observes entry before foreign prefixes. C01's immutable prefix
snapshot must establish the sole acquired owned prefix first in effective order. Reject prefix
factories before sorting; safe filtered metadata owns receipt checks and rollback. No ordinary/inner
body matching is needed for an entry observation that runs before that body. Foreign code, arguments,
original skipping, return and exceptions remain native. This does not claim to isolate arbitrary
foreign behavior or certify other windows' geometry. Inspection/callback/reporting failures disable
this contribution without throwing into native window processing.

## Initial fallback policy and composition

The adapter admits an Absent receipt only for the exact current UIRoot_Entry with Current.Game null,
the exact root WindowStack, native main-thread GUI context and stable logical viewport. Recheck that
context before any message write. No other state is inferred absent. Gameplay, world setup and unknown
providers remain ineligible in this slice. This is not a manual-only replacement for the full feature.

Only a previously successful sealed message collection in the same outer invocation can commit.
Use its existing bindings, generation/context checks and whole-stack translation. Window entry before
collection, during collection, for a different stack or with unavailable fallback context vetoes pending
movement before consumers continue. Entry outside an envelope is harmless. After a successful prepared-bar
commit, later window entry must not move again or falsely report a failed second commit. Outer finalizers
still discard unfinished pending references on exceptions. Never replay callbacks or create windows.

## Verification and remaining gates

The first explicit-Harmony regression collects native-shaped messages, invokes the actual fixture
WindowStack method, and observes translated coherent rectangles at its first ExtraOnGUI/shadow consumer.
Assert no movement at collection end, one registration, unchanged callback identity and one translation.
Cover early entry, repeated entry, prepared-bar then window, stale context, native/owned exceptions and
compatible foreign entry observers versus unsupported earlier ordering. Host fixtures are not a fake game.

Native root workflow: in a unique disposable menu profile with a prearranged nonzero global offset,
open native Options, use Reset learning helper and confirm. Its native action posts AdaptiveTutorIsReset;
capture before/action/after and personally inspect the moved message. Do not post the message directly.
Prearranged configuration is setup, not acceptance of the unfinished text-input/settings workflow.
The map-only E2E readiness does not yet cover this menu workflow; root may use existing typed minimized
Gateway controls without extending the runner in this slice.

Actual automatic bar geometry, visible labels/weapons/frames, first layout changes, S001 composition,
gameplay interactions, common-settings workflow and all feature acceptance remain open.
