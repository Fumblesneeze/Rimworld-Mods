# S030 — Brrr and Phew (Continued) research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-02**. Origin: replacement plus the user's original toddler-temperature
prevention/response amendment, P1-24. Full Toddlers remains the separate S031 deep dive.
This is source evidence and refinement, not implementation or live compatibility acceptance.

## Provenance and inspection boundaries

Public documentation was read before source:

- [Workshop 2195938471](https://steamcommunity.com/sharedfiles/filedetails/?id=2195938471),
  **Brrr and Phew (Continued)**, package **`Mlie.BrrandPhew`**. Original pelador, continuation Mlie;
  About's author field spells the original name `peldor`.
- [Public repository and README](https://github.com/emipa606/BrrandPhew), checked-out revision
  `94016d8990e2f0193ffda18b34492b9952d0f88c`, dated **2026-04-12T22:04:01+02:00**.
  Commit describes adding optional Combat Extended black-smoke response and translation updates.
- [Toddlers Workshop 2903359152](https://steamcommunity.com/sharedfiles/filedetails/?id=2903359152)
  was read for the narrow temperature/mobility intersection, not complete S031 coverage.

Installed source root: `F:/Steam/steamapps/workshop/content/294100/2195938471`.
Manifest version **1.6.8**; advertised game versions **1.1–1.6**, not 1.7. Harmony is required.
Installed `1.6/Assemblies/Brrr.dll` SHA256:
`858A6CFBFE48F28BD17E6D098DB5EA9AEAC59A18479EE21D2FB5654FB5C84584`.
The public checkout's committed 1.6 DLL has the same hash. About.xml SHA256:
`8F955AB5228719B130D81A3FDC40E9318FEF45F3CFF7E68852A85BFED4767BDB`.
Assembly version is `0.0.0.0`; the manifest is the useful release identity.

Ignored local evidence: `artifacts/Refinement/megamod-spec-20261102/brrr-and-phew/` contains
`upstream`, `installed-decompiled`, `game`, `toddlers-decompiled`, and `proxyheat-decompiled`.
All **35 source C# files** and **six Def XML files** were inspected. Installed decompilation was
generated and selected behavior/settings branches checked; this is not a claim of a second complete
line-by-line binary audit. Native classes and external-provider inspection below are explicitly scoped.

The repository LICENSE.md says MIT, copyright 2020 Mlie; Workshop/README retain CC BY-NC-SA 4.0
wording. Record the discrepancy rather than silently choosing permission to copy. Independently
implement observable behavior; do not copy source, DLLs, assets or translations. Shipped helpers
VersionFromManifest and Multiplayer API are not product dependencies or packaging inputs.

## Documentation versus code

Documentation promises early responses to cold, heat, toxic buildup, cabin fever, More Events heavy
air, Anomaly blood rain and Odyssey vacuum; animals are included except those following a master.
Pawns rest in a bed or on the floor until recovered, with food/recreation breaks. Outdoors breaks
give half ordinary recreation and aim for 75% outdoors need, ignoring allowed areas.

Older prose mentions 10–30% injury settings and a vanilla 35% response. Current source sliders are
**1–100%**. Native 1.7 inspection establishes a Serious-stage gate, not a verified universal 35%
threshold for every race/hediff. Do not repeat the old numeric comparison as current fact.
Workshop reports about swimming failing to reduce heatstroke are anecdotal. The driver discrepancy
below is independently inspected code, not an independently reproduced gameplay failure.

## Complete source inventory

| Files / surface | Inspected behavior |
| --- | --- |
| `Controller`, `Settings` | Mod settings, global persistence, defaults, numeric reset, version label |
| `BrrrJobDefOf` | Five recovery jobs plus outdoor walk and skygaze identities |
| `ThinkNode_CanBrrr/Phew/Yuk/Red/Gasp/Ooh` | Human eligibility and individual response switches |
| `ThinkNode_AnimalCanBrrr/Phew/Yuk/Red/Gasp` | Player-animal eligibility and following-master exclusion |
| `JobGiver_Brrr/Phew/Yuk/Red/Gasp`, corresponding five `JobGiver_Animal*` | Severity, needs and destination selection; shared-job exclusion |
| `JobGiver_Ooh` | Outdoors trigger, destination/walk construction and random activity choice |
| `JobDriver_Brrr/Phew/Yuk/Red/GaspRecovery` | Reservations, bed/floor travel, reports and shared lying-down behavior; special heat-swimming branch |
| `JobDriver_OohSkygaze`, `JobDriver_OohGoForWalk` | Outdoor activity duration, need gain and interruption |
| `BrrrGlobals`, `Toils_BrrrLayDown` | Random/roofed region and cell search, recreation calculations, recovery/sleep/thought behavior |
| `MultiplayerSupport` | Conditional patches to its own RNG-using methods; no accepted multiplayer claim |
| `Defs_Brrr/Phew/Yuk/Red/Gasp/Ooh` XML | Eleven inserted ThinkTreeDefs, seven JobDefs, labels/reports and joy parameters |

No bottom tab, pawn gizmo, main-button definition or operational bottom-right switch was found.
Ordinary game integration is through ThinkTree Def insertion, not ordinary Harmony patches to
game methods. Human/animal insertion tags are `Humanlike_PreMain` / `Animal_PreMain`.
Priority numbers: cold 99, heat 98, toxic and blood rain both 97, outdoors 96, breathing 95.
These are source scheduling details, not the desired danger ranking or a promise of instant interruption.

## Settings and persistence

| Source setting | Initial value | Source scope / notes |
| --- | --- | --- |
| Cold / heat / toxic response | On / on / on | Severity thresholds 20% / 20% / 10% |
| Breathing response | On | Shared 10% threshold for oxygen starvation, vacuum and optional smoke |
| Combat Extended smoke response | Off | Control shown only with breathing enabled and canonical CE package active |
| Blood-rain response | Anomaly installed | 10%; **neither switch nor threshold is serialized** |
| Outdoor breaks | On | Start at outdoors need <=20%; label's cabin-fever wording is misleading |
| Recreation may interrupt | On | Recreation below 12.5% blocks/ends recovery; animals have no joy test |
| Apply to animals | On | Five health responses; no outdoor breaks |
| Allow recovery in unsafe areas | On | Ambiguous label: parts of code mean outside allowed areas; inconsistently applied |
| Numeric sliders | 1–100% | Source reset restores seven numeric defaults only, not switches |

All are source-global static ModSettings. All except the blood-rain pair have explicit serialized
keys matching field names. Opening settings without Anomaly also overwrites UseRed=false.
Owned configuration will use the common global-default/save-override model and persist every
choice, retaining dormant optional-provider values. No automatic import from source settings.
Rename the area control to describe allowed-area escape rather than permission to seek danger.

## Human and animal recovery paths

Human conditionals require a spawned player-controlled colonist, able to move, awake, not downed,
burning, mentally broken, drafted or in need of medical rest. Native 1.7's player-controlled
colonist property can include colony slaves; Ooh separately excludes slaves. Visitors, prisoners,
wild humans, mechanoids and non-colonist mutants are not thereby included.
Animal gates additionally require player faction and ApplyAnimals; wild men are excluded. Trained
animals currently following a master are excluded through obedience and ShouldFollowMaster.

Job givers reject hungry pawns, human recreation below the configured threshold when enabled,
disturbance that prevents lying down, and any current source recovery/outdoor job. Existing urgent
native duties before the insertion point retain priority. These source gates mean self-preservation
is not an unconditional survival guarantee. The owned draft retains ordinary food/recreation
interruption but gives the custom toddler escape its own pre-injury movement rule.

Cold/heat use actual Hypothermia/Heatstroke severity >= threshold. They prefer one bed returned by
FindBedFor if it is indoors, unpolluted and within the comfortable temperature range; otherwise
search traversable, non-doorway regions, up to 9,999. Temperature is the region/room temperature.
With the allowed-area restriction, the region needs only *some* allowed cell; its final random
cell is not checked again. Animal heat floor fallback incorrectly creates a cold-recovery job.

Toxic and blood-rain response use ToxicBuildup/BloodRage severity >= threshold. A bed must be roofed,
unpolluted and comfortable. Floor selection seeks a roofed, unpolluted, standable, comfortable,
reservable/reachable Danger.None cell with applicable area and prisoner-room constraints. The
boolean success of the cell search is ignored; callers can construct a job with an invalid cell.

The four-argument `FindBedFor(pawn,pawn,false,true)` does **not** mean bypass allowed areas:
native 1.7 parameters are `checkSocialProperness=false, ignoreOtherReservations=true`.
Cold/toxic/blood human callers switch overload with AllowUnsafeAreas; heat/breathing and animals
use the four-argument form unconditionally. Later reservation failure does not make this a sensible
allowed-area policy. Owned behavior respects genuine bed access, ownership and reservations and
validates the final cell; unsuitable preferred beds do not prevent finding another valid refuge.

### Independent breathing hazards

Human and animal Gasp resolve `OxygenStarvation`, `VacuumExposure`, `SmokeInhalation` by Def name.
If more than one is registered, each registered Def's condition must exist on the pawn at threshold.
The smoke branch additionally requires the CE option. Thus an installed unrelated hazard can block
response to a single actual hazard. No exact provider-package guard is used by these job givers.
The bed/floor test is merely roof/pollution/temperature, not actual breathable-air or smoke safety.

Owned correction: each supported enabled hazard triggers independently. A roof is insufficient
evidence for vacuum, oxygen or smoke recovery. Each provider must supply an admitted exposure and
recovery interpretation. Unknown providers are explicitly unsupported, not inferred from a name.

### Rest, completion and failure

Bed drivers reserve and claim nonmedical beds; floor jobs do not reserve their resting cell. Shared
toils handle posture, ordinary sleep, bed/floor rest and comfort, icons and bed thoughts; floor rest
effectiveness is 0.8 in source. The copied thought logic uses race comfort stats in places rather
than the pawn's complete current thermal protection. Owned work should reuse native rest semantics.

Recovery ends when hunger/allowed low recreation interrupts, or when **all** listed cold, heat,
toxic, blood, oxygen, vacuum and smoke conditions are absent, regardless of which response is enabled
or caused the job. The finish lookup uses **`BrrrSmokeInhalation`**, unlike the start lookup's
`SmokeInhalation`. Unrelated or disabled conditions can keep the pawn resting; actual smoke can be
missed. There is no ongoing refuge-temperature/exposure revalidation. A bed-owner change cancels;
the downed branch directly relocates the pawn, which the independent implementation must not copy.

Owned correction: retain the actual enabled causes being recovered from, continue until those
conditions clear through normal health rules, re-evaluate changed surroundings, and release work
without teleportation or ownership theft. Removing a cause from enabled policy stops waiting for
that cause. Recovery from one condition cannot require an unrelated disabled/permanent condition
to vanish. Existing unrelated health conditions are never erased by this feature.

### Swimming

Human Phew checks **Royalty's package**, despite a comment saying Odyssey, before searching water
within successive radii 0–29. It checks water tag/reachability but lacks the native complete swimming
eligibility and exposure checks. The custom driver performs two GoTo operations to one water cell,
sets swimming pose/lastSwamTick, and ends. It does not execute native swimming's cooling loop.

Native 1.7 `JoyGiver_GoSwimming` is Odyssey-gated and checks path, water, toxic factor, fog, danger,
room/outdoor enjoyment and vacuum. `JobDriver_GoSwimming` walks a swim route, waits 240 ticks between
segments, and **removes heatstroke on an actual route-advance branch**. This is a real native
mechanic; do not claim that merely setting lastSwamTick performs it, nor that swimming never heals.
The owned proposal retains cooling by a real eligible swim, with correct DLC admission and dry
refuge fallback. It adds no independent instant-cure operation.

## Outdoor breaks

Ooh requires a qualifying nonslave colonist with an outdoors need at/below its threshold, and the
same food/joy/current-job gates. It rejects enabled cold, heat and toxic conditions at threshold,
but not blood rain or breathing hazards. Source constructs both a skygaze cell and a complete
walking route even if only one activity will be selected; failure of either blocks both.

Regions must be psychologically outdoors; destinations unroofed and not marked avoid-wander. Walk
generation has eight intermediate segments plus initial and return cells, sampling every third
radial-pattern index between radius-derived bounds of 2 and 14 cells, with standability,
line-of-sight and known-danger checks. It ignores allowed areas and
has no comprehensive thermal/toxic/vacuum eligibility check. The random choice uses integers
1–99 inclusive; <=45 walks, otherwise skygazes: **45/99 versus 54/99**, not exactly 45%/55%.

Walk duration is **6,000 ticks** (including travel); skygazing **4,000 ticks** after arrival.
Jobs end at **75% outdoors**, on route/duration end, or for hunger. Recreation rate is .5 of the
source's ordinary .36/2500 gain basis. Source enjoyment does not use the full native JoyTickCheckEnd
path; it need not stop when joy is full. Both jobs stop when the timetable does not allow joy,
because their ignoreJoyTimeAssignment flag is not set. Reports distinguish eclipse, aurora, night,
clouds, sunrise and sunset. The draft retains those contextual reports and native need mechanics.

Owned corrections: choose among actually feasible safe activities, don't require a walking circuit
to skygaze, consider all admitted hazards, and skip a break when already at the 75% goal even if
the user set an unusually high start threshold. Retain source weighting when both choices exist.
Keep the source's allowed-area exemption explicit, without treating it as permission to cross
locked doors or unsafe cells. Normal timetable/urgent-job interruption remains respected.

## Native 1.7 comparison

Inspected installation **1.7.4995 rev309**, `F:/Steam/steamapps/common/RimWorld`;
root `Assembly-CSharp.dll` SHA256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
This pins research, not a runtime admission hash or a compatibility claim.

| Native inspected surface | Relevant fact / ownership consequence |
| --- | --- |
| `JobGiver_SeekSafeTemperature` | Defaults require Serious-stage temperature injury; searches comfortable regions and verifies an allowed final cell; already-safe pawn can wait 500 ticks |
| `GenTemperature` relevant range/cell methods | Comfortable range uses current pawn stats; safe range extends each end by 10°C. Failed cell query has a 21°C fallback value: failure must not be accepted as safety |
| `HediffGiver_Heat`, `HediffGiver_Hypothermia` | Injury grows outside safe range; recovery depends on actual ambient conditions. Heat requires below comfortable maximum, cold above comfortable minimum (with additional native cold-recovery cases). Closed-boundary inclusion alone does not prove improvement |
| `ToxicUtility` | Toxic terrain, pollution and unroofed airborne exposure differ; actual pawn resistance matters |
| `HediffComp_VacuumExposure`, `HediffGiver_VacuumBurn` | Actual vacuum exposure/resistance matter; roof alone is insufficient; native vacuum severity decays when no longer exposed |
| `Hediff_BloodRage`, `BloodRainUtility` | Active blood rain, exposure/roof and sensitivity drive buildup and recovery |
| Native swimming giver/driver | Correct Odyssey availability and physical cooling route, described above |
| `JobGiver_Wander`, `DangerUtility` | Bounded wander selection has a no-move wait/null fallback; generic room danger is not a complete per-hazard cell safety test |
| `RestUtility` selected bed search/signatures | Ownership, partner, medical, use and reservation constraints; false/true arguments are social/reservation flags |
| `Pawn` selected control properties | Spawned colonist control includes some slaves, not arbitrary visitors/prisoners/mechs/subhumans |
| Core Humanlike/Animal think-tree XML | Human PreMain follows apparel/supplies; animal PreMain follows rope/lord duties; not the root of every life-stage tree |

Full native files were generated where needed; only relevant range/bed/control methods were read in
large utility/Pawn files. No universal full-game source or finalized-Def audit is claimed. Exact
race-specific injuries (including insectoid cold variants) need explicit support evidence before
expanding beyond the inspected standard conditions.

## Narrow Toddlers composition

`cyanobot.toddlers`, installed Workshop **2903359152**, advertises 1.4–1.6, requires Biotech and
Harmony. `1.6/Assemblies/Toddlers.dll` SHA256
`8CDBA87787B069B1CBBB44F6C78DF5527723CBC253E4404A548DB112F1FC08B5`.
Read its ThinkTrees and ThinkTree patches completely; inspected toddler identification, crawler
learning, door, crib, leave-crib and initial caregiver safety methods narrowly.

`HumanlikeToddler` has **no Humanlike_PreMain tag**. Its colonist branch uses native
SeekSafeTemperature with injury-required default, after allowed-area return. Its earlier crib
branch can select rest/leave/play/idle before that branch. Normal colony idle wander uses
Danger.None, while prisoner/current-room and fallback wander can permit Deadly. Some outdoor play
givers already check SafeTemperatureAtCell. Therefore it is inaccurate to say every toddler
activity lacks all thermal checks. It is equally inaccurate to assume Brrr's ordinary human
insertion automatically covers toddlers.

Crawlers (learning-to-walk stage 0) cannot open doors and are rejected by LeaveCrib; later walkers
have different capabilities. Toddler detection uses a particular life-stage/HAR worker contract,
not simply a guessed age. Caregiver `IsCurLocationSafe` checks forbidden status and native safe
temperature. Adult rescue reachability must remain separate from toddler reachability.

The user confirmed **both** safe idle wandering and autonomous escape when surroundings become
unsafe, before repeated rescue is necessary. The owned proposal uses actual current pawn thermal
protection/mobility; no fixed toddler Celsius window, magical door opening or teleportation. A
toddler unable to leave a crib/room still needs ordinary adult rescue. A single-cell room must
produce a bounded no-route result, not repeated move jobs or an endless search. This code evidence
suggests missing insertion/late response, but does not prove the user's reported failure in game.
The owned draft also prevents the earlier automatic allowed-area-return branch from immediately
undoing safe escape or caregiver placement outside a still-dangerous assigned area. Return resumes
with a safe permitted route/destination, without changing the assignment. Safe arrival ends escape
even above the injury threshold; a merely nonworsening refuge does not become an ineligible recovery
hold. These are explicit composition corrections, not claims about existing source behavior.

## Actual incoming consumer and other provider boundaries

An ASCII scan across installed Workshop/local source, XML and binaries plus a UTF-16 scan of
**5,485 installed DLLs** found the original's own versions and **Vanilla Temperature Expanded**
(`VanillaExpanded.Temperature`, Workshop **3202046258**). No exhaustive universal consumer claim.
Its `LoadFolders.xml` maps 1.7 explicitly to `1.6`; current ProxyHeat.dll SHA256:
`F3CE6241F83ABE864103E80C98C757C78F013CF07B18FBF19C560CA7F2E58355`.
The older 1.4 bundled source differs; the following records the selected 1.6 binary:

- Checks the exact active title **Brrr and Phew (Continued)** before patching Brrr-namespaced job
  givers, `BrrrGlobals.GenNewRRJob`, and **JobGiver_Phew.TryGiveJob**. It records a temporary pawn
  around source job selection and redirects some floor recovery to local heat-source cells.
- Its Phew transpiler inserts after job construction before return, not an owned target guard.
- General ambient-temperature patch applies local heat. Generic `TryGetTemperatureForCell` is
  modified only in a plant-growth context; calling it for a pawn destination does **not** guarantee
  local heat has been included. `HarmonyPatches.proxyHeatManagers`, the map manager's
  `GetTemperatureOutcomeFor(cell, baseline)`, and `ProxyHeatMod.settings.enableProxyHeat` are
  concrete adapter candidates; baseline/double-application/lifecycle need final verification.
- General wander patch rejects some cells made unsafe by local heat. It does not prove a new
  recovery planner considers local heating or safe paths.

Keep the behavioral integration through a narrow optional provider adapter. Do not spoof the title,
package, Brrr assembly or types to provoke foreign patches. A facade alone cannot satisfy the
title gate; there is no blanket drop-in claim. Preserve optional absence and fail closed for
uninterpretable active local-heat effects rather than presenting an unsafe refuge as verified safe.

Exact-name XML scanning found native Odyssey VacuumExposure but no installed XML declarations for
OxygenStarvation or SmokeInhalation. This is not proof that code-generated providers cannot exist.
More Events identity/exposure/recovery rules and exact Combat Extended smoke behavior remain
provider research. CE's canonical package is `CETeam.CombatExtended`; a matching Def name without
an admitted provider is insufficient. Neither source nor optional providers are required for the
core replacement; only the provider-specific branch is dormant/unsupported when unavailable.

Source MultiplayerSupport conditionally patches owned methods using PushState(TicksAbs)/PopState;
one requested `TryFindOohCell` differs in case from actual `tryFindOohCell`, and cleanup is not a
finalizer. Do not reproduce self-patches, promise multiplayer support, or infer determinism from
the source's advertised beta support.

## Proposed deviations, omissions and later questions

The [OpenSpec change](../../../../openspec/changes/specify-megamod-environmental-self-preservation/)
turns the above findings into observable contracts. Proposed corrections include independent
hazards, valid destinations and real exposure checks, correct swim capability, persistent blood
settings, bounded failure, cause-specific completion, safe feasible outdoor breaks and direct toddler
composition. These are authored decisions, not assertions that upstream already behaves that way.

Retain source threshold/default values and activity durations. Do not create new healing rates,
protective clothing, structures, research, costs or immunity. Survival still depends on reachable
shelter and ordinary care. Source all-condition rest, ambiguous unsafe-area behavior, random
unchecked floor cells, bed stealing, instant relocation and self-Harmony multiplayer shims are
deliberately not preserved. Source settings/jobs are not imported or migrated by this draft.

| Question | Type | Work remaining without interrupting the user |
| --- | --- | --- |
| BP-Q1 | Optional provider research | Pin More Events and CE identities/content/exposure, VTE effective-temperature contract and current beta compatibility; no speculative source-name binding |
| BP-Q2 | Native and life-stage research | Verify 1.7 effective insertion/job interruption, exact race injuries, toddler mobility/crib/caregiver handoff and source-removal save behavior; S031 owns full toddler mechanics |
| BP-Q3 | Proposed behavior / playability | Review native-needs interruption, source outside-area outdoor breaks, single-cause recovery completion, dry-refuge/swim preference and explicit bounded retry timing in actual play; do not ask the user to reverse-engineer them |
| BP-Q4 | Implementation and acceptance | Target predicates, provider absent/present/changed runs, global/save/main-menu/language UI, physical recovery/swim/outdoor/toddler workflows and shared-data cost remain unimplemented/unverified |

No test run, build, game launch, package change, source installation or gameplay acceptance was
performed for this documentation slice. Code-derived faults and public reports are not live proof.
