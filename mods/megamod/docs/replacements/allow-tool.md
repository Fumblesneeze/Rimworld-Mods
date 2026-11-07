# Allow Tool

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- Origin: **replacement**. S008 / P1-08. Source **Allow Tool**, `UnlimitedHugs.AllowTool`, Workshop
  **761421485**, inspected 2026-11-02. Source support ends at 1.6; this is not a 1.7 compatibility claim.
- Documentation first: [Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=761421485),
  installed About/Version/ModSync, then [author repository](https://github.com/UnlimitedHugs/RimworldAllowTool).
  Author forum returned 403. Steam's generic removed/incompatible banner was not used as evidence.
- Installed metadata/file version **3.14.0**, assembly **3.6.0.0**. Read-only input:
  `F:/Steam/steamapps/workshop/content/294100/761421485/v1.6/Assemblies/AllowTool.dll`, SHA256
  `471F01CBC116269CE5CCD2588E6748C704CB099F512B91531847D6944147DA5C`.
- Author revision **cc168e824f883582ef2e1e3fab9225ae9594ba1d**. Packaged repository DLL has the same
  hash. Source project and installed decompilation both inspected; obsolete excluded source files
  are not treated as active patches. Source `license.txt` dedicates the work to the public domain.
  Credit UnlimitedHugs; no upstream assembly or unaudited art is shipped.
- Ignored evidence: `artifacts/Refinement/megamod-spec-20261102/allow-tool/{upstream,decompiled,game,consumers}`.
  Game comparison: **1.7.4995 rev309**, Assembly-CSharp SHA256
  `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
- Source requires HugsLib 9.0.0; ours does not inherit that dependency. Harmony and the proposed
  XML Extensions integration become required only with their implementation slice. Other providers
  are optional. No runtime dependency changes are made by these documents.

The [owning change](../../../../openspec/changes/specify-megamod-allow-tool/) contains proposal,
design, four observable specs and unchecked implementation tasks. Source-code analysis is not a live
observation. Native target gaps AT-Q1–4 remain explicit; **nothing is implemented or accepted**.

## Behavior inventory

Specification links: [orders](../../../../openspec/changes/specify-megamod-allow-tool/specs/megamod-order-tools/spec.md),
[urgent hauling](../../../../openspec/changes/specify-megamod-allow-tool/specs/megamod-urgent-hauling/spec.md),
[finishing/hunting](../../../../openspec/changes/specify-megamod-allow-tool/specs/megamod-finishing-and-party-hunting/spec.md),
[mining](../../../../openspec/changes/specify-megamod-allow-tool/specs/megamod-mining-plans/spec.md).
All rows are **analyzed and specified; unimplemented**. File references below are relative to upstream
`Source/` unless identified as XML/native. Exact options/defaults live in these observable specs.

| ID | Input → observable result / edge cases | Evidence | Spec requirement |
| --- | --- | --- | --- |
| AT01 | Area allow/forbid; Allow everything with Shift remains and Ctrl non-haulables; counts, fog exclusion | Designators/Designator_Allow, Forbid, AllowAll, SelectableThings | Orders: Allow and forbid revealed objects |
| AT02 | Similar type/material seeds, cursor summary, Architect closing, Shift additions, Alt bypass, default 200 limit, map/view/home expansions nearest camera first | Designator_SelectSimilar, SelectSimilarReverse; MenuEntry_SelectSimilar* | Orders: Select by item type and material |
| AT03 | All 23 extra menu entries across 14 controls; counts, native eligibility and native menu preservation | DesignatorContextMenuController, BaseContextMenuEntry, ContextMenuProvider, all MenuEntry classes | Orders: Context actions have explicit scope |
| AT04 | Cancel matching types map-wide; all cancelable orders; all blueprints | MenuEntry_CancelSelected/Designations/Blueprints | Orders: cancellation scenarios |
| AT05 | Mature harvest with crop/wood modifiers; special-tree mass protection, deliberate native single-tree action | Designator_HarvestFullyGrown, SpecialTreeMassDesignationFix; plant patches | Orders: Mature harvesting and important-tree protection |
| AT06 | Global keys, first-enabled context shortcut, selected-object Shift pickup, overlay/hint and icon options | HotkeyHandler, AllowThingToggleHandler, context controller; root KeyBindings.xml | Orders: Keyboard and selected-object shortcuts |
| AT07 | Individual tool/menu/reverse visibility and global/save options; hide buttons without disabling existing work | ModSettingsHandler, WorldSettings; controller map-load/setup callbacks | Orders: Common settings and state separation |
| AT08 | Mark urgent, unforbid, supply ordinary chunk order, Shift work reconsideration, Haul+ scheduling | Designator_HaulUrgently, WorkGiver_HaulUrgently; root work-type/giver XML | Hauling: Explicit urgent item orders |
| AT09 | Hidden automatic priority 3; retain visible choices; source tie order Haul+ after Bed rest/before Basic, Finish after Basic/before Warden | Controller/utility, root work-type XML and Core 1.7 WorkTypes.xml | Hauling: Work-column presentation preserves player priorities |
| AT10 | Delivered vs failed/partial haul, split/merge/transfer, save/map lifecycle | ToilsHaul patch, DesignationCleanupHandler, native Toils_Haul | Hauling: Urgency lasts until successful delivery |
| AT11 | PUAH urgent-only trip composition, native fallback, real capacity, shared search freshness | PickUpAndHaulCompatHandler; installed PUAH source urgent validator; user intent | Hauling: Shared hauling behavior respects actual capacity |
| AT12 | No-destination current-map alert, up to five names, locate targets, exclude reservations | Alert_NoUrgentStorage, HaulUrgentlyCacheHandler | Hauling: Explain missing urgent storage |
| AT13 | Downed-target orders, Shift friendlies, drafted/undrafted context, Melee 6 non-animals, incapacity reasons | Designator_FinishOff, WorkGiver_FinishOff, float-menu patch | Finishing: Deliberately finish downed targets |
| AT14 | Approach, 60 ticks, finishing kill, thoughts/tale/record, 3×3 allow, optional <8-cell chain, interruption | JobDriver_FinishOff, root JobDef; cleanup | Finishing: visible job before killing blow |
| AT15 | Drafted membership, nearest visible wild prey, range, fire-at-will, no tame attacks, undraft cleanup | PartyHuntHandler, Command_PartyHunt, draft/wait/gizmo patches | Hunting: individual pawn state |
| AT16 | Auto-finish, designated-only and corpse policy, 20-cell coordination, return and commoner restriction | PartyHuntSettings/Handler, Pawn_Kill patch | Hunting: policies coordinate attacks and finishing |
| AT17 | Mine selected/designated revealed ore vein, including diagonal/fogged continuation with map bounds | MenuEntry_MineConnected; native GenAdj eight-neighbor definition | Mining: Extend mining orders along connected ore |
| AT18 | Large rectangle, preview, gaps 1–50 default 5, Shift increments/release, accept/cancel, fixed origin | Designator_StripMine, UnlimitedAreaDragger, MapCellHighlighter, Dialog_StripMineConfiguration, StripMine*Settings | Mining: adjustable grid and current geometry |

### Source facts and deliberate corrections

Source area eligibility dereferences Map/Position before null checks. The installed 1.6 repair patch
confirms real consumers guard these paths and filter stale map targets. Our direct implementation
must handle absent/transferred targets without needing a repair patch.

Source urgent placement removes a mark before success and cleanup removes marks when an item is
not in native haulable candidates. Our spec retains undelivered urgency, including partial stacks.
S021 reconciliation makes the owned rule explicit: exact urgent quantities survive mixed stacks,
partial held cancellation and reload; ordinary urgent fallback and batching obey the same limit.
This is a direct owned contract, not merely a safeguard for the external Allow Tool adapter.
The source's one-second cache and separate reservation/alert caches are replaced by the shared
candidate design; the proposed two-second alert bound is our observable target, not a source claim.

Source cancel-selected is map-wide by designation type, not selected-only; ours keeps that function
with an explicit label and protects plans/noncancelable entries. Source mature harvest removes all
designations; ours preserves unrelated ones. Native 1.7 plant restrictions/warnings still apply.
Source forbiddable-gizmo enhancement drops later gizmos; ours preserves them.

Source hidden work categories overwrite eligible priorities with 3 on map load. Ours retains player
choices while making hidden automatic priority explicit. Source hiding both Finish controls disables
its ordinary work giver; ours treats visibility as presentation. Source party auto-finish can choose
tame-designated prey and has an unguarded queued return; ours excludes automatically chosen tame
prey and respects intervening player commands. These are specified deviations, not fixes delivered.

The optional party-hunter corpse policy retains its actual broad source scope: any corpse from a
kill attributed to a member, including melee/finishing and non-prey victims. It is labeled accordingly
and is distinct from allowing the 3×3 neighborhood after finishing. Recovered targets lose their
finishing authorization, while interruption of a worker alone leaves a still-valid explicit order.
Large bounded area selection, cursor seed feedback and closing Architect after similar selection
are retained observable details, not separate source mods.

## Interactions and composed result

PUAH's explicit upstream cooperation is evidence against the earlier invented conflict. Owned
urgent hauling and inventory hauling will directly share order identity, eligibility checks and
capacity accounting; no patch of our own work giver. The PUAH source itself still gets its separate
change at S021. TD Pack has a real urgent-refill integration to analyze in its own selected feature
work. Automatic blight cutting, micro-designations and custom hunting likewise keep their own
changes; Allow Tool's manual actions do not imply automatic versions or precedence conflicts.

The custom item-destruction feature uses repeated melee against items with HP; it is not this
guaranteed finishing action against downed pawns. The user's hunting-seat range of about 40 cells
without LOS does not replace drafted party hunting's source weapon range and visibility rules.

## UI audit and relocation

| Source surface | Purpose | Megamod destination / impact |
| --- | --- | --- |
| Architect Orders, item reverse controls, right-click menus | Actual designations/selection | Keep native contextual surfaces; preserve native/foreign options and deduplicate equivalent 1.7 actions |
| Haul+ / optional Finish work columns | Per-pawn job priorities | Native Work tab, entity state; no new bottom tab |
| Pawn Party hunt gizmo | Membership of that pawn | Pawn gizmo and save state; not mod settings or global button |
| Party gizmo's three policy options | Save-wide configuration | Move to ordered common mod settings, global/default inheritance |
| Strip-mine floating dialog | Current map plan geometry plus source preferences | Keep plan editor for geometry; move initial defaults/show-editor preference to common settings |
| HugsLib options and visibility controls | Configuration | Common Mod settings; exact defaults and scope in orders spec |
| Developer-only skill bypass; source selection god-mode fog bypass | Unrestricted override | Top developer toolbar; inactive outside developer mode |
| Urgent storage alert | Explain blocked hauling | Native alert area; retain target navigation |
| Source bottom tabs | None found | No new bottom tab or wrapper window |

Native key bindings and remembered window position are global input/presentation exceptions with
reasons in the spec. All other actual configuration uses shared load/store/resolve/render and Use
global. Main-menu save mutation is disabled. Pawn membership, orders, priorities, selection and
current mining geometry are not settings. Seven baseline languages remain an implementation gate.

## Contracts consumed by other mods or saves

The [design's contract table](../../../../openspec/changes/specify-megamod-allow-tool/design.md)
records exact members and decisions. Confirmed inspected bindings:

| Consumer | Contract | Coverage / verification still required |
| --- | --- | --- |
| PUAH 1279012058, shipped source | Display name `Allow Tool` plus `HaulUrgentlyDesignation`; urgent trips collect only similarly marked items | Native fallback and owned composition specified; external name-detection adapter not verified |
| Some Things Float 2940072380, compiled 1.6 | Def-only lookup `HaulUrgentlyDesignation`, optional KAU fallback; adds it to spawned items/corpses | Preserve semantic Def identity when sole provider; actual river-spawn→haul workflow still required |
| Lifters Haul Urgently 2904986472, 1.7 XML | `HaulingUrgent`, `HaulUrgently`, mech enabled-work list, original package dependency | Cannot satisfy package dependency merely by keeping names; mech adapter not yet supported |
| TD Pack Continued 3525414162, shipped source | Package-gated work-type binding, urgent-refill work giver and texture path | Future direct composition; external compiled parity/adapter and original texture asset not promised |
| Allow Tool Fixed Patch 3105126782, compiled 1.6 | Original or dawnsglow package gate; reflected AllowTool class/method names | Fix the underlying invalid-target behavior; do not spoof old package/types for this patch |

Consumer evidence hashes: SomeThingsFloat.dll
`2A18308AE11741A5C1AD25A2AD4DFA9EF41C25C1773B46220EEEC2859643B71B`;
1.6 `No.016 Allow Tool Fixed Patch.dll`
`C1A018E240C6EE0D4CFB83A6C01F6E11B62990E3CA192C7A52C21F59988C5752`.
An earlier 1.4 repair DLL was inspected but superseded by the 1.6 analysis; it is not the current evidence.
Search covered installed source/About/relevant patch XML, not every compiled assembly; absence of
another match is not proof of no further consumers.

Source HugsLib keys, `WorldSettings` deep nodes, integer pawn IDs, job/mark/work Defs and public
extension APIs are inventoried in design. No automatic conversion or full binary/API compatibility
is promised. A future migration must define backup, atomic conversion, failure restoration and
verified postconditions. Removal with active owned jobs is not accepted yet.

## Implementation and coverage

Every AT01–18 row currently has **no code, target validator, RED/GREEN evidence or live acceptance**.
All functionality remains to implement. Deliberately omitted: source HugsLib UI, obsolete excluded
patch, positional XML insertion at `li[13]`, source bugs listed above, fake package identity and
automatic upstream-save import. Other mods' unselected functionality is not added to scope by finding
a consumer.

Co-installation policy: original Allow Tool active → suppress the entire replacement family before
both owned legacy-named Def registration and runtime hooks, explain once, leave original settings and
saves alone. Alternative providers need AT-Q2 detection research. Guard failures isolate their
affected feature/atomic group; compatible other patch owners are not automatically rejected.

XML integration uses XML Extensions presence/shape/package guards and preserves all unrelated Orders
entries. Only the native allow/forbid entries intentionally replaced by admitted owned equivalents
can be removed. No source list replacement or copied Assembly is authorized by this draft.

## Verification and acceptance

Source-derived scenarios and defaults are drafted; strict validation and independent review are
tracked in the [progress ledger](../refinement/specification-progress.md). No gameplay task is
checked by document validation. Later tests cover actual failures, split/merge lifecycle, native
fallback, priority retention, stale targets, menu composition and guards. Final reviewed-build native
workflows must visibly prove each tool/job, settings inheritance and save switching, plus selected
absent/present/changed-provider cases. Evidence must retain actual player actions, outcomes, package
identity and cleanup under the repository's isolated minimized-game rules.

Semantic balance pass: urgent work is still physical capacity-limited hauling; mining designations
do not create ore; mature harvest preserves native availability; guaranteed finishing and party
hunting materially affect combat and execution consequences. Melee 6, 60 ticks, 8/20-cell ranges and
default policies are source-derived, pending live 1.7 review. The alert freshness bound and safe
order-lifecycle corrections are explicit proposed differences. **AT-Q1–4 remain open** in design
and refinement; they are not questions being sent to the user mid-research.
