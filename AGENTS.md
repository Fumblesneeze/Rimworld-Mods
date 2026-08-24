# Repository instructions

These instructions apply to the entire repository. More specific `AGENTS.md` files may add local constraints, but may not weaken the acceptance gate below.

## Mission and boundaries

This is a RimWorld 1.6 mod monorepo:

- OpenSpec proposals, designs, capability specs, and task lists live under `openspec/` at the repository root.
- Playable mods live under `mods/<ModName>`.
- Host-safe shared contracts live under `shared/`, companion tools under `tools/`, and NUnit projects under `tests/`.
- Every OpenSpec change and capability must name exactly one owning mod.
- `fumblesneeze.immersivechefs` must never reference, depend on, load-order-hint, or ship `fumblesneeze.rimworlddevgateway`.
- Workshop/downloaded mods are read-only inspection inputs. Never edit them or copy their assemblies into a release package.
- Optional integrations must be package-ID/Def-resolved, absent-safe, shape-guarded, and isolated behind narrow adapters.
- Food preservation and food waste are future scope. Do not implement them as part of the current Immersive Chefs gameplay change.

Before changing RimWorld C#, XML, Defs, Harmony patches, compatibility adapters, tests, packaging, smoke tooling, or the gateway, read and follow `.agents/skills/rimworld-mod-development/SKILL.md` and its routed references. Use the repository's `tdd` and `code-review` skills when they apply.

Before arranging a colony, gameplay showcase, or lived-in fixture whose visual credibility matters, read and follow `.agents/skills/rimworld-realistic-base-generation/SKILL.md`. Ground room geometry, materials, traffic, decor, and optional-mod placement in its inspected player-reference catalog and exact game contracts; do not improvise a decorated cleared-map box and call it a colony.

Immersive Chefs gameplay showcases are human-deferred. Agents MUST NOT arrange, capture, synthesize,
promote, or publish those showcases. Their checked-in declarations and design records are human briefs,
not agent work queues or release inputs. Agents may publish Immersive Chefs updates only when the
immutable release plan explicitly excludes all gameplay-showcase media and evidence; the ordinary
reviewed feature cards remain separate Workshop presentation assets.

## Non-negotiable acceptance gate

TDD and code review are necessary, but they are never sufficient to accept RimWorld behavior.

No mod behavior, bug fix, Harmony/XML integration, compatibility claim, or game-facing gateway capability is complete or accepted until the acting agent personally verifies the reviewed build in a running RimWorld process and observes the behavior through a real player workflow.

Acceptance requires all of the following:

1. Perform an actual player action, or faithful automation of the same native UI, gizmo, designation, bill, job, hauling, ingestion, construction, selection, or game-command path a player uses.
2. Observe the resulting player-visible or player-observable game behavior in the running game. Examples include a pawn performing the expected job, a physical item changing or appearing, a building accepting work, an inspect pane or gauge changing, a thought or hediff appearing, or a native command visibly taking effect.
3. Personally inspect the relevant live view or before/action/after screenshots captured from that exact process. Record what was observed, not merely what an automation reported.
4. Retain enough evidence to connect the input action causally to the observed outcome on the tested build.

The following are useful supporting or diagnostic evidence, but do **not** satisfy live acceptance by themselves:

- a passing unit/integration test suite or Release build;
- a startup marker, log entry, absence of exceptions, loaded Def, or installed Harmony patch;
- a successful HTTP status, gateway JSON response, request journal, or synthetic state assertion;
- raw C#/REPL code that reads or directly mutates the expected state;
- direct spawning or setup code that constructs the desired end state while bypassing the player workflow;
- a static screenshot that shows only the prepared scene and not the action-to-result behavior.

The REPL, logs, Def/patch inspection, direct spawning, and state queries may arrange disposable preconditions, diagnose failures, correlate evidence, and clean up. They must not replace the real action or manufacture the result being claimed. For example, setting a component's `dirty` flag through C# does not prove that completing a cooking job dirties cookware; the pawn must perform the cooking path and the resulting cookware state must be observed in game.

If the agent cannot name and exercise a player workflow for a claimed behavior, the work remains incomplete. Do not waive the gate, reinterpret a diagnostic as acceptance, or check the corresponding OpenSpec task. Report the exact missing capability or blocker.

