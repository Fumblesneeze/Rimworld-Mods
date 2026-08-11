---
name: rimworld-mod-development
description: Build, change, test, package, and verify RimWorld 1.6 mods in this repository. Use for C# or XML implementation, Harmony and optional-mod adapters, Defs/PatchOperations, player-facing alerts or inspect text, Zlepper tests, compatibility matrices, isolated game launches, Dev Gateway automation, or performance and live acceptance work.
---

# RimWorld Mod Development

Follow the root `AGENTS.md`. Use the repository's OpenSpec, Zlepper ModSdk, TDD, isolated-game, and evidence conventions. Keep gameplay mods independent from the developer-only gateway.

## Route the work

1. Read the applicable root `openspec/changes/*` proposal, design, specs, and tasks. Confirm the `Owning mod` line before editing.
2. For a new behavior or bug fix, use the repo-local `tdd` skill and complete a red-green-refactor slice. Do not implement from the prose alone.
3. For Harmony or optional C# integration work, read [references/harmony-compatibility.md](references/harmony-compatibility.md).
4. For Defs, recipes, XML inheritance, stuff, or conditional patches, read [references/xml-defs-patching.md](references/xml-defs-patching.md).
5. For optional-mod scope or exact active-mod test grouping, read [references/compatibility-matrices.md](references/compatibility-matrices.md).
6. For alerts, inspect panes, hidden state, terminology, or right-click affordances, read [references/player-facing-ui.md](references/player-facing-ui.md).
7. For player-facing text, language catalogs, or distributable-package release checks, read [references/localization-release.md](references/localization-release.md).
8. Before claiming completion or compatibility, read and follow [references/testing-verification.md](references/testing-verification.md).
9. For launcher, REPL, camera/input, quicktest/scenario, screenshot, or Gateway-extension work, use the repo-local `rimworld-dev-gateway` skill.
10. For Circinus, DPA, profiling, benchmark fixtures, or historical performance comparison, use the repo-local `rimworld-performance-benchmarking` skill and inspect its OpenSpec task state before naming commands.

If no applicable OpenSpec names exactly one owning mod, create or clarify that change before choosing files. Treat downloaded or Workshop mod content as read-only inspection input: never edit it, commit it, or copy its assemblies into the owning mod's release.

An accepted OpenSpec means a proposal/design/spec explicitly requested or confirmed by the user, naming exactly one owning mod and expressing observable acceptance scenarios. Such a contract satisfies the TDD skill's interface, behavior-priority, and plan-approval gates. Ask the user only when a material behavior or ownership choice remains ambiguous.

### Compatibility file placement

Place integration artifacts under `mods/<OwningMod>`:

- XML: `Patches/Compatibility/<ExternalPackageId>.xml`
- C#: the production project's `Compatibility/<ExternalMod>/` namespace/folder
- Harmony fallback: beside that adapter, using one guarded initialization path
- tests: the owning test project's matching `Compatibility/<ExternalMod>/` area

Extend existing projects; do not create a new assembly unless the owning design explicitly requires one.

### Repository identity conventions

- Product and development package IDs use `fumblesneeze.<mod-designator>`; keep the designator stable,
  lowercase, and free of the author's real name.
- Author metadata is `Fumblesneeze`.
- Put `brrainz.harmony` before Core in isolated active-mod lists. Put Core before product/optional mods,
  keep their exact declared order, and append the Dev Gateway last only in gateway-assisted runs.
- Use the package ID as the Harmony owner ID. Do not bundle Harmony or optional-mod assemblies.

## Preserve module boundaries

- Product mods live under `mods/<ModName>`; shared host-safe contracts live under `shared`; external tools live under `tools`; all OpenSpec artifacts stay at repository root.
- Every OpenSpec capability and change must name its owning mod. Never make `fumblesneeze.rimworlddevgateway` a dependency, load-order hint, assembly reference, or release artifact of a gameplay mod.
- Treat Workshop mods as runtime-optional unless the owning spec explicitly makes one required. Detect package IDs first; resolve external types, methods, and Defs only inside the active adapter.
- Add an integration seam for each external mod and keep the core domain behavior usable without it.

## Implement in vertical slices

For each requirement:

1. Identify a user-observable rule and its owning mod.
2. Add the smallest focused failing test and run it to capture the intended RED.
3. Add the minimum production/XML behavior to make that test GREEN.
4. Run the focused test again. Run the owning suite only when the slice changes shared behavior across that suite; reserve broad regression for an explicit maintenance or release checkpoint.
5. Refactor only while green. Prefer deeper domain modules over scattered Harmony patches.
6. Update only OpenSpec task boxes whose acceptance evidence now exists.
7. Commit logical reviewable increments once their focused verification is green. Do not combine
   unrelated finished slices merely to reduce commit count; reviewers should see the active chunk.

