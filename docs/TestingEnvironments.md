# Host test environments

The repository's Zlepper testing SDK is a compiler and NUnit configuration layer, not a running RimWorld harness. Version `0.0.9` references `Assembly-CSharp.dll` and `UnityEngine.CoreModule.dll`; it does not load mods, construct `Mod` classes, apply Harmony, parse Def XML, populate `DefDatabase<T>`, or reset RimWorld static state.

That distinction determines which test environment to use.

## Decision table

| Test need | Host-test treatment | What it proves |
| --- | --- | --- |
| Pure calculations or state machines | Pass ordinary values or product-owned immutable snapshots | Domain behavior only |
| Optional package present/absent | Pass explicit package IDs to `IntegrationCatalog` or another narrow public boundary | Package-selection logic, not a loaded mod |
| One Immersive Chefs Harmony patch | Use the process-isolated `ImmersiveChefs.Harmony` suite; patch one explicit target with a unique owner and unpatch in cleanup | Patch method/signature behavior under the exact installed Harmony DLL |
| Lookup of a small known Def | Construct a minimal `Def` subclass and register it with `DefDatabaseScope<T>` in the process-isolated `ImmersiveChefs.Defs` suite | Code interacting with `DefDatabase<T>` and the fixture's declared fields |
| Source XML shape/XPath | Parse the source file with standard XML APIs | Repository XML structure only; no inheritance, cross-references, or PatchOperations |
| Deterministic Gateway crop metadata or generated image | Use the separate `RimWorldDevGateway.Snapshots` NUnit 4 project with Verify and Verify.ImageMagick | Stable host-side projection/image output; not a live Unity rendering assertion |
| Installed optional assembly shape | Use a dedicated process/project with an explicit path, hash/MVID, and reflection contract | Type/member compatibility only; not a loaded mod |
| Real `ThingDef`, `RecipeDef`, `DefOf`, XML inheritance, cross-references, or conditional patches | Load the exact mod list in isolated RimWorld and inspect the real post-load database as supporting evidence | RimWorld's actual loader result |
| Repeatable finalized Def/XML/full-Harmony assertions across selected mod lists | Use an opted-in `*.IntegrationTests.dll` staged outside `Assemblies`, then start the Dev Gateway with its integration-test flag | Once-per-process assertions at a real game lifecycle point; still not player-behavior acceptance |
| Repeatable multi-frame player workflows across exact mod combinations | Use a separately attributed `*.EndToEndTests.dll` through `Invoke-RimWorldEndToEndTests.ps1` | Native actions, waits, observations, screenshots, sequential disposable-map cleanup, and exact-group isolation |
| Another mod's constructor, static initialization, complete Harmony patch set, or gameplay interaction | Run the installed mod in isolated RimWorld | Genuine mod activation and runtime integration |

## Available suites

`Invoke-Tests.ps1` starts a separate `dotnet test` process for each registered project:

```powershell
# All three Immersive Chefs host environments
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs -Configuration Release

# Exact environments
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Unit -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Harmony -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Defs -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway.Snapshots -Configuration Release
```

The suites are:

- `ImmersiveChefs.Unit`: ordinary tests. An assembly-level setup proves `LoadedModManager.RunningModsListForReading` is empty, `0Harmony` is not loaded, and a representative Def database is empty before any test fixture runs; assembly teardown repeats the assertions as a leak guard.
- `ImmersiveChefs.Harmony`: references the exact installed Workshop `0Harmony.dll`, currently assembly version `2.4.1.0`, and owns failure-safe explicit patch cleanup. It compares the configured source DLL with the runtime copy and the wrapper retains name/version/MVID/SHA-256 in `ImmersiveChefs.Harmony.dependencies.json`. Override its location with `-HarmonyAssemblyPath` when the configured Workshop layout differs.
- `ImmersiveChefs.Defs`: owns host-side Def database mutation. Its fixture is non-parallel, requires the matching generic database to be empty, takes exclusive ownership of that whole database for the scope, and clears the whole database in cleanup. Tests must register every intended entry through the scope and must not add unrelated entries while it is active. It refuses pre-populated state without touching name or short-hash lookups.
- `RimWorldDevGateway`: ordinary Gateway host tests using the repository's existing NUnit 3/Zlepper environment.
- `RimWorldDevGateway.Snapshots`: a separate `net48` NUnit 4 host project using current Verify and Verify.ImageMagick packages. Its deterministic PNG comparison uses a 0.001 tolerance to avoid rejecting a visually identical re-encoded image. Approved `.verified.*` files are source; `.received.*` files are ignored. Do not approve uncontrolled live RimWorld frames as golden images.

