# Counselling mental breaks

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S100/P2-29 replacement; observable draft only. Values below are source-derived unless identified
as an owned correction. None is implemented or accepted yet.

## ADDED Requirements

### Requirement: Counselling is reachable through ordinary Warden work and contextual orders

The system SHALL let capable player-controlled humanlike wardens attempt to calm another eligible
pawn through automatic Warden work and a contextual Calm order. Automatic selection SHALL include
eligible colonists even when no prisoners or slaves exist. Work assignment, work priority, access,
reservation and ordinary emergency interruptions SHALL remain effective; one recipient SHALL have
at most one counsellor at a time. Counselling SHALL require Talking, Hearing, Moving and Manipulation
and an awake, non-downed helper capable of social work, without their own mental break. A direct
order SHALL NOT override these capabilities, target exclusions or counselling cooldown.

#### Scenario: Colonist-only colony
- **WHEN** a colonist enters an eligible break and an available capable colonist is assigned Warden work
- **THEN** the warden can select counselling even though the colony has no prisoners or slaves
- **AND** the job uses the player's ordinary work ordering rather than overriding higher-priority work

#### Scenario: Contextual order and competing helpers
- **WHEN** the player orders a capable warden to calm a reachable eligible pawn
- **THEN** the warden approaches and begins counselling
- **AND** another helper cannot simultaneously counsel the same recipient

#### Scenario: Work or capacity is unavailable
- **WHEN** the helper cannot perform social work, lacks a required capacity or cannot reach or reserve the recipient
- **THEN** automatic counselling does not start and a direct order is unavailable with a relevant reason
- **AND** unavailable work is not represented as a failed counselling roll

### Requirement: Target eligibility follows the person and the actual mental state

The system SHALL consider awake, non-downed, spawned humanlikes on the same map with an eligible
active mental state. Player colonists and slaves are included; colony prisoners and non-hostile
visitors have separate settings, both on by default. Prisoner permission SHALL govern a colony
prisoner even when their faction is hostile. Uncaptured hostile-faction pawns, factionless outsiders,
animals, babies and toddlers SHALL be excluded. Excluding babies/toddlers is an owned care boundary,
not a claim that the source already does so. Aggressive mental states SHALL be excluded by default
and require the aggressive-counselling option; temporary hostility from such a break SHALL NOT by
itself turn a colonist into an excluded hostile-faction outsider.

Running wild, panic flight, hunting-trip states, social fights and infant crying/giggling SHALL NOT
be treated as counselling targets. With the supported optional Vanilla Races Expanded - Android
provider, reformatting and solar-flare states SHALL also be excluded; counselling SHALL NOT bypass
their causes or durations. Other supported breaks, including giving up/leaving and prison
break states, SHALL be handled without changing faction or prisoner status. An unsupported state
SHALL stay unaffected and have an unavailable explanation. Ending, replacing or losing the target
state during approach/conversation SHALL cancel that attempt without curing a different state,
granting success rewards or charging a completed-attempt cooldown.

#### Scenario: Prisoner and visitor permissions
- **WHEN** prisoner counselling is off but visitor counselling is on
- **THEN** a colony prisoner is ineligible while a qualifying non-hostile visitor remains eligible
- **AND** turning prisoner counselling on permits that prisoner without releasing or recruiting them

#### Scenario: Aggression is explicitly enabled
- **WHEN** a colonist is in an aggressive break
- **THEN** ordinary counselling is unavailable by default
- **AND** enabling aggressive counselling permits an otherwise eligible attempt with the aggression risk

#### Scenario: Care and combat exclusions
- **WHEN** a baby cries, a toddler requests care, pawns fight socially, an animal goes manhunter or an uncaptured raider has a break
- **THEN** counselling does not replace their care, fight, animal or combat workflow

#### Scenario: Target state changes before completion
- **WHEN** the recipient naturally recovers, enters a different break, sleeps, becomes downed or leaves the map before the attempt finishes
- **THEN** the attempt ends safely without a success/failure roll, reward or new counselling cooldown
- **AND** the new state and ordinary care or recovery remain intact

#### Scenario: Android reformatting and solar flare
- **WHEN** an android is reformatting or affected by the supported provider's solar-flare mental state
- **THEN** counselling is unavailable and does not terminate that state
- **AND** absence of the Android provider does not disable ordinary counselling

### Requirement: A completed conversation has an explicit relationship-sensitive chance

The system SHALL require a conversation of **1,250 game ticks (0.5 game hours)** by default, showing
progress. A completed valid interaction SHALL grant **30 base Social experience**, subject to normal
learning modifiers, whether its calming roll succeeds or fails. An invalid or interrupted interaction
SHALL NOT earn completion experience or roll a result.

