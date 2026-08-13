## 1. mods/RimWorldDevGateway — Contracts and RED tests

- [ ] 1.1 Add RED unit tests for strict target-catalog parsing, unknown fields, duplicate target IDs, invalid depot/manifest identities, invalid derived C# symbols, and missing approved file hashes.
- [ ] 1.2 Add RED unit tests for per-mod release manifests, including package/About disagreement, undeclared development/supported targets, duplicate package IDs or compatibility folders, invalid required/optional dependency identities, missing Workshop identity, and invalid file/XML-override policies.
- [ ] 1.3 Add RED tests proving invalid manifests fail before downloader, build, process-launch, or publisher adapters are invoked.
- [ ] 1.4 Define versioned schemas and typed contracts for target catalogs, mod manifests, required/optional mod graphs, C# target symbols, XML projections, dependency receipts, staged candidates, presentation bundles, verification results, dry-run diffs, and publication receipts.
- [ ] 1.5 Add RED manifest and localization-policy tests for explicit distributable/development-only classification, the six baseline languages, canonical keyed/Def/runtime inventories, missing/duplicate/stale entries, malformed XML, placeholder/tag drift, raw guarded UI literals, and the exact Gateway exemption.

## 2. mods/RimWorldDevGateway — Contract GREEN and refactor

- [ ] 2.1 Implement the minimum strict catalog and release-manifest loaders required to make the contract RED tests green.
- [ ] 2.2 Add repository-owned `release/rimworld-targets.yaml` and one non-production current-1.6 example release manifest with no invented compatibility claims and explicit empty required/optional dependency collections.
- [ ] 2.3 Refactor validation into side-effect-free public services and keep actionable property paths in every validation error while the focused and owning suites remain green.
- [ ] 2.4 Document the target-onboarding review that turns external discovery data into pinned manifest IDs and approved `Version.txt`/managed-file hashes.
- [ ] 2.5 Add RED/GREEN projection tests proving supported versions and required mod entries in generated About metadata come only from the per-mod manifest.
- [ ] 2.6 Implement strict distribution/language manifest contracts and a side-effect-free localization inventory/validation service; require agent/human authorship evidence without adding a machine-translation facility.

## 3. mods/RimWorldDevGateway — Exact Steam acquisition TDD

- [ ] 3.1 Add RED tests around a fake depot client for normalized selection hashes, cache hits, corruption quarantine, inaccessible manifests, per-depot failure isolation, leases, and secret redaction.
- [ ] 3.2 Implement the external content-addressed compile cache and immutable file index using a narrow DepotDownloader process adapter, then make the focused tests green.
- [ ] 3.3 Add RED tests and GREEN implementation for full-install cache identities composed from ordered base/DLC manifests without mixing them with compilation entries.
- [ ] 3.4 Refactor acquisition so DepotDownloader and the documented SteamCMD diagnostic fallback produce distinct provenance and never substitute the normal Steam installation.
- [ ] 3.5 Perform an opt-in real Steam smoke run that downloads only the declared current managed-file selection, verifies approved hashes, reuses the cache on a second run, and retains a secret-free receipt.
- [ ] 3.6 Attempt one desired historical target with the owning account and record an explicit inaccessible-manifest result instead of approving a nearby version if Steam rejects it.

## 4. mods/RimWorldDevGateway — Target-isolated build and staging TDD

- [ ] 4.1 Add RED integration tests proving two fake target projections receive separate MSBuild intermediate/output roots, exactly one matching `RIMWORLD<major>_<minor>` symbol, no mismatched managed assembly set, and no two compile targets in one compatibility folder.
- [ ] 4.2 Implement compile projections and explicit per-target `Zlepper.RimWorld.ModSdk` invocations with manifest-derived symbols, make the ordinary local build resolve the development target through the same rule, then make the focused build-matrix tests green.
- [ ] 4.3 Add RED tests for merging verified target outputs, generating unified supported-version and required-dependency About metadata, and rejecting a failed, unverified, or undeclared target.
- [ ] 4.4 Add RED package-policy tests for source, symbols, caches, credentials, undeclared files, tests, and Gateway assemblies entering a product candidate.
- [ ] 4.5 Implement positive-allowlist staging, sorted path/size/SHA-256 manifests, candidate digests, and immutable leases until all staging tests are green.
- [ ] 4.6 Refactor build/stage orchestration into independently rerunnable phases and prove identical inputs either reproduce hashes or name each nondeterministic file.
- [ ] 4.7 Add a C# fixture whose unconditional feature code calls a narrow seam with a `RIMWORLD1_6` legacy implementation, and capture RED/GREEN builds against synthetic current and legacy API shapes.
- [ ] 4.8 Add RED XML projection tests for canonical pass-through, ordered legacy add/replace/remove operations, a rationale-required document replacement, selector-cardinality drift, invalid output, duplicate Def identity, and unchanged source files.
- [ ] 4.9 Implement the minimum parsed typed XML operation engine and per-target ignored projection required to make the focused XML tests green without raw text preprocessing or version source folders.
- [ ] 4.10 Refactor symbol and XML projection provenance into the target receipt and reject staged compatibility folders whose symbol, transform, About version, or manifest identity disagrees.
- [ ] 4.11 Add a policy check that permits RimWorld version directives only under the neutral `Compatibility/RimWorld/` area or in exact manifest-allowlisted files with rationale, without requiring speculative legacy branches before a demonstrated divergence.
- [ ] 4.12 Make candidate staging reject a distributable mod whose English/German/Spanish/French/ChineseSimplified/Russian keyed and Def-injected catalogs do not exactly cover its player-text inventory or whose placeholders/tags/raw guarded UI strings fail policy; retain catalog hashes in stage evidence and exempt only explicitly development-only mods.

