# Immersive Chefs visual assets

Selected sprites are tracked in the mod package. Generated candidates, chroma-key sources,
comparison montages, and rejected variants are retained under ignored `artifacts/VisualAssets`
during development.

## Cookware set

Selected: candidate A, generated 2026-08-06. It keeps the pot, pan, two lids, handles, and
spatula readable at 64 px; its taller circular silhouette also remains distinct from plates.
Candidate B was rejected because its low bundled silhouette became indistinct at item scale.

Both candidates requested original RimWorld-like, hand-painted, high three-quarter item art on a
pure green chroma background. Candidate A requested a nested deep pot, shallow pan, two loose lids,
and small spatula with neutral gray stuff-colorable surfaces and fixed brown handles. Candidate B
requested the same abstract set in a low, wide, mildly worn bundle. The selected image was keyed,
despilled, trimmed, and reduced to 256 px; transparent pixels were normalized to black to avoid
chroma leakage in renderers. `Cookware_m.png` marks the neutral cookware surfaces red for RimWorld's
primary Stuff tint and leaves the brown handles unmasked.
