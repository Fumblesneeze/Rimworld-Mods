## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This is replacement design for later work. Read the [proposal](proposal.md),
[observable capability](specs/megamod-designation-preview/spec.md) and
[research journal](../../../mods/megamod/docs/refinement/2026-11-02-designations-tooltip-research.md).

Source `CheaterEater.DesignationsTooltip`, Workshop 3743959668, About 1.11 / installed 1.7 assembly
1.1.0.0, uses one transpiler on `Verse.DesignationDragger.DraggerOnGUI` and two global settings.
Pinned upstream commit 659085e856e8644d77445678dc61ae2ef2e245c0 includes the same DLL as installed;
the research journal records hashes, license and exact native/provider inputs. Native 1.7 shape
workers, not rectangle multiplication, determine CellBuffer. DragCells is the accepted subset.

## Goals / Non-Goals

**Goals:** accurate read-only cell feedback, source presentation preferences through the common
settings path, unchanged native actions, reliable UI state and explicit per-patch/provider admission.

**Non-goals:** object/job/yield estimation, resource reservation, new designation actions, source
package/owner spoofing, an independent map cache, whole-method GUI replacement or automatic source
settings migration. No implementation, package metadata change or game launch in this document slice.

## Decisions

### 1. Use native selection data and direct owned composition

Obtain a consistent candidate/accepted snapshot from the actual selected tool/style/map. Prefer the
native dragger's existing lists and lifecycle. The source reads CellBuffer.Count before DragCells
can refresh it; reverse/coordinate refresh through the supported native lifecycle so X and Y cannot
describe different frames or shapes. Investigate same-frame tool changes: native accepted-list
reuse keys frame/style, while candidate reuse keys endpoints/style/game tick. Do not paper over
staleness with a separate forever-cached count or force redundant whole-map scans.

X counts accepted cells, not Things or stackCount. Y counts unique candidate shape cells, including
rejected candidates supplied beyond map bounds; never derive it from width×height for a hollow or
oval style. Native highlights are camera-clipped and therefore are not the right numerator. Preserve
the tool's CanDesignateCell semantics and hidden-target policy; preview must not call designation
mutation methods. Actual release still passes through the native designation action and rechecks.

For our own Allow Tool-derived/custom tools, expose existing candidate/accepted selection data by
direct owned calls. Some original UnlimitedAreaDragger callbacks have their own final selection
semantics; merely hooking native GUI cannot establish their accuracy. Do not invent a universal
provider interface before a concrete consumer needs it. Unknown external custom semantics retain
provider presentation. Map/game teardown clears only our ephemeral display state; no saved live lists.

### 2. Retain source visibility, correct misleading explanations

Default text is X / Y, Classic text X. Show zero for a nonempty candidate footprint with no accepted
cells, consistent with installed code, and fix the source help's contradictory zero-hiding claim.
Default small-drag preference is true. When false, native gates require both bounding dimensions
at least three; when true, bypass only those size conditions. Preserve active dragging, non-single-cell,
nonempty candidates and `drawArea`. Thus native Line still suppresses the center count. No new
operational toggle or bottom tab is appropriate; the two choices configure contextual presentation.

### 3. Minimal semantic patches, with separate dependent support states

Candidate target documents to create/index before code:

| Proposed target ID | Consumed semantics | Rejection/fallback |
| --- | --- | --- |
| `megamod.designation-preview.count` | Unique center-area DrawNumberOnMap receives the current candidate count, correct screen position and dragger context; edge-length calls remain separate; accepted/current data is available safely | Leave count call unchanged; do not enable dependent small-size override |
| `megamod.designation-preview.small` | Explicit size-only guard feeds the same center count path; nonempty/style and drag lifecycle conditions remain enforced | Retain supported count replacement at ordinary native sizes; warn only for skipped enabled small support |
| Concrete optional coordinate/selection adapter, if needed | Canonical active provider, actual selection/transform types and members, required effective ordering and coordinate semantics | Disable only unsafe integration and retain provider UI/action; no guessed counts |

A narrow transpiler is a candidate because the native count draws in the middle of the method: a
postfix would draw twice, while a prefix replacing the whole method would duplicate unrelated native
outline/measurement logic. XML cannot express this runtime rendering behavior. Harmony therefore
becomes a required external dependency when implemented; no DLL bundling or unconditional PatchAll.

Admission must prove uniqueness, operands, stack/control-flow relationships and retained checks,
not match the first convenient four instructions. Validate the actual incoming stream on each
transpiler run; return it unchanged on rejection. Respect labels and exception blocks. Compatible
foreign edits are allowed. Do not require a pristine method, exact version/hash or an empty patch list.
Account for skip-original prefixes and unsupported later repatches. Do not execute foreign transpilers
as speculative probes. Record startup ordering and revalidation boundaries in each target document.

Separate the two support decisions without leaving an accidental partial count edit. Count replacement
is prerequisite to our size override; the size override can fail while count display remains useful.
If installed together, stage and commit each declared group atomically with rollback only of newly
owned patches. Repeated initialization must not duplicate the renderer or warnings. Each skipped
enabled patch gets package/patch/spec/target/reason/behavior/fallback in its bounded startup diagnostic.

