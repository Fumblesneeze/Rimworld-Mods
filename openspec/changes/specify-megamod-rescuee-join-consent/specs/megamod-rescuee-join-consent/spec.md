# Rescuee joining consent

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld1.7 beta**.
Origin replacement S108. This is an observable contract for later implementation.

## ADDED Requirements

### Requirement: Ask at the existing rescue joining opportunities

The mod SHALL request player consent before an eligible humanlike rescuee joins through ordinary
recovery or the native rescue promise reached by Offer help/Free prisoner. It SHALL preserve each
route's eligibility, willingness and rescue work. Recovery willingness remains the native seeded
50% under safe outdoor temperature without toxic fallout or active noxious haze, otherwise100%,
subject to the supported game's rules and admitted compatible changes. The feature SHALL NOT reroll
willingness, create an offer from every rescue, or force willingness merely because god mode is on.

#### Scenario: Willing recovering guest
- **WHEN** an eligible rescued guest recovers and the ordinary joining decision is positive
- **THEN** an Accept/Reject request appears before the pawn becomes a colonist
- **AND** the completed rescue and its ordinary recovery gratitude remain intact

#### Scenario: Rescuee declines independently
- **WHEN** the same native recovery rules decide that a rescued guest will not join
- **THEN** the ordinary nonjoining outcome occurs without a consent request or another chance roll

#### Scenario: Rescue promise through a real interaction
- **WHEN** a player colonist completes an eligible Offer help or Free prisoner interaction with a humanlike rescue target
- **THEN** the player receives one joining decision before recruitment and its joining success announcement
- **AND** the interaction still requires the ordinary reachable target and native eligibility

#### Scenario: Unrelated recruitment and ineligible rescues
- **WHEN** an animal, ordinary prisoner recruitment, ritual joiner, wanderer event, non-player Offer help actor, or other unsupported joining route is processed
- **THEN** this feature does not add a rescue consent dialog or change that route's outcome
- **AND** an ordinary recovered prisoner or a rescuee whose faction forbids recovery joining is not newly offered by the recovery route

#### Scenario: Developer mode does not change willingness
- **WHEN** god mode is enabled and an otherwise eligible rescuee would decline under the ordinary rules
- **THEN** this feature does not turn that decline into a joining offer

### Requirement: Show useful ordinary information before deciding

The request SHALL identify the rescuee and relevant location, show available name, background role,
biological age, gender/kind, relations with colonists, visible work restrictions, traits and passions,
and offer the ordinary information card. It SHALL distinguish major/minor passions, empty results
and unavailable information. It SHALL respect normal information visibility and handle missing
optional data without losing the consent controls.

#### Scenario: Review a prospective colonist
- **WHEN** the request is opened for a pawn with ordinary biography and skill information
- **THEN** its summary identifies that pawn and lists their current visible restrictions, traits, passions and colony relationships
- **AND** major passions are distinguishable from minor passions

#### Scenario: No passions or incomplete biography
- **WHEN** the pawn has skills but no passions, or an optional provider supplies no biography section
- **THEN** the relevant section clearly says None or information unavailable as appropriate
- **AND** Accept, Reject and supported information remain usable

#### Scenario: Inspect without answering
- **WHEN** the player opens and closes the pawn's information card
- **THEN** the same undecided request remains available and no faction change occurs

#### Scenario: Hidden information stays hidden
- **WHEN** a pawn has a trait or condition that ordinary player information has not revealed
- **THEN** this summary does not reveal it through labels, omissions described as a secret, or extra diagnostic text

### Requirement: Require an explicit contextual decision

The decision SHALL use a paused contextual dialog with distinct Accept, Reject and More info actions
and a one-second real-time initial input delay. Escape, Enter, an outside click and viewing details
SHALL NOT count as acceptance or rejection. There SHALL be no timeout/default decision, new bottom
tab or mod-settings row for an individual rescuee.

#### Scenario: Opening click cannot accept
- **WHEN** input continues immediately as the request opens
- **THEN** decision controls wait for the initial one-second delay and do not consume the opening input as consent

#### Scenario: Dismissal keys are not consent
- **WHEN** the player presses Escape or Enter or clicks outside an unresolved request
- **THEN** the request remains undecided and the pawn remains outside the colony

