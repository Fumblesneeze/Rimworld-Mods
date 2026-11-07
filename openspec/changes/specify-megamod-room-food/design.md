## Context

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S111 replaces Room Food by TheLordDrake, derived from Uuugggg. This is design for future implementation.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-room-food-research.md) retains complete
source inventory, identities and native call chains. Inspected game: **1.7.5000 rev1275**.

The selected 1.6 source patches `FoodUtility.SpawnedFoodSearchInnerScan` with a last-priority
postfix. It reruns a district-food search but does not preserve the supplied candidate set or the
inner scan's separate reachability check. Its source/installed agreement is semantic, not a claimed
reproducible build; the public tree has stale metadata and no compiled artifact or license file.

## Goals / Non-Goals

**Goals:** observable room-first map selection; actual source/action eligibility; direct composition;
minimal semantic admission; common settings; current native room/lister data; ordinary feeding fallback.

**Non-Goals:** new food/content, table assignment, automatic restocking, medical drug/hemogen policies,
inventory borrowing/refilling, container unloading, arbitrary foreign provider emulation, source
identity spoofing, source-settings migration or implementation/live acceptance in this change.

## Decisions

### 1. One contextual selection policy inside the owned food path

Integrate with the existing owned food-selection composition, not independent patches against our
Common Sense, preserved-food or borrowing code. Preserve the outer native sequence: early inventory
returns; map choice with room preference; native harvest handling and inventory comparison; native
remaining fallback; then S055 borrowing only after ordinary failure. Apply S024 spoilage and S057
preservation once in each actual comparison, never add another copy at the room-policy boundary.

Candidate inputs need getter/eater, map/root, original search set or an equivalently constrained
candidate query, path end mode, traversal/danger, maximum distance and native eligibility predicate.
Do not blindly port the source postfix: a method's existence does not prove these semantics remain.
Do not intercept explicit-item ingestion or ToInventory requests merely because they use food scores.
Diagnostic food-search UI is not an ordinary feeding request; leave it native unless its context is
explicitly supported without moving or reserving anything. Choose the narrow hook after current beta
inspection; avoid duplicate native scans or speculative broad caller rewrites for convenience.

### 2. Select an eligible room, then rank permitted food

Use current native Room identity for the whole room. `ProperRoom` is a candidate for the observable
enclosure boundary (not map-edge-connected and containing normal regions); verify actual doors,
enclosed courtyards and unusual maps before adoption. The source's `IsHuge` means more than60 native
regions, not a player-visible area limit; omit that arbitrary gameplay cutoff. Its single District
search is deliberately widened to the current Room. A roof or a specific room role is not required.

Self-feeding: enumerate native eating-surface buildings, require current permitted reachable access,
Danger.None at the table location as in the source, a non-prison enclosed room and at least one valid
food candidate. Choose shortest horizontal squared distance from getter, then a stable existing Thing
identity for an exact tie. The spec permits either equal-distance qualifying room; no RNG is required.
Use source-derived base radius100, range1–200, extended once to the original selected map source's
horizontal distance. Compare inclusively. This is not a hard cap on food-fetch distance: preserve the
calling action's food radius and metric independently. Patient branch uses recipient room directly,
without table discovery or this setting. Cross-map/null/despawned participants safely yield no preference.

Reuse the original candidate domain; never replace it with every district-listed FoodSource. Require
spawn/holder contract, validator, `CanReach` with the actual path end/traversal and correct request
distance. Native `SpawnedFoodSearchInnerScan` presently uses Manhattan root-food distance while source
`ClosestThing_Global` limits Euclidean eater-food distance; preserve the caller's limit instead of this
mismatch. Honor current foodDef finalization and real dispenser delivery/refilling semantics.

Ordinary food items use their actual location. Native/explicitly admitted dispensing sources use the
interaction/serving cell's room, not an arbitrary footprint cell inside a wall. Do not claim this alone
supports refrigerator-held contents or remote paste networks. Native spawned shelf/refrigerator food
can work through native contracts; a foreign container or delivery network requires its own inspected
package/Def/shape adapter and normal physical access workflow. No generic provider framework now.

Rooms with only invalid food do not qualify; continue through remaining finite candidates. Within a
room use the composed ordinary `FoodOptimality` with getter-distance and existing tie behavior. No
query reserves or mutates food. Actual native actions recheck eligibility; changed preferences alone
do not cancel ongoing valid jobs. A failed source must not cause a tight reselection loop.

### 3. Native datasets first; share facts without caching decisions

Use existing map/room/region listers and the current search's candidate set. Group room candidates
once per request if useful; avoid re-enumerating every room's food for every table. Do not introduce
a RoomFood-specific persistent food index. If measurement justifies derived data, use the shared
map-owned Thing index already intended for Ctrl+F/hauling, with explicit consumer fields and lifecycle.
No pawn eligibility, reservations, room winner or mutable food score survives as a cached answer.

Room/door/table changes, spawn/despawn, holder/stack transitions, policy, forbidden state, reservations,
eater movement and settings must be fresh for the request and use time. No off-thread Verse/Unity
access or persisted live references. Benchmark combined consumers plus update/memory cost; do not
claim improvement from source-code shape alone. Current map listers avoid a new periodic scan.

### 4. Common settings; no new navigation or settings migration

