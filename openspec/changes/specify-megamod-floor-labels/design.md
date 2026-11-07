## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S093/P2-22. [Research](../../../mods/megamod/docs/refinement/2026-11-05-floor-labels-research.md)
pins the installed 1.7.5.0 DLL, older public source, native 1.7.4998 contracts and optional screenshot
source. This is a future implementation design, not patch admission or live compatibility evidence.

The continuation's selected binary contains substantial behavior absent from its public 1.6.4.0
source: unified appearance editing, orientation/travel fields, CJK fallback, overlap modes and
gravship support. The older source already persists color, visibility and offset overrides.
Use the selected binary for current behavior. Do not transplant its duplicate
settings library, global static lifecycle, unguarded PatchAll or inconsistent missing-field defaults.

## Goals / Non-Goals

**Goals:** readable automatic/contextual labels, deliberate edits, coherent native zone naming,
bounded current geometry/text resources, ordinary lifecycle persistence and narrow optional adapters.

**Non-goals:** changing room role calculation, storage/group membership, crop selection, pawn jobs,
zone travel semantics, automatic renaming for gameplay decisions, a new bottom tab, full Unicode
coverage, a general font plugin framework, importing source saves or implementing Progress Renderer.

## Decisions

### Separate primary label records, transient editor drafts and common preferences

Store primary customizations with the game: map plus room anchor and stable creation ordinal, or
native zone identity, nullable text/color/show/direction/offset choices, and zone-name provenance,
including the last applied/observed native name for reactivation reconciliation.
Serialize override presence independently of its value: Color.white cannot mean both Default and
explicit white. Resolve references after load and retain valid in-transit records. No live Room,
mesh or Material reference is durable save state. Native zone IDs are allocated game-wide by
UniqueIDsManager; still bind records to their owning game/map and explicit transfer lifecycle.

Do not create records merely by opening the editor. Draft state is isolated; optional visual preview
overlays the record without modifying it or Zone.label. On Apply validate target identity/revision,
text, bounds and native name, then commit the whole edit with rollback on failure. All close routes
discard. Switching between zone and containing room discards the first draft explicitly. A stale
target cannot silently overwrite a native rename. The source's direct preview mutation plus
PostClose refresh is rejected because close-X/outside dismissal can retain unconfirmed changes.

Use native IRenameable for committed zone names. Remember the native name before the first owned
custom-name application; ordinary external rename while customized becomes both the current name
and the reset baseline. Guard against treating our own commit/reset as an external rename. A
native-rename observation must establish the actual resulting value, not merely that a button was
drawn. Appearance-only edits do not touch native names. Clearing custom text restores the baseline
and resumes automatic crop/name display. This corrects stale source custom-name precedence after
an ordinary native rename. A room label never alters native Room.Role.

Room anchors intentionally survive temporary reconstruction. Merge conflicts use persisted creation
order, not changing native room IDs, shuffled cell order or dictionary iteration. Retain dormant
records for later splits; reset of a merged room removes all records whose anchors currently belong
to it. Actual map disposal discards its records except those already bound to a live gravship. Do
not attach a zone record to a new zone by matching its name or coordinates.

All presentation preferences use the common definition/resolver/serializer/UI path. The bottom-right
mode control writes the active save's explicit display-mode override, not source-style global
WriteSettings. The settings row shows that same state and Use global restores inheritance. This
avoids a second independent visibility field and needs no settings-scope exemption. Entity records
are contextual state and never receive Use global checkboxes. Default per-entity appearance means
the feature's current effective presentation preference, not a copied value.

Minimum/maximum scale is one validated compound setting: two value controls, one inheritance bit
and one dormant override pair. Separate inherited scalar rows could produce an invalid effective
pair from individually valid scopes after a global edit. Validate the pair atomically on every
load/edit/reset; never repair a global/save mixture by silently rewriting an unrelated override.

### Build labels from native owners and cache only derived presentation

Read current native room/zone collections. RoomRoleFinder behavior establishes the automatic
meaningful-room boundary; no private Thing list or per-tick world scan is justified. If placement
needs a shared derived building fact already maintained by other owned consumers, reuse it; keep
native room/zone ownership authoritative. Primary customizations are not a duplicate entity cache.

A map-owned presentation cache keys text, font/resource generation, language, geometry generation,
relevant furnishings/role/crop/name, customization revision and placement preferences. Count alone
does not identify a shape. Coalesce invalidations from native lifecycle events and direct owned
zone/Architect edits. Avoid clearing every frame while hidden. Release game/map references at
teardown, destroy owned meshes on replacement and unload, and use native material ownership rules
without destroying shared MaterialPool resources. Bound retained text/material combinations to
current demand; repeatedly typing arbitrary draft names cannot grow a permanent global cache.

