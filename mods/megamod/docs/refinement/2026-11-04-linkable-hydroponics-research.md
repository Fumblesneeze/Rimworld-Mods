# S076 — Linkable Hydroponics research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld1.7 beta**.
Read-only inspection **2026-11-04**. Source/native/provider research and an observable draft now
exist and passed independent review plus typed strict validation. No runtime implementation/acceptance.
S006 linked growing zones is an existing separate owned contract.

## Documentation first and identity

Attempted the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3535816519)
first; the web tool returned HTTP429 and a bounded exact-title/ID search returned no result. Read
the installed author's About description before code. It describes selecting/linking basins, changing
one crop to update the group, orange corner markers, merging/unlinking and saved groups. The brief
description does not settle initial synchronization or compatibility details; those are traced below.

Installed **Linkable Hydroponics**, author **Yakobo**, package **Yakobo.LinkableHydroponics**,
Workshop **3535816519**. About supports1.6 only and requires Harmony; loadAfter names optional
VanillaExpanded.VFEFarming. No LoadFolders/version route is supplied. Root Assemblies/hydro.dll is
assembly/file1.0.0.0, net472, title/product HydroponicsLinking, copyright2025. This does not establish
1.7 compatibility. The initial inventory missed installed Readme.md; independent review found it
and the host then read it fully. It corroborates linking, group controls, overlays and save/load,
without adding a license declaration. No license found; do not infer reuse rights. No source/assets
copied into product.

Installed root `F:/Steam/steamapps/workshop/content/294100/3535816519`.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `67A13F430C7F437BE8E0AE0E51CEDDBCA6EC84686AD7D5DF0091918664654DE2` |
| Assemblies/hydro.dll | `90FB50A7874D95DF1553C5E8DFF35FAC49C1774E2313A254A4D51D98975E2CC4` |
| Patches/HydroponicsBasin_Patch.xml | `04FA7018A4483C9422E9F2F6CEF80B7D8E83C0F20A75AEFA2EF37493B35BFF14` |
| Patches/VFEF_Compatibility_Patch.xml | `1AAA9B310B3721EF9DC873C867FBE2F278D272D1D3866D1E1276E7AA036CD70A` |

Read all ten supplied C# files and both XML patches, then all selected compiled runtime classes and
assembly metadata. Decompiled evidence is under
`artifacts/Refinement/megamod-spec-20261104/linkable-hydroponics/upstream/`. Some Unity/Harmony
references remain unresolved in bootstrap/rendering extraction; relevant control flow and supplied
source agree. Static inspection is not an executed Harmony or rendering test.

## Complete source behavior

The bootstrap runs unguarded Harmony.PatchAll with owner linkablehydroponics.mod. Hooks cover grower
GetGizmos, SetPlantDefToGrow, SelectionDrawer.DrawSelectionOverlays, Game.FinalizeInit and SaveGame.
No Mod settings window, bottom tab or operational toggle exists. All added labels/messages are literal
English. Source is contextual entity gameplay, not global/per-save configuration.

XML appends CompProperties_LinkableGrower to HydroponicsBasin after a basic node test in an Always
sequence. A second file conditionally appends it by Def name to VFE_PlanterBox, VFE_Ecosystem,
VFE_PlanterBox_Tilable, VFE_Hydroponics_Tilable and VFE_Ecosystem_Tilable. There is no package-ID gate,
comp-duplication check, current type validation or transaction guarantee. These are additive lists,
but not idempotently guarded. The helper accepts spawned, nondestroyed Building_PlantGrower instances
with this comp, or those five names even without the comp. Actual gizmos, IsLinked recognition and
unlinking through that member need its comp. GroupManager create/add/restore nevertheless retain
comp-less members and SyncGroupTo writes to them: a name-only match can become a one-way crop
recipient while lacking its own linked controls/recognition. It is not a reliable working integration.

