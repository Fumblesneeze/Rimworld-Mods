## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S027 is documentation only. [Research](../../../mods/megamod/docs/refinement/2026-11-02-visitor-deliveries-research.md)
pins source/installed 1.7 agreement, all three source classes, current native handover/storage/job
semantics and the installed Hospitality farewell-gift call. No runtime behavior is accepted here.

## Goals / Non-Goals

**Goals:** physical courtesy delivery after successful receipt; consistent courier exclusions;
bounded work and exact quantities; normal storage acceptance; safe interruption/reload; optional
Hospitality support; one composed owner with semantic startup checks.

**Non-goals:** visitor batch-hauling/general colony work, return-caravan unloading, orbital/world-map
trade rewriting, new gifts or prices, permanent visits, new content/settings UI, legacy import,
global hauling patches or an independent map-item index. Native unsuccessful handover semantics
are outside the start boundary; do not claim this feature repairs every possible native trade loss.

## Decisions

### 1. Observe successful receipt instead of replacing the whole trade

Prefer a narrow supported handover/placement boundary over the source's prefix replacement. Native
Pawn_TraderTracker performs pawn ownership/lord bookkeeping and item PreTraded once; do not copy that
body and suppress it wholesale. Capture only the successful incoming item branch and actual placed quantity/
resulting stack, including incoming silver from net sales. Native TradeDeal currency balancing and
Tradeable.ResolveTrade use this same incoming path; do not equate PlayerBuys with a player having
bought merchandise, or exclude a sell-only deal's proceeds. Outgoing goods remain untouched.
The gift boundary likewise needs successful placement results, not blindly trusting
the input list or a void method's return. A naive postfix scanning nearby items cannot prove which
units were newly received. Match placement merges/splits with exact counts and attributes, and scope
any temporary capture to the exact transaction, restoring it even after exceptions/nested calls.

An event is a completed on-map TradeDeal received batch or one GiveGift batch. The inspected native
TradeDeal.TryExecute(out actuallyTraded) checks transaction viability before resolving Tradeables;
reconfirm its exact completion/exception semantics before using it to close an event. Deferred job
assignment must not manufacture rollback of a partially completed native trade. Reconcile physical
receipts already handed over, release unused reservations, and never replay payment/gift effects.
No success/receipt means no owned assignment. Primitive receipt facts can be tested host-side, but
real merge/split/trade consequences require loaded-game verification.

Use the existing shared quantity/provenance machinery where suitable for mixed stacks; receipt units
must not expand into unrelated old stock. Record event/source identity, actual received units, assigned
load and remaining physical cargo, and retire quantities as they are delivered or leave the owned
workflow. Native stack transfers/consumption need attributable updates, not deductions from total
count alone when another actor can add or remove units. Never cache a stale Thing as proof of ownership.
For an unlabelled removal from identical mixed units, retire receipt quantity first, oldest event
first, preserving preexisting stock; explicitly attributed transfers retire their named portion.
This is the explicit S021 cargo-first composition rule, not a deduction about indistinguishable units.
Persist event ordering and reconcile each receipt once across merge/split/load. Do not replace an
exhausted event's quantity with a later receipt or unrelated inventory.

### 2. One native-sized load per courier with explicit origin intent

Directly compose a visitor-delivery intent with shared storage/reservation facts. Do not route it
through PUAH's inventory batch eligibility or Common Sense's optional side work. Inspect native listers
and reuse shared map facts; courier membership is a finite party/giver list, not a second map scan.
Revalidate current map/party role, actor availability, manipulation/carry space, item/route permission
and destination acceptance at selection and use. Main-trader exclusion applies to all origin paths.
Do not use source `defName.Contains("haul")`, CurLordToil.ToString substring or nullable assumptions
as policy. Existing deliveries count as busy even if a driver name changes.

Native Pawn_TraderTracker adds handed-over items to its lord.extraForbiddenThings; the inspected
1.7 ForbidUtility treats those as forbidden to members. HaulToCell remembers forbiddenInitially and
omits the source forbidden failure when initially forbidden, while container hauling checks its
destination. These native details do not authorize a blanket permission bypass. Define a scoped
visitor-delivery permission for the assigned receipt quantity only: it may bypass that anti-reacquisition
restriction for pickup/storage, while preserving explicit player forbiddance, route/destination,
reservation, ritual and provider constraints. Do not remove a whole Thing from extraForbiddenThings,
change global forbidden state, or allow personal ingestion/equipment/trade access. Revoke the
permission when the assignment ends; admit only a proven pickup path with current restriction checks.
Merged old stock and other visitors remain outside the exception. Cover this in VD-T3/T4 admission.

Allocate each event's received stacks in ordinary receipt order, trying currently eligible couriers
by distance to the physical receipt, with stable party order for ties. This is a proposed deterministic
allocation rule; expose no global optimal-routing claim. Each courier receives at most one load from
the event, with count capped by receipt remainder, current native hand-carry allowance and destination
space. Existing carrying/personal stock is not displaced or turned into entitlement. The feature uses
ordinary single-load capacity; it does not add a second inventory/carry allowance or grant general
nonplayer batch work. If a provider replaces carrying semantics, admit that exact path or fall back.

