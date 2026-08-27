## 1. Contract and public CLI TDD — `tools/RimWorldModding.Mcp`

- [x] 1.1 Record the official stdio MCP/config and C# SDK inputs, pinned versions, command names, risk classes, exit codes, JSON/table shapes, and migration-adapter inventory.
- [x] 1.2 RED: add focused tests and a real non-destructive CLI helper for repository status, invalid input, help, JSON/table output, and an MCP initialize/list/call round trip; retain the expected failures.
- [x] 1.3 GREEN: scaffold the .NET 8 single-file `tools/RimWorldModding.Mcp` project with the shared operation registry, source-generated JSON, clean stdout/stderr separation, and the minimum repository-status operation.
- [x] 1.4 REFACTOR: centralize root/path validation, process execution, deterministic results, errors, risk metadata, cancellation, and run evidence without adding an arbitrary-command operation.
- [x] 1.5 Publish the single-file executable and rerun the exact helper, focused tests, help/invalid cases, and MCP round trip green.

## 2. Repository operations TDD — `tools/RimWorldModding.Mcp`

- [x] 2.1 RED: specify focused scenarios for mod/profile discovery, exact OpenSpec validation, project/mod build, exact test/filter selection, zero-test rejection, package allowlist validation, and evidence reads.
- [x] 2.2 GREEN: implement typed discovery, spec, build, test, package, and evidence operations over the existing proven engines.
- [x] 2.3 REFACTOR: normalize runner outputs and artifact identities while keeping guarded full-suite execution explicit.
- [x] 2.4 Build and run focused operation tests against Guest Bed Gizmo plus one failure fixture.

## 3. Process and live-game operations TDD — `tools/RimWorldModding.Mcp`

- [x] 3.1 RED: add lifecycle tests for exact child PID/start identity, run status/cancel, bounded output, graceful shutdown, and unrelated-process protection.
- [x] 3.2 GREEN: implement leased run start/status/cancel for isolated game, smoke, integration, and grouped E2E adapters.
- [x] 3.3 RED: add authenticated Gateway diagnostic/mutation separation, bearer-redaction, request-correlation, and cancellation tests.
- [x] 3.4 GREEN: implement health, diagnostic, raw diagnostic, and explicit mutation operations bound to the selected run's live Gateway identity.
- [x] 3.5 REFACTOR: consolidate repeated process/Gateway parsing and mark internal scripts as migration adapters.
- [x] 3.6 Exercise one minimized exact-process player workflow through the MCP and personally inspect its retained before/action/after evidence.

## 4. Normal mod-list operations TDD — `tools/RimWorldModding.Mcp`

- [x] 4.1 RED: cover exact discovery, one-entry anchored insertion, duplicate handling, unrelated-order preservation, invalid XML, active-process refusal, atomic write failure, and exact backup restoration.
- [x] 4.2 GREEN: implement inspect/enable/disable/restore with hashes, timestamped backups, XML reparse, atomic replacement, and rollback.
- [x] 4.3 REFACTOR: isolate normal-config access from all automated savedata/config generation.
- [x] 4.4 Enable `fumblesneeze.guestbedgizmo` after `orion.hospitality` in the user's normal mod list and retain backup plus before/after validation evidence.

## 5. Universal release profiles and preparation TDD — `tools/RimWorldModding.Mcp`

- [x] 5.1 RED: test the strict profile schema with two valid mods plus unknown field, duplicate package ID, identity mismatch, path escape, arbitrary command, missing item/opt-in, and dependency mismatch failures.
- [x] 5.2 GREEN: implement profile discovery/validation and migrate Immersive Chefs plus Guest Bed Gizmo to the same schema without product names in publisher code.
- [x] 5.3 RED: test clean committed admission, dirty refusal, registered build/presentation steps, positive staging, forbidden-file rejection, deterministic manifests, and candidate digest invalidation.
- [x] 5.4 GREEN: implement universal candidate preparation and mutation-free local validation, using proven scripts only as typed migration engines.
- [x] 5.5 REFACTOR: remove product-specific policy from retained publisher adapters and document each remaining parity/removal owner.

## 6. Guarded Steam publication TDD — `tools/RimWorldModding.Mcp`

