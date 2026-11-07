# Verification boundaries

The initial scaffold has no C# source behavior, runtime entry point, Harmony patches, XML patches,
or Defs. Its assembly contains SDK metadata only. Build, schema, package, dependency, source/package
catalog parity, documentation links, and independent review are meaningful checks here. Creating an
empty test project or a startup message would not prove any gameplay. No native feature acceptance
is claimed for this scaffold.

On the initial 2026-11-01 check, Release builds with zero warnings/errors and the profile and strict
OpenSpec validation pass. At that checkpoint, canonical `package_validate` was blocked by the repository's
1.7 platform inventory drift against installed `1.7.4995 rev309`: `RimWorldWaylith.PrepatcherCompat`
and `Waylith.Injection` are undeclared, while `Ktx` and `gli` are no longer installed. This is not a
successful package-validation result. The tooling inventory must be reconciled under its own owner
before rerunning that gate; do not change or bypass it in megamod. Recheck the current tooling state
at implementation start rather than treating this historical failure as current. See the foundation tasks.

## First runtime slice

Implementation follows the [shared design](../../../openspec/changes/plan-megamod-parallel-implementation/design.md),
[parallel roadmap](../../../openspec/changes/plan-megamod-parallel-implementation/roadmap.md) and
[governing tasks](../../../openspec/changes/plan-megamod-parallel-implementation/tasks.md). The user
authorized a megamod-only scheduling exception: implementers produce focused host tests, reviewed
code and executable native scenarios; the orchestrator runs the integrated scenarios after all
selected chunks of a part are ready. Ready for integration is not accepted gameplay. Composite
feature tasks stay open or are split into traceable implementation and acceptance obligations.

Use [TestingEnvironments.md](../../../docs/TestingEnvironments.md) and the root development skills.
Create test projects under repository `tests/` only when concrete assertions need them, and register
their exact suites through the owning typed tooling contract. Suggested owning names are
`Megamod.Unit`, `Megamod.Harmony`, `Megamod.Defs`, `Megamod.InGame.IntegrationTests`, and
`Megamod.EndToEndTests`; **none exists or is registered yet**.

- Unit: pure rule/predicate/package-selection inputs; no loaded mods/Harmony/populated Def databases.
- Harmony: the explicitly owned target/patch only, with guaranteed cleanup of that owner.
- Defs: lightweight constructed real Def fixtures in an initially empty scoped database.
- Isolated game: actual active mods, complete patch startup, XML/Def inheritance, external consumers.
- Native E2E: player UI, designations, jobs, hauling, save/load, and other observable action outcomes.

Capture focused RED before minimum implementation, then nonzero GREEN and appropriate package
checks. Review independently before the final game run. Test negative target predicates and compatible
foreign patches, not just happy-path startup. XML slices include XML Extensions guards, absent or
changed external content, and merge preservation of other mods' entries.

UI slices verify the [placement rules](UIPlacement.md), native entry points, debug visibility,
ordered/scoped settings, disabled save controls from the main menu, and binding to the correct save
after switching games or opening settings in world view. Upstream bottom tabs are not a parity gate.
Settings cases include default inheritance, changed global values with inherited/overridden saves,
Use global round trips, persistence and missing values, the common three-control row's enablement,
and documented scope exceptions. Pawn/area/entity-specific gameplay state must stay in gameplay UI.

## Final acceptance for every behavior

The orchestrator is the acting acceptance agent and personally performs the checks below. Freeze
the reviewed integrated candidate before running its individual and combined scenarios. Return
product/scenario defects to their respective owners with exact-build evidence; after reviewed fixes,
rerun the affected workflows on the new candidate. Implementers do not run separate acceptance games.
Authoring, compiling or dry-running a scenario does not count as executing it. The orchestrator may
revise chunk boundaries and dependencies on evidence, retaining scope, status and revision history;
it may not waive the acceptance gate or silently discard requested behavior.

Use fresh minimized Gateway-backed 1.7 processes with unique disposable save data/logs and the
smallest exact ordered mod list. Drive the same native action a player uses and personally inspect
the before/action/after result from that exact process. Direct state mutation can arrange fixtures
but cannot manufacture the result. Repeat affected native workflows after runtime-affecting edits.

Run separate provider/consumer combinations for claimed compatibility and replacement-only versus
original-provider-present policy. Include composed behavior when selected source mods modify one
another. Preserve API/save/settings semantics or record the exact deviation; a compile pass cannot
establish a drop-in claim. Check contextual translations and render required locales with real UI.

Retain build/package hashes, active order, actions, observations, screenshots, relevant requests/logs,
normal-config before/after hashes, cleanup, and review decisions beneath ignored `artifacts/`.
Complete the balance/playability pass on progression, availability, costs, job reachability and
required/optional dependencies. Update only supported task/ledger states. No empty catalog, static
screenshot, successful endpoint, log message, or old-build run closes a gameplay task.

## Release readiness

The private profile is scaffolding for build/install/package tooling, not a finished release.
`presentation_render` has no renderer manifest yet; the SVG is only a labeled development placeholder
outside the product allowlist. Replace it with reviewed release presentation and complete exact-build
release pins and feature verification before preparing/publicizing a Workshop item. Do not weaken
asset acceptance to turn this placeholder into evidence.
