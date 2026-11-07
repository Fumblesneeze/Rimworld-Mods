# Shared Thing data

Status: specified, not implemented. The
[capability spec](../../../openspec/changes/add-megamod-foundation/specs/megamod-shared-thing-data/spec.md)
governs the first real shared-data slice. Inspect native map listers before adding an index.

The [implementation design](../../../openspec/changes/plan-megamod-parallel-implementation/design.md)
assigns shared candidates to C04/P1-A, physical quantities/claims to C05/P1-C and linked-storage
accounting to C06/P1-B. The [roadmap](../../../openspec/changes/plan-megamod-parallel-implementation/roadmap.md)
orders their prerequisite exports so search, hauling, storage, production and medicine use the same
facts without creating a storage–hauling dependency cycle. These are planned responsibilities, not
implemented services. The orchestrator coordinates contracts; each component has one writer.

For every proposed dataset, record:

| Decision | Required answer |
| --- | --- |
| Owner and consumers | Which map/game lifecycle owns it, and which real features share it? |
| Source | Can native data supply it? What additional materialization is demonstrably needed? |
| Keys and values | Map identity/generation, indexed fields, and valid Thing identity/lifetime. |
| Freshness | Which changes invalidate it, and at what exact boundary do queries see updates? |
| Updates | Incremental maintenance or one shared rebuild per invalid generation; no per-feature duplicates. |
| Derived views | Why any distinct filter/index needs separate storage, and its shared source. |
| Thread and teardown | Game-thread ownership, disposal on map/game removal, rebuild after load. |
| Fallback | Correct native query or unavailable feature if freshness/hook admission fails. |
| Performance | Query plus maintenance time, allocations, memory, workload and combined-consumer budget. |

Search and hauling may share candidate Things while keeping different query policies. A cached
candidate is not a reservation or promise of reachability. Revalidate pawn capability, forbidden and
access rules, ownership, reservations, reachability and provider eligibility before the native
action commits. Search visibility/freshness rules must also be explicit; do not infer them from hauling.

Test relevant spawn/despawn/destruction, movement, map/holder transfer, stack split/merge, indexed
metadata changes, save/load, map removal, competing pawns, and missing invalidation hooks. Every hook
needs its own patch target spec. Do not persist live references or share mutable Verse objects with
background workers. Performance claims require measured end-to-end workloads and native behavior
parity, not just fewer list allocations in a synthetic loop.
