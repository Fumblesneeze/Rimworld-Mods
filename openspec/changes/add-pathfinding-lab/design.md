## Context

The 2026-10-25 graph-sharing slice retains the user-requested Stopwatch diagnostic, not a
Circinus baseline. Collector v5 measures ApplySnapshot and immutable Publish under background
elapsed time. The legacy LockWaitMilliseconds field now records workspace admission elapsed
time (including first-use workspace construction), not blocked wait. Map/cohort fallback counts
separate graph-unavailable from capacity exhaustion. A test-only internal publication-boundary
observer permits deterministic invalidation/disposal during a real build; production supplies none.
Native 1.7 dispatch drains prior jobs and filters cancelled requests before dispatch, preserving
single-request ownership of a follower cursor; query workspaces are independently leased.

### Raid-stress snapshot diagnostic (2026-10-24)

Owner remains Pathfinding Lab. User explicitly authorizes adding raids to a disposable copy
of the preserved colony. Generate four native RaidEnemy incidents from the same live hostile
human faction using IncidentActionStep and native storyteller points/strategy/arrival choices;
retain actual counts, factions and Lord jobs. Drop-pod incidents advance natively until pods land
and open before counting their pawns; setup time may therefore elapse between incidents.
Do not change the permanent master. Allow normal
vanilla simulation to approach the colony before a native save/load boundary, then publish a
non-overwriting temporary snapshot plus hash/tick provenance under ignored raid-stress artifacts.
No teleportation, invulnerability, needs/RNG normalization, or hand-built raider jobs.

The setup deadline is600 wall seconds/40000 ticks for at least20 raiders within100 cells of
the initial colony center; failure retains diagnostics rather than silently making a weaker input.
Two comparison IDs reuse the unchanged Stopwatch collector and exact mod list, with Experimental
false versus true/horizon50. This means routing disabled, not assembly absent. Use four successive
600-tick windows per fresh process, V/E/E/V order, all-pawn and hostile-only timings and movement
outcomes. Actual tick overshoot is retained. Death/downing and destruction are expected combat
outcomes and do not reject a repetition; missing instrumentation, emergency fallback, no hostile
search/progress, or runtime failures do. Record initial actors through death or despawn so attrition
cannot masquerade as better throughput. Keep cold/background preparation and first movement separate.
Same snapshot does not guarantee identical stochastic trajectories; this is a Stopwatch diagnostic,
not Circinus/CPU/causal net-impact evidence. Existing peaceful-colony health rules remain unchanged.

### User-saved working colony diagnostic (2026-10-24)

The user supplied `medium size colony` on 1.7.4985. Its immutable read-only master lives outside
disposable artifacts at `C:/Manuel/RimWorldBenchmarkSaves/PathfindingLab/medium-size-colony-20261024.rws`;
SHA-256 `ECACE785FB75FE4A0B8D140B24749B17DFF71048A282739A14F90F5589D77F97`.
Product-owned E2E fixtures selected through `e2e_run_start` validate/copy it to the isolated save folder
and invoke native load as setup. No source overwrite, job normalization, teleportation, needs changes,
incident suppression or population replacement is allowed for this workload. Native time control
resumes autonomous work. Exact save identity, loaded population, workload snapshots and native views
are retained. Each fresh process measures successive 300-tick windows (first labelled cold, later warm),
three windows initially, at Fast speed. Vanilla and horizon50 are the primary comparison; horizon200
is an optional follow-up. Durations and repetitions are retained, not assumed equal native trajectories.
The existing user-requested Stopwatch diagnostic is reused, not called a Circinus baseline, CPU time,
or a causal net-speedup measurement. Shared native costs, worker elapsed/preparation subset, all-pawn
request counts, censored movement onset and routing/repair/fallback totals stay separate. Detail
overflow does not invalidate complete aggregates, but prevents percentile claims from that detail.
Loading, screenshots and setup are outside observation windows. Native autonomous advancement is
bounded by game ticks and wall deadlines; stalled or unhealthy runs remain failures, never discarded.
The first native load on 1.7.4985 advanced tick909 to910 before returning player control with
pause-on-load enabled. Accept only909/910 as the input handoff, report that delta explicitly, and
never rewind or normalize it. The rejected exact909 pilot is retained separately with no measurements.

