## Context

Owner: Thin Walls (`fumblesneeze.thinwalls`, `mods/ThinWalls`). The existing native region split
already supplies Core rooms; `RoomTempTracker.RegenerateEqualizeCells` ignores adjacent non-portal
air regions, so those boundaries never enter its whole-cell-wall samples.

Inspected installed Core 1.6 code: `RoomTempTracker.WallEqualizationTempChangePerInterval` uses
`120 * WallEqualizeFactor (0.00017) * boundary count * temperature difference / room.CellCount`.
It samples 20% of full-wall targets, uses half the outdoor difference for filled targets, and
multiplies the difference by `0.00005` in vacuum. `MapTemperatureTick` runs every 120 ticks at phase 7
(every tick in fast ecology). There is no wall Stuff insulation stat. Thus wood, steel and granite
regular walls all have the same coefficient; no new material whitelist or invented material factors.

## Goals / Non-Goals

Goals: finite half-resistance solid thin walls, real room volumes, bounded work proportional to
owned edges, native room/heater/roof lifecycle. Non-goals: changing regular walls or doors, vacuum
sealing, thermal-overhaul adapters, roof support, new settings, a second room system.

## Decisions

- Half insulation means half thermal resistance, hence twice conductance: per physical edge and
  normal thermal interval, heat in temperature-cell units is `(Tb-Ta) * 120 * 0.00017 * 2`.
- Use the existing map component's public tick hook, gated to Core's thermal cadence. No extra
  Harmony patch, tickable wall Thing, room cache, temperature grid, or full-map scan is needed.
  Iterate the canonical completed-edge dictionary once; only solid walls participate. Blueprints,
  frames and Thin Doors are excluded. Doors keep their established native open/closed rates.
- Resolve current incident Core rooms each interval. Same-room edges and absent air rooms transfer
  nothing (solid occupied cells have no air capacity; surrounding native solid-cell paths remain
  Core-owned). Do not invent an outdoor room through an impassable adjacent building.
- Apply equal/opposite energy to finite rooms, divided by each room's actual CellCount. Outdoor
  rooms are fixed-temperature reservoirs. With at least one cell per finite room the fixed factor
  satisfies `0.0408 * (1/Va + 1/Vb) <= 0.0816`, so pair transfer cannot overshoot even for
  one-cell pockets and needs no extra clamp. Exchange is sequential, like Core;
  exact system-wide simultaneous integration is not introduced.
- Reuse Core vacuum attenuation for conduction, without claiming thin edges block vacuum.
- Keep native roof/weather/deep-ground/heater calculations intact. Native rebuilds continue to
  preserve cached temperatures. No serialized thermal state; save/load rebuilds the existing edge index.

## Risks / Trade-offs

- Additional heat now escapes previously perfectly insulated thin rooms → intentional balance fix.
- Core regular-wall sampling/sequential updates are approximate → verify the exact coefficient
  independently under controlled uniform temperature, then native simulation for gameplay evidence.
- Temperature-overhaul mods can replace Core equations → no compatibility claim for uninspected
  executable overhauls; all ordinary modded Stuff remains accepted without special casing.
- Many small rooms → no per-room edge scans; one pass through unique owned solid edges per interval.

## Migration Plan

Rebuild/install the existing package; restart to load it. No save conversion or normal save/config edits.
Removal of the change restores prior behavior by rebuilding the previous code; no extra persisted data.
