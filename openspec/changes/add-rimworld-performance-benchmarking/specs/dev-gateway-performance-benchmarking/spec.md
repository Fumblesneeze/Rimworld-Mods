## ADDED Requirements

**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Performance fixtures declare comparable representative groups
Every performance fixture SHALL be a separately staged attributed type with a stable benchmark ID, comparison ID, staging-owner package, measured-subject package, complete ordered non-Gateway package sequence, workload version, warm-up ticks, sample ticks, game speed, repetition policy, and evidence lens. Every canonical sequence SHALL begin exactly `brrainz.harmony`, `ludeon.rimworld`, `astryl.Circinus`; product and optional packages follow in their declared order, and Gateway is appended last by the host. Dubs Performance Analyzer SHALL NOT be present in a canonical Circinus benchmark group. The explicitly noncanonical DPA diagnostic command is exempt from the Circinus requirement and SHALL instead exclude Circinus.

The ordinary historical benchmark SHALL be the instrumented representative workload repeated in fresh processes and aggregated statistically. Armed-disabled-wrapper, fully-disarmed, and product-absent controls MAY be declared and run when calibrating profiler overhead or investigating passive load-time impact, but they SHALL NOT be required for every product or optional-mod performance run. A control comparison SHALL share the same broad scenario definition, package policy, workload version, timing, speed, selectors, and throughput checkpoints; it SHALL NOT require equal generated pawns, random-number state, unique IDs, jobs, positions, inventories, or terminal world state.

Host-safe discovery SHALL reject more than 1,024 benchmark declarations, 64 active packages, 256 method selectors, or 64 throughput checkpoints per declaration. Package IDs SHALL use at most 128 ASCII package-token characters; benchmark/workload/comparison/category/checkpoint/control identities SHALL use at most 256 characters; and an explicit method selector SHALL use at most 1,024 characters. These reuse the repository's established Gateway integration-matrix and identity ceilings rather than Circinus or HTTP limits, are published contract values with exact/over boundary tests, and SHALL be changed only as a reviewed contract revision. Exact-group identities SHALL use unambiguous length-prefixed package sequences rather than delimiter joining.

#### Scenario: Base and all-supported benchmarks are selected
- **WHEN** the caller selects one base Immersive Chefs fixture and one all-supported fixture
- **THEN** the host launches one fresh process for each distinct exact sequence and records every declared workload parameter before sampling

### Requirement: Circinus is guarded, local-only, and test-only
The runtime SHALL activate performance collection only when package `astryl.Circinus` is active and assembly `Circinus` satisfies the exact validated RimWorld 1.6 reflection shape and readable run-document schema major. The result SHALL record package version metadata, product build, assembly identity, MVID, length, SHA-256, schema major/minor, and native profiler policy/caps. No shipping repository mod SHALL reference or bundle Circinus, and an absent, inactive, or incompatible analyzer SHALL fail only the performance run.

Every isolated performance launch SHALL stage and live-verify Circinus settings with `autoStartProfiler=false`, `autoArmProfiler=false`, no `armedTargetKeys`, `autoProfile=false`, `autoProfileAsked` equal to the guarded current asked-version constant, `showWarmupWindow=false`, `warmupSeconds=0`, `ingestEnabled=false`, `consentVersion` equal to the guarded current disclosure-version constant, and `autoRecord=false`. It SHALL answer the current consent and auto-profile prompts without enabling sharing and SHALL NOT alter the user's normal Circinus settings.

#### Scenario: Circinus updates incompatibly
- **WHEN** its package is active but a required settings, arming, recording, marker, stop, JSON, persistence, or schema member differs in declaring type, signature, return shape, accessibility, or supported schema major
- **THEN** the benchmark records the precise failed shape and executes no fuzzy lookalike, DPA fallback, or private timing replacement
- **THEN** ordinary gameplay and E2E tests remain available without Circinus

#### Scenario: Sharing or automatic profiling is enabled in the isolated run
- **WHEN** the live Circinus settings permit ingest, an automatic window, automatic arming, or a consent/auto-profile modal to control the process
- **THEN** sampling is refused before a benchmark run starts and no result is accepted

### Requirement: Relevant product methods are registered dynamically
Before recording, the runtime SHALL deterministically hand-arm all attached runtime Harmony prefixes, postfixes, and finalizers owned by each selected product's exact Harmony ID, every selected product tick/component method named by the performance contract, selected Circinus curated system targets, and every explicit benchmark-manifest method/type selector. It SHALL retain each resolved declaring type, method, signature, module, assembly, category, selection reason, and hand-armed state; reject unsupported or unresolved selectors; and avoid whole-assembly profiling unless explicitly requested.

