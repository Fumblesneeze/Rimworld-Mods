# Spinal contribution to manipulation

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S099/P2-28. Specification only; no runtime implementation or acceptance.

## ADDED Requirements

### Requirement: Apply spinal efficiency once to body-based manipulation
The mod SHALL multiply the Manipulation contribution from the pawn's manipulation limbs and
consciousness by that pawn's current spinal efficiency, exactly once.
Use the same spinal-efficiency meaning used by the game's ordinary movement calculation, not
movement speed, overall Moving capacity or a new spine-health percentage. Apply this factor before
ordinary direct Manipulation offsets, multipliers, maximum/minimum limits and final rounding.
Preserve those later rules and their normal ordering, including the native behavior when the
body-based result is zero. Do not add a second consciousness contribution or change Moving.
This is the baseline behavior whenever the contribution is supported; no new strength slider or
per-pawn toggle is introduced.

#### Scenario: Reduced spinal function
- **WHEN** limb function and consciousness together contribute 80% Manipulation and spinal efficiency is 50%, with no later modifiers
- **THEN** Manipulation is 40%, rather than retaining 80% or receiving the spinal penalty twice.

#### Scenario: Preserve later health offsets
- **WHEN** the body-based contribution is 100%, spinal efficiency is 50% and an ordinary later modifier subtracts ten percentage points
- **THEN** the resulting Manipulation is 40%, not 45%, before any other ordinary limits.

#### Scenario: Healthy spine with bad back
- **WHEN** spinal efficiency remains 100% but the pawn has the native bad-back condition and otherwise ordinary 100% manipulation inputs
- **THEN** the condition's existing ten-percentage-point Manipulation penalty remains, without inventing an additional spinal-efficiency loss from its name.

#### Scenario: Preserve consciousness and later limits
- **WHEN** an admitted consciousness change alters the limb-and-consciousness contribution, or a later health effect caps Manipulation
- **THEN** the one spinal factor uses the current contribution and the existing later cap still applies.

### Requirement: Follow actual anatomy and replacement efficiency
The contribution SHALL follow the pawn's actual supported spinal anatomy, irrespective of faction
or whether the pawn is a colonist.
A healthy 100% spinal efficiency is neutral. Anatomy with no spinal parts is also neutral; it is
not equivalent to a body whose existing spinal parts have been lost. Missing parts, damaged parts,
added parts and replaced ancestors retain their ordinary game-defined efficiency. Where multiple
spinal parts contribute, use their ordinary arithmetic mean, including nonfunctional parts, rather
than multiplying the parts together or selecting only the best. A valid efficiency above 100% can
improve body-based Manipulation without a new 100% ceiling. This feature does not add Manipulation
to a creature whose anatomy cannot have it or give animals/mechs new jobs or capabilities.

#### Scenario: Anatomy without a spine
- **WHEN** a supported pawn can manipulate but has no spinal part in its anatomy
- **THEN** its Manipulation remains unchanged by this contribution.

#### Scenario: Loss of an existing spine
- **WHEN** the only spinal part has zero effective function and ordinary capacity rules have a zero minimum
- **THEN** body-based Manipulation is zero and normal downstream incapacity rules apply, rather than treating the missing part as absent anatomy.

#### Scenario: Multiple spinal parts
- **WHEN** two supported spinal parts have efficiencies of 100% and 50%, with otherwise ordinary manipulation inputs
- **THEN** the spinal factor is 75%, not 50% or 100%.

#### Scenario: Native bionic replacement
- **WHEN** an ordinary vanilla bionic spine successfully replaces a damaged spine and provides 100% spinal efficiency
- **THEN** its spinal factor returns to neutral while all remaining health and limb conditions continue to affect Manipulation.

#### Scenario: Enhanced supported spinal part
- **WHEN** an admitted spinal replacement supplies 120% efficiency and the limb-and-consciousness contribution is 100%, with no later modifiers
- **THEN** body-based Manipulation is 120%, subject to the game's ordinary later limits rather than an added 100% clamp.

