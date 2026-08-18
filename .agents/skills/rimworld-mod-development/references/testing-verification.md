# Test and in-game verification workflow

## Fast tests

Use the exact owning test project for the red-green loop. During ordinary feature work, stop after the focused test unless shared behavior requires the owning suite. Reserve the repository wrapper for an explicit maintenance, regression, or release checkpoint:

```powershell
.\scripts\Invoke-Tests.ps1 -Suite <exact-suite> -Configuration Release -TestFilter "FullyQualifiedName~<focused-test>"
.\scripts\Invoke-Tests.ps1 -Suite <exact-suite> -Configuration Release
.\scripts\Invoke-Tests.ps1
```

For a red-green slice in a registered project, filter its exact wrapper suite and rerun that same focused selection after implementation or an affecting review fix. Rerun the exact owning suite only when the slice changes shared behavior across it. Do not replay completed scenarios or invoke the guarded repository/full E2E suite merely because another feature changed. A raw `dotnet test --filter` is acceptable only for an initial compile-time RED or with an explicit inspection of the executed count; VSTest can exit zero for zero matches. A command that reports zero executed tests is a failure even when tests were discovered/ignored and the underlying runner exits zero. Wrapper filters require one exact suite; grouped runs execute every selected project and aggregate failures. Keep deterministic seams for clocks, random tokens, process identity, filesystem roots, dispatch phases, and optional-mod catalogs.

The Zlepper testing SDK only references game assemblies; it does not load mods, apply Harmony, or load Def XML. Keep ordinary, explicit-Harmony, and DefDatabase tests in separate projects/processes. Simulate package presence through the public package-ID boundary; use constructed lightweight `Def` fixtures only in an initially empty isolated Def database; use actual RimWorld for populated/Core/Workshop databases, `ThingDef`/`RecipeDef`, XML inheritance/cross-references/PatchOperations, or another mod's real lifecycle. Read [the host-test decision table](../../../../docs/TestingEnvironments.md) before adding such a test.

Choose the environment from what the assertion genuinely needs:

| Need | Honest environment |
| --- | --- |
| Pure policy/calculation/package-ID selection | `ImmersiveChefs.Unit`; no loaded mods, Harmony, or representative populated Def database |
| One explicit Harmony target/patch | `ImmersiveChefs.Harmony`; acquire only that owner/patch and remove it in guaranteed cleanup |
| Lightweight Def behavior | `ImmersiveChefs.Defs`; construct real `Def` objects and register all fixtures in one initially empty scoped database |
| Core/Workshop Defs, XML inheritance, PatchOperations, `DefOf`, mod constructors, or full patch set | Fresh exact RimWorld process with the owner and complete ordered mod list |

Never mutate `LoadedModManager` to pretend a mod is installed. Def fixtures are real constructed Defs,
not XML-loaded content and not mocks; an assertion that depends on the actual loaded Def must run in
game.

## Startup-gated in-game integration tests

Use a separate assembly when an assertion needs RimWorld's real loader rather than a player action: finalized Def values, XML inheritance and PatchOperations, active-mod matrices, `DefOf`, or the complete set of startup Harmony patches. Do not add that assembly to an ordinary NUnit project or a mod's `Assemblies/` folder.

