# Contextual furniture orientation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, S048 Auto-Facing Chairs, with explicit user-required manual control.
Observable draft only; no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: AF-01 Orientation assistance covers ordinary furniture placement
The mod SHALL assist supported rotatable seats and facilities during new construction, installation
of a minified item and reinstallation of an existing item. Built furniture, blueprints and frames
SHALL supply their intended chair-use or facility geometry. Assistance SHALL change only the
current placement orientation, without rotating already placed objects, creating new furniture or
changing native placement eligibility. Nonrotatable objects and objects without a relevant seating
or facility relation SHALL retain ordinary orientation behavior.

#### Scenario: Construct a chair beside planned furniture
- **WHEN** the player places a rotatable chair beside a supported table blueprint or frame
- **THEN** the intended table geometry supplies the same orientation suggestion as a completed table.

#### Scenario: Install or reinstall the same furniture
- **WHEN** the player uses Install on a supported minified chair or Reinstall on an existing chair
- **THEN** the placement preview receives the same contextual assistance as new construction,
  while native installation keeps the selected item's identity, material, quality and condition.

#### Scenario: An unrelated object is placed
- **WHEN** the selected object is nonrotatable or has no applicable supported chair/facility relation
- **THEN** the feature does not change its orientation or invent a placement benefit.

### Requirement: AF-02 Seats follow actual use positions and adjacent eating surfaces
The mod SHALL suggest a seat facing consistent with a target's supported chair-use position and
chair marker, including reversed markers and multiple use positions. For that target, a matching
chair-use position SHALL take precedence over its eating-surface suggestion. Otherwise a seat whose
placement cell is directly beside a cardinal side of an eating surface SHALL face into that surface's
actual rotated footprint. Diagonal corners, interior cells, nonadjacent cells and markers that do
not describe seating SHALL NOT supply an eating-surface or chair-use suggestion by themselves.

#### Scenario: A workbench provides a chair-use marker
- **WHEN** a seat's placement cell matches a supported workbench chair-use position
- **THEN** the seat faces as that chair marker indicates, including when the workbench is rotated
  or its marker intentionally faces opposite to the workbench's own orientation.

#### Scenario: One target has several chair positions
- **WHEN** the placement cell matches one of a target's supported chair-use positions
- **THEN** that position can supply a suggestion without requiring it to be the first listed position.

#### Scenario: A chair is on an eating surface's side
- **WHEN** a chair is placed immediately west, east, south or north of a supported eating surface,
  aligned with that side rather than a diagonal corner
- **THEN** it respectively faces east, west, north or south into the surface, using its rotated shape.

#### Scenario: No seating relation exists at a corner
- **WHEN** the chair is diagonally beside a table and no other chair-use/facility relation applies
- **THEN** the current rotation is unchanged.

#### Scenario: A target supplies both kinds of seat guidance
- **WHEN** the same target supplies a matching chair-use position and an eating-surface suggestion
- **THEN** that target's chair-use facing is used rather than treating its two suggestions as a conflict.

### Requirement: AF-03 Facilities follow one-way geometric placement guidance
The mod SHALL assist a placing facility using its supported native geometric relation to furniture
that accepts that facility. When a rotatable target permits several facility orientations, its same
orientation SHALL be preferred if allowed, then its opposite if allowed, otherwise all its permitted
orientations remain candidates. A target with no geometrically permitted orientation SHALL supply
no relation. Placing affected furniture near an existing facility SHALL NOT by itself rotate that
furniture. Suggested orientation SHALL NOT promise an actual link or bypass native usability rules.

#### Scenario: A facility must face a bed head
- **WHEN** a supported facility beside a bed head has one geometrically permitted facing
- **THEN** that facing is suggested, subject to other applicable relations at the same placement cell.

#### Scenario: Several orientations satisfy one target
- **WHEN** several facility orientations satisfy a rotatable target and its own orientation is allowed
- **THEN** that target prefers the same orientation; if only its opposite is among those choices,
  the opposite is preferred instead.

