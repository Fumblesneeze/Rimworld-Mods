## 1. mods/ImmersiveChefs - Repository and framework setup

- [x] 1.1 Initialize Git and root OpenSpec, then install the OpenSpec, TDD, code-review, and FlaUI skills repo-locally.
- [x] 1.2 Recover the June testing session, inventory local RimWorld/Workshop dependencies, and validate Zlepper ModSdk plus Testing 0.0.9 in an isolated spike.
- [x] 1.3 Add global build properties/targets, ignored artifacts, solution/project scaffolding, and the grounded local dependency inventory.

## 2. mods/ImmersiveChefs - Compatibility catalog TDD

- [x] 2.1 RED: Add one public-behavior test for detecting a recognized integration and record the expected failing run.
- [x] 2.2 GREEN: Implement the minimum catalog/snapshot behavior needed for the first test and record the passing run.
- [x] 2.3 RED: Add one test for a complete inactive snapshot when package IDs are unknown or empty and record the expected failure.
- [x] 2.4 GREEN: Implement the minimum complete-snapshot behavior and record the passing run.
- [x] 2.5 RED: Add one test for case-insensitive, duplicate package IDs and immutable active results and record the expected failure.
- [x] 2.6 GREEN/REFACTOR: Implement normalization and immutability, keep the public interface small, and rerun all tests.

## 3. mods/ImmersiveChefs - Loadable runtime baseline

- [x] 3.1 Add the thin RimWorld Mod/Harmony entry point, stable startup marker, and optional load-order metadata without optional assembly references.
- [x] 3.2 Build without live deployment and verify generated About metadata, the RimWorld 1.6 assembly, and absence of bundled dependency DLLs.
- [x] 3.3 Migrate the stable package identity to `fumblesneeze.immersivechefs`, update every current contract/matrix/tooling reference, and verify the reviewed package through a fresh exact-mod RimWorld launch under the new identity. The reviewed DLL `7B2D6D72…` passed the Gateway-free Core/Harmony/product smoke at `artifacts/RimWorldSmoke/20260809T223956853Z` and the exact product integration-owner run at `artifacts/GatewaySmoke/20260809T224140197Z`; both visible main menus were clean and the latter passed 24/24 main-menu tests with only the Gateway's intentional danger warning.

## 4. mods/ImmersiveChefs - Test and game command runners

- [x] 4.1 RED: Execute the documented non-destructive contract calls before the command-runner scripts exist and record the expected failures.
- [x] 4.2 GREEN: Implement `Invoke-Tests.ps1` with path validation, TRX parsing, non-zero discovery enforcement, table/JSON output, and exit codes 0/1/2.
- [x] 4.3 Verify the test runner's passing, zero-discovery, invalid-path, and JSON-output contracts.
- [x] 4.4 GREEN: Implement `Invoke-RimWorldSmoke.ps1` with dry-run support, isolated configuration/log paths, owned-process cleanup, FlaUI evidence, table/JSON output, and exit codes 0/1/2.
- [x] 4.5 Verify dry-run configuration and prove the normal RimWorld configuration hash is unchanged.
- [x] 4.6 Run the real minimal-mod-list smoke check, capture FlaUI screenshot/log evidence, and verify owned process/service cleanup.

## 5. mods/ImmersiveChefs - Planned gameplay contract

- [x] 5.1 Create a separate planned OpenSpec change for all requested gameplay capabilities and identify Immersive Chefs as owner in every artifact.
- [x] 5.2 Define material/equipment, dishwashing, prep/stations, meal service, quality/temperature, expectations, compatibility, and sensible settings requirements with observable scenarios.
- [x] 5.3 Record food preservation and food waste as explicit future considerations outside the planned gameplay implementation scope.
- [x] 5.4 Strictly validate both OpenSpec changes.

## 6. mods/ImmersiveChefs - Repo-local development skill

- [x] 6.1 Initialize `.agents/skills/rimworld-mod-development` with skill-creator and focused reference resources.
- [x] 6.2 Write only the proven build/test/FlaUI workflow plus Harmony compatibility and Def/XML patching guidance.
- [x] 6.3 Validate the skill package and run a context-minimal forward test with an independent agent.

## 7. mods/ImmersiveChefs - Final verification and review

- [x] 7.1 Add concise contributor documentation with exact build, test, deploy, smoke, evidence, and OpenSpec commands.
- [x] 7.2 Run the complete clean build, guarded tests, strict OpenSpec validation, and artifact checks.
- [x] 7.3 Run independent code-review passes for correctness/tests and KISS/compatibility, fix accepted findings, and rerun verification.
