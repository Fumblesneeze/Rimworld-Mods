# SS-READ-WINDOW-CONSTRUCTION: refuse native window construction during discovery

Owner: `fumblesneeze.megamod`; S141 SS-06. Prefix owned by the existing
[action guard](../../Source/Features/SettingsSearch/Harmony/SettingsReadActionGuard.cs), bound by
[the native action family](../../Source/Features/SettingsSearch/Runtime/SettingsReadActionGuards.cs).
The [owning spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md)
requires failed-page reporting and continued ordinary access, not rollback of arbitrary callbacks.

## Target and consumed guarantee

Exact public instance `Verse.Window(IWindowDrawing)` constructor in the game assembly, with a
managed body and nongeneric declaring type. Throw before its body only while an owned settings
read scope is active. This prevents the inspected XML Extensions derived constructor body from
changing provider selection/cache or closing existing settings windows before WindowStack.Add.
It does not protect foreign derived field initializers or base-constructor argument evaluation
which occurred before the native base boundary. Outside reads, original construction is unchanged.
Static initializers, abstract/open generic declarations and missing/changed callable shapes fail
admission. The supplied native carrier/wrapper is constructed before entering the read; it is never
added to the window stack or closed as part of discovery.

## Composition and failure

Reuse C01's published metadata and exact owned prefix receipt. Require our prefix to be first in
effective order; prefix factories prevent proving that order. Compatible later prefixes,
transpilers and postfixes remain installed. Constructor finalizers are rejected because they can
suppress the refusal and resume a mutating derived constructor body. Recheck slot/order/finalizers
before reads and at the refusal boundary. Game build/hash is evidence, not an eligibility rule.

The constructor joins the seven-member atomic action family. A failed acquisition rolls back only
newly acquired owned slots; all foreign patches remain. Report one bounded C01 warning identifying
`SS-READ-WINDOW-CONSTRUCTION`, unavailable automatic discovery and ordinary-settings fallback.
During capture, latch `settings-read-window-construction`; publish no observations for that failed
page, retain its catalog identity and normal opening route, and continue other eligible pages.

## Verification

Focused host coverage uses only scoped owned constructors: refusal before base/derived bodies,
ordinary construction, cleanup, finalizer and initializer admission. The affected24 host checks
pass. The reviewed English `megamod.settings-search.native-discovery` workflow passes112 steps with
16 root-inspected captures, proving automatic refusal of both real XML Extensions generated pages,
preservation of an already-open provider window/selection/menu, and subsequent ordinary provider
editing/close/persistence/reopen/restoration. Its cache baseline is empty, so nonempty-cache
restoration is not claimed. Root's local `discovery/automatic-batch/root/verification.md` retains
exact reviewed identities, actions, captures, console/log classification, config hashes and cleanup.
This accepts the exercised constructor-refusal boundary only; provider hidden option reading,
broader arbitrary constructor effects and full S141 remain incomplete.