Harmony registry discovery SHALL validate the exact loaded public member shape and stop above the published repository safety ceilings of 65,536 global patched targets or 65,536 total patch attachments before further materialization. These provider-failure ceilings SHALL have exact/over boundary tests and MUST NOT be presented as Harmony, Circinus, request, or artifact limits. The selected hand-armed set SHALL still obey Circinus' separate native output ceilings.

The runtime SHALL also discover and retain every product-owned transpiler identity and patched target. It SHALL mark direct workload timing unsupported for transpiler methods rather than pretending their patch-time invocation is runtime cost. The separately automated DPA diagnostic MAY investigate transformed IL, but its result MUST NOT enter a Circinus baseline or satisfy a canonical performance group.

#### Scenario: A product adds a new Harmony postfix
- **WHEN** the reviewed build attaches that postfix under the product's Harmony owner and a benchmark group loads it
- **THEN** runtime discovery hand-arms the exact postfix automatically and the report attributes its calls and time to that build

#### Scenario: A manifest selector becomes stale
- **WHEN** an explicitly named work-giver or component method no longer resolves exactly after a game/mod update
- **THEN** the benchmark fails before sampling rather than silently omitting the metric

#### Scenario: A product owns a transpiler
- **WHEN** discovery finds the transpiler attached to an exact runtime target
- **THEN** the report retains both identities and the direct-timing refusal
- **THEN** the canonical run neither invokes the transpiler as workload nor imports a DPA measurement into its result

### Requirement: DPA deep diagnostics are automated and noncanonical
`scripts/Invoke-RimWorldPerformanceTests.ps1 -DiagnosticProfiler Dpa` SHALL launch a fresh exact process containing Harmony, Core, package `Dubwise.DubsPerformanceAnalyzer.steam`, the selected product/optional packages, and Gateway, without Circinus. A guarded test-only adapter SHALL record DPA's package/assembly identity and validate the exact installed registration, start, stop, snapshot, and cleanup member shape before registering only explicitly requested method, type, nested-type, mod, built-in category, internal-call, or transpiled-IL selectors. It SHALL drive DPA programmatically, retain raw diagnostic entries with the exact workload/checkpoint identity, and remove only run-owned state.

DPA output SHALL be labeled diagnostic, SHALL NOT offer accepted-baseline creation or satisfy a canonical performance group, and SHALL NOT be combined or normalized as though it used Circinus' schema, duty cycle, or sampling policy. An absent, inactive, or changed-shape DPA SHALL fail only the requested diagnostic.

#### Scenario: A Circinus regression needs transformed-IL diagnosis
- **WHEN** the caller requests `-DiagnosticProfiler Dpa` with one exact transpiled target or internal-call selector
- **THEN** the runner launches DPA without Circinus, exercises the same declared native workload, and returns a bounded raw diagnostic snapshot for that selector
- **THEN** no DPA value is written to or compared with a Circinus baseline

### Requirement: Sampling uses Circinus and ordinary game progression
The benchmark SHALL arrange its representative workload while paused, verify coarse fixture counts and required facilities, warm it through ordinary game frames/ticks while Circinus recording is disabled, reset only benchmark-owned measurement counters, start one labeled Circinus run, add sample-boundary markers, enable the exact hand-armed profiler set, and run the declared sample through native game-speed progression. It SHALL then disable profiling; snapshot `Method`, `HandArmed`, `SampleShift`, `TotalCalls`, `TotalTimedCalls`, `Empty`, and `CyclesSeen` from every exact run-owned live `DevProfiler`; stop Circinus; correlate each non-empty sidecar to its exact `PatchStat` or `MethodStat`; retain every empty/uninvoked sidecar with an explicit no-row reason; retain the stopped document and persisted isolated run JSON; and clean only run-owned instrumentation.

It MUST NOT seed or reset RimWorld's global random state or unique-ID manager, freeze ordinary actors into identical jobs, normalize the colony back to an exact post-warm-up state, reject a repetition because generated IDs or start/terminal state hashes differ, call `DoSingleTick`, directly invoke profiled methods to inflate counts, alter Circinus' native duty/adaptive sampling, construct expected terminal metrics, or accept a sample that silently overflowed Circinus' 2000-recorded-frame ring, 7200-sample run ceiling, or patch/method detail caps.

#### Scenario: A normal sample completes
- **WHEN** warm-up settles and the declared sample interval advances through the running game's normal tick manager
- **THEN** Circinus reports measurements caused by the active pawns, jobs, buildings, components, world objects, patches, and UI/update cycles in that interval
- **THEN** the stopped in-memory JSON and Circinus-persisted JSON agree on schema and run identity

