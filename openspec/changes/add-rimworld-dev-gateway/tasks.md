## 1. mods/RimWorldDevGateway — Project setup

**TDD audit note:** broad unchecked RED boxes below remain intentionally unchecked where a complete per-behavior RED artifact was not retained. Checked GREEN boxes report implemented, passing behavior, not retroactive RED evidence. Exact recovered and current red/green records remain in a local ignored ledger alongside `artifacts/TestResults/tdd/`.

- [x] 1.1 Add the loadable `fumblesneeze.rimworlddevgateway` mod project, pinned EmbedIO 3.5.2 and Mono.CSharp 4.0.0.143 package references, central test project, optional companion host-tool project, solution entries, and package metadata without referencing Immersive Chefs.
- [x] 1.2 Add shared API/manifest DTO contracts and test fixtures for deterministic clocks, tokens, process identities, temporary save-data roots, fake dispatch, and bounded EmbedIO request/response contexts.

## 2. mods/RimWorldDevGateway — TDD RED: Service foundation

- [ ] 2.1 Write failing tests for the EmbedIO adapter's strict `127.0.0.1` binding, rejection of configurable non-loopback addresses, route/method handling, body/header/response limits, and queue backpressure without testing or recreating EmbedIO's internal parser.
- [ ] 2.2 Write failing tests for per-run token generation, constant-time bearer validation, token-redacted logs/errors, atomic live/stale/stopped manifests, and exact-process identity.
- [ ] 2.3 Write failing tests for `/api/v1`, request-ID validation/generation, stable success/error envelopes, unsupported versions, dispatcher deadlines, cancellation, and shutdown cleanup.
- [ ] 2.4 Run only the new service-foundation tests and record that each fails for the intended missing behavior rather than build, discovery, or environment errors.

## 3. mods/RimWorldDevGateway — TDD GREEN: Service foundation

- [x] 3.1 Integrate pinned EmbedIO 3.5.2 as the sole HTTP transport, register the bounded loopback routes and gateway middleware needed to make the transport and limit tests pass, and remove/bypass any hand-written `TcpListener` HTTP parser.
- [x] 3.2 Implement cryptographic per-run sessions, bearer authentication, atomic manifest lifecycle, redaction, and stale-process cleanup needed to make the authentication tests pass.
- [x] 3.3 Implement the version-one envelopes, request correlation, bounded main-thread dispatcher component, timeout states, controlled shutdown, and teardown needed to make the lifecycle tests pass.
- [x] 3.4 Add About/startup/settings/status/indicator danger text and pass tests proving the gateway is developer-only, execution-enabled, and absent from Immersive Chefs dependency metadata.

## 4. mods/RimWorldDevGateway — TDD RED: Observability

- [ ] 4.1 Write failing tests for the exact immutable version-one status/UI fields at the main menu and on a map, including the 128-window/256-selection bounds and safe stable-in-run handles instead of recursive Verse object serialization.
- [ ] 4.2 Write failing tests for sequence/cursor log-ring retrieval, separate `HistoryEvicted`/`PageTruncated` reporting, 500-entry/3 MiB page bounds, severity/stack metadata, request correlation, and token redaction.
- [ ] 4.3 Write failing tests for end-of-frame PNG capture scheduling, metadata, concurrency/size/time bounds, and temporary Unity-resource cleanup seams.
- [ ] 4.4 Run only the new observability tests and record the expected red results.

## 5. mods/RimWorldDevGateway — TDD GREEN: Observability

- [x] 5.1 Implement `/api/v1/status` and `/api/v1/ui-state` snapshot assemblers on the dispatcher and make their contract tests pass.
- [x] 5.2 Implement bounded Unity/RimWorld log capture plus count- and aggregate-byte-bounded `/api/v1/logs` cursor pages and make log tests pass.
- [x] 5.3 Implement end-of-frame screenshot capture and PNG response metadata with disposable-resource seams and make screenshot tests pass.

