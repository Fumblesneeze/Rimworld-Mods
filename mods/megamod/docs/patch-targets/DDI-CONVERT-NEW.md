# DDI-CONVERT-NEW: current definition scope

Owner: `fumblesneeze.megamod`. **Body-scoped adapter composed and independently reviewed. The initial
default-loader log workflow was observed; the shared-helper rerun and broader S094 acceptance remain pending.**
The owning [S094 contract](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md)
requires identifiable conversion diagnostics, nested/exception restoration and ordinary fallback.
Source findings and current native identity are in its [design](../../../../openspec/changes/specify-megamod-definition-diagnostics/design.md).

## Target and prerequisites

Native `Assembly-CSharp`, `Verse.DirectXmlToObjectNew`, public static non-generic
`Verse.Def DefFromNodeNew(System.Xml.XmlNode node, Verse.LoadableXmlAsset loadingAsset)`.
The supplied node is the current conversion; the asset can be null. Current 1.7.5000 calls this
synchronously from `LoadedModManager.ParseAndProcessXML`. Mod construction occurs earlier.
XML mutation or a Def extension point cannot surround conversion errors; a scoped observation hook
is necessary. No owned code is patched and no parsed value/result is changed.

Consumed executable predicates:

1. Resolve the exact declaring type, static/public/non-generic method, argument and return shapes.
2. Establish current-node/asset binding and synchronous invocation lifetime for the observed effective
   surface. Do not use original IL to accept an altered effective conversion/handoff.
3. Enter context only for the admitted conversion invocation. A known skipped original must not
   manufacture conversion context. The chosen hook injects entry inside the actual original body,
   after Harmony's skip-original decision, rather than relying on prefix ordering.
4. Guaranteed restoration preserves the original exception/result. A distinct nested null asset
   replaces provenance with unknown; successful exit, failure and repeated finalization cannot leak
   the prior frame. Retain primitive snapshots, not XML/Def/mod references.
5. Shared `DDI-REPORT-WARNING` and `DDI-REPORT-ERROR` reporting capability is admitted first.

The current pure scope implementation requires entry and token finalization on the same execution
thread. The final native adapter must prove the admitted effective invocation does not defer or
transfer conversion to another thread, and its actual Harmony exit/exception path must dispose the
matching invocation token on that entering thread. Host thread-isolation tests and original-body
inspection do not establish this native predicate. Until executable checks and scoped native
evidence exist, this loader target remains unaccepted.

## Implemented native bridge

[NativeDefinitionConversionPatch](../../Source/Patching/Harmony/NativeDefinitionConversionPatch.cs)
checks the actual incoming instruction stream in the normal Harmony pipeline. It requires one exact
`XmlInheritance.GetResolvedNodeFor(XmlNode)` call fed by argument zero, and one exact native parser
delegate `Invoke(int,int,XmlNode,Type)` fed directly by the same node. Branches or exception boundaries
cannot enter after the proved node load and substitute another value. Argument stores/addresses,
tail calls, jmp, exception filters/fault handlers, unbalanced regions and returns inside exception
regions are unsupported; the inspected native catch/leave/return shape is supported. No full-body
hash, local-number signature, foreign-owner blacklist or foreign probe execution is used.

An owned outer try/finally wraps the actual body, including its existing catch/finally regions.
Every original return stores the native result and leaves through cleanup; native escaping exceptions
also dispose the token. Thus a skip-original prefix creates no context, and finalization runs on the
same synchronous call stack. The two native delegate signatures establish the consumed synchronous
call; arbitrary deferred/transferred foreign conversion is not covered or attributed on another thread.

Each rebuild invalidates publication and records its expected published version, immutable emitted
operations and unique generation token passed into Enter. Actual body entry refreshes final metadata
only at the safe pre-scope boundary under the reviewed HarmonyPublication monitor; the exact owned
callback must be unique and last, and factories are rejected before sorting. Older bodies cannot borrow
newer publication. Candidate, install and rollback receipt inspection use the same safe reader, not
public GetPatchInfo. An incompatible incoming body is returned unchanged. The runtime checks that the
current thread does not hold native Log.logLock before refresh. Unknown foreign locking/context
handoff remains unsupported.