Classic uses contiguous usable edge runs, with the source's bottom/left and horizontal-tie intent.
Smart evaluates valid candidates in the area with clearer room space favored; bedroom bed feet,
hospital doors and prison doors retain the documented preference directions. Source 5x5 chunk
scoring is evidence, not a required algorithm: its bounding-box fit and incomplete invalidation
can place text through holes or retain stale results. Validate the full transformed footprint,
including glyph height, map/fog and minimum scale. Recompute after customization offsets before
overlap and viewport tests. A manual offset can leave its source area deliberately, but cannot
leave the map or reveal fog. Forced orientation is a hard choice; do not retry Auto after failure.

Room-zone overlap filtering acts on final rendered footprints and only on otherwise visible labels.
No O(rooms times all things) scan or broad gameplay index is necessary. Viewport culling tests the
footprint, not just its starting cell. Benchmark label-heavy maps and repeated edits before claiming
optimization; retained memory, invalidation cost and actual rendered correctness all matter.

### Fonts are presentation resources, not a reason to preserve unconsumed public APIs

Source discovers Fonts/<name>/Font textures across all running mods and finds Atlas.json separately;
its public RegisterFont/IFont and legacy font classes have no established external callable consumer
in the bounded installed scan. An asset filename scan found only the source's six atlas providers.
No speculative assembly/type facade or generic folder scan is selected. Own the six documented
style roles with licensed resources and stable setting IDs; inspect any actual future font provider
before adding a package/shape-gated adapter. Bind texture, mapping and metadata from one admitted
provider rather than accidentally combining identically named files from different mods.

The source handles chars, drops unsupported glyphs, and its 16x16 mapping omits some letters such as
Russian Yo despite covering the main Cyrillic block. Use Unicode text-element-aware editing and
explicit readable fallback/missing markers without corrupting saved names. Inspect actual baseline
language labels plus supported CJK samples. Do not equate the source's CJK manifest with complete
Unicode support. Font/style fallback is separate from language translation.

The selected source's Noto CJK pages cite SIL OFL 1.1; the older public code has GPL-3.0. No asset or
code is selected for reuse here. Before selecting/creating any player-visible raster, follow the
asset skill, license/attribution inventory and its independent live visual gate. Font previews and
actual world text require review at materially different zooms, colors/backgrounds, rotations and
scripts. The promised observable styles need not preserve unconsumed source asset filenames.

### Optional native travel and screenshot observation

Native Gravship.TransferZones snapshots stockpile/growing zones into MoveableStockpile/MoveableGrowZone,
deletes the source zone if any ship cells were included, and recreates it with the same ID on landing.
Do not infer a surviving departure remainder. Bind only actual native transfers, preserve identity
through flight/load, and commit room-anchor translation using native rotation/local-position rules
after successful landing. Orientation and offset preferences remain map-relative; automatic placement
uses the new destination geometry. On cancelled/failed transfer, restore source ownership; never
pre-emptively orphan records just because a prefix ran. Any unresolved transfer record stays dormant
with a bounded explanation rather than binding by coincident coordinates. This behavior group needs
atomic admission across departure, persistence, successful landing and failure restoration.

Odyssey is optional. Rendering on a space map uses native map-view detection, not a check that the
planet renderer is absent. Hide during actual gravship cutscenes. Other DLCs are not dependencies.

Native UIRoot.screenshotMode.Active is a read-only visibility input. For an optional external capture,
the inspected archived Progress Renderer source identifies canonical neptimus7.progressrenderer and
ProgressRenderer.MapComponent_RenderManager.Rendering, a public instance bool getter which is true
during its rendering coroutine. It temporarily switches to the rendered map. Read the component of
the map being drawn; do not initiate capture or alter the provider's camera/settings. The archived
snapshot advertises through 1.6 and is not proof of current beta support. Its adapter remains subject
to exact installed-provider inspection and native acceptance (LFL-Q3).

Source aliases Jaxe.Progress.Renderer, Mlie.ProgressRenderer and community.progress.renderer are only
strings in its detector, not proven providers. Do not advertise them. Package resolution is canonical
and case-insensitive, then verify the consumed type, map ownership, getter and active-capture meaning.
Missing provider is normal; changed/throwing present-provider adapter disables only that integration
with one bounded diagnostic. No reflection or exception-log loop per frame. Continue native hiding.

### Candidate target-spec groups and minimal admission

The following are research seams, not installed patches or completed target documents. At coding,
register one stable target document and executable predicate for **each actual patch**, including
every selected invalidation member. Prefer owned callbacks/direct composition where available.

