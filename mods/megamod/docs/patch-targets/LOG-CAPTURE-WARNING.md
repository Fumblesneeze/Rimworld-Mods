# LOG-CAPTURE-WARNING: ordinary warning capture

Owner: `fumblesneeze.megamod`, S035. **Implemented-unverified warning-only slice, registered by the
shared bootstrap; the overload-admission correction passed review. Native capture/muting acceptance
remains pending.** This consumes the existing physical
[DDI-REPORT-WARNING](DDI-REPORT-WARNING.md) receipt, not another patch registration.
Owns [Muting is an explicit loss of future diagnostics](../../../../openspec/changes/specify-megamod-debug-log/specs/megamod-debug-log/spec.md).

## Target and consumed semantics

Public static non-generic `void Verse.Log.Warning(string)` in inspected 1.7.5000 Assembly-CSharp.
The native suppression getter dominates one category field / argument-zero / generic-string
conversion / Warning output region. Native WarningOnce applies its identity before forwarding.
The supplied-text output and subsequent PostMessage/open-window effects lie on the accepted branch;
the suppressed return/leave skips them. No global writer, Window, Once method, original provider
or owned implementation is patched.

`NativeLogPatch` validates callable/category/conversion shapes, supplied argument use, cardinality,
suppression dominance, illegal branch/handler entry and final effective transpiler order. It validates
the actual incoming stream on every execution and returns it unchanged on failure. Factories are
rejected from the same snapshot before sorting without executing foreign probes. Line numbers,
offsets, local numbering, full hashes and absence of other owners are not predicates. See
DDI-REPORT-WARNING for the supported structural region and exact receipt/ownership limitations.

## Transform and publication

C01 revision 12 adds exact published-version/body-token admission and immutable inner-effect
classification on the shared route. The emitted ShouldCapture call and its identity operands are
protected alongside native eligibility. An incompatible effect only on that helper exposes capture
unavailability while safe context can still publish. Native foreign-modified behavior is preserved;
disabling owned capture does not undo another mod's suppression. Void/no-argument observations,
void postfixes reading a single primitive/enum result by value, and unrelated selected operations
remain eligible; ref/out/mutable results and unknown outer/unchecked bindings do not. No callback
or factory is executed as an inspection probe. This first reporting slice awaits review/native proof.

The corrected shared reader also avoids attribute constructors: exact state-entry copy under
PatchProcessor → dictionary monitor order, maximum 4 MiB, inspected JSON/envelope validation and
provider-owned DeserializePayload only. Unknown shape/version/backend fails unavailable. See
DDI-REPORT-WARNING for the precise boundary; there are no state writes or serializer copies.
The 4 MiB ceiling is owned inspection policy, not a provider limit, with measured host metadata
headroom and real boundary fixtures retained in the owning design and handoff.

After validation, clone instructions and transfer the accepted entry label to the first capture
instruction. Invoke a no-throw decision before projecting/converting the text. Rejection branches
to the existing native suppressed return/leave, preserving lock cleanup. Native suppression and
Once rejection do not invoke the owned decision. Accepted events continue one original output;
muting never deletes retained/file-written records or restores an already-used native key.
For capture specifically, a suppressed leave must resolve (ignoring no-ops) to a return. An effectful
epilogue marks only capture semantics unavailable: omit the gate while retaining the valid context
transform and same physical receipt. The shared adapter retains baseline context tolerance even
with its non-null capture callback and a manually or automatically disabled mute preference.

The real `diagnostics.warning-channel` publishes after installation. It may provide capture while
the Error context group is unavailable; S094 additionally requires both routes and its own effective
setting. Default-off `log.muteWarnings` uses common global-only storage and immutable worker snapshots.
The revision17 bootstrap maps active `bs.betterlog` to common effective-off resolution and emits one
bounded startup warning even if the stored preference is already off. There is no parallel runtime
provider-availability gate. The effect and timing match manual off, with stored requests retained;
this is best effort, not coexistence compatibility. S094 originals map only definition context and
do not suppress independent capture. Foreign patches/settings remain untouched. Native verification
of the revised composition remains pending.

Every rebuild invalidates publication. Compatible routes resume only after explicit safe refresh
outside native logging locks; root must drive RefreshReporting even when S094 is disabled. Output
callbacks read lock-free state, never Harmony metadata. Unavailable capture fails open with a
distinct saved/effective UI status. Capture callback failure latches only capture off, attempts one
bounded protected warning and preserves working S094 formatting. Recorder failures cannot escape.
Each incoming generation carries separate capture-exit availability. A semantic warning is attempted
once only when effective muting is requested, never for a manually/automatically off capability. A later
compatible rebuild can restore capture semantics after safe refresh; a callback fault remains latched.
Structural/projection failure still makes the shared physical route unavailable; the UI does not
promise muting in that state. Failed initial acquisition is not retried. Arbitrary concurrent owner
mutation or arbitrary foreign logging locks are outside the declared guarantee.

## Protected output and evidence

[LOG-PROTECTED-WARNING](LOG-PROTECTED-WARNING.md) writes directly through the category or an owned
session fallback, bypassing ordinary capture without resuming it. Outside direct writers are excluded.

Implementation: [NativeLogPatch](../../Source/Patching/Harmony/NativeLogPatch.cs),
[NativeDiagnosticReporting](../../Source/Patching/Harmony/NativeDiagnosticReporting.cs),
[WarningCaptureRuntime](../../Source/Features/Diagnostics/WarningCaptureRuntime.cs) and
[common UI binding](../../Source/Features/Diagnostics/WarningCaptureSettingsUi.cs).
Focused fixtures: NativeLogPatchTests, NativeDiagnosticReportingTests and ProtectedNativeWarningTests
in Megamod.Harmony; WarningCaptureOptionsTests and DiagnosticOriginalFeaturesTests in Megamod.Unit. RED/GREEN receipts are retained in
`artifacts/Implementation/megamod-p0-a/tdd-ledger.md`.

The [native scenario](../replacements/debug-log-native-scenarios.md) must establish real settings,
reload, retained/file/UI effects, persistence, protected output and provider behavior on the reviewed
build. Seven-language rendering and native acceptance remain pending; host fixtures do not prove them.
