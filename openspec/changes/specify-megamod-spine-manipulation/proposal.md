## Why

Spinal damage already limits movement, but the inspected native Manipulation calculation does not
include spinal efficiency. S099 makes the same anatomical impairment affect manipulation-dependent
activities, while preserving ordinary injuries, implants and later health modifiers.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S099/P2-28, Spines Matter More Now**. This is specification work only; no
runtime behavior or compatibility is delivered or accepted.

## What Changes

- Scale body-based Manipulation once by current spinal efficiency, retaining native consciousness,
  limb contributions and subsequent capacity modifiers, limits and rounding.
- Preserve neutral healthy/no-spine anatomy, actual damaged or missing spinal parts, replacement
  efficiency above or below normal and the native average for multiple spinal parts.
- Let actual work/stat consumers use the resulting native Manipulation value. Do not create a
  separate penalty per job, a second movement modifier or a new injury/surgery system.
- Keep Health explanations consistent with the calculation, including ordinary native contributor
  details and the owned compact health view, without adding a tab or settings page.
- Admit the effective capacity path semantically and avoid duplicate spinal factors when an
  original/equivalent provider is present. Preserve unrelated compatible changes and bounded fallback.

## Capabilities

### New Capabilities

- `megamod-spine-manipulation`: Spinal efficiency in manipulation, anatomy/replacement behavior,
  native health/work consequences and guarded single application.

### Modified Capabilities

None. Existing consciousness, health display, work and mining-selection contracts remain unchanged.

## Impact

Future native Manipulation integration and shared health presentation are affected. Core and external
Harmony are required when implemented; no organ, race or DLC pack is required. Custom anatomies and
replacement parts use admitted native semantics, with narrow optional adapters only where necessary.
Original `jdalt.smmn` presence suppresses owned overlap without changing that provider.

No XML, static balance data, new body part, setting or entity save schema is proposed. A later XML
change would require XML Extensions and its content/package/merge guards. The source author reserves
reuse rights; independently implement this specified behavior and do not redistribute source code,
binary or assets. No Gateway dependency or speculative source API facade enters the product.
