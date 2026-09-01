# Development workflow

This repository is a RimWorld 1.6 mod monorepo. OpenSpec changes live at the repository root, playable mods live under `mods/`, shared host-safe contracts under `shared/`, companion tools under `tools/`, and NUnit projects under `tests/`. Re-inspect installed dependency assemblies and finalized Defs before changing optional-mod integration code; machine-local dependency audits are deliberately ignored. Downloaded Workshop content is read-only inspection input and must remain unmodified. See [Gateway.md](Gateway.md) for the developer gateway's security model and command/API reference.

Trusted local Codex sessions start the repository-owned `rimworld_modding` stdio MCP from
Codex's native `.codex/config.toml`. The root `.mcp.json` projects the same launcher for clients that
support that common convention; MCP itself does not define a universal configuration filename.
The shared launcher serializes and isolates cold source builds before executing a verified cached
server, so simultaneous local sessions do not race MSBuild output files. Use its typed operations for discovery, strict OpenSpec validation, focused
builds/tests, package checks, leased game and E2E runs, live Gateway diagnostics/mutations, normal
mod-list changes, evidence reads, and universal release preparation/publication. The identical
operation registry is available to CI and recovery through `RimWorldModding.Mcp tool list|call`.
See [RimWorldModdingMcp.md](RimWorldModdingMcp.md).

Every publishable mod owns a strict `Release/release.json`; the universal `release_prepare` operation
builds a clean positive-allowlist candidate and returns a mutation-free Steam diff, digest, nonce,
and expiry. Only the later `release_publish` operation may mutate Steam, and only with that exact
reviewed plan hash and nonce. It uses an isolated initialized-Steam Gateway process, persists admitted
callback state and an exact detached worker/Gateway lease atomically, persists the first returned
Workshop identity before later verification, verifies the
exact metadata/preview/dependencies/content baseline, reacquires the Workshop copy, runs the
prevalidated profile native subscriber workflow with the local product temporarily absent, restores
it, and leaves its screenshots awaiting personal review. Use `release_accept_subscriber_evidence`
only after inspecting those exact frames. A timed-out caller may invoke the same `release_publish`
again: it reattaches to the same callback or polls only the durable same item and never repeats
first-item creation or update submission. Subscriber isolation also records its exact reserved run, backup, and normal
configuration hash before moving the local package. Never delete durable state to force a retry.

Workshop descriptions that embed Steam-hosted feature cards use a separate typed first phase. Run
`release_presentation_sync` for the existing package when card bytes/order change, a gallery slot is
missing, or a retained CDN URL no longer resolves. It synchronizes the ordered additional previews,
downloads and hashes every current Steam image, and rewrites the checked-in description/provenance
without consuming the player-facing change note. Review and commit those resolved files before
`release_prepare`; preparation downloads them again and fails before publication on stale links,
redirects, origin drift, excess bytes, or hash mismatch.

`mod_build` builds the selected profile and then installs the successful positive-allowlist package
into the configured local RimWorld `Mods/<package-id>` directory by default. `local_mod_sync` is the
explicit install-only equivalent. Both stage outside the scanned Mods directory, replace only the
selected package, and retain no install backup; rebuild from the repository package to recover an
older copy. Subscriber isolation is different: its temporary move is a release-verification safety
record and remains recoverable while the subscribed Workshop copy is tested.

The PowerShell launchers and older companion tools documented below are internal migration engines,
not the agent-facing API. Before adding another script or repeating a workflow, inspect
`operation_list`; repeated orchestration, copied parsing, process ownership, safety boundaries, or
durable evidence require an owning OpenSpec scenario and typed MCP/CLI operation.

Each publishable mod may declare ordered gameplay showcases under its release presentation folder.
Immersive Chefs uses `mods\ImmersiveChefs\Release\workshop\showcases.json` for exact optional-mod
groups, natural scene direction, native player workflows, beats, and screenshot/GIF output. Capture
reviewed scenes directly into their versioned per-showcase directories under
`mods\ImmersiveChefs\Release\workshop\assets\showcases\<showcase-id>` with
`scripts\Invoke-RimWorldShowcaseCapture.ps1`; commit the selected PNG/GIF, adjacent `.capture.json`,
and its referenced `frames/` PNGs. The release builder replays the relocatable encoder recipe and
binds the exact loaded packages and product DLL. Keep unrelated exploratory captures in ignored
artifacts. Personally review every crop; each must remain under five seconds and Steam's 1 MiB
additional-preview limit. Use fixed cameras and hard cuts instead of
panning across walking time. For a multi-view sequence, capture two or more individually stable
segments from the same exact held process beneath the final showcase's `segments/` directory, then
run `scripts\Invoke-RimWorldShowcaseAssembly.ps1`. The assembler preserves each source capture/hash,
requires one process and package build, requires the exact ordered beat union, copies only retained
game frames, and emits the final screenshot, sub-five-second GIF, and assembly provenance. Do not
splice frames from separate launches or use an editor-generated transition as gameplay evidence.
Every non-initial release supplies a concise player-facing Steam change
note that says what visibly changed; blank, generic, auto-generated, or recycled notes are not useful
release notes and must not be published.

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

