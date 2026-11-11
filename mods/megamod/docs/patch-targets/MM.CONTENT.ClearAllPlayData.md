# MM.CONTENT.ClearAllPlayData: invalidate incident policy before Def clearing

Owner fumblesneeze.megamod, S038 Content selection and S029 drug preset baseline. Native Def rebuilding
invalidates both consumers. Prefix observation of exact public static nongeneric void
Verse.PlayDataLoader.ClearAllPlayData(), zero parameters, declared readable body in Assembly-CSharp.
The native method clears language/mod data and all Def databases before setting Current.Game=null
and Loaded=false. Observing the latter values alone is too late to invalidate old references.

This is the second required member of MM.CONTENT.DefinitionLifecycle, admitted at DefsReady through
NativeContentInvalidationPatch / shared AdmissionCoordinator / OwnedHarmonyPatch. XML cannot observe
this boundary and native code exposes no before-clear event. Missing/changed callable, unreadable
publication metadata or an occupied owned prefix slot rejects the atomic group. Unrelated foreign
factories are allowed; the shared reader inspects only the acquired receipt without executing them.
Successful native entry
only latches an immutable reason; no Store/native Def/UI access or cancellation occurs. Native
exceptions remain native. Other prefixes and unrelated body edits are compatible, including skips
that conservatively invalidate. No body offsets, local numbering or complete hashes are predicates.

See MM.CONTENT.HotReloadDefs.md for exact game hash, source paths, shared warning/rollback behavior,
later foreign-owned-observer-removal limits and the six scoped Harmony tests. Both IDs get their
own shared bounded warning if skipped. Returning, loading a save or idling never restores availability.
The existing AppliedContentPolicy retains its immutable stable-key decisions for future coherent
reconciliation; requested settings are not recopied or reset. Reconstructing the runtime, registry
or patch group is forbidden. Full play-data recovery and changed active-mod inventory require the
central owner to supply a new proven boundary; this milestone does not claim that recovery.

Native clear/reload acceptance and actual cleanup-path integration remain pending. The normal
player workflow must be identified from the inspected clear caller before that scenario is closed;
directly invoking ClearAllPlayData through diagnostic C# would not replace native workflow evidence.
