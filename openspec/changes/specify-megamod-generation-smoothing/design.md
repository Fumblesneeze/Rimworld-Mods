## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S078 is original work. [Research](../../../mods/megamod/docs/refinement/2026-11-04-generation-smoothing-research.md)
identifies the inspected 1.7.4998rev1299 binary, read-only extracts and limits of the call-graph audit.
The user requested distributed preparation and complete group arrival; they did not select a
threading model, budget, save format or overload policy. The proposal and observable spec are
drafts. GS-Q1–6 are gates, not permission to ship a partially working scheduler.

Native group enumeration is not resumable generation: its worker returns a fully built list.
Pawn generation may redress/remove an existing world pawn, create relationships and notify scenario
or ideology state. Raid strategies and arrival modes have effects beyond a pawn list. Incident
callers interpret synchronous success immediately. Map generation registers an unfinished map,
holds static working data and nested random state, and executes synchronous steps/finalization.
Wrapping those outer calls in an iterator would not establish this contract.

## Goals / Non-Goals

**Goals:** responsive preparation of the full requested raid/trader/map/encounter scope; atomic
player-visible publication; correct existing-pawn, incident, quest, save and map ownership; honest
measured latency and completion. Preserve native content and direct composition with owned features.

**Non-goals:** worker-thread access to Verse/Unity objects; speculative pawn/stock pre-generation;
changing raid difficulty, stock amounts or map richness; package spoofing; a universal asynchronous
replacement for every third-party API; copied game assemblies/source; a generic task framework or
new profiler; claim of implementation/acceptance from this document.

## Decisions

### 1. Own concrete operations, not a global async patch

Proposed operations carry admission identity, owning game/map, caller continuation, resolved
parameters, completed choices, acquired objects and publication/accounting state. Keep this model
local to generation, with a small shared lifecycle for the concrete raid, trader and map paths.
Do not introduce a repository-wide scheduling abstraction, cross-mod runtime dependency or separate
cache per feature. A single game-owned coordinator bounds total active work; map-scoped references
are released when their exact map/game ends. Boundaries/capacity and admission order remain GS-Q1.

Treat stages as **admitted → preparing → ready → publishing → completed**, with explicit terminal
failure/cancellation and recoverable failure handling. A receipt distinguishes a legitimate later
occurrence from a retry of the same occurrence. Identical parameters alone are not an event ID.
No speculative look-ahead into future storyteller choices. Preserve the native caller's order and
dependencies rather than inventing FIFO across unrelated event sources.

Alternative rejected: returning true from a prefix after enqueueing. Storyteller.TryFire then
updates StoryState and lastIncidentTick; IncidentQueue may remove the entry. Returning false instead
can trigger retries or drop zero-retry entries. QuestPart_Incident also temporarily assigns a target
and clears it immediately after its synchronous call. Copying a mutable IncidentParms reference is
therefore insufficient. Admit only an audited producer/continuation pair that preserves result,
target, accounting and exception semantics. Other callers continue through the loaded provider.

### 2. Slice real work on the game thread and isolate its effects

Use resumable owned stages at demonstrated safe boundaries on the Unity/game thread. Pure
host-safe calculations can be factored separately, but no new off-thread Verse/Unity access is
authorized. Profile before selecting split points: even one GeneratePawn, stock maker, scatter,
flood fill, finalization or spawn batch may dominate. Slicing only an already-materialized list,
or yielding only between huge GenSteps, fails the requested responsiveness.

Audit each native or optional callback for temporary globals, random state, mutable world effects
and cancellation before it can run inside a stage. Stage-local state must not leak through static
pawnsBeingGenerated collections, world-pawn finders, map listers, search, relationships or callbacks
while ordinary gameplay continues. Each yield restores the ambient state needed by ordinary play;
each resumption reinstates only the operation's own state. Reentrancy and nested generation need an
explicit stack/ownership test, not one process-global current-operation variable.

Classify every object as newly acquired for this operation, borrowed existing identity, or already
published. PawnGenerator.RedressPawn changes health, genes, gear, guest/faction and needs before
WorldPawns.RemovePawn; relations and scenario/ideology notifications can affect existing objects.
Do not call it early and assume despawning or destroying the returned list rolls those changes back.
Before admitting a path, prove either staged effects or reversible checkpoints for all such writes,
including recursive generated relatives and provider notifications. Keep existing pawn history;
forcing GenerateNewPawn is a gameplay change, not a cleanup strategy.

