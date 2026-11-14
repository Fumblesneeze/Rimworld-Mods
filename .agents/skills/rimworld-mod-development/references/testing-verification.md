# Test and in-game verification workflow

## Fast tests

Use the exact owning test project for the red-green loop. During ordinary feature work, stop after the focused test unless shared behavior requires the owning suite. Reserve the repository wrapper for an explicit maintenance, regression, or release checkpoint:

```powershell
.\scripts\Invoke-Tests.ps1 -Suite <exact-suite> -Configuration Release -TestFilter "FullyQualifiedName~<focused-test>"
.\scripts\Invoke-Tests.ps1 -Suite <exact-suite> -Configuration Release
.\scripts\Invoke-Tests.ps1
```

For a red-green slice in a registered project, filter its exact wrapper suite and rerun that same focused selection after implementation or an affecting review fix. Rerun the exact owning suite only when the slice changes shared behavior across it. Do not replay completed scenarios or invoke the guarded repository/full E2E suite merely because another feature changed. A raw `dotnet test --filter` is acceptable only for an initial compile-time RED or with an explicit inspection of the executed count; VSTest can exit zero for zero matches. A command that reports zero executed tests is a failure even when tests were discovered/ignored and the underlying runner exits zero. Wrapper filters require one exact suite; grouped runs execute every selected project and aggregate failures. Keep deterministic seams for clocks, random tokens, process identity, filesystem roots, dispatch phases, and optional-mod catalogs.

Source-linked host tests can compile product files under a newer C# language version than the owning
mod project. Keep shared source within the product project's language version and perform the owning
product build at the batch checkpoint; a passing host test does not prove product compilation.

Read generated evidence at the exact path returned by the operation. If only a run directory is
known, enumerate that bounded directory with `rg --files --no-ignore` before selecting its log or
TRX; do not guess filenames from a remembered suite or previous run. Evidence directories are
ignored by Git, so an ordinary `rg --files` can otherwise hide the files that actually exist.
When retaining a typed CLI run, capture stderr separately as well as stdout. Failed operations may
report their diagnostic on stderr and leave stdout empty; preserve the canonical test log/TRX and
its nonzero counters instead of mistaking an empty redirect for durable RED evidence or rerunning
an already retained failure merely to recover its output.

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

Do not read a live runner's mutable evidence JSON with `Get-Content` while it is being rewritten.
On Windows even a brief read can deny the writer's file access and abort an otherwise valid run.
Wait for the owned CLI session to finish before inspecting those files; use a typed live-status
operation when progress detail is necessary. A file-sharing abort is harness evidence, not a
gameplay failure or acceptance, and its cleanup must be checked before retrying the exact case.

Before the ordinary reset, remove all roofs, including overhead mountain, through the scenario-owned
test seam; then clear disposable state and alerts. For a plain quickstart, pass `-Quicktest` and omit
`-Scenario`; the resulting evidence label `none` is not a literal scenario argument. Fixture setup and
mutation occur only after the attributed test starts—never as implicit startup behavior.

The grouped runner passes each child's exact additional-mod order through a UTF-8 package-ID file and uses a short `smoke-NNN` runtime branch; do not replace either with repeated cross-process PowerShell array parameters or a descriptive save-data path that can exceed the selected runtime's Windows path limits. Retained stderr stays raw, while JUnit replaces only XML-invalid characters.

For a native right-click order, use `context.GetRequiredService<IEndToEndFloatMenuCatalog>()`, call `Query(actorRuntimeId, targetRuntimeId)` on the E2E execution thread, and choose exactly one enabled option by its visible label. Feed the returned `StableId` to `FloatMenuActionStep`. Never duplicate the stable-ID hash or directly start the job that the native option would create.

For a scenario that must cross RimWorld's incident boundary, use `IncidentActionStep` with one exact loaded `IncidentDef` name and, when required, the exact live faction load ID. The Gateway builds forced current-map storyteller parameters and invokes the real incident worker. Do not replace it with a generic callback, a guessed storyteller queue entry, translated labels, or synthetic result construction; retain a later wait and screenshot of the ordinary map/UI outcome.

