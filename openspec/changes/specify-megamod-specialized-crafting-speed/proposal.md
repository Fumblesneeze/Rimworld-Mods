## Why

Separate crafting speeds let traits, equipment and expertise distinguish a good tailor, sculptor
and smith without changing which work a pawn is allowed to do. Preserve that useful distinction
without deleting other mods' stats or accidentally replacing unrelated bonuses.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Origin replacement, **S122/P2-52**, De-generalize Work,
`Alias.DegeneralizeWork`, Workshop2011655761, author Alias, inspected mod version2.0.1.

## What Changes

- Supply distinct smithing, tailoring and sculpting speeds, each retaining ordinary general-labor
  effects once. Associate eligible recipes with the appropriate speed, including inherited recipes.
- Preserve native smelting, cooking and other already specialized recipe speeds. This does not
  split Work-tab categories, reprioritize hauling, add skill levels or change product quality.
- Provide the source's conservative Vanilla Skills Expanded integration by default, with an
  explicitly selected consolidated art/tailoring alternative. Preserve independent weapon,
  component and supported bionics expertise in both modes.
- Preserve consumed stat identities where practical; retain readable compatibility identities in
  consolidated mode rather than deleting them. Admit only coherent combinations; retain unrelated
  modifiers and report unsupported adapters without partially applying them.
- Place restart-dependent compatibility configuration in shared mod settings with a documented
  global-only scope exception. No new bottom tab, cheat control or operational toggle is needed.

**Specification only.** [Research](../../../mods/megamod/docs/refinement/2026-11-06-de-generalize-work-research.md)
records source behavior, exact inputs, deviations and remaining questions. No runtime behavior,
compatibility, migration or game acceptance is delivered by this draft.

## Capabilities

### New Capabilities

- `megamod-specialized-crafting-speed`: distinct crafting speeds, recipe selection, inspection,
  native work/quality boundaries and shared-consumer composition.
- `megamod-crafting-speed-integration`: optional expertise composition, conservative/consolidated
  modes, restart settings, identity preservation and safe startup admission.

### Modified Capabilities

None. S013 smelting, S014/S017 bill management and S121 query inspection consume the chosen native
recipe/stat values. S135 Basic Caravan Loading remains a separate work-assignment feature.

## Impact

- RimWorld is required. XML Extensions (`imranfish.xmlextensions`, Workshop2574315206) is required
  when this feature's guarded declarative patches ship. Harmony is required only if an admitted
  runtime adapter actually uses it; none is justified for the basic recipe/stat changes.
- Vanilla Skills Expanded, FSF bionics, Additional Traits, Primitive Tools and Numbers are optional
  external consumers/providers, not dependencies or full replacements delivered here. Installed
  Additional Traits, Primitive Tools and Vanilla Skills Rexamined declare only through1.6.
- Keep public stat names where admitted, but do not impersonate Alias' package, assemblies or XML
  patch framework. Original-provider co-installation selects the original for this overlap.
- **Intentional deviations:** repair broad factor replacement and inheritance misses; retain old
  stat bindings in consolidated mode; omit the source's commented-out Heavy hand option. Do not
  silently enable package-gated traits or claim whole-mod drop-in compatibility.
- No upstream assembly/source-code copying, new runtime library mod, per-source cache, repository
  product dependency or Dev Gateway dependency. Retain GPL-2.0 source provenance and attribution.