Owner: Pathfinding Lab (`fumblesneeze.pathfindinglab`). The inspected beta is 1.7.4984 rev1242, with asynchronous `PathRequest` and native-cost grids. Vanilla's map-data dirty queue must not be consumed by this mod. Product code cannot depend on the Gateway used for tests.

## Goals / Non-Goals

Goals: demonstrate reusable spatial abstraction, bounded forward refinement, conservative fallback, runtime A/B switching, and staged oversized-map viability.

Non-goals: optimality, transit-node all-pairs tables, 1.6 support, and Workshop release. The original drafted-OnCell restriction is superseded by the all-request integration below, explicitly requested on 2026-10-24.

## Decisions

### Shared immutable graph and bounded concurrent readers (2026-10-25)

Owner remains Pathfinding Lab. This supersedes exclusive mutable cohort guides below,
not native traversal policy or the50-cell diagnostic horizon. All five cohorts currently
use identical optimistic static geometry, so one map-owned background lane prepares one
immutable portal graph for every cohort before demand. Cohorts retain independent request
counters; permissions/customized costs remain request-local and every returned edge is
validated as before. No graph sharing between different maps.

Publish immutable chunk component/portal data with copy-on-write only for affected chunks;
reuse owned geometry chunks and unchanged graph chunks. A reader retains its exact graph
version until its result has been consumed. Never mutate published arrays or hold native
arrays/live Map/Pawn objects on builders. Coalesce revisions, reject stale publication and
retire disposal without main-thread waits. New requests during rebuild remain vanilla;
already admitted readers may finish against their native prepared request snapshot.

Use a bounded map-owned pool of eight persistent query workspaces (not per-pawn storage).
Each lease exclusively owns its router scratch, local caches, output/destination storage
and native request adapter; read-only graph data is shared. Capacity eight is a provisional
memory/concurrency tradeoff: lazy full-repair scratch may retain16MB per used slot on a
million-cell map (up to128MB plus queues), while local scratch remains bounded. Do not block
on pool exhaustion: count it separately from unavailable geometry and use native fallback.
Warm acquire/release and unchanged preparation must avoid managed allocations. Small bounded
temporary goal/neighbor data remains stack-backed; results use spans/memory with explicit
lease lifetimes. Coarse plans/repair cursors survive worker changes, but never cross map
identities, changed goals/movement rules, or invalid dependent chunks.

Keep native jobs, idle-wildlife behavior, output horizon, map size and experiment input
unchanged. Update test-only preparation scopes to the shared builder (one build, not five)
and retain workspace occupancy/exhaustion and graph-unavailable counters. Run focused
concurrent-reader/publication/disposal tests, independent review, native movement/edit and
multi-pawn work acceptance, then the same immutable pre-approach raid V/E/E/V protocol.
Retain historical results separately and report instrumentation/structure differences;
this remains a Stopwatch diagnostic, not causal CPU/TPS or optimality evidence.

### Reusable search storage and locality (2026-10-24)

Keep ownership at the existing exclusive cohort guide lease: one reusable local workspace
and a lazily allocated full-repair workspace per router, not per pawn or search. Store cost,
parent and generation together in contiguous node records; generation stamps avoid whole-map
clearing on each search. The local workspace covers at most65×65 cells. Reuse the array-backed
priority queue and path reconstruction storage, growing only when capacity is insufficient.
Full repair may retain16bytes per map cell per used cohort (16MB on1000×1000), plus its queue;
this is an explicit memory-for-allocation tradeoff, not a zero-memory optimization.

