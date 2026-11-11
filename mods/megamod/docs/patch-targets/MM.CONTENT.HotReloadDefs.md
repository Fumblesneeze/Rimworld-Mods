# MM.CONTENT.HotReloadDefs: invalidate incident policy before Def rebuilding

Owner fumblesneeze.megamod, S038 Content selection and S029 drug preset baseline. Native Def rebuilding
invalidates both consumers. Design-time Harmony fallback: there is no public finalized-generation notification,
and XML cannot observe runtime reload. This is an incomplete safety milestone, not recovery support.

## Target and admission

Public static nongeneric void Verse.PlayDataLoader.HotReloadDefs(), declared in Assembly-CSharp,
zero arguments, readable method body, nongeneric declaring type. NativeContentInvalidationPatch
validates the exact callable and acquires one OwnedHarmonyPatch prefix receipt. The atomic
MM.CONTENT.DefinitionLifecycle group also requires ClearAllPlayData. Root registers once at
DefsReady, before publishing the initial drug baseline or incident availability. Missing/changed shape, unreadable publication
metadata or an already-owned prefix slot reject the group before mutation. Install failure rolls back
only acquired owned receipts through the shared coordinator. Source hashes are evidence, not guards.

Current evidence: RimWorld 1.7.5000 rev1275 Assembly-CSharp SHA256
D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254.
Retained C# and IL: artifacts/Megamod/P0-C/native-1.7.5000/Verse.PlayDataLoader.

## Exact observed behavior and composition

The prefix only atomically latches the first observed entry name. It neither touches Store/native
Defs/UI nor changes arguments, return value or exceptions. It runs before the native body and
conservatively invalidates even when a foreign prefix skips that body. Other observers/skip prefixes
and unrelated body edits are permitted; no whole-method fingerprint or blanket foreign-owner ban.
It cannot certify arbitrary foreign code that mutates Defs before entering this target.

Ownership inspection now uses the existing immutable publication reader and filters to the acquired
owner before projecting a receipt. It does not sort foreign callbacks, construct their attributes or
invoke their factories. Unrelated factory-shaped patches are allowed because this observer consumes
only its own receipt. Busy metadata withholds admission through the common coordinator; no metadata
is read by the native entry callback. Independent scoped review is CLEAR after six affected tests,
including a genuine compatible-factory rejection RED followed by GREEN. Exact hashes and receipts:
artifacts/Implementation/megamod-p0/content-invalidator-reader-handoff.md. Root now creates/adopts this
one instance before initial drug-baseline publication and injects it into the later incident runtime.
That wiring is independently reviewed and compiles; the host observer tests do not prove its native
startup timing or either consumer's live invalidation workflow.

Native HotReloadDefs captures body-part indices then queues a synchronous long event. The outer
return is not completion. Its generated body b__3 loads mods with hotReload true, flushes pending
copy actions, resolves Defs and queues b__4 finishing work for comps/body parts/maps/windows.
Loaded remains true. LongEventHandler swallows body and finishing-action failures independently;
Loaded plus an idle queue is not a success receipt. No owned action is queued through
ExecuteWhenFinished for reload recovery, because it can execute immediately on the calling thread.
The separately documented initial post-CallAll callback only publishes the first baseline; it does
not clear this latch or certify a later reload.

The runtime reads the atomic latch on every policy lookup, including incident callbacks; it never
supplies the retained old policy as available after invalidation. The main-thread pump disables the
bridge, emits one protected bounded explanation, and leaves stored requests/prior immutable key
decisions intact. The UI disables rows with the localized Def-lifecycle reason. Ordinary load, idle
frames and patch refresh do not clear the latch. Same-instance catalog/UI recovery with a proven
successful generation remains open; no restart-only completion claim is made.

Coordinator-owned rollback withdraws group availability. This milestone does not claim automatic
revalidation after foreign code removes owned observers; there is no compilation-observer protocol
for this new group yet. Do not infer that later destructive patch removal is supported.

## Verification and diagnostics

NativeContentInvalidationPatchTests uses an ordinary static host loader with individually owned
Harmony patches: two entry cases prove pre-body invalidation and preserved native effects; worker
exception/repeated entry retains the first latch; foreign skip remains conservative; changed second
callable rejects the whole group. Guaranteed cleanup removes only acquired test owners. The 5/5
GREEN and actual 2-case RED are in artifacts/Megamod/P0-C/handoff-09.md. No loaded mods/Defs are faked.

Shared admission warns once per enabled skipped patch, including ID/target/spec/failed predicate,
unavailable incident behavior and native fallback. Runtime invalidation uses the supplied protected
sink once, not mutable Log.Warning. No per-frame warnings. The native developer action and visible
disabled-row workflow in DefReload-Verification.md remains unrun; host GREEN is not acceptance.