## 5. mods/RimWorldDevGateway — Exact-version regression runner TDD

- [ ] 5.1 Add RED runner tests for exact target resolution, exact executable/data paths, matching compatibility-folder package selection, full manifest-set evidence, and refusal to fall back to the normal installation.
- [ ] 5.2 Extend the scenario and grouped E2E runners with a declared target parameter while preserving their current default and exact ordered mod-list contracts.
- [ ] 5.3 Make the runner tests green for unique savedata, exact PID/start identity, normal `ModsConfig.xml` before/after hashes, graceful cleanup, and active-cache leases.
- [ ] 5.4 Refactor common target/process evidence without weakening player-action, screenshot-inspection, product-without-Gateway, or optional-mod gates.
- [ ] 5.5 Run the existing dry-run commands and one current-version process smoke to verify target plumbing and cleanup before attempting historical gameplay acceptance.

## 6. mods/RimWorldDevGateway — Presentation compiler TDD

- [ ] 6.1 Add RED tests for structured Workshop content, BBCode/template separation from About copy, complete concise mechanics/things/buildings inventories, manifest-derived compatibility with per-integration expected behavior plus Required Mods and Optional Mods sections, explicit empty states, exact RimWorld-line copy, a truthful final-section AI disclosure, required fields, link/tag allowlists, and the installed Steamworks SDK title/description limits.
- [ ] 6.2 Add RED renderer fixtures for sprite placement, missing/stale assets, fixed dimensions, offline-only loading, pinned fonts, and text-overflow bounds.
- [ ] 6.3 Implement deterministic BBCode compilation and HTML/CSS/Playwright rendering with pinned Chromium, local fonts, viewport, scale, and disabled animation/network access.
- [ ] 6.4 Make presentation validation green for deterministic rerenders, image dimensions/size, provenance hashes, local review HTML, and unchanged source art.
- [ ] 6.5 Refactor shared banner/feature-card templates into `release/templates/` while keeping mod-authored copy and sprite mappings under `mods/<ModName>/Release/workshop/`.
- [ ] 6.6 Personally inspect the fixture preview and generated images and record layout, readability, sprite, and overflow observations before accepting the renderer.
- [ ] 6.7 Add drift tests proving hand-authored Workshop copy cannot replace, omit, reclassify, or duplicate dependency entries supplied by the release manifest.

## 7. mods/RimWorldDevGateway — Inline-image hosting experiment

- [ ] 7.1 Define an isolated, update-only experiment against a dedicated unlisted Workshop item, with explicit user authorization, expected preview inventory, and recovery snapshot.
- [ ] 7.2 Upload additional preview fixtures, query their returned Steam CDN URLs/original names, embed them into a test description, and inspect the rendered Workshop result.
- [ ] 7.3 Test replacement/removal and a second update to determine URL stability and whether old descriptions break.
- [ ] 7.4 Decide between Steam-hosted preview URLs, a named immutable repository-owned static host, or carousel-only graphics; update this design/spec before production publisher implementation.

## 8. mods/RimWorldDevGateway — Publication state-machine RED tests

- [ ] 8.1 Define a narrow SteamUGC adapter and add RED tests for existing-item ownership, invalid/zero IDs, dry-run with no mutation, bounded metadata/content/required-item diffs, and nonce/hash binding.
- [ ] 8.2 Add RED tests for every setter failure, update-language/title/description/visibility/tags/content/metadata/preview mapping, authored change notes, update-only identity safety, and the guarantee that `CreateItem` is called only by an explicitly confirmed one-time bootstrap.
- [ ] 8.3 Add RED tests for asynchronous progress, one-operation ownership, pre-submit cancellation, polling timeout, late callback, indeterminate submission, retry, and immutable-stage lifetime.
- [ ] 8.4 Add RED tests for Steam authentication, connectivity, quota, legal-agreement, callback, wrong-item, and stale-remote-state failures with secret-free receipts.
- [ ] 8.5 Add RED tests for post-submit remote metadata/preview/required-item comparison, exact-item subscription/refresh, reacquired Workshop content-manifest verification, and refusal to substitute a repository-local package.
- [ ] 8.6 Add RED tests for asynchronous required-item addition/removal, optional-item exclusion, partial graph success, remote-state query before retry, and stale dependency confirmation.