#### Scenario: Gameplay stalls
- **WHEN** expected meals, dish cycles, nursing, hauling, dining, pathing, or caravan activity does not reach its declared throughput checkpoint
- **THEN** the sample is invalid even if its recorded CPU time is low

#### Scenario: Ordinary repetitions diverge
- **WHEN** fresh repetitions generate different pawns, jobs, paths, IDs, inventories, or terminal layouts while the declared scenario remains healthy
- **THEN** every repetition remains valid and its raw result is retained
- **THEN** the report aggregates the compatible measurements rather than trying to force or prove identical game state

#### Scenario: A declared sample exceeds Circinus' retained window
- **WHEN** the native recorded-frame ring or required patch/method-detail cap would omit an earlier required part of the sample
- **THEN** the run fails or is split into explicitly declared compatible windows/repetitions rather than accepting only the retained tail

### Requirement: Reports preserve direct, system-level, and sampling-policy metrics
Every sample SHALL retain the untouched local Circinus JSON entries for product methods, Harmony patches, per-mod costs, relevant curated targets, timestamped samples, markers, and errors. Because ordinary Circinus `MethodStat` JSON does not persist timed-call or adaptive-sampling fields, every non-empty pre-stop run-owned `DevProfiler` sidecar SHALL correlate by exact identity to the stopped `PatchStat` used for a Harmony patch method or the stopped `MethodStat` used for an ordinary method. Every empty/uninvoked sidecar SHALL survive with `noRowReason=empty-or-uninvoked`, matching Circinus' intentional exclusion of empty profilers from stopped rows. A non-empty missing row, empty sidecar with a row, duplicate/ambiguous match, or unexplained trimmed required row SHALL invalidate the sample. The combined evidence SHALL record calls, patch-row timed calls where present, live method `TotalTimedCalls`, `SampleShift`, `HandArmed`, `Empty`, `CyclesSeen`, total, mean per recorded profiler cycle, maximum recorded-cycle time, derived gross time per estimated call, shared cost, skip-capable cost, ambiguous-target state, profiler cycles, profiler-window milliseconds/ticks, duty percentage, sampled state, auto-shed count, dropped-detail counters, achieved TPS/FPS, frame-time mean/max/P95, tick-time mean/max, heap, pawn count, elapsed ticks/wall time, GC/memory checkpoints, exact workload counts, evidence lens, and control-mode identity.

Normalized summaries MUST preserve units, denominators, and the native sampling policy. They MUST label Circinus patch/mod timing as gross attribution rather than causal net impact and mean timing as per recorded profiler cycle rather than per call. Every emitted numeric metric MUST be finite. A share or rate whose native denominator is zero SHALL be omitted as unobserved rather than fabricated as zero or emitted as a non-finite value; a nonzero numerator against that denominator SHALL fail normalization. They MUST NOT present adaptively estimated calls as continuously timed calls, compare tick metrics to frame metrics as though they share one basis, compare raw milliseconds across incompatible machines, subtract semantically different product-present/product-absent workloads, or silently accept a missing/duplicate sidecar correlation or Circinus refusal/incomplete/truncation state for a required metric.

Every ordinary benchmark SHALL run at least three fresh instrumented repetitions by default. The report SHALL retain every raw repetition and aggregate compatible metric values with sample count, arithmetic mean, minimum, maximum, and sample standard deviation. A later workload may use more repetitions when observed variance warrants it. The runner SHALL NOT discard a healthy repetition merely because it is an outlier.

When specifically requested, a workload MAY additionally run compatible armed-disabled and fully-disarmed processes while retaining the same active Circinus recorder/sample collection. The armed-disabled process SHALL retain the same Circinus wrappers with `ProfilerRegistry.Enabled=false` and `Recording=false`; the fully disarmed process SHALL remove all run-owned Circinus wrappers before sampling. Reports SHALL label instrumented-minus-armed-disabled as active method-timing/sampling overhead, armed-disabled-minus-fully-disarmed as wrapper overhead, and instrumented-minus-fully-disarmed as total method-instrumentation overhead. No one of these controls SHALL be labeled analyzer-absent. Those calibration estimates SHALL remain separate from gross code attribution and independently reported product-present/product-absent samples.

#### Scenario: A work giver regresses
- **WHEN** its share, mean, or total CPU time rises while calls, timed calls, profiler window, workload, and sample policy remain compatible
- **THEN** raw and normalized reports expose the exact work-giver entry alongside overall work-scan/TPS context

