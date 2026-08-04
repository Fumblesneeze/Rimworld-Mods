## Context

The repository currently proves correctness but has no repeatable way to measure the cost of its Harmony patches, work scanners, jobs, ThingComps, map/world components, and ticks under load. Dubs Performance Analyzer (DPA) is installed locally at Workshop item `2038874626`, package ID `Dubwise.DubsPerformanceAnalyzer.steam`. The inspected RimWorld 1.6 assembly is `PerformanceAnalyzer.dll`, 238080 bytes, SHA-256 `A1758774137F5EFF19F6B98D8D29F46AE5F8247C3EADBF9F611D851568D471A8` as of 2026-08-05. Its documentation supports method/type/nested-type/mod profiling and pre-made tick/update categories, but its programmatic API is not a stable public compatibility contract.

This capability is owned by RimWorld Dev Gateway and depends on the exact-mod grouping, dynamic test loading, reset, process safety, and artifact collection defined by `add-rimworld-e2e-harness`. DPA and benchmark fixtures are test-only dependencies. Gameplay mods never reference DPA or Gateway.

## Goals / Non-Goals

**Goals:**

- Run a reproducible, substantial game workload under exact optional-mod combinations.
- Profile the repository mod's attached Harmony patches and relevant hot runtime methods without manually clicking through DPA.
- Retain raw DPA measurements and normalized summaries with enough environment/build identity for historical comparison.
- Detect both direct mod-method regressions and broader TPS/frame/pathing/work-scan regressions.
- Let future repository mods contribute their own benchmark scenarios and method selectors.

**Non-Goals:**

- Claiming laboratory-grade microbenchmark precision from a live Unity game.
- Making DPA a shipping dependency or copying its assembly into a repository mod.
- Automatically accepting a new baseline after a regression.
- Profiling every method in every assembly by default; instrumentation overhead would dominate the workload.
- Replacing external profilers when native/GC/rendering investigation requires them.

## Decisions

### 1. Performance runs extend E2E groups but use a separate command and contract

Performance fixtures use the same complete ordered package-set declaration and one-process-per-group orchestration as E2E tests. A separate `RimWorldDevGateway.PerformanceTesting` contract marks benchmark scenarios, warm-up/sample phases, workload scale, method selectors, and comparison policy. `scripts/Invoke-RimWorldPerformanceTests.ps1` performs discovery, group launch, repeated samples, reports, and comparison. Normal E2E runs do not load DPA or performance assemblies.

Keeping performance separate prevents slow, noisy benchmarks from becoming ordinary correctness tests and permits different repetition, timeout, and reporting semantics.

### 2. DPA is controlled through one exact reflection adapter

The Gateway detects only active package ID `Dubwise.DubsPerformanceAnalyzer.steam`, resolves assembly `PerformanceAnalyzer`, records its full identity/MVID/hash, and validates the exact types, fields, methods, parameter types, and result stores used by the installed version before doing anything. The adapter calls the same internal registration/start/stop/snapshot paths used by DPA's UI. It does not call method-name lookalikes, patch DPA itself, or retain reflected objects across incompatible lifecycle phases.

The documented `Analyzer.xml` mechanism was considered. It is useful for static author-owned tabs but does not satisfy dynamic per-run selection, exact attached-Harmony discovery, or programmatic result collection. It may be used as a diagnostic cross-check, not as the primary automation path.

When DPA updates and its guarded shape changes, the affected benchmark group fails with the observed package/assembly identity and missing member. Core E2E and gameplay remain unaffected. There is no silent fallback to invented timing code.

### 3. Method selection combines runtime discovery with an explicit manifest

For each selected repository product assembly the benchmark adapter registers:

- every currently attached Harmony prefix, postfix, transpiler, and finalizer owned by that package's exact Harmony ID;
- product-declared `Tick`, `TickRare`, `TickLong`, `CompTick`, `MapComponentTick`, `WorldComponentTick`, and `GameComponentTick` overrides;
- product-owned work-giver, job-giver, think-node, alert/report, inspect-string, and map-query methods selected by the benchmark manifest;
- explicitly named methods or types contributed by the benchmark fixture.

Registration is deterministic and rejects unresolved, generic-open, duplicate, compiler-generated-only, or unsupported methods. The exact resolved method identities are retained. Profiling an entire product assembly is an explicit opt-in diagnostic because it can add excessive overhead.

### 4. Capture both direct and system-level measurements

