# Need forecasts

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Status: specified, not implemented. These forecasts describe current conditions, not guaranteed
future actions or scheduled meals, sleep, recreation or movement.

## ADDED Requirements

### Requirement: Forecasts extend existing need tooltips

Hovering Food, Rest, Recreation, Indoors or Outdoors SHALL retain the original tooltip and append
the supported forecast. Need types absent from a pawn SHALL not be invented. Unsupported needs,
including Mood, Comfort and Beauty in this change, SHALL retain their existing descriptions without
a fabricated forecast. Merely hovering SHALL not change a need, job, schedule or pawn decision.

#### Scenario: Compare different pawns
- **WHEN** the player hovers Food for one pawn and then Rest for another
- **THEN** each tooltip shows the correct pawn's current forecast and retains the corresponding original text.
- **AND** no value from the first pawn remains in the second pawn's forecast.

### Requirement: Food forecasts follow the pawn's actual hunger progression

The Food forecast SHALL estimate time to each relevant remaining hunger boundary, including hungry,
ravenously hungry and empty nutrition/starvation onset. It SHALL use the pawn's actual capacity,
thresholds and consumption conditions, accounting for different depletion rates in successive hunger
stages. The zero-food estimate SHALL not claim to predict death or a later malnutrition severity.
Already passed boundaries SHALL not be presented as future events. Eating or changing metabolism
SHALL update the forecast, assuming no further food intake.

#### Scenario: Eat before becoming hungry
- **WHEN** a pawn eats and gains nutrition under otherwise unchanged conditions
- **THEN** the displayed time to the next hunger boundary increases to reflect the actual new level.
- **AND** the tooltip does not assume another future meal is guaranteed.

#### Scenario: Capacity differs
- **WHEN** two pawns have different nutrition capacities or consumption rates
- **THEN** their forecasts use their own values rather than treating both food bars as one fixed amount.

### Requirement: Rest forecasts distinguish depletion and recovery

While awake, Rest SHALL show the remaining time to relevant tiredness boundaries and an estimate
of rest required to reach full rest. While actually resting, it SHALL show time to full rest under
the current recovery conditions. The recovery estimate SHALL reflect the pawn's recovery factors
and effective sleeping conditions, including bed effectiveness when used. An awake estimate based
on the last observed recovery conditions SHALL say so; it SHALL not imply that an unselected or
newly assigned bed has already been measured. Unknown recovery conditions SHALL produce an
unavailable estimate rather than a precise invented time.

#### Scenario: Change sleeping conditions
- **WHEN** a pawn starts sleeping in a bed with different effective rest recovery
- **THEN** the rest forecast updates to those actual conditions instead of retaining the old bed's estimate.

#### Scenario: Wake up and continue working
- **WHEN** the pawn wakes before fully rested
- **THEN** the tooltip distinguishes future awake depletion from the conditional time needed to recover.

### Requirement: Recreation forecasts describe depletion separately from active enjoyment

Recreation SHALL estimate time to the remaining satisfaction and deprivation boundaries under the
pawn's current depletion conditions, accounting for changing rates across categories and current
caravan preparation where applicable. It SHALL not present a depletion estimate as a prediction of
how long the current activity will take to fill recreation. When recreation is being gained, any
retained depletion projection SHALL be clearly labeled as assuming the gain stops.

#### Scenario: Start an ordered recreation activity
- **WHEN** a pawn starts gaining recreation, including through the directed-recreation feature
- **THEN** the tooltip reflects its updated level and identifies any depletion projection as conditional.
- **AND** it does not show that projection as time to finish the activity.

### Requirement: Indoor and outdoor forecasts follow the current environment

Indoors and Outdoors SHALL show reachable category transitions in the current direction of change,
using the pawn's current environment and conditions. Movement between enclosed, roofed, open or
underground surroundings, and sleeping when relevant, SHALL update the projection. Recovery
forecasts SHALL list transitions in the order they will be reached; they SHALL not imply that a
farther transition occurs before a nearer one.

#### Scenario: Leave an enclosed room
- **WHEN** a pawn moves from a room to surroundings that reverse its Outdoors need trend
- **THEN** the forecast switches from deterioration to recovery and uses the new rate.
- **AND** the old deterioration countdown does not remain as the current prediction.

### Requirement: Detailed mode provides meaningful comparisons

The tooltip SHALL enter detailed mode while the configured key, initially either Control key, is
held over Food, Rest or Recreation, showing the current forecast and time from a full need to the same
remaining boundaries under the stated conditions. Rest SHALL additionally show the recovery time
from each displayed boundary back to full rest. Releasing the key SHALL restore the basic view.
Indoors and Outdoors SHALL keep their ordinary forecast without promising extra details they do
not supply. The prompt SHALL show the actual binding after rebinding.

#### Scenario: Compare current rest with a full-rest reference
- **WHEN** the player holds the details key over a partly depleted Rest bar
- **THEN** the tooltip distinguishes the pawn's current remaining time, the full-rest reference and
  the corresponding recovery time instead of presenting three unlabeled timers.

### Requirement: Time units and uncertainty are understandable

All forecasts SHALL explicitly use **in-game time**, with localized units: minutes below one hour,
hours below one day, and days for longer durations, shown to two decimal places. A tiny positive
duration that would round to zero SHALL be shown as less than 0.01 minute. Time SHALL not become a
different estimate merely because game speed changes. Pausing SHALL stop countdown progression.
Frozen needs, a zero rate, an unreachable boundary or an unsupported calculation SHALL show a
stable/unavailable explanation as appropriate, never a negative duration, invalid number or false
promise of immediate exhaustion. Estimates SHALL account for the game's actual need-update timing.

#### Scenario: Pause and change speed
- **WHEN** the player pauses while inspecting a need, then resumes at a faster speed
- **THEN** the paused forecast stays fixed and later decreases with elapsed game time, not elapsed wall time.

#### Scenario: Need stops changing
- **WHEN** a supported need becomes frozen or its effective rate becomes zero
- **THEN** a previous finite countdown is replaced with an appropriate explanation until a reliable trend returns.

### Requirement: Details input uses the game's global keybinding preferences

The details key SHALL be rebindable through the normal keybinding menu and remain a held action,
not a persistent mode toggle or new bottom-bar control. This binding SHALL be a documented
global-only exception to save inheritance because it is an input preference managed by the native
keybinding system; switching saves SHALL not remap a player's controls. No source-specific settings
window or new gameplay tab SHALL be introduced.

#### Scenario: Rebind and switch saves
- **WHEN** the player changes the details binding and loads a different save
- **THEN** the same global binding works and the tooltip prompt names it correctly.

### Requirement: Compatible additions survive and unsupported forecasts fail independently

Other compatible tooltip text SHALL be preserved. A forecast whose required game behavior cannot
be established SHALL remain inactive with a bounded startup warning naming the affected need;
unaffected forecasts and original tooltips SHALL remain usable. With the original Improved Need
Indicator active, megamod SHALL initially defer this feature to that provider with an explanation
rather than append duplicate forecasts. Supported changes to maximum need or overflow SHALL use
the actual current level and supported thresholds, not silently clamp the estimate to vanilla values.

#### Scenario: One need calculation changes incompatibly
- **WHEN** a current game or external provider makes the Rest forecast unsupported
- **THEN** startup identifies Rest forecasts as unavailable, Rest retains its normal tooltip, and
  independent supported Food forecasts continue working.

#### Scenario: Above-normal need level
- **WHEN** a supported overflow rule permits a need above its ordinary full level
- **THEN** its current forecast includes that additional reserve and clearly distinguishes the
  reference full-level comparison from the actual current amount.