Native HaulToCellStorageJob enables opportunistic duplicate pickup and can request more than the
receipt. Constrain the assigned count and disable unrelated duplicates for this intent. Use ordinary
movement/pickup/placement seams through a small owned job wrapper when necessary for lifetime and
quantity safety; do not call TryTakeOrderedJob as if the player forced a visitor and clear its queue.
Shared logic belongs under the owning feature or shared host-safe contracts, never another runtime
repository mod. No free extra inventory or general work permission is created.

### 3. Destination selection distinguishes delivery from automatic refill

Use the native better-storage search: enabled accepting player destination, strictly better priority,
actual access/reservations and real remaining capacity. Preserve native equal-priority tie behavior,
including slot-group preference over equal-priority container, rather than invent a global distance
optimizer. Revalidate provider acceptance immediately before pickup/placement. S005 groups share
policy only; member cells/containers retain intrinsic restrictions. Include courier incoming quantities
in the same actual-space accounting used by S021 so two planners do not claim the same room.

S049's confirmed X/Y refill policy is not an absolute storage capacity. Carry receipt intent into
the shared acceptance calculation: trade/gift delivery bypasses only the automatic-refill trigger/
target, while the storage-wide per-stack hauling ceiling and native item maximum still limit a haul.
Do not bypass ordinary filters or provider access. S049 remains queued for full source inspection;
this contract records the already confirmed user distinction without implementing speculative controls.
Ground handover stays native even when it lands above a later hauling/refill target.

No search/cache per source port. Reuse native storage lists and shared Thing facts, with invalidation
on movement, stack changes, pickup/drop, holder/destruction, storage policy/capacity and lifecycle
changes. Current courier eligibility stays uncached. Off-thread Verse/Unity access is forbidden.
Measure combined delivery/colony-hauling update and memory cost before claiming optimization.

### 4. Distinguish routine farewell from mandatory exit

All incoming trade receipts while the party is already leaving, including sell-only silver, must
use native handover only. Native visitors
and Hospitality create ordinary farewell gifts during departure transitions, so gift delivery cannot
use an indiscriminate exit-state veto. Represent a finite courtesy subtask around a confirmed routine
gift handover, then restore normal departure. Do not reset visit timers or repeatedly reenter a visit.
Dismissal, threat, hostile relations, temperature/other emergency exit and urgent care/rescue cancel
courtesy. Destination/provider rules may still make a routine gift undeliverable; ground fallback is
valid. S039's eventual fixed trading area controls gathering; this temporary delivery is not an allowed-
area override and neither changes its bounds nor grants entry permissions.

Hospitality reaches native GiveGift via LordToil_VisitPoint.Cleanup → Leave → GiveRandomGift, and its
LordJob graph distinguishes normal stay expiry/sent-away from emergencies. Source has no dedicated
adapter. Use native receipt admission if sufficient; if additional lifetime integration is needed,
gate `orion.hospitality` and exact type/member/transition semantics behind a narrow adapter. Preserve
scores, gift chance/options, notifications and guest permission policies. Source presence is not
proof that post-cleanup queued jobs survive departure, so verify actual native actions on the exact
reviewed build. Do not hold an entire party indefinitely to finish a failed courtesy.

### 5. Physical cargo and save recovery are part of admission

Receipt state is gameplay state, serialized with the game/map/assignment, never global settings.
Persist enough exact ownership to reconcile queued, carried, partially stored and completed portions
after save/load before continuing. Store stable identifiers and quantities, not static live references;
release map/game data on teardown. Do not carry an event's unused assignment budget into a second
delivery round after reload. A legitimate other actor moving/using cargo retires the corresponding
owned claim; it must not trigger item recreation or retrieval from unrelated inventory.

After interruption, release unused reservations. Actual owned carried remainder goes through native
safe local placement before exit and must not become personal/unsold trader stock. Preserve personal
cargo separately. Ordinary downing/death physical drop/corpse handling remains authoritative. If
placement cannot complete, retain the actual remainder in its holder with bounded recovery state and
an actionable explanation; prevent owned departure/export/destruction of that remainder without a
busy retry loop. Root safety does not allow inventing a replacement item elsewhere. Exact emergency
handoff/drop/despawn ordering is VD-Q2 and a prerequisite for admitting any scheduling path.

Failure of selection admission disables new assignments, not deserialization of existing obligations.
Keep recovery independent and conservatively retire pending jobs before source-provider handoff or
unsupported lifecycle continuation. Ambiguous cargo must remain physical and recoverable, never
silently discarded because an integration is absent. Ordinary completed trade state is not rolled back.

### 6. Candidate patch-target admission

Before implementation, instantiate and index individual stable target documents with the template
under `mods/megamod/docs/patch-targets/`. These candidates are not executable validators yet. Every
actual hook must be covered; remove unnecessary candidates when owned/public composition suffices.

