# Building appearance controls

Select an ordinary player-owned building to use the two cosmetic commands:

- **Shrink** cycles through 90%, 80%, 70%, 60%, 50%, and back to 100%.
- **Offset** moves the drawing 0.2 cells north, northeast, east, southeast, south, southwest, west,
  northwest, then back to its original center. Directions use the map, not the building's rotation.

Both commands can be hidden independently under **Options → Mod settings → Thin Walls**. Hiding them
does not undo saved adjustments. Choices belong to that individual building and survive saving,
uninstalling, and reinstalling it.

Shrink uses gold inward arrows; Offset shows an object with a cyan movement arrow. Both are drawn
from native game UI textures, with no bundled icon files. They adapt to UI scaling and larger text
so the state badge and command label remain visible. The label and tooltip distinguish Offset's
small visual nudge from moving the building's actual occupied cells.

These are visual adjustments: the building still occupies the same cells and retains the same
interaction spots, storage capacity, pathing, work, cost, and hit points. Separate objects such as
stored items and sleeping/seated pawns are not resized or moved. Decorative furniture artwork, such as
bookcase book-spine displays and statue figures, does follow the building's size. Selection brackets continue to show
the real gameplay footprint. Walls, doors, blueprints, frames, and linked structural graphics are
excluded from these controls.

Until an offset is chosen manually, nearby thin walls automatically assign one of the same offset
presets. Opposing walls cancel on that axis; a U-shaped enclosure moves the drawing toward its open
side. Shrinking does not disable this automatic offset.

Choosing an offset manually takes precedence over automatic placement, including an explicitly
chosen **Center**. That manual choice survives later wall changes, saving, and reinstalling.

The offset concept is inspired by [Perspective: Buildings (Continued)](https://github.com/emipa606/PerspectiveBuildings).
That mod is not required. No code, textures, or assemblies from it are bundled. Mod-added buildings
using native section printing or mesh submissions within the building draw can participate without a
Def allowlist. Attached component parts share the same adjustment, including direct DrawMesh calls.
Custom rendering outside the object's draw scope or through procedural/instanced APIs may need an
adapter. Combining manual
controls from multiple mods has not been accepted as a compatibility claim.
