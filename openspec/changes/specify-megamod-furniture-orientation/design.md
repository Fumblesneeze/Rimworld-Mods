## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S048 source/native evidence is in
[research](../../../mods/megamod/docs/refinement/2026-11-03-auto-facing-chairs-research.md).
The user's same-cell manual-rotation requirement is explicit. The inspected source already avoids
repeating its own suggestion within a cell but only hooks build designators. This is a documentation
change, not a runtime repair or beta compatibility claim.

## Goals / Non-Goals

**Goals:** source seat/facility behavior, preserved manual input, consistent build/install/reinstall
placement, correct native commit, common settings and minimal semantic admission.

**Non-goals:** rotating completed furniture without a placement order, choosing a nearest target at
the expense of conflicts, granting facility bonuses, changing construction costs/eligibility,
reverse-rotating beds toward facilities, a new placement toolbar or source settings migration.

## Decisions

### One orientation policy for ordinary placement paths

Apply the owned policy to supported Designator_Build and Designator_Install paths, including a
reinstall of an existing minifiable object. Inspect exact native context rather than subclass-name
guesses for future providers. Use the actual placing ThingDef, map, cell, current rotation and item
identity. Eligible input is rotatable furniture with building properties; when Chairs only is true
also require isSittable. Default false retains source facilities as well as seats.

This deliberately extends the source's build-only hooks. Native Designator_Install uses the same
placingRot for preview, placement checks and PlaceBlueprintForInstall/Reinstall, but has a different
selected-item lifecycle and deselects after commitment. Keep existing item's identity, material,
quality, condition and style; don't create a replacement or alter native cancellation/transport work.
Build likewise retains its existing native resource, blueprint, construction and research behavior.

Do not add XML tags for eating surfaces: inspected source reads resolved metadata and its documented
ChairUsingFurniture.xml no longer exists. No Def mutation or XML Extensions dependency is needed.
No FSharp.Core dependency: independently implement the selected behavior in owning C# with native
contracts. No copying of upstream code/art/assemblies is selected; preserve source provenance and
resolve actual AGPL obligations separately if later code reuse is proposed.

### Preserve exact seat and facility meanings

Resolve each candidate built Thing/blueprint/frame to its intended ThingDef while retaining current
position and rotation. Seats first try the target's real chair-use interaction position, including
multiple offsets and interactionCellIconReverse. For that same target only, fall back to a cardinal
cell beside a SurfaceType.Eat occupied rectangle. Table-facing uses the chair placement anchor cell,
retaining source behavior; native validity still governs larger seat footprints and obstruction.
Corners, interior cells, distant tables or non-seat interaction markers yield no seat constraint.

For facilities, only placing a CompFacility next to an affected target whose linkableFacilities
contains it contributes. Compute the four native geometric rotation possibilities using the supported
Def/position/rotation contract. A zero result contributes nothing; it is not a global veto from an
unreachable target. For a rotatable target with several possibilities, prefer its rotation if allowed,
otherwise its opposite if allowed, otherwise retain the set. This is the source's visible preference,
not a new link-count optimization. Never restore the removed reverse facility relation that rotated
placed beds/affected furniture toward an existing facility.

Intersect every applicable target relation and seat constraint. Duplicate cells of one target count
once. With no constraints or an empty intersection, leave current rotation unchanged. Otherwise keep
current rotation if in the final set; if not, choose the first allowed North/East/South/West. This
fixed tie order is observable and specified. Do not choose whichever target was enumerated first.

The inspected facility overload proves geometry only, not LOS, medical designation, activity/power,
capacity/simultaneous limits or actual selected links. Preserve ordinary native link evaluation and
present no promise that a suggested orientation grants a bonus. Runtime checks and tests must cover
the distinction; silently replacing this with full actual-link ranking would change source behavior.

### Cell-entry suggestion and manual-input lifetime

Maintain one transient context per active placement session: designator/item/Def identity, map and
last valid hovered cell, plus the orientation input needed by that session. On a new valid map cell,
make at most one automatic suggestion before the native preview/placement path uses its orientation.
Remember that evaluation even when there was no target or a conflict. Repeated updates and subcell
mouse movement do not recompute or write rotation. Native keys, middle-click and rotation controls
remain the manual source; support remapped bindings rather than literal Q/E matching.

Native input event ordering must preserve a manual change made at cell entry too: when a manual
rotation and an automatic candidate occur in one frame, the player's change wins. A tiny scoped
native manual-input observation may be needed; do not infer manual intent solely from any field change
because providers and auto-facing also change that field. Prove the exact seam before implementation.
Repeated manual changes stay effective until a different valid map cell or genuinely new placement
session. The placement click consumes the shown rotation; it must not trigger a second snap.

No automatic changes while over interface, out of bounds or with no current map. An excursion through
UI/out of bounds does not count as a new valid map cell; returning to the same remembered cell retains
the manual choice. New map, new selected build/item, cancel/reselect or load establishes a new session.
Architect category/search/page browsing that leaves the native tool selected does not reset the
session, agreeing with S036. Cancel clears state; after actual install/reinstall deselection no stale
source-item reference survives. No saved cursor state or serialized live designator/Thing reference.