1. Create a host-safe class library whose output name ends `.IntegrationTests.dll`.
2. Set `RimWorldInGameIntegrationTest=true` and one `RimWorldIntegrationTestOwnerPackageId` in its project.
3. Add a sibling `<assembly-base>.integrationtests.json` manifest with the exact owner and assembly filename. Use required/forbidden constraints for a reusable fixture, or `activePackageSetMode: "exact"` plus the complete ordered `activePackageIds` sequence for one product matrix; do not mix nonempty constraints into exact mode.
4. Write public static parameterless `void` methods using `[IntegrationTest(RunAt.MainMenuLoaded)]` or, only when necessary, `PlayableMapLoaded`. Throw through `IntegrationAssert` on failure; do not reference NUnit/VSTest.
5. Build and stage with `scripts/Build-InGameIntegrationTests.ps1 -ActivePackageIds <complete-active-package-sequence> -RimWorldPath <game> -SteamModContentFolder <workshop>`; the mandatory nonempty sequence includes Core and every selected mod in load order, not just the test owner.
6. Launch `scripts/Invoke-GatewaySmoke.ps1 -RunIntegrationTests` with the complete `-AdditionalModIds`, the owning repository project in `-AdditionalModProjectPaths`, and an exact `owner|RunAt|Fully.Qualified.TestName` in `-ExpectedIntegrationTests`; retain `integration-tests.json`, the session-persisted copy, Player.log, exact PID/mod list, product DLL/About/XML hashes, and the mandatory exact-process `gateway-screenshot.png`. Treat `main-menu.png` as an independent FlaUI supplement only when `flaui-evidence.json` reports `completed`; never copy or relabel the Gateway image as FlaUI evidence.

Without the exact `-devGatewayRunIntegrationTests` startup flag, the Gateway must not enumerate manifests or load test assemblies. The game process never builds tests. The host requires the complete active sequence, validates the strict source manifest before building, and rejects output drift. Exact mode rejects missing, extra, or reordered packages both before build/stage and again against the real active order before the runtime exposes the assembly for loading. The game incrementally discovers only validated bundles below active mods' version-resolved `DevIntegrationTests` folders and runs at most one test per frame after a stable lifecycle boundary. Every staged assembly must contribute a same-owner, exact-identity descriptor. Rotate explicit ordered mod lists to cover supported dependency matrices, including an inverse run proving a product assembly is absent when its owner is not active.

Use `POST /api/v1/defs/export` to retain a bounded deterministic JSON view of finalized Defs for patch authoring. Treat it as runtime diagnostic data, not canonical source XML: private/finalized fields, repeated references, truncation markers, and projection warnings are possible. Request exact Def types/names/package IDs plus only the exact top-level `fieldNames` needed (for example `label`, `statBases`, and `modExtensions`), and follow `NextCursor` until `Truncated` is false. A byte-bounded page can contain fewer items than `pageSize`; that continuation is expected, not an export failure. Retain source warnings, missing-field warnings, the exact ordered mod list, and the response alongside the XML-patch integration assertion.

## Grouped multi-frame E2E tests

Use a separately marked E2E project for a workflow that needs ordinary game frames, native actions, pawn jobs, waits, and player-visible observations. Set `RimWorldEndToEndTest=true` and one `RimWorldEndToEndTestOwnerPackageId`; keep the assembly outside product `Assemblies` and NUnit/VSTest. Each attributed test declares its globally stable ID, owner, complete ordered non-Gateway package set, and deadlines.

```powershell
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TimeoutSeconds 300 -Output table
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId <stable-group-id> -Output json
```

The runner discovers downloaded package IDs, deploys repo-owned products before marker-owned staging, appends Gateway last, launches one fresh isolated process per exact group, and runs same-group tests sequentially. Its runtime reset removes every destroyable disposable Thing/Pawn plus zones, designations, selection, interactions, and test windows, then clears and verifies live messages, visible and delayed letters, and active alert-readout entries; permanent non-destroyable map features remain environment. A failed reset taints the process and skips later tests. The host persists aggregate JSON/JUnit and screenshots, shuts down the exact PID, sanitizes credentials, and cleans only its exact lease in `finally`.

Before the ordinary reset, remove all roofs, including overhead mountain, through the scenario-owned
test seam; then clear disposable state and alerts. For a plain quickstart, pass `-Quicktest` and omit
`-Scenario`; the resulting evidence label `none` is not a literal scenario argument. Fixture setup and
mutation occur only after the attributed test starts—never as implicit startup behavior.

The grouped runner passes each child's exact additional-mod order through a UTF-8 package-ID file and uses a short `smoke-NNN` runtime branch; do not replace either with repeated cross-process PowerShell array parameters or a descriptive save-data path that can exceed Mono's Windows file boundary. Retained stderr stays raw, while JUnit replaces only XML-invalid characters.

