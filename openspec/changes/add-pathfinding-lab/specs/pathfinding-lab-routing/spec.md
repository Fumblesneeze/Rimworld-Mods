## ADDED Requirements

### Requirement: Opt-in partial hierarchical routing
Pathfinding Lab (`fumblesneeze.pathfindinglab`) SHALL own a 1.7-only experimental mode that caches connected portal geometry, returns no more than the selected 25/50/100/200 forward steps per native follower request (default 50), and retains the native final destination. Guided local refinement SHALL search at most one 32×32 chunk per leg; dynamic repair MAY search the full request grid. The horizon is an output bound, not an expansion budget. It SHALL default to vanilla and renew partial paths on the next native periodic path check at or below floor(horizon/2)+1 remaining nodes. All other queued and synchronous callers SHALL receive full experimental paths. The native check runs every 30 ticks, so this is a lookahead threshold, not an exact node-consumption interrupt. A changed horizon applies to subsequently refined segments.

#### Scenario: Long native movement
- **WHEN** a player enables experimental mode and orders an eligible drafted colonist to a distant OnCell destination
- **THEN** the pawn traverses successively refined segments and arrives at the original destination through the native path follower.

#### Scenario: All native cell-path requests
- **WHEN** experimental mode is enabled for any queued or synchronous cell-path request, including undrafted work, Thing targets, allowed areas, animals, hostiles, special traversal and custom tuning
- **THEN** the common search worker uses the experimental router with native prepared cost, connectivity and destination data when the requested cohort guide is ready; unavailable or rebuilding guides use separately counted vanilla readiness fallback without blocking. Admitted idle-wildlife walks bypass search entirely.
- **AND** only native follower requests may return partial paths; all other callers receive a complete route or not-found result. Reachability and world-map routing remain native.
- **AND** invalid portal guidance triggers owned request-aware repair; exceptional adapter failure is counted and reported separately from repair and successful hierarchical coverage.

#### Scenario: Ordinary work through a door
- **WHEN** undrafted colonists autonomously haul distinct items through a permitted door to a stockpile in experimental mode
- **THEN** native jobs pick up and deliver those items using experimental searches, with no eligibility fallback and no illegal blocked-cell or touch-corner traversal.

#### Scenario: User-owned oversized playtest
- **WHEN** the user requests a new 1000-square colony for building
- **THEN** create a fresh native generated map with the experimental algorithm enabled, retain its save and configuration independently of disposable acceptance runs, and hand off the paused minimized game without modifying normal saves or configuration.

#### Scenario: Return to vanilla
- **WHEN** the player switches to vanilla during or between orders
- **THEN** subsequent requests use vanilla and existing partial movement safely renews without losing the final destination.

### Requirement: Connected and bounded reusable caches
The mod SHALL preserve disconnected components within chunks, validate legal adjacent steps, use bounded directed local-path caches keyed by movement costs, and invalidate changed geometry before reusing paths. Coarse routes MAY be approximate and SHALL NOT be presented as optimal.

#### Scenario: Mountain barrier and disconnected regions
- **WHEN** a route crosses chunks containing a mountain barrier with one reachable gap
- **THEN** the refined path crosses the gap without crossing blocked cells or connecting disconnected chunk components.

#### Scenario: Warm reuse
- **WHEN** comparable pawns repeat routes across unchanged chunks
- **THEN** cached local portal paths are reused without repeating their local search.

#### Scenario: Persistent cohort hierarchy and native permissions
- **WHEN** different player pawns, factionless wildlife, allies, hostiles or neutral pawns request paths
- **THEN** all cohorts reuse one eagerly prepared immutable map graph; cohort identity remains usage accounting, while private query workspaces have bounded map-wide ownership without per-pawn graph copies.
- **AND** shared guidance never imports another pawn's permissions or customized grid; actual request data validates every returned edge and owns repair and reported cost.

