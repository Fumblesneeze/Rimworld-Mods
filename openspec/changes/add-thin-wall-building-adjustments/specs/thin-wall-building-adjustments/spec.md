## ADDED Requirements

### Requirement: Optional per-building visual controls
Owner: **mods/ThinWalls**. Eligible completed player buildings SHALL expose separate localized Shrink
and Offset gizmos, controlled by independently saved mod settings defaulting on. They SHALL work without
Perspective installed and without requiring a neighboring Thin Wall. Hiding a gizmo SHALL preserve its
building's saved state. Linked structures and doors SHALL not expose these cosmetic controls.

#### Scenario: Exact shrink cycle
- **WHEN** the player clicks Shrink six times on a default eligible building
- **THEN** its visible scale is successively 90%, 80%, 70%, 60%, 50%, 100%
- **AND** its neighbor of the same Def is unchanged

#### Scenario: Reversible offset
- **WHEN** the player clicks Offset nine times
- **THEN** it cycles N, NE, E, SE, S, SW, W, NW at 0.2-cell axis displacement, then centered
- **AND** offset is independent of scale and uses fixed map axes for every rotation

#### Scenario: Hide without losing adjustments
- **WHEN** the player disables one control's visibility in Thin Walls settings
- **THEN** that gizmo is absent, the other respects its own setting, and existing adjustments remain
- **AND** enabling visibility restores access without resetting the building

### Requirement: Saved cosmetic state preserves gameplay and native materials
The selected adjustment SHALL survive native save/load and minification on the same building. Automatic
adjacency and explicit offsets SHALL use only the same preset transform, scaling about the native center
before translating. Explicit offset choices, including center, SHALL override automatic choices independently of scale. Native section geometry, damage,
ground shadows and standard realtime Graphic meshes SHALL consume the same transform. Shared Graphic
objects, materials, UVs, and other buildings SHALL not be mutated. No new gameplay textures SHALL ship.
Footprint, interaction cells, collision, rooms, hit points, work and material costs SHALL remain native.
Stored items and pawns are separate objects and SHALL not be silently transformed. Decorative display
representations submitted as building-owned artwork (such as bookcase spines or statue figures) SHALL
follow the furniture; the independent stored Things and actual occupants SHALL remain unchanged.

#### Scenario: Compact enclosure remains usable
- **WHEN** the player adjusts a small shelf inside a legal single-cell Thin-wall U
- **THEN** scale and offset change its rendering without moving its cell or changing storage eligibility
- **AND** geometry does not jitter or leak into a neighboring building's draw

#### Scenario: Persistence and reset
- **WHEN** an adjusted building is saved and loaded or minified and reinstalled
- **THEN** the same building retains both adjustments
- **AND** cycling the offset to center explicitly centers it, without an additional automatic shift

#### Scenario: Content and render-path compatibility
- **WHEN** a compatible mod-added building or Stuff uses native section printing or standard Graphic drawing
- **THEN** the selected adjustment preserves its native appearance inputs
- **AND** bypassing native draw seams is recorded as a concrete compatibility limit, not used to exclude all mod content

#### Scenario: Multi-part building ownership
- **WHEN** a building renders its body plus attached component overlays or additional mesh parts
- **THEN** all mesh submissions owned by that building receive the same scale and offset exactly once
- **AND** direct component Graphics.DrawMesh calls, including the open cryptosleep-casket door, are covered
- **AND** drawing a nested separate Thing suspends the parent's transform and restores it afterward, including on exceptions

#### Scenario: Reviewed native workflow
- **WHEN** this feature is proposed for acceptance
- **THEN** focused RED/GREEN tests, scoped independent review and a fresh minimized Gateway native gizmo/save-load workflow pass
- **AND** the acting agent personally inspects causal screenshots, classifies logs and retains exact build/process/config-cleanup evidence

### Requirement: Readable appearance-control icons
Owner: **mods/ThinWalls**. Shrink and Offset SHALL have distinct action-recognizable icons composed
dynamically from native textures without packaged raster assets. Icons SHALL leave the state badge
and translated label legible, preserve native disabled/low-light rendering, and allocate no textures
per frame. Cosmetic icon changes SHALL not change either action or setting.

#### Scenario: Blind quality gate
- **WHEN** the final icons are captured from native in-game gizmos at 1x and another supported UI scale
- **THEN** an independent reviewer sees unlabelled crops without the intended actions or target score
- **AND** correctly identifies both actions and scores each at least 8/10 for clarity and visual quality
- **AND** the acting agent verifies native Shrink and Offset clicks still update the building and badges

### Requirement: Nearby thin walls choose the existing offset preset
Eligible completed buildings SHALL automatically use the existing 0.2-cell gizmo preset away from
adjacent Thin edge structures. Opposite sides SHALL cancel per axis. The old measured-clearance drawing
overrides SHALL be removed. Only local lifecycle changes SHALL recompute automatic offsets; rendering
SHALL read the same appearance state used by the gizmo. No per-frame sprite readback or map-wide cache
SHALL be introduced. Footprints and gameplay positions SHALL not change.

#### Scenario: Corner and compact U
- **WHEN** a building has north and east thin walls on its perimeter
- **THEN** its offset is southwest and the gizmo shows that same preset
- **WHEN** a west wall closes that corner into a U open only to the south
- **THEN** its automatic offset becomes south, with no west/east displacement
- **AND** the rotated cases and fully opposed axes follow the same cancellation rule

#### Scenario: Player override, persistence and lifecycle
- **WHEN** the player cycles Offset, even back to center, and adjacent walls later change
- **THEN** the explicit offset is retained while automatic buildings update locally
- **AND** Shrink does not change whether the offset is automatic or manual
- **WHEN** the building is loaded or reinstalled
- **THEN** automatic adjacency is recomputed and manual offsets and scale are preserved
- **AND** old saves infer manual only from a nonzero saved offset

#### Scenario: One rendering pipeline
- **WHEN** an automatic or manual offset is active
- **THEN** the existing appearance renderer transforms all owned parts, damage and shadows exactly once
- **AND** ordinary DrawPos/TrueCenter, blueprints and frames are no longer changed by the old renderer

#### Scenario: Claiming an existing building
- **WHEN** the player claims eligible neutral furniture beside thin walls
- **THEN** its automatic preset is assigned without requiring a new wall or reload
- **AND** losing eligibility clears only automatic offsets, never an explicit player's choice
