# Immersive Chefs RimWorld mods

This repository is the RimWorld 1.6 development workspace for **Immersive Chefs** and its separate, developer-only verification gateway. OpenSpec contracts live at the repository root; playable mods live under `mods/`.

| Mod | Package ID | Current state |
| --- | --- | --- |
| Immersive Chefs | `fumblesneeze.immersivechefs` | Playable pre-release implementation with Stuff-aware kitchenware, meal service, sanitation, preparation, cooperative stations, culinary quality/temperature, dining standards, and guarded optional integrations. |
| RimWorld Dev Gateway | `fumblesneeze.rimworlddevgateway` | Authenticated loopback development API, in-game integration-test runner, and unrestricted in-process C# REPL. Never enable it for ordinary or untrusted play. |

Immersive Chefs does not reference or ship the gateway. Harmony is its only required gameplay dependency; all other mod integrations are optional and resolved at runtime.

## Current gameplay

Immersive Chefs adds cookware sets, plates, stackable cutlery, belt-slot chef's knives, preparation and specialist stations, hand dishwashing, two dishwasher sizes, a microwave, meal temperature/quality, and colony/Royalty dining expectations. Covered meal recipes reserve cookware and plates, diners collect place settings, and used ware returns dirty for the Cleaning work type. Simple/Fine/Lavish recipes use separate configurable work multipliers; pemmican and travel/packaged meals remain hand foods.

Kitchenware is Stuff-aware. Primitive stone/adobe/wood paths, smithy-era metals, machining-era metals/plastics, and trade/quest-only self-cleaning glitterworld cookware use material and craftsmanship to derive cleanliness, speed, comfort, durability, and culinary modifiers. Optional adapters are detected for Processor Framework, Expanded Materials, ABS polymer, Dubs Bad Hygiene, Gastronomy, Hospitality, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, and Vanilla Nutrient Paste Expanded. Their individual `Auto` setting can be changed to `Off` when troubleshooting.

Active cooking, dining, service, and assistant jobs safely restart after loading a save. RimWorld persists the exact physical ware; Immersive Chefs returns any session-carried items and retries the interrupted job so process-local coordination state cannot duplicate or strand them.

This is still pre-release. Current-schema saves are supported, but migration from older development builds and uninstall cleanup are not. Ceramic/porcelain content, Vanilla Cooking Expanded complexity classification, food preservation, and food waste are deliberately deferred.

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

Gateway launches write an isolated `SavedData\Config\Prefs.xml` with `runInBackground=True` and `volumeMusic=0`, then start RimWorld minimized by default, so semantic/API-driven verification can continue without music or taking over the desktop. Add `-VisibleWindow` only when a person, computer-use tool, or desktop input check needs the game window; `-Scenario gateway-regression` selects a visible window automatically because it owns the FlaUI/raw-input checks. Dry-run and completed results report `Prefs`, `RunInBackground`, `MusicVolume`, and the requested `LaunchWindowStyle`; real runs also retain an informational exact-PID window snapshot. The user remains free to restore or maximize the window after launch. The launcher hashes the user's normal `Prefs.xml` before and after and fails if it changed.

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

