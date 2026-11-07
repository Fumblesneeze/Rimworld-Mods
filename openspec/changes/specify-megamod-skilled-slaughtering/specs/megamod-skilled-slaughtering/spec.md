# Skilled animal slaughter

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target **1.7 beta**.
S117 draft; no runtime implementation or acceptance. S118 separately specifies Cooking and yield.

## ADDED Requirements

### Requirement: SSK01 Preserve the native slaughter workflow and scope
The feature SHALL change the handling duration of native animal-slaughter jobs while preserving
their ordinary target selection, permission, reservation, approach, progress and completion rules.
This includes manual/automatic slaughter and the slaughterer mental-state job, without granting
new targets. It SHALL leave hunting, finishing attacks, prisoner/entity execution, milking,
shearing and corpse processing unchanged by this timing feature.

#### Scenario: Designated or automatically selected animal
- **WHEN** an eligible handler takes a native slaughter job for an eligible selected animal
- **THEN** the handler approaches and handles that animal through the ordinary job workflow
- **AND** the new duration changes neither which animals are selected nor their slaughter priority.

#### Scenario: Native restriction blocks the job
- **WHEN** native capacity, violence, ideology, faction, aggression, access or reservation rules
  prevent a slaughter job
- **THEN** a positive slaughter-speed value does not bypass the restriction or create an alternative kill.

#### Scenario: Slaughterer mental state
- **WHEN** the native slaughterer episode selects an animal and begins its ordinary slaughter job
- **THEN** its handling uses the same speed/size rule
- **AND** target selection, episode restrictions and ordinary completion consequences remain native.

#### Scenario: Other animal work or killing
- **WHEN** a pawn hunts, finishes downed prey, executes a prisoner/entity, milks, shears or butchers
- **THEN** this timing feature does not change that action's duration or outcome.

### Requirement: SSK02 Retain the source-derived slaughter-speed contribution
The feature SHALL expose a slaughter-speed contribution with base100%, multiplied by global work
speed, Animals factor `0.64 + 0.09 × level`, Melee factor `0.95 + 0.0125 × level`, Manipulation,
and Sight factor `0.5 + 0.5 × min(Sight, 1.2)`, with a final minimum of10% under the ordinary stat
rules. Capacity100% means1. Ordinary other modifiers SHALL remain effective. Missing skills on an
otherwise eligible skill-less worker SHALL use the native neutral skill contribution. S118's
additional Cooking contribution SHALL compose directly, without applying these factors twice.

#### Scenario: Ordinary healthy worker
- **WHEN** an otherwise unmodified worker has Animals10, Melee10, global work speed100%,
  Manipulation100% and Sight100%, with any separate Cooking contribution neutral
- **THEN** the source-derived slaughter speed is165.55%, displayed with ordinary percentage formatting.

#### Scenario: Skill progression
- **WHEN** the same ordinary inputs use Animals/Melee0 or Animals/Melee20
- **THEN** the source-derived speed is60.8% or292.8% respectively
- **AND** this changes handling speed without granting extra skill experience for elapsed time.

#### Scenario: Capacity and speed floor
- **WHEN** an eligible worker's Manipulation falls to50% with other inputs unchanged
- **THEN** the pre-minimum speed is halved
- **AND** Sight150% contributes the same110% sight factor as Sight120%; the finalized speed cannot
  fall below10%, without making an incapacitated worker eligible.

#### Scenario: Other valid modifiers and skill-less workers
- **WHEN** an admitted ordinary stat modifier changes slaughter speed or an otherwise eligible worker
  has no skills
- **THEN** the modifier remains effective and missing skills contribute neutrally rather than being
  fabricated as level-zero skills or causing a failed job.

### Requirement: SSK03 Use a finite size-adjusted duration from fresh attempt facts
The feature SHALL calculate handling work when handling starts, using current speed and the target's
current body size. With speed `S` expressed as a multiplier and body size `B`, first round `180/S`
down to whole game ticks, then multiply by `(1+B)/2` and round down again, with a minimum of one
game tick. There are60 game ticks per normal-speed second. That attempt SHALL keep its captured
duration; later skill/health/size changes affect the next attempt. Invalid or unrepresentable
required inputs SHALL retain ordinary native handling rather than create an instant, negative or
endless owned action.

