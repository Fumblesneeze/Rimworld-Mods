# Immersive Chefs RimWorld mods

This repository is the RimWorld 1.6 development workspace for **Immersive Chefs** and its separate, developer-only verification gateway. OpenSpec contracts live at the repository root; playable mods live under `mods/`.

| Mod | Package ID | Current state |
| --- | --- | --- |
| Immersive Chefs | `fumblesneeze.immersivechefs` | Loadable and host-tested baseline. The accepted cookware, tableware, sanitation, preparation, station, temperature, and meal-quality behavior is specified but not implemented yet. |
| RimWorld Dev Gateway | `fumblesneeze.rimworlddevgateway` | Authenticated loopback development API, in-game integration-test runner, and unrestricted in-process C# REPL. Never enable it for ordinary or untrusted play. |

Immersive Chefs does not reference or ship the gateway. Harmony is its only required gameplay dependency; all other mod integrations are optional and resolved at runtime.

## Install the toolchain

Development is Windows-only because RimWorld and the live UI checks run on Windows. Install these tools rather than copying them into this repository:

| Tool | Required for | Supported setup |
| --- | --- | --- |
| [Git for Windows](https://git-scm.com/download/win) | source control | Current Git; `winget install --id Git.Git -e` |
| [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/install-powershell-on-windows) | all repository scripts | `winget install --id Microsoft.PowerShell -e --source winget` |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | restore, build, and tests | Install SDK `8.0.401`, or a compatible newer .NET 8 feature band allowed by `global.json` |
| [RimWorld](https://store.steampowered.com/app/294100/RimWorld/) | game assemblies and live verification | Steam, RimWorld 1.6 |
| [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) | loading Immersive Chefs | Subscribe to Workshop item `2009463077` (`brrainz.harmony`) |
| [Node.js LTS](https://nodejs.org/en/download) | installing OpenSpec | `winget install --id OpenJS.NodeJS.LTS -e` |
| [OpenSpec](https://github.com/Fission-AI/OpenSpec) | contract validation | `npm install -g @fission-ai/openspec@1.2.0` |
| [FlaUiCli](https://github.com/opstudio-eu/FlaUiCli) | optional native-window smoke evidence | Build the current CLI and place `flaui.exe` on `PATH`; it also requires .NET 8 |
| Python 3 | optional repository-skill validation | Any current Python 3 on `PATH` |

The NuGet restore downloads the pinned Zlepper RimWorld SDK/testing packages, EmbedIO, Mono.CSharp, NUnit, and other managed build dependencies. Do not install or commit those packages manually.

Verify the command-line tools in a new PowerShell 7 session:

```powershell
git --version
pwsh --version
dotnet --version
node --version
openspec --version
flaui --version # optional until a live desktop run
```

FlaUiCli currently provides a source build rather than a required repository binary:

```powershell
git clone https://github.com/opstudio-eu/FlaUiCli.git C:\Tools\FlaUiCli-src
Set-Location C:\Tools\FlaUiCli-src
dotnet publish .\src\FlaUiCli\FlaUiCli.csproj -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o C:\Tools\FlaUiCli
# Add C:\Tools\FlaUiCli to the user PATH, then open a new terminal.
```

## Configure local paths

Checked-in defaults are:

- RimWorld: `F:\Steam\steamapps\common\RimWorld`
- Workshop content: `F:\Steam\steamapps\workshop\content\294100`
- managed assemblies: `<RimWorld>\RimWorldWin64_Data\Managed`

Every smoke/test script accepts `-RimWorldPath` and `-SteamModContentFolder`. For a normal build on another drive, override the MSBuild defaults:

```powershell
dotnet build .\ImmersiveChefs.sln -c Release --nologo `
  -p:DefaultRimWorldPath='D:\SteamLibrary\steamapps\common\RimWorld' `
  -p:DefaultSteamModContentFolder='D:\SteamLibrary\steamapps\workshop\content\294100'
```

The test wrapper also accepts `-HarmonyAssemblyPath` when Harmony cannot be found below the Workshop folder. Workshop content is always read-only input: never edit it or deploy a development package into it.

## Configure Codex-assisted development

The repository commits its purpose-built skill at `.codex/skills/rimworld-mod-development`. Generic or downloaded skills are deliberately local and ignored. For Codex-assisted changes, make `tdd`, `code-review`, `flaui-cli`, and the OpenSpec skills available from the user's trusted Codex skill library under `.codex/skills/`; `openspec update` refreshes OpenSpec's supported assistant instructions.

The optional skill check is available when Codex's `skill-creator` package is installed:

```powershell
python C:\Users\<you>\.codex\skills\.system\skill-creator\scripts\quick_validate.py `
  .\.codex\skills\rimworld-mod-development
```

Read [AGENTS.md](AGENTS.md) before changing game-facing code. TDD, code review, logs, API responses, and in-game integration tests are supporting checks; accepting RimWorld behavior additionally requires the acting agent to perform a player workflow and personally observe the resulting behavior in the running game.

## Restore, build, and test

From the repository root:

```powershell
dotnet restore .\ImmersiveChefs.sln
dotnet build .\ImmersiveChefs.sln -c Release --nologo
.\scripts\Invoke-Tests.ps1 -Configuration Release
openspec validate --all --strict --no-interactive
```

Focused test environments are separate on purpose:

```powershell
# All Immersive Chefs host environments
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs -Configuration Release

# No Harmony, loaded mods, or populated representative Def database
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Unit -Configuration Release

# Explicitly owned Harmony patches
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Harmony -Configuration Release

# Constructed, scoped Verse Def fixtures
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Defs -Configuration Release

# Gateway host tests
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release
```

`Invoke-Tests.ps1` rejects zero-test/all-ignored runs. A real loaded mod, XML `PatchOperation`, Core/Workshop Def database, `DefOf`, or complete patch set belongs in a fresh RimWorld process with the exact ordered mod list; see [TestingEnvironments.md](docs/TestingEnvironments.md).

## Validate an isolated RimWorld run

Inspect a dry run first:

```powershell
.\scripts\Invoke-RimWorldSmoke.ps1 -DryRun -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -DryRun -Output json
.\scripts\Build-InGameIntegrationTests.ps1 `
  -ActivePackageIds @('ludeon.rimworld','fumblesneeze.rimworlddevgateway') `
  -DryRun -Output json
```

Run the product-only startup smoke without the gateway:

```powershell
.\scripts\Invoke-RimWorldSmoke.ps1 -TimeoutSeconds 180
```

Run the gateway and its staged in-game tests:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests -TimeoutSeconds 180
```

Run the Immersive Chefs XML/integration matrix with both loaded mods:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|MainMenuLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ImmersiveChefsXmlProbeContainsItsFinalPatch' `
  -TimeoutSeconds 180
```

For a playable quickstart:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
```

The runners refuse to reuse an existing RimWorld process, use a unique `-savedatafolder`, preserve the normal `ModsConfig.xml`, bind evidence and cleanup to the launched process, and remove only their owned live mod stage. The gateway exposes unrestricted code execution by design, so use it only in an isolated developer session. Read [Gateway.md](docs/Gateway.md) before its first live run.

## Repository layout and ignored output

```text
mods/       playable mod source and XML
openspec/   proposals, designs, capability contracts, and task state
shared/     host-safe contracts shared by projects
tests/      host and in-game integration-test source
tools/      first-party companion/client source required by the solution
scripts/    build, test, staging, and isolated smoke commands
docs/       maintained development and API documentation
artifacts/  generated packages, logs, TRX files, screenshots, and evidence (ignored)
```

Generated `bin/`, `obj/`, packages, test results, screenshots, local dependency audits, evidence ledgers, downloaded tools, generic local skills, caches, binaries, and credentials are ignored. Generated `About/About.xml` files come from the Zlepper SDK and are also ignored. First-party source below `tools/` remains versioned because the solution builds and tests it.

Do not commit tokens, a gateway `current.json`, normal RimWorld configuration/saves, Workshop assemblies, or files from `artifacts/`. Build products are created below `artifacts/Mods`, `artifacts/GameMods`, `artifacts/HostTools`, and `artifacts/TestResults`.

## Design and workflow references

- [Development workflow](docs/Development.md)
- [Host tests, Harmony, loaded mods, and Defs](docs/TestingEnvironments.md)
- [Dev Gateway security, API, and evidence](docs/Gateway.md)
- [Accepted Immersive Chefs gameplay contract](openspec/changes/specify-immersive-chefs-gameplay/proposal.md)
- [Dev Gateway OpenSpec change](openspec/changes/add-rimworld-dev-gateway/proposal.md)

Food preservation, food waste, save migration for an unreleased mod, and ceramics research without a selected integration are explicit future scope.