Do not fight native required attachment rules or foreign post-preview rotation indefinitely. Admission
must prove the effective path preserves the user contract for that supported placing context. A
context with incompatible mandatory orientation behavior retains its native/manual path and is
unavailable for owned assistance with a bounded diagnostic. An unrelated attachment branch does not
disqualify ordinary chairs whose execution never uses it.

### Query native/shared data without inventing gameplay range limits

Use current native ThingGrid/listers first. Source's sparse offsets and facility search square are
implementation heuristics, not selected gameplay-radius limits. Discover all relevant targets whose
occupied footprint or interaction positions can supply a relation to this cell; account for rotated
multicell targets and longer declared offsets/ranges. Use proven finite metadata and native spatial
queries; don't silently miss a relation because its anchor lies beyond an arbitrary source radius.

Compute only on a valid cell/session change, never every pixel/frame. A local seen set avoids duplicate
target work. Any immutable Def-derived geometry projection is generation-bound to finalized content;
compatible runtime/provider changes invalidate the shared projection before the next evaluation.
If existing shared spatial data is needed, use the foundation/S033 owners and freshness contracts,
not a second per-port Thing cache. Map removal/load cannot reuse prior map identities. No per-tick
world scan. Measure representative map/Def/query costs before claiming an optimization.

Native/current data correctness comes first. Unsupported nonfinite geometry or unavailable freshness
leaves that assistance unavailable with manual fallback; do not invent a small gameplay cutoff or
enqueue a delayed snap that overrides the player's newer rotation. Exact bounded lookup strategy and
large provider geometry remain AF-Q2 proof obligations.

### Common settings and owned-feature composition

One Chairs only row, built-in false, through the shared load/resolve/store/render path. Missing saves
inherit global. Show Global default, Current save and Use global; disable the save editor while
inherited and both save controls/callbacks at the main menu. Preserve dormant overrides. A completed
edit affects the next new cell/session evaluation, never causes a snap in the existing cell. Switching
saves resolves the loaded save and starts a fresh placement session; don't retain a previous save's
policy. Seven-language help explains that off includes supported facility orientation.

No added operational enable toggle, hotkey, bottom tab or debug command. Native Architect/Install/
Reinstall and rotation controls are the gameplay surface; persistent furniture orientation is native
entity state. The setting belongs in structured mod settings. S036 keeps the same selected command
through browsing; S028 replacement construction retains its authoritative material/validity rules.
S129 Interaction Spot On X is not fully specified yet: before joint activation define one directly
owned resolver for actual interaction positions/markers, then use it here. Do not patch our own feature
or force old static offsets after another selected feature changes their intended meaning.

### Per-patch target specs and owned-only fallback

Before implementation publish stable target documents and link observable requirements, validators,
tests and eventual evidence. The source's two raw hooks are candidates, not a predetermined design.
A selection observer needs the existing callback and reset/context contract. Any write to placingRot
requires proof of its effective data flow to preview, validity and corresponding build/install/
reinstall commitment, and preservation of subsequent manual input. Merely finding the field or
method is insufficient. Check exact callable shapes, native facility/interaction semantics and
relevant skip-original/foreign ordering without whole-method hashes or banning other owners.

Select the smallest coherent group for reset, suggestion and commitment consistency. Validate before
install, roll back only newly added owned hooks on false/unknown predicate, exception or installation
failure. No partially active path that shows one rotation then commits another. If transpiling is
necessary, validate actual incoming instructions on every run, build edits after full unique semantic
matches, preserve labels/exception regions and return incoming unchanged on failure. Never replay
foreign transpilers as a probe; define later repatch invalidation/revalidation.

Original `scarf.autofacingchairs` suppresses the owned orientation feature as a coherent whole,
including its install/reinstall extension, to avoid mixed competing providers. Native/source behavior
is retained, not guaranteed repaired. Other content mods are not conflicts by presence; use their
resolved native-compatible definitions. Optional executable adapters are package-ID/shape-guarded.
One bounded startup warning per skipped enabled patch names package/patch/spec ID, target, failed
predicate, unavailable assistance and ordinary fallback. Later unsupported content has bounded
per-cause diagnostics, not mouse-update spam. Store common settings even when assistance unavailable.

## Risks / Trade-offs

- Manual and automatic events may occur in the same frame → prove event ordering and manual priority;
  don't rely on a later corrective snap.
- Chair use and facility geometry can conflict → retain intersection/no-change behavior, not nearest
  target guessing. Actual gameplay usability remains native.
- Source's broad wording exceeds build-only hooks → explicitly support and separately test install/
  reinstall, preserving item identity and native logistics.
- Native/provider orientation constraints may differ → semantic per-context admission and native
  fallback, keeping harmless patches/content supported.
- Larger geometry defeats source search heuristics → correct shared/native query coverage and measured
  cost, no silent arbitrary cutoff or duplicate map index.

## Migration Plan

No source settings import or saved-object conversion. Common Chairs only starts inherited; native
placed-item/blueprint rotations remain unchanged on load. New placement contexts start fresh. Adding,
disabling or removing the feature does not sweep existing furniture or rewrite saved orientations.
Any future source-key import needs its own backup, atomic application, rollback and postcondition
contract. This draft changes no runtime package or user data.

## Open Questions

AF-Q1–4 retain exact native input/commit/admission, geometry/query/provider coverage, S129 and
settings/lifecycle composition, focused tests/localization/review/live proof. Source behavior and
the selected install extension are specified; beta/provider acceptance remains unclaimed.
