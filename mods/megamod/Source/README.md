# Source placement

The initial build produces SDK assembly metadata only. There is intentionally no `Mod` subclass,
static startup initializer, patch registry, or placeholder gameplay code.

Add concrete behavior under `Features/<Feature>/` after its specification and first failing test.
Keep feature-local external adapters under that feature's `Compatibility/<ExternalMod>/` directory.
Use `Patching/` for the first genuinely shared target-admission mechanism and `ThingData/` for common
map data only when concrete consumers require them. These are destinations, not abstractions to
implement in advance. Compose changes from multiple source mods with direct calls, not self-patches.

Use the [shared-component design](../../../openspec/changes/plan-megamod-parallel-implementation/design.md)
and [chunk allocations](../../../openspec/changes/plan-megamod-parallel-implementation/roadmap.md) for
ownership and prerequisites. A shared component has one writer; consumers use its agreed contract.
The orchestrator leases disjoint paths and owns common registration/project/package edits. Inspect
referenced mods and existing owned helpers to locate useful game paths, then independently implement
our contracts. Upstream code is not copied and does not override the specification or design.

Declarative Defs and guarded XML mutations belong in the mod's `Defs/` and `Patches/` directories.
Root `tests/` owns separate honest test environments; see [verification](../docs/Verification.md).
