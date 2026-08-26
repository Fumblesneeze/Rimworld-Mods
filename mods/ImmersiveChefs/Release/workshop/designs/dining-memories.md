# The dining memories that follow you

- Showcase ID: `dining-memories`
- Design-record version: `1`
- Owner: `fumblesneeze.immersivechefs`
- Status: `live-reviewed`
- Intended crop: `1280x720, fixed gameplay room with the native Needs thought pane`
- Colony brief: `Small established boreal colony dining nook with granite walls, wood floor, lighting and adjacent kitchen context.`
- Exact presentation packages: `brrainz.harmony -> ludeon.rimworld -> imranfish.xmlextensions -> fumblesneeze.immersivechefs`
- Native workflow: `player invokes the enabled Consume simple meal float-menu option -> conscious colonist ingests one cold plated meal without cutlery or a usable table -> native Needs thoughts show all three memories`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R01 | finished interior | warm wood and stone palette | recurrent |
| R03 | lived-in dining context | mixed furniture and room use | recurrent |
| R06 | table proximity | legible dining adjacency and seating | recurrent |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| native-thought-pane | mechanical | Core Needs UI and Immersive Chefs ThoughtDefs | the native panel, not a label, communicates the result |
| room-before-fixture | recurrent | R01/R03/R06 | crop retains walls, flooring, lamp, chairs and kitchen edge |
| causal-missing-table | mechanical | Core ingest table search | no usable table exists in the diner's search path during ingestion |

## Adjacency graph

`cold plated meal --native Consume--> diner --memory--> Needs thought pane`

The camera remains fixed; only the UI inspection changes after the ordinary meal action.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| dining nook | retained room around diner | granite wall, WoodPlankFloor, DiningChair/wood | room-before-fixture | credible small-colony context |
| cold meal | one step from diner | MealSimple with embedded Plate/steel | causal-missing-table | short path keeps the action readable |
| table exclusion | outside reachable search | no usable table | causal-missing-table | causes the vanilla thought honestly |
| Needs UI | right-side native panel | Pawn Needs/Mood/Thoughts | native-thought-pane | requested player-visible evidence |

## Variation and history

Granite walls and wood flooring show a modest room upgraded from a starter shelter; the furniture is practical rather than uniform or luxurious.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| early retained frame | selected pawn bracket dominated an undeveloped exterior | rebuilt as a finished room and retained an unselected gameplay crop with the native thought pane |

## Live review

- Exact build/package identity: `ImmersiveChefs EB77E8EF2AD4F62DB4755D50678B2C6419DB491A299B73CDAFEBACA5391A87F4 with exact Harmony/Core/Immersive/Gateway capture identities`
- Exact process/evidence directory: `artifacts/ShowcaseRuns/memories-accepted-20260813T160603838Z and checked-in dining-memories.capture.json`
- Player action observed: `The enabled native Consume simple meal option was invoked for the conscious named diner.`
- Visible result observed: `Ate without cutlery, Ate without table and Cold meal appeared together in the native Needs thought pane after ingestion.`
- Geometry/material/traffic observations at final crop size: `The 1280x720 crop retains the lit wood-and-granite room, chairs and nearby kitchen context while the thought list stays readable; no selection bracket appears in the clean crop.`
- Remaining caveats: `This is the requested still-only showcase, so it does not attempt to summarize the full dining lifecycle as a GIF.`
