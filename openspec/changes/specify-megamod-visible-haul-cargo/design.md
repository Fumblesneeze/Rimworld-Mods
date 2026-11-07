## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S126 selects the visible-cargo subset of **dd.Armful Haul**, `dd.armfulhaul`, Workshop 3808100512,
version 0.4.2. Specification only. [Research](../../../mods/megamod/docs/refinement/2026-11-03-armful-haul-research.md)
records documentation-first inspection of all 17 installed source files and selected 1.7 binary
bodies, exact hashes, unresolved license, failure boundaries and broader comparison-only features.

Armful physically expands `Pawn_CarryTracker.innerContainer`, draws its additional Things with
`PawnRenderNodeWorker_Carried.PostDraw`, adds per-stack selection gizmos and replaces a localized
substring in pawn inspect text. Its independent per-stack native allowance conflicts with our
aggregate capacity decision. Drawing must be separable from that storage representation.

## Goals / Non-Goals

**Goals:** visible truthful moving cargo, exact contextual quantities, no duplicate native picture,
personal/cargo distinction, coherent view preferences and failure isolation from physical hauling.

**Non-goals:** implement now; port Armful job wrappers, multipliers, DSU/floor caches or prison-food
commands; adopt an entire hauling fork; copy code/art; migrate upstream active jobs/settings;
promise arbitrary external renderers or save formats. PH-Q5 still owns the final hauling design.

## Decisions

### Read physical cargo through the owning hauling feature

S021 owns cargo attribution, movement, persistence and capacity. S126 consumes an owned read-only
view of actual holder/Thing identity, hauled quantity, personal quantity and recovery status.
Derive one entry per physical stack containing positive cargo, not one per reservation or planned
pickup. A mixed stack's presentation uses its cargo quantity without splitting or modifying the
Thing. Never alter stackCount temporarily during drawing. If native icon/render APIs cannot show
the portion safely, use the item's normal recognizable graphic and exact labeled UI quantities;
do not pretend the full mixed quantity is deliverable cargo.

The actual cargo container may be hands or inventory after PH-Q5. Rendering additional carried
cargo must not require multiple full hand allowances. Track native primary representation by actual
Thing identity, so the same item is not drawn twice. Retained recovery goods remain readable even
if the active job changes. No map-wide Thing scan is needed for the selected pawn's held list.
Use native/owned held state and the common invalidation lifecycle; any genuinely shared map facts
come from the Ctrl+F/hauling index, not a new display cache. No independently saved item copies.

### Adapt native graphics without owning the job

Use the current beta carried-render boundary and native item graphics where shape checks permit.
Read actual material/color/stack state; leave quality/condition to ordinary info when not encoded
in the graphic. Compute a compact layout from current pawn/cargo bounds. Source constants
0.12/0.45 and spread 0.1–10 are comparators, not selected offsets or an exposed setting.
Do not add a speculative layout framework or copy source textures. Resolve exact placement with
representative native captures, including many light stacks and nonhuman workers. For native
carried pawns/corpses, preserve native rendering and physical semantics; omit conflicting extras
while retaining contextual cargo information. Any art selection/change follows the asset skill.

### One compact contextual cargo control

Preserve native carried-item selection and other commands. Add **Hauled cargo**, a contextual
scrollable read-only list with each physical cargo stack and ordinary info access. This deliberately
avoids Armful's additional gizmo per stack, which can overcrowd the bar for small-stack cargo under
our non-slot-multiplied hauling design. Same-looking Things retain distinct identity; never group
them solely by label/Def. Revalidate ownership on activation so a stale row cannot select another
item. The summary must use an owned additive text/control seam, not replace a translated sentence.
It reports current holdings, not promised future delivery sequence. Entity state stays outside
mod settings. Future S127/S132 HUD/inventory designs must share these facts and avoid duplicate UI;
this change does not claim their research or binary compatibility.

### A single view preference follows common settings