## 6. mods/RimWorldDevGateway — TDD RED: Input and semantic actions

- [ ] 6.1 Write failing tests for PID/HWND/client/DPI revalidation, click coordinates and event pairs, bounded drag interpolation/release, focus-loss handling, key/chord cleanup, and unsupported backends.
- [ ] 6.2 Write failing tests for discoverable versioned semantic actions, argument validation, main-thread invocation, availability reasons, and pause/speed/cancel/confirm outcomes.
- [ ] 6.3 Run only the new input/action tests and record the expected red results.

## 7. mods/RimWorldDevGateway — TDD GREEN: Input and semantic actions

- [x] 7.1 Implement the process-scoped Windows raw-input adapter, focus/ownership safeguards, event ledgers, and explicit unsupported-platform request result needed to pass input tests without widening status/UI state.
- [x] 7.2 Implement the semantic-action registry and initial pause/speed/cancel/confirm actions needed to pass action tests.

## 8. mods/RimWorldDevGateway — TDD RED: Unrestricted execution

- [ ] 8.1 Write failing tests proving unrestricted execution is enabled with default/no settings and requires no restart switch, allowlist, signature, confirmation, or predeclared automation.
- [ ] 8.2 Write failing raw-endpoint tests for exact `text/plain`/UTF-8 media handling, strict decoding and BOM removal, the 65,536-byte route bound, expression results, bounded value/type/diagnostics, compile errors, state retention after success/error, serialized concurrency, clean-process reset, main-thread affinity, and honest timeout behavior.
- [ ] 8.3 Write failing tests for bounded optional assembly upload, compatible assembly loading, exact public-static entry resolution, main-thread execution context, result normalization, inner-exception errors, cancellation semantics, upload accounting, and session-extension hooks.
- [x] 8.4 Add host-tool tests for isolated .NET Framework fallback compilation against current managed/contract assemblies, bounded diagnostics, unique assembly names, upload correlation, temporary cleanup, and absence of Roslyn from the mod package.
- [ ] 8.5 Run only the new raw-REPL, optional assembly, and compiler tests and record the expected red results.

## 9. mods/RimWorldDevGateway — TDD GREEN: Unrestricted execution

- [x] 9.1 Pin and package Mono.CSharp 4.0.0.143 and complete the always-enabled `/api/v1/executions/csharp` path: exact UTF-8 `text/plain` validation, endpoint/evaluator bounds, one serialized stateful evaluator per run, preloaded loaded-process references, main-thread dispatch, compile diagnostics, stable errors/timeouts, and shutdown-only reset semantics.
- [x] 9.2 Implement the optional `/api/v1/executions/assembly` loader with a 16 MiB per-upload bound, monotonic upload count, and exact public-static entry contract on the main-thread dispatcher, without a hard per-run ceiling.
- [ ] 9.3 Complete optional assembly execution-context cancellation, runtime extension hooks, and normalized bounded results; treat any future upload-count API telemetry as a separately versioned contract rather than widening version-one status implicitly.
- [x] 9.4 Implement the optional companion snippet compiler/uploader command using the installed host .NET SDK and make host-tool tests pass without shipping Roslyn into Unity.

## 10. mods/RimWorldDevGateway — TDD RED: Automations and quickstart

- [ ] 10.1 Write failing tests for registry discovery, versioned schemas, availability, synchronous POST-to-terminal execution, bounded completed progress/artifacts, internal cancellation handling, and idempotency-key replay/conflict, while proving version one exposes no remote list/get/incremental/cancel route.
- [ ] 10.2 Write failing tests for `quickstart.spawn` descriptor validation, all-feasible preflight, bounded clearing, Def resolution, buildings/Stuff/items/pawns/research/power/conditions, stable handles, and mutation ledger.
- [ ] 10.3 Write failing host tests for isolated `-savedatafolder`/`-quicktest` launch, ordered `-AdditionalModIds`, exact-PID manifest readiness, playable-map wait, post-quickstart status/UI/log evidence, failure diagnostics, and unchanged normal `ModsConfig.xml` hash.
- [ ] 10.4 Run only the new automation/quickstart tests and record the expected red results.

