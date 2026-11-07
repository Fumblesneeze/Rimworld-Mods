## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S008 replaces Allow Tool behavior. This is an analyzed specification draft, not an implementation or
compatibility acceptance. [Coverage/provenance](../../../mods/megamod/docs/replacements/allow-tool.md)
and the [research journal](../../../mods/megamod/docs/refinement/2026-11-02-allow-tool-research.md)
retain evidence and open work. Installed source supports through 1.6; native comparison uses
1.7.4995 rev309. Do not infer 1.7 support from a method name surviving.

## Goals / Non-Goals

**Goals:** preserve the complete user-facing tool family, document concrete consumer bindings,
compose future owned hauling directly, correct observable data-loss/stale-order defects, and use
the common settings/admission/lifecycle paths.

**Non-goals:** upstream assembly redistribution, a HugsLib clone, automatic save conversion, source
package impersonation, instant mining/killing debug shortcuts in gameplay UI, a new bottom tab, or
implementing every optional consumer as part of this prose slice. The custom hunting-seat, companion,
item-destruction, micro-designation and automatic-blight features remain separately owned changes.

## Decisions

### Prefer native actions and small owned behavior components

Use native designations, selection, storage search, work priority and job paths where their semantics
match the observable contract. Native 1.7 `Designator.RightClickFloatMenuOptions` already provides
designation-all and remove-all options; do not append source equivalents blindly. Preserve foreign
options and gizmos in enumeration order. Source `CompForbiddable_Gizmos_Patch` returns only its first
toggle, discarding later entries; ours must enhance the relevant toggle while retaining the sequence.
Source `Designator_ProcessInput_Patch.cs` is an obsolete file excluded by the source csproj; do not
treat every file in Source/Patches as an active installed patch.

Keep the four capabilities in one source-owned change, with direct owned calls between finishing,
party hunting and hauling. No self-Harmony patches or separate runtime library mod. Do not create
general provider/plugin abstractions just to reproduce the source's class hierarchy.

Source corrections now explicit in the specs:

- Safe map/spawn/bounds checks precede fog access; actual `(ThingDef, Stuff)` identity replaces the
  collision-prone arithmetic selection key.
- CancelSelected actually removes matching designation **types across the entire map**. Use an
  unambiguous label and preserve Plan/noncancelable designations in this action too.
- Fully-grown harvest removes only the conflicting cut order, not every designation. Respect 1.7
  `CompPlantPreventCutting` and important-plant/ideology warnings; source directly writes HarvestPlant.
- Source's urgent placement wrapper removes urgency **before** calling the original placement;
  native 1.7 can fail, redirect or partially place. Track material/order lifecycle through actual
  successful delivery and split/merge/transfer. Losing membership in `listerHaulables` is not delivery.
  Store exact outstanding quantities for owned orders and share them directly with S021. A visible
  loose-stack mark denotes a positive ordered portion, not authority over the whole stack. Clamp
  both batch and ordinary urgent fallback pickups; retain partial held cancellation and saved
  mixed-stack attribution. External Allow Tool's separate bridge implements the same observable
  quantity contract without replacing this direct owned lifecycle.
- Source map loading forces hidden work-type priorities to 3, overwriting any non-3 value. Separate
  hidden-mode effective priority from retained visible-mode pawn choices; never reset them on load.
- Hide-entry-point settings are presentation only. Source ties Finish work-giver availability to
  either button being visible; do not copy that surprising coupling.
- Party attack filter excludes Tame, but its automatic finish filter does not. Exclude tame-marked
  automatic prey consistently. Preserve explicit separately authorized finishing orders.
- `Pawn_Kill` allows any corpse killed by a party member when enabled; it is not restricted to
  ranged shots or eligible animal prey. Preserve that source behavior with an accurate setting
  label and explicit melee/finishing/non-prey scenarios, independently of nearby-unforbidding.
- Source return-to-position and opportunity chains need cancellation identity so a new player
  command cannot be overridden by an old queued return.

### Source lifecycle and settings map