Stable proposed setting ID `hauling.cargoDisplay.showExtraStacks`, built-in true, section
Hauling → Cargo display. Use the foundation's common load/store/resolve/render path. Bottom-right
toggle writes the same current-save override and switches inheritance off; it never writes a
second operational state or global default. Settings restore inheritance/dormant overrides.
Apply immediately at the next visible update; no refresh job or physical transfer. Disable current
save callbacks at main menu. Keep preference distinct from admission: unsupported rendering does
not silently rewrite a saved true to false. No separate spacing option initially. Diagnostic tracing,
if later needed, belongs in the top debug controls, hidden outside developer mode.

### Target specs and co-installation before hooks

Future candidate seams, not accepted target contracts:

| Purpose | Candidate target / minimum evidence needed |
| --- | --- |
| Additional carried pictures | PawnRenderNodeWorker_Carried.PostDraw; callable draw parameters plus effective native primary-render/position semantics so extras do not duplicate or hide ordinary carrying |
| Contextual control | Pawn_CarryTracker.GetGizmos or the owning pawn command surface; valid owner/current-state access and preservation of all native/foreign commands |
| Cargo summary | Pawn.GetInspectString or supported native inspect contribution; additive result semantics without relying on translated substring content |
| Refresh | Prefer existing owned cargo lifecycle; only add a hook where actual mutation invalidation is missing and document that callback's required semantics |

Write a stable per-patch target document and executable minimal semantic admission check before
implementation. Observe effective code and relevant skip-original/foreign-transpiler behavior;
do not reject merely because another owner patched the target. No PatchAll, binary hash gate or
exact whole-method equality. Validate actual incoming transpiler streams if one becomes necessary;
return unchanged on failure and roll back only newly installed owned members of a dependent group.
Separate drawing and contextual groups when they can fail independently. Warn once per skipped
enabled patch at startup with ID/target/predicate/unavailable behavior/fallback. Explicit view off
is not a failed candidate. Later invalidation follows the foundation's revalidation rule.

Harmony is required when these seams are implemented. No XML mutation is currently needed; if
one becomes necessary, use XML Extensions content/shape guards and declare that dependency.
Native graphics are referenced, not bundled. Gateway remains development-only.

Known `dd.armfulhaul` active: withhold the overlapping owned carried-stack drawing/inspection
enhancement and explain provider precedence, without unpatching it. S021 separately controls
collection coexistence and recovery. Unknown renderers need capability evidence, not an automatic
ban on every foreign Harmony owner. Optional adapters require canonical package IDs and callable
shapes, absence safety and bounded failure. No broad reflective duck typing or package spoofing.
Potential source API/save consumers remain AH-Q3; none is promised by this visual subset.

## Risks / Trade-offs

- Different storage models or mixed stacks → read exact owned quantities; never manipulate physical
  storage just for display. PH-Q5 must settle the integration before coding.
- Large/many/custom graphics and varied bodies → adaptive compact pile plus exact scrollable
  details; actual native multi-zoom/direction evidence is mandatory, not a generated mockup.
- Failed drops or source-provider changes → presentation follows actual surviving cargo, with
  recovery/admission status distinct from the saved preference. Source hand-only visibility is
  insufficient for goods moved into inventory.
- New contextual list differs from per-stack source gizmos → preserves individual access and
  ordinary selection while keeping the command bar usable; document this intentional deviation.
- Cost of rendering many cargo stacks → measure active hauling and UI refresh; avoid per-frame
  map scans. Do not claim a performance improvement from bounded source loops alone.

## Migration Plan

No runtime deployment or migration in this documentation change. Future first use defaults to
inherited visible cargo. Load existing owned cargo from its owner; persist only the common view
preference here. Source jobs, settings keys and assemblies are not imported. Removing/withholding
this display must leave physical cargo and the owner's recovery intact. Any later migration needs
an explicit separate contract with backup, atomic application and failure restoration.

## Open Questions

AH-Q1–4 are retained in research and the progress ledger: final hauling composition, exact native
visual layout/worker support, actual optional consumers/semantic targets, and native acceptance.
Initial adaptive layout and compact list are explicit proposed defaults. No question blocks
continued research of the next source; none is silently treated as a verified implementation.