Use ReadOnlySpan<int> for borrowed destination inputs and ReadOnlyMemory<int> for bounded
path views. Returned views are borrowed until the next call on that router/cursor; the native
adapter must consume them before releasing the profile lease. Copy only when ownership must
outlive scratch reuse: cursor repair routes, changed destination sets, and immutable asynchronous
geometry publication. Retain full-tail directed-edge validation without copying that tail.
Memory<T> is a view/lifetime tool, not automatic pooling or permission to mutate background
readers. No routing/cost/budget/goal/renewal behavior change is intended. Allocation tests compare
warmed deterministic host workloads, not total-game allocation or a promised speed multiplier.

### Native preparation coverage repair (2026-10-24 continuation)

Prior colony traces intermittently lacked door/provider samples although native dispatch runs
that job for each global search. Test-only instrumentation must wrap the actual ManagedJob at
both queued and synchronous dispatch sites, with exact structural guards, instead of depending
on late-installed parameterizer/Execute patches. Capture origin/pawn identity before scheduling;
preserve execution, dependency, exception and GCHandle disposal behavior. Require per-pawn
door/worker count equality, successful completion and no pending work before accepting a
full recorded-scope comparison. Direct idle walks are not global workers. This is diagnostic
instrumentation, not product behavior or a claim that remaining scheduler overhead is timed.

Snapshot capture now compares each cost/corner pair before allocating a replacement chunk.
Clone the previous payload only on the first difference, then fill remaining cells from the
same single native read pass. This preserves older immutable snapshots, geometry revisions
and clipped chunk behavior. An unchanged million-cell refresh may allocate a new chunk-reference
table and snapshot metadata, not a replacement payload per chunk. The host allocation regression
allows64bytes/chunk plus1024bytes metadata headroom; no wall-time threshold or game-speedup
claim follows from that deterministic memory bound.

The allocation follow-up also replaces per-cell graph flood-fill iterators and per-chunk
queues with one reusable1024-cell FIFO and one eight-integer stack span per Prepare call.
Boundary sampling, component membership and ordering remain unchanged. The256-square open
graph allocation regression allows1MB for durable portal nodes and boundary metadata;
it excludes constructor allocation and does not imply a zero-allocation graph build.

### Directional idle wildlife and asynchronous guides (2026-10-24 follow-up)

User-authorized owner remains Pathfinding Lab. Only exact native WanderAnywhere/WanderHerd
job givers for factionless, non-mental-state ordinary animals are changed. Preserve native
wait/job cadence and herd-root selection. Build 3–10 adjacent legal cells using a persistent
heading, bounded random forward variation and turn-away after blockage. No destination-first
cell search or hierarchy/grid preparation for admitted idle walks. Treat the native herd circle
as a wall; animals already outside it use ordinary regrouping. Purposeful fleeing, feeding,
hunting, map exit, duty/pen/other wander providers and special locomotion remain normal jobs.
Validate every edge again before handing a pooled path directly to the native request/follower;
invalidated short walks fail/rethink instead of invoking a global search. Direction/route caches
are transient; a loaded eligible idle order without a retained path fails/rethinks once.
Do not relax forbidden cells, hazards, doors/fences, diagonal corners or destination reservations.
Conservative local refusal may produce a native wait instead of vanilla's relaxed random fallback.

Construct and refresh every cohort graph on a bounded single background lane per cohort, using
only owned immutable geometry snapshots. Copy native geometry while its native lease is valid;
share unchanged immutable chunks across snapshots/cohorts. Never retain native arrays/Map/Pawn
on background work. Coalesce invalidations, suppress stale publication, and make unavailable,
busy or failed guides select vanilla without waiting. Native validation/result/follower handling
stays intact; readiness fallback is expected, counted separately from exceptional failures.
Local fast searches are allowed only after the requested guide revision is ready. Guide disposal
retires pending publication without waiting on the main thread. No synchronous EnsureGraph may
be reached by an admitted request. Keep snapshot-copy, background build, dispatch and native
fallback timing distinct; moved background work is not eliminated work. Reuse the immutable
saved-colony input and report request reduction plus movement latency, not only remaining-call means.


### Shared-cohort optimization (2026-10-24)

