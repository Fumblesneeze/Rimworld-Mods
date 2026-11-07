## Context

Sole owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
S135 is a documentation-only replacement of LM420.BCL. [Research](../../../mods/megamod/docs/refinement/2026-11-07-basic-caravan-loading-research.md)
contains the entire source patch, native intersections and hashes. There is no source C# assembly.

## Goals / Non-Goals

**Goals:** preserve two work-category changes with explicit player priority and cargo boundaries,
guarded XML and direct composition with owned carrier unloading.

**Non-goals:** configurable work taxonomy, new caravan batching, forcing all pawns to work,
changing party/lord duties, pod/shuttle reassignment, portal loading, migration or new caches.

## Decisions

Use XML, not runtime Def mutation or Harmony, to set only WorkGiverDef workType scalars for
HelpGatheringItemsForCaravan and UnloadCarriers to BasicWorker. Retain native classes, defNames,
priorities120/130, labels, required Manipulation and mech fields. Changing the work type intentionally
changes assignment/work-type eligibility; do not retain a blanket Hauling-disabled veto in the
owned S133 branch. This is a specific unload task permission, not permission for S125/general hauling.
S122 changes crafting-speed stats, not this Work-tab taxonomy.

Both scalar assignments are one admitted group. XML Extensions must confirm unique target Defs,
existing workType nodes, supported work-giver roles and BasicWorker target before either edit.
Inspect actual available XmlExtensions operations rather than guessing an API. Admit workType
Hauling or BasicWorker; BasicWorker is idempotent no-op. Unknown custom categories are an intentional
conflict boundary, not “every foreign patch is incompatible”. Preserve other values and all lists.
A scalar assignment cannot be meaningfully unioned; replacing this one field is intended, whereas
replacing WorkGiverDef, entire work lists or categories is not. Supply required XML Extensions
metadata/load ordering with the first implementation slice.

Create target documents BCL-HELPER-WORKTYPE and BCL-UNLOAD-WORKTYPE with executable checks and a
shared group preflight/rollback/postcondition. Account for later XML overrides using effective
finalized definitions: do not force-reset a later provider or claim success when it won. On an
unsupported group restore only owned edits where safe, leave foreign assignments intact, warn
per skipped enabled patch at startup. The implementation must establish a validated patch-order
strategy and atomic effective outcome; do not use runtime mutation to hide XML failures.
Hashes are provenance only. No whole-file/version/owner-absence runtime gate.

Original LM420.BCL is equivalent XML-only input; canonical package detection plus actual target
inspection admits no-op coexistence. A custom-work-category provider with a different assignment
is unsupported here, even if source author's recommendation says to load last. Optional external
XML targets, if later needed, require FindMod packageId=true and content/node guards; none selected.

Native helper NonScanJob scans gathering LordJob_FormAndSendCaravan, asks for a selected transferable
and a reachable usable carrier with allowColonists=false. Native JobDriver_PrepareCaravan_GatherItems
has separate participant inventory and helper carry paths. This reassignment leaves those paths,
existing durations/transfer accounting and caravan completion intact; it is not their bug-fix owner.
Native UnloadCarriers uses inventory.UnloadEverything and current native utility eligibility. S133
replaces the eligible physical batch directly while keeping this work authority. Share its existing
data/capacity/recovery; no new polling loop, Thing cache or duplicate job giver.

No new settings/save schema/UI text is selected. Existing native translated Work/caravan controls
remain; any added diagnostics or unavailable explanations use the common seven-language surface.
No main tab, settings toggle, debug tool or source-settings import.

## Risks / Trade-offs

- Basic can be higher priority and applies to a different set of workers → explicit intended effect;
  never rewrite player priorities or claim all previously hauling-capable mechs keep Basic eligibility.
- Unloading130 outranks assistance120 → retain source/native values and verify real competing work
  against flick500, release100 and open50. No guarantee of quickest caravan departure.
- A narrow helper carrier test can leave a colonist-only caravan without outside assistance → native
  behavior documented, not silently widened. Participants still have their own gathering duties.
- Source bare XML overrides load-order competitors → guarded effective assignment and warning instead.

## Migration Plan

No source settings or serialized types exist. No automatic migration or pawn-priority reset.
Existing native jobs/save references remain native; exact load/removal behavior still needs a fresh
native save workflow. Future migration beyond this scope needs backup, atomicity and rollback.

## Open Questions

BCL-Q1 source inventory resolved. BCL-Q2 effective atomic XML/provider ordering, BCL-Q3 Basic/mech
eligibility and native carrier edge cases, BCL-Q4 reviewed implementation and real workflows remain.
