## ADDED Requirements

Owning mod: **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`.

### Requirement: Product metadata and player text are localized
The product SHALL ship RimWorld 1.6 About metadata and complete English and German catalogs for every Thin-Walls-owned Def field and runtime player-facing key. Package identity SHALL be `fumblesneeze.thinwalls`, author SHALL be `Fumblesneeze`, and Harmony SHALL be the only required mod dependency.

#### Scenario: Native Mods screen metadata
- **WHEN** the reviewed package is opened in RimWorld's native Mods screen
- **THEN** the title, author, package ID, supported version, Harmony dependency, description, and About preview are visible without cropping or missing-key markers

#### Scenario: English and German surfaces
- **WHEN** representative Structure command, placement rejection, blueprint, info-card, and inspect surfaces are viewed in English and German
- **THEN** each surface uses the intended localized text with preserved meaning and no raw key or unintended fallback

### Requirement: Publishing descriptions are player-facing and honest
The mod SHALL own a Markdown source description and rendered Steam BBCode description that lead with space-saving edge walls, explain the Q/E click preview and right-hand multi-cell dragging, unique shared edges, two-sided building coexistence, Thin Door operation, pathing, footprint collision, room/temperature separation, junctions, square hybrid regular-wall merges, cost/durability, and no roof support. It SHALL distinguish room/temperature integration from unsupported cover, projectile, sight, pen, gas, wind, light, and vacuum behavior, and SHALL NOT claim optional integrations or replacement of regular walls.

#### Scenario: Description inventory
- **WHEN** the publishing source is validated
- **THEN** every implemented player-visible capability and limitation has one concise player-facing explanation and no developer-only implementation jargon is required to understand it

#### Scenario: Regular walls remain available
- **WHEN** a player reads the compatibility/scope section
- **THEN** it states that Thin Walls adds a separate build tool and leaves vanilla walls available

### Requirement: About and Workshop images are non-generative and reproducible
The mod SHALL own an approved 1280×720 title master, a deterministic 640×360 `About/Preview.png`, and at least three ordered 1164×655 Workshop feature cards under 1 MiB each. Every pictured Thin Wall, Thin Door, damage state, shadow, room, or regular-wall connection—including the title master—SHALL be an exact crop from a reviewed in-game capture in the accepted minimized Gateway run. Deterministic text, labels, arrows, comparison layout, and limitation symbols MAY be overlaid. Image generation and reconstructed wall/door sprite diagrams are forbidden for every publishing asset in this mod. A checked-in manifest and deterministic renderer SHALL bind card token, alt text, exact copy, promoted capture path, original run and screenshot identity, minimized/visibility state, evidence hashes, local licensed font, dimensions, bytes, output SHA-256, and an explicit `imageGenerationUsed: false` contract.

#### Scenario: Clean offline rerender
- **WHEN** the presentation renderer runs from the checked-in sources without Steam or network access
- **THEN** it reproduces the approved About and feature images byte-for-byte and passes text-overflow, corner-matte, dimension, and file-size checks
- **THEN** it consumes only accepted in-game captures, local fonts, and deterministic layout/vector operations and rejects any generated-image provenance or superseded rendering-run input

#### Scenario: Feature claims match pictured behavior
- **WHEN** each full-size card and the page-scale contact sheet are inspected
- **THEN** the pictured edge geometry directly illustrates its headline/bullets and no card uses generic unrelated decoration
- **THEN** every card's wall, door, room, junction, or Core-wall visual is recognizably sourced from the accepted in-game render rather than a reconstructed sprite diagram

### Requirement: Publishing art uses only the accepted in-game product geometry
The title, About preview, and feature-card gameplay panels SHALL use exact reviewed in-game captures from the final accepted renderer. Deterministic typography and explanatory overlays MAY frame those captures, but no wall geometry may be inferred, redrawn, generated, or taken from a superseded run. Any pictured cardinal segment, junction, regular-wall join, two-sided furnished layout, door, damage state, shadow, or room SHALL therefore be the actual shipped behavior.

Every lived-in title, About, or Workshop gameplay scene SHALL be designed and reviewed through the repository's `rimworld-realistic-base-generation` workflow before fixture/setup code is written. Its checked-in design record SHALL include the adjacency graph, exact loaded footprints, entrances, circulation, and sleeping-furniture orientation. An enclosed occupied shell SHALL have at least one functional exterior entrance visible or clearly traceable in the retained frame. A pictured bed SHALL have its head against a solid wall or intentional headboard/bay and retain a believable side or foot approach. A sealed decorative house or a bed floating in arbitrary open floor space SHALL be rejected even when the featured mod geometry is otherwise correct.

Every public candidate SHALL receive a separate itemized placement audit. This includes both each promoted furnished or door-bearing in-game source and every final title, About image, feature card, or contact sheet that composes it, because a final crop can create a defect absent from the source. A source or output containing only a technical wall/junction/material catalog with no realistic-placement fixture MAY explicitly opt into the technical-catalog scope instead; absence of that explicit scope fails closed. The reviewer SHALL enumerate every visible bed, chair, table/counter, workbench, door, interaction spot, and primary aisle and report any non-aesthetic, implausible, blocked, floating, wrongly oriented, clipped, unexplained, or realistic-base-rule-breaking placement. Chairs presented as table or workstation seating SHALL be cardinally adjacent to and face the exact occupied rectangle they serve with no empty-cell gap. Door cells and both approaches SHALL remain unobstructed. A generic coherence verdict cannot satisfy this audit, and any finding invalidates the candidate until the fixture is corrected and freshly captured.

The manifest cannot select those scopes itself. The publishing contract pins `workbenches`, `room-closed`, `room-presentation-clean`, `door-detail-closed`, and `door-detail-open` as itemized source targets; preview, junction, union, and material/damage sources are technical catalogs. It pins title, About, edge-placement, Thin Doors, and contact-sheet outputs as itemized targets; only continuous-joins and materials-damage outputs are technical catalogs. Each itemized target also has a fixed ordered category inventory derived from what that composition is intended to show. Missing, blank, duplicated, reordered, borrowed-from-another-target, or surplus category rows fail closed.

Mechanically measurable pictured claims SHALL be hard gates over both the exact promoted in-game source and final card. The native workflow SHALL record the canonical shared-edge endpoints and both adjacent cell centers by deriving them directly from the owner cell and side, independently of every Thin renderer placement helper. Cell-boundary overlays SHALL map those recorded points through the exact publication crop/resize/extent transform: the projected endpoint pair defines the complete tangential side, the adjacent-center separation defines one cell of normal extent on either side, and the shared boundary SHALL remain within one final pixel of the complete Thin structural silhouette midpoint, leaving equal visual intrusion into both cells within one source pixel. Hard-coded cell spans or an oracle that calls the product renderer are forbidden. The complete ghost silhouette SHALL be isolated by pixel-differencing a hash-bound same-camera background captured immediately before the preview; selecting only green or bright pixels is forbidden. The baseline and preview SHALL additionally record and exactly match map identity, camera world root, root size, viewport origin, and screenshot dimensions. Unrelated changed pixels outside the selected expected-edge structural component fail closed. Two-sided building panels SHALL use hash-bound, same-camera, shadow-free empty, wall-only, wall-plus-first-building, and complete structural stages. Every nonzero lossless RGB-channel difference, including a one-level antialias change, belongs to the full stage mask; brightness, hue, tolerance, wide-row, and achromatic classification are forbidden. Those masks SHALL reject any opaque/antialias overlap, wall-behind-building occlusion, clipped sprite, one-pixel or detouring bridge, or clearance beyond the specified minimal safety band. Ordinary shadows SHALL then be restored for the public candidate, and every staged structural-mask pixel SHALL remain intact in both that promoted source and the final card after the exact publication transform. Supporting shadow-free stages are measurement inputs only and SHALL never replace the ordinary-shadow public render. An annotated overlay SHALL NOT be treated as evidence unless its geometry passes those measurements.

#### Scenario: Art-to-game comparison
- **WHEN** publishing art is compared with the final exact-process topology screenshots
- **THEN** orientation, thickness, junction shape, and material treatment are consistent with the shipped mod

#### Scenario: Lived-in preview is a usable building
- **WHEN** the title or a feature card pictures an enclosed furnished building
- **THEN** the exact in-game scene contains a functional exterior door connected to its circulation route and every bed is wall-headed with a usable approach
- **THEN** the current design-record revision documents those placements before the fixture coordinates and rejects any earlier sealed-shell or floating-bed capture

#### Scenario: Publication placement audit is explicit and clean
- **WHEN** a furnished publication candidate is proposed for promotion
- **THEN** the retained independent review enumerates its visible beds, chairs, tables/counters, workbenches, doors, interaction spots, and main aisles and records zero unresolved aesthetic, common-sense, mechanical, or realistic-base placement findings

#### Scenario: Edge and workbench cards are mechanically truthful
- **WHEN** the edge-placement and two-sided-furnishing panels are rendered
- **THEN** deterministic pixel measurements prove the cell boundary bisects the complete Thin structural silhouette within tolerance and prove every pictured workbench silhouette remains intact with only the minimal measured clearance

### Requirement: Presentation completion does not imply Steam publication
This change SHALL stop after producing and reviewing local publishing inputs. It SHALL NOT create or update a Workshop item without a separate implemented release plan, mutation-free dry run, immutable presentation digest, and the user's explicit confirmation.

#### Scenario: Local publishing handoff
- **WHEN** all publishing tasks in this change are complete
- **THEN** the repository contains reviewed descriptions and images with no Steam remote mutation or invented published-file identity
