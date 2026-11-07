# dd.Armful Haul: source audit and selected visible cargo

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Research date: **2026-11-03**. S126 selects visible hauled stacks; the broader implementation is a
comparison input to S021, not automatic adoption of every Armful feature. Documentation and source
inspection only: no gameplay implementation, performance measurement or native acceptance.

## Evidence and coverage

Documentation was read first: [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3808100512),
installed About, and the complete Korean README. The public page calls PUAH incompatible. About
advertises several real hand stacks, construction, bill ingredients, transporters and visible cargo;
README additionally explains prison-food orders, DSUs/ports, Logical Floors and unverified live paths.
Neither modernity nor advertised safety/performance establishes an accepted behavior.

Installed root: `F:/Steam/steamapps/workshop/content/294100/3808100512`.
Title **dd.Armful Haul**, package **dd.armfulhaul**, metadata author **dd**, Workshop author
**moontmsai**, version **0.4.2**. Metadata and LoadFolders advertise/select 1.6/1.7, with Harmony
required; description still contains an older 1.6-only sentence. Inspected binary is **1.7**.
Source project targets net10.0 and writes the 1.7 assembly. Inventory has English/Korean keyed text,
two versioned assemblies, source, metadata and preview; no Defs/Patches or license file was found.
Source visibility does not grant reuse rights. Independently implement; copy no code, DLL or art.

| Evidence | SHA256 |
| --- | --- |
| About/About.xml | `29410BE7EC8206891DE33934C3DF52725AE5738EE72CEEE13CDE2387932965BD` |
| README.md | `18F9BE2A5F653836E6F58F50AF09D78EC0C6E700998659EC393E063ABB78DA0C` |
| 1.7/Assemblies/ArmfulHaul.dll | `0CE113A67871A95F74867FCAF7A83CE9A72AF9912B7894D61708F3A081289FCB` |
| Source/ArmfulHaul/DisplayPatches.cs | `B1612444B8F55EE72B32CFAADB38E96FB2D5044B2570048F21215CF6C6DA17C9` |
| Source/ArmfulHaul/ArmfulHaulUtility.cs | `493FE064A08FFF77B41F82D0CF7344B358AA9DEFF4DA03F229383F2DF07BE513` |
| Source/ArmfulHaul/HarmonyPatches.cs | `1477161452D3B00F44F846C93DABCA6FF9E82471304B6B6BBBB178EA0677712B` |

All **17 C# files** under `Source/ArmfulHaul` were read in full:

| Files | Traced responsibility |
| --- | --- |
| ArmfulHaulMod, HaulCapacity | Settings, initialization, worker classes and stack allowances |
| ArmfulHaulUtility, HarmonyPatches | Storage selection, physical pickup, delivery, cleanup and core hooks |
| TripState, ArmfulDebug | Saved job state, weak lifetime and diagnostic output |
| DisplayPatches | Drawing, individual cargo commands and inspect text |
| ArmfulConstruction, ConstructionToils, ConstructionDepositPatch | Existing native sites, material planning, delivery and primary accounting |
| ArmfulLoading, LoadingPatches | Transporter/portal demand, concurrent commitments and native loading |
| BillPatches | Existing bill queue, additional ingredient collection and placement |
| FloorCompat, MassStorageCompat | Reflected floor/DSU/port contracts and source-private caches |
| PrisonFoodHaul, PrisonFoodMenu | Explicit food return and room sweep commands |

Full installed DLL decompilation is retained locally at
`artifacts/Refinement/megamod-spec-20261103/armful-haul/installed-project/`.
Selected installed bodies were read and agree on the described capacity calculation, settings,
three display hooks, job-state serialization filter, extra-stack pickup/cleanup and storage
failed-drop branch. This is **not** a claim of whole-source/binary equivalence or live correctness.
Decompiled Unity types contain unresolved-reference comments; source provides the readable context.

## Cargo representation and capacity

The source raises the native carry container's `maxStacks` and places real split Things in it.
There is no extra private inventory bag. Each stack independently uses native `MaxStackSpaceEver`;
merges additionally respect the normal stack limit. Pickup uses SplitOff/TryAdd with merging off,
then sound and resource-count refresh. These paths do not share one mass/volume allowance across
hands and inventory. Two permitted stacks can each receive a full native stack allowance.

