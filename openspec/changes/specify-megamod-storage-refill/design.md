## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S049 replaces KanbanStockpile Continued, with the user's item/group refill model taking precedence.
This is a design for later implementation, not an accepted patch set. The
[research](../../../mods/megamod/docs/refinement/2026-11-03-kanban-stockpile-research.md) pins the
complete four behavior files plus AssemblyInfo, installed DLL, published source and native seams.

The installed1.6 DLL uses three per-storage integers keyed by owner.ToString(), broad
NoStorageBlockersIn checks, a HaulToStorageJob count postfix and an optional PUAH CapacityAt=0
postfix. The reference counts stacks in one SlotGroup and chooses the lowest mergeable stack in
one destination cell. Its percentage is a per-query threshold, not persisted hysteresis. Source
multiplayer registration is commented out; the inspected binary has no multiplayer reference.
Published source includes conditional debug calls absent from the selected DLL. Do not infer
binary equivalence, current debug spam or beta support from the source project's default build.

Native1.7.4995 rev309 differs materially: StorageGroup owns shared StorageSettings and exposes
member cells/haul sources; HaulToCellStorageJob uses the group when available and sums destination
capacity across its cells. ITab_Storage can resolve a selected group, which is not IHaulDestination.
The reference's cast therefore cannot provide controls for every native selected-group context.
StoreUtility.NoStorageBlockersIn participates in IsValidStorageFor as well as IsGoodStoreCell.
Toils_Haul uses these paths during pickup/transport. A global false there cannot represent an
automatic-only refill ceiling without changing other workflows. Exact effective callers remain
admission/proof work; inspect final delivery and opportunistic duplicate pickup, not just job creation.

## Goals / Non-Goals

**Goals:** one coherent entity policy, real quantity hysteresis, group totals, actual concurrent
delivery accounting, physical capacity preservation, common hauling/data composition, visible
status and reversible failure. Cover every source control explicitly, with deviations traceable.

**Non-Goals:** physical stack-size mutation; automatic inventory balancing; all-container support;
source settings/save migration; multiplayer synchronization; hidden universal compatibility;
new generic caching or settings frameworks; imported third-party binaries; runtime work in this slice.

## Decisions

### 1. Separate policy, real stock, cycle state and delivery allocation

Use a stable storage/group entity identity under the owning map/save, never labels or ToString().
Policy contains per-ThingDef enabled X/Y, one finite-or-unlimited stack-fill amount, and an off-or-1..8
similar-stack limit. Rows represent actual item type (ThingDef), not English label/category or a
CanStackWith equivalence class. Current filter plus member intrinsic rules determines counted units;
qualifying Stuff/quality variants share one row. This intentionally supersedes the source's global
ConsiderDifferentMaterialSimilar toggle. Physical CanStackWith rules remain authoritative separately.

Maintain open/waiting state for enabled quantity rows independently of policy. Store the cycle bit
with its stable scope/type and schema; retain unknown type identifiers as unresolved data. Derive
stock from current physical membership. Compute new assignable quantity from Y minus qualifying
stored units minus other valid incoming allocations, bounded below by zero. The current delivery's
own allocation is excluded when checking what it may still deliver. Incoming allocations never
close the cycle; actual stock reaching Y does. A cancellation while between X/Y therefore does
not strand an open cycle. Recheck all units after native/provider changes before committing work.

No quantity rule means ordinary automatic storage policy, still subject to enabled stack controls.
Defaults are deliberately neutral; the proposed first enabled rule X=1/Y=current Def stack maximum
means refill after depletion and is data-derived, not a universal meal/weapon balance constant.
Reject invalid/overflow edits atomically; calculations use checked or sufficiently wide sums so a
large colony cannot wrap into negative demand. The user did not prescribe source percentage parity.

Alternative rejected: deriving open/closed from the current threshold on every query loses the
user's continue-to-Y behavior. Counting in-flight units as actual stock can close a failed cycle.

### 2. Allocate physical stack destinations as well as quantity

The stack-fill slider offers positive integers through the maximum actual stackLimit of currently
accepted types plus Unlimited. Retain a previously higher finite value visibly after filter/provider
changes; it is never a physical override. No accepted types means no new finite selection. Precise
layout/drag precision remains implementation work, with keyboard accessibility and seven languages.

The source's similar-stack count is retained as Off/1..8, but group/type-based. Count real qualifying
Thing stacks once. Do not copy ceil(stackCount/mss): existing oversized stacks are one real stack.
Allocate arrivals to an existing compatible stack or a distinct admissible new position; multiple
incoming loads sharing one eventual position consume its remaining units, not multiple stack slots.
For multi-stack cells, distinguish actual stacks/provider slots, not merely cell coordinates.

