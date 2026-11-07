# Wanderer offer details

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. S109; future observable behavior, not implementation acceptance.

## ADDED Requirements

### Requirement: Enrich identified offers without changing their event

The feature SHALL enhance ordinary walk-in joining offers and, when Royalty supplies them,
paralytic-abasia joining offers. It SHALL retain each offer's identity, original title and event
description, destination context, relationships, best-skill information, age warnings, charity
information, available navigation and decisions. It MUST NOT generate another pawn, another offer
or another event to supply the details. Other recruitment, visitor and emergency-arrival workflows
SHALL remain outside this feature unless separately specified and admitted.

#### Scenario: Ordinary walk-in offer
- **WHEN** the player opens an ordinary wanderer offer
- **THEN** the existing offer includes the prospective colonist's additional details and information action
- **AND** its original event context and native choices remain available without a second prompt.

#### Scenario: Abasia offer retains its warning
- **WHEN** the player opens a Royalty paralytic-abasia offer
- **THEN** the description still explains the actual illness and care situation rather than describing a healthy walk-in
- **AND** any existing navigation to the afflicted pawn remains available.

#### Scenario: Relationship or child context
- **WHEN** the native offer includes a colonist relationship, child warning, best skill or charity expectation
- **THEN** enrichment preserves that information without replacing it with a generic biography or duplicating it.

#### Scenario: Other joining workflows
- **WHEN** a man in black, end-of-colony arrival, visitor recruitment, refugee-pod event or rescued-pawn decision follows a different workflow
- **THEN** this feature does not intercept that workflow merely because someone might join
- **AND** separately specified rescuee consent retains its own rules.

### Requirement: Show ordinary visible pawn information

An active enhanced offer SHALL show the offered pawn's current, ordinarily visible work
restrictions, traits and passionate skills, distinguishing major passions. Empty known categories
SHALL say that there are none; unavailable information SHALL be identified as unavailable rather
than guessed. A More information action SHALL open that same pawn's ordinary information card
without accepting, rejecting, spawning, moving or altering the pawn. This enhancement MUST NOT
expose hidden conditions, future betrayal or other provider secrets through content, option labels,
extra warnings or differing layouts. Existing ordinary information visibility remains authoritative.

#### Scenario: Work restrictions and passions
- **WHEN** the offered pawn has visible work restrictions, traits and a mixture of minor and major passions
- **THEN** the offer lists the current restrictions and traits and identifies the passionate skills with their passion level.

#### Scenario: Empty or unavailable information
- **WHEN** a supported pawn has no visible traits or passions, or a category cannot be read safely
- **THEN** the category states None or Information unavailable as appropriate
- **AND** the native offer remains usable.

#### Scenario: Inspect and return
- **WHEN** the player opens More information and closes the information card
- **THEN** the card describes the offered pawn and the player can return to the same unresolved offer
- **AND** no membership decision, arrival or hidden event is triggered by inspection.

#### Scenario: Information changes while pending
- **WHEN** the pawn's ordinarily visible information changes before the player reopens the unresolved offer
- **THEN** the added details reflect the current pawn rather than a different pawn or stale cached biography
- **AND** refreshing details does not reset the deadline or duplicate text.

#### Scenario: Hidden traitor state
- **WHEN** an admitted provider offers an otherwise ordinary-looking pawn with hidden betrayal state
- **THEN** added details and controls follow the same ordinary visibility rules as for a non-traitor
- **AND** they neither reveal nor remove or modify that hidden state.

### Requirement: Native decisions keep their distinct consequences

Accept, Reject and expiry SHALL retain the event's native or admitted provider consequences and
eligibility. Enrichment MUST NOT add a second recruitment action, change joining probability,
generate free colonists, cure illness or erase native refusal and charity consequences. An ordinary
walk-in remains outside the colony until native acceptance brings the offered pawn in. An abasia
pod can arrive before consent; accepting changes membership through the existing event, not the
pawn's need for care. A resolved decision MUST NOT become actionable again because the details
were opened, refreshed or restored.

#### Scenario: Accept a walk-in
- **WHEN** the player accepts an eligible ordinary walk-in offer
- **THEN** the existing event brings the same offered pawn into the colony through its normal arrival path
- **AND** only that event's membership and success consequences occur.

#### Scenario: Reject a walk-in
- **WHEN** the player rejects the offer
- **THEN** the event follows its native refusal outcome without an owned arrival or recruitment
- **AND** native denial thoughts and applicable charity consequences are preserved, with no additional owned penalty.

#### Scenario: Afflicted pawn is already arriving
- **WHEN** the abasia event sends its pod before the player has accepted
- **THEN** the pod and afflicted pawn remain part of the existing event, including their ordinary care needs
- **AND** the information enhancement does not hold them off-map pending the answer.