| Source option | Default / range / effect |
| --- | --- |
| Base hand stacks | 2; 1–5 |
| Colonist, animal, mech multiplier | Each 1; 0–2 in 0.1 UI increments; halves round up, result 1–10 stacks |
| Zero multiplier | One ordinary stack remains; extra collection disabled |
| Draw all stacks | True; only the extra pictures are disabled when false |
| Pile spread | 1; 0.1–10, rounded to tenths |
| Extra corpses | False; normal corpse delivery only, not grave jobs |
| Debug log | False; transition tracing |

Multiplier loading rejects non-finite values to 1. Pile-spread clamping has no explicit equivalent
non-finite check. Source settings are global only. Humanlikes use the colonist multiplier; native
mechs and a type/base named `AIRobot.X2_AIRobot` use mech; animals use animal; other/null cases use
one stack. Type-name detection is not a package-gated adapter. Ordinary collection checks player
faction, not drafted/downed, source eligibility and supported jobs; it does not create training.

**Selected deviation:** retain one actual aggregate allowance under S021. Do not import these
stack multipliers or enlarge carrying limits merely to obtain the display. S126 itself changes no
capacity, transfer, work eligibility or item ownership.

## Storage, construction and bill jobs

Ordinary storage wraps exact `JobDriver_HaulToCell`, excluding subclasses. It retains the first
native delivery and collects extras within **12 cells**, with at most **2 × allowed stacks**
searches (maximum 20). Native haulability, current priority, forbidden state, reach/reservations,
floor and destination limits participate. Subsequent deliveries choose the nearest planned cell
by straight-line distance, not a globally optimized route. Whole-cell reservations and this job's
planned quantities budget destinations; DSU capacity is shared within that plan. This is not a
general cross-worker quantity allocation service or a shared Ctrl+F index.

Storage toils are located by debug names, then extra toils inserted around native carry/place.
An unexpected layout returns the native sequence without a startup warning. Collection retries
consume the search budget; a pickup that takes nothing ends collection. The **delivery** fallback
is different: when there is no storage or haul-aside destination it calls `TryDropCarriedThing`
and jumps back to the same next-stack step without checking drop success or progress. If placement
keeps failing, the source branch can repeat without progress. This is a source-level risk, not a
reproduced freeze. Our S021 bounded recovery must retain cargo and end that attempt.

Construction uses exact native HaulToContainer jobs and existing B/C/queued native sites, excluding
installation blueprints. It does not invent new construction sites. The original primary is
protected; blocked/unreservable initial blueprints leave work vanilla. Planning is once per job,
within **12 cells for the original material / 20 for others**, at most SearchBudget source piles
and SearchBudget × 8 candidate Things. Native remaining demand, already held/original quantities,
en-route counts, site reservations and blueprint-to-frame conversion participate. Later delivery
records attempted stack/count and visited sites to stop no-progress repetition. These checks do
not prove every native queue or save interaction correct.

Construction's transpiler finds one generated DepositHauledThingInContainer closure and one
stackCount/local/subtract pattern, replacing the count with same-Def hand totals. It throws if
cardinality is wrong. This partial shape check is not atomic semantic admission for its whole
feature, and throwing is not our required unchanged-stream fallback.

Bill collection uses only the existing native ingredient queue and matching count queue, choosing
the nearest usable queued item, including port access; it does not discover arbitrary new recipe
ingredients. Inventory-held ingredients/unfinished items remain native. It merges/adds physical
hand stacks, subtracts actual taken quantities and places each at the bench or into an autonomous
bench. Additional toils are appended, retaining native indices, but the runtime pending item and
search count use TripState. The ExposeData hook serializes HaulToCell/HaulToContainer drivers,
**not DoBill**; bill continuation after loading needs specific verification rather than inferring
persistence from the appended layout. The source's straight-line triangle-inequality comment is
not proof of shorter real paths around walls or floors.

## Loading and manual prison-food work

Transporter/shuttle and portal extras are limited to exact HaulToTransporter/HaulToPortal drivers.
The source reads their native transfer lists. It sums own hands, other haulers' primary initial
counts, extra hands and pending extra pickups, then rechecks the remaining requested count at
pickup. Search radius is **12 cells**, same bounded search budget. It keeps the current primary
initialCount synchronized, registers eligible split stacks with transferables and adjusts vanilla
selection when extras already cover demand. No inventory-based caravan packing is implemented.

Before deposit it trims surplus after requested counts shrink. Whole-stack failed drops stop the
trim loop; partial-drop failure also returns. The caller then invokes the native deposit, so this
does not establish that every failed-surplus path can never overdeliver. Test changed load lists,
two haulers, partial transfers, absent destinations and failed drops before adopting the behavior.
S133 Bulk Load For Transporters and S135 Basic Caravan Loading retain their own complete analyses.