#### Scenario: A nearby target cannot geometrically link
- **WHEN** one nearby target has no geometrically permitted orientation for the placing facility
- **THEN** it does not veto a valid suggestion supplied by another applicable target.

#### Scenario: A bed is placed near an existing facility
- **WHEN** the player is placing an affected bed rather than the facility
- **THEN** the existing facility does not by itself auto-rotate the bed.

#### Scenario: Suggested facing does not guarantee a bonus
- **WHEN** geometry suggests an orientation but ordinary visibility, medical designation, activity
  or simultaneous-link limits prevent an actual facility benefit
- **THEN** those native limits still apply; no benefit is granted by the orientation suggestion.

### Requirement: AF-04 Conflicting relations preserve the player's current rotation
The mod SHALL consider all applicable supported relations at the placement cell together. A target
occupying several cells SHALL count once. If no relation applies or no orientation satisfies their
combined requirements, current rotation SHALL remain unchanged. Otherwise it SHALL remain when
allowed by every relation; if it is not allowed, the first permitted orientation in north, east,
south, west order SHALL be selected. A nearer or earlier-listed target SHALL NOT silently win a conflict.

#### Scenario: Two targets require opposite chair facings
- **WHEN** overlapping supported chair-use positions demand incompatible orientations
- **THEN** the chair keeps its current rotation for the player to choose manually.

#### Scenario: Compatible targets agree
- **WHEN** several applicable relations permit the same orientation
- **THEN** that common orientation is considered once and no target is lost because another appeared first.

#### Scenario: The existing orientation is already allowed
- **WHEN** the current rotation satisfies the combined relation choices
- **THEN** it is retained rather than changed unnecessarily.

#### Scenario: More than one final alternative remains
- **WHEN** current rotation is disallowed and both east and west remain permitted
- **THEN** east is selected by the declared north/east/south/west tie order.

### Requirement: AF-05 Manual rotation wins until the next cell or placement session
The mod SHALL make at most one automatic suggestion on entering a valid map cell during the same
placement session. The player SHALL then be able to rotate with ordinary native controls and retain
that choice throughout the same cell, including repeated updates and pixel-level mouse movement.
When manual rotation and an automatic opportunity coincide, the manual choice SHALL win. Entering a
different valid map cell permits a fresh suggestion; placing the order SHALL use the shown rotation
without snapping again. Cancel/reselect, a new placed item/tool, map change or load starts a new session.

#### Scenario: Subcell mouse movement preserves a manual choice
- **WHEN** the player rotates a chair while hovering a cell and moves the mouse within that cell
- **THEN** the selected rotation remains through further movement and updates in that cell.

#### Scenario: Manual input coincides with cell entry
- **WHEN** the player rotates as the cursor enters a cell where an automatic suggestion is available
- **THEN** the player's resulting rotation takes priority rather than being overwritten afterward.

#### Scenario: The cursor enters another cell and returns
- **WHEN** the cursor moves to another valid map cell and later returns to the earlier one
- **THEN** each new cell entry permits fresh contextual assistance; the previous manual choice is
  not a permanent orientation lock for that location.

#### Scenario: Scene changes while the cursor stays in place
- **WHEN** relevant furniture appears, disappears or changes while the player stays in the same cell
- **THEN** the feature does not resnap in that cell; the next cell/session evaluation uses current data.

#### Scenario: The cursor briefly crosses interface or map bounds
- **WHEN** the cursor leaves valid map space for interface or out-of-bounds space and returns to the
  same last valid cell without a new placement session
- **THEN** no automatic suggestion occurs in the excluded space and the same-cell manual choice remains.

#### Scenario: Browsing keeps the selected placement tool
- **WHEN** the player browses Architect search/category pages without changing the selected tool
- **THEN** browsing alone does not reset the current placement session or its manual rotation.

#### Scenario: The player starts a genuinely new placement
- **WHEN** the player cancels and reselects, selects another placed item/tool, switches maps or loads a save
- **THEN** stale same-cell state is discarded and the new valid placement can receive a suggestion.

