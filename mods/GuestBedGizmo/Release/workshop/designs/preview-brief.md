# Hospitality + Ideoligy Patch preview brief

## Purpose

Teach one idea at Workshop-page scale: Hospitality guests are the fourth choice in the familiar RimWorld bed-owner menu. The card must be recognizable as an in-game UI compatibility patch, not as new bed art or a colony showcase.

## Reference measurements

- The accepted final-DLL RimWorld frame is `20260824T121936256Z` screenshot `000005`, 1600×900, and visibly contains two separately legible selected beds plus the exact ordered `For colonists`, `For prisoners`, `For slaves`, and `For guests` menu. The source is retained without tonal or geometric edits. The same run's materially wider screenshot `000006` is the separate zoom comparator supplied to blind review.
- The repository Thin Walls title card uses a dark framed panel, high-contrast condensed type, and an actual in-game crop; its 640×360 derivative remains legible. This establishes the local presentation family.
- Hospitality Continued's installed 638×358 About preview uses a prominent title and immediately recognizable Hospitality palette, but it is illustrative rather than evidence-bearing. This card therefore names Hospitality in the eyebrow while keeping the verified game menu as the visual subject.
- The checked-in Thin Walls feature-card contract is 1164×655 below 1 MiB. Hospitality + Ideoligy Patch adopts that exact Steam-card size and creates the repository-standard 640×360 About derivative.

## Exact composition

- Canvas: 1164×655, opaque dark navy outer field and near-black rounded panel.
- Context crop: x=432, y=108, width=850, height=600 from the accepted 1600×900 frame. This preserves the two selected beds and their enclosed map context at 84.7% of source scale while excluding unrelated HUD fragments.
- Menu legibility inset: x=1474, y=786, width=126, height=114 from that same frame, resized with Lanczos sampling to 234×208. It isolates the exact four native rows at About-preview scale without repainting or reconstructing any UI.
- Crop placement: x=414, y=73, width=720, height=508, with a 3 px warm frame.
- Typeface: pinned local OFL Oswald SemiBold. Primary copy is warm ivory, accent copy orange-red, and the eyebrow muted blue-gray.
- Copy: `HOSPITALITY + / IDEOLIGY PATCH`; `GUESTS JOIN / THE MENU`; one support line, `FOUR TYPES. ONE GIZMO.` The exact requested public spelling is retained. The 42-point two-line title begins at x=48, y=98; the kicker begins at y=230 and support line at y=330, leaving measured separation before the menu inset.
- AI image generation: none. The renderer performs only a declared crop, Lanczos resize, flat background/frame drawing, and text layout.

## Acceptance criteria

- The four menu labels remain readable at source, 1164×655, 640×360, and ordinary Workshop-page scale.
- The card clearly reads as RimWorld bed ownership and guest selection without explanatory context.
- The live menu, bed, and gizmos remain perspective-coherent and unaltered; the crop does not imply a separate guest gizmo.
- Copy is unclipped, non-colliding, and subordinate to the behavior-bearing menu.
- Both outputs are opaque, have exact declared dimensions, remain below 1 MiB, and reproduce byte-for-byte under the pinned renderer inputs.
- Independent blind review receives only the close and moderate-zoom source frames plus the final output and finds no UI legibility, crop, style, coherence, or zoom defect.