Capacity is the intersection of group demand, effective per-stack fill, remaining similar-stack
slots, native/provider cell/container acceptance and the worker's shared carrying allowance. A large
Y is not silently clamped to current storage size; keep the policy and explain why full/blocked
storage cannot satisfy it. Do not create loops by redistributing within a group or by treating its
own outgoing stock as already removed. A temporarily unreachable stored item still exists and counts.

Alternative rejected: converting Y to a maximum number of stacks makes item quantities depend on
partial stacks and mixes the two distinct user controls. Raising Def.stackLimit is explicitly forbidden.

### 3. Directly compose automatic hauling without global storage vetoes

S021 owns the shared inventory/capacity and quantity plan, S125 future opportunities use it, and
S126 presents its actual cargo. Add refill demand as a direct policy input to those owned paths.
S008 urgent designations select automatic haul work and do not bypass refill. S027 trade receipts
and physical drops do not become refill jobs merely because they land in a stockpile. S133/S135
transport/caravan loading and consumer ingredient hauling are different intents, not refill targets.

Do not patch owned features or preserve the original PreventPickUpAndHaulOverHauling fallback.
No source-style toggle may disable correct in-flight accounting. External haulers need narrow
package/shape-admitted allocation, pickup, delivery and cleanup adapters; a name-only CapacityAt
match is insufficient. If coordination cannot be enforced, declare the affected context unavailable,
not silently enforce partial limits or disable batching universally. Native one-stack fallback must
also preserve quantity and recovery, as already required by S021.

Classify jobs by proven origin/intent and retain that context through their lifetime, not just a
HaulToCell def or player-forced flag. Explicit directed delivery differs from an urgent designation;
bill output can use hauling toils without being automatic replenishment. Emergency safe drops
cannot be prohibited by a policy quota. Existing items' storage validity must remain native.
Use scoped predicates at actual automatic planning/quantity/commit seams instead of globally
returning false from NoStorageBlockersIn. Do not leak a dynamic context across exceptions, nested
callbacks or jobs. Native duplicate pickup and delivery continuation must not refill beyond plan.

Each delivery claim belongs to a unique job/plan, destination scope, type and actual quantity. Dedup
source/destination reservations belonging to the same job, account for partial pickup/delivery and
different destination stacks, and release on cancellation, death, unload/reroute, map removal and
save transitions. Do not count the entire source stack or every Reservation record as a delivery.
On a demand reduction, keep actual cargo physically safe and reroute through ordinary valid storage;
never create mass, discard cargo, force re-hauling forever or permanently strand it in an inventory.
Native Toils_Haul.PlaceHauledThingInCell includes a last-resort Destroy after failed storage and
haul-aside placement. Merely delegating to that helper does not prove our no-loss promise: a policy
rejection must use S021's bounded lossless recovery, retain physical cargo if no safe placement exists
and surface the blockage. Do not broaden this into a promise to repair every unrelated native job.

### 4. Reuse native/shared membership and invalidate decisions

Use native SlotGroup/StorageGroup and holder APIs first. If combined query costs justify maintained
derived membership/counts, integrate with the same map-owned S033 dataset used by Ctrl+F, S032 and
S021; no Kanban-specific full-map scan/index. This consumer has its own storage/filter projection;
it does not reuse a search answer or pawn-specific eligibility as stock truth. Core StorageGroup's
CellsList and HaulSourcesList are mutable temporary lists; never retain them as a stable snapshot.
Native HeldThings covers ISlotGroupParent members, not every nested provider; resolve actual holdings
and visibility per admitted context, without exposing unknown/unowned inventory through counts.

Invalidate/recompute on spawn/despawn, actual ownership/location transfer, stack changes, filter/
quality/condition changes, group membership, storage resizing/destruction, provider reload and
map/game lifecycle. Queries on paused UI must reflect edits; final native job checks remain fresh.
Use Unity-thread game objects only. Measure total query/update/memory costs with combined consumers
and correct native fallback before claiming optimization. No arbitrary polling delay becomes a
gameplay default here.

### 5. Extend one linked policy and contextual surface

S005 supplies first-selected policy precedence, independent-copy unlinking and full-policy paste.
S049 adds its fields directly, not a parallel linked group. An old group retaining at least two
members keeps its cycle when members leave. Newly formed groups, receiving groups after linking,
new independent scopes and pasted-into scopes reevaluate from their own stock, as KS-08 specifies.
When a group dissolves, resulting independent scopes are new scopes.
Resize/rename do not reset surviving cycle intent; stock reaching Y closes it normally. Reassign
or discard invalid delivery claims before issuing replacements after membership changes.

