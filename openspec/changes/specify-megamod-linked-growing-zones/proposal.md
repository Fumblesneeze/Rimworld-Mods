## Why

Separated fields often grow the same crop and need the same sowing/cutting decisions. The player
wants to link growing zones so changing one updates the others without repeatedly selecting every field.

## What Changes

- Specify persistent same-map groups sharing the selected crop, Allow sowing and Allow cutting.
- Keep terrain, climate, research, pawn skill and actual plant work under normal game rules.
- Use contextual zone controls, with clear initial-policy selection and unlink preservation.
- Record the exact native command/work paths and unresolved provider questions separately.

This change delivers specification documents only; it does not implement or accept gameplay.

## Capabilities

### New Capabilities

- `megamod-linked-growing-zones`: Shared crop and growing-work choices across linked zones.

### Modified Capabilities

None.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: original, P1-06 / S006. Core required; no source mod dependency. Harmony required if used;
XML Extensions required on first use of its operations. Other crop providers are optional and need
shape/content checks. Linking hydroponics is separately listed and is not silently included here.