#### Scenario: Accept or reject an afflicted pawn
- **WHEN** the player answers an abasia offer
- **THEN** acceptance or refusal follows that event's existing membership and quest consequences
- **AND** the enhancement neither cures the pawn, automatically rescues them nor destroys or teleports them on refusal.

#### Scenario: Destination or eligibility changes
- **WHEN** the original destination disappears or the native event no longer permits acceptance in the current circumstances
- **THEN** existing native eligibility and any native destination recovery remain authoritative
- **AND** added details do not enable a forbidden acceptance or falsely assert that an obsolete destination is guaranteed.

#### Scenario: Repeated inspection after resolution
- **WHEN** the player revisits a resolved or archived offer
- **THEN** the enhancement cannot accept or reject it again, repeat rewards or create another arrival
- **AND** any retained information is read-only; a missing pawn has no actionable information control.

### Requirement: Preserve the original deadline and postponement

Enhanced offers SHALL keep the original remaining time, native postponement, final warning and
expiry. The inspected Core walk-in and Royalty abasia letters allow **60,000 game ticks, one game
day**, from their original creation; the enhancement SHALL inherit a supported event's actual
deadline, including admitted provider changes, rather than start a new day when opened or loaded.
It MUST NOT introduce the separate rescuee feature's untimed forced-choice policy. Expiry SHALL
remain the native event's expiry outcome, not an invented acceptance or an extra rejection.

#### Scenario: Postpone and reopen
- **WHEN** the player postpones an unresolved offer and later reopens it
- **THEN** the original offer is still present only while natively valid and shows its remaining time
- **AND** reopening does not restart its deadline or create a second letter.

#### Scenario: Save and reload partway through the day
- **WHEN** the player saves a pending offer partway through its deadline and reloads that save
- **THEN** its remaining game time, offered pawn, original event and decision state remain the same
- **AND** no consent, rejection or fresh duration is inferred from loading.

#### Scenario: Deadline is reached
- **WHEN** the native final warning or expiry occurs
- **THEN** the native final opportunity, restriction on postponement and eventual expiry remain intact
- **AND** enrichment does not keep the offer actionable afterward or send an additional refusal.

#### Scenario: Provider changes offer duration
- **WHEN** an admitted provider supplies a different valid deadline
- **THEN** the enhanced offer preserves that deadline and displays its real remaining time.

### Requirement: Contextual controls and localized presentation

Details and More information SHALL live in the existing offer and ordinary information card.
Native Accept and Reject SHALL remain clearly identifiable, with More information placed after
them and before ordinary postpone/navigation controls in the standard active layout. Additional
provider controls SHALL retain their purpose and order. Long content SHALL remain readable and
all actions reachable. The feature SHALL introduce no bottom tab, bottom-right toggle, top debug
control or configuration setting; offer choices are pawn/event state. Owned text SHALL support
English, German, Spanish, French, Simplified Chinese, Russian and Japanese without English-only
concatenations or forced casing of translated names.

#### Scenario: Standard active letter layout
- **WHEN** the ordinary enhanced letter is displayed
- **THEN** Accept, Reject and More information are distinct, and native postpone/navigation remains reachable
- **AND** opening an information card cannot be confused with accepting the pawn.

#### Scenario: Long translated information
- **WHEN** a supported language, long trait list, long pawn name or narrow view makes the letter lengthy
- **THEN** the full information can be read and the decision controls remain usable without overlapping content.

#### Scenario: No game is open
- **WHEN** the player opens the main menu or mod settings without a running game
- **THEN** this feature exposes no pawn-specific choice or editable save setting there.

### Requirement: Optional content retains its own authority

Core offers SHALL work without Royalty, Ideology, Biotech, Vanilla Events Expanded or Humanoid
Alien Races. Available optional content SHALL retain its ordinary context and visible pawn
identity without gaining an invented species category or hidden-information display. Support for
Vanilla Events Expanded's traitor offer SHALL be confined to an admitted version of that actual
event and SHALL preserve its original pawn, hidden behavior and later consequences. The feature
MUST NOT create or repair an external event by secretly substituting another generator. Unsupported
optional routes SHALL retain their provider workflow with the enhancement unavailable. When an
active hidden-event provider uses offers ordinarily indistinguishable from Core walk-ins, failure
to support that route SHALL withdraw the added summary and information action from all comparable
walk-in offers as one presentation group. This uniform fallback SHALL cover existing and future
offers, reopening and reload, and later loss of compatibility. Distinct abasia offers need not lose
their independent enhancement. Diagnostics MUST NOT identify a concealed pawn through this fallback.