Do not move patch or DefDatabase tests into the ordinary suite. NUnit fixtures in one assembly share an AppDomain and RimWorld/Harmony static state; `[NonParallelizable]` prevents concurrency but does not create a clean runtime.

## In-game integration assemblies

`RimWorldDevGateway.IntegrationTesting` is a small assertion/attribute contract, not another test engine. Opt-in projects set `RimWorldInGameIntegrationTest=true`, declare one `RimWorldIntegrationTestOwnerPackageId`, output a name ending `.IntegrationTests.dll`, and provide a matching `.integrationtests.json` manifest. They do not reference NUnit or VSTest and ordinary `Invoke-Tests.ps1` runs do not execute them.

Build and stage the selected owner before a fresh isolated launch:

```powershell
.\scripts\Build-InGameIntegrationTests.ps1 `
  -ActivePackageIds 'ludeon.rimworld','fumblesneeze.rimworlddevgateway' `
  -RimWorldPath 'F:\Steam\steamapps\common\RimWorld' `
  -SteamModContentFolder 'F:\Steam\steamapps\workshop\content\294100' `
  -Configuration Release

.\scripts\Invoke-GatewaySmoke.ps1 `
  -RunIntegrationTests `
  -TimeoutSeconds 180
```

The host command requires the complete nonempty ordered active package sequence; omission is invalid and never means "build all." It evaluates reusable required/forbidden constraints or an `activePackageSetMode: "exact"` sequence before building and stages only the DLL and strict, duplicate-free manifest under a marker-owned repository test-mod tree. Exact mode rejects missing, extra, and reordered packages before build, after-build manifest drift before staging, and the actual in-game active order before assembly loading. It prepares each complete owner bundle in a same-volume sibling directory, then publishes it by rename; a copy or commit failure exposes neither a partial stage nor destroys the previous complete owned stage. Gateway smoke first records the builder's full dry-run plan, then publishes those exact candidates under each owning live mod's versioned `DevIntegrationTests` directory only for that isolated run and removes every registered marker-owned candidate in `finally`. For every staged DLL it records the exact full CLR assembly identity and the runtime source identity `<owner package ID>/<manifest filename>`; completed endpoint, repeat, and persisted snapshots must contain a one-to-one `DiscoveredAssemblies` mapping with matching count/source/full identity, and every descriptor and result must map back to exactly one such assembly. It never places tests in `Assemblies`, and the game never compiles source. The Gateway scans only active mods and only with `-devGatewayRunIntegrationTests`. One bounded background worker at a time loads and reflects a source while Unity polls without waiting; only marked-method invocation runs on the main thread. One background persistence lane commits attachment, running, and terminal snapshots. `GET /api/v1/integration-tests` is retryable-pending before the first durable commit and thereafter exposes exactly the last token-free artifact state. The smoke retains this response and retries only exact HTTP `503`, exact `integration_test_status_pending`, and exact boolean `retryable: true`; every other error stays fatal. A failed write retries the same candidate without invoking or rerunning a test. Use a new process for each ordered mod matrix; that process boundary is also the only cancellation for a reflection worker stuck inside mod code.

Use `[IntegrationTest(RunAt.MainMenuLoaded)]` for finalized Def values, PatchOperations, `DefOf`, and complete startup Harmony ownership. Use `PlayableMapLoaded` only when the assertion genuinely needs a current map. These tests replace synthetic host assumptions, not the native player workflow required to accept gameplay.

