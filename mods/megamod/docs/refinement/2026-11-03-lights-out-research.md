# S052 — LightsOut research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Origin replacement **P1-46**. Inspected2026-11-03. Source behavior analyzed; the
[owning draft](../../../../openspec/changes/specify-megamod-lights-out/proposal.md) selects the intended
result. The findings below distinguish inspected behavior from chosen corrections and remaining
runtime evidence. None is runtime acceptance.
S053 now has a [separate source deep dive](2026-11-03-repowered-research.md) and direct composed draft.

## Documentation and loading

Read [Workshop2584269293](https://steamcommunity.com/sharedfiles/filedetails/?id=2584269293),
[source repository](https://github.com/NathanIkola/LightsOut),
[organization wiki](https://github.com/NathanIkola/LightsOut/wiki/Project-Organization) and
[API landing page](https://nathanikola.github.io/LightsOut/). The wiki is older architectural context,
not proof of the selected binary. Author describes occupancy/sleep-driven lights, per-light Keep On,
standby workbenches and configurable idle/active resource rates. Historical performance/compatibility
claims and issue comments are not verified behavior.

Installed `F:/Steam/steamapps/workshop/content/294100/2584269293`, package
`juanlopez2008.LightsOut`, authorjuanlopez2008, supports1.3–1.7, hard Harmony, load hints for
ChJees.Androids and Murmur.WallLight. Its **v1.7 loadFolders.xml selects root only**: selected
`Assemblies/LightsOut.dll`, SHA256
`3495CA55336EF70B2A224B33CC414D6ACC66690FBE206FEBF76C59927BED42A7`.
Root also contains0Harmony.dll; inspect eventual package/dependency concerns without copying it.
An early class inventory was taken from1.6, then corrected to the selected root assembly before
behavior reads. Root source is not included; the initial 25-type read was followed by complete
selected-binary decompilation and source pinning, recorded below.

Source HEAD: `59e32b423be6ae71fb99061168c86c1d89ef9a02`.
The completed audit below records checkout, license and binary/source correspondence limits.
No repository/source code was built or executed.

## Core behavior inventory

Five root XML patches and English keyed text read: BuildingGizmoPatch, Anon2BuildingBasePatch,
CeilingLightsPatch, LongCeilingLightsPatch, VPE_GasLampPatch. They add KeepOnComp to abstract/native
or external bases/content, using vanilla conditionals and display-name FindMod gates. External
targets cover More Furniture (Continued), Simple Utilities: Ceiling and VFE-Power gas lights.
Owned declarative changes will need XML Extensions package/content/shape guards and additive,
duplicate-safe composition, as selected in the design.

Selected root types read:

- Boilerplate: LightsOutSettings, ModSettings, LightsOutMod.
- Common: Lights, Resources, Rooms, Tables, Glowers, Enterables.
- ThingComps/Gizmos: KeepOnComp, KeepOnGizmo.
- Power patches: DisableBasePowerDrawOnGet, PerformPendingShutoff, DisableBuildingsOnSpawn,
  RechargeablePatch, AddStandbyInspectMessagePatch, CleanupBuildingsOnDespawn.
- Light patches: DetectPawnRoomChange, DisableLightGlowOnSleep, FixBuildingsOnPawnSpawn,
  FixLightsOnRoomSetup, DisableLightGlowPatch.
- Bench patches: TriggerOnPawnArrivedToBench, EnableAndDisableTelevisions.
- ModCompatibilityManager and its 17 adapter families, detailed in the completed audit below.

Observed settings: FlickLights=true, NightLights=false, AnimalParty=false, DelaySeconds=1.5,
StandbyResourceDrawPercent=0, ActiveResourceDrawPercent=100; pending shutoff checks every30ticks.
Settings are global static values. ExposeSettings clamps active percentage to at least100, though
the editor itself accepts0; standby editor0–100, delay nonnegative. Constructor attempts read-only
old HugsLib XML recovery. Debug message keys are a configurable filter; current default concatenates
Error+Mods in one string. The owned draft does not copy the automatic import or debug-spam option.

Lights classify approved glowers with defName substrings light/lamp/illuminated, exclude sun and
power-plant/heat-pusher/schedule/temp-control/landing-beacon comps. Table classification includes
worktable/researchbench types/subclasses and DeepDrill, excludes fire/darklight overlay comps.
Televisions use three named Core Defs. Enterables are registered exact types; Occupied uses
Building_Enterable.SelectedPawn. Provider registrations can change these classifications.

Room emptiness/sleep checks count ToolUser pawns plus animals when enabled, excluding the departing
pawn and pawns at/approaching a door; no faction check. Null/outdoor/doorway/unready rooms do not
qualify. Room collection snapshots retry on modified-collection exceptions up to100 times, then
can retain a null array—an analysis concern, not a reproduced game bug. Source uses several static
object dictionaries and memoized Def-name classifications; lifecycle findings appear below.

Lights switch on room transitions, sleep transitions, room setup and spawn. Sleep-off bypasses the
normal departure delay. Keep On is individual-building state saved as keepItOn, with native gizmo;
it is not global configuration. Source control changes automatic glow/resource policy rather than
creating ordinary flick-switch jobs. Native glower false is retained; source only adds suppression.

Resource state is bool?/pending queue. Off lights return -0.01W; standby resource calculation also
clamps consumption to at least0.01W. Active rate multiplication in the main power getter applies
only to recognized tables; rechargeable handling has another path. Pending shutdown rechecks
room eligibility for lights but uses a shared queue without per-building cancellation tokens.
Despawn clears some dictionaries; pending-entry and teardown gaps are recorded below.

Bench activation uses JobDriver.Notify_PatherArrived for bills/research/drilling/televisions and
enterable jobs. Finish callbacks disable tables, with separate other-viewer checks for TV and
occupancy checks for enterables. Reload/spawn handlers attempt to restore active bill/sleep state;
one decompiled captured-variable body was malformed output. The source check below resolves the
intended closure; this is not evidence of corruption. Autonomous and shared-user findings follow.

Compatibility manager discovers17 families (Androids, VFE/standalone wall lights, generic glowers,
Ideology, Helixien gas, self-lit hydroponics, Questionable Ethics, Erin furniture, Dubs hygiene,
Extinguish Refuelables, Colony Manager, Biotech, Mining Light, SOS2, Reinforced Mechanoid2,
Vanilla Temperature Expanded). Gates use display names or no target name; components scan loaded
types by exact/substring names and patch methods. Their consumed contracts are inventoried below.

## Completed source audit

Pinned clean source checkout: `59e32b423be6ae71fb99061168c86c1d89ef9a02`, retained in ignored
`artifacts/Refinement/megamod-spec-20261103/lights-out/source`. Complete selected DLL decompilation
is retained in sibling `compiled`. Assembly is net10.0 Release, version2026.10.17.738,
file/informational version2026.10.17.0738. About XML SHA256:
`99C098D6E8C32E49740E269923218DE18A0E55F0C333F7E5E557A3FE6374E9D7`.
The source contains80 C# files; decompilation82 including generated metadata/types. Counts are
inventory, not a claim of reading every source file. All selected runtime families were read in
compiled form, including debug/settings, TablesHelper, PatchInfo/PatchType and all17 adapters.
Targeted source checks include Resources, sleep/spawn restoration, gestator state and the project.
Historical1.3–1.6 binaries exist in the checkout, but no root1.7 binary for an equality check.
No license grant was found. The project deploys via xcopy after build and copies Harmony; do not
build it or reuse its packaging. Independent implementation only.

Source clarifications: FixBuildingsOnPawnSpawn correctly closes over `Pawn pawn = toil.actor`;
the malformed decompilation is not binary-corruption evidence. Table shutdown is immediate; room
departure alone uses the delay. Despawn clears building/rechargeable/glower/Keep On entries, but
not pending shutdowns, the static sleeping-pawn map or all classification state. No complete
map/game teardown was found in these paths. General shared-user accounting is absent.

### All adapter families

Rows describe selected compiled code, not verified provider compatibility. Verified IDs were read
from root About metadata, excluding dependency entries. Source display-name gates are insufficient.

| Family | Source behavior | Identity / evidence boundary |
| --- | --- | --- |
| Androids | Building_AndroidPrinter is a table; PawnCrafterStatus()==2 bypasses LightsOut power-postfix and standby-message changes | ChJees.Androids is a source load hint; provider metadata absent |
| VE wall lights | VFEA_Wall-prefixed lights use the facing-cell room | Verified VanillaExpanded.VFEA, Workshop2654846754 |
| Wall Light | WallLight-named class uses facing-cell room; excludes MURWallLight_Glower and MURWallSunLight_Glower helper objects | Verified Murmur.WallLight, Workshop1423699208 |
| Generic glowers | Discover foreign Glow-named ThingComp types, register exact type, patch one of three ShouldBeLitNow property spellings; UpdateLit accepts zero args or Map | No package gate upstream; owned admission needs actual owner and semantics |
| Ideology | Runtime loudspeaker helper enables on RitualStarted; outcome Count disables nearby speakers; Tick periodically enables them | Count evaluates outcome and is not a reliable idle-state callback |
| Helixien gas | CompGasTrader.GasConsumption and inspect-text patches; off gas uses negative electrical MinDraw; gas wall lights use facing room | Verified VanillaExpanded.VFEPower, Workshop2062943477; its v1.7 root+1.6 selection contains no CompGasTrader in VanillaPowerExpanded.dll and selected Def search found no old gas lamp targets. Gas sign/content remains unproved |
| Self Lit Hydroponics | Exclude HydroponicsLight | Verified Aidan.SelfLitHydroponics, Workshop2152790833 |
| Questionable Ethics | Building_PawnVatGrower table; Vat-named types with CraftingProgressPercent strictly between0 and1 bypass power/inspect changes | Source gate Questionable Ethics Enhanced; canonical identity/content unresolved |
| Erin furniture | Add Andon lamp-name fragment | Verified Erin.JapaneseFurniture, Workshop2354938860 |
| Dubs hygiene | Exclude CeilingFan fragment | Verified Dubwise.DubsBadHygiene, Workshop836308268 |
| Extinguish Refuelables | Exclude tables with FireOverlayExtinguishable-named comp | Source display gate; canonical identity/content unresolved |
| Colony Manager | Exclude AIManager Def fragment | Legacy provider absent; installed ilyvion.colonymanagerredux, Workshop3310027356, is not silently equivalent |
| Biotech | Eight components below | Native optional DLC branch |
| Mining Light | Exclude MiningLight | Verified Rikiki.MiningCo.MiningHelmet, Workshop755227567; retexture package distinct |
| SOS2 | After ShipInteriorMod2.MoveShip, reevaluate unique rooms from cached lights | Verified kentington.saveourship2, Workshop1909914131; movement target not admitted |
| Reinforced Mechanoid2 | Exclude RM_GestaltEngine table fragment | Continued display-name gate; canonical identity/content unresolved |
| Vanilla Temperature Expanded | Force CompTemperatureSource.ShouldBeActive false when marked off, never force true | Verified VanillaExpanded.Temperature, Workshop3202046258; heat/glow/resource behavior must stay coherent |

An additional unconditional patch excludes any table Def name containing case-sensitive Furnace.
These heuristics indicate continuous-work protections; they are not robust owned classification.
Many adapters patch LightsOut's own methods. Their owned equivalents must be direct policy calls.

Biotech details:

- Band nodes use tunedTo/tuningTo on spawn and TuneTo; tuned or tuning stays active.
- Gene assemblers enable themselves and connected GeneProcessors on Start; Finish disables all.
  Source does not account for another assembler using a shared processor.
- Gene extractors register as enterables; Finish/Cancel disables them.
- Growth vats register as enterables; selected embryo/pawn enables on spawn/embryo selection;
  OnStop disables. Selected occupants express readiness, not proof of current processing.
- Mech chargers use StartCharging/StopCharging and private IsAttachedToMech after spawn.
- Gestators enable at forming start, disable at completion/ejection. AfterSpawn's UpdateWorking
  enables a forming-but-off device, but its else-if disables an already-on one even while forming.
  Pinned source agrees; this is a source-derived risk, not a reproduced save bug.
- Subcore scanners register as enterables; EjectContents disables.
- Atomizers use !Empty on Notify_HauledTo/DoAtomize/EjectContents/PostSpawnSetup.

### Native beta cross-check

Input `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`,1.7.4995 rev309, SHA256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`; retained `native` directory.
Read full CompPowerTrader, CompProperties_Power, CompRechargeable, CompGlower, CompLoudspeaker,
RitualOutcomeComp_NumActiveLoudspeakers; targeted work/power/sleep/room portions of
Building_MechGestator, Building_GeneAssembler, Building_GrowthVat, Building_MechCharger,
Toils_LayDown and RegionAndRoomUpdater. Unread portions are not claimed as analyzed.

- Native PowerOutput preserves EMP suppression; flick/breakdown/PowerOn remain availability rules.
  PowerConsumption already applies research upgrades. Source rechargeable reflection of raw base
  consumption bypasses that calculation; owned policy uses the effective native baseline.
- Native idlePowerDraw exists. Gestators choose active/idle by forming, growth vats by Working,
  gene assemblers by recent actual work. Avoid double discounts and preserve phase differences.
  This native idle support does not implement room occupancy lighting.
- Source Resources.MinDraw comment explains0.01 W as protection against incorrect no-power handling
  and repeated bench pathing. Retain the documented electrical floor pending native proof; do not
  advertise literal zero or guess a replacement. Gas units/signs require separate evidence.
- CompGlower requires spawned/flick-enabled state and all IThingGlower approvals. Occupancy adds
  suppression only. Native color/radius/darklight changes remain authoritative.
- LayDown still uses tickAction in this beta: no suspected tickInterval mismatch here.
  FloodAndSetRooms takes District start and Room room.
- Loudspeakers already use CompAutoPowered with real ritual demand. Outcome counts powered presence
  over time. Reading or predicting outcome must never change power/activity.

### Actual external consumers

A broad binary LightsOut scan found two consumers missed by the first narrow namespace/package
scan. Neither is S098 Storage Stats in Description. Relevant paths were decompiled; no whole-consumer
audit is claimed. Public docs: [Relevant Stats](https://steamcommunity.com/sharedfiles/filedetails/?id=2692669482)
and [WOWGAG](https://steamcommunity.com/sharedfiles/filedetails/?id=3797541313).

| Consumer | Consumed contract | Owned decision |
| --- | --- | --- |
| Relevant Stats In Description, Mlie.RelevantStatsInDescription, Workshop2692669482; installed1.6 DLL SHA256 4F6448955BB2243CA5585EEDF77188FC47E22389ADA3F85424DA7AAFD05B7A2F | Checks juanlopez2008.LightsOut; reflects DisableBasePowerDrawOnGet.Postfix(CompPowerTrader, ref float, bool?) and ModSettings.ActiveResourceDrawRate. getMinMaxPower invokes postfix with false and uses hasInteractionCell for active estimate. Repowered branch wins. Descriptions are cached | Preserve correct displayed estimates through an admitted narrow consumer adapter and owned calculation/cache refresh. No package spoof or calling a patch as API. Installed version advertises through1.6; beta binding unverified |
| WOWGAG's Performance Patch, wowgag.guiperformancepatch, Workshop3797541313; selected1.7 DLL SHA256 FFE765933932B6BF887D631A43B8880DDA3E8B33F5586D2561392C441BE5CCF9 | GppLoadTrim28.InstallLightsOut finds ModCompatibilityManager.GetTypesToPatch() and replaces its IEnumerable<Type> result with a cached list; absent type returns without attachment | No facade for an upstream startup scan our direct adapters do not use. Other performance features remain independent; no whole-mod compatibility claim |

Retained consumer paths: `consumer-stats`, `consumer-gui`. Public Resources/Tables/Lights/Glowers
APIs and keepItOn save key are additional source surfaces; this installed scan found no other binary
consumer, which does not prove none exist. Source API compatibility is not a blanket drop-in claim.

### Selected deviations and remaining evidence

Keep On remains building state. Operational lighting control moves to bottom right; six actual
configuration defaults use common global/save rows. No source bottom tab was found. Diagnostic
tools belong at the top. Do not copy automatic HugsLib import, unbounded debug filters or broad
type-name scanning. Planned XML is additive and XML Extensions guarded. Simple Utilities: Ceiling
Adjusted uses verified Owlchemist.CeilingUtilities but advertises through1.6 and differs from the
source display gate. More Furniture and other missing identities stay explicit research gaps.

The draft cancels obsolete shutdowns, accounts for shared users, preserves native restrictions and
research upgrades in its rate scope, and leaves unknown/continuous devices native. S053 uses this
feature's activity for shared work devices and supplies their costs once; its absolute W choices
are explicit overrides, while generic rates remain research-adjusted. Lamps and other S052
equipment retain these rates, also used as fallback when S053 budgets are disabled. LO-Q1–4 retain
effective beta admission, optional/consumer contracts, verification of the now-specified S053
composition and native power/UI/lifecycle acceptance. The shared floor is capped to native positive
demand below0.01 W as an owned correction; source code does not establish that cap.
No upstream code was built, game launched, normal configuration changed or Workshop file modified.
