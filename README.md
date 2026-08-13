# Immersive Chefs RimWorld mods

This repository is the RimWorld 1.6 development workspace for **Immersive Chefs** and its separate, developer-only verification gateway. OpenSpec contracts live at the repository root; playable mods live under `mods/`.

| Mod | Package ID | Current state |
| --- | --- | --- |
| Immersive Chefs | `fumblesneeze.immersivechefs` | Playable RimWorld 1.6 release with Stuff-aware kitchenware, meal service, sanitation, preparation, cooperative stations, culinary quality/temperature, dining standards, and guarded optional integrations. |
| RimWorld Dev Gateway | `fumblesneeze.rimworlddevgateway` | Authenticated loopback development API, in-game integration-test runner, and unrestricted in-process C# REPL. Never enable it for ordinary or untrusted play. |

Immersive Chefs does not reference or ship the gateway. Harmony is its only required gameplay dependency; all other mod integrations are optional and resolved at runtime.

## Current gameplay

Immersive Chefs adds cookware sets, plates, stackable cutlery, belt-slot chef's knives, preparation and specialist stations, hand dishwashing, two dishwasher sizes, a fallback microwave, meal temperature/quality, and colony/Royalty dining expectations. Covered meal recipes reserve cookware and plates, diners collect place settings, and used ware returns dirty for the Cleaning work type. Simple/Fine/Lavish recipes use separate configurable work multipliers; pemmican and travel/packaged meals remain hand foods. When exact optional package `Mlie.DThermodynamicsHotMeals` is active, Thermodynamics - Hot Meals exclusively owns meal temperature, temperature thoughts, heating jobs/toils, settings, and the microwave; Immersive Chefs removes its fallback microwave before Def construction and keeps only its non-temperature culinary, ware, and sanitation behavior.

Kitchenware is Stuff-aware. Primitive stone/adobe/wood paths, smithy-era metals, machining-era metals/plastics, and trade/quest-only self-cleaning glitterworld cookware use material and craftsmanship to derive cleanliness, speed, comfort, durability, and culinary modifiers. Optional adapters are detected for Processor Framework, Expanded Materials, ABS polymer, Dubs Bad Hygiene, Gastronomy, Common Sense, Hospitality, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, and Vanilla Nutrient Paste Expanded. Their individual `Auto` setting can be changed to `Off` when troubleshooting.

The supported exact-package food ecosystem includes Adaptive Meal Bill, Meals on Wheels, Replimat Meals, the Vanilla Cooking Expanded family, Fried/Fast Meals, Food Texture Variety, Dynamic Meal Texture Replacer, Prioritize Meals over Preserved Foods, RimCuisine 2, Meal Printer, RimFridge, Adaptive Storage Framework with `[sbz] Fridge`, Overcooked Meals, No Vanilla Meals, and Thermodynamics - Hot Meals. See the [player guide](docs/ImmersiveChefs.md) for every exact package chain, ownership boundary, setting, incompatibility grouping, save guarantee, and troubleshooting path.

Active cooking, dining, service, and assistant jobs safely restart after loading a save. RimWorld persists the exact physical ware; Immersive Chefs returns any session-carried items and retries the interrupted job so process-local coordination state cannot duplicate or strand them.

Current-schema saves are supported, but migration from older development builds and uninstall cleanup are not. Ceramic/porcelain content, food preservation, and food waste are deliberately deferred.

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

The repository commits its complete agent skill set under the cross-client `.agents/skills/` location:

- `rimworld-mod-development` routes OpenSpec, TDD, Harmony/XML, compatibility, localization, testing,
  packaging, and native acceptance work;
- `rimworld-dev-gateway` covers isolated launchers, REPL/control/input/screenshot operation, scenarios,
  failure diagnosis, and safe Gateway extension;
- `rimworld-performance-benchmarking` records the Circinus-first benchmark contract and separate DPA
  diagnostic boundary, while requiring agents to check unfinished OpenSpec tasks before naming commands;
- `rimworld-asset-generation`, `rimworld-game-balance`, and `release-rimworld-mods` cover their
  specialized workflows and route back to the common acceptance gate;
- `steam-workshop-feedback` maintains the human-reviewed player-feedback ledger;
- `tdd`, `code-review`, `flaui-cli`, and the OpenSpec workflow skills make the full development
  process reproducible without a separately installed project skill library.

Agent clients that support the `.agents/skills/` convention discover these packages directly from
the checkout. `openspec update` refreshes OpenSpec's supported assistant instructions when needed.

The optional skill check is available when Codex's `skill-creator` package is installed:

