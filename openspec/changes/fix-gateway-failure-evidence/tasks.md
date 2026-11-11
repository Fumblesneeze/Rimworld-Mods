## 1. Failure evidence correction

- [x] 1.1 Capture focused host RED/GREEN for failed-scenario final summary, null prelaunch identity, cleanup failure, token-bearing diagnostics and summary-write failure; preserve success regression behavior.
- [x] 1.2 Correct the existing internal adapter, validate PowerShell/OpenSpec, independently review and resolve findings without changing the public launch surface.
- [x] 1.3 On a reviewed real isolated failed run, retain and inspect final process/build/configuration/cleanup evidence and nonzero status; do not reconstruct unavailable historical before-hashes.

Host evidence:21/21 actual completion-tail cases at
`artifacts/TestResults/20261107T165002104Z-47628-eae148084f594b45be95193d82eaf5df`, including
two retained RED corrections, plus PowerShell syntax and strict163/163 OpenSpec validation.
Independent read-only review found no issues. Exact hashes/dispositions are in
`artifacts/Implementation/gateway-native-window-input/failure-evidence-handoff.md` and the root
ledger. The later successful checkbox run retained its complete summary; it does not satisfy1.3.

Task1.3 was subsequently exercised by the real failed native replacement-control workflow in
`artifacts/EndToEndRuns/Grouped/20261107T181454130Z-5e9ccd3d134a42a1969454b25dff8192/`.
Root inspected the child final summary, actual PID/start and assembly hashes, both unchanged normal
configuration hash pairs, scenario failure, graceful exit0, sanitized credentials and cleaned E2E
stage. The typed operation returned exit1. The reviewed smoke adapter's SHA256 remained
`E2C7BC6FCEB67EAD9A571E0B76D64A24FB0274B92186831C050C2E45E62B5DF8`.
This accepts failure-evidence retention only; the failed input scenario remains unaccepted.
