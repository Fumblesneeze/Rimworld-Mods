## Why

Timed good and bad memories should lose their mood impact gradually instead of remaining at full
strength until an abrupt expiry. S068 independently specifies All Memories Fade, including its
strength, duration and inclusion controls, with stable settings and explicit special-memory behavior.

## What Changes

- Fade included timed memory mood effects linearly over their actual lifetime, once, preserving
  ordinary thought acquisition, renewal, stacking, nullification and expiry consequences.
- Retain 100% defaults and 0–200% controls for base mood strength and ordinary memory duration;
  select memories by their unmodified duration, with inclusive bounds initially 0 days to Unlimited.
- Propose including short timed memories rather than the source's hidden greater-than-0.1-day
  cutoff. Exclude permanent and ongoing situational effects from newly imposed fading.
- Preserve explicit individual lifetimes without multiplying them again. Specify native counselling
  as a special timed positive effect whose captured amount fades without being multiplied twice.
- Correct unstable range membership, the source's legitimate −1 mood-value omission, incomplete
  live fade updates, non-finite zero-duration results and hardcoded/misleading previews.
- Use common global/save settings and current contextual mood displays. Preserve social-opinion
  strength/curve, while documenting that ordinary social-memory duration can also be scaled.

This is **documentation only**: no gameplay implementation, package change or acceptance is delivered.

## Capabilities

### New Capabilities

- `megamod-memory-fading`: Timed memory eligibility, linear mood fading, strength/duration controls,
  special lifetimes, inherited settings, clear previews and compatible lifecycle behavior.

### Modified Capabilities

None in the archived capability baseline. Existing mood/needs displays and memory-producing features
consume current thought results. The related unarchived S038 availability draft is clarified for
retained memories, pending/applied acquisition choices and refused counselling award reporting.

## Impact

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Source: Mlie's All Memories Fade, `Mlie.AllMemoriesFade`, Workshop `2800155563`, inspected version
1.7.0. [Research](../../../mods/megamod/docs/refinement/2026-11-04-all-memories-fade-research.md)
records documentation, complete binary analysis, native evidence, proposed deviations and open questions.

Harmony is **required** on implementing the planned contextual interception. No XML change is
selected: mutating shared definitions cannot provide isolated save settings. XML Extensions becomes
required only if a later actual XML contribution is selected. The original source is optional and
supplies the behavior alone when co-installed; owned controls then explain their unavailable state.
Ideology counselling support is optional and content/shape-admitted, not a DLC requirement. No other
repository mod, Gateway, upstream binary, source settings import or new map index is required.
