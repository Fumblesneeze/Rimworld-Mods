## ADDED Requirements

**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Performance fixtures declare exact reproducible groups
Every performance fixture SHALL be a separately staged attributed type with a stable benchmark ID, owning package, complete ordered non-Gateway package sequence, deterministic seed, workload version, warm-up ticks, sample ticks, game speed, and repetition policy. DPA's package and Gateway SHALL be present in every launched group, while product and optional package order SHALL be exact and recorded.

#### Scenario: Base and all-supported benchmarks are selected
- **WHEN** the caller selects one base Immersive Chefs fixture and one all-supported fixture
- **THEN** the host launches one fresh process for each distinct exact sequence and records every declared workload parameter before sampling

### Requirement: Dubs Performance Analyzer is guarded and test-only
The runtime SHALL activate performance collection only when package `Dubwise.DubsPerformanceAnalyzer.steam` is active and assembly `PerformanceAnalyzer` satisfies the exact validated 1.6 reflection shape. The result SHALL record package version metadata, assembly identity, MVID, length, and SHA-256. No shipping repository mod SHALL reference or bundle DPA, and an absent, inactive, or incompatible analyzer SHALL fail only the performance run.

#### Scenario: DPA updates incompatibly
- **WHEN** its package is active but a required registration, recording, or result-store member differs in declaring type, signature, or accessibility
- **THEN** the benchmark records the precise failed shape and executes no fuzzy lookalike or private timing replacement
- **THEN** ordinary gameplay and E2E tests remain available without DPA

### Requirement: Relevant product methods are registered dynamically
Before recording, the runtime SHALL deterministically register all attached Harmony patch methods owned by each selected product's exact Harmony ID, every selected product tick/component method named by the performance contract, and every explicit benchmark-manifest method/type selector. It SHALL retain each resolved declaring type, method, signature, module, assembly, category, and selection reason, reject unsupported or unresolved selectors, and avoid whole-assembly profiling unless explicitly requested.

#### Scenario: A product adds a new Harmony postfix
- **WHEN** the reviewed build attaches that postfix under the product's Harmony owner and a benchmark group loads it
- **THEN** runtime discovery registers the exact postfix automatically and the report attributes its calls and time to that build

#### Scenario: A manifest selector becomes stale
- **WHEN** an explicitly named work-giver or component method no longer resolves exactly after a game/mod update
- **THEN** the benchmark fails before sampling rather than silently omitting the metric

### Requirement: Sampling uses DPA and ordinary game progression
The benchmark SHALL arrange its workload while paused, verify exact fixture counts, warm it through ordinary game frames/ticks, start DPA recording programmatically, run the declared sample through native game-speed progression, stop DPA recording, and collect a stable snapshot before cleanup. It MUST NOT call `DoSingleTick`, directly invoke profiled methods to inflate counts, or construct expected terminal metrics.

#### Scenario: A normal sample completes
- **WHEN** warm-up settles and the declared sample interval advances through the running game's normal tick manager
- **THEN** DPA reports measurements caused by the active pawns, jobs, buildings, components, world objects, and UI/update cycles in that interval

#### Scenario: Gameplay stalls
- **WHEN** expected meals, dish cycles, nursing, hauling, dining, pathing, or caravan activity does not reach its declared throughput checkpoint
- **THEN** the sample is invalid even if its recorded CPU time is low

### Requirement: Reports preserve direct and system-level metrics
Every sample SHALL retain raw DPA entries for registered product methods and relevant built-in tick, update, pawn/thing tick, work giver, think tree, pathing, and frame categories. It SHALL also record calls, total, average, maximum, category/update basis and available distributions, achieved TPS/FPS, elapsed ticks/wall time, GC/memory checkpoints, errors, exact workload counts, and instrumentation/control phase identity. Normalized summaries MUST preserve units and denominators and MUST NOT compare tick metrics to frame metrics as though they share one basis.

#### Scenario: A work giver regresses
- **WHEN** its average and total CPU time rise while calls and sample ticks are comparable
- **THEN** raw and normalized reports expose the exact work-giver entry alongside overall work-scan/TPS context

