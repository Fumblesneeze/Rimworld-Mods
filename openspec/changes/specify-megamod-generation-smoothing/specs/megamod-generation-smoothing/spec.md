## ADDED Requirements

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **original**, S078/P2-07. This is a future observable-behavior draft, not implemented behavior.

Confirmed: distribute preparation, reveal complete raids/traders together, include maps/encounters.
Ordinary simulation continues during background preparation of live incidents and destinations.
GS-Q1 retains measured scheduling/capacity/overload research, without a numeric completion deadline.
The user's rough1–2-second estimate is real elapsed time, not a limit, timeout, cancellation rule or
code/test assertion. GS-Q2–6 track other research and acceptance gates in refinement.

### Requirement: Preparation leaves opportunities to interact

For supported raids and trader caravans, the mod SHALL distribute substantial preparation across
ordinary play updates so the player can pan, select, inspect and pause between portions of the work.
For supported new maps and encounters requested during an active game, ordinary simulation SHALL
continue during background preparation, respecting the player's chosen pause/speed state. Initial
colony creation without a running simulation SHALL retain responsive loading. Completing the
operation SHALL remain necessary: a missing, weakened or indefinitely postponed event is not a
responsiveness improvement.

#### Scenario: Large group is prepared during play
- **WHEN** a supported large raid or trader caravan is being prepared
- **THEN** the player can operate the existing camera, selection and pause controls between portions of preparation
- **AND** the complete intended group subsequently becomes available through its ordinary arrival

#### Scenario: One expensive operation dominates preparation
- **WHEN** preparing one pawn, inventory, map stage or final arrival is responsible for the interruption
- **THEN** delaying delivery of already prepared results alone does not satisfy this requirement
- **AND** that path remains unaccepted until the actual interruption is reduced without removing required work

### Requirement: Arrivals become visible as complete groups

The mod SHALL keep a preparing group unavailable for combat, trading, selection and ordinary
search results until all its required members, equipment, goods and initial behavior are ready.
It SHALL publish one complete arrival without a playable interval containing only a prepared
subset. It SHALL NOT reveal future members or new relationships through other player information
before the corresponding arrival. Native arrival animations, pod opening delays and deliberate
multi-wave events SHALL remain distinct from preparation and retain their ordinary behavior.

#### Scenario: Only some traders and goods are ready
- **WHEN** the trader has been prepared but its required escorts, carriers or stock are still being prepared
- **THEN** none of that arrival can be selected, attacked or traded with yet
- **AND** the arrival becomes usable only with the complete intended group and stock

#### Scenario: Raiders are partially ready
- **WHEN** some raiders are ready and others are still being prepared
- **THEN** colonists cannot fight that subset and no subset starts its attack early
- **AND** the full prepared group enters its intended arrival behavior together

#### Scenario: The intended arrival uses drop pods
- **WHEN** a complete supported raid is ready to arrive in drop pods
- **THEN** all required pod contents are ready before that arrival is published
- **AND** the normal pod travel/opening behavior still determines when occupants emerge

#### Scenario: Searching while preparation is pending
- **WHEN** the player searches for items or views a known pawn's relationships during preparation
- **THEN** unpublished cargo, new arrivals and their newly generated relationships do not expose the pending event
- **AND** existing published objects and relationships remain available normally

### Requirement: Smoothing preserves the intended encounter

Preparation SHALL preserve the event's intended faction, threat budget, group composition rules,
stock and loot rules, gear, relationships, strategy, arrival mode and destination. It SHALL NOT
reduce numbers or quality, omit difficult content, force every visitor to be a new pawn, change
native probabilities or alter the intended destination merely to finish faster. Decisions already
made for a pending operation SHALL NOT be rerolled by changing speed, pausing or saving. The feature
does not promise that unrelated future random events match a game in which it was absent.

#### Scenario: A large raid takes more preparation
- **WHEN** the intended raid requires more work than a small raid
- **THEN** its intended strength, equipment, loot and strategy remain governed by the same gameplay rules
- **AND** preparation does not turn it into a smaller or weaker raid to meet a time allowance

