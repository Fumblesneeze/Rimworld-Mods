# Per-showcase design record

Create `mods/<ModName>/Release/workshop/designs/<showcase-id>.md` before writing setup code. The record is the
auditable connection between inspected references and exact cell placement. Do not fill it after the scene is
built merely to rationalize a guessed layout.

The release workflow does not yet automatically bind this record into the immutable publication plan. Keep that
automation task open until the release builder validates the record and hashes it into the plan.

## Template

```markdown
# <showcase title>

- Showcase ID: `<stable-id>`
- Design-record version: `1`
- Owner: `<mod package ID>`
- Status: `draft | arranged | live-reviewed | rejected`
- Intended crop: `<width>x<height>, fixed view or named hard cuts`
- Colony brief: `<biome, tech/wealth, population, form, history>`
- Exact presentation packages: `<ordered product and optional package IDs; Gateway is capture tooling, not a product dependency>`
- Native workflow: `<player action -> jobs/toils -> visible result>`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R02 | kitchen circulation | freezer buffer and clean worker island | recurrent |

Unattributed supplemental images may be listed separately, but cannot support a recurrent rule.

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| kitchen-buffer | mechanical/recurrent | Core work path; R02/R11/R19 | one-cell meal fridge at service edge |

## Adjacency graph

List directed, weighted relationships before coordinates. Example:

`raw freezer --high-frequency--> prep --high-frequency--> stove --output--> meal buffer --service--> dining`

Also list traffic that must remain separate, such as butcher intake versus diners or prisoners versus hoppers.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| clean kitchen | 9x7 interior | granite walls, sterile tile | kitchen-buffer | supports three cooks without through traffic |

Record interaction cells, occupied rects, doors, link radii, plumbing/power routes, stock filters, and camera
visibility. Use `pending exact-Def inspection` instead of inventing a value.

## Variation and history

Explain the restrained asymmetry, material transition, retained natural feature, repurposed room, or expansion
cue. If none fits the colony brief, say so; random damage and clutter are not required.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| first in-game frame | dishwasher blocked the stove interaction cell | moved service lane behind kitchen |

## Live review

- Exact build/package identity:
- Exact process/evidence directory:
- Player action observed:
- Visible result observed:
- Geometry/material/traffic observations at final crop size:
- Remaining caveats:
```

## Review requirements

- Every reference ID resolves in `source-catalog.md` and is eligible for the claimed class.
- Every recurrent rule has at least three independent eligible player-colony sources.
- Every mechanical rule names the exact game/mod contract inspected.
- Every placed gameplay object has a rationale and exact Def/rotation/material or an explicit unresolved marker.
- The adjacency graph precedes coordinates and matches the visible native workflow.
- The live-review section records both gameplay behavior and aesthetic/traffic observations; a setup screenshot
  alone is not approval.
