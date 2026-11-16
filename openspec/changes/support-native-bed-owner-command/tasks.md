## 1. Native command adapter
- [x] 1.1 Inspect both native input methods and retain the unsupported-interaction RED.
- [x] 1.2 Implement exact-type classification/invocation through the existing gizmo surface and independently review it.
- [x] 1.3 Verify the native menu-to-prisoner workflow on both targets, personally inspect screenshots, and validate OpenSpec and cleanup.

Reviewed implementation `0db24a2`; final product source `b1007b9`. The original native owner menu and prisoner transition passed without Hospitality on both 1.6 and 1.7, with before/menu/after screenshots personally inspected and configuration/process/credential cleanup verified. Evidence: `artifacts/Releases/fumblesneeze.guestbedgizmo/acceptance/20261116-matrix/evidence.md`. The Gateway host suite could not compile because of existing System.Text.Json/language-version errors; this was not reported as a passing test run.
