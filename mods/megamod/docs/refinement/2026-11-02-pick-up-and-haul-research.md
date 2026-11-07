# Pick Up And Haul: sequential source research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S021**, intake **P1-16**, origin **replacement**. Started **2026-11-02** after S020.
This record separates evidence from proposals. [Owning change](../../../../openspec/changes/specify-megamod-batch-hauling/)
was drafted and independently reviewed against the legacy source; no gameplay implementation or
native acceptance. **Reference selection changed on 2026-11-02–03:** S021 now uses
**Pick Up and Haul - Continued** (`Zzyxzz.PickUpAndHaulReworked`, 3829549663), comparing Reworked
(`Dreadarm.PickUpAndHaul`, 3813653998) and dd.Armful Haul (`dd.armfulhaul`, 3808100512).
This document remains **historical Mehni-source evidence**, not findings about those implementations.
PH-Q5 requires reconciliation of the draft. S125 now selects While You're Up with its 1.7 reference;
S126 requires visible hauled stacks at minimum. See the [reference update](2026-11-02-hauling-reference-update.md).

## Public documentation first and exact provenance

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=1279012058),
[latest change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/1279012058),
[official repository](https://github.com/Mehni/PickUpAndHaul) and
[release notes](https://github.com/Mehni/PickUpAndHaul/releases).
They describe inventory collection, unloading, personal pickup and Allow Tool cooperation. The Steam
description limits workers to humanlikes; the repository includes pack animals. Compatibility and
efficiency statements are author claims, not verified results. Recent change notes are mostly
auto-generated. Player reports about 1.7 and generic Steam page warnings are not acceptance evidence.

| Input | Pinned identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1279012058` |
| About | Pick Up And Haul; Mehni; `Mehni.PickUpAndHaul`; supports 1.0–1.6, no declared 1.7 |
| About SHA-256 | `E38FE74FB2928DC20A893D8D6DD0F7294957407D66A68396EC7E9329B0D7B7D7` |
| Selected main DLL | `1.6/Assemblies/PickUpAndHaul.dll`; assembly/file 1.0.0.0 |
| Main DLL SHA-256 | `CDAA18CD402A4CD7F9E3381F63EC000A6693440F34420B864C4FDD6CA04C86C6` |
| Informational version | `1.0.0+ff1022e1ba7c5c17256a00124f95be16bdc69799` |
| Contract DLL | `1.6/Assemblies/IHoldMultipleThings.dll` |
| Contract DLL SHA-256 | `F161D4ED759D4AFE3EE2021FB0CAAA8B35ACB803A465ED6D7919C02D293C373D` |
| Public checkout | `09f1fac96e70cc2a93d647615a5abd37734a3eba` |
| License | Installed LICENSE and public repository: MIT, copyright 2018 Mehni; retain notices if any substantial source portion is reused |
| Native comparison | 1.7.4995 rev309, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Local artifact root: `artifacts/Refinement/megamod-spec-20261102/pick-up-and-haul`.
All **14 installed C# source files** were read, including the generic FishTranspiler helper and separate
interface file. Their bytes, both selected DLLs and both gameplay XML files match the pinned public
checkout. Main DLL decompilation is retained under `installed-decompiled`; critical settings, CE
stubs, pickup clamping and planner arithmetic were cross-checked against it. This establishes the
inspected inputs, not a complete equivalence proof or 1.7 runtime compatibility.

Source includes versioned assemblies, Job/WorkGiver Defs, one pawn-comp XML patch, English and other
language files, source, README/license and a release ZIP. Modern About dependency entries require
Harmony; old folders bundle old Harmony binaries, which are not our package inputs. No bottom tab.
Source startup uses owner **mehni.rimworld.pickupandhaul.main**, manually installs ten native patch
sites (two conditional on CE absence), then logs its historical v1.1.2¼ greeting.

## Confirmed intent and native capacity distinction

The user requires an optimized replacement, the same item index/cache as Ctrl+F, and no oversized
separate hauling allowance. Their suspicion that the source ignores capacity is **not** adopted as
a blanket defect. Source ordinary inventory pickup does call the native mass clamp.

Native 1.7 has two distinct rules, read in MassUtility and Pawn_CarryTracker:

- Inventory mass capacity is **BodySize × 35**, when CanEverCarryAnything is true. Tool users except
  babies/subhumans qualify, otherwise packAnimal is required. GearAndInventoryMass includes apparel,
  equipment and every inventory stack, **not the hand-carried stack**. FreeSpace is nonnegative;
  CountToPickUpUntilOverEncumbered floors free mass divided by unit mass.
- Hands have one stack; MaxStackSpaceEver limits count to the smaller of the item stack limit and
  rounded **CarryingCapacity / VolumePerUnit**. This is not the same as inventory kilograms. It can
  legitimately allow an ordinary heavy item that will not fit the inventory mass allowance.
- Native ordered pickup permission normally uses orderedTakeGroup caps at home; away maps permit
  more. JobDriver_TakeInventory and Toils_Haul.TakeToInventory apply a mass clamp when the job's
  checkEncumbrance flag is set. The source's permission patch is not itself a universal mass bypass.

**Proposed interpretation PH-Q1:** owned multi-item trips enforce combined gear + all inventory +
hands mass, and combined inventory/hand volume against current CarryingCapacity, while each physical
stack keeps its native limit. Personal supplies consume space too; worn/equipped gear contributes
mass, not inventory volume. Extending the native hand-volume budget to all inventory is a proposed
constraint, not a claim that vanilla already treats all these containers as one volume allowance.
No second hand allowance after filling inventory. Before falling back to ordinary single-load
hauling of an oversized item, unload all owned cargo; the remaining vanilla single-load rules are
unchanged. This is explicit proposed behavior, not a user-approved numerical interpretation or
an attempt to redefine all of RimWorld's inventory/equipment rules.

## Source eligibility, settings and planning

Settings are global ModSettings: allowCorpses **false**, allowAnimals **true**, allowMechanoids
**true**, maximumOccupiedCapacityToConsiderHauling **0.8**. UI presents the latter as minimum free
space **20%**, with integer slider **0–100%**. IsAllowedRace admits humanlikes or enabled animal/mech
categories. Labels mention pack animals and hacked mechs; actual capability still comes from the
pawn/native inventory/mass paths, not a promised new pack or hauling training.

ShouldSkip adds player faction, no quest lodger, CompHauledToInventory, allowed race and a gear-only
threshold to native WorkGiver_HaulGeneral behavior. **GearMass / Capacity >= 0.8** skips by default:
it excludes equality and ignores personal inventory for this start threshold. At a requested 100%
free space even zero gear reaches the >= 0 test, contrary to the tooltip about naked pawns. Ordinary
JobOnThing can also be called directly by integrations, so this predicate alone is not a universal
entry gate. Our proposed common threshold uses actual combined occupied mass, permits equality,
requires positive capacity and applies before a new batch, not as a stop condition halfway through it.

GoodThingToHaul requires spawned/reservable/not-forbidden, allowed corpse and not valid best storage.
HasJobOnThing adds native fast hauling eligibility and a better storage destination. New WorkGiverDef
**HaulToInventory** uses Hauling, Manipulation and priorityInType **18**. Jobs HaulToInventory and
UnloadYourHauledInventory are nonsuspendable and not casually interruptible. Normal work priorities,
urgent designation and actual job eligibility remain separate.

Planner obtains native haulables, copies/sorts them by distance, then copies/sorts again for the
trip. Additional search distance is **max(12 cells, 0.5 × seed-to-destination straight-line distance)**.
It repeatedly chooses nearest remaining candidate from the last selected item's location, checks
reachability and eligibility and removes tested entries. Forced permission applies to the seed;
additional items use non-forced checks. If the seed is urgent, additional items must also be urgent.
Urgency is resolved by active **display name** Allow Tool and first designation identity; this is
not the owned direct policy or canonical external admission we will use.

The planner prefers better storage and can allocate multiple destination cells or container Things.
Native slot capacity or IHoldMultipleThings capacity is used; containers use their interactable
ThingOwner acceptance count. Source temporary skipCells/skipThings are **static** and not protected
by guaranteed cleanup. Allocation subtracts whole stacks, splits capacity across destinations and
builds target/count queues. A failed first allocation can leave an A target without a matching count.
These are code-derived risks, not reproduced failures.

The mass planner adds **whole candidate-stack** mass even if storage accepts less. CountPastCapacity
returns the excess count and assigns it to the final pickup count, not the remaining free count.
Actual normal pickup later clamps to current free mass, preventing a blanket inference of overflow,
but the plan can underuse capacity or disagree with destination allocation. We specify actual accepted
positive quantities, recomputed at pickup, instead of these incidental arithmetic mistakes.

Fallback to native single-load hauling occurs for gear threshold, missing comp, excluded corpse,
one unit exceeding free mass, zero destination capacity, or raw food for a Hopper. Ordinary native
hauling remains necessary for heavy items and specialized destinations; our cargo must be delivered
before that fallback creates another load.

## Source collection, unloading and state

HaulToInventory reserves as many queued items/destinations as possible, then requires the first
item and primary destination. It walks to each item, computes min(planned count, native free-mass
count), splits it and TryAdds to the existing pawn inventory. There is **no second physical inventory**;
CompHauledToInventory is a HashSet of references marking items in that same inventory. The driver
registers the split reference without checking TryAdd's return; merging can invalidate that reference.

If a source remainder stays spawned, it queues native HaulToStorageJob and ends collection after a
two-tick transition. This can pair a filled inventory with a separate hand-carried load. When the
queue ends normally it searches again within **12 cells** for newly available hauling, potentially
chaining more jobs; otherwise it travels to the primary storage and queues unload. Reservations can
be released between these jobs. Our proposed finite trip does not repeatedly grow from new arrivals.

PawnUnloadChecker is called by idle, DropUnusedInventory and a HaulToCell MakeNewToils postfix.
That last hook runs when the toils enumerable is obtained, not proof of successful delivery.
It requires an allowed player pawn and nonempty tracked/inventory sets. Because its condition contains
tracked.Count >= 1 after already requiring nonempty tracking, normal calls can enqueue unloading
regardless of the nominal **90%** mass threshold. The later near-rot **30000 ticks** and periodic
out-of-sync branches are generally bypassed after a successful reservation. Do not promise a real
timer/rot-aware policy solely from those unreachable-looking fallback branches.

Unload starts with **3 ticks** wait per loop, or **20** when display-name detection finds Extended
Storage/Core SK. It orders tracked items by first Thing category index then defName, finds currently
best storage from Unstored, reserves it, transfers the entire marked stack into the carry tracker,
and uses native cell/container placement and reservation release. Missing destination/reservation
failure drops that cargo near the pawn and ends the job. Lack of Manipulation or nonstorable cargo
also tries a nearby drop. It explicitly unforbids the pulled Thing. Failed transfer/drop return values
are not consistently checked; a whole marked stack can bypass normal hand-count splitting.

FirstUnloadableThing attempts to repair missing marked references by finding **any inventory stack
with the same Def**, then treating its whole count as hauled. That can select personal supplies or
a different quality/material object. The source save key is **ThingsHauledToInventory** (reference
collection); driver saves **countToDrop**. Notify_ItemRemoved removes tracking; DropUnusedInventory.Drop
is suppressed for tracked references. No independent quantity-based personal keep schema is present.

Source Gear rows tint tracked items grey/red through a transpiler replacing the first Color.white
getter. It warns if no getter was found, but does not prove the row-color semantic region. It does
not distinguish a partially hauled stack from personal units. Manual home pickup is broadened by
returning int.MaxValue from GetMaxAllowedToPickUp for non-lodgers and making CanPickUp true for them.
The source advertises keeping supplies, but its own code does not add a separate durable keep ledger.

## Compatibility and concrete consumers

Source mod checks use **display names**, not package IDs. CE detection disables two permission
patches; CompatHelper.CeOverweight returns **false**, CanFitInInventory returns **thing.stackCount**,
and UpdateInventory is **empty** in both installed source and DLL. The CE branch **replaces** the
normal pickup clamp with that helper result. No actual CE provider was found by the bounded canonical
metadata search; we cannot conclude whether an external CE patch independently prevents overload.
Advertised universal CE compatibility is therefore not adopted as verified behavior.

IHoldMultipleThings.IHoldMultipleThings exposes **bool CapacityAt(Thing, IntVec3, Map, out int)** and
**bool StackableAt(Thing, IntVec3, Map)**. The source queries matching comps on the storage parent or
Things at the cell. This contract is consumed to extend storage capacity; the source bundles a
separate interface DLL. Our adapter must resolve actual provider types/ownership without shipping
that downloaded assembly or treating missing support as unlimited capacity.

Bounded literal scans across Workshop and local Mods are retained as consumer-dll-paths.txt and
consumer-text-paths.txt. They found these substantive bindings; none is a broad compatibility proof:

| Consumer | Actual binding inspected | Replacement implication |
| --- | --- | --- |
| Allow Tool, `UnlimitedHugs.AllowTool` | Type-by-name WorkGiver_HaulToInventory, subclass of WorkGiver_HaulGeneral, public parameterless ctor; calls ShouldSkip and JobOnThing. Its public static JobOnThingDelegate has nested TryGetJobOnThing(Pawn,Thing,bool) → Job signature. CleanupDesignations removes marks from non-haulable Things; the placement wrapper removes urgency before actual placement | Owned S008 composes directly but is withheld with the original present. Optional external integration needs both guarded delegate ownership/fallback and a separately tracked quantity-based urgency lifetime with explicit cancellation; the delegate alone cannot supply it |
| Keyz' Allow Utilities, `keyz182.KeyzAllowUtilities`, 3524716849 | 1.6 conditional compatibility assembly directly references WorkGiver_HaulToInventory and assigns its own urgent delegate; loadFolders gates it on original package. 1.7 explicitly has no PUAH compatibility folder | Not automatically activated by our package; no blanket 1.7 bridge claim |
| KanbanStockpile Continued, `endag.kanbanstockpile`, 3767756434 | Reflects WorkGiver_HaulToInventory.CapacityAt; returns zero for cells with nondefault refill/limit settings, forcing source single-load fallback | Owned S049 must pass its actual acceptance budget directly to this planner, including linked totals, rather than patching our method or disabling batching wholesale |
| Stack gap, `Andromeda.StackGap`, 3071298014 | JobOnThingPrefix substitutes native single haul for limited destinations; CapacityAtPostfix clamps to remaining allowance | The user's storage-wide refill ceiling belongs to S049's refined storage behavior, not a selected full Stack gap replacement; external provider needs validated acceptance/budget or native fallback |
| While You're Up, `CodeOptimist.JobsOfOpportunity`, 2034960453, latest installed 1.5 | Reflects work-giver HasJob/Job/AllocateThingAtCell or StoreTarget/FindBetterStoreCell; both drivers; FirstUnloadableThing; skipCells, CompHauledToInventory/GetHashSet/private takenToInventory. havePuah requires all expected pieces | Private control-flow/cache emulation not promised. No current 1.7 compatibility claim; preserve as explicit unsupported legacy contract pending a narrow integration |
| Vehicle Map Framework, `OELS.VehicleMapFramework`, 3426502333 | Current 1.6 PUAH patch category and classes target candidate lists, JobOnThing, AllocateThingAtCell, Stackable and collection reservations | Cross-map positions/destinations need a dedicated verified adapter; same-map batch hauling is the initial contract, not a silent claim to support these hooks |
| SimpleBabyCarry, `b4ttl3m3ds.simplebabycarry`, 3692756679 | Canonical original package gate; postfix wraps private unload PullItemFromInventory toil to free an arm or drop cargo when a baby occupies the carry container | Never evict a carried pawn or claim compatibility because the old adapter exists. Our unload checks physical hands and yields/preserves cargo without touching the child |
| Pick Up And Haul – Shared Storage Access, `sugarcube.puahsharedstorage`, 3774922633 | Reflects HoldMultipleThings_Support.StackableAt; WorkGiver.OkThingToHaul, driver reservations and generated pickup action around SplitOff; reads CE helper, unload Def; also patches native reservation paths | Not a selected full replacement. Its private binding is not preserved by copying types; test native coexistence and withhold affected batching on unknown reservation semantics |
| Installed Immersive Chefs | Local DLL hit; current repo adapter names assembly PickUpAndHaul, Comp/Register/GetHashSet, PawnUnloadChecker(Pawn,bool), Settings.IsAllowedRace and unload driver, with original integration package gate | No automatic drop-in. Repository products remain independent; any later consumer update gets its own owner/change and narrow optional contract, not a megamod runtime dependency |

Old VFECore 1.2/1.3 DLLs also contain literals; no current-version binding was established from that
hit. Mod-manager catalogues, sample mod lists and About/language mentions are not executable consumers.
Consumer inspection stayed at the relevant adapter/contract surfaces; their whole features were not
researched out of queue. Source public helpers, native Def names, comp/save identity and private hooks
are materially broader than a simple job factory; do not assert complete drop-in compatibility.

Independent review identified the external urgency gap. Re-reading the retained Allow Tool
DesignationCleanupHandler.cs confirms cleanup every 60 ticks per map and removal when a Thing is
absent from native haulables. AllowTool.Patches/ToilsHaul_PlaceInCell_Patch.cs removes urgency before
calling the original placement initAction, so removal alone proves neither delivery nor cancellation.
The proposed correction applies only to owned batch cargo: preserve exact outstanding urgency,
classify explicit player cancellation separately, expose held cancellation in Gear, restore the
external marker on eligible dropped material and persist the outcome through reload. PH-T6 admits
the lifetime bridge together with the delegate, otherwise ordinary external hauling remains. Exact
explicit-cancellation hooks and native proof remain future work; no source behavior is claimed fixed.
The resolution review also caught the source's whole-Thing marker versus partial cancellation:
the proposed bridge keeps exact urgency on mixed loose stacks, displays the count and bounds both
batch and ordinary urgent fallback pickups to that count. An unprovable mixed-stack fallback is
deferred, not allowed to make cancelled units urgent again. This is an explicit owned adapter
requirement, not a quantity facility supplied by Allow Tool's delegate signature.

## Proposed composed behavior and remaining questions

Own collection uses native listers plus the **one shared map Thing index** for Ctrl+F, count consumers
and hauling. Candidate facts can be shared; eligibility/reservations/reachability/storage and capacity
are rechecked for the actual pawn at use time. No per-mod copy/sort cache, saved decisions or global
skip set. A route snapshot and local destination commitments are transaction state, not another map
index. Mixed items share genuine shelf stack capacity without declaring unlike things stackable.

Own cargo quantities survive split/merge/partial acceptance, and only successfully accepted units are
marked. Transfers preserve physical identity/quality/material and known kept quantities; no Def-only
repair or dump-everything. Interruptions stop collection promptly and retain recoverable cargo.
Idle/ordinary hauling opportunities resume unloading without overriding emergencies or player orders.
Optional provider failure blocks only affected batching; recovery must remain safe and explicit.

| ID | Type | Open work retained for later |
| --- | --- | --- |
| PH-Q1 | Proposed capacity/settings interpretation | Combined mass plus hauling volume, ordinary heavy single-load fallback only after cargo unload, inclusive full-burden starting threshold; review clarity and playability with measured native examples |
| PH-Q2 | Source corrections / runtime contracts | Quantity tracking, partial/mixed storage reservations, finite route construction, capacity changes and all native worker/unload lifecycle paths on exact beta |
| PH-Q3 | Proven external consumers | Preserve feasible public behavior through narrow adapters; resolve unsupported private/binary/save bindings and optional Allow Tool delegate plus cleanup/cancellation/placement/save-lifetime boundaries before claiming compatibility |
| PH-Q4 | Provider and acceptance work | Exact custom mass/bulk/storage capability, seven-language settings, combined Ctrl+F/hauling performance and memory measurement, RED/GREEN, independent review and native action/result evidence |

No question interrupts the deep dive. Numeric defaults and proposed corrections must appear in the
observable specs, with implementation details here/design. Direct S008/S005/S049 composition
does not close those other changes or authorize a new runtime implementation now.
