# Fumblesneeze RimWorld mods

This repository is the Windows development monorepo for Fumblesneeze's RimWorld 1.6 mods. It owns the playable mod sources, the developer-only RimWorld Dev Gateway, host and in-game tests, OpenSpec contracts, release profiles, and the typed MCP/CLI automation used to build, verify, install, and publish them.

## Mods

| Mod | Package ID | Role |
| --- | --- | --- |
| [Hospitality + Ideology Patch](mods/GuestBedGizmo/README.md) | `fumblesneeze.guestbedgizmo` | Adds Hospitality guests to RimWorld's unified bed-owner menu. |
| [Immersive Chefs](mods/ImmersiveChefs/README.md) | `fumblesneeze.immersivechefs` | Makes kitchenware, preparation, meal condition, service, dining, and sanitation part of colony play. |
| [Thin Walls](mods/ThinWalls/README.md) | `fumblesneeze.thinwalls` | Adds Stuff-made walls and doors that occupy tile edges instead of whole cells. |
| [RimWorld Dev Gateway](mods/RimWorldDevGateway/README.md) | `fumblesneeze.rimworlddevgateway` | Developer-only authenticated loopback control, observation, testing, and unrestricted C# execution. |

Each OpenSpec change and capability names exactly one owning mod. Product mods never ship, reference, or load-order-hint the Gateway; it is isolated development infrastructure, not a gameplay dependency.

## Requirements

| Tool | Purpose |
| --- | --- |
| Windows, Git, and PowerShell 7 | Repository development and isolated RimWorld process control. |
| .NET 8 SDK | Restore, build, test, MCP server, and companion tools. `global.json` pins the supported SDK policy. |
| RimWorld 1.6 from Steam | Managed assemblies and final live verification. |
| Harmony Workshop item `2009463077` | Required by the three product mods; not required by the Gateway itself. |
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

## Development workflow

Read [`AGENTS.md`](AGENTS.md) before changing game-facing behavior. The required path is:

1. Identify the owning OpenSpec change and update its observable contract when necessary.
2. Work vertically with focused RED/GREEN tests in the honest host environment.
3. Build the owning project and run only the affected package or compatibility checks.
4. Obtain an independent scoped review and resolve its findings.
5. Verify the reviewed build in a fresh isolated RimWorld process through a real player workflow, then personally inspect the retained before/action/after evidence.

A convenient repository-level restore and build is:

```powershell
dotnet restore .\RimWorldMods.sln
dotnet build .\RimWorldMods.sln -c Release --nologo
```

Host tests are deliberately split between ordinary unit contracts, explicitly owned Harmony patches, lightweight constructed Def fixtures, snapshots, and tool tests. Anything that depends on a real loaded mod, finalized Core/Workshop Defs, PatchOperations, or the complete Harmony set belongs in a fresh process-isolated RimWorld run. See [`docs/TestingEnvironments.md`](docs/TestingEnvironments.md).

The grouped E2E operation discovers attributed workflows, builds and stages only their owning bundles, deploys repository-owned products, launches one fresh minimized process per exact mod group, retains step evidence, and removes its stage. A green result is supporting evidence; acceptance still requires inspecting the native screenshots from that exact build.

## Dev Gateway safety

The Gateway binds an authenticated API to a dynamic IPv4 loopback port and creates a new bearer token for each isolated run. Its raw Mono.CSharp endpoint intentionally permits unrestricted in-process code execution with the RimWorld process's permissions.

- Never enable it in an ordinary or untrusted play session.
- Never commit or paste a live `SavedData\DevGateway\current.json`.
- Keep automated runs minimized and bound to their exact PID and process-start identity.
- Use a unique disposable `-savedatafolder`; never reuse a running RimWorld process.
- Request graceful shutdown first and retain cleanup/config-hash evidence.
- The in-game danger/status overlay appears only while RimWorld's native in-play Escape menu is open.

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

Do not commit tokens, live Gateway manifests, normal RimWorld configuration or saves, Workshop assemblies, generated packages, or files from `artifacts/`. Normal builds stay repository-local; intentional live deployment is performed only by an owned isolated operation.

## Further reading

- [`docs/Development.md`](docs/Development.md) — development conventions and package boundaries
- [`docs/TestingEnvironments.md`](docs/TestingEnvironments.md) — choosing an honest test environment
- [`docs/Gateway.md`](docs/Gateway.md) — Gateway security, API, control, and evidence
- [`docs/ImmersiveChefs.md`](docs/ImmersiveChefs.md) — detailed Immersive Chefs player guide and compatibility matrix
- [`docs/ScenarioMigrationInventory.md`](docs/ScenarioMigrationInventory.md) — legacy scenario status and E2E replacements
