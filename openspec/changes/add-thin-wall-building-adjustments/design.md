## Context

Owner: **mods/ThinWalls**. The user explicitly requests optional shrink and offset gizmos. This resolves
the manual-policy question in add-thin-walls task 23.5 without promising that every sprite fits at 50%.

Read-only reference: installed Workshop 3346955193, package Mlie.PerspectiveBuildings, 1.6.0,
https://github.com/emipa606/PerspectiveBuildings . Inspected CompOffsetter, ResourceBank, Graphic_Print,
Graphic_DrawWorker and Graphic_Shadow_Print on 2026-09-16. Upstream cycles standard north/south 0.2-cell
offsets and supports four/eight-way custom presets. Its unchanged-footprint model is adopted, not its
implementation or icons. No source, binaries or assets are copied, and no upstream adapter is required.

## Decisions

- Two independent settings default on: show shrink gizmo; show offset gizmo. Hiding controls does not
  reset saved appearances. There is no tick worker, map-wide per-frame scan, or compulsory wall nearby.
- Completed player buildings with ordinary non-linked graphics are eligible, including mod-added Defs.
  Thin structures, regular walls/doors, frames and blueprints are not cosmetic targets. Excluding linked
  structures prevents separating connected structural geometry from collision/room boundaries. Custom
  rendering is not rejected just because it comes from a mod; paths bypassing native Graphic/section
  printing are an explicit compatibility limit, not claimed universally supported.
- Use per-Building weak state plus Building.ExposeData (two optional integer fields and a manual-offset flag) rather than mutate
  every loaded ThingDef. This keeps existing saves, minification, destruction and new maps scoped to the
  object lifecycle, with no global strong-reference registry. Missing/default fields yield native state.
- Scale is an integer step 0..5, not repeated floating subtraction. Offset is 0..8: center, N, NE, E,
  SE, S, SW, W, NW. Each nonzero component is 0.2 cells in fixed map axes, independent of rotation.
  These eight-direction presets are an explicit usability assumption so either wall orientation works.
- Automatic adjacency and manual gizmos use the same BuildingAppearance transform and offset steps.
  The transform is applied around native TrueCenter to the completed object's own geometry; scale
  precedes translation. Remove the old DrawPos/TrueCenter clearance patches and sprite-alpha measurement
  path. Returning the offset to center is an explicit player choice, not a second rendering policy.
  It never changes Position, Size,
  interaction cells, reservations, pathing, selection footprint, rooms, cost, work or hit points.
- Preserve native print/realtime materials, UVs, styles, tints, and renderer behavior. Transform only
  geometry contributed by the target to cached section meshes, including native ground shadows and
  damage. Realtime rendering owns a scope around the nonvirtual Thing.DynamicDrawPhase/DrawNowAt
  dispatch, not just the primary Graphic: body, shadow, Comp DrawAt/PostDraw, property-block meshes,
  and direct component Graphics.DrawMesh submissions share the owner pivot. Transform both independent
  managed terminal DrawMesh matrix overloads immediately before native submission; preserve all other
  arguments, including property blocks and flags, and never mutate shared Graphic state. Nested Thing
  draws suspend the parent; Graphic.Draw and base DrawWorker recognize explicit different owners,
  while ownerless component graphics inherit. All scopes restore in finalizers, including exceptions.
  PawnRenderer entry points also suspend this transform for actual occupants drawn directly by vats,
  pods and containers. Native StatueColor marks decorative fake-pawn artwork, which stays attached to
  the building's transform rather than leaving a full-size statue figure above a shrunken pedestal.
  Unadjusted objects take a cheap lookup/early exit and allocate no state.
  Native edifice sun shadows are a separate path: replace only manually adjusted owners with a scaled,
  translated rectangular footprint and proportionally scaled cast height, retaining native sun shader
  and other owners. Partition footprints across intersecting native sections, with no shadow-casting
  faces on internal partition seams. Reopen the native buffer without discarding its geometry before
  uploading additions. Dirty Buildings along with Things/Damage on change. Packed/unspawned inner Things
  retain state but use native packed rendering, never a transform about their obsolete map position.
- Separate objects (stored items, meals, pawns sitting/sleeping) are not transformed: this is deliberately
  cosmetic, not a furniture-and-contents repositioning system. Tooltips describe this limit. Perspective
  may coexist without a hard reference; its rendering is an input to this mod's final transform.
  Direct DrawMesh users no longer have to pass through Graphic.DrawMeshInt. Rendering outside a Thing
  draw scope or through other APIs (procedural/instanced draws, RenderMesh, independent batched drawing)
  remains an explicit compatibility limit. This does not claim to own every arbitrary mod's renderer.
  Decorative content representations submitted with the building as their owner, such as Core
  bookcase book-spine displays, count as attached furniture artwork and follow its adjustment. This
  does not move, shrink, or otherwise change the independent stored Thing or its gameplay data.

### Unified automatic offset revision (2026-09-27)

