## 1. Scoped implementation and host verification

- [x] 1.1 Retain focused RED/GREEN for expected native refusal through the existing action boundary.
- [x] 1.2 Add the explicit incident-step option and exact refusal interpretation without changing native execution.
- [x] 1.3 Cover default execution, unexpected success, prerequisite/infrastructure failure and constructor overloads; update usage documentation.

## 2. Review and native acceptance

- [x] 2.1 Independently review the scoped change, resolve findings, rerun affected tests/builds and typed OpenSpec validation.
- [x] 2.2 On the reviewed build personally verify expected refusal through an actual native incident scenario in a fresh minimized isolated process, retain causal screenshots/build identity/config hashes and cleanup evidence under root AGENTS.md.

Evidence: `artifacts/Implementation/megamod-p0/gateway-expected-incident-refusal/tdd-review.md`.
Independent review found the removed CLR constructor signature; the regression was reproduced,
both original overloads restored, and the correction rechecked. Focused final result: 20 passed,
including the existing incident-contract fixture. Strict OpenSpec validation: 162 passed. These
checks originally closed host implementation/review only.

Native tooling acceptance: `megamod.content.native-incident-save-override` passed38 steps in fresh
minimized1.7 process44352, run `20261108T011851719Z-4e8b8d65ad5840f190cedda2f2c628de`.
Root personally inspected all8 causal captures: actual native refusal before and after a pending
save edit, then native execution and a physical ship chunk after save/load applied the choice.
Both refusal actions retained exact expected/observed `incident_rejected`; ordinary success still
executed afterward. Build identities, full log inspection, unchanged normal config/preferences and
graceful cleanup are recorded in
`artifacts/Implementation/megamod-p0/native-incident-save-override-reviewed.md`.
This closes only the Gateway expectation behavior, not megamod content selection or its P0 gate.