The user requests persistent hierarchy reuse for player pawns, wildlife, allies and enemies,
plus the measured preparation/invalidation/local-search improvements. This supersedes the
four-entry native MapGridRequest cache below. Keep one lazily built guide per map/cohort:
player, factionless wildlife/pawnless requests, allied, hostile, and neutral non-player.
No per-pawn or tuning/allowed-area hierarchy and no cross-cohort eviction. All cohorts use
the native gathered normal static cost geometry as optimistic guidance, not the first pawn's
customized cost grid. Permissions, allowed areas, movement costs, flying/bashing and directional
connectivity remain request-local native validation/repair inputs; cohort equality grants no
permission. Relationship changes choose a new cohort on the next request.

Observe (never consume) native GatherData cell/rectangle deltas before they are cleared, only
when that gather actually runs. Publish immutable per-chunk revision stamps. First use or native
full recomputation may read all cells; subsequent profile refreshes inspect only changed chunks.
Borrow native normal-cost arrays only within the engine's existing completed-job/gather lifetime;
do not read live Pawn/Map objects on workers. Keep corner geometry conservative for all cohorts.
Separate finite-cost dirtiness from connectivity dirtiness; only topology changes rebuild
components and boundaries. Cache/plan dependencies are chunk-local (including diagonal neighbors)
so unrelated edits do not cancel a valid route. Changed costs still invalidate affected cached legs.

A complete request-aware local search may precede guide refresh/locking: maximum 256 expansions,
within 32 cells of the start, only for nearby goals. It honors all native edges/costs and proves
arrival at a legal destination before returning a bounded follower prefix. Failure escalates to
shared portal guidance and then existing full repair. Constants are provisional diagnostic choices,
not global search caps. Long searches remain serialized per mutable guide; local work avoids that
lock. Add separate test-only refresh/graph/lock-wait timing subsets plus cohort build/reuse/cell-read,
local-hit and routing counts. Do not time per cell or ship Stopwatch instrumentation in the product.
Repeat the same saved-colony protocol, retaining prior immutable results and fresh vanilla controls;
report changed instrumentation identity and independent trajectories, not matched causal deltas.

- Use fixed 32-cell chunks rather than a quadtree initially. Flood-fill each chunk under the same diagonal legality as local search. Sample every contiguous passable boundary run at its ends and midpoint. Cross-boundary portal edges are exact; intra-component portal edges use optimistic octile distance. This is approximate hierarchical routing, not an admissible end-to-end optimal solver.
- Build the static map snapshot once, then refresh only dirty chunks and their neighbors. A graph generation invalidates coarse plans; unaffected local directed paths survive. Cache local paths by endpoints and cardinal/diagonal movement costs with a bounded capacity. No all-pairs preprocessing or unbounded per-pawn retention.
- Refine portal-to-portal segments lazily using chunk-confined A*. Return at most the selected 25/50/100/200 steps (default 50) and request renewal at half the selected horizon remaining. Each local leg is computed whole inside at most 1024 cells, including serpentine corridors longer than the output horizon; buffered lookahead is bounded by horizon+1024 cells. This is whole-chunk partial refinement, not a strict expansion-budget real-time search. Keep the final native job destination unchanged. Cached coarse plans resume from progress rather than rerouting the entire map at each segment. Repeated routes reuse local portal paths. Rebuild only boundaries incident to dirty chunks, preserving unrelated portals. Changing the horizon affects new segments, not the length of an already returned path.
- Intercept only the native pawn follower's request factory, preserving its bookkeeping. Resolve a real pooled PawnPath through a narrow shape-checked adapter; unsupported requests reach the original queue unchanged. Native path-following, collision handling, arrival, cancellation and jobs remain authoritative. Synchronous FindPathNow remains vanilla. Doors, fences, construction placeholders, dangerous cells and provider-occupied cells are excluded from the conservative graph; if that prevents routing the whole request falls back. Bulk path-cost recalculation invalidates the snapshot, without consuming vanilla's dirty queue.
- Vanilla is the default and a translated selected-colonist command switches modes. Settings provide the same mode switch and an explicitly warned settlement-size override: vanilla, 500, 750, or 1000. No quest-map override. Changes require the next generated map; no existing map resizing.
- No custom research, items, costs, stats or progression. Harmony is required; Gateway is development-only. Seven repository locales are supplied. No new raster art is needed for this local prototype.
- Tests link the engine-independent core source into a clean host unit environment. Live tests order drafted pawns through native commands, compare arrival and paths, and capture before/order/after views on the reviewed build. Test cases distinguish cold graph construction, warm cache reuse, invalidation, unsupported fallback and mode switching. Large maps advance 500 → 750 → 1000 only after the preceding size is viable, with bounded time and memory observation.

