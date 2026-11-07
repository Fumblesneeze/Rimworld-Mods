## Why

Players want capable builders to finish quality-bearing construction and to repeat a disappointing
result without manually replacing each blueprint. This must remain ordinary costly construction,
with bounded retries and cancellation that cannot unexpectedly destroy or recreate a building.

## What Changes

- Specify per-building quality targets, bulk blueprint/frame orders and contextual finished-building
  improvement, with automatic defaults and all seven native quality levels.
- Preserve relative-to-best builder eligibility, an absolute skill bypass, optional mech exclusion
  and deliberate forced orders. Move a named builder assignment out of configuration into contextual
  map gameplay controls.
- Repeat native deconstruction/rebuilding while preserving the construction intent, material,
  position, rotation, style and supported building configuration. Keep native work, resource returns,
  construction failures and quality rolls; no free quality upgrade.
- Retain the source default of three quality replacement cycles, with explicit counting, exhaustion,
  restart and cancellation. Correct stale completion flags, canceled-order confusion and mutation
  during work/menu queries rather than reproducing those source risks.
- Use common global/save configuration and map-correct eligibility data. Record actual VEF,
  Replace Stuff and retexture contracts without package impersonation or speculative parity claims.

This change **specifies later work only**, derived from QualityBuilder Unofficial 1.6. Safe retargeting,
explicit restart, ordinary-order ownership and configuration preservation are deliberate proposed
corrections. Remaining native/provider and presentation work is recorded, not a request for the
user to explain the source mod.

## Capabilities

### New Capabilities

- `megamod-quality-construction`: Targeted quality construction, builder eligibility and bounded
  native replacement cycles with recoverable state and explicit cancellation.

### Modified Capabilities

None. Later selected Replace Stuff and construction features must compose this behavior directly.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Source: **QualityBuilder Unofficial 1.6**, `hatti.qualitybuilder`, Workshop **3512466087**, S013 / P1-10.
See [source report](../../../mods/megamod/docs/replacements/quality-builder.md) and
[research](../../../mods/megamod/docs/refinement/2026-11-02-quality-builder-research.md).
The inspected source metadata supports only through 1.6; local 1.7 code inspection is not a port or
compatibility acceptance. Public source and exact installed DLL identity are recorded in research.

Future code affects construction orders, finishing eligibility, saved target/retry state and native
replacement lifecycle. Repeated work deliberately consumes resources and can temporarily remove a
useful building. QualityBuilder itself is a reference, **not a runtime dependency**. VEF and external
Replace Stuff are optional integration cases; no source facade or retexture compatibility is promised.
No dependency change occurs in these documents. Harmony is required if used; any XML Extensions
operation requires XML Extensions. Upstream assets/assemblies and the developer Gateway stay out of
the product. No quality probabilities, construction costs or skill experience rates are rebalanced.