For a native right-click order, use `context.GetRequiredService<IEndToEndFloatMenuCatalog>()`, call `Query(actorRuntimeId, targetRuntimeId)` on the E2E execution thread, and choose exactly one enabled option by its visible label. Feed the returned `StableId` to `FloatMenuActionStep`. Never duplicate the stable-ID hash or directly start the job that the native option would create.

For a scenario that must cross RimWorld's incident boundary, use `IncidentActionStep` with one exact loaded `IncidentDef` name and, when required, the exact live faction load ID. The Gateway builds forced current-map storyteller parameters and invokes the real incident worker. Do not replace it with a generic callback, a guessed storyteller queue entry, translated labels, or synthetic result construction; retain a later wait and screenshot of the ordinary map/UI outcome.

Prefer action → wait → observation steps that exercise the same native path a player uses. Direct arrangement may create preconditions but must not create the claimed result. Assert the admitted native action, its causally related observable outcome, and exact-process cleanup. Inspect the exact native screenshots yourself and describe the visible causal result for new or materially changed player-visible scenarios; unchanged regressions rely on deterministic assertions and an impact map proving stable relevant inputs. Endpoint state, logs, and synthetic/direct-state assertions do not independently satisfy acceptance.

### E2E authoring invariants learned from live failures

- Budget every group honestly. Its frame, game-tick, and wall-time caps must exceed the sum of all
  sequential per-step maxima plus bounded action/screenshot/assertion/save-load overhead. A combined
  group must cover every admitted case, not one case's deadline multiplied by hope.
- Keep later cases inert. Arrange or activate hunger, bills, jobs, and other autonomous triggers only
  when that iterator begins; zero work priorities do not disable ingest/think-tree behavior.
- Assert physical units with `stackCount` across spawned, carried, inventory, holder, and embedded
  state. Counting Thing objects misses duplicates merged into a stack. When exact fixtures matter,
  also assert each returned Thing identity and `stackCount == 1`.
- Exercise the real engine seam. Calling `meal.Graphic.GetType()` proves a CLR owner, not ingredient
  graphic selection; invoke `MatSingleFor`/the actual selected Graphic method. Starting a Job directly
  proves less than choosing its native float-menu/gizmo/bill action.
- At stable checkpoints assert all relevant persistent state. Save/load tests compare exact Thing IDs,
  stack counts, ingredients, every serving/component field, sanitation, selection/graphic provenance,
  and holder ownership—not one representative field.
- If time legitimately evolves state during a persistence-only fixture, disable that subsystem through
  the narrow product setting, register restoration before seeding state, and do not claim the fixture
  proves the disabled behavior.
- Preserve the primary failure during cleanup. Attempt destruction/restoration for each fixture
  independently; aggregate cleanup-only failures without masking an existing assertion exception.
- Frame related actors/Things with the camera, then select only the evidence target before a full-frame
  screenshot. RimWorld multi-selection hides the ordinary single-Thing inspect pane.
- Wait for `Game.PlayerHasControl` and a valid player faction before incidents/jobs/reset. Eliminate
  `Could not find player faction` sequencing errors; a green assertion with an error-severity log is
  not acceptable evidence.
- After any runtime/asset-affecting edit, rebuild and rerun affected live groups. Evidence whose product
  DLL hash predates the final reviewed package cannot close the task.

## Package checks

Build Release against `DefaultRimWorldPath` and inspect `artifacts/Mods/<package-id>/1.6`. Confirm:

- exact supported version and dependency metadata;
- expected assemblies/assets and no test/host/compiler DLLs;
- no product-mod dependency on the Dev Gateway: retain every package file by ordinal relative path/length/SHA-256, reject every case-insensitive `RimWorldDevGateway*.dll` filename, fail closed on metadata-bearing DLLs without an assembly definition, and inspect and retain every managed DLL's ECMA-335 AssemblyRef row without loading it to reject any `RimWorldDevGateway*` reference;
- intentionally bundled third-party DLLs are present and version-pinned; optional-mod, RimWorld, Unity, and Harmony-provider DLLs are not copied into the artifact;
- build has zero warnings and errors.

