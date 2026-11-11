## Why

A failed native verification currently exits after cleanup but before writing its final evidence
summary. Configuration hashes are computed but lost, making failed runs harder to audit.

## What Changes

- Retain a credential-free final summary on failed smoke and E2E child runs as well as success.
- Preserve actual process/build identity, normal configuration hashes, failure records and cleanup
  results, with explicit unavailable values when failure precedes launch or identity collection.
- Preserve nonzero failure status, including cleanup or summary-persistence failures.

## Capabilities

### New Capabilities
- `gateway-failure-evidence`: durable completion evidence when verification fails.

### Modified Capabilities
None.

## Impact

Exactly one owner: repository tooling **RimWorld Dev Gateway**. The existing smoke internal adapter
and its host tests change; no new public script, game behavior or product dependency is introduced.
