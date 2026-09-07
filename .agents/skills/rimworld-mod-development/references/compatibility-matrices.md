# Optional-mod compatibility and matrix design

## Inspect before specifying

Treat downloaded mods as read-only evidence. Record canonical package ID, display name, version,
About dependencies/load hints, assembly identity/MVID/SHA-256, Def names, and the exact public/internal
shape selected by the adapter. Do not edit Workshop content, copy its assemblies, or infer behavior
from a similarly named mod.

Map each shared compatibility surface to one owning mutation strategy:

- **passive** — product behavior composes without an adapter;
- **XML** — conditional targeted Def/PatchOperation composition;
- **public seam** — documented comp, job, extension point, or API;
- **guarded reflection** — exact supported runtime shape behind a narrow adapter;
- **ownership exclusion** — the external mod owns the feature, so the product Def/jobs/patches are
  absent (for example another mod owning meal temperature and microwave behavior);
- **unsupported safe fallback** — bounded warning/no-op without breaking core gameplay.

One external mod may require complementary strategies for distinct surfaces—for example XML ownership
for Defs plus a narrow runtime adapter for a job or graphic seam. Record those surfaces separately.
Never let XML, a public API, guarded reflection, and Harmony apply the same mutation twice.

## Design a bounded matrix

Do not test the Cartesian product and do not load every food mod together. Build groups by shared
engine seam and compatibility risk:

1. **base absence** — Harmony, Core, required libraries, and the product; Gateway is appended for automation;
2. **one owner/replacer** — mods that replace vanilla meals, temperature, dispensers, or graphics;
3. **service stack** — compatible waiter/guest/cleanup mods whose real interaction is the claim;
4. **production stack** — compatible recipe/bill/food-source mods sharing cooking or dispensing;
5. **storage stack** — refrigerators/storage graphics/temperature owners that can coexist;
6. **materials/graphics stack** — Stuff/category and visual-owner mods together;
7. **DLC/content stack** — relevant DLC and broad food catalogs;
8. **all-supported release matrix** — only mutually compatible packages, reserved for release or
   deliberate regression maintenance.

Separate mutually exclusive replacements and known-overlap owners. Use a small set-cover approach:
each supported adapter needs at least one exact group that exercises its unique seam, while one group
may cover several compatible adapters through one coherent native workflow.

Every attributed integration/E2E test declares its complete ordered non-Gateway package list; the runner appends Gateway for automation. This declaration convention does not require a separate run without Gateway. Do not
fake `LoadedModManager`, package presence, external Defs, or patched state in an ordinary unit test.

## Required evidence per integration

- **absent**: product loads and core behavior remains usable;
- **present-supported**: finalized shape/Def ownership plus a native player workflow visibly using the
  integration;
- **present-changed**: host fake or actually installed changed version proves fail-closed behavior and
  one bounded diagnostic; do not claim a live incompatible version unless it was actually run;
- **repeat initialization**: no duplicate Harmony owner/patch or duplicate gameplay effect;
- **save/load** when the adapter owns persistent state or external holders;
- **cleanup**: exact returned Things/state, config hashes, stage, credentials, and process.

An adapter must honor upstream eligibility and operation reports, not merely find a type. Recheck
native assignment/access/working/power/network/guest/prisoner/animal restrictions both when selecting
and when committing an operation. Validate declaring type, accessibility, parameters, and return type;
name-only reflection is not a supported shape.

## Conservation and upstream ownership

When Things move through external holders, dispensers, restaurants, storage, caravans, or save/load:

- count physical units with `stackCount`, not the number of Thing objects;
- retain exact fixture IDs when identity is part of the contract and assert each returned fixture has
  `stackCount == 1` before allowing stack merges to hide duplication;
- compare all owned per-instance state, not only the field needed by the happy path;
- preserve upstream data and public provenance while keeping intentionally hidden product provenance;
- do not return a plate from legacy/unplated or externally spawned meals that never contained one;
- let the external owner handle its feature when the contract says so; remove the product Def and
  feature-specific jobs/patches rather than running two systems.

## Matrix execution discipline

During a slice, run the smallest exact affected group. Mark completed tasks with their build/run
identity so later agents do not replay them. Run consolidated matrices only for release, explicit
regression work, or after a shared compatibility seam changes.