For an otherwise eligible supported break, its restriction on ordinary social interactions SHALL
allow the dedicated calming conversation. This exception SHALL NOT permit other blocked interactions
or override burning, unavailable social capacity, unsupported provider restrictions, invalid position
or a changed target state. In particular, berserk, fire-starting and animal-slaughter breaks SHALL
not become permanently impossible to counsel merely because they normally reject conversation.

The success percentage SHALL be derived as follows. Let N be the helper's current Negotiation
ability expressed in percentage points, capped at **165** by default, and O the recipient's opinion
of the helper. Start at **-25 points**, add **0.5 × N**, add **0.3 × O**, and subtract **0.25 points for
each negative opinion point**. Divide that score by **70** to obtain the success probability, bounded
to 0–100%. The division by 70 preserves the source's actual roll range; the score is not itself the
displayed percentage. The configurable negotiation/opinion weights and cap SHALL use the same
calculation. Chance information, if displayed, SHALL show that effective probability.

#### Scenario: Neutral relationship
- **WHEN** a conversation finishes with Negotiation ability 100%, recipient opinion 0 and default settings
- **THEN** its effective success chance is 25/70, approximately 35.71%
- **AND** one completed conversation earns the base interaction practice once

#### Scenario: Strong and hostile relationships
- **WHEN** default counselling uses Negotiation ability 165% and opinion +100
- **THEN** the success chance is 100%
- **AND** at Negotiation ability 100% and opinion -100 the chance is 0%

#### Scenario: Direction of opinion and capped ability
- **WHEN** the helper likes the recipient but the recipient dislikes the helper
- **THEN** the recipient's opinion governs the relationship contribution
- **AND** raising Negotiation ability beyond the configured cap provides no extra contribution

#### Scenario: Interruption during conversation
- **WHEN** an ordinary emergency or invalid interaction interrupts counselling before completion
- **THEN** no result or success reward is produced and the helper releases their claim
- **AND** a later attempt requires a fresh complete conversation

#### Scenario: Break normally rejects social conversation
- **WHEN** an otherwise eligible berserk recipient passes the enabled aggressive distraction check and completes a successful calming conversation
- **THEN** the dedicated conversation can occur and lead to time alone and recovery despite that break's ordinary social restrictions
- **AND** unrelated blocked conversations remain blocked; an otherwise eligible fire-starting or animal-slaughter break likewise permits only this dedicated intervention

### Requirement: Aggressive counselling includes a separate distraction risk

Before the conversation with an aggressive target, the system SHALL perform a distraction check.
Its success probability SHALL be the helper's current Social impact multiplied by **0.55**, divided
by **0.85**, and bounded to 0–100%; Social impact 100% is represented by 1 in this calculation.
A successful distraction SHALL stun the target for the configured conversation duration using
ordinary stun behavior, without causing damage. This counselling-owned stun SHALL last only while
that attempt remains active: cancellation, invalidation or completion SHALL end its remaining effect
by the next game update, preserving independent pre-existing or concurrently applied stuns. Save/load
SHALL retain ownership and remaining duration without converting it into an orphaned full-length
stun; an invalid loaded attempt SHALL release only its own effect. A failed distraction SHALL end the attempt before
conversation, start the recipient cooldown and permit an ordinary attack against the helper.
A failed final calming roll against an aggressive recipient SHALL likewise permit that attack.
Attack feedback SHALL distinguish actual retaliation from an attack that ordinary rules prevent.

#### Scenario: Aggressive target at ordinary Social impact
- **WHEN** the helper has Social impact 100% and the default distraction weight
- **THEN** the distraction succeeds with probability 0.55/0.85, approximately 64.71%
- **AND** passing it still requires the separate conversation and calming roll

#### Scenario: Failed distraction
- **WHEN** distraction fails
- **THEN** the conversation does not complete, the target can retaliate through normal combat and the cooldown begins
- **AND** the helper gains neither conversation-completion experience nor a success reward

#### Scenario: Zero weight and ordinary stun rules
- **WHEN** the distraction weight is zero or the target cannot be safely held for the conversation
- **THEN** the system does not claim a guaranteed stun or a completed calming interaction
- **AND** ordinary immunities and combat consequences remain in effect

#### Scenario: Cancel a long aggressive conversation
- **WHEN** distraction succeeds with a six-hour conversation configured and the player immediately drafts or redirects the helper
- **THEN** the counselling-owned stun ends by the next game update without a success reward
- **AND** any independent stun remains effective for its own remaining duration; cancelling does not buy a free six-hour disable

