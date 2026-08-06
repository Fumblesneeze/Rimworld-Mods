## 1. shared and host tooling — TDD contract/discovery

- [x] 1.1 TDD RED: Add failing host-safe contract tests for attributed concrete test types, stable IDs, complete ordered package sets, non-Gateway owner membership, implicit Gateway-owned self-tests, Gateway exclusion, deadlines, iterator signatures, assertion failures, and invalid declarations.
- [x] 1.2 TDD GREEN: Implement `RimWorldDevGateway.EndToEndTesting` with attributes, interfaces, typed steps, context/assertion contracts, result states, and no Unity/RimWorld dependency.
- [x] 1.3 TDD RED: Add failing metadata-discovery tests for marked project discovery, compiled attribute extraction without execution, exact grouping, duplicates, package resolution, owner/output drift, zero selection, and deterministic ordering.
- [x] 1.4 TDD GREEN: Implement the build/metadata descriptor/staging tool and atomic marker-owned `DevEndToEndTests` publication plan.

## 2. mods/RimWorldDevGateway — TDD runtime loading and execution

- [x] 2.1 TDD RED: Add failing runtime tests for disabled-by-default discovery, startup-flag gating, active-mod-only bundle validation, assembly identity/hash/attribute matching, exact package-set filtering, and isolated load/reflection failures.
- [x] 2.2 TDD GREEN: Implement dynamic bundle discovery/loading and durable `GET /api/v1/end-to-end-tests` discovery state without loading mismatched or unrequested assemblies.
- [x] 2.3 TDD RED: Add failing state-machine tests for Arrange, one-ready-transition-per-frame iterator execution, action/wait/observe step ordering, predicate/deadline behavior, assertion/exception isolation, incremental persistence, and process-abort synthesis.
- [x] 2.4 TDD GREEN: Implement the multi-frame E2E coordinator, typed native gizmo/float-menu/time/selection/camera/input actions, screenshots/checkpoints, durable results, and bounded diagnostics.
- [x] 2.5 TDD RED: Add failing contract/adapter tests for exact-Thing native trade adjustment, native Accept callback dispatch, invalid/ambiguous/out-of-range failures, and separation from the foreground Windows-input backend.
- [x] 2.6 TDD GREEN: Implement the typed trade-dialog adapter and migrate native trade E2E actions to it so minimized runs do not restore, focus, or maximize RimWorld.
- [x] 2.7 IN-GAME: Rerun the native orbital trade workflow from a minimized launch, observe the transfer and Accept actions take effect, inspect the delivered plate, and retain evidence that the exact owned process stayed minimized throughout both actions.
- [x] 2.8 TDD/E2E: Reproduce the countertop fixture's map-wide synchronous search blocking Unity long enough for Windows to restore/ghost the minimized process, replace it with a bounded deterministic candidate search, and prove the exact group remains minimized without weakening later user window control.
- [x] 2.9 TDD RED/GREEN: Add a host-safe exact-world-object settlement-trade step and a fail-closed RimWorld adapter that invokes the enabled native caravan visit trade command without translated selectors or desktop input.
- [x] 2.10 E2E/IN-GAME: Open native settlement trade from a real player caravan, purchase upstream-generated plated meal stock through typed dialog actions, and observe the exact physical plate transfer once while the process remains minimized.
- [x] 2.11 TDD/E2E: Wait for native player control before destructive isolation, bound the readiness wait, retain token-safe exact Gateway control exceptions, reject a non-preemptible generic callback step, and prove a quickstart cooking workflow no longer races the pause request.
- [x] 2.12 TDD/E2E: Add a host-safe exact-IncidentDef action with optional exact faction load ID, invoke the real native incident worker with forced current-map parameters, fail closed on missing or rejected shapes, and prove a native trader-caravan arrival reaches the loaded compatibility postfix and visible map/letter outcome.
- [x] 2.13 TDD: Project the already-captured native toggle state and exact hotkey Def name through the host-safe E2E gizmo catalog while preserving the original public constructor for prebuilt bundle compatibility.
- [x] 2.14 TDD/E2E: Add a host-safe exact-window confirmation step with a private fail-closed adapter registry, preserve the existing public native-action/backend interfaces, and confirm Replimat's native survival-batch dialog in a minimized run without exposing callback fields to test bundles.