### Requirement: AF-06 Chairs only uses common global and save settings
The mod SHALL expose Chairs only, built-in false, in structured common settings. False allows both
supported seat and facility assistance; true limits it to sittable furniture. Each save SHALL inherit
the current global value by default, with labeled Use global, Current save and Global default controls.
Inherited save editing SHALL be inactive; main-menu save editing and Use global SHALL be inactive.
Checking Use global retains the dormant override; unchecking restores it or seeds from the current
global. Changes SHALL affect the next new cell/session evaluation without resnapping the current cell.

#### Scenario: Default scope includes facilities
- **WHEN** a save has no override and the global remains at its built-in false default
- **THEN** both supported seats and facilities receive assistance through their respective rules.

#### Scenario: A save chooses chairs only
- **WHEN** the current save overrides Chairs only to true
- **THEN** nonsittable facilities receive ordinary manual placement and other saves/global defaults
  remain unchanged; checking then unchecking Use global preserves that dormant true override.

#### Scenario: The global value changes while a save inherits
- **WHEN** the global changes for an inheriting save
- **THEN** its next new cell/session evaluation uses the new value, while a save with an override
  retains its override and the current same-cell rotation is not changed by the settings edit.

#### Scenario: Settings are opened at the main menu
- **WHEN** no game is active
- **THEN** only the global control can change; Current save and Use global are disabled with an
  explanation and cannot modify a previously played save.

### Requirement: AF-07 Preview, native order and resulting furniture agree
The mod SHALL preserve the shown placement rotation through the corresponding successful native
construction/install/reinstall order. It SHALL NOT bypass ordinary validation, costs, research,
construction labor, material/style selection or item transport. Loading or enabling the feature
SHALL NOT sweep already placed furniture or rewrite existing blueprint rotations. Native rotation
controls remain the gameplay entry point; no added bottom tab or debug/colony-view control is required.
Own settings/help and unavailable-state text SHALL be localized in all supported languages.

#### Scenario: A manually rotated preview becomes a real order
- **WHEN** the player confirms a valid placement after manually rotating in the current cell
- **THEN** the native blueprint/order and completed furniture retain the chosen facing through the
  ordinary construction or installation workflow.

#### Scenario: Placement is invalid despite a suggested facing
- **WHEN** native placement rules reject the hovered location
- **THEN** the usual rejection remains and no order, free item or forced facility link is created.

#### Scenario: Existing furniture survives a load unchanged
- **WHEN** the player loads a game containing furniture and placement orders
- **THEN** their saved rotations remain unchanged; only subsequent placement interactions receive assistance.

### Requirement: AF-08 Unsupported assistance preserves ordinary placement
The mod SHALL enable orientation assistance only where its required native/provider behavior is
supported, while allowing compatible content and unrelated patches. An active original Auto-Facing
Chairs or unsupported required rotation behavior SHALL leave the coherent owned assistance unavailable
with a bounded startup warning identifying the skipped change, reason and native/provider fallback.
Stored settings SHALL remain intact. Later unsupported content SHALL not cause repeated mouse-update
warnings or override the latest manual choice. Suppression SHALL NOT claim to repair an existing
foreign placement failure.

#### Scenario: Compatible modded furniture supplies the ordinary relations
- **WHEN** modded seats, workbench chair-use positions or facilities supply supported current geometry
- **THEN** the same orientation rules apply without requiring a hardcoded furniture-name list.

#### Scenario: The original provider is active
- **WHEN** original Auto-Facing Chairs is active
- **THEN** the owned orientation feature, including its install/reinstall extension, remains inactive
  with a startup explanation, leaving the original and native paths in charge without duplicate snaps.

#### Scenario: A required placement behavior cannot be supported
- **WHEN** the game or another provider prevents the required manual/preview/order consistency
- **THEN** the affected owned assistance is unavailable, its warning explains the fallback, and
  no partially active assistance shows one rotation while committing another.
