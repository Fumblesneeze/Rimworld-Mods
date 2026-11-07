## Why

Blood and vomit should not linger on absorbent ground as long as they do on sealed floors. S105
replaces Permeable Terrain with explicit surface-sensitive lifetimes, while preserving ordinary
cleaning, rain and already-existing stains.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S105/P2-35**, Permeable Terrain / Permeable Terrain Continued
(`Owlchemist.PermeableTerrain`). This change specifies later implementation only.

## What Changes

- Assign liquid filth a shorter natural lifetime on permeable ground, with explicit ice, straw
  matting and packed-dirt rules. Keep a separate multiplier for all other filth/surface combinations.
- Use common global/save settings, with clear percentages and examples; changes affect newly
  assigned lifetimes rather than rewriting existing stains.
- Preserve natural cleaning, rain, thickening and independent provider behavior. Avoid repeated
  lifetime reductions on loading or gravship transport, and prevent rounding from making a
  temporary stain permanent.
- Preserve supported modded terrain and filth through actual content traits and explicit mappings.
  Replace label-based classification and destructive legacy ReGrowth compensation with narrow,
  guarded composition that retains current provider lifetimes and rain rules.
- Keep debugging in the top developer toolbar and measure the actual spawn-time cost before
  claiming optimization. The handwritten optimization question is not evidence of a performance bug.

## Capabilities

### New Capabilities

- `megamod-terrain-filth-lifetimes`: Surface-sensitive filth aging, lifecycle boundaries, inherited
  configuration, optional-content composition, diagnostics and safe admission.

### Modified Capabilities

None. Existing owned cleaning, construction, fire and vomit-producing features retain their own
rules and use the same native filth lifecycle.

## Impact

Core is required. Harmony is required with the native lifetime integration. XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) is required when the planned guarded classification
and optional mapping patches ship. ReGrowth and FSF filth changes are optional reference/integration
inputs; Odyssey is needed only for its transport workflow. No current dependency or package is edited.

Original or continued Permeable Terrain owns the overlap whenever its shared package is active.
No package impersonation, foreign unpatching, upstream assembly bundling or silent settings import
is planned. The original public source has an MIT license; the exact continuation binary and its
bundled source are recorded separately. Independently implement observable behavior and retain
Owlchemist/FerrisCG and license-file attribution without copying upstream code or art.
