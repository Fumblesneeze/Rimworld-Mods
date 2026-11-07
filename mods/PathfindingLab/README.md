# Pathfinding Lab

Local RimWorld 1.7 experiment: cached chunk portals plus rolling path refinement. Requires Harmony. Vanilla remains the default; this is not a production performance recommendation.

## Try it

Build/install with the repository `mod_build` operation and package ID `fumblesneeze.pathfindinglab`. Enable the local mod after Harmony. Select a colonist and use **Experimental paths**, or change the same option in the mod settings. The toggle is global.

Experimental mode routes ordinary jobs and synchronous queries through the common map-cell worker when their shared guide is ready. Cold, invalidated or busy guides use explicitly counted vanilla routing without waiting for background preparation. Eligible idle wildlife instead receives a short adjacent-cell walk before the search queue. Native jobs, collision handling and arrival remain authoritative. Only moving pawn followers receive partial hierarchical paths; other callers receive full routes. World routing and reachability remain native. Unexpected errors are distinct from expected readiness fallback. No Gateway dependency is packaged.

The settings also offer 500×500, 750×750 and 1000×1000 for the next player settlement. Existing maps and quest maps are not resized. These sizes can stall generation or exhaust memory. Disable the override to restore normal map generation.

## What is implemented

- 32×32 connected chunks, sampled boundary portals, approximate coarse A*.
- One eagerly prepared immutable graph per map, shared by player, factionless wildlife, allied, hostile and neutral requests. Eight reusable query workspaces own independent search state, native request adapters and 512-entry directed endpoint/movement-cost caches. Cohorts retain separate usage/fallback counters, not duplicate topology.
- Selectable 25/50/100/200 forward-step horizon (default 50); renewal is requested on the next native 30-tick path check once at most `floor(horizon/2)+1` path nodes remain. A changed setting affects subsequent segments. Local legs can extend beyond that output horizon but are confined to 1024 cells.
- Native dirty cells/rectangles refresh only affected chunks. Finite cost changes invalidate local paths without rebuilding connectivity; coarse plans depend on their own chunks, not a map-wide generation. Native pawn jobs, arrival, collision handling and final destinations remain authoritative.
- Request-aware validation of every emitted edge. Invalid guidance invokes the experiment's complete cell-search repair, retaining repair progress for subsequent movement prefixes. Guidance can ignore finite dynamic cost preferences; legal traversal and reported costs still use the actual request. This is not optimal routing.
- Ready-guide requests first try a complete local search capped at 256 expansions and radius 32; misses escalate to hierarchical guidance/repair. A full workspace pool selects vanilla without waiting; capacity exhaustion and graph-not-ready fallback are counted separately.
- Initial construction and incremental graph updates run on one coalescing background lane per map, even while paused and before any cohort requests a path. The main thread copies native geometry into owned immutable chunks. Publications share unchanged geometry/topology chunks; existing readers retain their previous publication safely. No background task retains native arrays or live Map/Pawn objects. Equal-cost/corner dirty notifications preserve readiness. Actual changes prevent new admissions until the latest revision is ready; disposal prevents late publication without waiting.
- Ordinary factionless ground-animal WanderAnywhere/WanderHerd jobs build 3–10 adjacent steps in a persistent direction, turning around obstacles and treating the native herd-root circle as a wall. Native wait cadence remains. Outside-radius regrouping, purposeful jobs, duty/pen providers, flying/swimming and water-seeking animals retain ordinary routing. Steps are revalidated before direct pooled-path handoff; blocked or loaded uncached idle paths fail/rethink. No global destination search or request grid is built for accepted idle walks.

Native renewal checks can run earlier than the added half-horizon threshold. In 1.7, being within 15 cells of a partial endpoint requests a new path; this can preempt our 13-node threshold at horizon 25. The mod does not suppress that native check.

