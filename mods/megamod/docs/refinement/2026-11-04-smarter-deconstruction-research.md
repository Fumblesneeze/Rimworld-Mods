# S061 — Smarter Deconstruction and Mining (Continued) research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Inspected **2026-11-04**, sequentially after S060. This is source/native analysis, not reproduced
gameplay or a claim of compatibility. No source build, gameplay implementation or game launch.
The [proposal](../../../../openspec/changes/specify-megamod-roof-safe-removal/proposal.md),
[design](../../../../openspec/changes/specify-megamod-roof-safe-removal/design.md) and
[observable spec](../../../../openspec/changes/specify-megamod-roof-safe-removal/specs/megamod-roof-safe-removal/spec.md)
separate intent, mechanisms and selected behavior.

## Documentation first and exact provenance

Read [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3261302741),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3261302741) and
[public repository](https://github.com/emipa606/SmarterDeconstructionAndMining) before source analysis.
The description advertises automatic roof preparation for mining/deconstruction, mountain-roof
options, a research choice and forced bypass of cancellation. It says no manual mountain drawing
tool is added. Its old automatic thick-option claim conflicts with the changelog's removal of that
automation in 1.5.3. The changelog also records colony-mech coverage, a research-error fix and 1.7.0.
Neither public compatibility/performance claims nor reported comments establish acceptance here.
The original Workshop 2398365712 page returned HTTP 429; original identity is corroborated locally,
not by a successful read of that page. Generic Steam warning banners are not compatibility evidence.

| Input | Inspected identity |
| --- | --- |
| Current source | `Mlie.SmarterDeconstructionAndMining`, Workshop `3261302741`, author Mlie; About advertises 1.2–1.7 and Harmony dependency |
| Original | `legodude17.smartdecon`, Workshop `2398365712`, legodude17; retained Harmony owner and source About metadata |
| Installed selection | `F:/Steam/steamapps/workshop/content/294100/3261302741/1.7/Assemblies/SmartDeconstruct.dll`; no LoadFolders.xml found |
| Version | Manifest/file 1.7.0; assembly 1.7.0.0; informational `1.0.0+75110e9f33056ea11826f3765a1c2e09cdb7586b` |
| Public revision | `12ee01b9689986523e8363e896205592cae18120`; bundled 1.7 DLL matches installed, not proof of a fresh build from that revision |
| Selected DLL SHA256 | `251C8222877678428E4C9AC8E3D4316A8655904F4403137B34AA5E34FDD629F8` |
| About.xml SHA256 | `3678FA9C71528C8F90E96480E0D6569ED0A314A7648FF104C2B52F98A9C9EBA5` |
| Native game | 1.7.4998 rev1299; `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| License | Public MIT, copyright 2020 Mlie; assembly attributes credit legodude17, 2026, MIT. Retain applicable notices for any future reuse; none is copied here |

Ignored local evidence is under
`artifacts/Refinement/megamod-spec-20261104/smarter-deconstruction/`: `upstream`, `compiled`,
`native`, `consumer-above-below` and `consumer-hits.txt`. These inspection artifacts are not shipped.
All four runtime source files and all four reconstructed behavior types were read: Main, JobInfo,
SmartDeconSettings and SmartDeconstructMod, plus metadata and English settings/alerts. The package
has five language folders and no gameplay Def/Patch XML. No artwork was selected or copied.
The public project targets net10.0 with 1.7.4990 unstable reference/Harmony 3 rc3 and a broad process
termination build target; it was only inspected, never built or executed. Upstream policy files do
not govern this repository.

## Installed behavior recovered from code

### Configuration and UI

The global mod-settings window has six checkboxes followed by searchable research selection, a
selected highlighted row and version text. Available project labels are sorted alphabetically;
clicking the selected project clears it. There is no bottom tab, gizmo, operational corner button,
manual mountain designation or debug-bar action. Every source preference is global only.

| Setting | Source field / load default | Consequence |
| --- | --- | --- |
| Buildings | true / true | All admitted `JobDriver_RemoveBuilding` instances, including native uninstall |
| Mining | true / true | `JobDriver_Mine`, including its native mine-vein path |
| CancelThick | true / true | Automatic unavailable-mountain branch |
| NonColonists | false / false | Required for workers that are neither colonists nor colony mechs |
| Animals | false / false | Additional `AnimalOrWildMan` gate after the previous test; player animals actually require both options |
| Thick | false field / true missing serialized value | Fresh settings object and loaded missing-key settings differ |
| ThickResearch | null/string project lookup | Null/missing lookup permits thick removal when Thick=true |

Native `Mod.GetSettings` delegates to `LoadedModManager.ReadModSettings`: no settings file returns
a new settings object, confirming fresh Thick=false. An existing serialized settings object missing
Thick receives the declared true load default. No live settings file was mutated or used as proof.

`ThickResearch` resolves a stored string with GetNamedSilentFail. `CanThick` is Thick plus either
resolved research finished or null. A removed project can therefore act as no requirement.
Selecting null sets `explicitNull`; selecting a non-null project does not reset it. `SetDefaults`
finds the first available `RTR_OverheadMountainRemoval`, `OverheadMountainRemoval`, then
`ThickStoneRoofRemoval`, with no package gate; it does not enable Thick. Main's static startup calls
SetDefaults after settings construction. ExposeData can later replace unresolved/non-explicit-null
research with the candidate. This serialization-timed behavior is not a stable user-intent model.
A bounded installed XML search found none of those three project definitions; no optional research
provider is claimed inspected or supported merely from the source's names.

### Planning, jobs and mutation

The constructor uses Harmony owner `legodude17.smartdecon` to patch RemoveBuilding.MakeNewToils and
Mine.MakeNewToils with `CheckForRoofsBeforeJob`. No source admission validator. The replacement
enumerator prepends an instant action and attaches a cleanup action to the original last toil.
JobInfo records only Def, playerForced and A/B/C. It does not retain arbitrary queue/count/provider
fields or removal-driver work.

The action returns for a target that does not hold roof. Otherwise it flood-fills roof within 13.8
cells of target origin or target footprint, collecting roof within 6.9. It collects other roof
supports touching visited cells, excluding supports with the **same selected designation type**.
From each support's origin it subtracts connected roof within 6.9. Deconstruction is inferred from
the shared RemoveBuilding base and selects Deconstruct, even for native Uninstall. Mining selects
Mine, not the native MineVein designation. Complete supporter footprints and mixed future removal
types are not represented by that same-type/origin subtraction.

If required roof includes thick roof, CanThick=false, CancelThick=true and the job is not forced,
the source can notify. Only a player-faction worker removes the found local designation and ends
its job; a nonplayer worker returns from this branch without stopping the original work. The
uninstall/vein designation mismatch can fail to remove the actual order. These are code-derived
failure leads, not reproduced bug reports.

Otherwise remove disallowed thick roof and cells the worker cannot reach at ClosestTouch/Danger.Some
from the plan. Sort the rest farthest from target first. Construct native RemoveRoof jobs and retain
only those whose pre-toil reservations succeed. If none remain, ordinary original work continues.
Thus unreachable/reservation-failed required cells can be omitted without proving final safety.
With accepted jobs, end the original, mark NoRoof, suppress BuildRoof, queue the roof jobs and a new
original job containing only the captured fields. `playerForced` is copied to those jobs. A forced
job bypasses mountain cancellation only; it still attempts removable-roof preparation. It does not
make CanThick true. Resumed jobs can repeat the start check, but there is no final-mutation recheck.

### Temporary area cleanup

Two static `List<IntVec3>` fields track added NoRoof and suppressed BuildRoof cells, without map,
operation or serialization ownership. Last-toil cleanup flood-fills connected tracked cells from
the target. If it finds another neighboring roof support marked Deconstruct near a remembered
BuildRoof cell, cleanup returns and retains the area; it does not generally model other related
mining/uninstall work. Otherwise it clears tracked NoRoof and restores remembered BuildRoof.
Original NoRoof=true plus BuildRoof=true can enter the clearable list via the BuildRoof branch.

Cross-map coordinate reuse, overlapping ownership, reload, interruption before the final toil and
later player edits are not durably represented. Restoring BuildRoof is only an area order; no source
code directly recreates the removed physical roof. The selected draft corrects ownership rather
than copying these static lists.

## Exact native comparisons

Read RemoveBuilding, Deconstruct, DeconstructForBlueprint, Uninstall, Mine, RemoveRoof,
AffectRoof, WorkGiver_RemoveRoof, RoofCollapseCellsFinder, RoofCollapseUtility, Mod and
LoadedModManager from the inspected binary. Review additionally inspected the relevant GenConstruct
blocker-removal path and construction-delivery workgivers for blueprints and frames.

- RemoveBuilding reserves the target, checks designation/forbidden/explosive state, walks, performs
  ConstructionSpeed * 1.7 work, then notifies/removes and clears designations. Work fields are saved.
  Deconstruct destroys with normal returns; Uninstall uninstalls; DeconstructForBlueprint has no
  required designation. Target support removal is later than MakeNewToils enumeration.
- Mine respects Mine or MineVein for ordinary work and destroys the rock on its final damage step,
  with ordinary results and vein progression. Its HP is physical work progress.
- Current GenConstruct.HandleBlockingThingJob creates ordinary Deconstruct or Mine with
  ignoreDesignations=true for construction blockers; blueprint/frame delivery workgivers can request
  those jobs again while the parent remains. The retained DeconstructForBlueprint class alone is
  not a complete account of this creation path. Removing a nonexistent designation cannot stop
  automatic recreation. The draft therefore retains an exact parent/blocker hold across reload,
  with changed-condition reevaluation, explicit Retry and cancellation that preserves resources.
- AffectRoof reserves a ceiling cell, moves to target B and performs 65 work at ConstructionSpeed *
  1.7 per game tick. RemoveRoof requires NoRoof, removes the actual roof and invokes native connected
  removal effects. Directly constructing this job is not proof the actor is eligible for roof work.
- WorkGiver_RemoveRoof scans actual NoRoof cells, checks roof presence and ceiling reservation.
  Native roof work must remain subject to the effective owned permission when another worker takes
  an owned temporary order; a residual area mark is not sufficient authorization.
- RoofCollapseUtility uses 6.9 horizontal distance, cardinal roof connections and actual edifice
  occupancy. RoofCollapseCellsFinder also follows disconnected roof components and can propagate
  beyond the source's collection region. Its helpers mutate collapse/roof state and must not be
  called as a supposedly read-only planning query.

These native reads inform candidate contracts, not proof that future effective patched code retains
them. No diagnostic or simulated roof mutation was performed in a running game.

## Actual installed consumer and contract boundary

Bounded ASCII and UTF16 DLL/source/metadata search found the source versions, a replacement map,
layout-harness mod lists and As above, So below II. The mapping corroborates original/current IDs;
test lists are not runtime consumers. No universal absence-of-consumers claim follows from this scan.

Selected consumer: `astryl.AsAboveSoBelow2`, Workshop 3776015553, 1.7 AsAboveSoBelow.dll SHA256
`B8BD33CB09115E623B5C2C385BFFC8BF39543BCDE0996086D80BBAC1B507CB99`.
Read SmarterDeconstructionCompat, Patch_SmartDecon_ABScopeRoofScan and
Patch_FloodFiller_ABSmartDeconBand including their reconstructed nested closures.
It resolves `SmartDeconstruct.SmartDeconstructMod`, finds
`CheckForRoofsBeforeJob(IEnumerable<Toil>, JobDriver) -> IEnumerable<Toil>`, remembers its assembly,
wraps only init delegates belonging to that assembly and scopes native FloodFiller calls. Within
that scope it clamps cells to the root's logical band and excludes gutters. Without the source type
its patches prepare false. Its source-algorithm/delegate assumptions are not satisfied by a mere
method facade or by using a different owned analysis path.

Selected approach: record that binding as unsupported, do not spoof identities, and implement an
admitted direct level-geometry adapter when its concrete API contract is inspected. Share the
applicable provider boundary with S060. Separate provider-present native workflows remain required.

## Selected deviations, composition and remaining work

The observable draft preserves real roof work, default building/mining protection, optional thick
removal, research choice and deliberate forced mountain risk. It adds current connected/final checks,
proper MineVein/uninstall handling, independent worker options, exact continuation ownership and
map/save/player-edit-safe temporary intent. Mountain removal is consistently off by default;
research initially None is explicit, missing selected research blocks permission, and automatic
research-name guessing is omitted. These choices are proposals under DM-Q1, not claims that the
user previously approved each source departure.

Direct composition: S008 selection/ordinary designations, S013 cycle budget, S028 physical replacement
transaction and S060 covered completion checks stay authoritative. Continuous real roof support
does not need preparation just because object identity changes. Shared data remains native-first.
No optional research provider, external facade, migration, XML extension, multiplayer certification,
new tab or performance claim is delivered. All runtime tasks remain unchecked.

| Question | Remaining decision/proof |
| --- | --- |
| DM-Q1 | Review worker-option clarification, explicit None default, omitted auto-detection and provisional mountain labor/progression |
| DM-Q2 | Effective mutation/continuation/area-edit contracts, connected consequences and exact bounded scheduling |
| DM-Q3 | Canonical optional level/roof/worker providers and directly shared geometry adapter; source internal binding remains unsupported |
| DM-Q4 | Honest focused tests, independent implementation review, measured cost/balance and causal native player acceptance |