## 11. mods/RimWorldDevGateway — TDD GREEN: Automations and quickstart

- [x] 11.1 Implement the automation registry, internal run store/progress/artifact/cancellation mechanics, synchronous terminal POST response, and idempotent terminal replay needed to pass registry/router tests without exposing remote incremental lifecycle routes.
- [x] 11.2 Implement version-one, mod-neutral `quickstart.spawn` validation/preflight and deterministic setup for bounded clearing, buildings/items/pawns, Stuff/quality/power, research, game conditions, handles/positions, camera centering, and mutation ledgers using runtime Def names.
- [x] 11.3 Implement the isolated quickstart host command, validated ordered additional-mod input, readiness/error handling, pre-cursor plus post-quickstart status/UI/log evidence collection, process cleanup, and configuration-hash protection needed to pass host tests.

## 12. mods/RimWorldDevGateway — TDD REFACTOR

- [ ] 12.1 Refactor only with the full focused suite green: deepen transport, session, dispatch, snapshot, input, execution, and automation modules while keeping public contracts stable.
- [ ] 12.2 Add adapter-level malformed-request, unsupported-method, oversized-input, and route-boundary cases without duplicating EmbedIO parser tests, plus race-focused tests for shutdown, timeout, queue saturation, manifest replacement, focus loss, and automation retries.
- [x] 12.3 Run the complete repository test wrapper, verify all gateway and Immersive Chefs tests are discovered, and reject a zero-test result.

## 13. mods/RimWorldDevGateway — Build and package verification

- [x] 13.1 Build Release against the configured RimWorld 1.6 assemblies with zero errors/warnings and verify repeatable incremental and clean outputs.
- [x] 13.2 Inspect the generated mod package for correct About metadata, API/contract assemblies, developer warning, supported version, exact compatible `EmbedIO.dll`, `Swan.Lite.dll`, `System.ValueTuple.dll`, and Mono.CSharp 4.0.0.143 `Mono.CSharp.dll` runtime dependencies, and no Immersive Chefs, optional Workshop, Kestrel/ASP.NET Core, Watson.Lite/Watson.Core/CavemanTcp, Roslyn, test, or host-tool runtime dependency.
- [x] 13.3 Verify a normal build writes only repository artifacts and an explicit deploy writes only the exact `fumblesneeze.rimworlddevgateway` game-mod folder.
- [x] 13.4 Document manifest discovery, exact version-one status/UI schemas, byte-paginated logs, synchronous automation lifecycle, direct curl/PowerShell raw-C# examples, state/lifecycle/limits and C# language compatibility, optional client/assembly fallback, automation descriptor, additional-mod isolated launch, post-quickstart evidence, danger model, cleanup, and extension procedure.

## 14. mods/RimWorldDevGateway — In-game verification

- [x] 14.1 Launch an isolated minimal Core-plus-gateway mod list, prove the exact launched PID reaches the main menu, inspect the danger indicator, and verify Unity Mono loads EmbedIO, Swan.Lite, System.ValueTuple, and Mono.CSharp with no missing-assembly, type-load, missing-method, compiler-initialization, dependency-resolution, or gateway initialization errors.
- [ ] 14.2 Discover the per-run manifest and prove an authenticated status request traverses the real in-game EmbedIO server before proving exact status/UI fields, cursor logs with separate eviction/page-truncation semantics, rejected missing/stale tokens, request correlation, valid PNG capture, and raw/semantic input against the exact RimWorld window.
- [ ] 14.3 Post raw `text/plain` C# directly while the game remains running, prove main-thread execution, compile diagnostics, bounded results, and state retention across submissions, and perform a reversible game-state inspection/mutation without a companion client; then separately smoke the optional compiled-assembly fallback and session-scoped automation registration without a restart.
- [x] 14.4 Launch isolated `-quicktest` with optional explicit additional package IDs, run `quickstart.spawn` for named buildings and Stuff/items on a playable map, and verify returned handles/positions, post-quickstart status/UI/log evidence, visible screenshot, and an idempotent terminal replay.
- [x] 14.5 Invoke controlled shutdown, verify the listener and live credential locator disappear and the stopped record has no token, then verify game-process/service cleanup and an unchanged normal `ModsConfig.xml` hash.
- [x] 14.6 Launch Immersive Chefs without the gateway and the gateway without Immersive Chefs to prove both dependency boundaries in-game, and retain the two evidence bundles.

