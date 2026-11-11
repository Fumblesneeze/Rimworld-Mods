# MM.CONTENT.IncidentEligibilityGeneration: revoke stale incident eligibility

Owner `fumblesneeze.megamod`, S038, owning contract
`openspec/changes/specify-megamod-content-selection/`. Candidate composed and independently reviewed;
native repatch acceptance remains pending.

## Target and minimal predicates

Transpiler on public instance nongeneric `bool RimWorld.IncidentWorker.CanFireNow(IncidentParms)`;
public instance `IncidentDef def` is required by the coupled decision patch. Implementation and
validator: `Source/Features/ContentSelection/Harmony/IncidentPatchBridge.cs`.

This callback observes compilation only. It changes no instruction, label, branch, local or exception
block, and returns the exact incoming enumerable. Its only action is atomically replacing the
compilation identity and clearing publication. Thus every incoming stream is valid for this observer;
there is no required body landmark and no structural transformation to partially apply. The coupled
eligibility veto consumes the final result independently of whether the native body was skipped.
This observation is necessary to avoid trusting old ordering after normal Harmony rebuilds.

## Composition, admission and invalidation

Part of the atomic five-member `MM.CONTENT.Incidents` DefsReady group. Require exactly one owned
generation callback and no duplicate callback registration in the target's transpiler metadata.
Foreign owners and transpiler order are permitted; the observer need not run last because it does
not inspect final code. Factory registrations in any slot reject initial admission before patching,
and explicit publication rejects them before any sorting. No speculative foreign transpiler probe.

Normal rebuilds retaining this observer revoke the old publication synchronously. The composition
owner may explicitly refresh outside foreign locks after rebuilding; exact generation identity must
still match after ordering checks. Refresh never applies pending settings. Removing the owned
observer itself is outside this automatic notification contract; explicit refresh rejects its absence.

Automatic composition uses `NeedsPublicationRefresh`, which compares current compilation with the
last actually attempted generation only for a fully installed, non-faulted group. Refresh consumes
that attempt before metadata inspection, including rejection or an inspection exception. A denied
generation is not rescanned every frame; a later observed compilation makes a new attempt eligible.
Explicit RefreshPublication remains available for intentional reinspection. Root reports a non-null
Failure once after each scheduled denied attempt through its protected sink; absent Failure after
an actual exception does not request a duplicate generic warning. This schedules inspection only,
not settings application, Def recovery or policy publication without the existing guards.

Common coordinator and exact OwnedHarmonyPatch receipts own installation/rollback. Inactive gates
remain false even if cleanup fails. Original Cherry Picker active suppresses all five members.
One bounded warning per skipped enabled member identifies patch/target/spec/reason, unavailable
incident exclusion and native behavior fallback. The observer has no standalone player effect.

## Verification and native proof

`IncidentPatchBridgeTests.RepatchRevokesPublicationUntilExplicitMetadataRefresh` exercises a real
scoped host Harmony rebuild, compatible observer, revocation and explicit refresh. Factory and
group-failure tests share that fixture. Native action scenarios and outstanding weighted-selection,
load/pending and original-provider controls are in `MM.CONTENT.IncidentExecute.md`. No game run,
Def fixture, startup marker or host patch test accepts this game-facing integration.