#### Scenario: A known world pawn is selected
- **WHEN** ordinary generation selects an eligible existing world pawn for an arrival
- **THEN** the arriving pawn retains the appropriate existing identity and history
- **AND** preparation does not replace that pawn with an unrelated newly generated substitute

#### Scenario: Pause or speed changes after a choice
- **WHEN** a pending arrival already has a chosen faction, membership or goods and the player changes speed or pauses
- **THEN** those choices remain the same when preparation resumes

### Requirement: Arrival outcomes and ordering remain truthful

Each admitted occurrence SHALL complete, fail or be cancelled once. Raid, caravan-ambush and
hostile-map entry notices/cards SHALL appear only after required generation is complete and its
source and intent remain valid. A successful publication SHALL deliver its appropriate arrival notice,
relationships notice, quest consequences, trade/combat behavior and event history. Pending work
SHALL NOT count as a successfully completed arrival or become a second attempt merely because
preparation spans updates. Required ordering between events SHALL be retained, including native
retry eligibility and expiry; the feature SHALL NOT silently reset retries or let a dependent
event proceed as though unfinished work succeeded.

#### Scenario: A queued incident needs several updates
- **WHEN** an eligible occurrence is preparing across several updates
- **THEN** only one preparation for that occurrence is active
- **AND** no notice/card reveals it before complete generation; a still-valid result and its notices are delivered once, without repeated groups or false completion

#### Scenario: A quest depends on generation
- **WHEN** a quest operation requires the outcome of a raid, trader or destination operation
- **THEN** it receives the real completed outcome at the appropriate point
- **AND** unsupported callers retain their existing generation rather than receiving a false success

#### Scenario: Two events have an ordering dependency
- **WHEN** a later event depends on an earlier arrival or destination becoming available
- **THEN** faster preparation of the later event does not make it overtake that prerequisite
- **AND** unrelated events retain their native scheduling rules rather than receiving invented priority

### Requirement: Player pause and speed choices remain effective

The mod SHALL preserve the player's chosen pause/speed state and native event-driven speed changes.
Preparation SHALL NOT secretly advance paused simulation, undo a pause request or grant extra
simulation steps merely to finish. A live incident still awaiting ordinary arrival when the player
pauses SHALL remain unrevealed until ordinary event progression is permitted again. Native paused
loading and explicitly invoked developer actions retain their distinct workflows.

#### Scenario: Player pauses a pending arrival
- **WHEN** the player pauses ordinary play before a pending live arrival is published
- **THEN** the colony remains paused and the event does not arrive while that pause remains in effect
- **AND** resuming permits the same pending occurrence to continue

#### Scenario: High speed is selected
- **WHEN** the player changes from normal to a faster supported game speed during preparation
- **THEN** the same complete arrival and event accounting are retained
- **AND** smoothing does not silently change the speed choice, apart from the ordinary arrival's own speed rules

### Requirement: Saving and leaving retain one valid outcome

A successful manual save or autosave during pending work SHALL retain the operation and its already
made choices, without saving a partly playable arrival or destination. Loading that save SHALL
produce the same still-valid pending, cancelled or completed occurrence once; it SHALL NOT revive an operation cancelled because its source or intent ceased to exist. A save SHALL NOT report success while
silently discarding pending work. Leaving with a successful save SHALL retain this guarantee;
leaving without saving SHALL NOT publish abandoned work into another game. The feature SHALL NOT
force an early visible arrival or advance game time solely to satisfy a save request.

#### Scenario: Save while an arrival is pending
- **WHEN** a manual save or autosave succeeds before the complete arrival is published
- **THEN** loading it retains the same pending occurrence and prior choices
- **AND** its later completion produces one group and one set of appropriate consequences

#### Scenario: Save after publication
- **WHEN** the player saves after the complete arrival is published
- **THEN** loading retains that ordinary arrival and does not start its preparation again

