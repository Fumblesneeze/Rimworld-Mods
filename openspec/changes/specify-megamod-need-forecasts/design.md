# Need forecast design

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.

## Context

The installed source is a 1.6 reference, not a 1.7 compatibility claim. Documentation, shipped source,
keybinding definitions and the decompiled installed DLL establish five active need handlers. Mood,
Comfort and Beauty implementations/strings exist but the registration does not select them.
See the [source report](../../../mods/megamod/docs/replacements/improved-need-indicator.md).

## Goals / Non-Goals

Goals: useful conditional estimates, native tooltip preservation, correct timing/units, clear detail
comparisons, independent per-need admission and no gameplay mutation.

Non-goals: guaranteeing future meals/jobs, simulating an entire pawn schedule, changing needs,
adding unused source features, copying old formulas without 1.7 analysis, or adding another Thing cache.

## Decisions

1. Separate pure forecast calculations from game-state capture and tooltip composition. Model actual
   category transitions and applicable rates rather than extrapolating one rate through every band.
   Deterministic calculation tests belong in Unit; loaded game contracts and tooltip action paths
   belong in isolated RimWorld acceptance, with lightweight Def fixtures only where honest.
2. The source appends to `Need.GetTipString` and `Need_Food.GetTipString`. Prefer similarly narrow
   append-only hooks, validating the result/string and need/pawn data actually consumed. Need
   calculation admission is separate from mere tooltip-method existence. Do not demand no other
   postfix owners, overwrite the complete tooltip or execute foreign transpilers speculatively.
3. Read effective native 1.7 rates/thresholds where possible; where a projection needs a future
   category's rate, inspect and guard that specific native relationship. Source private field
   `Need.pawn`, `Need_Rest.lastRestEffectiveness`, roof-need `lastEffectiveDelta`, and recreation
   `FallPerInterval` are candidate contracts to investigate, not accepted 1.7 prerequisites.
4. Source rest normalizes depletion using 0.7/0.3/0.559 category factors and multiplies rest
   recovery by the last rest effectiveness and RestRateMultiplier. Source recreation combines
   category fall factors, JoyFallRateFactor and caravan preparation. Food uses category-specific
   fall methods. Do not carry these values into 1.7 without verifying whether native methods
   already apply the factors; double application would produce plausible but wrong forecasts.
5. The source caches one last need manager and rate/detail values at about 150 ticks. Roof/enclosure
   managers define rate refresh methods but the inspected call path does not invoke them; Outdoors
   starts at zero and Indoors uses a construction-time snapshot. Our display must refresh on
   relevant state/direction changes and never reuse another pawn's estimate. Use no persistent
   prediction state. Keep UI-derived caching local and bounded, distinct from shared Thing indexing.
6. Correct misleading source edges explicitly: stale/zero-rate projections, reverse-order recovery
   thresholds, uncertain awake-bed assumptions and mixed units. The original formatter divides short
   durations by 60 and labels seconds, then divides larger durations by 2500 and labels hours.
   Our spec uses consistent in-game minutes/hours/days and explicitly conditional estimates.
7. Reuse native keybinding preferences, with a documented global-only scope exception. Source Def
   name `ShowDetails` is generic; introduce an owned name unless IN-Q3 proves a consumer requires
   a facade. Do not silently rewrite or import the old binding. No general mod settings are needed
   just to replace a held native keybinding; any future configuration uses the common system.
8. S103 [need overflow](../specify-megamod-need-overflow/design.md) retains ordinary MaxLevel while
   actual amounts can exceed it. Use its effective stage/rate policy directly, once, including food
   hunger stages and optional rest/recreation overflow rates. Traverse supported changing rates;
   otherwise expose an unavailable conditional model, not a fixed-rate exact timer. No self-patches,
   extra need index or duplicate multipliers. External providers still require effective admission.

## Target preparation

Before implementation, each real tooltip/rate adapter gets a stable target document. Pure append
hooks may require only method/result shape and consumed need access; native rate projections must
add semantic predicates for threshold units, category scaling, timing and frozen state. A private
field's existence alone is insufficient if its meaning changed. Separate per-need groups prevent
an unknown roof-need contract from disabling valid Food forecasts. A skipped enabled patch receives
the foundation warning. New keybinding Defs do not justify destructive XML patches to unrelated lists.

## Risks / Trade-offs

- Forecasts can appear more certain than the inputs → label conditions and unavailable estimates.
- Need changes may be discrete or modified by other mods → guard actual semantics and validate
  elapsed game time against a native workflow, not only arithmetic snapshots.
- Last observed rest effectiveness is not a promised future bed → state that assumption and refresh
  when actual sleeping conditions change; do not scan/select a bed on the player's behalf.
- Correcting source bugs changes display → retain every deviation in the report and test its visible outcome.
- GPL-3.0 source license is recorded as provenance → independently implement the observable contract;
  no upstream code, textures or binary is included in this documentation change.

## Migration Plan

No runtime or save migration here. Later introduce owned keybindings and guarded forecasts. There
is no source gameplay save component to reproduce. Clear transient display state on game switches.
If the original provider is active, initially suppress this feature with a warning to avoid duplicate
text. Removing the enhancement leaves ordinary needs/tooltips intact.

## Open Questions

- **IN-Q1 (game research):** finish the exact 1.7 rate/category/update-timing audit for all five needs,
  including frozen states, differing nutrition maxima and active recreation gains.
- **IN-Q2 (composition):** S103 resolves ordinary maxima, actual reserve and piecewise owned rates;
  NBO-Q1–2 retain exact effective/optional adapter verification. No blanket1.7 compatibility claim.
- **IN-Q3 (contracts):** audit actual external consumers of the source's public manager classes and
  generic keybinding Def before any compatibility facade or migration promise.
- **IN-Q4 (presentation):** confirm explanatory wording and crowded tooltip layout in all baseline
  languages; current spec intentionally labels conditional/unknown estimates and consistent units.

Record these for later; they do not block the next source's research.