```powershell
$validator = 'C:\Users\<you>\.codex\skills\.system\skill-creator\scripts\quick_validate.py'
Get-ChildItem .\.agents\skills -Directory | ForEach-Object {
  python $validator $_.FullName
  if ($LASTEXITCODE -ne 0) { throw "Skill validation failed: $($_.Name)" }
}
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

# Deterministic Gateway metadata and PNG snapshots
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway.Snapshots -Configuration Release
```

`Invoke-Tests.ps1` rejects zero-test/all-ignored runs. A real loaded mod, XML `PatchOperation`, Core/Workshop Def database, `DefOf`, or complete patch set belongs in a fresh RimWorld process with the exact ordered mod list; see [TestingEnvironments.md](docs/TestingEnvironments.md).

Gateway snapshots use a separate `net48` NUnit 4 project with current Verify and Verify.ImageMagick packages. The existing Zlepper projects remain on NUnit 3 because moving their assertion surface to NUnit 4 creates incompatible delegate overloads. Approved deterministic `.verified.*` baselines are committed; transient `.received.*` files are ignored. Live RimWorld frames remain scenario evidence unless a test deliberately controls or masks every dynamic pixel.

## Validate an isolated RimWorld run

Inspect a dry run first:

```powershell
.\scripts\Invoke-RimWorldSmoke.ps1 -DryRun -Output json
.\scripts\Invoke-GatewaySmoke.ps1 -DryRun -Output json
.\scripts\Build-InGameIntegrationTests.ps1 `
  -ActivePackageIds @('ludeon.rimworld','fumblesneeze.rimworlddevgateway') `
  -DryRun -Output json
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json
```

Gateway launches write an isolated `SavedData\Config\Prefs.xml` with `runInBackground=True`, `volumeMusic=0`, fullscreen disabled, and a deterministic 1600×900 render size. They pass the same windowed size to Unity before window creation and request an OS-minimized start so semantic/API-driven verification can continue without music. Windows may expose Unity's newly created HWND in its normal state for a few milliseconds before applying that request; the launcher neither maximizes the game nor treats that startup transition as a user action. Add `-VisibleWindow` only when a person, computer-use tool, or desktop input check needs the game window; `-Scenario gateway-regression` selects a visible window automatically because it owns the FlaUI/raw-input checks. Dry-run and completed results report the isolated preferences, render mode, Unity window arguments, and requested `LaunchWindowStyle`. After startup the launcher does not inspect or enforce window state: the user remains free to restore or maximize the window without affecting the run. The launcher hashes the user's normal `Prefs.xml` before and after and fails if it changed.

If an explicitly active package ID exists in multiple Workshop items, Gateway smoke refuses RimWorld's arbitrary duplicate selection. Dry-run reports the sole copy that declares the current major version; a real run places a marker-owned physical copy beneath local `Mods` before launch and removes only that copy after the exact process exits. Zero or multiple current-version candidates fail before launch, and Workshop content remains read-only. Publication and cleanup evidence is retained as `workshop-overrides.json` and `workshop-override-cleanup.json`.

Run the product-only startup smoke without the gateway:

```powershell
.\scripts\Invoke-RimWorldSmoke.ps1 -TimeoutSeconds 180
```

Run the gateway and its staged in-game tests:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests -TimeoutSeconds 180
```

Run dynamically loaded, multi-frame E2E workflows grouped by their exact declared mod lists:

```powershell
# Discover/build tests and print every planned exact-mod launch without touching the game install.
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json

# Run every resolvable group, one fresh minimized RimWorld process per group.
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TimeoutSeconds 300 -Output table

.\scripts\Invoke-RimWorldEndToEndTests.ps1 `
  -TestId immersive-chefs.localization-rendering `
  -Language German `
  -TimeoutSeconds 300 `
  -Output json

# Run one stable group ID.
.\scripts\Invoke-RimWorldEndToEndTests.ps1 `
  -GroupId 'brrainz.harmony|ludeon.rimworld|fumblesneeze.immersivechefs' `
  -TimeoutSeconds 300 `
  -Output json

# During feature work, run only the stable test IDs currently being changed.
.\scripts\Invoke-RimWorldEndToEndTests.ps1 `
  -TestId 'immersive-chefs.countertop-microwave-support-loss' `
  -TimeoutSeconds 300 `
  -Output json
