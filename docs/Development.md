# Development workflow

This repository is a RimWorld 1.6 mod monorepo. OpenSpec changes live at the repository root, playable mods live under `mods/`, shared host-safe contracts under `shared/`, companion tools under `tools/`, and NUnit projects under `tests/`. Re-inspect installed dependency assemblies and finalized Defs before changing optional-mod integration code; machine-local dependency audits are deliberately ignored. Downloaded Workshop content is read-only inspection input and must remain unmodified. See [Gateway.md](Gateway.md) for the developer gateway's security model and command/API reference.

The checked-in defaults expect:

- RimWorld: `F:\Steam\steamapps\common\RimWorld`
- Workshop content: `F:\Steam\steamapps\workshop\content\294100`
- a .NET SDK compatible with `global.json`
- PowerShell, OpenSpec, and (for live smoke tests) `flaui` on `PATH`

Override `-RimWorldPath` and `-SteamModContentFolder` on repository scripts when using another installation.

## Spec-first change flow

Every change must name its owning mod. Inspect the proposal, design, capability specs, and task state before editing:

```powershell
openspec list
openspec show add-rimworld-dev-gateway --type change --no-interactive
openspec status --change add-rimworld-dev-gateway
openspec validate --all --strict --no-interactive
```

Use the applicable change ID in place of `add-rimworld-dev-gateway`. Update a task checkbox only after its acceptance evidence exists. Planned Immersive Chefs gameplay behavior is deliberately separate from implemented baseline behavior.

## Red, green, regression

Run a focused test first, then the owning suite, then the repository wrapper:

```powershell
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release -TestFilter "FullyQualifiedName~GatewayApiRouterTests"
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs -Configuration Release
.\scripts\Invoke-Tests.ps1 -Configuration Release
```

`Invoke-Tests.ps1` supplies the local RimWorld and Workshop paths to the Zlepper test SDK, writes one TRX and build log per suite under `artifacts\TestResults\<UTC run id>`, and treats zero **executed** tests as failure even when tests were discovered but ignored. Grouped selections run every project and aggregate failures instead of stopping at the first. Its exit codes are `0` for at least one test executed in every selected suite with no failures, `1` for a test/runtime failure, and `2` for a path or semantic input rejected after parameter binding. Invalid `ValidateSet` values never enter the script; PowerShell reports its own nonzero parameter-binding failure, normally `1`. `-TestFilter '<dotnet-test filter>'` requires an exact suite such as `ImmersiveChefs.Harmony`; use `-Output json` for machine-readable output.

`-Suite ImmersiveChefs` is a group selection: it launches the unpatched unit, explicit-Harmony, and lightweight-Def projects as separate testhost processes. Select `ImmersiveChefs.Unit`, `ImmersiveChefs.Harmony`, or `ImmersiveChefs.Defs` to run one environment. The testing SDK does not load RimWorld mods or Def XML; read [TestingEnvironments.md](TestingEnvironments.md) before writing a test that needs patches, an optional assembly, or a Def database.

## Build, package, and explicit deploy

A normal build is repository-local:

```powershell
dotnet restore .\ImmersiveChefs.sln
dotnet build .\ImmersiveChefs.sln -c Release
```

Inspect distributable packages under `artifacts\Mods\<package-id>` and the SDK's non-live staging copy under `artifacts\GameMods\<package-id>`. `Directory.Build.targets` prevents a normal Zlepper ModSdk build from copying into the live game.

Only opt into a game deployment explicitly. For example:

```powershell
dotnet build .\mods\ImmersiveChefs\ImmersiveChefs.csproj -c Release `
  -p:DeployToGame=true `
  -p:RimWorldPath='F:\Steam\steamapps\common\RimWorld' `
  -p:RimWorldManagedPath='F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed' `
  -p:SteamModContentFolder='F:\Steam\steamapps\workshop\content\294100'

dotnet build .\mods\RimWorldDevGateway\RimWorldDevGateway.csproj -c Release `
  -p:DeployToGame=true `
  -p:RimWorldPath='F:\Steam\steamapps\common\RimWorld' `
  -p:RimWorldManagedPath='F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed' `
  -p:SteamModContentFolder='F:\Steam\steamapps\workshop\content\294100'
```

The live-game write target for each command is only its exact package folder, `fumblesneeze.immersivechefs` or `fumblesneeze.rimworlddevgateway`, beneath RimWorld's local `Mods` directory. Never edit or deploy into a Workshop item.

