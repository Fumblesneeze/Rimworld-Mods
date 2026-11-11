# DDI-CONVERT-LEGACY: legacy definition scope

Owner: `fumblesneeze.megamod`. **Body-scoped adapter composed and independently reviewed;
native legacy-loader acceptance remains pending.**
Owning [S094 contract](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md):
truthful current conversion context, unchanged conversion outcome, restoration and safe fallback.

Native `Assembly-CSharp`, `Verse.DirectXmlLoader`, public static non-generic
`Verse.Def DefFromNode(System.Xml.XmlNode node, Verse.LoadableXmlAsset loadingAsset)`.
Current native `LoadedModManager.ParseAndProcessXML` selects this path with the
`legacy-xml-deserializer` command-line switch. The matching 1.7.5000 original-body inspection is
retained under `artifacts/Implementation/megamod-p0-a/native-1.7.5000/`.

The exact callable/current-node/synchronous-lifetime, skip-original, exception restoration,
immutable nested-source and shared-reporting predicates are the same required guarantees described
in [DDI-CONVERT-NEW](DDI-CONVERT-NEW.md); this is a separately inspected target in the atomic S094
context family. Its validator resolves and inspects this method, not inferring compatibility from the new
loader passing. The actual incoming body uses the exact `Func<XmlNode,bool,object>.Invoke(node,true)`
anchor; its node input must not be replaceable through branch/handler entry. The chosen owned
outer try/finally starts inside the actual original body, so skip-original never creates a scope.
No XML/Def or public extension seam
supplies conversion lifetime. No parsed fields, return values or exceptions may be changed.

Admission occurs in Early after common globals and before initial conversion. Original providers
`mlie.logafterdeferror` and `ordpus.logafterdeferror` suppress owned overlap. Disabled settings are
silent; an enabled rejected patch logs one bounded package/patch/spec/target/prerequisite warning,
describing unavailable context and ordinary diagnostics fallback. Either loader's missing trusted
entry coverage disables owned context on both paths; independent S035 warning capture remains.

Allow unrelated instructions, compiler locals and compatible foreign patches. No hash/version or
owner blacklist replaces semantic admission. Account for effective code without executing foreign
transpilers as probes. The shared converter now checks published version, emitted body token and
phase-sensitive inner effects using the safe metadata reader described in DDI-CONVERT-NEW. Entry
cannot borrow a newer body's permission, and already acquired tokens remain disposable after
revocation. Prefix/finalizer effects on Exit and postfix/finalizer effects on Enter cannot be accepted
as ordinary observers: they can interrupt disposal or lose the newly acquired token. Compatible
observations and unrelated effects remain eligible. This migration is host-tested, awaiting review
and fresh native evidence. Unsupported later changes are not covered. Repeated initialization and native hot reload must not
install duplicate callbacks. Cleanup only removes newly added owned patches.

The pure scope requires entry and finalization on the same synchronous execution thread. The
eventual effective-loader predicate and real Harmony exit/exception path must establish that
contract; original-body inspection and host scope tests are not proof of the native handoff.

| Case | Required tier / observation | State |
| --- | --- | --- |
| Exact, benign and incompatible target | Scoped Harmony fixture and executable validator | Pending |
| Nested, skipped and exceptional invocation | Scoped Harmony fixture; no stale context | Pending |
| Initial legacy warning/error | Separate isolated legacy loader run → native record select/copy/screenshot | Pending |
| Legacy rejected leaves both loaders' diagnostics ordinary | Focused atomic admission test plus isolated native fallback | Pending |
| Original provider and repeated reload | Exact provider run and native reload; no duplicate context | Pending |

The shared [native bridge](../../Source/Patching/Harmony/NativeDefinitionConversionPatch.cs), runtime
composition and scoped host cases are described in DDI-CONVERT-NEW. Legacy target validation is
separate within atomic context coverage. Native fixture execution requires its own isolated legacy-deserializer process; host
cases, original IL inspection and this document do not accept that integration.

The same rejected-entry invalidation and frame-captured pair authority apply to legacy Enter.
Retained wrappers cannot borrow a newer parent's context, and individual legacy publication cannot
admit context after its guard was removed. Both-direction host cases preserve existing outer cleanup;
the DDI-GUARD-NEW removal/pre-guard limitation remains explicit rather than implying arbitrary unpatch
detection.