```

The runner discovers downloaded package IDs from the game, local mods, DLC data, and Workshop folders; transports both the large discovery inventory and each child process's exact additional-mod order through UTF-8 package-ID files; deploys repo-owned product mods before publishing marker-owned test bundles; and always removes its exact leased stage. `-TestId` validates stable IDs against that plan and makes the in-game Gateway admit and execute only those exact tests. `-Language` accepts one bounded RimWorld language-folder name, writes it only to the disposable process's `Prefs.xml`, records it in evidence, and defaults to `English`; repeat a localization test in separate processes for every required locale. When Core lacks a requested locale, the launcher uses a recorded, hash-verified lease of the Gateway's metadata-only language provider and removes it after the exact process exits without overwriting an installed provider. Use the runner for active feature slices; reserve unfiltered groups or all groups for explicit maintenance regression and release preparation. Each group gets an isolated save-data/config directory, aggregate JSON, XML-safe JUnit, durable endpoint state, and step screenshots below `artifacts\EndToEndRuns\Grouped`. The short `smoke-NNN` runtime branch avoids Unity Mono's classic Windows path-length failure while the descriptive group report directory remains intact. E2E assemblies stay outside product `Assemblies` and ordinary NUnit/VSTest runs. A passing result proves the codified workflow, but the acting agent must still inspect that run's native before/action/after screenshots before accepting game behavior.

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

Use the cooking-ware-selection scene to compare clean-first selection with urgent dirty fallback. Two sealed kitchens each contain a real Fueled stove and one pawn-restricted Simple-meal bill. The first offers deliberately inferior clean steel ware beside superior dirty golden ware; the second offers only dirty steel ware. A third sealed, starving pawn registers demand through vanilla's patched food-seeking boundary. Unpause and choose a native speed: both cooks should finish, but the golden controls must remain untouched while the used cookware moves to its stove and each produced meal contains one bound plate:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-cooking-ware-selection `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the recipe-complexity scene to compare the exact initial vanilla timing table. It creates three sealed, otherwise matched fueled-stove kitchens, assigns `CookMealSimple`, `CookMealFine`, and `CookMealLavish` through real one-shot bills, asks finalized `WorkGiver_DoBill` for all three native jobs, and pauses before work begins. Unpause at Normal speed: the Simple meal should visibly finish first, the Fine meal second, and the Lavish meal last:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-recipe-complexity `
  -InteractiveHoldSeconds 180 `
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

Use the Processor Framework dishwasher scene to exercise its real fill work giver without Dubs. The passive sealed fixture contains one powered domestic dishwasher, three hauling-only pawns, and exact dirty cookware, plate, and cutlery stacks totaling 5.25 plate-equivalents. Resume at Normal speed: the ordinary inspector should remain at zero washing progress while all three native hauling jobs join the loading batch, then advance and return those same items clean:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-processor-dishwasher `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','syrchalis.processor.framework','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Processor Framework adapter active; reusable dish identity is preserved.'
```

Use the interruption variant to verify the player-facing pause, resume, and cancellation paths. It adds one Basic-only switch worker but still starts no job or cycle. After native hauling begins the wash, use the dishwasher's `Designate toggle power` gizmo and let that pawn flick it off; progress must remain unchanged while time advances. Toggle it back on, verify progress resumes from the same percentage, then use `Eject dishes` and inspect the returned exact items: all three must still be dirty and retain their original damage.

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-processor-dishwasher-interruption `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','syrchalis.processor.framework','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Processor Framework adapter active; reusable dish identity is preserved.'
```

Add Dubs Bad Hygiene with the matching scenario to use a real connected water tower holding exactly 10.0 L. During the complete loading window the tower must remain at 10.0 L; once the 5.25-equivalent batch starts washing it should read 9.5 L, remain there through completion, and all three exact ware items should return clean:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-dubs-processor-dishwasher `
  -InteractiveHoldSeconds 240 `
  -AdditionalModIds 'brrainz.harmony','syrchalis.processor.framework','Dubwise.DubsBadHygiene','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Processor Framework adapter active; reusable dish identity is preserved.','[ImmersiveChefs] Dubs Bad Hygiene adapter active; dishwashers require supplied plumbing.'
```

Use the Common Sense cleanup scene to verify the optional post-dining handoff. It creates a selected self-diner, a drafted Doctor-capable nurse, a conscious hungry patient in a medical bed, two plated meals with cutlery, and a powered dishwasher. Right-click the diner's meal and choose the native `Consume simple meal` action. After that place setting reaches the dishwasher, select and undraft the nurse, right-click the patient, and choose `Prioritize feeding`. The dishwasher's ordinary inspector should reach 2.5 place settings—two plates plus two cutlery units—without any scenario-injected ingest, feed, or cleanup job:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-common-sense-cleanup `
  -InteractiveHoldSeconds 240 `
  -AdditionalModIds 'brrainz.harmony','avilmask.commonsense','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Common Sense adapter active'
```