#### Scenario: Incremental refresh and dependent plans
- **WHEN** native gathered static data changes a bounded region
- **THEN** only affected chunk snapshots are refreshed, without consuming native dirty queues; full reads are reserved for first use and explicit full invalidation.
- **AND** finite traversable cost edits invalidate local costs without rebuilding unchanged connectivity, while topology changes rebuild relevant components/boundaries; unrelated chunk edits preserve existing coarse plans.

#### Scenario: Bounded local work without global preparation
- **WHEN** a nearby request has a ready shared guide and a complete legal route within the local search radius and expansion budget
- **THEN** after a nonblocking guide lease it uses that route without building/refreshing the hierarchy; partial follower output is permitted only after proving the full local route. An unavailable lease uses counted native readiness fallback.
- **AND** budget exhaustion or request restrictions escalate safely to shared guidance/repair rather than returning a false not-found or a speculative dead-end prefix.

#### Scenario: Local map edit
- **WHEN** terrain or a building changes a used route
- **THEN** affected chunks and coarse plans are invalidated and the pawn takes a valid new route or falls back to vanilla.

#### Scenario: Nonblocking graph readiness
- **WHEN** the shared map graph is not ready, invalidated or rebuilding, or all query workspaces are leased
- **THEN** native requests use vanilla immediately while one background lane builds/updates from owned immutable snapshots; no native array or live map/pawn is read by that lane.
- **AND** newer revisions coalesce without publishing stale state, failures remain observable without retry storms, and map disposal prevents late publication without waiting.
- **AND** native dirty notifications that leave the owned cost/corner geometry unchanged are acknowledged without invalidating the ready guide; actual cost or corner changes advance the geometry revision.
- **AND** an unchanged dirty chunk reuses its owned payload without allocating replacement cell arrays. On the first observed difference, copy-on-write publishes a new payload without modifying prior snapshots or rereading native cells.
- **AND** background construction and snapshot-copy costs remain measured separately from search-worker time and expected readiness fallback is not labelled a failed experimental search.

#### Scenario: Preparation before demand
- **WHEN** a map becomes playable with experimental mode enabled, or the player enables it on a loaded map
- **THEN** its next map update captures owned geometry and schedules every routing cohort, including cohorts with no current pawns or requests, even while paused.
- **AND** native cell, rectangle and full-dirty notifications initiate incremental preparation on subsequent updates without waiting for a path request; unchanged geometry reuses prepared state and unchanged updates allocate no snapshot payload.
- **AND** disposal prevents further preparation, and requests during outstanding work retain nonblocking counted vanilla fallback.

#### Scenario: Timed edge-to-colony raid approach
- **WHEN** the corrected raid comparison is run
- **THEN** its immutable input contains four native edge-walk-in immediate-attack raids saved before their approach, and both modes load that exact input.
- **AND** experimental preparation is timed separately while paused, all five cohorts must be ready before native unpause, and no synthetic path is issued to warm them.
- **AND** measurement includes the first unpause and targets 12000 total ticks through cumulative native scheduled-pause boundaries, retaining actual frame-observed tick overshoot and normalizing by the actual duration, plus initial and subsequent hostile distances, search/renewal/repair/readiness counts and actor outcomes. Preparation ID -1 and gameplay IDs 0..5 remain consistent across exports and background tickets. Prior post-approach combat measurements are not pooled as approach evidence.

### Requirement: Reusable contiguous search storage
Pathfinding Lab SHALL reuse bounded map-owned query workspaces under exclusive per-workspace leases, accept borrowed span destination inputs, and expose borrowed memory path slices with documented invalidation lifetimes. Immutable published graph/geometry SHALL be shared by readers and remain safe during background updates. Search bounds, native permissions and complete-route proofs SHALL remain unchanged.

#### Scenario: Shared graph with independent concurrent requests
- **WHEN** several pawns, including members of the same cohort, request paths against ready geometry
- **THEN** they share one immutable map graph but hold separate reusable query scratch and native adapters; no request inherits another pawn's customized costs, permissions or output.
- **AND** a bounded eight-workspace pool limits retained query storage; exhaustion selects counted native fallback without waiting, separately from graph-not-ready fallback.
- **AND** warm acquire/release allocates no managed memory and moving cursors remain usable after changing workers.
- **AND** graph-unavailable and capacity-exhausted fallbacks are recorded both map-wide and per cohort; live telemetry retains actual peak concurrent leases.