## Risks / Trade-offs

## Proactive preparation and complete raid approach (2026-10-24)

On the first playable map update after loading or enabling experimental mode, capture
owned geometry and schedule all five cohort guides, including absent hostile/allied
populations. Map updates run while paused. Observe native cell/rectangle/full-dirty
notifications without consuming their queues; refresh changed chunks on subsequent map
updates even if no path is requested. Read normal path-grid costs and edifice full-fill/fence
flags on the game thread (the same inputs used by inspected 1.7 native cost sources).
Background tasks receive only owned immutable snapshots. Retain per-map reader delegates
and avoid allocation on unchanged updates. Existing request snapshots remain a safety net.
Disposal prevents new scheduling and late publication; cold/rebuilding/busy requests still
use counted nonblocking vanilla fallback. All five guides increase retained initial memory;
preparation is eager, not claimed free or guaranteed complete before an immediate raid.
The former GatherData invalidation hook is removed: replaying the native dirty queue after
proactive capture would duplicate scans, including a whole-map scan at the first native gather.

The earlier RaidStress snapshot starts during combat, so it cannot measure the initial
long-distance approach. Retain it separately. Create a new immutable snapshot with four
native EdgeWalkIn/ImmediateAttack raids while paused, before their approach. Load the same
input in fresh V/E/E/V runs. Install timing before enabling experimental mode and await
all-cohort readiness while paused, without synthetic path requests. Report that preparation
separately, then measure six windows targeting cumulative 2000-tick boundaries from the
first unpause. Native scheduled pause is frame-observed, not an exact per-tick interrupt;
retain actual boundary ticks and normalize by the actual simulated duration. Use cumulative
targets so small pause overshoots do not accumulate across six windows.
Include every measured initial approach request, renewal and rebuild. Record initial and
window-end hostile distance from the fixed original colony center, activity and attrition;
do not wait for arrival before starting measurement or normalize combat/random state.
Preparation has observation ID -1, gameplay IDs 0..5, consistently across every export
and background ticket. The native save/load handoff may advance zero or one tick; retain the actual amount.
Summed execution duration is not CPU time or whole-game speedup. Report hostile work
separately from wildlife, which has a distinct idle-wandering optimization.

Large native simulation can advance below 60 ticks/second. The observed first 1000-square traversal completed in about 132 seconds, while the return advanced only 8572 ticks before the former 180-second wall deadline. Large journey waits therefore allow 600 seconds / 40000 ticks / 60000 frames. On 1000-square maps use four alternating full-width crossings in ABBA mode order (vanilla outbound, experimental inbound, experimental outbound, vanilla inbound), instead of eight. Each algorithm still covers both directions; report the smaller sample count and retain first-use versus subsequent costs (not a repeated directed-route cache trial at 1000). 500/750 keep eight crossings. Record world seed, tile and biome; distinct native generated maps are not a controlled map-size scaling experiment. Obstacle/edit comparison can use the eight-crossing 750 fixture; the 1000 viability/timing run need not duplicate that scenario.

These are bounded experiments, not reserved per-step budgets: the existing 2400-second / 200000-frame / 400000-tick fixture ceiling is deliberately shared by setup and every journey. Launch large runs with the outer 3600-second cap, which also includes generation. Not every journey may consume its individual maximum. If a shared ceiling is exhausted, retain partial data and identify that precise limitation rather than declaring a complete comparison. Based on observed 1000 timings, four crossings leave substantially more room than eight; no unbounded retry or automatic deadline extension is permitted.