The converter policy protects required resolve/parser argument loads and calls, ignoring harmless
nops when identifying those inputs; it also protects emitted Enter identity/generation and Exit.
Published selectors are resolved against the actual emitted operations, including owned helpers.
The helper retains inner role: a prefix/finalizer on Exit can interrupt disposal; a postfix/finalizer
on Enter can interrupt the acquired-token handoff. These reject context even for otherwise read-only
observer signatures. Void/no-argument Enter prefixes and Exit postfixes remain compatible, as do
ordinary/unrelated observers that preserve required bindings. Their arbitrary exceptions or side
effects are not sandboxed: a pre-acquisition throw creates no scope; a post-disposal throw leaves
context restored. An already acquired token is disposed even after publication or route revocation.
Native and foreign exceptions retain their identity; diagnostics do not repair them.

[DefinitionDiagnosticsRuntime](../../Source/Features/Diagnostics/DefinitionDiagnosticsRuntime.cs)
admits the shared Warning/Error reporting first, then atomically admits both converters and both
native-entry guards as one context capability. Independent warning capture remains available.
Its entry callback refreshes reporting before context capture, uses only immutable settings on workers,
and snapshots known primitive source fields. Protected metadata failures are one-shot and cannot
decorate their own explanation. `DefinitionContextCapture` examines at most 4,096 direct children;
a simple text/CDATA defName is retained, and structured/missing/out-of-bound names remain unknown.
Generated sources with no folder retain an unresolved relative filename, never an invented absolute path.

Offsets, local numbering, unrelated instructions and compatible foreign observers are permitted.
Neither an exact hash/version nor another owner's mere presence is a predicate. The current assembly
hash identifies research only. Probe and installation must not execute a foreign transpiler merely
to inspect it.

## Admission and lifecycle

Register in C01's Early phase, after C02 global settings are loaded and before initial XML conversion.
Active `mlie.logafterdeferror` or `ordpus.logafterdeferror` suppresses owned overlap. An explicit
disabled setting is not a failed candidate. Losing shared reporting disables converter context;
missing trustworthy coverage for either loader disables owned context on both paths. This prevents
an unavailable nested loader from borrowing its parent's identity; ordinary diagnostics remain.

Supported repatching is ordinary Patch/Unpatch on the inspected provider, with exact published-version
and body-token checks. Cross-provider monitors, raw detours and abnormal partial publication failures
remain unsupported; original IL or inventory equality alone never re-admits a body.
Current native hot reload reuses the Mod instance, so admission must be idempotent and invocation
cleanup must work without reconstructing it. Rollback removes only newly installed owned members.

On enabled rejection, one bounded startup warning identifies package, patch/spec ID, exact target,
failed prerequisite, unavailable default-loader context, and ordinary-native-diagnostics fallback.
Use the independent warning recorder, outside definition decoration; never log per definition.

## Verification state

| Case | Required evidence | State |
| --- | --- | --- |
| Nested unknown source, exception and concurrent isolation | Pure context tests plus real wrapper fixtures | Host evidence; native pending |
| Actual direct-node binding and existing catch/finally | Focused Harmony fixtures; owned patch cleanup | Host evidence; native pending |
| Foreign transform, skip-original and final-order rebuild | Focused Harmony fixtures | Host evidence; native pending |
| Initial default XML warning/error | Isolated loader fixture followed by native log select/copy and screenshot | Pending |
| Setting disable/re-enable and hot reload | Native setting action, native reload, inspected changed records | Pending |

Eight [original wrapper cases](../../../../tests/Megamod.Harmony/Patching/NativeDefinitionConversionPatchTests.cs),
the [published-effect cases](../../../../tests/Megamod.Harmony/Patching/NativeDefinitionConversionPublishedEffectsTests.cs)
and existing source-capture Unit evidence use host-owned methods/DOM objects, not live Verse loaders.
The new converter publication revision still requires independent review and affected native scenarios.
Existing XML fixture, native log selection/Copy and toggle/reload scenarios remain available; earlier
native runs do not accept this changed runtime. The new [entry-guard cases](../../../../tests/Megamod.Harmony/Patching/NativeDefinitionConversionGuardTests.cs)
exercise both loader directions with actual scope frames, removed/rejected child bodies, earlier
prefix callbacks and original cleanup. No implementation task is accepted by host results.

Actual-body rejection invokes the owned frame invalidator without a provider read, including stale
body tokens and unavailable acquisition. This prevents a retained wrapper from borrowing a currently
valid parent. Successful acquisition captures the exact immutable two-loader receipt; a changed
generation hides that frame even before a newly earlier native prefix runs. The companion guard target
documents guard-only removal and the explicit unsupported body-removal plus pre-guard callback interval.
