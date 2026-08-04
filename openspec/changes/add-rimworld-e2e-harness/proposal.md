## Why

The repository has strong host tests and startup-gated in-game assertions, but its real pawn/job/UI workflows are still verified through individually driven scenarios. Those checks are expensive to repeat after RimWorld or third-party mod updates and cannot currently be run as one deterministic regression suite across multiple exact mod combinations.

## What Changes

- Add a separately staged, dynamically loaded E2E-test contract for multi-frame player workflows on a playable quickstart map.
- Mark every E2E test with its complete ordered non-Gateway mod set so the host can group tests and launch one isolated RimWorld process per exact group.
- Reset the disposable map before and after every test, run matching tests sequentially, isolate failures, and refuse to continue after an untrustworthy reset.
- Provide typed arrange/action/wait/observe steps backed by Dev Gateway capabilities, including native gizmos, float-menu orders, time control, selection, camera framing, screenshots, and bounded observable assertions.
- Add a host runner with test/group filters, deterministic discovery, `json` and table output, stable exit codes, per-test evidence, aggregate JSON/JUnit results, exact build/mod identities, and guaranteed process/stage/config cleanup.
- Keep E2E assemblies outside product packages and normal unit/integration runs; load them only under an explicit startup flag.
- Convert the existing Immersive Chefs manual verification scenarios into E2E fixtures incrementally, starting with a real adverse meal-ingestion workflow.
- Update repository workflow, skills, testing documentation, and README commands so E2E is a required reusable verification tier.
- Implement this developer-verification capability in this change; it does not alter shipping gameplay behavior.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`; repository path `mods/RimWorldDevGateway`; owning mod.

## Capabilities

### New Capabilities

- `dev-gateway-end-to-end-testing`: Dynamic E2E contracts, exact mod-set grouping, sequential playable-map execution, native player-action steps, isolation, evidence, result collection, and host orchestration.

### Modified Capabilities

None.

## Impact

This adds a host-safe E2E contract assembly, separately marked test projects, Gateway runtime discovery/execution and observation services, a Windows/PowerShell host runner that reuses the existing isolated-launch safety model, and new ignored evidence directories. `fumblesneeze.immersivechefs` will contribute test-only assemblies but will never reference or ship the Gateway from its product package.
