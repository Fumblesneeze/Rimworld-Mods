# DDI-REPORT-WARNING: one contextual warning occurrence

Owner: `fumblesneeze.megamod`. **Shared capture/context adapter implemented-unverified;
shared S035 composition and the overload-admission correction passed independent review. Initial
default-loader contextual warnings were observed; broader native acceptance remains pending.**
Owns the warning half of [S094's one-occurrence contract](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md),
composed directly with [S035 capture](../../../../openspec/changes/specify-megamod-debug-log/specs/megamod-debug-log/spec.md).

## Consumed native boundary

C01 revision 12 first reporting slice now also checks the published Harmony generation and inner
effects after ordinary transpilers. `HarmonyPublication` uses the inspected provider monitor and
published VersionCount, snapshots inner selector metadata, and classifies it against the actual
emitted operations, including owned helpers/tokens and caller provenance. Monitor reentry, busy
composition and unpublished attempts cannot renew permission. Older bodies carry different tokens.
Protected operations admit void/no-argument inner observations and void postfixes reading one
primitive/enum __result by value (no ref/out); unrelated selected effects remain eligible.
Outer/unchecked and mutable/unknown result bindings are unproven. This is not foreign-code sandboxing.
The helper is not yet used by conversion admission. Independent review and new affected native
evidence remain required; previous minimal-group results do not certify inner-patch compatibility.

The corrected reader copies only the exact published state entry under monitor order
PatchProcessor → state dictionary (nonblocking; at most 4 MiB), then uses the provider's metadata-only
ValidateJsonEnvelope/DeserializePayload. It supports plain JSON and the inspected HARMONY-INFIX
versions 1–5/backend 1. GetPatchInfo's attribute-constructing validation is excluded, including from
expected-version reads. Shape/format drift is unavailable; no dictionary writes or copied serializer.
Alternate-assembly HarmonyOuter markers are recognized by CustomAttributeData full name.
Factory/ownership preflight and receipt inspection use this reader only for NativeLogPatch; other
receipt consumers retain their existing behavior. The owned 4 MiB policy bounds copying/JSON work
with over 6,000-fold headroom over the 698-byte host marker fixture. Near-limit actual published
metadata tests admit 4,190,477 bytes and reject 4,194,573 with an explicit reason. This is not a
provider limit. Signed-byte selector parity is covered at the stream/metadata boundary; this provider's
emitter rejects boxed-byte ldc.i4.s, so executable unsafe admission through that form is not claimed.

`Assembly-CSharp`, public static non-generic `void Verse.Log.Warning(string text)`.
On inspected 1.7.5000, its accepted branch loads `Verse.Log.logCategory` and argument zero,
converts that string through `LogString<Brutal.Logging.Internal.Warning>.op_Implicit(string)`,
then calls instance `LogCategory.Warning(LogString<Warning>, string, string, int)`.
`get_PreventLogging()` controls entry to this branch. Native `WarningOnce` updates `usedKeys`
before forwarding here, and ordinary warning UI effects follow the category write.
Exact current IL is retained at
`artifacts/Implementation/megamod-p0-a/native-1.7.5000-il/Verse.Log.il`.

The candidate transpiler enriches the one string immediately before its native conversion,
after native suppression, without re-emitting a diagnostic. An observer callback is insufficient:
the inspected Waylith formatter writes file output before publishing the callback used for retention.
No global backend writer, third-party patch, owned implementation or once-only method is patched.

## Required executable checks before installation

1. Verify exact declaring type/static/public/non-generic/string/void callable shape.
   Resolve category overloads only after checking the complete message/string-conversion shape;
   unrelated Exception overloads do not create false ambiguity. Require one viable output and
   exactly one viable conversion, then prove their actual incoming use in step 2.
2. In the actual incoming stream, identify one native warning output whose message input derives
   from the supplied text through the exact consumed conversion. Require unambiguous data flow,
   native-suppression control and the native category; unrelated outputs cannot be mistaken for it.
3. Insert only a no-throw string projection, preserving original text verbatim and native trace
   provenance. Preserve original branch targets, labels, exception regions and UI effects.
4. Validate incoming semantics on every transpiler invocation and return the entire incoming stream
   unchanged on missing, ambiguous or incompatible evidence. Do not mutate it before validation.
5. Compose with S035 at its accepted-event boundary; never count context independently or decorate
   protected startup/control notices. Without S035 presentation, admitted native logging suffices.

Compiler offsets/local numbering, harmless intervening instructions and compatible foreign patches
are allowed. Neither full-body hashes nor absence of other owners is a predicate. Skip-original
prefixes naturally prevent the original output from running; they do not cause an extra context
record. Relevant argument changes and effective transpiler changes must satisfy the same actual
data-flow predicates. Running foreign transpilers repeatedly for inspection is prohibited.

## Implemented adapter boundary

[NativeLogPatch](../../Source/Patching/Harmony/NativeLogPatch.cs) and its
[scoped fixtures](../../../../tests/Megamod.Harmony/Patching/NativeLogPatchTests.cs) separate callable/
ownership checks from effective-stream validation in Harmony's normal install pipeline. Every
transpiler invocation rechecks the actual incoming instructions before creating a separate output
list. Failure returns the incoming instructions unchanged and initial installation fails through
C01, removing the inert registration. Callbacks remain unpublished until explicit `Enable` after
their physical group succeeds. S035 shares this route; S094 additionally requires both Warning and
Error through NativeDiagnosticReporting before its decoration can publish.

The supported region loads the native category field, argument zero once, its exact generic-string
conversion, caller member/file strings and integer line, then the resolved category output. It
requires one such output and one argument load, the preceding native bool suppression branch, and
a suppressed return/leave. Every branch/handler entry after the suppression getter through the
output is rejected except the proved suppression branch's accepted edge; adjacency alone cannot
prove that the getter controls the output. Unrelated instructions outside the region, local numbering, constant
values and no-ops are not fixed. Extra entry branches or exception boundaries into the consumed
region, changed argument use, and ambiguous outputs fail closed. This is a narrow native-region
check, not a general equivalent-IL evaluator. Incoming labels/exception blocks are not mutated;
the transform changes a cloned instruction list.

The owned transform has last priority. Before sorted-method inspection, one captured patch array
is checked for the inspected provider's static MethodInfo/DynamicMethod factory shape. Factories
are rejected without invocation. The same snapshot is sorted without running ordinary transpilers;
the owned callback must occur exactly once under the exact acquired owner/target/kind and last.
Check only after installation or explicit `RefreshPublication` at a safe composition boundary,
outside native logging locks. `Available` and the output callback read only lock-free publication
snapshots: querying Harmony's registry while native logging holds its lock can deadlock against a
foreign transpiler that logs while Harmony holds its registry lock.

Every actual rebuild invalidates publication and records a new immutable incoming-validation
identity. A compatible rebuilt route remains inactive until safe refresh checks its final metadata,
rejects factories before sorting, and publishes only if that identity is still current. Refresh does
not enable a group; explicit `Enable` remains the atomic admission owner's responsibility. Failed
input/order checks stay inactive. Projection failure permanently disables this adapter instance.
No foreign transforms are rerun as probes and no global Harmony pipeline patch is added.

Expected-version inspection failures are caught after publication and incoming admission are revoked.
The adapter retains the bounded failure reason and returns the incoming stream unchanged, allowing
another owner's removal to complete even while the previous published metadata exceeds the owned
4 MiB inspection limit. Native output and suppression remain effective; owned projection and capture
stay inactive. Refresh alone cannot admit that rejected generation: a later real compatible rebuild
and explicit safe refresh are required. The actual oversized-owner regression exercises removal,
plain native output/effects, suppression, and restored projection/muting. Focused host RED1/GREEN1
and affected GREEN58 are retained in `artifacts/Implementation/megamod-p0-a/native-log-metadata-fix/`;
independent recheck and affected native acceptance remain pending.

The future runtime owner must establish a safe refresh trigger before wiring: native converter entry
before the context/logging scope is the proposed normal phase, with main-thread maintenance if needed.
Calling conversion from inside arbitrary foreign logging locks is not supported by that trigger;
actual current native phase inspection and acceptance remain required. Concurrent registry ownership
mutations and arbitrary provider semantics are not claimed. Ordinary diagnostics remain available
while context awaits refresh, and previous captured context is never deleted.

Projection exceptions retain the original input and attempt one independent bounded explanation
(256-character reason) per adapter lifetime. Failure of that supplied recorder cannot throw through
the native logger. The production runtime must supply the protected durable recorder and ensure
protected notices never enter ordinary decoration; that composition is still pending.

## Admission, rollback and diagnostics

Early-phase physical `diagnostics.warning-channel`, loaded after C02 globals. S094 still requires
atomic publication with `DDI-REPORT-ERROR` before conversion context can activate. Error rejection
leaves useful admitted S035 warning muting active; see [LOG-CAPTURE-WARNING](LOG-CAPTURE-WARNING.md).
Active original LADE/continuation suppresses S094 overlap but not independent warning capture.
Setting edits affect subsequent occurrences and must not reset logging intervals or once-only keys.

Only newly added owned patches may be rolled back. Emit one bounded startup warning per skipped
enabled patch containing package, patch/spec ID, exact target, failed predicate/reason, unavailable
context and ordinary-warning fallback. Warnings use the protected recorder and are never decorated.
Explicitly disabled candidates are not failure warnings. Formatting failure retains the original
warning and emits at most one independent failure explanation, without recursion.

| Case | Required test / observation | State |
| --- | --- | --- |
| Supported, no-op, ambiguous and changed input | Scoped Harmony stream/patch fixtures | Host tests pass; native pending |
| Skip-original, foreign ordering/factories and supported repatch | Scoped Harmony effective-chain fixtures | Host tests pass; native pending |
| Failed installation, repeated initialization, owned rollback | Admission tests and owned Harmony fixtures | Host tests pass; native pending |
| Fractional-int conversion diagnostic | Actual loader fixture → select/copy native warning + screenshot | Pending |
| Muted/hidden/last-slot/Once/LogLock/protected warning | Focused policy tests, then exact native workflows | Pending |
| Preserved text/trace/file, distinct context repeats | Native inspected details/copy + retained process output | Pending |

The adapter has 18 focused host tests, including bounded lock-inversion and startup/repatch
suppression-bypass regressions; the two accepted P1 fixes await reviewer recheck. Receipts are in
the P0-A TDD ledger. Native binding, protected
composition, executable loader/UI scenarios and reviewed-build in-game observations remain pending.
