# S135 Basic Caravan Loading research — 2026-11-07

Owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
Research/specification only; no runtime compatibility or acceptance claim.

## Public documentation, identity and complete source

Read [author Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3370434966)
first. It explains moving loading/unloading into Basic so players can prioritize it separately from
ordinary hauling, and recommends late load order. That recommendation is not our conflict policy.
Workshop author Shakecataz; installed About author LM, package `LM420.BCL`, versions1.0–1.7,
no modVersion or required dependency. Flat XML-only layout; no assembly, settings, Defs, translation
folder, public-source link or license file. Four installed files including unused preview art.

Read-only input `F:/Steam/steamapps/workshop/content/294100/3370434966`:

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 469 | 0de108e9807083e43ea5026083ccb078f1f70a9e332198e21287d18667fefeb4 |
| About/PublishedFileId.txt | 10 | fa1bef382e9b26d5e7727efce1ddc96b451e2ee9863ee2c3460066d537763bd2 |
| Patches/WorkGiversPatch.xml | 460 | 2ee506cd1d979da1a637f7f8a7c8e6eff7f08e7ebfb235555ec3cb88991c45e4 |

Entire behavior is two PatchOperationReplace operations on WorkGiverDef/workType:
HelpGatheringItemsForCaravan→BasicWorker and UnloadCarriers→BasicWorker. No extra priority,
permission, quantity, scheduling or job modification. About mentions only loading; the actual XML
and current public description include unloading. Independent authored behavior; no art copied and
no reuse license inferred from downloadable XML. Source has no guard or atomic group preflight.

## Native1.7 intersections

Inspected Assembly-CSharp1.7.5000rev1275,15636480 bytes,
SHA256 `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254`.
Read Core WorkGivers.xml/WorkTypes.xml, full WorkGiver_HelpGatheringItemsForCaravan,
WorkGiver_UnloadCarriers, GatherItemsForCaravanUtility, UnloadCarriersJobGiverUtility and
JobDriver_PrepareCaravan_GatherItems. Ignored evidence under
`artifacts/Refinement/megamod-spec-20261107/basic-caravan-loading/` retains manifest/native extracts.

| Native comparator | Observed fact |
| --- | --- |
| UnloadCarriers | Hauling, priority130, Manipulation, canBeDoneByMechs=true; delegates native unload eligibility and creates UnloadInventory job |
| HelpGatheringItemsForCaravan | Hauling, priority120, Manipulation; scans current gathering caravan lords, selected transferables and reachable usable carrier with allowColonists=false |
| LoadTransporters | Separate Hauling entry, priority110; source does not edit it or HaulToPortal |
| BasicWorker | naturalPriority1150, alwaysStartActive, Commoner/AllWork tags; different eligibility from Hauling300 with ManualDumb/Hauling/Commoner/AllWork and requireCapableColonist |
| Existing Basic tasks | Flick500, prisoner release100, Open/EjectFuel50, skull extraction20; relocated unload130/helper120 sit between flick and release |

Native helper is not the caravan participant's whole gathering system. Participant can use an
inventory path; outside helper uses carry path. Native selected transfer counts and current other
workers' gathering jobs constrain the request. Carrier admission requires forming caravan, suitable
carrier state/access and, for pack animals/hosted pawns, not already overencumbered. The helper's
initial allowColonists=false test is narrower than later job fallback; do not promise assistance
for all colonist-only caravans. The source does not fix native whole-stack accounting/transfer
edge cases. The native job has25-tick handling waits,7500-tick collection bound and500-loop backstop;
S135 does not alter these internals or claim to make them optimal.

Native unloading rejects self, free colonist, no UnloadEverything, wrong faction/host, forbidden,
burning or unreservable target. Actual path still must be reachable. It does not mean empty every
colonist's personal inventory. Moving work type can change mech/work-tag eligibility: preserving
the canBeDoneByMechs field does not grant BasicWorker to every mech.

## Selected composition and compatibility

Fixed two-scalar reassignment, no new setting. Pawn priorities are entity state and remain unchanged.
S133 carrier batches use the selected Basic work authority without an extra Haul-enabled check;
S021/S133 physical capacity, protection/recovery, S126 visible cargo and S132 presentation remain.
Native caravan gathering path stays native. S125/general hauling remains separately permissioned.
S122 De-generalize Work changes crafting speed, not work category. Portal integration is excluded
by the user and not present in this source. Pod/shuttle assignment remains unchanged.

XML Extensions becomes required for owned guards. Confirm unique native targets, expected scalar
shape and Basic role; accept Hauling or already Basic, preserve siblings and every unrelated field.
The intended scalar replacement cannot be unioned; no list/category replacement is needed.
Preflight both before mutation, retain effective atomic result or withhold the group with per-patch
startup warnings. A different custom work assignment is not blindly overridden. Source-only
equivalent XML can coexist as no-op; late incompatible overrides need explicit effective handling.
This is a concrete semantic conflict rule, not a ban on other mods patching the same Def.

No source API/types/settings/save keys exist. Bounded installed XML/C#/JSON/Markdown/text search for
LM420.BCL or3370434966 found only its own metadata/PublishedFileId; no actual external consumer was
identified. This does not prove absence of every package-dependent consumer. Preserve native Def
identities; no source package spoof or migration. Installed original and custom-work providers still
need exact native present/absent runs before compatibility claims.

## UI, balance and remaining evidence

Only ordinary Work, formation and unload controls; no source main tab or settings. No new UI beyond
any necessary translated admission explanation. Seven-language verification applies to added text.
Priority changes are the benefit, not higher speed or capacity. Compare Basic-only helper, ordinary
Hauling assignment and S133 batched unloading with actual worker/party availability and real cargo.
No cost, worktime or travel discount selected. BCL-Q1 inventory resolved; BCL-Q2 effective atomic
XML/order, BCL-Q3 worker/carrier eligibility and BCL-Q4 implementation/native acceptance remain.
Final acceptance requires real formation/gather/deliver/unload workflows on the reviewed build,
not merely altered Def fields, startup logs or passing host tests.