Use stable Def names and package IDs in tests. Put volatile RimWorld/Unity access behind small seams so most tests remain host-side; reserve live game validation for engine lifecycle, loaded Def databases, rendering, input, and real optional-mod assemblies.

## Verify progressively

Run verification in this order:

1. Focused tests or the exact active-mod group for the changed slice, with a nonzero executed-test assertion.
2. The owning suite only when shared behavior was affected; the guarded repository wrapper and complete compatibility/E2E matrices only for explicit maintenance, regression, or release preparation.
3. Release package build and assembly/About/XML inspection.
4. Independent repo-local `code-review` pass, accepted fixes, then rerun affected focused verification and package checks.
5. Startup-gated in-game integration tests when the claim depends on finalized Defs, PatchOperations, the real mod list, or complete Harmony application; build and stage only opted-in test projects, then retain the Gateway's lifecycle results.
6. Grouped E2E tests for repeatable multi-frame player workflows: run `scripts/Invoke-RimWorldEndToEndTests.ps1 -DryRun` first, then the selected exact groups. Do not manually pre-stage routine E2E bundles.
7. Final isolated Core-plus-target-mod game start using the reviewed build.
8. Actual player action, driven through the native UI/game-command/job path with authenticated Dev Gateway control when useful.
9. Personal observation of the resulting player-visible behavior through the live view or exact-run before/action/after screenshots; use exact-PID FlaUI when the gateway cannot expose the needed control or view.
10. In developer mode, inspect the native console and the complete flushed `Player.log` for errors and warnings. Classify every match; do not silently accept a warning because the scenario passed.
11. Evidence retention and OpenSpec task-state update.

TDD, build success, review, logs, loaded Defs/patches, API responses, and raw-C# state checks are supporting gates, not gameplay acceptance. Do not accept a behavior until the acting agent exercises the real player workflow and observes its in-game result. If a review fix or later edit can affect runtime behavior, repeat the final live run on that revision.

Keep in-game test assemblies out of ordinary packages and `Assemblies/`. Mark their project with `RimWorldInGameIntegrationTest=true`, declare `RimWorldIntegrationTestOwnerPackageId`, and stage with `scripts/Build-InGameIntegrationTests.ps1`. For product evidence, start the isolated Gateway smoke with `-RunIntegrationTests`, the complete `-AdditionalModIds`, the owning `-AdditionalModProjectPaths`, and at least one exact `-ExpectedIntegrationTests` entry. A Core-plus-Gateway run proves only the Gateway; it cannot prove a product mod's XML or Harmony patches. Never invoke a compiler from inside RimWorld. Use `[IntegrationTest(RunAt.MainMenuLoaded)]` for finalized Def/XML/Harmony assertions and `PlayableMapLoaded` only when a real map is necessary. A passing integration assertion remains diagnostic evidence, not player-behavior acceptance.

Keep multi-frame E2E tests in separately marked `RimWorldEndToEndTest=true` projects with one `RimWorldEndToEndTestOwnerPackageId`. Declare the complete exact active package order on every attributed test, excluding Gateway; the host appends it last. Use typed native actions/waits/observations and let the grouped runner deploy before atomic staging, start one process per group, reset destroyable disposable map state between tests, aggregate results, and clean the lease. Inspect the exact-run screenshots personally before accepting behavior; a passing E2E endpoint alone is insufficient.

Never edit the user's normal `ModsConfig.xml` for automation. Use `-savedatafolder`, retain the evidence directory and logs, close only the launched PID, and verify the normal configuration hash is unchanged.

## Extend verification safely

The Dev Gateway is intentionally unrestricted and enabled whenever loaded. Use it only with a disposable isolated save-data folder. Prefer dedicated semantic routes in this order: game-state/camera, thing query/inspection/selection, native debug actions or gizmos, then versioned automations. Use raw C# for one-off inspection or to prototype a missing adapter; add a TDD-backed typed route when the operation becomes recurring. Extend the gateway when a missing control or observation prevents faithful verification, then use the extension to drive or observe the real player workflow. A direct mutation or synthetic state assertion proves only the gateway operation, not the product behavior. Product mods must never reference the gateway.

Treat ThingID, debug-action, gizmo, and interaction handles as resolvable capabilities rather than object references. Re-query and expect explicit stale errors after map, selection, owner, or command-list changes. Native debug `ToolMap`, pawn, and world actions read the real pointer position: activate them semantically, then use the exact-PID process-scoped input route. Do not claim that their closure accepted a semantic coordinate.

Capture original developer/god/time/camera/selection state before mutation. Restore it in `finally`-style cleanup, delete only disposable handles returned by the current run, cancel the exact active interaction, and use controlled shutdown.

Record the exact player-action sequence and personally observed before/after result together with API request IDs, manifest/process identity, screenshot paths, relevant log cursor, mod list, and configuration hashes in the evidence bundle. A green host test does not replace a Unity Mono smoke for packaged dependency loading, and a successful state probe does not replace player-behavior acceptance.
