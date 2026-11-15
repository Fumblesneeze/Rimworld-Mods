# SS-READ-TOOLTIP: observe literal help without registering hidden hover state

## Ownership and traceability

Owner `fumblesneeze.megamod`, S141 SS-01/SS-06. Prefix ID `SS-READ-TOOLTIP.prefix`, implemented
through SettingsReadControlGuard and SettingsReadControlGuards.Tooltip with the existing recorder.
See the [control entry contract](SS-READ-CONTROL-ENTRY.md) and owning settings-search design.

## Target and minimal predicates

Assembly-CSharp, Verse.TooltipHandler, exact `public static void TipRegion(Rect,TipSignal)` with
a synchronous managed body and no generics. During a read, observe only the literal `TipSignal.text`
and supplied rectangle through SettingsPageRecorder.ObserveTooltip, then skip the native body.
Never invoke textGetter, register an active tooltip or replace normal visible tooltip behavior.
The binder checks TipSignal.text is the expected string field; no arbitrary provider fields are read.

## Composition, ordering, and invalidation

Require the first effective outer prefix from C01's safe current publication. Prefix factories or
unknown order refuse admission. No finalizer/state slot is needed. Compatible foreign owners and
other patch kinds remain; body suppression does not promise undo of arbitrary foreign effects.
Supported repatches are rechecked before read entry; arbitrary changes within a callback remain a limit.

## Admission, failure, and diagnostics

This prefix is part of the atomic control group. Missing shapes, unsafe order or install failure
leave automatic reads unavailable with one bounded C01 warning per enabled skipped candidate and
ordinary-page fallback. Roll back only the acquired owned prefix. Recorder failure is failed/partial
capture as documented, never evidence of a completed help index; literal values are not logged.

## Verification matrix and final observation

Host prefix-only tests cover ordering/body skip and rollback. Native capture must retain a literal
tooltip, leave active tooltip state unchanged, and never execute a supplied throwing lazy getter.
Inspect the native panel after a real request and demonstrate normal editing afterward. The
reviewed 31-step component run passed those supporting tooltip assertions during both probes;
root inspected unchanged controls after each request and later deliberate native edits. See local
discovery/values/root/verification.md. Dynamic help and integrated discovery remain open coverage
gaps, not a provider whitelist.
