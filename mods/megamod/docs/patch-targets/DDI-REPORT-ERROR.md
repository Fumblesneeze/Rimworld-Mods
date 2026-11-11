# DDI-REPORT-ERROR: one contextual error occurrence

Owner: `fumblesneeze.megamod`. **Implemented-unverified and registered by the shared bootstrap;
the native-discovered overload-admission correction passed review and the initial default-loader
error workflow was observed. Broader native acceptance remains pending.**
Owning [S094 contract](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md):
one accepted diagnostic with original severity/text/trace, truthful conversion context and native
failure handling. It composes directly with S035's ordinary capture rather than an extra Message.

## Target and consumed semantics

`Assembly-CSharp`, public static non-generic `void Verse.Log.Error(string text)`.
Current 1.7.5000 checks `get_PreventLogging`, then projects argument zero through
`LogString<Brutal.Logging.Internal.Error>.op_Implicit(string)` into instance
`LogCategory.Error(LogString<Error>, string, string, int)`. Native `ErrorOnce` applies its identity
before forwarding. After output, native reentrancy handling, pause-on-error, PostMessage and
exception/finally cleanup remain unchanged.

The category also has `Error(Exception, string, string, int)`. Identify candidates using the complete
message/conversion callable prerequisites before checking uniqueness; that overload is unrelated to
the supplied-string route. Preflight conservatively rejects multiple viable outputs in the reflected
inventory, even when the body consumes only one, or multiple valid conversions for the selected
message type. The inventory check does not prove competing consumed semantics and does not claim
support for every benign valid-but-unused overload combination.
The actual incoming IL must still consume the selected conversion and output exactly as documented.

This method also contains a distinct category Error call for failures while logging. A matcher must
identify only the original supplied-text output; selecting all Error calls or relying on the source
line number would be incorrect. Exact IL lives in
`artifacts/Implementation/megamod-p0-a/native-1.7.5000-il/Verse.Log.il`.

Apply the callable, actual incoming string/data-flow, suppression/control-flow, unchanged failed
stream, no-throw projection, effective ordering and later-repatch requirements in
[DDI-REPORT-WARNING](DDI-REPORT-WARNING.md) to this exact Error target. The shared transpiler
must preserve native trace provenance and exception handling without calling Log.Error again.
No source code, foreign patch or megamod method is patched. XML and an after-emission callback cannot
satisfy the pre-output single-occurrence contract.

## Admission and failure boundary

The shared NativeLogPatch now applies the published-version/body-token and immutable inner-effect
assessment described by DDI-REPORT-WARNING to this target too. Relevant changes to native suppression,
projection or output/provenance fail closed; unrelated effects and compatible observations are not
rejected by owner. Output callbacks never acquire the provider metadata monitor. This new C01 slice
awaits independent review and affected native verification; converter admission remains unchanged.

Its publication reader shares DDI-REPORT-WARNING's bounded read-only state/envelope boundary and
provider-owned metadata-only payload reader. It never calls the attribute-constructing GetPatchInfo
validation path; changed provider shape/format leaves this route unavailable.

Early-phase physical `diagnostics.definition-error`, with atomic S094 publication requiring this
route plus DDI-REPORT-WARNING before converter contexts can activate. If either route is unavailable,
S094 falls back to native output without context; an admitted Warning route may still serve S035
muting independently. The former physical pair rollback is superseded by shared-consumer ownership.
Per-patch
startup warnings name package, patch/spec ID, exact target, failed prerequisite, unavailable context
and fallback; deduplicate them and use the protected route. Disabled settings are not failed
candidates. Provider suppression uses active canonical LADE identities, never installed-only state.
Preserve independent debug-log groups and remove only newly installed owned patches on failure.

Compatible foreign patches/unrelated edits remain allowed; exact hash/version and owner presence are
not guards. Known skipped-original paths cannot create an owned occurrence. Supported later-patching
ordering/revalidation is covered by scoped host fixtures under the warning target's explicit support
limits. Native caller, stack/file output, Once and full runtime composition still require isolated proof.

| Case | Required test / observation | State |
| --- | --- | --- |
| Two category Error calls, only one supplied-text output | Scoped Harmony structural fixture | Host lock/finally fixture passes; native pending |
| Unrelated Exception overload alongside the consumed string output | Scoped Harmony overload regression | Actual RED1/GREEN1; native rerun pending |
| Multiple convertible outputs in reflected inventory, one consumed | Scoped Harmony admission fixture | Conservative rejection preserves native output; native pending |
| Changed/ambiguous flow and benign edits | Scoped Harmony admission fixtures | Host tests pass; native pending |
| Exception/reentrancy/finalizer and native Once suppression | Scoped Harmony fixtures then isolated native run | Pending |
| Invalid declared Def type and caught conversion exception | Real loader fixture → native select/copy/screenshot | Pending |
| Hidden/muted/capped error with pause and auto-open | Native settings/action/result workflow | Pending |
| Failed group rollback and independent warning fallback | Owned-patch fixture plus isolated rejected-target workflow | Pending |

Implementation and tests are linked in the shared warning target. Native scenarios are authored;
reviewed-build action/result evidence and acceptance remain pending.