#### Scenario: Simultaneous rescuees
- **WHEN** more than one valid rescue joining opportunity occurs before the player answers
- **THEN** each pawn retains an individually identified request and resolving one does not resolve or overwrite another

### Requirement: Acceptance preserves the correct joining result exactly once

Accept SHALL perform that request's ordinary joining route once and announce success only after
membership actually changes. Recovery joining SHALL retain its ordinary faction transition; rescue
promise acceptance SHALL retain its native recruitment, rescue memories and applicable quest outcome.
Recruitment-only rewards and joining completion announcements SHALL NOT run while merely waiting.

#### Scenario: Accept recovered guest
- **WHEN** the player accepts a still-valid recovery request
- **THEN** that pawn becomes a colonist with ordinary colony controls and guest/faction consequences
- **AND** one correctly titled joining letter describes the actual accepted pawn

#### Scenario: Accept rescue promise
- **WHEN** the player accepts a still-valid Offer help or Free prisoner request
- **THEN** that pawn joins through the ordinary rescue recruitment outcome, including applicable guest cleanup, recruiter credit, memories and quest consequences
- **AND** its rescue-finished announcement appears only after that result succeeds

#### Scenario: Duplicate input or notification
- **WHEN** Accept is invoked repeatedly or a resolved opportunity is reported again
- **THEN** the pawn is not recruited twice and no letter, recruitment credit, memory or quest reward is repeated by this feature

#### Scenario: Competing rescue routes for one pawn
- **WHEN** a second supported rescue joining route offers the same pawn while their first valid request is pending
- **THEN** one request remains, retaining the first request's recovery or offered-help context through save/load
- **AND** Accept or Reject produces only that retained route's specified joining or refusal consequences
- **AND** the later offer does not add recruitment effects or erase independently completed rescue facts

#### Scenario: No premature reward
- **WHEN** a rescue-promise request is waiting for a decision
- **THEN** no joining success letter, recruitment credit or joined/recruited quest outcome is produced by the deferred joining action
- **AND** independently completed physical-rescue facts remain valid

### Requirement: Rejection preserves rescue without inventing consequences

Reject SHALL consume that joining opportunity, leave the pawn unjoined and report the decision
truthfully. It SHALL add no refusal-specific mood or goodwill penalty, recruitment credit or joining
reward. Independent native rescue, quest, faction and ideology consequences SHALL remain authoritative.
The pawn SHALL continue ordinary care, guest/site and departure rules; refusal is not removal,
teleportation, forced abandonment of care or permission to bypass physical exit restrictions.

#### Scenario: Reject a recovered guest
- **WHEN** the player rejects a recovery joining request
- **THEN** the pawn remains a noncolonist and the message says they will not join
- **AND** ordinary care/departure behavior continues without a new Denied joining or Banished penalty from this feature

#### Scenario: Reject a rescue promise
- **WHEN** the player rejects an Offer help or Free prisoner joining request
- **THEN** the promise does not recruit the pawn or announce a completed joining result
- **AND** no acceptance-only recruitment credit or joined quest reward is granted
- **AND** other rescue or quest facts are not erased or falsely declared successful

#### Scenario: Refusal with no immediate departure
- **WHEN** a rejected rescuee is downed, blocked from leaving, still subject to captivity, or on a rescue site
- **THEN** the feature neither deletes nor teleports them and does not falsely report that they have left
- **AND** ordinary care, captivity and exit rules determine subsequent behavior

#### Scenario: Repeat injury and recovery after refusal
- **WHEN** the same rejected recovery opportunity is encountered again after injury/recovery or reload
- **THEN** it does not produce another offer or a silent join
- **AND** the feature does not reset native consumed-opportunity restrictions to manufacture a fresh chance

### Requirement: Preserve decisions across valid lifecycle changes

Pending and resolved request identity SHALL belong to the current game and pawn. A saved unresolved
request SHALL return undecided after load without rerolling or replaying completed effects. A carried
pawn held on the request's map SHALL remain supported. Before applying a choice, the feature SHALL
check that the exact target and required rescue context are still valid; stale requests SHALL explain
why no action was taken and SHALL NOT change a replacement pawn or undo another system's outcome.

#### Scenario: Recovery while being carried
- **WHEN** a valid rescuee recovers inside a carrier on the map
- **THEN** a correctly identified request can be shown using the held location
- **AND** its information and decision do not require the rescuee to stand on a map cell