Use the Common Sense no-route scene to verify safe release when cleanup is initially impossible. It creates one selected diner with an exact plated meal and cutlery inside a sealed dry room; the only dishwasher is powered but switched off. Choose the native `Consume simple meal` action and let it finish. The returned plate and cutlery should remain freely selectable and visibly dirty. Select the dishwasher, use its native power-toggle designation, and resume; ordinary Basic/Cleaning work should switch it on, collect both exact items, and return them clean after the cycle. The scenario never starts an ingest, flick, or cleaning job:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-common-sense-no-route `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','avilmask.commonsense','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.','[ImmersiveChefs] Common Sense adapter active'
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

Use the product-owned countertop-microwave E2E test for the ordinary self-feeding path. It starts minimized, arranges a hungry drafted colonist with an exact frozen plated meal and clean cutlery beside a powered countertop microwave, then uses typed native Undraft and time controls. The retained screenshots cover active reheating, the steaming reheated meal, completed dining, and the exact dirty returned setting:

```powershell
.\scripts\Invoke-RimWorldEndToEndTests.ps1 `
  -TestId immersive-chefs.countertop-microwave-native-reheat `
  -Output json
```

Use the meal-cooling-holders scene to compare ordinary ambient, refrigerated, and frozen storage. It creates three identical plated Simple meals at 70 °C in separate sealed rooms, using a real powered heater set to 21 °C, a cooler set to 5 °C, and a cooler set to -10 °C. The scenario pauses without advancing temperature. Resume through native game-speed control for about two in-game hours, pause, and select each exact meal. With the default two-hour half-life, their inspectors should visibly separate into Warm, RoomTemperature, and Frozen bands:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-meal-cooling-holders `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the meal-serving-stack scene to observe a native one-serving split without synthetic component calls. It creates a paused stack whose ordered culinary records are Awful 20, Good 50, and Masterwork 80, with three embedded plates, three clean cutlery units, and one starving drafted diner. The initial selected inspector must show `Simple meal x3`, `Bound plates: 3`, and `Masterwork (80)`. Select the diner, invoke the native Undraft gizmo, and resume game time. After one ordinary ingestion, pause and reselect the same meal handle; it must visibly show `Simple meal x2`, `Bound plates: 2`, and `Good (50)`:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-meal-serving-stack `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the handheld-food exclusion scene to verify that Pemmican and packaged survival meals stay outside the serving-ware workflow. It builds two sealed rooms with one hungry colonist and one exact food fixture each, plus clean plate and cutlery controls. The arm step asks vanilla `JobGiver_GetFood` for each exact native ingest job and pauses. Resume at a native speed; Pemmican should decrease, the survival meal should disappear, and all four controls should remain clean without dirt or a returned dish:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-handheld-food-exclusions `
  -InteractiveHoldSeconds 120 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the nutrient-paste dining scene to verify the ordinary vanilla dispenser path. It creates a real powered dispenser, hopper, and forbidden rice feedstock beside one hungry colonist plus exact clean steel plate and cutlery controls. The arm step asks vanilla `JobGiver_GetFood` for the exact dispenser-targeted `Ingest` job and pauses. Resume at a native speed; the pawn should visibly carry and consume the freshly dispensed paste meal, then the meal should disappear and the same plate and cutlery should return dirty:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-nutrient-paste-dining `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the prepared-paste scene to verify the separate player-ordered ingredient output. It creates and closely frames a real powered dispenser/hopper fixture with one selected operator and no plate or cutlery. The scenario deliberately does not start a job: right-click the dispenser, choose `Dispense prepared cooking paste`, then resume at Normal speed. The resulting prepared-ingredient stack should visibly report preparation quality 20, source `nutrient paste`, and nutrition per unit 0.05 rather than becoming an edible nutrient-paste meal:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-prepared-paste-dispensing `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the VNPE variant to exercise the installed mod's real pipe-backed tap and vat. The explicit setup connects and powers the native network and starts it at 12 stored meals, but does not order or dispense anything. Right-click the tap, choose `Dispense prepared cooking paste`, and resume at Normal speed. The prepared stack should expose the same paste metadata while the vat falls to 11:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow `
  -Scenario immersive-chefs-vnpe-prepared-paste `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','oskarpotocki.vanillafactionsexpanded.core','vanillaexpanded.vnutriente','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Vanilla Nutrient Paste Expanded adapter active'
```