| Candidate ID/group | Inspected seam | Minimal semantic obligation |
| --- | --- | --- |
| LFL-DRAW | DynamicDrawManager.DrawDynamicThings or a narrower native map overlay callback | Correct map/view and valid drawing phase; do not suppress native draw, run twice or draw in another map; account for skip-original behavior if using an original-body callback |
| LFL-INPUT | MapInterface.HandleLowPriorityInput | Available unconsumed map event, typing/selection guards and successful action before event consumption; preserve native input |
| LFL-ORDERS | DesignationCategoryDef.ResolvedAllowedDesignators getter | Real Orders category and enumerable result; idempotently append one contextual action, retaining foreign/native entries |
| LFL-ROOM-* | Room.MakeNew, Notify_RoomShapeChanged, Notify_BedTypeChanged, RemoveDistrict; relevant ListerBuildings and GatherSpotLister changes; Building_Bed.FacilityChanged | Actual affected map and resulting room/geometry/role/furnishing change available; simple existence/callable shape suffices only for pure invalidation with independently authoritative re-read |
| LFL-ZONE-* | Zone.AddCell/RemoveCell, ZoneManager.RegisterZone/DeregisterZone, Zone_Growing.SetPlantDefToGrow, actual native rename path | Distinguish successful resulting edit/deletion from no-op/failure; resulting identity/name/shape; read-after-change rather than assume argument use |
| LFL-LIFECYCLE-* | Map/World finalize, play-data clear, language/resource change and teardown | Current game identity, post-crossref readiness and owned resource disposal; avoid broad static state or porting unused legacy migration hooks |
| LFL-SHIP-* | Gravship.TransferZones, MoveableStockpile/GrowZone creation and GravshipPlacementUtility.PlaceGravshipInMap | Exact native snapshot/recreate identity, ship-cell/rotation semantics, original/effective transfer actually executed, success boundary and failure rollback; signature alone insufficient |
| LFL-SCREENSHOT | Native mode plus admitted optional provider property | Read-only current-map capture state; no Harmony hook needed if direct observation is sufficient |

Rendering, primary-state lifecycle and required freshness hooks form the core admission group. R and
Orders entry points can be admitted independently while retaining an available alternative. Travel
is a separate atomic optional group. An incomplete travel adapter does not silently guess record
ownership; suspend unresolved travel-dependent labels while retaining their intent. Optional capture
failure does not disable ordinary labels. Saved data is not deleted on admission failure.

Validate effective execution and relevant prefix/transpiler interference, not only original IL.
If a chosen integration changes an argument, require the actual consumed dataflow; if a transpiler
is necessary, validate every actual incoming stream and return it unchanged on failure. Never
speculatively rerun foreign transpilers. Benign other owners and unrelated edits remain admissible.
Revalidate supported later repatches; false/unknown/exception/install failure skips the declared
group and rolls back only newly installed owned patches. Each skipped enabled patch emits the
required bounded startup package/patch/spec/target/reason/behavior/fallback warning. Hashes below
identify inspected evidence only. Do not copy source PatchAll or its static Harmony owner.

No declarative gameplay Def change is required. Own keybinding definitions can be additive; any
later XML patch requires XML Extensions existence/content/node guards, package-ID checks for external
content, merge preservation and its hard dependency at first use. Compose S036 Architect placement,
owned storage/growing zone updates and common UI directly, never with patches to our own feature.

## Risks / Trade-offs

- **Anchor-based room identity is visible after merges/rebuilds** → explicit deterministic policy,
  dormant records and merged reset scenarios; do not claim native rooms have stable saved identities.
- **Smart placement differs from the source's pixel layout** → preserve useful intent, document
  deliberate geometry/fog/forced-direction fixes and visually verify fixtures, not exact screenshots.
- **Font licensing and script coverage vary** → select resources only at implementation with measured
  glyph coverage and required asset acceptance; unsupported text stays stored and visibly marked.
- **Travel and optional capture callbacks can change** → narrow semantic admission and separate
  failure domains; offline analysis does not establish a working provider combination.
- **Settings and entity-name edits can overlap** → explicit scopes and version-checked whole edits;
  no global writes from contextual controls or native name restoration from stale drafts.

## Migration Plan

Owned records use a versioned schema with safe missing/invalid-field defaults. No source state is
automatically consumed. Source WorldComponents, its old WorldObject manager, source global keys and
public font APIs are not impersonated. A future importer requires its own backed-up atomic migration,
failure restoration and verified postconditions. Original falconne.labelsonfloor presence suppresses
all owned overlap, including name observers, retaining dormant owned records without writing source
data. Before reactivation, resolve each actual native zone. For records with an active custom-name
override, compare its current name to the last owned applied/observed value. A changed current name
becomes the custom name and reset baseline; keep other appearance fields. Appearance-only records
retain absent custom text and automatic crop/name behavior; never create a text override or reset
baseline merely because a native name changed. This is reconciliation of native game state, not source component
import. Do not replay the old name into the zone or bind a missing record by matching coordinates.
No safe-removal or source-save compatibility claim is made by this draft.

## Open Questions

LFL-Q1: exact effective hooks, native rename transaction boundaries, geometry/role invalidation and
language/map/resource cleanup on the implementation build. LFL-Q2: deterministic anchor, merge,
deletion and failed/in-transit transfer fixtures. LFL-Q3: exact optional screenshot provider and any
actual external font/API consumer. LFL-Q4: licensed fonts, glyph coverage, editor usability, settings
scope and seven-language/live visual review. LFL-Q5: focused TDD, causal native acceptance, bounded
memory/update measurements and final playability review. These remain recorded research, not questions
which pause the sequential specification work.