Raw output preserves DPA's calls, total, average, maximum, category/update basis, and any available distributions for every registered entry. The run also collects DPA's existing aggregate tick/update, pawn/thing tick, work giver, think tree, pathing, UI/update, and frame metrics that are active for the scenario, plus observed game ticks, wall time, achieved TPS/FPS, GC collection counts, managed-memory checkpoints, errors, and workload counts.

Reports never compare unlike bases as if they were the same. Tick-category measurements remain per tick/call; frame/update measurements remain per rendered update. Every normalized value identifies its denominator and unit.

### 5. The benchmark lifecycle has explicit calibrated phases

Each scenario declares a deterministic seed, exact workload version, warm-up ticks, sample ticks, game speed, and repetition count. Defaults are stored in the scenario source and may be changed only as a versioned workload change; the CLI may override them and records every override. Warm-up builds caches and settles jobs before DPA recording begins. Sampling starts from a checkpoint, runs ordinary game frames/ticks without direct ticking, stops recording through DPA, and then captures results before cleanup.

The host launches a fresh process for every exact mod group and optionally repeats a group in fresh processes. Historical summaries report every repetition plus median and range; they do not hide outliers. Failures, exceptions, throttling, unfinished jobs, or workload-count drift invalidate the sample rather than becoming suspiciously fast results.

### 6. Immersive Chefs gets one large versioned workload

The initial workload creates a deterministic multi-room colony with long corridors and separated storage/service areas; 36 human pawns distributed among cooking, assistance, cleaning/hauling, nursing/patient, and ordinary dining roles; 24 animals; multiple simultaneous simple/fine/lavish cooking bills; prep and four linked support station types; domestic and industrial dishwashers; microwaves; dirty/clean stockpiles; several patients and nurses; guest/service activity when the relevant package is active; and two world caravans with pawns, animals, meals, plates, and cutlery. Exact counts and Def identities live in a versioned manifest and the result verifies them before sampling.

Optional-mod variants add only behavior owned by their exact group: Processor/Dubs dishwashing, Gastronomy/Hospitality service, Common Sense cleanup, variety/provenance, VNPE paste, Expanded Materials, Royalty/Biotech, and an all-supported matrix. The base workload remains useful with only Core, Harmony, Immersive Chefs, DPA, and Gateway.

### 7. Historical comparison is explicit and reviewable

Each report writes raw JSON, normalized JSON, CSV, and a human Markdown summary. A baseline is an immutable reviewed report named by workload version, game version, mod group, product commit/build hash, DPA identity, and relevant hardware/runtime fingerprint. The runner compares only compatible identities unless the caller explicitly requests a cross-version informational diff.

Thresholds are configured per metric selector as absolute and/or relative limits in tracked baseline policy; the runner does not invent one global percentage. Missing metrics, workload drift, new exceptions, or incompatible units fail comparison. Creating or replacing a baseline requires an explicit command and writes a candidate for review; it never overwrites an accepted baseline silently.

## Risks / Trade-offs

- **DPA internal API changes** → Exact package/assembly/member guard, installed-shape tests, actionable failure, and no effect on product gameplay.
- **Profiling overhead changes the result** → Register a bounded relevant method set, retain the exact set, run an uninstrumented control phase, and report DPA overhead/context rather than hiding it.
- **Live-game noise obscures small regressions** → Deterministic workload/seed, warm-up, fresh-process repetitions, compatible-environment checks, medians/ranges, and per-metric thresholds.
- **A fast result is caused by stalled gameplay** → Assert actor/building/caravan/job throughput and absence of errors before accepting each sample.
- **Optional mods change workload availability** → Exact group-specific fixture branches and expected workload counts; no inferred or downloaded-only activation.
- **Performance artifacts become enormous** → Store raw data under ignored artifacts with stable summaries tracked only when explicitly accepted as baselines; do not truncate a method entry silently.

## Migration Plan

1. Finish and accept the E2E harness.
2. Add the performance contract and host discovery/report skeleton with a fake analyzer adapter for host tests.
3. Inspect the installed DPA assembly metadata and add an exact reflection-shape integration test.
4. Implement runtime registration, start/stop, raw snapshot capture, and one tiny calibration fixture.
5. Add the full versioned Immersive Chefs workload and base matrix.
6. Add optional-mod groups incrementally, then baseline/diff commands and documentation.

Rollback is omission of the performance startup flag and removal of marker-owned staged performance bundles. Every process uses isolated savedata and an isolated mod list; the runner verifies the user's normal `ModsConfig.xml` and `Prefs.xml` hashes in `finally` and stops only the exact launched PID.

## Open Questions

The exact DPA reflection member set will be named only after metadata inspection and one live shape probe of the installed 1.6 assembly. This is an implementation discovery task, not permission to use fuzzy reflection.
