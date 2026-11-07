## Why

Early malnutrition's flat consciousness penalties can kill an already severely impaired pawn even
though that hunger stage is mild. Consciously Malnourished replaces those deductions with ceilings;
the megamod needs that behavior specified without accidentally changing other capacities or hunger.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S045 / P1-37**, Consciously Malnourished (`ProfileName.MalCon`, Workshop 2935404464).
This is **specification only**, not implemented or accepted gameplay.

## What Changes

- Replace the trivial/minor/moderate/severe malnutrition consciousness deductions with maximums
  of 95%/90%/80%/70%, preserving stronger existing limits and all unrelated effects.
- Preserve extreme malnutrition, lethal starvation, hunger progression/recovery, native downing
  and other causes of death. A ceiling never raises low consciousness or heals a pawn.
- Apply the rule to all supported pawns with the ordinary Malnutrition condition, including
  non-colonists and animals; no new settings, tab, toggle or individual override is introduced.
- Specify guarded, merge-preserving XML behavior and clear unavailability diagnostics when the
  relevant condition/stages have changed. Correct source selectors that can hit unrelated capacities.
- Retain composition work with separately queued S046 Consciousness Patch before combined activation.

## Capabilities

### New Capabilities

- `megamod-malnutrition-consciousness`: Early malnutrition ceilings, preserved starvation behavior
  and observable compatibility/fallback boundaries.

### Modified Capabilities

None. S046 remains its own analysis/change and must directly compose the resulting capacity behavior.

## Impact

Future owning content: guarded XML transformation of Core Malnutrition's four Consciousness
modifiers and its startup target/diagnostic support. Core and **XML Extensions (`imranfish.xmlextensions`,
Workshop 2574315206)** are required for this implemented feature. XML Extensions requires Harmony
(`brrainz.harmony`); preserve that runtime dependency chain and declare dependencies in the implementing
slice. No owned gameplay Harmony patch is planned. DLC and original/provider mods are optional.
The inspected XML Extensions install advertises only through 1.6, so its exact beta loading/support
must be established before implementation; this proposal does not claim it works on 1.7.

No upstream/Framework/Gateway DLLs may be bundled; the Dev Gateway remains developer-only.
[Research](../../../mods/megamod/docs/refinement/2026-11-03-malnutrition-consciousness-research.md)
records the complete source XML, native capacity calculation and concrete third-party XML examples.
[Coverage](../../../mods/megamod/docs/replacements/malnutrition-consciousness.md) records deviations,
unfinished implementation and the absence of source API/save migration claims.