## 3. mods/RimWorldDevGateway — TDD destructive isolation

- [ ] 3.1 TDD RED: Add failing reset-plan tests for spawned Things/Pawns, jobs, zones, designations, interactions, windows, selection, world-fixture ledgers, input release, camera/control restoration, cleanup callbacks, and post-reset verification.
- [x] 3.2 TDD GREEN: Implement pre/post-test disposable-map reset and process tainting so ordinary test failures continue only after a verified empty baseline.
- [x] 3.2a TDD RED/GREEN: Remove constructed roofs and overhead mountain before any map content, include roofs in empty-baseline verification, and prove the exact sequential group has no roof-collapse or leaked-death evidence.
- [x] 3.3 REFACTOR: Keep test orchestration, native action adapters, reset ownership, persistence, and HTTP projection behind narrow modules shared with existing Gateway services rather than duplicate implementations.

## 4. host runner — CLI TDD and orchestration

- [x] 4.1 TDD RED: Add a real dry-run helper and failing CLI tests for discovery, group/test filters, one process per exact group, deterministic command plans, table/JSON output, JUnit/aggregate artifacts, exit codes 0/1/2, zero selection, failure continuation, and guaranteed cleanup.
- [x] 4.2 TDD GREEN: Implement `scripts/Invoke-RimWorldEndToEndTests.ps1` by composing the existing exact-PID isolated launcher and E2E staging/polling contracts.
- [x] 4.2a TDD/IN-GAME: Add exact stable `-TestId` selection from host planning through restart-bound Gateway admission/execution, reject missing or additional IDs, and prove one selected test runs alone inside a multi-test exact mod group.
- [x] 4.3 BUILD: Add E2E projects to repository build/discovery without registering their methods with NUnit/VSTest; reject every E2E/Gateway reference from ordinary product packages and ignore generated stages/results.

## 5. tests/ImmersiveChefs.EndToEndTests — tracer and scenario migration

- [ ] 5.1 TDD RED: Add the adverse-meal attributed tracer requiring exact Core/Harmony/Immersive Chefs, passive awful/frozen/dirty arrangement, native Undraft/time action, ordinary ingestion, dirty exact ware return, visible thoughts, deterministic food poisoning, and before/action/after screenshots.
- [ ] 5.2 TDD GREEN: Make the reviewed Gateway/test/product implementation pass the tracer without direct job assignment, ticking, memory injection, hediff injection, or synthetic ingestion.
- [ ] 5.3 INVENTORY: Classify every existing `scripts/Scenarios` descriptor as pending E2E migration, converted, interactive-only, or retired, naming its exact mod group and observable player workflow.
- [ ] 5.4 MIGRATE: Convert base kitchenware/cooking/dining/temperature/preparation/sanitation scenarios into sequential E2E tests and delete duplicate automation only after equivalent evidence passes.
- [ ] 5.5 MIGRATE: Convert Processor/Dubs, Gastronomy/Hospitality/Common Sense, VNPE/variety/material, DLC, caravan, patient/child/animal, save/load, trade, and other optional-mod scenarios into their exact E2E groups.

## 6. docs and verification — review and live acceptance

- [x] 6.1 DOCUMENT: Update `AGENTS.md`, README, `docs/TestingEnvironments.md`, `docs/Gateway.md`, development docs, repo-local RimWorld skill, and CLI examples with the distinct host/integration/E2E/interactive/performance tiers and migration procedure.
- [ ] 6.2 REGRESSION: Run focused tests, full Gateway and repository suites with nonzero discovery, Release/package inspection, and strict OpenSpec validation.
- [ ] 6.3 REVIEW: Independently review contract honesty, metadata/staging safety, multi-frame/thread ownership, reset completeness, native-action fidelity, failure isolation, CLI contracts, package boundaries, and KISS; resolve findings and rerun affected gates.
- [ ] 6.4 IN-GAME: On the reviewed build, run at least two exact mod groups in one host invocation, personally inspect retained native before/action/after behavior for the adverse-meal tracer and one optional-mod workflow, and prove sequential cleanup, controlled exact-PID shutdown, stage/credential removal, final log scans, and unchanged normal configuration hashes.
