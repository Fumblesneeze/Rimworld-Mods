# Designations Tooltip

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **replacement**, S016 / P1-13 / P1-36. Source **CheaterEater.DesignationsTooltip**,
  Workshop **3743959668**, author **CheaterEater**, installed About **1.11**, supports **1.6 / 1.7**.
- Inspected 2026-11-02: [primary Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3743959668)
  and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3743959668) first,
  then [author source](https://github.com/CheaterEater-RM/Designations-Tooltip/tree/659085e856e8644d77445678dc61ae2ef2e245c0),
  installed decompilation and relevant native 1.7 types.
- Read-only installed root `F:/Steam/steamapps/workshop/content/294100/3743959668`; 1.7 DLL assembly
  **1.1.0.0**, net10.0, SHA-256 **DBA5BBDE432DD71D27BA3EB1F6335DCCAB25F7BD725F515DB2030B4B2A0AC2D9**.
  The pinned upstream DLL matches. Source files: Patch_DraggerOnGUI.cs, Settings.cs and Setup.cs.
- MIT license, copyright **2026 CheaterEater**. Retain required notice for substantial reuse; no
  assembly or image is copied into the product. [Research](../refinement/2026-11-02-designations-tooltip-research.md)
  records the primary-fetch fallback, source/native discrepancies, hashes and real consumer binding.

## Behavior inventory

Every row is **specified, not implemented or verified**, in
[the owning change](../../../../openspec/changes/specify-megamod-designation-preview/).
Requirement names below refer to its
[observable capability](../../../../openspec/changes/specify-megamod-designation-preview/specs/megamod-designation-preview/spec.md).

| ID | Action → observable result | Defaults / boundaries | Evidence and owning requirement |
| --- | --- | --- | --- |
| DT-01 | Drag an area → accepted / candidate cells at its center | Per-cell count, actual shape, zero retained; no item/stack/yield/job estimate | Source renderer + native DragCells/CellBuffer; Count candidate and accepted cells accurately |
| DT-02 | Change Classic mode → accepted count only | Default false | Source settings/render; Two preferences use common global and save controls |
| DT-03 | Drag small area → count despite native size suppression | Default true; off requires both bounds ≥3; still respects area-disabled/single-cell/empty conditions | Source size edit + native GUI; Respect drawing-style and size visibility |
| DT-04 | Move/change style/tool/map → current matching pair | Paused changes, full footprint beyond camera, no stale cross-map/save preview | Native caches and source read ordering; Current counts for the current selection |
| DT-05 | Inspect/cancel/release → ordinary action preserved | No hidden target disclosure, mutation, free resource or duplicate designation | Native manager/DesignateMultiCell; Preview does not change designation actions |
| DT-06 | Large label or following UI → legible count and unmodified surrounding UI | Measured centered width; exact caller state restoration; seven languages | Source rendering risks; Readable contextual presentation without UI damage |
| DT-07 | Configure preferences globally/per save → inherited or overridden display | Use global default, inactive inherited/main-menu save controls, next-preview timing | User common settings policy; Two preferences use common global and save controls |
| DT-08 | Target/provider mismatch → safe prior display with diagnostic | Independent dependent small support, one renderer, no owner/package spoof | Source hooks + actual consumer; Semantic admission / One renderer and honest provider support |

Two source explanations are corrected deliberately: its help says zero is hidden although installed
code can show `0 / Y`; its comments call Y rectangle area although the 1.7 native buffer follows the
selected shape. A hollow 5×5 footprint has 16 cells. The draft follows inspected behavior and labels
it accurately. Proposed corrections also address stale count pairs, off-center wide boxes and GUI
state leakage. These are not claimed as fixes already implemented or observed in a running game.

## Interactions and composed result

- **Owned Allow Tool and later custom designation tools:** use the actual candidate/accepted set
  directly; keep the tool's final action unchanged. A bespoke selection callback is not automatically
  modeled by the native rectangle count. No internal Harmony stack between owned features.
- **Native 1.7 drawing styles:** keep outlines, snapping, side measurements, area-disabled styles and
  single-cell policy. Camera-visible highlights are not the accepted full-selection count.
- **Vehicle Map Framework:** actual external ordering dependency exists; exact supported provider
  adapter and transformed-map proof remain DT-Q3. Do not infer conflicts merely from another patch.
- **Common data:** use native transient selection lists and their appropriate refresh lifecycle;
  this display does not need a map-wide Thing index or private source cache.

## UI audit and settings

| Source surface | Actual role | Megamod destination / change |
| --- | --- | --- |
| Center label during supported designation drag | Read-only gameplay preview | Same contextual map position; one readable count |
| Classic mode checkbox | Presentation preference | Interface / Designation preview in mod settings, first row; global/save model |
| Always show count on small drags checkbox | Presentation preference | Same section, second row; global/save model |
| Bottom tab, debug command, operational toolbar toggle | None in source | None added |

Built-in defaults are **Classic off / small counts on**. Both rows use common load/store/resolve/UI,
Global default / Current save / Use global, inactive inherited save editors, main-menu save-control
and callback guards, dormant override retention and next-preview application. Save changes do not
write globals or other saves; world-view editing applies to the active save. No inheritance exception
is needed. Source global settings are not imported automatically. Translate all player text in the
seven baseline languages. The designation itself remains contextual entity gameplay, not a setting.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding and lifecycle | Preserve / adapt / boundary | Required verification |
| --- | --- | --- | --- |
| OELS.VehicleMapFramework 1.6.673, Workshop 3426502333; inspected DLL literal and decompiled DraggerOnGUI patch | HarmonyBefore `com.cheatereater.designationstooltip`; transform native count positions before replacement | Genuine ordering contract; needs explicit supported provider order/shape handling for our owner, not a source type facade or identity spoof | Native transformed-map drag on a supported exact provider; installed 1.6 is not beta acceptance |
| Source ModSettings | `DesignationsTooltipSettings`, global keys classicMode / alwaysShowSmall; static Settings and ForceShowSmall used by its own patch | Owned common preferences preserve defaults/effect, not source type/config identity; no external direct consumer found in bounded scan | Owned global/save persistence and source-active distinction |
| Native designation lifecycle | Current candidate/accepted lists; release through actual selected tool | Preserve data/action semantics; do not persist lists or count only visible highlights | Paused changes, cancellation, release, maps/styles and owned bespoke tools |

Text/XML/C#/JSON scans found only source entries; the DLL literal scan found Vehicle Map Framework
plus the source's two version assemblies. This bounded scan is not proof of no future/dynamic consumers.
The source has no custom gameplay save schema to migrate. Its package/Harmony identities are not
claimed as drop-in contracts; preserve the actual consumed ordering semantics through an admitted
adapter where feasible. No automatic source-settings or normal-save rewrite is planned.

## Implementation and coverage

**Nothing is implemented.** Every DT row lacks runtime code/validators, focused RED/GREEN and final
reviewed-build native acceptance. The target design separates count replacement from its dependent
small-size override; no unguarded PatchAll or exact-hash compatibility gate is permitted.

Required future runtime dependency: **Harmony**. No XML operation is proposed by this feature.
Original-provider co-installation withholds our overlapping renderer and explains why our retained
settings do not control the source. Optional drawing providers need canonical package/shape/order
checks. Unsupported custom selections retain their existing presentation, without guessed counts.
Missing count support disables its dependent small override; missing only small support leaves
ordinary-size affected counts available with an accurate diagnostic.

Intentional deviations: common save overrides; corrected zero/shape help; coherent fresh pairs;
proper long-label centering; guaranteed UI state restoration; guarded, bounded failure behavior.
Source-specific settings import, universal provider compatibility, object/yield estimates and new
designation actions are deliberately not promised. DT-Q1–4 retain exact implementation/acceptance work.

## Verification and acceptance

Document checks/review are tracked in [the sequential ledger](../refinement/specification-progress.md).
No game was launched for these prose artifacts. Final acceptance requires native drag/shape changes,
cancel/release, zero/partial/small/large/offscreen/map-edge cases, settings in main menu/world view/saves,
and observed unchanged designation/UI outcomes on the reviewed build. Verify actual transformed-map
and rejected-provider cases before claiming support; log markers and raw state queries are insufficient.

Retain build identity, exact mod order, causal screenshots, normal-config hashes and cleanup evidence.
Measure large-drag/update cost before performance claims. This is information display: it must not
change resource costs, yields, research or designation eligibility as a side effect.
