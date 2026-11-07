# Thought-based mood-break severity

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S119 replacement draft. No runtime implementation, finalized balance or compatibility accepted.

## ADDED Requirements

### Requirement: DB01 Limit severity by current mood-thought groups
The feature SHALL find the highest severity supported by at least one current mood-thought group.
Default qualifying effects SHALL be at most−10 for Minor,−15 for Major and−20 for Extreme,
including equality. It SHALL use the game's actual grouped mood effect after ordinary stacking,
nullification and admitted modifiers; it SHALL NOT sum unrelated thought groups, substitute an
opinion effect, round a displayed number before comparing, or read only an unmodified base value.

#### Scenario: Several unrelated discomforts
- **WHEN** an eligible pawn has separate mood-thought groups of−8,−8 and−8, with no worse group
- **THEN** none qualifies for an ordinary mood break under default limits
- **AND** their combined effect on the pawn's mood remains unchanged.

#### Scenario: Grouped repetitions
- **WHEN** native grouping gives repeated thoughts a current combined effect of−16
- **THEN** that group supports Major severity even if no individual instance is−15
- **AND** native stacking and grouping rules determine the effect, not an invented raw sum.

#### Scenario: Exact threshold and fractional boundary
- **WHEN** the worst current group is−10,−15 or−20 under defaults
- **THEN** the supported ceiling is Minor,Major or Extreme respectively
- **AND** a group of−9.99 does not qualify as−10 merely because the UI rounds its display.

#### Scenario: Nullified social or missing thoughts
- **WHEN** a thought has no current mood effect, contributes only social opinion, or is absent
- **THEN** no negative mood value is invented for it
- **AND** an empty eligible mood-group collection supports no severity.

### Requirement: DB02 Combine thought severity with ordinary mood pressure
For an ordinary mood-break selection, the feature SHALL use the lower of the game's currently
requested mood severity and the thought-supported ceiling. It SHALL preserve the native mood
thresholds, time below them, random opportunity timing, recovery cooldown and eligibility. A
severe thought alone SHALL NOT create a break opportunity, and a reduced severity SHALL NOT reset
timers or replace the opportunity rate with that of a less distressed pawn.

#### Scenario: Distressed pawn with only a minor qualifying thought
- **WHEN** the native opportunity requests Extreme and the strongest group is−12 under defaults
- **THEN** selection begins at Minor
- **AND** the severe mood's ordinary opportunity timing is preserved.

#### Scenario: Strong thought but mild or absent mood pressure
- **WHEN** a−25 group exists but native mood pressure requests Minor or no break
- **THEN** selection remains limited to Minor or no break respectively
- **AND** this feature does not turn that thought into an immediate Extreme episode.

#### Scenario: No qualifying thought at a normal opportunity
- **WHEN** a normal mood-break opportunity occurs but no current group meets any configured limit
- **THEN** no ordinary mood break starts from that opportunity
- **AND** no catharsis, recovery cooldown, repeated roll or forced replacement episode is awarded.

#### Scenario: Native eligibility or cooldown prevents a break
- **WHEN** sleep, incapacity, life stage, an existing mental state, quest rules, recovery cooldown
  or another ordinary restriction prevents the mood break
- **THEN** a qualifying thought does not bypass it or alter the pawn's current job/state.

### Requirement: DB03 Preserve candidate rules within the selected ceiling
The feature SHALL retain ordinary candidate eligibility, relative weights and lower-tier fallback
within the allowed severity. Eligible ideological alternatives SHALL retain their normal preference
only when they have a recognized Minor, Major or Extreme severity at or below the effective ceiling;
unset/unknown and higher alternatives SHALL not escape that rule. If none remains, selection SHALL
use ordinary candidates. Anomaly selection and its
normal same-tier/non-anomalous and lower-tier fallback SHALL remain native, without additional
random choices or repeated start attempts introduced by this feature.

#### Scenario: No eligible break at the permitted tier
- **WHEN** Major is permitted but its ordinary candidate pool is empty
- **THEN** the normal search can fall to Minor
- **AND** if no eligible lower candidate exists, no episode is manufactured.

#### Scenario: Ideological shortcut exceeds the limit
- **WHEN** an otherwise eligible ideological alternative is Extreme but only Minor is permitted
- **THEN** that alternative is excluded and ordinary permitted candidates remain available
- **AND** no ideological alternative is selected when the effective ceiling is no break.

#### Scenario: Eligible ideological choice remains
- **WHEN** an ideological alternative is otherwise eligible and within the effective ceiling
- **THEN** it retains native preference over the ordinary candidate pool
- **AND** existing restrictions and relative weights still govern the eventual choice.