The optimistic abstract costs can choose detours. Cache hits and walking ticks are not search-time measurements. Test-only Stopwatch instrumentation records all search workers and expected readiness dispatch separately. Shared native gathering/grid scheduling/grid-worker batches remain map-level; doors/providers are per pawn. Snapshot capture and idle destination/direct-path work have separate scopes. Background preparation is retained in unique `<probeId>-background.json` files, not hidden or added as a search-worker subset. Revision tickets retain their originating windows, queue timestamps, coalescing and pending-at-close state; execution durations exclude queue wait. Refresh and graph timings are subsets of background elapsed time. Remaining scheduler/result delivery, setup and walking remain excluded. Summed elapsed scopes can overlap and are neither CPU time nor a whole-game speedup or Circinus baseline.

First-movement latency is separately measured from native `StartPath` for an initially stationary pawn to its first successful movement-cost payment or cell transition. It excludes earlier UI/job-selection delay. Retargets, already-moving requests, cancellations and censored requests have explicit outcomes rather than fabricated zero latency. Raw detail is bounded, but aggregate counts, work and movement-latency count/sum/max survive overflow. Workers retain their originating window, with pending-at-close and late-completion counts. Cleanup writes diagnostic snapshots under the isolated savedata's `PathfindingLabMeasurements`, including failed runs; incomplete windows are not successful benchmark results.

## Verification

Use `test_run` with suite `PathfindingLab.Unit`. Native workflows are in `tests/PathfindingLab.EndToEndTests`: `pathfinding-lab.native-route-comparison` (open ABBA and active mode switching), `pathfinding-lab.barrier-route-comparison` (edit after cache warm-up), `pathfinding-lab.large-map` (native size override and movement), `pathfinding-lab.colony-timing-verification` (ordinary hauling), `pathfinding-lab.wildlife-wander` (herd movement and purposeful feeding), and `pathfinding-lab.background-readiness` (movement during held cold/invalidated builds, then ready-guide routing). Use typed `e2e_run_start`; size/horizon overrides use an isolated `prelaunchConfigDirectory`. Actual experimental handling must be observed; expected readiness fallback is attributed separately from emergency fallback. All automated game runs remain minimized and muted.

The owning contract is `openspec/changes/add-pathfinding-lab`. Retained run results and the eventual acceptance index are under ignored `artifacts/PathfindingLab`; task checkboxes distinguish delivered evidence from pending work. Seven locale catalogs are present; catalog parity alone does not establish rendered locale acceptance. The SVG in the local release profile is a non-shipping research label, not publication-ready Workshop art.

### Shared immutable graph (2026-10-25)

One eager map-wide builder now publishes immutable chunk data to eight reusable,
exclusive query workspaces. Cohorts share static optimistic guidance; native request
permissions, doors and dynamic costs remain authoritative. Old readers survive new
publications, while new requests use counted vanilla fallback until the replacement
is ready. Warm slot acquisition/release allocates no managed bytes in the focused
test. This is not an allocation-free routing claim: legacy guided searches still
allocate, and eight lazy repair buffers can retain 128 MB plus queues on a million-cell map.

Reviewed-build native readiness/invalidation and ordinary hauling passed. Four fresh,
minimized/muted V/E/E/V runs loaded the same immutable 1000×1000 pre-approach raid save:
21 colonists, 519 wildlife and 51 hostiles. Each observed approximately 12,000 ticks
(one repeat overshot by two ticks), including the
long walk to the colony. Experimental preparation completed while paused before unpause.

| Mean per run | Vanilla | Shared-graph experiment |
| --- | ---: | ---: |
| Observed-period recorded routing work | 85.188 s | 31.832 s |
| Including initial recorded preparation | 85.188 s | 33.941 s |
| Hostile search work, first ≈6,000 ticks | 0.956 s | 1.917 s |
| Hostile searches in that interval | 45.5 | 509 |
| Successful colonist movement onset | 24.64 ms | 21.69 ms |

These are summed Stopwatch scopes, not CPU time, TPS or whole-game elapsed speedup.
The colony-wide reduction is chiefly driven by the existing wildlife bypass; the fixed
50-cell horizon still loses on hostile approach work. Native grid preparation accounts
for 83.0% of experimental observed-period work. All worker/door coverage matched, with
no failed searches or emergency fallback. Both repeats reached eight concurrent slots;
each had only six expected graph/capacity fallbacks.