#### Scenario: Instrumentation overhead is requested
- **WHEN** compatible instrumented, armed-disabled, and fully disarmed samples complete in fresh processes
- **THEN** the report exposes active method-timing/sampling, wrapper-only, and total method-instrumentation deltas separately
- **THEN** disabling `ProfilerRegistry.Enabled` alone is never labeled fully disarmed or analyzer-absent

#### Scenario: Net impact is requested
- **WHEN** repeated product-present and product-absent controls use the same broad neutral scenario, checkpoints, hardware, timing, and sampling policy
- **THEN** the runner SHALL retain and report both independent distributions without pairing repetitions by index or fabricating a delta distribution
- **THEN** a descriptive difference between their aggregate means MAY be calculated outside the canonical baseline only when it is labeled as an independent-sample estimate
- **THEN** Circinus' per-patch execution share remains separately labeled gross attribution

#### Scenario: A stochastic historical run is repeated
- **WHEN** the same workload version runs in multiple fresh processes
- **THEN** RimWorld's ordinary random, ID, think-tree, pathing, and job systems remain untouched
- **THEN** coarse health checkpoints prove that the intended gameplay systems were exercised, while ordinary state variation is represented by the retained distribution

#### Scenario: The product owns the workload semantics
- **WHEN** removing the product removes or materially changes the jobs/buildings being measured
- **THEN** the runner refuses a net subtraction and reports only gross attribution, separately measured active-timing/wrapper/total method-instrumentation overhead, and compatible version regression

### Requirement: Immersive Chefs has a substantial versioned workload
The initial Immersive Chefs fixture SHALL build a representative multi-room, non-trivial-pathing colony with roughly 36 human pawns across cooking, assistance, cleaning/hauling, nursing/patient, and dining roles; roughly 24 animals; simultaneous recipe tiers; prep/support stations; domestic and industrial dishwashing; microwaves; separated storage; patients and nurses; and caravans containing pawns, animals, meals, plates, and cutlery. The layout version, broad population/facility envelope, required active systems, and minimum successful outcomes SHALL be machine-verified. Exact pawn identities, paths, jobs, generated IDs, and terminal object locations SHALL be allowed to vary normally. Optional groups SHALL activate the corresponding installed integrations without changing unrelated package or workload identities.

#### Scenario: Base Immersive Chefs performance run
- **WHEN** Harmony, Core, Circinus, XML Extensions, Immersive Chefs, and Gateway form the exact active set in that order
- **THEN** the workload continuously exercises cooking, assistance, dining, temperature, tableware, sanitation, washing, hauling, patient feeding, animal exclusion, and caravan state without requiring an optional mod

#### Scenario: Processor and Dubs group runs
- **WHEN** the exact supported Processor Framework and Dubs Bad Hygiene packages are added
- **THEN** the same workload uses connected water-consuming Processor dishwashers and the report includes both integration adapter and Circinus/Gateway system metrics

#### Scenario: Optional performance matrices run
- **WHEN** an optional Immersive Chefs performance family is selected
- **THEN** it declares an instrumented repeated benchmark, with optional calibration lenses when requested, using one of these complete ordered non-Gateway package sequences after the mandatory Harmony/Core/Circinus/XML Extensions prefix:
  - Processor/Dubs: `syrchalis.processor.framework`, `Dubwise.DubsBadHygiene`, Immersive Chefs;
  - guest service: `Orion.Hospitality`, `Orion.CashRegister`, `Orion.Gastronomy`, `avilmask.CommonSense`, Immersive Chefs;
  - variety/VNPE/material/DLC: Royalty, Biotech, `Argon.CoreLib`, Vanilla Expanded Framework, Expanded Materials Masonry, Expanded Materials Metals, Variety Matters, Vanilla Food Variety Expanded, VNPE, Immersive Chefs;
  - all-supported performance union: the DLC/framework/material/Processor/Dubs/guest-service/variety/VNPE packages above in their dependency-safe order, then Immersive Chefs
- **THEN** every family retains a group-specific checkpoint caused during the measured native sample: both Processor dishwasher types admit and clean through a supplied Dubs network; a Hospitality guest receives colony cutlery through native Gastronomy waiter service while Common Sense does not steal Gastronomy-owned clearing; a native VNPE prepared-paste meal preserves public ingredient provenance while modded metal kitchenware and Royalty/Biotech expectation paths remain active; and the union family satisfies every constituent branch without duplicating ware or ownership
- **AND** the all-supported performance union is not the release startup canary and does not inherit unrelated downloaded food ecosystems

