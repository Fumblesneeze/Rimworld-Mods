## Why

Pawn skills should show recognizable passion icons rather than repeated star text. S128 adopts
RimHUD Passion Icons' presentation, including admitted provider-defined passions, as a direct part
of our selected-pawn HUD while preserving the skill's meaning and native actions.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. This change **specifies later work only**; nothing is implemented or
accepted in game.

## What Changes

- Specify native minor/major passion icons and optional provider-defined icons beside skill names,
  with meaningful tooltips, readable long labels and safe text fallback when an icon is unavailable.
- Compose directly with S127's owned HUD. Preserve skill values, emphasis, click actions and the
  current pawn; add no separate settings system, bottom tab or duplicated passion state.
- Preserve provider-supplied passion identity and artwork selection without changing learning,
  skill levels, pawn generation or passion assignment. Record external contracts and unsupported
  private bindings rather than promising universal source-mod compatibility.
- Deliberately omit the source's older VSE–Prepare Moderately startup repair for the inspected
  current versions: VSE already targets the modern filter methods. Do not replay provider startup,
  duplicate randomization or quietly select a different passion as a side effect of displaying it.
- Follow S127's original-RimHUD co-installation fallback, retaining the original display and warning
  when the owned overlap cannot be admitted. Missing optional content leaves ordinary HUD use intact.

## Capabilities

### New Capabilities

- `megamod-passion-display`: truthful passion icons, layout, fallback, optional provider composition
  and non-interference with existing skill/filter behavior.

### Modified Capabilities

None. S127 remains a separately owned active draft consumed directly by this feature.

## Impact

- Documentation under this change, S128 research, replacement coverage and refinement progress.
- Future implementation uses the owned HUD and selected game. Harmony is required only for actual
  admitted game hooks; original RimHUD/Passion Icons, VSE and additional passion providers are
  optional reference/integration inputs, not required product dependencies. No XML mutation is
  selected. No dependency on another repository mod or the developer Gateway is introduced.
- No public license file was found in the pinned Passion Icons source tree. Independently engineer
  the observable contract; redistribute no upstream DLL, source or texture. Native/provider assets
  remain owned by their supplying content, and visual acceptance remains future work.
- Focused TDD, executable patch target checks, localization, independent review and final minimized
  native UI/provider workflows remain unchecked tasks. Source inspection is not 1.7 acceptance.
