# Restrained social-fight attacks

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
Future observable behavior only; source evidence and implementation choices are in refinement/design.

## ADDED Requirements

### Requirement: Social-fighting pawns refrain from sharp attacks
The mod SHALL exclude attacks whose primary damage is classified as sharp while the attacker is in the game's social-fighting state, unless that attacker has the active Bloodlust trait.

Apply the rule to every qualifying attacker, not only player colonists, and independently of the
opponent's faction, traits or equipment. The rule does not create social fights or grant access to
that state. It takes effect whenever the feature is admitted; there is no new configuration switch.

#### Scenario: A fighter has claws and a permitted blunt attack
- **WHEN** a pawn without active Bloodlust participates in a social fight and its otherwise usable attacks include sharp claws and a blunt strike
- **THEN** sharp claws are excluded from its attacks and ordinary selection can use the permitted blunt strike

#### Scenario: A non-colonist social fighter attacks
- **WHEN** a non-colonist pawn without active Bloodlust is in an otherwise valid social fight
- **THEN** the same primary-sharp restriction applies without granting or removing any other participant eligibility

### Requirement: Bloodlust is an exception for the individual attacker
The mod SHALL leave attack permission unchanged for an attacker whose Bloodlust trait is currently active, while retaining all ordinary restrictions and attack selection rules.

An opponent's Bloodlust does not grant the attacker an exception. A suppressed or absent Bloodlust
trait is not active; a pawn without a trait record receives no exception. The exception permits
ordinary eligible sharp attacks, but never guarantees their selection, a hit or an injury.

#### Scenario: One fighter has Bloodlust
- **WHEN** only one of two social fighters has active Bloodlust
- **THEN** that fighter keeps its ordinary attack choices while the other excludes primary-sharp attacks

#### Scenario: Bloodlust is suppressed
- **WHEN** a social fighter's Bloodlust trait is present but suppressed
- **THEN** that fighter excludes primary-sharp attacks just as a fighter without Bloodlust does

#### Scenario: Bloodlust does not override another restriction
- **WHEN** a Bloodlust fighter has an attack that the game or a compatible mod already disallows
- **THEN** this feature does not make that attack available

### Requirement: Classification concerns primary damage rather than injury appearance
The mod SHALL classify attacks by their current primary damage's sharp classification, without relying on attack names, weapon names, race names or the appearance of the resulting wound.

A valid attack with non-sharp primary damage, or with no primary damage classification, retains its
otherwise ordinary permission. This is not a requirement that every permitted attack deal only
blunt damage. Secondary attack effects, environmental injuries, existing bleeding and independent
damage remain unchanged. No attack is converted from sharp damage to blunt damage.

#### Scenario: Modded natural attacks use ordinary damage classifications
- **WHEN** a supported added natural attack has a different name but its primary damage is classified as sharp
- **THEN** it is excluded for a social fighter without active Bloodlust under the same rule as a native sharp attack

#### Scenario: A permitted attack has an additional effect
- **WHEN** an otherwise permitted non-sharp attack produces a secondary effect through the game's supported combat behavior
- **THEN** this feature leaves that effect intact instead of intercepting all damage to the fighter

#### Scenario: An attack has no primary damage classification
- **WHEN** a supported attack is otherwise usable but has no primary damage classification
- **THEN** this feature does not invent a sharp classification or make the attack unusable solely for that absence

### Requirement: Filtering preserves the remaining native choices
The mod SHALL only remove disallowed sharp attacks from the existing choices and SHALL preserve ordinary restrictions and the relative selection weights of the remaining attacks.

Do not add equipped weapons, attacks from other sources or a replacement punch to the social-fight
choices merely because a sharp attack was removed. If no permitted attack remains, the pawn makes
no attack until an ordinary permitted choice becomes available or its state ends. Preserve normal
no-job/recovery behavior without an immediate retry loop, repeated error flood or forced early
recovery. Do not preemptively prevent a fight from starting solely by applying this in-state rule
to a pawn that has not entered social fighting.

#### Scenario: Several permitted attacks remain
- **WHEN** removing sharp attacks leaves several otherwise usable social-fight attacks
- **THEN** ordinary weighted selection chooses among them without changing their damage, attack timing or relative weights

#### Scenario: Only sharp attacks remain
- **WHEN** a social fighter without active Bloodlust has no otherwise usable non-sharp or unclassified attack
- **THEN** it makes no attack, gains no fabricated replacement, and remains subject to ordinary fight recovery without an immediate retry loop or repeated errors

#### Scenario: A sharp-only pawn can enter an otherwise valid fight
- **WHEN** the native initiation rules permit a fight before a sharp-only pawn enters social fighting
- **THEN** this feature does not veto initiation in anticipation of its later attack restriction

