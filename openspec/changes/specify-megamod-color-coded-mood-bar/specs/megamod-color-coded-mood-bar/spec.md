# Colonist-bar information

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. Status: specified, not implemented or accepted in game.
The settings below use the foundation's global-default/current-save inheritance rules.

## ADDED Requirements

### Requirement: Mood is readable in each eligible portrait

The colonist bar SHALL show a proportional mood fill for living pawns with a mood need. It SHALL
select the first applicable band in this order: extreme break risk, major break risk, minor break
risk, mood at most 65%, mood at most 90%, remaining mood. Risk bands SHALL use that pawn's actual
break thresholds, including threshold changes from other game rules. Equality belongs to the lower
band. The initial colors SHALL be red, coral, yellow, pale green, cyan and green respectively, at
44% opacity; players SHALL be able to customize each color and opacity.

#### Scenario: Personal break thresholds take precedence
- **WHEN** two pawns have the same mood but only one is below their own minor-break threshold
- **THEN** that pawn shows the applicable risk color and the other uses the applicable non-risk band.

#### Scenario: Non-risk boundaries and missing mood
- **WHEN** a pawn outside all break-risk bands has mood exactly 65%, exactly 90%, or above 90%
- **THEN** the bar uses pale green, cyan, or green respectively, unless those colors were customized.
- **AND** a dead pawn or a pawn without mood receives no invented mood fill.

### Requirement: Risk and schedule outlines are independently configurable

The bar SHALL offer a break-risk outline, enabled initially and drawn behind the portrait by default,
with an option to draw it over the portrait. An additional outline for the non-risk band up to 65%
SHALL be off initially. A separate schedule outline SHALL be off initially and, when enabled, use
the current scheduled activity's color, except Anything and caravan members. When the game's mood
visibility option permits risk glow, extreme risk SHALL pulse and major/minor risk SHALL be steady.

#### Scenario: Schedule changes while mood stays constant
- **WHEN** schedule outlines are enabled and a pawn's current assignment changes from Work to Sleep
- **THEN** the schedule outline changes to Sleep's color without changing the mood fill.
- **AND** selecting Anything or viewing a caravan member removes the schedule outline.

#### Scenario: Independent outline choices
- **WHEN** the player disables break outlines but retains schedule outlines
- **THEN** schedule outlines continue to work and no break outline remains.
- **AND** enabling an over-portrait break outline changes its layering without hiding selection feedback.

### Requirement: Illness warnings distinguish the worst current immunity race

Illness and pregnancy indicators SHALL share one initially enabled option. Illness indicators SHALL
consider visible illnesses that can develop immunity, excluding injuries, missing body parts and
already immune conditions. Among eligible illnesses the greatest warning SHALL be displayed:
red when severity is at least immunity, pulsing red when also above 80% severity, yellow when
immunity leads by less than five percentage points, otherwise green.

#### Scenario: Two diseases have different urgency
- **WHEN** one eligible disease has immunity ahead by ten percentage points and another has
  severity ahead of immunity at 81% severity
- **THEN** the pawn's illness indicator pulses red.
- **AND** after the urgent disease is removed or becomes fully immune, only the remaining eligible
  disease determines the warning.

#### Scenario: Immunity boundary
- **WHEN** immunity leads severity by exactly five percentage points
- **THEN** the illness indicator is green.
- **AND** no illness indicator remains after all eligible illnesses have ended or become immune.

### Requirement: Pregnancy progress has a distinct indicator

Visible human pregnancy SHALL show green before 33.3% progress, yellow from 33.3% through 66.6%,
red above 66.6% through 95%, and pulsing red above 95%. This indicator SHALL be separate from
the illness warning and SHALL disappear when pregnancy ends or the shared option is disabled.

#### Scenario: Pregnancy and illness coexist
- **WHEN** a pawn has visible human pregnancy at 96% and an eligible illness with immunity well ahead
- **THEN** the pregnancy indicator pulses red while the illness indicator remains green.

### Requirement: Bleeding warning reflects estimated time remaining

An initially enabled bleeding indicator SHALL appear only while the pawn is bleeding. Its size and
animation SHALL reflect the game's current estimate of time until death from bleeding:

| Estimate | Presentation |
| --- | --- |
| Less than 3 hours | Large, pulsing |
| At least 3 but less than 8 hours | Large, steady |
| At least 8 but less than 24 hours | Medium, steady |
| At least 24 hours, or no finite estimate while still bleeding | Small, steady |

#### Scenario: Treatment reduces the danger
- **WHEN** treatment changes a bleeding pawn's estimate from two hours to eight hours
- **THEN** the indicator changes from large and pulsing to medium and steady.
- **AND** it disappears when bleeding stops.

### Requirement: Temperature and suspension are visible without inspecting the pawn

Initially enabled temperature indicators SHALL distinguish heatstroke from hypothermia at minor
or worse stages. Minor heat SHALL use yellow and serious heat orange; minor cold SHALL use light
blue and serious cold cyan. More severe stages SHALL pulse using the serious color. Players SHALL
be able to change these four colors. An independently enabled suspension indicator SHALL identify
pawns in cryptosleep or other suspended states represented in the bar. Dead pawns SHALL not retain
living health warnings.

#### Scenario: Temperature condition progresses and resolves
- **WHEN** a pawn's heatstroke progresses from minor to serious to a more severe stage
- **THEN** its heat indicator changes from yellow to orange to pulsing orange using the configured colors.
- **AND** resolving the condition removes the indicator.

#### Scenario: A suspended pawn returns
- **WHEN** a pawn shown as suspended leaves cryptosleep
- **THEN** the suspension indicator disappears and the portrait resumes its ordinary presentation.

