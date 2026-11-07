# Pick Up and Haul - Continued — source analysis, 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S021**; owning draft [batch hauling](../../../../openspec/changes/specify-megamod-batch-hauling/).
This is the first current implementation in **PH-Q5**, following the user's requested sequential
comparison. Subsequent [Reworked](2026-11-03-pick-up-and-haul-reworked-research.md) and
[Armful](2026-11-03-armful-haul-research.md) audits record the next comparisons; no overall winner
or reconciled final spec is claimed.

**Subsequent decision:** [PH-Q5 composition](2026-11-03-hauling-composition-decisions.md) now selects
and validates the draft's behavior. Pending-comparison language below preserves this audit's
original checkpoint; no source runtime verification is implied.

## Evidence and coverage

Primary reference: **Pick Up and Haul - Continued**, Zzyxzz, package
`Zzyxzz.PickUpAndHaulReworked`, Workshop **3829549663**, installed About version **0.3.0**.
The package ID's Reworked suffix does not make it Dreadarm's separate Reworked item.
Public documentation was read before implementation: the
[author repository](https://github.com/Zzyxz/RimWorld-PickUpAndHaulContinued), its README and installed
About. The Workshop retrieval limitation is recorded in the [selection update](2026-11-02-hauling-reference-update.md).

Pinned public commit: **`172f225578de7243a97b649b58fd00451fc1189f`**, 2026-11-01,
`Leave cart cargo out of hauling runs`. Its worktree was checked clean. Local read-only inputs:

- `artifacts/Refinement/megamod-spec-20261103/pick-up-and-haul-continued/upstream/` — all **17 C#
  source files** read, plus the main csproj, Job/WorkGiver Defs, pawn-comp XML, English keyed text,
  README and LICENSE. No build script was run.
- `artifacts/Refinement/megamod-spec-20261103/pick-up-and-haul-continued/installed-project/` — ILSpy
  project decompilation of the installed 1.7 DLL. Selected bodies cross-checked: capacity arithmetic,
  pickup/remainder queueing, cargo insertion, drafting, personal/cargo merge interception, CE
  weight/bulk limits, no-destination drop and stale-cargo selection. Earlier `installed-settings/`
  confirms settings/defaults/persistence. These checks are not whole-build/source equivalence.
- Installed Job/WorkGiver XML, pawn-comp XML and English keyed text are byte-identical to those
  files in the pinned public checkout. This does not establish assembly/source equivalence.
- Native `MassUtility` was re-read from the earlier retained 1.7.4995 rev309 decompilation; the
  current game assembly hash is unchanged. No live game or foreign mod was executed.

| Input | SHA256 |
| --- | --- |
| Installed `3829549663/About/About.xml` | `B5BC3A0D2A8A076C8062198DF63210557CA311D22881439D3FB6234BE57AA10C` |
| Installed `3829549663/1.7/Assemblies/PickUpAndHaul.dll` | `C23E389F023C74397E2F7D53C53EF03825EF67A2718C9E3491AEADB0A4AE243C` |
| Installed `3829549663/Defs/JobDefs/WorkGiver.xml` | `52C8E65D911AA757E63D2F1D5FC2B420FC34D105FCB93C15A6D346CA2BDEDDB6` |
| Installed `3829549663/Patches/PickUpAndHaul.xml` | `AC556AFB2A2BB122F20170A8078E4A4A9F43FB4DBEDBC5AD6E0FDA0CC3913A4E` |
| Installed English keyed text | `EE17F4600E13D60ED0E308D132E7523394197CE04ABC1270434F8C991E29E763` |
| Native `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

The public LICENSE is MIT, copyright Mehni 2018; README retains upstream contributors. The 1.7
project targets net10.0 and reference package 1.7.4977-alpha.2, with RW17 compilation paths. These
are provenance/build facts, not beta acceptance. Independent implementation remains the plan;
downloaded assemblies, upstream preview art and runtime patch scaffolding are not product inputs.

## Settings and actual capacity

`Settings.cs` / `Modbase.cs` expose global mod settings, with no per-save inheritance:

| Setting | Source default / bounds | Actual use or limitation |
| --- | --- | --- |
| Corpse batching | Off | Excluded corpses can take ordinary hauling paths |
| Animal / mech batching | Both on | Race permission alone does not supply inventory, training or capacity |
| Minimum free starting space | 20%, integer UI 0–100 | Stored maximum-occupied fraction 0.8; normal start test uses gear mass only and rejects equality |
| Hauling carry limit | 130%, integer UI 100–200; load clamp 1–2 | Multiplies native mass capacity and supported CE weight/bulk limits |
| Drop hauled items when drafted | On | Spawned humanlike pawns drop tracked inventory cargo when becoming drafted |

`WorkGiver_HaulToInventory.HaulCapacity` is native `MassUtility.Capacity` times the factor.
`FreeHaulMass` subtracts `GearAndInventoryMass`. The inspected native sum includes apparel,
equipment and inventory, **not hand-carried things**. Pickup recomputes whole units against that
free mass; nonpositive item mass returns the whole stack. Without active CE bulk support, the fork
has no analogous combined inventory-volume bound. For an otherwise unmodified body-size-1 pawn,
the native mass capacity is 35 kg and the source default hauling allowance is 45.5 kg. This is
arithmetic from inspected code, not a measured carrying experiment.

`OverAllowedGearCapacity` compares gear/native capacity against 0.8; inventory is not part of that
normal starting threshold. At the UI's 100% free-space setting, zero gear gives `0 >= 0`, so a
positive-capacity naked pawn is skipped. This differs from its tooltip. CE's separate starting
test uses its current weight and bulk fractions, also rejecting equality.

The pickup driver queues ordinary `HaulToStorageJob` for a still-spawned remainder after an
inventory pickup. The planner also falls back for a single item exceeding remaining mass, other
designations, unsuitable corpse/worker, hopper delivery, active Stack Gap cell rules and some
unsupported routes. These paths do not first require tracked inventory cargo to be unloaded.
Thus setting the multiplier to 100% alone would not satisfy our aggregate hands/inventory rule.
The existing owned prohibition of a second allowance remains necessary; no source multiplier is
selected. Native heavy-item access after safe owned-cargo unloading remains part of the draft.

There is an additional CE-only transition before queued targets: the driver attempts ordinary
hauling when native gear/inventory mass exceeds **90% of the multiplied hauling capacity**, or
`CompatHelper.CeFull` reports full. This differs from the starting-space test and actual pickup
fit check. Successful queueing ends the collection job; it does not first require inventory cargo
delivery. The threshold is reference behavior, not a selected megamod setting or limit.

With CE absent, Continued also retains broad manual-pickup permission overrides:
`PawnUtility.GetMaxAllowedToPickUp` returns `int.MaxValue` for non-quest-lodgers, while
`CanPickUp` returns `!IsQuestLodger`. These are current-fork findings in `HarmonyPatches.cs`,
confirmed in the installed body, not just historical Mehni behavior. Permission alone does not
prove what the native menu/job ultimately transfers. Our contextual Take and keep contract must
check the actual combined allowance through that entire player path instead of copying these
global overrides.

## Collection, storage and work

`WorkGiver_HaulToInventory.cs` and `JobDriver_HaulToInventory.cs` are the main route:

- WorkGiver `HaulToInventory` remains in Hauling, priority-in-type **18**, requiring Manipulation.
  Both owned source jobs are non-suspendable and not casually interruptible. The animal job-giver
  is separately redirected through a Harmony replacement. Player faction, allowed race, cargo
  comp, quest-lodger exclusion and CE support are checked, alongside inherited/native eligibility.
- Native haulables are copied and distance-sorted; the planner then uses a reusable buffer and
  repeated nearest-candidate removal. Its search distance remains max(**12 cells**, **half** the
  seed-to-destination straight-line distance). Extras use automatic, unforced eligibility. Urgent
  seed batches filter extras by the external urgent designation. Other designations, foreign
  animals' corpse-feeding reservations and recognized cart cargo exclude extra candidates.
- It accounts for allocated storage counts before applying mass/bulk limits. This replaces the
  old whole-stack mass-planning arithmetic. Partial fit trims the queued count; a no-fit first
  target still receives a planned minimum of one, while actual pickup checks the current fit.
- Storage comes from native better-storage selection plus copied slot/container search. Capacity
  uses `GetItemStackSpaceLeftFor` or the IHoldMultipleThings interface. Shared allocations require
  stackability or that interface; generic mixed-slot shelf acceptance is not proven from this.
  `StoreTarget` identifies a cell or container, within a map-local planning operation.
- The driver requeues a resumed current target if needed, removes unreservable extras with aligned
  counts, reserves the first item/ordinary destination, and attempts additional reservations.
  Enroute containers avoid ordinary exclusive reservation at that stage. This is not a complete
  cross-worker quantity commitment ledger for all destination types.
- On arrival, pickup checks the current reservation, requested count, surviving stack and current
  mass/CE fit. Physical splitting and movement remain required. After its queued items, it searches
  within **12 cells** for more work, trying up to **8** qualifying candidates for a new job. Repeated
  jobs can extend collection; this is not our draft's finite-trip guarantee. The scan itself is
  over native haulables, not bounded to eight total inspected entries.
- Returning to the first destination schedules an unloading job; current cell commitments are
  released between jobs. New collection's finite planned set does not guarantee delivery freshness
  or prevent continued collection on later jobs. The owned destination recheck and commitment
  requirements therefore remain relevant.

The reusable arrays/lists reduce some allocations relative to the earlier source. They are static
scratch space, not the requested common Ctrl+F item index. No measured performance conclusion or
native reservation correctness claim follows from these source changes. Own search/hauling/storage
consumers must continue to share item facts while resolving pawn-specific eligibility at use time.

## Cargo ownership, unloading and recovery

`CompHauledToInventory` still persists a **HashSet of Thing references**, key
`ThingsHauledToInventory`. It does not record separate personal/cargo/urgent quantities inside a
mixed stack. Successful collected cargo merges only with existing tracked cargo; otherwise it is
added with merging disabled. The general inventory-add prefix prevents incoming personal supplies
from merging into tracked cargo and can merge them with other personal stacks. This is a concrete
improvement over the earlier source's accidental ownership mixing, not a full quantity ledger.

`FirstUnloadableThing` now removes stale references and picks surviving inventory cargo by category
index then defName. The old same-Def substitution is gone. The unload driver records the pulled
Thing and count-to-drop, uses native carry/deposit paths, and attempts to return an interrupted
hand-carried portion to inventory with cargo tracking restored. Native item-removal notification
removes departed references. These changes make old Mehni recovery defects inappropriate as
unqualified descriptions of this fork.

Important source behavior and remaining risks:

- Unloading pauses **3 ticks** per loop, or **20** when an active display name matches Extended
  Storage/Core SK. The longer delay is name-based, not a demonstrated provider contract.
- Destination selection is refreshed per item. Enroute destinations use remaining capacity plus
  an enroute claim; ordinary destinations reserve before extraction. Missing space/reservation
  invokes `DropOrKeep`: on a player home map it tries a nearby drop and then removes cargo tracking
  **without testing the drop result**. Off-home it ends incompletable and retains inventory cargo.
  This is a source-level lost-tracking risk on failed placement, not reproduced item deletion.
- The incapacity/non-storable drop branch similarly clears tracking without checking drop success.
  Collection's failed-inventory-add branch attempts near placement without establishing its success.
  Owned conservation must inspect actual transfer/drop postconditions in every path.
- Hand leftovers that are not pawns are moved into inventory and added to cargo tracking; the helper
  does not itself prove they originated as hauling cargo. A carried pawn is left alone, but exact
  native transfer and Simple Baby Carry behavior still require verification.
- Automatic recovery checks home-map status and the **current race preference**. Turning animal/mech
  batching off can therefore prevent that recovery route for already-held cargo. This differs from
  our requirement to stop new pickup while keeping recovery available.
- Recovery is refused for downed/dead pawns, pawns assigned a Lord, deathrest, self-shutdown and
  controlled mechs outside Work mode. Duplicate haul/unload jobs are checked before queueing.
  The public `CheckIfPawnShouldUnloadInventory(Pawn,bool)` signature retains a forced parameter,
  but its body does not use that argument to bypass these restrictions.
- A **750-tick** per-pawn retry delay is set when an incompletable unloading job still has carried
  contents. It is not a universal no-destination cooldown. The static dictionary expires entries
  on access; no explicit map/game teardown hook was found in the inspected source.
- Drafting, with the default option, attempts to drop each tracked humanlike inventory stack and
  removes only references no longer held/destroyed. Undrafting queues recovery. Auto-drop selection
  for our replacement remains an explicit comparison decision, not a silent new default.

Drop-unused/idle/HaulToCell/draft hooks help schedule recovery, but source scheduling still
must not be confused with proof that a delivered item reached storage. Its HaulToCell hook observes
creation of the toil enumerable, not successful final placement. The existing owned actual-delivery,
urgent-quantity lifetime, safe recovery and contextual status requirements are retained.

## External boundaries and admission

| Boundary | Inspected Continued behavior | Consequence for our design |
| --- | --- | --- |
| Combat Extended | Package-gated reflection reads current/capacity weight and bulk plus UpdateInventory; missing members/Bulk disable this adapter and warn. It subtracts hauled items from loadout accounting, protects them from excess dropping and removes hauled weapons from cached usable weapons | This is real integration, replacing old stubs. Capacity multiplier still violates our chosen hard limit. Missing per-pawn comp returns permissive fallbacks in helpers; individual hook failure is not atomic. Require validated actual limits and complete needed semantics |
| Allow Tool | Canonical active package and `HaulUrgentlyDesignation` inform extra-item filtering; retained public work-giver/type names remain available | Actual external consumers and urgency cleanup/cancellation still require current binding checks; owned S008 composes directly |
| Stack Gap | Resolves its rule methods and installs the provider's own CapacityAt postfix if not already present; seed cells with active upper/lower rules use ordinary hauling | No broad support claim. Preserve actual owned storage acceptance instead of bypassing it through single-load fallback |
| Simple Baby Carry | Resolves and installs its existing private-toil postfix, deduplicated by declaring type/name | Private bridge semantics and safely occupied hands remain research; do not copy a self-patch stack |
| Achtung | RW17 extra-candidate checks switch to the older native entry point for forced humanlike work or unresolved adapter state; resolves ForcedWork.Instance/HasForcedJob | Current APIs and effective forced behavior need proof; source exceptions/missing methods do not establish a semantic contract |
| Carts | `tbmr.mutuiche`; resolves `WoodenCart.CartCargoAccess.Owner` and excludes recognized spawned cargo from extra pickup; unresolved/throwing lookup returns false | No universal protection for unknown cart implementations. Preserve actual provider ownership with absent-safe guards |
| IHoldMultipleThings | Two-method capacity/stackability interface and direct reference retained | Existing interface shape is evidence, not a promise of source binary/package impersonation |
| UI | Cargo tint in native Gear and optional CE row; settings window, no new bottom tab | No moving-pawn cargo visualization here. Required S126 visible stacks and selected S132 Gear presentation must compose with the same owned cargo facts |

The source retains `PickUpAndHaul` assembly/type names, Job/WorkGiver Def names and old Harmony owner
`mehni.rimworld.pickupandhaul.main` despite the changed package ID. Name preservation is not proof
that package-gated consumers activate or that private toils/fields have unchanged semantics. The
earlier consumer inventory remains historical evidence; actual 1.7 While You're Up, Bulk Load For
Transporters and newer fork bindings must be inspected separately. No automatic foreign migration
or drop-in parity is selected from the source's advertised save swap.

Patch admission remains materially weaker than the user's requirement: most core patches install
directly; Gear tint changes the first white-color getter; animal hauling replaces method calls
without a documented semantic target predicate. Some optional hooks warn/catch/deduplicate, but
they do not prove effective-code shape or atomic dependent-feature admission. Source XML adds a
cargo comp to Pawn ThingDefs and startup removes duplicate comp entries by runtime Def mutation.
Our existing guarded target-spec path, declarative content rules and no-runtime-Def-mutation policy
remain authoritative. Neither exact assembly hashes nor an absence of other patches becomes a guard.

## Reconciliation decisions and remaining work

Confirmed user constraints still require **one actual combined carrying allowance**, shared item
data, direct owned composition and S126's visible hauled stacks. Reject the source's extra hauling
multiplier as a default or escape setting. Retain the draft's recovery-after-disable, successful
placement accounting and finite-trip requirements. These are already in the proposed behavior;
this analysis strengthens their evidence rather than silently introducing another implementation.

Reworked is the next sequential source, then Armful. Compare the following before closing PH-Q5:
capacity/hand model, end-of-collection ordering, same-type supplies, actual destination commitments,
home/off-home recovery, draft drop behavior, occupied hands, provider failures and save boundaries.
The current source's corrections supersede the corresponding old defects **only in this reference**.
Do not replace historical evidence or erase useful behavior that another source does better.

The draft's proposed combined inventory-volume interpretation remains PH-Q1. Source defaults such
as draft dropping and retry/handling durations need explicit selected outcomes and observable cases
after comparison. S125, S126, S133 and S135 retain their own full source/custom specifications;
this research does not complete them. Native semantic target documents, consumer/provider runs,
performance measurement, UI/language/asset review and gameplay acceptance remain future work.