## 15. mods/RimWorldDevGateway — TDD RED: Semantic game control and inspection

- [x] 15.1 Add one failing public-interface test for dedicated game-state get/set, including developer/god-mode invariants and native pause/speed values; run it and record the intended RED.
- [x] 15.2 Add one failing public-interface test for absolute camera movement/zoom and stale-map rejection; run it and record the intended RED.
- [x] 15.3 Add failing public-interface tests incrementally for bounded map/view thing filtering, stable pagination, bounded inspection, and atomic multi-selection; record each intended RED before its implementation.
- [x] 15.4 Add failing public-interface tests incrementally for debug-action discovery/invocation and all-feasible idempotent thing/pawn spawning; record each intended RED before its implementation.
- [x] 15.5 Add failing public-interface tests incrementally for gizmo/designator discovery, stale-handle rejection, immediate/toggle invocation, typed target/placement/drag interactions, preflight, and cancellation; record each intended RED before its implementation.

## 16. mods/RimWorldDevGateway — TDD GREEN: Semantic game control and inspection

- [x] 16.1 Implement the dedicated game-state routes and Verse adapter for developer/god mode, pause, and speed, preserving the narrow status/UI contracts.
- [x] 16.2 Implement absolute map-scoped camera center/zoom control and view-state reporting through the native camera driver.
- [x] 16.3 Implement current-map/view thing query handles, filters, bounds/pagination, bounded inspection, and atomic selection operations.
- [x] 16.4 Implement debug-action tree discovery, stable re-resolution, immediate invocation, and exact native map/pawn/world tool activation with an explicit process-scoped pointer requirement.
- [x] 16.5 Implement preflighted, idempotent direct spawning for things and pawns with bounded mutation ledgers.
- [x] 16.6 Implement selected/owned gizmo and architect-designator discovery, fingerprinted handles, immediate/toggle invocation, and the single typed target/placement/drag interaction lifecycle.

## 17. mods/RimWorldDevGateway — Documentation and regression verification

- [x] 17.1 Document every new route, request/response example, handle lifetime, query/filter bound, dev/god invariant, camera semantics, spawn idempotency, gizmo classification, interaction shapes, unsupported fallback, and direct-HTTP workflow.
- [x] 17.2 Run focused tests after every green slice, then the owning suite and repository wrapper with nonzero discovery; build Release with zero warnings/errors and inspect the packaged dependency graph.
- [ ] 17.3 Extend the isolated gateway smoke to exercise and retain correlated game-state, camera, thing query/inspection, selection, spawn, immediate/toggle gizmo, and draggable designator evidence while leaving the normal ModsConfig hash unchanged.

## 18. mods/RimWorldDevGateway — In-game semantic-control acceptance

- [ ] 18.1 In one disposable isolated playable run, get/set/restore developer and god modes, pause/speed, and camera; prove reported before/after state matches visible/native state.
- [ ] 18.2 Spawn a thing and pawn, query them by map and view filters, inspect them, atomically select them, and prove stable handles/results through direct HTTP.
- [ ] 18.3 Discover and invoke one real immediate or toggle gizmo, one native debug action/tool, and one target or dragged architect designator; prove stale/disabled safety and retain screenshots/log/request IDs.
- [ ] 18.4 Perform controlled shutdown, scan logs for gateway/mod/runtime exceptions, verify credentials and exact PID are cleaned up, and verify the normal mod configuration hash is unchanged.
- [x] 18.5 Run an independent code-review pass over the complete change, resolve or explicitly defer every finding, and rerun all affected tests plus Release/package/OpenSpec validation.