Prefer action → wait → observation steps that exercise the same native path a player uses. Direct arrangement may create preconditions but must not create the claimed result. After a reviewed run, inspect the exact native screenshots yourself and describe the visible causal result. Endpoint state, logs, and synthetic assertions do not independently satisfy acceptance.

### E2E authoring invariants learned from live failures

- Audit the complete native UI sequence before launching a new fixture: exact public/private,
  static/instance and declaring-type reflection bindings, actual layout transforms, confirmation
  dialogs, and which parent windows an action closes. Use named missing-member assertions. Native
  windows can round calculated rectangles before rendering; compare the consumed projection and
  exact window identity rather than assuming raw rectangle equality. When one such assumption fails,
  inspect the remaining sequence together before retrying. Derive click points from the actual
  native control rectangle, including active owned column reservations or argument transforms
  applied before native layout. A former outer-window-width formula can target a neighboring
  control after another integrated feature reserves space. Read the current geometry/measurement
  without invoking preparation, drawing or action callbacks, and verify the actual clicked result.
  Include asymmetric one-button dialogs; a window's center is not
  necessarily its button's center. After acknowledgement, wait for that exact window to disappear
  before capturing or interacting with a dialog beneath it. An accepted input receipt does not
  establish that the intended control received the click.
  Keep observation-only window capture separate from input geometry admission. Exact type/ID and
  cleanup ownership are sufficient to observe a titled native console; retain untitled-window,
  content-origin, dimensions and current-scale guards on every path that actually derives input
  coordinates or replaces text.
- Snapshot native job identity and consumed fields while the job is active. RimWorld returns
  completed jobs to a pool and clears/reuses their fields, so a retained `Job` reference is not
  historical evidence. Store immutable load ID, owner, Def/giver, targets, count and relevant flags
  before completion; compare later observations to that snapshot without changing native pooling.
- For independent restrictions, use a fresh control that still satisfies the native prerequisites.
  A previously consumed dose may already be blocked by native interval/history, so its later refusal
  cannot prove another restriction survived a setting or availability change. Retain passive native
  permission and final decision observations for the same actual attempt, plus the physical outcome;
  do not clear history or invoke the decision merely to manufacture a passing control.
  Also include a successful action while the new restriction is enabled and relevant. A disabled
  feature or unrestricted threshold proves only its bypass, and cannot catch an adapter that rejects
  every relevant candidate despite a correct detached rule.
- Prove fault injection is reached before judging failure handling. Install an unarmed interceptor
  before a successful native baseline, retain actual hit/phase counters, then arm only the intended
  field or owned callback. A registered Harmony detour, especially a closed generic added after
  warm-up, does not establish interception. After the real action finishes, checkpoint hit counts
  and bounded window/log state before waiting for an expected notice; a missed injection should
  fail immediately with diagnostics. Keep intentional errors precisely accounted for and preserve
  unrelated error detection. Trace native failure handlers through disposal and scene transitions:
  an attempted return to the menu is not proof of completed recovery. If an injected failure
  exposes a native cleanup defect, retain its exact causal evidence and distinguish the rejected
  operation from successful scene recovery before changing fixture expectations.
- Preserve native fixture data contracts. Generated human pawns may participate in later relation
  generation, so use native human-style names (`NameTriple`) instead of replacing them with
  `NameSingle` before generating additional humans. Direct setup must not create the tested result.
  Before reusing an observation getter for another item kind, inspect its eligibility gates:
  `JobDriver_Ingest.GainingNutritionNow` excludes non-nutritive drugs. Observe their actual native
  chewing-toil identity with the same job and physical source instead; never add nutrition or
  change a Def to make an observer succeed.
  Inspect getter side effects too: `MainButtonDef.TabWindow` can construct a window after load,
  pawn `CurrentPolicy` can choose/create a default, and `SettingsRuntime.CurrentBinding` can pump
  pending lifecycle work. Fresh-load evidence must inspect existing windows, stored policy and
  already-published binding fields with checked shapes and identities, including malformed states;
  it must not create a default or complete the lifecycle it claims to observe.

