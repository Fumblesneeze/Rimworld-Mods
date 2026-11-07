## Why

A stranded colonist with an infected expendable body part needs a desperate, costly chance to survive.
Cut It Off supplies that idea, but its selected build disagrees with its advertised solitude rule
and can lose track of the chosen medicine or report survival without confirming removal.

Owner: **Make Rimworld Great Again**, package `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Origin replacement S056/P1-50, Cut It Off
(`LegendaryMinuteman.CutItOff`, Workshop3628405994). **Specification only; no gameplay delivered.**
See [source research](../../../mods/megamod/docs/refinement/2026-11-04-cut-it-off-research.md).

## What Changes

- Specify a contextual emergency action for an eligible infected colonist with no other actually available
  medical helper, checking capability, assignment and access rather than population or mere mobility.
- Make the chosen infected part, medicine, chance and permanent consequences clear before starting.
  A worse untreatable infection does not hide another eligible part.
- Use one permitted medicine unit consistently, or an explicitly confirmed lower-chance unmedicated
  attempt with a substantially more forgiving50–90% skill curve; medicine never lowers this chance
  or its original medicine-based chance. Retain work time, injury consequences and living memories.
- Require actual removal and truthful survival feedback, safe interruption/save recovery, and no
  automatic retries, arbitrary healthy surgery, harvested products or duplicated treatment.
- Compose self-tending and shared medicine/cargo ownership directly with other owned features.
  Record deliberate source deviations and remaining product/runtime questions without stopping research.
- **BREAKING relative to source parity:** source job/Def/type identities are not impersonated;
  automatic source-save migration and simultaneous overlapping providers are not promised.

## Capabilities

### New Capabilities

- `megamod-emergency-self-amputation`: contextual eligibility, infected-part choice, medicine and
  work costs, physical outcomes, truthful feedback, recovery and unavailable-feature behavior.

### Modified Capabilities

None. Existing self-tend, health and cargo capabilities are consumed without changing their contracts.

## Impact

Future implementation belongs only to megamod's pawn medical action, owned job and outcome data.
RimWorld1.7 is required. Harmony becomes required when its command integration is implemented.
No DLC, original source mod or other repository product is required. Optional modded anatomy or
medical providers need demonstrated narrow integration; unknown removal semantics stay unavailable.
Owned Job/Thought/Tale declarations are planned; no foreign XML mutation is selected. Any later
XmlExtensions operation makes XML Extensions (`imranfish.xmlextensions`, Workshop2574315206) required
and needs guarded merge-preserving behavior. Never ship upstream assemblies/assets or the Dev Gateway.

The source installed build, provenance, possible consumers, save boundaries and unverified balance
are documented separately. All implementation, tests and native player-workflow acceptance remain open.