Relative to the preceding five-builder prototype, initial recorded preparation fell
4.403→2.110 s, but elapsed readiness increased 1.024→2.170 s. Observed-period work rose
6.0%; including preparation it fell only 1.4%. Independent native trajectories and changed
instrumentation prevent an isolated shared-graph speedup claim. The next targets are
native grid preparation, fewer long-route prefix requests, and chunk-local builder access.

Two independent reviews, 62 focused final tests, exact package identities, per-run scopes,
movement distributions, personally inspected action/result screenshots and cleanup are in
`artifacts/PathfindingLab/shared-graph-20261025.md` and
`shared-graph-results-20261025.json`. Original saves/configurations are unchanged and all
owned games exited gracefully. Raw per-call timing overflowed, aggregate totals remained
complete, and known beta shutdown allocation warnings remain unattributed. This is not
release approval or evidence of long-session stability.

### Complete-timer follow-up (2026-10-24, before shared graph)

Door/provider jobs are now wrapped at both native dispatch sites. Each observation window
requires exact per-pawn worker/preparation counts; missing or failed preparation cannot become
a zero-cost result. All 2,370 global searches in four fresh V/E/E/V colony runs had matching
door samples. The immutable input remains 1000×1000, 21 colonists, 519 wildlife and one tame fox.

| Metric | Vanilla | Experimental |
|---|---:|---:|
| Global searches / observed ticks | 2039 / 1939 | 331 / 1954 |
| Recorded routing-related elapsed work per 1000 ticks, including background | 7468.01 ms | 3526.57 ms |
| Mean observed first movement, all pawns | 33.17 ms | 16.34 ms |
| Mean observed first movement, colonists | 33.94 ms | 30.57 ms |

This is 52.8% less recorded work per simulated tick, not a whole-game speedup or an isolated
effect of the latest allocation fix. Scopes overlap across threads; remaining scheduler/result
delivery overhead is excluded. Independent trajectories and changed wandering alter the mix.
Two short processes per mode do not establish steady-state throughput or long-session stability.

The allocation fix delays dirty-chunk payload copying until the first actual difference:
the unchanged million-cell host regression fell from 5,300,296 to 8,264 allocated bytes.
Native grid workers still account for 45.2% of experimental recorded work and background
preparation 39.7%, overwhelmingly initial construction. Of 273 experimental searches, 254
used the bounded local fast path; only one portal-cache hit was observed. This workload
does not yet establish the value of caching frequent long routes. Native held-build movement,
invalidation, ordinary hauling through a door and synchronous timing passed reviewed-build checks.

Raw scopes, distributions, exact identities, reviews and personally inspected native observations:
`artifacts/PathfindingLab/dispatch-timing-20261024.md` and
`dispatch-timing-results-20261024.md`/`.json`. The earlier incomplete-timer results below remain
historical. Beta shutdown allocation warnings remain unattributed.

### Background/wildlife results before timer repair (2026-10-24, historical)

Two visually accepted fresh processes per mode loaded the immutable 1000×1000 user colony:
21 colonists, 519 wildlife and one tame fox. Native autonomous work, three short windows per
process, no graph prewarming. Native herd/feeding and held-builder movement also passed
separate reviewed-build player-workflow checks.

| Metric | Vanilla | Experimental |
|---|---:|---:|
| Global searches / observed ticks | 2023 / 1933 | 342 / 1948 |
| Direct idle path handoffs | 0 | 1472 |
| Common recorded elapsed scopes per1000 ticks, including background | 7057.50 ms | 3439.82 ms |
| Mean observed first movement, all pawns | 33.85 ms | 15.11 ms |
| Mean observed first movement, colonists | 36.34 ms | 27.46 ms |