The 1.7 float-menu provider offers explicit prison-food return and, with multiple eligible stacks,
a room sweep. It is not automatic prisoner-food theft. Player faction, undrafted manipulation,
hauling ability, reach, reservation and outside-prison storage availability gate the command;
the selected food is unforbidden when ordered, other forbidden room food is skipped. Storage can
have equal/lower priority than the prison shelf, but never be another prison cell. The sweep uses
the same finite hand-trip collection; it is not a promise to repeat until the entire room is empty.
This added command is **comparison-only**, not selected by the visible-cargo requirement.

## Rendering and contextual information

`DisplayPatches.cs` has three postfixes:

- Native carried renderer PostDraw draws real additional hands after the native first stack.
  Offset starts at the native matrix/worker position; each extra is drawn using native
  `DrawCarriedThing`. Source step is min(0.12 × spread, 0.45 × spread / extra count), with a small
  altitude separation. At maximum spread the nominal pile extends as far as 4.5 cells; adopting
  that range is not justified by the user's request for legibility.
- CarryTracker gizmos preserve native commands and add native carried-item selection for each
  extra; additional commands are non-groupable so identical-looking stacks remain selectable.
  Stack icons use the actual item graphic for quantities above one, retaining native pawn/corpse
  and absent-icon handling. Order is current hand order; future deliveries can reorder by distance,
  so this is **not a guaranteed future delivery itinerary**.
- Pawn inspect text finds the exact localized native `Carrying: first-label` substring and
  replaces it with all hand labels. Changed native/foreign wording can silently defeat that
  replacement. Our complete cargo information cannot depend on matching one translated sentence.

No source bottom tab exists. Source display settings are mixed with capacity and debug settings.
Selected UI: contextual actual-cargo inspection; bottom-right extra-picture toggle; common
global/save default; any future diagnostic tracing belongs in developer controls. Personal supplies
are not made into displayed haul cargo. Native carrying of a baby/rescued pawn remains intact.

**Proposed initial visual policy:** adaptive pile layout with no upstream spread slider. Show every
actual cargo stack, avoid duplicating the native hand picture, and retain distinct inspection of
same-looking stacks. Exact numeric offsets are implementation details that require native captures,
not balancing choices. View off hides only extra pictures, never the cargo information. S126's
own [observable change](../../../../openspec/changes/specify-megamod-visible-haul-cargo/) includes
mixed personal/cargo quantities, failed drops, saved state, zoom and provider fallback cases.

## Lifetime, adapters and external contracts

Job finishes, DropAndForbidEverything and ExitMap call ReleaseExtraStacks. It tries to drop hands
from last to index 1, then transfer failures into inventory. It leaves the first stack to vanilla.
If both operations fail, extra Things remain in hands and the enlarged limit is retained; no
independent retry is installed by that helper. If inventory transfer succeeds, hand-only drawing
no longer exposes it as extra cargo. Pickup/rotation rollback also has unchecked merge/place
results. These are recovery boundaries to improve, not proof of lost items in a running game.

TripState is weakly keyed by JobDriver and saves construction plan/counts/cursor, loading pending
item/count, searches and prison-sweep cell for the two supported haul-driver families. Mismatched
pile/count arrays clear; cursor clamps. Serialized state plus native hands do not prove safe
removal, all toil-index transitions or restoration after changing mods. No source save import is
selected, and S126 needs no independently persisted item copies.

| Adapter / contract | Source behavior and limitation |
| --- | --- |
| Logical Floors | Reflects `LogicalFloors.API.FloorApi` SupportsApi(1), GetFloor, HasFloor, EnterFloor, GetTargetFloor; optional v2 GetFloors. Also uses private FloorNavigation manager/estimate and FloorHauling.searchedItem plus ladder types/Defs. Binding catches some failures; not all runtime calls are isolated. No canonical package gate. |
| DSU / input ports | Shape-reflects `bool CapacityAt(Thing,IntVec3,Map,out int)` and BoundStorageUnit; sums planned capacity at a shared unit. No active-package gate. |
| Pull ports | Reflects PlaceThingNow, CanGetNewItem and BoundStorageUnit; navigates to reachable ports and requests material at arrival. Some calls catch failures, other property reads can throw. |
| Misc. Robots | Type/base-name classification, not a validated active-package contract. |
| Native save/settings | `ddArmfulTrip` plus global settings keys; actual hand Things remain native. No automatic migration selected. |