Pure documentation or repository-maintenance changes that cannot affect packaged/runtime behavior do not require launching RimWorld, but they cannot close a gameplay task or be used as evidence for one.

## Host-test environment truthfulness

- Put ordinary calculations and package-ID policy in `ImmersiveChefs.Unit`; this process must remain free of Harmony, loaded mods, and populated representative Def databases.
- Put an explicitly owned patch in `ImmersiveChefs.Harmony`; acquire only the target/patch needed by the test and remove that owner in guaranteed cleanup.
- Put constructed lightweight `Verse.Def` fixtures in `ImmersiveChefs.Defs`; register every required fixture in one initially empty scoped database. These are real `Def` objects, not XML-loaded game Defs and not mocks.
- A test requiring a real active mod, its constructor/static initialization/full patch set, Core or Workshop `ThingDef`/`RecipeDef`, inheritance, cross-references, `DefOf`, or PatchOperations belongs in a fresh isolated RimWorld process with the exact ordered mod list. Def and patch inspection is preflight/supporting evidence; the player workflow remains the acceptance proof.

Read `docs/TestingEnvironments.md` before adding a test that depends on RimWorld global state. Create a new process-isolated test project only for a concrete combination that cannot fit one of the existing honest environments; never fake a loaded mod by mutating `LoadedModManager`.

## Required change workflow

1. **OpenSpec:** Identify the applicable change, confirm its owning mod, and read its proposal, design, capability specs, and tasks before editing. Add or correct observable scenarios when the contract is missing. Do not implement from an unowned requirement.
2. **Vertical TDD:** Work one behavior at a time: focused RED, minimum GREEN, then refactor while green. Test public behavior rather than private implementation. Record durable RED/GREEN evidence and reject zero-test runs.
3. **Regression and package checks:** During feature work, run only the focused test IDs, exact active-mod groups, owning project build, and package checks affected by the current slice. Do not replay completed scenarios or run the guarded repository/full E2E suite merely because another feature changed. Reserve full-suite and full compatibility-matrix runs for explicit release preparation or deliberate maintenance/regression work. Run `openspec validate --all --strict --no-interactive` for specification changes.
4. **Independent review:** Use the `code-review` skill on the scoped diff. Resolve or explicitly reject each finding with evidence, then rerun affected tests and builds.
5. **Final in-game acceptance:** On the reviewed build, run the isolated player workflow and personally observe its result. Earlier exploratory runs are not final evidence. If review fixes or later edits can affect runtime behavior, repeat the live acceptance run.
6. **Evidence and task state:** Retain the action sequence and observed result with the tested revision/build identity. Check an OpenSpec task only after all of its required evidence exists.

Never reuse live evidence from an older build to accept a changed runtime revision. Host tests should cover deterministic rules and failure cases, while the final RimWorld run proves engine wiring, real jobs/UI, loaded Defs, rendering, input, lifecycle, and supported optional-mod behavior together.

## Live-run safety and evidence

- Use a unique disposable `-savedatafolder`, a dedicated log, and the smallest explicit mod list that proves the scenario.
- Retain at least one product-mod acceptance run without the Dev Gateway. Use separate installed-mod-present runs for compatibility claims. This mandatory product-only run is the sole Gateway-free exception below: launch it isolated and minimized, but when its native player action genuinely has no process-local semantic control, automation may briefly restore or foreground only that exact PID for the minimum required input, then return it to minimized state. Do not use that exception for presentation capture or ordinary Gateway-capable verification.
- Keep automated RimWorld processes minimized and use the Dev Gateway as the primary control, native-player-action, observation, and screenshot surface so verification does not disturb the user's desktop. Use direct OS-level screen, mouse, or keyboard input only when a faithful workflow is genuinely impossible through the Gateway and no practical Gateway extension can provide it.
- Refuse to reuse an existing RimWorld process. Bind automation, screenshots, input, diagnostics, and cleanup to the exact launched PID and process start identity.
- Hash the user's normal `ModsConfig.xml` before and after. Never use the normal save/configuration for automation.
- Capture the tested build/package identity, ordered mod list, exact player actions, before/after observable state, screenshots, relevant supporting logs, request IDs where applicable, configuration hashes, and cleanup result in one evidence directory.
- Restore camera, selection, developer/god mode, speed, and other mutated state. Delete only disposable objects created by that run.
- Request graceful shutdown first. Use bounded diagnostics and an exact-PID fallback only when necessary; never kill an unrelated process.
- Never retain bearer tokens or a live `current.json` in durable artifacts.
- Respect a user's stop or no-launch request. Do not relaunch until explicitly authorized; leave live acceptance incomplete instead.