During ordinary feature work, run the exact focused test or exact active-mod group for the current slice. Run an owning suite only when the change affects shared behavior within that suite. Reserve the repository wrapper and complete compatibility/E2E matrices for explicit release preparation or deliberate maintenance/regression work:

```powershell
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway.Unit -Configuration Release -TestFilter "FullyQualifiedName~GatewayApiRouterTests"
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs -Configuration Release
.\scripts\Invoke-Tests.ps1 -Configuration Release
```

`Invoke-Tests.ps1` supplies the local RimWorld and Workshop paths to the Zlepper test SDK, writes one TRX and build log per suite under `artifacts\TestResults\<UTC run id>`, and treats zero **executed** tests as failure even when tests were discovered but ignored. Grouped selections run every project and aggregate failures instead of stopping at the first. Its exit codes are `0` for at least one test executed in every selected suite with no failures, `1` for a test/runtime failure, and `2` for a path or semantic input rejected after parameter binding. Invalid `ValidateSet` values never enter the script; PowerShell reports its own nonzero parameter-binding failure, normally `1`. `-TestFilter '<dotnet-test filter>'` requires an exact suite such as `ImmersiveChefs.Harmony`; use `-Output json` for machine-readable output.

`-Suite ImmersiveChefs` is a group selection: it launches the unpatched unit, explicit-Harmony, and lightweight-Def projects as separate testhost processes. Select `ImmersiveChefs.Unit`, `ImmersiveChefs.Harmony`, or `ImmersiveChefs.Defs` to run one environment. The testing SDK does not load RimWorld mods or Def XML; read [TestingEnvironments.md](TestingEnvironments.md) before writing a test that needs patches, an optional assembly, or a Def database.

## Build, package, and local install

Raw MSBuild remains repository-local and is useful while iterating on source:

```powershell
dotnet restore .\RimWorldMods.sln
dotnet build .\RimWorldMods.sln -c Release
```

Inspect distributable packages under `artifacts\Mods\<package-id>` and the SDK's non-live staging copy under `artifacts\GameMods\<package-id>`. `Directory.Build.targets` prevents a raw Zlepper ModSdk build from copying into the live game.

For the normal development loop, use the typed `mod_build` operation. It builds the selected release
profile and, only after a successful build, installs the exact positive-allowlist package into the
configured local RimWorld `Mods/<package-id>` directory. It stages outside the scanned Mods folder,
replaces only that package, and retains no install backup; `local_mod_sync` is the install-only
equivalent. A failed build does not touch the installed package:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'
dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.immersivechefs","configuration":"Release","modsRoot":null}' -o json
```

Here and throughout the repository, "install", "deploy locally", or "copy to the game" means that
local `Mods/<package-id>` synchronization. It does not mean subscribing through Steam. Do not keep a
repository-owned mod both locally installed and Workshop-subscribed during development; RimWorld sees
duplicate copies. A subscribed-copy release check must be requested as such, temporarily remove the
local copy from discovery, unsubscribe during cleanup, and restore the local package afterward.

The low-level SDK deployment switch remains available for migration/debugging, but is not the
canonical new-mod workflow. If it is needed, target only the exact package folder and never a
Workshop item:

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

The live-game write target for each command is only the exact package folder named by that command's
profile beneath RimWorld's local `Mods` directory (for example, `fumblesneeze.immersivechefs`). Never
edit or deploy into a Workshop item, and never substitute Workshop subscription for local installation.

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
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TestId immersive-chefs.localization-rendering -Language German -TimeoutSeconds 300 -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow -InteractiveHoldSeconds 900
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -AdditionalModIds 'brrainz.harmony','imranfish.xmlextensions','fumblesneeze.immersivechefs' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -TimeoutSeconds 300

.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests `
  -AdditionalModIds 'brrainz.harmony','imranfish.xmlextensions','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|MainMenuLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ImmersiveChefsXmlProbeContainsItsFinalPatch' `
  -TimeoutSeconds 180
```

