## Context

The current verification stack has four distinct pieces: host NUnit suites, startup-gated in-game integration methods, isolated Gateway launches, and named scenario scripts that an agent finishes manually. The first two are repeatable but cannot prove multi-frame pawn jobs and player-visible consequences. The last two do prove those workflows, but every scenario is separately orchestrated and there is no command that discovers all applicable workflows, groups them by exact mod list, resets state, executes them in sequence, and emits one machine-readable result.

The owning mod is RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`). Product mods remain independent. E2E assemblies are test-only inputs staged below their active owning mod for an isolated run and are never copied into a product's `Assemblies` directory or release artifact.

## Goals / Non-Goals

**Goals:**

- Make real playable-map workflows repeatable after game and third-party mod updates.
- Run many isolated tests per exact mod combination while paying the RimWorld startup cost once per group.
- Express direct fixture arrangement separately from native player actions and observable results.
- Preserve the current exact-PID, isolated savedata, minimized/muted background launch, token hygiene, log scan, and normal-configuration restoration guarantees.
- Retain enough per-test evidence to diagnose a failed step without rerunning the whole matrix interactively.
- Provide a generic contract that future repository mods can use without changing Gateway core code.

**Non-Goals:**

- Replacing fast unit tests or startup-gated Def/Harmony integration assertions.
- Treating direct state mutation or a log marker as proof of gameplay behavior.
- Running E2E tests inside a normal colony, normal savedata folder, product assembly, or Workshop package.
- Parallel test execution inside one RimWorld process. Verse global state and one active map make this unsafe and harder to diagnose.
- Silently inferring load order from folder names, downloaded-but-inactive mods, or optional dependency heuristics.

## Decisions

### 1. Tests are attributed classes with a complete exact product mod set

A host-safe `RimWorldDevGateway.EndToEndTesting` contract will define `RimWorldEndToEndTestAttribute`, `IRimWorldEndToEndTest`, the step model, context boundary, assertions, result states, and failure types. Every concrete test class has one stable test ID and one complete ordered active package sequence excluding only the always-injected Gateway package. The sequence starts with Core when Harmony is absent; when Harmony is present, it starts with Harmony and Core in that order to honor Harmony's installed `loadBefore` declaration. It then includes DLC and every required library/product/optional package, contains a non-Gateway owning product package, and has no extras. A Gateway-owned infrastructure self-test instead names `fumblesneeze.rimworlddevgateway` as its owner while relying on the host's one implicit final Gateway entry; it MUST NOT list Gateway in the declared sequence.

The host build tool reads those attributes from compiled metadata without executing the test assembly. It rejects missing non-Gateway owners, duplicate IDs, empty/incomplete sequences, Gateway listed anywhere but the implicit final position, an owner that is neither present nor the exact implicit Gateway owner, unresolvable package IDs, output/attribute drift, and assemblies that do not implement the test interface. The runtime repeats the validation against the real active order before loading a test. Exact sequences are preferred over required/forbidden subsets because an E2E result is only meaningful for the combination actually running.

An explicit source manifest was considered, but it would duplicate method identity and mod requirements. A source generator was also considered; metadata inspection is smaller and keeps the attribute itself authoritative while still allowing the host to group before launch.

### 2. The contract is an iterator-based multi-frame protocol

`Arrange` runs once on the Unity main thread against an already reset playable map and may directly create disposable preconditions. `Execute` returns an `IEnumerator<EndToEndStep>`. The Gateway advances at most one ready transition per frame, allowing a step to wait across rendered frames and game ticks without blocking Unity.

An existing quickstart `Map` is not sufficient readiness: `TickManager.Pause` follows RimWorld's player-control guard and can legitimately reject during the brief generated-game handoff. The state machine therefore waits for `Game.PlayerHasControl` before capturing the isolation baseline, retries on later frames, and fails with a bounded infrastructure result if the test deadline expires. Iterator-side fixture maintenance may adjust fixed-cost test-only preconditions immediately after a persisted wait boundary, but it cannot substitute for a native action or visible observation. A generic synchronous callback step was rejected because a blocked delegate cannot be preempted from Unity's main thread; the host watchdog remains the outer defense for faulty test code.

Initial typed steps cover:

- exact native gizmo invocation from Thing owners and/or architect category Def names, including optional cardinal cell/line direction and independent exact Stuff selection for native build designators, plus exact float-menu order invocation;
- explicitly named `MayMaximizeWindowInputActionStep` click, drag, chord, key, and text input when no semantic path exists, warning at the call site that it may restore, foreground, or result in a maximized RimWorld window;
- an exact native Architect-category action that opens/closes the main tab and selects one loaded
  `DesignationCategoryDef` without restoring or foregrounding the process;
- an exact native in-play Escape-menu action that opens/closes `MainButtonDefOf.Menu` through
  `MainTabsRoot` without restoring or foregrounding the process;
- exact native trade-dialog transfer and acceptance actions that can run without restoring the minimized game window;
- exact allowlisted optional-mod dialog confirmations that retain private assembly/type/callback shapes in the Gateway rather than accepting reflection details from test bundles;
- pause and native speed control;
- thing selection and camera framing;
- reversible native screenshot-mode control for UI-free visual evidence without desktop input;
- predicate waits with test-declared frame, game-tick, and wall-clock deadlines;
- end-of-frame full or object-bounded screenshots;
- named observable checkpoints and bounded assertion details.

Product-owned test assemblies discover current right-click options through a host-safe `IEndToEndFloatMenuCatalog` context service. The returned metadata contains only visible label, disabled state, and a callback-sensitive stable identity. The test then submits that identity through `FloatMenuActionStep`; Gateway re-queries immediately and invokes the one exact native callback. This avoids a product-test dependency on Gateway internals and avoids retaining Unity/RimWorld delegates across frames.

For a `Command_Action` that opens its own on-screen `FloatMenu`, the Gateway snapshots existing menus before the native callback and retains only one exact newly opened instance as a short-lived automation lease. The following `CurrentFloatMenuActionStep` accepts only that same sole open window, uses its actual colonist-ordering mode, and mirrors the native tutorial allow/chosen/notify/close sequence. Replacement or ambiguity fails closed; successful close consumes the lease, while the next semantic action and isolation cleanup clear it. Any release of a still-open lease restores that exact menu's prior mouse-distance behavior.

`TradeDialogActionStep` is a narrow semantic adapter for the native trade window. Adjustment re-resolves the exact current `Tradeable` by physical Thing ID, uses the native count setter and dialog refresh, and acceptance invokes the exact compiler-generated callback owned by `Dialog_Trade`; version-shape drift fails instead of approximating the deal. This is deliberately separate from `MayMaximizeWindowInputActionStep`: known trade semantics remain minimized and deterministic, while map pointer tools, drags, keys, text, and unknown surfaces retain explicitly disruptive foreground Win32 input. Observable waits and screenshots still prove that the real trade deal and delivery occurred.

`DialogConfirmationActionStep` is likewise semantic but deliberately narrower than a generic reflection action. The shared bundle can name only the expected exact open-window runtime type. Gateway-owned registration pins the optional assembly identity and private callback/argument shape, requires one matching current window and a void callback, then reproduces the inspected close-before-confirm order. Unknown types and member drift fail closed without falling back to Win32 input. This keeps optional UI workflows minimized while preserving the contract's ban on test-supplied synchronous callbacks.

`ArchitectCategoryActionStep` is a narrow semantic adapter for build-menu evidence. The Gateway
uses RimWorld's native main-button activation/escape paths, resolves one exact loaded category, and
invokes the inspected category-click method against that tab's own cached category object. It fails
closed if player control, the category, tab activation, cached-tab cardinality, or the current
RimWorld method shape has drifted. It never uses desktop input, focus, restore, resize, or maximize.
The action opens UI only; a later screenshot and exact architect-designator catalog assertion prove
the player-visible menu contains the intended buildables.

Tests may inspect Verse state inside predicates and assertions, but a direct mutation cannot be registered as the player action or observable result. Each test result records which steps were `arrange`, `act`, `wait`, and `observe`. This makes dishonest fixtures reviewable without attempting to sandbox test code.

`ScreenshotModeActionStep` is the narrow presentation-state adapter for evidence captures. It changes only RimWorld's own `ScreenshotModeHandler.Active` flag on the Unity thread, is recorded as an `act`, and never synthesizes a key or touches desktop focus. A test that enables it must restore the prior value in guaranteed cleanup even when later steps fail. This is evidence preparation, not proof of product behavior; the actual player workflow and resulting rendered objects remain independently required.

`ShadowRenderingActionStep` is the corresponding narrow raster-measurement adapter. It changes only `DebugViewSettings.drawShadows`, invokes RimWorld's own `drawShadowsToggled` map-mesh invalidation path, and records the transition as an `act`. A test uses the shadow-free phase only to derive exact structural masks from same-camera staged screenshots, restores normal shadows before the public candidate, and restores the original global value again in guaranteed cleanup. It is never publication art or gameplay proof.

Damage-rendering catalogs have one deliberately narrow exception to the ordinary native-action rule: `SupportingHitPointFixtureActionStep` performs bounded, exact current-map hit-point setup as an explicitly named `act`. It accepts at most 64 unique Things and only ratios strictly between zero and one, resolves and validates every target before applying any mutation, and marks its outcome as direct supporting setup. It cannot prove combat or damage behavior; a separate native bash/melee workflow carries that acceptance. This avoids disguising fixture construction inside an assertion while retaining deterministic material/grade screenshots.

`Task`/`async void` test methods were rejected because Unity Mono continuations and process shutdown are difficult to own deterministically. A synchronous one-shot method was rejected because it would either block the main thread or directly tick the result into existence.

### 3. One process runs one exact group sequentially

The host command discovers and builds every marked E2E project, groups tests by the ordinal package sequence, and launches groups in deterministic sequence. Each process uses `-quicktest`, an isolated `-savedatafolder`, the exact group plus Gateway last, and a new explicit E2E startup flag. The Gateway discovers only staged bundles owned by active mods and executes only tests whose declared sequence equals the real active sequence after removing Gateway.

Cross-process PowerShell array binding is not used for a group's additional package IDs. The host writes the exact ordered values as one UTF-8 line per ID and passes a single file parameter to the child smoke launcher. Runtime save data lives under a short `smoke-NNN` branch independent of the descriptive group ID so Mono's Windows file APIs can still create atomic session filenames; the full group ID remains in aggregate and JUnit metadata. Raw child stderr is retained unchanged, while the JUnit writer replaces only characters that XML 1.0 cannot represent.

Within a process tests run by stable test ID. A failed assertion or test exception fails that test but does not prevent the reset and later tests. A failed or unverifiable reset taints the process, skips the remaining group as infrastructure failures, and makes the host launch result fail. A hung test is bounded by its declared deadlines plus a host watchdog; process termination remains the final cancellation boundary for arbitrary test code.

### 4. Every test gets a destructive disposable-map reset

Before the first test and in `finally` after every test, the runtime pauses the game, cancels targeters/designators, closes test-opened windows, clears selection, then removes every roof cell—including constructed roofs and overhead mountain—before destroying any map content. It next stops pawn jobs, removes every destroyable spawned map Thing/Pawn, removes zones and designations, releases reservations through normal destruction/job cleanup, and removes scenario-owned world objects registered with the context. Removing roofs first prevents unsupported roofs from collapsing onto fixtures while the map is being emptied. After map removal has finished, the reset clears RimWorld's live messages, visible and delayed letters, and currently active alert-readout cache so destruction notices or prior scenario alerts cannot contaminate the next test. Newly arranged conditions may populate fresh alerts normally. Permanent non-destroyable map features (for example quicktest steam geysers) remain part of the environment and are excluded from disposable-state verification. It restores developer/god mode, speed, camera, and input state to the group baseline.

The reset then verifies that no roof, destroyable spawned thing, pawn, zone, designation, active interaction, tracked world fixture, live message, visible/delayed letter, or active alert-readout entry remains. Exact private collection shapes needed only for delayed letters and the active readout are Gateway-owned and fail closed when the current RimWorld build drifts. Tests must register changes outside those generic surfaces through `DeferCleanup`. A failure in cleanup or verification is not ignored. The runner does not try to reset Def databases, Harmony patches, loaded packages, mod constructors, or static caches; tests requiring a different such state belong to another exact process group.

Generating a new map per test was considered, but repeatedly entering map-generation lifecycle paths is slower and introduces more global/world state than clearing one disposable quickstart map. Removing only a test's returned fixture handles was also considered, but it cannot protect later tests from leaked pawns, filth, zones, products, or jobs after an exception.

### 5. Runtime discovery and results reuse the integration-test safety model but remain separate

E2E bundles live under version-resolved `DevEndToEndTests` directories and are loaded only with the E2E startup flag. The host stages complete owner bundles atomically and removes only marker-owned stages in `finally`. The game never builds tests. Discovery, assembly loading/reflection, screenshots, result persistence, and endpoint snapshots use bounded background lanes; only test arrangement, steps, predicates, and assertions run on the main thread.

`GET /api/v1/end-to-end-tests` exposes durable discovery, current test/step, group state, and terminal results. It does not reuse `GET /integration-tests` because lifecycle assertions and multi-frame destructive workflows have different contracts, flags, state machines, and failure semantics.

The execution state machine sanitizes every failure before exposing a persistable snapshot. It replaces the exact session credential while streaming only a bounded prefix, then applies the existing 8 KiB Gateway diagnostic-message budget by encoded UTF-8 bytes; failure identities and stacks retain their narrower independent ceilings. This prevents a mod-controlled native rejection reason from turning an otherwise bounded four-target diagnostic into an oversized or credential-bearing artifact.

### 6. The host runner is a PowerShell command built on the existing launcher

`scripts/Invoke-RimWorldEndToEndTests.ps1` is Windows-only because it coordinates RimWorld, exact process identity, isolated configuration, screen-local input, and existing PowerShell launch/cleanup helpers. It provides test/group filters, configuration and path overrides, watchdog controls, and `-Output table|json`. Exit code `0` means every selected test and cleanup passed, `1` means a runtime/test/infrastructure failure, and `2` means invalid usage or discovery/selection failure.

The runner writes one aggregate JSON result and JUnit XML plus one directory per group and test. Evidence records exact package order, product/test/Gateway assembly identities and hashes, game version, PID/start identity, steps, assertions, screenshots, logs, endpoint snapshots, cleanup, and normal `ModsConfig.xml`/`Prefs.xml` before/after hashes. Standard output is stable and concise; diagnostics go to standard error.

### 7. Existing manual scenarios migrate, they do not disappear immediately

The first tracer is the adverse meal outcome because it requires arrangement, a native Undraft/time action, a real ingestion job, returned ware, visible thoughts, and an actual poisoning hediff. Existing scenario descriptors remain available until their E2E equivalents pass on the same reviewed behavior. Each migration deletes duplicate manual orchestration only after the E2E test retains equivalent or stronger evidence. Interactive scenarios can remain when their purpose is visual exploration rather than regression.

## Risks / Trade-offs

- **A test mutates undeclared static state** → Require exact process groups, context cleanup registration, reset verification, and fail/taint rather than pretending later results are isolated.
- **Destructive clearing exercises unstable RimWorld internals** → Keep reset code small, version-tested, exception-isolated, and validated before proceeding; use the process boundary when reset cannot be trusted.
- **Native UI labels are translated or modded** → Prefer stable gizmo/action identities and exact runtime cardinality; label matching must include expected type/owner and fail closed.
- **Rotated placement is faked after construction** → Carry an optional cardinal through the host-safe `GizmoActionStep` only for `Place`, and let the Gateway configure the exact native `Designator_Place` before its own preflight/designation path.
- **A waiter/pawn workflow takes nondeterministic time** → Tests declare predicate-based deadlines and capture current job/target/screenshot evidence on timeout rather than sleeping guessed durations.
- **One process crash loses group results** → Persist admitted/running/terminal test snapshots incrementally and let the host synthesize explicit aborted results for missing terminals.
- **Instrumentation makes E2E tests look like gameplay code** → Keep all contracts and assemblies in shared/tests/staged paths and add package checks rejecting them from releases.

## Migration Plan

1. Add the contract and host metadata discovery with host RED/GREEN tests.
2. Add runtime discovery, map reset, step execution, persistence, and endpoint behind the disabled-by-default startup flag.
3. Add the grouped host runner and one Gateway-owned infrastructure self-test bundle for Core plus the implicitly appended Gateway.
4. Add the first Immersive Chefs E2E assembly and adverse-meal tracer.
5. Convert existing manual scenarios by behavior area and retain old descriptors until each replacement is accepted.
6. Make E2E a documented pre-acceptance gate for relevant gameplay changes.

Rollback is deletion of marker-owned staged E2E bundles and omission of the startup flag. The launcher always uses isolated `ModsConfig.xml` and `Prefs.xml`; it never edits the user's normal files, verifies both hashes in `finally`, and closes only the exact launched PID.

## Open Questions

None blocking. The initial typed step set will grow only when an actual migrated workflow requires a new faithful player-control or observable checkpoint.