#### Scenario: Save an unresolved request
- **WHEN** a game with a pending request is saved through a supported save action and loaded
- **THEN** the same valid request returns undecided without new willingness rolls or prior effects repeating

#### Scenario: Change the viewed map or load another game
- **WHEN** the viewed map changes while a request exists, or another save is loaded
- **THEN** viewing another map alone does not retarget or invalidate the original request
- **AND** another saved game never receives the previous game's request

#### Scenario: Target or required context becomes invalid
- **WHEN** the target dies, leaves its eligible held-map context, is captured, changes to an incompatible faction, or loses a required rescue actor before a choice
- **THEN** the stale request cannot recruit them and explains that it is no longer available

#### Scenario: Another system already recruited the pawn
- **WHEN** a pending pawn joins through another legitimate action
- **THEN** the stale request closes without another recruitment or duplicate benefits
- **AND** it does not revert the other action

#### Scenario: Acceptance preflight fails without effects
- **WHEN** acceptance cannot start and no joining effects have run
- **THEN** the player sees that joining did not complete and can safely reject the blocked request
- **AND** there is no automatic acceptance or success announcement

#### Scenario: Partial effects before membership changes
- **WHEN** attempted acceptance changes apparel locks, titles, guest status or other native/provider state but fails before colony membership changes
- **THEN** the request becomes an incomplete resolved attempt with an acknowledgement and accurate unjoined feedback
- **AND** it cannot be retried or presented as an untouched refusal, including after reload
- **AND** this feature does not blindly roll back or repeat effects, grant missing recruitment rewards, or announce success

#### Scenario: Error after membership changed
- **WHEN** membership changed during acceptance but a later joining effect fails
- **THEN** the feature reports incomplete processing without retrying recruitment or undoing foreign effects
- **AND** the old request becomes an incomplete resolved attempt with an acknowledgement rather than Accept/Reject callbacks
- **AND** the resolved membership cannot be granted again through the old request, including after reload

### Requirement: Compose with existing content and remain understandable

The feature SHALL keep native rescue jobs, child-care restrictions, rescue-site rules and ordinary
recruitment intact. Missing optional race/information providers SHALL not disable ordinary rescue
consent. All owned text SHALL support English, German, Spanish, French, ChineseSimplified, Russian
and Japanese with correct identity/pronouns and readable long summaries. It SHALL NOT import source
settings, retroactively reconsider previous rescues or expose internal patch details in the dialog.

#### Scenario: Modded or child rescuee
- **WHEN** an eligible humanlike child or supported modded pawn generates a request
- **THEN** current native visible information is shown without requiring an alien-race framework
- **AND** the request does not alter the pawn's care needs or work eligibility

#### Scenario: Translate and read a long summary
- **WHEN** the request is opened in any required language for a pawn with many traits and relations
- **THEN** summary and decision controls remain readable and accessible, with accurate pawn references and no hardcoded male pronouns

#### Scenario: Existing game without pending records
- **WHEN** an existing save first loads with this feature
- **THEN** existing colonists and prior rescues remain unchanged and only subsequent supported joining opportunities request consent

### Requirement: Admit complete routes or retain an explained fallback

Each enabled patch SHALL have a documented target contract and startup compatibility check. Compatible
unrelated changes SHALL remain allowed. Missing, ambiguous or incompatible required behavior SHALL
leave the affected route inactive with one bounded startup warning per skipped enabled patch naming
the missing behavior and fallback. The original provider SHALL own overlap when active; this feature
SHALL NOT duplicate requests or remove another provider. An already captured owned request SHALL
never be silently accepted merely because its route later becomes unavailable.

#### Scenario: Original provider is active
- **WHEN** RescueeJoinsPlus is active alongside this mod
- **THEN** overlapping owned consent patches remain inactive with startup diagnostics explaining provider ownership
- **AND** this feature does not add a second request or unpatch the original

#### Scenario: Compatible changes coexist
- **WHEN** another patch changes unrelated behavior while all required consent-route semantics remain true
- **THEN** the supported rescue consent route remains available

#### Scenario: Unsupported route at startup or later
- **WHEN** a required target is missing or no longer satisfies its contract
- **THEN** that route's owned patches remain or become inactive without leaving a partial consent workflow
- **AND** diagnostics explain that native/provider fallback can still join automatically for new events
- **AND** existing owned pending requests remain unaccepted, with the unavailable reason visible