`Invoke-RimWorldEndToEndTests.ps1` is the retained multi-frame workflow runner. It discovers separately marked E2E assemblies, groups them by the complete declared non-Gateway package order, appends Gateway last, deploys repo-owned products before marker-owned staging, hashes every deployed product file into `product-deployment-evidence.json`, and launches one fresh isolated RimWorld process per selected group. The evidence always has an array root, even for one product; unreadable or reparse package entries fail closed. Test-only `DevEndToEndTests` staging is excluded from the product hash inventory. It scans installed package metadata and passes each child group's ordered additional mods through UTF-8 package-ID files instead of relying on Windows command-line array binding. Runtime save data uses a short `smoke-NNN` branch to stay below Unity Mono's classic Windows path boundary; descriptive group reports remain separate. The runner continues after ordinary group failures, replaces XML-invalid terminal controls in JUnit failure text, and writes aggregate JSON/JUnit before removing the exact leased stage. Use `-DryRun` first; it builds/discovers tests but does not deploy, stage, create a run directory, or launch RimWorld. Inspect the exact run's screenshots and match them to the retained product hashes before accepting gameplay.

`-Language <RimWorldFolderName>` writes the exact bounded leaf token only to the disposable process's `Prefs.xml` as `langFolderName`, records it in dry-run and final evidence, and defaults to `English`. Repeat a selected localization test once per required language so each observation comes from a fresh process; this option never reads or changes the user's normal language preference. If Core does not contain the requested language pack, the launcher temporarily publishes the Gateway's metadata-only `LanguageInfo.xml` into the exact Core language folder so RimWorld can activate the product catalog. The launcher hashes that lease, never overwrites an installed provider, and removes only its own unchanged file after the exact process exits; inspect `language-provider.json` and `language-provider-cleanup.json`. This provider deliberately does not translate Core, so unrelated base-game UI may use RimWorld's missing-translation fallback during such a product-only check.

Both scripts refuse to start while RimWorld is already running. They create a per-run `-savedatafolder`, write an isolated minimal `ModsConfig.xml`, use a dedicated `Player.log`, attach FlaUI only to the exact launched PID, and stop only that process. Gateway smoke also writes isolated `Prefs.xml` with `runInBackground=True`, `volumeMaster=0`, `volumeMusic=0`, fullscreen disabled, and a deterministic 1600×900 render size; matching Unity startup arguments prevent a full-desktop window from being created before the OS-minimized request takes effect. This RimWorld master-volume setting makes direct Gateway, MCP game, and grouped E2E launches silent by default without touching the OS mixer or normal preferences. Pass `-EnableAudio` (or MCP `enableAudio:true`) only for sound-focused work; master volume becomes `1` while music remains `0`. Pass `-VisibleWindow` for hands-on/computer-use interaction; `gateway-regression` selects a normal visible window automatically because it owns FlaUI and raw-input checks. Dry-run and completed results report the preference path, audio/master/music/render settings, Unity window arguments, and requested window style. Once the window is created, the launcher does not inspect native window state, so restoring or maximizing RimWorld cannot fail verification. Gateway smoke journals each request, requests a normal close first, and attempts a diagnostic dump before exact-PID force fallback if the owned process will not exit. FlaUI commands are child-process time-bounded; connection/service attempts are marked before invocation and reconciled to observed disconnected/stopped state even after malformed output or a timeout, while a pre-existing disconnected service is preserved. They hash the user's normal `ModsConfig.xml` before and after and fail if it changes; Gateway smoke applies the same protection to the user's normal `Prefs.xml`. Gateway smoke defaults to Core + RimWorld Dev Gateway. `-AdditionalModIds` validates and de-duplicates caller-supplied package IDs; when Harmony is present the launcher moves only it ahead of Core to honor its `loadBefore` declaration, preserves every other caller package's relative order after Core, and keeps Gateway last. The isolated configured order is checked against RimWorld's actual loaded-mod order through the raw endpoint. For a product test, also pass its repository project through `-AdditionalModProjectPaths` and name at least one exact owner/lifecycle/test through `-ExpectedIntegrationTests`; otherwise a Gateway-only run proves only the Gateway. Pass one or more `-ExpectedLogMarkers` when useful to prove that a product initializer reached a known-success marker; this is a startup prerequisite, not product-behavior acceptance. Common mod/gateway exception signatures are rejected independently.

For an explicitly active package ID duplicated across immediate Workshop item directories, Gateway smoke reads matching `About.xml` metadata without changing it. It proceeds only when one copy declares the current RimWorld major version, reports that choice in dry-run, and publishes a marker-owned physical local-mod copy before process start. Physical copying preserves the integration scanner's reparse-point safety boundary. After the exact owned process is confirmed stopped, cleanup validates ownership and deletes only that copy and its adjacent marker; an ambiguous selection or unverified cleanup fails closed. Inspect `workshop-overrides.json`, `workshop-override-cleanup.json`, and the `WorkshopOverrides` entry in `cleanup-status.json` for the retained contract.

Evidence is retained at:

