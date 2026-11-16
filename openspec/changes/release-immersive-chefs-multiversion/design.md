## Owner and scope
Owner: Immersive Chefs (`fumblesneeze.immersivechefs`). Preserve existing gameplay, balance, translations and raster art. Support exactly the reviewed 1.6 and 1.7 targets; 1.7 remains a beta runtime and is described as such.

## Decisions
Use common source and the repository versioned build contracts. Keep any observed runtime difference in `Source/Compatibility/RimWorld` with target-derived symbols, and document each divergence here when discovered. Do not broaden optional-mod compatibility claims beyond installed-target native evidence.

## Acceptance
Run isolated Gateway-driven native workflows against both target builds. Observe actual pawn cooking, clean/dirty utensil use and recovery through existing scenarios. Keep source/package identities and before/action/after screenshots; compilation or loaded Defs alone are insufficient. Publish one package only after both gates pass.

## Verification fixture correction
The native cookware workflow assumes each sealed room has only its injected washing source. Walkable natural water violated that assumption: the product chooses nearer water while the test waits at the injected cell. Select dry room footprints and assert each intended destination before execution. Retain the native work orders, exact physical ware, wash durations and screenshots. This changes verification setup only; no gameplay, balance or runtime API divergence is introduced.