#### Scenario: Ideological alternative has no valid severity
- **WHEN** an otherwise eligible ideological alternative has unset or unrecognized severity
- **THEN** it cannot retain preference or hide ordinary permitted candidates
- **AND** if no other eligible ideological alternative remains, the normal ordinary fallback proceeds.

#### Scenario: Anomalous or mod-added candidates
- **WHEN** the ordinary anomaly choice or a compatible mod supplies an eligible candidate
- **THEN** ordinary eligibility, severity, weight and applicable fallback determine its availability
- **AND** the feature neither rerolls the anomaly choice per tier nor uses a fixed named-break allowlist.

### Requirement: DB04 Keep population scope and unrelated episodes distinct
The feature SHALL apply by default to all otherwise eligible ordinary mood-break selections with
usable mood thoughts. Its Colonists only option SHALL default off and, when on, use the game's
current colonist classification. It SHALL leave non-mood triggers, direct/forced episode starts,
explicit alternative selection paths and already-active episodes unchanged. Pawns without usable
mood thoughts SHALL retain ordinary behavior without a fabricated mood tracker or new eligibility.

#### Scenario: Colonists only is enabled
- **WHEN** an ordinary colonist or game-classified secured colony slave reaches a mood opportunity
- **THEN** the thought limit applies
- **AND** an outsider prisoner, visitor, raider or animal not classified as a colonist retains its
  ordinary behavior rather than becoming included merely because it is on the colony map.

#### Scenario: Scope changes or unusual colonist status
- **WHEN** recruitment, faction or secure-slave status changes before the next opportunity
- **THEN** current native classification controls inclusion
- **AND** a player colonist held elsewhere is not excluded solely by location or captivity.

#### Scenario: Direct or non-mood episode
- **WHEN** an incident, trait, gene, ability or developer action starts an episode outside ordinary
  mood-break candidate selection
- **THEN** this feature does not block or downgrade it merely because no qualifying thought exists
- **AND** counselling and recovery of an already-active state retain their own rules.

#### Scenario: No mood tracker
- **WHEN** an otherwise handled entity lacks usable mood thoughts
- **THEN** the feature leaves its ordinary behavior intact without exceptions or invented thoughts.

### Requirement: DB05 Provide common inheritable limits with consistent defaults
The feature SHALL expose the ordered controls below in the common mental-break settings section.
All SHALL use editable Global default, Current save and Use global controls. Missing save entries
SHALL inherit current globals; missing global entries SHALL use these built-ins. Invalid edits
SHALL retain the prior valid value, while malformed persisted data SHALL use the common validated
fallback with a bounded diagnostic. Changes SHALL apply at the next candidate evaluation without
rewriting thoughts, resetting native timers or ending an existing episode.

| Setting | Built-in default | Allowed values |
| --- | --- | --- |
| Minor break thought-group limit |−10|Whole numbers−100 through0|
| Major break thought-group limit |−15|Whole numbers−100 through0|
| Extreme break thought-group limit |−20|Whole numbers−100 through0|
| Colonists only |Off|On/off|

Thresholds SHALL remain independently editable, including unconventional ordering. The highest
severity whose threshold any group meets SHALL determine the ceiling; lower severities remain
permitted under it even if their separate thresholds are stricter. A zero threshold SHALL still
require an actual eligible group with effect at most zero, not make an empty group set qualify.

#### Scenario: Missing values and reload
- **WHEN** settings are first used or an existing record lacks one of these values
- **THEN** it receives the declared built-in/global inheritance behavior, not a silent zero threshold
- **AND** saved overrides and scope reload consistently.

#### Scenario: Independent thresholds and zero
- **WHEN** Extreme is−5, Major is−20, Minor is−30 and a current group is−7
- **THEN** the thought-supported ceiling is Extreme, still limited by native mood pressure
- **AND** the UI explains highest-tier precedence rather than silently reordering the user's values.

#### Scenario: Inherited and overridden saves
- **WHEN** a global limit changes while one save inherits it and another has an override
- **THEN** only the inheriting save follows that global for subsequent evaluations
- **AND** checking Use global retains a dormant override; unchecking restores it, or seeds a first
  override from the current effective global value, without writing another save.

#### Scenario: Main menu and loaded world view
- **WHEN** settings are opened at the main menu
- **THEN** global controls work while Current save and Use global are visibly and functionally
  inactive, including mutation callbacks, with an explanation and no previous save bound
- **AND** a loaded game's world view uses that active save without needing a selected map.