- Budget every group honestly. Its frame, game-tick, and wall-time caps must exceed the sum of all
  sequential per-step maxima plus bounded action/screenshot/assertion/save-load overhead. A combined
  group must cover every admitted case, not one case's deadline multiplied by hope. Calculate the
  caps before writing the test attribute; do not copy another fixture's wall/frame limits. Retain
  a count × actual provider deadline calculation for screenshots, native input/commands and
  other sequential operations; prior fast runs do not justify a cap below those declared allowances.
  Identify operations without their own bound and allocate an explicit shared scenario reserve,
  rather than claiming an engine timing guarantee. Frame/tick/wall limits are independent stop
  conditions: do not derive a frame ceiling from an assumed refresh rate. For a paused UI workflow
  with no established frame-rate ceiling, explicitly defer the frame stop to the finite wall budget
  while retaining the independent tick limit. Recalculate after adding captures or controls, and make
  the launcher allowance cover the selected scenario plus startup and cleanup. If an individual
  scenario cap changes, update every documented combined sum that includes it in the same diff.
- Keep later cases inert. Arrange or activate hunger, bills, jobs, and other autonomous triggers only
  when that iterator begins; zero work priorities do not disable ingest/think-tree behavior.
- Assert physical units with `stackCount` across spawned, carried, inventory, holder, and embedded
  state. Counting Thing objects misses duplicates merged into a stack. When exact fixtures matter,
  also assert each returned Thing identity and `stackCount == 1`.
  Distinguish remaining stock from cleanup ownership. Native partial `Thing.Ingested` can discard
  an undestroyed, holderless `SplitOff` result; tracking every result and summing `!Destroyed`
  double-counts the consumed unit. Retain its causal split evidence and cleanup handle, but use the
  actual native ownership/consumption path to decide whether it remains physical stock.
- Exercise the real engine seam. Calling `meal.Graphic.GetType()` proves a CLR owner, not ingredient
  graphic selection; invoke `MatSingleFor`/the actual selected Graphic method. Starting a Job directly
  proves less than choosing its native float-menu/gizmo/bill action.
- At stable checkpoints assert all relevant persistent state. Save/load tests compare exact Thing IDs,
  stack counts, ingredients, every serving/component field, sanitation, selection/graphic provenance,
  and holder ownership—not one representative field.
  Separate serialized values from native load initialization. Inspect the selected game's load path:
  pause-on-load may follow a mandatory native tick, advancing both game time and an executing job.
  Retain saved and fresh loaded IDs/ticks/toil progress before assertions, prove the exact applicable
  native delta and its preconditions, and preserve job/source identity. Never force loaded values back
  or add arbitrary timing tolerance to satisfy a zero-progress assumption.
- If time legitimately evolves state during a persistence-only fixture, disable that subsystem through
  the narrow product setting, register restoration before seeding state, and do not claim the fixture
  proves the disabled behavior.
- Preserve the primary failure during cleanup. Attempt destruction/restoration for each fixture
  independently; aggregate cleanup-only failures without masking an existing assertion exception.
  Within one registered cleanup callback, protect each owned object, policy and patch target
  separately too: the harness can continue with another callback, but cannot resume a loop after
  its first exception. Complete every safe cleanup attempt and verification before aggregating.
  Keep process-owned UI restoration in callbacks separate from Game-dependent cleanup: rejecting
  a stale or partial loaded Game must not skip safe renderer-scroll or other process-state restoration.
- Frame related actors/Things with the camera, then select only the evidence target before a full-frame
  screenshot. RimWorld multi-selection hides the ordinary single-Thing inspect pane.
  Every native pawn-inspect action requires that exact pawn to already be the sole selection;
  camera framing and policy-list selection do not establish it. Have a shared inspection sequence
  emit native sole selection followed by the tab action, including when switching subjects. Expand
  every call when counting actions and deadlines; keep Draft/Undraft selection/gizmo pairs intact.
  Distinguish the selected pawn's inner Health/Gear pane from the main Inspect tab: native selection
  can reopen Inspect on later GUI frames after a successful pane close. Use actual native save
  eligibility and closed owned settings/editor windows for save boundaries; do not require a benign
  Inspect main tab to stay absent while keeping its pawn selected. Verify the needed inner-pane state
  separately, and do not infer later-frame state from one synchronous native close result.
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