Startup, a clean log, and one end-state screenshot are prerequisites, not proof of a gameplay scenario.

## Extending and using the Dev Gateway

Future agents are authorized to extend the Dev Gateway whenever a missing capability makes faithful game control or observation difficult. Treat a missing Gateway capability as specification and implementation work rather than a reason to take over the foreground desktop: update the owning Dev Gateway OpenSpec contract, implement and test the capability, then use it with RimWorld minimized. Prefer a small TDD-backed typed route or versioned automation for a recurring operation; use raw C# only to explore the needed seam or for a genuinely one-off diagnostic.

Gateway extensions must follow these rules:

- Update the owning Dev Gateway OpenSpec contract and tests.
- Keep the server dev-only, authenticated, loopback-bound, and unrestricted by default. EmbedIO owns HTTP parsing; the raw-text Mono.CSharp endpoint must remain directly usable without a companion client unless the user changes that contract.
- Keep player-action controls semantically distinct from diagnostic reads and direct mutation helpers.
- For control, invoke the native player path wherever possible. For observation, expose state that corresponds to something a player can perceive, and correlate it with the live view.
- Use the new capability in an actual in-game verification. A route returning `ok`, or directly constructing the expected result, proves only the route—not the product behavior.
- Never introduce a gateway reference into a product mod or package.

If verification needs clicking a custom window, choosing a material, drawing a zone, operating a multi-stage gizmo, inspecting a pawn thought, or following a job sequence and the gateway cannot do it reliably, extend the gateway rather than guessing. The extension should make the real workflow controllable or observable, not bypass it.

Gateway work must remain bounded and robust: do not block the Unity thread with network I/O, sleeps, recursive lazy-menu evaluation, or unbounded enumeration. Serialize raw input, propagate cancellation, isolate failures from individual modded objects/providers, journal admitted requests, restore pressed inputs in cleanup, and preserve retryable lifecycle ownership after timeouts.

## Repository automation surface

Use the repository-owned `RimWorldModding.Mcp` operation registry as the public automation surface for
RimWorld mod development, verification, live diagnostics, local installation, and publishing. Trusted
local Codex sessions start it from `.codex/config.toml`; the matching CLI projection is for CI,
recovery, and transparent reproduction. PowerShell scripts and companion executables may remain only
as internal migration adapters owned by typed operations with bounded execution, validation,
cancellation, and evidence handling.

Before adding or repeating orchestration, copied parsing, process ownership, safety boundaries, or
evidence-generation logic, inspect `operation_list`. If the capability is absent or a workflow is being
repeated, add an owning OpenSpec scenario and a typed MCP/CLI operation instead of another public
script or ad hoc command sequence.

## Common commands

```powershell
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- serve --repository-root .
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- tool list -o table
dotnet run --project .\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj -- tool call repository_status --arguments '{}' -o json
```

Use `operation_list` to discover the typed arguments for builds, tests, package validation, game and
E2E runs, Gateway control, evidence reads, local mod-list changes, and release preparation/publication.

Use the grouped E2E runner for repeatable multi-frame player workflows. Each attributed test declares its complete exact non-Gateway package order; the runner appends Gateway last, deploys before staging, starts one fresh isolated process per group, executes same-group tests sequentially, persists screenshots/results, and cleans its exact lease. Do not manually pre-stage E2E bundles for routine verification. A green group is supporting evidence until the acting agent personally inspects its exact-run screenshots and confirms the native action caused the visible outcome.

Normal builds must remain repository-local. Deploy into only the exact local package-ID folder and only for an intentional isolated run; never deploy into Workshop content. Inspect `README.md`, `docs/Development.md`, and `docs/Gateway.md` for current commands, API details, and evidence conventions. Durable run evidence and its TDD ledger remain local under ignored artifact/report paths and must be regenerated for the revision under test.