Define one integer setting `food.roomPreference.tableSearchDistance`, default100/range1–200, in the
Food category's room-preference section. Use the foundation's common load/store/effective-value/UI
path, dormant override and main-menu callback guards. Document the expansion rule in the translated
label/help. Invalid persisted values use100 with a bounded diagnostic; valid global/save values remain
independent. Changes apply next request without interrupting a valid job. No separate feature toggle
is invented from the source's always-on behavior; normal feature admission governs availability.

All seven baseline languages: English, German, Spanish, French, ChineseSimplified, Russian, Japanese.
No bottom tab, debug control, bottom-right toggle, per-pawn configuration or selected source art.
The source's global `maxDistanceToTable` file is neither read nor rewritten. No import is promised.

### 5. Minimal semantic target documents before code

Future patch documents use the [target template](../../../mods/megamod/docs/patch-targets/TEMPLATE.md)
and link RF01–RF08, actual implementation predicates/tests and eventual live evidence. Candidate
obligations are design hypotheses, not installed validators:

| Candidate boundary | Minimum semantics to establish | Failure boundary |
| --- | --- | --- |
| Ordinary spawned map-food choice | Getter/eater identity, source set, native eligibility, path/distance checks, result/foodDef relationship and actual ordinary request context | Room preference inactive; preserve incoming ordinary result |
| Food-score composition if a new hook is needed | One score per actual eater/source with unchanged context; correct S024/S057 contribution points and native outer ordering | Disable affected owned atomic group, never partially double-score |
| Optional source provider, only after concrete inspection | Canonical package/Defs/types, permitted enumeration, serving location, physical retrieval/consumption and no side effects during query | Adapter inactive; provider's ordinary path stays intact |
| Any necessary shared-index invalidation | Actual notification/lifecycle semantics and required current data | Revert to admitted native/current-request data or disable preference if truth cannot be preserved |

No unguarded PatchAll, exact-version/hash admission or blanket other-owner rejection. Account for
skip-original prefixes, result-changing postfixes and effective transpiled candidate/validator use.
If modifying an argument, validate its semantic use in the relevant branch, not just signature.
Validate each transpiler's actual incoming stream on every run; leave it unchanged on failed match.
Do not execute foreign transpilers repeatedly as probes. Document installation order and later repatch
invalidation/revalidation; original IL alone cannot establish effective behavior.

One idempotent registration path. False/unknown predicate, validator exception or installation failure
leaves that patch/declared atomic group inactive, rolling back only newly added owned patches. One
bounded startup warning per skipped enabled patch contains package/patch/spec ID, target, reason,
unavailable behavior and fallback. Missing unrequested optional content is quiet. A later invalidation
deactivates the affected owned path without repeatedly retrying or disabling other mods.

Active `TheLordDrake.RoomFood`, `Uuugggg.RoomFood` or the inspected development identity
`DEBUuugggg.RoomFood` suppresses owned room preference with one warning. No source unpatch/removal,
package spoof or assembly facade. Installed search found metadata/list references only, not a runtime
consumer of `Room_Food.FoodFinder`; this is bounded evidence, not proof none exists elsewhere.

### 6. Dependencies and implementation gates

Selection is dynamic pawn/job behavior, not a declarative Def change; C# is appropriate. Harmony
becomes a hard dependency with actual usage and is never bundled. No XML or XML Extensions operation
is selected. Any later XML integration needs its own package/Def/shape guard and merge-preserving
operation plus required XML Extensions metadata. No hard source/provider/DLC or Gateway dependency.

Pure rules belong in honest Unit tests; an owned minimal patch in Harmony tests; real food Defs,
loaded providers, jobs/rooms and native settings require isolated game workflows. No loaded-mod
simulation in host tests. Test positive and failed target predicates, compatible foreign changes,
candidate restrictions, nulls, exceptions, rollback, repeated startup and later repatches.

## Risks / Trade-offs

- Room priority intentionally beats a better external meal, including fresher food → make precedence
  visible in docs and measure choices alongside Common Sense and S057; retain actual food effects.
- Nearest table is geometric, not shortest travel time → preserve source intent and actual path
  eligibility; do not claim optimum routes or a guaranteed eating seat.
- Whole rooms and skipping unusable nearer rooms widen source coverage → document deliberate
  corrections and measure worst-case table/food requests without arbitrary hidden gameplay exclusions.
- Beta food/room/provider behavior can drift → semantic admission and new exact-build evidence,
  not reliance on source1.6 metadata or an author's untested compatibility comment.
- Source has no license in inspected installed/public tree → independently implement observed behavior;
  preserve attribution, do not infer permission to copy source, binary, icon or preview.

## Migration Plan

No runtime or settings migration in this draft. Future owned setting entries inherit current global
defaults when absent; no saved room/food/job component is introduced. Turning off/removing owned
room preference leaves ordinary food and jobs intact. Source configuration remains untouched.
Any later import must separately specify backup, atomicity, restoration and verified postconditions.

## Open Questions

- **RF-Q1:** Current beta effective hook/caller semantics, native room/access/serving-cell boundaries
  and executable target predicates, including later repatches and concrete provider contracts.
- **RF-Q2:** Final composed food-search plan and representative performance/food-choice measurements
  with S024/S055/S057/S123; avoid duplicate scans, preferences, capacity or quantity ownership.
- **RF-Q3:** Complete focused TDD, seven-language settings, original/optional-provider cases and
  reviewed-build native self-feeding/caregiving action-to-result acceptance. No user interview needed.
