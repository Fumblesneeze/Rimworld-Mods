# Linkable Hydroponics

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin: replacement, S076 / P2-05. Source **Linkable Hydroponics**, **Yakobo**,
`Yakobo.LinkableHydroponics`, [Workshop3535816519](https://steamcommunity.com/sharedfiles/filedetails/?id=3535816519).
Read-only inspection2026-11-04, installed1.6 metadata, root hydro.dll1.0.0.0 SHA-256
`90FB50A7874D95DF1553C5E8DFF35FAC49C1774E2313A254A4D51D98975E2CC4`.

Steam lookup returned429; installed author description preceded full supplied source, both XML
patches and selected assembly decompilation. Independent review found the initially missed Readme.md;
the host read it and confirmed its controls/save descriptions. No license found; no reuse
permission is inferred and no source/assets are copied into product. Exact inputs, native/provider
hashes and evidence limitations are in the [research note](../refinement/2026-11-04-linkable-hydroponics-research.md).
The [owning change](../../../../openspec/changes/specify-megamod-linkable-hydroponics/) is a future
observable draft, not implemented or live accepted.

## Behavior inventory

Requirement IDs below refer to the [observable contract](../../../../openspec/changes/specify-megamod-linkable-hydroponics/specs/megamod-linked-growing-buildings/spec.md).

| ID | Source action/result and evidence | Proposed owned result | Status |
| --- | --- | --- | --- |
| LB01 | Link selected basins; LinkingOperations creates/adds/merges entire represented groups, no initial crop sync | Shown first source immediately supplies crop; selected incoming members alone move | Explicit correction drafted |
| LB02 | Equal sowTag check; later incoming setter argument broadcast without per-member eligibility | Entire affected set uses effective native/provider selectable crops, with commit-time validation | Correction drafted |
| LB03 | Static recursion flag prevents setter reentry; source has only building crop state | One direct action across owned groups, separate zone permissions/membership | Drafted |
| LB04 | Setter changes crop, not plant or building stats | Normal future work, environmental limits, power damage and existing-job validation | Preserved intent drafted |
| LB05 | Unlink selected; group of one dissolves; disband all represented groups | Preserve final crops; clearly identify whole-group disband scope | Drafted |
| LB06 | Link/Unlink/Disband/Select linked gizmos, orange corner overlay, literal English | Contextual group name/count/crop/recovery, readable identity, seven languages | Extended UI drafted |
| LB07 | groupID plus global static dictionary; save component and IDs, cleanup every300ticks | Map-owned saved links, immediate invalidation, reinstall independent, conflicting choices require relink | Correction drafted |
| LB08 | No missing-crop recovery; native menu and native/VFE inspection assume a crop | Null-safe selection/inspection/recovery, no substitute sowing, preserve normal harvest | Correction drafted |
| LB09 | Unconditional PatchAll and source XML comp additions | One authority, minimal semantic admission, isolated provider fallback and bounded warnings | Correction drafted |

## Interactions and composed result

[S006 linked growing zones](../../../../openspec/changes/specify-megamod-linked-growing-zones/)
already selects immediate first-source synchronization and selected-only transfers. S076 adopts
those gestures with shared direct owned operations, not patches to our zone code. Building groups
remain crop-only; native buildings have no zone allowSow/allowCut controls. Mixed membership is not
specified. A shared native crop action can reach both group kinds without merging them or altering
zone permissions. Preserve future growth/blight/crop-availability contracts through their normal
work paths, rather than building a separate grower scheduler or per-source Thing cache.

Compatible native-growing content is eligible by capability. Optional **Vanilla Furniture Expanded -
Farming**, `VanillaExpanded.VFEFarming`, explicitly selects1.6 files for1.7; the five source-named
growers and current Framework extra-tag eligibility were inspected. Preserve effective crop rules;
neither provider metadata nor source static inspection proves runtime beta support. Any special
executable adapter needs canonical package and consumed-shape checks. Changed/absent optional
providers keep independent behavior without broad rejection of unrelated compatible content.

Original `Yakobo.LinkableHydroponics` active: owned building links stay inactive with a bounded
startup warning. Foreign groups/comps/patches remain untouched. S006 must not accidentally broadcast
through original-owned building groups via its shared crop menu. No source identity spoof or automatic
conversion. Harmony becomes required on runtime use; no XML patch is selected. A later necessary
static Def addition must use guarded additive/idempotent XML Extensions and declare its dependency.

## UI audit and relocation

| Source entry | Purpose | Owned destination/change |
| --- | --- | --- |
| Building crop gizmo | Choose crop | Native contextual chooser, entire affected owned groups considered |
| Link / Unlink selected / Disband group | Membership | Contextual; source and whole-group scope identified before action |
| Select linked | Select members | Contextual; adds current-map members, retains unrelated selections |
| Orange corner markers | Identify unselected linked members | Contextual visualization plus text identity/count, no color-only dependency |
| No source settings/tab | No configuration window | No new bottom tab, toolbar toggle or settings rows |

Group names, membership and crop choices are entity gameplay state, so global/save/Use global
settings do not apply. No invented setting or exception. Native unrelated controls remain present.
New player text uses all seven baseline languages; technical log-only text/IDs remain invariant.

## Contracts consumed by other mods or saves

| Consumer/evidence | Binding | Disposition and verification gap |
| --- | --- | --- |
| Source saved comp/component | groupID, savedGroups, nextGroupID and public LinkableHydroponics types in hydro.dll | No import/type alias; owned save/recovery contract is separate |
| Source's own patches | Public GroupManager/CompLinkableGrower/LinkingOperations helpers | No actual external text consumer found; inspect compiled/reflected users before claiming preservation |
| Native growers/jobs | Native crop field/setter, native work/plant state | Preserve ordinary crop/work results with explicit owned group transactions; effective beta proof open |
| VFE Farming/Framework | Native grower subclass, extraSowTags predicate | Preserve effective eligibility via admitted optional paths; exact-present native verification pending |

No universal drop-in or safe-uninstall claim. Native valid crop fields remain useful when linking
is inactive, but full removal loses owned UI/metadata guarantees. A future source importer needs
backup, atomicity, failure restoration and verified postconditions before implementation.

## Implementation and coverage

**Nothing implemented.** All LB01–09 implementation, target specs, executable validators, focused
RED/GREEN, package checks, native acceptance and measured performance remain outstanding. The source
static cache, paused repaint save snapshots, unchecked PatchAll, implicit whole-group merge, delayed
initial sync and five-name comp whitelist are intentionally not carried over. No new crops or basin
sowing/cutting controls; no claim to cover arbitrary third-party setters or source saved types.

## Verification and acceptance

This is documentation/source analysis only. Native job checks show crop grouping must leave cost,
fertility, research, power, skill, protected plants, growth/yield and already-running job validation
unchanged. Compare Core hydroponics, VFE planter and ecosystem conditions in final loaded data and
actual jobs before accepting that boundary; the source metadata is not a finalized balance measure.

LH-Q1–4 in the [progress ledger](../refinement/specification-progress.md) retain scope/UX, effective
transaction/lifecycle/recovery, providers/contracts and implementation/native acceptance. Future
evidence requires real contextual actions followed by personally observed crop/work/save outcomes on
the reviewed build in exact isolated mod lists. Documentation validation accepts no gameplay.
