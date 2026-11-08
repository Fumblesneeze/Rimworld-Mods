## Why

Native incident scenarios sometimes need to prove that RimWorld refuses an incident. The current
`IncidentActionStep` fails immediately on that valid expected outcome, preventing the scenario from
observing the unchanged map and incident history.

## What Changes

- Add an explicit `expectRejected` option to the typed native incident step; ordinary calls continue
  to require successful incident execution.
- Accept only the existing `incident_rejected` result from the real incident worker when requested.
  Unexpected success and infrastructure failures remain failures, with retained outcome evidence.
- Document and test the distinction without adding product hooks or bypassing native execution.

## Capabilities

### New Capabilities
- `gateway-expected-incident-refusal`: typed expectations for native incident execution refusal.

### Modified Capabilities
None.

## Impact

Exactly one owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.
Affected components are the shared E2E step contract, Gateway native action dispatch, host tests and
Gateway documentation. No external runtime dependency is added; the Gateway's required Harmony
dependency is unchanged. Product mods gain no Gateway dependency. This delivers verification tooling,
not gameplay behavior; product acceptance remains separate.
