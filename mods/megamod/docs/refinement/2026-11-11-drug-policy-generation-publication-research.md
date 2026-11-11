# S139 DG-Q4: detached native policy preparation and publication

Owner: `fumblesneeze.megamod`, S139, consuming S029/C01/S138. Read-only native inspection on
2026-11-11; proposed contract awaiting independent review and executable proof. This document does
not claim that native creation, finalized data admission, persistence or any player workflow passed.

## Exact inspected input and method facts

Installed game: RimWorld **1.7.5005 rev1082**, `F:/Steam/steamapps/common/RimWorld`.
`Assembly-CSharp.dll` SHA256
`82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125`, rechecked for this inspection.
The retained `artifacts/Tools/ilspycmd/ilspycmd.cmd` wrapper uses ILSpy command-line 11.2.0.10050.
Each named type was decompiled directly from that read-only assembly. These identities identify
the research input; neither its version nor hash is a runtime admission rule. No source algorithm
or downloaded assembly is copied into the product.

| Native member | Observed behavior | Consequence |
| --- | --- | --- |
| `Policy..ctor()`; `DrugPolicy..ctor()` | Parameterless base and derived bodies perform no initialization beyond base construction; no catalog scan or database access. | A native policy can be constructed while unpublished. Effective constructor semantics still need admission. |
| `Policy.id`, `Policy.label`; `DrugPolicy.sourceDef` | Public instance fields. | A prepared object can receive the final identity/name with no shared template; sourceDef remains null. |
| `Policy.GetUniqueLoadID()` | Combines virtual LoadKey, label and ID; DrugPolicy's key is `DrugPolicy`. | Name alone is not identity; choose a positive noncolliding native ID. No load-ID helper must run during the atomic section. |
| `DrugPolicy..ctor(int,string)` / `InitializeIfNeeded` | Initializes entries from the global finalized Item/IsDrug Def list, enables addiction, and sorts by drug-comp listOrder. | This constructor does not consume the current S029 available catalog and can throw on a missing comp; do not use it for creation preparation. |
| `DrugPolicy.entriesInt` | Private `List<DrugPolicyEntry>`; the parameterless object initially has null entries. | Existing S029 storage can attach a complete prepared entry list. Do not expose the object before that happens. |
| `DrugPolicyEntry..ctor()` | Field initializers set frequency/mood/joy to 1; ordinary bool/int/reference defaults remain zero/false/null. | Fresh entries are usable when every intended field is written. Native raw addiction false is not S029's normalized default. |
| `DrugPolicyDatabase.AllPolicies` | Public getter returns the actual private `List<DrugPolicy>` field. | Capture its exact identity and append to that list after semantic admission; no separate database cache is needed. |
| `MakeNewDrugPolicy()` | Computes max ID + 1 without explicit checked overflow, constructs with the initializing constructor, appends immediately and returns. | Inappropriate preparation/publication boundary. Use checked widened ID arithmetic instead. |
| `NewDrugPolicyFromDef(DrugPolicyDef)` | Calls MakeNew first, then writes label/source and performs later throwing row lookups/copies. | Catching failure cannot make this an unpublished factory. No temporary global Def/template is needed. |
| `DefaultDrugPolicy()` | Creates a new policy when the list is empty; otherwise returns element zero. | Read the existing first element directly. An empty database cannot satisfy unchanged-default creation. |
| `SetDefault(DrugPolicy)` | Swaps the selected policy into element zero. | Appending to a nonempty unchanged prefix preserves the default without this method. |
| `Pawn_DrugPolicyTracker.CurrentPolicy` getter | Can fill null curPolicy from the native default; mutant policy handling is another branch. | Do not read this getter to take supposedly read-only snapshots. The writer does not inspect or assign pawn policies. |
| `TryDelete(DrugPolicy)` | Reads pawn CurrentPolicy, refuses in-use policies, and clears other references before removing. | Not an owned-append rollback primitive; it can touch unrelated assignment state. |
| `DrugPolicy.CopyFrom`, `ExposeData` | Copy constructs/copies mutable entries; load initializes missing rows during PostLoadInit. | Ordinary later copy/load is a separate S029/S138 integration obligation, not preparation. |

## Existing owned consumer boundary

`Features/DrugPolicyMaintenance/Core/DrugPolicyReconciler.cs` already produces complete normalized
row plans from the current `DrugCatalogEntry` identities and an admitted native-order callback.
`Plan(null, catalog, ordering)` produces new rows whose `RowIdentity` is null. Final draft choices
can be overlaid by canonical drug identity after input validation; preserve the planned order and
complete membership. Every output row must retain a null RowIdentity.

