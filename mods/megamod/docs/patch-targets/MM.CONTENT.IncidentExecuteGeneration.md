# MM.CONTENT.IncidentExecuteGeneration: revoke stale incident execution

Owner `fumblesneeze.megamod`, S038; owning contract
`openspec/changes/specify-megamod-content-selection/`. Candidate composed and independently
reviewed; native repatch acceptance remains pending. Implementation: `Source/Features/ContentSelection/Harmony/IncidentPatchBridge.cs`.

## Target and minimal predicates

Compilation-observer transpiler on public instance nongeneric
`bool RimWorld.IncidentWorker.TryExecute(IncidentParms)`, paired with the public instance
`IncidentDef def` identity field. The exact native shape is checked before installation. Unlike an IL
rewrite, this observer accepts every incoming stream and returns the same enumerable unchanged,
including labels and exception blocks. It only clears publication and replaces a fresh compilation
identity before the new Harmony wrapper can run. No body hash/offset/local assumption or foreign
code execution is used to inspect compatibility.

## Composition and failure

The atomic `MM.CONTENT.Incidents` group contains this member, the eligibility generation observer,
eligibility postfix, execution prefix and execution-result postfix. Final installation revalidates the
group; rejection rolls back all attempted exact owned slots through the common coordinator.
Require one owned callback in this target/kind, no duplicated callback and no factory-shaped
metadata before installing or sorting. Permit other owners and arbitrary transpiler order: this
observer's effect is generation invalidation, not reliance on a final instruction snippet.

A normal Harmony rebuild retaining the observer invalidates old availability. Explicit owner refresh
outside foreign locks may admit the new final prefix/postfix/finalizer ordering only if the captured
compilation identity remains current. It restores the prior policy within its exact load boundary;
it never applies pending choices. Foreign removal of the observer itself is outside automatic
notification; explicit refresh detects absent ownership. Callback faults and failed group cleanup
leave owned availability false. Active original Cherry Picker suppresses the group from the root's
canonical active package snapshot, with native behavior as fallback and stored requests retained.

`NeedsPublicationRefresh` permits one automatic inspection per observed compilation once all group
members are installed. Refresh records the attempted generation before reading metadata. A denied
generation leaves that property false; a newer compilation makes it true. Explicit calls may still
reinspect intentionally. Root emits the retained non-null Failure through its protected sink only
after a scheduled denied attempt, avoiding per-frame warning or metadata-inspection loops.

Common bounded startup warnings name this ID, exact target/spec, failed predicate, unavailable
incident exclusion and fallback once per skipped enabled member. No per-action warning loop.

## Verification and observation

Scoped host `IncidentPatchBridgeTests` exercises rebuild invalidation, explicit compatible refresh,
factory preflight, group rejection, native-shaped effect refusal and failure latching. The test classes
are ordinary CLR types, not loaded mods or game Defs. The executable allowed/refused native incident
scenarios are linked in `MM.CONTENT.IncidentExecute.md`; weighted selection, settings load/pending
and original-provider controls remain pending. Root owns reviewed-build game execution, screenshots,
ordered mods/config hashes and cleanup. This document does not claim live acceptance.
