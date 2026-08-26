# Fumblesneeze RimWorld mods

This repository is the Windows development monorepo for Fumblesneeze's RimWorld 1.6 mods. It owns the playable mod sources, the developer-only RimWorld Dev Gateway, host and in-game tests, OpenSpec contracts, release profiles, and the typed MCP/CLI automation used to build, verify, install, and publish them.

## Mods

| Mod | Package ID | Role |
| --- | --- | --- |
| [Hospitality + Ideology Patch](mods/GuestBedGizmo/README.md) | `fumblesneeze.guestbedgizmo` | Adds Hospitality guests to RimWorld's unified bed-owner menu. |
| [Immersive Chefs](mods/ImmersiveChefs/README.md) | `fumblesneeze.immersivechefs` | Makes kitchenware, preparation, meal condition, service, dining, and sanitation part of colony play. |
| [Thin Walls](mods/ThinWalls/README.md) | `fumblesneeze.thinwalls` | Adds Stuff-made walls and doors that occupy tile edges instead of whole cells. |
| [RimWorld Dev Gateway](mods/RimWorldDevGateway/README.md) | `fumblesneeze.rimworlddevgateway` | Developer-only authenticated loopback control, observation, testing, and unrestricted C# execution. |

Each OpenSpec change and capability has exactly one owner: either one mod or one repository-tooling component. Repository-owned mods do not depend on one another, and product mods never ship, reference, or load-order-hint the Gateway; it is isolated development infrastructure, not a gameplay dependency.

## Requirements

| Tool | Purpose |
| --- | --- |
| Windows, Git, and PowerShell 7 | Repository development and isolated RimWorld process control. |
| .NET 8 SDK | Restore, build, test, MCP server, and companion tools. `global.json` pins the supported SDK policy. |
| RimWorld 1.6 from Steam | Managed assemblies and final live verification. |
| Harmony Workshop item `2009463077` | Required by the three product mods; not required by the Gateway itself. |
| XML Extensions Workshop item `2574315206` | Required by Immersive Chefs, and by any other owning mod that uses an `XmlExtensions.*` patch operation; never bundled. |
| Node.js LTS and OpenSpec `1.2.0` | Contract validation. Install with `npm install -g @fission-ai/openspec@1.2.0`. |
| FlaUiCli | Optional exact-window evidence for workflows that genuinely require desktop UI. |

NuGet restore supplies the managed build and test dependencies. Workshop content is read-only inspection input: never edit it, copy its assemblies into a package, or deploy development output into it.

Checked-in defaults point at:

- RimWorld: `F:\Steam\steamapps\common\RimWorld`
- Workshop content: `F:\Steam\steamapps\workshop\content\294100`

Override those MSBuild properties when the Steam library is elsewhere:

```powershell
dotnet build .\RimWorldMods.sln -c Release --nologo `
  -p:DefaultRimWorldPath='D:\SteamLibrary\steamapps\common\RimWorld' `
  -p:DefaultSteamModContentFolder='D:\SteamLibrary\steamapps\workshop\content\294100'
```

## MCP and CLI automation

`RimWorldModding.Mcp` is the repository's public automation surface. Use its typed operations for tests, isolated game runs, Gateway diagnostics and actions, local mod-list changes, evidence access, packaging, and publishing. The PowerShell files under `scripts/` are internal adapters behind those operations, not the user-facing command interface.

Compatible clients can discover [`.mcp.json`](.mcp.json). Codex uses [`.codex/config.toml`](.codex/config.toml). Both start the repository bootstrap, which builds or reuses a fingerprinted MCP binary without writing protocol noise to stdout.

Start the server manually when a client does not support repository configuration:

```powershell
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- `
  serve --repository-root .
```

The same registry has a transparent CLI projection:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

# Discover every operation and its risk, timeout, and evidence policy.
dotnet run --project $mcp -- tool list -o table

# Inspect repository and release state.
dotnet run --project $mcp -- tool call repository_status --arguments '{}' -o json

# Build and install the selected profile into the local RimWorld Mods directory.
# The optional modsRoot override is useful when the Steam library is elsewhere.
dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.immersivechefs","configuration":"Release","modsRoot":null}' -o json

# Run one registered host-test environment and reject zero-test results.
dotnet run --project $mcp -- tool call test_run `
  --arguments '{"suite":"RimWorldDevGateway.Unit","filter":null,"configuration":"Release"}' -o json

# Validate every OpenSpec change and capability strictly.
dotnet run --project $mcp -- tool call openspec_validate --arguments '{}' -o json

# Dry-run one exact native E2E workflow without launching RimWorld.
dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"gateway.escape-menu-overlay","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

Call `operation_list` or `tool list` before inventing orchestration. If a repeated workflow is missing, add an owning OpenSpec scenario and a typed MCP/CLI operation instead of adding another public script.

## Starting a new mod