#### Scenario: Immutable publication and reader lifetime
- **WHEN** a map edit is prepared while an older graph has an active reader
- **THEN** the reader's geometry, components and portal data remain unchanged; publication reuses unchanged chunks and replaces affected owned chunks only.
- **AND** one map-owned background lane prepares the graph for all five cohorts, including absent cohorts, without waiting for a path request; stale/disposed builds cannot become available to new readers.
- **AND** new requests during outstanding preparation use native fallback, while existing readers may finish safely and return their workspaces.

#### Scenario: Repeated local requests
- **WHEN** warmed requests fit existing local node, queue and path capacities
- **THEN** the core search reuses contiguous storage without per-request managed allocation, copies or whole-map clearing; native result transfer remains outside this core allocation claim.
- **AND** changing goals, directed edges, costs or map positions does not reuse stale search state.

#### Scenario: Rolling directed repair
- **WHEN** a follower resumes a cached repair route
- **THEN** the complete remaining tail is validated in place and the emitted prefix is a memory slice, without allocating or copying the remaining route on each renewal.
- **AND** another cursor's search cannot mutate the retained repair; callers needing an independently retained result explicitly copy the borrowed output.

#### Scenario: Background component construction
- **WHEN** the guide flood-fills connected chunks
- **THEN** it reuses a bounded contiguous FIFO and stack neighbor scratch instead of allocating an iterator for every expanded cell, while preserving component and diagonal legality.

### Requirement: Directional idle wildlife wandering
The experimental mode SHALL replace only ordinary factionless animal WanderAnywhere/WanderHerd destination selection with a bounded directional walk of 3–10 legal adjacent steps, preserving native wait/job/follower behavior. Obstruction may shorten a walk or cause a native wait. Purposeful or specialized jobs SHALL keep ordinary routing.

#### Scenario: Local idle walk without path search
- **WHEN** an eligible wild animal begins an ordinary idle wander
- **THEN** it picks bounded adjacent steps in a persistent generally forward direction and receives that validated path without either global pathfinder or prepared request grid.
- **AND** repeated blocked directions turn away; forbidden/hazardous cells, door/fence barriers, invalid diagonal corners and destination reservations remain respected; a changed path fails/rethinks rather than escalating idle wandering to a global search.

#### Scenario: Herd boundary and regrouping
- **WHEN** the native herd provider supplies a reachable herd-mate root and the animal is within its configured radius
- **THEN** steps outside that circle act as blocked cells and the directional walker turns inward.
- **AND** an animal already outside that circle retains native regrouping; fleeing, eating, hunting, map exit, duty/pen wandering and unsupported locomotion do not use the idle shortcut.

### Requirement: Explicit oversized settlement maps
The mod SHALL expose warned, opt-in next-settlement map sizes of 500, 750 and 1000 cells per side, preserve vanilla by default, leave existing and quest maps unchanged, and make no crash-free or general compatibility claim.

#### Scenario: Native settlement generation
- **WHEN** a player selects an experimental size and creates a settlement through normal game initialization
- **THEN** the requested square size reaches the native settlement generator and the resulting map, if viable, supports native pawn movement.

### Requirement: Reproducible comparison and independent package
The product SHALL require Harmony but SHALL NOT depend on or ship the Dev Gateway. Mode and size controls SHALL be translated into English, German, Spanish, French, Simplified Chinese, Russian and Japanese. Comparisons SHALL retain exact build identity, ordered mods, native actions, observed path quality, cold/warm distinction, screenshots, resource outcomes and cleanup. At the user's explicit request, this experiment SHALL collect direct Stopwatch timestamps around actual searches during native orders. These diagnostic measurements are separate from canonical Circinus baselines.

