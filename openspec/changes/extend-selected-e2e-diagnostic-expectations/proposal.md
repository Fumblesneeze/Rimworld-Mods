## Why

Intentional native save/load failure scenarios can pass their player-workflow assertions while the
grouped runner rejects their expected diagnostics. Existing allowances belong only to selected XML
fixtures; ordinary selected E2E tests have no declaration or provenance contract. Manual log
classification must not become an error bypass.

## What Changes

- Add bounded compiled test declarations for exact diagnostic severity, first-line text and count.
- Project declarations through metadata-only discovery, hash-bound manifests and exact selected-test
  launch receipts in the existing typed `e2e_run_start` and matching CLI lifecycle.
- Reuse the existing intermediate/final log accounting while preserving XML-fixture behavior.
- Allow only the closed `{savedata}` expansion for a quoted, literal test-owned save path beneath
  the exact isolated run directory. No caller ignore list, regex or path wildcard is introduced.
- Retain missing, extra, changed, adjacent and unrelated diagnostics as failures.

## Capabilities

### New Capabilities

- `selected-e2e-diagnostic-expectations`: Exact compiled-test-scoped runtime diagnostic accounting.

### Modified Capabilities

None. Existing XML fixture declarations keep their exact ERROR-only meaning.

## Impact

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at
`tools/RimWorldDevGateway.EndToEndHost`.

Affected components are shared development-test metadata/manifests, host discovery and projection,
Gateway compiled-declaration admission, the existing typed E2E adapter and private runner helpers,
and existing honest host tests. No new public script or operation is introduced. Product behavior,
product packaging and test scenario outcome rules remain unchanged. Root separately owns native
fixture annotations and live verification; host checks alone do not accept game behavior.