Renewal uses the follower's existing 30-tick `NeedNewPath` cadence and a <=26-node threshold, not an exact 25-step interrupt. A pawn can consume several nodes between checks. Live assertions require observed early renewal before exhaustion and bounded segments, and retain the actual node counts. Fixture pawns start fed/rested; movement costs before/after each trip are recorded to expose mobility drift in walking-time diagnostics.

The v2 direct-timing fixture creates a new 25-year-old colonist with no gear/addictions. Before each journey it removes hediffs and fills needs outside the timer, equally for both modes; it never changes position or resets state during a journey. This follows a demonstrated 750-square v1 failure: needs deteriorated, and the fifth arrival screenshot shows the pawn in Murderous rage rather than drafted movement. The timing window closes as soon as arrival is observed, before camera/UI steps. Assertions require a still-drafted, non-mental-state pawn at the destination and no failed/fallback samples. Targets are forbidden to autonomous haulers. Standard 250-square runs use Fast; large runs use Superfast equally for both modes, recorded per trip and reported separately. Large open fixtures clear a five-cell corridor; large barrier fixtures additionally clear and restore an 81-square detour area. The generated world outside those bounded areas remains intact. Keep v1 exploratory measurements distinct from v2 comparisons.

The v3 fixture follows the user-observed mechanoid downing on generated 750 map run `20261021T232908528Z` (first arrival timed out, no completed timing sample). Setup removes all non-player pawns, including held casket occupants, hostile buildings and identified pawn/hive spawner Things from this disposable generated environment; these removed environmental objects are not claimed restored. Neutral geometry and vegetation remain. It temporarily replaces the storyteller incident queue, disables new storyteller generation, and applies one test-owned, map-instance-only prefix to the native wildlife tick because there is no per-map spawn-disable switch. Cleanup restores the original queue/control and removes that patch. Damage remains enabled. A fixed normal-quality cloth parka, pants and collar shirt replace the naked fixture; native temperature range and actual movement costs are recorded. Every arrival poll rejects injury/downing/death/job interruption immediately. No in-trip healing, resurrection, invulnerability or teleporting is permitted. Keep v2 and v3 results distinct; this changes benchmark conditions, not the product algorithms.

- Sampled portals and optimistic abstract costs can yield detours → measure route quality and label results approximate; vanilla fallback if refinement fails.
- First-use snapshot and flood fills can stall → report cold costs separately; no claim that preprocessing is free.
- Beta API drift → explicit supported version and narrow shape checks; disable experimental hooks with one diagnostic on mismatch.
- Cost rules are broader than geometry → conservatively restrict eligibility and verify each returned segment against current cells; do not claim arbitrary pawn equivalence.
- Large maps can exceed engine limits despite successful generation → record only observed scenarios and stop escalation on resource failure.
- Direct timing is now explicitly requested by the user: test-only Harmony scopes collect Stopwatch timestamps around native worker search and experimental request resolution. Retain per-query samples and per-trip totals; isolate the measured pawn by request/output identity. Experimental map snapshot and graph maintenance are nested preparation subsets. Vanilla shared cost-grid jobs/scheduling are excluded, not mislabeled as free. First use includes JIT/cache cold effects; repeated native round trips establish warm behavior. This diagnostic is not a Circinus baseline or a whole-game/TPS speedup claim.

## Timing follow-up (2026-10-24)

Retain the existing direct-Stopwatch diagnostic, not a Circinus baseline. Add all-pawn,
map-scoped windows with per-source attribution and map-shared preparation. On the inspected
1.7 shim, wrap only grid batch actions created inside this map's ScheduleGridJob scope;
do not time each of a million grid cells or change scheduler dependencies/batching.
Workers hold immutable IDs and their originating sink, not live Pawn/Map objects.
Nested experimental preparation uses outermost scopes to avoid double counting.
Report pending callbacks and detail overflow. Preparation is included in recorded total;
recorded total is the sum of execution durations and can exceed observation wall time.
Uninstrumented scheduler/result delivery overhead remains explicitly excluded.