### Requirement: DB06 Read current shared effects without mutating pawn history
The feature SHALL evaluate current thought groups at each relevant selection, reflecting native
memory changes and owned fading exactly once. It SHALL not persist a second copy of thoughts,
replay missed opportunities after load or migration, or change thought ages, mood, skills or XP.
Normal recovery/counselling and death/transfer/save lifecycle SHALL remain owned by their existing
systems, without retaining stale pawn references or decisions across saves.

#### Scenario: Memory fades or changes
- **WHEN** a formerly−20 group now has current effect−14 through normal change or owned fading
- **THEN** its next default ceiling is Minor
- **AND** the feature does not apply fading again or keep the old Extreme decision.

#### Scenario: Load or move the pawn
- **WHEN** a pawn is saved/reloaded or moves between maps/caravans
- **THEN** subsequent ordinary opportunities use its current thoughts, active-save settings and
  preserved native timing
- **AND** no old-map cache or catch-up burst creates extra episodes.

#### Scenario: Break begins and later gets counselled
- **WHEN** an admitted break starts and an owned counselling workflow later handles it
- **THEN** ordinary state, notifications, recovery and counselling rewards remain authoritative
- **AND** this severity policy neither repeats the start nor creates another recovery reward.

### Requirement: DB07 Explain the controls without changing mood information
Owned configuration SHALL use the common mod-settings UI and English, German, Spanish, French,
Simplified Chinese, Russian and Japanese. It SHALL explain grouped effects, highest-tier precedence,
colonist classification, and that severity limits do not change mood-based warning thresholds or
opportunity timing. Existing mood bars, risk alerts and native final-straw selection SHALL remain
unchanged by this feature; no new bottom tab, cheat button or per-pawn setting SHALL be added.

#### Scenario: Mood warning remains severe while the ceiling is mild
- **WHEN** a pawn's native mood warning is severe but its thoughts permit only Minor
- **THEN** the normal mood display remains a warning about mood
- **AND** the settings explanation makes clear that the feature limits possible ordinary break
  severity rather than recoloring mood or promising no other kind of episode.

#### Scenario: Final-straw message names another thought
- **WHEN** native final-straw selection names a thought other than the group that allowed the ceiling
- **THEN** the normal message is preserved
- **AND** the policy does not replace it with an invented causal claim or trigger another break.

#### Scenario: Translate and configure
- **WHEN** a baseline language is selected and the player opens the feature's settings
- **THEN** headings, four controls, scope labels, precedence explanation and disabled-state text
  are readable without clipped controls or a separate settings implementation.

### Requirement: DB08 Admit one implementation and preserve native fallback
Every owned patch SHALL require a documented minimal target contract for actual mood selection,
thought grouping and the candidate branches it consumes. Compatible foreign changes SHALL be
allowed. Source-provider selection or false/unknown required semantics SHALL disable the affected
owned policy without removing foreign patches. Each skipped enabled patch SHALL emit one bounded
startup warning naming package, patch/spec, target, failed condition, unavailable behavior and fallback.

#### Scenario: Original provider present
- **WHEN** the original Dubs Break Mod is active and selected as provider
- **THEN** the owned overlapping feature is inactive and its controls are marked unavailable
- **AND** source behavior remains; owned default, ideological-ceiling and localization corrections
  are not falsely claimed for that provider.

#### Scenario: Compatible thoughts or candidate changes
- **WHEN** another mod changes mood effects or candidates while preserving the required semantics
- **THEN** the policy uses those current effects and preserves compatible candidate restrictions
- **AND** admission does not demand a fixed binary hash or the absence of other patch owners.

#### Scenario: Failed admission or partial installation
- **WHEN** required semantics are missing/ambiguous, checking throws or installation partly fails
- **THEN** the affected owned group stays inactive, only its newly installed patches are rolled back,
  ordinary provider/native behavior remains and each skipped enabled patch receives its bounded warning.

#### Scenario: Reinitialization later changes and evaluation failure
- **WHEN** startup repeats, supported later patching invalidates a target, or current data cannot be safely evaluated
- **THEN** no duplicate severity pass, episode, RNG retry or repeated warning spam is introduced
- **AND** the affected evaluation/policy falls back before starting an episode; later reactivation
  requires successful revalidation and cannot replay earlier opportunities.

#### Scenario: Failure after selection has begun
- **WHEN** an error occurs after a candidate evaluation has already consumed randomness or run
  potentially stateful candidate checks
- **THEN** that opportunity ends without retrying selection, starting a guessed episode or awarding recovery
- **AND** subsequent opportunities use the safely disabled policy's native/provider fallback.
