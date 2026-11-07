# S060 — Smarter Construction research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S060/P1-54. Inspected 2026-11-04. Source/native/consumer research and observable
draft complete; independent source/composition reviews and strict validation passed. Nothing
implemented or accepted in game.

## Documentation and pinned inputs

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2202185773),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2202185773), installed
metadata and [repository README](https://github.com/dhultgren/rimworld-smarter-construction) before
the source. The author describes preventing automatic construction from trapping pawns or unfinished
work, forced right-click construction as a bypass, installation and custom replacement limitations,
and a bounded small-area check. The changelog records increasing its limit from 50 to 100 in 2024;
current metadata and the October 2026 author update advertise 1.7. Public compatibility/performance
and safe-removal statements are source claims, not evidence for our implementation. A comment also
mentions delivery ordering; selected code only changes finishing-frame selection.

Installed root `F:/Steam/steamapps/workshop/content/294100/2202185773`; package
`dhultgren.smarterconstruction`, author dhultgren/Hultis, assembly/file **1.7.0.0**. LoadFolders
selects root plus `v1.7`. Harmony is the only declared required dependency. No Def/XML patches,
player translation/UI assets, settings interface or saved custom gameplay fields are supplied;
About preview and debugging strings do not imply a player control.

| Input | SHA256 or revision |
| --- | --- |
| Installed v1.7 SmarterConstruction.dll | `8005E094B69EE22E6D642898CC4B8F4EEC3EDBB0829BBDCB0093367702BCB9D4` |
| About/About.xml | `C79E234B6F7FB7E010A2822AAF8B8C315EB717FA52D4D216F996AA07ED0DEDC9` |
| LoadFolders.xml | `A3E9C91D5529DC5BB13454D4EE5EC8253914FC2E571ABBD0B7FB96C6900AA9C6` |
| Public checkout | `c9953f1cc122cc7c0227466224e411d7805754bd` |
| Native 1.7.4998 rev1299 Assembly-CSharp | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| MultiFloors 1.6 SmarterConstruction adapter | `6E86F64A26203424459DBD7B3F9EC15D5F369D1BFBEE0A8240A679B84A5D88FC` |
| Vehicle Map Framework 1.6 | `98C07524A064AB10FEA7E82547B06E75F41CAD0D9785911E1C5251F9DED9973D` |
| As above, So below II 1.7 | `B8BD33CB09115E623B5C2C385BFFC8BF39543BCDE0996086D80BBAC1B507CB99` |

Evidence root: ignored `artifacts/Refinement/megamod-spec-20261104/smarter-construction/`, with
`upstream`, `compiled`, `native`, `consumer-multifloors`, `consumer-vehicle-all`,
`consumer-vehicle-resolved`, `consumer-above-below` and `consumer-hits.txt`.
Read all 14 current runtime C# source files plus assembly metadata, the source's test file, and all
19 selected reconstructed behavior types plus assembly metadata. Inspected behavior agrees between
current source and installed binary; no upstream build or test run was performed. Project targets
net472 and references Harmony 3.0.0-rc.2; do not execute its machine-specific build output paths.

No LICENSE/COPYING file was found in the inspected public tree or installed file inventory. Assembly
copyright says 2026 and dhultgren is the trademark. Public source availability is not a reuse grant;
this is independent behavior engineering, with no source code/test/art/binary copied into product.

## Complete selected implementation

| Source area | Actual behavior |
| --- | --- |
| SmarterConstruction / Compatibility | Startup creates a plain readonly settings object, calls an empty compatibility initializer and unconditionally PatchAll with owner `SmarterConstruction` |
| Settings / debug logging | LogLevel None; FinishCacheTicks 0; GetJobCacheTicks 20; throttled cache age 600; MaxRegionSize 100; MaxDistanceForPriority 15; EnableCaching true. No ModSettings, player editor or persistence |
| NeighborCounter | Full rotated Thing footprint; eight-neighbor exterior set for blocked-neighbor count, four cardinal neighbors for region/safe-position traversal; deduplicated HashSets |
| WalkabilityHandler | MapComponent reads `map.pathing.Normal.pathGrid.Walkable`; subscribes BuildingSpawned at FinalizeInit and recalculates only a non-frame building's origin cell to reduce delayed path updates |
| ClosedRegionDetector | Hypothetically blocks footprint and flood-fills adjacent cardinal regions, stopping at 100; unions nonempty regions smaller than 100. A nonwalkable neighbor with no walkable cardinal exit becomes a singleton region |
| EncloseThingsResult | EnclosesRegion for any returned region; EnclosesThings for any Blueprint/Frame or any player-faction pawn other than actor; EnclosesSelf for the acting player pawn. Loose items and foreign pawns do not trigger the veto |
| EncloseThingsCache | Map-local target-only dictionary with result/time; returns if cachedTick + requestedAge > currentTick; zero age forces fresh detection. Stores builder-relative flags without builder identity; no general topology/occupant invalidation or cleanup except later lookup |
| WorkGiver_ConstructFinishFrames_JobOnThing | For any non-null unforced result and impassable finished Def, run enclosure; veto only EnclosesThings, allow self to start. First 20 checks per game tick use age 20, later checks age 600; a cache miss still computes. Counter is shared across maps |
| PatchMakeFinishFramesPrioritized | Forces finish-frame scanners into prioritized JobGiver_Work search and substitutes CustomGenClosest only on that path; locates instructions by text/offsets without semantic admission |
| CustomGenClosest | Validate candidates, calculate squared horizontal distance; prioritize impassable-result targets within inclusive 15 cells, then supplied priority descending, then distance; test actual reach in that order. Other targets still sort by supplied priority. Optional haul-source flag is unused |
| PriorityPostfix | Only finish-frame scanner, nonnegative original result, impassable result within 15, and walkable target origin; add count of blocked surrounding cells. Cache is static keyed only by cell, with 3000 ticks plus random -300..299 expiry. Completion invalidates neighboring coordinates |
| Completion transpiler | Selects closure name containing DisplayClass8_0 and b__1; uses fixed locals and the CompleteConstruction call. Sets workDone to workToBuild and calls EndJobIfEnclosing; an early branch skips completion and ReadyForNextToil together |
| EndJobIfEnclosing | Skip nonimpassable and playerForced. Freshly reject other protected contents; for actor at origin or EnclosesSelf, get cardinal walkable positions outside small regions. No safe positions ends incompletable. Otherwise first HashSet cell is used, enqueue a new same-job-def/target finish job, start Goto with ignoreForbidden=true; no pawn-specific validation of that chosen cell |
| Patch_PawnsGettingStuck / PawnPositionCache | Every 50 game ticks during all finish-driver PatherArrival toils, compare pawn position; 300 unchanged ticks fail the toil and remove the entry. Cache is per map/pawn, not journey. Cleanup every >2000 ticks drops entries whose deadline is >2000 ticks old. No door/transit distinction or forced-job exemption |

No custom XML membership, recipes, work amounts, building stats, lower-bar buttons or player settings
are present. Automatic material delivery is not a patched work type in this selected implementation.
Public source tests use mocked walkability and do not prove native movement. At least one safe-spot
test expects seven surrounding positions after blocking one neighbor, whereas the current cardinal
implementation yields three. The tests are therefore not an authoritative current contract and were
not run or copied.

## Static risks and deliberate correction boundaries

- Static priority keys omit map and footprint; equal coordinates on another map can reuse a score.
  Occupant-result caching omits actor identity, though self versus other pawn depends on the actor.
  Short/long TTLs are not correctness proof. Completion is fresh, but selection can be wrong/stale.
- A full-footprint result can affect more cells than the origin-only path refresh. Current native
  PathGrid.Walkable is bounds-safe, but the special unwalkable-neighbor branch can produce an
  out-of-map singleton; subsequent GetThingList requires explicit bounds treatment in owned logic.
- Safe geometry is not permission/reachability. First HashSet position can be unreachable and
  ignoreForbidden overrides ordinary permission. Source actor-at-origin check does not explicitly
  cover every cell of a multi-cell footprint.
- Target JobOnThing can return a preparatory blocking-item job. Source applies its veto to any
  non-null result rather than only actual completion work; owned rules must preserve preparation.
- A fresh return job copies only job Def and target. Changes, cancellation and save/resume need a
  single owned continuation contract. Full-work retries must not repeatedly award skill, roll
  construction failures or enqueue inaccessible movement. These are static risks, not reproduced bugs.
- Same-cell travel timeout confuses legitimate waiting/transfer with failure and can span distinct
  jobs. Owned progress checks need active journey identity and bounded nonrecursive recovery.
- The bounded after-footprint model is not a general before/after reachability proof. Regions of
  100 or more cells are treated as open for this purpose; cardinal topology can be conservative.
  Empty pockets can close, and foreign pawns/loose goods are deliberately not protected by this rule.

## Native 1.7 comparisons

Read complete WorkGiver_ConstructFinishFrames and JobDriver_ConstructFinishFrame, the completion/
failure portion of Frame, relevant prioritized selection in JobGiver_Work and PathGrid.Walkable.
The native giver requires matching faction, supplied frame, valid touching access and CanConstruct;
it can return a clearing job for FirstBlockingThing before FinishFrame. Native MaxPathDanger is
Deadly and eligibility remains native, not invented safety permission.

The driver reserves the target, uses GotoBuild or attachment OnCell, grants ordinary construction
experience while working, applies ConstructionSpeed/Stuff speed and failure probability, increments
workDone, then invokes CompleteConstruction and advances the toil. Native completion first sends a
quest signal, destroys actual contained resources and the frame, creates the building or terrain,
generates quality/style, preserves relevant storage state and records completion. Owned deferral
must precede those irreversible consequences, not compensate after creating a result.

Native job selection retains WorkGiver priority, candidate eligibility, reach, danger and ordinary
distance. S058's structure > terrain > spike-trap categories are the direct composition baseline;
the added local constrained-frame order belongs within a category. S013 quality work retains its
worker/cycle policy. S028 owned replacement constructed from resources must use the common decision
where it adds blocking footprint, preserving its distinct physical replacement transaction rather
than inheriting source's documented custom-replacement gap. Minified install-over remains outside
this feature's explicit scope.

## Actual external consumers

Bounded case-insensitive ASCII and UTF16LE scan of installed Workshop DLL/C#/XML/JSON found source
versions, catalog/test mod lists and three executable integration families. No universal absence
claim is made. Inspected only the relevant consumer classes, not every implementation of each mod.

1. **MultiFloors**, `telardo.MultiFloors`, Workshop 3384660931: its 1.5/1.6 LoadFolders include a
   SmarterConstruction assembly adapter only if `dhultgren.smarterconstruction` is active. The 1.6
   adapter strongly references `SmarterConstruction.Core.ClosedRegionDetector.WouldEncloseThings`
   and `EncloseThingsResult`, skipping the check and returning all flags false on nonzero floor or
   null pawn map. No owned package/assembly would satisfy that binding automatically. Metadata stops
   at 1.6; this sample is contract evidence, not 1.7 support.
2. **Vehicle Map Framework**, `OELS.VehicleMapFramework`, Workshop 3426502333: the selected 1.6
   ModCompat flag requires `dhultgren.smarterconstruction`. Its category patches string-named
   `SmarterConstruction.Core.WalkabilityHandler.Walkable` and
   `SmarterConstruction.Patches.Patch_WorkGiver_Scanner_GetPriority.PriorityPostfix`, plus inner
   `CustomGenClosest` methods whose body reads Thing.Position. It treats vehicle structures/outside
   coordinates as walkable for source geometry and rewrites priority/finder coordinates onto the base
   map. Its general JobGiver_Work patch orders after Harmony owner `SmarterConstruction`. Read the
   relevant gate, three patch bodies and ordering attribute; not the entire unrelated job rewrite.
3. **As above, So below II**, `astryl.AsAboveSoBelow2`, Workshop 3776015553: read all seven selected
   1.7 bridge/patch classes. Type-name resolution consumes detector WouldEncloseThings(Thing,Pawn,...),
   FindSafeConstructionSpots(Thing), ClosedRegionCreatedByAddingImpassable(...,HashSet<IntVec3>), the
   nine-argument custom finder and compiler-generated projections, and PawnPositionCache.IsPawnStuck.
   Nested scoped prefix/finalizers remove returned pockets with a link anchor within two cells,
   rewrite distance through cross-level travel, and suppress a stuck verdict during pending transit.
   This is concrete compatibility work but does not itself prove an anchor is an exit usable by each
   protected pawn or establish our compatibility.

No narrow stable external command contract was found here: these consumers modify implementation
internals, including anonymous methods and strongly bound types. The owned design documents those
unsatisfied bindings, avoids package/assembly/compiler-shape impersonation, and selects direct narrow
geometry/distance/transit adapters only after exact provider admission. Provider-free play does not
require them; unsupported geometry gets an honest ordinary-work fallback. No automatic multiplayer
claim follows from source use of native jobs.

## Selected draft and remaining work

Owned [change](../../../../openspec/changes/specify-megamod-construction-access/proposal.md) retains
small-space protection, local ordering and deliberate forced enclosure, with permitted physical
relocation, current map/worker facts, explicit limits, preserved completion state and bounded journey
recovery. S058's order is clarified directly. No separate Thing cache, settings framework, source
facade, source data migration or runtime code is introduced. Policy numbers remain named constants;
exposing controls later requires common global/save settings.

| ID | Open work | Draft position |
| --- | --- | --- |
| CT-Q1 | Scope and configuration | Explicit <100-cell regions, inclusive 15-cell order, protected occupants and cardinal geometry retained; possible controls or broader protection require deliberate specification |
| CT-Q2 | Native completion and recovery | Actual effective boundary, full-work wait without XP/failure reruns, one permitted continuation, correct forced semantics and genuine-stall detection before implementation |
| CT-Q3 | Provider and consumed contracts | Three real consumer families, unsatisfied source bindings and narrow owned alternatives; exact beta geometry/exits/transit and provider-present acceptance remain open |
| CT-Q4 | Composition, balance and acceptance | S058/S013/S028 direct composition, focused RED/GREEN, shared-data cost and actual build/move/wait/force/reload workflows on reviewed builds |

Semantic pass compares ordinary frame finishing, source local ordering and S058 category ordering.
The draft changes scheduling/delay, not material economy, work amounts, quality generation or research.
Avoiding a trap can increase walking; size limits leave unprotected cases and category order can
delay floors/traps. Actual throughput, geometry correctness, concurrent builders and provider behavior
need live proof. No tests, build/install, game launch, normal user save/configuration or Workshop edits.