The texture-variation gate has two process-isolated finalized-Def groups. The ordinary
Core/Harmony/Immersive/Gateway group proves all four supported portable Defs retain
`Graphic_Single` and all eight supported buildings retain no VEF comp when VEF/VTEX is absent. The
exact Core/Harmony/VEF/VTEX/Immersive/Gateway group loads
`ImmersiveChefs.TextureVariations.InGame.IntegrationTests` and proves only cookware, plate,
cutlery, and chef's knives receive `Graphic_PortableKitchenwareVariation`; fixed adobe and
glitterworld art remain ordinary graphics; and each dishwasher, specialist station, and fallback
microwave receives one two-member `CompProperties_RandomBuildingGraphic` family. The accepted
portable finalized-Def runs are
`artifacts/GatewaySmoke/texture-variation-base-green-20260806/20260806T165802541Z` and
`artifacts/GatewaySmoke/texture-variation-vtex-20260806/20260806T165852388Z`. The current two-member
building shape plus native action and persistence is covered by the focused exact-mod E2E run
`artifacts/EndToEndRuns/Grouped/20260806T181659772Z`; it admitted only
`immersive-chefs.texture-variation-visual-catalog`, invoked every native VEF `Random Graphic`
gizmo, observed every rendered path move to the opposite configured family member, restored each
real two-member family, then used RimWorld's native save/load action. The same exact buildings
retained their selected graphics, and the same portable Things retained their concrete rendered
texture, Stuff, and sanitation state. The run retained five before/cycle/portable/reloaded frames,
deleted its exact save, preserved both normal configuration hashes, and completed process,
credential, stage, and fixture cleanup. The acting agent inspected all five retained frames. During
development, rerun only this stable ID when changing this slice; do not replay unrelated completed
catalogs.

The focused base portable-art acceptance is
`artifacts/EndToEndRuns/Grouped/20260806T191943249Z`. It admitted only
`immersive-chefs.base-portable-visual-catalog` in the exact Core/Harmony/Immersive group and retained
17 frames. The catalog aligned every eligible custom wood, granite, steel, silver, and gold fixture
above a selected native material reference, rendered all seven portable Defs, and exposed native
clean/dirty inspectors plus stack counts and selection brackets. The acting agent inspected every
frame and observed the distinct Stuff colors, fixed-art exceptions, and readable item-scale
silhouettes. Because the base plate changed from masked secondary tint to full-surface Stuff tint,
the only directly affected optional stable ID was rerun at
`artifacts/EndToEndRuns/Grouped/20260806T192325293Z`; its five inspected frames prove the exact
VEF/VTEX group still cycles its native building graphics and preserves portable material/sanitation
graphics through native save/load. Both runs completed process, credential, configuration, fixture,
and marker-owned stage cleanup. Do not replay either completed catalog during unrelated feature
work; select only a current stable ID, except for deliberate regression maintenance or release
preparation.

The focused base building-art acceptance is
`artifacts/EndToEndRuns/Grouped/20260806T201719254Z`. It admitted only
`immersive-chefs.base-building-visual-catalog` in the exact Core/Harmony/Immersive group, launched
RimWorld minimized, and retained 16 frames. The catalog rendered every concrete building Def in all
four rotations beside native production and dining references, selected each building through the
ordinary inspector, and showed the fallback microwave on both a real steel dining table and a real
machining table. A typed fail-closed semantic action opened the actual native Production Architect
category without foreground input; the retained menu visibly exposes all eight Immersive Chefs
labels and icons, then closes through RimWorld's native escape path. The acting agent inspected all
16 frames, and the run completed process, credential, configuration, fixture, and marker-owned
stage cleanup. During feature work, rerun only this stable ID when changing the base building or
Architect-menu slice.

The accepted RimCuisine replacement-Def integration run is `artifacts\GatewaySmoke\20260805T145248752Z`. Its exact process loaded Core, Harmony, the selected RimWorld 1.6 Processor Framework Workshop item, all four RimCuisine 2 modules, No Vanilla Meals, Immersive Chefs, and Gateway. The exact compatibility assembly observed all ten official meal ThingDefs absent, every surviving vanilla-product recipe unclassified with its initialization-time multiplier at `1`, and the surviving pottage, rubaboo, pizza, and extravagant-meal Defs finalized with their intended ware/culinary components and tiers. This is finalized-loader evidence; native cooking and trade remain separate E2E acceptance work.

The no-replacement inverse uses two lifecycle-correct processes with exact Core/Harmony/No-Vanilla/Immersive/Gateway order: `artifacts\GatewaySmoke\20260805T150502834Z` proves the settled main-menu registry contains no fallback meal, and `artifacts\GatewaySmoke\20260805T150931026Z` reaches a playable map and proves a Cooking-capable colonist can reach an operational fueled stove whose retained disabled `CookMealSimple` metadata creates neither a covered cooking path nor a missing-kitchenware alert. Main-menu and quicktest lifecycles are intentionally separate because `-quicktest` bypasses the main-menu trigger.