For a quiet playable quicktest (wait for the map and run only read-only raw-C#/live-mod health checks; do not spawn, select, click, pan, zoom, toggle game state, or run a setup scenario):

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300
```

Run the comprehensive Gateway surface regression explicitly when that is what you intend to test:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -Scenario gateway-regression -TimeoutSeconds 300
```

Use the kitchenware-fabrication scene to exercise two real one-shot bills side by side: primitive granite cookware at a crafting spot and modern steel cookware at a powered machining table. The generated workers are restricted to their bill, both native `DoBill` jobs are armed while paused, and no product is synthesized. Unpause with Space and choose a native speed; afterward exactly one Stuff-colored cookware set should lie at each station, with the granite inspector showing the deliberately inferior primitive profile and the steel inspector showing the modern profile:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-kitchenware-fabrication `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the larger route matrix to exercise wooden plates and cutlery, fixed-adobe plates from the real Expanded Materials - Masonry Def, and silver cookware, plates, and cutlery at fueled smithies. It creates six real one-shot bills with exact ingredient stacks and pauses after vanilla accepts each `DoBill` job; unpause with Space and select a native speed to watch the work complete:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-kitchenware-route-matrix `
  -InteractiveHoldSeconds 240 `
  -AdditionalModIds 'brrainz.harmony','argon.corelib','oskarpotocki.vanillafactionsexpanded.core','argon.expandedmaterials.masonry','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the chef's-knife scene to watch a real machining bill consume exactly 30 steel. The pawn begins with an ordinary steel weapon as a control. After fabrication, select the pawn, right-click the produced set, and use the native `Force equip` apparel action; RimWorld 1.6 displays belt/utility apparel in the Gear tab's `Equipment` group while leaving the weapon equipped:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-chefs-knife `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the glitterworld-trade scene to verify that exceptional self-cleaning cookware is acquired rather than manufactured. The scenario builds a powered orbital trade fixture, places colony silver under its beacon, and gives an exotic-goods trader one Good-or-better set without granting it to the colony. Complete the purchase through RimWorld's native trade dialog, advance time for the drop-pod delivery, and inspect the delivered set:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-glitterworld-trade `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Start directly at the reusable Immersive Chefs caravan-dining scene, with the caravan selected, its native Items tab showing a plated meal plus cutlery (the plate is embedded and therefore not yet a separate inventory row), the pawn hungry, and the game paused:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-caravan-dining `
  -InteractiveHoldSeconds 900 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the equivalent animal-exclusion scene to observe a Labrador retriever eat an intentionally cold, poor, contaminated plated meal without collecting the visible cutlery. Its fully fed human escort has no Plants skill, preventing caravan forage from masking which food the animal consumes:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-animal-caravan-dining `
  -InteractiveHoldSeconds 120 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the regular-map animal scene to observe the same exclusion through RimWorld's ordinary map ingest job. A selected factionless wild raccoon starts hungry inside a small sealed fixture beside the plated meal and loose cutlery; after unpausing, the meal should disappear, the clean embedded plate should appear at the eating location, and the cutlery should not move:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-animal-map-dining `
  -InteractiveHoldSeconds 120 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the cutlery-free colonist scene to verify the opposite humanlike behavior. It selects a colonist with a plated Simple meal and no cutlery in a sealed concrete room. Unpause with the game's native Space control: the meal should disappear, the returned plate and one dirt event should become visible at the eating location, and the selected pawn's Needs panel should report `Ate without cutlery`:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-cutlery-free-map `
  -InteractiveHoldSeconds 120 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the Hospitality guest scene to verify colony-first and personal-inventory fallback behavior. It creates two arrived guests in separate rooms, gives both personal cutlery, exposes golden colony cutlery only to the first, arms their native ingest jobs, and pauses. Unpause with Space; after eating, the first guest should return the golden setting dirty while retaining clean personal cutlery, and the second should retain its now-dirty personal setting in the Gear inventory:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-hospitality-guest-dining `
  -InteractiveHoldSeconds 150 `
  -AdditionalModIds 'brrainz.harmony','orion.hospitality','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Hospitality adapter active'
```

Use the Biotech child scene to verify the independent-eating boundary. It creates a real eight-year-old child with one plated lavish meal and one clean golden cutlery setting, arms the exact fixture meal's native ingest job, and pauses. Unpause with Space and choose a native speed; afterward the meal should be gone, both exact service items should be visibly dirty, and the child's Needs panel should show `Legendary cooking`, `Steaming hot meal`, and `Proper place setting`. The loaded integration test also asks the full vanilla think trees to prove that the child receives `JobGiver_GetFood` while a two-year-old toddler does not receive an ordinary ingest job:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests -VisibleWindow `
  -Scenario immersive-chefs-independent-child-dining `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'ludeon.rimworld.biotech','brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|PlayableMapLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ActiveBiotechIndependentChildCompletesOrdinaryDiningWorkflow' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the patient-feeding scene to compare three real `FeedPatient` jobs side by side: a nurse with cutlery and a cold plated meal, a conscious patient without cutlery, and an unconscious patient without cutlery. The first nurse should collect the cutlery and visibly reheat before feeding; only the conscious no-cutlery patient should receive `Ate without cutlery`; both no-cutlery feedings should create one native dirt event at the patient cell:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-patient-feeding `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Keep an otherwise unmodified isolated game open for hands-on behavior checks:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow -InteractiveHoldSeconds 900 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Scenario descriptors live in `scripts\Scenarios`. Selecting one is always explicit through `-Scenario`; omitting the option never runs one. Required packages are checked against both the configured set and RimWorld's actual live loaded set. Screenshot step names must be unique and begin with `scenario-`, preventing them from replacing standard evidence. The hold is bounded to 0–3600 seconds, begins only after the selected setup and normal non-mutating readiness assertions pass, records its state in the ignored evidence directory, and still uses exact-PID cleanup. The final flushed Player log is rescanned after scenario/hold execution and exact-process shutdown. During the hold, use the generated `SavedData\DevGateway\current.json` only as a live local credential; never commit or paste it.

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