## 19. mods/RimWorldDevGateway — Freeze diagnosis and robust harness cleanup

- [x] 19.1 RED/GREEN: prove the default frame drain starts one operation, then cap both dispatcher/runtime defaults and skip new drains after shutdown is requested.
- [x] 19.2 RED/GREEN: observe and request-correlate late post-timeout faults, and preserve/log the native debug-discovery causal exception.
- [x] 19.3 RED/GREEN: write token-free per-run started/terminal request journals with an atomic last-request record.
- [x] 19.4 Change the host smoke to journal outbound requests, classify exit versus suspected hang, request graceful close first, and attempt a dump before exact-PID force fallback.
- [ ] 19.5 On a future explicitly authorized live run, prove a real off-center camera pan and the new graceful cleanup/diagnostic artifacts; do not launch solely to close this evidence gap after the user's stop request.
- [x] 19.6 TDD/REGRESSION: keep timed native drag delays off Unity's main thread, carry HTTP-client cancellation into queued operations, stop dispatcher admission before joining transport, log post-start timeouts immediately, and keep the extended debug deadline on discovery only. Durable RED artifacts exist for most slices; the original cancellation compile failure is retained only in the development transcript.
- [x] 19.7 REGRESSION: isolate failing modded gizmo/thing metadata, preserve causal exceptions, emit correlated semantic/automation/gizmo diagnostics, and retain healthy query results. The object-isolation development observations are transcript-only; retained GREEN regressions and the correlated gizmo-mapping RED/GREEN artifact cover the current behavior.
- [x] 19.8 RED/GREEN: serialize click, drag, chord, and text through one off-main-thread cancellation-aware input lane and retain that lane through guaranteed best-effort input release.
- [x] 19.9 RED/GREEN: retain and retry listener, runtime, and session ownership when shutdown cleanup fails instead of committing a false stopped state or blocking recovery with an orphaned locator.
- [x] 19.10 RED/GREEN: keep `last-request.json` on the newest active admission under concurrent completion and isolate/log broken selected-Thing and gizmo-owner enumeration while retaining healthy results.
- [ ] 19.11 RED/GREEN: keep transport-worker request diagnostics out of `Verse.Log`, append them directly to the gateway ring buffer with correlation, and prove an open developer-log window remains responsive during HTTP traffic.

## 20. mods/RimWorldDevGateway — Finalized Def export

- [ ] 20.1 RED: add focused public-contract tests for exact filter intersection, deterministic pagination, retained provenance, nested Def references, cycles, broken fields/collections, fixed projection bounds, main-thread routing, and explicit XML rejection; retain the intended failing evidence.
- [ ] 20.2 GREEN: implement the bounded finalized-Def JSON source/projector and `POST /api/v1/defs/export`, making the focused tests pass without property getters, recursive live serialization, or a second XML/Def loader.
- [ ] 20.3 REFACTOR/REGRESSION: document direct HTTP download examples and the `DirectXmlSaver` limitation, run the owning/repository suites, and inspect the package for no new runtime dependency.

## 21. mods/RimWorldDevGateway — In-game integration-test contract and runner

