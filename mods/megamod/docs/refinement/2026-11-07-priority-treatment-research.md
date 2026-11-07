# S134 Priority Treatment research — 2026-11-07

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target1.7 beta.
Documentation/source observations and selected design, not a tested compatibility or runtime claim.

## Evidence order and identity

Read author documentation first: [Workshop3813351382](https://steamcommunity.com/sharedfiles/filedetails/?id=3813351382),
[guide](https://rmapi.dreadarm.net/mods/priority-treatment/guide/),
[API](https://rmapi.dreadarm.net/mods/priority-treatment/api/) and
[binding instructions](https://rmapi.dreadarm.net/how-to-use/). The guide/API match0.1.22. Workshop's
generic removal/incompatibility boilerplate is not independent compatibility evidence. The guide's
broad rescue wording is narrower in actual native automatic faction eligibility.

Installed read-only `F:/Steam/steamapps/workshop/content/294100/3813351382`,
`Dreadarm.PriorityTreatment`, Dreadarm, About version0.1.22, supports1.6/1.7, requires Harmony only.
LoadFolders selects Common plus matching version. 1.7 assembly version0.1.22.0, informational
`0.1.22+db747c2783cd7b7cca5c88634132b1f035c03b91`; the embedded ID is not a verified public revision.
No supplied source; all16 decompiled compiled-source files were read, including settings and API.
Local LICENSE: all rights reserved, Dreadarm2026. Independently implement behavior; copy no code,
binary, translation or artwork. No public repository revision was found in this bounded search.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 3662 | 6761c6e3e184b035657e56abedfd1f441f5af7e9e88aa8d27e18cbd9d2525e17 |
| LoadFolders.xml | 261 | 270c88586c23b4c9469d3a195ad8d355e91a38338eccdd145d0906caface76d7 |
| LICENSE | 306 | c2a08a0d1ebf76ac69fbb39aff05fac78b2814d07f112a393987d4169268bb24 |
| 1.7/Assemblies/Dreadarm.PriorityTreatment.dll | 20992 | aec9d8db18a1e080ce42f9baac584aecb654d65a2f083b084078c6abb3570f1a |
| 1.6/Assemblies/Dreadarm.PriorityTreatment.dll | 20992 | 11c22609c0039d427c74b74ab8dcd268970aee345941a4b82a9b66d9a894121c |
| Common/Defs/WorkGiverDefs/PriorityTreatment.xml | 2269 | 191edd5045685a92686ad4cd0a1e972df04bc6560fb2d05f765d9abb882b74bd |
| Common/Patches/EmergencyTending.xml | 291 | 9238e61cd3650606f30d7babd1843346816f3f51d8a5a8c6db62ea26ef5437e6 |
| Common/Languages/English/Keyed/PriorityTreatment.xml | 2997 | 58ec212e97abbad313279e6e89f44ad161f1dcf854188a131568f12c6fceb69b |

Native inspected1.7.5000rev1275 Assembly-CSharp.dll,15636480 bytes,
SHA256 `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254`.
Ignored local evidence: `artifacts/Refinement/megamod-spec-20261107/priority-treatment/` contains
input-manifest, public HTML, full decompilation/read list and focused native extracts. Hashes identify
research inputs, never runtime compatibility predicates. Finalized Defs still need isolated runs.

## Complete behavior inventory

| Source path/family | Observed behavior and edge cases |
| --- | --- |
| DoctorReadiness, Shared.PlayerOrders | Spawned controlled colonist, Doctor priority1, interruptible job; excludes draft/downed/mental/burning/duty, explicit current/queued/prioritized work, deliberate rest, mandatory jobs except configurable ordinary ingestion, ongoing patient care and carrying a pawn. Rest<=0.1 or Starving blocks; hunger alone does not. Sleeping can wake; meals can finish. Medical-rest/urgent-bleeding doctor gets Rescue/UrgentOnly. |
| JobGiver_EmergencyTend, EmergencyResponse | Constant-tree decision scans rescue then custom tending for healthy doctors or native urgent tending for a patient-doctor. Builds candidates then closest reachable; not a full native priority115/110/105 scan. |
| WorkGiver_PriorityRescue | Native rescue rules, baby branch delegated; extra bed reservation checks and own-LayDown same-bed fallback. Does not create beds/slots. Native faction limits still apply. |
| WorkGiver_PriorityTend, TreatmentRules | Base TendOther eligibility plus currently tendable Injury/MissingPart; optional sickness admits all tendable conditions. Scar need not be tendable. |
| InjuredSurgeonPatch | One prefix on WorkGiver_DoBill.JobOnThing; null result/skip when bill giver is pawn, doctor has any Injury including scar, enabled option, and not forced. Includes drug/quill operations, not just surgery; missing part alone does not qualify. Ordinary tending remains. |
| WorkGiver_QueueAdministerDrug | Side-effect scan in ShouldSkip, always returns true/no job. Enabled regular Doctor work, not emergency priority1 or readiness; scans potential human/animal operation patients, schedules drug then quill, excludes injured scheduler. Does not itself check patient reach/reservation. |
| DrugAdministration | Living spawned player/hosted patient, care allowed, not slaughtered, in usable bed, bill count<15. Cannot wake OR deathrest; ordinary sleep is insufficient. Unmet addiction Need_Chemical or Biotech dependency, distinct chemicals. Matching existing Bill_Medical including suspended blocks. First matching reachable/reservable/nonforbidden drug in native lister, policy allowedForAddiction (no policy facility permits; missing policy/row rejects); first matching one-fixed-ingredient administration recipe and AvailableOnNow. Adds native Bill_Medical. |
| QuillRemoval | Odyssey gated, same patient/bed/count rules, any visible quill, awake or asleep. Existing matching operation including suspended blocks. Recipe_RemoveHediff, exact removesHediff, targetsBodyPart=false, AvailableNow+AvailableOnNow. Adds one native operation. |
| Api.V1 | Read-only/null-safe policy/readiness predicates; does not dispatch jobs. |
| Mod, settings, DefOf, AssemblyInfo | Constructor unguarded Harmony PatchAll. Seven global booleans; no map component, alternate bottom tab or independent persistence/cancel ledger. |

XML defines Doctor emergency Rescue priority115, emergency Tend105 and QueueAdministerDrug71
(above native human operations70); rescue/tend need Manipulation, cannot be directly ordered or done
by mechs. Plain PatchOperationAdd appends the emergency node to HumanlikeConstant/thinkRoot/subNodes
without shape/idempotency guards. Do not copy that installation strategy.

Settings keys/defaults: enabled=true, includeSickness=false, wakeDoctors=true, allowMeals=true,
blockInjuredSurgeons=true, autoAdministerDrugs=true, autoRemoveQuills=true. English tooltip explicitly
includes scars and notes deleting a source quill bill immediately allows it to be recreated.

## Focused native findings

- HealthAIUtility automatic rescue requires same faction, downed/not already in bed, not caravan
  gathering, suitable ritual/mental/slaughter/infant conditions and no nearby enemy within25 cells;
  preserves reach/reservation. Do not derive automatic prisoner/outsider rescue from broad author prose.
- WorkGiver_RescueDowned/TakeToBed and JobDriver_TakeToBed find and reserve a real bed place, carry and
  tuck the patient in. Source bed checks do not prove the subsequent actual reservation succeeds.
- TendOther requires another pawn, suitable posture, care permission and reservation. Urgent variant
  adds TicksUntilDeathDueToBloodLoss<45000, i.e.18 game hours. S062 field-care behavior is owned extension.
- WorkGiver_DoBill.JobOnThing retains usable patient/operation, reservation, burning, skill, ingredient
  and medical-faction checks. Patient CurrentlyUsableForBills only means InBed plus valid interaction
  cell; that alone does not prove doctor access or general care permission.
- Need_Chemical is Satisfied above0.1, Desire above0.01, otherwise Withdrawal. Source assistance starts
  at desire as well as withdrawal. GeneticDrugNeed in Biotech Hediffs_Various.xml has deficiency
  threshold1, severity/day0.2; ShouldSatify uses stage[2].minSeverity-0.1 =0.9, hence4.5 days for the
  inspected unmodified Def. This is a derived native comparator, not a new prescribed interval.
- Odyssey Recipes_Surgery_Misc.xml RemovePorcupineQuill has work600, targetsBodyPart=false,
  removesHediffPorcupineQuill, anesthetize=false, null outcome effect, empty ingredient list.
  Recipe_RemoveHediff removes all visible matching conditions on the whole pawn; it supplies no
  harvested products. Fixed medicine category does not by itself imply an ingredient cost.

## Actual composition and selected differences

Confirmed overlap with [S124 Drug Response](2026-11-06-drug-response-research.md): both can schedule
drug operations. Selected PT05 consumes DR09–10/13 directly: one chemical dose, reasons and actual
consumption, suspended/manual/foreign blockers, player holds, priority for incapacitated dependency,
positive scheduled interval exception only for S134 and current biological rechecks.
The source has no such shared authority, recovery cleanup or persistent cancellation hold.

Retain source defaults and precise eligibility, with these explicit changes:

1. Compose S058 rescue/tend ordering and S062 necessary field care rather than closest-only duplicate
   scans. Rescue outranks even urgent tending; S062 field care can precede transporting that selected
   rescue patient, not substitute another casualty. Preserve admitted external ordering or withhold
   that selection. No medical survival claim; rescue-first/disease-first trade-offs remain visible.
2. Validate current doctor access and complete recipe availability before owned scheduling; nearest
   eligible drug supply replaces insertion-order selection. Native operation costs/effects remain.
3. Player cancellation holds for both automatic drug and quill work; no requeue loop. Editing hands
   ownership to player. Never mutate foreign/manual bills or force resume.
4. Common inherited configuration, current automation switch bottom right, patient reasons/Resume
   in Health. No main tab. S137 condition filtering is discovery only.
5. Executable semantic admission, guarded merge-preserving XML and unsupported-family fallback.

S042 self-tend permission, S062 condition/medicine/operation inventory/field rest and S080 stabilization
remain their own contracts. No other repository mod dependency; direct owned decisions, no internal
patching. Share native/index candidates, not stale pawn eligibility. No optimization claim yet.
Source readiness explicitly excludes automatic self-tending from protected ongoing medical work;
it can yield to a permitted response. Player-forced self-tending remains protected. This distinction
is retained, not replaced with blanket immunity for every tending job.

## External contracts, lifecycle and licensing

Public API type Dreadarm.PriorityTreatment.Api.V1, static ContractVersion2:
bool BlocksSurgery(Pawn doctor, Thing billGiver, bool forced),
bool MatchesEmergencyPolicy(Pawn patient), bool CanRespondToEmergency(Pawn doctor).
Documented binder checks package ID, assembly, API type, version>=1 and exact methods, binds after
startup, caches delegates and falls back neutrally if missing/failing. Same type in our assembly
would not meet a package/assembly-bound lookup. No blanket drop-in claim or identity spoofing.

Author identifies Ability Loadout as readiness/policy consumer for deferring support heals.
It was not found in installed About metadata. Bounded source/XML/JSON and ASCII assembly scans
found only Priority Treatment's own files; they cannot exclude UTF16/reflection-built names or
uninstalled consumers. Actual consumer port remains PT-Q3, not invented facade work.

Source global settings keys are above; source WorkGiver/ThinkNode/Def names are externally visible
but no installed consumer was verified. Its generated bills are native Bill_Medical with native
recipe names. Runtime jobs can still refer to source WorkGiverDefs. Author safe-removal claim is
not accepted without native save tests. No migration selected; any future import needs backup,
atomicity, restoration on failure and verified postconditions. Co-installed source withholds owned
S134 overlap. Unsupported external dose schedulers retain S124's conservative ownership policy.

## Open work and acceptance

PT-Q1 source inventory resolved. PT-Q2 restrictive default/class-order balance; PT-Q3 actual
consumer/save compatibility; PT-Q4 effective targets and finalized optional Defs; PT-Q5 shared
history/holds/performance; PT-Q6 implementation, seven languages and native acceptance remain.
Compare native care, source behavior and composed S058/S062/S124 outcomes; scars must not be hidden
as a trivial convenience setting. No material/work discount, free treatment or new content selected.
Final acceptance requires actual rescue/tend/operate/cancel/resume/save/load workflows and inspected
causal screenshots on a reviewed build, including exact optional/source-provider runs. Source
inspection, host tests, loaded Defs and clean logs alone do not accept gameplay.