- `artifacts\RimWorldSmoke\<UTC run id>` for Harmony + Core + XML Extensions + Immersive Chefs;
- `artifacts\GatewaySmoke\<UTC run id>` for Core + RimWorld Dev Gateway.

Each run retains its isolated mod list, build log, Player log, and screenshots, and its command result reports the before/after normal-configuration hashes. Gateway runs additionally retain API responses, a bounded finalized Steel Def export, raw-C# declaration/state/mutation-restore evidence, and a credential-free stopped session record. `-RunIntegrationTests` passes the mandatory complete ordered active package sequence to the host builder. The builder rejects omitted/empty input, validates each strict source manifest's reusable required/forbidden constraints or exact package sequence before build, and builds/stages every matching opted-in owner beneath marker-owned `artifacts\InGameIntegrationTests\StagingMods`. Exact mode also rejects reordered packages, built-manifest drift, and a mismatched real loaded order before the Gateway loads the test assembly. The smoke starts RimWorld with the exact discovery flag, polls the exact retryable `integration_test_status_pending` response while initial attachment commits, and rejects zero discovery, a staged assembly with no same-owner descriptor, omitted results, or unexpected discovery/test failures. It requires the terminal endpoint object and token-free session artifact to describe the same durable snapshot and retains both. Product project evidence also binds the deployed product DLL, About metadata, and XML patch hashes to that run. `-Quicktest` proves the checked-in quickstart fixture and an idempotent terminal replay on a playable map, then exercises the semantic game-state/camera, thing query/inspection/selection, direct spawn, native debug-action, gizmo, and typed designator routes with exact request IDs and restoration/cleanup evidence. It captures a log cursor before setup and writes the correlated post-setup JSON and log page into the same bundle; a log page may report `PageTruncated`. These are gateway/control diagnostics. Gameplay acceptance additionally requires the acting agent to perform a real player action through its native UI/game-command/job path, personally observe the player-visible result, and retain the exact action plus before/action/after evidence. Loaded Defs, patches, logs, API results, raw-C# assertions, direct mutations, and a static screenshot do not satisfy that gate alone.

The historical combined gateway-control record is `artifacts\GatewaySmoke\20260801T125136445Z`; it verified inside RimWorld the ordered isolated list Core, Harmony, Immersive Chefs, and RimWorld Dev Gateway plus the recorded gateway mechanics. It is not gameplay acceptance under `AGENTS.md` and must not be reused for a changed build. See [Gateway.md](Gateway.md) for the exact raw-execution, quickstart, screenshot, input, log-correlation, and cleanup assertions it proves.

## Repository-local skills

For a new distributable mod, start with [NewModChecklist.md](NewModChecklist.md), then read
`.agents/skills/rimworld-mod-development/SKILL.md`; it routes Harmony/XML, compatibility,
player-facing UI, localization, TDD, and live verification. Use `rimworld-dev-gateway` for launcher,
REPL, semantic/input control, scenarios, screenshots, or gateway failure diagnosis;
`rimworld-performance-benchmarking` for Circinus/DPA work; and the asset, balance, or release skill for
those specialized workflows. Use `rimworld-realistic-base-generation` before arranging a colony,
presentation showcase, or lived-in gameplay fixture so room geometry and optional-mod placement come from
inspected player references and mechanics rather than improvised test-map decoration. Validate changed skills
with the installed skill-creator validator:

```powershell
$validator = 'C:\Users\<you>\.codex\skills\.system\skill-creator\scripts\quick_validate.py'
Get-ChildItem .\.agents\skills -Directory | ForEach-Object {
  python $validator $_.FullName
  if ($LASTEXITCODE -ne 0) { throw "Skill validation failed: $($_.Name)" }
}
```

For distributable-mod language layout, contextual terminology, focused localization checks, and the seven-language live release gate, see [Localization.md](Localization.md).

## Extending a mod

1. Confirm or create an OpenSpec change with one owning mod and observable scenarios.
2. Read the relevant repository-local skill reference: Harmony/compatibility for C# patches, XML/Defs for assets and conditional patches, and testing/verification before making compatibility claims.
3. Add the smallest failing test, capture the intended red result, implement the vertical slice, and keep refactors green.
4. Keep optional integrations behind package-ID/Def resolution and a narrow adapter. Product mods must never reference or depend on the Dev Gateway.
5. Run the focused test or exact active-mod group plus the affected Release build/package inspection; then run an independent code review, apply accepted fixes, and rerun only affected verification. Run owning/full suites and complete matrices only at an explicit regression or release checkpoint.
6. Run final in-game acceptance on the reviewed build: perform the player action, personally observe the player-visible result, and retain before/action/after evidence from the exact process.
7. Mark only OpenSpec tasks backed by that complete evidence.