- [ ] 21.1 RED: add focused tests for the host-safe attribute/assertion contract, valid and invalid method discovery, deterministic order, lifecycle filtering, once-per-process execution, main-thread affinity, isolated assertion/target/load failures, immutable bounded results, and disabled-by-default behavior; retain the intended failing evidence.
- [ ] 21.2 GREEN: implement `RimWorldDevGateway.IntegrationTesting`, active-mod `DevIntegrationTests/*.IntegrationTests.dll` discovery, byte loading/dependency resolution, the startup flag, lifecycle coordinator, exception-isolated runner, and `GET /api/v1/integration-tests`.
- [ ] 21.3 GREEN: add an in-game fixture assembly that proves a real Core Def and one real XML PatchOperation result at `MainMenuLoaded`, without registering it with ordinary NUnit/VSTest runs or packaging it in a product mod.
- [ ] 21.4 REVIEW HARDENING: keep assembly loading/reflection and artifact serialization/I/O on separately polled single background lanes; reject async-void tests from metadata, count failed loads toward admission, retry the exact uncommitted snapshot without invocation/rerun, expose only durable endpoint state, and format exceptions without arbitrary virtual text. Retain focused blocking-worker, retry/identity, pending-endpoint, bound, and hostile-object evidence before checking this task.

## 22. mods/RimWorldDevGateway — Integration-test build and launch tooling

- [ ] 22.1 RED/GREEN: add host tests for discovery of only `RimWorldInGameIntegrationTest=true` projects, explicit active-package filtering, deterministic build/stage paths, stale-stage cleanup limited to exact owned directories, build failures, and zero-project rejection when tests were requested.
- [ ] 22.2 Implement the host staging command and an explicit Gateway smoke switch that builds/stages matching projects, passes `-devGatewayRunIntegrationTests`, waits for terminal main-menu results, and retains token-free Def export and integration-test artifacts for the exact PID/build/mod list.
- [ ] 22.3 Document how a product or compatibility test project references live RimWorld/mod/Harmony assemblies, writes Def and patch assertions, and selects exact ordered mod lists for version/update matrices.

## 23. mods/RimWorldDevGateway — Review and verification

- [ ] 23.1 Run focused tests after every green slice, then the owning suite and guarded repository wrapper with nonzero discovery; build Release and run strict OpenSpec validation.
- [ ] 23.2 Run an independent code-review pass over Def export, test loading/execution, lifecycle wiring, and staging security/cleanup; resolve every accepted finding and rerun affected checks.
- [ ] 23.3 In a fresh isolated Core-plus-Gateway process with the startup flag, observe the settled main menu, retrieve a known finalized Core Def export, and prove the staged main-menu fixture reports the expected real XML-patched value without gateway/runtime/test exceptions.
- [ ] 23.4 In a separate isolated playable-map run, prove a `PlayableMapLoaded` fixture executes once and the Gateway stays responsive after one deliberately failing fixture; retain screenshots and exact-process evidence. Integration assertions remain supporting evidence and do not waive player-workflow acceptance for gameplay features.

## 24. mods/RimWorldDevGateway — Explicit verification scenarios

- [x] 24.1 RED/GREEN: prove quicktest resolves to no scenario by default, reserves the existing comprehensive surface battery behind explicit `gateway-regression`, and resolves named descriptors deterministically.
- [x] 24.2 Keep default quicktest free of temporary spawning, selection, debug/gizmo/designator, camera, REPL dev-mode, FlaUI, and raw-input mutations; retain those probes behind the explicit Gateway regression scenario.
- [x] 24.3 Add the version-one host descriptor runner with required-package validation, descriptor-local raw C# sources, per-step artifacts, and exact-process screenshots.
- [x] 24.4 Run and observe the `immersive-chefs-caravan-dining` scenario with Immersive Chefs loaded, confirm the native Items scene hides the embedded plate before eating and shows that exact plate after deliberate unpausing/ingestion, and retain the behavior evidence.
- [x] 24.5 Resolve the focused runner review: retain non-mutating Mono.CSharp/live-mod health in every smoke mode, validate scenario requirements before dry-run and against the live loaded set, reject colliding/reserved screenshot names, and rescan the flushed Player log after scenario/hold and exact-PID shutdown.
- [x] 24.6 Resolve the final re-review by making a missing final Player log fatal, clearing generated pawn inventory for repeatable scenes, naming source-shape tests honestly, rerunning the full Gateway matrix, and repeating the native before/action/after observation with clean exact-PID cleanup.
- [x] 24.7 Add and observe the explicit animal caravan exclusion scenario: meal and untouched cutlery before, returned plate and untouched cutlery after native ingestion, with loaded integration evidence for exact identity, unchanged sanitation, and no custom poisoning.
- [x] 24.8 Wait for a playable quicktest map before invoking main-thread Def export or later verification, preventing startup contention from timing out the export queue.
- [x] 24.9 Add and observe an explicit regular-map animal exclusion scenario: native animal ingestion consumes the plated meal, returns the clean embedded plate at the eating location, and leaves adjacent cutlery untouched.

