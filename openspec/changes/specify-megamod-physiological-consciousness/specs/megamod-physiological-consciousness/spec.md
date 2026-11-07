# Physiological contributions to consciousness

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, S046 Consciousness Patch. Specification draft; no implemented gameplay claim.

## ADDED Requirements

### Requirement: CO-01 Improved physiological capacities contribute to consciousness
The mod SHALL allow supported blood pumping, breathing and blood filtration above 100% to improve
consciousness for every pawn using the ordinary supported calculation. Default relative weights
are 20%, 20% and 10% respectively. Starting from consciousness after brain and pain effects, each
input changes the current result by its weight times that input's difference from 100%; contributions
act successively and multiply together. Inputs below 100% retain their corresponding impairment.
Ordinary later condition effects, limits and rounding still apply. These examples assume supported
finite values, no later effects and a 100% brain/pain baseline unless stated.

#### Scenario: Ordinary baseline remains ordinary
- **WHEN** all three physiological capacities are 100% with default weights
- **THEN** the contribution result remains 100%.

#### Scenario: Enhanced pumping gives a relative benefit
- **WHEN** pumping is 150%, other inputs are 100% and default weights apply
- **THEN** the result is 110%; at a 50% brain/pain baseline the corresponding result is 55%.

#### Scenario: Filtration uses its own lower default weight
- **WHEN** filtration is 150% and the other inputs are 100% with default weights
- **THEN** the result is 105%.

#### Scenario: Combined benefits multiply
- **WHEN** all three inputs are 200% with default weights and no input limit
- **THEN** the result before ordinary final rounding is 158.4%, displayed through normal rounding
  as 158%, rather than a sum of three percentage-point bonuses.

#### Scenario: Impairment still matters
- **WHEN** pumping is 50% and the other inputs are 100% with default weights
- **THEN** the result is 90%; raising the pumping weight to 40% instead yields 80%.

### Requirement: CO-02 The optional ceiling limits each contributing input
The mod SHALL provide a common optional ceiling on each of the three physiological inputs, default
off, with a retained value of 300% and allowed values from 100% through 1000%. This ceiling SHALL NOT
be presented or applied as a maximum on final consciousness. Turning it off retains its stored value.

#### Scenario: Disabled ceiling does not constrain an enhanced input
- **WHEN** pumping is 400%, other inputs are 100%, weights are default and the ceiling is off
- **THEN** a 100% brain/pain baseline becomes 160% before later health effects.

#### Scenario: Enabled ceiling limits the input before contribution
- **WHEN** the same pawn has a 300% input ceiling enabled
- **THEN** pumping contributes as 300% and the result is 140% before later health effects.

#### Scenario: Inputs below the ceiling are not raised
- **WHEN** a 300% ceiling is enabled while pumping is 50% and the other inputs are 100%
- **THEN** default weights still give 90% from a 100% baseline; the ceiling supplies no minimum.

#### Scenario: A disabled ceiling retains its next-use value
- **WHEN** the player changes the ceiling while it is off and later enables it
- **THEN** the retained value is used and the menu explains that it was dormant while disabled.

### Requirement: CO-03 Native health consequences and condition ceilings remain authoritative
The mod SHALL preserve brain and pain effects, other ordinary health contributions, health limits,
rounding and the game's wakefulness, downing, death and recovery rules. Resulting capacity changes
may change native health outcomes; the feature SHALL NOT force a pawn awake, heal it, create an
organ or bypass a condition limit. Supported non-colonists and animals receive the same calculation.

#### Scenario: Malnutrition constrains physiological improvements
- **WHEN** a pawn has a 100% brain/pain baseline, all three inputs at 200%, default weights, no input
  ceiling and S045 trivial malnutrition with its 95% consciousness ceiling
- **THEN** the physiological result is 158.4% before later effects, but final consciousness is at
  most 95%; the remaining malnutrition stages retain their respective 90/80/70/10% ceilings.

#### Scenario: A stronger condition remains limiting
- **WHEN** another ordinary health condition limits consciousness below the physiological result
- **THEN** that stronger limit still governs, and native unconsciousness/downing/death rules apply.

#### Scenario: Health outcomes can change without an override
- **WHEN** a legitimate capacity change crosses a native wakefulness or health threshold
- **THEN** the game determines the resulting state through its normal rules; changing policy back
  does not promise to reverse damage, death or other consequences already incurred.