Build the companion gateway client separately when needed:

```powershell
dotnet build .\tools\RimWorldDevGateway.Client\RimWorldDevGateway.Client.csproj -c Release
```

The executable is `artifacts\HostTools\Release\net480\RimWorldDevGateway.Client.exe`.

## Isolated in-game verification

First inspect what a smoke run will launch, then run it:

```powershell
.\scripts\Invoke-RimWorldSmoke.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldSmoke.ps1 -TimeoutSeconds 180

.\scripts\Invoke-GatewaySmoke.ps1 -DryRun -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -TimeoutSeconds 180
.\scripts\Build-InGameIntegrationTests.ps1 -ActivePackageIds 'ludeon.rimworld','fumblesneeze.rimworlddevgateway' -RimWorldPath 'F:\Steam\steamapps\common\RimWorld' -SteamModContentFolder 'F:\Steam\steamapps\workshop\content\294100' -DryRun -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests -TimeoutSeconds 180
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests -IntegrationFailureProbe -TimeoutSeconds 300
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId ludeon.rimworld -TimeoutSeconds 300 -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow -InteractiveHoldSeconds 900
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -TimeoutSeconds 300

.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|MainMenuLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ImmersiveChefsXmlProbeContainsItsFinalPatch' `
  -TimeoutSeconds 180
