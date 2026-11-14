# DT-DIRECT-JOY and shared scheduled dependency: routine selection decisions

Owner: `fumblesneeze.megamod`, S138. Contract: the selection-boundary, native-permission,
direct-recreation-exception and atomic availability requirements in the owning
[spec](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).
Implementation is feature-local; no megamod method is patched. Current native research is
1.7.5005 rev1082, assembly SHA256
`82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125` (evidence only).

| ID | Native callable | Route and inputs |
| --- | --- | --- |
| DT-SCHEDULED-DEPENDENCY | Shared `DRUG-SCHEDULED-COMPOSITION` receipt for `Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow(ThingDef)` | Read-only dependency; actual pawn/current policy and selected drug, no joy exception |
| DT-DIRECT-JOY | protected virtual instance bool `JoyGiver_Ingest.CanIngestForJoy(Pawn,Thing)` | Actual recipient and candidate Thing; positive DrugDesire or mental-state exception only here |

Map `JobGiver_TakeDrugs` and caravan scheduled selection reach ShouldTryToTakeScheduledNow,
which consumes AllowedToTakeScheduledNow before its additional overdose/timing rules. Native
inventory refill remains separate; extra stock attached to a rejected scheduled dose is withheld.
JoyGiver_TakeDrug inherits the observed direct-joy eligibility method; its inventory and map
candidate paths both call it. The runtime must reject a loaded direct drug joy provider that
overrides this boundary without delegating to the admitted target. The initial adapter admits
the inherited method identity; it does not yet prove override delegation and therefore leaves
such a provider unavailable. No general provider compatibility is claimed. S024 scheduled mood
is composed by the [shared scheduled owner](DRUG-SCHEDULED-COMPOSITION.md). Its future patient-treat
selector remains unimplemented and may call the owned rule directly for its actual recipient.

Inherited decision lookup compares module, metadata token and exact declaring type for the
nongeneric native method. Its reflected worker type is not declaration identity: the ordinary
TakeDrug worker inherits CanIngestForJoy. A different override or hidden method remains unavailable,
including an override that calls base; GetBaseDefinition alone does not prove delegation semantics.

An owned last prefix captures the final native argument identities; a first postfix records the
successful effective native result before other outer postfixes. An owned last finalizer adds
only an AND restriction, preserving native false, other outer rejection and native exceptions.
The extra rule runs only when the original ran successfully and the captured generation survives.
A skipped original or failed capture makes the whole enforcement/editor group unavailable; it
does not manufacture a native result. Scheduled use observes only this callable/result boundary
and needs no body fingerprint. Direct joy additionally consumes the native exception meaning:
its incoming body must retain the final story/traits, positive DrugDesire or mental-state decision,
with the same true/false destinations and unmodified pawn argument. A changed threshold or a
branch into a partial predicate is unavailable. The guard matches this consumed suffix rather
than the whole native method; no foreign transpiler is replayed to obtain it. Its observer must
be last, and final provider inner effects cannot rewrite the inspected body generation.

Exact instance/nongeneric bool shape and reference-argument count/type are resolved before install.
Existing HarmonyPublication supplies metadata and byte-array identity; no foreign attributes or
transpilers are invoked speculatively. Owned last-prefix, first-postfix and last-finalizer order
is required; factories whose ordering cannot be safely inspected are unavailable. An instruction-
preserving observer tracks recompilation. Admitted immutable receipts plus current publication
identity gate normal callbacks; compatible repatches re-admit, removed callbacks revoke use, and
busy metadata withholds the extra restriction. No per-selection metadata reconstruction is used.

`NativeDrugToleranceRuntime` forwards the exact current tracker/pawn and candidate to the owned
reader and rule. It resolves the current Game database, exact policy membership and unique current
row by reference, rather than policy/drug names. A mapped but unreadable current pawn value retains
native permission and emits one localized player message and diagnostic per Game; weak notice
tracking does not retain that Game or cache permission. An unsupported drug mapping is a disabled
Unavailable row, with its choice retained. Direct joy exceptions stay on the direct route, and
neither scheduled nor social selection imports them.

The direct-joy candidate and read-only shared scheduled dependency belong to one group with DT-SOCIAL-DRINK, the policy header/row editor and the
native runtime forwarding registration. Persistence CanUseChoices, common effective enablement,
current finalized game and supported UI layout are required before applying any extra veto or
accepting an edit. Installation failure rolls back only newly acquired owned slots. One bounded
diagnostic identifies a failed enabled target and the native-behavior fallback; stored choices
are not rewritten. Its dependency rollback never removes the shared scheduled owner's callbacks.
S138-only failure leaves supported S024 scheduled mood independent; shared-boundary failure still
invalidates the whole S138 capability. No final-ingestion veto, job cancellation or persisted job-purpose tag exists.

Harmony fixtures must exercise actual patched callables: native true/false, current extra result,
captured actor/candidate despite later argument changes, skip/throw, compatible outer edits,
repatch/removal, unavailable group and native exceptions. Root's final native scenarios must
observe real scheduled/direct-recreation jobs, tolerance boundaries, exceptions, setting changes,
save/reload and exclusions; host fixtures and installed callbacks are not acceptance.
