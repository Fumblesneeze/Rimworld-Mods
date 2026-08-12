# Circinus facts and benchmark constraints

These facts came from read-only inspection of the installed RimWorld 1.6 package and are pinned in
`openspec/changes/add-rimworld-performance-benchmarking/design.md`. Reinspect and update the guarded
contract when the installed package changes; never assume an internal API is stable.

## Current inspected identity

- Package ID: `astryl.Circinus`.
- Assembly: `Circinus.dll`; run-document schema `1.15` in the inspected build.
- The exact product build, MVID, length, SHA-256, declaring types, accessibility, parameter/return
  shapes, settings fields, and prompt versions live in the OpenSpec design. Use those facts rather
  than name-only reflection.
- Circinus requires Harmony and loads after it. Canonical repository groups still declare Harmony,
  Core, Circinus, product/optional packages, Gateway in exact order.

## Native sampling and retention limits

These are Circinus limits, not repository-chosen HTTP budgets:

- roughly 60 recorded frames followed by 240 skipped frames;
- adaptive timing above 64 calls per recorded frame, maximum sample shift `8`;
- 2000-recorded-frame profiler ring;
- automatic shedding below the native floor unless a method is hand-armed;
- 3000 patch-row and 1000 method-row stopped-document detail caps;
- 7200-sample run ceiling and 30-sample partial flush behavior.

Keep the native duty/adaptive policy intact. Split long samples into declared compatible windows;
never accept a silently truncated tail.

## Adapter surface

Guard exact supported shapes for:

- `RunRecorder` start/marker/stop/current document;
- `Instrumenter` arm/disarm and patched-state queries;
- `ProfilerRegistry` enabled/recording flags, counters, lookup, and reset;
- `DevProfiler` method identity, hand-armed state, sample shift, calls/timed calls, emptiness, cycles;
- `TargetCatalogue` and `ProfileTarget` identity/category/method count;
- `RunDocument` identity/schema and its own JSON writer;
- `CircinusSettings` plus consent/auto-profile prompt versions.

Use Circinus' own persisted JSON. Its temp/delete/move persistence sequence is not an atomic replace;
retain both the stopped in-memory JSON and isolated persisted JSON and require matching run/schema
identity.

## Sidecar correlation

Circinus' ordinary method JSON omits some live adaptive/timed-call fields. Snapshot each exact
run-owned `DevProfiler` immediately before stop/disarm. Correlate every non-empty sidecar by exact
method identity to one patch or method row. Retain empty/uninvoked profilers with
`noRowReason=empty-or-uninvoked` because Circinus intentionally omits them.

Reject non-empty missing rows, empty rows that unexpectedly materialize, ambiguous/duplicate matches,
and unexplained trimming. Preserve calls, timed calls, sample shift, cycles, gross/shared/skip-capable
cost, sampled state, duty, dropped detail, TPS/FPS/frame/tick/heap series, and workload checkpoints
without rewriting their meaning.

## Control modes

Run each compatible workload in fresh processes:

1. **instrumented** — run-owned wrappers armed, registry enabled, recording active;
2. **armed-disabled** — same wrappers remain, registry/recording disabled during the sample;
3. **fully disarmed** — run-owned wrappers removed while the same recorder/system sampling remains.

Keep these controls separate from an optional product-absent workload. A product-absent fixture must
be owned/staged by a non-product test owner, contain no product reference, and preserve workload
meaning and throughput checkpoints.

## Current repository status rule

Always read `tasks.md`. The Circinus inspection/design may be complete while the shared contract,
adapter, runner, fixtures, live calibration, and documentation/regression gates remain incomplete.
Until the relevant tasks and command help exist, use this reference for implementation and review,
not as evidence that a runnable benchmark pipeline already exists.