| Action | Exact selected source result |
| --- | --- |
| Select at least two eligible basins; Link Hydroponics | Require identical building.sowTag strings among selected basins; otherwise reject. There is no actual common-crop intersection check. |
| Link all independent basins | Create one group, assign IDs; do not change any current crop. |
| Link independent basins with members of one group | Add independent selections to that existing whole group; do not synchronize crops immediately. |
| Link selections from several groups | Merge the whole groups, including unselected members, then add selected independent basins. Target comes from group-ID enumeration, not an explicit crop source. |
| Link selections already in one group | Reject with an already-linked message. |
| Change one linked basin's crop | Postfix propagates the incoming nonnull crop to every other live spawned member, with a static recursion guard reset in finally. No per-member crop validity check. |
| Unlink Selected | Remove only selected members; a group with at most one survivor dissolves and clears the survivor's group ID. Crops stay as they were. |
| Disband Group | Dissolve every group represented in the selection, including unselected members; no explicit confirmation shown by this code. |
| Select Linked | Add all live spawned members of represented groups to current selection; preserve already-selected objects. |
| Select any linked basin | Orange brackets identify other unselected live members on the current map; one group is drawn once. |

Link/merge changes **membership only** until a later crop edit. Notifications count selected basins
rather than necessarily all members of merged groups. The source stores no shared canonical crop,
group name, Allow sowing or Allow cutting state. Same sowTag is neither a current-crop match nor proof
that every member permits the incoming plant. Normal selected-gizmo composition is not expanded to
all unselected linked members before native crop choices are offered.

The SetPlantDefToGrow postfix broadcasts its incoming argument, not an independently reread effective
result. A compatible future owned hook must consider skipped/modified native writes and the whole
group's current eligibility; method name alone cannot prove a successful crop change. Catching a
mid-group exception logs an error but leaves earlier writes in place. No rollback exists in the source.

## Persistence, lifecycle and performance

Each comp saves groupID, default-1. Static GroupManager owns Dictionary<int,List<Building_PlantGrower>>
and monotonically increasing IDs; validity means the list contains more than one member. Public
create/add/restore APIs do not validate ownership, same-map membership or compatible crop capability.
GetAllGroups copies the dictionary but retains member-list references; GetGroupMembers copies a list.
No external API consumer was established by the bounded search below.

GameComponent saves groupID→list of integer thing IDs as savedGroups and nextGroupID. SaveGame prefix
captures current spawned members; paused GameComponentOnGUI also rebuilds that snapshot every call.
Every300 ticks it cleans destroyed/nonspawned members. This is repeated work, not a performance model
to copy. Restore clears static groups, resolves every saved ID by scanning every map's AllThings,
and restores groups with at least two found growers. It does not enforce same-map membership or
validate the restored members' source comp/content capabilities. The restored-count log uses the
saved dictionary count rather than the number actually restored.

Do **not** infer that Dictionary<int,List<int>> with value LookMode.Value is automatically broken:
current Scribe_Collections serializes dictionary values through its list routine, detects nested
List<> and uses LookListOfLists for the integer contents. The relevant current native path was read;
an actual source save/load workflow still remains unverified.

PostDestroy removes a linked member. PostSpawnSetup on a non-load spawn resets its comp ID to-1.
There is no immediate owned DeSpawn handler; periodic cleanup removes nonspawned references later.
Therefore reinstall/transfer membership needs explicit owned policy and testing. In particular, an
object respawned before cleanup can have an unlinked comp while its reference remains in an old list;
this is a static lifecycle risk, not an observed live reproduction. Missing crop intent, disabled
feature recovery, map disposal and new-game/static ownership also need an explicit contract.

## Current native grower and crop-selection evidence