```

`Invoke-RimWorldEndToEndTests.ps1` is the retained multi-frame workflow runner. It discovers separately marked E2E assemblies, groups them by the complete declared non-Gateway package order, appends Gateway last, deploys repo-owned products before marker-owned staging, and launches one fresh isolated RimWorld process per selected group. It scans installed package metadata and passes each child group's ordered additional mods through UTF-8 package-ID files instead of relying on Windows command-line array binding. Runtime save data uses a short `smoke-NNN` branch to stay below Unity Mono's classic Windows path boundary; descriptive group reports remain separate. The runner continues after ordinary group failures, replaces XML-invalid terminal controls in JUnit failure text, and writes aggregate JSON/JUnit before removing the exact leased stage. Use `-DryRun` first; it builds/discovers tests but does not deploy, stage, create a run directory, or launch RimWorld. Inspect the exact run's screenshots before accepting gameplay.

Both scripts refuse to start while RimWorld is already running. They create a per-run `-savedatafolder`, write an isolated minimal `ModsConfig.xml`, use a dedicated `Player.log`, attach FlaUI only to the exact launched PID, and stop only that process. Gateway smoke also writes isolated `Prefs.xml` with `runInBackground=True`, `volumeMusic=0`, fullscreen disabled, and a deterministic 1600×900 render size; matching Unity startup arguments prevent a full-desktop window from being created before the OS-minimized request takes effect. Pass `-VisibleWindow` for hands-on/computer-use interaction; `gateway-regression` selects a normal visible window automatically because it owns FlaUI and raw-input checks. Dry-run and completed results report the preference path, music/render settings, Unity window arguments, and requested window style. Once the window is created, the launcher does not inspect native window state, so restoring or maximizing RimWorld cannot fail verification. Gateway smoke journals each request, requests a normal close first, and attempts a diagnostic dump before exact-PID force fallback if the owned process will not exit. FlaUI commands are child-process time-bounded; connection/service attempts are marked before invocation and reconciled to observed disconnected/stopped state even after malformed output or a timeout, while a pre-existing disconnected service is preserved. They hash the user's normal `ModsConfig.xml` before and after and fail if it changes; Gateway smoke applies the same protection to the user's normal `Prefs.xml`. Gateway smoke defaults to Core + RimWorld Dev Gateway; `-AdditionalModIds` validates and de-duplicates caller-supplied package IDs, preserves their order between Core and the gateway, affects only the isolated configuration, and is checked against RimWorld's actual loaded-mod order through the raw endpoint. For a product test, also pass its repository project through `-AdditionalModProjectPaths` and name at least one exact owner/lifecycle/test through `-ExpectedIntegrationTests`; otherwise a Gateway-only run proves only the Gateway. Pass one or more `-ExpectedLogMarkers` when useful to prove that a product initializer reached a known-success marker; this is a startup prerequisite, not product-behavior acceptance. Common mod/gateway exception signatures are rejected independently.

For an explicitly active package ID duplicated across immediate Workshop item directories, Gateway smoke reads matching `About.xml` metadata without changing it. It proceeds only when one copy declares the current RimWorld major version, reports that choice in dry-run, and publishes a marker-owned physical local-mod copy before process start. Physical copying preserves the integration scanner's reparse-point safety boundary. After the exact owned process is confirmed stopped, cleanup validates ownership and deletes only that copy and its adjacent marker; an ambiguous selection or unverified cleanup fails closed. Inspect `workshop-overrides.json`, `workshop-override-cleanup.json`, and the `WorkshopOverrides` entry in `cleanup-status.json` for the retained contract.

Evidence is retained at:

- `artifacts\RimWorldSmoke\<UTC run id>` for Core + Harmony + Immersive Chefs;
- `artifacts\GatewaySmoke\<UTC run id>` for Core + RimWorld Dev Gateway.

Each run retains its isolated mod list, build log, Player log, and screenshots, and its command result reports the before/after normal-configuration hashes. Gateway runs additionally retain API responses, a bounded finalized Steel Def export, raw-C# declaration/state/mutation-restore evidence, and a credential-free stopped session record. `-RunIntegrationTests` passes the mandatory complete ordered active package sequence to the host builder. The builder rejects omitted/empty input, validates each strict source manifest's reusable required/forbidden constraints or exact package sequence before build, and builds/stages every matching opted-in owner beneath marker-owned `artifacts\InGameIntegrationTests\StagingMods`. Exact mode also rejects reordered packages, built-manifest drift, and a mismatched real loaded order before the Gateway loads the test assembly. The smoke starts RimWorld with the exact discovery flag, polls the exact retryable `integration_test_status_pending` response while initial attachment commits, and rejects zero discovery, a staged assembly with no same-owner descriptor, omitted results, or unexpected discovery/test failures. It requires the terminal endpoint object and token-free session artifact to describe the same durable snapshot and retains both. Product project evidence also binds the deployed product DLL, About metadata, and XML patch hashes to that run. `-Quicktest` proves the checked-in quickstart fixture and an idempotent terminal replay on a playable map, then exercises the semantic game-state/camera, thing query/inspection/selection, direct spawn, native debug-action, gizmo, and typed designator routes with exact request IDs and restoration/cleanup evidence. It captures a log cursor before setup and writes the correlated post-setup JSON and log page into the same bundle; a log page may report `PageTruncated`. These are gateway/control diagnostics. Gameplay acceptance additionally requires the acting agent to perform a real player action through its native UI/game-command/job path, personally observe the player-visible result, and retain the exact action plus before/action/after evidence. Loaded Defs, patches, logs, API results, raw-C# assertions, direct mutations, and a static screenshot do not satisfy that gate alone.

The historical combined gateway-control record is `artifacts\GatewaySmoke\20260801T125136445Z`; it verified inside RimWorld the ordered isolated list Core, Harmony, Immersive Chefs, and RimWorld Dev Gateway plus the recorded gateway mechanics. It is not gameplay acceptance under `AGENTS.md` and must not be reused for a changed build. See [Gateway.md](Gateway.md) for the exact raw-execution, quickstart, screenshot, input, log-correlation, and cleanup assertions it proves.

## Repository-local skill

Use `.codex/skills/rimworld-mod-development/SKILL.md` for Harmony, XML patching, optional integration, TDD, and live-verification conventions. Validate changes to that skill with the installed skill-creator validator:

```powershell
python C:\Users\<you>\.codex\skills\.system\skill-creator\scripts\quick_validate.py .\.codex\skills\rimworld-mod-development
```

## Extending a mod

1. Confirm or create an OpenSpec change with one owning mod and observable scenarios.
2. Read the relevant repository-local skill reference: Harmony/compatibility for C# patches, XML/Defs for assets and conditional patches, and testing/verification before making compatibility claims.
3. Add the smallest failing test, capture the intended red result, implement the vertical slice, and keep refactors green.
4. Keep optional integrations behind package-ID/Def resolution and a narrow adapter. Product mods must never reference or depend on the Dev Gateway.
5. Run the focused test, owning suite, all-suite wrapper, Release build, and package inspection; then run an independent code review, apply accepted fixes, and rerun regressions and packaging.
6. Run final in-game acceptance on the reviewed build: perform the player action, personally observe the player-visible result, and retain before/action/after evidence from the exact process.
7. Mark only OpenSpec tasks backed by that complete evidence.