#### Scenario: Core-only game
- **WHEN** only Core and the required implementation dependencies are active
- **THEN** ordinary walk-in offers can be enhanced without optional-content errors or phantom abasia/traitor offers.

#### Scenario: Royalty or Ideology is present
- **WHEN** Royalty supplies the abasia event or Ideology supplies charity consequences
- **THEN** the enhanced event preserves that content's normal care and belief consequences
- **AND** removing an absent optional provider from consideration does not disable Core offers.

#### Scenario: Admitted traitor event
- **WHEN** an admitted Vanilla Events Expanded traitor offer is inspected and accepted
- **THEN** the event's original pawn and concealed future behavior remain intact through inspection and acceptance
- **AND** the added information gives no special betrayal warning or immunity.

#### Scenario: Unproven optional route
- **WHEN** the installed optional event cannot be safely identified or its required behavior is unavailable
- **THEN** the affected enhancement is skipped with the startup diagnostic required below
- **AND** no replacement event, hidden condition or second pawn is fabricated.

#### Scenario: Unavailable traitor route does not identify a traitor
- **WHEN** the active traitor provider's enhancement is unavailable at startup or becomes unavailable after a supported later change
- **THEN** ordinary and concealed walk-in offers use the same native presentation without the added summary or information action
- **AND** that fallback also applies when an already-pending offer is reopened or reloaded, without changing its event, choices or deadline
- **AND** no pawn-specific diagnostic reveals which offer has concealed state; independent abasia details remain available when compatible.

#### Scenario: Original provider is also active
- **WHEN** Ogliss.WanderJoinsPlus is active
- **THEN** the owned overlapping enhancement remains inactive with its startup explanation
- **AND** the original provider is neither removed nor altered and no duplicate information action is added.

### Requirement: Save boundaries and missing information remain honest

Owned enhancement state SHALL retain only its own unresolved-offer association and presentation
needs, respect native event/pawn lifetime, and never turn archived data into a new decision. A lost
or ambiguous association SHALL omit or disable the added action with an explanation rather than
inspect a guessed pawn. Switching games SHALL not reuse another game's offer. Automatic conversion
of saved Wander Joiner Plus letters is outside this change: documentation SHALL identify those
legacy saves as unsupported for provider replacement, without claiming that removing the source
is safe or that merely installing megamod repairs them.

#### Scenario: Two concurrent offers
- **WHEN** offers for different pawns or maps are open or pending together
- **THEN** each added summary and information action stays with its own pawn and event through refresh and reload.

#### Scenario: Missing association
- **WHEN** the offered pawn or the unambiguous offer association is unavailable
- **THEN** the added details/action state that information is unavailable or are omitted with an explanation
- **AND** the feature does not choose a colonist, another offer's pawn or a stale pawn from another game.

#### Scenario: Legacy source save
- **WHEN** a player considers replacing Wander Joiner Plus in a save containing its custom letters
- **THEN** the replacement documentation identifies the unsupported saved-letter contract and absence of automatic migration
- **AND** does not promise that removing the source, resolving pending offers or installing megamod alone makes that save safe.

### Requirement: Failed compatibility preserves the existing offer

Each enabled enhancement patch SHALL require its documented semantic target conditions before
application. Unrelated changes and compatible foreign patches SHALL be allowed. Missing,
ambiguous or changed required behavior, check failure or installation failure SHALL leave only
the affected patch or declared feature group inactive and emit one bounded startup warning per
skipped enabled patch identifying the owning package, patch/spec, target, failed condition,
unavailable behavior and fallback. Repeated initialization MUST NOT duplicate text, choices or
warnings. A later supported compatibility change SHALL suspend the affected enhancement until
revalidation; it MUST NOT remove foreign changes, recreate offers or reset deadlines. Failure of
added information SHALL leave native decisions available to the extent their original event permits.

#### Scenario: Compatible foreign change
- **WHEN** another mod changes unrelated letter details while the required offer behavior remains valid
- **THEN** the enhancement can coexist and preserve that mod's content and controls.

#### Scenario: Required behavior cannot be established
- **WHEN** a required target is missing, changed, ambiguous or throws during checking or installation
- **THEN** the affected enhancement stays inactive and the startup log explains the missing information feature and native/provider fallback
- **AND** no partly replaced letter or duplicated decision is left behind.

#### Scenario: Repeated startup or later change
- **WHEN** registration repeats or a supported later modification invalidates the admitted behavior
- **THEN** the feature adds no duplicate enrichment or warnings and withdraws only its affected enhancement until revalidation
- **AND** existing event state, deadlines and foreign behavior are preserved.
