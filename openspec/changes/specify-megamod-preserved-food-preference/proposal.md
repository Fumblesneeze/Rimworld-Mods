## Why

Colonists should usually eat ordinary meals while keeping suitable preserved food available for
travel and shortages. The selected source also adjusts emergency/raw-food choices and supplies
unprovisioned visiting traders; those behaviors need explicit contracts rather than a title-only port.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S057/P1-51, Prioritize Meals over Preserved Foods (`seekiworksmod.no10`,
Workshop2899127245). **Specification only; no runtime behavior delivered.**
[Research](../../../mods/megamod/docs/refinement/2026-11-04-preserved-food-preference-research.md)
distinguishes the stale included source from the selected assembly and records actual consumers.

## What Changes

- Specify finite humanlike food preferences for emergency/raw, preserved and very short-lived food,
  preserving ordinary permissions, distance and individual food concerns rather than banning rations.
- Keep separate map and caravan selection rules; preserved food remains available when appropriate,
  without changing food policies for every journey or changing animal feeding preferences.
- Scope taste compensation to the actual eater's applicable baseline reactions; preserve actual mood,
  nutrition, food categories, special beliefs and unrelated preference contributions.
- Retain a bounded arrival-provision behavior for humanlike trading visitors without edible supplies,
  correcting unsafe food/count/capacity choices and duplicate grants.
- Compose once with Common Sense's spoilage preference and Meals On Wheels' existing fallback,
  shared food history and quantity ownership. No new menu or independent cache is selected.
- **BREAKING relative to source parity:** no global food-category rewriting, arbitrary nonfood/Stuff
  provision fallback, foreign package/assembly impersonation or unchanged external startup binding.

## Capabilities

### New Capabilities

- `megamod-preserved-food-preference`: humanlike food ranking on maps and caravans, eater-specific
  taste weighting, bounded trader provisions, direct composition and safe unavailable-feature behavior.

### Modified Capabilities

None. This change consumes the existing native-first shared-food fallback and spoilage/quantity
contracts without broadening them.

## Impact

Future implementation touches only megamod's food-selection and visitor-arrival integration.
RimWorld1.7 and Harmony are required when implemented. DLCs and modded food providers are optional;
their actual content/contracts must be validated. Original mod and other repository products are
not dependencies. No copied source/data/art is permitted by the inspected source notice.

Per-eater/current-action behavior cannot be represented by static Def edits; no foreign XML mutation
is selected. Any later XmlExtensions operation requires XML Extensions (`imranfish.xmlextensions`,
Workshop2574315206), content/node checks and merge preservation. Never ship upstream assemblies or
the Dev Gateway. All code, tests, finalized balance and actual player-workflow acceptance remain open.
