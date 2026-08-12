---
name: rimworld-performance-benchmarking
description: Design, implement, run, and interpret isolated RimWorld mod performance benchmarks in this repository. Use for Circinus profiling, historical baselines, product-present/product-absent workload comparisons, Harmony or tick-method attribution, instrumentation-overhead controls, Dubs Performance Analyzer diagnostics, or performance fixture and runner work.
---

# RimWorld Performance Benchmarking

Follow root `AGENTS.md`, `rimworld-mod-development`, and `rimworld-dev-gateway`. Performance evidence
supplements correctness/E2E acceptance; it never replaces a native gameplay verification.

## Check implementation status first

Read the proposal, design, spec, and tasks under
`openspec/changes/add-rimworld-performance-benchmarking/` before naming a command. Inspect actual
script help and task state. Do not invent `Invoke-RimWorldPerformanceTests.ps1` behavior or claim a
benchmark can run while its implementation task is unchecked.

Read [references/circinus.md](references/circinus.md) before changing the adapter, fixture, metrics,
controls, or baseline policy.

## Use Circinus as the canonical profiler

- Canonical performance groups use exact ordered Harmony, Core, Circinus, product/optional mods,
  then Gateway. They exclude Dubs Performance Analyzer.
- Detect package `astryl.Circinus` and validate its assembly identity, MVID/hash, schema, exact member
  shapes, settings, and prompt constants before touching it. Keep reflection isolated and fail only
  the benchmark group when absent/incompatible.
- Never reference or bundle Circinus from a shipping product or Gateway package. Dynamically stage
  performance fixtures outside product `Assemblies/`.
- Force isolated local-only settings: no automatic profiler/arming/recording, no warm-up window, no
  automatic UI, no ingest/sharing, and no mutation of the user's normal Circinus settings.
- Use Circinus' recorder, instrumenter, profiler registry, curated targets, and JSON writer. Do not
  reimplement its profiler or silently fall back to another analyzer.

## Measure an ordinary workload

1. Declare a stable benchmark ID, staging owner, measured subject, complete exact package order,
   representative workload version, warm-up/sample ticks, speed, repetitions, evidence lens,
   method selectors, and throughput checkpoints.
2. Arrange the fixture while paused. Warm it through ordinary game frames/ticks with recording off.
   Never call `DoSingleTick` or directly invoke measured methods to inflate counters.
3. Resolve and hand-arm the exact product Harmony prefix/postfix/finalizer methods, selected product
   tick/component methods, explicit selectors, and relevant curated targets. Record full identities
   and reject unresolved/ambiguous/duplicate/open-generic selectors.
4. Reset only profiler and throughput counters after warm-up, start one labeled Circinus run, add boundary markers,
   enable profiling, and progress the native workload at the declared game speed.
5. Disable profiling, snapshot every run-owned live `DevProfiler` before stop/disarm, stop the run,
   retain both in-memory and persisted raw JSON, then clean only run-owned instrumentation.
6. Verify workload counts and ordinary colony behavior in game. Inspect the live armed/recording state,
   logs, exact process, settings restoration, and cleanup.

## Keep claims distinct

- Circinus per-patch/per-mod time is **gross attribution**, not causal net impact.
- Product-present and product-absent workloads are independent stochastic diagnostics. Keep both
  distributions; do not pair repetitions by process order or call the result a matched net delta.
- Instrumented minus armed-disabled estimates active timing/sampling overhead; armed-disabled minus
  fully disarmed estimates wrapper overhead; instrumented minus fully disarmed estimates total method
  instrumentation overhead. None is an analyzer-absent control.
- Version-over-version regression is valid only across compatible workload, package, game, product,
  profiler, schema/policy, unit, denominator, lens, and hardware/runtime identities.
- Preserve raw units and denominators. Do not relabel per-recorded-cycle means as per-call means or
  adaptive estimated calls as continuously timed calls.

Treat repetitions as ordinary stochastic RimWorld samples. Do not seed global `Rand`, reset
`UniqueIDsManager`, teleport actors back into identical post-warm-up state, or reject healthy differences
in jobs, positions, needs, inventories, weather, or outcomes. Retain the raw repetitions and report count,
mean, minimum, maximum, and sample standard deviation. Calibration lenses and product-absent controls are
optional diagnostic tools, not a mandatory cost paid by every historical trend run.
For per-call metrics, keep zero-call repetitions as explicit unobserved/null samples with zero weight.
Pool total time over total calls for the aggregate; never fabricate `0 ms/call` or silently drop the run.
Retain an explicit 0/1 observation-rate vector as well, so a selector that is uncalled in every repetition
does not vanish from historical reports.
The runner therefore defaults to instrumented benchmarks only. Pass `-IncludeProfilerControls` only
for an explicit overhead-calibration run; an exact `-BenchmarkId` may also select one control directly.

Product-present and product-absent simulations are independent stochastic samples. Do not subtract
repetitions by array index or present that arbitrary ordering as a delta distribution. Keep both source
distributions and compare their averages descriptively unless a real statistical design is added.

Use explicit per-selector absolute/relative thresholds when a trend is mature enough to gate. With no
reviewed threshold, retain a compatible historical delta as informational. Missing metrics, workload drift, profiler
refusal/incomplete/truncation, identity drift, new runtime errors, or incompatible units fail a
comparison. Baseline creation must produce a review candidate; never overwrite an accepted baseline.

## Use DPA only for bounded diagnosis

Run Dubs Performance Analyzer in a separate fresh group without Circinus when investigating an
internal call, transpiled IL, allocation/native/GC detail, or another question Circinus cannot
attribute directly. Guard its installed reflection shape, register exact requested selectors, retain
raw output, and clean run-owned state. Label the result diagnostic; never merge it into a Circinus
baseline or use it to satisfy a canonical performance group.

## Preserve isolation and package boundaries

Use fresh savedata, muted background preferences, exact PID ownership, controlled shutdown, clean
developer logs, and before/after hashes for normal RimWorld and Circinus settings. Performance test
assemblies, Circinus, DPA, raw reports, and profiler state must never enter product packages or normal
E2E runs.