- [x] 6.1 RED: cover existing-item updates, explicit first-publication opt-in, exact-title owner scan, Private bootstrap, canonical dry-run diff, digest/nonce/expiry binding, owner mismatch, changed candidate, and duplicate-create prevention.
- [x] 6.2 GREEN: implement `release_prepare`, `release_status`, and `release_publish` over the initialized Gateway Steam publisher with retained admitted-operation identity.
- [x] 6.3 RED: cover remote metadata/preview/required-item/content verification, dual identity persistence, first-opt-in disablement, identity-only commit, reacquisition, local-package removal/restoration, and incomplete post-upload recovery.
- [x] 6.4 GREEN: implement post-upload remote verification, identity persistence, Workshop reacquisition, subscriber workflow dispatch, and local restoration.
- [x] 6.5 REFACTOR: retire the Immersive Chefs-specific agent-facing publisher entry point in favor of universal profile selection.

## 7. Repo-local MCP configuration and process — `tools/RimWorldModding.Mcp`

- [x] 7.1 Add `.codex/config.toml` with the required stdio server, explicit command/args/timeouts, and Codex's logical workspace-root fallback; expose operation risk metadata without repository-added approval prompts, then validate it with Codex MCP discovery.
- [x] 7.2 Update `AGENTS.md`, development/Gateway/release documentation, and common commands so the MCP/CLI is the public surface and repeated automation triggers an OpenSpec/tool change.
- [x] 7.3 Inventory every remaining public RimWorld script/host entry point, map it to an owning operation or documented exceptional backend, and reject new unowned duplicates.
- [x] 7.4 Run strict OpenSpec validation and the full focused MCP/CLI test/build suite.
- [x] 7.5 RED: reproduce cold concurrent Codex-style starts and add focused bootstrap/config tests which reject shared build races, protocol-stdout contamination, divergent TOML/JSON launch projections, and non-retryable partial publication.
- [x] 7.6 GREEN: add the bounded internal source bootstrap, immutable verified build cache, and root `.mcp.json`; re-enable `.codex/config.toml` on the shared bootstrap command without adding a second operation surface.
- [x] 7.7 Run cold concurrent initialize/list/call verification, focused MCP tests, strict OpenSpec validation, and an ephemeral Codex client discovery against the enabled project configuration.

## 8. Guest Bed rename and release use — `tools/RimWorldModding.Mcp`

- [x] 8.1 Update the Guest Bed OpenSpec contract and all public metadata to the initially ordered title while retaining `fumblesneeze.guestbedgizmo` and internal assembly identity; the corrected spelling is owned by the later annotated Workshop slice.
- [x] 8.2 Regenerate the deterministic Steam/About preview with the exact title, update its pinned manifest/brief, inspect both outputs, and obtain context-free review against the retained raw in-game frames.
- [x] 8.3 Build and package the renamed mod through the MCP, rerun focused tests, validate the product package, and repeat live acceptance only if runtime bytes changed.
- [x] 8.4 Perform independent scoped code review, resolve every finding, and rerun affected tests/builds.
- [x] 8.5 From a clean committed release worktree, run universal MCP release preparation, verify that its exact Steam remote diff, candidate digest, nonce, visibility, dependency graph, and hashes match the user’s standing publication order, and proceed without a redundant second confirmation.
- [x] 8.6 Publish the admitted first item Private, verify remote truth, persist/commit its ID, reacquire the subscribed copy, exercise and inspect the native subscriber workflow, restore the local package/mod list, and retain evidence.
- [x] 8.7 Hibernate Windows only after all requested release and verification work is complete and no recovery action remains outstanding.
- [x] 8.8 Reproduce the subscribed-copy startup race and direct screenshot-result mismatch; require a playing map with no active/waiting long event, bound every readiness request/delay by the workflow deadline, accept the Gateway CLI's exact direct screenshot metadata, add focused regression coverage, independently review the fixes, and resume verification against the same Private Workshop item.

## 9. Universal Steam DLC/Application Relationships — `tools/RimWorldModding.Mcp`

- [x] 9.1 RED: add focused profile, plan, baseline, and Gateway fixture tests that distinguish Workshop required items from required DLC/application IDs and reject invalid/duplicate app IDs.
- [x] 9.2 GREEN: freeze, diff, reconcile, and verify the exact Steam app-dependency graph through the universal release profile and authenticated Gateway publisher without adding product-specific code.
- [x] 9.3 Prove the existing required-item path remains exact, run focused MCP/compiler/OpenSpec checks, and independently review the scoped publisher change before using it on the retained Private Guest Bed item.

## 10. Self-contained End-to-End Host tests — `tools/RimWorldModding.Mcp`

- [x] 10.1 RED: select the existing End-to-End Host metadata contract through `test_run` and prove the Gateway group omits that suite and its required Immersive Chefs performance fixture can remain stale.
- [x] 10.2 GREEN: register `RimWorldDevGateway.EndToEndHost.Unit` and make its build own the exact Immersive Chefs performance fixture dependency.
- [x] 10.3 Run the registered suite through the typed operation and prove the XML Extensions package order against freshly compiled metadata.

