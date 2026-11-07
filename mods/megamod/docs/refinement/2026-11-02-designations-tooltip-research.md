# Designations Tooltip: sequential research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Queue **S016**, photos **P1-13 / P1-36**, origin **replacement**. Inspected **2026-11-02**.
This is documentation/source analysis, not observed gameplay or compatibility acceptance.

## Public documentation before source

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3743959668)
describes affected versus total cells during designation dragging, an affected-only Classic mode,
and counts on small drags enabled by default. Examples concern chopping, mining and smoothing.
Its compatibility answer does not promise all other designation mods. The
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3743959668) describe the
two settings in v1.1. A direct fetch first returned HTTP 429; indexed primary Steam content and its
linked changelog were then read. That cached page shows the older 1.6 release, so its date/version is
not substituted for current installed metadata. Generic Steam warning banners are not test evidence.

The author links [GitHub source](https://github.com/CheaterEater-RM/Designations-Tooltip).
Read-only checkout is pinned at **659085e856e8644d77445678dc61ae2ef2e245c0**,
2026-10-29T21:46:33+01:00, whose changelog adds 1.7 support. All three source files were read:
`Source/Patch_DraggerOnGUI.cs`, `Settings.cs`, `Setup.cs`; the project and 1.7 changelog were checked.
The repository's 1.7 DLL hash equals the installed DLL hash below. Installed decompilation confirms
the relevant control flow/defaults; source comments are not accepted over conflicting code.

## Installed identity and evidence

Read-only root: `F:/Steam/steamapps/workshop/content/294100/3743959668`.
Title **Designations Tooltip**, author **CheaterEater**, package **CheaterEater.DesignationsTooltip**.
About version **1.11**, supported **1.6 / 1.7**. The 1.7 assembly is **1.1.0.0**, net10.0; informational
version 1.0.0. Different version fields are recorded rather than collapsed into one invented version.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `900D627BB3F469BAC00BA032950113A5968BDB0DEDFDC7A868EA4583BEC37224` |
| 1.7/Assemblies/DesignationsTooltip.dll | `DBA5BBDE432DD71D27BA3EB1F6335DCCAB25F7BD725F515DB2030B4B2A0AC2D9` |
| Inspected native 1.7.4995 rev309 Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

The installed inventory includes About, root MIT LICENSE, per-version DLLs and English keyed text;
no custom Defs, map components or saved gameplay state. Harmony is required; Core/DLC load-after
entries are hints, not evidence that every DLC is required. MIT copyright **2026 CheaterEater**;
retain the notice for substantial reuse. Nothing from this source is shipped by the document pass.
Local ignored evidence: `artifacts/Refinement/megamod-spec-20261102/designations-tooltip/`, containing
`upstream`, `source-17`, `game` and `consumers`. Workshop content was not edited.

## Source behavior and discrepancies

1. One Harmony transpiler changes `DesignationDragger.DraggerOnGUI`'s center area-number draw to a
   custom renderer. Numerator is `DragCells.Count` (accepted candidate cells); denominator is
   `CellBuffer.Count` (candidate shape cells). Classic mode renders only the numerator. It counts
   **cells**, not distinct Things, stacks, jobs, resource yield or eventual completed work.
2. The native outline and side-length measurements are retained. The source copies the native center
   position into a gray-backed white-text label; width grows for long text, but its left offset stays
   fixed at 20 pixels, so the larger box is not actually centered on the supplied position.
3. A second edit bypasses only the native size conditions when always-show-small is on. It retains
   nonempty candidate buffer and style `drawArea` checks. With the option off, each bounding side
   must be at least three cells. Native active-drag and non-single-cell checks remain in both modes.
4. **Zero discrepancy:** English help says the count hides when nothing can be designated, but the
   code gates on candidate buffer count, not accepted count. A nonempty area with no accepted cells
   can show **0 / Y** (Classic **0**). Proposed fidelity follows the code and corrects that explanation.
5. **Shape discrepancy:** comments call the denominator the enclosing rectangle. In inspected 1.7,
   the buffer comes from the selected drawing style; a hollow 5×5 rectangle has 16 edge candidates,
   not 25. Filled/empty ovals and angled lines likewise need their real candidate footprint.
   Straight Line has `drawArea=false`; small-drag mode does not override that policy.
6. Two global ModSettings fields: **classicMode=false**, **alwaysShowSmall=true**, saved under those
   exact keys. Changes are read during display; there is no restart-only patch switch. The source
   has no per-save override. Our required common settings model is an intentional enhancement.
7. Drawing resets GUI color/font/anchor to assumed defaults, rather than restoring the caller's
   previous values. Its catch logs once but has no guaranteed state restoration or native draw
   fallback. Proposed owned rendering restores state in all paths and preserves native fallback.
8. Startup uses unguarded PatchAll under **com.cheatereater.designationstooltip**. The count edit checks
   the first matching instruction window, not uniqueness/full data flow. Failure leaves original
   instructions and logs an error; the small-edit anchor can fail independently after count replacement.
   These are research facts, not an approved target validator. Owned admission must meet our stronger
   per-patch semantic contract while still allowing benign foreign edits.

## Native execution and refresh evidence

Read-only 1.7 types: `DesignationDragger`, `DesignatorManager`, `DrawStyleDef`,
`DrawStyle_FilledRectangle`, `DrawStyle_EmptyRectangle`; prior `Verse.Designator` analysis reused.
Core `Data/Core/Defs/DrawStyleDefs/DrawStyles.xml` supplies current drawing-style flags.

- Native `UpdateCellBuffer` generates candidate cells from the chosen style; it reuses the buffer
  for matching endpoints/style/game tick and removes duplicates when the style permits them.
  Filled rectangles use inclusive CellRect cells; empty rectangles use its edges. The generation
  itself is not clipped to the map. Rejected/out-of-map cells can therefore contribute to Y but not X
  when the selected designator correctly rejects them.
- `DragCells` updates the accepted list once per frame/style through `CanDesignateCell`. Native
  highlight updates are camera-clipped, whereas accepted drag cells are not merely visible highlights.
  Do not replace the numerator with the camera-visible count.
- The source reads Y before requesting DragCells; that second access can refresh the buffer. An
  owned display must read a consistent current candidate/accepted snapshot, not pair an old Y with
  a newly updated X. Paused pointer movement and same-frame tool/style changes need explicit proof.
- Mouse release passes the accepted list to `DesignateMultiCell`; native per-cell execution rechecks
  acceptance and tutorial/action constraints. A preview is not a guarantee that later work succeeds.
  Hovering must not commit orders, reveal hidden targets or bypass the tool's own rules.
- Source uses native lists, not a map-wide cache. Keep the same ownership and use direct composed
  candidate/acceptance data for our own designation tools; do not introduce another map scan/index.

## Actual external consumer and composition

Bounded installed Workshop/local scans of text/XML/C#/JSON and DLL literals found source files and
one external DLL: **Vehicle Map Framework**, package **OELS.VehicleMapFramework**, Workshop
**3426502333**, About **1.6.673**, installed supported versions **1.5 / 1.6**.
Its 1.6 DLL hash is **98C07524A064AB10FEA7E82547B06E75F41CAD0D9785911E1C5251F9DED9973D**.

Decompiled `VehicleMapFramework.ModCompat.DesignationsTooltip` publishes the source Harmony ID;
`VMF_HarmonyPatches.Patch_DesignationDragger_DraggerOnGUI` declares **HarmonyBefore** that ID.
Its transpiler converts map/screen coordinates and locates the native number-drawing calls before
the source replaces one. This is a real **patch-ordering contract**, not a direct call to the source
assembly or proof of a package-ID facade requirement. An owned patch with a different ID does not
automatically inherit that ordering. Do not spoof the old owner; investigate explicit ordering and
semantic admission for a supported provider version. No beta Vehicle Map Framework support is proven.

Allow Tool's inspected UnlimitedAreaDragger uses its own selection callbacks over native dragging;
its final action can differ from a simple per-cell designation. Our selected owned tools must supply
their actual candidate/accepted cell set to the shared preview directly. Do not assume all bespoke
tools become accurate from merely patching the native renderer. Unsupported external custom selection
semantics retain their own presentation instead of receiving a guessed affected count.

## Proposed scope and open work

Retain both source settings/defaults through common global defaults plus save overrides, in ordered
mod settings under interface/designation preview. Retain zero as useful feedback, actual shape-cell
counts, source size/style rules and contextual display. Correct text centering/state restoration,
stale-pair risk and unsupported-provider behavior. No source settings import or private gameplay
schema is needed. Original-provider coexistence selects one renderer and warns; source settings are
not silently imported into megamod.

- **DT-Q1:** exact beta drawing-style/tool lifecycle, current candidate/accepted snapshots, map-edge,
  paused and custom-owned-tool semantics. Confirm with native actions, not only these code excerpts.
- **DT-Q2:** minimal semantic target predicates, current incoming code, independent small-drag fallback,
  safe GUI failure and repeated/repatched initialization. No first-match or full-body-hash shortcut.
- **DT-Q3:** supported Vehicle Map Framework ordering/coordinate adapter and any newly found consumers;
  no automatic compatibility claim for the installed 1.6 provider in 1.7.
- **DT-Q4:** seven-language settings/UI, inherited/overridden saves, runtime rendering and measured
  large-drag overhead. No optimized-performance or live acceptance claim yet.

These gaps are recorded for later rather than interrupting the sequential specification pass.