Generation random choices belong to the operation once made. Preserve native map seed/per-step
ordering and provider-specific seed rules; restore ambient random context at yields without
rewinding unrelated play. Do not claim passing PawnGroupMakerParms.seed makes Trader deterministic:
the inspected worker explicitly does not implement that seed. GS-Q2 must select and test a safe
random-state strategy, including nested callbacks. Scheduling must not reroll fixed choices, but
bit-identical future global randomness with smoothing absent is not promised.

### 3. Publication is a verified boundary, not merely a loop of Spawn calls

Preparation includes required pawn/stock/gear/loot and owned arrival contributions, plus valid
placement and initial controller plans. Trader gather reachability must be resolved before an
owned success is published: current native code can send its letter and then fail to find a common
spot. Raid strategies can create threats themselves; edge walk-in, drop arrivals and explicit waves
need distinct publication adapters. Pod animations/open delay remain native, not smoothing delay.

Publish all required state with no intervening simulation/input observation of a subset, and emit
the appropriate successful consequences once. Account for callbacks that execute synchronously
inside publication, not just visual frames. If the final commit itself is the expensive burst,
restructure the supported path and verify hidden staged construction rather than moving the stall
to the end. Preflight acquisition/placement, but do not assume preflight makes commit infallible.
Owned rollback and a terminal receipt must cover the admitted failure cases before enabling it.
After successful publication, normal native lifecycle owns the group and no replay is permitted.

Known native effects requiring reconciliation include IncidentWorker's success-only tales but
unconditional incident-record/codex path after its worker, Storyteller's success-only story state,
RaidEnemy's count/last-faction/normal-speed signal, queue retry/removal, quest consequences, trade
lord setup and relationship letters. Do not simplify all of these to one success-only counter.

Direct owned composition:

- S004 clothing and S130 vacuum protection finish before the group can be seen. Ordinary temperature
  scope/raider options do not narrow the all-humanlike vacuum requirement. No late dressing of a
  visible partial arrival and no free gear for existing player-owned pawns.
- S057 preserved-food preference's arrival provisions remain once-only, capacity constrained and
  conditional on a successful trader arrival. Include their staged insertion in the successful
  publication branch; do not grant provisions merely because a letter was constructed.
- S039/S040 gathering/trading-area policy supplies the valid contextual destination directly.
  S027 delivery jobs begin only after visitors legitimately arrive; preparation is not a source of
  free haulers or goods. These changes do not rewrite those feature contracts.
- Shared Thing search and hauling consume native published candidates and their one shared derived
  index. No generation-only full-map Thing cache. Pending internal objects never enter that index;
  publication/teardown uses the same established lifecycle invalidation, not a second scan per port.

### 4. Preserve pause, save and teardown before exposing incremental incidents

For live incidents, ordinary play can continue between admitted preparation stages. A user pause
must not advance simulation or publish a pending arrival. Whether CPU-only preparation continues
while paused is an internal choice only if all hidden-state rules hold. Do not turn the existing
LongEventHandler.ForcePause semantics into an undocumented automatic pause for every live arrival.
The inspected native map-loading path pauses; this is source behavior deliberately changed for
active-game destination preparation. Keep simulation running between safe preparation slices,
subject to player pause. Initial colony creation still uses responsive frame-based loading.

Save correctness is a gate, not an afterthought. A durable operation must retain one occurrence,
resolved choices, exact ownership and stage state through native save/load; raw iterators, delegates,
threads and live static references are not a save schema. Alternatively a proven quiescent checkpoint
may precede a save, but cannot publish while user-paused, advance time, reroll or silently lose work.
**GS-Q3 selects the concrete persistence route before implementation.** It must cover manual save,
autosave, save-and-quit, failed write, load switching, native retry expiry and callbacks that temporarily
mutate parameters. A successful save must never require unknowable reconstruction of side effects.

Retain previous valid saves using the normal atomic-save/backup path; no automatic user-save migration
is proposed. Any schema migration later needs its own backup, atomicity, restoration and postcondition
contract. Teardown cancels only owned unpublished work and reconciles borrowed objects before the
game scope is discarded. During later patch invalidation, stop new admissions and safely settle or
restore pending work under its proven ownership; never unpatch in the middle of an unprotected commit.

### 5. Map and encounter loading is part of the feature

Keep native destination resolution, map-generation order and seeded content. GetOrGenerateMapUtility
reuses an existing map; otherwise it can create/register a MapParent before generation. MapGenerator
then registers the unfinished map, uses MapInitializing/static working data/Rand scopes and performs
scenario, component, parent and post-init callbacks. Native VisitSite awaits the returned map before
its hostile-map notification, relationship/entry letters, diplomacy and caravan entry.

