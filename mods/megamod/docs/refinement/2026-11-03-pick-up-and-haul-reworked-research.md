# Pick Up And Haul - Reworked — source analysis, 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Reference for **S021 / PH-Q5**, following the [Continued audit](2026-11-03-pick-up-and-haul-continued-research.md).
This is comparison evidence for the existing [batch-hauling change](../../../../openspec/changes/specify-megamod-batch-hauling/),
not another selected feature or an implemented replacement. The subsequent
[Armful audit](2026-11-03-armful-haul-research.md) records the next comparison; PH-Q5 remains open.

**Subsequent decision:** [PH-Q5 composition](2026-11-03-hauling-composition-decisions.md) now closes
the draft's reference selection. Pending-choice language below records this earlier audit checkpoint;
full S125/loading and native verification remain separate.

## Evidence and coverage

Reference: **Pick Up And Haul - Reworked**, Dreadarm, package `Dreadarm.PickUpAndHaul`, Workshop
**3813653998**, installed version **0.1.40**. Public documentation was read before implementation:
[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3813653998),
[author overview](https://rmapi.dreadarm.net/mods/pick-up-and-haul/),
[user guide](https://rmapi.dreadarm.net/mods/pick-up-and-haul/guide/), and
[API reference](https://rmapi.dreadarm.net/mods/pick-up-and-haul/api/).
The guide/API were inaccessible through the web reader but retrieved successfully as static HTML
through a direct HTTP read. This resolves the earlier intake's documentation retrieval limitation.

Installed metadata, LoadFolders, common Defs/patch, English keyed text and UPSTREAM-LICENSE were read.
No public implementation repository was linked by these inspected inputs. The installed **1.7 DLL**
was decompiled read-only with ILSpy; all **44 behavior/API/support C# files**, plus generated assembly
metadata, were read. Evidence is under ignored
`artifacts/Refinement/megamod-spec-20261103/pick-up-and-haul-reworked/` (`installed-project/`,
`guide.html`, `api.html`, `native-pickup/`); the overview is adjacent as `reworked-guide.html`.
No foreign code was executed, compiled, installed or modified. Decompilation has unresolved external
type/attribute annotations; precise signatures/enum values at such boundaries need reference-resolved
confirmation before implementation. This is code-derived behavior, not live compatibility proof.

| Input | SHA256 |
| --- | --- |
| Installed `3813653998/About/About.xml` | `4067FD072A8D718384449255C60EA01B1850AF973052AB59E71AD288C8924AAB` |
| Installed `3813653998/1.7/Assemblies/Dreadarm.PickUpAndHaul.dll` | `26E8D95E44BAB8B703617E06A7BD53EF85E23CB62E8DB0708E13A20E9D453BBE` |
| `Common/Defs/Hauling.xml` | `CD7B39907D34BEC0FD634193F0CE0C0C665B38BFB654600ADD95B9EA086C7B48` |
| `Common/Patches/CargoStoryteller.xml` | `976D614DA36280EED09417C4E07AFDDA819F6BF3FD04FEB22B2EAF82A6193E1D` |
| English keyed text | `E505EBF695337B8F381C1B48595A55DC19D60535ED5EBEC7D803C83E2C215221` |
| Retained author guide HTML | `5B1861AA911DBB29DB5D8153A5CF1967C5CEE28E0FA4DD451262135E8436539D` |
| Retained author API HTML | `BEA866DB6CF99C763DF6349687EE0FC32A795E97EB0F3B2F2B8932551DEA605D` |

Assembly informational version is `0.1.40+b0ffc1370f1f46e4635eb5c9502681da0ad2c7d6`; this is an
embedded identity, not a checked-out source commit. The assembly targets net10.0. LoadFolders selects
`Common` plus `1.7`. Native pickup-menu code was inspected from the same current game assembly as
the Continued audit, SHA256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`;
retained native MassUtility, TakeInventory and TakeToInventory were also consulted.

UPSTREAM-LICENSE contains Mehni's 2018 MIT notice. That establishes the retained upstream notice,
not independently verified licensing of every new contribution. Independent behavior implementation
remains selected; no upstream assembly, preview, source patch stack or new dependency is adopted.

## Settings, eligibility and capacity

Four global settings are persisted: **Enabled on**, **ForcedBatch on**, **SearchRadius 12** (integer
0–12, invalid saved values reset to 12 with a warning), **SearchBeyondRadius on**. Zero radius stops
extra searches. Changes gate future pickup/extension; existing cargo recovery does not depend on
Enabled. No corpse/animal/mech preferences or 20% starting-space preference exist in this fork.
Our existing draft's corresponding source-derived choices therefore need explicit reconciliation.

`HaulingWork.Eligible` requires a spawned, alive, standing, undrafted, player-controlled humanlike
colonist with inventory, carry tracker and Manipulation, excluding quest lodgers. This is narrower
than Continued's animal/mech support. `CanHaul` excludes corpses from ordinary batches, and checks
native hauling, map, item, designation/storage, forbidden, reachability and destination conditions.
Corpse exclusion here does not establish the same rule for every transport/caravan path.

Ordinary automatic pickups yield to forced/queued player orders, occupied hands, unloadable owned
cargo and a failure cooldown. Explicit batch orders bypass several automatic gates, including the
current cooldown and existing-cargo check; the driver first returns tracked hand cargo to inventory
or attempts to drop untracked hand contents. Failure to free hands stops the job. A carried pawn
has no special exclusion in that cleanup branch; preserving rescue/occupied-hand semantics remains
an owned requirement rather than an assumed property of the reference.

The actual capacity calculation confirms the guide's **two allowances**:

- Inventory fit is the minimum of native mass fit and the inventory container's acceptance.
- Hand fit uses native stack/container capacity, without subtracting inventory mass.
- `PawnAllowance` adds the two. `BatchPickupPlan` decreases remaining mass only for the inventory
  portion; its hand template and remaining hand count are separate. Actual acquisition can split
  one selected count between inventory and hands. Recovery contents are included in InventoryMass
  through another patch, but ordinary hand mass is not.

There is no Continued-style 1.3 multiplier, yet a full inventory can still receive an additional
hand load. Our single aggregate allowance must replace that rule, including forced collection and
transport/caravan paths. A recovery holder protecting existing physical items must never become a
second usable pickup allowance. No unlimited container or exact native mass constant is selected.

### Manual pickup: documentation versus inspected beta path

The guide advertises home-map manual pickup of weapons, apparel and loose items, kept as personal
supplies. Installed prefixes for `PawnUtility.CanPickUp` and `GetMaxAllowedToPickUp` instead allow
any mapped pawn with inventory and a non-null Def, except quest lodgers, while Enabled is on; the
latter returns `int.MaxValue`. They do not themselves require a colonist or home map. A menu postfix
lowers enabled pickup options' priority on home maps.

The inspected native **1.7** menu still rejects a home-map item unless
`JobGiver_DropUnusedInventory.ShouldKeepDrugInInventory` accepts it. No inspected Reworked hook
removes that condition. Its general home-map pickup advertising is therefore not established for
this beta input. This is a source-path mismatch requiring a native workflow check, not a reproduced
runtime report. Native menu/job mass checks remain: selected TakeInventory jobs set checkEncumbrance,
and TakeToInventory rechecks mass before transfer. Returning int.MaxValue for permission alone is
not proof of an unlimited actual transfer. Our bounded contextual Take and keep remains a separate
deliberate contract.

## Collection, storage and delivery

- General batch work is Hauling priority **18**, merging **6**. A humanlike pre-main think-tree
  insertion supplies cargo cleanup at priority **100**. The two source jobs are casually
  interruptible, non-suspendable, carry items after the job and do not drop before it.
- `BatchPickupPlan` records source counts and local destination budgets. Candidate choice prefers
  the closest matching Def, then other nearby types. Extras respect pending/queued-job source
  reservations; forced selection is limited to the initial choice. The actual driver reorders
  remaining pickups from its current position and rechecks eligibility/counts before transfer.
- Each plan admits at most **256 targets**, scans at most **512 new cells per radial search** and
  at most **4 origins**. Default search-beyond-radius also copies map haulables into candidate work
  and tries at most **256** map candidates, stopping after **8 consecutive failed additions**.
  Those bounds do not cap total initial list enumeration or nearest-candidate comparison cost.
- The driver can create up to **4 further plans** during one collection job. Thus the guide's
  four-search wording is not a four-search cap on the complete trip. Opportunistic planning is
  local and pending work prevents ordinary extension. This differs from Continued's open-ended
  follow-on collection and from our current single finite-plan proposal; final selection is pending.
- Source reservations are acquired for planned quantities and released as sources are consumed or
  invalidated. The planning dictionaries account for carried cargo, containers, cell fractions,
  item types and native linked-storage cells, but are **local allocations**, not persistent global
  destination reservations. The unload job reserves its actual destination later. No proof of
  competing-hauler or arbitrary multi-slot-provider correctness follows from those dictionaries.
- Storage selection prefers fuller partial stacks within applicable equal-priority groups;
  same-group merging uses stack-count/identity ordering to avoid equal-stack oscillation. It checks
  native storage availability before that merge. Actual storage queries include current capacity,
  reservations and enroute space. `ContainerCapacity` temporarily sets a live stack's count to
  int.MaxValue while querying acceptance, restoring it in finally; do not copy that mutation into
  an owned shared-data query. Provider semantics must be observed without fictitious item counts.
- Pickup stages a physical split, transfers actual accepted portions to inventory/hands, records
  successful quantities, and returns rejected remnants to the source, a safe nearby cell or saved
  recovery. This is a more explicit failure path than reference-only cargo tracking.
- Unloading rechecks ownership, personal retention, reservations and destination capacity before
  pulling items. It uses native container delivery or direct cell placement, checks actual progress,
  returns remaining hand cargo and selects another destination. There is no source 3/20-tick
  handling pause here. The **8 completed deposits** limit is per cleanup job; same-source refill
  loops may make several physical placements inside one deposit, and forced delivery may start a
  further cleanup job. It is not a universal eight-item or eight-transfer limit.

`StorageExists` first tries the native finder without a pawn, then examines acceptance/capacity and
claims. Some fallback branches return false even for an otherwise usable unclaimed candidate after
the native finder failed. Do not elevate the guide's distinction between universally unavailable
storage and merely pawn-restricted storage to proven behavior for every provider/path. Keep the
owned reason/retention rule and verify these boundaries rather than copying the helper by name.

The code has multiple local lists, sets and scans. It does not provide the requested common Ctrl+F
index. Bounded loops are useful evidence; neither they nor a newer assembly establish measured
performance superiority. Our candidates/quantities/invalidation remain shared, final permission local.

## Cargo ownership, personal supplies and recovery

`CargoLedger<Thing>` records **exact stack reference plus owned quantity**. Split/merge hooks move
observed quantities; completed add/transfer hooks reconcile actual holders. Spawned/destroyed items
lose owned cargo state; another pawn's held stack can transfer ownership. Pure cargo stacks merge
up to normal stack limits, while personal/mixed stacks are not proactively merged. No same-Def
substitution is used to reconstruct lost cargo ownership. Unqualified splits consume tracked units
first; this policy must be reconciled with our existing mixed-stack/urgency/explicit-personal rules.

Native unload allowances and optional provider minimums constrain cleanup, and retained supplies
do not block all new batches or consume destination allocations. `GetRequiredCount` and the native
allowance are re-evaluated at selection and extraction. The source's generic ledger does not by
itself distinguish ordinary from urgent cargo, or deliberate keep/release quantities.

Missing destinations retain cargo first. Eligible unstoreable cargo can be dropped near the pawn
after its own **600-tick** delay, only while standing in the home **area**. An allowed-area/reach or
claim restriction is intended to retain cargo rather than dump it, subject to the StorageExists
caveat above. Failed work also uses a **600-tick** retry cooldown. Successful movement/reconciliation,
not merely invoking a drop, determines final ownership. Disabling Enabled does not erase cargo or
disable the pre-main cleanup route. Drafting prevents eligibility; there is no Continued-style
drop-on-draft setting/hook in this reference.

The game component persists pawn records, quantity references, retry/forced-delivery state and
physical recovery holders. Each holder ticks its contents; restoration is retried every **600 ticks**.
Departure, caravan addition, map removal and a storyteller death callback attempt to preserve or
place remaining cargo. The fallback holder is described in code as unbounded; it is a loss-recovery
mechanism, not evidence of legal extra carrying room. Recovery mass is added to inventory mass.
No live transfer, decay, caravan accounting, save/load or removal result has been accepted.

The settings window's **Recover orphaned cargo** action places existing recovery items without a
living owner near the selected map center, including items saved for another map. It confirms the
quantity, verifies the same game/map before execution, retains failed remnants and reports results.
This is a recovery/debug action, not a configuration setting: our UI rules put such map-relocating
repair under the **top debug bar**, if selected. Actual settings use common global/save inheritance;
no new bottom tab is justified. This source does not render all inventory cargo on a moving pawn;
S126's visible stacks remain required.

## Transport, caravan and opportunistic intersections

Transporter loading changes native selection and toils for `JobDriver_HaulToTransporter`. It counts
current loading workers' carried/inventory cargo against transferables, prefers matching items,
collects more into inventory, then delivers through the container path and transfers matching owned
inventory surplus. Real pawn cargo is handled separately. Native work remains the fallback when
batch pickup cannot start. The toils are located by debug names; absent names silently leave that
portion unchanged, without an atomic startup guarantee.

Caravan loading separately changes selection, gather state, count determination, carrier choice
and hand-stack top-up. It uses native unpacked-caravan bookkeeping rather than the ordinary cargo
ledger. Full inventory may still get a hand load; if no better carrier exists, unpacked items can
be transferred without a new mass clamp to the selected carrier. Interrupted helper packing is
dropped back and re-added to transferables, with a same-Def ground lookup in one merge-recovery
case. Exact capacity, material/quality identity, cancellation and partial acceptance need owned
observable cases; ordinary cargo-ledger safety does not automatically cover this different path.

Not all transport/caravan entry points apply the same optional-provider failure gates as ordinary
batch work. For example, transporter acquisition checks native retention availability but does not
use `InventoryRetention.PausesAllPickups`, and caravan selection uses Enabled/eligibility. Provider
failure claims therefore need path-specific verification, not a universal claim from the settings UI.

The opportunistic postfix replaces a native opportunity with cleanup or a local batch, using the
preempted job to protect its reservations. This is an S125 intersection, not a full While You're Up
spec. S133 Bulk Load For Transporters and S135 Basic Caravan Loading likewise keep their own
research/changes. Compose chosen behavior directly with shared capacity and storage rules rather
than retaining multiple loading patches against our implementation.

## Public contracts, integration and admission

The source uses its own assembly/namespace/Def/Harmony identities, not Mehni's. The author explicitly
does not import original cargo, settings or active jobs. Its public API type is
`Dreadarm.PickUpAndHaul.Api.V1`, **ContractVersion 3**:

| Member | Inspected behavior |
| --- | --- |
| `GetHauledCount(Pawn,Thing)` | Exact current held stack quantity, clamped to stack size; zero for absent state, spawned/destroyed/unowned things or a different holder |
| `HasHauledCargo(Pawn)` | Positive held cargo for a living, non-destroyed pawn; false without state/personal-only inventory |
| `GetSearchRadius()` | Global setting, default 12 before settings exist |
| `SearchesBeyondRadius()` | Global setting, default true before settings exist |
| `IsHaulingJob(Job)` | Only the two owned source JobDefs; no current game or Enabled requirement |

Provider discovery requires the exact active `Dreadarm.SimpleSidearms` or `Dreadarm.SmartMedicine`
package and its same-named loaded assembly, then visible static `Api.V1`, ContractVersion >=1 and
matching query signatures. Absent package/assembly is neutral; ambiguous/unsupported/faulted active
queries warn once and conservatively retain affected inventory. Sidearms failure blocks ordinary
weapon batching, medicine failure ordinary batching of all item types. A present package with no
matching assembly is classified absent, so that does not prove fail-closed admission under our rule.
The documented Simple Sidearms consumer uses GetHauledCount; its installed consuming body has not
been inspected here. Preserve no package/API facade without actual consumer evidence. No dedicated
CE or external Allow Tool urgency adapter was found in these 44 files; generic native hooks do not
establish equivalent compatibility.

The source constructor runs unguarded `PatchAll`. A separate native-retention adapter has actual
incoming-IL checks (loop shape, candidate count, local/field types), failure warnings and cleanup,
but its transpiler throws on mismatch rather than returning incoming code unchanged. It is useful
design evidence, not compliance with our per-patch startup target-spec requirement. Other source
hooks use signatures/debug names without full semantic predicates or atomic dependent admission.

Storyteller XML additively creates/appends its comp; it does not replace unrelated lists. It lacks
our XML Extensions content guards and duplicate prevention. Keep the user's declarative merging,
per-patch diagnostics and source co-installation policy. Do not copy runtime patches or assume
uninspected external patches are compatible because both mods mention version 1.7.

## Comparison checkpoint and remaining decisions

Compared with Continued, Reworked supplies quantity-based ownership, explicit physical recovery,
native-retention queries, batch merging and loading paths. It narrows worker support, changes search
and interruption behavior, and still grants a separate hand allowance. These are tradeoffs to use
in a composed design, not a reason to adopt either implementation wholesale.

**Subsequent audit:** [Armful](2026-11-03-armful-haul-research.md) now records its real hand-stack
representation, limits, native-job reuse, visible cargo, lifecycle and stated incompatibility.
PH-Q5 stays open until the comparison choices and the narrow
current While You're Up bindings are reconciled into proposal/design/observable specs/tasks.
PH-Q1 still owns the proposed combined-volume interpretation. New source settings, handling/retry
durations, worker scope, repair UI and search choices remain evidence awaiting explicit selection.
S125/S126/S133/S135 are not completed by this comparison. All runtime and acceptance tasks remain open.