The common scopes exclude doors/providers in **both** modes: one experimental run missed
that sub-timer (0/174 requests). This unresolved coverage gap prevents a full-pathfinding-time
claim. Common measured work decreased51.3% per simulated tick and search frequency83.2%,
but elapsed scopes overlap across threads and are not CPU time or a whole-game speedup.
Independent trajectories and altered wandering change the request mix. The first vanilla
run had black captures and was excluded/replaced; accepted order was E/E/V/V, not ABBA.

Native grid workers (~45%) and background work (~42%) dominate the experimental common
scopes. Each active cohort created its graph once, then applied incremental updates. Cold
builds still cost approximately0.66–0.76s off the request critical path. Colonist worker p95
increased7.13→12.94ms despite improved movement onset: long-route tails remain a target.
No failed worker/emergency fallback or pending/overflowed windows occurred. These short
samples do not establish steady-state throughput or long-session memory stability.

Raw identities, scope totals, native observations, reviews and safety checks are retained in
`artifacts/PathfindingLab/async-wander-20261024.md` and
`async-wander-results-20261024.md`/`.json`. All games closed gracefully; the permanent save
and normal configuration hashes are unchanged. The canonical nine-file package contains no
Gateway or test collector. Beta shutdown allocation warnings remain unattributed.

### Shared-cohort results before background/wildlife changes (2026-10-24, historical)

Two fresh runs per mode loaded the same checksum-verified user colony: 1000×1000, 21 colonists, 519 active wildlife and one tame fox. Native autonomous work produced 2,037 vanilla and 2,046 experimental searches across three short observation windows per run. No failed search, repair, emergency fallback, pending work or detail overflow occurred in these colony comparisons.

| Mean per request | Vanilla | Shared-cohort experiment |
|---|---:|---:|
| Inclusive search worker | 3.42 ms | 1.67 ms |
| Recorded worker + native preparation scopes | 7.02 ms | 5.27 ms |
| Observed first-movement latency, all pawns | 31.36 ms | 33.59 ms |

Each active cohort built once per process; 98.6% of requests completed through the local fast path. This reduces mean worker time about 51% against fresh vanilla and 79% against the earlier prototype's 8.06 ms. It does **not** establish a whole-game speedup: durations overlap across threads, trajectories differ, and approximately 16 simulated seconds per run is not steady-state testing. Initial graph construction still causes 0.7–0.9-second movement-onset outliers, including when a cohort is first needed in a later window. Player-only mean first movement is 40.22 ms vanilla versus 54.79 ms experimental.

The next measured targets are initial graph construction and retained native grid preparation, not horizon tuning or lock contention. Detailed raw-file identities, per-run/window numbers, review and native workflow evidence live under `artifacts/PathfindingLab/cohort-optimization-20261024.md` and `cohort-results-20261024.md`/`.json`. The permanent read-only save is outside disposable artifacts; every run loads a verified private copy. These local reports are evidence, not shipped mod content. Earlier sections below are historical and do not describe the current implementation.

### Colony collector follow-up (2026-10-24, historical)

The following results predate the all-request worker replacement. They do not validate or measure the current algorithm.

The reviewed collector passed a three-pawn ordinary-hauling workflow on a 250×250 empty fixture. Personally inspected captures show the three pawns picking up distinct steel/wood/granite cargo and delivering all three stacks to the stockpile. The run recorded eight successful vanilla searches and eight declined experimental attempts, with no overflow or outstanding workers. This verifies all-pawn instrumentation, not experimental ordinary-job support or a megabase speedup. Cold recorded work was 914.8704 ms: 906.1174 ms preparation and 8.7530 ms other recorded work. Movement-onset latency was retained separately; it is not pathfinding execution time.

