# SS-READ-CLIPBOARD: refuse clipboard writes during settings discovery

## Ownership and traceability

Owner: `fumblesneeze.megamod`; S141 [SS-06](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md).
Kind: prefix. [Design](../../../../openspec/changes/specify-megamod-settings-search/design.md).
Implementation/admission: [SettingsReadActionGuard](../../Source/Features/SettingsSearch/Harmony/SettingsReadActionGuard.cs);
exact native binding: [SettingsReadActionGuards](../../Source/Features/SettingsSearch/Runtime/SettingsReadActionGuards.cs).
[Owned host tests](../../../../tests/Megamod.Harmony/Features/SettingsSearch/SettingsReadActionGuardTests.cs);
[native workflow](../../../../tests/Megamod.EndToEndTests/SettingsSearch/NativeSettingsActionTests.cs).
The native component workflow is verified; complete reader acceptance remains pending. A prefix is necessary because
the arbitrary foreign callback has no public read-only mode or XML interception point.

## Target and minimal predicates

Target: **UnityStubs / UnityEngine.GUIUtility**, nongeneric `public static void set_systemCopyBuffer(string value)`.
The binder requires this exact declaring type, accessibility, instance/static kind, arguments and
return type. The shared helper additionally requires a concrete synchronous managed body, closed
nongeneric declaration and readable metadata. No async/iterator declaration is admitted.
Acquire at the reader's guarded initialization, then revalidate before every read scope.

The consumed guarantee is abort-before-body, not a rewritten argument or body relationship.
No argument is written, no original IL is rewritten, and unrelated body edits, offsets and locals
are permitted. Game build/hash identifies evidence, never product eligibility. This only guards
this callable boundary; direct foreign field writes, I/O and mid-callback arbitrary repatching are
not a general transaction or rollback promise.

## Composition, ordering, and invalidation

Use C01's safe published metadata reader, without constructing foreign attributes or invoking
factories as inspection. Require the owned prefix to be the first effective outer prefix; its
maximum priority is not sufficient proof. A prefix factory makes ordering unknown and refuses
admission. Compatible later prefixes (including skip-original), transpilers, postfixes and
finalizers remain installed. Another owner alone is never a rejection reason.
Check current acquired slot and order whenever availability is consumed; supported later
repatching invalidates unsafe order or missing/replaced ownership. A reached guard still refuses
the action if its inspection fails. The reader must check the whole group before entering.
A finalizer may suppress the thrown exception, but the thread-owned scope's first failure remains
latched and prevents publishing successful observations.

## Admission, failure, and diagnostics

C01 admits the six guards as one atomic family. Acquired guards are enabled during installation;
the reader admits no read scope until every member succeeds.
A missing/changed/ambiguous target, inspection exception, unsafe order or install failure leaves
automatic discovery unavailable while ordinary page editing remains native. Roll back only newly
acquired owned prefix slots; preserve all other owners and other patch kinds. Failed rollback
keeps the reader disabled. Repeated initialization must not acquire duplicate slots.
No automatic hidden callbacks are registered by this component checkpoint.

One bounded warning per skipped enabled patch is supplied by C01, for example:
`[fumblesneeze.megamod] Patch SS-READ-CLIPBOARD not applied; target=public static void set_systemCopyBuffer(string value); targetSpec=SS-READ-CLIPBOARD; reason=Unsafe prefix ordering; unavailable=automatic settings discovery; fallback=ordinary settings pages`.
No per-frame logging or values from foreign settings are retained.

## Verification matrix and final observation

| Case | Expected result | Honest tier / state |
| --- | --- | --- |
| Supported acquired target | Ordinary call unchanged; read scope throws before effects | Focused Harmony; native pending |
| Caught exception / suppressing finalizer | First failure stays latched | Focused Harmony |
| Benign body / compatible other patches | Admitted and preserved | Focused Harmony |
| Missing/changed callable shape | Rejected before acquisition | Binder/native and focused Harmony |
| Foreign earlier prefix / later repatch | Unavailable; reader must not enter | Focused Harmony |
| Install failure / repeated initialization | Owned-only rollback, bounded warning, no duplicates | Focused Harmony |
| Real button request, unchanged window/settings, later edit | Personally inspected four causal captures | English native16/16steps; component only |
| XML merge or argument-use IL proof | Not applicable: no XML or argument/body rewrite | Explicitly not claimed |

Retain exact reviewed build, ordered mods, input/action/result, screenshots, logs, configuration
hashes and cleanup under local `settings-search/discovery/actions/` evidence. Clipboard verification
uses a later test backstop to prevent OS writes even in RED; it is supporting ordering evidence,
not a claim that a real clipboard write occurred. This component does not close S141-D2/D3.
Reviewed run20261115T100613552Z-355ecaa0188d4a69a824a49f092c97ab is recorded in local
`discovery/actions/root/verification.md`:23 focused host checks,16 native steps, four root-inspected
captures, exact package identity and successful cleanup. Remaining full-reader cases stay open.
