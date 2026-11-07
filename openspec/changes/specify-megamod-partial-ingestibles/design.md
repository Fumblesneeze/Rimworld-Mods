## Context

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This is a specification draft, not executable support. [Research](../../../mods/megamod/docs/refinement/2026-11-05-food-tracker-research.md)
pins installed FoodTracker1.6, public source and inspected native1.7.4998. No game was launched.

The source generates `FoodTracker_*` virtual ThingDefs, records singleton/list fractions, intercepts
chewing/cleanup/splitting/merging, and patches selected UI/trade/caravan paths. That architecture does
not provide coherent quantity semantics to all consumers. Its permissive ThingFilter prefix and
policy edits are especially unsuitable for a consolidated mod. Source completion and interruption
also take different effect paths. Native1.7 cleanup ends in `bool?`, while the source patch asks for
`bool`; matching the method name is not a compatibility proof.

## Goals / Non-Goals

**Goals:** conservation of consumed/remaining quantities; unchanged authorization and item condition;
shared, fresh quantity facts across real consumers; complete dose/effect handling; safe persisted
state; understandable native contextual UI; minimal semantic patch admission.

**Non-Goals:** executing or porting the source assembly; copying its internalized Harmony; new
content/research; a food dashboard; automatic save migration; arbitrary fractional medicine/special
items; source duration/discard balance changes; unsupported external framework compatibility claims.

## Decisions

### One owned quantity model attached to actual items

Prefer retaining the original ThingDef/Thing identity with owned persisted per-unit remaining state.
Use guarded XML Extensions to add a component only to genuinely compatible finalized shapes where
declarative integration is appropriate. Preserve unrelated comp/list entries, add once, and guard
external packages with package-ID FindMod plus actual content/node shape. This makes XML Extensions
required at first use; do not perform equivalent load-time Def edits imperatively. Runtime instance
state remains C#. Do not reproduce generated Defs, short-hash allocation, resource-counter resets
and `ThingFilter.Allows=true` as an alternative identity system.

Initial untouched items are full without allocating a list per ordinary unit. Materialize bounded
per-unit distinctions only where partial contents/meaningful provenance need them; equal runs may be
represented compactly. Physical stackCount remains physical count. Do not average distinct portions.
Stack compatibility must retain ingredients, poison state, rot, HP/quality and provider condition;
some combinations legitimately cannot merge. Preserve the native oldest-condition/poison semantics
where applicable and the selected poisoning fix, never normalize by cleaning or inventing quantities.

An item-level remaining model must expose total and exact selected-subset contributions, distinct
from per-full-unit baseline stats. Do not globally return stack-total Nutrition/Mass/MarketValue
from a per-unit stat hook and then let consumers multiply by stackCount again. Inventory, recipes,
trade, travel and selection must agree on the same physical unit ordering. Revalidate the selected
subset before commit; if it changed, refresh/reject instead of moving another subset for the old price.

S041's [finite-taint contract](../specify-megamod-food-taint-accounting/specs/megamod-food-taint-accounting/spec.md)
requires each physical unit to have equal selection chance without replacement, including partial
transfers; empty/failed transfers cannot change future allocation. That is not automatically compatible
with a fixed quoted half versus quarter portion. FTR-Q4 explicitly blocks the combined path until one
observable selection/quote policy preserves fairness, hidden-condition boundaries, exact delivery and
read-only previews. Do not choose a fresh random portion after quoting a different one or silently
change S041. Any required change to that contract needs its own delta before implementation.

### Transactional consumption at the actual ingestion boundary

Track actual actor, recipient, job/toil identity, item/holder, available batch and remaining work.
Capture work after native initialization succeeds; feeding can have different actor and recipient.
Use actual progress, not elapsed wall time or a stale job.count. Integrate normal finalization and
interruption through one owned commit path with idempotent progress tokens. Account for native
interval ticks, changes in duration, reentrancy, zero/invalid counts, fast completion, downing/death,
external destruction, reservations and lawful carry/drop behavior. Do not globally suppress TryDrop
for a tracked Thing or call cleanup again as a speculative probe.

Before mutation establish which portions will be consumed, which effects will commit and where the
remainder will reside. Allocation/placement failure must not follow irreversible effects with a
second vanilla invocation. Keep the actual holder until a supported transfer commits. Do not recreate
destroyed items from snapshots. Persist committed amounts/work sufficiently for mid-toil save/load;
job reinitialization cannot reset or duplicate progress. Scope transient coordination to the game,
clear it at teardown, and rebuild from serialized identities rather than persisting live references.

