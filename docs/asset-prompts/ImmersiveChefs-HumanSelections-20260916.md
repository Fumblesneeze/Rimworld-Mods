# Human review decisions — 16 September 2026

Owner: `mods/ImmersiveChefs`. Source: the user's review of `artifacts/VisualAssets/HumanReview-20260916/review.html` and its candidate IDs. These decisions supersede the agent's earlier aesthetic rankings. Historical rejected iterations are not requirements for further design exploration.

| Family | Selected baseline | Requested revision |
|---|---|---|
| Domestic dishwasher | DISH-3 / F19v2-A gray cabinet | Complete consistent cardinal family; keep one-cell appliance and no hoses. |
| Industrial dishwasher | IND-3 / F10-A compact enclosed washer | More top-down empty trays; recessed washing basin under raised hood; front fits Core kitchen benches. |
| Prep station | PREP-1 | Keep. |
| Sauce station | SAUCE-2 | Promote preferred composition to default. |
| Meat station | MEAT-2 | Promote less cluttered composition to default. |
| Vegetable station | VEG-2 | Replace middle square basket with round metal salad sieve; retain board, slicer and peeler. |
| Pastry station | PASTRY-1 | Shift tools slightly toward the top of the tabletop. |
| Microwave | New derivative of DISH-3 | Parallel rectangular top edges, half dishwasher front height, window, swing-door handle, two dials and a button on the right. |
| Cookware | COOK-1 / C6 | Keep. |
| Cutlery | CUT-2 / CU7v5 | Correct fork proportions; dark inset tine marks rather than wide transparent gaps. |
| Primitive cookware | PRIM-1 / PC8 | Keep. |
| Glitterworld cookware | GLIT-1 / GC8 | Strengthen cyan band at distant map zoom. |
| Plates | PLATE-1 / P8v2 | Keep sprite; render physical stack with up to five visible plates. |
| Chef's knives | KNIFE-2 / K5 carrier | Use leather carrier instead of exposed weapon-like single knife. |
| Prepared food | FOOD-2 / PF5-B | Keep chopped-ingredient tray. |

Plate and cutlery stacks should accept different materials without converting their contents. Material identity, hit points and sanitation must survive splitting, save/load and actual use. The later user decision removes crafting quality from plates and cutlery. The stack renderer reuses existing sprites; plate layers rise upward and cutlery units sit alongside one another, capped at five visible units. The implementation and native acceptance remain under the owning gameplay specification.

Reference geometry: Core workbench baseline uses 256 pixels/cell, 640×384 for two-cell benches, 896×384 for three-cell benches, rectangular 256-pixel-deep tops and screen-bottom aprons. Selected DISH-3 is 384×384 on draw size1.5×1.5, with a256-pixel-wide body,144-pixel roof depth and176-pixel front. The microwave target retains the rectangular roof construction and halves the front to88pixels; no converging roof edges. Selected industrial master is1216×1024 on4.75×4 with3×1 footprint. Preserve body/table landmarks; reorient only trays and expose a recessed basin. Artifact source hashes and candidate paths are retained in the original review catalog.

Edits use the built-in imagegen tool for raster revision and retain original outputs under ignored artifacts. Completed user selections must not be reopened through endless minor candidate variations. Fresh affected in-game comparisons and runtime acceptance are required before claiming completion.

The user subsequently authorized local image-processing code for precise edits. The industrial annotation `codex-clipboard-7b380f61-bca3-40fb-955d-f52849b6b728.png` further fixes the front-view geometry: the rear hood-lifting frame ends at the rear tabletop edge (source y408), instead of projecting forward to the table middle; the recessed wash compartment ends alongside the external sink at y616, leaving the same clear front counter margin. Apply this to raised and closed endpoints and the consistent cardinal family.

The follow-up annotation `codex-clipboard-0a2de322-9eb2-4bb0-9902-56dee9de8417.png` requires the basin to fit inside the hood's sealing footprint. Its front-view outer rim is therefore narrowed to x508–707 and shortened to y601 so the closed casing covers the whole opening; it stays behind the sink's y616 front limit. The empty rack remains inset within this rim. Verify the raised-to-closed overlay for exposed basin edges, including the side views.

Selected DISH-3 roof luminance measures146.064 (alternate warm finish147.621), with the rear casing/roof ratio0.681. Preserve these selected-source values instead of imposing the superseded145 roof ceiling. The existing optional finish paths share the selected shapes and use a warm neutral-metal finish; colored trays, tools and food retain their original colors. Default building entries now link to the final observed native rotation gallery; optional finishes retain their separate live-review state.

The final hood-handle choice is **B**, the broad pale lifting bar with dark mounts shown in `artifacts/VisualAssets/HumanFeedback-20260916/handle-round/review.html`. Its approved front pixels are retained exactly for raised/closed endpoints. East and west show the same grip along the visible side edge, without the superseded wraparound U. The rear casing hides the worker-facing grip. Preserve the approved machine geometry and complete both existing finishes. The requested final review is a native four-rotation gallery of all eight selected building families, not another candidate search.

The final rotation audit corrected the sieve handles to the side-view axis at matching scale, moved the pastry group toward the corresponding rear edge in each direction, removed residual front controls from microwave side/rear panels, and aligned optional prep/sauce/meat finishes with the selected compositions. The plain appliance side panels intentionally differ only at their door seam: domestic strips x77/306, y180, 3×152; microwave strips x77/306, y224, 3×68. Their package test verifies the seam changes sides with at least25 luminance contrast, rather than requiring an unrelated percentage of the complete body to differ.

Final native gallery: `artifacts/VisualAssets/FinalBuildings-20260916/review.html`, captured in isolated run `20260916T142136380Z`. All 32 native placements and 108 camera images completed with passing cleanup. Image-only reviews found no rotation inconsistency or clipping; small-scale identity limits are retained in the gallery notes. This bounded gallery does not reopen the human-selected aesthetic choices or close unrelated gameplay/material/damage tasks.

The user approved the final collection for publication with one west-view correction: retain the complete industrial tap arch below the washing compartment, clear of both hood endpoints. The same78×162tap moves70sourcepixels along the sink side to(390,744), retaining its mounting foot beside the basin. Both finishes share the correction; all other machine geometry and directions remain exact. Human approval ends the previous autonomous score iteration; retained independent scores and limits are not rewritten.

Release regression contracts preserve the human-selected sprites rather than recoloring them to satisfy superseded candidate metrics. Selected cookware has shaded pan interiors: mean diffuse luminance175.82 on material mask R>=200 (minimum guard170); plate/cutlery retain minimum210. Selected cutlery uses partial red masks (grime lies mainly on clean-mask R209), so grime measurement includes R>=200. Its fixed high-contrast grime covers915/13010material pixels (7.03%); minimum5% protects the working-end stains without covering clean handles. PRIM-1 visible extent169pixels fits the recorded160–230bound. The dish-derived microwave canvas is384square. Sink-capability tests apply to kitchen stations; the industrial dishwasher sink is part of its washing machine, not a cooking-assistance station.

Fork follow-up: the user identified inconsistent fork styling after publication. The prior head edit left a sharp shade boundary and a missing dark contour at the neck. Repair only the head/neck (measured changed envelope x42–142,y28–150 on the256canvas), using the existing handle/knife's neutral grayscale facets and continuous approximately5sourcepixel contour. Preserve every alpha value, the knife pixels, the selected tine layout, material response and working-end dirt. Derive all four material/sanitation variants and their masks from this one corrected base. This is a targeted finish correction, not a new candidate search. Evidence: `artifacts/VisualAssets/ForkConsistency-20260916`.