### Requirement: Other combat and mental states keep their ordinary behavior
The mod SHALL leave attack permission unchanged when the attacker is not in the game's social-fighting state, and for attacks not made by a pawn.

Being a target of a social fight does not itself restrict an attacker's choices. Berserk and other
mental states are not treated as social fighting. Ordinary drafted combat, hunting, item destruction
and other separately owned actions retain their own rules. This feature does not globally remove
claws, bites, weapons or damage types from a pawn.

#### Scenario: The same pawn fights outside a social fight
- **WHEN** a pawn with a sharp natural attack uses an otherwise permitted ordinary combat action outside social fighting
- **THEN** its sharp attack remains available under the ordinary rules

#### Scenario: A pawn is berserk
- **WHEN** a pawn is in a different mental state such as berserk
- **THEN** this feature does not apply the social-fight sharp restriction merely because it is a mental state

### Requirement: Permission follows the current state and trait
The mod SHALL reconsider the attacker's current social-fighting state and active Bloodlust exception when an attack is considered and before it is executed, without retaining a previous fight's permission.

Ending social fighting removes this feature's restriction; gaining, losing or suppressing Bloodlust
changes the next attack decision accordingly. Already inflicted damage is never undone. Loading a
save with an ongoing fight uses the loaded current pawn state, without needing an extra player
toggle or retaining another save's decision.

#### Scenario: Bloodlust becomes inactive before a sharp strike
- **WHEN** a pending sharp strike's attacker remains social fighting but loses the active Bloodlust exception before execution
- **THEN** that strike is not executed as a permitted sharp attack, and subsequent choices follow the current restrictions

#### Scenario: Resume a saved social fight
- **WHEN** a save is loaded with an ongoing social fight
- **THEN** each fighter's next attack follows its current state and active trait, with no stale exception from a previously loaded game

### Requirement: Social fights retain their ordinary consequences
The mod SHALL preserve native fight initiation, participant rules, recovery, hit and dodge chances, armor handling, permitted-attack damage, injuries, experience, memories, notifications and combat feedback except for the excluded attack choices themselves.

The restriction is not nonlethal combat: permitted blunt attacks can still cause serious injury or
death, and Bloodlust fighters may still use eligible sharp attacks. Existing injuries are neither
healed nor prevented from progressing. No new damage multiplier, injury cap, recovery timer or
fight-chance adjustment is introduced. The ordinary consequences of choosing different attacks
remain, including different resulting injuries or time until a fighter is downed.

#### Scenario: A permitted blunt strike lands
- **WHEN** a permitted blunt strike hits during a social fight
- **THEN** native damage, armor and injury behavior applies without an added safety cap or healing

#### Scenario: A fight ends normally
- **WHEN** ordinary recovery or an ordinary stopping condition ends the fight
- **THEN** its normal job cleanup, memories and notification behavior still occurs without an extra owned recovery reward or penalty

### Requirement: No additional player interface or saved choice is introduced
The mod SHALL present the behavior through existing social-fight, combat and Health feedback without adding a bottom tab, gizmo, overlay, debug action or settings row for this feature.

There is no pawn-specific restraint toggle or configuration parameter to persist. Startup
compatibility warnings remain part of the common diagnostic behavior. Removing the feature does
not require migrating a new owned pawn record; already inflicted native injuries remain native.

#### Scenario: Play with the admitted feature
- **WHEN** the player observes a social fight and inspects its participants
- **THEN** the ordinary fight/combat/Health surfaces report its consequences and no new feature window or per-pawn configuration is required

### Requirement: Unsupported integrations preserve ordinary behavior with a warning
The mod SHALL leave affected restraint behavior inactive when the current game or combat provider cannot support it safely, while preserving ordinary usable combat and explaining the unavailable restriction at startup.

A bounded startup warning identifies the unavailable behavior, reason and fallback, without
repeating on every attack. The presence of another mod alone does not disable restraint when their
behaviors are compatible. Unsupported combat providers keep their own
behavior; do not claim that unverified custom attacks are restrained. Do not alter another mod's
behavior to silently override its attack restrictions. If the original Blunt Social Fights package is active,
leave the owned duplicate rule inactive with a startup warning and leave the original untouched.
An installed but inactive copy does not suppress the owned feature.

#### Scenario: Another mod imposes a compatible attack restriction
- **WHEN** another mod disallows an attack while the required social-fight semantics remain valid
- **THEN** sharp restraint still applies and never restores the disallowed attack merely because it is non-sharp or the attacker has Bloodlust

#### Scenario: The current game cannot support restraint
- **WHEN** the current game or combat provider cannot safely support the required attack restriction
- **THEN** the affected restraint remains inactive with a clear startup warning and ordinary or provider combat remains usable

#### Scenario: The original provider is active
- **WHEN** the original Blunt Social Fights package is active
- **THEN** the owned duplicate rule is inactive with a startup warning, without changing the original mod's behavior