### Requirement: Let ordinary work and health consume the resulting capacity
The resulting Manipulation SHALL be the ordinary capacity used by health displays, work eligibility
and stats that already depend on Manipulation.
Do not apply a separate spinal multiplier to each job, output or stat. Each consumer keeps its
existing weight, ceiling, minimum, rounding and permission rules; a capacity reduction is not a
promise that every task slows by the same percentage. Current injury, healing, replacement and
other ordinary health changes refresh the contribution through the normal health lifecycle. Preserve
unconscious/dead-pawn handling, existing jobs' native response to incapacity, and save/load behavior.
Do not manufacture injuries, force surgery, cancel jobs independently or create a parallel saved
capacity. Parts, implants, treatment costs, research, work and outcomes remain otherwise unchanged.

#### Scenario: Manipulation-dependent work
- **WHEN** a pawn with reduced spinal function performs an otherwise permitted manipulation-dependent job
- **THEN** its work uses the resulting native capacity through that job's normal stat rules, with no additional job-specific spinal penalty.

#### Scenario: Healing or replacement
- **WHEN** ordinary healing or a successful native operation changes spinal efficiency
- **THEN** the next ordinary health refresh updates the capacity, its explanation and subsequent work calculations consistently.

#### Scenario: Save and reload
- **WHEN** a pawn with an existing spinal injury is saved and loaded
- **THEN** its current native health state determines Manipulation without accumulating another penalty or requiring a new feature-specific injury.

#### Scenario: No ability from a multiplier
- **WHEN** a pawn is unconscious, dead or anatomically unable to manipulate
- **THEN** a high-efficiency spinal part does not grant wakefulness, life or otherwise unavailable work.

### Requirement: Explain the same result in existing health information
The mod SHALL preserve the ordinary Health Manipulation display and its contributor explanation,
including in the owned compact health view and other supported consumers of native capacity.
When changed spinal efficiency contributes to the result, include the relevant native spinal part
health or replacement/condition contribution without losing limb, consciousness, gene or other
health information. Do not repeat the same contributor merely because the explanation is opened
again. Healthy neutral anatomy does not require a misleading penalty line. Reading the explanation
cannot change health, the calculated value or work. Labels and owned diagnostic text support all
seven baseline languages. No new settings window, bottom tab, debug action or view toggle is added;
individual injuries and replacements remain native pawn state.

#### Scenario: Inspect a damaged spine's effect
- **WHEN** the player opens Manipulation details for a pawn whose spinal injury reduces efficiency
- **THEN** the explanation includes the spinal contributor alongside the existing relevant contributors and agrees with the capacity used for work.

#### Scenario: Repeated and compact inspection
- **WHEN** the player repeatedly opens the native or owned compact health details
- **THEN** the same current effect is shown without accumulating contributors or applying another penalty.

#### Scenario: Localized explanation
- **WHEN** the player reads these details in a baseline language
- **THEN** existing localized body-part/condition names and translated owned messages remain readable, without raw keys or a separate English-only panel.

### Requirement: Avoid duplicate or unsupported spinal contributions
The mod SHALL activate this contribution only when the relevant capacity behavior is supported,
while allowing unrelated compatible modifications.
If the active game or another provider already supplies an equivalent spinal factor, do not add
it again. With original Spines Matter More Now active, withhold owned overlap and explain the choice
without changing that provider. An incompatible or uncertain capacity path leaves its remaining
native/provider behavior intact with a bounded explanation. Unknown or invalid efficiency is not
a valid zero and must not paralyze a pawn through an invented result. Startup warnings identify
each skipped enabled patch, its failed requirement, unavailable behavior and fallback. Unsupported
optional anatomy does not justify rewriting other pawns' health or blocking independently supported
cases. No foreign patch removal, source package impersonation or source save migration is implied.

#### Scenario: Original provider present
- **WHEN** original Spines Matter More Now is active alongside megamod
- **THEN** the owned spinal contribution is inactive with an explanation and no second multiplier is applied by megamod.

#### Scenario: Equivalent existing behavior
- **WHEN** the admitted game or provider already applies the same spinal factor at the appropriate stage
- **THEN** the factor remains single and the owned integration reports that it is already supplied.

#### Scenario: Unknown capacity semantics
- **WHEN** a changed capacity path or invalid efficiency cannot support the contribution safely
- **THEN** the owned contribution leaves the existing result and explanation intact and reports the unavailable behavior without substituting zero.

#### Scenario: Compatible unrelated change
- **WHEN** a compatible mod changes a limb contribution or adds a later health modifier while the admitted spinal integration remains valid
- **THEN** that change remains and the single spinal factor is applied at its specified stage.