Observe StartPath-to-first-native-movement separately. StartPath calls made while already
moving are not fresh-start latency observations. Cancellation, retargeting and no movement
before window closure must not become zero milliseconds. Use unpaused native orders, with
screenshots outside the first-movement interval. All-pawn totals include ordinary jobs even
when the product declines them. No claim of undrafted/Thing-target/door routing support.

Expose a normalized refinement horizon of 25/50/100/200 (default 50), with renewal at
floor(horizon/2)+1 remaining nodes. This is still an output bound, not an expansion budget;
whole-chunk leg lookahead remains up to horizon+1024. Native periodic checks can consume
multiple nodes between observations. Record the selected horizon with every comparison.
Native NeedNewPath remains authoritative: its 15-cell proximity test for an endpoint that
cannot reach the final destination can renew before the added threshold, notably at horizon
25. Never mask a native true result to force an exact halfway renewal. Added-policy telemetry
counts only observations of our threshold, not every renewed path.

Research downloaded saves only as read-only inputs under ignored research artifacts.
Do not rewrite their dimensions, strip missing gameplay mods, clone pawns or reconstruct
an invented colony and label it the original shared workload. If no matching save can be
verified, retain candidate/rejection evidence and leave the requested stress test pending.

## Migration Plan (unchanged)

Native dirty notifications also cover fog, reservations and other data not present in the
optimistic shared guide. Snapshot capture compares copied costs/corner flags to the previous
owned chunks. It acknowledges the native stamp but advances the geometry revision only for
actual changes; equal-geometry snapshot objects can lease the same ready guide by revision.
Native per-request grids still enforce the omitted permissions and preferences.

## All-request integration (2026-10-24, original adapter)

The shared-cohort optimization above supersedes the original prepared-grid profile key
and full-scan invalidation below. The native request and lifecycle contracts remain.

The user explicitly requests ordinary colony work and all other pathfinding to use the
experiment. Replace `PathFinderJob.Execute`, the common queued/synchronous search seam
on inspected 1.7.4985. Native request validation, destination rectangles/excluded touch
corners, cost-grid/customizer preparation, door permissions, provider costs, blocked-pawn
penalties, connectivity and movement costs remain authoritative. No drafted, faction,
area, destination-Thing or traversal-mode eligibility filter remains. Reachability/job
selection and world/caravan routing are not cell-path searches and are not replaced.

Reuse the existing 32-cell portal router as optimistic static guidance, keyed by the native
prepared-grid identity and invalidated with native gathered-data invalidation. Bound retained
grid profiles. Synchronize each shared router; workers use prepared native arrays and pure
managed state only, never live Pawn/Map objects. Exact request checks reject illegal edges,
including directional connectivity and door/fence diagonal rules. If guidance cannot be
refined legally, run an owned request-aware cell search to any legal destination cell;
record this as repair, not vanilla fallback. Cache repair progress only for the moving
follower and revalidate every returned prefix. Approximate routes may ignore finite dynamic
cost preferences in cached guidance; legality and reported path costs use actual request
costs. No optimality or speedup claim follows from broader coverage.

Only requests created by the native follower may return bounded prefixes. Other queued
callers and synchronous queries receive complete paths. Preserve native arrival, renewal,
retargeting and cancellation; mode switching cannot strand an existing partial route.
Unexpected adapter failures are explicit, counted emergency vanilla fallback, not normal
eligibility exclusions. Instrument actual worker execution and retain repair/fallback
coverage. Verify autonomous hauling to Things and cells through doors, plus full synchronous
queries as supporting diagnostics, before handing off a new paused 1000-square colony.
User playtest saves must be retained separately from disposable acceptance cleanup.

Install locally under the canonical package ID. Vanilla mode is the rollback and default. Disable/remove the mod to restore normal map creation and path selection; no save-owned custom world objects are introduced.
