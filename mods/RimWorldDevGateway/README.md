# RimWorld Dev Gateway

Package ID: `fumblesneeze.rimworlddevgateway`

> **Danger:** this is a development-only RimWorld 1.6 mod. Its authenticated loopback API includes unrestricted in-process C# execution with the RimWorld process's permissions. Never enable it for normal play or an untrusted session.

The Gateway gives repository automation reliable, game-native observation and control without foreground desktop automation. Product mods never depend on it, and their test assemblies are staged separately from distributable packages.

## Capabilities

- Versioned authenticated HTTP API on a dynamic IPv4 loopback port.
- Status, UI state, logs, screenshots, bounded Def export, and request journals.
- Semantic time, camera, selection, thing, debug-action, gizmo, float-menu, and window control.
- Exact-PID input and controlled process shutdown for the few native paths that require it.
- Named automations plus staged integration and multi-frame E2E test execution.
- Stateful raw Mono.CSharp REPL and optional compiled-assembly fallback.
- Circinus-first performance runs and bounded DPA diagnostics.

The in-game danger and test-status overlay is hidden during ordinary play and appears only while RimWorld's native in-play Escape menu is open.

## Security and lifecycle

Every isolated launch creates a fresh bearer token and writes a live `SavedData\DevGateway\current.json` manifest inside that run's disposable save-data folder. Never commit, retain, or paste that credential. The server is loopback-only, but any local holder of the token can execute unrestricted code.

Automation must use a fresh minimized RimWorld process, bind all actions and evidence to its exact PID and start identity, preserve the user's normal configuration hashes, and request graceful shutdown before an exact-PID fallback. Direct raw mutation is diagnostic/setup evidence; it does not replace a real player action or visible result.

## Repository operation

The repository MCP wraps launches, diagnostics, semantic mutations, evidence, and cancellation in typed operations. Discover the current surface rather than calling Gateway scripts directly:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet run --project $mcp -- tool list -o table
dotnet run --project $mcp -- tool call repository_status --arguments '{}' -o json

dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"gateway.escape-menu-overlay","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

Use `game_run_start` for an owned interactive Gateway lease, `gateway_health` and `gateway_diagnostic` for reads, `gateway_mutation` for registered semantic actions, `gateway_raw_mutation` only for bounded one-off development, `run_status` for observation, and `run_cancel` for graceful owned cleanup. `operation_list` provides the exact current schemas and safety metadata.

## Development

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet build .\mods\RimWorldDevGateway\RimWorldDevGateway.csproj -c Release --nologo

dotnet run --project $mcp -- tool call test_run `
  --arguments '{"suite":"RimWorldDevGateway.Unit","filter":null,"configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call openspec_validate --arguments '{}' -o json
```

Gateway extensions must remain authenticated, loopback-bound, bounded on Unity's main thread, cancellation-aware, and semantically distinct between player actions, observations, and direct mutation. A new route is not accepted until it is exercised in a real minimized RimWorld workflow.

Read [`docs/Gateway.md`](../../docs/Gateway.md) for protocol, client, evidence, and troubleshooting details. The owning contract is [`openspec/changes/add-rimworld-dev-gateway`](../../openspec/changes/add-rimworld-dev-gateway/).

