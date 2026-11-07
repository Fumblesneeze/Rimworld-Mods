# S053 — Turn It On and Off - RePowered source research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **P1-46**, inspected 2026-11-03. The [owning draft](../../../../openspec/changes/specify-megamod-repowered/)
separates source facts below from the selected composed behavior. No runtime acceptance. S052's
[LightsOut research](2026-11-03-lights-out-research.md) is the preceding completed source draft.

## Documentation, identity and retained inputs

Read [Workshop2228219259](https://steamcommunity.com/sharedfiles/filedetails/?id=2228219259),
[source/README](https://github.com/emipa606/TurnOnOffRePowered), installed About, MIT license and
English keyed text. Mlie describes combined Turn It On and Off/RePower behavior: configurable idle
draw and active multipliers, turrets active when targeting, powered doors, Project RimFactory work,
optional insufficient-power work blocking and RePower definition support. Save/performance claims
and user bug comments remain unverified. The source credits Ryder, Thom Blair III, Fluffy, Xen,
EbonJaeger, NubsPixel and Texel; MIT copyright2020 Mlie in the installed license.

Installed root `F:/Steam/steamapps/workshop/content/294100/2228219259`, package
`Mlie.TurnOnOffRePowered`, modVersion1.6.8, advertised1.1–1.6 only. No loadFolders or1.7 binary was
found in the inspected inventory. The1.6 DLL is a behavior reference, not an admitted beta provider.
Its SHA256 `A647D5C9E60D2BB71426AADAA2B0ADA6C1351CECCFFA6185C6CC8A5286DC2592` is byte-identical to
the pinned source repository's1.6 DLL. For1.6 metadata requires Harmony, no HugsLib; loadAfter still
lists both. IncompatibleWith lists EbonJaeger.RePowerReborn and ryder.turnitonoff.

Source pinned clean at `88447b6019dcbb10f0b8719ac91deed942e35eb8`, retained under ignored
`artifacts/Refinement/megamod-spec-20261103/repowered/source`; complete reference-DLL decompilation
in sibling `compiled`. The active source project targets net48, AssemblyVersion1.6, FileVersion1.6.8,
output1.6/Assemblies, with compile references to RimWorld/Harmony and VersionFromManifest. Historical
backup/migration projects exist and are not the current source implementation. No upstream code built
or executed; Workshop input remains read-only.

## Read inventory

Fully read all11 current source files: TurnOnOffSettings, TurnOnOffMod, TurnItOnandOffDef, RePowerDef,
TurnOnOffGameComponent, TurnItOnUtility, TurnOnOffSettingsGUI and all four HarmonyPatches files;
also the current csproj. Compared compiled classification/definition loaders, usage evaluators,
HasEnoughPower, safe-map access and public member inventory. Full decompilation is retained, not
a claim that every generated body was reread. Compiled informational version names commit72bf979,
but it contains the later TryGetMapSafe changes from pinned HEAD; do not infer exact build provenance
from that informational attribute. Source and reference DLL are compared evidence, not a verified
clean reproducible build.

Settings fields/defaults:

| Setting | Initial source value |
| --- | --- |
| lowValue | 10 W |
| highMultiplier | 2.5 |
| doorMultiplier | 10 |
| applyRepowerVanilla | true |
| blockUseWhenLowPower | true |
| verboseLogging | false |
| customPowerValues | Empty dictionary keyed by building Def name, Vector2 idle/active values |

ExposeData saves those exact keys; reset restores defaults and clears custom values. Custom pairs
are highest priority at runtime and in inspection, but not in the public base PowerLevels dictionary.
English UI has General/Devices tabs, device idle/active columns, search by Def name/label/mod name,
per-row reset and full reset. All current source settings are global. Generic source sliders accept
idle1–100 W and multipliers0.1–10. Device rows use positive-facing numeric controls over negative
stored values; no explicit custom bounds were passed in the inspected GUI. SaveDeviceOverrides
replaces the dictionary with current rows differing from base, losing overrides for absent/untracked
types. Search itself does not delete hidden current rows. Source full reset clears every override.

The constructor loads settings then automatically imports matching old HugsLib XML fields. It
parses floats/bools, writes current settings, removes the old mod element and saves HugsLib XML.
This is a destructive migration path with no inspected backup/atomic recovery; it was never run.
Owned behavior must not copy this automatically. WriteSettings saves pending GUI edits, clears
static variables and reruns DefsLoaded. Initialization uses Harmony PatchAll and queued Def loading;
the owned semantic-admission contract will replace unguarded installation.

The GameComponent tick starts a usage tick, returns when Find.CurrentMap is null, rescans after
current-map/building-count changes or a requested rescan and otherwise every2000 ticks. Fast,
regular and slow usage evaluations are scheduled at4/15/60 ticks; power reconciliation every60.
Utility scans all Find.Maps and colonist buildings, despite the tick's CurrentMap-null early return.
Thus world-view updates stop; offscreen new building counts can wait until a later rescan. Runtime
sets are static, without a source GameComponent save/load reset contract.
Broad catch logs an error each failing tick; do not infer a bounded owned failure policy from it.

Four source patches:

- Building_WorkTable.UsedThisTick Prefix calls AddBuildingUsed.
- JobDriver_WatchBuilding.WatchTickAction Prefix reports targetA as used.
- Building_WorkTable.UsableForBillsAfterFueling Postfix only changes a native true result to false
  when HasEnoughPower rejects it. The source forecast is detailed below.
- CompPowerTrader.CompInspectStringExtra Postfix uses tracked-building membership, PowerLevels and
  custom overrides to replace the localized PowerNeeded line with current/opposite-state values.
  It uses a five-second regex timeout and requires the matching line/newline. Preserve unrelated
  inspection content in the owned result; null/newline/localization failure cases were not run.

Public Def schema: TurnItOnandOffDef has targetDef, highPower, lowPower, poweredReservable,
poweredWorkbench; RePower.RePowerDef adds scheduledPower. Both store signed int power values and
targetDef string. Loading/merge precedence and actual producers/consumers are detailed below.

The source Utility inventory exposes PowerLevels, AddBuildingUsed, several tracked-building sets,
TryGetOverridedPowerLevels and initialization/rebuild methods. Relevant Stats In Description is
already a confirmed consumer of PowerLevels: its RePowered branch takes precedence over its
LightsOut branch. That narrow evidence is recorded in S052 and expanded below.

## Classification, rates and activity facts

RePowerDef entries load first, TurnItOnandOffDef entries second, generic classification last. Missing
targetDef quietly skips. RePower workbench/reservable flags register pairs; reservable also adds the
target to a set; scheduled adds a schedule set. No RePower flags still registers a pair. TIO entries
with neither workbench nor reservable do nothing. Duplicate rates overwrite by enumeration order.

Preset-on values are crematorium200/750 W, smelter400/4500, high-tech research100/1000, hydroponics5/75.
The first three overwrite external pairs; hydro uses special registration that preserves an existing
pair. Generic special devices include MultiAnalyzer, VitalsMonitor, DeepDrill and Television joy
buildings. Generic ordinary selection covers worktable/turret classes, powered doors, scanner
properties subclasses and selected PRF assemblers. Doors use10×, others2.5×; idle10 W. Presets Off
leaves high-tech research generic but hydroponics no longer automatically registered. AlwaysIgnored
contains Furnace/ServerRack, Building_MechGestator and classes containing ProjectRimFactory; however
external definitions were already registered. FM_AIManager is explicitly removed at the end.

Activity source paths:

| Source path | Inspected behavior / limitation |
| --- | --- |
| Worktable/TV | Tick-use callbacks, current/previous tick sets; multiple users union |
| Research/drill/scanner | Any same-faction reservation, even approach; research marks linked facilities |
| Medical facilities | Occupants of exactly HospitalBed Def, then all its linked facilities; ordinary medical-designated beds not scanned by this special branch |
| Turret | Any non-invalid current target, not only a firing tick |
| Native door | Open plus not blocked, closing readiness/hold conditions and opening progress; Open=false closing animation is missed |
| VVE garage | Public reflected OpenProgress() strictly between0 and1 |
| Doors Expanded | Reflected Open, BlockedOpenMomentary, HoldOpenRemotely, TicksTillFullyOpened |
| Hydroponics | Any Plant on occupied cells, including mature/dormant; with SelfLitHydroponics active the evaluator returns, even if power tracking remains |
| Reservable | reservableBuildings populated but never evaluated/merged into use |
| Scheduled | ScheduledBuildingsDefs populated, scheduledBuildings never populated; EvalScheduledBuildings therefore has no tracked members |
| PRF | Assembler inspection lines containing parentheses but not W, or interaction-cell machine inspection containing %)] ; language-sensitive heuristic, broad PRF class exclusion undermines auto-registration |

