# Designation cell preview

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement of Designations Tooltip, S016 / P1-13 / P1-36. These observable requirements
specify later work; DT-Q1–4 retain source/provider and native-verification gaps in refinement.

## ADDED Requirements

### Requirement: Count candidate and accepted cells accurately
The feature SHALL display accepted cells over total candidate cells for a supported active designation drag, normally as `X / Y`.
Y shall count the distinct cells in the selected drawing shape's candidate footprint; X shall count
the subset accepted by the current designation tool. A cell counts once even when it contains several
Things. Counts shall not be described as numbers of objects, stacked units, resources, future jobs
or eventual completed work. A nonempty footprint with no accepted cells shall display zero when the
ordinary visibility conditions below allow a count. Classic mode shall show X alone.

#### Scenario: Partly usable filled rectangle
- **WHEN** a 10-by-10 filled-rectangle drag has eight accepted cells and Classic mode is off
- **THEN** the center readout is `8 / 100`.

#### Scenario: Hollow shape uses its actual footprint
- **WHEN** a 5-by-5 hollow rectangle has 16 candidate edge cells and all are accepted
- **THEN** the default readout is `16 / 16`, not a total of 25.

#### Scenario: No accepted cells
- **WHEN** a visible-count drag has 100 candidate cells and none is accepted
- **THEN** the default readout is `0 / 100`, or `0` in Classic mode
- **AND** help text does not promise to hide this zero result.

#### Scenario: Several targets in one cell
- **WHEN** a supported tool accepts one candidate cell containing several eligible items
- **THEN** that cell contributes one to X, regardless of stack size or the number of items acted upon.

### Requirement: Respect drawing-style and size visibility
The feature SHALL show counts only during a supported active multi-cell drag whose drawing style permits an area count and whose candidate footprint is nonempty.
With Always show count on small drags enabled, it shall show counts even when a bounding side is
one or two cells. With it disabled, both bounding dimensions must be at least three cells. Neither
mode shall force a count for single-cell placement tools, empty footprints or styles that disable
area counts. Existing outlines, snap behavior and side-length measurements shall remain unchanged.

#### Scenario: Small rectangle enabled
- **WHEN** the player drags a 2-by-2 rectangle with two accepted cells and the small-drag preference enabled
- **THEN** the default readout is `2 / 4`.

#### Scenario: Small rectangle disabled
- **WHEN** the same 2-by-2 rectangle is dragged with the small-drag preference disabled
- **THEN** the center count is hidden
- **AND** it appears when both bounding dimensions reach three cells, subject to the other visibility rules.

#### Scenario: Style opts out of area counts
- **WHEN** the selected drawing style disables area counts, as the inspected straight-line style does
- **THEN** enabling small-drag counts does not force a center count onto that style.

#### Scenario: Single-cell placement is not a drag preview
- **WHEN** the player uses a single-cell placement style or a supported style produces no candidates
- **THEN** no extra affected/total label is shown by this feature.

### Requirement: Preview does not change designation actions
Displaying the counts SHALL leave the selected tool's acceptance, visibility, action, tutorial restrictions and final commitment unchanged.
The preview shall not reveal hidden targets, add orders, reserve resources, expand the chosen shape
or promise that a later job can complete. Releasing the drag shall still use the ordinary tool's
action and current restrictions; canceling shall not commit anything because a count was displayed.
The total can include rejected candidates, including candidates outside the map when the selected
style produces them, while those cells must not be invented as accepted targets.

#### Scenario: Inspect and cancel
- **WHEN** the player drags across trees, reads the count and cancels
- **THEN** no chopping orders or resource changes are created by the preview.

#### Scenario: Release under ordinary restrictions
- **WHEN** the player releases a supported drag after inspecting the count
- **THEN** the tool performs its ordinary designation action subject to its current restrictions
- **AND** the preview does not bypass a rejection or cause an extra action.

#### Scenario: Rejected map-edge candidates
- **WHEN** a selected shape supplies nine candidates, three are outside the map and the tool accepts the remaining six
- **THEN** the default preview is `6 / 9`
- **AND** the outside cells do not become valid targets merely to make X equal Y.

### Requirement: Current counts for the current selection
Each displayed pair SHALL describe the same current tool, drawing style, footprint and map, including while paused.
Moving the endpoint, changing the tool/style or changing relevant acceptance conditions shall refresh
the visible result by the next displayed preview. Camera clipping shall not reduce X to only the
currently visible highlights. Ending or canceling a drag, changing maps or leaving the game shall
remove its old preview; another map/save shall not inherit it. Owned custom designation tools shall
use their actual candidate and accepted cells, rather than an unrelated native rectangle estimate.

#### Scenario: Change shape while paused
- **WHEN** the player changes a paused 5-by-5 drag from filled to hollow with every candidate accepted
- **THEN** the next default preview changes from `25 / 25` to `16 / 16`
- **AND** it does not pair the new numerator with the old denominator.

#### Scenario: Camera moves without changing the selection
- **WHEN** part of the same drag footprint moves outside the visible camera area
- **THEN** the counts still describe the full candidate footprint and accepted subset.

#### Scenario: Start another map's selection
- **WHEN** the player ends a drag and switches to another map or save
- **THEN** the former readout disappears
- **AND** a new drag counts only that map's current selection.

### Requirement: Readable contextual presentation without UI damage
The feature SHALL draw one legible count near the ordinary drag center, sized for its full text, while preserving surrounding native UI.
Long counts and relevant UI scales shall not truncate the values or leave a wider label visibly
anchored as if it were a short number. Its drawing shall not alter later text size, alignment or
color elsewhere. The preview adds no bottom tab, debug action or operational toolbar toggle.
Player-facing settings labels, explanations and unavailable-feature messages shall be translated
in all seven baseline languages.