#### Scenario: Direct search timing
- **WHEN** a selected pawn receives a native movement order in either mode
- **THEN** test-only instrumentation times vanilla's worker `PathFinderJob.Execute` (not queue latency) and the experimental request handler, associates samples with that pawn, and retains raw elapsed milliseconds, counts and total per completed journey.
- **AND** experimental snapshot copying and background graph preparation are reported in separate scopes, not included in search-worker time; vanilla shared cost-grid construction and scheduling are likewise separate from the worker-search measurement. Background revision tickets preserve originating windows, queue/coalescing state and execution duration. Timings are elapsed work duration, not thread CPU cycles or walking ticks.
- **AND** zero samples, failed searches and truncated sample buffers cannot be reported as successful zero-cost routes. No test invokes search methods in a synthetic loop or ships instrumentation in the product.

#### Scenario: Reviewed-build A/B run
- **WHEN** vanilla and experimental runs are compared
- **THEN** identical scenario inputs and native orders are exercised on the reviewed build, correctness and route quality are reported separately from performance, and unavailable profiling or untested large sizes remain explicitly incomplete.

#### Scenario: Colony-wide timing and first movement
- **WHEN** a test opens an observation window
- **THEN** every pawn's search on the selected map is recorded, including autonomous jobs, synchronous queries, unsuccessful experimental attempts and vanilla fallback; samples identify the pawn and execution source. Shared native preparation is recorded once at map level, never charged to each pawn.
- **AND** the report separates inclusive recorded pathfinding work, its preparation subset, and search/refinement exclusive of preparation. Native preparation includes map-data gathering, grid scheduling, actual grid-worker batches and door/provider work. Queue waiting, remaining scheduler/result-delivery overhead, ordinary walking, fixture setup and screenshots are excluded and named; summed work durations are neither wall time nor thread CPU time.
- **AND** first-movement latency starts at native StartPath entry for an initially stationary pawn and ends on the first successful native path-follower tick that pays movement cost or enters a cell. Already-moving calls, cancelled/superseded calls, exceptions and window-end censoring remain explicit outcomes with null latency when movement is not observed. Order acceptance latency before StartPath is not included. Paused UI/screenshot steps are not inserted between StartPath and first movement.
- **AND** raw detail is bounded with explicit overflow, but aggregate counts and timings include every completion. Queued workers retain the originating window; pending work and late completions are reported instead of migrating into another window.
- **AND** movement onset is correlated to the same StartPath generation, including when a single native tick finishes one job leg and starts another. Cleanup retains window snapshots on failure/cancellation as well as success, with unresolved workers and censored movements explicit; a partial snapshot cannot establish a successful benchmark.

#### Scenario: Adjustable refinement horizon
- **WHEN** an experiment selects 25, 50, 100 or 200 cells as the output horizon
- **THEN** each newly refined segment obeys that horizon and renewal is requested at half that many remaining forward steps, rounded down, on the existing 30-tick check. The default remains 50; invalid settings revert to 50. Changing the horizon does not resize the map or change the destination.
- **AND** native renewal conditions remain authoritative and may request a new segment earlier. In particular, 1.7's 15-cell proximity to a partial endpoint can preempt the added 13-node threshold at horizon 25; the added-policy counter alone is not a count of all renewals.

#### Scenario: Shared colony workload provenance
- **WHEN** a publicly shared save is considered for a large working-colony stress test
- **THEN** its author/source, download availability, hash, game version, actual map dimensions, pawn population and content dependencies are inspected before acceptance. Modded, synthetic, smaller or unavailable candidates are labelled honestly, not presented as a verified vanilla 1000-square hundred-pawn colony.
- **AND** ordinary-job experimental coverage remains separately reported. The later all-request change explicitly broadens the original drafted-OnCell contract; a fallback-dominated run cannot establish experimental colony-routing speedup.

