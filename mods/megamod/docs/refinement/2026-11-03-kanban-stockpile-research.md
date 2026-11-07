# S049 — KanbanStockpile Continued research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, **P1-43**. Complete selected-source and relevant native
analysis and observable draft passed independent review and strict validation. No runtime acceptance.

The user's [refinement decisions](2026-11-01-source-audit.md) take precedence: X/Y quantities per
individual item type across all linked storage; start below X and continue to Y; automatic refill
only, allowing other deliveries above Y; one standard slider for all stacks, initially infinite/
undefined, limiting further hauling without overriding actual item maxima or resizing existing stock.
The suspected source bugs and performance issues are not accepted as reproduced without live evidence.

## Documentation first and exact provenance

Read [the selected Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3767756434)
and its linked [published repository](https://github.com/Endag/RimWorld-KanbanStockpile) before filling
gaps with source/decompilation. The page describes percentage refill, similar-stack limits and
optional maximum stack size, while its continuation notice explicitly drops multiplayer and reports
PUAH shuffling. Its embedded older multiplayer claims do not supersede that notice. The repository
README also retains old version/compatibility text. These are claims, not beta acceptance evidence.
The installed README, changelog, English catalog and metadata were inspected too.

| Identity | Inspected value |
| --- | --- |
| Selected mod / author | KanbanStockpile Continued / endag |
| Package / Workshop | `endag.kanbanstockpile` / `3767756434` |
| Installed path | `F:/Steam/steamapps/workshop/content/294100/3767756434` |
| Supported/load version | About and LoadFolders explicitly1.6; no1.7 entry |
| Manifest | 1.2.1; identifier still `net.ubergarm.rimworld.mods.kanbanstockpile`, distinct from package/Harmony owner |
| Selected DLL | `1.6/Assemblies/KanbanStockpile.dll`, assembly1.0.9716.36123, net472 |
| Required dependency | Harmony `brrainz.harmony`; no multiplayer runtime reference in selected binary |
| Load-after hints | `lwm.deepstorage`, `memegoddess.stockpileranking`, `mehni.pickupandhaul` |
| Source revision | `ff8f8385a78c2d3a8f6ed105272f068253f52349`, initial1.6 port; clone clean |
| License | Installed/published MIT, copyright2020 John Leimgruber III (ubergarm); preserve notice if code is later reused |
| Local evidence | `artifacts/Refinement/megamod-spec-20261103/kanban-stockpile/{source,compiled,native}` |
| Native comparator | Installed1.7.4995 rev309, Assembly-CSharp SHA256 below |

SHA256:

| Artifact | Hash |
| --- | --- |
| Selected DLL | `D1C10B4E7322372C602B1041A95CEEF0077D3942197FE998DD78223594885E34` |
| Installed About.xml | `CE41481FE05AAC7E46FBB0F70FF85F73C85070CC89091A8FF4518DDF88FC85E1` |
| Source Main.cs | `E33C7D4C05D2D48A1F771C076F5F9EC282C98448981CD25E999B2EDAFBF13FF5` |
| Source Patches.cs | `39687D78D1D8B7D92B47D1632DC0D7549F3F47CD3EF3196346D3291532697907` |
| Native Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Published source consists of Main.cs, Patches.cs, State.cs, Settings.cs and Properties/AssemblyInfo.cs;
all were read, as were the project and license. Installed inventory contains one runtime DLL,
metadata, documentation, English keys and preview art; no Defs or XML patches. ILSpy11.2.0.10050
decompiled the entire DLL. All functional types below were inspected, including generated closures.
Behavioral algorithms correspond to published source; exact reproducible binary equality is not
claimed. In particular, source project defaults to Debug and includes KSLog.Message calls, but the
selected compiled callers contain none of those conditional calls. Four ordinary startup detection/
patch Log.Message call sites remain; historical debug-spam comments do not describe proven current
runtime behavior. Source imports/references Multiplayer.API, but its synchronization is commented
out and selected assembly references contain no multiplayer API. No source or game was built/run.

## Full source behavior inventory

| Area / complete inspected types | Actual behavior and edge cases |
| --- | --- |
| Startup: KanbanStockpileLoader, KanbanStockpile, KSLog | Static Harmony owner `net.endag.rimworld.mods.kanbanstockpile`, unguarded PatchAll, then provider flags and optional PUAH patch. Settings loaded by Mod constructor; settings category hardcoded KanbanStockpile. Conditional logger remains defined but its selected call sites are absent. |
| State, KanbanSettings | Static dictionary keyed by storage owner.ToString(); struct ints srt/ssl/mss, missing defaults100/0/0. Get does not insert, Set stores, Del removes. Null owner treated as shared `___clipboard` by save/copy helpers. No game/map key or whole-game clearing observed. |
| ITab_Storage_TopAreaHeight_Patch | Getter postfix adds56 to height unconditionally. |
| ITab_Storage_FillTab_Patch | Subtract56 after first TopAreaHeight call and inject drawer after every Widgets.BeginGroup(Rect). No target semantics/cardinality guard. Resolve selected parent by reflection, then require IHaulDestination; GetStoreSettings supplies state key. StockpileRanking flag moves controls up26. |
| Storage controls | srt slider0..100 by5, ssl0..8 by1 (0=Off), mss checkbox plus numeric field min1 (off0; first on1). Hardcoded Stack Size label. Static numeric buffer and selected hash; buffer overwritten from value before text editor each draw. Direct State.Set on change; no native settings notification in this drawer. |
| KSUtil.TryGetKanbanSettings | Resolve destination cell's SlotGroup/Settings, state by owner string; false for null map/group or all defaults. This fast path is neutral source configuration, not complete provider admission. |
| KSUtil.IsSimilarStack | Exclude null, non-storable or identical object. Accept CanStackWith, or when global material option true, non-stackable items with the same defName. Does not combine distinct meats. |
| KSUtil.CountStoredSimilarStacks | Scan all cells/things of that SlotGroup until enough similar stacks. Counts each once, or ceil(stackCount/mss) when custom size active. Thus one larger physical stack can become several imagined stacks. Not an aggregate over every linked member. |
| KSUtil.TryGetStackRefillThresholdDesired | In one destination cell choose lowest-count storable stack that CanStackWith input. Effective cap=min(actual stackLimit,mss) when mss>0. Above cap*srt/100 yields desired0; at/below threshold yields cap-current. No stack gives false. No persisted refill cycle, so a later trip above the threshold need not continue to cap. |
| KSUtil.TryGetHaulingDestination | Container job uses targetB Thing.Map/Position; otherwise targetB cell and targetA Thing.Map or MapHeld. Requires destination SlotGroup. The else path assumes targetA Thing exists. |
| KSUtil.CountReservedSimilarStacks | Reflect private ReservationManager.reservations; inspect HaulToCell/HaulToContainer jobs to same SlotGroup, test targetA similarity. Count each reservation's source stack once or ceil(full source stackCount/mss); does not use actual job delivery quantity or deduplicate records for one job. Reflection field absence is not guarded. |
| StoreUtility_NoStorageBlockersIn_Patch | Preserve false. If nondefault and mergeable stack exists, return desired>0 immediately. Otherwise enforce ssl on stored similar stacks, then optional reserved similar stacks. No explicit automatic-versus-production context. |
| HaulAIUtility_HaulToStorageJob_Patch | On returned job resolve destination, then query policy using input Thing.Map rather than resolved map. Clamp job.count to desired for an existing stack and then effective stack cap; cancel <=0. Held-source null-map can miss policy. Does not physically resize existing items. |
| PickUpAndHaul_WorkGiver_HaulToInventory_Patch | If detected and global flag enabled at startup, find type by name and CapacityAt by name; missing type/method warning only. Postfix sets positive capacity to0 for any nondefault srt/ssl/mss. No fallback job is created here; relies on foreign caller behavior. Tooltip mentions only ssl, broader code wins. |
| StorageSettings_ExposeData_Patch | Force-save ints `stackRefillThreshold`100, `similarStackLimit`0, `maxStackSize`0; LoadingVars reads and State.Set. No schema/migration validation. |
| StorageSettings_CopyFrom_Patch | Inject helper before every TryNotifyChanged call. Copy by source/destination owner strings (null=clipboard). StackTrace frame2 comparison skips Building_Storage.PostMake with frame-count check. Not a semantic copy-context predicate. |
| Zone_Stockpile_PostDeregister_Patch | Delete that zone's key. No corresponding building/group/game cleanup found. Rename patch and MP sync blocks are commented source, not active behavior. |
| KanbanStockpileSettings | Three global booleans all true, force-saved with field-name keys: ReservedSimilarStackChecking, ConsiderDifferentMaterialSimilar, PreventPickUpAndHaulOverHauling. First two queried dynamically; last controls patch installation at startup and tooltip requires restart. |

The LWM detection flag has no remaining behavioral reader in the inspected source/DLL; actual
compatibility depends on native/provider behavior, not that message. StockpileRanking changes layout
only. PUAH detects `Mehni.PickUpAndHaul` or `Mlie.PickUpAndHaul`; no analysis of the newer selected
Continued/Reworked/Armful implementations is supplied by this old adapter. Those references already
have their own [owned hauling reconciliation](2026-11-03-hauling-composition-decisions.md).

## Native1.7 seams and static limitations

Read decompiled StorageSettings, StorageGroup, ITab_Storage, StoreUtility and relevant complete
hauling/pickup/placement methods in Verse.AI.HaulAIUtility and Toils_Haul. Building_Storage and
SlotGroup confirm settings delegation, membership and real held-item enumeration.

- StorageSettings.CopyFrom sets priority, copies filter and notifies owner; ExposeData persists
  priority/filter. AllowedToAccept composes filter with parent restrictions. Refill must not convert
  a quota into the item's physical acceptability or removal classification.
- StorageGroup owns settings and unique loadID; CellsList/HaulSourcesList are reused static temporary
  collections, not durable snapshots. HeldThings enumerates slot-group members only. The group does
  not implement IHaulDestination and lacks ToString override. Building_Storage.GetStoreSettings
  returns its group settings when linked, and SlotGroup.Settings delegates there. Unchanged source
  owner.ToString keys would therefore collide for distinct native groups on this beta. This is a
  static integration defect, not a launched/reproduced source bug.
- ITab_Storage resolves Thing/comp, individual IStoreSettingsParent or a consistent multi-selected
  StorageGroup. The source's IHaulDestination cast misses the group case. Native filter/priority
  layout and bill-output validation cannot be replaced by unguarded geometry injection.
- StoreUtility.NoStorageBlockersIn checks actual mergeable room, blueprint/building blockers and
  item counts. IsValidStorageFor and IsGoodStoreCell both consume it. The latter also checks access,
  reservations, fire/construction and reachability. A global quota veto affects more than choosing
  automatic refill; whether every foreign caller is safe cannot be inferred from the method name.
- Native HaulToStorageJob distinguishes cell/container; HaulToCellStorageJob uses StorageGroup when
  linked and counts available cell capacity. Native duplicate collection and destination checks can
  later change work. Job creation alone is not enough to enforce exact delivered quantity.
- Toils_Haul.PlaceHauledThingInCell serves DoBill/RecolorApparel/RefuelAtomic/RearmTurretAtomic too,
  calls native carried-item drop and may seek another cell. Its final failed-storage/haul-aside
  path can Destroy carried cargo. A refill-induced rejection cannot claim no loss merely by calling
  this helper; owned bounded recovery must preserve cargo and report a blockage. No blanket fix to
  all native jobs is proposed.

## Contracts, consumers and persistence

Public source surface includes KanbanSettings fields, KSUtil helpers, loader flags, static Mod
Settings and public patch methods. Bounded raw binary search across installed Workshop and local
Mods for KanbanStockpile, TryGetKanbanSettings, its Harmony owner and package found **only the
selected source DLL**. This is no discovered inbound binary consumer, not proof that no reflection,
uninstalled source, old fork or future mod consumes it. No external facade/assembly identity is
selected. Package identity remains fumblesneeze.megamod.

Outbound dependencies actually consumed: Harmony; native private fields/properties/methods above;
optional PUAH type/method; package checks for LWM, StockpileRanking (`Uuugggg.StockpileRanking` or
`memegoddess.stockpileranking`) and older PUAH. No XML Def names or multiplayer public registration
is provided by the selected binary. Source save ints and global keys are recorded, but a stack-local
percentage cannot be losslessly interpreted as X/Y group quantities. No automatic source migration;
future explicit migration requires reviewed mapping, preview/backup, atomicity, restoration and
postconditions. Source co-installation disables owned integration rather than mixing enforcement.

## Chosen owned behavior versus source

| Source behavior | Owned decision / reason |
| --- | --- |
| Per-stack0..100% threshold; default100 | Per-type optional X/Y across linked storage, persisted open cycle. Initially no rules; first enable proposes X1/Yphysical maximum. User-required group hysteresis; no source percentage mode. |
| Similar-stack limit0..8 | Retain Off/1..8, but actual physical stacks by type/group, not imaginary ceil divisions. Default Off; limits never evict stock. |
| Max stack-size checkbox/numeric0=off | Required single slider, initially Unlimited, applies automatic filling only and respects every actual maximum. Existing larger stacks remain. |
| Three global booleans | No replacement settings rows: claim accounting mandatory, type grouping explicit, owned batching directly coordinated. Source performance opt-out and PUAH block removed. |
| Stored+reserved scan | Real units plus unique incoming quantities/slots, one shared map dataset with Ctrl+F/hauling where justified. No duplicated per-port scan/cache, no stale decisions. |
| Broad storage veto | Restrict automatic job intent only; production/direct/trade/recovery can exceed limits without losing native physical rules. Urgent automatic designations still respect refill. |
| String-key copy/persistence | Stable entity/group/save identity, full owned policy copy, separate cycle state, reconstructed claims, unknown-type retention and lifecycle cleanup. |
| Source UI insertion | Structured native contextual storage section, no extra bottom tab and no entity entries in mod settings. Search/categories help find type rows. |
| Original/provider compatibility claims | Exact admitted contexts only; original endag provider disables owned group; preserve foreign ownership and warn about skipped enabled patches. |

Retaining source stack-count choices is a specification choice, not user-confirmed mandatory
numerical balance. New enabled-rule defaults, strict boundary, filters/variants, waiting-on-new-rule,
claim behavior, slider range retention, group lifecycle and automatic-versus-directed distinction
are explicit proposed decisions in KS-01–09, not hidden implementation assumptions. Neither source
performance wording nor analysis establishes measured improvement or a native accepted workflow.

## Remaining work

| ID | Obligation |
| --- | --- |
| KS-Q1 | Exact effective1.7 job-intent, pickup/continuation/commit/recovery and coherent per-context semantic admission, foreign ordering and startup diagnostics |
| KS-Q2 | Native/provider real holdings and stack slots, shared-data invalidation, unique incoming claims/reconstruction and measured combined costs |
| KS-Q3 | Native contextual layout, S005 and future S050 policy/lifecycle composition, missing data and unsupported contexts; facade/migration only if later evidenced and explicitly specified |
| KS-Q4 | Honest-tier TDD, seven-language player interactions, independent implementation review/package and personally observed concurrent hauling, production, recovery and save/lifecycle workflows |

See the [owned proposal](../../../../openspec/changes/specify-megamod-storage-refill/proposal.md),
[design](../../../../openspec/changes/specify-megamod-storage-refill/design.md),
[observable requirements](../../../../openspec/changes/specify-megamod-storage-refill/specs/megamod-storage-refill/spec.md)
and [replacement record](../replacements/kanban-stockpile.md). No runtime changes, build/install,
normal saves/configuration, Workshop edits or game launches were made for this documentation.

Independent review verified the complete source/compiled/native account and all five hashes. Its
one P2 lifecycle ambiguity was resolved: an existing group surviving with two or more members
retains its cycle, while newly independent scopes reevaluate their own quantities. Two concrete
departure/dissolution scenarios distinguish these cases; focused re-review found no issues.