## 11. Build-to-local deployment default — `tools/RimWorldModding.Mcp`

- [x] 11.1 RED: add focused tests proving a successful build installs the selected positive-allowlist package and that a failed/non-zero build leaves the existing local package unchanged.
- [x] 11.2 GREEN: make `mod_build` install its successful package into the configured local RimWorld `Mods/<package-id>` directory by default; stage outside the scanned Mods folder and remove the old install-backup path from normal synchronization.
- [x] 11.3 REFACTOR: expose the combined build/install result through MCP/CLI JSON and table output, update the operation contract and development documentation, and retain subscriber-verification recovery as a separate explicit workflow.

## 12. New-mod baseline guidance — `tools/RimWorldModding.Mcp`

- [x] 12.1 Document the canonical new-mod folder/project/profile layout, package identity, OpenSpec ownership, test-layer routing, and optional-integration boundaries.
- [x] 12.2 Make the no-backup, successful-build-to-local-install behavior explicit in AGENTS, README, development docs, and the repo-local mod-development skill.
- [x] 12.3 Add the repeatable new-mod checklist with typed MCP/CLI commands for profile validation, build/install, package validation, focused verification, and release preparation.

## 13. Steam additional-preview query resilience — `tools/RimWorldModding.Mcp`

- [x] 13.1 RED/GREEN: reproduce authenticated SteamUGC image entries with empty URLs; add an exact-count, bounded, Steam-CDN-only Community-page fallback parser and use its ordered URLs to hash the same remote preview bytes without weakening the immutable baseline.
- [x] 13.1a RED/GREEN: reproduce Steam omitting the screenshot inventory for the fallback's custom client fingerprint while serving it to an ordinary browser-shaped request; pin that exact bounded request shape without weakening origin, redirect, or byte ceilings. RED `20260827T085339893Z-72796-2070b61209a4463292fae499377ec85b` failed the exact missing browser User-Agent/language assertions before the minimum request-shape fix; GREEN `20260827T092000000Z-WorkshopRemoteBrowserGreen` passed 7/7 focused fallback, request, redirect, and byte-boundary tests with the production custom client fingerprint retained as the precondition.
- [x] 13.1b RED/GREEN: reproduce the retained Workshop item exposing fewer visible Community screenshots than its API-declared additional-image slots; preserve a URL-empty hidden slot only when the visible Steam-CDN sequence unambiguously aligns around exact nonempty API anchors, and fail closed for ambiguous incomplete inventories. RED `20260827T093500000Z-SteamHiddenPreviewRed` failed the live-shaped 3-slot/2-visible alignment before the exception existed; GREEN `20260827T100000000Z-SteamHiddenPreviewGreen` passed 8/8 exact/ranged-count, hidden-slot, ambiguity, browser-shape, origin, redirect, and byte-boundary tests.
- [x] 13.1c RED/GREEN: retain the bounded exact Gateway automation failure code/message when publication fails before mutation so retry exhaustion remains diagnosable without exposing credentials. RED `20260827T103000000Z-SteamAutomationDiagnosticRed` failed to compile because the diagnostic projection did not exist; GREEN `20260827T103500000Z-SteamAutomationDiagnosticGreen` passed the exact failed-run code/message retention regression.
- [x] 13.1d RED/GREEN: reproduce an existing-item update from a fresh clean worktree with no ignored durable identity; atomically seed it only from matching checked-in and immutable candidate identities after authenticated preflight, and reject mismatches without overwrite. RED `20260827T105000000Z-SteamDurableIdentityRed` failed to compile because the guarded identity projection did not exist; GREEN `20260827T105500000Z-SteamDurableIdentityGreen` passed exact creation, existing-mismatch non-overwrite, and source-mismatch no-create assertions.
- [x] 13.1e RED/GREEN: reproduce Steam rejecting the final 8,341-byte resolved Workshop description with `k_EResultInvalidParam` while the shorter tokenized template remained green; validate the exact submitted UTF-8 file and terminator boundary during profile discovery/preparation, then shorten redundant player-facing copy without dropping required inventories, dependency chains, images, or the final Author's Note. RED failed to compile before the shared resolved-description policy existed; GREEN passed the exact 7,999/8,000-byte boundary, NUL rejection, and Immersive Chefs' 7,706-byte resolved description with every presentation contract intact.
- [ ] 13.2 REVIEW/LIVE: independently review the fallback, run the focused MCP tests and strict OpenSpec validation, then prepare and publish the retained Immersive Chefs update through the exact live Steam response that triggered the regression.