`Features/DrugPolicyMaintenance/Runtime/NativeDrugPolicyStorage.cs` already owns the private native
entry-list field. Its Apply creates a `DrugPolicyEntry` for a null RowIdentity, writes all seven
native choices, writes S138 through `NativeDrugToleranceState`, and attaches the completed new list
only after iterating all rows. If a nonnull RowIdentity is supplied it reuses and mutates that entry:
**the generator must reject borrowed row identities**, even when they appear to have matching values.
With fresh entries and an unpublished policy, earlier constructed objects/weak S138 side-state are
unreachable from the database if preparation throws. No existing policy is a rollback target.

`DrugPolicyBatch` remains reconciliation over captured storages; it is not an arbitrary edited-row
commit API and must not be used to rewrite prior policies. S139 should consume the existing plan and
storage directly, with a small feature-local creation controller and the existing S029 content-
boundary/currentness receipt. Do not introduce a second row store, global transaction registry,
ambient current draft or retained pawn cache.

## Chosen publication protocol

1. Require the game thread, current nonempty native policy list, a complete current catalog and
   a valid draft confirmation. Capture Game/database/list identity, ordered prior policy identities,
   ID/label/source and entry snapshots, plus current catalog/fact/calculation/preference and required
   S138/C01 receipts. The existing default is list element zero. Capture only actual storage; no
   native default/assignment getter is needed. The operation never writes pawn trackers/history.
2. Choose checked `max(0, prior policy IDs) + 1` in widened arithmetic. A result above Int32.MaxValue
   is an explicit unavailable reason; never wrap or adopt the native unchecked behavior. Reject a
   malformed current list that cannot be fully inspected. Validate the editable name separately.
3. Construct an admitted parameterless DrugPolicy. Set its ID, label and null sourceDef. Build a
   complete S029 plan with fresh identities and overlay the validated displayed choices, including
   Any when S138 is unavailable. Apply that plan to this detached policy through native storage.
4. Verify all prepared rows and choices, native order, fresh object identities, S138 values and
   exact current-state receipts/snapshots. Refuse stale/reentrant work. Constructor/getter semantics
   must prove there was no prepared-object escape or unrollbackable callback; a later relevant
   repatch invalidates this admission. No unsupported foreign side-effect rollback is promised.
5. In one synchronous no-callback section, perform the final current-state check, one ordinary
   List.Add of the complete object, and the postcheck: unchanged prior prefix/order/values/default,
   one owned tail, and the fully prepared row state. An immutable success receipt can then allow
   the owning window to select the result. No factory, native getter, event callback or UI selection
   runs between final validation and postcheck. No async yield or autosave occurs.
6. A pre-append failure discards detached objects only. For an ordinary post-append failure, remove
   only the exact owned tail after confirming the same list and original prefix; verify restoration.
   Do not call TryDelete or restore over independent changes. Unexpected foreign mutation means
   conflict and invalid admission, never a claim of restored state. The admitted native path has
   no such callbacks/interleaving; it does not support arbitrary concurrent mutation of game lists.
7. A window-local one-shot confirmation records Pending/Completed/Failed ownership. Reentry during
   preparation cannot append; repeat delivery after success returns the same completion receipt
   without appending again. Failed/stale actions require a fresh explicit preview/reset. New deliberate
   workflows may create another policy with the same label. Dispose state with the window/game.

Empty-database refusal is deliberate: an append would become element zero and thereby establish
a new default. Ordinary native policy controls can establish one; generation does not manufacture
an extra default or silently broaden the player's requested action. Identity exhaustion is similarly
an unavailable boundary, not an excuse to recycle IDs or overwrite an old policy.

## Admission and remaining proof

The consumed native constructor/getter/field predicates are in
[DG-POLICY-CREATE](../patch-targets/DG-POLICY-CREATE.md). C01 already accepts MethodBase publication
inspection, so constructors do not justify a parallel patch registry. The implementation must still
prove effective-call semantics and supported invalidation, including compatible foreign work; the
fact that an original constructor is short does not establish its effective runtime behavior.

This is a concrete static contract, not the implementation of task 1.4. Focused honest Defs tests
must cover detached complete rows, current identity/order/values/S138, invalid/stale/duplicate
confirmation, construction failures and owned-tail rollback. Constructed Def fixtures remain in
the initially empty scoped database; no loaded-Core or fake-active-mod claims. Relevant actual
foreign-constructor/getter changes need scoped Harmony cases with guaranteed owner cleanup.
Publication and UI acceptance still need an isolated native Generate → edit → Create workflow,
repeat/cancel/reset, unchanged prior policies/default/assignments/history, and ordinary copy/save/load
on the reviewed build. The existing C1 Unit tranche does not satisfy any of those native obligations.
