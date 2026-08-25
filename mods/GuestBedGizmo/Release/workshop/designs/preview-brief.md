# Hospitality + Ideology Patch preview brief

## Purpose

Teach one idea at Workshop-page scale: Hospitality guests are the fourth choice in the familiar RimWorld bed-owner menu. The annotated page review identified the four-choice menu as the only important visual, so the card must make that native menu dominant without promotional copy or a competing colony crop.

## Reference measurements

- The accepted final-DLL RimWorld frame is `20260824T121936256Z` screenshot `000005`, 1600×900, and visibly contains the exact ordered `For colonists`, `For prisoners`, `For slaves`, and `For guests` menu. The menu pixels are retained without repainting or reconstruction. The same run's materially wider screenshot `000006` remains the separate zoom comparator for context-free review.
- The checked-in Workshop contract is 1164×655 below 1 MiB with a 640×360 About derivative. Both need the native labels to remain immediately readable.

## Exact composition

- Canvas: 1164×655, opaque dark navy outer field and near-black rounded panel.
- Menu crop: x=1474, y=786, width=126, height=114 from the accepted frame, resized with Lanczos sampling to 620×561 and placed at x=272, y=47 with a 3 px warm frame.
- No context crop, title, eyebrow, kicker, support line, or other copy appears in the image. The Steam page supplies the corrected title separately.
- AI image generation: none. The renderer performs only the declared crop, Lanczos resize, and flat background/frame drawing.

## Acceptance criteria

- The four menu labels remain readable at source, 1164×655, 640×360, and ordinary Workshop-page scale.
- The card clearly reads as RimWorld bed ownership and guest selection without explanatory context.
- The native menu remains unaltered and the crop cannot be mistaken for a separate guest gizmo.
- No text or context scene competes with the menu.
- Both outputs are opaque, have exact declared dimensions, remain below 1 MiB, and reproduce byte-for-byte under the pinned renderer inputs.
- Independent blind review receives only the close and moderate-zoom source frames plus the final output and finds no UI legibility, crop, style, coherence, or zoom defect.