Follow the repository's [new-mod checklist](docs/NewModChecklist.md) before writing gameplay code.
Every distributable mod gets its own `mods/<ModName>` folder, a stable
`fumblesneeze.<mod-designator>` package ID, a Zlepper ModSdk project, generated About metadata, and
an owning OpenSpec change. Add the release profile before using the universal build operation; it is
the source of truth for the project, package allowlist, dependencies, and verification inputs. Keep
optional integrations package-gated and keep the Dev Gateway out of product dependencies.

The standard edit/build loop for a new or existing mod is:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

# Validate the profile and package before installing anything.
dotnet run --project $mcp -- tool call release_profile_validate `
  --arguments '{"packageId":"fumblesneeze.examplemod"}' -o json

# Build and install the successful allowlisted package by default.
# Use modsRoot only when the local Steam library is not the checked-in default.
dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.examplemod","configuration":"Release","modsRoot":null}' -o json

# Inspect the exact package without changing the game installation.
dotnet run --project $mcp -- tool call package_validate `
  --arguments '{"packageId":"fumblesneeze.examplemod"}' -o json
```

`mod_build` and `local_mod_sync` replace only the selected local package, stage on the install
volume outside the scanned `Mods` directory, and retain no install backup. Recover an older copy by
rebuilding from the repository package. Do not use Workshop content as a build or deployment target.

## Development workflow

[`AGENTS.md`](AGENTS.md) is authoritative for repository boundaries and acceptance. In brief: identify the single mod or tooling owner, update its observable OpenSpec contract, work through focused tests and package checks, resolve an independent review, then verify game-facing behavior through a real player workflow on the reviewed build and inspect the retained evidence personally.

A convenient repository-level restore and build is:

```powershell
dotnet restore .\RimWorldMods.sln
dotnet build .\RimWorldMods.sln -c Release --nologo
```

Use [`docs/TestingEnvironments.md`](docs/TestingEnvironments.md) to choose the honest host or isolated-game tier. Grouped E2E results are supporting evidence until the native screenshots from that exact reviewed build have been personally inspected.

## Dev Gateway safety

The Gateway binds an authenticated API to a dynamic IPv4 loopback port and creates a new bearer token for each isolated run. Its raw Mono.CSharp endpoint intentionally permits unrestricted in-process code execution with the RimWorld process's permissions.

- Never enable it in an ordinary or untrusted play session.
- Never commit or paste a live `SavedData\DevGateway\current.json`.
- Keep automated runs minimized and bound to their exact PID and process-start identity.
- Use a unique disposable `-savedatafolder` for development and acceptance. Touch an existing user process only for an explicitly requested live diagnostic or repair, never as acceptance evidence.
- Request graceful shutdown first and retain cleanup/config-hash evidence.

Use semantic Gateway operations for repeatable actions and observations. Raw C# is appropriate for one-off diagnosis or disposable setup, but direct mutation does not prove a player workflow. Read [`docs/Gateway.md`](docs/Gateway.md) for the protocol and evidence contract.

## Release operations

Release-capable mods with a `mods/<Mod>/Release/release.json` profile bind the project, package inventory, exact RimWorld/Steam build, Workshop metadata, dependencies, preview, change note, and subscriber-verification workflow. Use the typed `release_profile_validate`, `release_prepare`, `release_publish`, `release_status`, and `release_accept_subscriber_evidence` operations; do not call Workshop publishing scripts directly.

Preparation builds and stages repository artifacts and creates an immutable candidate/plan, but does not mutate Steam. Publication consumes that admitted plan, persists durable recovery state, verifies the remote graph and reacquired subscriber copy, and leaves visual acceptance separate until the exact retained screenshots have been personally reviewed.

## Repository boundaries and output

- `mods/` contains mod-owned source and player documentation.
- `openspec/` contains proposals, designs, capability contracts, and task state.
- `shared/`, `tools/`, and `tests/` contain host-safe contracts, first-party tooling, and verification source.
- `docs/` contains repository-wide development, testing, Gateway, and detailed player references.
- `artifacts/` contains ignored builds, logs, screenshots, test results, and run evidence.

Do not commit tokens, live Gateway manifests, normal RimWorld configuration or saves, Workshop assemblies, generated packages, or files from `artifacts/`. Raw builds stay repository-local; intentional local installation is performed by the owned `mod_build`/`local_mod_sync` operations, never by copying into Workshop content.

## Further reading

- [`docs/Development.md`](docs/Development.md) — development conventions and package boundaries
- [`docs/TestingEnvironments.md`](docs/TestingEnvironments.md) — choosing an honest test environment
- [`docs/Gateway.md`](docs/Gateway.md) — Gateway security, API, control, and evidence
- [`docs/ImmersiveChefs.md`](docs/ImmersiveChefs.md) — detailed Immersive Chefs player guide and compatibility matrix
- [`docs/ScenarioMigrationInventory.md`](docs/ScenarioMigrationInventory.md) — legacy scenario status and E2E replacements
- [`docs/NewModChecklist.md`](docs/NewModChecklist.md) — canonical layout, profile, test, build/install, and release steps for new mods