Root `LoadFolders.xml` selects `/` plus `v1.6` for 1.6: active work types/givers/designators/keys come
from root `Defs`, not a neighboring 1.5 folder. Native 1.7 fallback selection must be recorded during
implementation, not assumed from this unsupported source package's metadata.

Source HugsLib pack `AllowTool` stores `globalHotkeys`, `contextOverlay`, `contextWatermark`,
`replaceIcons`, `haulUrgentlyWorktype`, `finishOffWorktype`, `extendedContextActionKey`,
`reverseDesignatorPick`, `finishOffUnforbids`, `partyHunt`, `storageSpaceAlert`, `selectionLimit`,
`finishOffSkill`, `stripMineSettings`, `show<DesignatorDef>`, `showrev<ReverseDef>`, and
`contextEntry_<suffix>`. Settings callbacks rebuild all resolved categories and reverse designators;
our implementation should update only owned presentation and preserve foreign additions.

Source work ordering uses `HaulingUrgent.naturalPriority=1180` and `FinishingOff=1140`. Inspected Core
1.7 WorkTypes.xml has Firefighter 1400, Patient 1350, Doctor 1300, PatientBedRest 1200, BasicWorker 1150
and Warden 1100. The observable spec retains the resulting relative order while explicit pawn
priorities win. These raw values are design evidence, not a replacement for the tie-order scenario.

Source `AllowTool.Settings.WorldSettings` persists deep `stripMine` and `partyHunt`:

- Strip mine: `hSpacing`, `vSpacing`, `variableOffset`, `showWindow`, `lastOffset` (world-wide).
- Party hunting: integer pawn-ID list `pawns`, plus `finishOff`, `designatedOnly`, `unforbid`.
- Global hidden strip settings retain window position.

Ours uses common setting definitions/render/load/store and inheritance, with scoped identity and
timing from the specs. Party membership is pawn state; accepted grid geometry is map state. Moving
world-wide grid origins to map-specific state is deliberate. Current planning editor edits geometry;
default/show-editor preferences belong in mod settings. Native keybindings/global window placement
are documented scope exceptions, not separate save-setting implementations. Skill/fog bypasses are
ephemeral developer commands, switched off when developer mode ends. All UI needs seven baseline
languages, including modifier explanations, scope labels, warnings and disabled reasons.

### Shared candidates, fresh decisions

Inspect native designation, pawn and hauling listers first. Add required candidate fields to the
single map-owned Thing index used by Ctrl+F and inventory hauling, not another per-feature scan cache.
Needed facts: identity/map/cell, type/stuff, quantity, urgent order, spawn/destruction state and
storage membership. Consumer-specific queries filter these candidates; pawn reachability, reservation,
forbidding, capacity and destination remain native action-time checks.

Invalidate spawn/despawn, movement/map transfer, order changes, stack split/merge, consumption,
storage filters/priority/capacity and relevant provider changes. Do not store live references in
saves or access Verse off-thread. Teardown clears map references; load reconstructs the index.
Source has a 1-second real-time list cache, 50-FixedUpdate no-storage slices, a reservation expiration
comparison adding the interval twice (approximately 100 updates), and 60-game-tick cleanup. These
are source evidence, not performance goals. The spec chooses an explicit two-real-second alert
freshness bound including pause. Budget work and benchmark combined consumers and mutation costs;
do not claim optimization from reduced scans alone. If shared data is unavailable, native queries
are the fallback; a failed index invalidation must not silently drive stale jobs.

### Incoming and outgoing contracts

Installed `.cs`, About.xml and relevant XML searches found actual consumers; bounded decompilation
checked two consumers with no shipped source. This is not an exhaustive compiled-assembly scan.

