# Weather-ready arrival design

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.

## Context

Steam/author documentation was read first, then the continuation's exact installed 1.7 assembly and
repository source. Provenance and coverage are in the
[source report](../../../mods/megamod/docs/replacements/dress-for-the-weather.md).
The source modifies neutral group spawning and optional raid generation, not every pawn spawn.

## Goals / Non-Goals

Goals: appropriate legal arrival gear, bounded work, meaningful settings and isolated optional hazard
support. Preserve native arrival workflows and generated gear; disclose balance effects.
Non-goals: perpetual outfit automation, absolute survival guarantees, introducing content, changing
raid composition, or retaining source defects as parity requirements.

## Decisions

1. Candidate selection uses effective loaded apparel/material contracts and the common settings
   resolver. Reuse native wearability, faction Stuff restrictions and conflict rules; do not copy
   the source's approximation using body-part/layer intersections if native rules are stronger.
2. Recompute actual comfort after accepted changes and stop once adequate. Source snapshots min/max
   before both loops and never refreshes them. Its outer limit is 100 attempts, but `getNewApparel`
   can loop forever when no sampled garment is wearable. Our implementation must bound every search,
   prefilter impossible candidates where practical, and make monotonic accepted improvements.
3. Source special gas-mask/veil/vacsuit provision precedes weather loops. Masks and vacsuits bypass
   the candidate filter; war veils come from `allApparelPairs`, which is candidate-filtered, and the
   spec preserves that distinction. All three special branches bypass the replacement filter.
   It can displace armor despite the ordinary replacement filter, does not check existing adequate
   protection first, and protects the children's suit inconsistently between heat/cold paths.
   Apply selected WF-Q1 survival-before-armor precedence, retaining already adequate and compatible
   gear. Do not remove better hazard protection for a nominally equal garment.
4. Correct replacement-filter initialization: source applies the utility exclusion to `apparelFilter`
   rather than the replacement filter. The spec explicitly protects utility apparel in both filters
   by default. This is an intentional safety/correctness deviation, not source-observed behavior.
5. Distinguish failures: no legal warming outfit is ordinary bounded fallback; missing compatibility
   prerequisites skip the affected integration at startup. Preparation should plan a valid set before
   committing replacement so a mid-operation failure does not strand a partially stripped pawn.
6. Optional content requires active package IDs and necessary Def/member shapes, not installed-DLC
   flags alone. Biotech `ludeon.rimworld.biotech`, Odyssey `ludeon.rimworld.odyssey`; resolve the
   spore-weather provider's identity before supporting `PoisonForestSpores`. Missing Defs never justify
   `MakeThing(null)` or a throwing lookup. No dynamic Def edits or XML patches are needed here.
7. Settings are genuine configuration: two inherited filter values and one boolean. No pawn-specific
   overrides or new bottom tab. Source global-only filter persistence and version helper are not copied.
   Shared definitions handle missing content and resets. No implicit original-settings import.
8. Later staged incident generation must call this owned preparation directly before publishing
   the complete group; do not retain a patch against our staged-generation feature. Share native
   candidate definitions where useful, not a second per-feature Thing index. Measure group-generation
   cost before claiming lag elimination.

## Patch target preparation

Potential native hooks: `IncidentWorker_NeutralGroup.SpawnPawns` and
`IncidentWorker_Raid.TryGenerateRaidInfo`. Each needs its own minimal semantic target document:
returned/generated pawn collection, map target, generation success, timing relative to visible
arrival and competing gear changes. A method-name match is insufficient if the list no longer
represents the group being introduced. Postfix original-skip behavior and repeated generation must
be accounted for. Optional hazard adapters form separate guarded units; rollback only owned work.
Original provider ID `Mlie.DressForTheWeather` triggers initial feature suppression with warning.
External APIs publicly exposed by the source are not copied absent an actual incoming consumer.

## Risks / Trade-offs

- Improved enemy gear affects protection and loot → ordinary raider temperature/pollution preparation remains off by default, while universal vacuum preparation remains active; later balance
  review compares actual generated groups, technology and acquisition outcomes.
- Generated gear may displace valuable items → apply ordinary replacement policy with the selected survival-protection exception,
  and verify no loose duplication or unintended removal of unrelated gear.
- Extreme weather may have no solution → retain explicit bounded fallback, never promise immunity.
- Race/material constraints vary → inspect actual supported consumers/provider runs; CE remains deferred.
- Mutation at the wrong arrival phase can show half-dressed groups → own completion ordering, including
  the later generation-smoothing capability.

## Migration Plan

No implementation or migration in this change. Future generated apparel is ordinary save content;
disabling the feature does not revert gear. Save overrides start inherited. Source settings import
would need a separate backed-up, atomic, recoverable contract. Adding/removing the enhancement is
not accepted as save-safe merely because the original author says so; verify native save workflows.

## Open Questions

- **WF-Q1 (resolved policy):** required survival protection takes precedence over protected armor;
  prefer all-hazard protection and retain vacuum protection if hazards cannot be jointly satisfied.
  Exact admitted provider outfit calculations and native gear preservation still require research.
- **WF-Q2 (research):** exact 1.7 incident coverage, factionless guests and third-party body/required
  apparel contracts; no claim to handle every quest guest or every spawning pathway yet.
- **WF-Q3 (integration):** canonical spore-weather provider identity and actual CE/other incoming
  consumers of the source's public utility/settings types. Author compatibility claim is only a lead.

Keep these in the refinement ledger; continue the next source instead of asking midway.


## Reconciled survival and player ownership

Separate ordinary visitor/optional-raider temperature preparation from universal humanlike vacuum
spawn admission. The latter covers newly generated and reused world NPCs, any faction/age, independently
of the raider toggle. Player-owned existing pawns never receive generated protective gifts: use only
real gear through ordinary availability/equip rules and retain actual exposure when insufficient.
Do not broaden this into a player outfit-manager or overwrite S130/manual policy exclusions.

Share finalized apparel facts with S130, but compute complete wearability and actual hazard resistance
through the current native/provider contract. Positive declared resistance is only policy eligibility.
Use package/Def/shape-guarded adapters for actual Odyssey and SOS2 map environments/protection; exact
SOS2 package/content/API and1.7 availability need inspection before implementation, not a guessed
compatibility claim. Keep those integrations independent where possible; missing one must not disable
native supported protection. Ordinary loot/gear effects of NPC provisioning remain explicit balance
work. S078 must complete protection before publishing the arrival or its notice.