- external-package-absence run: Core, required libraries, owning mod, and Dev Gateway for disposable automation;
- external-package-present run: the absence list plus the external mod and its actual dependencies;
- gateway-assisted integration run: the exact integration list with Dev Gateway for disposable automation;
- gateway self-test run: Core plus Dev Gateway.

Gateway-based native player workflows satisfy live acceptance. No separate run without Dev Gateway is required for development, compatibility verification, or release. Verify product independence through dependency metadata, package contents, and assembly references. For every claimed compatibility strategy, run an installed version that actually selects that strategy; never claim a runtime fallback that was only inferred or host-tested.

Wait for the exact launched PID, not any window with the same title. Every run SHALL retain Player.log, ModsConfig, screenshots where applicable, the build log, and hashes under one evidence directory. A gateway-assisted run SHALL additionally retain the credential-free stopped session tombstone, API request IDs, and JSON responses. A product-only run cannot produce gateway artifacts and MUST NOT be marked incomplete for their absence. Never retain a live `current.json` manifest or bearer token in durable evidence.

## Player-behavior acceptance

Run final live acceptance after independent review and all accepted fixes. The acting agent must perform an actual player action—or faithful automation of the same native UI, gizmo, bill, job, designation, hauling, ingestion, construction, selection, or game-command path—and personally observe the resulting player-visible behavior in the running game or exact-run before/action/after screenshots.

Logs, loaded Defs, Harmony patch inspection, API success, gateway JSON, raw-C# reads, and direct state mutation are supporting diagnostics only. They cannot accept gameplay by themselves. Quickstart/direct spawn may prepare disposable fixtures, but must not construct the outcome under test. Retain the exact action sequence and describe the observed result. If review or later edits can affect runtime behavior, discard older acceptance evidence and repeat the run.

## Gateway-assisted control and diagnostics

Discover `DevGateway/current.json` beneath the isolated save-data root, verify its PID and process start time, then use its base URL and bearer token. Prove in this order:

1. authenticated status; missing and stale token rejection;
2. UI state and cursor logs;
3. valid end-of-frame PNG;
4. semantic action, then raw input only when needed;
5. a one-off raw C# main-thread probe; use uploaded assembly execution only when the probe genuinely needs compiled multi-file code;
6. named automation/quickstart and returned mutation ledger;
7. controlled shutdown and credential-free tombstone.

Run final gameplay acceptance with developer mode enabled. Before shutdown, open and inspect RimWorld's native developer log/console so visible warnings are not missed; after controlled shutdown, inspect the complete flushed `Player.log`. Search at minimum for `Exception`, `Error`, `Warning`, `FileNotFoundException`, `TypeLoadException`, `MissingMethodException`, Harmony patch failures, XML errors, unresolved cross-references, and gateway request failures. Classify every match in the retained evidence. Known engine probes or one explicitly intentional dev-only warning may be documented, but a passing scenario never excuses an unexplained warning or error. A package only passes a third-party-library decision after those DLLs load and serve a real request inside the exact selected game runtime.

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

For Harmony, prove the resolved target signature and exactly one expected owned registration using
the provider-appropriate inspection path. Do not assume `Harmony.GetPatchInfo` is side-effect-free;
follow [harmony-compatibility.md](harmony-compatibility.md) for metadata-sensitive admission. For XML,
inspect the loaded Def database, not only packaged XML. Both are supporting checks. Drive the actual
player-facing bill/job/UI path and personally observe its result; fixture setup or a semantic state
probe cannot replace that workflow. Startup, static screenshots and absence of log errors are not
sufficient. Retain external package/version, assembly MVID or SHA-256, and the selected strategy.
