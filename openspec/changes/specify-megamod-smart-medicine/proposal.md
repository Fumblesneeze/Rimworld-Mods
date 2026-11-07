## Why

Medical supplies and individual care orders should work together without repeatedly fetching small
stacks, delaying urgent treatment to restock, or changing the patient's saved general care choice.
S062 replaces the selected Smart Medicine behavior within megamod's shared medical and hauling rules.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
Origin: replacement of **Smart Medicine - Continued**, `Memegoddess.SmartMedicine`, Workshop
3526251680, plus explicit corrections recorded in the research. This change **only specifies later
work**; no gameplay implementation, compatibility acceptance or source-save migration is delivered.

## What Changes

- Give individual conditions clear care and bed-rest choices in ordinary and compact Health views;
  preserve general patient policy and use one consistent treatment decision from selection to use.
- Select permitted physical medicine from ground, doctor, patient and eligible nearby donors, with
  configurable non-urgent economy and timely emergency care. Keep ordinary treatment effects/costs.
- Allow surgery to use the best permitted recipe medicine independently of routine tending limits.
- Provide emergency, no-bed and deliberate field tending with temporary resting places and reliable
  cleanup. Combat Extended stabilization is retained as source research and deferred from initial implementation.
- Unify personal supply targets with 1.7 inventory stocking, drug retention and shared hauling
  quantities; expose targets, shortages, copy/paste and deliberate drops in pawn inventory controls.
- Move genuine configuration to common global/save settings. Pawn and condition choices remain
  gameplay state. No additional bottom tab, free medicine, extra carrying capacity or internal patches.
- Correct source query side effects, grouped reset, stale references, unsafe medicine fetching and
  stocking/hauling duplication. Preserve useful source intent rather than its accidental behavior.

## Capabilities

### New Capabilities

- `megamod-medical-care`: condition choices, medicine selection, surgery, configuration and fallback.
- `megamod-field-care`: field treatment, resting-place lifecycle and feeding; CE stabilization deferred.
- `megamod-personal-supplies`: individual targets, scarcity, real stocking/return and retention.

### Modified Capabilities

None. Existing unimplemented S007, S021, S024, S042, S055 and S058 contracts are composed explicitly
here without changing their eligibility or accepting their implementation. S080, S123, S124, S132
and S134 retain separate research entries; this change does not invent their behavior.

## Impact

Future work affects native Health/Gear/Assign controls, tending, surgery ingredients, patient rest,
inventory stocking and shared map data. Core is required; Harmony becomes required with the first
Harmony use. XML Extensions (`imranfish.xmlextensions`, Workshop 2574315206) is required with the
selected guarded declarative integrations. Neither dependency metadata nor runtime files change now.
Original/continued Smart Medicine are reference inputs, not runtime dependencies. Their presence
suppresses owned S062 overlap with a warning. External health displays, Pharmacist,
bedroll and special-patient providers are optional and require individually admitted adapters.

Exact source identities, inspected consumers, unsupported package/assembly bindings, defaults and
open questions are in [research](../../../mods/megamod/docs/refinement/2026-11-04-smart-medicine-research.md)
and [replacement coverage](../../../mods/megamod/docs/replacements/smart-medicine.md). No broad
drop-in compatibility, performance improvement or existing-save conversion is claimed.