| Consumer | Inspected binding | Intended treatment / remaining limitation |
| --- | --- | --- |
| Pick Up And Haul `Mehni.PickUpAndHaul`, 1279012058 | `ModCompatibilityCheck.AllowToolIsActive` checks **display name** `Allow Tool`; `PickUpAndHaulDesignationDefOf.haulUrgently` resolves `HaulUrgentlyDesignation`; urgent-trip validator restricts added items by that designation | Owned implementations compose directly; no fake name/package. External adapter requires a separately guarded binding for its detection/selection semantics; otherwise native urgent hauling. Do not claim preserving the Def alone suffices. |
| Some Things Float `Mlie.SomeThingsFloat`, 2940072380, installed 1.6 DLL | Resolves `HaulUrgentlyDesignation` then `KAU_HaulUrgentlyDesignation`; settings and river-spawn/death paths add that designation without requiring the source assembly | Preserve the former designation's semantic identity when this family owns it. Candidate for a narrow Def-only compatible contract; verify actual spawned-item hauling in its exact process before claiming support. |
| Lifters Haul Urgently `Spacemoth.LHaulUrgently`, 2904986472, 1.7 XML | Requires original package metadata; patches `HaulUrgently` WorkGiver and `HaulingUrgent` WorkType on `Mech_Lifter` | Def identity alone cannot satisfy original package dependency. Its duplicate check looks for a list under a boolean `canBeDoneByMechs`, not existing mech work membership. Do not copy this bug or silently add unrequested mech support. Record adapter/metadata limit. |
| TD Enhancement Pack Continued `MemeGoddess.TDPack`, 3525414162 source | `AllowToolDefOf.HaulingUrgent` has `[MayRequire("UnlimitedHugs.AllowTool")]`; constructs `HaulUrgentlyRefill` with priorityInType 100; reads texture `haulUrgently`; `RefillZoneComp` owns zone/building state | Future selected-source refinement must compose refill directly. Package-gated binding and asset path require analysis for an external adapter. No fake package or borrowed icon. |
| Allow Tool Fixed Patch `seekiworksmod.no16`, 3105126782, installed 1.6 DLL | Active original or `dawnsglow.allowtool`; reflects Allow/Forbid `CanDesignateThing` and urgent work giver `GetHaulablesForPawn`; removes stale-map candidates | Correct invalid-target handling internally. Its metadata still requires original; do not expose fake AllowTool types merely to attract repair patches. Not a source selected for replacement by this task. |

Public source extension surfaces also include `ThingDesignatorDef`, `ReverseDesignatorDef`,
`IReversePickableDesignator`, `IGlobalHotKeyProvider`, context classes and controller singleton.
No incoming consumer of these extension APIs was proved by the bounded search. Preserve only proven
necessary contracts; keep a gap instead of claiming all source APIs. Def identities also include
`FinishOffDesignation`, `FinishOffPawn`, work types `HaulingUrgent`/`FinishingOff`, work givers
`HaulUrgently`/`FinishOff`, eight designator Defs and four reverse Defs. Their appearance in a save
does not prove our types can read the entire old world component/job state.

### Semantic admission before any patch

The following is a target-analysis inventory, **not completed target documents or validators**.
Before adding each patch, create its stable target-spec document, executable admission, failure
group, warning and tests per `mods/megamod/AGENTS.md`. Hashes identify research artifacts only.

| Integration candidate | Minimal semantics to establish; failure scope |
| --- | --- |
| Orders XML and owned Def availability | Orders and required nodes exist; preserve foreign list entries; insert by semantic anchor, not source `li[13]`; remove only the specific native Allow/Forbid entries after replacement availability is established. Absent original-provider guard prevents duplicate legacy-named Defs. XML Extensions is required when used; failure leaves native orders intact. |
| Native command/context rendering and reverse control pairing | Actual command/result identity, interaction ownership and callback semantics; original body may be skipped by other prefixes. Preserve the full gizmo sequence. Context overlay alone can fail independently of order behavior. |
| `CompForbiddable.CompGetGizmosExtra` | Enumerated relevant toggle and consumed state/action shape; no assumption that it is the sole/first gizmo. Native toggle fallback. |
| `Toils_Haul.PlaceHauledThingInCell` / container or inventory delivery | Actual incoming placement/deposit callback uses carried thing and destination and exposes success/remaining material; existence of a toil-returning method is insufficient to prove delivery. Recheck callbacks after compatible wrappers and every incoming transpiler stream. Urgency lifecycle group must be atomic. |
| Pawn gizmos, drafting and wait/attack lifecycle | Observation callbacks exist and supply actual pawn/state; when autoattack-body completion matters, verify effective path and skip-original implications. Stop party hunting alone on failure. |
| Pawn death corpse policy | Actual damage instigator and corpse availability; do not assume every Kill call has a pawn instigator. Leave native corpse state when unsupported. |
| Plant designation protection | Accepted report still concerns the inspected target and mass/single selection context; do not reject already-rejected or unrelated actions. Preserve native prevent-cutting and warnings. |
| Work-category presentation / priorities | Current game scope, ordering and effective vs stored priorities; Def-wide mutation alone cannot express per-save policy safely across save switching. Fail this category feature visibly if no safe path exists. |
| Optional providers | Canonical package ID plus exact consumed type/member/Def shape and effective semantics; no global search by display name as admission. |