| Candidate | Minimum required semantics / dependencies | Failure boundary |
| --- | --- | --- |
| VD-T1 trade receipt | Pawn_TraderTracker item handover and needed TradeDeal completion; exact count, successful placed output, PreTraded once, pawn/outgoing branches untouched, no skipped-original phantom success | Trade-courtesy group inactive, native trade/ground handling preserved |
| VD-T2 gift receipt | GiveGift successful placement results and actual giver/event context; preserve failed placements, gift generation and letter side effects | Gift-courtesy group inactive, native gift behavior preserved |
| VD-T3 assignment/lifecycle | Admitted party/giver ownership, receipt-scoped anti-reacquisition permission with other restrictions intact, routine versus emergency transition, no forced-job queue destruction, pickup/carry/delivery/cleanup/exit ordering | No scheduling without physical-cargo recovery; preserve/retire existing obligations |
| VD-T4 physical quantity/storage | Actual merged/split receipt tracking, native carrying/placement and destination acceptance; any required shared invalidation seam's causal effect | Ground fallback before pickup; local physical recovery afterward; no made-up success |
| VD-T5 optional Hospitality | Canonical active package, exact Leave/Cleanup/gift and ordinary/urgent departure semantics, needed guest permissions | Disable only Hospitality delivery enhancement, not native gifts/trade |
| VD-T6 state lifecycle | Stable map/event/pawn/item save/load and teardown context, round-trip inactive/unknown obligations, reconciliation before jobs resume | Retain physical state/recovery; no source schema guessed or replayed |

A receipt-observing postfix needs more than method existence when success/body execution is assumed.
An argument-changing prefix/transpiler must prove required use/control flow in effective code, not
just a signature or original IL. Accept compatible foreign owners and harmless edits; refuse false/
unknown/missing/ambiguous semantics. Every transpiler validates its incoming stream on every run and
returns it unchanged if unsupported. Never rerun foreign transpilers speculatively. Handle later
repatch invalidation; install/rollback only the owned atomic group. Guard exceptions/install failure
produce inactivity and one bounded startup warning per skipped enabled patch with stable IDs,
target, predicate/reason, unavailable behavior and fallback. Do not spam each failed ordinary haul.

Canonical original `mlie.justputitoverthere` active means owned trade/gift overlap is withheld. Recover
old owned loads without claiming to migrate them to the source. No package/assembly spoof or global
unpatching. Harmony becomes required when referenced; optional providers remain absent-safe. Dynamic
handover/jobs need C# seams, not XML Def mutation. No XML Extensions use is proposed in this slice.

## Risks / Trade-offs

- Receipt merges can move old colony stock accidentally → exact count/attribute ownership, mixed-stack
  tests and native placed results; raw total deltas alone cannot prove who removed units.
- Departure gifts can be canceled by blanket exit guards → separate routine gift courtesy from
  already-leaving trade and emergency/dismissal, with real native/Hospitality departure tests.
- Native ordered jobs can clear unrelated visitor queues → a guarded automatic context, not synthetic
  player force; preserve important existing work and one finite assigned load.
- Single-load courtesy can leave some goods outside → explicit source-aligned fallback; automatic
  multi-trip service is not silently added. Colony hauling remains available.
- Storage/provider acceptance can drift mid-walk → current checks and physical remainder recovery;
  no claim of remote delivery, capacity creation or universal provider support.

## Migration Plan

No deployment or normal-save/config changes now. Implement receipt state and recovery before enabling
new scheduling. Original uses native jobs, with no custom state importer to reuse. Do not infer our
receipt ownership from arbitrary preexisting native haul jobs. Any future migration needs explicit
backup/preview, atomic application/rollback and verified postconditions. Safe removal remains
unclaimed until pending delivery/cargo behavior is proven. No external source build or assembly ships.

## Open Questions

VD-Q1–4 in research/progress retain finite effort policy, exact receipt and emergency lifecycle proof,
Hospitality/storage/S049/S039 composition, and implementation/native acceptance. No question requires
stopping this specification work or asking the user to explain an existing mod.

## Verification and semantic review

Unit tests cover receipt accounting, finite assignments and policy decisions without loaded mods;
scoped Defs fixtures only where honest, owned Harmony cleanup for guard tests. Real loaded storage,
trade, visitor and Hospitality contexts require fresh isolated game runs. Follow focused vertical
RED/GREEN, affected builds/package checks, independent review, then actual native trade/gift and
movement/pickup/storage workflows on the reviewed build through the minimized Gateway process.
Inspect exact before/action/after screenshots personally and retain build/mod order, causal actions,
quantities, relevant logs, normal-config hashes and cleanup. Diagnostics/direct spawn only prepare
preconditions; successful patch installation cannot prove delivery. Seven-language explanations and
native UI access require actual render checks. All tasks remain unchecked in this documentation turn.

Semantic pass: no changed currency, sale/gift value, chance, research, gift generation or storage
capacity. Courtesy shifts finite hauling labor to available visitors and can expose them to a route;
current access/danger, main-trader/animal exclusions and urgent departure prevent unbounded services.
Receipt quantity, old-stock isolation, automatic-refill versus hauling ceilings and physical recovery
are balance/playability boundaries. Final provider/native observation remains unverified.
