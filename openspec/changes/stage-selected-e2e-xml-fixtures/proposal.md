## Why

The selected Megamod diagnostic workflow declares a standalone XML fixture, but the grouped runner can currently stage only development test assemblies inside an installed owner. The real XML loader cannot consume the fixture until its separate package is safely available for that exact isolated group.

## What Changes

- Add explicit repository-owned XML fixture manifests with owner, package identity, supported game version and positive file inventory.
- Plan selected fixtures separately from the discovered installed-package inventory and ordinary product deployment.
- Publish each selected fixture transactionally as a standalone local package, preserving every existing destination, and remove only the exact leased fixture after its isolated process stops.
- Expose planned inventory, publication and cleanup evidence through the existing typed `e2e_run_start` lifecycle; dry-run remains non-mutating.
- Register the existing Megamod diagnostic XML inputs without adding an assembly, project, release profile or product payload.

## Capabilities

### New Capabilities

- `selected-e2e-xml-fixtures`: Explicit, selected-group XML fixture planning, owned staging and cleanup.

### Modified Capabilities

None. This capability extends the existing E2E lifecycle without changing test selection or native player-action semantics.

## Impact

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at `tools/RimWorldDevGateway.EndToEndHost`.

Affected components: the typed `e2e_run_start` adapter, host tests, and fixture metadata under `tests/Fixtures`. Runner/CLI wiring is coordinated separately from the new planner/publisher implementation.

No gameplay behavior is delivered or accepted by this tooling change. There are no new external runtime dependencies. The development fixture requires its owning Megamod package only in its explicitly declared native group; no product depends on the fixture or Gateway, and no Workshop files are changed.