Source transpilers (`Command.GizmoOnGUIInt`, gizmo-list clearing) are not blanket templates for ours.
If a prefix alters an argument, validate the consumed operation/control-flow snippet; all transpilers
validate the actual incoming stream and return it unchanged when incompatible. Do not require clean
Harmony owners or exact IL bytes. Roll back only newly owned patches in an atomic group, log one
bounded warning per skipped enabled patch, and isolate unrelated behavior. Later repatching needs
revalidation/invalidation; do not speculatively execute foreign transpilers to probe them.

## Risks / Trade-offs

- **Source parity versus safe order lifecycle:** urgency following split/merge and retained hidden
  priorities deliberately differ from source bugs. Test native and owned inventory hauling together.
- **Finishing is balance-significant:** Melee 6, a 60-tick preparation and guaranteed execution reduce
  time/corpse damage relative to ordinary attacks. Preserve source values provisionally, record native
  consequences and required victim types; no claim of balanced live behavior yet.
- **Hunting convenience affects safety:** waiting on nearby party members does not ensure no friendly
  fire from others. Source commoner restriction and explicit tame protection are specified; do not
  promise risk-free hunting. Existing separately designated finishing may include other targets.
- **Large selections and plans:** allow the documented ranges with bounded map traversal, no hidden
  reveal and cancellation on lifecycle changes; benchmark the maximum useful workload before acceptance.
- **Co-installation and old saves:** suppressing runtime hooks alone is insufficient if XML created
  duplicate Defs. Provider selection must happen before both Def registration and runtime activation.

## Migration Plan

No migration in this change. First implementation requires fresh/disposable saves for acceptance.
Do not silently import HugsLib settings, source world components, old jobs or numeric pawn IDs.
Any later migration needs its own observable contract: explicit source detection, backup before
mutation, atomic conversion, restoration on failure and verified postconditions. Removing megamod
with active owned jobs/designations is not yet supported or advertised. Normal user files remain
untouched by specification work.

## Open Questions

- **AT-Q1 — 1.7 integration research:** finish native float-menu, drafting/auto-undraft, selector,
  grouping, category/priority and container/inventory delivery paths; derive each minimal effective
  target spec and owned rollback group. Investigate native equivalent menu options rather than
  duplicate them. No implementation starts on an unadmitted path.
- **AT-Q2 — external contracts:** verify compiled versions of PUAH and TD Pack against shipped
  source; inspect remaining incoming assembly/reflection consumers, alternative Allow Tool providers,
  1.7 DLC worker eligibility and category additions. Decide concrete adapters per proven consumer.
- **AT-Q3 — saved-state transitions:** characterize split/merge and inventory order identity, current
  job reload, map transfer, fixed-grid origin and pending returns in native 1.7. Define source-save
  migration only if later desired; no implicit conversion is promised.
- **AT-Q4 — acceptance and balance:** measure alert freshness/index costs, confirm current native
  social/ideology effects and finishing victim coverage, and check retained source defaults in live
  workflows. The two-second feedback bound is provisional until measured, not upstream fact.

These are recorded research/validation work, not questions to interrupt the refinement interview.