#### Scenario: Saving cannot complete safely
- **WHEN** the game cannot safely retain a pending operation in the requested save
- **THEN** it clearly reports the failed save without replacing the previous valid save with a partial result
- **AND** the current game retains a coherent operation for recovery rather than silently losing or rerolling it

#### Scenario: Leave one game and load another
- **WHEN** the player leaves without saving pending preparation and opens another save or a new game
- **THEN** no abandoned group, destination, notice or event consequence appears in that game

### Requirement: Invalid or failed preparation leaves coherent gameplay

Before publication the mod SHALL honor the actual event's target and eligibility rules, including
its forced/quest exceptions. If the source map was abandoned, the initiating caravan no longer
exists, or its enter-map target/intent changed, the prepared result SHALL be discarded and that
occurrence cancelled without a success notice, entry card, success history or replacement destination.
Only newly prepared temporary work owned by that occurrence may be discarded; existing world pawns,
player property and borrowed objects SHALL remain intact and accounted for. Other invalid or failed
operations SHALL retain their appropriate failure/retry outcome without inventing an event. Failed or cancelled
preparation SHALL NOT leave half an arrival, duplicate goods, damaged or missing pre-existing
world pawns, invented relationships or a destination that appears usable but is incomplete.
After publication, normal gameplay owns the arrival; the mod SHALL NOT replay it as recovery.

#### Scenario: Target map disappears
- **WHEN** the target map is removed before a prepared occurrence can arrive there
- **THEN** it cannot publish onto the removed map or an unrelated replacement map
- **AND** that pending occurrence is cancelled once and its temporary prepared result discarded, with no retargeting or success notice

#### Scenario: Caravan disappears or changes its entry target
- **WHEN** generation finishes after the initiating caravan ceased to exist or no longer intends to enter the requested map
- **THEN** its temporary result is discarded without a raid/ambush/entry notice, phantom successful event or transfer to a different destination
- **AND** existing pawns and player property remain accounted for rather than being deleted as temporary generation output.

#### Scenario: Faction conditions change
- **WHEN** a pending event's faction or quest state changes before publication
- **THEN** the event applies its own actual eligibility and forced-event exceptions
- **AND** the feature neither sends an invalid peaceful trader nor cancels a valid forced event through a broader invented rule

#### Scenario: Preparation fails after selecting a known pawn
- **WHEN** preparation cannot complete after choosing an existing world pawn
- **THEN** cleanup does not destroy, lose or partially redress that pawn
- **AND** the failed arrival leaves no new cargo, relationship or success notice behind

#### Scenario: Recovery follows a completed arrival
- **WHEN** a later unrelated failure occurs after the arrival was published successfully
- **THEN** recovery does not generate or announce that occurrence again

### Requirement: New destinations are complete before entry

New colony maps and encounter destinations SHALL retain their intended size, terrain, resources,
structures, inhabitants, hazards and other enabled content. During an active game their preparation
SHALL happen in the background while ordinary simulation continues, subject to player pause.
Initial colony creation SHALL remain responsive without inventing a running colony. Players, caravans and transport contents
SHALL enter only the finalized destination; they SHALL NOT be duplicated, lost or placed on a
partly generated map. Existing destinations SHALL be reused normally rather than regenerated.

#### Scenario: Caravan enters a new encounter
- **WHEN** a caravan reaches a supported site that needs a new map
- **THEN** ordinary simulation continues while the full destination is prepared in the background, with no early entry notice/card
- **AND** the caravan enters the finished map once, with the appropriate entry notice, drafting and quest/diplomacy consequences

#### Scenario: A destination is already generated
- **WHEN** a caravan or other supported arrival enters an existing destination
- **THEN** its existing terrain, objects and inhabitants remain intact
- **AND** smoothing does not regenerate it or repeat first-entry consequences

#### Scenario: Map preparation fails
- **WHEN** a new destination cannot be completed
- **THEN** the player receives a clear failure and no partly generated destination becomes playable
- **AND** the original caravan or transport contents remain accounted for in a recoverable state

