## Why

Megamod's first independently implemented policies need a real .NET 10 host test suite. The typed
test runner currently rejects `Megamod.Unit`, so the agreed implementation plan cannot retain
nonzero RED/GREEN evidence through the repository automation surface.

## Owner

Exactly one owner: repository tooling, **RimWorldModding.Mcp**. Product tests and behavior remain
owned by megamod's existing feature changes; this change owns suite dispatch and truthful reporting.

## What Changes

- Register the exact `Megamod.Unit` suite through the existing typed `test_run` and CLI projection.
- Run its host-only .NET 10 project without requiring the legacy 1.6 Managed directory.
- Preserve exact filtering, nonzero-test enforcement, failed-test exit status and canonical evidence.
- Keep the opt-in megamod suite out of the legacy `All` aggregate.
- Register the first concrete owned-patch suite `Megamod.Harmony` separately on .NET 10, resolving
  the installed 1.7 Harmony provider and retaining its identity without loading a real game/mod.
- Register `Megamod.Defs` separately for S029's actual native policy data and scoped constructed
  definitions, without active mods, Harmony or XML-loaded game content.

## Capabilities

### New Capabilities
- `megamod-host-test-dispatch`: select and report the isolated pure host suite.

### Modified Capabilities
None.

## Impact

The retained `scripts/Invoke-Tests.ps1` adapter and testing documentation change. No game is launched,
no normal configuration changes, and no new public script or product dependency is introduced.
