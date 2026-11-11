# DG-POLICY-CREATE: publish one complete generated policy

**Proposed guarded native adapter; not implemented, registered or accepted.** Complete executable
admission and index this target before runtime wiring. This target describes the native preparation
and publication boundary; the contextual Generate UI requires its own target contract.

## Ownership and traceability

- Owner: `fumblesneeze.megamod`, S139, consuming the existing S029 row/storage and C01 admission paths.
- Owning requirements: [current complete preview and one independent creation](../../../../openspec/changes/specify-megamod-drug-policy-generation/specs/megamod-drug-policy-generation/spec.md).
- Design and exact native evidence: [DG-Q4 research](../refinement/2026-11-11-drug-policy-generation-publication-research.md).
- Kind: guarded native calls/field storage and one direct native database-list append. No mutation
  patch or patch of owned megamod code is proposed here. XML cannot prepare transient editable
  gameplay objects or atomically append a reviewed draft. Native factories publish before copying.
- Implementation/validator/tests: pending; `DrugPolicySuggestions` is only the separate pure C1 model.
- Evidence game: 1.7.5005 rev1082, Assembly-CSharp SHA256
  `82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125`.

## Target and minimal predicates

Assembly-CSharp, exact nongeneric native members:

- `RimWorld.DrugPolicy`: public instance parameterless constructor; base `RimWorld.Policy` protected
  parameterless constructor; private instance `entriesInt : List<DrugPolicyEntry>`; public instance
  `sourceDef : DrugPolicyDef`; inherited public `id : int` and `label : string` fields.
- `RimWorld.DrugPolicyEntry`: public instance parameterless constructor and public instance `drug :
  Verse.ThingDef`, `allowedForAddiction/allowedForJoy/allowScheduled : bool`,
  `daysFrequency/onlyIfMoodBelow/onlyIfJoyBelow : float`, `takeToInventory : int` fields.
- `RimWorld.DrugPolicyDatabase`: public instance nongeneric `get_AllPolicies() : List<DrugPolicy>`
  returning the actual private instance `policies : List<DrugPolicy>` field.

These facts are consumed only for a current loaded game, finalized S029 catalog and current valid
draft. No worker/global scan or invocation from the main menu is supported.

1. Prove parameterless policy/base construction cannot scan Defs, publish/escape the prepared
   receiver or call unadmitted external work. Entry construction must not escape a fresh row or
   touch existing policies. Harmless local initialization/compatible composition may be admitted;
   names, original short bodies and absent foreign owners are insufficient semantic proof.
2. Prove the captured AllPolicies value is the actual current database list and no consumed getter
   performs unrelated mutation or substitutes another list. Bind the field types needed by S029
   storage. Later validation uses captured ordinary storage without extra callback-producing reads.
3. Prove current catalog identity, complete membership and native execution order through S029's
   existing admitted ordering. All prepared RowIdentity values must be null; no old native entry
   can be mutated by preparation. Every displayed native/S138 choice is written and verified.
4. Require a nonempty unchanged list and checked positive noncolliding next ID. Keep existing
   objects, order, default and fields untouched. No pawn CurrentPolicy, default factory, assignment,
   dose/history, native deletion or global template operation participates.
5. The final current-state check, one append and postcheck form a synchronous callback-free section.
   Reentry cannot append twice. Failure before append discards detached state; owned-tail removal
   requires exact prior-list/prefix identity. Do not restore over a detected independent change.

Executable checks are pending; these are obligations rather than claims of current admission.
Permit unrelated IL, local numbering, instruction offsets, other Harmony owners and compatible
observers when the consumed semantics remain established. No whole-body hash or version gate.

## Composition, ordering, and invalidation

Use C01's effective MethodBase publication inspection for consumed native constructors/getter and
its supported later-repatch invalidation. Inspect relevant prefixes, skip-original, transpiler
changes and prepared-object escape, not only original metadata. A foreign observer unrelated to
these semantics is not a reason to disable the feature. An unproved callback that can publish a
prepared object, mutate the database during preparation or bypass final currentness is unsupported.

Reuse S029's content-boundary/currentness receipt and exact catalog identities. A changed Game,
catalog/fact/preference/calculation/S138/admission receipt invalidates the draft before append.
Do not add a separate invalidation registry. The feature-local confirmation receipt and reentry
guard are transient window state, disposed on exit/save change/off; no ambient policy cache exists.
There is no XML merge/replacement and no registration or generated DrugPolicyDef.

## Admission, failure, and diagnostics

Generation enabled/current-game admission must include this complete adapter and its required row
ordering/S138 state semantics. Unavailable creation keeps ordinary policy editing usable, with a
bounded reason. No new policy is created merely to probe support. Missing/changed/ambiguous native
shapes, unreadable effective bodies, exhausted ID range, empty database or stale receipts refuse
before mutation. Unsupported foreign state is neither overwritten nor silently treated as restored.

Example bounded diagnostic:
`[fumblesneeze.megamod] Target DG-POLICY-CREATE unavailable; target=DrugPolicy..ctor(); targetSpec=DG-POLICY-CREATE; predicate=unpublished-construction; reason=unproved-receiver-escape; unavailable=Generate; fallback=native-policy-controls`.
Emit on an admission change, never per pawn/job/tick. Do not remove foreign patches. This proposed
adapter installs no mutation patches of its own; the separate UI group's rollback owns its hooks.

## Verification matrix and final observation

| Case | Expected result | Honest tier | Evidence / state |
| --- | --- | --- | --- |
| Admitted native construction and S029 complete plan | Prepared object absent from database; fresh ordered rows and exact choices/S138 | Defs with scoped constructed fixtures | Pending |
| Duplicate labels, high/negative prior IDs, exhausted next ID | Checked unique positive ID or explicit refusal; no overwrite | Unit controller and scoped Defs | Pending |
| Empty database | Unavailable; no implicit default creation | Scoped Defs/native UI | Pending |
| Preparation failure and post-append failure | No publication or only exact owned-tail rollback; prior prefix unchanged | Scoped Defs | Pending |
| Currentness change or reentry/repeated confirmation | No stale/duplicate append; completed receipt reused | Unit/Defs | Pending |
| Benign foreign constructor/getter edits | Admitted when consumed semantics hold; foreign owner retained | Owned Harmony fixture | Pending |
| Escape, list substitution, skip-original or unproved callback | Refuse unchanged before preparing/publishing; bounded reason | Owned Harmony fixture | Pending |
| Relevant later repatch | Invalidate/revalidate before next creation | Owned Harmony fixture | Pending |
| Generate/edit/Create/repeat/cancel, ordinary copy/save/load | One complete selected policy; prior defaults/assignments/history preserved | Fresh isolated native Gateway workflow | Pending |

Retain reviewed build/package identity, ordered active mods, actual player input, causal captures,
complete relevant logs/config hashes and cleanup. Source inspection and passing fixtures are not
native acceptance. Native persistence and contextual layout remain distinct S138/UI obligations.