### Requirement: Optional relationship and identity information uses current pawn state

Relationship indicators SHALL be off initially. When enabled, spouse relationships SHALL use rings
and lover/fiancé relationships hearts, only for partners in the player's faction. Both ends of a
relationship SHALL share its color and multiple relationships SHALL receive separate indicators.
Gender indicators SHALL be off initially, with configurable colors initially blue for male, red
for female and green for no gender. Biological age in completed years SHALL be independently off
initially; enabling it SHALL append age to the name and truncate the name as needed to fit.

#### Scenario: A relationship changes
- **WHEN** two player-faction lovers marry while relationship indicators are enabled
- **THEN** their paired hearts become paired rings without leaving the old hearts behind.

#### Scenario: Age display does not require a technical option
- **WHEN** the player enables age labels
- **THEN** visible labels include biological age without requiring a separate renderer or performance toggle.

### Requirement: Equipped weapon presentation is optional and adjustable

Weapon icons SHALL be off initially. When enabled, the player SHALL choose drafted-only (initial)
or always, and SHALL see the equipped weapon's appearance rather than an arbitrary inventory item.
Scale SHALL initially be 1, adjustable continuously from 0.1 to 5; vertical offset SHALL initially
be -10, adjustable from -200 to 200 in colonist-bar display units. These controls SHALL not alter
equipment, combat rules or the portrait's clickable area.

#### Scenario: Fractional size and drafting
- **WHEN** the player selects size 0.5 with drafted-only display and drafts an armed pawn
- **THEN** a half-size weapon icon appears, remaining visible rather than rounding down to zero.
- **AND** undrafting or unequipping the weapon removes that icon on the next display update.

### Requirement: Portrait filters have explicit reduced presentations

The player SHALL choose all pawns (initial), none, current map, currently on screen, break risk,
or troubled pawns. Break risk means any of the three mood-risk bands. Troubled SHALL include risk,
drafted, dead, needing tending, mental state, burning, idle, fleeing or actively attacking pawns.
Pawns excluded by the selected filter SHALL use one chosen reduced presentation: completely hidden
(initial), name only, name and frame, or name/frame/portrait without the added mood and condition
overlays. A completely hidden entry SHALL show no stray glow, weapon or other added overlay.
Filters SHALL update when the condition ceases, not latch permanently.

#### Scenario: Trouble ends
- **WHEN** a pawn visible solely because it was drafted is undrafted and has no other trouble
- **THEN** the troubled-pawns filter returns it to the chosen reduced presentation.

#### Scenario: View changes
- **WHEN** current-map filtering is active and the player changes the viewed map
- **THEN** portraits from the previous map become reduced and portraits from the new map become eligible.
- **AND** on-screen filtering additionally responds to the camera's visible area.

### Requirement: Native colonist-bar actions and information remain usable

Eligible portraits SHALL preserve native selection, highlighting, reordering, draft feedback,
death presentation and activity information, including mental states, medical rest, sleeping,
caravan formation, combat, fleeing, idle, burning, inspiration, roles and guest/faction indicators
where applicable. The feature SHALL not introduce a new bottom tab or change pawn behavior.
An optional refresh-on-draft presentation setting SHALL be off initially. Visible name labels SHALL
retain a translucent red health strip when summary health is below 99.9%, with width proportional
to remaining summary health. Name coloring and the drafted underline SHALL remain legible. A
completely hidden entry SHALL hide its label and health strip together.

#### Scenario: Select and draft through normal controls
- **WHEN** the player selects a visible portrait, drafts the pawn and reorders the portrait
- **THEN** native selection, draft feedback and the new order work with the added information present.
- **AND** an ended activity does not leave its old indicator behind.

#### Scenario: Health changes beneath the name
- **WHEN** a pawn's summary health decreases below 99.9% and later recovers
- **THEN** its visible label shows a red strip proportional to remaining summary health, updates
  as health changes, and removes the strip at 99.9% or above.

### Requirement: Configuration follows the shared settings workflow

All configuration SHALL be grouped in the colonist-bar section of megamod settings and searchable
by feature, category, name and description. Each setting SHALL have a global default and an
optional save override. The current-save editor SHALL be inactive while Use global is checked
and at the main menu; main-menu global editing SHALL remain available. Display update interval
SHALL initially be 0.15 seconds, adjustable from 0.015 to 2 seconds. Changing a display setting
SHALL take effect without reloading; disabling an indicator SHALL remove it on the next redraw.
Reset SHALL restore the same defaults as a first installation within the chosen scope.

#### Scenario: Save override and reset
- **WHEN** one save overrides relationship indicators to on and the global default remains off
- **THEN** that save shows relationships while an inheriting save does not.
- **AND** resetting the overriding setting restores off, matching a new installation.

#### Scenario: No current save
- **WHEN** the player opens these settings from the main menu
- **THEN** global controls work and all current-save controls, including reset and Use global, are inactive.

### Requirement: Unavailable enhancements leave a usable colonist bar

If required game or optional-provider behavior is incompatible, the affected enhancement SHALL
remain inactive, preserve the usable native/provider bar, and produce a bounded startup warning
identifying the unavailable behavior and reason. Unrelated compatible changes SHALL not alone
disable it. If the original CM provider is also active, megamod SHALL initially leave this feature
inactive and explain that the original provider is supplying it rather than draw both versions.

#### Scenario: Unsupported bar provider
- **WHEN** startup finds an enabled enhancement cannot safely work with the current bar
- **THEN** startup logs explain the skipped enhancement and the available fallback.
- **AND** unrelated compatible enhancements remain available.