Canonical repository-only product build command:

```powershell
dotnet build <owning-production-project.csproj> -c Release -p:RimWorldPath="<absolute-game-path>" -p:RimWorldManagedPath="<absolute-managed-path>" -p:SteamModContentFolder="<absolute-workshop-path>"
```

Inspect repository output under `artifacts/Mods/<package-id>` after this command. Deploy only for a deliberate isolated smoke or explicit live-game deployment by adding `-p:DeployToGame=true`; that opt-in stages only the exact package-ID folder under the configured RimWorld `Mods` directory. `-savedatafolder` isolates configuration and saves but does not stage a mod.

## Isolated RimWorld checks

Always launch with a per-run `-savedatafolder` containing its own `Config/ModsConfig.xml`, plus a dedicated `-logFile`. Hash the normal ModsConfig before and after. Refuse to launch if any RimWorld process is already open; acceptance must use a fresh isolated process owned by the current run.

Use separate minimal runs that prove each boundary:

- external-package-absence run: Core, required libraries, owning mod;
- external-package-present run: the absence list plus the external mod and its actual dependencies;
- gateway-assisted integration run: the integration list plus Dev Gateway, used only for disposable automation;
- gateway self-test run: Core plus Dev Gateway.

Use Dev Gateway control by default. Run a product-only duplicate only when explicitly requested or when a recorded Gateway capability gap prevents faithful control or observation. For every impacted claimed compatibility strategy, run an installed version that actually selects that strategy; never claim a runtime fallback that was only inferred or host-tested.

Wait for the exact launched PID, not any window with the same title. Every run SHALL retain Player.log, ModsConfig, screenshots where applicable, the build log, and hashes under one evidence directory. A gateway-assisted run SHALL additionally retain the credential-free stopped session tombstone, API request IDs, and JSON responses. A product-only run cannot produce gateway artifacts and MUST NOT be marked incomplete for their absence. If the user stops or forbids a profile, record the directive, exact-process shutdown, and zero-process check; keep that profile disabled until fresh explicit authorization. Never retain a live `current.json` manifest or bearer token in durable evidence.

## Player-behavior acceptance

Run impact-selected live acceptance after independent review and all accepted fixes. Every selected scenario must perform an actual player action—or faithful automation of the same native UI, gizmo, bill, job, designation, hauling, ingestion, construction, selection, or game-command path—and assert its causally related observable result. The acting agent personally observes the result in the running game or exact-run before/action/after screenshots when the player-visible workflow is new or materially changed. Unchanged scenarios do not require duplicate manual screenshot review when their relevant staged bytes and inputs are identical.

Logs, loaded Defs, Harmony patch inspection, API success, uncorrelated gateway JSON, raw-C# reads, and direct state mutation are supporting diagnostics only. They cannot accept gameplay by themselves. Quickstart/direct spawn may prepare disposable fixtures, but must not construct the outcome under test. Retain the exact action sequence, assertions, impact classification, and required observation. If review or later edits can affect a scenario, rerun it; repeat manual screenshot review only when its player-visible behavior or observation contract changed.

## Gateway-assisted control and diagnostics

Discover `DevGateway/current.json` beneath the isolated save-data root, verify its PID and process start time, then use its base URL and bearer token. Prove in this order:

1. authenticated status; missing and stale token rejection;
2. UI state and cursor logs;
3. valid end-of-frame PNG;
4. semantic action, then raw input only when needed;
5. a one-off raw C# main-thread probe; use uploaded assembly execution only when the probe genuinely needs compiled multi-file code;
6. named automation/quickstart and returned mutation ledger;
7. controlled shutdown and credential-free tombstone.

