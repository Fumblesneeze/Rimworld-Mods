# Transcription annotations

The two user-supplied originals are retained without changes in `images/page-1-original.png` and
`images/page-2-original.png`. The [image manifest](image-manifest.json) records their SHA-256 values. They are the
authoritative source for handwriting and layout.

The annotated `page-1-read.png` and `page-2-read.png` copies use **red strike-through = confidently
transcribed**, not rejection or implementation. The existing black cancellation is the author's.
The uncertain cancelled line, Trading Spot parenthetical, and folded-corner annotation under Skilled
Slaughtering are deliberately left without new red marks. The title Trading Spot and the preceding
Skilled Slaughtering / cooking text are confidently read and marked.

Tool: built-in `image_gen` editing, one request per page and focused corrections for missed strokes.
One page 2 correction duplicated a word; it was rejected and replaced with a strike-only correction
from the preceding image.
The exact [prompt set](annotation-prompts.json) is retained for reproducibility. Original scans were
inspected before editing, and the results were visually compared with them. These are generated
review copies, not pixel-preserving archival overlays: rendering/resolution and letter shapes can
shift. Do not use their generated lettering as new transcription evidence; consult the originals.
No game raster art was created, and none of these documents/images is part of the playable package.

Local PaddleOCR was used as an initial extraction aid, followed by visual reading and comparison with
installed `About.xml` metadata. It did not reliably separate columns or rotated notes. The curated
Markdown transcript, rather than the raw OCR, records the reading decisions and uncertainties.

The installed-source index records metadata only. It is not an upstream source-code audit, license
review, active-mod list, runtime test, or version-compatibility result. Those steps belong to later
feature refinement/specification and verification.
