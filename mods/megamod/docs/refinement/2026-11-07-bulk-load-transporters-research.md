# S133 — Bulk Load For Transporters research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Inspected2026-11-07, read-only. **Specification only**, no build, installation or game run.
[Change](../../../../openspec/changes/specify-megamod-bulk-transporter-loading/),
[coverage](../replacements/bulk-load-transporters.md), [progress](specification-progress.md).

## Source and evidence

Public documentation was read first: [Workshop3559292908](https://steamcommunity.com/sharedfiles/filedetails/?id=3559292908)
by Ilarion advertises batched pod/shuttle/portal/vehicle loading, multiple workers, carrier unloading,
PUAH inventory pickups and handling/recovery options. Its performance, fork compatibility and
safe-removal assertions are author claims, not accepted results. Comments identify robot concerns
and a missing-PUAH-component fallback fix. That guard exists in inspected loading entry points.

The [public repository](https://github.com/Ilarion33/BulkLoadForTransporters) root/brief README and
[MIT license](https://github.com/Ilarion33/BulkLoadForTransporters/blob/main/LICENSE) were read;
license copyright2025 Ilarion. No immutable public revision or full public source tree was pinned:
tree access was restricted and direct fetch routes failed. The installed DLL supplies the complete
behavior inspection; do not assert it equals current public main. No upstream code/assets copied.

Installed root `F:/Steam/steamapps/workshop/content/294100/3559292908`. About names
`Ilarion.BulkLoadForTransporters`, author Ilarion, supports1.6/1.7, no modVersion. Flat assembly
version1.0.0.0. Required Harmony and `Mehni.PickUpAndHaul`; load-after also names
`teemo.pickupandhaulforked`, `memegoddess.pickupandhaul`, `mehni.pickupandhaul.unoffical` and
`SmashPhil.VehicleFramework`. These declarations are provenance, not owned dependency requirements.
Local English/ChineseSimplified translations; no local source, license, README or LoadFolders found.

| Inspected input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 1885 | `f3e6b8470b643c44cebc4551d496aaa06496bccd8bdb046309eb4e8525706e83` |
| Assemblies/BulkLoadForTransporters.dll | 189440 | `aa46929c3cfe4e27528f6fbf877f160b3e18783f1ed1349e7a4f408bfb4a19e1` |
| Defs/BulkLoad_Jobs.xml | 932 | `d7977188b64ded77bab3d5826d92adb25074acbffc838a550675090f847d7143` |
| Languages/English/Keyed/BulkLoad_Keys.xml | 11595 | `167ef69f74c8e1e47d4c4d0b8a68ed38a41846d41abbb1b795cef09e097a05b8` |
| Native Assembly-CSharp.dll,1.7.5000rev1275 | 15636480 | `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |

Ignored evidence: `artifacts/Refinement/megamod-spec-20261107/bulk-load-transporters/` contains
input manifest, all73 decompiled C# files, read-file inventory and focused native extracts. Native
inspection covers transport/portal job utilities, transport/shuttle/portal classes, loading dialog,
base loading driver, native carrier eligibility/inventory protection and PortalContainerProxy.
These are static behavioral findings; no reported defect was reproduced live during this stage.

## User decision: portals excluded

On2026-11-07 the user explicitly rejected including portal loading. The owned contract therefore
covers pod/shuttle batching and carrier unloading only. No portal job, menu, automatic redirect,
adapter or patch is selected. Ordinary game portal behavior remains outside this feature's authority.
The source/native portal findings below remain historical research, not deferred implementation tasks.
Vehicle loading is separately deferred because its advertised adapter was not found in this DLL.

## Complete source behavior inventory

| Source paths/classes | Code-derived behavior and significant boundaries |
| --- | --- |
| Main; DefInjection | Unguarded PatchAll; startup mutation of all transporter/MapPortal Def comps; checks comps before a later null guard. Owned design rejects both patterns |
| CompBulkLoadable / Portal | Float-menu eligibility,60tick per-pawn cache; normal load plus transport-only continuous mode. Requires PUAH comp, undrafted/manipulation/Hauling work, target reservation/reach. Disabled reasons distinguish claims/carried cargo/no work. Click interrupts with Wait2 before deferred planning; may drop the entire PUAH bag if directed unload cannot be created |
| WorkGiver_Utility | Item-type demand/claims, native candidate listers, bookcase fallback; one-frame pawn/group cache; missing PUAH comp falls back for pod/portal job interception. Failed planning can return Wait2 as success. Directed cargo-first probe is discarded by menu; later opportunity scan can choose another target |
| LoadingPlanner | Scans HaulableEver and spawned pawns, adds selected books from bookcases; greedy exact/fungible and fallback matching. Backpack budget is min free mass/group mass. First N rounds path-test N nearest candidates; later nearest heuristic. No reachable candidate in shortlist ends search; a too-heavy chosen item ends packing. Additional hands are planned outside combined backpack/destination budget; zero-count pawn bookmark signals hand mode |
| Global_Utility | Colony/home/stockpile eligibility; unbackpackable live pawns and optional corpses. IsFungible excludes quality/Stuff/damage/art/ingredients/genetics/biocoding/taint/pawns/corpses, but FindBestMatchFor still falls back to same Def for stackLimit>1. Exact membership first does not make fallback variant-safe |
| CentralLoadManager | Game component saves map→integer task ID→per-Def needed/claimed and pawn claims. One pawn's new claim replaces its previous one for that task. Per-map group/portal integers are not namespaced; no variant-level commitment identity. Postload removes null/invalid quantities but does not reconstruct current valid job ownership |
| Adapters | Transport group sums leftToLoad, mass capacity/usage; MapPortal owns one request, floatMax capacity and forwarded container. Source aggregate capacities do not enforce individual member capacity at deposit |
| JobDriver_BulkLoadBase / transport / portal | Saves hauled/surplus/original-PUAH item references, mode flags, unload transferables and per-Def needs. Reserves as many as possible and returns success. Replan toil claims existing queues rather than recalculating. Job.loadID==1 is continuous mode. Separate pickups then item-by-item hand presentation/deposit |
| Pickup/container toils | Release pickup reservation before splitting; inventory mass recheck, whole-reference cargo tracking and same-type demand caps. Last/hand-mode item uses hands without combined limit. Held source is dropped as one item via holder before pickup. Several placement results are ignored; periodic changed-demand check in Goto only logs |
| Unload/session/deposit toils | PUAH whole-stack references become job cargo; PrepareNext removes carried item from hauled tracking before wait/deposit. Transport deposit measures accepted type-count delta and attempts to avoid duplicate native subtraction. Portal deposit decrements requested amount regardless of partial/failure result. Surplus goes to bag/ground; failed placement is not consistently reconciled |
| EndUnloadSession | Transport continuous mode searches ten cells around current pawn for another nearby group, so it can move beyond the chosen group; no equivalent portal continuation |
| Carrier workgiver/driver/toils | Native eligibility first; specialized CompMechCarrier native fallback; excludes carrier's active loading/hauling/unload jobs, occupied worker hands and another owned unloader. Reserves carrier only if configured; item selection scans all inventory, with whole last/hand-stack fallback exceeding mass fit. Native protected FirstUnloadableThing portions are not used by the replacement loop. Successful finish queues storage haul or drops hands, registering inventory cargo in PUAH |
| ClaimLifecycleUtility and job/queue/map hooks | Reconcile remaining current/queued loading claims, release on removal/despawn/map teardown; current-map and aggregate-claim limitations remain. EndCurrentJob salvage only recognizes a hand reference still in HauledThings, creating a vulnerable interval after PrepareNext |
| SafeUnloadManager / ScribeSaver patch | Before saving, replace managed active jobs with Wait2 and remove queued managed jobs when enabled; not evidence of universally safe mod removal |
| SoftlockCleaner | Rebuild player-pawn queue every1800ticks, inspect one per tick; drop whole tracked bag items if hauling unavailable or mech charging/shutdown/dormant. Skips managed jobs/drafted pawns; no owned per-portion supply protection |
| TransporterUtility_AllSendables / portal enumeration | Append physically carried tracked cargo for matching active jobs, deduplicate reference; shuttle-required hand item also included. Does not by itself solve quantity/variant protection or stale job refs |
| AnyPawnCanLoadAnythingNow | Counts a matching owned job as active loading; stale jobs can suppress native cannot-load messages |
| CompShuttle_CheckAutoload | Temporarily writes private autoload=false while claims exist then restores in postfix; can defer requirement refresh, lacks guaranteed restoration on an original exception |
| JobGiver_EnterTransporter | Blocks boarding while potential bulk work or any group claims exist; no proof stale claims eventually resolve |
| BoardingInventorySync | On shuttle pawn arrival, transfers matching PUAH inventory whole-reference portions into shuttle; no private-supply quantity distinction, relies on native notifications |
| SmartSubtraction / portal counterpart | During global managed-unload flag, match cargo and reflect private transfer-count/edit-buffer fields; skip original. Current native methods return matching demand subtracted, which the prefix does not set; this is not physical container acceptance. Transport source also recreates completion notification |
| Dialog_LoadTransporters_DistributionFix | Transpiler patches first ceil/share local pattern; postfix clears pending requests, merges by source-row references/order, allocates heavy units by projected fill ratio/count. If no destination fits, deliberately tries again without capacity requirement. Not atomic and not a guarantee of preserved foreign request state |
| Selector / InspectPane auto-open | Single selection schedules Contents, or Gear for trader-caravan-role carrier; next draw applies hardcoded tab type then clears pending type. Source does not validate selection generation or actual replaced Gear tab |
| Settings UI / diagnostics | Global options below; log category functions only in DevMode, message generator called twice. No source bottom main tab; configuration, float-menu actions and auto inspector opening |

## Defaults and selected differences

| Source setting / saved key | Source default / UI range | Owned result |
| --- | --- | --- |
| enableBulkLoadTransporters | true | Common initial pod/shuttle default; bottom-right current operational choice |
| enableBulkLoadPortal | true | Omitted by explicit user decision |
| enableBulkLoadVehicles | true; serialized/reset but no UI/control flow beyond unused detection property found | Deferred; no supported vehicle adapter inferred |
| enableBulkUnloadCarriers | true | Common initial carrier default; bottom-right current operational choice |
| AiUpdateFrequency | 60;10–240 | No duplicated tuning setting; recheck eligibility at action boundaries, bounded planning internally |
| visualUnloadDelay | 15;0–30 | S0213ticks per unloaded portion plus native destination handling; carrier extraction native10ticks; no second cosmetic delay |
| cleanupOnSave | true | Persist/reconcile owned state; no blanket save-time job interruption |
| enableSoftlockCleaner | true | S021 per-portion bounded recovery,600tick unchanged-problem retry |
| cheatIgnoreInventoryMass | false | Omitted; actual capacity always applies |
| opportunityScanRadius | 90;30–150;150 labeled Unlimited | No separate scanner/settings; S125 parent-job bounds; explicit orders stay at chosen group |
| pathfindingHeuristicCandidates | 3;1–6 | Internal budget only; unreachable shortlist must not permanently hide farther eligible candidates |
| enableContinuousLoading | true | Contextual until-complete for chosen pod/shuttle request; no ten-cell cross-group continuation |
| autoOpenTransporterContents / autoOpenCarrierGear | true / true | Common preferences; current selection/actual-tab validation, eligible carrier rather than trader-role guess |
| reserveCarrierOnUnload | false | Coordinate exclusive extraction safely, no player option to permit duplicate extraction |
| logPlanner / logWorkGiver / logToils / logManager | all false | Developer-only top toolbar, not ordinary mod settings |

All source settings are global ModSettings with same-named Scribe keys. Owned defaults, current-save
operational choices and entity orders remain separate; no source-key import is promised. Every
selected timing/balance/UI change appears in the observable spec. No new resource/skill/work priority,
travel/launch authorization or carrying capacity is granted.

## Native1.7 findings and implications

- `LoadTransportersJobUtility` uses actual selected transferable things, existing native loaders'
  initialCount commitments, manipulation/reach, forbidden/reservation and native pawn carriage.
  `JobOnTransporter` sets ignoreForbidden, but discovery still filters; preserve actual forced/native
  semantics rather than copying only one flag. Native held-source access exists in base loading jobs.
- `CompTransporter.AddToTheToLoadList` uses PodsOrCaravanPacking equivalence. Add and merge callbacks
  subtract, invalidate mass and retain quest/native effects. `SubtractFromToLoadList(Thing,int,bool)`
  returns the matching outstanding amount subtracted as an int, not physical transfer acceptance.
  The loading dialog uses that result to identify surplus already in the container. Required
  positive count, callback timing and return value need admission separately from actual transfer proof.
- Dialog distribution uses Ceil(remaining/groupCount); its non-final share is not always clamped to
  positive remainder. Static example five units/four destinations gives2,2,2 before a negative final
  remainder. Runtime consequences need reproduction; owned totals must remain five. Request edits
  also subtract actual loaded contents and drop surplus, so a standalone postfix reset is inadequate.
- Shuttle autoload rebuilds outstanding requirements from actual delivered contents, candidate/in-transit
  goods and current quest requirements. Turning autoload off while claims exist is not equivalent to
  truthful in-transit tracking. Boarding must not wait on stale quantities indefinitely.
- Native carrier eligibility requires non-self/non-free-colonist target, UnloadEverything, matching
  faction/host, not forbidden/burning and a reservation. `FirstUnloadableThing` preserves drug-policy,
  inventory-stock and food portions. Native extraction waits10ticks; base container loading uses
  destination HaulToContainerDuration. Owned batching retains these distinct costs and S021 handling.
- **Excluded portal evidence:** native haul-to-portal waits90ticks. MapPortal's holder is a
  `PortalContainerProxy`, always Count0: TryAdd calls Notify_ThingAdded then TryDropSpawn on the other
  map and returns true without checking placement. It is not a physical warehouse container. Source
  unchecked transfer accounting therefore needs special semantics, but no portal implementation is
  selected following the user's exclusion. Do not infer ordinary portal behavior is a new cheat added
  by this source; the user decision limits what the mega-mod integrates.

## Contracts and consumers

| Exposed contract | Evidence and selected treatment |
| --- | --- |
| ILoadable / IManagedLoadable | Requests, capacity/usage, map, native transferables, task ID, physical container/parent, abstract demands; useful analysis boundary, no legacy type/assembly facade promised |
| IBulkHaulState | Lists of hauled/surplus Thing references, add/remove and original-PUAH tracking; insufficient for owned protected/mixed portions |
| Extensibility_Utility | Public scanner and adapter-factory lists; default transport/portal factories. CreateGroupAdapterFor hardcodes two native types and does not consult factory list |
| JobDefRegistry | Public loading/unloading JobDefs, sets, ThingDef→JobDef map and OnRegisterDefs event; event fires during static initialization, unsafe to assume late consumers can register successfully |
| PUAH references | CompHauledToInventory.GetHashSet/RegisterHauledItem, source settings and named jobs; direct S021 composition replaces these outgoing bindings. Existing S021 Continued/Reworked/Armful/While You're Up research remains authoritative |
| Source save/job identity | LoadTransportersInBulk_Load, UnloadPackAnimalInBulk, LoadPortalInBulk; drivers and claim component save source types/keys. No automatic source save migration or removal guarantee |

Bounded read-only installed Workshop/local searches for BulkLoadForTransporters in XML/C# and
assembly bytes outside this source returned no incoming consumer. This misses constructed names,
external/uninstalled consumers and some encodings; it is not proof of universal absence. Complete
source inventory contains only transport/portal adapters, and vehicle references only the package
detection property and stored setting. Vehicle API/support remains unestablished; no generic
registry or Vehicle Framework runtime dependency is selected merely to match an advertisement.

Original Bulk Load active → withhold owned overlap, preserve source/native control and warn. S021
handles its own upstream hauling-provider authority; the new feature cannot assume any PUAH fork
is a compatible mixed-quantity adapter. Optional DLC and specialized carriers retain native fallback.
No bottom tab, source comp injection, source ID spoof, destructive XML list replacement or owned
self-patch stack is selected. Per-patch semantic target documents/tests are implementation tasks.

## Open questions and acceptance

| ID | State / remaining evidence |
| --- | --- |
| BLT-Q1 | Resolved source inventory: author docs, all73 files, native intersections, defaults/contracts and explicit portal exclusion recorded |
| BLT-Q2 | Exact effective beta patch predicates, atomic groups and live reproduction of inferred distribution/transfer failure cases |
| BLT-Q3 | Actual optional vehicle/worker contracts and incoming consumers if selected later; no vehicle or binary drop-in claim |
| BLT-Q4 | Shared cargo/index/opportunity/supply/visible Gear composition, native loader coordination, bounded recovery and measured combined workload |
| BLT-Q5 | Reviewed-build seven-language UI, source absence/presence, disabled modes, save/reload and causal actual pickup/delivery/boarding/launch evidence |

Focused TDD must distinguish deterministic quantities/policies from Harmony-owned fixtures and real
loaded game state. Final acceptance needs actual native orders, moving pawns/physical cargo and
resulting destination/quest state in a fresh minimized process, personally inspected after review.
Static source findings, document review and OpenSpec validation do not accept gameplay.