Run final gameplay acceptance with developer mode enabled. Before shutdown, open and inspect RimWorld's native developer log/console so visible warnings are not missed; after controlled shutdown, inspect the complete flushed `Player.log`. Search at minimum for `Exception`, `Error`, `Warning`, `FileNotFoundException`, `TypeLoadException`, `MissingMethodException`, Harmony patch failures, XML errors, unresolved cross-references, and gateway request failures. Classify every match in the retained evidence. Known engine probes or one explicitly intentional dev-only warning may be documented, but a passing scenario never excuses an unexplained warning or error. A package only passes a third-party-library decision after those DLLs load and serve a real request inside Unity Mono.

Use FlaUI against the exact PID for visible menu/window evidence or capabilities not yet surfaced by the gateway. Revalidate the window handle before click/drag/key input and release pressed input in failure cleanup. Bound every FlaUI child command, terminate it only through its retained process handle, mark service/connect attempts before invocation, and reconcile observed disconnected/stopped state after timeouts or malformed responses. Preserve a service that was already running and disconnected; an unconfirmed child termination or final connection/service state fails the run.

For a playable-map gateway scenario, prefer this reversible order:

1. capture `/game-state`, the current selection, camera, and a pre-action log cursor;
2. set and assert developer/god mode and pause/speed, then restore the exact originals;
3. run bounded map/view queries, inspect exact returned ThingIDs, and atomically select only resolved handles;
4. use `/dev-tools/spawn` with a unique idempotency key and retain its handles/mutation ledger; delete only those disposable handles;
5. query an exact known-safe native debug leaf before invocation; for pointer tools, activate first and then use process-scoped input against the exact PID;
6. query selected/owned gizmos, invoke a reversible immediate/toggle command, and restore its observed state;
7. apply one disposable typed cell/line/rectangle designator interaction, retain accepted/rejected targets, then cancel any still-active exact handle;
8. restore selection/camera/control state, capture screenshot/log evidence, request controlled shutdown, and recheck the normal `ModsConfig.xml` hash.

Never invoke a fuzzy debug/gizmo match merely to make a smoke pass. Require the expected path, runtime kind, availability, and cardinality; retain discovery evidence and fail safely when the installed RimWorld/mod version differs. Treat `stale_*`, `unsupported_*`, and `pointerRequired` as useful contract results, not reasons to guess another callback.

Before destructive E2E isolation, wait for native `Game.PlayerHasControl`; a quickstart map may exist while RimWorld still rejects pause commands. Keep that readiness wait bounded by the test deadline. Do not add a generic synchronous callback step: Unity's main thread cannot preempt a blocking delegate. Keep iterator-side fixture maintenance fixed-cost and bounded, place it after a persisted step boundary, and never count it as the real action or visible observation used for acceptance.

For optional compatibility, cover at minimum:

- external package absent: owning mod loads and core behavior remains usable;
- present-supported: exactly one selected integration produces the observable cooking result;
- present-incompatible: specified safe no-op or fallback plus one bounded warning; use a host fake for the incompatible shape unless an actually installed incompatible version is available for an isolated live run;
- repeated initialization: no duplicate Harmony patch or duplicated behavior;
- missing XML target: safe inactive result when the integration actually contains a conditional XML patch target.

For Harmony, prove the resolved target signature and that `Harmony.GetPatchInfo(target)` contains the owning Harmony ID for the expected patch exactly once. For XML, inspect the loaded Def database, not only packaged XML. These are supporting checks. In both cases drive the actual cooking scenario through its player-facing bill/job path and assert the observable result; personally observe it when that integration behavior is new or materially changed. Add a scenario-owned quickstart fixture only to arrange preconditions when the stock fixture is insufficient. A semantic/raw-C# state probe may corroborate the observation but cannot replace the native-action/outcome assertion. Startup, static screenshots, and absence of log errors are necessary but not sufficient. Record the external mod package ID, version, assembly filename, MVID or SHA-256, and selected integration strategy.