#### Scenario: Large count remains readable
- **WHEN** a large supported drag produces a long affected/total value
- **THEN** one complete readable label is centered near the selection's ordinary count position
- **AND** the existing outline and side measurements remain usable.

#### Scenario: Other UI draws after the preview
- **WHEN** another normal game panel draws after the designation count
- **THEN** its font, alignment and color remain those appropriate to that panel.

### Requirement: Two preferences use common global and save controls
The feature SHALL provide Classic mode, default off, followed by Always show count on small drags, default on, in the ordered Interface / Designation preview mod-settings section.
Each row shall use the common Global default, Current save and Use global controls. New/missing save
entries inherit the current global default; inherited per-save editors are inactive. The main menu
shall disable both per-save editing and Use global, including mutation callbacks, while allowing
global edits. Save overrides shall not alter globals or other saves. Re-enabling Use global shall
retain a dormant override; disabling it shall restore that override or seed the first one from the
current effective value. Changes shall affect the next preview without restarting, including in
an active save opened from world view. There is no source-settings import in this feature.

#### Scenario: New save uses source-derived defaults
- **WHEN** a new save has no override and globals retain their built-in values
- **THEN** it uses affected/total mode and shows eligible small-drag counts
- **AND** both rows show Use global checked with inactive per-save editors.

#### Scenario: Save changes its presentation
- **WHEN** the player disables Use global for Classic mode and enables that save's value
- **THEN** that save's next eligible drag shows X alone
- **AND** the global default and another save's override remain unchanged.

#### Scenario: Main-menu controls are safe
- **WHEN** settings are opened with no active save
- **THEN** Global default can be edited, but Current save and Use global cannot be edited or invoked
- **AND** no previous or implicit save is mutated.

#### Scenario: Inheritance and dormant override
- **WHEN** the player re-enables Use global after setting a save override, changes the global value, then disables Use global again
- **THEN** the save follows the changed global while inheriting and restores its dormant override afterward.

#### Scenario: Reload and world-view editing
- **WHEN** an active save's preference is edited from world view and that save is later reloaded
- **THEN** its inheritance/override choice persists and governs the next map drag
- **AND** no stale preference or drag data is taken from another save.

### Requirement: Semantic admission and safe display fallback
Each enabled patch SHALL verify its documented minimal target behavior before application and preserve native or compatible-provider behavior when its target is missing, ambiguous, changed or cannot be checked safely.
Unrelated changes or other patch owners alone shall not reject a compatible target. Each skipped
enabled patch shall produce one bounded startup warning identifying the feature, patch/target spec,
failed prerequisite, unavailable display behavior and fallback, without per-frame spam. The count
replacement and small-size override shall have explicit support states: if only small-size support
fails, supported counts remain available at ordinary sizes; if count replacement fails, this feature
shall not enable a size-only alteration of an unrelated display. A rendering failure shall restore
surrounding UI and use safe native/provider fallback, with a bounded diagnostic rather than repeated
exceptions. Retain stored preferences and explain when disabled support prevents their effect.

#### Scenario: Count target is incompatible
- **WHEN** the required count-display semantics are missing or ambiguous at startup
- **THEN** our affected-count replacement and dependent small-drag override remain inactive
- **AND** the prior display/action remains intact with the bounded startup explanation.

#### Scenario: Only small-drag support is unavailable
- **WHEN** count replacement is supported but the separate small-size override cannot be admitted
- **THEN** affected counts still work at ordinary supported sizes
- **AND** a warning explains why the stored small-drag preference has no effect.

#### Scenario: Compatible unrelated patch
- **WHEN** another mod changes an unrelated drawing detail without violating the consumed count/size semantics
- **THEN** that other patch alone does not disable our supported preview.

#### Scenario: Drawing fails safely
- **WHEN** an unexpected failure prevents rendering our label
- **THEN** safe prior display behavior is retained or restored without corrupting other UI
- **AND** the diagnostic is bounded instead of repeating every frame.

### Requirement: One renderer and honest provider support
An active original Designations Tooltip provider SHALL disable our overlapping renderer with one explanatory startup diagnostic, without removing the source or changing its settings.
An inactive installed copy shall not disable ours. Optional external drawing/coordinate providers
shall be supported only where their actual shape, acceptance, position and patch-order contracts
are understood and admitted. Unsupported custom selection semantics shall retain their own display
instead of receiving guessed affected counts. No upstream package or patch-owner identity shall be
impersonated to force compatibility; owned selected features shall compose directly.

#### Scenario: Source active
- **WHEN** Designations Tooltip is active alongside megamod
- **THEN** only the source's overlapping display is selected and our overlap diagnostic explains this
- **AND** our retained preferences are not presented as controlling the source's display.

#### Scenario: Source installed but inactive
- **WHEN** the source files exist on disk but are not active
- **THEN** that alone does not prevent our admitted preview.

#### Scenario: Supported transformed map
- **WHEN** an admitted optional provider presents a rotated or otherwise transformed map
- **THEN** the counts use that selection's actual cells and appear at the correct transformed drag position
- **AND** they do not count or draw against the unrelated base-map selection.

#### Scenario: Unknown custom selection
- **WHEN** a provider's selection semantics cannot be safely interpreted
- **THEN** its existing display and action are left intact
- **AND** our feature does not label a guessed rectangle count as the number of affected cells.