The future S050 preset specification must distinguish filter-only presets from complete policy.
Applying a filter-only preset may alter qualifying stock but must preserve existing refill fields;
any preset that includes refill fields needs an explicit owning contract and visible scope.
Keep unavailable rows retained/identified rather than deleting their choices when a filter hides
them. Explicit copy/paste of complete storage policy includes refill fields; it never copies live
claims or the source cycle bit. New object defaults must not accidentally copy a shared clipboard.

The inspected Building_Storage.GetStoreSettings returns its group's settings when linked. Native
StorageGroup has a unique load ID but no ToString override. The source string-key approach therefore
maps distinct native groups to the same type-name key if used unchanged on this beta; this is a
static contract mismatch, not a reproduced live bug. Use stable group identity and scoped teardown.

Resolve ITab_Storage's actual selected IStoreSettingsParent/group/comp instead of narrowing every
surface to IHaulDestination. Integrate one structured refill section with the existing filter,
priority and preset surfaces, showing physical stock, incoming amount and blocked reason. No hardcoded
56px injection or StockpileRanking y-=26 workaround, no dynamic extra bottom tab. These are entity
choices, not settings inheritance exceptions. The source's three global booleans are superseded
by mandatory accounting, explicit type semantics and direct batch composition; no empty new settings
section. Any later actual configuration must use the foundation's common global/save mechanism.

### 6. Semantic admission and dependencies

The full enabled context consists of UI edit/display, stable policy/cycle persistence, group/copy
lifecycle, automatic job classification, quantity allocation, native duplicate pickup/commit and
cleanup. Document stable per-patch target specs with exact minimal consumed data/control flow.
Selection callbacks that merely observe may need only callable shape; job count or argument edits
must prove those values reach actual pickup/placement and preserve non-refill paths. Inspect effective
foreign prefix/transpiler behavior, including skip-original and late repatches. Every transpiler
checks its actual incoming stream and returns it unchanged on mismatch. No exact byte/hash ban or
requirement that all other Harmony owners be absent.

Validate before installation; on false/unknown/exception/install failure roll back only newly added
owned patches in that declared context. Log one bounded startup warning per skipped enabled patch
with package, patch/spec ID, target, failed predicate, unavailable behavior and fallback. Preserve
inert policy data and mark controls unavailable. Do not leave an editable UI with unenforced limits.
Original endag.kanbanstockpile suppresses the whole owned integration, without foreign unpatching.
Other original forks require positive identity/effective contract inspection before a compatibility
claim; the current bounded scan found no external binary consumer of the selected source API.

Core and Harmony are required when implemented. Optional providers are absent-safe and isolated.
No Def mutation/XML patch is proposed. If future declarative work needs XML Extensions, first use
must add its hard dependency and guarded merge-preserving targets. No multiplayer dependency or
synchronization claim, no KanbanStockpile assembly/package facade, no source binary distribution.

## Risks / Trade-offs

- Broad native hauling helpers serve production/recovery too → verify origin and actual commit,
  then reject a context that cannot keep these workflows separate.
- Native/group/provider holdings differ → retain exact adapter evidence, unavailable status and
  fresh physical accounting; do not pretend an empty scan is zero stock.
- Concurrent arrival or partial failure → unique quantity/slot claims and native revalidation,
  never whole-source-stack reservation counts or stale success state.
- Low limits can deliberately prevent Y → retain user's policy and explain immediate constraints;
  do not adjust Y, priorities, physical capacity or production to manufacture success.
- Large colonies make broad scans expensive → share concrete native/data work and benchmark the
  combined workload; correctness cannot be an optional performance checkbox.

## Migration Plan

New owned policy defaults to no quantity rules, Unlimited stack fill and Off stack count. Persist
owned stable identities/schema and cycle state; rebuild runtime claims from valid work after load.
Source fields stackRefillThreshold/similarStackLimit/maxStackSize and global keys are recorded for
provenance, not automatically imported. They cannot express X/Y hysteresis faithfully. A future
opt-in importer requires its own preview, backup, atomic conversion, failure restoration and verified
postconditions. Deactivation retains policy inert and leaves all physical items/native capacities
unchanged; it does not promise original or unknown providers suddenly become compatible.

## Open Questions

KS-Q1: exact1.7 effective automatic/directed/production classification, pickup/continuation/commit
guards and provider isolation. KS-Q2: actual native/provider held-item/slot semantics, shared data,
claim reconstruction and measured costs. KS-Q3: contextual layout, S005 and future S050 lifecycle,
unsupported contexts and future source migration/facades only if evidenced. KS-Q4: honest-tier TDD,
seven-language interaction, independent implementation review, package and personally observed live
hauling/lifecycle/composed-provider acceptance. These are later research/proof obligations, not
requests for the user to explain the source mod.