Native ABBA runs at horizons 25 and 100 each completed eight 189-cell crossings plus switching to vanilla during an active order. Personally inspected movement/arrival captures and counters establish zero measured-pawn fallback. Experimental requests per crossing dropped from 15 to 3. Final experimental outward/return recorded totals were 8.5355/7.0660 ms at 25 and 21.7655/31.4675 ms at 100; preparation subsets were 6.7073/5.4686 and 20.2151/29.7934 ms. Final vanilla outward/return totals in those respective runs were 5.1155/9.7112 and 12.3480/10.9728 ms. These are different generated maps with evolving costs and cold-first-use effects, not a controlled horizon-speedup study. The evidence index retains every crossing's preparation, exclusive work and first-movement latency in order. Both games stayed minimized/muted, shut down gracefully and preserved normal configuration hashes.

Public-save research did not establish a suitable working vanilla megabase. The downloadable [TheVillageGuy benchmark](https://www.reddit.com/r/RimWorld/comments/wgu1qp/rimworld_benchmark/) is actually a Core-only **1.1, 400×400, 10,001-Human synthetic** save. A downloadable 1.7 repository fixture is only 250×250 and requires custom mods. Other large-colony posts did not yield a verified suitable download. Originals and provenance are retained under `artifacts/Research/SharedColonySaves-20261024`; none was loaded into normal saves, enlarged, stripped or repopulated.

The stress comparison remains pending both a suitable working-colony input and broader experimental job routing. `artifacts/PathfindingLab/colony-timing-evidence.md` records TDD, review, build identities, native observations and limitations. The beta still emits unattributed persistent-allocation shutdown warnings; this is not release sign-off.

### Earlier search timing experiment (1.7.4985 rev1269)

The historical table below predates the all-pawn/native-preparation collector and configurable horizon. Do not compare its narrower totals directly with new collector results or treat its live evidence as acceptance of the newer build.

The timing collector is test-only. Native drafted **Go here** orders drive both algorithms; results retain each actual search call and sum every partial-refinement call needed for a complete journey. Vanilla is measured inside `PathFinderJob.Execute`; the experiment is measured inside `PathRuntime.TryResolve`, including its snapshot/refresh/graph preparation. The latter preparation is reported as a subset, not added twice. Vanilla's shared cost-grid jobs and scheduling are outside the measured scope.

The final fixture fills needs and removes hediffs before each journey, outside timing, to prevent unrelated mental breaks from contaminating long runs. It never changes pawn position or resets state during a journey. A journey must finish still drafted at its requested destination, with successful non-truncated samples and no experimental fallback. Sampling closes at arrival, before subsequent camera/UI steps.

After the user observed mechanoids downing the naked test pawn on a generated 750 map, v3 adds combat isolation: setup removes non-player pawns/held occupants and hostile/spawner objects, temporarily suppresses storyteller incidents and automatic wildlife entry, and equips ordinary normal-quality cloth clothing. Damage stays enabled; any injury, downing, death or job interruption rejects the trip immediately. Original controls and the test-owned spawn patch are restored on cleanup. This is an isolated pathfinding workload, not an ordinary-colony survival test. Earlier v2 results remain separate.

500/750 maps use ABBA round trips: four full crossings per algorithm. At 1000, the bounded protocol uses four alternating full crossings, **V outbound, E inbound, E outbound, V inbound**. Both algorithms cover both directions, but the second experimental trip is subsequent graph use, not an identical directed-route cache trial. Large runs use native Superfast equally for both modes. Live generated maps retain changing plants and original player pawns outside the prepared corridor; different seeds are not a controlled map-size scaling study. First use includes cold initialization; these are not JIT-stabilized microbenchmarks.

The completed runs below use product SHA-256 `E7910DE57F5A0FC99F60C6FA94E3B9756DEC15A47E2582B13622146A4F3603E4`, RimWorld 1.7.4985 rev1269, an AMD Ryzen 5 3600 and approximately 80 GiB system RAM. Values are **milliseconds of measured search work summed over a complete journey**, in chronological order within each mode, not walking duration. Experimental preparation is already included in the experimental total.

| Map / scenario | Vanilla ms | Experimental ms | Preparation subset ms | Experimental calls per journey |
| --- | --- | --- | --- | --- |
| 500×500 open/repeated, v3 | 30.0967, 4.0785, 16.9620, 7.7991 | 181.2114, 61.7609, 27.7307, 42.7667 | 133.3821, 25.8603, 23.6578, 37.4646 | 17 each |
| 750×750 before barrier, v3 | 103.8031, 102.8368 | 825.3496, 418.5699 | 743.7120, 396.8564 | 27 each |
| 750×750 after barrier, v3 | 36.6810, 35.9499 | 389.3853, 354.5360 | 376.9805, 340.8180 | 28 each |
| 1000×1000 open, earlier v2 | 217.4063, 115.0873 | 1173.7275, 836.8927 | 1059.4673, 810.9707 | 37, 38 |

Every measured vanilla journey required one worker search. The first experimental journey in each run starts cold. The second experimental outbound/inbound pair at 500 repeats the same directed routes; the second pair at 750 follows insertion of a 51-cell wall across the central corridor, with a cleared detour area. No measured pawn fell back to vanilla. The 500 run's global fallback counter includes other pawns; per-pawn counters and the timing collector exclude those requests. These are small, sequential live workloads, not confidence intervals or an identical-weight algorithm comparison: recorded movement costs and map contents changed over simulated time.

The 500 and 750 maps completed all eight journeys on the reviewed v3 fixture, spanning 439 and 689 cells respectively. I personally inspected native order lines, mode toggles, healthy drafted arrivals in both modes, repeated/post-edit arrivals and the cleanup view. The barrier itself is not visible in the endpoint captures, so they do not establish detailed route optimality. Peak process working sets were **2.85 GiB** and **4.45 GiB**. Retained grouped runs are `20261022T080414803Z` (500) and `20261021T235434955Z` (750).

`artifacts/PathfindingLab/stopwatch-evidence.json` retains build identities, raw per-query results, reviews, failed attempts and observations. Earlier v1 measurements with needs drift and the earlier v2 1000 run are kept separate; do not pool them as one controlled scaling series.

The 1000×1000 run generated natively and completed four 939-cell crossings. Its measured sequence was vanilla **217.4063 ms**, experimental **1173.7275 ms** (37 calls), experimental **836.8927 ms** (38 calls), vanilla **115.0873 ms**. Experimental preparation accounted for **1059.4673 / 810.9707 ms** of those totals. Both vanilla journeys needed one measured worker call. No measured-pawn fallback occurred. Peak process working set was **6.74 GiB**. Movement costs changed during later trips, so these are observed workload totals, not a controlled algorithmic speedup.

That run (`20261021T225442225Z`) passed automated movement and cleanup assertions and shut down gracefully. **1000×1000 visual acceptance remains incomplete:** the inspected captures were black, and the beta screenshot API logged that no rendered frame was readable. Black PNGs do not prove the native workflow visually. Autosave emitted discarded/missing saved-object reference warnings in the 750/1000 fixtures; save/load correctness and ordinary-colony playability are not established. Native persistent-allocation-at-shutdown warnings also remain unattributed; this is not a clean-log or release-ready claim.

All three completed runs used isolated savedata, remained minimized/muted, closed gracefully without forced termination, cleaned test staging/credentials and left normal ModsConfig/Prefs hashes unchanged. The final canonical build installed the same product DLL as a nine-file allowlisted local package, without the test collector or Gateway. Formal large-map acceptance tasks remain unchecked because of the 1000 capture gap and unresolved warnings.

The prototype is currently slower in these workloads. It treats changed soft costs like connectivity changes: it rebuilds affected chunk components/boundaries and increments one map-wide generation. A generation change discards even a coarse plan whose route is unaffected. At 500, 68 experimental calls caused 65 abstract searches/generations; at 750, 110 calls caused 110; on the million-cell run, 75 calls caused 74. Preparation accounts for roughly 96% of experimental time on the post-barrier 750 trips. The intended persistent coarse-route benefit was therefore largely lost. The next optimization target would be separate topology/cost invalidation and route-local dependencies, before drawing conclusions about hierarchical routing itself.

Rendered locale coverage and the placeholder gizmo icon remain unfinished. No publication is approved.
