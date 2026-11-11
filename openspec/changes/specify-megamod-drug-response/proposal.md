## Why

Pain relief and condition-specific medication currently require repeated manual orders, while Drug
Response's separate bottom tab hides saved rules behind single-condition selectors. The user wants
its behavior independently specified with a redesigned UI, and one coherent result alongside
Priority Treatment's automatic medication orders.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S124, replacement of **Drug Response (Continued)**, `Mlie.DrugResponse`, Workshop2196401201.
This change delivers a documentation draft only. No gameplay implementation or compatibility
acceptance is claimed. Detailed provenance and distinctions between fact and proposal are in
[research](../../../mods/megamod/docs/refinement/2026-11-06-drug-response-research.md).

## What Changes

- Specify four pain bands, up to two responses per supported condition, self-administration,
  medical administration, caravan use, eligibility, stock access, dose history and policy boundaries.
- Replace the source's configuration tab with ordered mod-settings sections, a searchable saved-rule
  list and per-condition Global default / Current save / Use global controls. One local condition
  override leaves other conditions inheriting. Keep patient status,
  response holds, ordinary drug policies and operations contextual to the patient.
- Correct source order-time cooldowns, first-dose delay, forced-job interruption, premature success
  messages, unavailable-drug handling and second-rule editing. Preserve actual consumption, time,
  costs, side effects, medical requirements and player authority.
- Specify one pending-dose authority shared directly with later S134 Priority Treatment. Addiction
  permission, actual unmet chemical need, medical work priority and existing operations remain
  distinct from general pain/condition rules. S134 retains its own full specification task.
- Keep S137's condition-click medical-recipe filtering a separate explicit discovery action. Neither
  a match nor a click authorizes an automatic operation.

## Capabilities

### New Capabilities

- `megamod-drug-response`: Observable response rules, scope, settings and patient UI, real map and
  caravan doses, cancellation/history, shared medical ordering and compatibility fallback.

### Modified Capabilities

None. Compose with S032 drug-policy maintenance and S062 medical care/supplies without changing
their policy definitions, medicine ceilings, quantities or donor permissions.

## Impact

- RimWorld is required. Harmony is required when actual owned hooks use its types; XML Extensions
  (`imranfish.xmlextensions`, Workshop2574315206) is required with any declarative XML integration.
  No source assemblies, source runtime dependencies, or repository product/Gateway dependencies.
- Drug and illness providers, DLC and UI providers are optional, package-resolved and shape-admitted.
  An absent provider supplies no content. Genuine gameplay-content adapters are catalogued in design;
  each needs individual admission and native evidence before being called supported.
- Active original Drug Response or Priority Treatment produces a bounded startup warning and
  requests the ordinary per-feature settings disable path for owned overlapping scheduling,
  best effort. This is not a cooperative handoff or compatibility guarantee. Never remove,
  configure or unpatch another provider; no original-provider compatibility run is required.
- **Intentional contract changes:** configuration navigation, successful-dose timing, safe automatic
  interruption and bounded pending-order ownership. No upstream save/settings import, package
  spoof, API/binary compatibility facade, consumer-parity checks or multiplayer guarantee is included.
