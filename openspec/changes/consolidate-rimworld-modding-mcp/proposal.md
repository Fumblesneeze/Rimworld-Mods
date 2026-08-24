## Why

RimWorld development in this repository currently exposes important workflows through a growing set of project-specific PowerShell scripts, host executables, raw HTTP calls, and one-off release entry points. That fragmentation encourages repeated orchestration, makes safety guarantees uneven, and already pushed a new mod toward another bespoke publisher instead of one reusable repository contract.

## What Changes

- Add one repository-owned C# executable, `RimWorldModding.Mcp`, that serves a local stdio Model Context Protocol endpoint and exposes the same typed operations through a discoverable CLI.
- Add a repo-local `.codex/config.toml` entry that starts the server for trusted local sessions with the repository root passed explicitly.
- Define typed tools for repository discovery, focused build/test/spec/package work, isolated RimWorld and grouped-E2E runs, evidence inspection, live authenticated Gateway diagnostics and mutations, normal-mod-list management, and universal manifest-driven release preparation/publication/verification.
- Replace per-mod publishing scripts with validated per-mod release profiles consumed by the universal release tools. First publication remains private and every Steam mutation remains bound to a clean committed candidate, immutable dry-run digest and nonce, an explicit publication order whose material scope matches the prepared plan, retained Workshop identity, and post-upload subscriber verification. Preparation does not introduce a second confirmation prompt.
- Introduce an operation catalog and promotion rule: when repository work repeats or requires custom safety, parsing, evidence, or lifecycle logic, its behavior is specified and added as a typed MCP/CLI operation instead of another agent-facing script or raw command recipe.
- Keep existing scripts only as bounded migration engines until their behavior moves behind typed operations; new agent-facing RimWorld automation SHALL NOT add a standalone script when the MCP/CLI can own it.
- Specify and implement development/release infrastructure only. This change does not alter shipping gameplay behavior.

## Capabilities

### New Capabilities

- `rimworld-modding-mcp`: Local MCP/CLI transport, repository configuration, discovery, tool contracts, output/error semantics, and capability promotion governance.
- `rimworld-modding-operations`: Typed build, test, OpenSpec, isolated game, E2E, evidence, Gateway diagnostic/mutation, and normal-mod-list operations.
- `universal-rimworld-mod-publishing`: Validated per-mod release profiles plus guarded, reusable candidate, dry-run, Steam publication, persistence, reacquisition, and subscriber-verification operations.

### Modified Capabilities

None. This change supersedes the still-unimplemented project-specific tool direction in the active `automate-multiversion-mod-releases` change without changing any product mod's gameplay contract.

## Impact

The change adds a .NET tool and tests under `tools/` and `tests/`, a trusted-repository `.codex/config.toml`, schemas/profiles under `release/` and `mods/<ModName>/Release/`, documentation and process rules, and typed adapters around existing build, test, runner, Gateway, and Steam publishing primitives. Existing reusable runners and the in-game authenticated Gateway remain execution backends during migration, while product packages remain free of the MCP, Gateway, test, host, and publishing assemblies.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`; repository path `mods/RimWorldDevGateway`; sole owning mod. Product mods are operated on by the companion tool but do not reference or ship it.