- Resolve perimeter contacts once at completed-building spawn/claim and when an incident Thin edge is
  built, planned, removed or cancelled, not in a render/tick loop. Inspect only that footprint or the
  changed edge's two owner cells. Keep no full-map building index or sprite-clearance cache.
  Core Claim changes faction without emitting BuildingSpawned: a narrow Building.SetFaction postfix
  refreshes that same building. Losing eligibility clears only automatic offsets; manual choices persist.
- Any Thin wall/door structure (including blueprint/frame, matching previous adjacency scope) on a
  side counts once. Opposing sides cancel that axis. N+E yields SW (step 6); N+E+W yields S (step 5).
  All rotations, a fully enclosed cell and rectangular footprints follow this rule; no contacts or
  fully balanced contacts yield center. Magnitude is exactly the existing 0.2-cell per-axis preset,
  irrespective of sprite alpha. This is not a guarantee that every sprite fits; Shrink remains manual.
- Offset gizmo use marks the offset manual, even at center. Automatic refresh never replaces that
  choice. Shrink is independent and does not freeze the automatic offset. No new Auto mode/control.
- Persist the manual flag. For old saves without it, nonzero saved offsets mean manual; zero means
  automatic (old saves cannot distinguish a historical explicit-center choice). Preserve scale.
  On load/reinstall, recompute automatic adjacency; preserve manual choices. Packed objects stay
  untransformed. Construction ghosts/frames now retain native rendering, because the unified cosmetic
  system intentionally targets only eligible completed player buildings.
- User has not authorized game interaction yet: implement/review/build locally only; defer deployment
  and native construction/gizmo/save-load verification until explicit continuation.

The user's cryptosleep-casket report invalidated the earlier primary-Graphic-only visual acceptance.
CompEmptyStateGraphic.PostDraw submits the open-door texture directly through Graphics.DrawMesh.
The regression test records actual terminal mesh matrices after native gizmo actions, checking every
vertex of body, shadow and component door in all four rotations against the same owner transform.
It also verifies nested independent Thing draw suppression and exception restoration. No per-casket
production branch is permitted.

## Verification

### Appearance-control icon revision

Keep the user's no-owned-textures policy: compose Core's complete `UI/Overlays/Arrow`
(64x64, white fill and dark outline, alpha>=128 bounds x2..61/y1..63) with
`BaseContent.WhiteTex` rectangles at GUI time. Do not
generate, package or read back raster pixels. Core `Command` is 75x75 logical pixels, with a
0.85 icon fit and top-right state badge; reserve y=0..21 for that badge. Native labels are placed
at button.yMax - textHeight + 12; observed single-line English labels start at about y72.
The new symbol occupies x=10.5..64.5, y=22..68, retaining four pixels to that label. The initial
40px-high canvas wasted usable space and made small-size edges less legible. The measured label
placement permits 46px-high art for the normal font. Measure actual native label height and badge
line height each draw: proportionally reduce and center the symbol if Tiny text is unavailable
or a label wraps, retaining two pixels below the badge and four above the label. Shrink uses two
complete inward diagonal arrows. Offset uses one rightward positioning arrow alongside one
neutral object. Do not imply resizing through four outward arrows, or state changes through
two differently filled boxes; keep the object and translation direction visually separate.
Warm gold versus cool blue supplements,
but never substitutes for, the distinct silhouettes. Use dark contours from the native arrow.
Earlier candidates were rejected: competing brackets/crosses obscured identity; small separately
composed arrowheads/stems looked fragmentary; a bidirectional arrow implied swapping; source/destination
boxes implied changing an object's fill or copying it. Complete
native arrows avoid the construction seams and keep the silhouette coherent at small sizes.
Compare candidates at actual UI size; retain
native screenshots and crops without label/context for independent review. Every icon must
receive at least 8/10 and be interpreted correctly by a blind reviewer. Review 1x and a second
supported UI scale; no map-material, damage, or cardinal sprite family is relevant to UI art.
Retain translated text, tooltips, state badges, native disabled/low-light treatment and actions.
Mechanically gate symbol bounds/state-label clearance, inward shrink direction, offset object/arrow alignment,
and actual command wiring. No per-frame texture allocations or shared texture mutation.

One RED/GREEN behavior at a time: exact resize wrap, offset wrap/bounds, transform composition and
defaults; then loaded/native gizmo cycles, static and realtime graphics, materials, four rotations,
compact-U shelf, damage/shadows, hidden settings, save/load, unchanged footprint/interaction cells and
unchanged neighboring instance. Review before final fresh minimized Gateway acceptance. Retain raw
before/action/after renders and build identity. No Workshop capture is promoted by this feature.

New control/settings catalogs are contextually authored for the seven repository locales. The settings
category retains the existing localized product-name catalog; percentages and compass abbreviations are intentional compact UI tokens.
The vocabulary distinguishes visual size/offset from occupied cells and interaction positions. Validate
the exact 16-key inventory and inspect the new settings/gizmo surface in each isolated locale; this
feature adds no job reports, alerts, recipes, Def labels, or info-card fields to localize.