FTR-Q3 also blocks partial multi-item batch handling. Native JobDriver_Ingest uses one duration for
a batch, not a duration multiplied by count. For three original items, 75% completed work could leave
0.75 item with 25% of original batch time outstanding; recalculating per-item work instead gives75%
of a one-item duration. Choose and specify resumed/recombined work and another eater's modifiers
before enabling that path. The single-item quarter-meal scenario does not settle batch timing.

The spec intentionally keeps every positive portion and normal whole-item duration. Fractional work
has only the one-tick minimum needed to finish in simulation. Source40-tick,4.5% discard and drug XML
overrides are not adopted. Numerical tolerance may detect invalid state; it cannot become a hidden
gameplay disposal threshold. Invalid state must remain recoverable, never padded with invented1%
portions or reconciled by silently shrinking physical stacks.

### Effects are an admitted whole contract, not a nutrition-only hook

Native `Thing.Ingested` includes thoughts/history, joy, poison checks, hediff/gene notifications,
splitting, comp callbacks, outcome doers and destruction. `Toils_Ingest.FinalizeIngest` separately
credits the recipient's food need/records and contextual meal thoughts. Caravan needs call Ingested
directly. `CompDrug` owns addiction, chemical need, overdose, drug-policy history and timestamps.
`IngestionOutcomeDoer_GiveHediff` can apply one configured severity regardless of ingestedCount.
Changing only `IngestedCalculateAmounts` cannot safely scale this pipeline.

Use resolved native/full-portion effects and recipient modifiers, not the six hardcoded source
effect tables or direct need writes that bypass native bookkeeping. Keep baseline additive direction:
source subtracts positive existingAddictionSeverityOffset, while inspected native CompDrug adds it.
Source's fractional reingestion flag checks generated names before resolving originals and its manual
effect path scales by current-attempt progress; both are rejected. Unknown outcome doers/callbacks
need an explicit absent-safe adapter and complete contract, not automatic fractional invocation.

FTR-Q1/Q2 block affected paths: settle fractional stochastic outcomes and categorical events, including
exposure provenance across split/merge, multiple recipients and later sessions, native letters,
chemical-dependency resets, meal/table thoughts and food poisoning. A proposed invariant is that
repeating cancellation/reload cannot reroll consumed exposure; this is not yet a chosen probability
formula. Do not silently choose linear probability, repeated Bernoulli rolls, a threshold per bite,
or a full-dose event on the first taste. Add exact observable scenarios before implementation.

### Direct composition and shared data

Native listers remain primary. Ctrl+F/hauling's one map-owned index may index current remaining
quantity, condition and holder, with invalidation on consumption, split/merge, transfer, spawn,
despawn, destruction and metadata changes. It supplies candidates, not permissions or reservations.
No independent FoodTracker dictionary of all map food. Selection-specific snapshots belong to their
dialog/request, expire on selection/world changes and never mutate simulation or RNG while rendering.

Compose directly with selected personal supplies/Meals on Wheels, hauling providers, preserved-food
preference, Food Poisoning Stack Fix, Drug Policy Fix, S086 medical go-juice, Drug Response(S124),
Priority Treatment(S134), RPG inventory(S132), Bulk Load(S133) and Basic Caravan Loading(S135).
These remain separate feature owners within the same mod, not patches against one another. A supply
target expressed as full meals/doses uses usable equivalents; stack thresholds remain physical.
Medical authorization/priorities are not altered. Shared quantity/condition changes must invalidate
both displays and selection candidates; actual action rechecks native reachability/ownership.

Nutrition-based ingredient selection can consume supported fractional nutrition. Count-only recipe
consumers need explicit fractional accounting or must exclude partial units with a reason. No tiny
leftover-to-full-product conversion. Balance/exact consumer coverage remains FTR-Q4 and a gate for
the affected path. No direct runtime dependency on Immersive Chefs or any other repository mod.

### Contextual UI and state scope

No new bottom tab, separate meal dashboard or routine toggle. Native inspect/stat, inventory, trade
and loading surfaces show remaining totals and individual portions. Keep useful upstream stat
explanations instead of replacing them with English literals. Amounts too small for two decimals
need a positive-value presentation. Show selected totals and refresh on subset changes. Seven baseline
languages require actual native layout/input verification later.

Source's only setting is verbose logging, defaultfalse; several helpers log unconditionally anyway.
Do not import it as a gameplay setting. If needed, transient diagnostic controls belong in the top
developer toolbar, defaultoff, cleared on game change and inaccessible with developer mode off.
This scope exception reflects diagnostics, not save policy. Item quantities/progress are entity
state. No arbitrary new sliders or empty settings framework. A future actual balance preference
must first add observable defaults/timing and use the common global/save/Use-global mechanism.