Floor list and port caches are static map/tick scoped; stair cache lasts 250 ticks. Fallback stair
cost adds 4 per climb or 40 when stairs unknown. These are source heuristics, not selected game
values. They do not replace the required map-owned shared index or use-time eligibility checks.

Installed `.cs`/`.xml` text search for Armful namespace/package references found only Armful's own
initialization/metadata; it does **not** exclude binary consumers. Source comments mention outbound
Logical Floors interaction. No inbound consumer body has been established here. Preserve proven
contracts only after inspecting the actual provider/consumer; no drop-in binary identity claim.

The constructor uses unguarded PatchAll (`dd.armfulhaul`). Core toil-name checks, one throwing
transpiler and reflection catches do not meet per-patch startup target specs. Future owned hooks
need semantic incoming-code checks, compatible-foreign-patch tolerance, atomic dependent admission,
unchanged-stream failure, bounded warnings and lifecycle-safe fallback. Hashes above identify
research inputs and must not become blanket runtime version locks.

## Comparison decisions and remaining work

| Concern | Continued | Reworked | Armful | Current direction |
| --- | --- | --- | --- | --- |
| Physical cargo | Native inventory; isolated cargo/personal merging | Inventory with exact quantity ledger and recovery | Multiple real hand stacks | Retain actual quantities and personal protection; representation remains a design decision |
| Capacity | Default 1.3 factor plus separate hands | No factor, still separate hands allowance | Full native limit per allowed hand stack | Reject all extra independent allowances; S021 aggregate rule remains |
| Workers | Colonists, animals, mechs with options | Ordinary batching humanlike colonists | Extends supported native worker jobs; group multipliers | No silent narrowing or newly granted training |
| Job reuse | Custom collection/unload jobs | Custom jobs plus native retention/loading integrations | Wraps native storage/construction/bill/loading toils | Choose per workflow after semantic admission/lifetime analysis |
| Recovery | Some preference-gated/unchecked-drop risks | Physical quantity recovery; explicit bounded attempts | Hand drop/inventory fallback; no-progress branch risk | Preserve cargo, report retained amounts, bounded recovery |
| Visibility | No selected moving multi-stack view established | No selected moving multi-stack view established | Real extras, contextual selection/inspect | Armful-inspired S126 is required independently of hauling winner |
| Search/data | Source-private buffers | Local bounded plans and provider queries | Local radius plans, port/floor caches | Shared Ctrl+F facts, native/action eligibility rechecks, measured cost |

References: [Continued audit](2026-11-03-pick-up-and-haul-continued-research.md),
[Reworked audit](2026-11-03-pick-up-and-haul-reworked-research.md).
The stated PUAH incompatibility is credible as a **contract overlap**: extra hand stacks/native
toil wrapping interact with forks that assume one carried Thing, move goods into inventory and
replace/extend those jobs. No exact fork/version failure has been reproduced; do not claim all
combinations break. Implement one owned composition rather than combine these source patch sets.

- **AH-Q1:** S126 draft specifies the selected subset; the subsequent [PH-Q5 decision](2026-11-03-hauling-composition-decisions.md)
  selects one inventory-based quantity model and directly composed behavior after the narrow modern
  While You're Up intersection. Resolve capacity PH-Q1 and native job/provider seams before
  implementation; no whole fork is adopted. The comparison table above records the earlier checkpoint.
- **AH-Q2:** Native visual proof: adaptive spacing, large/custom item graphics, same-looking
  stacks, all supported worker shapes/directions/zooms, selection and foreign renderers. Revisit a
  spacing control only if evidence demonstrates a player need; initial draft deliberately omits it.
- **AH-Q3:** Inspect actual optional consumers and current beta render/inspection/lifetime paths;
  define target specs and co-installation admission. Source/binary migration is unselected.
- **AH-Q4:** Future focused tests, exact-build native pickup/move/deliver/interruption/reload
  observations, visual review and combined data/performance measurement. Nothing accepted yet.

The scope decision adds no resources, work speed, capacity or free delivery. Visual knowledge is
limited to the player's already observable cargo. Broader construction/loading efficiency and
prison-food authority would need their own explicitly selected observable contracts.

Independent document review found no issues after checking the six recorded hashes, source and
selected installed bodies, observable scenarios and cross-feature boundaries. Strict validation
passed for S126 and S021; typed repository validation passed 52 items. This establishes document
consistency, not runtime compatibility. All 17 S126 implementation/acceptance tasks remain unchecked.
