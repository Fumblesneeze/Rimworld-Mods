## Why

RimWorld 1.7 selects its legacy XML deserializer before initial definition conversion, using a native
command-line flag. The existing isolated E2E runner groups only by ordered packages and cannot declare,
launch or verify that early startup choice, leaving native legacy-loader verification unavailable.

## What Changes

- Add one explicit legacy-loader declaration to E2E metadata; omission retains native default loading.
- Separate otherwise identical package groups when their declared loader modes differ.
- Propagate the selected mode through the existing typed E2E planning, fixture staging and minimized
  exact-process launch; emit only the fixed native legacy flag, with no arbitrary-arguments interface.
- Record requested and observed modes and reject a mismatch before any selected test reaches Arrange.
- Preserve existing default group IDs, launch arguments, fixture identity, diagnostic accounting and
  exact-process cleanup. Dry-run exposes the selected mode without starting or mutating a game.

## Capabilities

### New Capabilities

- `e2e-native-xml-loader`: Declared native default/legacy startup selection, separate process grouping,
  truthful runtime admission and retained mode evidence for isolated E2E workflows.

### Modified Capabilities

None. Existing fixture publication and native player-action contracts retain their safety requirements.

## Impact

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at `tools/RimWorldDevGateway.EndToEndHost`.

Affected components are host-safe E2E contracts/metadata, the existing typed `e2e_run_start` adapter,
internal grouped/smoke launch adapters, Gateway E2E catalog/coordinator and focused verification tests.
Gateway changes are integration surfaces of this single tooling owner, not another capability owner.

This delivers verification tooling, not product gameplay. The concrete S094 legacy player workflow
remains owned by megamod's separate feature change. No product runtime or XML fixture payload changes
are part of this prerequisite. There are no new external runtime dependencies: a selected legacy run
requires the inspected supported RimWorld 1.7 installation plus the existing development Gateway and
Harmony. Default 1.6/1.7 runs retain their existing requirements. XML Extensions is not used; products
do not acquire a Gateway dependency, and Workshop content remains read-only.