### Requirement: Immersive Chefs has a substantial versioned workload
The initial Immersive Chefs fixture SHALL build a deterministic multi-room, non-trivial-pathing colony with 36 human pawns across cooking, assistance, cleaning/hauling, nursing/patient, and dining roles; 24 animals; simultaneous recipe tiers; prep/support stations; domestic and industrial dishwashing; microwaves; separated storage; patients and nurses; and two caravans containing pawns, animals, meals, plates, and cutlery. Exact Defs, counts, layout version, jobs, and expected throughput SHALL be machine-verified before and after every sample. Optional groups SHALL activate the corresponding installed integrations without changing unrelated base identities.

#### Scenario: Base Immersive Chefs performance run
- **WHEN** Harmony, Core, Immersive Chefs, DPA, and Gateway form the exact active set
- **THEN** the workload continuously exercises cooking, assistance, dining, temperature, tableware, sanitation, washing, hauling, patient feeding, animal exclusion, and caravan state without requiring an optional mod

#### Scenario: Processor and Dubs group runs
- **WHEN** the exact supported Processor Framework and Dubs Bad Hygiene packages are added
- **THEN** the same workload uses connected water-consuming Processor dishwashers and the report includes both integration adapter and built-in system metrics

### Requirement: Exact optional-mod matrices are reusable
The host SHALL allow benchmark selection/filtering by benchmark ID and exact mod-group ID. Future repository mods SHALL be able to add performance fixtures and method selectors in separate test projects without editing Gateway runtime code. A group SHALL never inherit an optional package merely because it is downloaded locally.

#### Scenario: A future product adds a benchmark
- **WHEN** its marked performance assembly declares a new exact group and fixture
- **THEN** host discovery builds, groups, stages, runs, and reports it through the same command surface

### Requirement: Historical comparison is explicit and compatible
The runner SHALL emit raw JSON, normalized JSON, CSV, and a Markdown summary for every run and SHALL compare results only against a compatible accepted baseline by default. Compatibility SHALL include workload version, package order, game version, product/test/DPA identities, metric units, and relevant hardware/runtime fingerprint. Regression policies SHALL be tracked per selector with explicit relative and/or absolute thresholds. A missing metric, new runtime error, workload drift, or exceeded threshold SHALL fail comparison.

#### Scenario: A compatible regression exceeds policy
- **WHEN** a selected product method exceeds its reviewed relative or absolute threshold across the configured samples
- **THEN** the command exits nonzero and the report shows baseline, current samples, delta, threshold, and exact method identity

#### Scenario: A caller records a new baseline
- **WHEN** baseline creation is explicitly requested after a reviewed run
- **THEN** the runner writes a candidate without overwriting an accepted baseline and requires an ordinary repository review/commit to accept it

### Requirement: Performance commands are safe and automation-friendly
`scripts/Invoke-RimWorldPerformanceTests.ps1` SHALL provide discoverable filters and path/sample overrides plus table and JSON console output. Exit code `0` SHALL mean all samples, cleanup, and requested comparisons passed; `1` SHALL mean a sample/runtime/comparison failure; `2` SHALL mean invalid usage or discovery. Every launch SHALL use isolated savedata, muted background preferences, exact PID ownership, controlled shutdown, credential/stage cleanup, final log scanning, and unchanged normal configuration hashes.

#### Scenario: Periodic benchmark automation succeeds
- **WHEN** every selected group completes its declared repetitions without errors or threshold regressions
- **THEN** the command exits `0` and the durable aggregate links every group, sample, raw report, summary, and cleanup record

### Requirement: Performance workflow is documented separately from correctness
Repository documentation and the RimWorld development skill SHALL explain setup for DPA, benchmark discovery, exact matrix selection, raw versus normalized metrics, baseline review, limitations of live-game profiling, and how to add a future product fixture. Performance results MUST supplement rather than replace correctness/E2E acceptance.

#### Scenario: A mod update changes internals without breaking behavior
- **WHEN** ordinary tests and E2E workflows still pass but the performance comparison fails
- **THEN** the change remains behaviorally correct but is not performance-accepted until the regression is investigated or the policy/baseline change is explicitly reviewed
