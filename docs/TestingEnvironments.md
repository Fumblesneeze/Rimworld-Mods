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

The accepted RimCuisine replacement-Def integration run is `artifacts\GatewaySmoke\20260805T145248752Z`. Its exact process loaded Core, Harmony, the selected RimWorld 1.6 Processor Framework Workshop item, all four RimCuisine 2 modules, No Vanilla Meals, Immersive Chefs, and Gateway. The exact compatibility assembly observed all ten official meal ThingDefs absent, every surviving vanilla-product recipe unclassified with its initialization-time multiplier at `1`, and the surviving pottage, rubaboo, pizza, and extravagant-meal Defs finalized with their intended ware/culinary components and tiers. This is finalized-loader evidence; native cooking and trade remain separate E2E acceptance work.

The no-replacement inverse uses two lifecycle-correct processes with exact Core/Harmony/No-Vanilla/Immersive/Gateway order: `artifacts\GatewaySmoke\20260805T150502834Z` proves the settled main-menu registry contains no fallback meal, and `artifacts\GatewaySmoke\20260805T150931026Z` reaches a playable map and proves a Cooking-capable colonist can reach an operational fueled stove whose retained disabled `CookMealSimple` metadata creates neither a covered cooking path nor a missing-kitchenware alert. Main-menu and quicktest lifecycles are intentionally separate because `-quicktest` bypasses the main-menu trigger.

## Dynamically loaded E2E assemblies

Use E2E tests for repeatable workflows that span multiple Unity frames: ordinary pawn jobs, native gizmos or float menus, typed trade-dialog transfer/acceptance, time progression, selection/camera behavior, ingestion, hauling, construction, and their player-visible results. Prefer a typed semantic action for a known native control that can be resolved exactly; `TradeDialogActionStep` keeps orbital/settlement trade minimized by resolving the exact physical Thing's `Tradeable`, using RimWorld's native count setter/refresh, and invoking the exact native Accept callback. Keep process-scoped Win32 input for unknown UI, map pointer tools, drags, keys, and text because that route explicitly requires a visible foreground window. E2E projects set `RimWorldEndToEndTest=true`, declare one `RimWorldEndToEndTestOwnerPackageId`, reference only the shared E2E contract plus required game assemblies, and remain outside product packages and ordinary NUnit/VSTest registration.

```powershell
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TimeoutSeconds 300 -Output table
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId ludeon.rimworld -Output json
```

Every attributed test declares the complete ordered active package sequence excluding Gateway; the host appends `fumblesneeze.rimworlddevgateway` last and starts one fresh process per exact group. Same-group tests run sequentially on one disposable quicktest map. Reset removes every destroyable Thing/Pawn plus jobs, zones, designations, selections, interactions, and test windows; permanent non-destroyable map features such as steam geysers remain environment. An unverifiable cleanup taints the process and skips later tests. Host deployment happens before atomic marker-owned staging, and the exact lease is cleaned in `finally`.

The runner discovers installed/DLC/Workshop package IDs, writes aggregate JSON and JUnit, and retains each group's durable endpoint state and screenshots. API success or a green state assertion alone is not acceptance: the acting agent must inspect the exact-run native screenshots and verify the player action visibly caused the claimed result. Use in-game integration tests for settled loader assertions and E2E tests for multi-frame player behavior; do not make one assembly serve both roles.

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
