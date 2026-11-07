## Why

Repeatedly selecting the same crop for individual hydroponics basins is avoidable administration.
S076 Linkable Hydroponics supplies the reference for persistent crop groups; megamod should make
their initial state, membership changes and eligibility as clear as its linked growing zones.

## What Changes

- Specify contextual crop linking for player-controlled growing buildings, including compatible
  optional growers, with persistence, member selection, identification, unlinking and disbanding.
- Immediately adopt the identified first selected member's crop when linking. Move only selected
  members, rather than silently merging every unselected member of their previous groups.
- Offer crops valid for the entire affected group and recheck before applying a choice. Retain
  normal crop warnings, work, power, plant growth and physical lifecycle behavior.
- Share proven grouping operations with S006 while keeping building crop groups separate from zone
  crop/sowing/cutting groups. Mixed building/zone membership is a later scope question.
- Keep optional provider authority, missing-crop recovery and failed integration explicit.

## Capabilities

### New Capabilities

- `megamod-linked-growing-buildings`: Persistent, contextual crop groups for growing buildings.

### Modified Capabilities

None. S006 remains zone-only; its design receives a composition cross-reference, not new requirements.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This change specifies future work only; it delivers no gameplay and accepts no runtime tasks.

Reference: Yakobo's **Linkable Hydroponics**, `Yakobo.LinkableHydroponics`, Workshop3535816519.
The inspected source is 1.6-labelled; it is research input, not a dependency or beta compatibility proof.
Harmony will be required on first runtime hook use. No static Def change or XML Extensions operation
is selected for this map-owned grouping feature; any later XML contribution requires its own target
spec and XML Extensions dependency. The optional grower source inspected is
**Vanilla Furniture Expanded - Farming**, `VanillaExpanded.VFEFarming`, with
its **Vanilla Expanded Framework**, `OskarPotocki.VanillaFactionsExpanded.Core`. Neither is required
by megamod. The original linking provider takes authority when active; our building linking stays off.
No upstream assemblies, package aliases, automatic group import or Gateway dependency are introduced.

[Source research](../../../mods/megamod/docs/refinement/2026-11-04-linkable-hydroponics-research.md)
and [replacement report](../../../mods/megamod/docs/replacements/linkable-hydroponics.md) distinguish
source behavior, deliberate corrections, remaining research and future verification.
