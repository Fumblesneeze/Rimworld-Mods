# DT-POLICY-PERSISTENCE: retain owned tolerance choices

Owner: `fumblesneeze.megamod`, S138. Contracts: [policy choice maintenance and malformed saved
choices](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).
Implementation: `Features/DrugTolerance/Harmony/DrugTolerancePersistencePatch.cs` and
`Runtime/NativeDrugTolerancePersistence.cs`. This support group does not enable an editor or veto
drug use. Current 1.7.5005 assembly research SHA256 is
`82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125` (evidence, not admission).

## Target and consumed semantics

Four public declared instance nongeneric void native methods:

| Stable target | Native callable | Consumed boundary |
| --- | --- | --- |
| DT-POLICY-COPY | `RimWorld.DrugPolicyEntry.CopyFrom(DrugPolicyEntry)` | Successful original call copies exact source choice |
| DT-POLICY-SCRIBE | `RimWorld.DrugPolicyEntry.ExposeData()` | Successful original call exposes owned field at the current row node |
| DT-SAVE-FINALIZE | `Verse.ScribeSaver.FinalizeSaving()` | Abort before native successful save finalization after owned failure |
| DT-LOAD-FINALIZE | `Verse.ScribeLoader.FinalizeLoading()` | Abort before native successful load finalization after owned failure |

There is no public extension slot for an
independently owned field, and XML cannot retain per-instance gameplay state. Nothing owned by
megamod is patched.

An outer prefix captures the exact receiver and, for copying, the immutable owned choice of the
final outer-prefix source argument. The owned prefix must sort last. A first outer finalizer commits
the captured copy or scribes the owned field only when the native body actually ran and the call
reached that boundary without an exception. A skipped or failed native call leaves owned state
unchanged and preserves its native exception. This extends the successful callable boundary;
it does not depend on native field-write IL or change any native input/result.

`ExposeData` calls `Scribe_Values.Look<string>` for `megamodTolerance` at the native row's current
Scribe node. LoadingVars imports its raw string (missing = Any); Saving exports it unchanged.
Other Scribe phases do not change owned state. Scribe's existing string escaping handles raw data;
unknown versions remain Needs review with their exact text. No original-mod field is imported.

Native `Scribe_Deep.Look` catches row exceptions and continues. An entry finalizer exception alone
cannot protect the enclosing file. Finalization prefixes inspect the sticky owned support-failure
latch and throw an ordinary InvalidOperationException before finalization, including on retry.
An admitted first finalizer checks again after the native body: the loader's cross-reference and
post-load phases can first encounter and swallow a row failure inside FinalizeLoading itself.
That completion guard propagates an existing native exception unchanged, otherwise returns the
owned abort before the caller can observe successful completion. Save has the symmetric guard.
`SafeSaver` finalizes its temporary file before replacing an existing save; an abort therefore
does not replace that old save. No invented OutOfMemoryException or native catch modification is
used. This is a narrow native finalization boundary extension, with no changed native argument.

## Admission, composition and failure

The C01 `drug-tolerance.persistence` group admits all four targets at Early startup before any policy
factory/copy/save/load uses. Support remains active while enforcement is effectively off. Callbacks
use only entry identity and immutable choices until the native Scribe adapter executes on the
original caller thread; no Unity/Def lookup or main-thread-only service is introduced.

Exact callable shape, owned prefix/finalizer slot ownership, last-prefix and first-finalizer order
are checked with existing shape-guarded `HarmonyPublication` snapshots, never public metadata
parsing that executes foreign attributes. Prefix/finalizer factories make that ordering unavailable;
ordinary unrelated owners and body transpilers remain allowed because no native body instructions
are consumed. No final-body IL claim is made. Argument-writing prefixes are supported before the
owned capture; later postfix argument changes cannot replace the captured source. Foreign exception
handling cannot precede the owned success decision. Callback code may not directly mutate our weak
state outside the declared contract; arbitrary foreign writes are not certifiable. The finalization
guard prefix and completion finalizer must sort first; foreign finalizers must return void and accept no by-reference parameters,
so their ordinary callback cannot suppress or replace the owned abort exception. This additional
restriction concerns only the two finalization targets.

An instruction-preserving generation observer revokes the prior immutable receipt whenever Harmony
recompiles a target. The next surviving callback admits that new publication; normal calls reuse
its receipt without rebuilding metadata. Explicit availability queries refresh all four receipts.
Removed, duplicated or reordered owned callbacks, unreadable metadata, or adapter failure disables
the whole support group and emits one bounded diagnostic. Owned state is not committed after a
generation change during a native call. Native copy and native exceptions stay authoritative;
owned Scribe errors additionally latch a finalization abort until restart. Full removal of every
owned callback cannot self-report until an external availability query and is not claimed
observable. Startup rejection has no installed guard; later unsupported exception-suppressing
foreign code can defeat an abort. Those compositions are unavailable and outside the persistence
guarantee, rather than reported as supported preservation.
The native adapter also sets a one-time atomic pending-notice flag. The existing root lifecycle
pump displays the localized failure dialog on the main thread after native long events finish;
loader callbacks never create windows. It explains that saves/loads are blocked until restart
and that native Saved as feedback is not proof of success after this error. No recurring notice,
automatic repair or preference rewrite is introduced.
No automatic save migration, blanket mod exclusion or foreign unpatching is introduced.

Startup validation/install failure rejects all members and rolls back only acquired owned slots.
Warning identifies the failing stable target, target callable, failure reason, unavailable owned
choice preservation and native policy fallback. Root must not expose/enforce tolerance choices
unless this support and the required enforcement/editor group are all available.

## Verification

Focused Harmony fixtures invoke actual patched host targets for successful copy/expose, native
skip/throw, compatible foreign argument/observer patches, post-call source changes, failed group
installation, idempotence, later reorder/removal, swallowed row failure, finalization abort/retry,
healthy native finalization, failure first swallowed inside finalization, preserved native exception
identity and bounded failures. Native adapter Scribe phases
need real native save/load. The executable isolated scenario must click native policy copy and
save/load, retain exact values/identities as supporting assertions, and capture the visible policy
workflow. Until the tolerance editor exists, hidden-field persistence is component evidence only;
it cannot accept the full visible tolerance feature. The executable scenario is
`megamod.drug-tolerance.native-copy-save`. The separate
`megamod.drug-tolerance.save-failure-preserves-file` scenario repeats that baseline, injects an
owned-field-only test fault, clicks native Overwrite twice, observes the restart notice and exact
old-file hash, and verifies no second notice. Run that ID alone in its fresh process because the
intended failure latch requires restart. Root owns reviewed build/native observation.
`megamod.drug-tolerance.late-load-failure` separately injects a PostLoadInit-only owned-field fault
after a successful native baseline, then clicks the actual native Load button. It requires native
failed-load state, the exact owned notice, and an unchanged source-file hash. It runs alone and
does not mutate disposed Games during cleanup. Both fault scenarios remain unexecuted until the
orchestrator observes them on the reviewed build.