#### Scenario: Load during a distraction
- **WHEN** the game is saved during aggressive counselling and reloaded
- **THEN** a still-valid attempt retains only its remaining conversation and owned stun time
- **AND** an invalidated attempt releases its effect without removing another source's stun or rerolling the distraction

### Requirement: Failure creates a recipient cooldown and can prompt a limited retreat

A resolved failure SHALL leave the mental break active and impose a **15,000-tick (6-hour)**
recipient counselling cooldown by default. It SHALL apply to all helpers and manual orders. The
cooldown SHALL be separate from unrelated social/warden interactions and survive save/load;
changing settings SHALL affect later completed attempts, not rewrite an already-started cooldown.

After a non-aggressive failed roll, a recipient with a usable owned room SHALL have a **66%** chance
to move to an ordinarily accessible safe cell in that room and spend **1,750–3,499 ticks** there.
This temporary retreat SHALL NOT cure the break. If no safe reachable room/cell exists, the break
continues without a forced trip, stall or repeated retry. A changed/departed/downed target SHALL
not be forced back into retreat. This source-derived retreat is not permission to ignore prison,
room-access or danger restrictions.

#### Scenario: Failure is shared across helpers
- **WHEN** a completed attempt fails and the player immediately selects another helper
- **THEN** the target remains unavailable for counselling until the same six-hour default cooldown expires
- **AND** unrelated chatting or prisoner work neither starts nor extends this cooldown

#### Scenario: A failed recipient retreats
- **WHEN** the non-aggressive failure retreat is selected and the recipient has an accessible safe owned room
- **THEN** they move there and briefly try to relax for the selected duration
- **AND** the mental break remains active and no success reward or catharsis is created by the retreat

#### Scenario: Unsafe or disappearing retreat
- **WHEN** the owned room is unreachable, loses its safe destination or the recipient's state changes
- **THEN** the retreat ends or is omitted without teleportation, endless retries or clearing a newer state

#### Scenario: Cooldown survives loading
- **WHEN** a save with an active counselling cooldown is loaded or the configured duration is changed
- **THEN** the already-established expiry remains effective without resetting from unrelated interaction times
- **AND** the newly configured duration governs subsequent completed attempts

### Requirement: Success leads to safe time alone and one complete recovery

A successful conversation SHALL stop the harmful activity of the addressed break and visibly begin
a **spending time alone** phase. The recipient SHALL prefer their accessible safe owned bed location,
otherwise a nearby safe accessible cell, and spend **500 ticks (0.2 hours)** there before finishing
recovery. Travel time is additional. If the preferred route is unavailable or later fails, the phase
SHALL fall back once to a safe local destination, including the current cell when suitable; if no
such destination exists, it SHALL complete recovery in place without waiting for an impossible trip.
This last fallback is an owned safety correction. The recipient SHALL NOT continue the old harmful
break while the success message is shown.

The helper SHALL gain one **Helped someone** memory, **+3 mood for 0.5 days**, at most one copy,
nullified by Psychopath, and **50–124 additional base Social experience**, subject to native learning
modifiers. Rewards occur once when a successful conversation is accepted. Recovery SHALL preserve
ordinary state-end effects and permit the normal catharsis only when that addressed break warrants
it; a transitional recovery phase SHALL NOT produce a second catharsis. The option to disable
catharsis SHALL suppress only catharsis caused by this intervention, preserving prior memories.

#### Scenario: Successful colonist counselling
- **WHEN** a valid conversation succeeds and an owned bed location is safe and reachable
- **THEN** the recipient stops the old break activity, travels there, spends 500 ticks alone and returns to ordinary behavior
- **AND** the helper receives the success memory and extra practice once

#### Scenario: Visitor or prisoner without a usable bed
- **WHEN** a successfully calmed recipient lacks a usable owned bed
- **THEN** recovery uses a safe accessible local destination under their ordinary access restrictions
- **AND** the recipient is neither recruited nor released and ordinary sad-wander rules elsewhere remain unchanged

#### Scenario: Recovery cannot travel
- **WHEN** the recovery route fails and no safe local destination is available
- **THEN** the recipient finishes recovery in place without resuming the addressed harmful behavior or endlessly seeking a bed

#### Scenario: No collateral catharsis removal
- **WHEN** an intervention finishes with catharsis suppression enabled and the recipient already has catharsis from an earlier event
- **THEN** only this intervention's new catharsis is suppressed
- **AND** the earlier memory and unrelated state-end effects remain intact

#### Scenario: Save or new state during time alone
- **WHEN** time alone is saved/reloaded, or the recipient dies, leaves, becomes downed or enters a different state
- **THEN** loading preserves remaining owned progress without another roll or reward
- **AND** invalidated recovery relinquishes control without clearing a newer state, deleting unrelated orders or granting duplicate catharsis