The accepted Adaptive Meal Bill/Overcooked Meals loader run is `artifacts\GatewaySmoke\20260805T205254586Z`. Its exact Core/Harmony/Adaptive/Overcooked/Immersive/Gateway process validates both installed assembly identities, the upstream concrete-recipe and final-product Harmony owners, the unclassified adaptive wrapper, the Advanced concrete Fine recipe, and exactly one embedded-ware and culinary component on the upstream overcooked Def. The matching native workflow is `artifacts\EndToEndRuns\Grouped\20260805T205100848Z`: an ordinary cook selected the adaptive Fine vegetable bill, worked at the stove, and produced the upstream overcooked survivor on its second native attempt. The personally inspected result visibly shows Rice provenance, one bound plate, Awful culinary quality, and steaming-hot temperature; the separately selected steel cookware visibly reads dirty, while the ineligible wood plate remains untouched.

## Dynamically loaded E2E assemblies

The runtime waits for `Game.PlayerHasControl` before capturing or clearing its first isolation baseline; a map that exists during quickstart handoff is not yet safe to pause. The shared contract deliberately has no generic synchronous callback action because Unity cannot preempt a blocking main-thread delegate. Keep iterator-side fixture maintenance fixed-cost, immediately after a persisted step boundary, and separate from the native player action and visible observation used for acceptance.

Use E2E tests for repeatable workflows that span multiple Unity frames: ordinary pawn jobs, native gizmos or float menus, typed trade-dialog transfer/acceptance, registered exact-window confirmation, native incidents, time progression, selection/camera behavior, ingestion, hauling, construction, and their player-visible results. Prefer a typed semantic action for a known native control that can be resolved exactly. `SettlementTradeActionStep` requires one exact same-tile player caravan and settlement, then invokes RimWorld's enabled `CaravanVisitUtility.TradeCommand`; it can also require one exact fail-closed code for negative compatibility tests. `TradeDialogActionStep` resolves the exact physical Thing's `Tradeable`, uses RimWorld's native count setter/refresh, and invokes the exact native Accept callback. `DialogConfirmationActionStep` supplies only an expected exact window type; the Gateway must own a private exact assembly/type/argument/callback registration, require exactly one matching window and a valid void callback, close before invoking it when that is the inspected native order, and fail closed for every unregistered or drifted shape. `IncidentActionStep` resolves one exact loaded `IncidentDef`, optionally resolves one exact faction load ID, constructs forced current-map storyteller parameters, and invokes the real incident worker; missing, ambiguous, rejected, or throwing native state fails closed. None of these semantic paths requests focus, restore, resize, maximize, mouse, or keyboard input, so they can operate after a minimized launch. Keep process-scoped Win32 input for unknown UI, map pointer tools, drags, keys, and text because that route explicitly requires a visible foreground window. E2E projects set `RimWorldEndToEndTest=true`, declare one `RimWorldEndToEndTestOwnerPackageId`, reference only the shared E2E contract plus required game assemblies, and remain outside product packages and ordinary NUnit/VSTest registration.

```powershell
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TimeoutSeconds 300 -Output table
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId ludeon.rimworld -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TestId immersive-chefs.countertop-microwave-support-loss -Output json
```

Every attributed test declares the complete ordered active package sequence excluding Gateway; the host appends `fumblesneeze.rimworlddevgateway` last and starts one fresh process per exact group. During active development, select only the current stable test IDs with `-TestId`; the Gateway validates and executes exactly those tests inside their required group. Run complete groups or the complete matrix only for deliberate regression maintenance and release preparation. Same-group tests otherwise run sequentially on one disposable quicktest map. Reset removes every destroyable Thing/Pawn plus jobs, zones, designations, selections, interactions, and test windows; permanent non-destroyable map features such as steam geysers remain environment. An unverifiable cleanup taints the process and skips later tests. Host deployment happens before atomic marker-owned staging, and the exact lease is cleaned in `finally`.

`SaveLoadActionStep` is the typed E2E persistence boundary. It uses a safe leaf in the isolated save folder, refuses an existing name, verifies a nonempty native save, and waits for a replacement `Current.Game` with player control and a current map. Tests must reacquire Things by stable `ThingID`; references from the pre-load game are disposed state. Register deferred cleanup for the exact save before invoking the action, then retain before/after screenshots and checkpoints. A component-only Scribe round trip is useful supporting integration evidence, but it is not a substitute for observing the same gameplay object across native player-save behavior.