Game1.7.4998 rev1299, current assembly hash
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` (rechecked in the preceding source).
Native extracts under sibling `native/`: Building_PlantGrower and Command_SetPlantToGrow fully read;
relevant PlantUtility and nested-list Scribe methods read, not every unrelated method.

Building_PlantGrower implements IPlantToGrowSettable and owns a saved plantDefToGrow, initialized from
its building default. SetPlantDefToGrow simply writes that field. Native grower GetGizmos contributes
the crop command, with no zone-style allowSow/allowCut fields/toggles. CanAcceptSowNow checks power.
TickRare damages plants when power is off; DeSpawn destroys contained plants except WillReplace.
Linking cannot turn those existing world/work conditions into free growth or protect plants during
ordinary dismantling merely by retaining membership.

Command_SetPlantToGrow combines selected growers. PlantUtility.ValidPlantTypesForGrowers accepts a
plant only if CanSowOnGrower accepts it for **every** participating grower. Current CanSowOnGrower
checks pollution, Ground tags for zones and the building's sowTag for things. Command IsPlantAvailable
returns true early when sowResearchPrerequisites is null; otherwise it checks research and the relevant
darkness/wild-plant conditions. Do not silently repair this native condition in a linking feature.
The command retains skill labels,
information-card access, roof/light cautions and lack-of-qualified-grower warnings. Actual cell
fertility, access, power and job restrictions remain separate from simply choosing a crop.

The existing [S006 contract](../../../../openspec/changes/specify-megamod-linked-growing-zones/)
already selects same-map persistent zone groups, first-selected source identified before linking,
immediate crop/sowing/cutting synchronization, actual crop eligibility and missing-choice recovery.
Its design explicitly deferred hydroponics to this turn. Source hydroponics differs in whole-group
merge, delayed initial sync and crop-only state. Reconcile those decisions explicitly; do not stack
patches between two owned group implementations or silently assume basins expose zone work flags.

## Optional VFE Farming evidence and consumers

The installed author description and metadata for Vanilla Furniture Expanded - Farming were read.
Package **VanillaExpanded.VFEFarming**, Workshop1957158779, declares1.7; its explicit v1.7 LoadFolders
uses root plus1.6. It requires Harmony and optional-to-megamod Vanilla Expanded Framework. This is
an explicit selected path, not a claim that all provider code has been verified.

The five source-named definitions exist in its selected Buildings_Growers/Buildings_Growers_Tilable
XML. Planter boxes use VFEF.Building_PlantGrower_NoEmptyLines, a Building_PlantGrower subclass whose
only inspected override changes inspect text. Other growers use the native class. Planters/ordinary
hydroponics use Hydroponic tags; ecosystems use Ground plus VEF.Plants.SowerExtension extra Oxyalgae.
Thus raw sowTag equality can reject growers sharing a usable crop or miss provider eligibility details.

Selected VFEF.dll SHA-256
`7EA3702C85240D097EB3D86BFF1D8A169063898A34BACDFBABB7B9520D7D9F8A`.
VEF package **OskarPotocki.VanillaFactionsExpanded.Core**, Workshop2023507013, explicit1.7 VEF.dll hash
`6D78CF272F2E4B2F29EE71B7D3A3B56B4A9354451B0F427AF91096E39A6993F4`.
Read the full selected SowerExtension and CanSowOnGrower patch types under sibling `provider/`.
Its transpiler replaces the building tag membership check with DetectTags, which accepts the normal
tag or intersection with extraSowTags. No provider code was executed. Preserve effective eligible
crop results through an admitted native/provider path; do not reconstruct a narrower raw-tag chooser.

Bounded installed C#/XML/JSON search for LinkableHydroponics/package identity returned only the source
itself. Public helpers and saved comp/GameComponent types still exist; absence of a text hit does not
prove absence of compiled/reflected consumers. No package/type facade or automatic save importer is
selected. An importer would require separate backup, atomicity and verified restoration semantics.

## Native work and missing-content follow-up

Read current WorkGiver_GrowerSow and WorkGiver_GrowerHarvest extracts retained under sibling
`smarter-doctors/native/`. Sow ExtraRequirements checks CanAcceptSowNow, zone-only allowSow and a
non-null wanted crop. Cell work separately checks conditions, reservations, pawn skill, blocked cells
and protected plants. Harvest checks maturity/yield/reservation and zone-only allowCut semantics.
No building sow/cut toggle or new scheduler is justified by this source.

Read the entire current Command_SetPlantToGrow. Its constructor collects selected crop choices and,
unless mixed non-null choices are found, dereferences the resulting crop's uiIcon/label. An all-null
saved crop case therefore cannot safely use it unchanged. This is a static control-flow finding,
not an observed crash. Independent review also identified native Building_PlantGrower.GetInspectString
and VFEF.Building_PlantGrower_NoEmptyLines.GetInspectString calling GrowthSeasonNow with the crop.
The host reread those types and GrowthSeasonNow: where a room is available, it dereferences plantDef.plant.
Selection/inspection therefore needs null-safe recovery before that call, not just a replacement menu.
Recovery must preserve missing identity without substituting a crop just to keep the UI alive. Normal future jobs and
harvest remain native; exact queued-job and patched-provider paths are LH-Q2 research.

## Proposed composed result and remaining work

[Proposal/design/spec/tasks](../../../../openspec/changes/specify-megamod-linkable-hydroponics/)
and [replacement coverage](../replacements/linkable-hydroponics.md) now distinguish these decisions:

- Same-map, player-controlled building groups are crop-only. S006 zone groups retain crop/sow/cut.
  Share direct proven operations, not internal patches. Mixed membership/new basin flags remain
  outside both drafts; a combined crop command can still update their distinct owned groups.
- Adopt S006's shown first-selected source, immediate synchronization and selected-only incoming
  moves. This intentionally differs from source whole-group merges and delayed initial sync.
- Use all affected members' effective crop eligibility, including VEF extra tags and unselected
  members, and revalidate at activation. Preserve native warnings, research/work and conditions.
- Contextual name/count/crop/recovery and non-color identity are proposed UX extensions. No tabs,
  settings rows or source-specific inheritance mechanism; seven-language player text.
- Map-owned membership/native crop state replaces static groups, OnGUI snapshots and repeated map
  scans. No Def comp addition is selected; no XML contribution/dependency is needed for this feature
  as designed. Any later static addition must follow guarded merge-preserving XML Extensions rules.
- Despawn/uninstall/transfer removes a member before further group edits; saved placed members retain
  valid links. Reinstall is independent. Missing choices recover without sowing substitutes, including
  after unlink. Conflicting saved member choices require explicit relink, not arbitrary overwrite.
- Original provider has authority; owned building linking stays inactive before hooks. S006 cannot
  bypass that through a mixed menu. Provider-specific adapters need package/shape admission; ordinary
  native-compatible content is not rejected for an unfamiliar Def name.
- Actual targets still require effective semantic predicates, incoming-stream validation if relevant,
  later repatch handling, atomic owned rollback and bounded per-skipped-enabled-patch startup warnings.
  No arbitrary external setter side-effect rollback or source save migration is promised.

LH-Q1–4 retain proposed scope/UX, exact integration/lifecycle/recovery, providers/consumers and final
implementation/acceptance. Static balance pass preserves costs, crop/research availability, power,
fertility, yield and actual jobs. Finalized comparators and native workflows remain required.

### Static comparator boundary

Read Core Buildings_Production.xml and the selected VFE Buildings_Growers.xml definitions.
These are source values, not finalized measurements or a rebalance proposal:

| Building | Footprint | Fertility | Listed power | Listed construction / research |
| --- | --- | --- | --- | --- |
| Core HydroponicsBasin | 1×4 | 2.8 | 70 | Steel100 + industrial component1; work2800; Hydroponics |
| VFE_PlanterBox | 4×2 | 0.95 | No power comp | Stuff30, metallic/woody/stony; work2000; no local research prerequisite |
| VFE_Ecosystem | 2×2 | 3.5 | 200, with built-in light | Steel100 + Plasteel20 + spacer components3; work4200; advanced hydroponics |

Linking must not copy these quantities, native grower limitations or power state from the source
member. Only crop orders synchronize. The different crop tags/extra tags must still yield a real
common crop; no cheaper building inherits another member's fertility, light or progression benefit.

### Independent review and validation checkpoint

Source review found two P2 documentation gaps: the native/VFE inspect null path and asymmetric
comp-less source recipients. Both were corrected with exact evidence; LB08 gained an observable
inspection scenario and focused re-review closed both. Readme.md was found/read and the inventory
claim corrected. A separate composition review found no actionable P1/P2 issue against S006 and
foundation contracts. No reviewer performed runtime work.

Draft: **9 requirements, 25 scenarios, 23 unchecked tasks**. Typed strict OpenSpec validation:
**98 items passed, zero failed**, exit0/no stderr. Nine scoped Markdown documents passed local-link
and whitespace checks. This verifies the documentation only, not any native compatibility claim.

No runtime product edits, tests, build/install, game launch, source execution, Workshop writes or
normal save/configuration mutation occurred. No gameplay task is closed.
