## 1. shared performance contract — TDD model and discovery

- [ ] 1.1 TDD RED: Add failing tests for attributed benchmark IDs, exact package groups, deterministic seed, workload version, warm-up/sample ticks, speed, repetitions, method selectors, throughput checkpoints, and invalid declarations.
- [ ] 1.2 TDD GREEN: Implement the host-safe `RimWorldDevGateway.PerformanceTesting` contract and metadata discovery/grouping on top of the accepted E2E harness.

## 2. mods/RimWorldDevGateway — DPA compatibility adapter

- [ ] 2.1 INSPECT: Retain the installed DPA package ID, About metadata, `PerformanceAnalyzer.dll` identity/MVID/hash, and exact registration/start/stop/result-store member signatures without editing Workshop content.
- [ ] 2.2 TDD RED: Add absent, inactive, supported-shape, changed-shape, duplicate-registration, stale-selector, and lifecycle-cleanup tests for the reflection adapter.
- [ ] 2.3 TDD GREEN: Implement exact package/assembly/member guards and programmatic DPA method registration, recording, stop, and raw snapshot collection with no hard optional reference.
- [ ] 2.4 IN-GAME: In a minimal calibration process, register one known Gateway method, exercise it through an ordinary request/game workflow, and collect its DPA metric plus built-in category metrics.

## 3. mods/RimWorldDevGateway — dynamic method selection and metrics

- [ ] 3.1 TDD RED: Add failing tests for attached Harmony-owner discovery, product tick/component overrides, explicit method/type selectors, deterministic de-duplication, unsupported generic/compiler-generated members, categories, and retained identities.
- [ ] 3.2 TDD GREEN: Implement bounded method-set discovery and registration plus raw/normalized call, total, average, maximum, distribution, TPS/FPS, GC/memory, unit, denominator, workload, and control-phase capture.
- [ ] 3.3 REVIEW: Measure and document analyzer overhead; keep whole-assembly and internal-method profiling explicit opt-ins rather than default registration.

## 4. host runner — CLI TDD, reports, and baselines

- [ ] 4.1 TDD RED: Add a real dry-run helper and failing command tests for benchmark/group filters, sample overrides, exact process plans, repetitions, table/JSON output, raw/normalized JSON, CSV/Markdown reports, exit codes, and cleanup.
- [ ] 4.2 TDD GREEN: Implement `scripts/Invoke-RimWorldPerformanceTests.ps1` using the E2E launcher/grouping/staging substrate.
- [ ] 4.3 TDD RED/GREEN: Implement compatible-baseline selection, per-metric absolute/relative thresholds, missing/unit/workload drift failures, informational cross-version diffs, and explicit non-overwriting baseline candidates.

## 5. tests/ImmersiveChefs.PerformanceTests — versioned workload

- [ ] 5.1 TDD RED: Define and validate the exact 36-pawn, 24-animal, multi-room/pathing/kitchen/dishwashing/microwave/patient/nurse/caravan workload and its throughput checkpoints.
- [ ] 5.2 TDD GREEN: Implement the deterministic base fixture without direct ticking or synthetic terminal work and make a complete base sample valid.
- [ ] 5.3 MATRIX: Add exact Processor/Dubs, Gastronomy/Hospitality/Common Sense, variety/VNPE/material/DLC, and all-supported groups with group-specific fixture branches and checkpoints.
- [ ] 5.4 IN-GAME: Run the reviewed base and at least one optional-mod performance group, inspect DPA's live registration/recording state and ordinary active colony behavior, and retain compatible raw reports.

## 6. docs and release gates — review and historical use

- [ ] 6.1 DOCUMENT: Update README, Gateway/development/testing docs, AGENTS, and the repo-local RimWorld skill with DPA setup, commands, metrics, limitations, exact matrices, fixture authoring, and baseline review.
- [ ] 6.2 REGRESSION: Run focused/complete host suites, Release/package checks, E2E regression, strict OpenSpec validation, and prove DPA/performance assemblies never enter product packages.
- [ ] 6.3 REVIEW: Independently review DPA compatibility, instrumentation bias, workload realism, metric units, comparison honesty, cleanup, and generic future-mod extensibility; resolve findings and rerun affected benchmarks.