#### Scenario: Reusable user-supplied colony
- **WHEN** the user supplies their medium-size-colony save for experiments
- **THEN** a checksum-verified read-only master and provenance are retained outside disposable run artifacts. Each fresh experiment validates the master identity, refuses to overwrite any destination save, loads a private copy natively, and leaves the original and user's current game untouched.
- **AND** native unpause advances the saved colony's real autonomous jobs without changing needs, populations, geography or random state. All map searches, including wildlife, are measured; loaded population, jobs, positions, health, native work records, exact build and save identity accompany cold/warm windows and first-movement outcomes.
- **AND** fresh vanilla/experimental runs use the same source save but retain independent trajectories and failures. Screenshot pauses occur outside measurement windows. Preparation subsets are not added twice; pending workers, overflow, fallback and incomplete runs remain explicit. Findings distinguish measured hotspots from untested optimization hypotheses.

#### Scenario: Raid-stress snapshot comparison
- **WHEN** the user requests multiple raids against the preserved colony
- **THEN** a separate minimized setup process loads a verified private copy, triggers four native enemy raids, observes their approach, and saves a temporary non-overwriting snapshot without touching the permanent master.
- **AND** fresh vanilla-disabled/experimental-enabled processes load that same verified snapshot, record all-pawn and hostile timing/preparation/first-movement evidence, and visibly advance native attacks through ordinary time controls.
- **AND** casualties and destroyed buildings remain measured outcomes, not reasons to discard an otherwise complete run; native random state and combat are not normalized. Pending, failed, overflowed or fallback evidence remains explicit.

#### Scenario: No hidden fallback in the controlled comparison
- **WHEN** an experimental trip is accepted in the clear-ground fixture
- **THEN** per-pawn monotonic counters prove actual experimental handling with zero emergency fallback; any cold/invalidated/busy-guide vanilla searches are explicitly matched to readiness dispatch, without counting unrelated animals or colonists. Warm partial trips prove sufficient bounded requests to cover the distance; an all-native run cannot establish experimental coverage.
- **AND** native switching during an active partial order preserves its destination without issuing a replacement order; fixture terrain, roofs and fog are restored during cleanup.

#### Scenario: Combat-free benchmark preconditions
- **WHEN** a native generated map is prepared for a timed comparison
- **THEN** test-only arrangement removes non-player pawns, including held occupants, hostile buildings and identified pawn/hive spawners from that disposable map; ordinary terrain, neutral geometry and vegetation remain outside the routing fixture.
- **AND** storyteller/queued incidents and automatic wildlife entry are suppressed only for that test, with original controls and the test-owned Harmony patch restored in cleanup. Damage is not disabled, and no pawn is teleported, healed or revived during a journey.
- **AND** a fixed normal-quality cloth outfit provides ordinary thermal protection; measurements retain actual movement costs. Injury, downing, death, loss of draft or mental-state interruption fails the journey immediately instead of waiting for arrival or accepting its timing.
- **AND** failure evidence and prior unisolated measurements remain separate; a failed or unobserved journey is never promoted to a successful timing result.

### Requirement: Complete native preparation diagnostic coverage
The Pathfinding Lab test-only collector SHALL retain one door/provider execution sample per actual global worker, per pawn and originating window, in both routing modes. Missing, duplicate, failed or pending preparation SHALL invalidate a complete comparison rather than count as zero. Direct idle paths SHALL not require a global preparation sample.

#### Scenario: Late-installed instrumentation after native save loading
- **WHEN** a measurement probe is installed after a saved colony has exercised native pathfinding
- **THEN** queued and synchronous door/provider jobs are captured at their native dispatch sites and their actual execution is timed without changing scheduling dependencies, exceptions or disposal.
- **AND** each window exports coverage alongside raw evidence and refuses comparison acceptance when preparation and worker counts disagree, even when aggregate totals happen to match across different pawns.
- **AND** yielded native pause actions execute before closing the window; routing-counter snapshots are frozen after originating workers drain, so later runner advances cannot silently enter the count comparison.
- **AND** ordinary multi-pawn acceptance may prepare its guide while paused before enabling native hauling, but supporting queries never count as gameplay evidence. Every hauler's worker, door and readiness counts must agree; the workload must contain successful experimental handling, while individual haulers may legitimately encounter only busy/invalidated-guide fallback. The cold/invalidated held-builder workflow remains separate.