ApplyStateChanges and60-tick reconciliation directly assign fixed PowerOutput. Rebuild uses custom
overrides; removing tracked entries restores old idle, not native demand. Dynamic native/provider
changes can be overwritten. ClearVariables does not clear AutodoorDefs. These are static code
findings/risks, not reproduced player failures. No source bug is copied as the desired owned behavior.

HasEnoughPower returns true when disabled, untracked, power-optional, already used last tick or no
PowerNet; native false remains false in the postfix. Otherwise it permits when current net spare W
is at least the *full* active draw, or stored energy in converted units is strictly greater than
active draw×500. It ignores already-counted idle demand and the whole-network deficit in the battery
test. Fail reason is localized notEnoughPower.failreason. Owned RP05 deliberately uses incremental
whole-network demand, equality acceptance and a configurable500-tick-equivalent horizon.

## Native1.7 and optional provider evidence

Native reference Assembly-CSharp SHA256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`, build1.7.4995rev309.
Retained in sibling native: Building_Door, Building_WorkTable, WorkGiver_DoBill, PowerNet,
CompScanner, CompAffectedByFacilities and CompFacility. Read targeted activity/power/eligibility/
link bodies, not all native classes. WorkTable respects PowerOn unless CanWorkWithoutPower;
DoBill queries availability before reservation/job start. Scanner.Used is a real work callback.
PowerNet net gain includes PowerOn devices, stored energy sums batteries; retain native unit
conversion and switching. Door Tick decrements opening progress while Open is false during closing.
Facility.CanBeActive requires PowerOn when electrically powered, so readiness must avoid a circular
dependency between requesting power and already having an active facility.

Core Buildings_Production.xml source comparators (not finalized loaded Defs):

| Building | Core base W | RePowered idle/active W | Active/base ratio |
| --- | --- | --- | --- |
| Crematorium | 250 | 200/750 | 3 |
| Smelter | 700 | 400/4500 | about6.43 |
| High-tech research bench | 250 | 100/1000 | 4 |
| Hydroponics basin | 70 | 5/75 | about1.07 |

This is a substantial active-load rebalance, especially smelting/research. Source defaults are
provisional owned balance choices, exposed in settings; native finalized comparisons and real job
reachability remain RP-Q3/RP-Q4. Source claims of efficiency/performance were not measured.

- [VVE](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877),
  OskarPotocki.VanillaVehiclesExpanded: installed LoadFolders ends at1.6. DLL SHA256
  `B7665E9421011C0C4B3937A8BDB2B934807C70705CE402F2F66E3BB5323EB5B4`.
  Decompiled GarageAutoDoor fully and GarageDoor relevant movement/replacement bodies into
  provider-vve. OpenProgress is indeed a public float method; closed/opened forms replace the
  building. Native provider gizmos enforce electricity and obstruction. No1.7 support inferred.
- [PRF Revived documentation/source](https://github.com/zymex22/Project-RimFactory-Revived),
  Workshop2033979700, package spdskatr.projectrimfactory verified in About. Pinned
  `ede0db655d1f97120a6b42da265716919537ccf2` under provider-rimfactory. Read programmable assembler
  work/current-report/inspection and CompPowerWorkSetting cost sections. It has structured work
  state and independent speed/range/additional power costs; plain inspect punctuation is unnecessary
  and unsafe. This is a bounded provider sample, not all-machine or1.7 admission.
- [Doors Expanded dev documentation](https://steamcommunity.com/workshop/filedetails/?id=2195733555)
  links lbmaian/DoorsExpanded steam. Shallow source commit4546098050b43e600d40a5bdebe5c18effc3f99e
  exists in provider-doors, but sparse content checkout stalled and was cancelled. No current
  provider member or canonical package assertion from that incomplete checkout; remains RP-Q2.
- SelfLitHydroponics Aidan.SelfLitHydroponics and exact content are preceding S052 evidence.
  Its independent glow is a reason to retain native behavior until a combined power adapter is proved.

## Actual external contracts and licensing

A bounded installed Workshop/local-DLL scan checked ASCII and UTF16LE strings for
TurnOnOffRePowered/RePowerDef. Besides the source's own six version DLLs it found Relevant Stats In
Description1.4/1.5/1.6. The actual1.6 body was read in S052: package gate then reflected PowerLevels
getter, Dictionary<string,Vector2>, signed values converted to positive range. RePowered wins over
LightsOut. It does not read TryGetOverridedPowerLevels, so custom rates are absent from that range.
The source's public alias buildingsThatExternalCodeMightQuery points to buildingsToModifyPowerOn;
no additional current consumer found for mutable sets or initialization methods. A bounded search
is not a universal no-consumer proof.

XML/source searches found EPOE Forked vat.epoeforked Workshop1949064302 historical1.0–1.4
Patches/OtherMods/RePower.xml. Read1.4: old display-name FindMod gates, replace
RePowerTableSurrogates with RePowerTissuePrinter and remove RePowerUpgradingStation, successAlways.
Those gates do not prove a current RePowered or1.7 integration.

[RePower Patch Pack](https://steamcommunity.com/sharedfiles/filedetails/?id=1836577627) and its
[official source](https://github.com/emipa606/RePowerPatchPack) were read first, then cloned/pinned
`a07a2ff346b476cbbab80791ec300dbfb659d17b` under provider-patch-pack. About identifies
Mlie.RePower.PatchPack, hard dependency/loadAfter Mlie.TurnOnOffRePowered, advertised1.0–1.6.
Read EPOE, PRF, Reinforced Doors and dummy definitions; inventoried the remaining files/flags.
Unconditional RePower.RePowerDef entries target strings, with signed pairs; source loader tolerates
absent targets. Reinforced autodoor has poweredReservable=true, a real producer for the unwired
reservation contract. No scheduledPower entry found in this pack. EPOE pair is10/1000 W,
TableRoboticMachining10/900 W, reinforced autodoor5/500 W. Not all pack content has been deep-dived or
selected for replacement. Pack license is MIT; preserve its notice if future reuse is chosen.
The pack is not a new selected feature or a dependency of megamod. Its unchanged metadata cannot
be satisfied by merely recreating a Def class; exact drop-in support is explicitly unclaimed.

Source RePowered MIT notice applies to any reused source; this work specifies independent behavior
and ships none. Provider sources and compiled assemblies remain ignored read-only research inputs.

## Selected composition and remaining work

RP01–RP09 specify one S052/S053 pipeline. S052 activity wins for overlapping work equipment; S053
rates replace its percentages for admitted S053 devices. Lighting and other S052 equipment keep
their own rates. S053 disable restores those fallback policies. Generic rates track native research;
fixed pairs explicitly override those base values. Player pair > admitted external pair > preset >
generic is a deliberate correction of source external/preset precedence. Actual work replaces
ordinary reservation-only detection; declared external reservation/schedule support remains explicit.
Native restrictions, independent provider services and safe readiness remain required.

Independent document review identified three corrected draft gaps: apply the0.01 W readiness floor
to positive sub-floor standby as well as zero; partition forecast load/reserves by actual circuit
with explicit optional/required facility rules and topology invalidation; distinguish deliberate
disable (S052 fallback) from failed combined admission (native behavior for that device).
Retained PowerNet uses a1E-07 per-tick tolerance in power switching, supporting the tiny-draw risk;
facility linking does not require the same PowerNet. These are static findings, not reproduced
game failures. Added scenarios include minimum-standby blackout/recovery and separate circuits.

No bottom tab exists in source: General/Devices are global mod settings, inspection is contextual,
and verboseLogging is developer diagnostics. Owned configuration gains common inheritance and
type-pair rows; no source importer. Missing-type settings survive, unlike the source rebuild.

RP-Q1 retains effective beta targets/forecast/readiness; RP-Q2 exact optional-provider and producer/
consumer admission; RP-Q3 measured balance/default review; RP-Q4 implementation and native acceptance.
These are research/verification obligations, not requests for the user to explain source behavior.
All implementation tasks remain unchecked. Document review and validation are recorded in the ledger.
