# RimWorld Modding MCP

`RimWorldModding.Mcp` is the repository's single agent-facing automation surface. It runs as a local stdio MCP server for Codex and exposes the same operation registry as a CLI for deterministic debugging and CI.

## Pinned transport and runtime

- Repository SDK: .NET 8 (`global.json`), single-file publish.
- MCP SDK: official `ModelContextProtocol` 2.2.0 package, stdio transport.
- CLI parser: `System.CommandLine` 2.0.0.
- Codex project configuration: `.codex/config.toml`, `[mcp_servers.rimworld_modding]`, explicit `command` and `args`, and startup/tool timeouts. It intentionally omits both repository-added approval prompts and `cwd`: an explicit task order is governed by the session's normal policy, while Codex uses the task's logical workspace root instead of resolving `.` or `..` against an arbitrary host-process directory.
- Portable client projection: root `.mcp.json`, with the same `command` and `args` and a project-root `cwd` for clients that support that convention. MCP standardizes the protocol, not a configuration filename or discovery convention, so Codex still needs its native TOML entry.
- Source bootstrap: `.codex/Start-RimWorldModdingMcp.ps1` uses a bounded cross-process lease, a shorter explicit build deadline, isolated build directories, and an atomically published content-addressed cache before executing the exact server DLL. Concurrent cold sessions therefore never share MSBuild `obj` outputs, and failed or cancelled builds cannot become runnable cache entries.
- stdout contains only MCP protocol frames or the selected CLI result. Diagnostics use stderr.
- CLI output is `table` by default and `json` with `-o|--output json`.
- Exit codes: `0` success, `2` invalid usage or validation, `1` runtime failure.

The local server is intentionally stdio rather than HTTP: Codex owns the child process and the operations act on this exact local repository, local RimWorld processes, and the local Steam client.

The launcher requires PowerShell 7 (`pwsh`) and the SDK pinned by `global.json`. Both checked-in client configurations intentionally invoke the same launcher. A focused regression test parses both files and rejects command drift, then exercises concurrent cold initialization and cancelled-build retry through real stdio handshakes.

## Operation families

| Family | Initial operations | Risk |
|---|---|---|
| Discovery | `repository_status`, `operation_list`, `evidence_read` | read |
| Repository | `openspec_validate`, `mod_build`, `test_run`, `package_validate` | workspace-write |
| Runtime | `game_run_start`, `e2e_run_start`, `run_status`, `run_cancel` | workspace-write / destructive-local |
| Gateway | `gateway_health`, `gateway_diagnostic`, `gateway_mutation`, `gateway_raw_mutation` | read / destructive-local |
| User config | `modlist_inspect`, `modlist_enable`, `modlist_disable`, `modlist_restore`, `local_mod_sync` | read / destructive-local |
| Release | `release_profile_validate`, `release_prepare`, `release_status`, `release_publish`, `release_accept_subscriber_evidence` | read / workspace-write / external-write |

Operations never expose arbitrary shell execution. Allowlisted read-only Gateway diagnostics and explicit semantic/raw mutations are deliberately different operations. A direct mutation can prepare or diagnose a scene, but cannot serve as gameplay acceptance.

## Migration adapter inventory

These are execution engines during migration, not public agent APIs:

| Existing engine | Owning MCP operation |
|---|---|
| `scripts/Invoke-Tests.ps1` | `test_run` |
| `scripts/Invoke-RimWorldSmoke.ps1` | `game_run_start` |
| `scripts/Invoke-GatewaySmoke.ps1` | `game_run_start` |
| `scripts/Invoke-RimWorldEndToEndTests.ps1` and `tools/RimWorldDevGateway.EndToEndHost` | `e2e_run_start` |
| `tools/RimWorldDevGateway.Client` | Gateway operation family |
| `scripts/Build-ImmersiveChefsRelease.ps1` | retired product-specific release backend; parity retained by universal profile/operations |
| `scripts/Invoke-ImmersiveChefsWorkshopRelease.ps1` | retired product-specific publisher; no longer an agent-facing entry point |
| mod-owned preview renderers | registered presentation step selected by release profile |

An engine may remain only while its MCP operation owns validation, parsing, cancellation, evidence, and safety. The universal C# publisher and subscriber verifier contain no product name or Workshop ID; all identity, dependency, presentation, and verification selection comes from strict per-mod profiles. Repeated workflows, copied script logic, structured parsing, long-lived process ownership, safety boundaries, or evidence generation require an OpenSpec scenario and typed operation instead of another public script.

`release_prepare` is mutation-free but authenticated. It validates the pinned game/depot/managed build, exact product-only package boundary (including no reparse traversal and no undeclared runtime AssemblyRefs), frozen subscriber inputs, specific change note, first-title absence or existing-item identity, and an exact remote metadata/content/preview/dependency baseline. Release profiles distinguish Steam required items, RimWorld hard dependencies, and explicitly permitted hard runtime AssemblyRefs; optional integrations stay load-after/reflection-resolved. The returned plan hash and legacy-named `confirmationNonce` are exact one-time admission inputs for `release_publish`, which repeats all local and remote checks immediately before dispatch. An explicit publish order authorizes a subsequently prepared matching plan; callers must pass those inputs directly and must not stop for a redundant second confirmation.

After dispatch, the publisher completes in a detached same-tool worker even if the initiating client is cancelled; `.codex/config.toml` allows 3900 seconds for the operation's 3600-second bound. Atomic worker and Gateway leases let a retry reattach to the exact callback or poll only the already known same item—never a second create or update submission. Subscriber isolation reserves and records its own exact game run and local-package backup before moving anything. The first returned Workshop ID is persisted and committed before later verification. `release_status` remains usable after plan expiry or subsequent local changes. A successful reacquired-copy automation ends at `subscriber-evidence-awaiting-review`; personally inspect its exact screenshots, then call `release_accept_subscriber_evidence` with the receipt and concrete observation. Only that operation records complete-reviewed evidence.

`local_mod_sync` and subscriber isolation retain recoverable prior copies beneath RimWorld's sibling `.rimworld-modding-mcp/Mods` directory, never inside the scanned `Mods` folder.

## Common CLI diagnostics

```powershell
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- tool list -o table
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- tool call repository_status --arguments '{}' -o json
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- serve --repository-root .
```

Normal work should call the MCP tools surfaced by Codex. The CLI exists for testing, CI, recovery, and transparent reproduction.
