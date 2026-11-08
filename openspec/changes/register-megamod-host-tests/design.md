## Context

Owner: repository tooling **RimWorldModding.Mcp**. The current runner has an explicit suite registry;
its Unit alternatives already support .NET 10 without loading game assemblies. Megamod now has
concrete first behavior tests, owned by its P0 feature changes, requiring the same honest environment.

## Goals / Non-Goals

Register one exact suite and preserve existing output/zero-test rules. Do not create an alternate test
runner, fake a loaded mod, or make this tooling change own product behavior.

## Decisions

Add Megamod.Unit to the retained adapter's validated registry, map it to tests/Megamod.Unit, exempt
it from the legacy managed-assembly check, and report .NET 10. Exclude it from the legacy All group
because its product targets 1.7 explicitly. The product project source-links only host-pure Core
folders and has no game/Harmony references. Existing typed operation arguments remain unchanged.

Use the real typed invocation as the non-destructive dispatch regression: reject the unregistered
suite before the change; after registration execute exactly one selected product test, preserving
its real RED then GREEN result and canonical TRX/log path. Also select a nonexistent test and prove
the runner fails for zero executed tests. This is command verification, not gameplay acceptance.

## Risks / Trade-offs

Registry metadata can drift → verify actual selected framework, counts and filter in the returned
result. Product tests can fail independently → preserve their failure status rather than treating
dispatch success as a passing behavior test. No broad aggregate run is needed.

## First native data adapter environment

S029 now needs actual `DrugPolicy`, `DrugPolicyEntry` and constructed `ThingDef` storage, which fits
neither Unit nor Harmony. Register `Megamod.Defs` with the installed 1.7 root assembly layout and
.NET 10 reporting, outside All. Source-link only its concrete policy core and native storage adapter;
do not reference the product assembly or initialize a mod. Reference the matching game/platform
assemblies solely for native data types. Assembly entry/exit guards reject active mods, Harmony and
leaked ThingDefs. Nonparallel fixtures register every required Def in an initially empty scoped
database and guarantee cleanup. The first test preserves a duplicate winner's native object,
choices and edit buffer while removing invalid membership. Subsequent vertical slices cover
defaults, normalization, null storage and rollback. No XML-loaded content or game lifecycle is
simulated; early repair, UI entry and real saves remain native scenarios.

Verify dispatch using the actual rejected pre-registration selection, then the product test's
nonzero RED/GREEN pair and a deliberate unmatched filter. Reuse the existing typed runner and its
canonical evidence, without adding a public script or a new orchestration path.