### Admission and external contracts

Core plus external Harmony are required for actual C# patches; XML Extensions with first guarded XML
integration. No unconditional PatchAll. Before each real patch, add a stable target document/index
linking its scenario, executable validator, tests and evidence. Proposed seams to investigate:

| Seam / atomic responsibility | Minimal facts that must be proved |
| --- | --- |
| ChewIngestible/init/progress | Correct actor/recipient/item, duration argument feeding actual chew duration and progress; native init success and interval work semantics |
| FinalizeIngest, Ingested and amount calculation | Single consumption/effect commit; intended portion selection, nutrition and all required callbacks; skip-original interactions cannot bypass ownership |
| Job cleanup / carry-drop / item destruction | Exact callable overload including nullable final argument; cleanup ordering relative to driver/holder state, and no later duplicate consumption |
| SplitOff / TryAbsorbStack | Actual moved subset/count, native ownership and compatibility; no fixed local-number assumptions or silent failed insertion |
| Save/load/lifecycle | Owned component data available at restoration, mid-job resumption and stale-state cleanup; absent required provider cannot expand quantities |
| Nutrition/recipe/supply consumers | Whole-unit baseline versus remaining selected quantity; existing authorization and consuming path really use the supplied amount |
| Mass/value/trade | Correct subset and price/mass contribution, buy/sell direction and final transfer; no UI-only correction |
| Travel estimator and direct ingestion | Selected inventory identity and count, diet/forage/grazing/baby assumptions; no cloned broad native calculator or RNG mutation |
| Contextual displays | Exact quantity/view binding; a pure observer needs only the callable seam it observes unless original execution matters |

Use effective semantics and every transpiler's incoming stream, not just original IL. Permit benign
foreign owners and unrelated changes. No repeated execution of foreign transpilers as a probe.
Fail false/unknown/exception/ambiguous checks closed; roll back newly installed owned members of
the declared atomic group only. Log one bounded startup warning per skipped enabled patch, with
package/patch/spec/target/predicate, lost behavior and fallback. Revalidate supported later repatches.
Creation of new partial items requires coherent persistence/consumption/accounting support; existing
partials require recovery-safe handlers even when further creation is disabled. Missing safety
support blocks affected actions, not silent native full-item fallback.

With `b0arl0ck.foodtracker` active, original owns overlap. No foreign state or patch mutation. A save
already containing owned partials cannot be handed to that provider without a specified migration.
Bounded installed text/ASCII assembly scans found no incoming consumer; that is not proof of no
reflection/UTF-16 strings or third-party consumers. No exported FoodTracker assembly/types/settings
or `FoodTracker_*` aliases are promised. Actual consumers require a named, narrow adapter contract.

## Risks / Trade-offs

- Broad native ingestion hooks → smallest supported item/path groups, focused effect inventory and
  atomic admission; no universal content compatibility claim.
- Rich per-unit provenance may cost memory → compact equivalent runs, native/shared facts, bounded
  benchmarks for combined hauling/search/stack churn; do not average away observable differences.
- Pure display fixes hide economic bugs → compare actual selected transfers and completed trades,
  recipes, capacity and travel eating against the same quantities.
- Fractional effects can change balance → inspect finalized effects for all six drugs plus meals/raw
  food, settle FTR-Q1/Q2, then verify native outcomes; source code is evidence, not acceptance.
- Provider disablement with saved state → preserve state and block affected actions with a recovery
  explanation; never turn leftovers into full items or silently rewrite the original save.

## Migration Plan

No automatic FoodTracker save/config import, generated-name aliasing or removal-safe conversion in
this change. Implement owned schema/version validation and honest missing-content recovery first.
Any later migration needs an owned observable contract for explicit selection, backup, atomic commit,
failure restoration and verified postconditions. Development/acceptance uses disposable saves only.

## Open Questions

FTR-Q1 stochastic dose/food risk and event schedules; FTR-Q2 complete categorical/native/provider
effects and supported item/path matrix; FTR-Q3 batch timing, save/feeding/lifecycle and migration limits;
FTR-Q4 exact recipe/supply/trade/transfer subsets, S041 fair selection/quotes and composition; FTR-Q5 optional providers, consumed
contracts, performance and localized UI; FTR-Q6 implementation/target checks and final native evidence.
See the refinement ledger for resolution criteria. Artifact completeness is not implementation readiness
for unresolved paths, nor acceptance of any gameplay.