#### Scenario: Native-size baseline and rounding
- **WHEN** handling starts at100% speed with body size1
- **THEN** its duration is180 ticks
- **AND** at165.55% speed and body size2.4, its duration is183 ticks under the two-step rounding rule.

#### Scenario: Size comparison
- **WHEN** otherwise identical healthy Animals10/Melee10 workers handle targets of current body
  size0.2,2.4 and4 with no other speed contribution
- **THEN** handling takes64,183 and270 ticks respectively
- **AND** current life-stage/body-size effects matter, rather than assuming every animal is an adult.

#### Scenario: Fresh attempt snapshot
- **WHEN** a worker's relevant skill/capacity changes before handling starts
- **THEN** that attempt uses the changed facts without a previous animal's cached speed
- **AND** a change after handling starts does not reset or shorten that attempt's remaining work.

#### Scenario: Exceptional or unusable inputs
- **WHEN** a valid very high speed would otherwise round the duration to zero
- **THEN** handling still takes at least one game tick
- **AND** missing, non-finite, nonpositive or unrepresentably large required inputs do not become
  an owned invalid wait; the ordinary native action remains available under its own rules.

### Requirement: SSK04 Coordinate the animal wait with handling and release it safely
The feature SHALL keep the handled animal in the ordinary temporary waiting interaction for the
same finite handling duration, subject to normal emergencies and interruptions. It SHALL release
only the wait belonging to that attempt when the attempt ends or is abandoned, and SHALL NOT
cancel a later animal job or force an escaped/unavailable animal to remain a valid victim.

#### Scenario: Slow handler completes without timer mismatch
- **WHEN** eligible handling takes longer than180 ticks and no ordinary interruption occurs
- **THEN** the animal does not resume wandering merely because the old180-tick wait expires
- **AND** the handler can finish the calculated work without a timer-induced chase/retry cycle.

#### Scenario: Cancel or lose the target
- **WHEN** the handler is drafted, redirected, incapacitated, removed, or otherwise loses a valid
  target before completion
- **THEN** the attempt does not execute its final kill and its still-owned animal wait is released
- **AND** a later fresh attempt starts its own work without inherited partial credit.

#### Scenario: Animal interruption takes precedence
- **WHEN** aggression, damage, fire, despawning or another native interruption replaces the animal's wait
- **THEN** ordinary game rules determine the response and whether the handler can continue
- **AND** cleanup never cancels that replacement animal job to restore the old wait.

#### Scenario: Handoff to completion
- **WHEN** uninterrupted handling finishes
- **THEN** the native final slaughter action follows once without an intervening owned wandering step
- **AND** no abandoned temporary wait remains after the attempt ends.

### Requirement: SSK05 Preserve progress and ordinary death consequences
The feature SHALL preserve an owned attempt's captured total and remaining handling work across
save/reload, without replaying completion. Native death, blood, corpse, records, mood/history and
mental-state consequences SHALL occur through the ordinary action once. This timing feature SHALL
not change base yields, grant additional products or apply slaughter bonuses to other killing.

#### Scenario: Reload unfinished handling
- **WHEN** a valid owned attempt is saved partway through handling and reloaded
- **THEN** it resumes with the same remaining work and corresponding animal wait
- **AND** progress neither resets to full nor finishes early because current skills have changed.

#### Scenario: Unprovable saved association
- **WHEN** the saved owned attempt can no longer establish its worker, target, remaining work or wait ownership
- **THEN** that owned attempt is abandoned without executing the cut
- **AND** only a proved owned wait is released, with ordinary fresh work remaining possible.

#### Scenario: Successful slaughter
- **WHEN** the native final action kills the intended animal
- **THEN** its ordinary corpse and applicable native consequences occur once
- **AND** this timing feature adds no separate meat, leather, trophy, Cooking experience or yield multiplier.