## 25. mods/RimWorldDevGateway — Screen-local input and speed observability

- [x] 25.1 SPEC/AUDIT: Confirm the existing process-scoped click, drag, key/chord/text, and `GET/POST /api/v1/game-state` speed-control routes; specify the rendered client coordinate space and UI-state speed projection rather than introducing duplicate endpoints.
- [x] 25.2 TDD RED/GREEN: Make the UI-state contract require nullable pause/native speed and positive rendered-client dimensions with a top-left origin; retain main-menu null safety and the existing bounded snapshot fields.
- [x] 25.3 REGRESSION: Prove the existing screen-local mouse and keyboard routes still validate PID/HWND/bounds, serialize input, and return exact event ledgers; run the full Gateway matrix and strict OpenSpec validation.
- [x] 25.4 REVIEW: Independently review the UI-state/input slice for coordinate-space truth, main-thread safety, stale state, contract drift, and unnecessary endpoint duplication; resolve findings and rerun affected verification.
- [x] 25.5 IN-GAME: In one isolated playable run, read rendered-client dimensions, use Gateway keyboard input to change pause state, use Gateway screen-local click input on a visible native control, and observe the resulting pause/speed and UI change through Gateway UI state plus retained rendered screenshots.
- [x] 25.6 TDD RED/GREEN: Reproduce a transient Windows lock on the owned run manifest during stopped-tombstone replacement, retry for a short bounded interval, and retain fail-closed retryable ownership for a persistent lock.
- [x] 25.7 REGRESSION/REVIEW: Run focused and full Gateway tests, independently review the shutdown hardening, and repeat the rejected current-build cutlery-item observation through a clean exact-PID shutdown.
- [x] 25.8 TDD/IN-GAME: Keep a Windows foreground input-queue lease through each serialized operation, retry one pre-button transient click focus loss, fail closed afterward, and observe a real RimWorld right-click float menu plus selection changes through screen-local Gateway clicks.

## 26. mods/RimWorldDevGateway — Unobtrusive background launch

- [x] 26.1 TDD RED/GREEN: prove every isolated Gateway launch writes `Prefs.xml` with `runInBackground=True` and `volumeMusic=0`, and selects a minimized window by default.
- [x] 26.2 TDD RED/GREEN: add an explicit visible-window override and automatically select it for the desktop-owning `gateway-regression` scenario.
- [x] 26.3 REGRESSION/REVIEW: document the launch contract, run the full Gateway suite and strict OpenSpec validation, and resolve an independent review.
- [x] 26.4 IN-GAME: on the reviewed build, start fresh isolated runs and natively observe the exact owned window minimized by default, normal with explicit `-VisibleWindow`, and normal through automatic `gateway-regression` selection; prove the Gateway remains responsive while the minimized RimWorld process advances under the isolated background preference, then open native Options and visibly observe the Music slider at zero.
- [x] 26.5 TDD/LAUNCH: Perform no exact-PID window-state probe after launch; do not compare, classify, warn on, or enforce a user restore or maximize.
- [x] 26.6 TDD/PERSISTENCE: Reproduce a destination snapshot held by a temporary Windows reader and retry its atomic replacement on the background persistence task without surfacing a false in-game failure.