Support requires an audited producer-to-handoff continuation, not returning null or an unfinished
Map from a synchronous API. Continue the existing game during active-game preparation and isolate
the unpublished destination from normal
map selection, ticking, saves, search and unrelated callbacks. Native registration may remain inside
the controlled generation context if needed by gen steps; it cannot become public availability.
Terrain's full-cell loop, rock placement/roof flood fill, scatter and finalization require their own
safe splits where profiling shows a burst. Preserve pathing/region update scopes and restore them on
every supported terminal path. Debug-step mode repeatedly finalizes and is not the production API.

LongEventHandler's IEnumerator route loops MoveNext for up to 0.1 seconds and cannot preempt one long
MoveNext; its async Action route creates a thread. Neither is sufficient by itself. Use the native
loading presentation with an owned bounded continuation after proving scheduling/cleanup semantics;
do not globally alter all other mods' long-event budgets. Complete colony-map, caravan-site and
transport/pocket/gravship branches according to their exact admitted contracts. The latter branches
remain untraced GS-Q4 work, not silently omitted scope or a blanket compatibility promise.

On failure, restore the original caravan/transport ownership, map parent and registration as their
actual contracts require. Native MapGenerator catches some GenStep exceptions and continues; do not
mistake that for a valid complete destination. Define completeness checks and recovery at the owning
entry path before enabling it. Do not delete a pre-existing MapParent or other provider's objects.

### 6. Minimal target specs and admission groups

Create actual stable-ID [target documents](../../../mods/megamod/docs/patch-targets/TEMPLATE.md)
before patches. These are candidate semantic groups, not claimed implemented targets:

| Candidate group | Required facts | Inactive fallback |
| --- | --- | --- |
| GS-INCIDENT | Exact producer/continuation, parameter lifetime, outcome and all native accounting/retry semantics | Loaded incident caller and provider execute normally |
| GS-PAWN-STOCK | Genuine split boundary, current generated-list ownership, world-pawn/relationship/stock effects and reversible failure | That generation path remains synchronous |
| GS-ARRIVAL | Strategy/mode, complete content, placement/controller publication, once-only consequences and failure settlement | That arrival retains its provider |
| GS-MAP | Request/reuse, generation globals/order/seed, complete hidden destination, finalization and entry/ownership recovery | That destination uses native/provider loading |
| GS-SAVE-LIFECYCLE | Save/load identity and choices, previous-save preservation, exact game/map removal and pause/speed boundaries | No incremental work requiring those missing guarantees is admitted |

All required groups for a path must admit together; available subsets cannot expose half an owned
operation. Check minimal callable shapes and consumed data/control flow, including actual effective
prefix skip behavior, transpiler input and later patch rebuilds. Method existence alone does not
prove parameter use or successful completion. Allow compatible foreign owners and unrelated IL edits;
do not require pristine methods, exact binaries or arbitrary whitelists of assembly versions.

Every transpiler validates its actual incoming stream on every invocation and returns it unchanged
on mismatch; never replay foreign transpilers as a probe. Validator exception/unknown, ambiguous
match or install failure disables the affected atomic owned group and removes only newly installed
owned contributions. Reconcile pending work before later invalidation removes its required hooks.
Warn once per skipped enabled patch at startup with package/patch/spec ID, target, failed condition,
unavailable behavior and loaded-provider fallback. Log later invalidation once per affected change,
not per tick. Explicit optional absence is inactive, not a failing candidate.

No XML changes are selected. Harmony becomes required with its first eager use. Optional generation
providers are package-ID/Def-resolved and shape-guarded. No upstream provider identity exists for this
original feature; inspect actual active generation/optimization providers before claiming cooperation.
Fallback preserves loaded-game behavior, not an assertion of pristine vanilla.

### 7. Measurement and UI policy

The [benchmarking contract](../add-rimworld-performance-benchmarking/) and its Circinus reference were
read. Use that owned typed operation surface and exact runtime support, not ad hoc orchestration or
a product profiler. Existing historical 1.6 evidence is not proof of the exact 1.7 beta path. A missing
recurring measurement capability belongs in its separately owned tooling change, not this product's
runtime or an unowned public script.

Proposed workload families: ordinary trader arrival through completed trade; raid arrival through
actual combat with complete equipment/loot; native new-colony/site generation through playable map
entry. Include increasing representative group/stock/map complexity, normal/high speed, save/pause,
multiple pending requests and one expensive inner operation. Events must arise through the native
workflow, with disposable setup separated from measured action and no direct profiled-method loops.
Do not accept results with missing content, stalled jobs or incomplete arrival accounting.