The runner discovers installed/DLC/Workshop package IDs, writes aggregate JSON and JUnit, and retains each group's durable endpoint state and screenshots. API success or a green state assertion alone is not acceptance: the acting agent must inspect the exact-run native screenshots and verify the player action visibly caused the claimed result. Use in-game integration tests for settled loader assertions and E2E tests for multi-frame player behavior; do not make one assembly serve both roles.

The accepted current-binary dispenser examples are `artifacts/EndToEndRuns/Grouped/20260806T065124501Z` for Replimat and `artifacts/EndToEndRuns/Grouped/20260806T084425004Z` for Meal Printer. The four-test Replimat process includes the successful lifecycle, a real failed-dispense race, native animal feeding, native survival batching, and Common Sense cleanup of the successful returned setting. Two ordinary hungry diners reserve exact clean settings against one Ramen-scale feedstock serving; the nearer pawn visibly carries the sole native Ramen, while the rejected pawn's same plate and cutlery visibly return clean with no second feedstock loss. The successful Cleaning-capable diner then visibly carries its exact steel plate with `Doing dishes` and a second `Doing dishes` job queued; the real powered dishwasher and Dubs water tower are visible, and the selected dishwasher reports `loading` plus `Capacity: 1.25/16 place settings`. A raccoon visibly consumes the feeder's native loose Kibble while adjacent steel controls remain clean. Finally, the ordinary terminal gizmo opens Replimat's real batch-survival-meal dialog and creates one selected Packaged survival meal while adjacent golden controls remain clean. The checkpoint distinguishes Common Sense's two-item queue from ordinary one-at-a-time Cleaning and records the exact dirty return, current cleanup job, queued second item, and 1.25 capacity. The acting agent inspected every retained ordinary pawn/building/inspector/dialog frame. Matching loaded-main-menu identity, shape, exclusion, and validated Common Sense-adapter evidence is `artifacts/GatewaySmoke/20260806T065013055Z`.

The accepted fallback-countertop-microwave placement and ordinary-reheat workflow is `artifacts/EndToEndRuns/Grouped/20260806T094131972Z`. Native Architect placement created the appliance on a dining table and machining table, left both exact supports selectable, preserved the machining-table interaction cell, and rejected shelf, bed, unfinished blueprint, and bare-floor targets. A separate ordinary diner carried a frozen plated meal and clean cutlery to a real powered tabletop microwave, visibly heated it, received the configured `80 -> 75` quality change and `-5 -> 60 C` temperature change, then returned the same plate and cutlery dirty after eating. The final focused interruption proof is `artifacts/EndToEndRuns/Grouped/20260806T100447475Z`: the new stable-ID filter admitted and executed only `immersive-chefs.countertop-microwave-support-loss` from its nine-test group. Ordinary Construction removed the table during the captured heating job; that exact job and dining session ended, quality remained `80`, reheat count remained `0`, the same plate stayed embedded, cutlery stayed clean, and normal elapsed-time ambient warming changed `-4.996 -> -3.148 C`. After the bounded fixture made the local four-cell radius temporarily unstandable, the unsupported original appliance appeared exactly once as a selected minified microwave beyond that radius, proving map-wide recovery. The acting agent inspected all four focused frames.

The three-test Meal Printer process uses the exact Core/Harmony/Hospitality/Meal Printer/Cash Register/Gastronomy/Immersive Chefs group. Its new service case prints a native Fine meal from feedstock, preserves the exact embedded plate, moves that physical meal onto a real cash-register stock field, starts the native Gastronomy Dine and Serve drivers, and observes the waiter serve it to an arrived Hospitality guest. Personally inspected frames show `Serving fine meal`, the guest carrying the Fine meal, the waiter carrying the exact returned steel plate with `Doing dishes` and `Queued: Doing dishes (+1)`, and the selected powered dishwasher at `loading` with `Capacity: 1.25/16 place settings`. The checkpoint records both exact dirty returns, both waiter cleanup jobs, `1.25` used capacity, and the guest's personal cutlery remaining clean. The loaded-main-menu matrix `artifacts/GatewaySmoke/20260806T083923840Z` validates the exact package/assembly sequence and one Immersive Chefs postfix on the current four-target Gastronomy `ClearOrder` factory. Both accepted dispenser processes launched minimized and used no focus, restore, resize, maximize, mouse, or keyboard action.

