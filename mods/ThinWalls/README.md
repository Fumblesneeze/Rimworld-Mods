# Thin Walls

Package ID: `fumblesneeze.thinwalls`

Thin Walls is a RimWorld 1.6 mod for building Stuff-made walls and doors along tile edges while keeping both neighboring cells usable. Regular walls and doors remain available for structures that need their full behavior.

## Requirements

- RimWorld 1.6
- Harmony (`brrainz.harmony`)

There are no optional mod integrations or Gateway dependency.

## Building

The Structure architect category contains separate **Thin Wall** and **Thin Door** tools. Choose a Stuff, hover the first cell, and rotate its edge with Q or E before clicking. Once a drag enters a second cell, every segment follows the right-hand side of the origin-to-destination direction.

- The owner cell and its neighbor remain standable and can hold floors, zones, items, pawns, and compatible buildings.
- Movement and building footprints cannot cross a completed, planned, or framed occupied edge.
- Different north, east, south, and west segments can coexist on one tile; the same undirected shared edge can contain only one wall or door phase.
- Thin runs connect visually with corners, junctions, Thin Doors, and ordinary walls.
- Thin Doors open for movement across their own edge and support ordinary access, hold-open, damage, repair, and deconstruction behavior.
- Completed Thin Walls and Thin Doors divide rooms. Doors exchange heat only between the rooms on opposite sides of their own edge.

Thin Walls cost 3 units of Stuff and have 150 base hit points. Thin Doors cost 13 units and have 80 base hit points. Their wall, door, junction, damage, and shadow surfaces are derived at runtime from the installed Core wall materials; the mod ships no generated structural wall sprites.

## Important limitations

Thin Walls and Thin Doors do not support roofs or create automatic roof areas. Room and temperature separation does not imply cover, projectile or line-of-sight blocking, pen containment, gas, wind, light, or vacuum separation. Use regular walls and doors when those behaviors matter.

## Development

Use the repository MCP/CLI from the repository root:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet run --project $mcp -- tool call test_run `
  --arguments '{"suite":"ThinWalls.Unit","filter":null,"configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"thin-walls.native-construction-save-deconstruct","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

The owning build is also part of [`RimWorldMods.sln`](../../RimWorldMods.sln). Host environments are split into `ThinWalls.Unit`, `ThinWalls.Harmony`, and `ThinWalls.Defs`; pathing, placement, construction, rooms, doors, and rendering are accepted only through their reviewed in-game workflows.

The owning contract is [`openspec/changes/add-thin-walls`](../../openspec/changes/add-thin-walls/). Current draft Workshop copy and presentation sources live under [`Release/workshop`](Release/workshop/) and remain subject to the owning OpenSpec review tasks.
