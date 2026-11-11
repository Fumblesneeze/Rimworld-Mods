# DDI-GUARD-NEW: default converter entry coverage

Owner fumblesneeze.megamod, S094. Implemented, host-tested and independently reviewed; native acceptance pending.
Target: Verse.DirectXmlToObjectNew.DefFromNodeNew(XmlNode, LoadableXmlAsset), public static Def.
Owning observable contract: [nested loading coverage](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md).

A void native prefix establishes coverage before actual nested conversion. It does not create a
definition context or change arguments/results. Together with DDI-GUARD-LEGACY and both body adapters,
this is one atomic context capability, independent of S035 Warning capture. Require exact callable,
unique acquired own prefix, factory-free inspected sorting and own prefix first on BOTH targets.
Callbacks earlier than the guard could enter the other loader; guard presence alone is insufficient.
Inspect both targets within one provider publication interval; no probe callback execution.

On rejected nested coverage, mark the current frame non-visible without popping it. Its existing
cleanup token still restores its caller; the rejected child needs no trusted cleanup. Parent remainder
uses ordinary diagnostics. Native Log-lock-held entry suppresses context without provider inspection.
Project reads only thread-local visibility and the frame's captured pair-validity check. Availability
requires both exact captured body-generation identities, not non-null individual publications.
Guard-only removal rebuilds the body and invalidates the pair. A fresh compatible pair cannot revive
an older active frame. Rejected body entry hides the frame even for a retained older wrapper.
Removed body transpilers are observed on fresh guard inspection, which every nested entry requires.
An unchanged top-level entry may reuse its cached receipt and defer that inspection. Removing a body transpiler while
placing a reporting callback ahead of the guard leaves an unsupported pre-guard interval. Complete
removal of all observers is also unsupported, not silently detectable. Initial prefix ordering remains
strictly checked; there is no metadata read under Log.logLock or additional observer layer.
Exact rollback preserves foreign prefixes and other owned targets/kinds. Enabled rejection uses C01
bounded startup warnings with ordinary diagnostics fallback. Both-direction real Harmony fixtures and
fresh reviewed default/legacy native loading remain required.

Implementation and validator: [NativeDefinitionConversionGuard](../../Source/Patching/Harmony/NativeDefinitionConversionGuard.cs),
using [paired published metadata](../../Source/Patching/Harmony/HarmonyPublication.cs) and
[non-visible scope frames](../../Source/Features/Diagnostics/Core/DefinitionContextScope.cs).
Focused [real-provider guard cases](../../../../tests/Megamod.Harmony/Patching/NativeDefinitionConversionGuardTests.cs)
and [pair interval checks](../../../../tests/Megamod.Harmony/Patching/HarmonyPublicationPairTests.cs) are
supporting host evidence, not native acceptance. This revised runtime requires fresh affected workflows.