## 9. mods/RimWorldDevGateway — Publication GREEN and refactor

- [ ] 9.1 Implement authenticated loopback-only versioned dry-run, confirm, and status contracts that validate the isolated RimWorld PID/start identity and bind the required-item graph into preflight hashes.
- [ ] 9.2 Implement the main-thread SteamUGC update-only adapter with checked setter results and asynchronous callback state until mapping tests are green.
- [ ] 9.3 Implement leased operation persistence, bounded progress, legal-agreement reporting, asynchronous required-item reconciliation, indeterminate recovery, and explicit remote-query-before-retry until lifecycle tests are green.
- [ ] 9.4 Implement remote metadata/preview/required-item query, exact-item subscription/refresh, separate Workshop-content reacquisition, and subscribed-path identity evidence until verification tests are green.
- [ ] 9.5 Refactor publisher code so HTTP parsing, release policy, SteamUGC calls, and evidence serialization remain narrow and independently testable while the owning suite stays green.

## 10. mods/RimWorldDevGateway — Release CLI integration

- [ ] 10.1 Add the repository-local `RimWorldReleaseTool` command surface and thin PowerShell entry point for targets, acquire, build, test, present, stage, dry-run, confirm, and verify.
- [ ] 10.2 Add integration tests proving phase receipts can resume safely, changed candidates or dependency graphs invalidate dry-run, dirty candidates cannot publish, and ordinary builds/tags never publish implicitly.
- [ ] 10.3 Add a dry-run end-to-end fixture from manifest validation through candidate/presentation review without launching RimWorld or changing Steam.
- [ ] 10.4 Update development, Gateway, testing-environment, and release documentation with bootstrap, cache, current-first version onboarding, C# seam/XML override authoring, dependency metadata, presentation review, recovery, and evidence procedures.
- [ ] 10.5 Update the repository release skill from planning guidance to tested commands only after the implemented command surface exists.

## 11. mods/RimWorldDevGateway — Builds, regression, and independent review

- [ ] 11.1 Run focused tests after every RED/GREEN/refactor slice and retain non-zero RED plus GREEN evidence; reject zero-test runs.
- [ ] 11.2 Run the owning Gateway/unit/integration suites, guarded repository suite, and Release builds, then inspect the generated Gateway and example product packages.
- [ ] 11.3 Run `openspec validate --all --strict --no-interactive` after every contract change.
- [ ] 11.4 Perform an independent scoped code review, resolve or explicitly reject every finding with evidence, and rerun all affected tests and builds.

## 12. mods/RimWorldDevGateway — Final isolated in-game verification

- [ ] 12.1 With explicit user authorization, launch a fresh isolated reviewed build against the dedicated unlisted Workshop item and bind Gateway discovery, actions, screenshots, diagnostics, and cleanup to its exact PID/start identity.
- [ ] 12.2 Perform dry-run, personally inspect the exact package/presentation/required-item remote diff, and confirm only the bound hashes.
- [ ] 12.3 Observe Steam progress and terminal callbacks, personally inspect the resulting Workshop metadata, required items, optional-mod behavior descriptions, final AI disclosure, and previews; then subscribe/refresh the exact item, reacquire its Workshop copy, and compare its content manifest with the reviewed stage.
- [ ] 12.4 Run each selected product mod's declared native player workflow on every claimed exact RimWorld target from the subscribed Workshop package path, never a local deployment, and personally inspect before/action/after screenshots.
- [ ] 12.5 Complete at least one separate product-mod acceptance run without the Gateway and retain its native workflow evidence.
- [ ] 12.6 Record source/build identities, target symbols, XML projection hashes, packaged and remote dependency graphs, ordered depot/mod lists, native actions, observable outcomes, screenshots, remote verification, normal-configuration hashes, and cleanup in one secret-free evidence directory.
- [ ] 12.7 Check implementation tasks only after the reviewed revision's tests, package checks, live publication proof, per-target player acceptance, Gateway-free product proof, and cleanup evidence all exist.

## 13. mods/RimWorldDevGateway — Immersive Chefs 1.6 release bootstrap

- [ ] 13.1 TDD: define and validate the clean committed single-target release descriptor, positive-allowlist staging, sorted SHA-256 package manifest, presentation hashes, and mutation-free first-publication plan.
- [ ] 13.2 REVIEW/GATE: independently review the bootstrap, run the complete release gates, inspect the staged package and presentation, and obtain explicit confirmation bound to the exact candidate and presentation hashes.
- [ ] 13.3 STEAM: create the first item through RimWorld's initialized Steam session, persist its returned identity before submission, check every setter/callback and Harmony dependency result, and retain a secret-free receipt.
- [ ] 13.4 SUBSCRIBER: subscribe/reacquire the exact item, compare its downloaded file manifest with the reviewed candidate, then launch RimWorld 1.6 with the subscribed copy and personally observe the declared native smoke workflow.
