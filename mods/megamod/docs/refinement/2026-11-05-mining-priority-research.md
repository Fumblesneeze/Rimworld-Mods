# S097 — Mining Priority (Continued) research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Replacement P2-26. Inspected2026-11-05. [Owned draft](../../../../openspec/changes/specify-megamod-mining-priorities/)
is observable behavior; code analysis and proposed integration remain separate. Nothing implemented
or game-verified. No source build, game launch or normal configuration edit occurred.

## Documentation and exact provenance

Read the [continued Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3610712721)
first, then About/readme/language documentation and linked
[public source](https://github.com/Halicade/RimWorld-MiningPriority). The page describes prioritizing
scarce resources, protecting yield, relaxing the threshold, finishing partly mined targets and
nearly exhausted drills. Continued notes mention caching improvements and a separate drill
restriction option. Its FAQ about disabled right-clicks is not proof of the selected1.7 route.
The page links original Workshop1345928533 and identifies Halicade as continuer; About retains Uuugggg.

| Input | Inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3610712721` |
| Name / package | Mining Priority (Continued); `Uuugggg.MiningPriority`, canonical `uuugggg.miningpriority` |
| Support / dependency | About1.6 and1.7, required Harmony; no LoadFolders or runtime XML patches found |
| Selected assembly | `1.7/Assemblies/Mining_Priority.dll`, assembly/file1.0.0.0 |
| DLL SHA256 | `0463E18FBB43DB70E95AA96425A2465A0143A5795D50683805A19ACB96948EFA` |
| About SHA256 | `49F1B5134F2ADE54900C553C12EC6B3CB54AF5A31B0795023FA1DB98B7405F4C` |
| Public commit | `a1429a00feadaad8123950883b594a546bbc75c7`,2026-10-16; checkout1.7 DLL is byte-identical |
| Native target | RimWorld1.7.4998rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Complete selected source classes, utility/debug helper, project and generated assembly metadata were
read, then all selected compiled types. Public project targets net10.0/Harmony3rc3 with private-false
game references; FileVersion property says1.6.6 but GenerateAssemblyInfo=false and actual assembly
attributes say1.0.0.0. No behavior is inferred from that stale project property. No LICENSE/COPYING
was found in tracked checkout or installed inventory; public source is an inspection input, not
permission to copy code/art. Independently implement behavior. Ignored evidence is under
`artifacts/Refinement/megamod-spec-20261105/mining-priority/{upstream,binary,native}`.

## Source target ordering

`Mod` reads global Settings and unconditionally PatchAll owner
`Uuugggg.rimworld.Mining_Priority.main`. Debug-only logging is compiled out of active call sites.
No bottom tab, gameplay window, per-item saved component or runtime Def mutation was found.

Both priority postfixes patch base WorkGiver_Scanner.GetPriority(Pawn,TargetInfo), filter their
respective WorkGiver_Miner/WorkGiver_DeepDrill instances and replace the prior result. Mining uses
building.mineableScatterCommonality and average mineableScatterLumpSizeRange. Drilling uses the
next resource's deepCommonality and deepLumpSizeRange. When its rarity option is on, score is
**commonality0 → -5; otherwise -commonality − averageLump/10000**. This is occurrence rarity,
not market value. With rarity off the initial score is0.

Mining's continueWork adds **(MaxHitPoints−HitPoints)/1000000**, so it favors absolute damage,
including damage from other causes, rather than fraction complete. Drilling's finishUpDrills
subtracts **remainingFiniteCount/1000000**, but only sums the first **9** GenRadial cells clipped
to the map, across all resource types. It does not inspect portion progress. Static risks:

- Weighted terms can reverse intended rarity/lump/progress precedence for close or extreme values.
- Nonzero commonality greater than5 can put a resource behind the commonality0 sentinel.
- With drill rarity off, fallback rock has count0 and can outrank finite deposits when exhaustion
  preference alone is enabled; inexhaustible is not the same as exhausted.
- Native drill extraction reaches21 radial cells, so9 misses remaining extractable resources.

`Prioritized_Patch` transpiles JobGiver_Work.TryIssueJobPackage, locating the last local whose exact
type is WorkGiver and injecting a helper after every scanner.Prioritized getter call. It does not
prove consumed local/data flow, expected call count or effective foreign changes. Helper ORs
surface priorityMining/continueWork for miners; for drills it incorrectly ORs **priorityMining**/
finishUpDrills instead of priorityDrilling. Thus deep rarity alone can fail to activate priority
selection. Postfixes overwrite provider priority even when their own options yield zero. These
are source/binary facts and static risk findings, not reproduced game failures.

## Worker restriction and cache

Mining JobOnThing prefix skips restriction if qualityMining=false or forced=true. Otherwise it
applies only with building metadata, mineableYieldWasteable=true and nonnull mineableThing. Drilling
HasJobOnThing prefix skips if qualityDrilling=false or forced=true, or if a present CompDeepDrill
reports no valuable finite resources. Its nullable comp expression means a missing comp does not
take the intended harmless return; do not copy it as absent-safe provider handling.

`IsGoodMiner` compares pawn.GetStatValue(MiningYield) against cached bestMiningYield. Recalculation
enumerates PawnsInFaction(OfPlayer) on pawn.Map; a pawn qualifies if it **is the caller**, or has
workSettings, active Mining, a normal workgiver whose exact GetType equals the requested route,
and passes the busy option. Ignore busy true admits only jobs exactly Mine or OperateDeepDrill,
apart from the caller shortcut; idle other pawns are therefore omitted. It takes Max and multiplies
by qualityGoodEnough. No explicit downed/drafted/capacity/target-access check exists in that pool.
Subclassed workgivers do not match its exact-type check.

One static storedMap/lastMineCheck/bestMiningYield serves **both routes and all callers**. It refreshes
on MapHeld change, backward game time, or strict lastCheck < ticks−refreshCacheTime. No work route,
settings, worker/job/health or target key is included. Settings or membership changes can stay stale
for2500ticks by default; within that interval a mining caller can reuse a drilling benchmark. Busy
filter plus caller exception makes the cached answer depend on who caused recalculation. Same-map
repeated checks are cheaper, but this is not proof of correct shared caching or measured performance.
Failed comparisons set `TD.JobFailReasonNotBestMiner` and skip the native job check, potentially
obscuring a more basic native failure. Existing running jobs are not intercepted.

## Settings and UI

Fields and Scribe fallback defaults agree:

| Field / saved key | Default | Source UI |
| --- | --- | --- |
| priorityMining / same | true | Mine in order of value |
| priorityDrilling / same | true | Drill in order of value |
| continueWork / same | true | Prefer partially mined targets |
| finishUpDrills / same | true | Prefer deep drills nearer exhaustion |
| qualityMining / same | true | Restrict mining with yield to best miners |
| qualityDrilling / same | true | Separate drilling restriction |
| qualityMiningIgnoreBusy / same | false | Shown when either yield restriction is on |
| qualityGoodEnough / **priorityGoodEnough** | 1.0 | Continuous0–1 slider, whole-percent display |
| refreshCacheTime / same | 2500 | Numeric ticks field, buffer recreated each draw |

Source catalogs are English, German, French and ChineseSimplified. The shared explanation claims
yield rises only through skill8, which current1.7 data contradicts. No standalone operational toggle
or bottom tab exists. Owned settings retain eight gameplay preferences through common global/save
inheritance, clarify rarity/relative yield and use whole-percent slider steps. Cache-refresh timing
is deliberately omitted because freshness is an implementation obligation. Individual designations,
HP, jobs and drill progress remain native gameplay state, not settings.

## Native1.7 evidence

Targeted native reads cover WorkGiver_Miner, WorkGiver_DeepDrill, WorkGiver_Scanner, MineAIUtility,
JobGiver_Work's relevant selection/permission/forced routes, FloatMenuOptionProvider_WorkGivers,
JobDriver_Mine, JobDriver_OperateDeepDrill, Mineable, CompDeepDrill and DeepDrillUtility. Core XML
reads include Buildings_Natural, Stats/Stats_Pawns_WorkGeneral and Items_Resource_Stuff.

- Miner candidates come from Mine/MineVein designations with exposed adjacent walkable cells.
  MineAIUtility validates actual mineability/designation/reservation/ideology and usable mining
  position; it can first return HaulAside to clear a blocking item. No ownership policy should
  manufacture mining permission from rank or an invented full-map ore search.
- Native miners and drillers use Danger.Deadly on their actual work paths. Preserve actual native
  danger/access, not an invented universally safer or unrestricted path. Drill eligibility includes
  faction/building/reservation, CanDrillNow, no uninstall designation and no fire.
- Scanner.HasJobOnThing delegates to JobOnThing with its forced argument. Native float-menu work
  passes forced:true to both checks. The selected source therefore permits initial explicit
  lower-yield orders, contrary to the older disabled-right-click FAQ; native capacity/path restrictions remain.
- Native GiverTryGiveJobPrioritized renews active prioritized work through HasJobOnThing/JobOnThing
  with default forced:false; TryIssueJobPackage sets playerForced only after the job is returned.
  Source yield restrictions can consequently block renewals even though initial orders bypass them.
  Native drilling jobs have1500tick expiry with checkOverrideOnExpiry. The owned draft deliberately
  honors the override during the same still-active explicit order's renewal, without extending
  native order lifetime or relaxing native permission.
- JobGiver_Work preserves work priority levels, validates candidates and calls prioritized global
  reachable selection only when scanner.Prioritized. It also has a scan-cell priority branch.
  Source injection cannot be accepted just because current original code has the expected getter.
- Mineable accumulates yieldPct from the fraction of HP removed by each worker and that worker's
  MiningYield. Mining ends through DestroyMined with physical drops/history/records, not a simple
  last-miner skill test. Preserve earlier contributions; assignment cannot undo wasted yield.
- JobDriver_Mine uses native mining damage80 for natural rock/40 otherwise, interval based on
  MiningSpeed, ordinary Mining XP0.07 per working tick, and saves ticksToPickHit. No source patch
  changes those values. S097 preserves them rather than defining a new work mechanic.
- CompDeepDrill accumulates progress and weighted yield from DeepDrillingSpeed, produces when work
  passes10000, debits actual finite count, places physical output and retains portion state in saves.
  Native operation learns Mining0.065 per working tick. Exhaustion may forbid nearby empty drills;
  priority must not undo that native decision.
- DeepDrillUtility scans **21** radial cells for its actual next finite resource, otherwise derives
  fallback rock from terrain/biome. Current source exhaustion scans only9. Use the exact supported
  provider footprint; counting its facts must not change extraction or scan new locations.
- MiningYield currently has minimum0/maximum1.25 and skill factors0.6 at0,1 at8,1.13 at20, plus
  manipulation/sight and optional Dig factors. Actual effective stat is authoritative; no fixed
  skill8 or100% clamp belongs in the owned restriction.

## Nearby balance comparators

These are inspected Core inputs, **not finalized loaded-Def or gameplay evidence**. Keep actual
modded values and distinct surface/deep occurrence. A material's price is not the rank.

| Comparator | Surface occurrence / mean lump | Deep occurrence / mean lump | Native significance |
| --- | --- | --- | --- |
| Plasteel | 0.05 /5 | 1 /6 | Surface rock8000HP,40 base resource yield; deep portion10 |
| Compacted machinery | 1 /4.5 | No ordinary deep resource entry selected | 2000HP,2 base components; smaller lumps precede equal-frequency steel |
| Steel | 1 /35 | 4 /25 | 1500HP,40 base surface yield; deep portion45 |
| Ordinary nonresource rock | No positive ore occurrence | Inexhaustible native fallback | Not protected by finite/wasteable-resource restriction |

The draft preserves actual work, yield sensitivity, power and progression. It intentionally makes
priority comparisons lexicographic, gives unknown-rarity resources a documented place, counts the
complete drill footprint and excludes unsuitable workers. Strict100%/busy-off can intentionally
reduce throughput; normal rock work, adjusted relative threshold and explicit orders provide
ordinary recovery. Final MNP-Q3 must compare finalized provider data, actual work reachability and
no extra output; no balance or compatibility acceptance follows from this table.

## Public contracts and consumers

Public namespace/assembly `Mining_Priority`: Mod.settings, Settings and its nine fields, both
GetPriority patch classes' Priority(float,IntRange), miner restriction IsGoodMiner(Pawn,Type),
Prioritized_Patch.TranspilerPostfix(bool,WorkGiver), plus patch entry methods. Public
TD.Utilities.Listing_StandardExtensions has int/float SliderLabeled overloads. These are callable
surfaces, not evidence another installed mod consumes them. Source settings are global and no
owned per-map/entity save schema was found.

A completed bounded normal-string `rg -a -l -i` scan of installed Workshop `.dll/.cs/.xml` for
Mining_Priority and Uuugggg.MiningPriority matched only this package's About and1.6/1.7 assemblies.
No external consumer was found within that scope; this is not an exhaustive UTF16/reflection or
universal absence claim. Preserve proven consumers if later found, but do not invent a facade or
spoof source identity. The original/continued package is the same canonical coexistence identity.
No explicit custom-provider adapter or external hard dependency beyond Harmony was found.

## Selected changes and remaining work

Selected changes: exact comparison precedence; unknown-rarity resource category; complete drill
footprint; finite-before-fallback exhaustion; independent drilling toggle; current route/target-
suitable yield benchmark; explicit idle/work-seeking busy semantics and valid empty-to-ready
population handling; coherent manual override at initial start and native renewal of the same
active order; common settings/seven locales; no stale source TTL or private copied cache. These are
documented corrections/extensions, not silent source parity. Preserve source absolute-damage
progress meaning and actual native extraction rather than changing those mechanics by inference.

Compose directly with S008 mining plans, S058 work-selection infrastructure, S019 shared worker facts
without its different skill/bill policy, S052/S053 drill power/activity and S021/S033 physical indexes.
MNP-Q1–4 retain exact admission/read-only probes, optional contracts/license, finalized comparators
and reviewed live acceptance. All implementation tasks remain unchecked.

Two independent documentation reviews covered behavior/composition and source/native provenance.
Review clarified the valid empty-to-ready population and the native explicit-order renewal boundary;
both focused re-reviews found no issues. Strict typed OpenSpec validation passed119items with zero
failures. This verifies the draft's structure and review state, not implementation or gameplay.
