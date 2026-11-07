## Why

A suitable artificial limb should not prevent a pawn from wearing gloves, boots or jewelry merely
because the game represents the replaced extremity as missing. Preserve the actual body's health
and clothing restrictions while recognizing the replacement for this specific clothing decision.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S025 / P1-19**, Prosthetic No Missing Body Parts (Continued),
`Mlie.ProstheticNoMissingBodyParts`, Workshop 2739055353. **Specification only:** nothing implemented.

## What Changes

- Specify garment eligibility through approved installed arm, hand, leg or foot replacements,
  retaining the native any-covered-part rule, actual side/branch and all other clothing restrictions.
- Specify searchable replacement preferences with common global/save inheritance, stable absent-
  content retention, correct defaults and immediate use at the next relevant eligibility check.
- Correct first-matching-group rejection, label-dependent side checks, duplicated settings rows,
  misplaced foot defaults and settings loss when the discovered catalog changes.
- Specify admitted Integrated Implants modular replacements and consistent Choose Your Outfit
  previews through narrow owned integrations, without impersonating the original package or settings.
- Preserve actual anatomy, surgery costs/outcomes and physical apparel through changing eligibility;
  no regenerated fingers, extra body groups, synthetic armor or free equipment.

## Capabilities

### New Capabilities

- `megamod-prosthetic-apparel`: replacement-aware garment eligibility, configuration, lifecycle and
  optional consumer behavior.

### Modified Capabilities

None. The foundation's common settings/admission rules and existing clothing/unloading contracts
remain authoritative; this change supplies a specific body-availability decision.

## Impact

Future implementation affects native apparel availability, ordinary wear/retention checks, replacement
settings and optional clothing previews. Harmony will be required for the selected runtime seam.
No XML mutation is currently selected; any later XML Extensions operation also requires its declared
dependency and existence/shape/package guards. Prosthetic, apparel, Android, Integrated Implants,
EBSG and Choose Your Outfit providers remain optional and absent-safe; their support requires exact
admission and native proof. The original mod is a co-installation alternative, not a dependency.
No source assembly, copied assets, source settings importer or runtime repository-mod dependency.
