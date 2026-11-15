# SS-READ-LISTING-CACHE: preserve native listing label-scroll state

Owner `fumblesneeze.megamod`, S141 SS-06 in the
[settings search spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md).
Implementation: `SettingsSearch/Runtime/SettingsReadListingGuards.cs`, using the existing
`SettingsReadControlGuard` first-prefix/last-finalizer contract and C01 atomic ownership.

## Targets and minimal predicates

| Patch ID | Exact native declared target |
| --- | --- |
| SS-READ-LISTING-LABEL-CACHE | public instance nonvirtual `Listing_Standard.Label(string,float,TipSignal?):Rect` |
| SS-READ-LISTING-END-CACHE | public instance virtual `Listing_Standard.End():void` |

Both targets are synchronous, managed and nongeneric. Require the two exact private mutable
instance fields declared on Listing_Standard: `labelScrollbarPositions` with type
`List<Pair<Vector2,Vector2>>`, and `labelScrollbarPositionsSetThisFrame` with type `List<Vector2>`.
Both element types must contain no references. Bind direct field accessors once; binding errors
produce rejected candidates without partial installation. No whole-method or assembly hash is an
admission predicate, and no provider/package whitelist is introduced.

## Scoped behavior and composition

Ordinary calls remain native. In an admitted read, the first outer prefix retains the original
instance and both collection references, copies their contents through direct List members and
swaps in detached lists. Null stays null. Acquire all copies before field mutation; reject more
than 65,536 combined entries before mutation. This is a per-call snapshot bound, not preemption of
arbitrary foreign code. The last outer finalizer restores both originals on the acquired instance
on success or exception; restoration does not depend on the current scope or admission state.
No content is written back into original lists, preserving their contents/capacity/enumerator
versions against the inspected native mutations. Nested calls restore their immediate outer lists.

Use separate concrete callback declaring types for Label and End, matched acquired state, and the
existing safe publication reader. Current first/last ordering, paired-state conflicts, factories,
rollback and repatch checks follow [ENTRY](SS-READ-CONTROL-ENTRY.md) and
[EXIT](SS-READ-CONTROL-EXIT.md). Whole-group preflight belongs to the eventual reader. A reached
capture refusal latches a fixed read failure; unknown shape disables read entry with bounded C01
diagnostics and normal-page fallback. Only owned acquired patches are rolled back.

This boundary protects the exact native cache fields; arbitrary foreign retained-alias writes,
other instances, skipped native overrides and unrelated settings effects remain outside it. No
listing geometry, culling semantics, scroll delegate or whole settings object is changed or scanned.

## Verification

The focused English `megamod.settings-search.native-listing-cache` workflow scrolls a real native
long label, requests a hidden draw through a button, observes unchanged visible scrolling and
then scrolls the original label normally again. Supporting probes compare original references,
contents and nullness, including nonempty seen state before End, nested/repeated Label and native
callback exceptions. Local evidence belongs under `discovery/listing/root/verification.md`.
The reviewed component passed 22 native steps after retained RED, with five root-inspected captures
showing native scroll, unchanged scroll after the request, and subsequent ordinary scroll. Exact
and over-limit snapshot probes also passed. It does not accept listing unculling, complete reader
integration or full S141.