### Requirement: Exact optional-mod matrices are reusable
The host SHALL allow benchmark selection/filtering by benchmark ID and exact mod-group ID. A staged performance manifest MAY contain only the filtered comparison family and its required control from a larger compiled fixture assembly. The in-game catalog SHALL require every manifest entry to match exactly one compiled attributed type and SHALL admit only those listed entries; additional valid compiled benchmark types SHALL remain unlisted and uninstantiated rather than causing whole-assembly manifest mismatch. Future repository mods SHALL be able to add performance fixtures and method selectors in separate test projects without editing Gateway runtime code. A group SHALL never inherit an optional package merely because it is downloaded locally.

#### Scenario: A future product adds a benchmark
- **WHEN** its marked performance assembly declares a new exact group and fixture
- **THEN** host discovery builds, groups, stages, runs, and reports it through the same command surface

#### Scenario: A focused family shares an assembly with unrelated benchmarks
- **WHEN** a benchmark-ID filter selects one complete comparison family and required control from an assembly containing other valid attributed benchmarks
- **THEN** staging lists only the filtered family and control, the in-game catalog verifies those exact entries against compiled types, and no unrelated compiled benchmark is admitted or instantiated

### Requirement: Historical comparison is explicit and compatible
The runner SHALL emit untouched Circinus JSON, normalized JSON, CSV, and a Markdown summary for every run and SHALL compare aggregate results only against a compatible accepted baseline by default. Compatibility SHALL include workload version, package order, game version, product/test/Circinus assembly and schema identities, profiling/sampling policy, evidence lens, metric units, relevant hardware/runtime fingerprint, duration, and repetition policy. It SHALL NOT include an applied random seed, global random state, generated IDs, or exact start/terminal fixture hashes. Every aggregate SHALL retain every repetition as an observed value or an explicit unobserved/null value, sample count, aggregation kind, minimum, maximum, and sample standard deviation so historical trends and noise are visible. Ordinary metrics SHALL use an arithmetic mean. Per-call metrics SHALL use total measured time divided by total calls, retain the matching per-repetition call weights, and MUST NOT fabricate a zero per-call value for a repetition with no calls. Every method/patch SHALL additionally retain a bounded per-repetition observation indicator, including an all-zero vector when no repetition observed a call. Regression policies MAY be tracked per selector with explicit relative and/or absolute thresholds; absent an explicit reviewed policy, historical deltas are informational. A new runtime error, missing required profiler data, failed coarse scenario-health checkpoint, Circinus refusal/incomplete/truncation state, or exceeded explicit threshold SHALL fail comparison.

#### Scenario: A compatible regression exceeds policy
- **WHEN** a selected product method exceeds its reviewed relative or absolute threshold across the configured samples
- **THEN** the command exits nonzero and the report shows baseline, current samples, delta, threshold, calls/timed-calls/duty context, and exact method identity

#### Scenario: A caller records a new baseline
- **WHEN** baseline creation is explicitly requested after a reviewed run
- **THEN** the runner writes a candidate without overwriting an accepted baseline and requires an ordinary repository review/commit to accept it

### Requirement: Performance commands are safe and automation-friendly
`scripts/Invoke-RimWorldPerformanceTests.ps1` SHALL provide discoverable filters and path/sample overrides plus table and JSON console output. Exit code `0` SHALL mean all samples, cleanup, and requested comparisons passed; `1` SHALL mean a sample/runtime/comparison failure; `2` SHALL mean invalid usage or discovery. Every launch SHALL use isolated savedata, muted background preferences, local-only Circinus settings, exact PID ownership, controlled shutdown, credential/stage cleanup, final log scanning, and unchanged normal configuration/settings hashes.

#### Scenario: Periodic benchmark automation succeeds
- **WHEN** every selected group completes its declared repetitions without errors or threshold regressions
- **THEN** the command exits `0` and the durable aggregate links every group, sample, raw Circinus run, normalized report, summary, and cleanup record

### Requirement: Performance workflow is documented separately from correctness
Repository documentation and the RimWorld development skill SHALL explain setup for Circinus, isolated local-only settings, benchmark discovery, exact matrix selection, raw versus normalized metrics, native duty/adaptive sampling and retained-window limits, baseline review, the automated noncanonical DPA diagnostic command, limitations of live-game profiling, and how to add a future product fixture. Performance results MUST supplement rather than replace correctness/E2E acceptance.

#### Scenario: A mod update changes internals without breaking behavior
- **WHEN** ordinary tests and E2E workflows still pass but the performance comparison fails
- **THEN** the change remains behaviorally correct but is not performance-accepted until the regression is investigated or the policy/baseline change is explicitly reviewed