For native right-click actions, resolve `IEndToEndFloatMenuCatalog` from the E2E context, query the current actor/target options, require one enabled visible-label match, and submit that option's returned stable identity through `FloatMenuActionStep`. The catalog is discovery only: invoking the step still runs RimWorld's real callback and preserves its normal job/order boundary.

## Are test Defs mocked or loaded?

Neither description fits every tier:

- Prefer not to expose a raw RimWorld Def to domain code. Map the needed fields into a small immutable product-owned value and test that value normally.
- A host DefDatabase test uses a **constructed real `Verse.Def` fixture**. It is not a mock, but it was not loaded from XML and does not carry RimWorld's full post-load invariants.
- Register every lightweight Def required by that test in one scope. Do not seed Core/Workshop Defs or snapshot-and-restore a populated database: rebuilding RimWorld's short-hash cache reaches a Unity-Mono collection API unavailable in this testhost.
- Avoid constructing `ThingDef` and similar Unity-backed Defs in the host. `ThingDef` initializes `BaseContent`/shaders and fails without Unity. An uninitialized object is at most a narrowly documented field-reading fake, never evidence of valid game data.
- Do not call `DirectXmlToObject`, `DirectXmlLoader`, or `PlayDataLoader` as a shortcut in NUnit. The actual probe reached `XmlInheritance`/type discovery and failed on a Unity internal call. Do not build a substitute RimWorld loader.
- Real mod/Core Defs are **loaded in RimWorld**, using the exact isolated mod list. Inspecting the final Def database or Harmony owner is useful supporting evidence, but acceptance still requires the player action and observable game behavior mandated by `AGENTS.md`.
- A bounded Gateway Def export is another view of that same finalized runtime database. It is diagnostic JSON, not canonical source XML; use exact filters and continuation cursors when authoring patches.

## Tests involving an optional mod

Use the least powerful tier that answers the question:

1. Test package-ID decisions by injecting the active package set. This is the ordinary unit-test path.
2. When an adapter depends on an external type/member shape, add a dedicated process-isolated test project that loads the exact installed assembly by explicit path and verifies its hash/MVID and public shape.
3. When Immersive Chefs supplies a Harmony adapter, apply only that adapter's patch in its isolated Harmony process and remove its unique owner in cleanup.
4. When correctness depends on the external mod's normal loader, constructor, patches, Defs, XML, or static state, stop calling it a unit test. Use the isolated in-game mod combination and complete the real player workflow.

Referencing an external DLL is not the same as loading its RimWorld mod. Manually setting `LoadedModManager` or calling another mod's `PatchAll` in a partly initialized host would create a synthetic environment with misleading confidence.

## When a test appears to need a mod, patches, and Defs together

First separate the claim being tested:

- If package presence alone selects product policy, inject the exact package-ID set into the ordinary unpatched public boundary.
- If one Immersive Chefs patch supplies the behavior, add that explicit target and owner to the isolated Harmony suite; do not bootstrap the production `Mod` or another mod's complete `PatchAll` lifecycle.
- If the rule needs several small product-owned Def-shaped records, construct all required lightweight `Def` fixtures and pass them together to one `DefDatabaseScope<T>.Register(...)` call in the Def suite.
- If correctness requires both a Harmony patch and lightweight Def fixtures but not Unity, create a dedicated process-isolated test project for that concrete adapter rather than contaminating either general suite. Add it only when a real behavior requires the combination.
- If the named mod must actually be active, or any required Def is a real Core/Workshop `ThingDef`, `RecipeDef`, `DefOf`, inherited XML object, cross-reference, or PatchOperation result, the test is an isolated RimWorld integration profile, not a unit test. Launch the ordered mod list, assert the required Def names in the real post-load database as preflight evidence, and then perform the player workflow required by `AGENTS.md`.

This boundary allows test-specific environments without pretending that a DLL reference or hand-populated static list is a normally loaded RimWorld mod.

## Current capability probes

- [Unpatched host characterization](../tests/ImmersiveChefs.Tests/TestHostIsolationTests.cs)
- [Scoped Harmony patch](../tests/ImmersiveChefs.Harmony.Tests/HarmonyIsolationTests.cs)
- [Scoped Def database](../tests/ImmersiveChefs.Defs.Tests/DefDatabaseIsolationTests.cs)
- [OpenSpec contract](../openspec/changes/prove-host-test-environments/specs/developer-verification/spec.md)
