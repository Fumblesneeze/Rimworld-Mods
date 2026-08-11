## 1. shared performance contract — TDD model and discovery

- [x] 1.1 TDD RED: Add failing tests for attributed benchmark IDs, staging-owner and measured-subject package IDs, exact package groups, deterministic seed, workload version, warm-up/sample ticks, speed, repetitions, evidence lens, optional semantically compatible product-absent control group, method selectors, throughput checkpoints, and invalid declarations. Retained REDs: `20260811T141021937Z-34612-09f2b09b1323478b9ca9fbee34c14312` (missing contract) and `20260811-performance-host-total-red` (missing aggregate declaration ceiling); final focused GREEN: `20260811-performance-contract-final` (28/28).
- [x] 1.2 TDD GREEN: Implement the host-safe `RimWorldDevGateway.PerformanceTesting` contract and metadata discovery/grouping on top of the accepted E2E harness. The reader discovers compiled attributes without loading the fixture assembly, exact package/lens/control semantics and published boundaries are enforced before launch, and `20260811-performance-host-final` passes 34/34 host tests.

## 2. mods/RimWorldDevGateway — Circinus compatibility adapter

- [x] 2.1 INSPECT: Retain the installed Circinus package/About metadata; `Circinus.dll` identity, product build, MVID, length, and hash; schema `1.15`; native 60/240 duty policy, 64-call adaptive threshold and maximum sample shift `8`, 2000-frame ring, 3000-patch/1000-method output caps, 7200-sample run ceiling, 30-sample partial flush, and temp/delete/move persistence behavior; exact `RunRecorder`, `Instrumenter`, `ProfilerRegistry`, `DevProfiler`, `TargetCatalogue`, `ProfileTarget`, and `RunDocument` declaring types/member kinds/accessibility/parameter/return shapes; exact `CircinusSettings` field types and required values; and prompt-version constants without editing Workshop content. The retained facts and DPA comparison are recorded in `design.md` (2026-08-11).
- [ ] 2.2 TDD RED: Add absent, inactive, supported-shape, changed-shape/schema, sharing-or-automatic-mode, duplicate-registration, stale-selector, native-cap overflow, persisted-document mismatch, non-empty patch/method-row correlation, empty/uninvoked no-row retention, and lifecycle-cleanup tests for the reflection adapter.
- [ ] 2.3 TDD GREEN: Implement exact package/assembly/schema/member guards, isolated local-only Circinus settings, deterministic hand-armed method registration, controlled recording/markers/stop, dual raw-JSON capture, and run-owned cleanup with no hard optional reference.
- [ ] 2.4 IN-GAME: In a minimal calibration process, register one known Gateway method, exercise it through an ordinary request/game workflow, and collect its Circinus method/patch metric, mod attribution, time series, raw persisted run, duty policy, and Gateway control metrics.

## 3. mods/RimWorldDevGateway — dynamic method selection and metrics

- [ ] 3.1 TDD RED: Add failing tests for attached Harmony-owner discovery, product tick/component overrides, explicit method/type selectors, deterministic de-duplication, unsupported generic/compiler-generated members, categories, and retained identities.
- [ ] 3.2 TDD GREEN: Implement bounded method-set discovery and hand-armed registration plus untouched Circinus JSON; a pre-stop `DevProfiler` sidecar for method identity, hand-armed state, sample shift, calls/timed calls, empty state, and cycles; exact non-empty correlation to `PatchStat` or `MethodStat`; retained empty/uninvoked no-row reasons; and normalized total, mean, maximum, gross/shared/skip-capable/ambiguous frame-share, TPS/FPS/frame/tick/heap samples, GC/memory checkpoints, unit, denominator, workload, profiler-policy, evidence-lens, and control-mode capture.
- [ ] 3.3 REVIEW: Measure and document Circinus duty/adaptive limits using fresh instrumented, armed-disabled-wrapper, and fully disarmed-recorder controls; separate active-timing, wrapper, and total method-instrumentation overhead without claiming an analyzer-absent control; and keep whole-assembly profiling explicit.
- [ ] 3.4 INSPECT/TDD RED: Retain DPA's exact installed package/assembly identity and registration/start/stop/snapshot/cleanup shape, then add absent/inactive/changed-shape, bounded-selector, raw-snapshot, and run-owned-cleanup tests for the separate diagnostic adapter.
- [ ] 3.5 TDD GREEN/IN-GAME: Implement `-DiagnosticProfiler Dpa`; in a fresh DPA-without-Circinus process exercise one exact internal-call or transpiled-IL selector through the native workload; retain the raw diagnostic result; and prove it cannot create, satisfy, or contaminate a Circinus baseline.

## 4. host runner — CLI TDD, reports, and baselines

- [ ] 4.1 TDD RED: Add a real dry-run helper and failing command tests for benchmark/group filters, sample overrides, exact process plans, repetitions, table/JSON output, raw/normalized JSON, CSV/Markdown reports, exit codes, and cleanup.
- [ ] 4.2 TDD GREEN: Implement `scripts/Invoke-RimWorldPerformanceTests.ps1` using the E2E launcher/grouping/staging substrate.
- [ ] 4.3 TDD RED/GREEN: Implement compatible-baseline selection, per-metric absolute/relative thresholds, missing/unit/workload drift failures, informational cross-version diffs, and explicit non-overwriting baseline candidates.

## 5. performance fixtures — Immersive Chefs workload and Gateway-owned neutral control

- [ ] 5.1 TDD RED: Define and validate the exact 36-pawn, 24-animal, multi-room/pathing/kitchen/dishwashing/microwave/patient/nurse/caravan workload, its throughput checkpoints, and a smaller Gateway-owned, product-reference-free, semantically identical product-present/product-absent neutral colony control for passive net system delta.
- [ ] 5.2 TDD GREEN: Implement the deterministic product and Gateway-owned neutral-control fixtures without direct ticking or synthetic terminal work and make complete product, instrumented, armed-disabled, fully disarmed, and paired-control samples valid.
- [ ] 5.3 MATRIX: Add exact Processor/Dubs, Gastronomy/Hospitality/Common Sense, variety/VNPE/material/DLC, and all-supported groups with group-specific fixture branches and checkpoints.
- [ ] 5.4 IN-GAME: Run the reviewed base and at least one optional-mod performance group, inspect Circinus' live hand-armed/recording state and ordinary active colony behavior, and retain compatible raw local-only reports.

## 6. docs and release gates — review and historical use

- [ ] 6.1 DOCUMENT: Update README, Gateway/development/testing docs, AGENTS, and the repo-local RimWorld skill with Circinus setup, local-only settings, commands, metrics, native caps/sampling limitations, exact matrices, active-timing/wrapper/total-method-instrumentation controls, the automated noncanonical DPA diagnostic command, fixture authoring, and baseline review.
- [ ] 6.2 REGRESSION: Run focused/complete host suites, Release/package checks, E2E regression, strict OpenSpec validation, and prove Circinus/DPA/performance-test assemblies never enter product packages.
- [ ] 6.3 REVIEW: Independently review Circinus compatibility, native sampling/instrumentation bias, local-only operation, workload realism, metric units, comparison honesty, DPA diagnostic separation, cleanup, and generic future-mod extensibility; resolve findings and rerun affected benchmarks.
