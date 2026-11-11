# DT-SCHEDULED / DT-DIRECT-JOY: routine selection decisions

Owner: `fumblesneeze.megamod`, S138. Contract: the selection-boundary, native-permission,
direct-recreation-exception and atomic availability requirements in the owning
[spec](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).
Implementation is feature-local; no megamod method is patched. Current native research is
1.7.5005 rev1082, assembly SHA256
`82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125` (evidence only).

| ID | Native callable | Route and inputs |
| --- | --- | --- |
| DT-SCHEDULED | public instance bool `Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow(ThingDef)` | Tracker's actual pawn/current policy and selected drug; no joy exception |
| DT-DIRECT-JOY | protected virtual instance bool `JoyGiver_Ingest.CanIngestForJoy(Pawn,Thing)` | Actual recipient and candidate Thing; positive DrugDesire or mental-state exception only here |

Map `JobGiver_TakeDrugs` and caravan scheduled selection reach ShouldTryToTakeScheduledNow,
which consumes AllowedToTakeScheduledNow before its additional overdose/timing rules. Native
inventory refill remains separate; extra stock attached to a rejected scheduled dose is withheld.
JoyGiver_TakeDrug inherits the observed direct-joy eligibility method; its inventory and map
candidate paths both call it. The runtime must reject a loaded direct drug joy provider that
overrides this boundary without delegating to the admitted target; no general provider compatibility
is claimed. S024 is absent and will call the owned rule directly for its actual treat recipient.

An owned last prefix captures the final native argument identities; a first postfix records the
successful effective native result before other outer postfixes. An owned last finalizer adds
only an AND restriction, preserving native false, other outer rejection and native exceptions.
The extra rule runs only when the original ran successfully and the captured generation survives.
A skipped original or failed capture makes the whole enforcement/editor group unavailable; it
does not manufacture a native result. This observes a callable/result boundary and changes no
native argument or body instruction, so no native body fingerprint/field-write pattern is required.

Exact instance/nongeneric bool shape and reference-argument count/type are resolved before install.
Existing HarmonyPublication supplies metadata and byte-array identity; no foreign attributes or
transpilers are invoked speculatively. Owned last-prefix, first-postfix and last-finalizer order
is required; factories whose ordering cannot be safely inspected are unavailable. An instruction-
preserving observer tracks recompilation. Admitted immutable receipts plus current publication
identity gate normal callbacks; compatible repatches re-admit, removed callbacks revoke use, and
busy metadata withholds the extra restriction. No per-selection metadata reconstruction is used.

These candidates belong to one group with DT-SOCIAL-DRINK, the policy header/row editor and the
native runtime forwarding registration. Persistence CanUseChoices, common effective enablement,
current finalized game and supported UI layout are required before applying any extra veto or
accepting an edit. Installation failure rolls back only newly acquired owned slots. One bounded
diagnostic identifies a failed enabled target and the native-behavior fallback; stored choices
are not rewritten. No final-ingestion veto, job cancellation or persisted job-purpose tag exists.

Harmony fixtures must exercise actual patched callables: native true/false, current extra result,
captured actor/candidate despite later argument changes, skip/throw, compatible outer edits,
repatch/removal, unavailable group and native exceptions. Root's final native scenarios must
observe real scheduled/direct-recreation jobs, tolerance boundaries, exceptions, setting changes,
save/reload and exclusions; host fixtures and installed callbacks are not acceptance.