### Requirement: Settings and feedback use the common surfaces

The system SHALL place ordinary options under **Social → Counselling** in mod settings with common
Global default, Current save and Use global controls. New/missing entries SHALL inherit; main-menu
save controls SHALL be inactive; checking Use global SHALL retain the dormant override. Presentation
choices apply immediately; each admitted attempt SHALL retain its effective gameplay options through
completion, with later attempts using updated values. Automatic eligibility is re-evaluated when
selecting the next attempt. Recipient cooldown/progress is pawn state, not a mod setting.

Defaults SHALL be: speech text on; prisoners on; non-hostile visitors on; aggressive counselling off;
catharsis suppression off; negotiation weight 50% (0–100%); opinion weight 30% (0–100%); negotiation
cap 165 percentage points (40–190); distraction weight 55% (0–100%); conversation 0.5 hours (0.2–6);
failed-attempt cooldown 6 hours (1–24). The base score -25, negative-opinion penalty and reward/
recovery values above are fixed baseline rules for this draft. Controls SHALL reject invalid values.

Speech-text off SHALL hide floating calming phrases but retain outcome notifications. Feedback SHALL
distinguish unsuccessful counselling, retaliatory aggression, time alone and completed recovery.
Diagnostics and a clearly labelled guaranteed-outcome override SHALL exist only in the top debug
surface, default off, reset for a new game session and never inherited or saved as ordinary settings.
The override SHALL bypass both random checks for an otherwise valid attempt, not eligibility, time,
access or lifecycle rules. No new bottom tab or permanent bottom-right toggle SHALL be added.
All labels, explanations, notifications and speech SHALL support the seven baseline languages.

#### Scenario: Settings inheritance and timing
- **WHEN** the player edits the global duration, overrides it in one save and switches saves
- **THEN** inheriting saves resolve the current global duration while the override remains isolated
- **AND** an ongoing attempt keeps its admitted duration while the next attempt uses the new effective value

#### Scenario: Main menu and invalid controls
- **WHEN** counselling settings are opened at the main menu or invalid numeric input is submitted
- **THEN** only the global controls are editable and invalid values cannot become effective

#### Scenario: Speech and debugging are separate
- **WHEN** speech text is disabled and the top debug guaranteed-outcome override is enabled
- **THEN** an otherwise valid attempt still takes its normal time, succeeds without random failure and shows outcome feedback without floating speech
- **AND** starting a new game session restores ordinary random outcomes

### Requirement: External providers compose without duplicate counselling or rewards

The system SHALL select one counselling provider. With original Snap Out! active, owned counselling
and overlapping definitions SHALL be inactive with a bounded explanation, without changing or
unpatching the original. A supported optional Character Development integration SHALL preserve its
existing **Help calm someone** want: with original Snap Out! absent and owned counselling available,
a helper who receives the helper memory can satisfy the want under that provider's existing rules.
The integration SHALL NOT change the provider's Social eligibility, reward amount or other wants,
and SHALL NOT pretend the original package is installed. If required content/behavior is unavailable
or changed, the affected integration SHALL stay inactive with a bounded startup warning.

Each enabled external patch SHALL be admitted only when its documented minimal behavior contract
holds. Compatible unrelated changes SHALL be allowed. Failed or ambiguous admission SHALL disable
the affected patch or atomic feature group, retain unrelated behavior and log one bounded startup
warning per skipped enabled patch stating unavailable behavior and fallback. Already queued owned
actions SHALL fail safely if their required behavior becomes unavailable. No migration of original
in-progress jobs/settings SHALL be silently performed.

#### Scenario: Original is co-installed
- **WHEN** original Snap Out! is active
- **THEN** it remains the only provider of overlapping counselling and helper rewards
- **AND** the owned feature reports its inactive state without duplicate work choices or definitions

#### Scenario: Character Development consumes a success
- **WHEN** the supported provider is installed without original Snap Out! and an eligible active Help calm someone want exists
- **THEN** receiving the successful helper memory satisfies that want through the provider's normal reward path
- **AND** failure or an interrupted conversation does not create that success memory

#### Scenario: Optional provider is absent or changed
- **WHEN** Character Development is absent or its relevant want/reward contract is incompatible
- **THEN** ordinary counselling remains available
- **AND** the optional integration is absent or safely inactive, with a warning for an enabled integration that fails admission

#### Scenario: Native compatibility changes
- **WHEN** an unrelated compatible game patch is present, or a required counselling/recovery contract is removed
- **THEN** the compatible case continues to work and the incompatible affected group remains inactive with a bounded diagnostic
- **AND** no foreign patch, unrelated mental state or saved order is removed to force compatibility