Retain complete-operation wall time, elapsed simulation ticks, largest frame interruption, input-to-
visible-response observations, queue wait, completion/commit tail, pending high-water memory and
cleanup alongside Circinus attribution and native sampling limitations. These are distinct units and
claims. Circinus duty/adaptive sampling can miss a rare generation/commit burst; prove full-interval
frame/input coverage through supported tooling rather than inventing missing samples or relabeling
gross patch time as end-to-end latency. Fresh independent stochastic repetitions retain distributions;
no global seed/ID resets or array-index pairing of product-present/absent runs. Correctness checks of
already-fixed choices are separate from stochastic performance fixtures.

GS-Q1 must use measurements to choose work scheduling, capacity and overload handling. The user's
rough1–2 seconds refers to real elapsed time and is only an estimate: do not turn it into a timeout,
maximum delay, cancellation rule, code assertion or test/performance pass-fail threshold. Report
elapsed time separately from simulation ticks; do not invent a different numeric deadline either. A path that always falls back to synchronous generation does not satisfy this feature's
requested baseline scope. No universal no-stall or speedup claim without the actual final tail.

UI uses native loading context and honest stage labels, no bottom tab/debug preview/finish button.
No setting is selected yet. If measurement/user policy warrants configuration, amend the spec with
values and effect timing; use the common global/save inheritance UI, never a private scheduler menu.
Pending operation state is not configuration. Translate new player text into seven languages;
technical log-only IDs and details stay invariant. Do not invent a UI just to display startup logs.

## Risks / Trade-offs

- **Live simulation changes context while work waits** → capacity/overload policy and current source/intent validation before publication,
  actual event-specific revalidation and no reroll of resolved choices (GS-Q1–2).
- **Borrowed pawns or provider callbacks cannot be rolled back** → prove staging/checkpoints first;
  retain native authority for unsupported paths, never destroy or clone away an existing identity.
- **Synchronous callers observe incomplete results** → concrete producer/continuation ownership;
  no global boolean/null/Map substitution and no assumption that false means no side effects.
- **Saving loses or duplicates pending work** → GS-Q3 blocks incremental admission until the full
  persistence model and failure recovery are specified and tested.
- **A final spawn/finalization or single callback dominates** → measure the actual tail; preserve
  semantics while splitting it or leave that path explicitly unaccepted.
- **A huge compatibility surface turns into an engine rewrite** → sequential concrete paths with
  audited semantic boundaries, no speculative universal wrappers; keep remaining scope visible.

## Migration Plan

1. Resolve GS-Q1–4 and extend the relevant observable scenarios before implementation.
2. Write executable target admission and focused lifecycle/ownership tests, then one complete
   vertical path at a time, with actual inner work split and failure/save behavior included.
3. Compose owned arrival features directly; add optional providers only with proven contracts.
4. Review, run focused checks and verify each reviewed native workflow with causal evidence and
   measured completion/responsiveness. Keep unsupported paths and all remaining scope explicit.
5. Add no normal-save migration or feature-removal promise until a concrete pending-state schema
   exists. At safe session boundaries, failed admission retains loaded native/provider behavior.

## Open Questions

GS-Q1 timing/overload/settings; GS-Q2 incident callers, random/world effects and atomic publication;
GS-Q3 save/load/pause/teardown and schema; GS-Q4 complete map/encounter/transport scope and split
points; GS-Q5 external providers and direct composition; GS-Q6 measurements, target specs, tests,
languages and reviewed-build native acceptance. Record decisions in refinement and amend the spec;
do not stop this specification pass to interview the user about native behavior.


## Reconciled publication and cancellation

Decision12 requires raid/ambush/hostile-entry notices only after complete generation and final
source/intent validation. Bind requests to exact map, caravan and entry-intent identities; a changed
intent invalidates its old request even if coordinates later coincide. Discard owned temporary output
when the source map is abandoned, caravan disappears or entry intent changes. Do not retarget the old
result or publish success accounting. Restore/release borrowed world-pawn and player-property claims;
never classify an existing pawn as disposable because it participated in preparation. Persist terminal
cancellation so save/load cannot resurrect it. Revalidate immediately before the atomic publication.

Background describes observable continued simulation. It does not authorize Verse/Unity access on
worker threads. Audit safe continuations, isolate shared static/RNG state between slices, and use
worker execution only for proven isolated work. Required gear, goods and map finalization precede
notices and handoff. An unsupported synchronous-only path remains explicitly unsupported, not an
accepted background-generation implementation.
