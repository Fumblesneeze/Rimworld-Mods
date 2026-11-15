# SS-READ-SLIDER-SOUND: suppress hidden native slider sounds

## Ownership and traceability

Owner `fumblesneeze.megamod`, S141 SS-06. Prefix ID `SS-READ-SLIDER-SOUND.prefix`, implemented
through SettingsReadControlGuard and SettingsReadControlGuards.Sound. See the
[control entry contract](SS-READ-CONTROL-ENTRY.md) and owning settings-search design.

## Target and minimal predicates

Assembly-CSharp, Verse.Widgets, exact `private static void CheckPlayDragSliderSound()` with a
concrete synchronous managed body and no generics. The prefix returns false only during an admitted
read; no native argument/value is rewritten. Skipping this specific native sound boundary avoids
its sound/timestamp body while ordinary invocations remain native. Idle neutral input separately
prevents the slider's direct mouse-down sound path. No promise to silence arbitrary foreign calls.

## Composition, ordering, and invalidation

The owned prefix must be first in the current effective outer order. Use C01 safe publication,
without foreign factories/attributes as inspection. There is no finalizer receipt or state slot.
Other owners and other owned kinds are retained; a native-body skip does not undo arbitrary foreign
postfix/finalizer effects. Revalidate before each read. Unknown order disables discovery entry;
arbitrary mid-callback repatching is outside rollback guarantees.

## Admission, failure, and diagnostics

Use the atomic control group and one bounded C01 warning per skipped enabled candidate. A missing
target/unsafe order/install failure disables automatic reads while ordinary page access continues.
Roll back only newly acquired owned prefix. Do not replace the method's normal body globally.

## Verification matrix and final observation

Host prefix-only tests prove body suppression only in scope, compatible-owner retention and
ordering/rollback. Native tests must draw a rounded slider on an isolated repaint, retain its
original returned value and observe the sound timestamp before state restoration, then edit the
ordinary visible slider. The reviewed 31-step component run retained the native timestamp during
both probes and root inspected the later real drag changing 0.3737 to 0.9344. Timestamp assertions are
supporting diagnostics; audio was disabled, so no audible-sound proof is claimed. See local
discovery/values/root/verification.md. Complete reader acceptance remains open.