Use the imported-meal plating scene to verify trade, quest, drop-pod, scenario, or mod-created covered meals without recooking them. The passive setup creates an unplated three-meal stack with Rice provenance, quality 73, temperature/rot state, three clean plates, a fueled stove, and one Cooking-only pawn, then pauses without calling the plating workgiver or starting a job. Resume game time; the native work scheduler should move the stack to the stove and its ordinary inspector should change from `Service ware: unplated` to `Bound plates: 3` while the existing food data remains:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests -VisibleWindow `
  -Scenario immersive-chefs-imported-meal-plating `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|PlayableMapLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.ImportedMealPlatingQueueAndDiningGateAreFinalized' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the hidden-provenance scene to verify paste-derived ingredient eligibility and food restrictions without exposing hopper contents. It creates two paused sealed rooms. In each room the closer left stack contains hidden human meat and the farther right stack contains hidden rice; the native cooking bill and diner policy allow rice but reject human meat. Resume through a native speed and confirm both right stacks are consumed while both closer left stacks remain:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -Scenario immersive-chefs-hidden-provenance `
  -InteractiveHoldSeconds 180 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the terminal plate-conservation scene to verify expiry and fire without assuming a material name. It dynamically chooses one allowed plate Stuff with positive finalized `Flammability` and one with zero finalized `Flammability`, creates three isolated plated meals, and pauses without destroying or burning a target. Resume game time to let the selected meal rot, then use RimWorld's native `T: Attach Fire` developer action on each 1-HP control. Expiry must return the exact first plate dirty; fire must consume the positive-flammability plate and return the exact zero-flammability plate dirty:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests `
  -Scenario immersive-chefs-terminal-plate-conservation `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|PlayableMapLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.TerminalMealDestructionUsesEffectivePlateFlammabilityAndConservesIdentity' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Use the kitchenware-alert scene to verify the deliberately narrow, intent-aware alert. It creates an operational fueled stove with a runnable simple-meal bill, an undrafted active cook, dirty forbidden cookware and plate supply, plus a campfire bill and raw berries as silent controls. It pauses without querying the alert, ticking the game, or assigning a job. The dirty ware physically exists, so `Missing kitchenware` must initially be absent. With dev mode enabled, use the selected items' native destroy actions to remove both products; the alert must then appear and cycle to the fueled stove, never the campfire or berries. Drafting the only active cook must suppress the alert during combat, undrafting must restore it, and natively spawning either missing product must remove only that product from the explanation until both exist:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests `
  -Scenario immersive-chefs-kitchenware-alerts `
  -InteractiveHoldSeconds 300 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedIntegrationTests 'fumblesneeze.immersivechefs|PlayableMapLoaded|ImmersiveChefs.InGame.IntegrationTests.FinalizedImmersiveChefsIntegrationTests.KitchenwareAlertOnlyReportsAbsentWareForRunnableOwnedKitchenBills' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Keep an otherwise unmodified isolated game open for hands-on behavior checks:

```powershell
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow -InteractiveHoldSeconds 900 `
  -AdditionalModIds 'brrainz.harmony','fumblesneeze.immersivechefs' `
  -AdditionalModProjectPaths '.\mods\ImmersiveChefs\ImmersiveChefs.csproj' `
  -ExpectedLogMarkers '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs.'
```

Scenario descriptors live in `scripts\Scenarios`. Selecting one is always explicit through `-Scenario`; omitting the option never runs one. Required packages are checked against both the configured set and RimWorld's actual live loaded set. Screenshot step names must be unique and begin with `scenario-`, preventing them from replacing standard evidence. The hold is bounded to 0–3600 seconds, begins only after the selected setup and normal non-mutating readiness assertions pass, records its state in the ignored evidence directory, and still uses exact-PID cleanup. The final flushed Player log is rescanned after scenario/hold execution and exact-process shutdown. During the hold, use the generated `SavedData\DevGateway\current.json` only as a live local credential; never commit or paste it. The complete descriptor status, exact groups, observable workflows, and replacement IDs are tracked in [`docs/ScenarioMigrationInventory.md`](docs/ScenarioMigrationInventory.md).

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

- [Immersive Chefs player settings, compatibility, saves, and troubleshooting](docs/ImmersiveChefs.md)
- [Development workflow](docs/Development.md)
- [Host tests, Harmony, loaded mods, and Defs](docs/TestingEnvironments.md)
- [Dev Gateway security, API, and evidence](docs/Gateway.md)
- [Accepted Immersive Chefs gameplay contract](openspec/changes/specify-immersive-chefs-gameplay/proposal.md)
- [Dev Gateway OpenSpec change](openspec/changes/add-rimworld-dev-gateway/proposal.md)

Food preservation, food waste, save migration for an unreleased mod, and ceramics research without a selected integration are explicit future scope.