#### Scenario: Scope follows the supported capacity calculation
- **WHEN** an animal or non-player pawn uses the same supported consciousness calculation
- **THEN** the same contribution policy applies without granting new organs, capacities or needs.

### Requirement: CO-04 Configuration uses common global and save controls
The mod SHALL expose five policy values in its structured settings: input-ceiling enablement,
input-ceiling value, and pumping/breathing/filtration weights. Weights accept 10–100% and default to
20/20/10%. Each row SHALL show Use global, Current save and Global default using the common model.
Missing save choices inherit current globals; inherited editors are inactive. Checking Use global
retains the dormant override; unchecking restores it or starts from the current global value.
Main-menu save controls and save mutations SHALL be inactive. Labels and help explain relative
weights and input limits in all supported languages. No new bottom tab or gameplay toggle is added.

#### Scenario: A new save inherits current defaults
- **WHEN** a save has no stored choices for these rows
- **THEN** all five use the current globals, with inactive save editors while Use global is checked.

#### Scenario: An override remains separate from global changes
- **WHEN** a save overrides pumping weight and the player changes the global pumping weight
- **THEN** that save retains its override while inherited saves use the changed global value.

#### Scenario: Inheritance retains the player's previous override
- **WHEN** the player checks and later unchecks Use global for a previously overridden value
- **THEN** the dormant override is restored without writing another save or changing the global.

#### Scenario: The main menu cannot edit a save
- **WHEN** settings are opened without an active game
- **THEN** global values remain editable, while Current save and Use global are disabled with an
  explanation and cannot mutate a previously played or implicit save.

#### Scenario: Help distinguishes weight from final capacity
- **WHEN** the player inspects pumping-weight or input-ceiling help
- **THEN** the menu explains that 150% pumping at 20% weight gives a 10% relative contribution,
  that higher weights also worsen low-input impairment, and that the ceiling limits inputs.

### Requirement: CO-05 Policy changes become effective together at the next unpaused update
The mod SHALL store valid completed setting edits and apply the resolved policy to the active save
at the next unpaused game update before ordinary work advances. Until then the previous applied
policy remains effective; paused edits show a pending explanation and do not advance simulation.
Health information and actual health/work outcomes SHALL agree once the new policy is applied.
Loading a save SHALL use its own resolved policy before its first ordinary update.

#### Scenario: Paused changes have a visible pending state
- **WHEN** the player changes several policy values while paused
- **THEN** the old applied policy remains effective, the menu marks the change pending, and the
  latest valid complete policy takes effect together on resuming before ordinary work advances.

#### Scenario: Existing pawns receive the changed policy
- **WHEN** a committed change becomes effective in an active game
- **THEN** existing supported pawns, including those away from the displayed map, use the new
  policy and ordinary health presentation and consequences reflect that same policy.

#### Scenario: Save switching cannot retain the previous save's policy
- **WHEN** the player loads a save whose effective values differ from the previous save
- **THEN** the loaded save's own effective policy governs its first ordinary update and health
  results, with no carried-over override from the previous save.

#### Scenario: Invalid choices do not become health policy
- **WHEN** a setting cannot be represented as a valid value within its stated range
- **THEN** the last valid choice remains effective, or built-in defaults apply if none exists,
  and the settings interface explains the invalid choice rather than silently applying it.

### Requirement: CO-06 Unsupported calculation behavior has an explicit ordinary fallback
The mod SHALL enable the physiological change only when the required calculation behavior is
supported. Compatible unrelated changes SHALL remain effective. An active original Consciousness
Patch or an unsupported required behavior leaves the owned overlap unavailable, preserves the
ordinary/provider result and reports a bounded startup warning identifying the skipped change,
reason and fallback. The settings interface SHALL distinguish stored choices from unavailable
behavior. Availability of S045's separate supported malnutrition policy remains independent.

#### Scenario: Compatible health additions remain present
- **WHEN** another provider adds a supported ordinary health contribution without replacing the
  required physiological behavior
- **THEN** its contribution remains present alongside the owned feature and ordinary limits.

#### Scenario: The original provider is active
- **WHEN** the original Consciousness Patch is active
- **THEN** the owned overlap is unavailable with an explanatory startup warning, and the original's
  result is left in charge instead of receiving a duplicate physiological benefit.

#### Scenario: A required contribution cannot be supported
- **WHEN** the game or a provider changes one required contribution so its meaning is unknown
- **THEN** none of the owned three-contribution change takes effect, the ordinary/provider result
  remains available, and the warning and settings explain the unavailable behavior without repeated
  per-pawn warnings.