#### Scenario: Different preparation schedules generate the same requested map
- **WHEN** the same supported map request with the same fixed generation choices is divided into different portions
- **THEN** its seeded terrain, content and generation ordering do not change merely because portions were scheduled differently

### Requirement: Progress belongs to the current workflow

The feature SHALL use the existing loading/play context for necessary progress and failures. It
SHALL NOT add a bottom tab, a future-raid/trader preview or a new button that forces events to finish.
Progress SHALL describe completed stages honestly rather than displaying an invented completion
percentage or guaranteed time remaining. New player-facing text SHALL be available in all seven
baseline languages. Technical startup diagnostic identifiers SHALL remain invariant.

No new setting or default is selected in this draft. If GS-Q1 results in actual configuration,
its exact values, effect timing and validation SHALL be added to this contract first, using the
common Global default / Current save / Use global controls. Main-menu save controls SHALL remain
disabled. A particular pending operation is gameplay state, not a mod setting.

#### Scenario: Loading progress is visible
- **WHEN** the player waits for a supported destination to be prepared
- **THEN** the existing loading context identifies the current preparation stage without promising an unsupported percentage or duration
- **AND** no additional bottom tab or settings window is needed to enter the destination

#### Scenario: Timing configuration is later introduced
- **WHEN** an approved specification amendment adds a timing setting
- **THEN** it uses the shared inheritance and persistence behavior, with inactive per-save editing while Use global is checked or no game is loaded
- **AND** it cannot silently change or reroll an already admitted operation

### Requirement: Unsupported generation keeps its existing authority

Smoothing SHALL activate only where its required generation, arrival and lifecycle conditions are
satisfied. Compatible external content and harmless changes SHALL remain allowed. Where conditions
are unknown or incompatible, the affected path SHALL retain its existing provider's behavior;
other supported paths SHALL remain usable. A skipped enabled patch SHALL emit one bounded startup
warning identifying the patch, target condition, unavailable behavior and fallback. It SHALL NOT
remove another provider's behavior or claim that all foreign generation paths are supported.

#### Scenario: Compatible content is present
- **WHEN** an optional content provider preserves the required generation and completion behavior
- **THEN** its eligible content participates without being omitted merely for being modded

#### Scenario: A required generation condition changes
- **WHEN** a required target condition is absent, ambiguous or no longer true
- **THEN** the affected smoothing contribution stays inactive with its bounded startup warning
- **AND** the existing generation path remains responsible for that operation, without a partly installed replacement

#### Scenario: Another provider changes the path later
- **WHEN** a later provider change invalidates the conditions required by an admitted smoothing path
- **THEN** new work on that path stops using smoothing and unfinished work is safely resolved without duplication
- **AND** the mod preserves other providers and reports the affected behavior once rather than on every update

### Requirement: Responsiveness claims include completed gameplay

Acceptance SHALL demonstrate real interaction during preparation and successful complete outcomes
for raids, trading and new-map entry. It SHALL retain the largest interruption, overall completion
time and in-game arrival delay alongside ordinary work continuing correctly. A smoother average
with a hidden final stall, an absent event or unbounded pending work SHALL NOT be accepted. The
scheduling and overload policy researched in GS-Q1 SHALL preserve these outcomes. Measurements
SHALL record real elapsed time separately from simulation time, without treating the user's rough
1–2-second estimate as an assertion, deadline or acceptance threshold. This draft makes no numeric speedup claim.

#### Scenario: Performance appears improved because work was omitted
- **WHEN** preparation measurements look faster but a group, goods, loot, map content or completion consequence is missing
- **THEN** the feature fails acceptance despite the faster measurement

#### Scenario: Final publication remains a large interruption
- **WHEN** early preparation is responsive but the complete arrival or final map handoff still causes the original interruption
- **THEN** the path remains unaccepted until the remaining interruption is addressed and the full workflow is reverified