#### Scenario: Wounded animal and other yield modifiers
- **WHEN** an animal has native injury, missing-part, difficulty or other admitted yield effects
- **THEN** changing handling duration does not remove or bypass those effects
- **AND** S118's separately specified yield policy cannot be inferred merely from an execution cut.

### Requirement: SSK06 Present contextual speed without new configuration clutter
The feature SHALL explain its effective slaughter-speed factors in the existing pawn statistics
surface and retain ordinary handling progress. It SHALL introduce no bottom tab, debug override,
operational toggle or setting. Owned text SHALL support all seven baseline languages, and an
inactive owned timing feature SHALL not present its unused speed as governing current work.

#### Scenario: Inspect speed and progress
- **WHEN** the player inspects an eligible pawn's slaughter-speed stat and observes it handling an animal
- **THEN** the native stat view explains the contributing factors and the ordinary progress display
  reflects that attempt's actual captured handling work.

#### Scenario: Translation and inactive behavior
- **WHEN** the player uses English, German, Spanish, French, Simplified Chinese, Russian or Japanese
- **THEN** owned labels/descriptions are localized in the existing surface
- **AND** failed admission does not leave a misleading owned speed entry claiming to control native work.

### Requirement: SSK07 Preserve compatible definitions and select one provider
The feature SHALL preserve ordinary external slaughter-speed contributions where admitted and
add its definition without overwriting unrelated content. With the original Skilled Slaughtering
provider active, it SHALL suppress its overlapping timing and avoid a duplicate stat definition.
Unknown conflicting definitions or provider semantics SHALL leave affected owned behavior inactive,
without altering another mod's settings, definitions or patches.

#### Scenario: Original provider present
- **WHEN** the original Skilled Slaughtering provider is active
- **THEN** its implementation and stat remain authoritative and no second owned duration adjustment
  or duplicate stat is added
- **AND** the owned wait correction is not falsely claimed for the source-selected behavior.

#### Scenario: Compatible contributor
- **WHEN** another admitted mod adds a valid modifier to the shared slaughter-speed stat without
  replacing required handling behavior
- **THEN** the contribution remains effective and the owned calculation is not applied twice.

#### Scenario: Missing prerequisites or conflicting definition
- **WHEN** required stat/skill/capacity content is absent or a same-name definition cannot safely
  satisfy the required contract
- **THEN** affected owned behavior stays inactive with a bounded explanation
- **AND** unrelated definitions and their existing lists remain unchanged.

### Requirement: SSK08 Guard all changes and isolate failures
Every enabled patch SHALL have a documented minimal target contract checked before application.
The coordinated duration, wait, cleanup and restore behavior SHALL be admitted atomically. Unrelated
changes and compatible foreign patches SHALL be allowed; failed/unknown conditions, checker
exceptions or installation failure SHALL leave the affected owned group inactive with one bounded
startup warning per skipped enabled patch and the native/provider fallback intact.

#### Scenario: Compatible effective behavior
- **WHEN** unrelated instructions or compatible additional job stages preserve the required victim,
  handling and completion relationships
- **THEN** the feature remains eligible without an exact version/hash or exclusive patch ownership
- **AND** those additional stages remain intact.

#### Scenario: Missing or ambiguous timing relationship
- **WHEN** the intended handling stage or its duration/wait relationship is missing or ambiguous,
  or its result cannot be safely connected to the native completion
- **THEN** no partial owned timing/wait adjustment is applied
- **AND** each skipped enabled patch receives one bounded startup warning identifying package,
  patch/spec, target, reason, unavailable behavior and fallback.

#### Scenario: Partial installation or later invalidation
- **WHEN** a group installation fails or a supported later change invalidates the owned contract
- **THEN** only newly installed owned patches are rolled back, unfinished affected owned attempts
  are safely abandoned, and matching owned animal waits are released
- **AND** native/provider behavior handles future jobs; foreign patches remain untouched and owned
  behavior resumes only after successful revalidation.

#### Scenario: Repeated initialization
- **WHEN** startup or supported revalidation is repeated
- **THEN** definitions, handling stages, stat factors, animal waits and warnings do not accumulate
- **AND** no work from another map, pawn, game or save is reused.
