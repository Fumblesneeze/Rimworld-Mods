# MM.CONTENT.IncidentExecuteResult: preserve an incident refusal

Owner `fumblesneeze.megamod`; S038 content availability and preserved existing-world state.
Implemented candidate, centrally composed and independently reviewed; native refusal acceptance remains pending. The bridge is
`Source/Features/ContentSelection/Harmony/IncidentPatchBridge.cs`, validator `IncidentPatchOrder.cs`.
Owning contract: `openspec/changes/specify-megamod-content-selection/`.

## Target and predicates

Postfix of public instance nongeneric `bool RimWorld.IncidentWorker.TryExecute(IncidentParms)` in
Assembly-CSharp. Binding requires the public instance `IncidentDef def` field on that exact type.
The result veto is needed because a foreign prefix can skip the owned refusal prefix while
returning true without executing the native body. Static XML cannot express per-save result vetoes.

The postfix preserves false and changes true to false only for the exact applied excluded identity.
An invocation-local `__state` preserves a refusal already made by the owned prefix even if its
notification callback faults and deactivates the group. No native Def or physical outcome is edited.

The exact callback must occur once under the owned target/kind slot. Actual sorted metadata must
place every potentially result-writing void postfix before this veto; later void observers are allowed.
Non-void pass-through postfixes have no established false-preservation contract and reject the group
regardless of their metadata position. The inspected Harmony provider emits all void postfixes first,
then its pass-through chain; a pass-through writer sorted before this callback still runs after it.
Finalizers accepting writable `__result` are incompatible because they run after postfixes.
Factory-shaped metadata is rejected before sorting, since Harmony sorting can invoke factories.
The checked factory-free immutable patch array is the one passed to the sorter. Foreign transpilers
are never run as an inspection probe. Unrelated owners, IL offsets and native local numbering do
not matter: this patch consumes the wrapper result and identity, not a native body snippet.

## Composition and failure

All five incident patches form `MM.CONTENT.Incidents`, admitted after finalized Defs. The final
installation performs full publication validation; failure throws into the common coordinator's
reverse rollback of all attempted exact owned slots. Until publication and explicit enable, callbacks
are inert. Ordinary native behavior is the fallback; requests persist. Original Cherry Picker active
suppresses the group from the root's canonical active package snapshot, never an installed inventory.

The two Generation observers invalidate availability on rebuild. Explicit refresh runs outside
foreign locks; benign changes can restore the same applied policy without applying pending edits.
Unknown result/identity ordering remains unavailable. Removing the observer itself is not a supported
rebuild notification; refresh catches missing ownership. Callback faults latch this bridge inactive,
report once, and cannot re-enable through metadata refresh. Existing known refusal remains false.

Common admission warning names this patch ID, exact target, target spec, failed predicate,
unavailable incident exclusion and native fallback. It is bounded and never emitted per incident.

## Verification

`IncidentPatchBridgeTests` proves foreign skip/true veto, native effect prevention, native false
preservation, finalizer writer rejection versus observer compatibility, callback faults and normal
rebuild invalidation. The pass-through regression exercises both CanFireNow and TryExecute with a
bool-returning writer sorted before/after the owned veto, plus read-only void result observers in both
positions. Both methods reproduced a true result after an admitted exclusion before the review fix.
`IncidentPatchOrderTests` proves exact owner and factory-safe metadata checks.
These are scoped Harmony host classes with guaranteed owner cleanup, not native acceptance.
Executable native action scenarios are documented in `MM.CONTENT.IncidentExecute.md`; no live run
has been performed. Weighted selection, settings UI/load, original-active and installed-only controls
remain required. Evidence identities belong to the implementation ledger until root accepts a build.