### 4. Preserve transformed coordinates without impersonating the source

The actual consumer is installed **OELS.VehicleMapFramework**, Workshop 3426502333, version 1.6.673,
whose inspected DLL supports 1.5/1.6. Its DraggerOnGUI transpiler declares
`HarmonyBefore("com.cheatereater.designationstooltip")` and transforms native number positions before
the source replaces the center draw. This does not become an ordering dependency on our new owner.

Do not reuse the source owner ID. For a genuinely supported provider, determine its actual patch
owner and arrange explicit order through a narrow guarded integration; verify effective incoming
code and the native transformed-map action. A direct source-type facade would not satisfy an ordering
contract. The installed 1.6 consumer is evidence of a boundary, not beta support. DT-Q3 remains open.

Active original package `CheaterEater.DesignationsTooltip` selects the external renderer, disables
our overlapping groups and explains that our saved preferences are currently inactive. No automatic
unpatch, mod-list edit or source-settings mutation. Inactive installed directories are irrelevant.
Unknown custom providers are not rejected merely for existing; reject only unproven consumed
selection/coordinate semantics and retain their original UI/action.

### 5. Render once and restore the exact caller state

Use the native count position after supported coordinate conversion. Size the label for its entire
text and center by measured width rather than the source's constant short-label offset. Keep native
outline/side measurements and readable contrast; incidental pixel padding belongs to implementation,
not gameplay balance. Do not add source artwork.

Save GUI color and Text font/anchor and restore them in guaranteed cleanup, rather than setting
assumed defaults. Compute safe data/text before drawing where possible. A runtime failure must
retain/restore ordinary native/provider display without leaving global UI state altered, with one
bounded diagnostic per failure category/session. Establish an exact fallback before accepting the
renderer; swallowed exceptions with no count and damaged state are not successful compatibility.

### 6. Common settings and persistence

Proposed owned row IDs: `designationPreview.classicMode` (false) and
`designationPreview.alwaysShowSmall` (true), ordered in Interface / Designation preview. Use the
common loading, storage, effective-value and row-rendering path. Both use global defaults plus save
inheritance/overrides, including dormant override retention and main-menu mutation guards. Effect
timing is the next preview; patch installation is not repeated for preference changes. Active-save
world view can edit the save's preference even when there is no map preview.

Source keys `classicMode` / `alwaysShowSmall` live in global ModSettings only. No automatic import,
source assembly/type alias or custom gameplay migration is justified. Owned per-save preferences
live in common megamod settings; counts and selection lists are never saved. Translate controls,
scope labels/help and unavailable explanations in all seven baseline languages.

### 7. Evidence and performance boundaries

Host Unit tests cover pure count formatting, snapshot association and settings resolution only where
they test real rules rather than mirror constants. Owned Harmony tests use exact targets/patches and
guaranteed owner cleanup. Real active providers, native loaded tools/Defs and UI require fresh isolated
game runs. Do not mutate LoadedModManager to fake a loaded provider.

Final acceptance exercises native drag, shape change, cancel/release and settings controls on the
reviewed build and personally inspects before/action/after screenshots. Include paused motion,
zero/partial/large selection, offscreen/map-edge cells, long labels/UI scales, save/world/main-menu
settings and actual optional-provider cases before claiming those supported. Measure large-drag
cost on the normal path plus candidate updates; source reuse alone is not a performance result.

## Risks / Trade-offs

- **Cells can be mistaken for objects or job results** → Accurate explanatory text and multi-item,
  rejection and release scenarios; never broaden what the tool accepts.
- **Native frame/style cache can leave a stale pair** → Coordinate one current selection snapshot;
  verify paused endpoint and same-frame tool/style changes under DT-Q1.
- **Foreign coordinate patches depend on source owner order** → Real consumer documented; explicit
  canonical provider/order/shape checks under DT-Q3, no owner spoof or blanket rejection.
- **Small override accidentally bypasses more than size** → Separate dependent target and tests for
  drawArea, empty/single-cell/ended drag conditions, incoming changes and partial-support fallback.
- **Rendering exceptions leak state or double-draw** → Precompute where possible, one draw path,
  exact state cleanup and proven native/provider fallback under DT-Q2.

## Migration Plan

No deployment now. Later add the scoped implementation and required Harmony metadata in one tested
slice, using the canonical package build/install path and isolated saves. Default missing owned
preferences to Use global. Do not rewrite source configuration, normal saves or active mod lists.
Reverting the preview restores the prior drawing behavior; no designation or resource migration is
needed because this feature does not change those records. Common settings retain their own schema policy.

## Open Questions

- **DT-Q1:** exact beta style/tool/refresh and custom-owned selection behavior, including map edges.
- **DT-Q2:** semantic admission, incoming-code composition, rendering fallback and idempotent warnings.
- **DT-Q3:** supported transformed-map provider version, ordering and coordinate/selection contract.
- **DT-Q4:** seven-language/UI/settings lifecycle, native acceptance and measured large-drag overhead.

Record results in refinement as sequential work proceeds; these are not requests for the user to
explain the source mod, and do not block drafting other owned changes later.
